using System.Collections.ObjectModel;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace Tunetastic.Views.LibraryViews;

/// <summary>
/// Represents the view page for displaying and managing genres in the library.
/// </summary>
/// <remarks>
/// This class is primarily used as a UI page for viewing a collection of genres
/// and interacting with genre-related content such as navigation and selection.
/// </remarks>
/// <remarks>
/// The page utilizes <see cref="ObservableCollection{T}"/> to manage genre data.
/// It also implements custom navigation handlers and animations.
/// </remarks>
/// <remarks>
/// This page is mapped to the navigation dictionary in the application through
/// its fully qualified type name.
/// </remarks>
public sealed partial class GenresViewPage : Page
{
	/// <summary>
	/// Gets or sets the collection of <c>GenreModel</c> objects, representing groups of songs organized by Genre.
	/// </summary>
	/// <remarks>
	/// This property is used in the <c>GenresViewPage</c> to manage the grouped data displayed in the library view.
	/// It leverages an <c>ObservableCollection</c> to enable dynamic data updates in the UI when the collection changes.
	/// The grouping and sorting of the Genres are updated based on user interaction or application logic.
	/// The data within this property is cleared and refreshed as necessary during page lifecycle events.
	/// </remarks>
	public ObservableCollection<GenreModel> GenresGroup
	{
		get; set;
	} = new();

	private readonly DispatcherQueue _dispatcherQueue;

