
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.UI.Dispatching;
using static Tunetastic.Common.Services.TaskbarOverlay.NativeMethods;

namespace Tunetastic.Common.Services.TaskbarOverlay;

internal static class NativeMethods
{
	// ── Constants ──────────────────────────────────────────────────────────
	public const int GWL_STYLE = -16;
	public const int GWL_EXSTYLE = -20;

	public const uint WS_POPUP = 0x80000000;
	public const uint WS_BORDER = 0x00800000;
	public const uint WS_THICKFRAME = 0x00040000;
	public const uint WS_CAPTION = 0x00C00000;
	public const uint WS_DLGFRAME = 0x00400000;
	public const uint WS_EX_TOOLWINDOW = 0x00000080;
	public const uint WS_EX_NOACTIVATE = 0x08000000;
	public const uint WS_EX_TRANSPARENT = 0x00000020;
	public const uint WS_EX_CLIENTEDGE = 0x00000200;
	public const uint WS_EX_WINDOWEDGE = 0x00000100;
	public const uint WS_EX_DLGMODALFRAME = 0x00000001;
	public const uint WS_EX_STATICEDGE = 0x00020000;

	public const uint WM_DISPLAYCHANGE = 0x007E;
	public const uint WM_SETTINGCHANGE = 0x001A;
	public const uint WM_DPICHANGED = 0x02E0;

	public const uint MONITOR_DEFAULTTONEAREST = 2;
	public const int NULL_BRUSH = 5;

	// ── Enums ─────────────────────────────────────────────────────────────
	public enum ABM : uint
	{
		QueryPos = 2,
		GetTaskbarPos = 5,
		GetState = 4,
	}

	public enum ABE : uint
	{
		Left = 0,
		Top = 1,
		Right = 2,
		Bottom = 3,
	}

	[Flags]
	public enum ABS : int
	{
		AutoHide = 0x01,
		AlwaysOnTop = 0x02,
	}

	public enum QUNS
	{
		NotPresent = 1,
		Busy = 2,
		RunningD3dFullScreen = 3,
		PresentationMode = 4,
		AcceptsNotifications = 5,
	}

	// ── Structs ───────────────────────────────────────────────────────────
	[StructLayout(LayoutKind.Sequential)]
	public struct RECT
	{
		public int Left, Top, Right, Bottom;
		public int Width => Right - Left;
		public int Height => Bottom - Top;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct POINT { public int X, Y; }

