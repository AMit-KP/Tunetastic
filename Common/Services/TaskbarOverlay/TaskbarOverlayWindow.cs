using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Input;
using WinUIEx;
using static Tunetastic.Common.Services.TaskbarOverlay.NativeMethods;

namespace Tunetastic.Common.Services.TaskbarOverlay;

/// <summary>
/// Top-level, topmost, borderless WinUIEx.WindowEx that hosts user-supplied overlay content.
/// Sits in the taskbar's free zone without being a child of Shell_TrayWnd.
/// Uses a periodic timer to re-assert HWND_TOPMOST so the overlay is never
/// pushed behind the taskbar when the user clicks elsewhere.
/// </summary>
internal sealed class TaskbarOverlayWindow : WindowEx
{
	public TaskbarInfo Taskbar { get; }
	public FreeZone CurrentFreeZone { get; private set; }

	/// <summary>Fired after a drag completes. Arg = new screen X (physical px).</summary>
	public event EventHandler<int>? DragCommitted;

	private readonly Grid _rootGrid;
	private OverlayRect _pendingRect;
	private int _windowScreenXAtDragStart;

	// Timer that re-asserts topmost Z-order every tick.
	private readonly DispatcherTimer _topmostTimer;

	private readonly DispatcherTimer _deferredRepinTimer;
	private static readonly int[] s_repinScheduleMs = { 80, 250, 500, 1000, 2000, 4000 };
	private int _repinStep;

	private NativeMethods.SUBCLASSPROC? _hideFilter;
	private const nuint HideFilterSubclassId = 0xC0FFEE;

	private NativeMethods.WinEventDelegate? _foregroundHookProc;
	private IntPtr _foregroundHook;
	private bool _userIntendedVisible = true;

	// ── Drag state ────────────────────────────────────────────────────────
	private bool _dragEnabled;
	private bool _isDragging;
	private int _dragStartScreenX;
	private bool _chromeReapplyPending;

	public TaskbarOverlayWindow(TaskbarInfo tb, OverlayRect rect, FreeZone zone, UIElement content)
	{
		Taskbar = tb;
		CurrentFreeZone = zone;
		_pendingRect = rect;

		// WinUIEx affordances
		Title = "Overlay";
		IsAlwaysOnTop = true;
		IsResizable = false;
		IsMinimizable = false;
		IsMaximizable = false;
		IsShownInSwitchers = false;

		// Remove title bar and borders completely
		if (AppWindow.Presenter is OverlappedPresenter ovp)
			ovp.SetBorderAndTitleBar(hasBorder: false, hasTitleBar: false);

		// WinUIEx's TransparentTintBackdrop makes the entire window transparent
		// (no opaque WinUI root background, no system backdrop)
		SystemBackdrop = new WinUIEx.TransparentTintBackdrop();

		// Wrap user content in a transparent Grid for drag handling
		_rootGrid = new Grid
		{
			Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(1, 0, 0, 0)),
			ManipulationMode = ManipulationModes.None,
		};

		if (content is FrameworkElement fe && fe.Parent is Panel oldParent)
			oldParent.Children.Remove(content);

		_rootGrid.Children.Add(content);
		Content = _rootGrid;

