using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Navigation;
using Tunetastic.Common;
using Tunetastic.Common.Operations;
using Tunetastic.Common.Services;

namespace Tunetastic.Views.Common;

/// <summary>
/// Common base class for folder view pages in the music library.
/// Provides shared navigation, sorting, layout toggling, playback, playlists, and selection behavior.
/// </summary>
public abstract partial class FolderPageBase : SongListPageBase
{
	protected readonly List<string> GoToMessages = new()
	{
		"No music folders found.\nPlease check your settings to ensure your libraries are added and songs have been scanned."
	};

	/// <summary>
	/// Gets or sets the collection of root folder tiles displayed in the top-level view.
	/// </summary>
	public ObservableCollection<FolderModel> RootFolders { get; set; } = new();

	/// <summary>
	/// Gets the collection of songs displayed inside the active folder.
	/// </summary>
	public ObservableCollection<Song> FolderGroupSongs => ViewModel.Songs;

	/// <summary>
	/// Current folder path being viewed. When null or empty, the page displays root folders.
	/// </summary>
	public string? CurrentFolderPath { get; set; }

	/// <summary>
	/// Navigation history stack for navigating up folder hierarchies.
	/// </summary>
	protected readonly Stack<string?> _navHistory = new();

	/// <summary>
	/// Gets a value indicating whether the page is currently displaying root level folders.
	/// </summary>
	public bool IsAtRoot => string.IsNullOrEmpty(CurrentFolderPath);

	/// <summary>
	/// Gets the mode name representing this page ("All Music Folders" or "Folder Hierarchy").
	/// </summary>
	public abstract string PageModeName { get; }

	/// <inheritdoc/>
	protected override string PlaylistKey => IsAtRoot ? GetType().Name : $"FolderGroup>{CurrentFolderPath}";

	/// <inheritdoc/>
	protected override List<string> EmptyLibraryMessages => GoToMessages;

	/// <inheritdoc/>
	protected override ListView GetCurrentViewStyle() => CurrentViewStyleText() switch
	{
		"List View" => FolderDetailListViewControl,
		"Compact View" => FolderDetailCompactViewControl,
		_ => FolderDetailCompactViewControl
	};

	// ── Common Controls resolved by name without colliding with x:Name in derived classes ───

	protected Button UpButtonControl => (Button)FindName("UpButton");
	protected TextBlock FolderTitleLabel => (TextBlock)FindName("FolderTitleTextBlock");
	protected StackPanel PlayAllPanel => (StackPanel)FindName("PlayAllButtonStackPanel");
	protected StackPanel SortAndViewPanel => (StackPanel)FindName("SortAndViewButtonPanel");
	protected DropDownButton SortDropDownButton => (DropDownButton)FindName("SortDropDown");
	protected MenuFlyout SortFlyout => (MenuFlyout)FindName("Sort");
	protected RadioMenuFlyoutItem TitleSortItem => (RadioMenuFlyoutItem)FindName("TitleSort");
	protected RadioMenuFlyoutItem ArtistsSortItem => (RadioMenuFlyoutItem)FindName("ArtistsSort");
	protected RadioMenuFlyoutItem AlbumSortItem => (RadioMenuFlyoutItem)FindName("AlbumSort");
	protected RadioMenuFlyoutItem DurationSortItem => (RadioMenuFlyoutItem)FindName("DurationSort");
	protected RadioMenuFlyoutItem TrackSortItem => (RadioMenuFlyoutItem)FindName("TrackSort");
	protected MenuFlyoutSeparator SortSeparatorItem => (MenuFlyoutSeparator)FindName("SortSeparator");
	protected RadioMenuFlyoutItem AscendingItem => (RadioMenuFlyoutItem)FindName("Ascending");
	protected RadioMenuFlyoutItem DescendingItem => (RadioMenuFlyoutItem)FindName("Descending");
	protected DropDownButton ViewDropDownButton => (DropDownButton)FindName("ViewButton");
	protected MenuFlyout ViewStyleFlyout => (MenuFlyout)FindName("ViewStyle");
	protected RadioMenuFlyoutItem AllMusicFoldersModeItem => (RadioMenuFlyoutItem)FindName("AllMusicFoldersViewStyle");
	protected RadioMenuFlyoutItem FolderHierarchyModeItem => (RadioMenuFlyoutItem)FindName("FolderHierarchyViewStyle");
	protected MenuFlyoutSeparator ViewSeparatorItem => (MenuFlyoutSeparator)FindName("ViewSeparator");
	protected RadioMenuFlyoutItem ListViewStyleItem => (RadioMenuFlyoutItem)FindName("ListViewStyle");
	protected RadioMenuFlyoutItem CompactViewStyleItem => (RadioMenuFlyoutItem)FindName("CompactViewStyle");
	protected ContentDialog DeleteConfirmationDialog => (ContentDialog)FindName("DeleteDialog");
	protected TextBlock DeleteConfirmationText => (TextBlock)FindName("DeleteDialogText");
	protected ListView FolderTileViewControl => (ListView)FindName("FolderTileView");
	protected Grid FolderContentContainerControl => (Grid)FindName("FolderContentContainer");
	protected ListView FolderDetailListViewControl => (ListView)FindName("FolderDetailListView");
	protected ListView FolderDetailCompactViewControl => (ListView)FindName("FolderDetailCompactView");
	protected CommunityToolkit.WinUI.Controls.DataTable HeaderControl => (CommunityToolkit.WinUI.Controls.DataTable)FindName("Header");