	public GenresViewPage()
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
		GenreTileView.Visibility = Visibility.Collapsed;
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
				this.Content = new GenresViewPage();
			});
			return;
		}

		if (await DatabaseHelper.Instance.GetSongsCount() > 0)
		{
			GoToSettings.Visibility = Visibility.Collapsed;
			SortDropDown.Visibility = Visibility.Visible;
			PageButtons.Visibility = Visibility.Visible;
			GenreTileView.Visibility = Visibility.Visible;
			UpdateAsPerLastSorting();
		}
	}

	/// <summary>
	/// Updates the sorting preferences for the song list displayed on the GenresViewPage.
	/// </summary>
	/// <remarks>
	/// This method determines the sorting criteria and order (e.g., by title, artist, album, duration.)
	/// based on the user's saved preferences in local settings. It also updates the selection status
	/// of the UI elements corresponding to the sorting options and triggers the list update.
	/// </remarks>
	private void UpdateAsPerLastSorting()
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var sortOrder = localSettings.Values[nameof(LocalSave.GenresViewSortOrder)]?.ToString() ?? "Ascending";

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
		_ = UpdateListBasedOnSorting();
	}

	/// <summary>
	/// Updates the list of songs based on the selected sorting criteria and order.
	/// </summary>
	/// <remarks>
	/// This method processes the current sorting preferences, such as the column to sort by (e.g., Title, Artists, Album, or Duration)
	/// and the order (Ascending or Descending). It modifies the displayed song list accordingly and ensures the current selection remains intact.
	/// Additional functionality includes updating the user interface with the sorting details and storing the preferences
	/// in local application settings for persistence. The alphabet navigation is also refreshed with relevant data based on the sorting criteria.
	/// </remarks>
	private async Task UpdateListBasedOnSorting()
	{
		var genreModel = GenreTileView.SelectedItem as GenreModel;
		var orderBy = Sort.Items.OfType<RadioMenuFlyoutItem>().Where(item => item.GroupName == "Order" && item.IsChecked).Select(item => item.Text).FirstOrDefault() ?? "Ascending";
		bool AscOrder = orderBy == "Ascending";

		var groups = await DatabaseHelper.Instance.GetSongsGroupedByGenre(AscOrder);
		GenresGroup.Clear();
		GenresGroup.AddRange(groups);

		groups = null;

		var sortDropdownContent = new TextBlock();
		sortDropdownContent.Inlines.Add(new Run { Text = "Order: " });
		sortDropdownContent.Inlines.Add(new Run { Text = $" {(AscOrder ? "⬆️" : "⬇️")}" });

		var orderDropdownTooltip = new TextBlock();
		orderDropdownTooltip.Inlines.Add(new Run { Text = "The tiles are sorted in " });
		orderDropdownTooltip.Inlines.Add(new Run { Text = orderBy, FontWeight = Microsoft.UI.Text.FontWeights.Bold });
		orderDropdownTooltip.Inlines.Add(new Run { Text = " order." });

		SortDropDown.Content = sortDropdownContent;
		ToolTipService.SetToolTip(SortDropDown, orderDropdownTooltip);
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.GenresViewSortOrder)] = orderBy;

		genreModel = GenresGroup.Select(s => s).Where(s => s.Genre == genreModel?.Genre).FirstOrDefault();
		await Task.Delay(500);
		await ScrollToTile(genreModel);
	}

	/// <summary>
	/// Handles the Loaded event for the GenreTileList control, ensuring proper initialization of the associated panel.
	/// </summary>
	/// <remarks>
	/// This method adjusts the behavior of the SmartWrapPanel used as the items panel for the GenreTileView ListView control.
	/// It sets the panel's <c>ListViewWidthSource</c> property to the <c>GenreTileView</c> instance
	/// and invalidates its layout, allowing the panel to adapt to the current dimensions and layout requirements.
	/// </remarks>
	/// <param name="sender">The source of the event, typically the <c>GenreTileView</c> object.</param>
	/// <param name="e">Event data that provides additional information about the Loaded event.</param>
	private void GenreTileList_Loaded(object sender, RoutedEventArgs e)
	{
		if (GenreTileView.ItemsPanelRoot is SmartWrapPanel panel)
		{
			panel.ListViewWidthSource = GenreTileView;
			panel.InvalidateMeasure();
		}
	}

	/// <summary>
	/// Handles the `Loaded` event for the GenresViewPage.
	/// </summary>
	/// <param name="sender">The source of the event, generally the page itself.</param>
	/// <param name="e">The event data associated with the `Loaded` event.</param>
	/// <remarks>
	/// This method initializes content and handles animations when the page is loaded. It ensures that the `GenresGroup` collection is populated before proceeding. If a connected animation is active, it retrieves the selected genre and attempts to animate the transition back to the associated UI element. The method also manages navigation states and scrolls to the current playing track if applicable.
	/// </remarks>
	private async void Page_Loaded(object sender, RoutedEventArgs e)
	{
		while (GenresGroup == null || GenresGroup.Count == 0)
		{
			await Task.Delay(100);
		}

		if (connectedAnimation)
		{
			var selectedGenre = (Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.SelectedGenre)]?.ToString());
			var selectedGenreModel = GenresGroup.Select(s => s).Where(s => s.Genre == selectedGenre).FirstOrDefault();

			var animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("GenreHeaderAnimationBack");

			if (animation != null && selectedGenreModel != null)
			{
				await Task.Delay(30);
				await GenreTileView.SmoothScrollIntoViewWithItemAsync(selectedGenreModel, itemPlacement: ScrollItemPlacement.Top, disableAnimation: false, scrollIfVisible: false);

				var container = GenreTileView.ContainerFromItem(selectedGenreModel) as ListViewItem;
				if (container != null)
				{
					var genreTextBlock = DevWinUI.DependencyObjectEx.FindDescendant(container, "GenreTextBlock");
					if (genreTextBlock != null)
						animation.TryStart(genreTextBlock);
				}
			}
			connectedAnimation = false;
			var currentPlaylist = Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CurrentPlayinglist)]?.ToString() ?? string.Empty;
			if (currentPlaylist.StartsWith("GenreGroup>"))
			{
				var SelectedTile = GenresGroup.Select(s => s).Where(s =>
				{
					var v = currentPlaylist.Substring("GenreGroup>".Length);
					return s.Genre == (v == "Unknown Genre" ? "Unknown" : v);
				}).FirstOrDefault();
				GenreTileView.SelectedItem = SelectedTile;
			}
		}
		else
			ScrollToCurrentPlayingTrack();
		await Task.Delay(100);
	}

	/// <summary>
	/// Scrolls the view to the currently playing track if the current playlist corresponds to the "GenresViewPage".
	/// </summary>
	/// <remarks>
	/// This method checks the local application settings to determine if the "GenresViewPage" is the active playlist.
	/// If it is, the method retrieves the last played track based on its path from the saved settings and attempts to scroll
	/// the page to that specific song within the song collection.
	/// </remarks>
	private void ScrollToCurrentPlayingTrack()
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var currentPlaylist = localSettings.Values[nameof(LocalSave.CurrentPlayinglist)]?.ToString() ?? string.Empty;
		if (currentPlaylist.StartsWith("GenreGroup>"))
		{
			var SelectedTile = GenresGroup.Select(s => s).Where(s =>
			{
				var v = currentPlaylist.Substring("GenreGroup>".Length);
				return s.Genre == (v == "Unknown Genre" ? "Unknown" : v);
			}).FirstOrDefault();
			_ = ScrollToTile(SelectedTile);
		}
	}

	/// <summary>
	/// Scrolls to a specific song in the View.
	/// </summary>
	/// <param name="song">The song object to scroll to. If null, no action is performed.</param>
	/// <returns>A task representing the asynchronous operation of scrolling to the specified song.</returns>
	private async Task ScrollToTile(GenreModel? tile)
	{
		if (tile != null)
		{
			GenreTileView.SelectedItem = tile;
			await GenreTileView.SmoothScrollIntoViewWithItemAsync(tile, itemPlacement: ScrollItemPlacement.Center, disableAnimation: false, scrollIfVisible: false);
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
		if (GenreTileView.IsMultiSelectCheckBoxEnabled)
		{
			MoreButton.IsEnabled = GenreTileView.SelectedItems.Count > 0;
		}
	}

	/// <summary>
	/// Handles the Unloaded event for the GenresViewPage.
	/// </summary>
	/// <remarks>
	/// This method is triggered when the page is unloaded. It performs cleanup operations such as clearing
	/// the song collection, releasing memory resources, and initiating garbage collection.
	/// </remarks>
	/// <param name="sender">The source of the event, typically the page being unloaded.</param>
	/// <param name="e">The event arguments associated with the Unloaded event.</param>
	private void Page_Unloaded(object sender, RoutedEventArgs e)
	{
		GenresGroup.Clear();
		GenresGroup = null;
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
	/// If multiple genres are selected, it fetches all songs corresponding to those genres and adds them to the chosen playlist.
	/// In single selection mode, the process targets the respective group of genre.
	/// Updates are performed through interactions with the database using asynchronous operations.
	/// A notification is displayed upon successfully adding the songs to the playlist.
	/// </remarks>
	private async void AddToPlaylist_Click(object sender, RoutedEventArgs e)
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

		if (GenreTileView.IsMultiSelectCheckBoxEnabled)
		{
			var genreModels = GenreTileView.SelectedItems;
			var songList = await DatabaseHelper.Instance.LoadSongsFromDB(orderBy: Enum.Parse<SongProperty>(localSettings.Values[nameof(LocalSave.GenreDetailViewSortBy)]?.ToString() ?? "Title"),
																		 ascending: (localSettings.Values[nameof(LocalSave.GenreDetailViewSortOrder)]?.ToString() ?? "Ascending") == "Ascending",
																		 whereCondition: $"{SongProperty.Genre.ToString()} IN ({string.Join(",", genreModels.Select(y => $"'{((y as GenreModel)?.Genre == "Unknown" ? "Unknown Genre" : (y as GenreModel)?.Genre)}'"))})");

			var playlist = (sender as MenuFlyoutItem)?.Text;

			if (playlist != null)
				await DatabaseHelper.Instance.AddSongsToPlaylist(playlist, songList.Select(s => s.Path).ToList());

			GlobalNotification.Info($"All {songList.Count} songs/tracks of selected genres, added to {playlist} playlist.");
		}
		else
		{
			var genreModel = (sender as MenuFlyoutItem)?.DataContext as GenreModel;
			var playlist = (sender as MenuFlyoutItem)?.Text;

			if (playlist != null && genreModel != null)
			{
				var songList = await DatabaseHelper.Instance.LoadSongsFromDB(orderBy: Enum.Parse<SongProperty>(localSettings.Values[nameof(LocalSave.GenreDetailViewSortBy)]?.ToString() ?? "Title"),
																			 ascending: (localSettings.Values[nameof(LocalSave.GenreDetailViewSortOrder)]?.ToString() ?? "Ascending") == "Ascending",
																			 whereCondition: $"{SongProperty.Genre.ToString()} = '{(genreModel.Genre == "Unknown" ? "Unknown Genre" : genreModel.Genre)}'");
				await DatabaseHelper.Instance.AddSongsToPlaylist(playlist, songList.Select(s => s.Path).ToList());
				GlobalNotification.Info($"All {songList.Count} {(songList.Count == 1 ? "song/track" : "songs/tracks")} of Genre {genreModel.Genre} added to {playlist} playlist.");
			}
		}
	}

	/// <summary>
	/// Handles the click event for the "Play" menu flyout item in the genre group view.
	/// </summary>
	/// <param name="sender">The source of the event, typically a menu flyout item representing a genre group.</param>
	/// <param name="e">Provides event data for the click event.</param>
	/// <remarks>
	/// This method processes the selected genre group's data to retrieve all associated songs from the database. It builds a playlist containing the songs
	/// and initiates playback through the application's music player, starting with the first song. It also updates local settings to maintain
	/// the current playback context and highlights the selected genre group in the user interface.
	/// </remarks>
	private async void MenuFlyoutItemPlay_OnClick(object sender, RoutedEventArgs e)
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var genreModel = (sender as MenuFlyoutItem)?.DataContext as GenreModel;
		var songList = await DatabaseHelper.Instance.LoadSongsFromDB(orderBy: Enum.Parse<SongProperty>(localSettings.Values[nameof(LocalSave.GenreDetailViewSortBy)]?.ToString() ?? "Title"),
			ascending: (localSettings.Values[nameof(LocalSave.GenreDetailViewSortOrder)]?.ToString() ?? "Ascending") == "Ascending",
			whereCondition: $"{SongProperty.Genre.ToString()} = '{(genreModel?.Genre == "Unknown" ? "Unknown Genre" : genreModel?.Genre)}'");
		List<string> songPaths = songList.Select(s => s.Path).ToList();
		MusicPlayer.Instance.LoadPlaylist(songPaths, songPaths[0]);
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CurrentPlayinglist)] = $"GenreGroup>{genreModel?.Genre}";
		GenreTileView.SelectedItem = genreModel;
	}

	/// <summary>
	/// Handles the click event for the "Add to queue" menu flyout item.
	/// </summary>
	/// <remarks>
	/// This method retrieves song data associated with a specific genre using the menu item's data context. It queries the database
	/// for songs matching the selected genre, sorts them based on user preferences, and adds their file paths to the queued playing list.
	/// A notification is displayed upon successfully adding songs to the queue.
	/// </remarks>
	/// <param name="sender">The source of the event, typically the MenuFlyoutItem that was clicked.</param>
	/// <param name="e">The event data associated with the routed event.</param>
	private async void MenuFlyoutItemAddToQueue_OnClick(object sender, RoutedEventArgs e)
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var genreModel = (sender as MenuFlyoutItem)?.DataContext as GenreModel;
		var songList = await DatabaseHelper.Instance.LoadSongsFromDB(orderBy: Enum.Parse<SongProperty>(localSettings.Values[nameof(LocalSave.GenreDetailViewSortBy)]?.ToString() ?? "Title"),
																	 ascending: (localSettings.Values[nameof(LocalSave.GenreDetailViewSortOrder)]?.ToString() ?? "Ascending") == "Ascending",
																	 whereCondition: $"{SongProperty.Genre.ToString()} = '{(genreModel?.Genre == "Unknown" ? "Unknown Genre" : genreModel?.Genre)}'");
		List<string> songPaths = songList.Select(s => s.Path).ToList();

		await DatabaseHelper.Instance.AddSongsToQueuedPlayingList(songPaths);

		GlobalNotification.Info($"All {songList.Count} {(songList.Count == 1 ? "song/track" : "songs/tracks")} of Genre {genreModel?.Genre} added to queue.");
	}

	/// <summary>
	/// Handles the delete action when a menu flyout item is clicked, prompting the user with a confirmation dialog and deleting songs corresponding to a specific genre if confirmed.
	/// </summary>
	/// <param name="sender">The event sender, usually a MenuFlyoutItem representing the delete option.</param>
	/// <param name="e">The event data associated with the click event.</param>
	/// <remarks>
	/// This method retrieves songs matching the specified genre value from the database, generates a confirmation prompt displaying
	/// the number of songs to be deleted, and upon user confirmation:
	/// 1. Deletes the files associated with the songs from the local storage.
	/// 2. Removes the corresponding records from the database.
	/// 3. Updates the UI to reflect the changes, including any adjustments to song counts or visibility of genre entries.
	/// If no songs are present that match the specified genre, no action is taken.
	/// </remarks>
	private async void MenuFlyoutItemDelete_OnClick(object sender, RoutedEventArgs e)
	{
		DeleteDialog.Visibility = Visibility.Visible;
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var genreModel = (sender as MenuFlyoutItem)?.DataContext as GenreModel;
		var songList = await DatabaseHelper.Instance.LoadSongsFromDB(orderBy: Enum.Parse<SongProperty>(localSettings.Values[nameof(LocalSave.GenreDetailViewSortBy)]?.ToString() ?? "Title"),
																	 ascending: (localSettings.Values[nameof(LocalSave.GenreDetailViewSortOrder)]?.ToString() ?? "Ascending") == "Ascending",
																	 whereCondition: $"{SongProperty.Genre.ToString()} = '{(genreModel?.Genre == "Unknown" ? "Unknown Genre" : genreModel?.Genre)}'");
		List<string> songPaths = songList.Select(s => s.Path).ToList();

		if (songPaths?.Count > 0)
		{
			DeleteDialogText.Text = $"Are you sure you want to delete {(songPaths.Count > 1 ? "these songs/tracks" : "this song/track")} of Genre {genreModel?.Genre} from your system?";

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
				GenresGroup.Remove(genreModel);
				MusicPlayer.Instance.HandleAfterDelete();
				GlobalNotification.Info($"All {songPaths.Count} {(songPaths.Count > 1 ? "songs/tracks" : "song/track")} of Genre {genreModel?.Genre} deleted.");
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
		if (MultiSelectButton.IsChecked == true)
		{
			MoreButton.Visibility = Visibility.Visible;
			MoreButton.IsEnabled = false;
			SortAndViewButtonPanel.Visibility = Visibility.Collapsed;
			ToolTipService.SetToolTip(MultiSelectButton, "Turn off multi-select mode");
			GenreTileView.SelectionMode = ListViewSelectionMode.Multiple;
			GenreTileView.IsItemClickEnabled = false;
			GenreTileView.IsMultiSelectCheckBoxEnabled = true;
			GenreTileView.IsRightTapEnabled = false;
			var ItemGrids = DevWinUI.DependencyObjectEx.FindDescendants(GenreTileView);

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
			GenreTileView.SelectionMode = ListViewSelectionMode.Single;
			GenreTileView.IsItemClickEnabled = true;
			GenreTileView.IsMultiSelectCheckBoxEnabled = false;
			GenreTileView.IsRightTapEnabled = true;
			var ItemGrids = DevWinUI.DependencyObjectEx.FindDescendants(GenreTileView);

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
	/// Handles the click event for adding multiple selected genres' songs to the playing queue from the menu flyout.
	/// </summary>
	/// <param name="sender">The source of the event, typically the menu flyout item that was clicked.</param>
	/// <param name="e">The routed event data that contains event-specific information.</param>
	/// <remarks>
	/// This method retrieves the genres selected by the user from the UI, uses the <see cref="DatabaseHelper"/> instance to fetch all associated songs from the database,
	/// and asynchronously adds their file paths to the playing queue. A notification is displayed upon successfully adding the songs to the queue.
	/// The number of songs added, and the selected genres are also considered in the feedback notification.
	/// </remarks>
	private async void MenuFlyoutMultiItemAddToQueue_OnClick(object sender, RoutedEventArgs e)
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var genreModels = GenreTileView.SelectedItems;
		var songList = await DatabaseHelper.Instance.LoadSongsFromDB(orderBy: Enum.Parse<SongProperty>(localSettings.Values[nameof(LocalSave.GenreDetailViewSortBy)]?.ToString() ?? "Title"),
																	 ascending: (localSettings.Values[nameof(LocalSave.GenreDetailViewSortOrder)]?.ToString() ?? "Ascending") == "Ascending",
																	 whereCondition: $"{SongProperty.Genre.ToString()} IN ({string.Join(",", genreModels.Select(y => $"'{((y as GenreModel)?.Genre == "Unknown" ? "Unknown Genre" : (y as GenreModel)?.Genre)}'"))})");
		List<string> songPaths = songList.Select(s => s.Path).ToList();

		await DatabaseHelper.Instance.AddSongsToQueuedPlayingList(songPaths);

		GlobalNotification.Info($"All {songList.Count} songs/tracks of selected genres, added to queue.");
	}

	/// <summary>
	/// Handles the click event for the "Delete" menu flyout item, facilitating the deletion of selected song groups from the system.
	/// </summary>
	/// <remarks>
	/// This method retrieves the songs associated with the selected genres, prompts the user for confirmation via a dialog,
	/// and deletes the songs from both the application's database and the file system if the user confirms.
	/// Additionally, it updates the UI components by removing the selected genre groups and displaying appropriate notifications
	/// or controls based on the results of the deletion process.
	/// </remarks>
	/// <param name="sender">The source object of the event, typically the "Delete" menu flyout item.</param>
	/// <param name="e">Event data providing context about the "Delete" menu flyout item click action.</param>
	private async void MenuFlyoutMultiItemDelete_OnClick(object sender, RoutedEventArgs e)
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var genreModels = GenreTileView.SelectedItems;
		var songList = await DatabaseHelper.Instance.LoadSongsFromDB(orderBy: Enum.Parse<SongProperty>(localSettings.Values[nameof(LocalSave.GenreDetailViewSortBy)]?.ToString() ?? "Title"),
			ascending: (localSettings.Values[nameof(LocalSave.GenreDetailViewSortOrder)]?.ToString() ?? "Ascending") == "Ascending",
			whereCondition: $"{SongProperty.Genre.ToString()} IN ({string.Join(",", genreModels.Select(y => $"'{((y as GenreModel)?.Genre == "Unknown" ? "Unknown Genre" : (y as GenreModel)?.Genre)}'"))})");
		List<string> songPaths = songList.Select(s => s.Path).ToList();

		DeleteDialog.Visibility = Visibility.Visible;
		DeleteDialogText.Text = $"Are you sure you want to delete {(songPaths.Count > 1 ? "these songs/tracks" : "this song/track")} of selected genres from your system?";

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

			List<GenreModel> genreList = new();
			foreach (var item in genreModels)
				genreList.Add((GenreModel)item);

			foreach (var genreModel in genreList)
				GenreTileView.Items.Remove(genreModel);

			MusicPlayer.Instance.HandleAfterDelete();
			GlobalNotification.Info($"All {songPaths.Count} {(songPaths.Count > 1 ? "songs/tracks" : "song/track")} of selected genres deleted.");
		}
		if (await DatabaseHelper.Instance.GetSongsCount() <= 0)
		{
			GoToSettings.Visibility = Visibility.Visible;
			PageButtons.Visibility = Visibility.Collapsed;
		}
	}

	/// <summary>
	/// Handles the item click event for the GenreTileView and navigates to the detail page corresponding to the selected genre.
	/// </summary>
	/// <param name="sender">The object that raised the event, typically the ListView control.</param>
	/// <param name="e">Provides data about the clicked item, including the <see cref="GenreModel"/> representing the selected genre.</param>
	/// <remarks>
	/// This method processes the clicked item by extracting its details, performing additional validations, and navigating to the <c>GenreDetailPage</c>.
	/// It also stores the selected genre in application settings for retrieval in subsequent operations.
	/// </remarks>
	private void GenreTileView_ItemClick(object sender, ItemClickEventArgs e)
	{
		var genreModel = e.ClickedItem as GenreModel;

		if (genreModel != null)
		{
			var container = GenreTileView.ContainerFromItem(genreModel) as ListViewItem;
			if (container != null)
			{
				var genreTextBlock = DevWinUI.DependencyObjectEx.FindDescendant(container, "GenreTextBlock");
				if (genreTextBlock != null)
					ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("GenreHeaderAnimation", genreTextBlock);
			}

			Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.SelectedGenre)] = genreModel.Genre;
			App.Current.NavService.NavigateTo(typeof(GenreDetailPage), genreModel.Genre, false, new DrillInNavigationTransitionInfo());
		}
	}

	/// <summary>
	/// A boolean flag indicating the state of the connected animation during navigation within the <c>GenresViewPage</c>.
	/// </summary>
	/// <remarks>
	/// This variable is used to determine whether to invoke the connected animation sequence when navigating
	/// to or from the <c>GenresViewPage</c>. It is set based on the navigation mode or parameters passed during
	/// navigation, ensuring a smooth transition effect between pages. When true, connected animations
	/// are enabled to provide a seamless visual experience for the user, such as animating focused genre tiles.
	/// The value is modified dynamically during the page lifecycle to manage transitions and UI updates properly.
	/// </remarks>
	private bool connectedAnimation = false;
	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);

		connectedAnimation = (e.NavigationMode == NavigationMode.Back) || (e.Parameter is string && e.Parameter == "Genres");
	}

	/// <summary>
	/// Handles navigation away from the page and prepares connected animations for the transition.
	/// </summary>
	/// <param name="e">An instance of <see cref="NavigatingCancelEventArgs"/> that contains the event data related to the navigation operation.</param>
	/// <remarks>
	/// This method is invoked when the application navigates away from the current page. It retrieves the currently selected genre from local settings
	/// and identifies the corresponding <see cref="GenreModel"/> instance from the <see cref="GenresGroup"/> collection. If a matching genre is found,
	/// it attempts to locate the associated UI element in the visual tree of the page. If successful, it prepares a connected animation for a smooth
	/// transition to the target page.
	/// </remarks>
	protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
	{
		var selectedGenre = (Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.SelectedGenre)]?.ToString());
		var selectedGenreModel = GenresGroup.Select(s => s).Where(s => s.Genre == selectedGenre).FirstOrDefault();

		var container = GenreTileView.ContainerFromItem(selectedGenreModel) as ListViewItem;
		if (container != null)
		{
			var genreTextBlock = DevWinUI.DependencyObjectEx.FindDescendant(container, "GenreTextBlock");
			if (genreTextBlock != null)
				ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("GenreHeaderAnimation", genreTextBlock);
		}
	}

}
