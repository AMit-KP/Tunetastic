using System.Text.RegularExpressions;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Storage.Pickers;
using Tunetastic.Views.LibraryViews;
using Tunetastic.Views.PlaylistViews;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Services.Store;
using Windows.Storage;
using Windows.Storage.Streams;
using TextBox = Microsoft.UI.Xaml.Controls.TextBox;

namespace Tunetastic.Views;

/// <summary>
/// Represents the primary page of the application.
/// This page acts as the main navigation hub and entry point for user interaction.
/// </summary>
public sealed partial class MainPage : Page
{
	public static MainPage? _instance;
	private bool _isUpdatingSlider = false;

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

		AppIcon.Source = new BitmapImage(new Uri(this.ActualTheme == ElementTheme.Dark ? "ms-appx:///Assets/Store/Logo_Dark.png" : "ms-appx:///Assets/Store/Logo_Light.png"));

		var navService = App.GetService<IJsonNavigationService>() as JsonNavigationService;
		navService?.Initialize(NavView, NavFrame, NavigationPageMappings.PageDictionary)
				   .ConfigureDefaultPage(typeof(MainPlayerPage))
				   .ConfigureSettingsPage(typeof(SettingsPage))
				   .ConfigureJsonFile("Assets/NavViewMenu/AppData.json")
				   .ConfigureTitleBar(AppTitleBar);
		MusicControlsArea.Navigate(typeof(MusicControl));

		InitializeVolumeSliderAndService();

		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		VersionInfo.Visibility = bool.Parse(localSettings.Values[nameof(LocalSave.ShowVersionInfoOnTitleBar)]?.ToString() ?? "true") ? Visibility.Visible : Visibility.Collapsed;

