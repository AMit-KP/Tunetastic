using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using static Tunetastic.Common.Services.TaskbarOverlay.NativeMethods;

namespace Tunetastic.Common.Services.TaskbarOverlay;

/// <summary>Windows 11 taskbar icon alignment (HKCU\…\Explorer\Advanced\TaskbarAl).</summary>
internal enum TaskbarAlignment { Left, Center }

/// <summary>
/// Live snapshot of where the actual taskbar UI elements are sitting right now.
/// All coordinates are in physical screen pixels.
/// </summary>
internal readonly record struct TaskbarIconLayout(
	int TaskbarLeftScreen,
	int TaskbarRightScreen,
	int TrayLeftScreen,
	int IconsLeftScreen,
	int IconsRightScreen,
	bool IconsFound,
	TaskbarAlignment Alignment,
	bool WidgetsEnabled);

/// <summary>Immutable snapshot of a single taskbar's geometry and monitor info.</summary>
internal sealed record TaskbarInfo(
	IntPtr Hwnd,
	IntPtr HMonitor,
	string MonitorDeviceName,
	RECT MonitorBounds,
	RECT MonitorWorkArea,
	RECT TaskbarBoundsScreen,
	ABE Edge,
	uint Dpi,
	bool IsPrimary);

internal sealed class TaskbarDiscoveryService
{
	public IReadOnlyList<TaskbarInfo> Discover()
	{
		var results = new List<TaskbarInfo>();

		// Primary taskbar
		IntPtr primary = FindWindow("Shell_TrayWnd", null);
		if (primary == IntPtr.Zero)
			return results;

		var info = BuildTaskbarInfo(primary, isPrimary: true);
		if (info is not null) results.Add(info);

		// Secondary taskbars (multi-monitor)
		IntPtr prev = IntPtr.Zero;
		while (true)
		{
			IntPtr secondary = FindWindowEx(IntPtr.Zero, prev, "Shell_SecondaryTrayWnd", null);
			if (secondary == IntPtr.Zero) break;
			var secondaryInfo = BuildTaskbarInfo(secondary, isPrimary: false);
			if (secondaryInfo is not null) results.Add(secondaryInfo);
			prev = secondary;
		}

		return results;
	}

	private static TaskbarInfo? BuildTaskbarInfo(IntPtr hwnd, bool isPrimary)
	{
		if (!GetWindowRect(hwnd, out RECT taskbarRect)) return null;

		IntPtr hMon = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
		if (hMon == IntPtr.Zero) return null;

		var monInfo = new MONITORINFOEX { cbSize = (uint)Marshal.SizeOf<MONITORINFOEX>() };
		if (!GetMonitorInfo(hMon, ref monInfo)) return null;

		uint dpi = GetDpiForWindow(hwnd);
		if (dpi == 0) dpi = 96;

		ABE edge = isPrimary ? GetPrimaryEdge(hwnd, taskbarRect) : InferEdge(taskbarRect, monInfo.rcMonitor);

		return new TaskbarInfo(
			Hwnd: hwnd,
			HMonitor: hMon,
			MonitorDeviceName: monInfo.szDevice,
			MonitorBounds: monInfo.rcMonitor,
			MonitorWorkArea: monInfo.rcWork,
			TaskbarBoundsScreen: taskbarRect,
			Edge: edge,
			Dpi: dpi,
			IsPrimary: isPrimary);
	}

	private static ABE GetPrimaryEdge(IntPtr hwnd, RECT taskbarRect)
	{
		var data = new APPBARDATA
		{
			cbSize = (uint)Marshal.SizeOf<APPBARDATA>(),
			hWnd = hwnd,
		};
		SHAppBarMessage((uint)ABM.GetTaskbarPos, ref data);
		return (ABE)data.uEdge;
	}

	private static ABE InferEdge(RECT bar, RECT monitor)
	{
		if (bar.Bottom == monitor.Bottom && bar.Left == monitor.Left && bar.Right == monitor.Right)
			return ABE.Bottom;
		if (bar.Top == monitor.Top && bar.Left == monitor.Left && bar.Right == monitor.Right)
			return ABE.Top;
		if (bar.Left == monitor.Left)
			return ABE.Left;
		return ABE.Right;
	}
}

