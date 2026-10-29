using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Tunetastic.Common;
using Tunetastic.Common.Operations;
using Tunetastic.Common.Services;
using Tunetastic.Views.Common;

namespace Tunetastic.Views.LibraryViews;

/// <summary>
/// Detail page for a music folder, displaying subfolders (in hierarchical mode) and songs directly in the folder.
/// Features a breadcrumb header ("Folders > [Folder Name]") with connected animation.
/// </summary>
public sealed partial class FolderDetailPage : SongListPageBase
{
	/// <summary>
	/// Gets the collection of songs displayed inside the active folder.
	/// </summary>
	public ObservableCollection<Song> FolderGroupSongs => ViewModel.Songs;

	/// <summary>
	/// Gets or sets the collection of immediate subfolders displayed in hierarchical mode.
	/// </summary>
	public ObservableCollection<FolderModel> SubFolders { get; set; } = new();

	/// <summary>
	/// Gets or sets the absolute path of the folder currently displayed.
	/// </summary>
	public string CurrentFolderPath { get; set; } = string.Empty;

	/// <inheritdoc/>
	protected override string PlaylistKey => $"FolderGroup>{CurrentFolderPath}";

	/// <inheritdoc/>
	protected override Button ShuffleAndPlayControl => ShuffleAndPlay;

	/// <inheritdoc/>
	protected override Button MoreButtonControl => MoreButton;

	/// <inheritdoc/>
	protected override ListView GetCurrentViewStyle() => CurrentViewStyleText() switch
	{
		"List View" => FolderDetailListView,
		"Compact View" => FolderDetailCompactView,
		_ => FolderDetailCompactView
	};

