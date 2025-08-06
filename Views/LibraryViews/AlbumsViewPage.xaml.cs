using System.Collections.ObjectModel;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;

namespace Tunetastic.Views.LibraryViews;

/// <summary>
/// Represents a page in the application that displays a collection of albums grouped as part of the library view.
/// </summary>
/// <remarks>
/// This class manages the UI interactions and data presentation for a library view showing media items
/// organized by album. It leverages binding to display an ObservableCollection of <c>AlbumModel</c> objects.
/// </remarks>
public sealed partial class AlbumsViewPage : Page
{
	/// <summary>
	/// Gets or sets the collection of <c>AlbumModel</c> objects, representing groups of songs organized by album.
	/// </summary>
	/// <remarks>
	/// This property is used in the <c>AlbumsViewPage</c> to manage the grouped data displayed in the library view.
	/// It leverages an <c>ObservableCollection</c> to enable dynamic data updates in the UI when the collection changes.
	/// The grouping and sorting of the albums are updated based on user interaction or application logic.
	/// The data within this property is cleared and refreshed as necessary during page lifecycle events.
	/// </remarks>
	public ObservableCollection<AlbumModel> AlbumsGroup
	{
		get; set;
	} = new();

	private readonly DispatcherQueue _dispatcherQueue;

	public AlbumsViewPage()
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
		AlbumTileView.Visibility = Visibility.Collapsed;
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
				this.Content = new AlbumsViewPage();
			});
			return;
		}

		if (await DatabaseHelper.Instance.GetSongsCount() > 0)
		{
			GoToSettings.Visibility = Visibility.Collapsed;
			SortDropDown.Visibility = Visibility.Visible;
			AlbumTileView.Visibility = Visibility.Visible;
			await UpdateAsPerLastSorting();
			PageButtons.Visibility = Visibility.Visible;
		}
	}

	/// <summary>
	/// Updates the sorting preferences for the song list displayed on the AlbumsViewPage.
	/// </summary>
	/// <remarks>
	/// This method determines the sorting criteria and order (e.g., by title, artist, album, duration.)
	/// based on the user's saved preferences in local settings. It also updates the selection status
	/// of the UI elements corresponding to the sorting options and triggers the list update.
	/// </remarks>
	private async Task UpdateAsPerLastSorting()
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var sortOrder = localSettings.Values[nameof(LocalSave.AlbumsViewSortOrder)]?.ToString() ?? "Ascending";

		switch (sortOrder)
		{
			case "Descending":
				Descending.IsChecked = true;
				break;

			case "Ascending":
			default:
				Ascending.IsChecked = true;
				break;
		}
		await UpdateListBasedOnSorting();
	}

	/// <summary>
	/// Updates the album list view based on the current sorting order and preferences.
	/// </summary>
	/// <remarks>
	/// This asynchronous method fetches album data grouped by the selected sorting order (ascending or descending).
	/// It refreshes the album tiles to reflect the updated order while maintaining the currently selected album, if applicable.
	/// The method also updates UI elements such as sort indicators and tooltips to match the latest sorting state.
	/// The updated sorting preferences are stored in local settings for consistent behavior across application sessions.
	/// </remarks>
	/// <returns>
	/// A task that represents the asynchronous operation of updating the album list and user interface.
	/// </returns>
	private async Task UpdateListBasedOnSorting()
	{
		var albumModel = AlbumTileView.SelectedItem as AlbumModel;
		ShimmerRepeater.Visibility = Visibility.Visible;

		var orderBy = Sort.Items.OfType<RadioMenuFlyoutItem>().Where(item => item.GroupName == "Order" && item.IsChecked).Select(item => item.Text).FirstOrDefault() ?? "Ascending";
		bool AscOrder = orderBy == "Ascending";

		var groups = await DatabaseHelper.Instance.GetSongsGroupedByAlbum(AscOrder);
		AlbumsGroup.Clear();
		AlbumsGroup.AddRange(groups);
		groups = null;

		ShimmerRepeater.Visibility = Visibility.Collapsed;
		AlbumTileView.Visibility = Visibility.Visible;

		NavigationPanelEvaluate();

		var sortDropdownContent = new TextBlock();
		sortDropdownContent.Inlines.Add(new Run { Text = "Order: " });
		sortDropdownContent.Inlines.Add(new Run { Text = $" {(AscOrder ? "⬆️" : "⬇️")}" });

		var orderDropdownTooltip = new TextBlock();
		orderDropdownTooltip.Inlines.Add(new Run { Text = "The tiles are sorted in " });
		orderDropdownTooltip.Inlines.Add(new Run { Text = orderBy, FontWeight = Microsoft.UI.Text.FontWeights.Bold });
		orderDropdownTooltip.Inlines.Add(new Run { Text = " order." });

		SortDropDown.Content = sortDropdownContent;
		ToolTipService.SetToolTip(SortDropDown, orderDropdownTooltip);
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.AlbumsViewSortOrder)] = orderBy;

		albumModel = AlbumsGroup.Select(s => s).Where(s => s.Album == albumModel?.Album).FirstOrDefault();
		await Task.Delay(500);
		await ScrollToTile(albumModel);
	}

	/// <summary>
	/// Handles the `Loaded` event for the AlbumsViewPage.
	/// </summary>
	/// <param name="sender">The source of the event, generally the page itself.</param>
	/// <param name="e">The event data associated with the `Loaded` event.</param>
	/// <remarks>
	/// This method initializes content and handles animations when the page is loaded. It ensures that the `AlbumsGroup` collection is populated before proceeding. If a connected animation is active, it retrieves the selected album and attempts to animate the transition back to the associated UI element. The method also manages navigation states and scrolls to the current playing track if applicable.
	/// </remarks>
	private async void Page_Loaded(object sender, RoutedEventArgs e)
	{
		while (AlbumsGroup == null || AlbumsGroup.Count == 0)
		{
			await Task.Delay(100);
		}

		AlbumTileView_SizeChanged(null, null);
		AlbumTileView.ContainerContentChanging += AlbumTileView_ContainerContentChanging;

		if (connectedAnimation)
		{
			var selectedAlbum = (Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.SelectedAlbum)]?.ToString());
			var selectedAlbumModel = AlbumsGroup.Select(s => s).Where(s => s.Album == selectedAlbum).FirstOrDefault();

			var animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("AlbumHeaderAnimationBack");

			if (animation != null && selectedAlbumModel != null)
			{
				await Task.Delay(30);
				await AlbumTileView.SmoothScrollIntoViewWithItemAsync(selectedAlbumModel, itemPlacement: ScrollItemPlacement.Top, disableAnimation: true, scrollIfVisible: false);
				await Task.Delay(100);

				var container = AlbumTileView.ContainerFromItem(selectedAlbumModel) as ListViewItem;
				if (container != null)
				{
					var albumTextBlock = DevWinUI.DependencyObjectEx.FindDescendant(container, "AlbumTextBlock");
					if (albumTextBlock != null)
						animation.TryStart(albumTextBlock);
				}
			}
			connectedAnimation = false;
			var currentPlaylist = Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CurrentPlayinglist)]?.ToString() ?? string.Empty;
			if (currentPlaylist.StartsWith("AlbumGroup>"))
			{
				var SelectedTile = AlbumsGroup.Select(s => s).Where(s =>
				{
					var v = currentPlaylist.Substring("AlbumGroup>".Length);
					return s.Album == (v == "Unknown Album" ? "Unknown" : v);
				}).FirstOrDefault();
				AlbumTileView.SelectedItem = SelectedTile;
			}
		}
		else
			ScrollToCurrentPlayingTrack();
		await Task.Delay(100);
		AlbumTileView.SizeChanged += AlbumTileView_SizeChanged;
	}

	/// <summary>
	/// Scrolls the view to the currently playing track if the current playlist corresponds to the "AlbumsViewPage".
	/// </summary>
	/// <remarks>
	/// This method checks the local application settings to determine if the "AlbumsViewPage" is the active playlist.
	/// If it is, the method retrieves the last played track based on its path from the saved settings and attempts to scroll
	/// the page to that specific song within the song collection.
	/// </remarks>
	private void ScrollToCurrentPlayingTrack()
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var currentPlaylist = localSettings.Values[nameof(LocalSave.CurrentPlayinglist)]?.ToString() ?? string.Empty;
		if (currentPlaylist.StartsWith("AlbumGroup>"))
		{
			var SelectedTile = AlbumsGroup.Select(s => s).Where(s =>
			{
				var v = currentPlaylist.Substring("AlbumGroup>".Length);
				return s.Album == (v == "Unknown Album" ? "Unknown" : v);
			}).FirstOrDefault();
			_ = ScrollToTile(SelectedTile);
		}
	}

	/// <summary>
	/// Scrolls to a specific song in the View.
	/// </summary>
	/// <param name="song">The song object to scroll to. If null, no action is performed.</param>
	/// <returns>A task representing the asynchronous operation of scrolling to the specified song.</returns>
	private async Task ScrollToTile(AlbumModel? tile)
	{
		if (tile != null)
		{
			AlbumTileView.SelectedItem = tile;
			await AlbumTileView.SmoothScrollIntoViewWithItemAsync(tile, itemPlacement: ScrollItemPlacement.Center, disableAnimation: false, scrollIfVisible: false);
		}
	}

	/// <summary>
	/// Handles the Sort button click event to update the song list based on the selected sorting criteria.
	/// </summary>
	/// <param name="sender">The control that triggered the event, typically a UI element like a menu flyout item.</param>
	/// <param name="e">Event data associated with the Sort button click.</param>
	private async void SortButton_OnClick(object sender, RoutedEventArgs e)
	{
		await UpdateListBasedOnSorting();
		await AdjustAlphabetSize();
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
		if (AlbumTileView.IsMultiSelectCheckBoxEnabled)
		{
			MoreButton.IsEnabled = AlbumTileView.SelectedItems.Count > 0;
		}
	}

	/// <summary>
	/// Handles the Unloaded event for the AlbumsViewPage.
	/// </summary>
	/// <remarks>
	/// This method is invoked when the AlbumsViewPage is unloaded. It performs a series of cleanup tasks, including clearing album collections,
	/// nullifying resources associated with the album tile view, collapsing its visibility, and triggering garbage collection to
	/// free up memory resources. These operations ensure efficient memory management and improve application performance.
	/// </remarks>
	/// <param name="sender">The source of the Unloaded event, typically the AlbumsViewPage instance.</param>
	/// <param name="e">The event arguments associated with the Unloaded event.</param>
	private async void Page_Unloaded(object sender, RoutedEventArgs e)
	{
		foreach (var item in AlbumsGroup)
		{
			var container = AlbumTileView.ContainerFromItem(item) as ListViewItem;
			if (container != null)
			{
				var image = DevWinUI.DependencyObjectEx.FindDescendant(container, "AlbumCover") as Image;
				if (image != null)
				{
					var bmp = image.Source as BitmapImage;
					if (bmp != null) bmp.UriSource = null;
					image.Source = null;
				}
			}
		}

		AlbumsGroup.Clear();
		AlbumsGroup = null;
		AlbumTileView.ItemsSource = null;
		AlbumTileView.ItemTemplate = null;
		AlbumTileView.ItemsPanel = null;
		AlbumTileView.Visibility = Visibility.Collapsed;
		GC.Collect();
		GC.WaitForPendingFinalizers();
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
			ToolTipService.SetToolTip(menuItem, $"Add {(MultiSelectButton.IsChecked == true ? "selected groups" : "this group")} of songs/tracks to {playList} playlist");
			menuItem.Click += AddToPlaylist_Click;
			addToPlaylist?.Items.Add(menuItem);
		}
	}

	/// <summary>
	/// Handles the addition of songs or tracks from the current library view to a specified playlist.
	/// </summary>
	/// <param name="sender">The source of the event, typically the menu item representing the target playlist.</param>
	/// <param name="e">The event data triggered by the menu item's click action.</param>
	/// <remarks>
	/// This method is invoked when the user selects the "Add to Playlist" option.
	/// It identifies the selected songs or groups of songs based on the view's multi-select mode.
	/// If multiple albums are selected, it fetches all songs corresponding to those albums and adds them to the chosen playlist.
	/// In single selection mode, the process targets the respective group of album.
	/// Updates are performed through interactions with the database using asynchronous operations.
	/// A notification is displayed upon successfully adding the songs to the playlist.
	/// </remarks>
	private async void AddToPlaylist_Click(object sender, RoutedEventArgs e)
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

		if (AlbumTileView.IsMultiSelectCheckBoxEnabled)
		{
			var albumModels = AlbumTileView.SelectedItems;
			var songList = await DatabaseHelper.Instance.LoadSongsFromDB(orderBy: Enum.Parse<SongProperty>(localSettings.Values[nameof(LocalSave.AlbumDetailViewSortBy)]?.ToString() ?? "Title"),
																		 ascending: (localSettings.Values[nameof(LocalSave.AlbumDetailViewSortOrder)]?.ToString() ?? "Ascending") == "Ascending",
																		 whereCondition: $"{SongProperty.Album.ToString()} IN ({string.Join(",", albumModels.Select(y => $"'{((y as AlbumModel)?.Album == "Unknown" ? "Unknown Album" : (y as AlbumModel)?.Album)?.Replace("'", "''").Replace("\\", "\\\\").Replace("\"", "\\\"")}'"))})");

			var playlist = (sender as MenuFlyoutItem)?.Text;

			if (playlist != null)
				await DatabaseHelper.Instance.AddSongsToPlaylist(playlist, songList.Select(s => s.Path).ToList());

			GlobalNotification.Info($"All {songList.Count} {(songList.Count > 1 ? "songs/tracks" : "song/track")} of selected albums, added to {playlist} playlist.");
		}
		else
		{
			var albumModel = (sender as MenuFlyoutItem)?.DataContext as AlbumModel;
			var playlist = (sender as MenuFlyoutItem)?.Text;

			if (playlist != null && albumModel != null)
			{
				var songList = await DatabaseHelper.Instance.LoadSongsFromDB(orderBy: Enum.Parse<SongProperty>(localSettings.Values[nameof(LocalSave.AlbumDetailViewSortBy)]?.ToString() ?? "Title"),
																			 ascending: (localSettings.Values[nameof(LocalSave.AlbumDetailViewSortOrder)]?.ToString() ?? "Ascending") == "Ascending",
																			 whereCondition: $"{SongProperty.Album.ToString()} = '{(albumModel.Album == "Unknown" ? "Unknown Album" : albumModel.Album).Replace("'", "''").Replace("\\", "\\\\").Replace("\"", "\\\"")}'");
				await DatabaseHelper.Instance.AddSongsToPlaylist(playlist, songList.Select(s => s.Path).ToList());
				GlobalNotification.Info($"All {songList.Count} {(songList.Count == 1 ? "song/track" : "songs/tracks")} of Album {albumModel.Album} added to {playlist} playlist.");
			}
		}
	}

	/// <summary>
	/// Handles the click event for the "Play" menu flyout item in the album group view.
	/// </summary>
	/// <param name="sender">The source of the event, typically a menu flyout item representing a album group.</param>
	/// <param name="e">Provides event data for the click event.</param>
	/// <remarks>
	/// This method processes the selected album group's data to retrieve all associated songs from the database. It builds a playlist containing the songs
	/// and initiates playback through the application's music player, starting with the first song. It also updates local settings to maintain
	/// the current playback context and highlights the selected album group in the user interface.
	/// </remarks>
	private async void MenuFlyoutItemPlay_OnClick(object sender, RoutedEventArgs e)
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var albumModel = (sender as MenuFlyoutItem)?.DataContext as AlbumModel;
		var songList = await DatabaseHelper.Instance.LoadSongsFromDB(orderBy: Enum.Parse<SongProperty>(localSettings.Values[nameof(LocalSave.AlbumDetailViewSortBy)]?.ToString() ?? "Title"),
																	 ascending: (localSettings.Values[nameof(LocalSave.AlbumDetailViewSortOrder)]?.ToString() ?? "Ascending") == "Ascending",
																	 whereCondition: $"{SongProperty.Album.ToString()} = '{(albumModel?.Album == "Unknown" ? "Unknown Album" : albumModel?.Album)?.Replace("'", "''").Replace("\\", "\\\\").Replace("\"", "\\\"")}'");
		List<string> songPaths = songList.Select(s => s.Path).ToList();
		MusicPlayer.Instance.LoadPlaylist(songPaths, songPaths[0]);
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CurrentPlayinglist)] = $"AlbumGroup>{albumModel?.Album}";
		AlbumTileView.SelectedItem = albumModel;
	}

	/// <summary>
	/// Handles the click event for the "Add to queue" menu flyout item.
	/// </summary>
	/// <remarks>
	/// This method retrieves song data associated with a specific album using the menu item's data context. It queries the database
	/// for songs matching the selected album, sorts them based on user preferences, and adds their file paths to the queued playing list.
	/// A notification is displayed upon successfully adding songs to the queue.
	/// </remarks>
	/// <param name="sender">The source of the event, typically the MenuFlyoutItem that was clicked.</param>
	/// <param name="e">The event data associated with the routed event.</param>
	private async void MenuFlyoutItemAddToQueue_OnClick(object sender, RoutedEventArgs e)
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var albumModel = (sender as MenuFlyoutItem)?.DataContext as AlbumModel;
		var songList = await DatabaseHelper.Instance.LoadSongsFromDB(orderBy: Enum.Parse<SongProperty>(localSettings.Values[nameof(LocalSave.AlbumDetailViewSortBy)]?.ToString() ?? "Title"),
																	 ascending: (localSettings.Values[nameof(LocalSave.AlbumDetailViewSortOrder)]?.ToString() ?? "Ascending") == "Ascending",
																	 whereCondition: $"{SongProperty.Album.ToString()} = '{(albumModel?.Album == "Unknown" ? "Unknown Album" : albumModel?.Album)?.Replace("'", "''").Replace("\\", "\\\\").Replace("\"", "\\\"")}'");
		List<string> songPaths = songList.Select(s => s.Path).ToList();

		await DatabaseHelper.Instance.AddSongsToQueuedPlayingList(songPaths);

		GlobalNotification.Info($"All {songList.Count} {(songList.Count == 1 ? "song/track" : "songs/tracks")} of Album {albumModel?.Album} added to queue.");
	}

	/// <summary>
	/// Handles the delete action when a menu flyout item is clicked, prompting the user with a confirmation dialog and deleting songs corresponding to a specific album if confirmed.
	/// </summary>
	/// <param name="sender">The event sender, usually a MenuFlyoutItem representing the delete option.</param>
	/// <param name="e">The event data associated with the click event.</param>
	/// <remarks>
	/// This method retrieves songs matching the specified album value from the database, generates a confirmation prompt displaying
	/// the number of songs to be deleted, and upon user confirmation:
	/// 1. Deletes the files associated with the songs from the local storage.
	/// 2. Removes the corresponding records from the database.
	/// 3. Updates the UI to reflect the changes, including any adjustments to song counts or visibility of album entries.
	/// If no songs are present that match the specified album, no action is taken.
	/// </remarks>
	private async void MenuFlyoutItemDelete_OnClick(object sender, RoutedEventArgs e)
	{
		DeleteDialog.Visibility = Visibility.Visible;
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var albumModel = (sender as MenuFlyoutItem)?.DataContext as AlbumModel;
		var songList = await DatabaseHelper.Instance.LoadSongsFromDB(orderBy: Enum.Parse<SongProperty>(localSettings.Values[nameof(LocalSave.AlbumDetailViewSortBy)]?.ToString() ?? "Title"),
																	 ascending: (localSettings.Values[nameof(LocalSave.AlbumDetailViewSortOrder)]?.ToString() ?? "Ascending") == "Ascending",
																	 whereCondition: $"{SongProperty.Album.ToString()} = '{(albumModel?.Album == "Unknown" ? "Unknown Album" : albumModel?.Album)?.Replace("'", "''").Replace("\\", "\\\\").Replace("\"", "\\\"")}'");
		List<string> songPaths = songList.Select(s => s.Path).ToList();

		if (songPaths?.Count > 0)
		{
			DeleteDialogText.Text = $"Are you sure you want to delete {(songPaths.Count > 1 ? "these songs/tracks" : "this song/track")} of Album {albumModel?.Album} from your system?";

			var result = await DeleteDialog.ShowAsync();
			if (result == ContentDialogResult.Primary)
			{
				foreach (string songPath in songPaths)
				{
					if (File.Exists(songPath))
					{
						File.Delete(songPath);
						await DatabaseHelper.Instance.DeleteSongFromDB(songPath);
					}
				}
				AlbumsGroup.Remove(albumModel);
				MusicPlayer.Instance.HandleAfterDelete();
				GlobalNotification.Info($"All {songPaths.Count} {(songPaths.Count > 1 ? "songs/tracks" : "song/track")} of Album {albumModel?.Album} deleted.");
			}
			if (await DatabaseHelper.Instance.GetSongsCount() <= 0)
			{
				GoToSettings.Visibility = Visibility.Visible;
				PageButtons.Visibility = Visibility.Collapsed;
			}
			else
			{
				NavigationPanelEvaluate();
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
		if (MultiSelectButton.IsChecked == true)
		{
			MoreButton.Visibility = Visibility.Visible;
			MoreButton.IsEnabled = false;
			SortAndViewButtonPanel.Visibility = Visibility.Collapsed;
			ToolTipService.SetToolTip(MultiSelectButton, "Turn off multi-select mode");
			AlbumTileView.SelectionMode = ListViewSelectionMode.Multiple;
			AlbumTileView.IsItemClickEnabled = false;
			AlbumTileView.IsMultiSelectCheckBoxEnabled = true;
			AlbumTileView.IsRightTapEnabled = false;
			var ItemGrids = DevWinUI.DependencyObjectEx.FindDescendants(AlbumTileView);

			foreach (var item in ItemGrids)
			{
				if (item is UIElement uiElement)
				{
					uiElement.IsRightTapEnabled = false;
				}
			}
		}
		else
		{
			MoreButton.Visibility = Visibility.Collapsed;
			SortAndViewButtonPanel.Visibility = Visibility.Visible;
			ToolTipService.SetToolTip(MultiSelectButton, "Turn on multi-select mode");
			AlbumTileView.SelectionMode = ListViewSelectionMode.Single;
			AlbumTileView.IsItemClickEnabled = true;
			AlbumTileView.IsMultiSelectCheckBoxEnabled = false;
			AlbumTileView.IsRightTapEnabled = true;
			var ItemGrids = DevWinUI.DependencyObjectEx.FindDescendants(AlbumTileView);

			foreach (var item in ItemGrids)
			{
				if (item is UIElement uiElement)
				{
					uiElement.IsRightTapEnabled = true;
				}
			}
		}
	}

	/// <summary>
	/// Handles the click event for adding multiple selected albums' songs to the playing queue from the menu flyout.
	/// </summary>
	/// <param name="sender">The source of the event, typically the menu flyout item that was clicked.</param>
	/// <param name="e">The routed event data that contains event-specific information.</param>
	/// <remarks>
	/// This method retrieves the albums selected by the user from the UI, uses the <see cref="DatabaseHelper"/> instance to fetch all associated songs from the database,
	/// and asynchronously adds their file paths to the playing queue. A notification is displayed upon successfully adding the songs to the queue.
	/// The number of songs added, and the selected albums are also considered in the feedback notification.
	/// </remarks>
	private async void MenuFlyoutMultiItemAddToQueue_OnClick(object sender, RoutedEventArgs e)
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var albumModels = AlbumTileView.SelectedItems;
		var songList = await DatabaseHelper.Instance.LoadSongsFromDB(orderBy: Enum.Parse<SongProperty>(localSettings.Values[nameof(LocalSave.AlbumDetailViewSortBy)]?.ToString() ?? "Title"),
																	 ascending: (localSettings.Values[nameof(LocalSave.AlbumDetailViewSortOrder)]?.ToString() ?? "Ascending") == "Ascending",
																	 whereCondition: $"{SongProperty.Album.ToString()} IN ({string.Join(",", albumModels.Select(y => $"'{((y as AlbumModel)?.Album == "Unknown" ? "Unknown Album" : (y as AlbumModel)?.Album)?.Replace("'", "''").Replace("\\", "\\\\").Replace("\"", "\\\"")}'"))})");
		List<string> songPaths = songList.Select(s => s.Path).ToList();

		await DatabaseHelper.Instance.AddSongsToQueuedPlayingList(songPaths);

		GlobalNotification.Info($"All {songList.Count} {(songPaths.Count > 1 ? "songs/tracks" : "song/track")} of selected albums, added to queue.");
	}

	/// <summary>
	/// Handles the click event for the "Delete" menu flyout item, facilitating the deletion of selected song groups from the system.
	/// </summary>
	/// <remarks>
	/// This method retrieves the songs associated with the selected albums, prompts the user for confirmation via a dialog,
	/// and deletes the songs from both the application's database and the file system if the user confirms.
	/// Additionally, it updates the UI components by removing the selected album groups and displaying appropriate notifications
	/// or controls based on the results of the deletion process.
	/// </remarks>
	/// <param name="sender">The source object of the event, typically the "Delete" menu flyout item.</param>
	/// <param name="e">Event data providing context about the "Delete" menu flyout item click action.</param>
	private async void MenuFlyoutMultiItemDelete_OnClick(object sender, RoutedEventArgs e)
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var albumModels = AlbumTileView.SelectedItems;
		var songList = await DatabaseHelper.Instance.LoadSongsFromDB(orderBy: Enum.Parse<SongProperty>(localSettings.Values[nameof(LocalSave.AlbumDetailViewSortBy)]?.ToString() ?? "Title"),
																	 ascending: (localSettings.Values[nameof(LocalSave.AlbumDetailViewSortOrder)]?.ToString() ?? "Ascending") == "Ascending",
																	 whereCondition: $"{SongProperty.Album.ToString()} IN ({string.Join(",", albumModels.Select(y => $"'{((y as AlbumModel)?.Album == "Unknown" ? "Unknown Album" : (y as AlbumModel)?.Album)?.Replace("'", "''").Replace("\\", "\\\\").Replace("\"", "\\\"")}'"))})");
		List<string> songPaths = songList.Select(s => s.Path).ToList();

		DeleteDialog.Visibility = Visibility.Visible;
		DeleteDialogText.Text = $"Are you sure you want to delete {(songPaths.Count > 1 ? "these songs/tracks" : "this song/track")} of selected albums from your system?";

		var result = await DeleteDialog.ShowAsync();

		if (result == ContentDialogResult.Primary)
		{
			foreach (string songPath in songPaths)
			{
				if (File.Exists(songPath))
				{
					File.Delete(songPath);
					await DatabaseHelper.Instance.DeleteSongFromDB(songPath);
				}
			}

			List<AlbumModel> albumList = new();
			foreach (var item in albumModels)
				albumList.Add((AlbumModel)item);

			foreach (var albumModel in albumList)
				AlbumTileView.Items.Remove(albumModel);

			MusicPlayer.Instance.HandleAfterDelete();
			GlobalNotification.Info($"All {songPaths.Count} {(songPaths.Count > 1 ? "songs/tracks" : "song/track")} of selected albums deleted.");
		}
		if (await DatabaseHelper.Instance.GetSongsCount() <= 0)
		{
			GoToSettings.Visibility = Visibility.Visible;
			PageButtons.Visibility = Visibility.Collapsed;
		}
		else
		{
			NavigationPanelEvaluate();
		}
	}

	/// <summary>
	/// Handles the item click event for the AlbumTileView and navigates to the detail page corresponding to the selected album.
	/// </summary>
	/// <param name="sender">The object that raised the event, typically the ListView control.</param>
	/// <param name="e">Provides data about the clicked item, including the <see cref="AlbumModel"/> representing the selected album.</param>
	/// <remarks>
	/// This method processes the clicked item by extracting its details, performing additional validations, and navigating to the <c>AlbumDetailPage</c>.
	/// It also stores the selected album in application settings for retrieval in subsequent operations.
	/// </remarks>
	private void AlbumTileView_ItemClick(object sender, ItemClickEventArgs e)
	{
		var albumModel = e.ClickedItem as AlbumModel;

		if (albumModel != null)
		{
			var container = AlbumTileView.ContainerFromItem(albumModel) as ListViewItem;
			if (container != null)
			{
				var albumTextBlock = DevWinUI.DependencyObjectEx.FindDescendant(container, "AlbumTextBlock");
				if (albumTextBlock != null)
				{
					ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("AlbumHeaderAnimation", albumTextBlock);

					var fadeOut = new DoubleAnimation
					{
						To = 0,
						Duration = TimeSpan.FromMilliseconds(30),
						FillBehavior = FillBehavior.Stop
					};
					Storyboard.SetTarget(fadeOut, albumTextBlock);
					Storyboard.SetTargetProperty(fadeOut, "Opacity");

					var sb = new Storyboard();
					sb.Children.Add(fadeOut);
					sb.Completed += (_, __) => albumTextBlock.Visibility = Visibility.Collapsed;
					sb.Begin();
				}
			}

			Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.SelectedAlbum)] = albumModel.Album;
			App.Current.NavService.NavigateTo(typeof(AlbumDetailPage), albumModel.Album, false, new DrillInNavigationTransitionInfo());
		}
	}

	/// <summary>
	/// A boolean flag indicating the state of the connected animation during navigation within the <c>AlbumsViewPage</c>.
	/// </summary>
	/// <remarks>
	/// This variable is used to determine whether to invoke the connected animation sequence when navigating
	/// to or from the <c>AlbumsViewPage</c>. It is set based on the navigation mode or parameters passed during
	/// navigation, ensuring a smooth transition effect between pages. When true, connected animations
	/// are enabled to provide a seamless visual experience for the user, such as animating focused album tiles.
	/// The value is modified dynamically during the page lifecycle to manage transitions and UI updates properly.
	/// </remarks>
	private bool connectedAnimation = false;
	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);

		connectedAnimation = (e.NavigationMode == NavigationMode.Back) || (e.Parameter is string && e.Parameter == "Albums");

		if (ShimmerRepeater.Visibility == Visibility.Visible && (ContentGrid.ActualWidth > 0 && ContentGrid.ActualHeight > 0))
		{
			Size currentSize = new Size(ContentGrid.ActualWidth, ContentGrid.ActualHeight);
			UpdateShimmerPlaceholderCount(currentSize);
		}
	}

	/// <summary>
	/// Handles navigation away from the page and prepares connected animations for the transition.
	/// </summary>
	/// <param name="e">An instance of <see cref="NavigatingCancelEventArgs"/> that contains the event data related to the navigation operation.</param>
	/// <remarks>
	/// This method is invoked when the application navigates away from the current page. It retrieves the currently selected album from local settings
	/// and identifies the corresponding <see cref="AlbumModel"/> instance from the <see cref="AlbumsGroup"/> collection. If a matching album is found,
	/// it attempts to locate the associated UI element in the visual tree of the page. If successful, it prepares a connected animation for a smooth
	/// transition to the target page.
	/// </remarks>
	protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
	{
		if (e.SourcePageType.Name == "AlbumDetailPage")
		{
			var selectedAlbum = (Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.SelectedAlbum)]?.ToString());
			var selectedAlbumModel = AlbumsGroup.Select(s => s).Where(s => s.Album == selectedAlbum).FirstOrDefault();

			var container = AlbumTileView.ContainerFromItem(selectedAlbumModel) as ListViewItem;
			if (container != null)
			{
				var albumTextBlock = DevWinUI.DependencyObjectEx.FindDescendant(container, "AlbumTextBlock");
				if (albumTextBlock != null)
				{
					ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("AlbumHeaderAnimation", albumTextBlock);

					var fadeOut = new DoubleAnimation
					{
						To = 0,
						Duration = TimeSpan.FromMilliseconds(30),
						FillBehavior = FillBehavior.Stop
					};
					Storyboard.SetTarget(fadeOut, albumTextBlock);
					Storyboard.SetTargetProperty(fadeOut, "Opacity");

					var sb = new Storyboard();
					sb.Children.Add(fadeOut);
					sb.Completed += (_, __) => albumTextBlock.Visibility = Visibility.Collapsed;
					sb.Begin();
				}
			}
		}
	}

	private const double TileWidth = 190;
	private const double TileHeight = 230;
	private const int MinPlaceholderCount = 50;

	/// <summary>
	/// Updates the number of shimmer placeholders displayed in the user interface based on the available content size.
	/// </summary>
	/// <param name="availableSize">The current size of the content area, typically specified as width and height dimensions.</param>
	/// <remarks>
	/// This method calculates the number of columns and rows that can fit within the given available size
	/// based on predefined tile dimensions. It ensures a minimum placeholder count is met and updates the
	/// ItemsSource of the shimmer repeater for placeholder visualization.
	/// </remarks>
	private void UpdateShimmerPlaceholderCount(Size availableSize)
	{
		int columns = Math.Max(1, (int)(availableSize.Width / TileWidth));
		int rows = Math.Max(1, (int)(availableSize.Height / TileHeight)) + 1;
		int requiredCount = Math.Max(columns * rows, MinPlaceholderCount);

		ShimmerRepeater.ItemsSource = Enumerable.Range(0, requiredCount);
	}

	/// <summary>
	/// Adjusts the layout and styling of items within the AlbumTileView control dynamically based on the available width.
	/// </summary>
	/// <param name="sender">The source of the event, usually the AlbumTileView control.</param>
	/// <param name="e">Event data that provides information about the size changes.</param>
	/// <remarks>
	/// This method calculates the optimal dimensions for items in the AlbumTileView control,
	/// ensuring that items are spaced appropriately and the layout adapts to different screen sizes.
	/// The method determines the maximum number of items that can fit horizontally and adjusts item width and margins.
	/// For wrapping layouts where all items cannot fit, the method redistributes the available space among visible items.
	/// </remarks>
	private void AlbumTileView_SizeChanged(object? sender, SizeChangedEventArgs? e)
	{
		const double baseContentWidth = 220;
		const double baseMargin = 10;

		double availableWidth = AlbumTileView.ActualWidth;
		if (availableWidth <= 0 || AlbumTileView.ItemsPanelRoot is not ItemsWrapGrid grid)
			return;

		int itemCount = AlbumTileView.Items.Count;
		if (itemCount <= 0) return;

		int maxFitCount = (int)(availableWidth / (baseContentWidth + 2 * baseMargin));
		int count = Math.Min(itemCount, maxFitCount);

		double usedWidth = count * baseContentWidth + (count + 1) * baseMargin;
		double remainingSpace = availableWidth - usedWidth;

		bool layoutIsWrapping = itemCount > count || remainingSpace < baseMargin * 2;

		double adjustedItemWidth = baseContentWidth;
		double adjustedMargin = baseMargin;

		if (layoutIsWrapping && count > 0)
		{
			double extraPerTile = remainingSpace / count;
			adjustedItemWidth += extraPerTile;
			adjustedMargin += extraPerTile * 0.5;
		}

		for (int i = 0; i < itemCount; i++)
		{
			var container = AlbumTileView.ContainerFromItem(AlbumTileView.Items[i]) as ListViewItem;
			if (container != null)
			{
				container.Margin = new Thickness(adjustedMargin, 15, adjustedMargin, 15);
			}
		}
		grid.ItemWidth = adjustedItemWidth;
	}

	/// <summary>
	/// Occurs during the progressive content rendering of an album tile within the album list view, dynamically adapting the UI for items that are becoming visible.
	/// </summary>
	/// <param name="sender">
	/// The ListViewBase control that triggered the event.
	/// </param>
	/// <param name="args">
	/// Provides data for the container content changing event, indicating the specific item and its container that are being updated or visualized.
	/// </param>
	/// <remarks>
	/// This method is invoked whenever a container's UI content is about to change for a specific album tile in the <c>AlbumTileView</c>.
	/// It ensures that size adjustments or UI modifications are handled depending on the content visibility. The method contributes
	/// to maintaining responsive performance by managing how elements are dynamically rendered as the user navigates or scrolls through the album view.
	/// </remarks>
	private void AlbumTileView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
	{
		AlbumTileView_SizeChanged(null, null);
	}

	/// <summary>
	/// Updates the navigation panel based on the current state of the album collection and the selected sorting order.
	/// </summary>
	/// <remarks>
	/// This method evaluates the available albums and their initial characters, determines their order based on the current sorting criteria,
	/// and assesses the presence of special characters. Using this information, it generates and updates the navigation panel to
	/// reflect the available alphabet options for quick navigation within the album view.
	/// </remarks>
	private void NavigationPanelEvaluate()
	{
		var orderBy = Sort.Items.OfType<RadioMenuFlyoutItem>().Where(item => item.GroupName == "Order" && item.IsChecked).Select(item => item.Text).FirstOrDefault() ?? "Ascending";
		bool AscOrder = orderBy == "Ascending";

		IOrderedEnumerable<string>? availableLetters = null;
		availableLetters = AlbumsGroup.Select(album => album.Album.Substring(0, 1).ToUpper()).Distinct().OrderBy(c => c);
		bool hasSpecialCharacters = AlbumsGroup.Select(album => album.Album.Substring(0, 1)).Where(c => !char.IsLetter(c[0])).Distinct().OrderBy(c => c).ToList().Any();

		PopulateAlphabetNavigation(availableLetters, AscOrder, hasSpecialCharacters);
	}

	/// <summary>
	/// Populates the alphabet navigation panel with letters and optionally a special character marker
	/// for navigating sections of songs.
	/// </summary>
	/// <param name="availableLetters">
	/// A collection of letters representing song sections to be included in navigation. Null indicates
	/// all letters are marked as unavailable.
	/// </param>
	/// <param name="order">
	/// A flag indicating whether the letters are ordered in ascending or descending order.
	/// </param>
	/// <param name="sortBy">
	/// The sorting criterion to define navigation to specific column in the song collection.
	/// </param>
	/// <param name="hasSpecialCharacters">
	/// A flag specifying whether special characters (e.g., "#") are included in the navigation.
	/// </param>
	/// <remarks>
	/// This method clears all existing child elements in the alphabet navigation panel before creating
	/// and adding dynamically generated navigation elements. Each letter element is configured based on
	/// its validity from the provided collection. Additionally, it defines interaction behavior for
	/// navigable elements to handle user input.
	/// </remarks>
	private async void PopulateAlphabetNavigation(IOrderedEnumerable<string>? availableLetters, bool order, bool hasSpecialCharacters)
	{
		AlphabetNavigationPanel.Children.Clear();
		if (availableLetters == null && !hasSpecialCharacters) return;

		var fullAlphabet = Enumerable.Range('A', 26).Select(x => ((char)x).ToString());
		if (hasSpecialCharacters) fullAlphabet = fullAlphabet.Reverse().Append("#").Reverse();
		if (!order) fullAlphabet = fullAlphabet.Reverse();

		double availableSpace = AlbumTileView.ActualHeight;

		double autoHeight = availableSpace / fullAlphabet.Count();

		foreach (var letter in fullAlphabet)
		{
			bool hasSongs = availableLetters == null ? false : (availableLetters.Contains(letter)) || (letter == "#" && hasSpecialCharacters);

			var Button = new Button
			{
				Content = letter,
				Foreground = new SolidColorBrush(hasSongs ? App.Current.ThemeService.GetActualTheme() == ElementTheme.Dark ? Colors.White : Colors.Black : Colors.Gray),
				Opacity = hasSongs ? 1 : 0.5,
				Background = new SolidColorBrush(Colors.Transparent),
				BorderBrush = new SolidColorBrush(Colors.Transparent),
				BorderThickness = new Thickness(0),
				IsHitTestVisible = hasSongs,
				Margin = new Thickness(0),
				HorizontalContentAlignment = HorizontalAlignment.Right,
				VerticalContentAlignment = VerticalAlignment.Stretch,
				Padding = new Thickness(0),
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Stretch,
				Height = autoHeight
			};

			if (hasSongs)
			{
				ToolTipService.SetPlacement(Button, Microsoft.UI.Xaml.Controls.Primitives.PlacementMode.Left);
				ToolTipService.SetToolTip(Button, letter);
				Button.Tapped += (s, e) => ScrollToSection(letter);
			}

			AlphabetNavigationPanel.Children.Add(Button);
			await Task.Delay(1);
		}
		_ = AdjustAlphabetSize();
		availableLetters = null;
	}

	/// <summary>
	/// Scrolls the view to a specific section of the song list based on the specified letter and sorting criteria.
	/// </summary>
	/// <param name="letter">The starting letter of the section to scroll to, or "#" to scroll to non-alphabetic entries.</param>
	/// <param name="sortBy">The property by which the song list is currently sorted. Valid values include "Title", "Artists", and "Album".</param>
	/// <remarks>
	/// This method locates the first song in the collection that matches the specified starting letter and sorting property.
	/// If a matching song is found, the view scrolls to bring the song into focus.
	/// </remarks>
	private async void ScrollToSection(string letter)
	{
		AlbumModel? targetAlbum = null;

		targetAlbum = letter != "#" ? AlbumsGroup.FirstOrDefault(album => album.Album.StartsWith(letter, StringComparison.OrdinalIgnoreCase)) : AlbumsGroup.FirstOrDefault(album => !char.IsLetter(album.Album[0]));

		if (targetAlbum != null)
		{
			await AlbumTileView.SmoothScrollIntoViewWithItemAsync(targetAlbum, itemPlacement: ScrollItemPlacement.Top, disableAnimation: false, scrollIfVisible: false);
			AlbumTileView.SelectedItem = targetAlbum;
			await Task.Delay(500);
			await AlbumTileView.SmoothScrollIntoViewWithItemAsync(targetAlbum, itemPlacement: ScrollItemPlacement.Top, disableAnimation: false, scrollIfVisible: false);
		}
	}

	/// <summary>
	/// Adjusts the size of the elements in the alphabet navigation panel based on the available vertical space.
	/// </summary>
	/// <remarks>
	/// This method calculates the height for each element in the alphabet navigation panel dynamically, ensuring
	/// that the elements are evenly spaced and fit within the available vertical space.
	/// </remarks>
	/// <returns>
	/// A task that represents the asynchronous operation of resizing the elements in the alphabet navigation panel.
	/// </returns>
	private Task<Task> AdjustAlphabetSize()
	{
		double availableSpace = AlbumTileView.ActualHeight;

		AlphabetNavigationPanel.Margin = new Thickness(0, 10, 30, 10);

		if (availableSpace <= 0) return Task.FromResult(Task.CompletedTask);

		double totalLetters = AlphabetNavigationPanel.Children.Count;

		double autoHeight = availableSpace / totalLetters;

		foreach (var button in AlphabetNavigationPanel.Children.OfType<Button>())
		{
			button.Height = autoHeight;
		}
		return Task.FromResult(Task.CompletedTask);
	}

	/// <summary>
	/// Handles the event triggered when the page's size changes.
	/// </summary>
	/// <param name="sender">The source of the event, typically the page.</param>
	/// <param name="e">The event data containing information about the new size of the page.</param>
	/// <remarks>
	/// This method adjusts the layout or size of elements on the page whenever the size of the page changes.
	/// It enqueues a task on the dispatcher queue to perform required layout updates asynchronously.
	/// </remarks>
	private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		UpdateShimmerPlaceholderCount(e.NewSize);

		var pageHeight = e.NewSize.Height;
		_dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, async () =>
		{
			await AdjustAlphabetSize();
		});
	}

	/// <summary>
	/// Handles the theme change event for the page.
	/// </summary>
	/// <param name="sender">The <see cref="FrameworkElement"/> that triggered the theme change event.</param>
	/// <param name="args">The event data associated with the theme change event.</param>
	/// <remarks>
	/// This method updates the foreground color of visible text elements in the AlphabetNavigationPanel
	/// based on the current theme of the page. If the theme is dark, white color is applied; otherwise, black color is applied.
	/// </remarks>
	private void Page_ActualThemeChanged(FrameworkElement sender, object args)
	{
		Brush themeBrush = (sender.ActualTheme == ElementTheme.Dark) ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black);
		AlphabetNavigationPanel.Children.OfType<Button>().Where(button => button.Opacity == 1).ToList().ForEach(textElement => textElement.Foreground = themeBrush);
	}
}