/// <summary>Overlay window position in physical screen pixels.</summary>
internal sealed record OverlayRect(int ScreenX, int ScreenY, int Width, int Height);

/// <summary>Drag-clamp boundaries in physical screen pixels.</summary>
internal sealed record FreeZone(int ScreenLeft, int ScreenRight);

internal sealed class TaskbarLayoutService
{
	/// <summary>
	/// Computes the overlay rect and active free zone for the given taskbar.
	/// Returns null when there is not enough room to show the overlay.
	/// Probes live every call: tray drawer left edge and icon cluster right edge
	/// are read from the actual shell windows on each invocation, so the overlay
	/// follows the taskbar as apps open/close and the tray expands/contracts.
	/// </summary>
	public (OverlayRect rect, FreeZone zone)? Compute(
		TaskbarInfo tb,
		int widthPx,
		int marginPx,
		OverlaySide side,
		Dictionary<string, int> draggedPositions)
	{
		TaskbarIconLayout layout = ProbeIconLayout(tb);
		List<FreeZone> zones = ComputeFreeZones(layout, marginPx, tb.Dpi);

		if (zones.Count == 0) return null;

		bool hasDragged = draggedPositions.TryGetValue(tb.MonitorDeviceName, out int savedX);

		// widthPx is content width in DIPs; convert to physical pixels for comparison.
		double scale = tb.Dpi / 96.0;
		int widthPhysical = (int)Math.Round(widthPx * scale);

		FreeZone activeZone = SelectActiveZone(zones, side, hasDragged, savedX, widthPhysical);

		int zoneWidth = activeZone.ScreenRight - activeZone.ScreenLeft;
		if (zoneWidth < 40) return null;

		int width = Math.Min(widthPhysical, zoneWidth);
		int height = tb.TaskbarBoundsScreen.Height;

		// CRITICAL: clamp so the overlay's RIGHT edge stays inside the zone, not just its left.
		// This prevents the overlay from spilling over the tray drawer when it grows.
		int maxX = activeZone.ScreenRight - width;
		int minX = activeZone.ScreenLeft;
		int x;
		if (hasDragged)
			x = Math.Clamp(savedX, minX, maxX);
		else if (side == OverlaySide.Right)
			x = maxX;
		else
			x = minX;

		int y = tb.TaskbarBoundsScreen.Top;

		return (new OverlayRect(x, y, width, height), activeZone);
	}

	// ── Probing ────────────────────────────────────────────────────────────

	private static TaskbarIconLayout ProbeIconLayout(TaskbarInfo tb)
	{
		int trayLeftClient = FindTrayLeftClient(tb);
		bool iconsFound = TryFindIconsBounds(tb, out int iconsLeftClient, out int iconsRightClient);
		TaskbarAlignment alignment = ReadTaskbarAlignment();
		bool widgetsEnabled = ReadWidgetsEnabled();

		int taskbarLeft = tb.TaskbarBoundsScreen.Left;
		return new TaskbarIconLayout(
			TaskbarLeftScreen: taskbarLeft,
			TaskbarRightScreen: tb.TaskbarBoundsScreen.Right,
			TrayLeftScreen: taskbarLeft + trayLeftClient,
			IconsLeftScreen: taskbarLeft + iconsLeftClient,
			IconsRightScreen: taskbarLeft + iconsRightClient,
			IconsFound: iconsFound,
			Alignment: alignment,
			WidgetsEnabled: widgetsEnabled);
	}

