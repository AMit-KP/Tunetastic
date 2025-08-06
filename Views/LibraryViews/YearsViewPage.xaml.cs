using System.Collections.ObjectModel;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace Tunetastic.Views.LibraryViews;

/// <summary>
/// Represents a page in the application that displays a collection of years grouped as part of the library view.
/// </summary>
/// <remarks>
/// This class manages the UI interactions and data presentation for a library view showing media items
/// organized by year. It leverages binding to display an ObservableCollection of <c>YearModel</c> objects.
/// </remarks>
public sealed partial class YearsViewPage : Page
{
	/// <summary>
	/// Gets or sets the collection of <c>YearModel</c> objects, representing groups of songs organized by year.
	/// </summary>
	/// <remarks>
	/// This property is used in the <c>YearsViewPage</c> to manage the grouped data displayed in the library view.
	/// It leverages an <c>ObservableCollection</c> to enable dynamic data updates in the UI when the collection changes.
	/// The grouping and sorting of the years are updated based on user interaction or application logic.
	/// The data within this property is cleared and refreshed as necessary during page lifecycle events.
	/// </remarks>
	public ObservableCollection<YearModel> YearsGroup
	{
		get; set;
	} = new();

	private readonly DispatcherQueue _dispatcherQueue;

	public YearsViewPage()
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
		YearTileView.Visibility = Visibility.Collapsed;
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
				this.Content = new YearsViewPage();
			});
			return;
		}

		if (await DatabaseHelper.Instance.GetSongsCount() > 0)
		{
			GoToSettings.Visibility = Visibility.Collapsed;
			SortDropDown.Visibility = Visibility.Visible;
			PageButtons.Visibility = Visibility.Visible;
			YearTileView.Visibility = Visibility.Visible;
			UpdateAsPerLastSorting();
		}
	}

	/// <summary>
	/// Updates the sorting preferences for the song list displayed on the YearsViewPage.
	/// </summary>
	/// <remarks>
	/// This method determines the sorting criteria and order
	/// based on the user's saved preferences in local settings. It also updates the selection status
	/// of the UI elements corresponding to the sorting options and triggers the list update.
	/// </remarks>
	private void UpdateAsPerLastSorting()
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var sortOrder = localSettings.Values[nameof(LocalSave.YearsViewSortOrder)]?.ToString() ?? "Ascending";

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
	/// Updates the displayed song list and user interface according to the selected sorting criteria and order.
	/// </summary>
	/// <remarks>
	/// This method retrieves the current sorting preferences, such as ascending or descending order, and applies them to organize the songs grouped by year.
	/// The updated list is displayed in the UI, with related elements, such as tooltips and dropdown content, being refreshed to reflect the new sorting order.
	/// Additionally, the user's selection is preserved, and the UI scroll position is adjusted to maintain the focus on the previously selected year.
	/// Sorting preferences are saved to local application settings to ensure they persist across sessions.
	/// </remarks>
	/// <returns>
	/// A task representing the asynchronous operation of updating the song list, refreshing the user interface, and adjusting the scroll position.
	/// </returns>
	private async Task UpdateListBasedOnSorting()
	{
		var yearModel = YearTileView.SelectedItem as YearModel;
		var orderBy = Sort.Items.OfType<RadioMenuFlyoutItem>().Where(item => item.GroupName == "Order" && item.IsChecked).Select(item => item.Text).FirstOrDefault() ?? "Ascending";
		bool AscOrder = orderBy == "Ascending";

		var groups = await DatabaseHelper.Instance.GetSongsGroupedByYear(AscOrder);
		YearsGroup.Clear();
		YearsGroup.AddRange(groups);

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
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.YearsViewSortOrder)] = orderBy;

		yearModel = YearsGroup.Select(s => s).Where(s => s.Year == yearModel?.Year).FirstOrDefault();
		await Task.Delay(500);
		await ScrollToTile(yearModel);
	}


	/// <summary>
	/// Handles the `Loaded` event for the YearsViewPage.
	/// </summary>
	/// <param name="sender">The source of the event, generally the page itself.</param>
	/// <param name="e">The event data associated with the `Loaded` event.</param>
	/// <remarks>
	/// This method initializes content and handles animations when the page is loaded. It ensures that the `YearsGroup` collection is populated before proceeding. If a connected animation is active, it retrieves the selected year and attempts to animate the transition back to the associated UI element. The method also manages navigation states and scrolls to the current playing track if applicable.
	/// </remarks>
	private async void Page_Loaded(object sender, RoutedEventArgs e)
	{
		while (YearsGroup == null || YearsGroup.Count == 0)
		{
			await Task.Delay(100);
		}

		YearTileView_SizeChanged(null, null);
		YearTileView.ContainerContentChanging += YearTileView_ContainerContentChanging;

		if (connectedAnimation)
		{
			var selectedYear = (Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.SelectedYear)]?.ToString());
			var selectedYearModel = YearsGroup.Select(s => s).Where(s => s.Year == selectedYear).FirstOrDefault();

			var animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("YearHeaderAnimationBack");

			if (animation != null && selectedYearModel != null)
			{
				await Task.Delay(30);
				await YearTileView.SmoothScrollIntoViewWithItemAsync(selectedYearModel, itemPlacement: ScrollItemPlacement.Top, disableAnimation: true, scrollIfVisible: false);

				var container = YearTileView.ContainerFromItem(selectedYearModel) as ListViewItem;
				if (container != null)
				{
					var yearTextBlock = DevWinUI.DependencyObjectEx.FindDescendant(container, "YearTextBlock");
					if (yearTextBlock != null)
						animation.TryStart(yearTextBlock);
				}
			}
			connectedAnimation = false;
			var currentPlaylist = Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CurrentPlayinglist)]?.ToString() ?? string.Empty;
			if (currentPlaylist.StartsWith("YearGroup>"))
			{
				var SelectedTile = YearsGroup.Select(s => s).Where(s =>
				{
					var v = currentPlaylist.Substring("YearGroup>".Length);
					return s.Year == (v == "Unknown Year" ? "Unknown" : v);
				}).FirstOrDefault();
				YearTileView.SelectedItem = SelectedTile;
			}
		}
		else
			ScrollToCurrentPlayingTrack();
		await Task.Delay(100);
		YearTileView.SizeChanged += YearTileView_SizeChanged;

	}

	/// <summary>
	/// Scrolls the view to the currently playing track if the current playlist corresponds to the "YearsViewPage".
	/// </summary>
	/// <remarks>
	/// This method checks the local application settings to determine if the "YearsViewPage" is the active playlist.
	/// If it is, the method retrieves the last played track based on its path from the saved settings and attempts to scroll
	/// the page to that specific song within the song collection.
	/// </remarks>
	private void ScrollToCurrentPlayingTrack()
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var currentPlaylist = localSettings.Values[nameof(LocalSave.CurrentPlayinglist)]?.ToString() ?? string.Empty;
		if (currentPlaylist.StartsWith("YearGroup>"))
		{
			var SelectedTile = YearsGroup.Select(s => s).Where(s =>
			{
				var v = currentPlaylist.Substring("YearGroup>".Length);
				return s.Year == (v == "Unknown Year" ? "Unknown" : v);
			}).FirstOrDefault();
			_ = ScrollToTile(SelectedTile);
		}
	}

	/// <summary>
	/// Scrolls to a specific song in the View.
	/// </summary>
	/// <param name="song">The song object to scroll to. If null, no action is performed.</param>
	/// <returns>A task representing the asynchronous operation of scrolling to the specified song.</returns>
	private async Task ScrollToTile(YearModel? tile)
	{
		if (tile != null)
		{
			YearTileView.SelectedItem = tile;
			await YearTileView.SmoothScrollIntoViewWithItemAsync(tile, itemPlacement: ScrollItemPlacement.Center, disableAnimation: false, scrollIfVisible: false);
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
		if (YearTileView.IsMultiSelectCheckBoxEnabled)
		{
			MoreButton.IsEnabled = YearTileView.SelectedItems.Count > 0;
		}
	}

	/// <summary>
	/// Handles the Unloaded event for the YearsViewPage.
	/// </summary>
	/// <remarks>
	/// This method is triggered when the page is unloaded. It performs cleanup operations such as clearing
	/// the song collection, releasing memory resources, and initiating garbage collection.
	/// </remarks>
	/// <param name="sender">The source of the event, typically the page being unloaded.</param>
	/// <param name="e">The event arguments associated with the Unloaded event.</param>
	private void Page_Unloaded(object sender, RoutedEventArgs e)
	{
		YearsGroup.Clear();
		YearsGroup = null;
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
	/// If multiple years are selected, it fetches all songs corresponding to those years and adds them to the chosen playlist.
	/// In single selection mode, the process targets the respective group of year.
	/// Updates are performed through interactions with the database using asynchronous operations.
	/// A notification is displayed upon successfully adding the songs to the playlist.
	/// </remarks>
	private async void AddToPlaylist_Click(object sender, RoutedEventArgs e)
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

		if (YearTileView.IsMultiSelectCheckBoxEnabled)
		{
			var yearModels = YearTileView.SelectedItems;
			var songList = await DatabaseHelper.Instance.LoadSongsFromDB(orderBy: Enum.Parse<SongProperty>(localSettings.Values[nameof(LocalSave.YearDetailViewSortBy)]?.ToString() ?? "Title"),
																		 ascending: (localSettings.Values[nameof(LocalSave.YearDetailViewSortOrder)]?.ToString() ?? "Ascending") == "Ascending",
																		 whereCondition: $"{SongProperty.Year.ToString()} IN ({string.Join(",", yearModels.Select(y => $"'{((y as YearModel)?.Year == "Unknown" ? "Unknown Year" : (y as YearModel)?.Year)}'"))})");

			var playlist = (sender as MenuFlyoutItem)?.Text;

			if (playlist != null)
				await DatabaseHelper.Instance.AddSongsToPlaylist(playlist, songList.Select(s => s.Path).ToList());

			GlobalNotification.Info($"All {songList.Count} {(songList.Count > 1 ? "songs/tracks" : "song/track")} of selected years, added to {playlist} playlist.");
		}
		else
		{
			var yearModel = (sender as MenuFlyoutItem)?.DataContext as YearModel;
			var playlist = (sender as MenuFlyoutItem)?.Text;

			if (playlist != null && yearModel != null)
			{
				var songList = await DatabaseHelper.Instance.LoadSongsFromDB(orderBy: Enum.Parse<SongProperty>(localSettings.Values[nameof(LocalSave.YearDetailViewSortBy)]?.ToString() ?? "Title"),
																			 ascending: (localSettings.Values[nameof(LocalSave.YearDetailViewSortOrder)]?.ToString() ?? "Ascending") == "Ascending",
																			 whereCondition: $"{SongProperty.Year.ToString()} = '{(yearModel.Year == "Unknown" ? "Unknown Year" : yearModel.Year)}'");
				await DatabaseHelper.Instance.AddSongsToPlaylist(playlist, songList.Select(s => s.Path).ToList());
				GlobalNotification.Info($"All {songList.Count} {(songList.Count == 1 ? "song/track" : "songs/tracks")} of Year {yearModel.Year} added to {playlist} playlist.");
			}
		}
	}

	/// <summary>
	/// Handles the click event for the "Play" menu flyout item in the year group view.
	/// </summary>
	/// <param name="sender">The source of the event, typically a menu flyout item representing a year group.</param>
	/// <param name="e">Provides event data for the click event.</param>
	/// <remarks>
	/// This method processes the selected year group's data to retrieve all associated songs from the database. It builds a playlist containing the songs
	/// and initiates playback through the application's music player, starting with the first song. It also updates local settings to maintain
	/// the current playback context and highlights the selected year group in the user interface.
	/// </remarks>
	private async void MenuFlyoutItemPlay_OnClick(object sender, RoutedEventArgs e)
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var yearModel = (sender as MenuFlyoutItem)?.DataContext as YearModel;
		var songList = await DatabaseHelper.Instance.LoadSongsFromDB(orderBy: Enum.Parse<SongProperty>(localSettings.Values[nameof(LocalSave.YearDetailViewSortBy)]?.ToString() ?? "Title"),
			ascending: (localSettings.Values[nameof(LocalSave.YearDetailViewSortOrder)]?.ToString() ?? "Ascending") == "Ascending",
			whereCondition: $"{SongProperty.Year.ToString()} = '{(yearModel?.Year == "Unknown" ? "Unknown Year" : yearModel?.Year)}'");
		List<string> songPaths = songList.Select(s => s.Path).ToList();
		MusicPlayer.Instance.LoadPlaylist(songPaths, songPaths[0]);
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CurrentPlayinglist)] = $"YearGroup>{yearModel?.Year}";
		YearTileView.SelectedItem = yearModel;
	}

	/// <summary>
	/// Handles the click event for the "Add to queue" menu flyout item.
	/// </summary>
	/// <remarks>
	/// This method retrieves song data associated with a specific year using the menu item's data context. It queries the database
	/// for songs matching the selected year, sorts them based on user preferences, and adds their file paths to the queued playing list.
	/// A notification is displayed upon successfully adding songs to the queue.
	/// </remarks>
	/// <param name="sender">The source of the event, typically the MenuFlyoutItem that was clicked.</param>
	/// <param name="e">The event data associated with the routed event.</param>
	private async void MenuFlyoutItemAddToQueue_OnClick(object sender, RoutedEventArgs e)
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var yearModel = (sender as MenuFlyoutItem)?.DataContext as YearModel;
		var songList = await DatabaseHelper.Instance.LoadSongsFromDB(orderBy: Enum.Parse<SongProperty>(localSettings.Values[nameof(LocalSave.YearDetailViewSortBy)]?.ToString() ?? "Title"),
																	 ascending: (localSettings.Values[nameof(LocalSave.YearDetailViewSortOrder)]?.ToString() ?? "Ascending") == "Ascending",
																	 whereCondition: $"{SongProperty.Year.ToString()} = '{(yearModel?.Year == "Unknown" ? "Unknown Year" : yearModel?.Year)}'");
		List<string> songPaths = songList.Select(s => s.Path).ToList();

		await DatabaseHelper.Instance.AddSongsToQueuedPlayingList(songPaths);

		GlobalNotification.Info($"All {songList.Count} {(songList.Count == 1 ? "song/track" : "songs/tracks")} of Year {yearModel?.Year} added to queue.");
	}

	/// <summary>
	/// Handles the delete action when a menu flyout item is clicked, prompting the user with a confirmation dialog and deleting songs corresponding to a specific year if confirmed.
	/// </summary>
	/// <param name="sender">The event sender, usually a MenuFlyoutItem representing the delete option.</param>
	/// <param name="e">The event data associated with the click event.</param>
	/// <remarks>
	/// This method retrieves songs matching the specified year value from the database, generates a confirmation prompt displaying
	/// the number of songs to be deleted, and upon user confirmation:
	/// 1. Deletes the files associated with the songs from the local storage.
	/// 2. Removes the corresponding records from the database.
	/// 3. Updates the UI to reflect the changes, including any adjustments to song counts or visibility of year entries.
	/// If no songs are present that match the specified year, no action is taken.
	/// </remarks>
	private async void MenuFlyoutItemDelete_OnClick(object sender, RoutedEventArgs e)
	{
		DeleteDialog.Visibility = Visibility.Visible;
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var yearModel = (sender as MenuFlyoutItem)?.DataContext as YearModel;
		var songList = await DatabaseHelper.Instance.LoadSongsFromDB(orderBy: Enum.Parse<SongProperty>(localSettings.Values[nameof(LocalSave.YearDetailViewSortBy)]?.ToString() ?? "Title"),
																	 ascending: (localSettings.Values[nameof(LocalSave.YearDetailViewSortOrder)]?.ToString() ?? "Ascending") == "Ascending",
																	 whereCondition: $"{SongProperty.Year.ToString()} = '{(yearModel?.Year == "Unknown" ? "Unknown Year" : yearModel?.Year)}'");
		List<string> songPaths = songList.Select(s => s.Path).ToList();

		if (songPaths?.Count > 0)
		{
			DeleteDialogText.Text = $"Are you sure you want to delete {(songPaths.Count > 1 ? "these songs/tracks" : "this song/track")} of Year {yearModel?.Year} from your system?";

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
				YearsGroup.Remove(yearModel);
				MusicPlayer.Instance.HandleAfterDelete();
				GlobalNotification.Info($"All {songPaths.Count} {(songPaths.Count > 1 ? "songs/tracks" : "song/track")} of Year {yearModel?.Year} deleted.");
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
			YearTileView.SelectionMode = ListViewSelectionMode.Multiple;
			YearTileView.IsItemClickEnabled = false;
			YearTileView.IsMultiSelectCheckBoxEnabled = true;
			YearTileView.IsRightTapEnabled = false;
			var ItemGrids = DevWinUI.DependencyObjectEx.FindDescendants(YearTileView);

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
			YearTileView.SelectionMode = ListViewSelectionMode.Single;
			YearTileView.IsItemClickEnabled = true;
			YearTileView.IsMultiSelectCheckBoxEnabled = false;
			YearTileView.IsRightTapEnabled = true;
			var ItemGrids = DevWinUI.DependencyObjectEx.FindDescendants(YearTileView);

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
	/// Handles the click event for adding multiple selected years' songs to the playing queue from the menu flyout.
	/// </summary>
	/// <param name="sender">The source of the event, typically the menu flyout item that was clicked.</param>
	/// <param name="e">The routed event data that contains event-specific information.</param>
	/// <remarks>
	/// This method retrieves the years selected by the user from the UI, uses the <see cref="DatabaseHelper"/> instance to fetch all associated songs from the database,
	/// and asynchronously adds their file paths to the playing queue. A notification is displayed upon successfully adding the songs to the queue.
	/// The number of songs added, and the selected years are also considered in the feedback notification.
	/// </remarks>
	private async void MenuFlyoutMultiItemAddToQueue_OnClick(object sender, RoutedEventArgs e)
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var yearModels = YearTileView.SelectedItems;
		var songList = await DatabaseHelper.Instance.LoadSongsFromDB(orderBy: Enum.Parse<SongProperty>(localSettings.Values[nameof(LocalSave.YearDetailViewSortBy)]?.ToString() ?? "Title"),
																	 ascending: (localSettings.Values[nameof(LocalSave.YearDetailViewSortOrder)]?.ToString() ?? "Ascending") == "Ascending",
																	 whereCondition: $"{SongProperty.Year.ToString()} IN ({string.Join(",", yearModels.Select(y => $"'{((y as YearModel)?.Year == "Unknown" ? "Unknown Year" : (y as YearModel)?.Year)}'"))})");
		List<string> songPaths = songList.Select(s => s.Path).ToList();

		await DatabaseHelper.Instance.AddSongsToQueuedPlayingList(songPaths);

		GlobalNotification.Info($"All {songList.Count} {(songPaths.Count > 1 ? "songs/tracks" : "song/track")} of selected years, added to queue.");
	}

	/// <summary>
	/// Handles the click event for the "Delete" menu flyout item, facilitating the deletion of selected song groups from the system.
	/// </summary>
	/// <remarks>
	/// This method retrieves the songs associated with the selected years, prompts the user for confirmation via a dialog,
	/// and deletes the songs from both the application's database and the file system if the user confirms.
	/// Additionally, it updates the UI components by removing the selected year groups and displaying appropriate notifications
	/// or controls based on the results of the deletion process.
	/// </remarks>
	/// <param name="sender">The source object of the event, typically the "Delete" menu flyout item.</param>
	/// <param name="e">Event data providing context about the "Delete" menu flyout item click action.</param>
	private async void MenuFlyoutMultiItemDelete_OnClick(object sender, RoutedEventArgs e)
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var yearModels = YearTileView.SelectedItems;
		var songList = await DatabaseHelper.Instance.LoadSongsFromDB(orderBy: Enum.Parse<SongProperty>(localSettings.Values[nameof(LocalSave.YearDetailViewSortBy)]?.ToString() ?? "Title"),
			ascending: (localSettings.Values[nameof(LocalSave.YearDetailViewSortOrder)]?.ToString() ?? "Ascending") == "Ascending",
			whereCondition: $"{SongProperty.Year.ToString()} IN ({string.Join(",", yearModels.Select(y => $"'{((y as YearModel)?.Year == "Unknown" ? "Unknown Year" : (y as YearModel)?.Year)}'"))})");
		List<string> songPaths = songList.Select(s => s.Path).ToList();

		DeleteDialog.Visibility = Visibility.Visible;
		DeleteDialogText.Text = $"Are you sure you want to delete {(songPaths.Count > 1 ? "these songs/tracks" : "this song/track")} of selected years from your system?";

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

			List<YearModel> yearList = new();
			foreach (var item in yearModels)
				yearList.Add((YearModel)item);

			foreach (var yearModel in yearList)
				YearTileView.Items.Remove(yearModel);

			MusicPlayer.Instance.HandleAfterDelete();
			GlobalNotification.Info($"All {songPaths.Count} {(songPaths.Count > 1 ? "songs/tracks" : "song/track")} of selected years deleted.");
		}
		if (await DatabaseHelper.Instance.GetSongsCount() <= 0)
		{
			GoToSettings.Visibility = Visibility.Visible;
			PageButtons.Visibility = Visibility.Collapsed;
		}
	}

	/// <summary>
	/// Handles the item click event for the YearTileView and navigates to the detail page corresponding to the selected year.
	/// </summary>
	/// <param name="sender">The object that raised the event, typically the ListView control.</param>
	/// <param name="e">Provides data about the clicked item, including the <see cref="YearModel"/> representing the selected year.</param>
	/// <remarks>
	/// This method processes the clicked item by extracting its details, performing additional validations, and navigating to the <c>YearDetailPage</c>.
	/// It also stores the selected year in application settings for retrieval in subsequent operations.
	/// </remarks>
	private void YearTileView_ItemClick(object sender, ItemClickEventArgs e)
	{
		var yearModel = e.ClickedItem as YearModel;

		if (yearModel != null)
		{
			var container = YearTileView.ContainerFromItem(yearModel) as ListViewItem;
			if (container != null)
			{
				var yearTextBlock = DevWinUI.DependencyObjectEx.FindDescendant(container, "YearTextBlock");
				if (yearTextBlock != null)
				{
					ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("YearHeaderAnimation", yearTextBlock);

					var fadeOut = new DoubleAnimation
					{
						To = 0,
						Duration = TimeSpan.FromMilliseconds(30),
						FillBehavior = FillBehavior.Stop
					};
					Storyboard.SetTarget(fadeOut, yearTextBlock);
					Storyboard.SetTargetProperty(fadeOut, "Opacity");

					var sb = new Storyboard();
					sb.Children.Add(fadeOut);
					sb.Completed += (_, __) => yearTextBlock.Visibility = Visibility.Collapsed;
					sb.Begin();
				}
			}

			Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.SelectedYear)] = yearModel.Year;
			App.Current.NavService.NavigateTo(typeof(YearDetailPage), yearModel.Year, false, new DrillInNavigationTransitionInfo());
		}
	}

	/// <summary>
	/// A boolean flag indicating the state of the connected animation during navigation within the <c>YearsViewPage</c>.
	/// </summary>
	/// <remarks>
	/// This variable is used to determine whether to invoke the connected animation sequence when navigating
	/// to or from the <c>YearsViewPage</c>. It is set based on the navigation mode or parameters passed during
	/// navigation, ensuring a smooth transition effect between pages. When true, connected animations
	/// are enabled to provide a seamless visual experience for the user, such as animating focused year tiles.
	/// The value is modified dynamically during the page lifecycle to manage transitions and UI updates properly.
	/// </remarks>
	private bool connectedAnimation = false;
	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);

		connectedAnimation = (e.NavigationMode == NavigationMode.Back) || (e.Parameter is string && e.Parameter == "Years");
	}

	/// <summary>
	/// Handles navigation away from the page and prepares connected animations for the transition.
	/// </summary>
	/// <param name="e">An instance of <see cref="NavigatingCancelEventArgs"/> that contains the event data related to the navigation operation.</param>
	/// <remarks>
	/// This method is invoked when the application navigates away from the current page. It retrieves the currently selected year from local settings
	/// and identifies the corresponding <see cref="YearModel"/> instance from the <see cref="YearsGroup"/> collection. If a matching year is found,
	/// it attempts to locate the associated UI element in the visual tree of the page. If successful, it prepares a connected animation for a smooth
	/// transition to the target page.
	/// </remarks>
	protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
	{
		if (e.SourcePageType.Name == "YearDetailPage")
		{
			var selectedYear = (Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.SelectedYear)]?.ToString());
			var selectedYearModel = YearsGroup.Select(s => s).Where(s => s.Year == selectedYear).FirstOrDefault();

			var container = YearTileView.ContainerFromItem(selectedYearModel) as ListViewItem;
			if (container != null)
			{
				var yearTextBlock = DevWinUI.DependencyObjectEx.FindDescendant(container, "YearTextBlock");
				if (yearTextBlock != null)
				{
					ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("YearHeaderAnimation", yearTextBlock);

					var fadeOut = new DoubleAnimation
					{
						To = 0,
						Duration = TimeSpan.FromMilliseconds(30),
						FillBehavior = FillBehavior.Stop
					};
					Storyboard.SetTarget(fadeOut, yearTextBlock);
					Storyboard.SetTargetProperty(fadeOut, "Opacity");

					var sb = new Storyboard();
					sb.Children.Add(fadeOut);
					sb.Completed += (_, __) => yearTextBlock.Visibility = Visibility.Collapsed;
					sb.Begin();
				}
			}
		}
	}

	/// <summary>
	/// Adjusts the layout and styling of items within the YearTileView control dynamically based on the available width.
	/// </summary>
	/// <param name="sender">The source of the event, usually the YearTileView control.</param>
	/// <param name="e">Event data that provides information about the size changes.</param>
	/// <remarks>
	/// This method calculates the optimal dimensions for items in the YearTileView control,
	/// ensuring that items are spaced appropriately and the layout adapts to different screen sizes.
	/// The method determines the maximum number of items that can fit horizontally and adjusts item width and margins.
	/// For wrapping layouts where all items cannot fit, the method redistributes the available space among visible items.
	/// </remarks>
	private void YearTileView_SizeChanged(object? sender, SizeChangedEventArgs? e)
	{
		const double baseContentWidth = 220;
		const double baseMargin = 10;

		double availableWidth = YearTileView.ActualWidth;
		if (availableWidth <= 0 || YearTileView.ItemsPanelRoot is not ItemsWrapGrid grid)
			return;

		int itemCount = YearTileView.Items.Count;
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
			var container = YearTileView.ContainerFromItem(YearTileView.Items[i]) as ListViewItem;
			if (container != null)
			{
				container.Margin = new Thickness(adjustedMargin, 15, adjustedMargin, 15);
			}
		}

		grid.ItemWidth = adjustedItemWidth;
	}

	/// <summary>
	/// Occurs during the progressive content rendering of an year tile within the year list view, dynamically adapting the UI for items that are becoming visible.
	/// </summary>
	/// <param name="sender">
	/// The ListViewBase control that triggered the event.
	/// </param>
	/// <param name="args">
	/// Provides data for the container content changing event, indicating the specific item and its container that are being updated or visualized.
	/// </param>
	/// <remarks>
	/// This method is invoked whenever a container's UI content is about to change for a specific year tile in the <c>YearTileView</c>.
	/// It ensures that size adjustments or UI modifications are handled depending on the content visibility. The method contributes
	/// to maintaining responsive performance by managing how elements are dynamically rendered as the user navigates or scrolls through the year view.
	/// </remarks>
	private void YearTileView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
	{
		YearTileView_SizeChanged(null, null);
	}
}
