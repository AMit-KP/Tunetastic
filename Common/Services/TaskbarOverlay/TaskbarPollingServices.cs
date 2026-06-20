using System.Runtime.InteropServices;
using static Tunetastic.Common.Services.TaskbarOverlay.NativeMethods;

namespace Tunetastic.Common.Services.TaskbarOverlay;

internal sealed partial class TaskbarStateService : IDisposable
{
	// taskbarHwnd → last known visible state
	private readonly Dictionary<IntPtr, bool> _lastState = new();
	private IReadOnlyList<TaskbarInfo> _taskbars = Array.Empty<TaskbarInfo>();
	private DispatcherTimer? _timer;

	/// <summary>Fired when the effective visibility of a taskbar changes. Bool = isVisible.</summary>
	public event Action<IntPtr, bool>? Changed;

	public void Start(IReadOnlyList<TaskbarInfo> taskbars)
	{
		_taskbars = taskbars;
		_lastState.Clear();

		_timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
		_timer.Tick += OnTick;
		_timer.Start();
	}

	public void Stop()
	{
		_timer?.Stop();
		_timer = null;
		_lastState.Clear();
	}

	private void OnTick(object? sender, object e)
	{
		foreach (var tb in _taskbars)
			CheckTaskbar(tb);
	}

	private void CheckTaskbar(TaskbarInfo tb)
	{
		bool visible = IsEffectivelyVisible(tb);

		if (!_lastState.TryGetValue(tb.Hwnd, out bool prev) || prev != visible)
		{
			_lastState[tb.Hwnd] = visible;
			Changed?.Invoke(tb.Hwnd, visible);
		}
	}

	private static bool IsEffectivelyVisible(TaskbarInfo tb)
	{
		var data = new APPBARDATA
		{
			cbSize = (uint)Marshal.SizeOf<APPBARDATA>(),
			hWnd = tb.Hwnd,
		};

		int state = (int)SHAppBarMessage((uint)ABM.GetState, ref data).ToInt64();

		if ((state & (int)ABS.AutoHide) == 0)
			return true; // auto-hide is off — always visible

		// Auto-hide is on: check if the taskbar is currently in its hidden (slid-off) position.
		if (!GetWindowRect(tb.Hwnd, out RECT cur)) return true;

		// Taskbar is hidden when its leading edge has slid past the monitor boundary.
		return tb.Edge switch
		{
			ABE.Bottom => cur.Top < tb.MonitorBounds.Bottom - 2,
			ABE.Top => cur.Bottom > tb.MonitorBounds.Top + 2,
			ABE.Left => cur.Right > tb.MonitorBounds.Left + 2,
			ABE.Right => cur.Left < tb.MonitorBounds.Right - 2,
			_ => true,
		};
	}

	public void Dispose() => Stop();
}

internal sealed partial class FullscreenStateService : IDisposable
{
	// hMonitor → last known fullscreen state
	private readonly Dictionary<IntPtr, bool> _lastState = new();
	private IReadOnlyList<TaskbarInfo> _taskbars = Array.Empty<TaskbarInfo>();
	private DispatcherTimer? _timer;

	/// <summary>Fired when fullscreen state changes on a monitor. Bool = isFullscreen.</summary>
	public event Action<IntPtr, bool>? Changed;

	public void Start(IReadOnlyList<TaskbarInfo> taskbars)
	{
		_taskbars = taskbars;
		_lastState.Clear();

		_timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
		_timer.Tick += OnTick;
		_timer.Start();
	}

	public void Stop()
	{
		_timer?.Stop();
		_timer = null;
		_lastState.Clear();
	}

	private void OnTick(object? sender, object e)
	{
		foreach (var tb in _taskbars)
			CheckMonitor(tb);
	}

	private void CheckMonitor(TaskbarInfo tb)
	{
		bool fullscreen = DetectFullscreen(tb);

		if (!_lastState.TryGetValue(tb.HMonitor, out bool prev) || prev != fullscreen)
		{
			_lastState[tb.HMonitor] = fullscreen;
			Changed?.Invoke(tb.HMonitor, fullscreen);
		}
	}

	private static bool DetectFullscreen(TaskbarInfo tb)
	{
		// Primary signal: Shell_TrayWnd visibility.
		// When a true fullscreen app takes over, the shell hides the taskbar.
		if (!IsWindowVisible(tb.Hwnd))
			return true;

		// Secondary: SHQueryUserNotificationState
		if (SHQueryUserNotificationState(out QUNS state) == 0)
		{
			if (state is QUNS.RunningD3dFullScreen or QUNS.PresentationMode)
				return true;
		}

		// Tertiary: check if the foreground window covers the entire work monitor.
		// Modern fullscreen apps (browsers F11, borderless games, UWP apps) don't
		// hide Shell_TrayWnd and don't trigger D3D exclusive mode, but they DO
		// have a window rect that covers the full monitor area.
		IntPtr fg = GetForegroundWindow();
		if (fg == IntPtr.Zero || fg == tb.Hwnd)
			return false;

		// Ignore our own process windows and the desktop/shell
		GetWindowThreadProcessId(fg, out uint fgPid);
		if (fgPid == Environment.ProcessId)
			return false;

		if (!GetWindowRect(fg, out RECT fgRect))
			return false;

		// Get the monitor info for the taskbar's monitor
		IntPtr hMon = MonitorFromWindow(tb.Hwnd, MONITOR_DEFAULTTONEAREST);
		MONITORINFOEX mi = default;
		mi.cbSize = (uint)Marshal.SizeOf<MONITORINFOEX>();
		if (!GetMonitorInfo(hMon, ref mi))
			return false;

		// If foreground window covers the full monitor rect (not just work area),
		// it's a fullscreen app. Allow 1px tolerance for off-by-one from DPI.
		return fgRect.Left <= mi.rcMonitor.Left + 1 &&
		fgRect.Top <= mi.rcMonitor.Top + 1 &&
		fgRect.Right >= mi.rcMonitor.Right - 1 &&
		fgRect.Bottom >= mi.rcMonitor.Bottom - 1;
	}

	public void Dispose() => Stop();
}
