using System.Collections.ObjectModel;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

namespace Tunetastic.Views.PlaylistViews;

/// <summary>
/// Represents the Most Played playlist page that displays songs with the highest play counts in the library.
/// </summary>
/// <remarks>
/// This class is responsible for initializing and managing the user interface for the Most Played playlist. It handles the presentation of popular songs based on play statistics and updates the UI accordingly during runtime or data operations.
/// </remarks>
public sealed partial class MostPlayed : Page
{
	/// <summary>
	/// Gets or sets the collection of songs that are most frequently played.
	/// The collection is automatically updated based on play count and
	/// a user-defined maximum display limit.
	/// </summary>
	public ObservableCollection<Song> MostPlayedSongs
	{
		get;
		set;
	} = new();

	private Song? selectedSong;
	private readonly DispatcherQueue _dispatcherQueue;

	/// <summary>
	/// Represents the Most Played playlist page that displays songs with the highest play counts in the library.
	/// </summary>
	/// <remarks>
	/// This class is responsible for initializing and managing the user interface for the Most Played playlist. It handles the presentation of popular songs based on play statistics and updates the UI accordingly during runtime or data operations.
	/// </remarks>
	public MostPlayed()
	{
		this.InitializeComponent();
		_dispatcherQueue = DispatcherQueue.GetForCurrentThread();
		_ = CheckScanning();
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
		AddGoToSettingsMessage();
		MostPlayedSongsListViewGrid.Visibility = Visibility.Collapsed;
		MostPlayedSongsCompactViewGrid.Visibility = Visibility.Collapsed;
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
				this.Content = new MostPlayed();
			});
			return;
		}

		if (await DatabaseHelper.Instance.GetSongsCount() > 0)
		{
			GoToSettings.Visibility = Visibility.Collapsed;
			ViewButton.Visibility = Visibility.Visible;
			MaxLimitDropDown.Visibility = Visibility.Visible;
			UpdateAsPerLastViewStyle();
			UpdateAsPerLastMaxLimit();
			PageButtons.Visibility = Visibility.Visible;
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
		var viewStyle = localSettings.Values[nameof(LocalSave.MostPlayedSongViewStyle)]?.ToString() ?? "Compact View";
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
				MostPlayedSongsListViewGrid.Visibility = Visibility.Visible;
				MostPlayedSongsCompactViewGrid.Visibility = Visibility.Collapsed;
				glyph = "\uE8FD";
				break;

			case "Compact View":
			default:
				MostPlayedSongsListViewGrid.Visibility = Visibility.Collapsed;
				MostPlayedSongsCompactViewGrid.Visibility = Visibility.Visible;
				glyph = "\uE71D";
				break;
		}
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.MostPlayedSongViewStyle)] = viewStyle;
		ViewButton.Content = new FontIcon() { Glyph = glyph };
		ToolTipService.SetToolTip(ViewButton, viewStyle);
		await ScrollToSong(selectedSong);       //somehow this doesn't work
		await Task.Delay(500);
		await ScrollToSong(selectedSong);
	}

	/// <summary>
	/// Updates the UI to reflect the user's last selected maximum song display limit for the Most Played playlist.
	/// </summary>
	/// <remarks>
	/// This method retrieves the user's previously saved preference for the maximum number of songs to display
	/// from local application settings and adjusts the corresponding UI element (e.g., RadioMenuFlyoutItem)
	/// to indicate the selected option. If no preference exists, it defaults to a limit of 100 songs.
	/// Additionally, it initiates the process to update the song list according to the selected limit.
	/// </remarks>
	private void UpdateAsPerLastMaxLimit()
	{
		var maxLimit = Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.MostPlayedMaxLimit)]?.ToString() ?? "100";
		switch (maxLimit)
		{
			case "50":
				Limit50.IsChecked = true;
				break;

			case "200":
				Limit200.IsChecked = true;
				break;

			case "500":
				Limit500.IsChecked = true;
				break;

			case "Unlimited":
				Unlimited.IsChecked = true;
				break;

			case "100":
			default:
				Limit100.IsChecked = true;
				break;
		}

		_ = UpdateListBasedOnMaxLimit();
	}

	/// <summary>
	/// Updates the "Most Played" songs list based on the maximum limit selected by the user.
	/// </summary>
	/// <remarks>
	/// This method handles fetching a specific number of songs corresponding to the selected maximum limit from the database
	/// and updates the "Most Played" UI list accordingly. It also ensures that the selected limit is saved in local settings
	/// and modifies related UI elements such as tooltips and labels to reflect the new limit.
	/// </remarks>
	/// <returns>
	/// A task representing the asynchronous operation of updating and refreshing the "Most Played" songs list.
	/// </returns>
	private async Task UpdateListBasedOnMaxLimit()
	{
		var song = selectedSong;
		var maxLimit = MaxLimit.Items.OfType<RadioMenuFlyoutItem>().Where(item => item.GroupName == "Limit" && item.IsChecked).Select(item => item.Text).FirstOrDefault() ?? "100";

		var newList = await DatabaseHelper.Instance.LoadSongsFromDB(SongProperty.PlayCount, ascending: false, limit: maxLimit == "Unlimited" ? 0 : int.Parse(maxLimit), whereCondition: $"{SongProperty.PlayCount.ToString()} > 0");
		MostPlayedSongs.Clear();
		MostPlayedSongs.AddRange(newList);
		newList = null;

		var maxLimitDropDownContent = new TextBlock();
		maxLimitDropDownContent.Inlines.Add(new Run { Text = "Max Limit: " });
		maxLimitDropDownContent.Inlines.Add(new Run { Text = maxLimit, FontWeight = Microsoft.UI.Text.FontWeights.Bold });

		var maxLimitDropDownTooltip = new TextBlock();
		maxLimitDropDownTooltip.Inlines.Add(new Run { Text = "The maximum number of songs/tracks to display in the list: " });
		maxLimitDropDownTooltip.Inlines.Add(new Run { Text = maxLimit, FontWeight = Microsoft.UI.Text.FontWeights.Bold });
		maxLimitDropDownTooltip.TextWrapping = TextWrapping.WrapWholeWords;

		MaxLimitDropDown.Content = maxLimitDropDownContent;
		ToolTipService.SetToolTip(MaxLimitDropDown, maxLimitDropDownTooltip);
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.MostPlayedMaxLimit)] = maxLimit;


		if (MostPlayedSongs.Count > 0)
		{
			PageButtons.Visibility = Visibility.Visible;
			NoResultsGrid.Visibility = Visibility.Collapsed;
			song = MostPlayedSongs.Select(s => s).Where(s => s.Path == song?.Path).FirstOrDefault();
			await ScrollToSong(song);       //somehow this doesn't work
			await Task.Delay(1000);
			await ScrollToSong(song);
		}
		else
		{
			NoResultsGrid.Visibility = Visibility.Visible;
			AddNoResultsMessage();
			PageButtons.Visibility = Visibility.Collapsed;
		}
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
			"List View" => MostPlayedSongsListView,
			"Compact View" => MostPlayedSongsCompactView,
			_ => MostPlayedSongsCompactView
		};
	}

	/// <summary>
	/// Handles the ItemClick event for the ListView control in the MostPlayed page.
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
		List<string> songPaths = MostPlayedSongs.Select(s => s.Path).ToList();
		MusicPlayer.Instance.LoadPlaylist(songPaths, track?.Path);
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CurrentPlayinglist)] = "MostPlayed";
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
			try
			{
				await listView.SmoothScrollIntoViewWithItemAsync(song, itemPlacement: ScrollItemPlacement.Center, disableAnimation: false, scrollIfVisible: false);
			}
			catch (Exception)
			{
			}
			listView.SelectedItem = song;
		}
	}

	/// <summary>
	/// Handles the Loaded event for the MostPlayed page.
	/// </summary>
	/// <param name="sender">The source of the event, typically the page itself.</param>
	/// <param name="e">The event data associated with the Loaded event.</param>
	/// <remarks>
	/// This method is responsible for managing the initialization operations required when the page is loaded. It checks whether the current playlist corresponds to the "MostPlayed" and retrieves the last played song from the application's local settings, if available. It then attempts to scroll to the position of the last played song in the song collection asynchronously with a minor delay.
	/// </remarks>
	private async void Page_Loaded(object sender, RoutedEventArgs e)
	{
		while (MostPlayedSongs == null || MostPlayedSongs.Count == 0)
		{
			await Task.Delay(100);
		}
		ScrollToCurrentPlayingTrack();
	}

	/// <summary>
	/// Scrolls the view to the currently playing track if the current playlist corresponds to the "MostPlayed".
	/// </summary>
	/// <remarks>
	/// This method checks the local application settings to determine if the "MostPlayed" is the active playlist.
	/// If it is, the method retrieves the last played track based on its path from the saved settings and attempts to scroll
	/// the page to that specific song within the song collection.
	/// </remarks>
	private void ScrollToCurrentPlayingTrack()
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		if (localSettings.Values[nameof(LocalSave.CurrentPlayinglist)]?.ToString() == "MostPlayed")
		{
			var SelectedSong = MostPlayedSongs.Select(s => s).Where(s => s.Path == localSettings.Values[nameof(LocalSave.LastPlayedTrack)]?.ToString()).FirstOrDefault();
			_ = ScrollToSong(SelectedSong);
		}
	}

	/// <summary>
	/// Handles the click event for the maximum limit button in the "Most Played" playlist view.
	/// </summary>
	/// <param name="sender">The trigger source of the event, typically the menu item representing the selected limit.</param>
	/// <param name="e">The event data related to the button click action.</param>
	/// <remarks>
	/// This method updates the song list in the "Most Played" playlist according to the selected maximum limit. If "Most Played" is the current playlist in the music player,
	/// the playlist is reloaded to reflect the changes and ensures the playback state consistency.
	/// </remarks>
	private async void MaxLimitButton_OnClick(object sender, RoutedEventArgs e)
	{
		await UpdateListBasedOnMaxLimit();

		if (Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CurrentPlayinglist)]?.ToString() == "MostPlayed")
		{
			List<string> songPaths = MostPlayedSongs.Select(s => s.Path).ToList();
			MusicPlayer.Instance.LoadPlaylist(songPaths, MusicPlayer.Instance.CurrentSong, MusicPlayer.Instance.MediaPlayer.IsPlaying, dontReloadCurrent: true);
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
		List<string> songPaths = MostPlayedSongs.Select(s => s.Path).ToList();

		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		localSettings.Values[nameof(LocalSave.CurrentPlayinglist)] = "MostPlayed";

		var startingSong = songPaths[new Random().Next(songPaths.Count)];
		MusicPlayer.Instance.LoadPlaylist(songPaths, startingSong);
		var SelectedSong = MostPlayedSongs.Select(s => s).Where(s => s.Path == startingSong).FirstOrDefault();
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
		List<string> songPaths = MostPlayedSongs.Select(s => s.Path).ToList();
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		localSettings.Values[nameof(LocalSave.CurrentPlayinglist)] = "MostPlayed";
		MusicPlayer.Instance.LoadPlaylist(songPaths);
		await ScrollToSong(MostPlayedSongs[0]);
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
	/// Handles the Unloaded event for the MostPlayedPage.
	/// </summary>
	/// <remarks>
	/// This method is triggered when the page is unloaded. It performs cleanup operations such as clearing
	/// the song collection, releasing memory resources, and initiating garbage collection.
	/// </remarks>
	/// <param name="sender">The source of the event, typically the page being unloaded.</param>
	/// <param name="e">The event arguments associated with the Unloaded event.</param>
	private void Page_Unloaded(object sender, RoutedEventArgs e)
	{
		MostPlayedSongs.Clear();
		MostPlayedSongs = null;
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
			ToolTipService.SetToolTip(menuItem, $"Add {(MultiSelectButton.IsChecked == true ? "selected songs/tracks" : "this song/track")} to {playList} playlist");
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

			GlobalNotification.Info($"{songs.Count} {(songs.Count > 1 ? "songs/tracks" : "song/track")} added to {playlist} playlist.");
		}
		else
		{
			var song = (sender as MenuFlyoutItem)?.DataContext as Song;
			var playlist = (sender as MenuFlyoutItem)?.Text;

			if (playlist != null && song != null)
			{
				await DatabaseHelper.Instance.AddSongToPlaylist(playlist, song.Path);
				GlobalNotification.Info($"{song.Title} added to {playlist} playlist.");
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
		List<string> songPaths = MostPlayedSongs.Select(s => s.Path).ToList();
		MusicPlayer.Instance.LoadPlaylist(songPaths, songData?.Path);
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CurrentPlayinglist)] = "MostPlayed";
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

		GlobalNotification.Info($"{songData?.Title} added to queue.");
	}

	/// <summary>
	/// Handles the click event for the "Info/Tag" menu flyout item, displaying detailed song information.
	/// </summary>
	/// <param name="sender">The source of the event, typically the MenuFlyoutItem.</param>
	/// <param name="e">The event data associated with the click action.</param>
	/// <remarks>
	/// This method retrieves the song associated with the selected menu item, queries the song data from
	/// the database, and invokes the main page to display the song's detailed information.
	/// </remarks>
	private async void MenuFlyoutItemInfoTag_OnClick(object sender, RoutedEventArgs e)
	{
		var songData = (sender as MenuFlyoutItem)?.DataContext as Song;
		if (songData is not null) MainPage._instance.ShowSongInfo(await DatabaseHelper.Instance.GetSongByPath(songData.Path));
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
					MostPlayedSongs.Remove(songData);
					MusicPlayer.Instance.HandleAfterDelete();
					GlobalNotification.Info("Song/Track deleted." +
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
			LimitAndViewButtonPanel.Visibility = Visibility.Collapsed;
			ToolTipService.SetToolTip(MultiSelectButton, "Turn off multi-select mode");
			view.SelectionMode = ListViewSelectionMode.Multiple;
			view.IsItemClickEnabled = false;
			view.IsMultiSelectCheckBoxEnabled = true;
			view.IsRightTapEnabled = false;
			var ItemGrids = DevWinUI.DependencyObjectExtensions.FindDescendants(view);

			foreach (var item in ItemGrids)
			{
				if (item is UIElement uiElement)
				{
					uiElement.IsRightTapEnabled = false;
				}
			}
			if (view.Name == "MostPlayedSongsListView") Header.Margin = new Thickness(40, 0, 0, 0);
		}
		else
		{
			MoreButton.Visibility = Visibility.Collapsed;
			PlayAllButtonStackPanel.Visibility = Visibility.Visible;
			LimitAndViewButtonPanel.Visibility = Visibility.Visible;
			ToolTipService.SetToolTip(MultiSelectButton, "Turn on multi-select mode");
			view.SelectionMode = ListViewSelectionMode.Single;
			view.IsItemClickEnabled = true;
			view.IsMultiSelectCheckBoxEnabled = false;
			view.IsRightTapEnabled = true;
			var ItemGrids = DevWinUI.DependencyObjectExtensions.FindDescendants(view);

			foreach (var item in ItemGrids)
			{
				if (item is UIElement uiElement)
				{
					uiElement.IsRightTapEnabled = true;
				}
			}
			if (view.Name == "MostPlayedSongsListView") Header.Margin = new Thickness(12, 0, 0, 0);
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

		GlobalNotification.Info($"{songPaths.Count} {(songPaths.Count > 1 ? "songs/tracks" : "song/track")} added to the queue.");
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
		DeleteDialogText.Text = $"Are you sure you want to delete {(songList.Count > 1 ? "these" : "this")} {songList.Count} {(songList.Count > 1 ? "songs/tracks" : "song/track")} from your system?";

		var result = await DeleteDialog.ShowAsync();

		if (result == ContentDialogResult.Primary)
		{
			foreach (Song songData in songList)
			{
				if (File.Exists(songData.Path))
				{
					File.Delete(songData.Path);
					await DatabaseHelper.Instance.DeleteSongFromDB(songData.Path);
					MostPlayedSongs.Remove(songData);
				}
			}
			MusicPlayer.Instance.HandleAfterDelete();
			GlobalNotification.Info($"{songList.Count} {(songList.Count > 1 ? "songs/tracks" : "song/track")} deleted.");
		}
		if (await DatabaseHelper.Instance.GetSongsCount() <= 0)
		{
			GoToSettings.Visibility = Visibility.Visible;
			PageButtons.Visibility = Visibility.Collapsed;
		}
	}

	/// <summary>
	/// Prepares and updates the content of a TextBlock with a random, formatted message encouraging users to go to the settings.
	/// </summary>
	/// <remarks>
	/// This method selects a random message from a predefined collection of witty texts, splits it into formatted lines,
	/// and populates the target TextBlock with these lines. Decorative elements, such as line breaks and font styles,
	/// are applied to enhance the appearance of the messages.
	/// </remarks>
	private void AddGoToSettingsMessage()
	{
		string message = GoToMessages[Random.Shared.Next(GoToMessages.Count)];
		var lines = message.Split('\n');

		GoToSettingsTextBlock.Inlines.Clear();
		GoToSettingsTextBlock.Inlines.Add(new Run
		{
			Text = lines[0],
			FontStyle = Windows.UI.Text.FontStyle.Italic
		});

		GoToSettingsTextBlock.Inlines.Add(new LineBreak());

		GoToSettingsTextBlock.Inlines.Add(new Run
		{
			Text = lines[1]
		});
	}

	/// <summary>
	/// Stores a collection of humorous or witty messages displayed when the user's song library is empty or unconfigured.
	/// </summary>
	/// <remarks>
	/// The <c>GoToMessages</c> field contains a predefined list of strings, each consisting of two lines of text split by a newline character.
	/// These messages are intended to provide a lighthearted and engaging user experience, encouraging users to either add tracks,
	/// configure their settings, or scan their music libraries. Each usage randomly selects a message from this list.
	/// </remarks>
	private readonly List<string> GoToMessages = new()
	{
		"“This is a popularity contest—and no one ran for office.”\nPlease, check your settings to ensure your libraries are added and songs/tracks have been scanned—or this page declares itself the winner by default.",
		"“We tried ranking your favorites. The void won.”\nScan before this page starts replaying your last existential thought.",
		"“We’d show your favorite tracks—if we had any.”\nCheck your settings to make sure your library’s been added and scanned.",
		"“Analytics unavailable. Playlist potential: unlimited.”\nAdd your tracks so this page can stop pretending to be mysterious.",
		"“Your top songs are trapped in limbo.”\nScan your library before this page hosts a séance for missing metadata.",
		"“This was meant to be your personal Billboard. Now it's just bored.”\nEnsure libraries are added or the page keeps practicing jazz hands in silence.",
		"“Most played metrics? Still waiting for their debut.”\nYour settings might need a tweak—because right now, even silence is outperforming.",
		"“This space was meant to celebrate your jams, not meditate in emptiness.”\nScan some tracks before it starts whispering affirmations to itself.",
		"“Without scanned songs, even your sneeze could top the charts.”\nHelp us help your music—before this page gives ‘Most Played’ to a notification sound.",
		"“There’s nothing to rank but the silence between your clicks.”\nMake sure your libraries are added so this page can start throwing stars around.",
		"“We have charts. We have algorithms. We just don’t have your songs.”\nTime to scan, or this becomes the Hall of Unplayed Potential.",
		"“MostPlayed is ready for your listening history. It just has amnesia.”\nAdd and scan your library before it starts inventing fake stats to feel useful."
	};

	/// <summary>
	/// Adds a formatted "No Results" message to the UI for scenarios where the most played songs list is empty.
	/// </summary>
	/// <remarks>
	/// This method randomly selects a message from a predefined list of witty "No Results" messages.
	/// It splits the selected message into lines, formats the first line with an italic font style,
	/// and then displays both lines in corresponding text blocks within the UI. This serves to provide
	/// a user-friendly notification while the data is unavailable or insufficient to populate the most played list.
	/// </remarks>
	private void AddNoResultsMessage()
	{
		string message = NoResultsMessages[Random.Shared.Next(NoResultsMessages.Count)];
		var lines = message.Split('\n');

		NoResultsTextBlock.Inlines.Clear();
		NoResultsTextBlock.Inlines.Add(new Run
		{
			Text = lines[0],
			FontStyle = Windows.UI.Text.FontStyle.Italic
		});

		NoResultsTextBlock.Inlines.Add(new LineBreak());

		NoResultsTextBlock.Inlines.Add(new Run
		{
			Text = lines[1]
		});
	}

	/// <summary>
	/// Contains a collection of entertaining and motivational messages to display when no results are available on the page.
	/// </summary>
	/// <remarks>
	/// This collection is designed to engage the user in a playful and encouraging manner when the page has no data to show, such as
	/// when songs have not been played enough to meet the ranking criteria. Each message consists of two parts separated by a newline
	/// character and is intended to provide humor and incentive for deeper interaction with the music library.
	/// </remarks>
	private readonly List<string> NoResultsMessages = new()
	{
		"“No top tracks yet—we’re still waiting on a rhythm worth ranking.”\nHit play, let it ride, and come back when your ears have had a proper workout.",
		"“The algorithm won’t fall for your quick skips.”\nGive a track some real love. It only counts if your headphones get emotionally attached.",
		"“You can't fast-forward your way to the hall of fame.”\nStay a while, vibe a bit, and let this page start building your legend.",
		"“It’s not about pressing play—it’s about meaning it.”\nSpin a few tunes like you care and this page will reward your loyalty.",
		"“We’ve got charts. Now we need heart.”\nListen deep. Once the groove hits critical mass, stats will bloom like fireworks.",
		"“This page respects commitment. Not flings.”\nStick around for a few solid listens before claiming your sonic throne.",
		"“Most played? We don’t count one-night stands.”\nFind your favorite banger and vibe like it’s the only song in the world.",
		"“Data lives here—but only for real listeners.”\nPlay something long enough for this page to stop feeling existential dread.",
		"“We only track relationships—not music speed dates.”\nLet your playlist woo you, and return with numbers worth bragging about.",
		"“Your ears need a real moment. So does this page.”\nDon't just breeze through. Let a track leave an emotional dent."
	};
}