	[StructLayout(LayoutKind.Sequential)]
	public struct APPBARDATA
	{
		public uint cbSize;
		public IntPtr hWnd;
		public uint uCallbackMessage;
		public uint uEdge;
		public RECT rc;
		public int lParam;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct MONITORINFOEX
	{
		public uint cbSize;
		public RECT rcMonitor;
		public RECT rcWork;
		public uint dwFlags;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		public string szDevice;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct MARGINS
	{
		public int LeftWidth, RightWidth, TopHeight, BottomHeight;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct WNDCLASSEX
	{
		public uint cbSize;
		public uint style;
		public IntPtr lpfnWndProc;
		public int cbClsExtra;
		public int cbWndExtra;
		public IntPtr hInstance;
		public IntPtr hIcon;
		public IntPtr hCursor;
		public IntPtr hbrBackground;
		[MarshalAs(UnmanagedType.LPWStr)]
		public string? lpszMenuName;
		[MarshalAs(UnmanagedType.LPWStr)]
		public string lpszClassName;
		public IntPtr hIconSm;
	}

	// ── Delegates ─────────────────────────────────────────────────────────
	public delegate IntPtr WndProcDelegate(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);
	public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

	// ── user32.dll ────────────────────────────────────────────────────────
	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	public static extern IntPtr FindWindow(string className, string? windowName);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	public static extern IntPtr FindWindowEx(IntPtr parent, IntPtr childAfter, string className, string? windowName);

	[DllImport("user32.dll")]
	public static extern bool EnumChildWindows(IntPtr parent, EnumWindowsProc cb, IntPtr lParam);

	[DllImport("user32.dll")]
	public static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);

	[DllImport("user32.dll")]
	public static extern bool GetClientRect(IntPtr hwnd, out RECT rect);

	[DllImport("user32.dll")]
	public static extern bool ScreenToClient(IntPtr hwnd, ref POINT pt);

	[DllImport("user32.dll")]
	public static extern bool IsWindowVisible(IntPtr hwnd);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	public static extern int GetClassName(IntPtr hwnd, StringBuilder buf, int max);

	[DllImport("user32.dll")]
	public static extern IntPtr GetForegroundWindow();

	[DllImport("user32.dll")]
	public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	public static extern bool GetMonitorInfo(IntPtr hMon, ref MONITORINFOEX info);

	[DllImport("user32.dll")]
	public static extern uint GetDpiForWindow(IntPtr hwnd);

	[DllImport("user32.dll")]
	public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

	[DllImport("user32.dll")]
	public static extern bool GetCursorPos(out POINT pt);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	public static extern IntPtr CreateWindowEx(
	  uint exStyle, string className, string? windowName, uint style,
	  int x, int y, int w, int h,
	  IntPtr parent, IntPtr menu, IntPtr hInst, IntPtr lpParam);

	[DllImport("user32.dll")]
	public static extern bool DestroyWindow(IntPtr hwnd);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	public static extern ushort RegisterClassEx(ref WNDCLASSEX wc);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	public static extern bool UnregisterClass(string className, IntPtr hInst);

	[DllImport("user32.dll")]
	public static extern IntPtr DefWindowProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	public static extern uint RegisterWindowMessage(string msg);

	// GetWindowLongPtr / SetWindowLongPtr — safe for both x86 and x64
	public static nint GetWindowLongPtr(IntPtr hwnd, int index)
	=> IntPtr.Size == 8 ? GetWindowLongPtr64(hwnd, index) : GetWindowLong32(hwnd, index);

	public static nint SetWindowLongPtr(IntPtr hwnd, int index, nint newLong)
	  => IntPtr.Size == 8 ? SetWindowLongPtr64(hwnd, index, newLong) : SetWindowLong32(hwnd, index, newLong);

	[DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
	private static extern nint GetWindowLongPtr64(IntPtr hwnd, int index);

	[DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
	private static extern nint GetWindowLong32(IntPtr hwnd, int index);

	[DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
	private static extern nint SetWindowLongPtr64(IntPtr hwnd, int index, nint newLong);

	[DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
	private static extern nint SetWindowLong32(IntPtr hwnd, int index, nint newLong);

	// ── SetWindowPos ──────────────────────────────────────────────────────
	public static readonly IntPtr HWND_TOPMOST = new(-1);
	public const uint SWP_NOMOVE = 0x0002;
	public const uint SWP_NOSIZE = 0x0001;
	public const uint SWP_NOACTIVATE = 0x0010;
	public const uint SWP_FRAMECHANGED = 0x0020;

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool SetWindowPos(
	  IntPtr hwnd, IntPtr hWndInsertAfter,
	  int x, int y, int cx, int cy, uint uFlags);

	// ── WM_WINDOWPOSCHANGING ──────────────────────────────────────────────
	public const uint WM_WINDOWPOSCHANGING = 0x0046;

	[StructLayout(LayoutKind.Sequential)]
	public struct WINDOWPOS
	{
		public IntPtr hwnd;
		public IntPtr hwndInsertAfter;
		public int x;
		public int y;
		public int cx;
		public int cy;
		public uint flags;
	}

	// ── Subclass (comctl32 v6) ────────────────────────────────────────────
	public delegate IntPtr SUBCLASSPROC(
	IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam,
	nuint uIdSubclass, nuint dwRefData);

	[DllImport("comctl32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool SetWindowSubclass(
	  IntPtr hwnd, SUBCLASSPROC pfnSubclass,
	  nuint uIdSubclass, nuint dwRefData);

	[DllImport("comctl32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool RemoveWindowSubclass(
	  IntPtr hwnd, SUBCLASSPROC pfnSubclass, nuint uIdSubclass);

	[DllImport("comctl32.dll")]
	public static extern IntPtr DefSubclassProc(
	  IntPtr hwnd, uint uMsg, IntPtr wParam, IntPtr lParam);

	// ── kernel32.dll ──────────────────────────────────────────────────────
	[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
	public static extern IntPtr GetModuleHandle(string? name);

	// ── shell32.dll ───────────────────────────────────────────────────────
	[DllImport("shell32.dll")]
	public static extern IntPtr SHAppBarMessage(uint msg, ref APPBARDATA data);

	[DllImport("shell32.dll")]
	public static extern int SHQueryUserNotificationState(out QUNS state);

	// ── gdi32.dll ─────────────────────────────────────────────────────────
	[DllImport("gdi32.dll")]
	public static extern IntPtr GetStockObject(int index);

	// ── dwmapi.dll ────────────────────────────────────────────────────────
	[DllImport("dwmapi.dll")]
	public static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, in MARGINS margins);

	[DllImport("dwmapi.dll")]
	public static extern int DwmSetWindowAttribute(IntPtr hwnd, uint dwAttribute, in uint pvAttribute, uint cbAttribute);

	public const uint DWMWA_BORDER_COLOR = 34;
	public const uint DWMWA_COLOR_NONE = 0xFFFFFFFE;
	public const uint DWMWA_WINDOW_CORNER_PREFERENCE = 33;
	public const uint DWMWCP_DEFAULT = 0;
	public const uint DWMWCP_DONOTROUND = 1;
	public const uint DWMWCP_ROUND = 2;
	public const uint DWMWCP_ROUNDSMALL = 3;
}

/// <summary>
/// A hidden zero-size WS_POPUP top-level window (NOT HWND_MESSAGE) that
/// receives broadcast messages and fires .NET events on the supplied
/// DispatcherQueue. HWND_MESSAGE windows are excluded from HWND_BROADCAST
/// delivery, so we use a normal invisible WS_POPUP instead.
/// </summary>
internal sealed partial class ShellNotificationWindow : IDisposable
{
	// Static WndProc delegate kept alive to prevent GC collection.
	private static readonly WndProcDelegate s_wndProc = StaticWndProc;

	// Map HWND → instance so the static WndProc can find the right instance.
	private static readonly Dictionary<IntPtr, ShellNotificationWindow> s_instances = new();

	private readonly DispatcherQueue _dispatcherQueue;
	private readonly string _className;
	private readonly IntPtr _hwnd;
	private readonly uint _wmTaskbarCreated;
	private bool _disposed;

	public event Action? DisplayChanged;
	public event Action? DpiChanged;
	public event Action? SettingChanged;
	public event Action? TaskbarCreated;

	public ShellNotificationWindow(DispatcherQueue dispatcherQueue)
	{
		_dispatcherQueue = dispatcherQueue;
		_className = "TestRunner_ShellNotify_" + Guid.NewGuid().ToString("N")[..8];
		_wmTaskbarCreated = RegisterWindowMessage("TaskbarCreated");

		IntPtr hInst = GetModuleHandle(null);

		var wc = new WNDCLASSEX
		{
			cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
			lpfnWndProc = Marshal.GetFunctionPointerForDelegate(s_wndProc),
			hInstance = hInst,
			hbrBackground = GetStockObject(NULL_BRUSH),
			lpszClassName = _className,
		};

		if (RegisterClassEx(ref wc) == 0)
			throw new InvalidOperationException($"RegisterClassEx failed ({Marshal.GetLastWin32Error()})");

		_hwnd = CreateWindowEx(
		  exStyle: 0,
		  className: _className,
		  windowName: string.Empty,
		  style: WS_POPUP,
		  x: 0, y: 0, w: 0, h: 0,
		  parent: IntPtr.Zero,
		  menu: IntPtr.Zero,
		  hInst: hInst,
		  lpParam: IntPtr.Zero);

		if (_hwnd == IntPtr.Zero)
			throw new InvalidOperationException($"CreateWindowEx failed ({Marshal.GetLastWin32Error()})");

		lock (s_instances) s_instances[_hwnd] = this;
	}

	private static IntPtr StaticWndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
	{
		ShellNotificationWindow? self;
		lock (s_instances) s_instances.TryGetValue(hwnd, out self);

		if (self is not null)
			return self.InstanceWndProc(hwnd, msg, wParam, lParam);

		return DefWindowProc(hwnd, msg, wParam, lParam);
	}

	private IntPtr InstanceWndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
	{
		if (msg == _wmTaskbarCreated)
		{
			_dispatcherQueue.TryEnqueue(() => TaskbarCreated?.Invoke());
			return IntPtr.Zero;
		}

		switch (msg)
		{
			case WM_DISPLAYCHANGE:
				_dispatcherQueue.TryEnqueue(() => DisplayChanged?.Invoke());
				return IntPtr.Zero;

			case WM_DPICHANGED:
				_dispatcherQueue.TryEnqueue(() => DpiChanged?.Invoke());
				return IntPtr.Zero;

			case WM_SETTINGCHANGE:
				_dispatcherQueue.TryEnqueue(() => SettingChanged?.Invoke());
				return IntPtr.Zero;
		}

		return DefWindowProc(hwnd, msg, wParam, lParam);
	}

	public void Dispose()
	{
		if (_disposed) return;
		_disposed = true;

		lock (s_instances) s_instances.Remove(_hwnd);

		if (_hwnd != IntPtr.Zero)
			DestroyWindow(_hwnd);

		UnregisterClass(_className, GetModuleHandle(null));
	}
}