	/// <summary>Left edge of the system tray drawer (TrayNotifyWnd) in taskbar client coords.</summary>
	private static int FindTrayLeftClient(TaskbarInfo tb)
	{
		IntPtr trayHwnd = FindWindowEx(tb.Hwnd, IntPtr.Zero, "TrayNotifyWnd", null);
		if (trayHwnd != IntPtr.Zero && GetWindowRect(trayHwnd, out RECT trayScreen))
		{
			var pt = new POINT { X = trayScreen.Left, Y = trayScreen.Top };
			ScreenToClient(tb.Hwnd, ref pt);
			return pt.X;
		}

		// Fallback: assume the last 100px of the taskbar is the tray area.
		if (GetClientRect(tb.Hwnd, out RECT client))
			return client.Width - 100;

		return 0;
	}

	/// <summary>
	/// Returns the live horizontal bounds of the taskbar icon cluster
	/// (Start button + pinned/running apps + widgets) in <c>Shell_TrayWnd</c>
	/// client coordinates.
	///
	/// On Windows 11 the real icon HWNDs (Start, MSTaskListWClass via
	/// ReBarWindow32 → MSTaskSwWClass) report correct screen rects, but
	/// <c>IsWindowVisible</c> often returns false for them because the
	/// shell paints icons through a composition surface rather than a
	/// classic GDI surface. We therefore trust the rect of these known
	/// shell classes regardless of visibility, and union them in.
	///
	/// Anything we don't recognize (e.g. <c>DesktopWindowContentBridge</c>
	/// that spans the entire bar) is ignored so it can't poison the union.
	/// </summary>
	private static bool TryFindIconsBounds(TaskbarInfo tb, out int leftClient, out int rightClient)
	{
		leftClient = 0;
		rightClient = 0;

		if (!GetClientRect(tb.Hwnd, out RECT tbClient)) return false;
		int tbClientWidth = tbClient.Width;
		if (tbClientWidth <= 0) return false;

		// Tray drawer rect in screen coords — strict right boundary.
		IntPtr trayHwnd = FindWindowEx(tb.Hwnd, IntPtr.Zero, "TrayNotifyWnd", null);
		bool hasTray = trayHwnd != IntPtr.Zero && GetWindowRect(trayHwnd, out _);
		RECT trayRect = default;
		if (hasTray) GetWindowRect(trayHwnd, out trayRect);

		var u = new IconUnion();
		EnumerateIconChildren(tb.Hwnd, hasTray, trayRect, u);

		if (u.Left == int.MaxValue || u.Right == int.MinValue || u.Right <= u.Left)
			return false;

		// Clip to tray-left so a stray wide rect can't extend past the drawer.
		if (hasTray && u.Right > trayRect.Left)
			u.Right = trayRect.Left;

		var ptL = new POINT { X = u.Left, Y = tb.TaskbarBoundsScreen.Top };
		var ptR = new POINT { X = u.Right, Y = tb.TaskbarBoundsScreen.Top };
		ScreenToClient(tb.Hwnd, ref ptL);
		ScreenToClient(tb.Hwnd, ref ptR);

		leftClient = Math.Max(0, ptL.X);
		rightClient = Math.Min(tbClientWidth, ptR.X);
		return rightClient > leftClient;
	}

	/// <summary>Mutable holder used to thread the union through closures.</summary>
	private sealed class IconUnion
	{
		public int Left = int.MaxValue;
		public int Right = int.MinValue;
	}

	// Shell window classes whose rect always represents real icon area, even
	// if IsWindowVisible says otherwise (Win11 paints them via composition).
	private static readonly HashSet<string> IconClasses = new(StringComparer.OrdinalIgnoreCase)
	{
		"Start",
		"TrayDummySearchControl",
		"MSTaskListWClass",
		"MSTaskSwWClass",
		"TrayButton",
		"Taskbar.TaskListButton",
		"Taskbar.TaskListButtonExperience",
		"WidgetIconBackgroundContextWrapper",
		"TrayShowDesktopButtonWClass",
	};

	// Shell containers that wrap icon hosts; we recurse into them but do not
	// count their own rect (they often span the whole taskbar).
	private static readonly HashSet<string> ContainerClasses = new(StringComparer.OrdinalIgnoreCase)
	{
		"ReBarWindow32",
		"MSTaskSwWClass",                                  // also recursable
        "Windows.UI.Composition.DesktopWindowContentBridge",
		"Windows.UI.Input.InputSite.WindowClass",
		"WorkerW",
	};

