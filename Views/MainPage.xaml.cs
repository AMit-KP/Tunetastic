using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Input;
using AutoSuggestBoxHelper = DevWinUI.AutoSuggestBoxHelper;
using TextBox = Microsoft.UI.Xaml.Controls.TextBox;

namespace Tunetastic.Views;

/// <summary>
/// Represents the primary page of the application.
/// This page acts as the main navigation hub and entry point for user interaction.
/// </summary>
public sealed partial class MainPage : Page
{
	public static MainPage? _instance;

	/// <summary>
	/// Event triggered when the main player page's visibility state changes.
	/// </summary>
	/// <remarks>
	/// This event allows subscribers to be notified whenever the user navigates to or away from
	/// the main player page, enabling components to adjust their behavior based on the navigation state.
	/// The event passes a boolean value indicating the new state: true if the main player page is opened, false otherwise.
	/// </remarks>
	public event EventHandler<bool>? MainPlayerPageOpened;

	private bool _isMainPlayerPageOpened = false;

	/// <summary>
	/// Indicates whether the main player page is currently opened.
	/// </summary>
	/// <value>
	/// A boolean value representing the visibility state of the main player page.
	/// Returns true if the main player page is currently active and visible, otherwise false.
	/// </value>
	/// <remarks>
	/// This property is used to track and respond to the navigation state of the application, particularly
	/// whether the user has navigated to the main player page. When the state changes, the
	/// <see cref="MainPlayerPageOpened"/> event is triggered, enabling observers to respond to page changes.
	/// </remarks>
	public bool IsMainPlayerPageOpened
	{
		get => _isMainPlayerPageOpened;
		set
		{
			if (_isMainPlayerPageOpened != value)
			{
				_isMainPlayerPageOpened = value;
				MainPlayerPageOpened?.Invoke(this, _isMainPlayerPageOpened);
			}
		}
	}

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
		_instance = this;
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
	/// Determines whether the search box in the main page is currently focused.
	/// </summary>
	/// <value>
	/// A boolean value indicating the focus state of the search box.
	/// Returns true if the currently focused element in the main page is a <see cref="Microsoft.UI.Xaml.Controls.TextBox"/>, otherwise false.
	/// </value>
	/// <remarks>
	/// This property is used to identify whether the search box is actively focused, allowing conditional logic based on user interaction
	/// with the search box. For instance, it is utilized to block specific key events, such as spacebar, when the search box is in focus.
	/// </remarks>
	public bool searchBoxFocused
	{
		get
		{
			return FocusManager.GetFocusedElement(XamlRoot) is TextBox;
		}
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

	/// <summary>
	/// Handles the selection changed event of the NavigationView component.
	/// </summary>
	/// <param name="sender">The NavigationView that raised the event.</param>
	/// <param name="args">The event data containing details about the selection change, such as the selected item.</param>
	/// <remarks>
	/// This method determines the tag of the selected NavigationViewItem and updates the state of the IsMainPlayerPageOpened property accordingly.
	/// It ensures that the application correctly tracks whether the MainPlayerPage is opened based on the selection.
	/// </remarks>
	private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
	{
		if (args.SelectedItem is NavigationViewItem selectedItem)
		{
			string? selectedTag = selectedItem.Tag.ToString();
			IsMainPlayerPageOpened = selectedTag == "Tunetastic.Views.MainPlayerPage";
		}
	}
}
