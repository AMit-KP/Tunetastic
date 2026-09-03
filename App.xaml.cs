using System.IO.Pipes;
using WinUIEx;

namespace Tunetastic;

public partial class App : Application
{
	/// <summary>
	/// Gets the current application instance.
	/// </summary>
	public new static App Current => (App)Application.Current;

	/// <summary>
	/// Gets or sets the main window of the application.
	/// </summary>
	public static Window MainWindow = null!;

	/// <summary>
	/// Gets the window handle (HWND) of the main window.
	/// </summary>
	public static IntPtr Hwnd => WinRT.Interop.WindowNative.GetWindowHandle(MainWindow);

	/// <summary>
	/// Gets the service provider for dependency injection.
	/// </summary>
	public IServiceProvider Services { get; }

	/// <summary>
	/// Gets the JSON navigation service for page navigation.
	/// </summary>
	public IJsonNavigationService NavService => GetService<IJsonNavigationService>();

	/// <summary>
	/// Gets the theme service for UI theming.
	/// </summary>
	public IThemeService ThemeService => GetService<IThemeService>();

	/// <summary>
	/// Gets the rainbow frame service for visual effects.
	/// </summary>
	public IRainbowFrame RainbowFrame => GetService<IRainbowFrame>();

	/// <summary>
	/// Gets or sets the system tray icon for the application.
	/// </summary>
	public static SystemTrayIcon? TrayIcon { get; set; }

	/// <summary>
	/// Gets the audio service for media playback control.
	/// </summary>
	public AudioService AudioService { get; private set; } = null!;

	/// <summary>
	/// Retrieves a service of type <typeparamref name="T"/> from the application's service container.
	/// </summary>
	/// <typeparam name="T">The type of service to retrieve.</typeparam>
	/// <returns>An instance of the requested service type.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when the requested service type <typeparamref name="T"/> is not registered in the service container.
	/// </exception>
	public static T GetService<T>() where T : class
	{
		if ((App.Current as App)!.Services.GetService(typeof(T)) is not T service)
		{
			throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
		}

		return service;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="App"/> class.
	/// Sets up dependency injection services and initializes profiling.
	/// </summary>
	public App()
	{
		Services = ConfigureServices();
		this.InitializeComponent();

		// Enables Multicore JIT with the specified profile
		System.Runtime.ProfileOptimization.SetProfileRoot(Constants.RootDirectoryPath);
		System.Runtime.ProfileOptimization.StartProfile("Startup.Profile");
	}

	/// <summary>
	/// Configures the dependency injection services for the application.
	/// </summary>
	/// <returns>The configured service provider.</returns>
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
		AudioService = new AudioService();

		MainWindow = new MainWindow();
		MainWindow.Title = MainWindow.AppWindow.Title = ProcessInfoHelper.ProductName;
		MainWindow.AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.png"));

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
			var color = Windows.UI.Color.FromArgb(a: byte.Parse(localSettings.Values[nameof(LocalSave.BackdropTintColorA)]?.ToString() ?? "0"),
												  r: byte.Parse(localSettings.Values[nameof(LocalSave.BackdropTintColorR)]?.ToString() ?? "0"),
												  g: byte.Parse(localSettings.Values[nameof(LocalSave.BackdropTintColorG)]?.ToString() ?? "0"),
												  b: byte.Parse(localSettings.Values[nameof(LocalSave.BackdropTintColorB)]?.ToString() ?? "0"));

			App.Current.ThemeService.GetMicaSystemBackdrop().TintColor = color;
		}

		bool scanAtStartup = bool.Parse(localSettings.Values[nameof(LocalSave.ScanAtStartup)]?.ToString() ?? "false");
		await DatabaseHelper.Instance.InitializeDatabase();

		if (scanAtStartup)
			await new LibraryScanner().UpdateMetaData();
		else
			await Task.Delay(500);


		rootFrame.Navigate(typeof(MainPage));

		RainbowFrame.Initialize(App.MainWindow);

		InitializeApp();

		if (bool.Parse(localSettings.Values[nameof(LocalSave.RainbowFrameStatus)]?.ToString() ?? "false") && !bool.Parse(localSettings.Values[nameof(LocalSave.RainbowOnlyDuringPlayback)]?.ToString() ?? "false"))
		{
			RainbowFrame.StartRainbowFrame();
			RainbowFrame.UpdateEffectSpeed(51 - int.Parse(localSettings.Values[nameof(LocalSave.RainbowFrameSpeed)]?.ToString() ?? "31"));
		}

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