	/// <summary>
	/// Walks direct children of <paramref name="parent"/>, unions the rects of
	/// known icon hosts (regardless of IsWindowVisible — Win11 lies about it),
	/// recurses into known shell containers, and ignores everything else.
	/// </summary>
	private static void EnumerateIconChildren(
		IntPtr parent,
		bool hasTray,
		RECT trayRect,
		IconUnion union)
	{
		var sb = new StringBuilder(256);

		bool Cb(IntPtr child, IntPtr _)
		{
			sb.Clear();
			GetClassName(child, sb, sb.Capacity);
			string cls = sb.ToString();

			// Never recurse into / count the tray drawer.
			if (string.Equals(cls, "TrayNotifyWnd", StringComparison.OrdinalIgnoreCase))
				return true;

			bool isIcon = IconClasses.Contains(cls);
			bool isContainer = ContainerClasses.Contains(cls);

			if (!isIcon && !isContainer)
				return true;

			// Recurse into shell containers (their own rect is often the entire bar).
			if (isContainer)
				EnumerateIconChildren(child, hasTray, trayRect, union);

			if (!isIcon) return true;

			if (!GetWindowRect(child, out RECT r)) return true;
			if (r.Width <= 0) return true;

			// Anything fully inside the tray drawer area belongs to it, not us.
			if (hasTray && r.Left >= trayRect.Left && r.Right <= trayRect.Right + 2)
				return true;

			// Clip to the tray-left so wide hosts can't bleed in.
			int right = hasTray ? Math.Min(r.Right, trayRect.Left) : r.Right;
			int left = r.Left;
			if (right <= left) return true;

			if (left < union.Left) union.Left = left;
			if (right > union.Right) union.Right = right;
			return true;
		}

		EnumChildWindows(parent, Cb, IntPtr.Zero);
	}

	/// <summary>
	/// Reads the user's taskbar icon alignment from the registry.
	/// 0 = Left, 1 = Center. Defaults to Center if unreadable.
	/// </summary>
	internal static TaskbarAlignment ReadTaskbarAlignment()
	{
		try
		{
			using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
				@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
			if (key?.GetValue("TaskbarAl") is int al)
				return al == 0 ? TaskbarAlignment.Left : TaskbarAlignment.Center;
		}
		catch
		{
			// ignored — fall through to default
		}
		return TaskbarAlignment.Center;
	}

	/// <summary>
	/// Reads whether the Win11 Widgets button is currently shown on the taskbar.
	/// Source: HKCU\…\Explorer\Advanced\TaskbarDa  (1 = shown, 0 = hidden).
	/// Defaults to <c>true</c> if unreadable so we stay conservative.
	/// </summary>
	private static bool ReadWidgetsEnabled()
	{
		try
		{
			using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
				@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
			if (key?.GetValue("TaskbarDa") is int da)
				return da != 0;
		}
		catch
		{
			// ignored — fall through to default
		}
		return true;
	}

	// ── Free-zone computation ──────────────────────────────────────────────

	// Width in DIPs (96-DPI base) reserved for the Win11 Widgets / Search /
	// Copilot cluster. These buttons live in a XAML island
	// (Windows.UI.Composition.DesktopWindowContentBridge) and do NOT expose
	// an enumerable child HWND, so Win32 cannot measure them. The shell uses
	// ~48 px per button at 100% DPI; we reserve a generous block to cover
	// weather/news widgets that can expand to ~160 px wide when activated.
	// Only applied when HKCU\…\Explorer\Advanced\TaskbarDa = 1.
	private const int Win11WidgetClusterDip = 200;

