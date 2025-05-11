using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using WinRT.Interop;
using WinUIEx;

namespace Tunetastic.Views;
public sealed partial class MainWindow : WindowEx
{
	public MainViewModel ViewModel { get; }
	private OverlappedPresenter overlappedPresenter;
	public AppWindow CurrentAppWindow { get; }

	/// <summary>
	/// Represents the main application window for the Tunetastic application.
	/// </summary>
	/// <remarks>
	/// This class serves as the entry point for the application's UI. It initializes necessary components,
	/// sets up the application's main view model, and configures application window settings such as title and presenter.
	/// </remarks>
	public MainWindow()
	{
		ViewModel = App.GetService<MainViewModel>();
		this.InitializeComponent();

		CurrentAppWindow = this.AppWindow;
		ExtendsContentIntoTitleBar = true;
		overlappedPresenter = ((OverlappedPresenter)AppWindow.Presenter);

		Activated += MainWindow_Activated;
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

		// Piecewise linear interpolation for base factor at 100% zoom:
		if (screenHeight <= 1440)
		{
			// Between 768px and 1440px:
			// At 768    => factor = 0.9,
			// At 1440   => factor = 0.6.
			baseFactor = 0.9 - 0.3 * ((screenHeight - 768) / (1440 - 768));
		}
		else if (screenHeight <= 2400)
		{
			// Between 1440px and 2400px:
			// At 1440   => factor = 0.6,
			// At 2400   => factor = 0.35 (rough average between 0.3 and 0.4).
			baseFactor = 0.6 - 0.25 * ((screenHeight - 1440) / (2400 - 1440));
		}
		else
		{
			// For screens taller than 2400px, you can default to the factor at 2400px.
			baseFactor = 0.35;
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

