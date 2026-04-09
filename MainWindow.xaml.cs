using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using WinRT.Interop;
using WinUIEx;

namespace Tunetastic.Views;

public sealed partial class MainWindow : WindowEx
{
	private OverlappedPresenter overlappedPresenter;
	public AppWindow CurrentAppWindow { get; }
	public static MainWindow _instance = null!;

	/// <summary>
	/// Represents the primary window of the application and initializes its components and settings.
	/// </summary>
	/// <remarks>
	/// This class serves as the main interface for the application, configuring the application window's behavior (such as extending content to the title bar), and interacting with system tray settings.
	/// </remarks>
	public MainWindow()
	{
		InitializeComponent();
		_instance = this;

		CurrentAppWindow = this.AppWindow;
		ExtendsContentIntoTitleBar = true;
		overlappedPresenter = ((OverlappedPresenter)AppWindow.Presenter);

		Activated += MainWindow_Activated;

		#region Don't remove this causes thread issue for some unknown reason
		var _ = new System.Windows.Forms.ContextMenuStrip();
		#endregion

		AddTrayIcon();
		SetMinimizeBehaviour(bool.Parse(Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.MinimizeToTray)]?.ToString() ?? "true"));
	}

	public void AddTrayIcon()
	{
		uint iconId = 7823;
		if (App.TrayIcon is null)
		{
			App.TrayIcon = new SystemTrayIcon(iconId, Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico"), "Tunetastic");

		}
		App.TrayIcon.RightClick += OnTrayIconRightClick;
		App.TrayIcon.IsVisible = true;
	}

	public void RemoveTrayIcon()
	{
		if (App.TrayIcon is not null)
		{
			App.TrayIcon.RightClick -= OnTrayIconRightClick;
			App.TrayIcon.IsVisible = false;
			App.TrayIcon.Dispose();
			App.TrayIcon = null;
		}
	}

	private void OnTrayIconRightClick(SystemTrayIcon sender, SystemTrayIconEventArgs args)
	{
		var flyout = new MenuFlyout();
		flyout.Items.Add(new MenuFlyoutItem() { Text = "Tunetastic", IsEnabled = false });
		flyout.Items.Add(new MenuFlyoutSeparator());
		flyout.Items.Add(new MenuFlyoutItem() { Text = "Open" });
		flyout.Items.Add(new MenuFlyoutItem() { Text = "Exit" });

		((MenuFlyoutItem)flyout.Items[2]).Click += (s, e) => RestoreFromTray();
		((MenuFlyoutItem)flyout.Items[3]).Click += (s, e) => ExitApp();

		args.Flyout = flyout;
	}

	/// <summary>
	/// Configures the behavior of the application when it is minimized, determining whether the application minimizes to the system tray or behaves normally.
	/// </summary>
	/// <param name="minimizeToTray">A boolean value indicating whether the application should minimize to the system tray. If set to true, the application minimizes to the tray; otherwise, it minimizes normally.</param>
	private void SetMinimizeBehaviour(bool minimizeToTray)
	{
		Closed -= MainWindowClose;
		Closed -= MinimizeToTray;

		if (minimizeToTray)
			Closed += MinimizeToTray;
		else
			Closed += MainWindowClose;
	}

	/// <summary>
	/// Sets the minimize behavior for the application, determining whether it minimizes to the system tray or behaves normally, using a static context.
	/// </summary>
	/// <param name="minimizeToTray">A boolean value indicating whether the application should minimize to the system tray. If true, the application minimizes to the tray; otherwise, it minimizes normally.</param>
	public static void SetMinimizeBehaviourStatic(bool minimizeToTray)
	{
		_instance?.SetMinimizeBehaviour(minimizeToTray);
	}

	/// <summary>
	/// Handles the close event for the main application window.
	/// </summary>
	/// <param name="sender">The source of the close event, typically the main window instance.</param>
	/// <param name="args">Event data that provides information about the window close event.</param>
	private async void MainWindowClose(object sender, WindowEventArgs args)
	{
		args.Handled = true;
		this.Closed -= MainWindowClose;
		this.Closed -= MinimizeToTray;
		RemoveTrayIcon();
		await Task.Delay(100);

		await MusicPlayer.Instance.SaveOnExitActionsAsync();
		this.Close();
	}

	/// <summary>
	/// Minimizes the main application window to the system tray.
	/// </summary>
	/// <param name="sender">The source of the close event, typically the main window instance.</param>
	/// <param name="args">Event data that provides information about the window close event.</param>
	/// <remarks>
	/// When this method is invoked, the main application window is hidden from view and a notification is displayed
	/// to inform the user that the application has been minimized to the system tray. The application remains active
	/// and accessible through the system tray icon.
	/// </remarks>
	private void MinimizeToTray(object sender, WindowEventArgs args)
	{
		args.Handled = true;
		this.Hide();
		GlobalNotification.Info("Minimized to system tray");
	}

	/// <summary>
	/// Restores the main application window from the system tray and brings it to the foreground.
	/// </summary>
	/// <remarks>
	/// This method ensures that the application window is made visible and activated when it's restored from the system tray. It is typically invoked through the system tray context menu to allow the user to reopen the main window after minimizing it to the tray.
	/// </remarks>
	public void RestoreFromTray()
	{
		this.Show();
		this.Activate();
		this.BringToFront();
	}

	/// <summary>
	/// Exits the Tunetastic application entirely, removing the system tray icon and closing the main window.
	/// </summary>
	/// <remarks>
	/// This method is invoked to terminate the application. It detaches the handler for the window close event, makes the system tray icon invisible, and closes the main application window. After calling this method, the application process will end.
	/// </remarks>
	private async void ExitApp()
	{
		this.Closed -= MainWindowClose;
		this.Closed -= MinimizeToTray;
		RemoveTrayIcon();
		await MusicPlayer.Instance.SaveOnExitActionsAsync();
		this.Close();
	}

	private bool centered;

	/// <summary>
	/// Handles the event triggered when the main window is activated.
	/// </summary>
	/// <param name="sender">The source of the event, typically the main window instance.</param>
	/// <param name="args">Event data that provides information about the window activation state.</param>
	/// <remarks>
	/// Ensures that the main window is centered on the screen and its minimum size is set when the window is activated for the first time.
	/// </remarks>
	private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
	{
		if (this.centered is false)
		{
			SetWindowMinimumSizeAndToCenter(this);
			centered = true;
		}
	}

	/// <summary>
	/// Retrieves the DPI (Dots Per Inch) value for a specific window handle.
	/// </summary>
	/// <param name="hwnd">The handle to the window for which the DPI is being retrieved.</param>
	/// <returns>The DPI value for the given window handle.</returns>
	[DllImport("User32.dll")]
	public static extern int GetDpiForWindow(IntPtr hwnd);


	/// <summary>
	/// Sets the minimum size of the specified window and centers it on the screen.
	/// </summary>
	/// <param name="window">The window instance to adjust and center.</param>
	/// <remarks>
	/// This method calculates the appropriate minimum size for the given window based on the display area
	/// and ensures that the window is positioned at the center of the screen. It takes into account
	/// factors such as the DPI scaling and the screen's working area dimensions to determine
	/// the optimal positioning and size constraints.
	/// </remarks>
	private void SetWindowMinimumSizeAndToCenter(Window window)
	{
		IntPtr hWnd = WindowNative.GetWindowHandle(window);
		WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);

		if (AppWindow.GetFromWindowId(windowId) is AppWindow appWindow &&
			DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest) is DisplayArea displayArea)
		{
			int dpi = GetDpiForWindow(hWnd);
			double zoomFactor = dpi / 96.0;

			double screenWidth = displayArea.WorkArea.Width;
			double screenHeight = displayArea.WorkArea.Height;

			var minWidth = GetMinimumWidth(zoomFactor, screenWidth);
			var minHeight = GetMinimumHeight(zoomFactor, screenHeight);

			overlappedPresenter.PreferredMinimumWidth = minWidth > (int)screenWidth ? (int)screenWidth : minWidth;
			overlappedPresenter.PreferredMinimumHeight = minHeight > (int)screenHeight ? (int)screenHeight : minHeight;

			PointInt32 CenteredPosition = appWindow.Position;
			CenteredPosition.X = (displayArea.WorkArea.Width - appWindow.Size.Width) / 2;
			CenteredPosition.Y = (displayArea.WorkArea.Height - appWindow.Size.Height) / 2;
			appWindow.Move(CenteredPosition);
		}
	}

	#region App Window Size Calculation

	/// <summary>
	/// Calculates the minimum window height based on the given zoom factor and screen height.
	/// </summary>
	/// <param name="zoomFactor">The scaling factor, typically determined by DPI settings or display settings.</param>
	/// <param name="screenHeight">The height of the screen's work area in pixels.</param>
	/// <returns>The calculated minimum window height in pixels.</returns>
	private static int GetMinimumHeight(double zoomFactor, double screenHeight)
	{
		double baseFactor;

		switch (screenHeight)
		{
			// Piecewise linear interpolation for base factor at 100% zoom:
			case <= 1440:
				// Between 768px and 1440px:
				// At 768    => factor = 0.9,
				// At 1440   => factor = 0.6.
				baseFactor = 0.9 - 0.3 * ((screenHeight - 768) / (1440 - 768));
				break;
			case <= 2400:
				// Between 1440px and 2400px:
				// At 1440   => factor = 0.6,
				// At 2400   => factor = 0.35 (rough average between 0.3 and 0.4).
				baseFactor = 0.6 - 0.25 * ((screenHeight - 1440) / (2400 - 1440));
				break;
			default:
				// For screens taller than 2400px, you can default to the factor at 2400px.
				baseFactor = 0.35;
				break;
		}

		// Apply the zoom factor.
		double effectiveFactor = baseFactor * zoomFactor;

		// Clamp the effective factor to a maximum of 0.9.
		effectiveFactor = Math.Min(effectiveFactor, 0.9);

		int minHeight = (int)(screenHeight * effectiveFactor);
		return minHeight;
	}

	/// <summary>
	/// Calculates the minimum width for the window based on the provided zoom factor and screen width.
	/// </summary>
	/// <param name="zoomFactor">The scaling factor applied to the screen resolution (DPI scaling).</param>
	/// <param name="screenWidth">The width of the screen in pixels.</param>
	/// <returns>The calculated minimum width for the window in pixels.</returns>
	private static int GetMinimumWidth(double zoomFactor, double screenWidth)
	{
		double effectiveFactor;
		if (screenWidth > 1366)
		{
			// Define your resolution range and base factors:
			double minScreenWidth = 1366.0;
			double maxScreenWidth = 3840.0;

			double baseFactorAtMin = 0.6; // Desired factor at 1366 px and 100% zoom
			double baseFactorAtMax = 0.3; // Desired factor at 3840 px and 100% zoom

			// Calculate an interpolation (clamped between 0 and 1)
			double t = (screenWidth - minScreenWidth) / (maxScreenWidth - minScreenWidth);
			t = Math.Clamp(t, 0.0, 1.0);

			// Linear interpolation formula:
			// baseFactor = baseFactorAtMin + (baseFactorAtMax - baseFactorAtMin) * t
			double baseFactor = baseFactorAtMin + (baseFactorAtMax - baseFactorAtMin) * t;

			// Now adjust for zoom.
			// This calculation keeps baseFactor unchanged at 100% zoom,
			// and scales it up when zoomFactor > 1.
			effectiveFactor = (zoomFactor > 1.0 ? baseFactor * zoomFactor : baseFactor);
		}
		else
			effectiveFactor = 0.9;

		int minWidth = (int)(screenWidth * effectiveFactor);
		return minWidth;
	}
	#endregion
}

/// <summary>
/// Provides a custom renderer for the context menu in the application, incorporating support for light and dark themes.
/// </summary>
/// <remarks>
/// This class customizes the appearance of the context menu's background, menu items, and text to match the current application theme.
/// It extends <see cref="ToolStripRenderer"/> to override default rendering behavior and apply a modern visual style.
/// </remarks>
public class ModernMenuRenderer : ToolStripRenderer
{   //TODO: Needs work
	private bool IsDarkMode => App.Current.ThemeService.ActualTheme == ElementTheme.Dark;

	protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
	{
		Color backgroundColor = IsDarkMode ? Color.Black : Color.White;
		e.Graphics.FillRectangle(new SolidBrush(backgroundColor), e.Item.Bounds);
	}

	protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
	{
		Color backgroundColor = IsDarkMode ? Color.Black : Color.White;
		e.Graphics.Clear(backgroundColor);
	}

	protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
	{
		e.TextColor = IsDarkMode ? Color.White : Color.Black;
		base.OnRenderItemText(e);
	}
}