		if (bool.Parse(localSettings.Values[nameof(LocalSave.CheckForUpdatesAtStatup)]?.ToString() ?? "true"))
			CheckForUpdate();
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
			return Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot) is TextBox;
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
		await App.Current.ThemeService.SetElementThemeWithoutSaveAsync();
	}

	/// <summary>
	/// Handles changes in the text input of the AutoSuggestBox and updates its item source with relevant suggestions.
	/// </summary>
	/// <param name="sender">The AutoSuggestBox control that triggered the event.</param>
	/// <param name="args">Provides data about the TextChanged event, including the reason for the text change.</param>
	private async void OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
	{
		sender.ItemsSource = await GetSuggestions(sender.Text);
	}

	/// <summary>
	/// Handles the query submitted event from the AutoSuggestBox control.
	/// This method is triggered when the user finalizes their query, either by pressing enter or selecting a suggested item.
	/// </summary>
	/// <param name="sender">The source AutoSuggestBox control that triggered the event.</param>
	/// <param name="args">The event data containing the user's query text and details about the submitted query.</param>
	private async void OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
	{
		sender.ItemsSource = await GetSuggestions(args.QueryText);
	}

	/// <summary>
	/// Handles the event when a suggestion is selected from the AutoSuggestBox.
	/// </summary>
	/// <param name="sender">The AutoSuggestBox control triggering the event.</param>
	/// <param name="args">Contains details about the selected suggestion, including its type and value.</param>
	/// <remarks>
	/// This method determines the type of the selected suggestion (e.g., title, artist, or album) and navigates
	/// the application to the corresponding detail page. After navigation, the search box's text is updated
	/// or cleared to maintain the search functionality's state.
	/// </remarks>
	private void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
	{
		if (args.SelectedItem is KeyValuePair<SearchItemType, string> keyValuePair)
		{
			switch (keyValuePair.Key)
			{
				case SearchItemType.Title:
					App.Current.NavService.EnsureNavigationSelection("Tunetastic.Views.LibraryViews.AllSongsViewPage");
					App.Current.NavService.NavigateTo("Tunetastic.Views.LibraryViews.AllSongsViewPage", keyValuePair.Value);
					break;

				case SearchItemType.Artist:
					App.Current.NavService.EnsureNavigationSelection("Tunetastic.Views.LibraryViews.ArtistsViewPage");
					App.Current.NavService.NavigateTo(typeof(ArtistDetailPage), keyValuePair.Value == "Unknown" ? "Unknown Artist" : keyValuePair.Value, false);
					break;

				case SearchItemType.Album:
					App.Current.NavService.EnsureNavigationSelection("Tunetastic.Views.LibraryViews.AlbumsViewPage");
					App.Current.NavService.NavigateTo(typeof(AlbumDetailPage), keyValuePair.Value == "Unknown" ? "Unknown Album" : keyValuePair.Value, false);
					break;
			}
			sender.Text = keyValuePair.Value;
		}
		else
			sender.Text = String.Empty;
	}

	/// <summary>
	/// Retrieves a list of search suggestions based on the provided search text.
	/// </summary>
	/// <param name="searchText">The text input used to generate search suggestions.</param>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result contains
	/// a list of key-value pairs where the key is the type of the search item and the value is its corresponding name or details.
	/// </returns>
	private static async Task<List<KeyValuePair<SearchItemType, string>>> GetSuggestions(string searchText)
	{
		var SuggestionList = new List<KeyValuePair<SearchItemType, string>>();

		var result = await DatabaseHelper.Instance.Search(searchText, limitPerCategory: 3);
		foreach (var item in result.Items)
		{
			switch (item.Type)
			{
				case SearchItemType.Title:
					SuggestionList.Add(new(SearchItemType.Title, item.Title.Title + "\n" + item.Title.Artists));
					break;

				case SearchItemType.Artist:
					SuggestionList.Add(new(SearchItemType.Artist, item.Artist));
					break;

				case SearchItemType.Album:
					SuggestionList.Add(new(SearchItemType.Album, item.Album.Album));
					break;
			}
		}

		return SuggestionList;
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
			IsMainPlayerPageOpened = (selectedTag == "Library") || (selectedTag == "Playlists") || (selectedTag == "AddNewPlaylist") ? IsMainPlayerPageOpened : selectedTag == "Tunetastic.Views.MainPlayerPage";
		}
		if (args.SelectedItemContainer is NavigationViewItem navigationViewItem && Regex.IsMatch(navigationViewItem.Tag.ToString(), @"^Tunetastic\.Views\.PlaylistViews\.\S+CustomPlaylist$"))
		{
			App.Current.NavService.NavigateTo(typeof(PlayListTemplate), (navigationViewItem.DataContext as DataGroup).Title);
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
	/// This asynchronous method makes the `AddPlaylistDialog` visible and configures its properties,
	/// including the theme, primary button's state, and description text. It resets the input field,
	/// prepares any accompanying UI elements, and retrieves the existing playlist names from persistent storage.
	/// Upon user confirmation, it validates and handles the creation or addition of a new playlist.
	/// </remarks>
	private async void ShowAddPlaylistDialog()
	{
		AddPlaylistDialog.Visibility = Visibility.Visible;
		AddPlaylistDialog.RequestedTheme = App.Current.ThemeService.ElementTheme;
		PlaylistNameBox.Text = string.Empty;
		ErrorMessage.Text = "";
		AddPlaylistDialog.IsPrimaryButtonEnabled = false;
		var desc = new TextBlock();
		desc.Inlines.Add(new Run() { Text = "You can also select an existing playlist file." });
		desc.Inlines.Add(new LineBreak());
		desc.Inlines.Add(new Run() { Text = "Supported formats: M3U, M3U8, PLS, WPL, ZPL" });
		AddPlaylistDialogDescription.Text = desc.Text;
		AddPlaylistDialog.PrimaryButtonText = "Create";
		playLists = await DatabaseHelper.Instance.GetAllPlaylistNames();

		MainWindow._instance.WindowResizePermission(false);
		ContentDialogResult result = await AddPlaylistDialog.ShowAsync();
		MainWindow._instance.WindowResizePermission(true);

		if (result == ContentDialogResult.Primary)
		{
			if (CreateNewPlaylist(PlaylistNameBox.Text.Trim()))
			{
				await DatabaseHelper.Instance.CreatePlaylist(PlaylistNameBox.Text.Trim());
				GlobalNotification.Info($"{PlaylistNameBox.Text.Trim()} Playlist created.");
			}
			if (AddPlaylistDialog.PrimaryButtonText == "Add Playlist")
			{
				await DatabaseHelper.Instance.AddSongsToPlaylist(PlaylistNameBox.Text.Trim(), PlaylistFileSongs);
				GlobalNotification.Info($"{PlaylistNameBox.Text.Trim()} Playlist added with {PlaylistFileSongs.Count} {(PlaylistFileSongs.Count > 1 ? "songs/tracks" : "song/track")}.");
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
	/// This method iterates through the navigation backstack and removes any e Imntry that matches the provided page name.
	/// This is typically used to ensure that certain pages are not accessible via the back navigation once they are hidden or disabled in the UI.
	/// </remarks>
	public async void RemovePageFromHistory(string pageName)
	{
		var history = NavFrame.BackStack;
		for (int i = history.Count - 1; i >= 0; i--)
		{
			if (history[i].Parameter.ToString() == pageName || history[i].SourcePageType.FullName == pageName)
			{
				history.RemoveAt(i);
			}
		}
	}

	/// <summary>
	/// A collection of file paths corresponding to songs in the currently processed playlist.
	/// </summary>
	/// <remarks>
	/// This property holds a list of song paths that are selected from a playlist file during the import operation.
	/// It is primarily used when creating or updating a playlist to store the songs extracted from the imported playlist file.
	/// The content of this list gets updated dynamically based on user actions, such as importing a playlist through a file picker.
	/// </remarks>
	private List<string> PlaylistFileSongs { get; } = new();

	/// <summary>
	/// Handles the click event of the Browse button within the Add Playlist dialog.
	/// </summary>
	/// <param name="sender">The source of the event, typically the Browse button.</param>
	/// <param name="e">The event data associated with the button click.</param>
	/// <remarks>
	/// This method opens a file picker for the user to select a playlist file. It filters the file types to common playlist formats
	/// such as `.m3u`, `.m3u8`, `.pls`, `.wpl`, and `.zpl`. Once a file is selected, the method imports playlist details, including the
	/// name, total track count, and list of tracks found in the user's library. The imported information is then populated in the
	/// Add Playlist dialog for confirmation or further action by the user.
	/// </remarks>
	private async void BrowseButton_Click(object sender, RoutedEventArgs e)
	{
		//TODO: Drag n Drop
		var picker = new FileOpenPicker((sender as Button).XamlRoot.ContentIslandEnvironment.AppWindowId);

		picker.FileTypeFilter.Add("*.m3u");
		picker.FileTypeFilter.Add("*.m3u8");
		picker.FileTypeFilter.Add("*.pls");
		picker.FileTypeFilter.Add("*.wpl");
		picker.FileTypeFilter.Add("*.zpl");

		picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
		picker.CommitButtonText = "Import Playlist";

		var file = await picker.PickSingleFileAsync();
		if (file != null)
		{
			var (name, totalcount, songList) = await ImportExportPlaylist.ImportData(file.Path);

			var desc = new TextBlock();
			desc.Inlines.Add(new Run() { Text = "Do you want to import this playlist?" });
			desc.Inlines.Add(new LineBreak());
			desc.Inlines.Add(new Run() { Text = $"Playlist: {file.Path}" });
			desc.Inlines.Add(new LineBreak());
			desc.Inlines.Add(new Run() { Text = $"Tracks: {songList.Count} out of {totalcount} found in library" });

			PlaylistNameBox.Text = name;
			AddPlaylistDialogDescription.Text = desc.Text;
			AddPlaylistDialog.PrimaryButtonText = "Add Playlist";
			PlaylistFileSongs.Clear();
			PlaylistFileSongs.AddRange(songList);
		}
	}

	/// <summary>
	/// Handles the theme change event for the page.
	/// </summary>
	/// <param name="sender">The <see cref="FrameworkElement"/> that triggered the event.</param>
	/// <param name="args">The event data associated with the theme change.</param>
	/// <remarks>
	/// This method updates the caption buttons and adjusts the application icon source to reflect the new theme.
	/// It ensures that the icon dynamically switches between light and dark mode assets depending on the current theme.
	/// </remarks>
	private void Page_ActualThemeChanged(FrameworkElement sender, object args)
	{
		AppIcon.Source = new BitmapImage(new Uri(sender.ActualTheme == ElementTheme.Dark ? "ms-appx:///Assets/Store/Logo_Dark.png" : "ms-appx:///Assets/Store/Logo_Light.png"));
	}

	/// <summary>
	/// Toggles the animation state of the application title.
	/// </summary>
	/// <param name="startAnimation">A boolean indicating whether the animation should begin (true) or stop (false).</param>
	/// <remarks>
	/// This method adjusts the title's visual appearance by changing its stroke thickness and animation state.
	/// When <paramref name="startAnimation"/> is set to true, the title's stroke thickness is increased, and the animation is enabled, enhancing the visual effect.
	/// When set to false, the stroke thickness is reset, and the animation is disabled.
	/// </remarks>
	public void AnimateTitle(bool startAnimation)
	{
		AppTitle.StrokeThickness = startAnimation ? 1 : 0;
		AppTitle.Animate = startAnimation;
	}

	public async void ShowSongInfo(Song? songData)
	{
		if (songData != null)
		{
			SongTitle.Text = songData.Title;
			SongArtists.Text = songData.Artists;
			SongAlbum.Text = songData.Album;
			StorageFile file = await StorageFile.GetFileFromPathAsync(songData.Cover);
			using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read))
			{
				BitmapImage bitmapImage = new BitmapImage();
				await bitmapImage.SetSourceAsync(stream);
				SongCoverImage.Source = bitmapImage;
			}
			SongGenre.Text = songData.Genre;
			SongYear.Text = songData.Year;
			SongDuration.Text = new DurationToFullTimeConverter().Convert(songData.Duration, null, null, null).ToString();
			SongSize.Text = songData.FileSize;
			SongAdded.Text = new DateFormatConverter().Convert(songData.DateAdded, null, "ddd, dd MMM, yyyy", null).ToString() + " at " + new DateFormatConverter().Convert(songData.DateAdded, null, "T", null).ToString();
			SongPath.Text = songData.Path;
			SongChannel.Text = songData.AudioChannels;
			SongBitrate.Text = songData.AudioBitrate ?? string.Empty;
			SongSampleRate.Text = songData.AudioSampleRate ?? string.Empty;
			SongCodecDescription.Text = songData.AudioCodecDescription ?? string.Empty;
			SongPlayCount.Text = songData.PlayCount.ToString() ?? "0";
			if (songData.DateLastPlayed != null)
				SongLastPlayed.Text = new DateFormatConverter().Convert(songData.DateLastPlayed, null, "ddd, dd MMM, yyyy", null).ToString() + " at " + new DateFormatConverter().Convert(songData.DateLastPlayed, null, "T", null).ToString();
			else
				SongLastPlayed.Text = "Never";

			ClearButton.IsEnabled = SongPlayCount.Text != "0" && SongLastPlayed.Text != "Never";

			MainWindow._instance.WindowResizePermission(false);
			var result = await SongInfo.ShowAsync();
			MainWindow._instance.WindowResizePermission(true);

			if (result == ContentDialogResult.Primary)
			{

				MainWindow._instance.WindowResizePermission(false);
				TitleTextBox.Text = songData.Title;
				ArtistTextBox.Text = songData.Artists;
				AlbumTextBox.Text = songData.Album;
				GenreAutoSuggestBox.Text = songData.Genre;
				YearNumberBox.Text = songData.Year;
				LyricsTextBox.Text = songData.Lyrics;
				await EditSongInfo.ShowAsync();
				MainWindow._instance.WindowResizePermission(true);
			}
		}
	}

	private async void ClearButton_Click(object sender, RoutedEventArgs e)
	{
		await DatabaseHelper.Instance.ResetPlayCount(SongPath.Text);
		await DatabaseHelper.Instance.ResetDateLastPlayed(SongPath.Text);
		SongPlayCount.Text = "0";
		SongLastPlayed.Text = "Never";
		ClearButton.IsEnabled = false;
	}

	private async void CheckForUpdate()
	{
		var context = StoreContext.GetDefault();

		WinRT.Interop.InitializeWithWindow.Initialize(context,
			WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow));

		var updates = await context.GetAppAndOptionalStorePackageUpdatesAsync();

		if (updates.Count != 0)
			await MessageBox.ShowInfoAsync(isModal: true, owner: App.MainWindow,
				"App update is available.\n\nOpen Settings. Click 'Check for New Version' under About section to install it. Or open Microsoft Store to install it.", "Update check",
				buttons: MessageBoxButtons.OK);
	}

	public void SetVersionInfoVisibility(bool visible)
	{
		VersionInfo.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
	}

	private void InitializeVolumeSliderAndService()
	{
		if (bool.Parse(Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.UseSystemVolumeStatus)]?.ToString() ?? "true"))
			SwitchToSystemVolumeSliderControl();
		else
			SwitchToAppVolumeSliderControl();

		VolumeSlider.ValueChanged += VolumeSlider_ValueChanged;
		AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(App.Hwnd)).Changed += (s, e) => UpdateDragRects();
		VolumeSlider.Loaded += (s, e) => UpdateDragRects();
	}

	public void SwitchToSystemVolumeSliderControl()
	{
		var audioService = App.Current.AudioService;

		audioService.SystemVolumeChanged -= OnVolumeChanged;
		audioService.AppVolumeChanged -= OnVolumeChanged;

		audioService.SystemVolumeChanged += OnVolumeChanged;

		var volume = audioService.GetVolume();
		var isMuted = audioService.IsMuted();

		VolumeSlider.Value = volume;
		VolumeButtonGlyph.Glyph = isMuted ? "\uE74F" : volume <= 0 ? "\uE992" : volume < 33 ? "\uE993" : volume < 66 ? "\uE994" : "\uE995";
	}

	public void SwitchToAppVolumeSliderControl()
	{
		var audioService = App.Current.AudioService;

		audioService.SystemVolumeChanged -= OnVolumeChanged;
		audioService.AppVolumeChanged -= OnVolumeChanged;

		audioService.AppVolumeChanged += OnVolumeChanged;

		var volume = audioService.GetAppVolume();
		var isMuted = audioService.IsAppMuted();

		VolumeSlider.Value = volume;
		VolumeButtonGlyph.Glyph = isMuted ? "\uE74F" : volume <= 0 ? "\uE992" : volume < 33 ? "\uE993" : volume < 66 ? "\uE994" : "\uE995";
	}

	private void UpdateDragRects()
	{
		if (VolumeSlider.XamlRoot == null) return;

		var nonClientSource = InputNonClientPointerSource.GetForWindowId(Win32Interop.GetWindowIdFromWindow(App.Hwnd));
		nonClientSource.SetRegionRects(NonClientRegionKind.Passthrough, new RectInt32[]
		{
			GetRectForElement(VolumeSlider),
		});
	}

	private RectInt32 GetRectForElement(FrameworkElement element)
	{
		var transform = element.TransformToVisual(null);
		var bounds = transform.TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));

		var scale = XamlRoot.RasterizationScale;

		return new RectInt32(
			(int)(bounds.X * scale),
			(int)(bounds.Y * scale),
			(int)(bounds.Width * scale),
			(int)(bounds.Height * scale)
		);
	}

	private void VolumeMuteButton_Click(object sender, RoutedEventArgs e)
	{
		var audioService = App.Current.AudioService;

		if (bool.Parse(Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.UseSystemVolumeStatus)]?.ToString() ?? "true"))
			audioService.SetMute(!audioService.IsMuted());
		else
			audioService.SetAppMute(!audioService.IsAppMuted());
	}

	private void VolumeSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
	{
		if (_isUpdatingSlider) return;

		var slider = sender as Slider;
		if (slider == null) return;

		if (bool.Parse(Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.UseSystemVolumeStatus)]?.ToString() ?? "true"))
			App.Current.AudioService.SetVolume(slider.Value);
		else
			App.Current.AudioService.SetAppVolume(slider.Value);
	}

	private void OnVolumeChanged(double volume, bool isMuted)
	{
		DispatcherQueue.TryEnqueue(() =>
		{
			_isUpdatingSlider = true;
			VolumeSlider.Value = volume;
			_isUpdatingSlider = false;

			VolumeButtonGlyph.Glyph = isMuted ? "\uE74F" : volume <= 0 ? "\uE992" : volume < 33 ? "\uE993" : volume < 66 ? "\uE994" : "\uE995";

			if (bool.Parse(Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.PauseOnMuteStatus)]?.ToString() ?? "true") && (isMuted || volume == 0))
				MusicPlayer.Instance.Pause();
		});
	}

	private void ArtistTextBox_GotFocus(object sender, RoutedEventArgs e)
	{
		ArtistTeachingTip.IsOpen = true;
	}

	private void ArtistTextBox_LostFocus(object sender, RoutedEventArgs e)
	{
		ArtistTeachingTip.IsOpen = false;
	}
}