	// Forwarding shims for base event handlers
	protected void PlayAllButton_OnClick(object sender, RoutedEventArgs e) => PlayAll_OnClick(sender, e);
	protected void ShuffleAndPlayButton_OnClick(object sender, RoutedEventArgs e) => ShuffleAndPlay_OnClick(sender, e);
	protected void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e) => SongListView_SelectionChanged(sender, e);
	protected void MenuFlyout_Opened(object sender, object e) => MenuFlyoutOpened(sender, e);

	/// <inheritdoc/>
	protected override void OnInitializingContent()
	{
		AddGoToSettingsMessage();
		FolderTileViewControl.Visibility = Visibility.Collapsed;
		FolderContentContainerControl.Visibility = Visibility.Collapsed;
	}

	/// <inheritdoc/>
	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);
		if (e.Parameter is string folderPath && !string.IsNullOrWhiteSpace(folderPath))
		{
			CurrentFolderPath = folderPath;
		}
	}

	/// <inheritdoc/>
	protected override async Task OnLibraryReadyAsync()
	{
		PageButtonsPanel.Visibility = Visibility.Visible;
		SortDropDownButton.Visibility = Visibility.Visible;
		ViewDropDownButton.Visibility = Visibility.Visible;
		UpdateAsPerLastSettings();
		await RefreshCurrentView();
	}

	/// <summary>
	/// Restores sort, order, mode, and view layout preferences from saved application settings.
	/// </summary>
	protected void UpdateAsPerLastSettings()
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var sortOrder = localSettings.Values[nameof(LocalSave.FoldersViewSortOrder)]?.ToString() ?? "Ascending";
		var detailSortBy = localSettings.Values[nameof(LocalSave.FolderDetailViewSortBy)]?.ToString() ?? "Title";
		var viewStyle = localSettings.Values[nameof(LocalSave.FolderDetailViewStyle)]?.ToString() ?? "Compact View";

		if (PageModeName == "Folder Hierarchy")
			FolderHierarchyModeItem.IsChecked = true;
		else
			AllMusicFoldersModeItem.IsChecked = true;

		if (sortOrder == "Descending")
			DescendingItem.IsChecked = true;
		else
			AscendingItem.IsChecked = true;

		switch (detailSortBy)
		{
			case "Artists":
				ArtistsSortItem.IsChecked = true;
				break;
			case "Album":
				AlbumSortItem.IsChecked = true;
				break;
			case "Duration":
				DurationSortItem.IsChecked = true;
				break;
			case "Track":
				TrackSortItem.IsChecked = true;
				break;
			case "Title":
			default:
				TitleSortItem.IsChecked = true;
				break;
		}

		if (viewStyle == "List View")
			ListViewStyleItem.IsChecked = true;
		else
			CompactViewStyleItem.IsChecked = true;
	}

	protected string GetCurrentSortOrder()
	{
		return SortFlyout.Items.OfType<RadioMenuFlyoutItem>()
			.Where(item => item.GroupName == "Order" && item.IsChecked)
			.Select(item => item.Text)
			.FirstOrDefault() ?? "Ascending";
	}

	protected string GetCurrentSongSortBy()
	{
		return SortFlyout.Items.OfType<RadioMenuFlyoutItem>()
			.Where(item => item.GroupName == "SortBy" && item.IsChecked)
			.Select(item => item.Text)
			.FirstOrDefault() ?? "Title";
	}

	protected string GetCurrentViewStyleChoice()
	{
		return ViewStyleFlyout.Items.OfType<RadioMenuFlyoutItem>()
			.Where(item => item.GroupName == "View" && item.IsChecked)
			.Select(item => item.Text)
			.FirstOrDefault() ?? "Compact View";
	}

	/// <summary>
	/// Refreshes the display depending on whether the page is at root level or drilled into a folder.
	/// </summary>
	public async Task RefreshCurrentView()
	{
		bool ascOrder = GetCurrentSortOrder() == "Ascending";

		if (IsAtRoot)
		{
			FolderTitleLabel.Text = "Folders";
			UpButtonControl.Visibility = Visibility.Collapsed;
			PlayAllPanel.Visibility = Visibility.Collapsed;
			FolderTileViewControl.Visibility = Visibility.Visible;
			FolderContentContainerControl.Visibility = Visibility.Collapsed;

			// Hide song sort options at root
			TitleSortItem.Visibility = Visibility.Collapsed;
			ArtistsSortItem.Visibility = Visibility.Collapsed;
			AlbumSortItem.Visibility = Visibility.Collapsed;
			DurationSortItem.Visibility = Visibility.Collapsed;
			TrackSortItem.Visibility = Visibility.Collapsed;
			SortSeparatorItem.Visibility = Visibility.Collapsed;

			// Hide song layout options at root
			ListViewStyleItem.Visibility = Visibility.Collapsed;
			CompactViewStyleItem.Visibility = Visibility.Collapsed;
			ViewSeparatorItem.Visibility = Visibility.Collapsed;

			// Update Sort button label
			UpdateSortButtonContent("Order", ascOrder);

			// Update View button label
			var viewDropdownContent = new TextBlock();
			viewDropdownContent.Inlines.Add(new Run { Text = PageModeName == "Folder Hierarchy" ? "Hierarchy" : "All Folders" });
			ViewDropDownButton.Content = viewDropdownContent;
			ToolTipService.SetToolTip(ViewDropDownButton, $"Current view mode: {PageModeName}");

			// Load root folders via page-specific implementation
			await LoadRootFoldersAsync(ascOrder);

			if (RootFolders.Count == 0)
			{
				GoToSettingsPrompt.Visibility = Visibility.Visible;
				FolderTileViewControl.Visibility = Visibility.Collapsed;
				PageButtonsPanel.Visibility = Visibility.Collapsed;
			}
			else
			{
				GoToSettingsPrompt.Visibility = Visibility.Collapsed;
				FolderTileViewControl.Visibility = Visibility.Visible;
				PageButtonsPanel.Visibility = Visibility.Visible;
			}
		}
		else
		{
			// Inside folder
			var folderName = System.IO.Path.GetFileName(CurrentFolderPath);
			FolderTitleLabel.Text = string.IsNullOrEmpty(folderName) ? CurrentFolderPath! : folderName;
			UpButtonControl.Visibility = Visibility.Visible;
			PlayAllPanel.Visibility = Visibility.Visible;
			FolderTileViewControl.Visibility = Visibility.Collapsed;
			FolderContentContainerControl.Visibility = Visibility.Visible;

			// Show song sort options
			TitleSortItem.Visibility = Visibility.Visible;
			ArtistsSortItem.Visibility = Visibility.Visible;
			AlbumSortItem.Visibility = Visibility.Visible;
			DurationSortItem.Visibility = Visibility.Visible;
			TrackSortItem.Visibility = Visibility.Visible;
			SortSeparatorItem.Visibility = Visibility.Visible;

			// Show song layout options
			ListViewStyleItem.Visibility = Visibility.Visible;
			CompactViewStyleItem.Visibility = Visibility.Visible;
			ViewSeparatorItem.Visibility = Visibility.Visible;

			var sortBy = GetCurrentSongSortBy();
			SongProperty sortByEnum = Enum.TryParse<SongProperty>(sortBy, out var parsed) ? parsed : SongProperty.Title;

			// Update Sort button label
			UpdateSortButtonContent(sortBy, ascOrder);

			// Update View style layout (List vs Compact)
			ApplyViewStyleLayout();

			// Load folder contents via page-specific implementation
			await LoadFolderContentsAsync(CurrentFolderPath!, sortByEnum, ascOrder);

			// Update alphabet scrubber
			UpdateScrubber(sortBy, ascOrder);
			await AdjustAlphabetSize();
		}
	}

	/// <summary>Loads root folders for the active mode.</summary>
	protected abstract Task LoadRootFoldersAsync(bool ascOrder);

	/// <summary>Loads folder contents (subfolders and songs) for the active folder.</summary>
	protected abstract Task LoadFolderContentsAsync(string folderPath, SongProperty sortBy, bool ascOrder);

	protected void UpdateSortButtonContent(string field, bool ascOrder)
	{
		var sortDropdownContent = new TextBlock();
		sortDropdownContent.Inlines.Add(new Run { Text = IsAtRoot ? "Order: " : "Sort By: " });
		if (!IsAtRoot)
		{
			sortDropdownContent.Inlines.Add(new Run { Text = field, FontWeight = Microsoft.UI.Text.FontWeights.Bold });
		}
		sortDropdownContent.Inlines.Add(new Run { Text = $" {(ascOrder ? "⬆️" : "⬇️")}" });

		var orderDropdownTooltip = new TextBlock();
		orderDropdownTooltip.Inlines.Add(new Run { Text = IsAtRoot ? "Folders are sorted in " : $"The list is sorted by {field} column in " });
		orderDropdownTooltip.Inlines.Add(new Run { Text = ascOrder ? "Ascending" : "Descending", FontWeight = Microsoft.UI.Text.FontWeights.Bold });
		orderDropdownTooltip.Inlines.Add(new Run { Text = " order." });

		SortDropDownButton.Content = sortDropdownContent;
		ToolTipService.SetToolTip(SortDropDownButton, orderDropdownTooltip);
	}

	protected void ApplyViewStyleLayout()
	{
		var viewStyle = GetCurrentViewStyleChoice();
		if (viewStyle == "List View")
		{
			FolderDetailListViewControl.Visibility = Visibility.Visible;
			FolderDetailCompactViewControl.Visibility = Visibility.Collapsed;
			ViewDropDownButton.Content = new FontIcon { Glyph = "\uE8FD" };
		}
		else
		{
			FolderDetailListViewControl.Visibility = Visibility.Collapsed;
			FolderDetailCompactViewControl.Visibility = Visibility.Visible;
			ViewDropDownButton.Content = new FontIcon { Glyph = "\uE71D" };
		}
		ToolTipService.SetToolTip(ViewDropDownButton, viewStyle);
	}

	protected void UpdateScrubber(string sortBy, bool ascOrder)
	{
		IOrderedEnumerable<string>? availableLetters = null;
		bool hasSpecialCharacters = false;

		switch (sortBy)
		{
			case "Artists":
				availableLetters = FolderGroupSongs.Select(song => FirstLetter(song.Artists)).Where(c => c != "#").Distinct().OrderBy(c => c);
				hasSpecialCharacters = FolderGroupSongs.Any(song => FirstLetter(song.Artists) == "#");
				break;
			case "Album":
				availableLetters = FolderGroupSongs.Select(song => FirstLetter(song.Album)).Where(c => c != "#").Distinct().OrderBy(c => c);
				hasSpecialCharacters = FolderGroupSongs.Any(song => FirstLetter(song.Album) == "#");
				break;
			case "Title":
			default:
				availableLetters = FolderGroupSongs.Select(song => FirstLetter(song.Title)).Where(c => c != "#").Distinct().OrderBy(c => c);
				hasSpecialCharacters = FolderGroupSongs.Any(song => FirstLetter(song.Title) == "#");
				break;
		}

		PopulateAlphabetNavigation(availableLetters, ascOrder, sortBy, hasSpecialCharacters);
	}

	/// <inheritdoc/>
	protected override async void ScrollToSection(string letter, string sortBy)
	{
		if (IsAtRoot) return;

		Song? targetSong = null;
		switch (sortBy)
		{
			case "Title":
				targetSong = letter != "#"
					? FolderGroupSongs.FirstOrDefault(song => song.Title.StartsWith(letter, StringComparison.OrdinalIgnoreCase))
					: FolderGroupSongs.FirstOrDefault(song => FirstLetter(song.Title) == "#");
				break;

			case "Artists":
				targetSong = letter != "#"
					? FolderGroupSongs.FirstOrDefault(song => song.Artists.StartsWith(letter, StringComparison.OrdinalIgnoreCase))
					: FolderGroupSongs.FirstOrDefault(song => FirstLetter(song.Artists) == "#");
				break;

			case "Album":
				targetSong = letter != "#"
					? FolderGroupSongs.FirstOrDefault(song => song.Album.StartsWith(letter, StringComparison.OrdinalIgnoreCase))
					: FolderGroupSongs.FirstOrDefault(song => FirstLetter(song.Album) == "#");
				break;
		}

		if (targetSong != null)
		{
			var listView = GetCurrentViewStyle();
			try
			{
				await listView.SmoothScrollIntoViewWithItemAsync(targetSong, itemPlacement: ScrollItemPlacement.Top, disableAnimation: false, scrollIfVisible: false, additionalVerticalOffset: listView == FolderDetailListViewControl ? -40 : 0);
			}
			catch (Exception)
			{
			}
			listView.SelectedItem = targetSong;
		}
	}

	/// <inheritdoc/>
	protected override string LetterForItem(object item)
	{
		if (item is Song song)
		{
			return _lastSortBy switch
			{
				"Artists" => FirstLetter(song.Artists),
				"Album" => FirstLetter(song.Album),
				_ => FirstLetter(song.Title)
			};
		}
		return "#";
	}

	// ── Navigation Handlers ──────────────────────────────────────

	protected async void FolderTileView_ItemClick(object sender, ItemClickEventArgs e)
	{
		if (e.ClickedItem is FolderModel folder)
		{
			_navHistory.Push(CurrentFolderPath);
			CurrentFolderPath = folder.Path;
			Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.SelectedFolder)] = folder.Path;
			await RefreshCurrentView();
		}
	}

	protected async void UpButton_Click(object sender, RoutedEventArgs e)
	{
		if (_navHistory.Count > 0)
		{
			CurrentFolderPath = _navHistory.Pop();
		}
		else
		{
			CurrentFolderPath = null;
		}
		await RefreshCurrentView();
	}

	// ── Mode, Layout & Sort Button Handlers ─────────────────────────

	protected abstract void ModeButton_OnClick(object sender, RoutedEventArgs e);

	protected async void LayoutButton_OnClick(object sender, RoutedEventArgs e)
	{
		var viewStyle = GetCurrentViewStyleChoice();
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.FolderDetailViewStyle)] = viewStyle;
		ApplyViewStyleLayout();
		await AdjustAlphabetSize();
	}

	protected async void SortButton_OnClick(object sender, RoutedEventArgs e)
	{
		var orderBy = GetCurrentSortOrder();
		if (IsAtRoot)
		{
			Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.FoldersViewSortOrder)] = orderBy;
		}
		else
		{
			var sortBy = GetCurrentSongSortBy();
			Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.FolderDetailViewSortBy)] = sortBy;
			Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.FolderDetailViewSortOrder)] = orderBy;
		}
		await RefreshCurrentView();
	}

	// ── Song Click & Playback Handlers ───────────────────────────

	protected void ListView_ItemClick(object sender, ItemClickEventArgs e)
	{
		if (e.ClickedItem is Song track)
		{
			var songPaths = FolderGroupSongs.Select(s => s.Path).ToList();
			MusicPlayer.Instance.LoadPlaylist(songPaths, track.Path);
			Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CurrentPlayinglist)] = PlaylistKey;
		}
	}

	protected async void FolderTilePlay_OnClick(object sender, RoutedEventArgs e)
	{
		if ((sender as MenuFlyoutItem)?.DataContext is FolderModel folder)
		{
			bool isHierarchy = PageModeName == "Folder Hierarchy";
			var songs = await FolderOperations.GetSongsInFolderAsync(folder.Path, includeSubfolders: isHierarchy);
			if (songs.Count > 0)
			{
				var songPaths = songs.Select(s => s.Path).ToList();
				MusicPlayer.Instance.LoadPlaylist(songPaths, songPaths[0]);
				Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CurrentPlayinglist)] = $"FolderGroup>{folder.Path}";
			}
		}
	}

	protected async void FolderTileAddToQueue_OnClick(object sender, RoutedEventArgs e)
	{
		if ((sender as MenuFlyoutItem)?.DataContext is FolderModel folder)
		{
			bool isHierarchy = PageModeName == "Folder Hierarchy";
			var songs = await FolderOperations.GetSongsInFolderAsync(folder.Path, includeSubfolders: isHierarchy);
			if (songs.Count > 0)
			{
				await DatabaseHelper.Instance.AddSongsToQueuedPlayingList(songs.Select(s => s.Path).ToList());
				GlobalNotification.Info($"Added {songs.Count} songs from '{folder.Name}' to queue.");
			}
		}
	}

	protected void MenuFlyoutItemPlay_OnClick(object sender, RoutedEventArgs e)
	{
		var songData = (sender as MenuFlyoutItem)?.DataContext as Song;
		var songPaths = FolderGroupSongs.Select(s => s.Path).ToList();
		MusicPlayer.Instance.LoadPlaylist(songPaths, songData?.Path);
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CurrentPlayinglist)] = PlaylistKey;
	}

	protected async void MenuFlyoutItemAddToQueue_OnClick(object sender, RoutedEventArgs e)
	{
		var songData = (sender as MenuFlyoutItem)?.DataContext as Song;
		if (songData != null)
		{
			await DatabaseHelper.Instance.AddSongsToQueuedPlayingList(new List<string> { songData.Path });
			GlobalNotification.Info($"{songData.Title} added to queue.");
		}
	}

	protected async void MenuFlyoutItemInfoTag_OnClick(object sender, RoutedEventArgs e)
	{
		var songData = (sender as MenuFlyoutItem)?.DataContext as Song;
		if (songData != null)
		{
			MainPage._instance?.ShowSongInfo(await DatabaseHelper.Instance.GetSongByPath(songData.Path));
		}
	}

	protected async void MenuFlyoutItemDelete_OnClick(object sender, RoutedEventArgs e)
	{
		var songData = (sender as MenuFlyoutItem)?.DataContext as Song;
		if (songData != null)
		{
			DeleteConfirmationText.Text = $"Are you sure you want to delete this song/track from your system?" +
			                              $"\nTitle: {songData.Title}" +
			                              $"\nArtist: {songData.Artists}" +
			                              $"\nAlbum: {songData.Album}" +
			                              $"\nFile: {songData.Path}";

			DeleteConfirmationDialog.Visibility = Visibility.Visible;
			MainWindow._instance.WindowResizePermission(false);
			var result = await DeleteConfirmationDialog.ShowAsync();
			MainWindow._instance.WindowResizePermission(true);
			DeleteConfirmationDialog.Visibility = Visibility.Collapsed;

			if (result == ContentDialogResult.Primary)
			{
				if (System.IO.File.Exists(songData.Path))
				{
					LibraryWatcherService.MarkSelfInitiated(songData.Path);
					System.IO.File.Delete(songData.Path);
				}
				await DatabaseHelper.Instance.DeleteSongFromDB(songData.Path);
				FolderGroupSongs.Remove(songData);
				MusicPlayer.Instance.HandleAfterDelete();
				GlobalNotification.Success($"Song deleted: {songData.Title}");
			}
		}
	}

	protected async void MenuFlyoutMultiItemAddToQueue_OnClick(object sender, RoutedEventArgs e)
	{
		var listView = GetCurrentViewStyle();
		var songs = listView.SelectedItems.OfType<Song>().ToList();
		var paths = songs.Select(s => s.Path).ToList();
		if (paths.Count > 0)
		{
			await DatabaseHelper.Instance.AddSongsToQueuedPlayingList(paths);
			GlobalNotification.Info($"{paths.Count} songs added to queue.");
		}
	}

	protected async void MenuFlyoutMultiItemDelete_OnClick(object sender, RoutedEventArgs e)
	{
		var listView = GetCurrentViewStyle();
		var songs = listView.SelectedItems.OfType<Song>().ToList();
		if (songs.Count > 0)
		{
			DeleteConfirmationText.Text = $"Are you sure you want to delete {songs.Count} songs/tracks from your system?";
			DeleteConfirmationDialog.Visibility = Visibility.Visible;
			MainWindow._instance.WindowResizePermission(false);
			var result = await DeleteConfirmationDialog.ShowAsync();
			MainWindow._instance.WindowResizePermission(true);
			DeleteConfirmationDialog.Visibility = Visibility.Collapsed;

			if (result == ContentDialogResult.Primary)
			{
				foreach (var song in songs)
				{
					if (System.IO.File.Exists(song.Path))
					{
						LibraryWatcherService.MarkSelfInitiated(song.Path);
						System.IO.File.Delete(song.Path);
					}
					await DatabaseHelper.Instance.DeleteSongFromDB(song.Path);
					FolderGroupSongs.Remove(song);
				}
				MusicPlayer.Instance.HandleAfterDelete();
				GlobalNotification.Success($"{songs.Count} songs deleted.");
			}
		}
	}

	// ── Multi-select Toggle ──────────────────────────────────────

	protected void MultiSelectButton_Click(object sender, RoutedEventArgs e)
	{
		var view = GetCurrentViewStyle();
		if (MultiSelectToggle.IsChecked == true)
		{
			MoreButtonControl.Visibility = Visibility.Visible;
			MoreButtonControl.IsEnabled = false;
			PlayAllPanel.Visibility = Visibility.Collapsed;
			SortAndViewPanel.Visibility = Visibility.Collapsed;
			ToolTipService.SetToolTip(MultiSelectToggle, "Turn off multi-select mode");
			view.SelectionMode = ListViewSelectionMode.Multiple;
			view.IsItemClickEnabled = false;
			view.IsMultiSelectCheckBoxEnabled = true;
			view.IsRightTapEnabled = false;

			var itemGrids = DevWinUI.DependencyObjectExtensions.FindDescendants(view);
			foreach (var item in itemGrids)
			{
				if (item is UIElement uiElement)
				{
					uiElement.IsRightTapEnabled = false;
				}
			}
			if (view == FolderDetailListViewControl) HeaderControl.Margin = new Thickness(40, 0, 0, 0);
		}
		else
		{
			MoreButtonControl.Visibility = Visibility.Collapsed;
			PlayAllPanel.Visibility = IsAtRoot ? Visibility.Collapsed : Visibility.Visible;
			SortAndViewPanel.Visibility = Visibility.Visible;
			ToolTipService.SetToolTip(MultiSelectToggle, "Turn on multi-select mode");
			view.SelectionMode = ListViewSelectionMode.Single;
			view.IsItemClickEnabled = true;
			view.IsMultiSelectCheckBoxEnabled = false;
			view.IsRightTapEnabled = true;

			var itemGrids = DevWinUI.DependencyObjectExtensions.FindDescendants(view);
			foreach (var item in itemGrids)
			{
				if (item is UIElement uiElement)
				{
					uiElement.IsRightTapEnabled = true;
				}
			}
			if (view == FolderDetailListViewControl) HeaderControl.Margin = new Thickness(12, 0, 0, 0);
		}
	}

	// ── Playlists ────────────────────────────────────────────────

	/// <inheritdoc/>
	protected override string BuildAddToPlaylistTooltip(bool multiSelect, string playList) =>
		$"Add {(multiSelect ? "selected songs/tracks" : "this song/track")} to {playList} playlist";

	/// <inheritdoc/>
	protected override async void NewPlaylist_Click(object sender, RoutedEventArgs e)
	{
		var result = await MainPage._instance!.ShowAddPlaylistDialog(songsToAddAfterCreate: true);
		if (result.Result != AddPlaylistResult.Cancelled)
		{
			string playlistName = result.Name!;
			var listView = GetCurrentViewStyle();
			if (listView.IsMultiSelectCheckBoxEnabled)
			{
				var songs = listView.SelectedItems.OfType<Song>().ToList();
				await DatabaseHelper.Instance.AddSongsToPlaylist(playlistName, songs.Select(s => s.Path).ToList());
				GlobalNotification.Info($"{songs.Count} songs added to {playlistName} playlist.");
			}
			else
			{
				var song = (sender as MenuFlyoutItem)?.DataContext as Song;
				if (song != null)
				{
					await DatabaseHelper.Instance.AddSongToPlaylist(playlistName, song.Path);
					GlobalNotification.Info($"{song.Title} added to {playlistName} playlist.");
				}
			}
		}
	}

	/// <inheritdoc/>
	protected override async void AddToPlaylist_Click(object sender, RoutedEventArgs e)
	{
		var playlist = (sender as MenuFlyoutItem)?.Text;
		if (playlist == null) return;

		var listView = GetCurrentViewStyle();
		if (listView.IsMultiSelectCheckBoxEnabled)
		{
			var songs = listView.SelectedItems.OfType<Song>().ToList();
			await DatabaseHelper.Instance.AddSongsToPlaylist(playlist, songs.Select(s => s.Path).ToList());
			GlobalNotification.Info($"{songs.Count} songs added to {playlist} playlist.");
		}
		else
		{
			var song = (sender as MenuFlyoutItem)?.DataContext as Song;
			if (song != null)
			{
				await DatabaseHelper.Instance.AddSongToPlaylist(playlist, song.Path);
				GlobalNotification.Info($"{song.Title} added to {playlist} playlist.");
			}
		}
	}

	// ── Page Lifecycle & Settings Events ─────────────────────────

	protected void GotoSettigsButton_Click(object sender, RoutedEventArgs e)
	{
		App.Current.NavService.NavigateTo(typeof(SettingsPage));
	}

	protected void Page_Loaded(object sender, RoutedEventArgs e)
	{
		_ = AdjustAlphabetSize();
	}

	protected void Page_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		_dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, async () => await AdjustAlphabetSize());
	}
}
