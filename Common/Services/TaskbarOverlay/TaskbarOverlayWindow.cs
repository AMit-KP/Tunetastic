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

	// ── Drag state ────────────────────────────────────────────────────────
	private bool _dragEnabled;
	private bool _isDragging;
	private int _dragStartScreenX;

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
			Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(1,0,0,0)),
			ManipulationMode = ManipulationModes.None,
		};
		_rootGrid.Children.Add(content);
		Content = _rootGrid;

		// Timer that re-asserts topmost every 20ms.
		_topmostTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(20)
		};
		_topmostTimer.Tick += (_, _) => ReassertTopmost();

		Closed += (_, _) => _topmostTimer.Stop();
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
		uint colorNone = NativeMethods.DWMWA_COLOR_NONE;
		NativeMethods.DwmSetWindowAttribute(hwnd, NativeMethods.DWMWA_BORDER_COLOR, in colorNone, sizeof(uint));

		// Extend frame fully into client area to eliminate any residual border
		var margins = new NativeMethods.MARGINS { LeftWidth = -1, RightWidth = -1, TopHeight = -1, BottomHeight = -1 };
		NativeMethods.DwmExtendFrameIntoClientArea(hwnd, in margins);

		ReassertTopmost();
		ApplyRect(_pendingRect);
		_topmostTimer.Start();
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

	// ── Topmost enforcement ───────────────────────────────────────────────

	private void ReassertTopmost()
	{
		IntPtr hwnd = this.GetWindowHandle();
		if (hwnd == IntPtr.Zero) return;

		SetWindowPos(hwnd, NativeMethods.HWND_TOPMOST,
		  0, 0, 0, 0,
		  SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
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
