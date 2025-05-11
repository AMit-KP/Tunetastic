using Microsoft.UI.Windowing;
using AutoSuggestBoxHelper = DevWinUI.AutoSuggestBoxHelper;

namespace Tunetastic.Views;

/// <summary>
/// Represents the primary page of the application.
/// This page acts as the main navigation hub and entry point for user interaction.
/// </summary>
public sealed partial class MainPage : Page
{
	/// <summary>
	/// Initializes a new instance of the MainPage class.
	/// </summary>
	/// <remarks>
	/// This constructor initializes the MainPage, configures the title bar, sets up navigation services, and handles page-specific configurations.
	/// It ensures integration with the application's window and navigation system, such as setting the title bar, configuring breadcrumbs, and initializing the music controls area.
	/// </remarks>
	public MainPage()
	{
		this.InitializeComponent();
		this.InitializeComponent();
		App.MainWindow.ExtendsContentIntoTitleBar = true;
		App.MainWindow.SetTitleBar(AppTitleBar);
		var mainWin = App.MainWindow as MainWindow; // ✅ Get MainWindow instance

		if (mainWin?.CurrentAppWindow != null)
		{
			mainWin.CurrentAppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall; // ✅ Apply preferred height from MainPage
		}


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
	}

	/// <summary>
	/// Handles the click event for the theme toggle button.
	/// </summary>
	/// <remarks>
	/// This method changes the application's theme dynamically without saving the selected theme preference.
	/// It interacts with the ThemeService to apply the theme change to the main application window.
	/// </remarks>
	/// <param name="sender">The source of the event, typically the ThemeButton.</param>
	/// <param name="e">The event data associated with the button click.</param>
	private void ThemeButton_Click(object sender, RoutedEventArgs e)
	{
		ThemeService.ChangeThemeWithoutSave(App.MainWindow);
		App.Current.ThemeService.UpdateCaptionButtons();
	}

	private void OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
	{
		AutoSuggestBoxHelper.OnITitleBarAutoSuggestBoxTextChangedEvent(sender, args, NavFrame);
	}

	private void OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
	{
		AutoSuggestBoxHelper.OnITitleBarAutoSuggestBoxQuerySubmittedEvent(sender, args, NavFrame);
	}
}
