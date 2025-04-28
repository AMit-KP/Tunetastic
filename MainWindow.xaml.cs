using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using Windows.UI.ViewManagement;
using WinRT.Interop;
using WinUIEx;

namespace Tunetastic.Views;
public sealed partial class MainWindow : WindowEx
{
    public MainViewModel ViewModel { get; }
    private DispatcherQueue dispatcherQueue;
    private OverlappedPresenter overlappedPresenter;
    public MainWindow()
    {
        ViewModel = App.GetService<MainViewModel>();
        this.InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

        overlappedPresenter = ((OverlappedPresenter)AppWindow.Presenter);

        var navService = App.GetService<IJsonNavigationService>() as JsonNavigationService;
        if (navService != null)
        {
            navService.Initialize(NavView, NavFrame, NavigationPageMappings.PageDictionary)
                .ConfigureDefaultPage(typeof(MainPlayerPage))
                .ConfigureSettingsPage(typeof(SettingsPage))
                .ConfigureJsonFile("Assets/NavViewMenu/AppData.json")
                .ConfigureTitleBar(AppTitleBar)
                .ConfigureBreadcrumbBar(BreadCrumbNav, BreadcrumbPageMappings.PageDictionary);
        }
        MusicControlsArea.Navigate(typeof(MusicControl));

        // Theme change code picked from https://github.com/microsoft/WinUI-Gallery/pull/1239
        dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        settings = new UISettings();
        //settings.ColorValuesChanged += Settings_ColorValuesChanged;// cannot use FrameworkElement.ActualThemeChanged event

        Activated += MainWindow_Activated;
    }

    private void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        ThemeService.ChangeThemeWithoutSave(App.MainWindow);
    }

    private void OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        AutoSuggestBoxHelper.OnITitleBarAutoSuggestBoxTextChangedEvent(sender, args, NavFrame);
    }

    private void OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        AutoSuggestBoxHelper.OnITitleBarAutoSuggestBoxQuerySubmittedEvent(sender, args, NavFrame);
    }

    private readonly UISettings settings;


    // this handles updating the caption button colors correctly when indows system theme is changed
    // while the app is open
    //private void Settings_ColorValuesChanged(UISettings sender, object args) =>
    // This calls comes off-thread, hence we will need to dispatch it to current app's thread
    //dispatcherQueue.TryEnqueue(() =>
    //{
    //    //TitleBarHelper.ApplySystemThemeToCaptionButtons();
    //});

    private bool centered;
    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (this.centered is false)
        {
            Center(this);
            centered = true;
        }
    }

    [DllImport("User32.dll")]
    public static extern int GetDpiForWindow(IntPtr hwnd);


    private void Center(Window window)
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