	private void PlayAllButton_OnClick(object sender, RoutedEventArgs e) => PlayAll_OnClick(sender, e);
	private void ShuffleAndPlayButton_OnClick(object sender, RoutedEventArgs e) => ShuffleAndPlay_OnClick(sender, e);
	private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e) => SongListView_SelectionChanged(sender, e);
	private void MenuFlyout_Opened(object sender, object e) => MenuFlyoutOpened(sender, e);

	private void ListView_ItemClick(object sender, ItemClickEventArgs e)
	{
		var track = e.ClickedItem as Song;
		List<string> songPaths = FolderGroupSongs.Select(s => s.Path).ToList();
		MusicPlayer.Instance.LoadPlaylist(songPaths, track?.Path);
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CurrentPlayinglist)] = $"FolderGroup>{CurrentFolderPath}";
	}

	private void MenuFlyoutItemPlay_OnClick(object sender, RoutedEventArgs e)
	{
		var songData = (sender as MenuFlyoutItem)?.DataContext as Song;
		List<string> songPaths = FolderGroupSongs.Select(s => s.Path).ToList();
		MusicPlayer.Instance.LoadPlaylist(songPaths, songData?.Path);
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CurrentPlayinglist)] = $"FolderGroup>{CurrentFolderPath}";
	}

	private async void MenuFlyoutItemAddToQueue_OnClick(object sender, RoutedEventArgs e)
	{
		var songData = (sender as MenuFlyoutItem)?.DataContext as Song;
		List<string> songPaths = new List<string> { songData?.Path ?? "" };
		await DatabaseHelper.Instance.AddSongsToQueuedPlayingList(songPaths);
		GlobalNotification.Info($"{songData?.Title} added to queue.");
	}

	private async void MenuFlyoutItemInfoTag_OnClick(object sender, RoutedEventArgs e)
	{
		var songData = (sender as MenuFlyoutItem)?.DataContext as Song;
		if (songData is not null) MainPage._instance?.ShowSongInfo(await DatabaseHelper.Instance.GetSongByPath(songData.Path));
	}

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

			MainWindow._instance.WindowResizePermission(false);
			var result = await DeleteDialog.ShowAsync();
			MainWindow._instance.WindowResizePermission(true);
			if (result == ContentDialogResult.Primary)
			{
				if (File.Exists(songData.Path))
				{
					LibraryWatcherService.MarkSelfInitiated(songData.Path);
					File.Delete(songData.Path);
					await DatabaseHelper.Instance.DeleteSongFromDB(songData.Path);
					FolderGroupSongs.Remove(songData);
					MusicPlayer.Instance.HandleAfterDelete();
					GlobalNotification.Success("Song/Track deleted." +
												$"\nTitle: {songData.Title}" +
												$"\nArtist: {songData.Artists}" +
												$"\nAlbum: {songData.Album}" +
												$"\nFile: {songData.Path}");
				}
			}
			if (FolderGroupSongs.Count <= 0 && SubFolders.Count <= 0)
			{
				App.Current.NavService.GoBack();
			}
		}
	}

	public FolderDetailPage()
	{
		this.InitializeComponent();
		_ = RunScanningAwareInit();
	}

	/// <inheritdoc/>
	protected override async void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);

		if (e.Parameter is string folderPath && !string.IsNullOrWhiteSpace(folderPath))
		{
			CurrentFolderPath = folderPath;
			var folderName = Path.GetFileName(folderPath);
			if (string.IsNullOrEmpty(folderName)) folderName = folderPath;
			ActualFolderGroup.Text = folderName;
			ActualFolderGroup.Visibility = Visibility.Visible;
			ActualFolderGroup.Opacity = 1;
			ToolTipService.SetToolTip(ActualFolderGroup, folderPath);
		}

		var animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("FolderHeaderAnimation");
		if (animation != null)
		{
			await ActualFolderGroup.DispatcherQueue.EnqueueAsync(async () =>
			{
				animation.TryStart(ActualFolderGroup);
				await Task.Delay(1000);
				AutoScrollHeader.IsPlaying = true;
			});
		}

		SelectFoldersOnNavigation();
	}

	/// <summary>
	/// Handles the navigation animation and visibility when navigating away from the current page.
	/// </summary>
	protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
	{
		if (e.SourcePageType.Name == "FoldersViewPage")
		{
			AutoScrollHeader.IsPlaying = false;
			ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("FolderHeaderAnimationBack", ActualFolderGroup);

			var fadeOut = new DoubleAnimation
			{
				To = 0,
				Duration = TimeSpan.FromMilliseconds(30),
				FillBehavior = FillBehavior.Stop
			};
			Storyboard.SetTarget(fadeOut, ActualFolderGroup);
			Storyboard.SetTargetProperty(fadeOut, "Opacity");

			var sb = new Storyboard();
			sb.Children.Add(fadeOut);
			sb.Completed += (_, __) => ActualFolderGroup.Visibility = Visibility.Collapsed;
			sb.Begin();
		}
		base.OnNavigatingFrom(e);
	}

	private void Folders_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
	{
		App.Current.NavService.NavigateTo(typeof(FoldersViewPage), "Folders", false, new DrillInNavigationTransitionInfo());
		SelectFoldersOnNavigation();
	}

	private static void SelectFoldersOnNavigation()
	{
		var librariesGroup = App.Current.NavService?.MenuItems?[1] as NavigationViewItem;
		var libraryNavigationItem = librariesGroup?.MenuItems.Select(x => x as NavigationViewItem)
			.FirstOrDefault(x => x?.Tag?.ToString() == "Tunetastic.Views.LibraryViews.FoldersViewPage");
		if (libraryNavigationItem != null)
			libraryNavigationItem.IsSelected = true;
	}

	/// <inheritdoc/>
	protected override Page CreateFreshPage() => new FolderDetailPage();

	/// <inheritdoc/>
	protected override void OnInitializingContent()
	{
		AddGoToSettingsMessage();
		FolderDetailListViewGrid.Visibility = Visibility.Collapsed;
		FolderDetailCompactViewGrid.Visibility = Visibility.Collapsed;
	}

	/// <inheritdoc/>
	protected override Task OnLibraryReadyAsync()
	{
		PageButtons.Visibility = Visibility.Visible;
		PlayAllButtonStackPanel.Visibility = Visibility.Visible;
		SortAndViewButtonPanel.Visibility = Visibility.Visible;
		UpdateAsPerLastSettings();
		return Task.CompletedTask;
	}

	private void UpdateAsPerLastSettings()
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var sortBy = localSettings.Values[nameof(LocalSave.FolderDetailViewSortBy)]?.ToString() ?? "Title";
		var sortOrder = localSettings.Values[nameof(LocalSave.FolderDetailViewSortOrder)]?.ToString() ?? "Ascending";
		var viewStyle = localSettings.Values[nameof(LocalSave.FolderDetailViewStyle)]?.ToString() ?? "Compact View";

		switch (sortBy)
		{
			case "Artists": ArtistsSort.IsChecked = true; break;
			case "Album": AlbumSort.IsChecked = true; break;
			case "Duration": DurationSort.IsChecked = true; break;
			case "Track": TrackSort.IsChecked = true; break;
			case "Title":
			default: TitleSort.IsChecked = true; break;
		}

		if (sortOrder == "Descending")
			Descending.IsChecked = true;
		else
			Ascending.IsChecked = true;

		if (viewStyle == "List View")
			ListViewStyle.IsChecked = true;
		else
			CompactViewStyle.IsChecked = true;

		_ = UpdateListBasedOnSorting();
		ApplyViewStyle(viewStyle);
	}

	private void ApplyViewStyle(string viewStyle)
	{
		if (viewStyle == "List View")
		{
			FolderDetailListViewGrid.Visibility = Visibility.Visible;
			FolderDetailCompactViewGrid.Visibility = Visibility.Collapsed;
			Scrubber.TargetList = FolderDetailListView;
		}
		else
		{
			FolderDetailListViewGrid.Visibility = Visibility.Collapsed;
			FolderDetailCompactViewGrid.Visibility = Visibility.Visible;
			Scrubber.TargetList = FolderDetailCompactView;
		}

		var viewDropdownContent = new TextBlock();
		viewDropdownContent.Inlines.Add(new Run { Text = "View: " });
		viewDropdownContent.Inlines.Add(new Run { Text = viewStyle == "List View" ? "List" : "Compact" });
		ViewButton.Content = viewDropdownContent;
		ToolTipService.SetToolTip(ViewButton, $"Current View: {viewStyle}");
	}

	private async Task UpdateListBasedOnSorting()
	{
		var sortBy = Sort.Items.OfType<RadioMenuFlyoutItem>().FirstOrDefault(item => item.GroupName == "SortBy" && item.IsChecked)?.Text ?? "Title";
		var orderBy = Sort.Items.OfType<RadioMenuFlyoutItem>().FirstOrDefault(item => item.GroupName == "Order" && item.IsChecked)?.Text ?? "Ascending";
		bool AscOrder = orderBy == "Ascending";

		if (string.IsNullOrEmpty(CurrentFolderPath))
			return;

		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var viewMode = localSettings.Values[nameof(LocalSave.FoldersViewMode)]?.ToString() ?? "All Music Folders";
		bool isHierarchy = viewMode == "Folder Hierarchy";

		if (isHierarchy)
		{
			var subfolders = await FolderOperations.GetSubFoldersAsync(CurrentFolderPath, AscOrder);
			SubFolders.Clear();
			SubFolders.AddRange(subfolders);
			SubFoldersSection.Visibility = SubFolders.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
		}
		else
		{
			SubFoldersSection.Visibility = Visibility.Collapsed;
		}

		var songs = await FolderOperations.GetSongsInFolderAsync(
			CurrentFolderPath,
			includeSubfolders: false,
			orderBy: Enum.Parse<SongProperty>(sortBy),
			ascending: AscOrder);

		FolderGroupSongs.Clear();
		FolderGroupSongs.AddRange(songs);

		IOrderedEnumerable<string>? availableLetters = null;
		bool hasSpecialCharacters = false;

		switch (sortBy)
		{
			case "Title":
				availableLetters = FolderGroupSongs.Select(s => FirstLetter(s.Title)).Where(c => c != "#").Distinct().OrderBy(c => c);
				hasSpecialCharacters = FolderGroupSongs.Any(s => FirstLetter(s.Title) == "#");
				break;
			case "Artists":
				availableLetters = FolderGroupSongs.Select(s => FirstLetter(s.Artists)).Where(c => c != "#").Distinct().OrderBy(c => c);
				hasSpecialCharacters = FolderGroupSongs.Any(s => FirstLetter(s.Artists) == "#");
				break;
			case "Album":
				availableLetters = FolderGroupSongs.Select(s => FirstLetter(s.Album)).Where(c => c != "#").Distinct().OrderBy(c => c);
				hasSpecialCharacters = FolderGroupSongs.Any(s => FirstLetter(s.Album) == "#");
				break;
		}

		PopulateAlphabetNavigation(availableLetters, AscOrder, sortBy, hasSpecialCharacters);

		var sortDropdownContent = new TextBlock();
		sortDropdownContent.Inlines.Add(new Run { Text = $"{sortBy}: " });
		sortDropdownContent.Inlines.Add(new Run { Text = $" {(AscOrder ? "⬆️" : "⬇️")}" });

		var orderDropdownTooltip = new TextBlock();
		orderDropdownTooltip.Inlines.Add(new Run { Text = "The list is sorted by " });
		orderDropdownTooltip.Inlines.Add(new Run { Text = sortBy, FontWeight = Microsoft.UI.Text.FontWeights.Bold });
		orderDropdownTooltip.Inlines.Add(new Run { Text = " in " });
		orderDropdownTooltip.Inlines.Add(new Run { Text = orderBy, FontWeight = Microsoft.UI.Text.FontWeights.Bold });
		orderDropdownTooltip.Inlines.Add(new Run { Text = " order." });

		SortDropDown.Content = sortDropdownContent;
		ToolTipService.SetToolTip(SortDropDown, orderDropdownTooltip);

		localSettings.Values[nameof(LocalSave.FolderDetailViewSortBy)] = sortBy;
		localSettings.Values[nameof(LocalSave.FolderDetailViewSortOrder)] = orderBy;

		GoToSettings.Visibility = (FolderGroupSongs.Count == 0 && SubFolders.Count == 0) ? Visibility.Visible : Visibility.Collapsed;
	}

	private async void SortButton_OnClick(object sender, RoutedEventArgs e)
	{
		await UpdateListBasedOnSorting();
	}

	private void ViewButton_OnClick(object sender, RoutedEventArgs e)
	{
		var viewStyle = ListViewStyle.IsChecked ? "List View" : "Compact View";
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.FolderDetailViewStyle)] = viewStyle;
		ApplyViewStyle(viewStyle);
	}

	private void SubFolderTileView_ItemClick(object sender, ItemClickEventArgs e)
	{
		if (e.ClickedItem is FolderModel subfolder)
		{
			Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.SelectedFolder)] = subfolder.Path;
			App.Current.NavService.NavigateTo(typeof(FolderDetailPage), subfolder.Path, false, new DrillInNavigationTransitionInfo());
		}
	}

	private async void SubFolderPlay_OnClick(object sender, RoutedEventArgs e)
	{
		if ((sender as MenuFlyoutItem)?.DataContext is FolderModel subfolder)
		{
			var songs = await FolderOperations.GetSongsInFolderAsync(subfolder.Path, includeSubfolders: true);
			if (songs.Count > 0)
			{
				var songPaths = songs.Select(s => s.Path).ToList();
				MusicPlayer.Instance.LoadPlaylist(songPaths, songPaths[0]);
				Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CurrentPlayinglist)] = $"FolderGroup>{subfolder.Path}";
			}
		}
	}

	private async void SubFolderAddToQueue_OnClick(object sender, RoutedEventArgs e)
	{
		if ((sender as MenuFlyoutItem)?.DataContext is FolderModel subfolder)
		{
			var songs = await FolderOperations.GetSongsInFolderAsync(subfolder.Path, includeSubfolders: true);
			if (songs.Count > 0)
			{
				await DatabaseHelper.Instance.AddSongsToQueuedPlayingList(songs.Select(s => s.Path).ToList());
				GlobalNotification.Info($"Added {songs.Count} songs from '{subfolder.Name}' to queue.");
			}
		}
	}

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
					uiElement.IsRightTapEnabled = false;
			}
			if (view == FolderDetailListView) Header.Margin = new Thickness(40, 0, 0, 0);
		}
		else
		{
			MoreButton.Visibility = Visibility.Collapsed;
			PlayAllButtonStackPanel.Visibility = Visibility.Visible;
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
					uiElement.IsRightTapEnabled = true;
			}
			if (view == FolderDetailListView) Header.Margin = new Thickness(12, 0, 0, 0);
		}
	}

	private async void MenuFlyoutMultiItemAddToQueue_OnClick(object sender, RoutedEventArgs e)
	{
		var songs = GetCurrentViewStyle().SelectedItems;
		var songPaths = songs.Select(s => ((Song)s).Path).ToList();
		await DatabaseHelper.Instance.AddSongsToQueuedPlayingList(songPaths);
		GlobalNotification.Info($"{songPaths.Count} songs added to queue.");
	}

	private async void MenuFlyoutMultiItemDelete_OnClick(object sender, RoutedEventArgs e)
	{
		var songs = GetCurrentViewStyle().SelectedItems.OfType<Song>().ToList();
		if (songs.Count == 0) return;

		DeleteDialog.Visibility = Visibility.Visible;
		DeleteDialogText.Text = $"Are you sure you want to delete {songs.Count} songs from your system?";

		MainWindow._instance.WindowResizePermission(false);
		var result = await DeleteDialog.ShowAsync();
		MainWindow._instance.WindowResizePermission(true);

		if (result == ContentDialogResult.Primary)
		{
			foreach (var song in songs)
			{
				if (File.Exists(song.Path))
				{
					LibraryWatcherService.MarkSelfInitiated(song.Path);
					File.Delete(song.Path);
					await DatabaseHelper.Instance.DeleteSongFromDB(song.Path);
					FolderGroupSongs.Remove(song);
				}
			}
			MusicPlayer.Instance.HandleAfterDelete();
			GlobalNotification.Success($"Deleted {songs.Count} songs.");
		}
	}

	/// <inheritdoc/>
	protected override string BuildAddToPlaylistTooltip(bool multiSelect, string playList) =>
		$"Add {(multiSelect ? "selected songs" : "this song")} to {playList} playlist";

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
			if ((sender as MenuFlyoutItem)?.DataContext is Song song)
			{
				await DatabaseHelper.Instance.AddSongToPlaylist(playlist, song.Path);
				GlobalNotification.Info($"{song.Title} added to {playlist} playlist.");
			}
		}
	}

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
				if ((sender as MenuFlyoutItem)?.DataContext is Song song)
				{
					await DatabaseHelper.Instance.AddSongToPlaylist(playlistName, song.Path);
					GlobalNotification.Info($"{song.Title} added to {playlistName} playlist.");
				}
			}
		}
	}

	private void GotoSettigsButton_Click(object sender, RoutedEventArgs e)
	{
		App.Current.NavService.NavigateTo(typeof(SettingsPage));
	}

	private async void Page_Loaded(object sender, RoutedEventArgs e)
	{
		_ = AdjustAlphabetSize();
		while (FolderGroupSongs == null || FolderGroupSongs.Count == 0)
		{
			await Task.Delay(100);
		}
		ScrollToCurrentPlayingTrack();
	}

	private void ScrollToCurrentPlayingTrack()
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var currentPlaylist = localSettings.Values[nameof(LocalSave.CurrentPlayinglist)]?.ToString() ?? string.Empty;
		if (currentPlaylist == $"FolderGroup>{CurrentFolderPath}")
		{
			var lastTrack = localSettings.Values[nameof(LocalSave.LastPlayedTrack)]?.ToString();
			var song = FolderGroupSongs.FirstOrDefault(s => s.Path == lastTrack);
			_ = ScrollToSong(song);
		}
	}

	private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		_dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, async () => await AdjustAlphabetSize());
	}
}