	/// <summary>
	/// Builds the list of candidate free zones for an overlay on this taskbar,
	/// honoring the user's alignment setting AND whether the Widgets button
	/// is enabled. Concrete rules:
	/// <list type="number">
	///   <item>Center align + right side       → right of icons, left of tray.</item>
	///   <item>Center align + left side + widgets  → right of widget block, left of icons.</item>
	///   <item>Center align + left side + no widgets → taskbar left edge, left of icons.</item>
	///   <item>Left align   + right side + no widgets → right of icons, left of tray.</item>
	///   <item>Left align   + right side + widgets  → right of icons, left of (tray − widget block).</item>
	/// </list>
	/// </summary>
	private static List<FreeZone> ComputeFreeZones(TaskbarIconLayout layout, int marginPx, uint dpi)
	{
		double scale = dpi / 96.0;
		int widgetBlockPx = layout.WidgetsEnabled
								? (int)Math.Round(Win11WidgetClusterDip * scale)
								: 0;

		var zones = new List<FreeZone>(2);

		// Far-left boundary: bare taskbar edge OR past the widget cluster
		// when widgets are enabled and live on the LEFT (always true under
		// Center alignment; Left alignment puts them on the RIGHT instead).
		bool widgetsOnLeft = layout.WidgetsEnabled && layout.Alignment == TaskbarAlignment.Center;
		bool widgetsOnRight = layout.WidgetsEnabled && layout.Alignment == TaskbarAlignment.Left;

		int leftBoundary = layout.TaskbarLeftScreen + (widgetsOnLeft ? widgetBlockPx + marginPx : marginPx);
		int trayBoundary = layout.TrayLeftScreen - marginPx - (widgetsOnRight ? widgetBlockPx + marginPx : 0);

		if (!layout.IconsFound)
		{
			// Couldn't find the icon cluster — fall back to the conservative
			// "right half of the taskbar" heuristic so the overlay still has
			// somewhere safe to render.
			int mid = (layout.TaskbarLeftScreen + layout.TaskbarRightScreen) / 2;
			mid = Math.Max(mid, leftBoundary);
			if (mid < trayBoundary) zones.Add(new FreeZone(mid, trayBoundary));
			return zones;
		}

		if (layout.Alignment == TaskbarAlignment.Left)
		{
			// Single zone: right of the icon cluster → left of (tray − widget block when present).
			int zoneLeft = Math.Max(layout.IconsRightScreen + marginPx, leftBoundary);
			if (zoneLeft < trayBoundary) zones.Add(new FreeZone(zoneLeft, trayBoundary));
		}
		else
		{
			// Center alignment: two candidate zones.
			//
			// Left zone: widget-area-right (or taskbar edge when widgets disabled) → icons-left.
			int leftZoneRight = layout.IconsLeftScreen - marginPx;
			if (leftBoundary < leftZoneRight)
				zones.Add(new FreeZone(leftBoundary, leftZoneRight));

			// Right zone: icons-right → tray-left.
			int rightZoneLeft = layout.IconsRightScreen + marginPx;
			if (rightZoneLeft < trayBoundary)
				zones.Add(new FreeZone(rightZoneLeft, trayBoundary));
		}

		return zones;
	}

	/// <summary>
	/// Picks the zone the overlay will live in this tick. When the user has dragged the overlay,
	/// we prefer the zone that can still fully contain the overlay at (or near) the dragged X.
	/// Otherwise we honor the side preference.
	/// </summary>
	private static FreeZone SelectActiveZone(
		List<FreeZone> zones,
		OverlaySide side,
		bool hasDragged,
		int savedX,
		int widthPhysical)
	{
		if (zones.Count == 1) return zones[0];

		if (hasDragged)
		{
			// Prefer a zone where the *entire* overlay fits at the dragged X.
			foreach (var z in zones)
			{
				if (savedX >= z.ScreenLeft - 5 &&
					savedX + widthPhysical <= z.ScreenRight + 5)
					return z;
			}

			// Fallback: nearest zone center to the dragged X.
			FreeZone best = zones[0];
			int bestDist = int.MaxValue;
			foreach (var z in zones)
			{
				int center = (z.ScreenLeft + z.ScreenRight) / 2;
				int dist = Math.Abs(center - savedX);
				if (dist < bestDist) { bestDist = dist; best = z; }
			}
			return best;
		}

		return side == OverlaySide.Right ? zones[^1] : zones[0];
	}
}
