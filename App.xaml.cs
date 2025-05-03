using Tunetastic.Services;

namespace Tunetastic;

public partial class App : Application
{
    public new static App Current => (App)Application.Current;
    public static Window MainWindow = Window.Current;
    public static IntPtr Hwnd => WinRT.Interop.WindowNative.GetWindowHandle(MainWindow);
    public IServiceProvider Services { get; }
    public IJsonNavigationService NavService => GetService<IJsonNavigationService>();
    public IThemeService ThemeService => GetService<IThemeService>();
    public IRainbowFrame RainbowFrame => GetService<IRainbowFrame>();

    public static T GetService<T>() where T : class
    {
        if ((App.Current as App)!.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public App()
    {
        Services = ConfigureServices();
        this.InitializeComponent();

        // Enables Multicore JIT with the specified profile
        System.Runtime.ProfileOptimization.SetProfileRoot(Constants.RootDirectoryPath);
        System.Runtime.ProfileOptimization.StartProfile("Startup.Profile");
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IJsonNavigationService, JsonNavigationService>();

        services.AddTransient<MainViewModel>();
        services.AddSingleton<ContextMenuService>();
        services.AddTransient<SettingViewModel>();
        services.AddTransient<MusicControlViewModel>();
        services.AddSingleton<IRainbowFrame, RainbowFrame>();

        return services.BuildServiceProvider();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();

        MainWindow.Title = MainWindow.AppWindow.Title = ProcessInfoHelper.ProductName;
        MainWindow.AppWindow.SetIcon("Assets/AppIcon.ico");

        var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

        ThemeService.Initialize(MainWindow, false);
        ThemeService.SetElementTheme(Enum.Parse<ElementTheme>(localSettings.Values[nameof(LocalSave.Theme)]?.ToString() ?? "Default"));
        var backdrop = localSettings.Values[nameof(LocalSave.Backdrop)]?.ToString() ?? "Mica";
        ThemeService.SetBackdropType(Enum.Parse<BackdropType>(backdrop));
        ThemeService.UpdateCaptionButtons();

        if (backdrop == "Mica" && bool.Parse(localSettings.Values[nameof(LocalSave.BackdropTintColorStatus)]?.ToString() ?? "false"))
        {
            var color = Windows.UI.Color.FromArgb(a: byte.Parse(localSettings.Values[nameof(LocalSave.BackdropTintColorA)]?.ToString() ?? "255"),
                                                  r: byte.Parse(localSettings.Values[nameof(LocalSave.BackdropTintColorR)]?.ToString() ?? "32"),
                                                  g: byte.Parse(localSettings.Values[nameof(LocalSave.BackdropTintColorG)]?.ToString() ?? "32"),
                                                  b: byte.Parse(localSettings.Values[nameof(LocalSave.BackdropTintColorB)]?.ToString() ?? "32"));

            App.Current.ThemeService.SetBackdropTintColor(color);
        }


        MainWindow.Activate();

        await new GetMusicDataService().UpdateMetaData();

        RainbowFrame.Initialize(App.MainWindow);
        InitializeApp();
        if (bool.Parse(localSettings.Values[nameof(LocalSave.RainbowFrameStatus)]?.ToString() ?? "false") && !bool.Parse(localSettings.Values[nameof(LocalSave.RainbowOnlyDuringPlayback)]?.ToString() ?? "false"))
        {
            RainbowFrame.StartRainbowFrame();
            RainbowFrame.UpdateEffectSpeed(51 - int.Parse(localSettings.Values[nameof(LocalSave.RainbowFrameSpeed)]?.ToString() ?? "31"));
        }
        MainWindow.Closed += (s, e) => MusicPlayer.Instance.SavePlayBackPosition();
    }

    private async void InitializeApp()
    {
        var menuService = GetService<ContextMenuService>();
        if (menuService != null && RuntimeHelper.IsPackaged())
        {
            ContextMenuItem menu = new ContextMenuItem
            {
                Title = "Open Tunetastic Here",
                Param = @"""{path}""",
                AcceptFileFlag = (int)FileMatchFlagEnum.All,
                AcceptDirectoryFlag = (int)(DirectoryMatchFlagEnum.Directory | DirectoryMatchFlagEnum.Background | DirectoryMatchFlagEnum.Desktop),
                AcceptMultipleFilesFlag = (int)FilesMatchFlagEnum.Each,
                Index = 0,
                Enabled = true,
                Icon = ProcessInfoHelper.GetFileVersionInfo().FileName,
                Exe = "Tunetastic.exe"
            };

            await menuService.SaveAsync(menu);
        }
    }
}

