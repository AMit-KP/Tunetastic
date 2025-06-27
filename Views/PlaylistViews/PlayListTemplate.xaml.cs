using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

namespace Tunetastic.Views.PlaylistViews;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class PlayListTemplate : Page
{
	/// <summary>
	/// Gets or sets the collection of songs that are added to the respective playlist.
	/// </summary>
	public ObservableCollection<Song> PlayListSongs
	{
		get; set;
	} = new();

	private Song? selectedSong;
	private readonly DispatcherQueue _dispatcherQueue;

	public PlayListTemplate()
	{
		this.InitializeComponent();
		_dispatcherQueue = DispatcherQueue.GetForCurrentThread();
		_ = CheckScanning();
	}

	/// <summary>
	/// Invoked when the page is navigated to, allowing for parameters to be passed and handled during navigation.
	/// </summary>
	/// <param name="e">An object that provides data about the navigation event, including the navigation parameter.</param>
	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		if (e.Parameter is DataGroup dataGroup)
		{
			PlaylistHeader.Text = dataGroup.Title;
		}
	}

	/// <summary>
	/// Handles the click event for deleting a playlist, removing it from the navigation service,
	/// internal data storage, and the UI navigation menu.
	/// </summary>
	/// <param name="sender">The source of the event, typically the button that was clicked.</param>
	/// <param name="e">An object containing event data.</param>
	private async void DeletePlayList_Click(object sender, RoutedEventArgs e)
	{
		DeleteDialog.Visibility = Visibility.Visible;
		DeleteDialogText.Inlines.Clear();
		DeleteDialogText.Inlines.Add(new Run { Text = "Are you sure you want to delete " });
		DeleteDialogText.Inlines.Add(new Run { Text = PlaylistHeader.Text, FontWeight = Microsoft.UI.Text.FontWeights.Bold });
		DeleteDialogText.Inlines.Add(new Run { Text = " PlayList?" });

		var result = await DeleteDialog.ShowAsync();

		if (result == ContentDialogResult.Primary)
		{
			App.Current.NavService.GoBack();
			var playListTag = "Tunetastic.Views.PlaylistViews." + Regex.Replace(PlaylistHeader.Text, @"\s+", "_") + "CustomPlaylist";

			await DatabaseHelper.Instance.RemovePlaylist(PlaylistHeader.Text);

			var a = NavigationPageMappings.PageDictionary.Remove(playListTag);

			var playlistsGroup = App.Current.NavService.MenuItems[2] as NavigationViewItem;
			foreach (NavigationViewItem item in playlistsGroup.MenuItems)
			{
				if (item.Tag.ToString() == playListTag)
				{
					playlistsGroup.MenuItems.Remove(item);
					break;
				}
			}
		}
	}

	/// <summary>
	/// Asynchronously checks the status of the ongoing music data scanning process and interacts with the user interface based on the status.
	/// </summary>
	/// <remarks>
	/// This method verifies whether a music scanning operation is in progress through the data service. If scanning is active,
	/// it modifies the application's UI by displaying loading indicators and hiding specific content elements until the process completes.
	/// Once scanning finishes or no scanning is detected, it populates the song collection with metadata, applies sorting and view style updates,
	/// and adjusts the visibility of various UI components.
	/// </remarks>
	/// <returns>
	/// A task representing the asynchronous operation of scanning status monitoring, UI adjustments, and song collection management.
	/// </returns>
	private async Task CheckScanning()
	{
		GoToSettings.Visibility = Visibility.Visible;
		PlayListSongsListViewGrid.Visibility = Visibility.Collapsed;
		PlayListSongsCompactViewGrid.Visibility = Visibility.Collapsed;
		PageButtons.Visibility = Visibility.Collapsed;

		if (GetMusicData.IsScanning)
		{
			GoToSettings.Visibility = Visibility.Collapsed;
			LoadingProgress.Opacity = 0;
			LoadingProgress.Visibility = Visibility.Visible;

			for (double i = 0; i <= 1; i += 0.05)
			{
				LoadingProgress.Opacity = i;
				await Task.Delay(1);
			}

			while (GetMusicData.IsScanning)
			{
				ProgressFill.Width = GetMusicData.ScanProgress * 4;
				ProgressFillText.Text = $"{GetMusicData.ScanProgress.ToString()}%";
				await Task.Delay(1);
			}

			for (double i = 1; i >= 0; i -= 0.05)
			{
				LoadingProgress.Opacity = i;
				await Task.Delay(1);
			}
			LoadingProgress.Visibility = Visibility.Collapsed;
			await _dispatcherQueue.EnqueueAsync(() =>
			{
				this.Content = new PlayListTemplate();
			});
			return;
		}

		if (await DatabaseHelper.Instance.GetSongsCount() > 0)
		{
			GoToSettings.Visibility = Visibility.Collapsed;
			ViewButton.Visibility = Visibility.Visible;
			PageButtons.Visibility = Visibility.Visible;
			UpdateAsPerLastViewStyle();
			LoadPlayListSongs();
		}
	}

	private async void LoadPlayListSongs()
	{
		var list = await DatabaseHelper.Instance.GetSongsInPlaylist(PlaylistHeader.Text);
		PlayListSongs.Clear();
		PlayListSongs.AddRange(list);
		list = null;
		HandleEmptyPlayList();
	}

	/// <summary>
	/// Manages the visibility and accessibility of UI elements related to the playlist view,
	/// based on whether the playlist contains any songs.
	/// </summary>
	private void HandleEmptyPlayList()
	{
		if (PlayListSongs.Count > 0)
		{
			NoResultsGrid.Visibility = Visibility.Collapsed;
			MultiSelectButton.Visibility = Visibility.Visible;
			PlayAllButtonStackPanel.Visibility = Visibility.Visible;
			ViewButton.Visibility = Visibility.Visible;
			SortPlayList.IsEnabled = true;
		}
		else
		{
			NoResultsGrid.Visibility = Visibility.Visible;
			MultiSelectButton.Visibility = Visibility.Collapsed;
			PlayAllButtonStackPanel.Visibility = Visibility.Collapsed;
			ViewButton.Visibility = Visibility.Collapsed;
			SortPlayList.IsEnabled = false;
		}
	}

	/// <summary>
	/// Updates the current view style of the song collection display using the last saved preference.
	/// </summary>
	/// <remarks>
	/// This method retrieves the previously saved view style setting from the application's local settings and
	/// applies it to the song collection display. Supported view styles include "List View", "Compact View", and "Card View".
	/// If no preference is found, the default view style is set to "Compact View".
	/// </remarks>
	private void UpdateAsPerLastViewStyle()
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var viewStyle = localSettings.Values[Regex.Replace(PlaylistHeader.Text, @"\s+", "_") + "_Playlist_ViewStyle"]?.ToString() ?? "Compact View";
		switch (viewStyle)
		{
			case "List View":
				ListViewStyle.IsChecked = true;
				break;

			case "Compact View":
			default:
				CompactViewStyle.IsChecked = true;
				break;
		}
		_ = UpdateListBasedOnViewStyle();
	}

	/// <summary>
	/// Updates the UI elements and layout to match the current view style selected for displaying songs.
	/// </summary>
	/// <remarks>
	/// This method dynamically adjusts the visibility of UI components depending on the selected view style,
	/// such as "List View" or "Compact View". It also persists the user's selection in local settings for future sessions.
	/// Additionally, it attempts to scroll to a previously selected song after applying the view style changes.
	/// </remarks>
	/// <returns>
	/// A Task representing the asynchronous operation of updating the view style and scrolling to a specific song.
	/// </returns>
	private async Task UpdateListBasedOnViewStyle()
	{
		var viewStyle = ViewStyle.Items.OfType<RadioMenuFlyoutItem>().Where(item => item.GroupName == "View" && item.IsChecked).Select(item => item.Text).FirstOrDefault() ?? "Compact View";
		string? glyph = null;
		switch (viewStyle)
		{
			case "List View":
				PlayListSongsListViewGrid.Visibility = Visibility.Visible;
				PlayListSongsCompactViewGrid.Visibility = Visibility.Collapsed;
				glyph = "\uE8FD";
				break;

			case "Compact View":
			default:
				PlayListSongsListViewGrid.Visibility = Visibility.Collapsed;
				PlayListSongsCompactViewGrid.Visibility = Visibility.Visible;
				glyph = "\uE71D";
				break;
		}
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[Regex.Replace(PlaylistHeader.Text, @"\s+", "_") + "_Playlist_ViewStyle"] = viewStyle;
		ViewButton.Content = new FontIcon() { Glyph = glyph };
		ToolTipService.SetToolTip(ViewButton, viewStyle);
		await ScrollToSong(selectedSong);       //somehow this doesn't work
		await Task.Delay(500);
		await ScrollToSong(selectedSong);
	}

	/// <summary>
	/// Determines the currently selected view style for the song collection display.
	/// </summary>
	/// <remarks>
	/// This method identifies the active view style based on the selection in the view style menu
	/// and returns the corresponding ListView instance. The supported view styles include
	/// "List View" and "Compact View", with "Compact View" being the default.
	/// </remarks>
	/// <returns>
	/// The <see cref="ListView"/> instance corresponding to the currently selected view style.
	/// </returns>
	private ListView GetCurrentViewStyle()
	{
		var viewStyle = ViewStyle.Items.OfType<RadioMenuFlyoutItem>().Where(item => item.GroupName == "View" && item.IsChecked).Select(item => item.Text).FirstOrDefault() ?? "Compact View";
		return viewStyle switch
		{
			"List View" => PlayListSongsListView,
			"Compact View" => PlayListSongsCompactView,
			_ => PlayListSongsCompactView
		};
	}

	/// <summary>
	/// Handles the ItemClick event for the ListView control in the PlayListPage.
	/// </summary>
	/// <param name="sender">The source of the event, typically the ListView control.</param>
	/// <param name="e">Provides data for the ItemClick event, including the clicked item.</param>
	/// <remarks>
	/// This method is triggered when a user clicks an item in the song list. It retrieves the clicked song,
	/// generates a playlist from the current collection of songs, and loads the clicked song into the music player for playback.
	/// The playlist is also saved as the current playlist in the application's local settings.
	/// </remarks>
	private void ListView_ItemClick(object sender, ItemClickEventArgs e)
	{
		var track = e.ClickedItem as Song;
		List<string> songPaths = PlayListSongs.Select(s => s.Path).ToList();
		MusicPlayer.Instance.LoadPlaylist(songPaths, track?.Path);
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CurrentPlaylist)] = Regex.Replace(PlaylistHeader.Text, @"\s+", "_") + "CustomPlaylist";
	}

	/// <summary>
	/// Scrolls to a specific song in the View.
	/// </summary>
	/// <param name="song">The song object to scroll to. If null, no action is performed.</param>
	/// <returns>A task representing the asynchronous operation of scrolling to the specified song.</returns>
	private async Task ScrollToSong(Song? song)
	{
		var listView = GetCurrentViewStyle();
		if (song != null)
		{
			await listView.SmoothScrollIntoViewWithItemAsync(song, itemPlacement: ScrollItemPlacement.Center, disableAnimation: false, scrollIfVisible: false);
			listView.SelectedItem = song;
		}
	}

	/// <summary>
	/// Handles the Loaded event for the PlayList Page.
	/// </summary>
	/// <param name="sender">The source of the event, typically the page itself.</param>
	/// <param name="e">The event data associated with the Loaded event.</param>
	/// <remarks>
	/// This method is responsible for managing the initialization operations required when the page is loaded. It checks whether the current playlist corresponds to the "AllSongsViewPage" and retrieves the last played song from the application's local settings, if available. It then attempts to scroll to the position of the last played song in the song collection asynchronously with a minor delay.
	/// </remarks>
	private async void Page_Loaded(object sender, RoutedEventArgs e)
	{
		while (PlayListSongs == null || PlayListSongs.Count == 0)
		{
			await Task.Delay(100);
		}
		ScrollToCurrentPlayingTrack();
	}

	/// <summary>
	/// Scrolls the view to the currently playing track if the current playlist corresponds to the "PlayList Page".
	/// </summary>
	/// <remarks>
	/// This method checks the local application settings to determine if the "PlayList Page" is the active playlist.
	/// If it is, the method retrieves the last played track based on its path from the saved settings and attempts to scroll
	/// the page to that specific song within the song collection.
	/// </remarks>
	private void ScrollToCurrentPlayingTrack()
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		if (localSettings.Values[nameof(LocalSave.CurrentPlaylist)]?.ToString() == Regex.Replace(PlaylistHeader.Text, @"\s+", "_") + "CustomPlaylist")
		{
			var SelectedSong = PlayListSongs.Select(s => s).Where(s => s.Path == localSettings.Values[nameof(LocalSave.LastPlayedTrack)]?.ToString()).FirstOrDefault();
			_ = ScrollToSong(SelectedSong);
		}
	}

	/// <summary>
	/// Handles the click event triggered by a view style button in the UI, updating the display style
	/// of the song collection based on the selected view style.
	/// </summary>
	/// <param name="sender">The source of the event, typically the view style button that was clicked.</param>
	/// <param name="e">The event data associated with the click event.</param>
	/// <remarks>
	/// This method is responsible for determining the selected view style (e.g., List View, Compact View, Card View),
	/// updating the visibility of UI elements accordingly, and persisting the selected view style for future use.
	/// It also performs additional UI adjustments such as resizing the alphabet display.
	/// </remarks>
	private async void ViewButton_OnClick(object sender, RoutedEventArgs e)
	{
		await UpdateListBasedOnViewStyle();
	}

	/// <summary>
	/// Handles the click event of the "Shuffle and Play" button to shuffle the song list
	/// and begin playback from a randomly selected song.
	/// </summary>
	/// <param name="sender">The source of the click event, typically the "Shuffle and Play" button.</param>
	/// <param name="e">Provides data about the click event.</param>
	/// <remarks>
	/// This method disables the button to prevent repeated triggers, enables shuffle mode on the music player,
	/// and retrieves the list of song paths to shuffle and load as a playlist. It then randomly selects a starting song
	/// from the playlist and scrolls to that song in the user interface. After a brief delay, it ensures that the song
	/// is properly scrolled into view and re-enables the button.
	/// </remarks>
	private async void ShuffleAndPlayButton_OnClick(object sender, RoutedEventArgs e)
	{
		ShuffleAndPlay.IsEnabled = false;
		MusicPlayer.Instance.ToggleShuffle(ShuffleMode.On);
		List<string> songPaths = PlayListSongs.Select(s => s.Path).ToList();

		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		localSettings.Values[nameof(LocalSave.CurrentPlaylist)] = Regex.Replace(PlaylistHeader.Text, @"\s+", "_") + "CustomPlaylist";

		var startingSong = songPaths[new Random().Next(songPaths.Count)];
		MusicPlayer.Instance.LoadPlaylist(songPaths, startingSong);
		var SelectedSong = PlayListSongs.Select(s => s).Where(s => s.Path == startingSong).FirstOrDefault();
		await ScrollToSong(SelectedSong);       //somehow this doesn't work
		await Task.Delay(500);
		await ScrollToSong(SelectedSong);
		ShuffleAndPlay.IsEnabled = true;
	}

	/// <summary>
	/// Handles the click event for the "Play All" button and initiates playback of all songs in the current view.
	/// </summary>
	/// <param name="sender">The source of the event, typically the "Play All" button.</param>
	/// <param name="e">Provides data for the routed event that triggered the method.</param>
	/// <remarks>
	/// This method disables shuffle mode, creates a playlist from all songs in the current view,
	/// stores the name of the current playlist in application settings, and starts playing the songs in order.
	/// It also scrolls to the first song in the playlist after initiating playback.
	/// </remarks>
	private async void PlayAllButton_OnClick(object sender, RoutedEventArgs e)
	{
		ShuffleAndPlay.IsEnabled = false;
		MusicPlayer.Instance.ToggleShuffle(ShuffleMode.Off);
		List<string> songPaths = PlayListSongs.Select(s => s.Path).ToList();
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		localSettings.Values[nameof(LocalSave.CurrentPlaylist)] = Regex.Replace(PlaylistHeader.Text, @"\s+", "_") + "CustomPlaylist";
		MusicPlayer.Instance.LoadPlaylist(songPaths);
		await ScrollToSong(PlayListSongs[0]);
		ShuffleAndPlay.IsEnabled = true;
	}

	/// <summary>
	/// Handles the event when the "Go to Settings" button is clicked.
	/// </summary>
	/// <param name="sender">The source of the event, typically the button being clicked.</param>
	/// <param name="e">The event data associated with the button click.</param>
	/// <remarks>
	/// This method navigates the application to the SettingsPage. It utilizes the application's navigation service (IJsonNavigationService)
	/// to redirect the user to the appropriate page.
	/// </remarks>
	private void GotoSettigsButton_Click(object sender, RoutedEventArgs e)
	{
		App.Current.NavService.NavigateTo(typeof(SettingsPage));
	}

	/// <summary>
	/// Handles the SelectionChanged event for the ListView to modify UI elements or update internal state based on user selection changes.
	/// </summary>
	/// <param name="sender">The source of the event, typically the ListView control where the selection was changed.</param>
	/// <param name="e">Provides data about the SelectionChanged event, including the modified selection.</param>
	/// <remarks>
	/// This method dynamically updates the `MoreButton`'s state depending on the selection mode and selected items count.
	/// If the ListView is in multi-select mode, the button is enabled or disabled based on whether any items are selected.
	/// In single-select mode, updates the `selectedSong` property with the currently selected song.
	/// </remarks>
	private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		var listView = GetCurrentViewStyle();
		if (listView.IsMultiSelectCheckBoxEnabled)
		{
			MoreButton.IsEnabled = listView.SelectedItems.Count > 0;
		}
		else
			selectedSong = listView.SelectedItem as Song;
	}

	/// <summary>
	/// Handles the Unloaded event for the PlayList Page.
	/// </summary>
	/// <remarks>
	/// This method is triggered when the page is unloaded. It performs cleanup operations such as clearing
	/// the song collection, releasing memory resources, and initiating garbage collection.
	/// </remarks>
	/// <param name="sender">The source of the event, typically the page being unloaded.</param>
	/// <param name="e">The event arguments associated with the Unloaded event.</param>
	private void Page_Unloaded(object sender, RoutedEventArgs e)
	{
		PlayListSongs.Clear();
		PlayListSongs = null;
		GC.Collect();
	}

	/// <summary>
	/// Handles the 'Opened' event of the <see cref="MenuFlyout"/> control in the context menu.
	/// </summary>
	/// <remarks>
	/// This method dynamically populates the "Add to Playlist" submenu with the available playlists retrieved from the database.
	/// If no playlists exist, a single "No Playlists created" item is added to the submenu with a red text color.
	/// The method ensures that all items in the submenu are cleared before adding new items.
	/// </remarks>
	/// <param name="sender">The source object where the event is triggered.</param>
	/// <param name="e">An object containing event data related to the 'Opened' event.</param>
	private async void MenuFlyout_Opened(object sender, object e)
	{
		var menu = sender as MenuFlyout;
		var addToPlaylist = menu?.Items.OfType<MenuFlyoutSubItem>().FirstOrDefault();

		addToPlaylist?.Items.Clear();

		List<string> playLists = await DatabaseHelper.Instance.GetAllPlaylistNames();

		if (playLists == null || playLists.Count == 0)
		{
			var menuItem = new MenuFlyoutItem
			{
				Text = "No Playlists created",
				Foreground = new SolidColorBrush(Colors.Red)
			};
			addToPlaylist?.Items.Add(menuItem);
			return;
		}

		foreach (var playList in playLists)
		{
			var menuItem = new MenuFlyoutItem
			{
				Text = playList
			};
			menuItem.Click += AddToPlaylist_Click;
			addToPlaylist?.Items.Add(menuItem);
		}
	}

	/// <summary>
	/// Handles the logic for adding songs to a selected playlist from the current view.
	/// </summary>
	/// <param name="sender">The source of the event, typically the menu item representing a playlist.</param>
	/// <param name="e">Event data associated with the click action.</param>
	/// <remarks>
	/// This method is triggered when a user selects the "Add to Playlist" option for one or more songs.
	/// It determines the selected songs from the active view style and adds them to the chosen playlist.
	/// Uses asynchronous operations to interact with the database for playlist updates.
	/// </remarks>
	private async void AddToPlaylist_Click(object sender, RoutedEventArgs e)
	{
		var listView = GetCurrentViewStyle();
		if (listView.IsMultiSelectCheckBoxEnabled)
		{
			var songs = listView.SelectedItems;
			List<string> songPaths = songs.Select(s => ((Song)s).Path).ToList();

			var playlist = (sender as MenuFlyoutItem)?.Text;

			if (playlist != null)
				await DatabaseHelper.Instance.AddSongsToPlaylist(playlist, songPaths);

			GlobalNotification.Info($"{songs.Count} Song/Track{(songs.Count > 1 ? "s" : "")} added successfully to {playlist} playlist.");
		}
		else
		{
			var song = (sender as MenuFlyoutItem)?.DataContext as Song;
			var playlist = (sender as MenuFlyoutItem)?.Text;

			if (playlist != null && song != null)
			{
				await DatabaseHelper.Instance.AddSongToPlaylist(playlist, song.Path);
				GlobalNotification.Info($"{song.Title} added successfully to {playlist} playlist.");
			}
		}
	}

	/// <summary>
	/// Handles the click event for the "Play" menu flyout item in the song list UI.
	/// </summary>
	/// <param name="sender">The source of the event, typically a menu flyout item associated with a song.</param>
	/// <param name="e">Provides event data for the click event.</param>
	/// <remarks>
	/// This method retrieves the selected song's data from the sender control and prepares a playlist with all available songs.
	/// The playlist is then passed to the application's music player for playback, starting with the selected song.
	/// Additionally, this method updates the application's local settings to store the current playlist context.
	/// </remarks>
	private void MenuFlyoutItemPlay_OnClick(object sender, RoutedEventArgs e)
	{
		var songData = (sender as MenuFlyoutItem)?.DataContext as Song;
		List<string> songPaths = PlayListSongs.Select(s => s.Path).ToList();
		MusicPlayer.Instance.LoadPlaylist(songPaths, songData?.Path);
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CurrentPlaylist)] = Regex.Replace(PlaylistHeader.Text, @"\s+", "_") + "CustomPlaylist";
	}

	/// <summary>
	/// Handles the click event for the "Add to queue" menu flyout item.
	/// </summary>
	/// <remarks>
	/// This method retrieves the associated song data from the menu item's data context
	/// and adds the song's file path to the queued playing list asynchronously.
	/// </remarks>
	/// <param name="sender">The source of the event, typically the MenuFlyoutItem that was clicked.</param>
	/// <param name="e">The event data associated with the routed event.</param>
	private async void MenuFlyoutItemAddToQueue_OnClick(object sender, RoutedEventArgs e)
	{
		var songData = (sender as MenuFlyoutItem)?.DataContext as Song;
		List<string> songPaths = new List<string> { songData?.Path ?? "" };
		await DatabaseHelper.Instance.AddSongsToQueuedPlayingList(songPaths);

		GlobalNotification.Info($"{songData?.Title} added successfully to queue.");
	}

	private void MenuFlyoutItemInfoTag_OnClick(object sender, RoutedEventArgs e)
	{
		//TODO add Card Display
	}

	/// <summary>
	/// Handles the delete action when a menu flyout item is clicked, initiating a confirmation dialog and deleting the selected song.
	/// </summary>
	/// <param name="sender">The source of the event, typically a MenuFlyoutItem representing the delete option.</param>
	/// <param name="e">The event data associated with the click event.</param>
	/// <remarks>
	/// This method displays a confirmation dialog to the user with details about the song to be deleted. If the user confirms the
	/// action, it deletes the file from the local file system, removes the song entry from the database, and updates the collection
	/// of displayed songs to reflect the changes.
	/// </remarks>
	private async void MenuFlyoutItemDelete_OnClick(object sender, RoutedEventArgs e)
	{
		DeleteDialog.Visibility = Visibility.Visible;
		var songData = (sender as MenuFlyoutItem)?.DataContext as Song;

		if (songData != null)
		{
			DeleteDialogText.Text = $"Are you sure you want to delete this song/track from your system?" +
									$"\nTitle: {songData.Title}" +
									$"\nArtist: {songData.Artists}" +
									$"\nAlbum: {songData.Album}" +
									$"\nFile: {songData.Path}";

			var result = await DeleteDialog.ShowAsync();
			if (result == ContentDialogResult.Primary)
			{
				if (File.Exists(songData.Path))
				{
					File.Delete(songData.Path);
					await DatabaseHelper.Instance.DeleteSongFromDB(songData.Path);
					PlayListSongs.Remove(songData);
					MusicPlayer.Instance.HandleAfterDelete();
					GlobalNotification.Info("Song/Track deleted successfully." +
											$"\nTitle: {songData.Title}" +
											$"\nArtist: {songData.Artists}" +
											$"\nAlbum: {songData.Album}" +
											$"\nFile: {songData.Path}");
				}
			}
			if (await DatabaseHelper.Instance.GetSongsCount() <= 0)
			{
				GoToSettings.Visibility = Visibility.Visible;
				PageButtons.Visibility = Visibility.Collapsed;
			}
		}
	}

	/// <summary>
	/// Handles the click event for the MultiSelectButton to toggle between multi-select mode
	/// and single-select mode in the all songs page.
	/// </summary>
	/// <param name="sender">The source of the event, typically the MultiSelectButton.</param>
	/// <param name="e">The event data for the RoutedEventArgs associated with the click action.</param>
	/// <remarks>
	/// When the button is toggled, it switches the functionality of the song list view between multi-select
	/// and single-select modes. In multi-select mode, checkboxes are enabled for selecting multiple items,
	/// and certain UI elements such as context menus are disabled to prevent conflicts. In single-select
	/// mode, standard selection behavior is restored, and UI elements such as context menus are re-enabled.
	/// This method also adjusts the visibility and interactivity of related UI components (e.g., MoreButton,
	/// PlayAllButtonStackPanel) based on the current mode.
	/// </remarks>
	private void MultiSelectButton_Click(object sender, RoutedEventArgs e)
	{
		var view = GetCurrentViewStyle();
		if (MultiSelectButton.IsChecked == true)
		{
			MoreButton.Visibility = Visibility.Visible;
			MoreButton.IsEnabled = false;
			PlayAllButtonStackPanel.Visibility = Visibility.Collapsed;
			ViewButton.Visibility = Visibility.Collapsed;
			SettingsButton.Visibility = Visibility.Collapsed;
			ToolTipService.SetToolTip(MultiSelectButton, "Turn off multi-select mode");
			view.SelectionMode = ListViewSelectionMode.Multiple;
			view.IsItemClickEnabled = false;
			view.IsMultiSelectCheckBoxEnabled = true;
			view.IsRightTapEnabled = false;
			var ItemGrids = DevWinUI.DependencyObjectEx.FindDescendants(view);

			foreach (var item in ItemGrids)
			{
				if (item is UIElement uiElement)
				{
					uiElement.IsRightTapEnabled = false;
				}
			}
			if (view.Name == "PlayListSongsListView") Header.Margin = new Thickness(40, 0, 0, 0);
		}
		else
		{
			MoreButton.Visibility = Visibility.Collapsed;
			PlayAllButtonStackPanel.Visibility = Visibility.Visible;
			ViewButton.Visibility = Visibility.Visible;
			SettingsButton.Visibility = Visibility.Visible;
			ToolTipService.SetToolTip(MultiSelectButton, "Turn on multi-select mode");
			view.SelectionMode = ListViewSelectionMode.Single;
			view.IsItemClickEnabled = true;
			view.IsMultiSelectCheckBoxEnabled = false;
			view.IsRightTapEnabled = true;
			var ItemGrids = DevWinUI.DependencyObjectEx.FindDescendants(view);

			foreach (var item in ItemGrids)
			{
				if (item is UIElement uiElement)
				{
					uiElement.IsRightTapEnabled = true;
				}
			}
			if (view.Name == "PlayListSongsListView") Header.Margin = new Thickness(12, 0, 0, 0);
		}
	}

	/// <summary>
	/// Handles the click event for adding multiple selected songs from the menu flyout to the playing queue.
	/// </summary>
	/// <param name="sender">The source of the event, typically the menu flyout item.</param>
	/// <param name="e">The event data associated with the routed event.</param>
	/// <remarks>
	/// This method retrieves the selected items from the current song view style, extracts their file paths,
	/// and asynchronously adds them to the queued playing list using the <see cref="DatabaseHelper"/> instance.
	/// </remarks>
	private async void MenuFlyoutMultiItemAddToQueue_OnClick(object sender, RoutedEventArgs e)
	{
		var songs = GetCurrentViewStyle().SelectedItems;
		List<string> songPaths = songs.Select(s => ((Song)s).Path).ToList();

		await DatabaseHelper.Instance.AddSongsToQueuedPlayingList(songPaths);

		GlobalNotification.Info($"{songPaths.Count} Song/Track{(songPaths.Count > 1 ? "s" : "")} added to the queue.");
	}

	/// <summary>
	/// Handles the click event for the "Delete" menu flyout item, allowing the user to delete selected songs from the system.
	/// </summary>
	/// <remarks>
	/// This method retrieves the currently selected songs, prompts the user for confirmation through a dialog,
	/// and deletes the selected songs from both the file system and the application's database if confirmed.
	/// </remarks>
	/// <param name="sender">The source of the event, typically the menu flyout item.</param>
	/// <param name="e">Provides data for the routed event, including information about the source and state of the event.</param>
	private async void MenuFlyoutMultiItemDelete_OnClick(object sender, RoutedEventArgs e)
	{
		var songs = GetCurrentViewStyle().SelectedItems;

		List<Song> songList = new();
		foreach (var item in songs)
			songList.Add((Song)item);


		DeleteDialog.Visibility = Visibility.Visible;
		DeleteDialogText.Text = $"Are you sure you want to delete {(songList.Count > 1 ? "these" : "this")} {songList.Count} song{(songList.Count > 1 ? "s" : "")}/track{(songs.Count > 1 ? "s" : "")} from your system?";

		var result = await DeleteDialog.ShowAsync();

		if (result == ContentDialogResult.Primary)
		{
			foreach (Song songData in songList)
			{
				if (File.Exists(songData.Path))
				{
					File.Delete(songData.Path);
					await DatabaseHelper.Instance.DeleteSongFromDB(songData.Path);
					PlayListSongs.Remove(songData);
				}
			}
			MusicPlayer.Instance.HandleAfterDelete();
			GlobalNotification.Info($"{songList.Count} Song{(songList.Count > 1 ? "s" : "")}/Track{(songList.Count > 1 ? "s" : "")} deleted successfully.");
		}
		if (await DatabaseHelper.Instance.GetSongsCount() <= 0)
		{
			GoToSettings.Visibility = Visibility.Visible;
			PageButtons.Visibility = Visibility.Collapsed;
		}
	}

	/// <summary>
	/// Handles the click event of the "Remove" menu flyout item, removing the selected song from the playlist and updating the data store.
	/// </summary>
	/// <param name="sender">The source of the event, typically the MenuFlyoutItem that was clicked.</param>
	/// <param name="e">The event data containing information about the routed event.</param>
	private async void MenuFlyoutItemRemove_OnClick(object sender, RoutedEventArgs e)
	{
		var songData = (sender as MenuFlyoutItem)?.DataContext as Song;
		if (songData != null)
		{
			var index = songData.Path == MusicPlayer.Instance.CurrentSong ? PlayListSongs.IndexOf(songData) : -1;
			PlayListSongs.Remove(songData);

			List<string> songPaths = new List<string> { songData?.Path ?? "" };
			await DatabaseHelper.Instance.RemoveSongsFromPlaylist(PlaylistHeader.Text, songPaths);


			GlobalNotification.Info($"Song/Track removed successfully from {PlaylistHeader.Text}." +
									$"\nTitle: {songData.Title}" +
									$"\nArtist: {songData.Artists}" +
									$"\nAlbum: {songData.Album}" +
									$"\nFile: {songData.Path}");

			HandleAfterRemove(index);
		}
	}

	/// <summary>
	/// Handles the click event for removing multiple selected items from the playlist menu,
	/// updates the playlist by removing the selected songs, displays a notification,
	/// and performs post-removal actions.
	/// </summary>
	/// <param name="sender">The source of the event, typically a menu flyout item.</param>
	/// <param name="e">Event data that provides details about the routed event.</param>
	private async void MenuFlyoutMultiItemRemove_OnClick(object sender, RoutedEventArgs e)
	{
		var songs = GetCurrentViewStyle().SelectedItems;
		List<Song> songList = new();
		foreach (var item in songs)
			songList.Add((Song)item);

		List<string> songPaths = songs.Select(s => ((Song)s).Path).ToList();

		int index = songPaths.Contains(MusicPlayer.Instance.CurrentSong) ? PlayListSongs.Select((s, idx) => new { Song = s, Index = idx })
																						.Where(x => x.Song.Path == MusicPlayer.Instance.CurrentSong)
																						.Select(x => x.Index)
																						.FirstOrDefault()
																		: -1;

		await DatabaseHelper.Instance.RemoveSongsFromPlaylist(PlaylistHeader.Text, songPaths);

		GlobalNotification.Info($"{songList.Count} Song/Track{(songList.Count > 1 ? "s" : "")} removed successfully from {PlaylistHeader.Text}.");

		foreach (Song song in songList)
		{
			PlayListSongs.Remove(song);
			await Task.Delay(10);
		}

		HandleAfterRemove(index);
	}

	/// <summary>
	/// Performs necessary operations after a song or multiple songs are removed from the playlist.
	/// Updates the playlist's current state, reloads the player as required, and handles empty playlist scenarios.
	/// </summary>
	/// <param name="index">The index of the removed song if applicable, or -1 if the removed song is not playing.</param>
	private void HandleAfterRemove(int index)
	{
		if (Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CurrentPlaylist)]?.ToString() == Regex.Replace(PlaylistHeader.Text, @"\s+", "_") + "CustomPlaylist")
		{
			if (PlayListSongs.Count > 0)
			{
				string track = "";
				if (index == -1)
				{
					track = MusicPlayer.Instance.CurrentSong;
				}
				else
				{
					if (index < PlayListSongs.Count)
						track = PlayListSongs[index].Path;
					else
						track = PlayListSongs[PlayListSongs.Count - 1].Path;
				}

				List<string> songPaths = PlayListSongs.Select(s => s.Path).ToList();
				MusicPlayer.Instance.LoadPlaylist(songPaths, track);
			}
			else
			{
				MusicPlayer.Instance.CurrentSong = "";
				MusicPlayer.Instance.ResetOrReloadPlayer();
			}
		}

		HandleEmptyPlayList();
	}

	/// <summary>
	/// Handles the sorting of playlist songs based on the selected sorting criteria from the menu.
	/// </summary>
	/// <param name="sender">The source of the event, typically a MenuFlyoutItem, containing the sorting criterion in its Tag property.</param>
	/// <param name="e">Event data that provides information about the Click event.</param>
	private async void SortPlayList_Click(object sender, RoutedEventArgs e)
	{
		var orderWay = (sender as MenuFlyoutItem)?.Tag?.ToString() switch
		{
			"TitleAsc" => (SongProperty.Title, true),
			"TitleDesc" => (SongProperty.Title, false),
			"ArtistsAsc" => (SongProperty.Artists, true),
			"ArtistsDesc" => (SongProperty.Artists, false),
			"AlbumAsc" => (SongProperty.Album, true),
			"AlbumDesc" => (SongProperty.Album, false),
			"YearAsc" => (SongProperty.Year, true),
			"YearDesc" => (SongProperty.Year, false),
			"DurationAsc" => (SongProperty.Duration, true),
			"DurationDesc" => (SongProperty.Duration, false),
			"PlayCountAsc" => (SongProperty.PlayCount, true),
			"PlayCountDesc" => (SongProperty.PlayCount, false),
			"ModifiedTimeAsc" => (SongProperty.DateAdded, true),
			"ModifiedTimeDesc" => (SongProperty.DateAdded, false),
			_ => (SongProperty.Title, true)
		};

		await DatabaseHelper.Instance.SortPlaylistSongs(PlaylistHeader.Text, orderWay.Item1, orderWay.Item2);
		LoadPlayListSongs();
	}

	/// <summary>
	/// Represents a collection of playlist names retrieved from the data source.
	/// </summary>
	private List<string>? playLists;

	/// <summary>
	/// Handles the click event triggered for renaming a playlist.
	/// Shows a dialog where the user can enter a new name for the playlist, performs the renaming
	/// operation in the database, and updates the playlist header and navigation structure.
	/// </summary>
	/// <param name="sender">The source of the event, typically the MenuFlyoutItem that is clicked.</param>
	/// <param name="e">The event-specific data for the click action.</param>
	private async void RenamePlayList_Click(object sender, RoutedEventArgs e)
	{
		RenamePlaylistDialog.Visibility = Visibility.Visible;
		RenamePlaylistDialog.RequestedTheme = App.Current.ThemeService.GetElementTheme();
		PlaylistNameBox.Text = string.Empty;

		playLists = await DatabaseHelper.Instance.GetAllPlaylistNames();

		ContentDialogResult result = await RenamePlaylistDialog.ShowAsync();

		if (result == ContentDialogResult.Primary)
		{
			var playlistsGroup = App.Current.NavService.MenuItems[2] as NavigationViewItem;
			var tag = "Tunetastic.Views.PlaylistViews." + Regex.Replace(PlaylistNameBox.Text.Trim(), @"\s+", "_") + "CustomPlaylist";
			if (playlistsGroup != null)
			{
				DataGroup dataGroup = new();
				dataGroup.UniqueId = tag;
				dataGroup.Title = PlaylistNameBox.Text.Trim();

				var playListNavigationItem = playlistsGroup.MenuItems.Select(x => x as NavigationViewItem).FirstOrDefault(x => x?.Tag.ToString() == "Tunetastic.Views.PlaylistViews." + Regex.Replace(PlaylistHeader.Text, @"\s+", "_") + "CustomPlaylist");
				if (playListNavigationItem != null)
				{
					var oldTag = playListNavigationItem.Tag.ToString();
					if (oldTag != null) NavigationPageMappings.PageDictionary.Remove(oldTag);

					playListNavigationItem.Content = new TextBlock
					{
						Text = dataGroup.Title,
						TextTrimming = TextTrimming.CharacterEllipsis
					};
					playListNavigationItem.Tag = tag;
					playListNavigationItem.DataContext = dataGroup;
					ToolTipService.SetToolTip(playListNavigationItem, dataGroup.Title);

					NavigationPageMappings.PageDictionary.Add(tag, typeof(PlayListTemplate));
				}
			}
			await DatabaseHelper.Instance.RenamePlaylist(PlaylistHeader.Text, PlaylistNameBox.Text.Trim());
			PlaylistHeader.Text = PlaylistNameBox.Text.Trim();
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
			RenamePlaylistDialog.IsPrimaryButtonEnabled = false;
		}
		else
		{
			ErrorMessage.Visibility = Visibility.Collapsed;
			RenamePlaylistDialog.IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(PlaylistNameBox.Text.Trim());
		}

	}
}
