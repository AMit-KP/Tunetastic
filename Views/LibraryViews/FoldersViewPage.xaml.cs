using System.Collections.ObjectModel;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Navigation;
using Tunetastic.Common;
using Tunetastic.Common.Operations;
using Tunetastic.Common.Services;
using Tunetastic.Views.Common;

namespace Tunetastic.Views.LibraryViews;

/// <summary>
/// Displays music folders from libraries with support for two modes:
/// 1. "All Music Folders" (Mode 1): Flat list of all folders directly containing music files.
/// 2. "Folder Hierarchy" (Mode 2): Hierarchical view starting from top-level music folders,
///    with explorer-style in-folder browsing showing subfolders first, then songs second.
/// </summary>
public sealed partial class FoldersViewPage : SongListPageBase
{
	private readonly List<string> GoToMessages = new()
	{
		"No music folders found.\nPlease check your settings to ensure your libraries are added and songs have been scanned."
	};

	/// <summary>
	/// Gets or sets the collection of root folder tiles displayed in the top-level view.
	/// </summary>
	public ObservableCollection<FolderModel> RootFolders { get; set; } = new();

	/// <summary>
	/// Gets or sets the collection of subfolder tiles displayed when drilled inside a folder in hierarchy mode.
	/// </summary>
	public ObservableCollection<FolderModel> SubFolders { get; set; } = new();

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
	private readonly Stack<string?> _navHistory = new();

	/// <summary>
	/// Gets a value indicating whether the page is currently displaying root level folders.
	/// </summary>
	public bool IsAtRoot => string.IsNullOrEmpty(CurrentFolderPath);

	/// <inheritdoc/>
	protected override string PlaylistKey => IsAtRoot ? "FoldersViewPage" : $"FolderGroup>{CurrentFolderPath}";

	/// <inheritdoc/>
	protected override Button ShuffleAndPlayControl => ShuffleAndPlay;

	/// <inheritdoc/>
	protected override Button MoreButtonControl => MoreButton;

	/// <inheritdoc/>
	protected override List<string> EmptyLibraryMessages => GoToMessages;

	/// <inheritdoc/>
	protected override Page CreateFreshPage() => new FoldersViewPage();

	/// <inheritdoc/>
	protected override ListView GetCurrentViewStyle() => CurrentViewStyleText() switch
	{
		"List View" => FolderDetailListView,
		"Compact View" => FolderDetailCompactView,
		_ => FolderDetailCompactView
	};

	// Forwarding shims for base event handlers
	private void PlayAllButton_OnClick(object sender, RoutedEventArgs e) => PlayAll_OnClick(sender, e);

	private void ShuffleAndPlayButton_OnClick(object sender, RoutedEventArgs e) => ShuffleAndPlay_OnClick(sender, e);