		// Timer that re-asserts topmost every 20ms.
		_topmostTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(20)
		};
		_topmostTimer.Tick += (_, _) =>
		{
			if (_chromeReapplyPending)
			{
				_chromeReapplyPending = false;
				IntPtr h = this.GetWindowHandle();
				if (h != IntPtr.Zero) ApplyChromeStripping(h);
			}
			_topmostTimer.Stop();
		};

		_deferredRepinTimer = new DispatcherTimer();
		_deferredRepinTimer.Tick += (_, _) =>
		{
			_deferredRepinTimer.Stop();

			if (this.GetWindowHandle() == IntPtr.Zero) return;
			if (!_userIntendedVisible) return;

			ReassertTopmost();

			if (IsAboveTaskbar()) return;

			_repinStep++;
			if (_repinStep >= s_repinScheduleMs.Length) return;
			_deferredRepinTimer.Interval = TimeSpan.FromMilliseconds(s_repinScheduleMs[_repinStep]);
			_deferredRepinTimer.Start();
		};

		Closed += (_, _) =>
		{
			_topmostTimer.Stop();
			_deferredRepinTimer.Stop();
			RemoveHideFilter(this.GetWindowHandle());
			RemoveForegroundHook();
		};
	}

	/// <summary>
	   /// Call once, immediately after Activate(), to place the window at the correct position.
	   /// </summary>
	public void ApplyPendingRect()
	{
		IntPtr hwnd = this.GetWindowHandle();

		// Strip all Win32 frame/border styles that DWM renders
		nint style = GetWindowLongPtr(hwnd, GWL_STYLE);
		style &= ~(nint)((long)WS_BORDER | (long)WS_THICKFRAME | (long)WS_CAPTION | (long)WS_DLGFRAME);
		SetWindowLongPtr(hwnd, GWL_STYLE, style);

		nint exStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE);
		exStyle |= (nint)((long)WS_EX_TOOLWINDOW | (long)WS_EX_NOACTIVATE);
		exStyle &= ~(nint)((long)WS_EX_CLIENTEDGE | (long)WS_EX_WINDOWEDGE | (long)WS_EX_DLGMODALFRAME | (long)WS_EX_STATICEDGE);
		SetWindowLongPtr(hwnd, GWL_EXSTYLE, exStyle);

		// Force Windows to recalculate the non-client area (removes the border)
		SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
	  SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_FRAMECHANGED);

		// Remove the thin DWM-rendered border (Windows 11+)
		ApplyChromeStripping(hwnd);

		if (Taskbar.Hwnd != IntPtr.Zero)
		{
			nint prevOwner = SetWindowLongPtr(hwnd, NativeMethods.GWLP_HWNDPARENT, Taskbar.Hwnd);
			int ownerErr = Marshal.GetLastWin32Error();
			System.Diagnostics.Debug.WriteLine($"[TaskbarOverlay] SetOwner(taskbar=0x{Taskbar.Hwnd.ToInt64():X8} -> prevOwner=0x{prevOwner:X8} err={ownerErr})");
		}

		InstallHideFilter(hwnd);
		InstallForegroundHook();

		ReassertTopmost();
		ApplyRect(_pendingRect);
		_topmostTimer.Start();
		_chromeReapplyPending = true;
	}

	private static void ApplyChromeStripping(IntPtr hwnd)
	{
		uint colorNone = NativeMethods.DWMWA_COLOR_NONE;
		NativeMethods.DwmSetWindowAttribute(hwnd, NativeMethods.DWMWA_BORDER_COLOR, in colorNone, sizeof(uint));

		uint dontRound = NativeMethods.DWMWCP_DONOTROUND;
		NativeMethods.DwmSetWindowAttribute(hwnd, NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE, in dontRound, sizeof(uint));

		var margins = new NativeMethods.MARGINS
		{
			LeftWidth = -1,
			RightWidth = -1,
			TopHeight = -1,
			BottomHeight = -1
		};
		NativeMethods.DwmExtendFrameIntoClientArea(hwnd, in margins);
	}

	// --Hide-message filter (subclass)------
	private void InstallHideFilter(IntPtr hwnd)
	{
		if (hwnd == IntPtr.Zero || _hideFilter is not null) return;

		_hideFilter = HideFilterProc;
		bool ok = NativeMethods.SetWindowSubclass(hwnd, _hideFilter, HideFilterSubclassId, 0);
		System.Diagnostics.Debug.WriteLine($"[TaskbarOverlay] InstallHideFilter -> {ok}");
	}

	private void RemoveHideFilter(IntPtr hwnd)
	{
		if (hwnd == IntPtr.Zero || _hideFilter is null) return;

		NativeMethods.RemoveWindowSubclass(hwnd, _hideFilter, HideFilterSubclassId);
		_hideFilter = null;
	}

	private void InstallForegroundHook()
	{
		if (_foregroundHook != IntPtr.Zero) return;

		_foregroundHookProc = ForegroundChangedProc;
		_foregroundHook = NativeMethods.SetWinEventHook(NativeMethods.EVENT_SYSTEM_FOREGROUND,
														NativeMethods.EVENT_SYSTEM_MOVESIZEEND,
														IntPtr.Zero,
														_foregroundHookProc,
														idProcess: 0,
														idThread: 0,
														NativeMethods.WINEVENT_OUTOFCONTEXT);

		System.Diagnostics.Debug.WriteLine($"[TaskbarOverlay] InstallForegroundHook -> 0x{_foregroundHook.ToInt64():X}");
	}

	private void RemoveForegroundHook()
	{
		if (_foregroundHook == IntPtr.Zero) return;
		NativeMethods.UnhookWinEvent(_foregroundHook);
		_foregroundHook = IntPtr.Zero;
		_foregroundHookProc = null;
	}

	private void ForegroundChangedProc(IntPtr hWinEventHook, uint eventTypr, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
	{
		if (idObject != 0) return;
		if (hwnd == IntPtr.Zero) return;

		DispatcherQueue.TryEnqueue(() =>
		{
			if (this.GetWindowHandle() == IntPtr.Zero) return;
			if (!_userIntendedVisible) return;
			ReassertTopmost();

			_deferredRepinTimer.Stop();
			_repinStep = 0;
			_deferredRepinTimer.Interval = TimeSpan.FromMilliseconds(s_repinScheduleMs[0]);
			_deferredRepinTimer.Start();
		});
	}

	private IntPtr HideFilterProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, nuint uIdSubclass, nuint dwRefData)
	{
		switch (uMsg)
		{
			case NativeMethods.WM_SHOWWINDOW:
				if (_userIntendedVisible && (uint)lParam.ToInt64() == NativeMethods.SW_PARENTCLOSING)
				{
					System.Diagnostics.Debug.WriteLine("[TaskbarOverlay] swallowed WM_SHOWWINDOW(SW_PARENTCLOSING)");
					return IntPtr.Zero;
				}
				break;

			case NativeMethods.WM_SYSCOMMAND:
				if (((uint)wParam.ToInt64() & 0xFFF0) == NativeMethods.SC_MINIMIZE)
				{
					System.Diagnostics.Debug.WriteLine("[TaskbarOverlay] swallowed WM_SYSCOMMAND(SC_MINIMIZE)");
					return IntPtr.Zero;
				}
				break;

			case NativeMethods.WM_WINDOWPOSCHANGING:
				if (lParam != IntPtr.Zero)
				{
					var wp = Marshal.PtrToStructure<NativeMethods.WINDOWPOS>(lParam);
					bool mutated = false;

					if (_userIntendedVisible && (wp.flags & NativeMethods.SWP_HIDEWINDOW) != 0 && (wp.flags & NativeMethods.SWP_SHOWWINDOW) == 0)
					{
						wp.flags &= ~NativeMethods.SWP_HIDEWINDOW;
						mutated = true;
					}

					if (_userIntendedVisible && (wp.flags & NativeMethods.SWP_NOZORDER) == 0)
					{
						if (wp.hwndInsertAfter != NativeMethods.HWND_TOPMOST)
						{
							wp.hwndInsertAfter = NativeMethods.HWND_TOPMOST;
							mutated = true;
						}
					}

					if (mutated)
					{
						Marshal.StructureToPtr(wp, lParam, fDeleteOld: false);
					}
				}
				break;
		}

		return NativeMethods.DefSubclassProc(hWnd, uMsg, wParam, lParam);
	}

	public void SetBackground(Microsoft.UI.Xaml.Media.Brush? background)
	{
		if (background is null)
		{
			_rootGrid.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(1, 0, 0, 0));
			SystemBackdrop = new WinUIEx.TransparentTintBackdrop();
		}
		else
		{
			_rootGrid.Background = background;
		}
	}

	public void ApplyRect(OverlayRect rect)
	{
		double scale = Taskbar.Dpi / 96.0;

		// AppWindow.MoveAndResize expects physical pixels for ALL 4 fields.
		// The WinUIEx wrapper helpfully does (int)x, (int)y, but (int)(width*scale)
		// and (int)(height*scale). To get physical pixels out for width/height
		// we therefore pass them as DIPs (width / scale, height / scale).
		this.MoveAndResize(
	  rect.ScreenX,
	  rect.ScreenY,
	  rect.Width / scale,
	  rect.Height / scale);

	}

	public void UpdateFreeZone(FreeZone zone) => CurrentFreeZone = zone;

	public void SetVisible(bool visible)
	{
		_userIntendedVisible = visible;
		if (visible)
		{
			AppWindow.Show(false);
			ReassertTopmost();
			_topmostTimer.Start();
		}
		else
		{
			_topmostTimer.Stop();
			AppWindow.Hide();
		}
	}

	public void EnableDrag(bool enable)
	{
		if (enable == _dragEnabled) return;
		_dragEnabled = enable;
		if (enable) WireDragHandlers();
		else UnwireDragHandlers();
	}

	public IntPtr GetWindowHandle() => ((WindowEx)this).GetWindowHandle();

	public void DetachContent() => _rootGrid.Children.Clear();

	// ── Topmost enforcement ───────────────────────────────────────────────

	private void ReassertTopmost()
	{
		IntPtr hwnd = this.GetWindowHandle();
		if (hwnd == IntPtr.Zero) return;

		SetWindowPos(hwnd, NativeMethods.HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
		SetWindowPos(hwnd, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
	}

	private bool IsAboveTaskbar()
	{
		IntPtr self = this.GetWindowHandle();
		if (self == IntPtr.Zero) return true;

		IntPtr taskbar = Taskbar.Hwnd;
		if (taskbar == IntPtr.Zero) return true;

		const int MaxHops = 256;
		IntPtr cur = taskbar;
		for (int i = 0; i < MaxHops; i++)
		{
			cur = NativeMethods.GetWindow(cur, NativeMethods.GW_HWNDPREV);
			if (cur == IntPtr.Zero) return false;
			if (cur == self) return true;
		}

		return false;
	}

	// ── Drag handlers ─────────────────────────────────────────────────────

	private void WireDragHandlers()
	{
		_rootGrid.PointerPressed += RootGrid_PointerPressed;
		_rootGrid.PointerMoved += RootGrid_PointerMoved;
		_rootGrid.PointerReleased += RootGrid_PointerReleased;
		_rootGrid.PointerCaptureLost += RootGrid_PointerCaptureLost;
	}

	private void UnwireDragHandlers()
	{
		_rootGrid.PointerPressed -= RootGrid_PointerPressed;
		_rootGrid.PointerMoved -= RootGrid_PointerMoved;
		_rootGrid.PointerReleased -= RootGrid_PointerReleased;
		_rootGrid.PointerCaptureLost -= RootGrid_PointerCaptureLost;
	}

	private void RootGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
	{
		if (!_dragEnabled) return;
		if (!e.GetCurrentPoint(_rootGrid).Properties.IsLeftButtonPressed) return;
		if (e.OriginalSource is Button) return;

		if (_rootGrid.CapturePointer(e.Pointer))
		{
			_isDragging = true;
			GetCursorPos(out POINT pt);
			_dragStartScreenX = pt.X;

			GetWindowRect(this.GetWindowHandle(), out RECT cur);
			_windowScreenXAtDragStart = cur.Left;
			e.Handled = true;
		}
	}

	private void RootGrid_PointerMoved(object sender, PointerRoutedEventArgs e)
	{
		if (!_isDragging) return;
		GetCursorPos(out POINT pt);
		int deltaScreenX = pt.X - _dragStartScreenX;

		GetWindowRect(this.GetWindowHandle(), out RECT curRect);
		int widthPx = curRect.Width;
		int proposedX = _windowScreenXAtDragStart + deltaScreenX;
		int clampedX = Math.Clamp(proposedX,
					CurrentFreeZone.ScreenLeft,
					CurrentFreeZone.ScreenRight - widthPx);

		double scale = Taskbar.Dpi / 96.0;
		this.MoveAndResize(
		  clampedX,
		  Taskbar.TaskbarBoundsScreen.Top,
		  widthPx / scale,
		  curRect.Height / scale);

		e.Handled = true;
	}

	private void RootGrid_PointerReleased(object sender, PointerRoutedEventArgs e)
	{
		EndDrag();
		e.Handled = true;
	}

	private void RootGrid_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
	  => EndDrag();

	private void EndDrag()
	{
		if (!_isDragging) return;
		_isDragging = false;
		_windowScreenXAtDragStart = 0;
		GetWindowRect(this.GetWindowHandle(), out RECT cur);
		DragCommitted?.Invoke(this, cur.Left);
	}
}
