using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;
using static Tunetastic.Common.Services.TaskbarOverlay.NativeMethods;

namespace Tunetastic.Common.Services.TaskbarOverlay;

/// <summary>Which side of the taskbar free zone to anchor the overlay.</summary>
internal enum OverlaySide { Left, Right }

/// <summary>
/// Single entry point for the taskbar overlay feature.
/// Call <see cref="Initialize"/> to start, <see cref="Shutdown"/> to stop.
/// Everything else (taskbar discovery, fullscreen/autohide polling, shell messages) runs automatically.
/// </summary>
internal static class TaskbarOverlayManager
{
	private static ShellNotificationWindow? _shellNotify;
	private static TaskbarDiscoveryService? _discovery;
	private static TaskbarLayoutService? _layout;
	private static FullscreenStateService? _fullscreen;
	private static TaskbarStateService? _tbState;
	private static readonly List<TaskbarOverlayWindow> _overlays = new();
	private static DispatcherQueue? _dispatcherQueue;

	private static readonly Dictionary<IntPtr, bool> _monitorFullscreen = new();
	private static readonly Dictionary<IntPtr, bool> _taskbarVisible = new();

	private static UIElement? _content;
	private static OverlaySide _side = OverlaySide.Right;
	private static int _marginPx = 4;
	private static Brush? _background;

	private static bool _dragEnabled;
	private static DispatcherTimer? _dragAutoStopTimer;
	private static DispatcherTimer? _repositionTimer;
	private static readonly Dictionary<string, int> _draggedPositions = new();

	private static bool _multiMonitor = false;
	private static string? _targetMonitor; // null = primary

	private static bool _initialized;

	// ----------------------------------------------------------------------
	//  PUBLIC API
	// ----------------------------------------------------------------------