	private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e) => SongListView_SelectionChanged(sender, e);

	private void MenuFlyout_Opened(object sender, object e) => MenuFlyoutOpened(sender, e);

	public FoldersViewPage()
	{
		this.InitializeComponent();
		_ = RunScanningAwareInit();
	}

	/// <inheritdoc/>
	protected override void OnInitializingContent()
	{
		AddGoToSettingsMessage();
		FolderTileView.Visibility = Visibility.Collapsed;
		FolderContentContainer.Visibility = Visibility.Collapsed;
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
		PageButtons.Visibility = Visibility.Visible;
		SortDropDown.Visibility = Visibility.Visible;
		ViewButton.Visibility = Visibility.Visible;
		UpdateAsPerLastSettings();
		await RefreshCurrentView();
	}

	/// <summary>
	/// Restores sort, order, mode, and view layout preferences from saved application settings.
	/// </summary>
	private void UpdateAsPerLastSettings()
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var sortOrder = localSettings.Values[nameof(LocalSave.FoldersViewSortOrder)]?.ToString() ?? "Ascending";
		var viewMode = localSettings.Values[nameof(LocalSave.FoldersViewMode)]?.ToString() ?? "All Music Folders";
		var detailSortBy = localSettings.Values[nameof(LocalSave.FolderDetailViewSortBy)]?.ToString() ?? "Title";
		var detailSortOrder = localSettings.Values[nameof(LocalSave.FolderDetailViewSortOrder)]?.ToString() ?? "Ascending";
		var viewStyle = localSettings.Values[nameof(LocalSave.FolderDetailViewStyle)]?.ToString() ?? "Compact View";

		if (viewMode == "Folder Hierarchy")
			FolderHierarchyViewStyle.IsChecked = true;
		else
			AllMusicFoldersViewStyle.IsChecked = true;

		if (sortOrder == "Descending")
			Descending.IsChecked = true;
		else
			Ascending.IsChecked = true;

		switch (detailSortBy)
		{
			case "Artists":
				ArtistsSort.IsChecked = true;
				break;
			case "Album":
				AlbumSort.IsChecked = true;
				break;
			case "Duration":
				DurationSort.IsChecked = true;
				break;
			case "Track":
				TrackSort.IsChecked = true;
				break;
			case "Title":
			default:
				TitleSort.IsChecked = true;
				break;
		}

		if (viewStyle == "List View")
			ListViewStyle.IsChecked = true;
		else
			CompactViewStyle.IsChecked = true;
	}

	private string GetCurrentViewMode()
	{
		return ViewStyle.Items.OfType<RadioMenuFlyoutItem>()
			.Where(item => item.GroupName == "FolderMode" && item.IsChecked)
			.Select(item => item.Text)
			.FirstOrDefault() ?? "All Music Folders";
	}

	private string GetCurrentSortOrder()
	{
		return Sort.Items.OfType<RadioMenuFlyoutItem>()
			.Where(item => item.GroupName == "Order" && item.IsChecked)
			.Select(item => item.Text)
			.FirstOrDefault() ?? "Ascending";
	}

	private string GetCurrentSongSortBy()
	{
		return Sort.Items.OfType<RadioMenuFlyoutItem>()
			.Where(item => item.GroupName == "SortBy" && item.IsChecked)
			.Select(item => item.Text)
			.FirstOrDefault() ?? "Title";
	}

	private string GetCurrentViewStyleChoice()
	{
		return ViewStyle.Items.OfType<RadioMenuFlyoutItem>()
			.Where(item => item.GroupName == "View" && item.IsChecked)
			.Select(item => item.Text)
			.FirstOrDefault() ?? "Compact View";
	}

	/// <summary>
	/// Refreshes the display depending on whether the page is at root level or drilled into a folder.
	/// </summary>
	private async Task RefreshCurrentView()
	{
		bool isHierarchy = GetCurrentViewMode() == "Folder Hierarchy";
		bool ascOrder = GetCurrentSortOrder() == "Ascending";

		if (IsAtRoot)
		{
			FolderTitleTextBlock.Text = "Folders";
			UpButton.Visibility = Visibility.Collapsed;
			PlayAllButtonStackPanel.Visibility = Visibility.Collapsed;
			FolderTileView.Visibility = Visibility.Visible;
			FolderContentContainer.Visibility = Visibility.Collapsed;

			// Hide song sort options at root
			TitleSort.Visibility = Visibility.Collapsed;
			ArtistsSort.Visibility = Visibility.Collapsed;
			AlbumSort.Visibility = Visibility.Collapsed;
			DurationSort.Visibility = Visibility.Collapsed;
			TrackSort.Visibility = Visibility.Collapsed;
			SortSeparator.Visibility = Visibility.Collapsed;

			// Hide song layout options at root
			ListViewStyle.Visibility = Visibility.Collapsed;
			CompactViewStyle.Visibility = Visibility.Collapsed;
			ViewSeparator.Visibility = Visibility.Collapsed;

			// Update Sort button label
			UpdateSortButtonContent("Order", ascOrder);

			// Update View button label
			var viewDropdownContent = new TextBlock();
			viewDropdownContent.Inlines.Add(new Run { Text = isHierarchy ? "Hierarchy" : "All Folders" });
			ViewButton.Content = viewDropdownContent;
			ToolTipService.SetToolTip(ViewButton, $"Current view mode: {GetCurrentViewMode()}");

			// Query root folders
			List<FolderModel> folders;
			if (isHierarchy)
			{
				folders = await FolderOperations.GetRootFoldersHierarchicalAsync(ascOrder);
			}
			else
			{
				folders = await FolderOperations.GetFoldersFlatAsync(ascOrder);
			}

			RootFolders.Clear();
			RootFolders.AddRange(folders);

			if (RootFolders.Count == 0)
			{
				GoToSettings.Visibility = Visibility.Visible;
				FolderTileView.Visibility = Visibility.Collapsed;
				PageButtons.Visibility = Visibility.Collapsed;
			}
			else
			{
				GoToSettings.Visibility = Visibility.Collapsed;
				FolderTileView.Visibility = Visibility.Visible;
				PageButtons.Visibility = Visibility.Visible;
			}
		}
		else
		{
			// Inside folder
			var folderName = System.IO.Path.GetFileName(CurrentFolderPath);
			FolderTitleTextBlock.Text = string.IsNullOrEmpty(folderName) ? CurrentFolderPath! : folderName;
			UpButton.Visibility = Visibility.Visible;
			PlayAllButtonStackPanel.Visibility = Visibility.Visible;
			FolderTileView.Visibility = Visibility.Collapsed;
			FolderContentContainer.Visibility = Visibility.Visible;

			// Show song sort options
			TitleSort.Visibility = Visibility.Visible;
			ArtistsSort.Visibility = Visibility.Visible;
			AlbumSort.Visibility = Visibility.Visible;
			DurationSort.Visibility = Visibility.Visible;
			TrackSort.Visibility = Visibility.Visible;
			SortSeparator.Visibility = Visibility.Visible;

			// Show song layout options
			ListViewStyle.Visibility = Visibility.Visible;
			CompactViewStyle.Visibility = Visibility.Visible;
			ViewSeparator.Visibility = Visibility.Visible;

			var sortBy = GetCurrentSongSortBy();
			SongProperty sortByEnum = Enum.TryParse<SongProperty>(sortBy, out var parsed) ? parsed : SongProperty.Title;

			// Update Sort button label
			UpdateSortButtonContent(sortBy, ascOrder);

			// Update View style layout (List vs Compact)
			ApplyViewStyleLayout();

			// In Mode 2 ("Folder Hierarchy"), show subfolders first!
			if (isHierarchy)
			{
				var subfolders = await FolderOperations.GetSubFoldersAsync(CurrentFolderPath!, ascOrder);
				SubFolders.Clear();
				SubFolders.AddRange(subfolders);
				SubFoldersSection.Visibility = SubFolders.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
			}
			else
			{
				SubFolders.Clear();
				SubFoldersSection.Visibility = Visibility.Collapsed;
			}

			// Query direct songs in this folder (shown second!)
			var songs = await FolderOperations.GetSongsInFolderAsync(CurrentFolderPath!, includeSubfolders: false, orderBy: sortByEnum, ascending: ascOrder);
			FolderGroupSongs.Clear();
			FolderGroupSongs.AddRange(songs);

			// Update alphabet scrubber
			UpdateScrubber(sortBy, ascOrder);
			await AdjustAlphabetSize();
		}
	}

	private void UpdateSortButtonContent(string field, bool ascOrder)
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

		SortDropDown.Content = sortDropdownContent;
		ToolTipService.SetToolTip(SortDropDown, orderDropdownTooltip);
	}

	private void ApplyViewStyleLayout()
	{
		var viewStyle = GetCurrentViewStyleChoice();
		if (viewStyle == "List View")
		{
			FolderDetailListView.Visibility = Visibility.Visible;
			FolderDetailCompactView.Visibility = Visibility.Collapsed;
			ViewButton.Content = new FontIcon { Glyph = "\uE8FD" };
		}
		else
		{
			FolderDetailListView.Visibility = Visibility.Collapsed;
			FolderDetailCompactView.Visibility = Visibility.Visible;
			ViewButton.Content = new FontIcon { Glyph = "\uE71D" };
		}
		ToolTipService.SetToolTip(ViewButton, viewStyle);
	}

	private void UpdateScrubber(string sortBy, bool ascOrder)
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
				await listView.SmoothScrollIntoViewWithItemAsync(targetSong, itemPlacement: ScrollItemPlacement.Top, disableAnimation: false, scrollIfVisible: false, additionalVerticalOffset: listView == FolderDetailListView ? -40 : 0);
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

	private async void FolderTileView_ItemClick(object sender, ItemClickEventArgs e)
	{
		if (e.ClickedItem is FolderModel folder)
		{
			_navHistory.Push(CurrentFolderPath);
			CurrentFolderPath = folder.Path;
			Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.SelectedFolder)] = folder.Path;
			await RefreshCurrentView();
		}
	}

	private async void SubFolder_ItemClick(object sender, ItemClickEventArgs e)
	{
		if (e.ClickedItem is FolderModel folder)
		{
			_navHistory.Push(CurrentFolderPath);
			CurrentFolderPath = folder.Path;
			Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.SelectedFolder)] = folder.Path;
			await RefreshCurrentView();
		}
	}

	private async void UpButton_Click(object sender, RoutedEventArgs e)
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

	private async void ModeButton_OnClick(object sender, RoutedEventArgs e)
	{
		var currentMode = GetCurrentViewMode();
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.FoldersViewMode)] = currentMode;
		await RefreshCurrentView();
	}

	private async void LayoutButton_OnClick(object sender, RoutedEventArgs e)
	{
		var viewStyle = GetCurrentViewStyleChoice();
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.FolderDetailViewStyle)] = viewStyle;
		ApplyViewStyleLayout();
		await AdjustAlphabetSize();
	}

	private async void SortButton_OnClick(object sender, RoutedEventArgs e)
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

	private void ListView_ItemClick(object sender, ItemClickEventArgs e)
	{
		if (e.ClickedItem is Song track)
		{
			var songPaths = FolderGroupSongs.Select(s => s.Path).ToList();
			MusicPlayer.Instance.LoadPlaylist(songPaths, track.Path);
			Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CurrentPlayinglist)] = PlaylistKey;
		}
	}

	private async void FolderTilePlay_OnClick(object sender, RoutedEventArgs e)
	{
		if ((sender as MenuFlyoutItem)?.DataContext is FolderModel folder)
		{
			bool isHierarchy = GetCurrentViewMode() == "Folder Hierarchy";
			var songs = await FolderOperations.GetSongsInFolderAsync(folder.Path, includeSubfolders: isHierarchy);
			if (songs.Count > 0)
			{
				var songPaths = songs.Select(s => s.Path).ToList();
				MusicPlayer.Instance.LoadPlaylist(songPaths, songPaths[0]);
				Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CurrentPlayinglist)] = $"FolderGroup>{folder.Path}";
			}
		}
	}

	private async void FolderTileAddToQueue_OnClick(object sender, RoutedEventArgs e)
	{
		if ((sender as MenuFlyoutItem)?.DataContext is FolderModel folder)
		{
			bool isHierarchy = GetCurrentViewMode() == "Folder Hierarchy";
			var songs = await FolderOperations.GetSongsInFolderAsync(folder.Path, includeSubfolders: isHierarchy);
			if (songs.Count > 0)
			{
				await DatabaseHelper.Instance.AddSongsToQueuedPlayingList(songs.Select(s => s.Path).ToList());
				GlobalNotification.Info($"Added {songs.Count} songs from '{folder.Name}' to queue.");
			}
		}
	}

	private async void SubFolderPlay_OnClick(object sender, RoutedEventArgs e)
	{
		if ((sender as MenuFlyoutItem)?.DataContext is FolderModel folder)
		{
			var songs = await FolderOperations.GetSongsInFolderAsync(folder.Path, includeSubfolders: true);
			if (songs.Count > 0)
			{
				var songPaths = songs.Select(s => s.Path).ToList();
				MusicPlayer.Instance.LoadPlaylist(songPaths, songPaths[0]);
				Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CurrentPlayinglist)] = $"FolderGroup>{folder.Path}";
			}
		}
	}

	private async void SubFolderAddToQueue_OnClick(object sender, RoutedEventArgs e)
	{
		if ((sender as MenuFlyoutItem)?.DataContext is FolderModel folder)
		{
			var songs = await FolderOperations.GetSongsInFolderAsync(folder.Path, includeSubfolders: true);
			if (songs.Count > 0)
			{
				await DatabaseHelper.Instance.AddSongsToQueuedPlayingList(songs.Select(s => s.Path).ToList());
				GlobalNotification.Info($"Added {songs.Count} songs from '{folder.Name}' to queue.");
			}
		}
	}

	private void MenuFlyoutItemPlay_OnClick(object sender, RoutedEventArgs e)
	{
		var songData = (sender as MenuFlyoutItem)?.DataContext as Song;
		var songPaths = FolderGroupSongs.Select(s => s.Path).ToList();
		MusicPlayer.Instance.LoadPlaylist(songPaths, songData?.Path);
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CurrentPlayinglist)] = PlaylistKey;
	}

	private async void MenuFlyoutItemAddToQueue_OnClick(object sender, RoutedEventArgs e)
	{
		var songData = (sender as MenuFlyoutItem)?.DataContext as Song;
		if (songData != null)
		{
			await DatabaseHelper.Instance.AddSongsToQueuedPlayingList(new List<string> { songData.Path });
			GlobalNotification.Info($"{songData.Title} added to queue.");
		}
	}

	private async void MenuFlyoutItemInfoTag_OnClick(object sender, RoutedEventArgs e)
	{
		var songData = (sender as MenuFlyoutItem)?.DataContext as Song;
		if (songData != null)
		{
			MainPage._instance?.ShowSongInfo(await DatabaseHelper.Instance.GetSongByPath(songData.Path));
		}
	}

	private async void MenuFlyoutItemDelete_OnClick(object sender, RoutedEventArgs e)
	{
		var songData = (sender as MenuFlyoutItem)?.DataContext as Song;
		if (songData != null)
		{
			DeleteDialogText.Text = $"Are you sure you want to delete this song/track from your system?" +
			                        $"\nTitle: {songData.Title}" +
			                        $"\nArtist: {songData.Artists}" +
			                        $"\nAlbum: {songData.Album}" +
			                        $"\nFile: {songData.Path}";

			DeleteDialog.Visibility = Visibility.Visible;
			MainWindow._instance.WindowResizePermission(false);
			var result = await DeleteDialog.ShowAsync();
			MainWindow._instance.WindowResizePermission(true);
			DeleteDialog.Visibility = Visibility.Collapsed;

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

	private async void MenuFlyoutMultiItemAddToQueue_OnClick(object sender, RoutedEventArgs e)
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

	private async void MenuFlyoutMultiItemDelete_OnClick(object sender, RoutedEventArgs e)
	{
		var listView = GetCurrentViewStyle();
		var songs = listView.SelectedItems.OfType<Song>().ToList();
		if (songs.Count > 0)
		{
			DeleteDialogText.Text = $"Are you sure you want to delete {songs.Count} songs/tracks from your system?";
			DeleteDialog.Visibility = Visibility.Visible;
			MainWindow._instance.WindowResizePermission(false);
			var result = await DeleteDialog.ShowAsync();
			MainWindow._instance.WindowResizePermission(true);
			DeleteDialog.Visibility = Visibility.Collapsed;

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

	private void MultiSelectButton_Click(object sender, RoutedEventArgs e)
	{
		var view = GetCurrentViewStyle();
		if (MultiSelectButton.IsChecked == true)
		{
			MoreButton.Visibility = Visibility.Visible;
			MoreButton.IsEnabled = false;
			PlayAllButtonStackPanel.Visibility = Visibility.Collapsed;
			SortAndViewButtonPanel.Visibility = Visibility.Collapsed;
			ToolTipService.SetToolTip(MultiSelectButton, "Turn off multi-select mode");
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
			if (view == FolderDetailListView) Header.Margin = new Thickness(40, 0, 0, 0);
		}
		else
		{
			MoreButton.Visibility = Visibility.Collapsed;
			PlayAllButtonStackPanel.Visibility = IsAtRoot ? Visibility.Collapsed : Visibility.Visible;
			SortAndViewButtonPanel.Visibility = Visibility.Visible;
			ToolTipService.SetToolTip(MultiSelectButton, "Turn on multi-select mode");
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
			if (view == FolderDetailListView) Header.Margin = new Thickness(12, 0, 0, 0);
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

	private void GotoSettigsButton_Click(object sender, RoutedEventArgs e)
	{
		App.Current.NavService.NavigateTo(typeof(SettingsPage));
	}

	private void Page_Loaded(object sender, RoutedEventArgs e)
	{
		_ = AdjustAlphabetSize();
	}

	private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		_dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, async () => await AdjustAlphabetSize());
	}
}
