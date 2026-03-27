using System.IO.Pipes;
using WinUIEx;

namespace Tunetastic;

public partial class App : Application
{
	public new static App Current => (App)Application.Current;
	public static Window MainWindow = null!;
	public static IntPtr Hwnd => WinRT.Interop.WindowNative.GetWindowHandle(MainWindow);
	public IServiceProvider Services { get; }
	public IJsonNavigationService NavService => GetService<IJsonNavigationService>();
	public IThemeService ThemeService => GetService<IThemeService>();
	public IRainbowFrame RainbowFrame => GetService<IRainbowFrame>();
	public static System.Windows.Forms.NotifyIcon TrayIcon { get; private set; } = new System.Windows.Forms.NotifyIcon
	{
		Icon = new System.Drawing.Icon(Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico")),
		Visible = true,
		Text = "Tunetastic",
	};

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

		services.AddSingleton<ContextMenuService>();
		services.AddTransient<SettingViewModel>();
		services.AddSingleton<MusicControlViewModel>();
		services.AddSingleton<IRainbowFrame, RainbowFrame>();

		return services.BuildServiceProvider();
	}

	/// <summary>
	/// Handles the application's launch process by setting up the main window, initializing services, applying user-defined theme and backdrop settings,
	/// starting necessary background tasks, and navigating to the initial page.
	/// </summary>
	/// <param name="args">Contains event data related to the application's launch event.</param>
	protected override async void OnLaunched(LaunchActivatedEventArgs args)
	{
		MainWindow = new MainWindow();
		MainWindow.Title = MainWindow.AppWindow.Title = ProcessInfoHelper.ProductName;
		MainWindow.AppWindow.SetIcon("Assets/AppIcon.png");

		var rootFrame = new Frame();
		MainWindow.Content = rootFrame;

		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

		ThemeService.Initialize(MainWindow);
		await ThemeService.SetElementThemeAsync(Enum.Parse<ElementTheme>(localSettings.Values[nameof(LocalSave.Theme)]?.ToString() ?? "Default"));
		var backdrop = localSettings.Values[nameof(LocalSave.Backdrop)]?.ToString() ?? "Acrylic";
		await ThemeService.SetBackdropTypeAsync(Enum.Parse<BackdropType>(backdrop));

		rootFrame.Navigate(typeof(Views.SplashScreen));

		MainWindow.Activate();

		if (backdrop == "Mica" && bool.Parse(localSettings.Values[nameof(LocalSave.BackdropTintColorStatus)]?.ToString() ?? "false"))
		{
			var color = Windows.UI.Color.FromArgb(a: byte.Parse(localSettings.Values[nameof(LocalSave.BackdropTintColorA)]?.ToString() ?? "255"),
												  r: byte.Parse(localSettings.Values[nameof(LocalSave.BackdropTintColorR)]?.ToString() ?? "32"),
												  g: byte.Parse(localSettings.Values[nameof(LocalSave.BackdropTintColorG)]?.ToString() ?? "32"),
												  b: byte.Parse(localSettings.Values[nameof(LocalSave.BackdropTintColorB)]?.ToString() ?? "32"));

			App.Current.ThemeService.GetMicaSystemBackdrop().TintColor = color;
		}

		bool scanAtStartup = bool.Parse(localSettings.Values[nameof(LocalSave.ScanAtStartup)]?.ToString() ?? "false");
		if (scanAtStartup)
			await new GetMusicData().UpdateMetaData();
		else
			await Task.Delay(500);

		await DatabaseHelper.Instance.InitializeDatabase();

		rootFrame.Navigate(typeof(MainPage));

		RainbowFrame.Initialize(App.MainWindow);
		InitializeApp();

		if (bool.Parse(localSettings.Values[nameof(LocalSave.RainbowFrameStatus)]?.ToString() ?? "false") && !bool.Parse(localSettings.Values[nameof(LocalSave.RainbowOnlyDuringPlayback)]?.ToString() ?? "false"))
		{
			RainbowFrame.StartRainbowFrame();
			RainbowFrame.UpdateEffectSpeed(51 - int.Parse(localSettings.Values[nameof(LocalSave.RainbowFrameSpeed)]?.ToString() ?? "31"));
		}
		MainWindow.Closed += (s, e) => MusicPlayer.Instance.SavePlayBackPosition();

		StartInstanceListener();
	}

	/// <summary>
	/// Initiates a listener for external instances of the application communicating through
	/// a named pipe. This ensures that only one instance of the application remains active,
	/// and restores the main window if another instance attempts to launch.
	/// </summary>
	private static void StartInstanceListener()
	{
		_ = Task.Run(async () =>
		{
			while (true)
			{
				try
				{
					using var server = new NamedPipeServerStream("Tunetastic.InstancePing", PipeDirection.In);
					await server.WaitForConnectionAsync().ConfigureAwait(false);

					using var reader = new StreamReader(server);
					var message = await reader.ReadLineAsync().ConfigureAwait(false);

					if (message == "PING" && MainWindow is not null)
					{
						MainWindow.DispatcherQueue.TryEnqueue(() =>
						{
							MainWindow.Restore();
						});
					}
				}
				catch
				{
					//Ignore
				}
			}
		});
	}

	/// <summary>
	/// Configures and initializes application-specific settings and services,
	/// such as context menu customization, enabling additional functionalities
	/// for better user experience.
	/// </summary>
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