	/// <summary>
	    /// Starts the overlay system. Discovers all taskbars, creates overlay windows,
	    /// begins polling for fullscreen/autohide changes, and listens for shell events.
	    /// Call <see cref="SetContent"/> before or after this to provide overlay UI.
	    /// </summary>
	public static void Initialize()
	{
		if (_initialized) return;
		_initialized = true;

		_dispatcherQueue = DispatcherQueue.GetForCurrentThread();
		_discovery = new TaskbarDiscoveryService();
		_layout = new TaskbarLayoutService();
		_fullscreen = new FullscreenStateService();
		_tbState = new TaskbarStateService();
		_shellNotify = new ShellNotificationWindow(_dispatcherQueue);

		if (_content is not null)
			RebuildOverlays();

		_fullscreen.Changed += (hMon, isFs) =>
		{
			_monitorFullscreen[hMon] = isFs;
			UpdateVisibilityForMonitor(hMon);
		};

		_tbState.Changed += (tbHwnd, isVis) =>
		{
			_taskbarVisible[tbHwnd] = isVis;
			var overlay = _overlays.FirstOrDefault(o => o.Taskbar.Hwnd == tbHwnd);
			if (overlay is null) return;
			bool fsHidden = _monitorFullscreen.TryGetValue(overlay.Taskbar.HMonitor, out bool fs) && fs;
			overlay.SetVisible(isVis && !fsHidden);
		};

		_shellNotify.TaskbarCreated += () =>
		{
			DisposeOverlays();
			_ = Task.Delay(1000).ContinueWith(_ =>
			  _dispatcherQueue!.TryEnqueue(RebuildOverlays));
		};
		_shellNotify.DisplayChanged += () => { DisposeOverlays(); RebuildOverlays(); };
		_shellNotify.DpiChanged += () => { DisposeOverlays(); RebuildOverlays(); };
		_shellNotify.SettingChanged += RepositionAll;

		// Periodically re-compute layout to handle dynamic taskbar changes
		// (apps launching/closing shift the centered icon cluster, tray drawer
		// grows/shrinks as system icons appear). Faster than 1s keeps the overlay
		// tracking the live free zone smoothly.
		_repositionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(750) };
		_repositionTimer.Tick += (_, _) => RepositionAll();
		_repositionTimer.Start();
	}

	/// <summary>
	    /// Sets or replaces the overlay content. Call anytime to update what the overlay displays.
	    /// The overlay width is determined by the content's width (auto).
	    /// Triggers a full rebuild so the new content appears immediately.
	    /// </summary>
	public static void SetContent(UIElement content)
	{
		_content = content;
		if (_initialized)
		{
			DisposeOverlays();
			RebuildOverlays();
		}
	}

	/// <summary>
	    /// Sets the overlay background. Pass null for fully transparent (default).
	    /// </summary>
	public static void SetBackground(Brush? background)
	{
		_background = background;
		foreach (var overlay in _overlays)
			overlay.SetBackground(_background);
	}

	/// <summary>
	    /// Returns the current drag position (screen X) for the primary monitor, or null
	    /// if StartDrag has never been called or no drag has occurred.
	    /// </summary>
	public static int? GetDragPosition()
	{
		// Return the first saved position (primary monitor)
		foreach (var kvp in _draggedPositions)
			return kvp.Value;
		return null;
	}

	/// <summary>
	    /// Returns the current drag position for a specific monitor device name, or null.
	    /// </summary>
	public static int? GetDragPosition(string monitorDeviceName)
	{
		return _draggedPositions.TryGetValue(monitorDeviceName, out int x) ? x : null;
	}

	/// <summary>
	/// Sets the current drag position for a specific monitor device name.
	/// </summary>
	/// <param name="screenX">The current drag position (screen X)</param>
	/// <param name="monitorDeviceName">The monitor device name</param>
	public static void SetDragPosition(int screenX, string? monitorDeviceName = null)
	{
		string? deviceName = monitorDeviceName ?? ResolvePrimaryMonitorDeviceName();
		if (deviceName is null) return;

		_draggedPositions[deviceName] = screenX;
		if (_initialized) RepositionAll();
	}

	/// <summary>
	/// Clears the current drag position for a specific monitor device name.
	/// </summary>
	/// <param name="monitorDeviceName">The monitor device name</param>
	public static void ClearDragPosition(string? monitorDeviceName = null)
	{
		string? deviceName = monitorDeviceName ?? ResolvePrimaryMonitorDeviceName();
		if (deviceName is null) return;

		if (_draggedPositions.Remove(deviceName) && _initialized)
			RepositionAll();
	}

	/// <summary>
	    /// Stops everything — closes overlay windows, disposes polling services and shell listener.
	    /// </summary>
	public static void Shutdown()
	{
		if (!_initialized) return;
		_initialized = false;

		StopDrag();
		_repositionTimer?.Stop();
		_repositionTimer = null;
		DisposeOverlays();
		_fullscreen?.Dispose();
		_tbState?.Dispose();
		_shellNotify?.Dispose();
		_fullscreen = null;
		_tbState = null;
		_shellNotify = null;
	}

	/// <summary>
	    /// Sets which side of the taskbar the overlay anchors to (when not dragged).
	    /// Can be called before or after <see cref="Initialize"/>.
	    /// </summary>
	public static void SetSide(OverlaySide side)
	{
		_side = side;
		if (_initialized) RepositionAll();
	}

	/// <summary>
	    /// Enables or disables multi-monitor mode.
	    /// When true, the overlay appears on all monitors' taskbars.
	    /// When false, only the target monitor is used (defaults to primary).
	    /// </summary>
	public static void SetMultiMonitor(bool enabled)
	{
		if (_multiMonitor == enabled) return;
		_multiMonitor = enabled;
		if (_initialized) { DisposeOverlays(); RebuildOverlays(); }
	}

	/// <summary>
	    /// Returns formatted monitor names discovered from the current taskbar layout.
	    /// Each entry contains the device name and whether it is the primary monitor.
	    /// Can be called before or after <see cref="Initialize"/>.
	    /// </summary>
	public static IReadOnlyList<(string DeviceName, bool IsPrimary)> GetMonitorNames()
	{
		var discovery = _discovery ?? new TaskbarDiscoveryService();
		return discovery.Discover()
		  .Select(tb => (tb.MonitorDeviceName, tb.IsPrimary))
		  .ToList();
	}

	/// <summary>
	    /// Sets which monitor the overlay should appear on when multi-monitor is disabled.
	    /// Pass null to default to the primary monitor.
	    /// Use <see cref="GetMonitorNames"/> to discover valid device names.
	    /// </summary>
	public static void SetTargetMonitor(string? monitorDeviceName)
	{
		_targetMonitor = monitorDeviceName;
		if (_initialized && !_multiMonitor) { DisposeOverlays(); RebuildOverlays(); }
	}

	/// <summary>
	    /// Returns whether the taskbar is currently configured for center alignment.
	    /// Reads the live registry value each call, so it reflects changes without restart.
	    /// </summary>
	public static bool IsTaskbarCenterAligned()
	=> TaskbarLayoutService.ReadTaskbarAlignment() == TaskbarAlignment.Center;

	/// <summary>
	    /// Enables drag mode — the user can drag the overlay horizontally within the taskbar free zone.
	    /// Drag automatically stops after 5 minutes and saves the position.
	    /// </summary>
	public static void StartDrag()
	{
		if (_dragEnabled) return;
		_dragEnabled = true;

		foreach (var overlay in _overlays)
			overlay.EnableDrag(true);

		// Auto-stop after 5 minutes
		_dragAutoStopTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) };
		_dragAutoStopTimer.Tick += (_, _) => StopDrag();
		_dragAutoStopTimer.Start();
	}

	/// <summary>
	    /// Disables drag mode and saves the current drag position (in memory).
	    /// Also called automatically 5 minutes after <see cref="StartDrag"/>.
	    /// </summary>
	public static void StopDrag()
	{
		if (!_dragEnabled) return;
		_dragEnabled = false;

		_dragAutoStopTimer?.Stop();
		_dragAutoStopTimer = null;

		foreach (var overlay in _overlays)
		{
			overlay.EnableDrag(false);
			// Save current position
			GetWindowRect(overlay.GetWindowHandle(), out RECT cur);
			_draggedPositions[overlay.Taskbar.MonitorDeviceName] = cur.Left;
		}
	}

	// ----------------------------------------------------------------------
	//  INTERNAL MACHINERY
	// ----------------------------------------------------------------------

	private static int MeasureContentWidth()
	{
		if (_content is FrameworkElement fe)
		{
			// If the content has an explicit Width set, use it
			if (!double.IsNaN(fe.Width) && fe.Width > 0)
				return (int)fe.Width;

			// Otherwise measure to get desired size
			fe.Measure(new Windows.Foundation.Size(2000, 100));
			int desired = (int)fe.DesiredSize.Width;
			return desired > 0 ? desired : 200;
		}
		return 200;
	}

	private static string? ResolvePrimaryMonitorDeviceName()
	{
		var discovery = _discovery ?? new TaskbarDiscoveryService();
		return discovery.Discover().FirstOrDefault(tb => tb.IsPrimary)?.MonitorDeviceName;
	}

	private static void RebuildOverlays()
	{
		if (_content is null) return;

		var taskbars = _discovery!.Discover();

		_fullscreen!.Stop();
		_tbState!.Stop();

		foreach (var o in _overlays)
		{
			try { o.DetachContent(); } catch { }
			try { o.Close(); } catch { }
		}
		_overlays.Clear();
		_monitorFullscreen.Clear();
		_taskbarVisible.Clear();

		int widthPx = MeasureContentWidth();

		// Apply multi-monitor filter
		var filtered = _multiMonitor
	  ? taskbars
	  : taskbars.Where(tb => _targetMonitor is not null
		? tb.MonitorDeviceName == _targetMonitor
		: tb.IsPrimary).ToList();

		foreach (var tb in filtered)
		{
			var result = _layout!.Compute(tb, widthPx, _marginPx, _side, _draggedPositions);
			if (result is null) continue;

			var (rect, zone) = result.Value;
			var w = new TaskbarOverlayWindow(tb, rect, zone, _content);

			if (_background is not null)
				w.SetBackground(_background);

			w.DragCommitted += (_, screenX) =>
			{
				// Re-clamp against the latest live zone before persisting, so the saved
				// position can never sit inside the icons or the tray drawer once the
				// taskbar layout shifts.
				var live = _layout!.Compute(tb, widthPx, _marginPx, _side, _draggedPositions);
				if (live is not null)
				{
					var (liveRect, liveZone) = live.Value;
					int clamped = Math.Clamp(
					  screenX,
					  liveZone.ScreenLeft,
					  Math.Max(liveZone.ScreenLeft, liveZone.ScreenRight - liveRect.Width));
					_draggedPositions[tb.MonitorDeviceName] = clamped;
				}
				else
				{
					_draggedPositions[tb.MonitorDeviceName] = screenX;
				}

				int mid = tb.TaskbarBoundsScreen.Left + tb.TaskbarBoundsScreen.Width / 2;
				_side = (screenX + widthPx / 2 < mid) ? OverlaySide.Left : OverlaySide.Right;
			};

			if (_dragEnabled)
				w.EnableDrag(true);

			w.Activate();
			w.ApplyPendingRect();
			_overlays.Add(w);
			_taskbarVisible[tb.Hwnd] = true;
			_monitorFullscreen[tb.HMonitor] = false;
		}

		_fullscreen.Start(taskbars);
		_tbState.Start(taskbars);

		// The very first layout snapshot can be taken before the shell has finished
		// painting (tray drawer size, icon cluster bounds) — schedule a follow-up
		// reposition tick to pick up the final live geometry.
		if (_dispatcherQueue is not null)
		{
			_ = Task.Delay(150).ContinueWith(_ =>
			  _dispatcherQueue.TryEnqueue(RepositionAll));
			_ = Task.Delay(500).ContinueWith(_ =>
			  _dispatcherQueue.TryEnqueue(RepositionAll));
		}
	}

	private static void RepositionAll()
	{
		int fallbackDip = MeasureContentWidth();
		foreach (var overlay in _overlays)
		{
			// WinUI auto-sizes the window to its rendered content (which can be
			// wider than the up-front Measure pass predicted, especially for text +
			// padded buttons). So instead of trusting our pre-measured width on
			// every tick, read what the OS actually drew the window at and feed
			// that physical width back through the layout service. This guarantees
			// the right edge stays inside the live free zone after the first frame.
			double scale = overlay.Taskbar.Dpi / 96.0;
			int widthDip = fallbackDip;
			IntPtr hwnd = overlay.GetWindowHandle();
			if (hwnd != IntPtr.Zero && GetWindowRect(hwnd, out RECT actual) && actual.Width > 0)
			{
				widthDip = (int)Math.Ceiling(actual.Width / scale);
			}

			var result = _layout!.Compute(overlay.Taskbar, widthDip, _marginPx, _side, _draggedPositions);
			if (result is null) continue;
			var (rect, zone) = result.Value;
			overlay.UpdateFreeZone(zone);
			overlay.ApplyRect(rect);
		}
	}

	private static void DisposeOverlays()
	{
		foreach (var o in _overlays)
		{
			try { o.DetachContent(); } catch { }
			try { o.Close(); } catch { }
		}
		_overlays.Clear();
		_monitorFullscreen.Clear();
		_taskbarVisible.Clear();
	}

	private static void UpdateVisibilityForMonitor(IntPtr hMon)
	{
		bool fsHidden = _monitorFullscreen.TryGetValue(hMon, out bool fs) && fs;
		foreach (var overlay in _overlays.Where(o => o.Taskbar.HMonitor == hMon))
		{
			bool tbVis = !_taskbarVisible.TryGetValue(overlay.Taskbar.Hwnd, out bool v) || v;
			overlay.SetVisible(!fsHidden && tbVis);
		}
	}
}
