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
		navService?.Initialize(NavView, NavFrame, NavigationPageMappings.PageDictionary)
			.ConfigureDefaultPage(typeof(MainPlayerPage))
			.ConfigureSettingsPage(typeof(SettingsPage))
			.ConfigureJsonFile("Assets/NavViewMenu/AppData.json")
			.ConfigureTitleBar(AppTitleBar);
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
		ErrorMessage.Text = "";
		AddPlaylistDialog.IsPrimaryButtonEnabled = false;
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
	/// Handles the event triggered when the text in the playlist name input box changes.
	/// </summary>
	/// <param name="sender">The object that raised the event, typically the PlaylistNameBox TextBox control.</param>
	/// <param name="e">Arguments that describe the change in the text content of the TextBox.</param>
	/// <remarks>
	/// This method validates the input for the playlist name based on several criteria, such as:
	/// ensuring the name is not empty,
	/// avoiding reserved system names,
	/// disallowing invalid characters,
	/// checking for maximum length, and
	/// verifying that the name does not already exist in the list of playlists.
	/// If validation fails, an appropriate error message will be displayed and the "Add" button
	/// in the playlist creation dialog will be disabled. Otherwise, the "Add" button will be enabled.
	/// </remarks>
	private void OnPlaylistNameChanged(object sender, TextChangedEventArgs e)
	{
		string name = PlaylistNameBox.Text.Trim();
		ErrorMessage.Text = "";
		AddPlaylistDialog.IsPrimaryButtonEnabled = false;

		ErrorMessage.Text = name switch
		{
			_ when string.IsNullOrWhiteSpace(name) => "Playlist name cannot be empty",
			_ when new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" }.Contains(name.ToUpper()) => "This name is reserved by the system",
			_ when name.Any(c => Path.GetInvalidFileNameChars().Contains(c)) => "Name contains invalid characters: \\ / : * ? \" < > |",
			_ when name.EndsWith(" ") || name.EndsWith(".") => "Name cannot end with a period",
			_ when name.Length > 255 => "Name is too long",
			_ when playLists != null && playLists.Contains(name) => "Playlist name already exists",
			_ => ""
		};

		AddPlaylistDialog.IsPrimaryButtonEnabled = ErrorMessage.Text == "";
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
	private async void Page_Loaded(object sender, RoutedEventArgs e)
	{
		AddPlayLists();
		HidePreDefinedPlayLists();
		HidePreDefinedLibraries();
		await Task.Delay(1000);
		NavView.IsPaneOpen = false;
	}

	/// <summary>
	/// Hides predefined library views based on the user's saved preferences.
	/// </summary>
	/// <remarks>
	/// This method retrieves the library visibility preferences stored in application settings and hides the corresponding libraries in the UI.
	/// It checks whether libraries such as "Artists", "Albums", "Genres", and "Years" are enabled, and if not, calls the <c>HideLibrary</c> method to remove them from view.
	/// </remarks>
	private void HidePreDefinedLibraries()
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

		if (!bool.Parse(localSettings.Values[nameof(LocalSave.ArtistsEnabled)]?.ToString() ?? "true")) HideLibrary("Artists");
		if (!bool.Parse(localSettings.Values[nameof(LocalSave.AlbumsEnabled)]?.ToString() ?? "true")) HideLibrary("Albums");
		if (!bool.Parse(localSettings.Values[nameof(LocalSave.GenresEnabled)]?.ToString() ?? "true")) HideLibrary("Genres");
		if (!bool.Parse(localSettings.Values[nameof(LocalSave.YearsEnabled)]?.ToString() ?? "true")) HideLibrary("Years");
	}

	/// <summary>
	/// Hides predefined playlists based on user preferences stored in application settings.
	/// </summary>
	/// <remarks>
	/// This method checks the local application settings to determine which predefined playlists
	/// (e.g., "Recently Added", "Recently Played", "Most Played") should be hidden. If the corresponding
	/// settings indicate that a playlist is disabled, it invokes the <c>HidePlayList</c> method to hide the playlist.
	/// </remarks>
	private void HidePreDefinedPlayLists()
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

		if (!bool.Parse(localSettings.Values[nameof(LocalSave.RecentlyAddedEnabled)]?.ToString() ?? "true")) HidePlayList("Recently Added");
		if (!bool.Parse(localSettings.Values[nameof(LocalSave.RecentlyPlayedEnabled)]?.ToString() ?? "true")) HidePlayList("Recently Played");
		if (!bool.Parse(localSettings.Values[nameof(LocalSave.MostPlayedEnabled)]?.ToString() ?? "true")) HidePlayList("Most Played");
	}

	/// <summary>
	/// Hides a specific library from the application's navigation view.
	/// </summary>
	/// <param name="libraryName">
	/// The name of the library to be hidden. This should correspond to the tag of the navigation item representing the library.
	/// </param>
	/// <remarks>
	/// This method attempts to locate the navigation item corresponding to the specified library in the application's navigation menu and sets its visibility to collapsed.
	/// It also removes the associated page from the navigation history.
	/// In case of an exception during this process, the method retries after a brief delay.
	/// </remarks>
	private async void HideLibrary(string libraryName)
	{
		try
		{
			var librariesGroup = App.Current.NavService.MenuItems[1] as NavigationViewItem;
			var libraryNavigationItem = librariesGroup?.MenuItems.Select(x => x as NavigationViewItem).FirstOrDefault(x => x?.Tag.ToString() == $"Tunetastic.Views.LibraryViews.{libraryName}ViewPage");
			if (libraryNavigationItem != null) libraryNavigationItem.Visibility = Visibility.Collapsed;
			RemovePageFromHistory(libraryName);
		}
		catch (Exception)
		{
			await Task.Delay(100);
			HideLibrary(libraryName);
		}
	}

	/// <summary>
	/// Hides the specified playlist from the navigation menu.
	/// </summary>
	/// <param name="playlistName">The name of the playlist to be hidden.</param>
	/// <remarks>
	/// This method locates the specified playlist in the navigation menu and sets its visibility to collapsed.
	/// It also removes the playlist's page from the navigation history.
	/// If an exception occurs, a retry mechanism is implemented with a delay.
	/// </remarks>
	private async void HidePlayList(string playlistName)
	{
		try
		{
			var playlistsGroup = App.Current.NavService.MenuItems[2] as NavigationViewItem;
			var playListNavigationItem = playlistsGroup?.MenuItems.Select(x => x as NavigationViewItem).FirstOrDefault(x => x?.Tag.ToString() == $"Tunetastic.Views.PlaylistViews.{playlistName.Replace(" ", "")}");
			if (playListNavigationItem != null) playListNavigationItem.Visibility = Visibility.Collapsed;
			RemovePageFromHistory(playlistName);
		}
		catch (Exception)
		{
			await Task.Delay(100);
			HidePlayList(playlistName);
		}
	}

	/// <summary>
	/// Removes a specified page from the navigation history.
	/// </summary>
	/// <param name="pageName">
	/// The name of the page to be removed from the navigation backstack. This should match the parameter used when the page was originally navigated to.
	/// </param>
	/// <remarks>
	/// This method iterates through the navigation backstack and removes any entry that matches the provided page name.
	/// This is typically used to ensure that certain pages are not accessible via the back navigation once they are hidden or disabled in the UI.
	/// </remarks>
	public async void RemovePageFromHistory(string pageName)
	{
		var history = NavFrame.BackStack;
		for (int i = history.Count - 1; i >= 0; i--)
		{
			if (history[i].Parameter.ToString() == pageName)
			{
				history.RemoveAt(i);
			}
		}
	}
}
