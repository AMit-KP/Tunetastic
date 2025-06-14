using System.Text.RegularExpressions;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Input;
using Tunetastic.Views.PlaylistViews;
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
	/// It ensures integration with the application's window and navigation system, such as setting the title bar and initializing the music controls area.
	/// </remarks>
	public MainPage()
	{
		this.InitializeComponent();
		_instance = this;
		App.MainWindow.ExtendsContentIntoTitleBar = true;
		App.MainWindow.SetTitleBar(AppTitleBar);
		var mainWin = App.MainWindow as MainWindow;

		if (mainWin?.CurrentAppWindow != null)
		{
			mainWin.CurrentAppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
		}

		var navService = App.GetService<IJsonNavigationService>() as JsonNavigationService;
		if (navService != null)
		{
			navService.Initialize(NavView, NavFrame, NavigationPageMappings.PageDictionary)
				.ConfigureDefaultPage(typeof(MainPlayerPage))
				.ConfigureSettingsPage(typeof(SettingsPage))
				.ConfigureJsonFile("Assets/NavViewMenu/AppData.json")
				.ConfigureTitleBar(AppTitleBar);
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
	private async void ThemeButton_Click(object sender, RoutedEventArgs e)
	{
		ThemeService.ChangeThemeWithoutSave(App.MainWindow);
		await Task.Delay(100);
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
	private async void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
	{
		if (args.SelectedItem is NavigationViewItem selectedItem)
		{
			string? selectedTag = selectedItem.Tag.ToString();
			IsMainPlayerPageOpened = (selectedTag == "Library") || (selectedTag == "Playlists") || (selectedTag == "AddNewPlaylist") ? IsMainPlayerPageOpened : selectedTag == "Tunetastic.Views.MainPlayerPage";
		}
	}

	/// <summary>
	/// Handles the ItemInvoked event triggered by the NavigationView.
	/// </summary>
	/// <remarks>
	/// This method is invoked when an item in the NavigationView is selected by the user.
	/// It checks the invoked item's text and performs an action if it matches a specific condition,
	/// such as showing a dialog for adding a new playlist.
	/// </remarks>
	/// <param name="sender">The NavigationView control that raised the event.</param>
	/// <param name="args">Event arguments containing details of the invoked item.</param>
	private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
	{
		if (args.InvokedItem is string itemText && itemText == "Add New Playlist")
		{
			ShowAddPlaylistDialog();
		}
	}

	private List<string>? playLists;

	/// <summary>
	/// Displays the dialog for adding a new playlist.
	/// </summary>
	/// <remarks>
	/// This method triggers the `AddPlaylistDialog` to become visible and sets its theme based on the current application theme.
	/// It clears any existing text in the input box, loads custom playlists from persistent storage,
	/// and upon user confirmation, adds the new playlist name to the current list and saves it back to storage.
	/// </remarks>
	private async void ShowAddPlaylistDialog()
	{
		AddPlaylistDialog.Visibility = Visibility.Visible;
		AddPlaylistDialog.RequestedTheme = App.Current.ThemeService.GetElementTheme();
		PlaylistNameBox.Text = string.Empty;
		playLists = await DatabaseHelper.Instance.GetAllPlaylistNames();

		ContentDialogResult result = await AddPlaylistDialog.ShowAsync();

		if (result == ContentDialogResult.Primary)
		{
			if (CreateNewPlaylist(PlaylistNameBox.Text.Trim()))
			{
				await DatabaseHelper.Instance.CreatePlaylist(PlaylistNameBox.Text.Trim());
			}
		}
		playLists = null;
	}

	/// <summary>
	/// Handles changes to the playlist name entered in the input box.
	/// </summary>
	/// <param name="sender">The source of the event, typically the TextBox control.</param>
	/// <param name="e">Provides data for the event when the text in the TextBox changes.</param>
	/// <remarks>
	/// This method checks if the entered playlist name already exists in the list of playlists.
	/// If it exists, it displays an error message and disables the "Add" button in the dialog.
	/// Otherwise, it hides the error message and enables the "Add" button if the input is not empty or whitespace.
	/// </remarks>
	private void OnPlaylistNameChanged(object sender, TextChangedEventArgs e)
	{
		if (playLists != null && playLists.Contains(PlaylistNameBox.Text.Trim()))
		{
			ErrorMessage.Visibility = Visibility.Visible;
			AddPlaylistDialog.IsPrimaryButtonEnabled = false;
		}
		else
		{
			ErrorMessage.Visibility = Visibility.Collapsed;
			AddPlaylistDialog.IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(PlaylistNameBox.Text.Trim());
		}

	}

	/// <summary>
	/// Creates a new playlist with the specified name and adds it to the navigation menu.
	/// </summary>
	/// <param name="playlistName">The name of the playlist to be created. It is used as the display label and navigation tag for the playlist.</param>
	/// <returns>
	/// A boolean value indicating whether the playlist was successfully created and added to the navigation menu.
	/// Returns true if the playlist is successfully added; otherwise, returns false.
	/// </returns>
	private bool CreateNewPlaylist(string playlistName)
	{
		var playlistsGroup = App.Current.NavService.MenuItems[2] as NavigationViewItem;
		var tag = "Tunetastic.Views.PlaylistViews." + Regex.Replace(playlistName, @"\s+", "_") + "CustomPlaylist";
		if (playlistsGroup != null)
		{
			DataGroup dataGroup = new();
			dataGroup.UniqueId = tag;
			dataGroup.Title = playlistName;

			NavigationViewItem newItem = new NavigationViewItem
			{
				Content = new TextBlock
				{
					Text = playlistName,
					TextTrimming = TextTrimming.CharacterEllipsis
				},
				Tag = tag,
				Icon = new FontIcon { Glyph = "\uE728" },
				DataContext = dataGroup
			};
			ToolTipService.SetToolTip(newItem, playlistName);

			var lastItem = playlistsGroup.MenuItems[playlistsGroup.MenuItems.Count - 1];
			playlistsGroup.MenuItems.Remove(lastItem);
			playlistsGroup.MenuItems.Add(newItem);
			playlistsGroup.MenuItems.Add(lastItem);

			NavigationPageMappings.PageDictionary.Add(tag, typeof(PlayListTemplate));

			return true;
		}
		return false;
	}

	/// <summary>
	/// Dynamically generates and adds playlist navigation items to the navigation menu.
	/// </summary>
	/// <remarks>
	/// This method retrieves playlist names from the database using the `DatabaseHelper` class
	/// and creates corresponding navigation view items for each playlist. It ensures that
	/// navigation items are properly integrated with relevant page mappings through
	/// the navigation system. If an exception occurs during execution, the method retries
	/// after a brief delay to ensure stability.
	/// </remarks>
	public async void AddPlayLists()
	{
		try
		{
			var playLists = await DatabaseHelper.Instance.GetAllPlaylistNames();

			var playlistsGroup = App.Current.NavService.MenuItems[2] as NavigationViewItem;
			var lastItem = playlistsGroup.MenuItems[playlistsGroup.MenuItems.Count - 1];
			playlistsGroup.MenuItems.Remove(lastItem);

			foreach (var playlistName in playLists)
			{
				var tag = "Tunetastic.Views.PlaylistViews." + Regex.Replace(playlistName, @"\s+", "_") + "CustomPlaylist";
				DataGroup dataGroup = new();
				dataGroup.UniqueId = tag;
				dataGroup.Title = playlistName;

				NavigationViewItem newItem = new NavigationViewItem
				{
					Content = new TextBlock
					{
						Text = playlistName,
						TextTrimming = TextTrimming.CharacterEllipsis
					},
					Tag = tag,
					Icon = new FontIcon { Glyph = "\uE728" },
					DataContext = dataGroup
				};
				ToolTipService.SetToolTip(newItem, playlistName);
				playlistsGroup.MenuItems.Add(newItem);
				NavigationPageMappings.PageDictionary.Add(tag, typeof(PlayListTemplate));
			}
			playlistsGroup.MenuItems.Add(lastItem);
		}
		catch (Exception)
		{
			await Task.Delay(100);
			AddPlayLists();
		}
	}

	/// <summary>
	/// Handles the Loaded event for the page.
	/// </summary>
	/// <param name="sender">The source of the event, typically the page itself.</param>
	/// <param name="e">The event data associated with the Loaded event.</param>
	/// <remarks>
	/// This method is triggered when the page is fully loaded and initializes page-specific configurations,
	/// such as dynamically adding playlists to the navigation menu through the AddPlayLists method.
	/// It ensures that the page is adequately prepared for user interaction upon loading.
	/// </remarks>
	private void Page_Loaded(object sender, RoutedEventArgs e)
	{
		AddPlayLists();
	}
}
