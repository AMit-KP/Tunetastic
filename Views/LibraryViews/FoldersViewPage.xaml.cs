using System.Collections.ObjectModel;
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
/// Displays music folders from libraries with support for two modes:
/// 1. "All Music Folders": Flat listing of all folders directly containing music files.
/// 2. "Folder Hierarchy": Hierarchical root music folders.
/// Clicking any folder opens <see cref="FolderDetailPage"/>.
/// </summary>
public sealed partial class FoldersViewPage : TileListPageBase
{
	private double _lastSizedWidth = -1;
	private int _lastSizedCount = -1;
	private double _adjustedTileMargin = 10;
	private bool connectedAnimation = false;

	/// <summary>
	/// Predefined messages displayed when no folders are found in the library.
	/// </summary>
	private readonly List<string> GoToMessages = new()
	{
		"No music folders found.\nPlease check your settings to ensure your libraries are added and songs have been scanned."
	};

	/// <summary>
	/// Gets or sets the collection of folder models displayed in the tile view.
	/// </summary>
	public ObservableCollection<FolderModel> FoldersGroup { get; set; } = new();

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
	}

	/// <inheritdoc/>
	protected override Page CreateFreshPage() => new FoldersViewPage();

	/// <inheritdoc/>
	protected override Task OnLibraryReadyAsync()
	{
		SortDropDown.Visibility = Visibility.Visible;
		ViewButton.Visibility = Visibility.Visible;
		PageButtons.Visibility = Visibility.Visible;
		FolderTileView.Visibility = Visibility.Visible;
		UpdateAsPerLastSettings();
		return Task.CompletedTask;
	}

	/// <inheritdoc/>
	protected override List<string> EmptyLibraryMessages => GoToMessages;

	/// <inheritdoc/>
	protected override ListViewBase TileView => FolderTileView;

	/// <inheritdoc/>
	protected override Button MoreButtonControl => MoreButton;

	/// <inheritdoc/>
	protected override string BuildAddToPlaylistTooltip(bool multiSelect, string playList) =>
		$"Add {(multiSelect ? "selected folders" : "this folder")} of songs/tracks to {playList} playlist";

	private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e) => TileView_SelectionChanged(sender, e);

	private void MenuFlyout_Opened(object sender, object e) => MenuFlyoutOpened(sender, e);

	private void UpdateAsPerLastSettings()
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var sortOrder = localSettings.Values[nameof(LocalSave.FoldersViewSortOrder)]?.ToString() ?? "Ascending";
		var viewMode = localSettings.Values[nameof(LocalSave.FoldersViewMode)]?.ToString() ?? "All Music Folders";

		if (sortOrder == "Descending")
			Descending.IsChecked = true;
		else
			Ascending.IsChecked = true;

		if (viewMode == "Folder Hierarchy")
			FolderHierarchyViewStyle.IsChecked = true;
		else
			AllMusicFoldersViewStyle.IsChecked = true;

		_ = UpdateListBasedOnSorting();
	}

	private async Task UpdateListBasedOnSorting()
	{
		var folderModel = FolderTileView.SelectedItem as FolderModel;
		var orderBy = Sort.Items.OfType<RadioMenuFlyoutItem>().Where(item => item.GroupName == "Order" && item.IsChecked).Select(item => item.Text).FirstOrDefault() ?? "Ascending";
		bool AscOrder = orderBy == "Ascending";

		bool isHierarchy = FolderHierarchyViewStyle.IsChecked;
		string viewModeText = isHierarchy ? "Folder Hierarchy" : "All Music Folders";

		var groups = isHierarchy
			? await FolderOperations.GetRootFoldersHierarchicalAsync(AscOrder)
			: await FolderOperations.GetFoldersFlatAsync(AscOrder);

		FoldersGroup.Clear();
		FoldersGroup.AddRange(groups);

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

		var viewDropdownContent = new TextBlock();
		viewDropdownContent.Inlines.Add(new Run { Text = "Mode: " });
		viewDropdownContent.Inlines.Add(new Run { Text = isHierarchy ? "Hierarchy" : "Flat" });
		ViewButton.Content = viewDropdownContent;
		ToolTipService.SetToolTip(ViewButton, $"Current Mode: {viewModeText}");

		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		localSettings.Values[nameof(LocalSave.FoldersViewSortOrder)] = orderBy;
		localSettings.Values[nameof(LocalSave.FoldersViewMode)] = viewModeText;

		if (folderModel != null)
		{
			folderModel = FoldersGroup.FirstOrDefault(s => s.Path.Equals(folderModel.Path, StringComparison.OrdinalIgnoreCase));
			await Task.Delay(300);
			await ScrollToTile(folderModel);
		}
	}

	private async Task ScrollToTile(FolderModel? tile)
	{
		if (tile != null)
		{
			FolderTileView.SelectedItem = tile;
			try
			{
				await FolderTileView.SmoothScrollIntoViewWithItemAsync(tile, itemPlacement: ScrollItemPlacement.Center, disableAnimation: false, scrollIfVisible: false);
			}
			catch (Exception)
			{
			}
		}
	}

	private async void SortButton_OnClick(object sender, RoutedEventArgs e)
	{
		_lastSizedWidth = -1;
		_lastSizedCount = -1;
		await UpdateListBasedOnSorting();
		await AdjustAlphabetSize();
	}

	private async void ViewButton_OnClick(object sender, RoutedEventArgs e)
	{
		_lastSizedWidth = -1;
		_lastSizedCount = -1;
		await UpdateListBasedOnSorting();
		await AdjustAlphabetSize();
	}

	private void FolderTileView_ItemClick(object sender, ItemClickEventArgs e)
	{
		if (e.ClickedItem is FolderModel folderModel)
		{
			var container = FolderTileView.ContainerFromItem(folderModel) as ListViewItem;
			if (container != null)
			{
				var folderTextBlock = DevWinUI.DependencyObjectExtensions.FindDescendant(container, "FolderTextBlock");
				if (folderTextBlock != null)
				{
					ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("FolderHeaderAnimation", folderTextBlock);

					var fadeOut = new DoubleAnimation
					{
						To = 0,
						Duration = TimeSpan.FromMilliseconds(30),
						FillBehavior = FillBehavior.Stop
					};
					Storyboard.SetTarget(fadeOut, folderTextBlock);
					Storyboard.SetTargetProperty(fadeOut, "Opacity");

					var sb = new Storyboard();
					sb.Children.Add(fadeOut);
					sb.Completed += (_, __) => folderTextBlock.Visibility = Visibility.Collapsed;
					sb.Begin();
				}
			}

			Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.SelectedFolder)] = folderModel.Path;
			App.Current.NavService.NavigateTo(typeof(FolderDetailPage), folderModel.Path, false, new DrillInNavigationTransitionInfo());
		}
	}

	private ConnectedAnimation? _pendingBackAnimation = null;
	private bool _isExecutingBackAnimation = false;

	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);
		connectedAnimation = (e.NavigationMode == NavigationMode.Back) || (e.Parameter is string s && s == "Folders");
		if (connectedAnimation)
		{
			var anim = ConnectedAnimationService.GetForCurrentView().GetAnimation("FolderHeaderAnimationBack");
			if (anim != null)
			{
				_pendingBackAnimation = anim;
			}
			if (_pendingBackAnimation != null)
			{
				_ = TryPlayBackAnimationAsync();
			}
		}
	}

	protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
	{
		if (e.SourcePageType.Name == "FolderDetailPage")
		{
			var selectedFolder = Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.SelectedFolder)]?.ToString();
			var selectedFolderModel = FoldersGroup.FirstOrDefault(s => s.Path == selectedFolder);

			if (selectedFolderModel != null)
			{
				var container = FolderTileView.ContainerFromItem(selectedFolderModel) as ListViewItem;
				if (container != null)
				{
					var folderTextBlock = DevWinUI.DependencyObjectExtensions.FindDescendant(container, "FolderTextBlock");
					if (folderTextBlock != null)
					{
						ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("FolderHeaderAnimation", folderTextBlock);

						var fadeOut = new DoubleAnimation
						{
							To = 0,
							Duration = TimeSpan.FromMilliseconds(30),
							FillBehavior = FillBehavior.Stop
						};
						Storyboard.SetTarget(fadeOut, folderTextBlock);
						Storyboard.SetTargetProperty(fadeOut, "Opacity");

						var sb = new Storyboard();
						sb.Children.Add(fadeOut);
						sb.Completed += (_, __) => folderTextBlock.Visibility = Visibility.Collapsed;
						sb.Begin();
					}
				}
			}
		}
		base.OnNavigatingFrom(e);
	}

	private async Task TryPlayBackAnimationAsync()
	{
		if (_isExecutingBackAnimation) return;

		if (_pendingBackAnimation == null)
		{
			_pendingBackAnimation = ConnectedAnimationService.GetForCurrentView().GetAnimation("FolderHeaderAnimationBack");
		}

		if (_pendingBackAnimation == null) return;

		var selectedFolder = Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.SelectedFolder)]?.ToString();
		if (string.IsNullOrEmpty(selectedFolder))
		{
			_pendingBackAnimation = null;
			return;
		}

		_isExecutingBackAnimation = true;
		try
		{
			var normSelected = FolderOperations.NormalizeFolderPath(selectedFolder);

			int waitCount = 0;
			while ((FoldersGroup == null || FoldersGroup.Count == 0) && waitCount < 30)
			{
				await Task.Delay(50);
				waitCount++;
			}

			if (FoldersGroup == null || FoldersGroup.Count == 0)
			{
				_pendingBackAnimation = null;
				return;
			}

			var selectedFolderModel = FoldersGroup.FirstOrDefault(s =>
				FolderOperations.NormalizeFolderPath(s.Path).Equals(normSelected, StringComparison.OrdinalIgnoreCase))
				?? FoldersGroup.FirstOrDefault(s =>
				normSelected.StartsWith(FolderOperations.NormalizeFolderPath(s.Path), StringComparison.OrdinalIgnoreCase));

			if (selectedFolderModel == null)
			{
				_pendingBackAnimation = null;
				return;
			}

			try
			{
				await FolderTileView.SmoothScrollIntoViewWithItemAsync(selectedFolderModel, itemPlacement: ScrollItemPlacement.Center, disableAnimation: true, scrollIfVisible: false);
			}
			catch (Exception)
			{
			}

			ListViewItem? container = null;
			for (int i = 0; i < 20; i++)
			{
				container = FolderTileView.ContainerFromItem(selectedFolderModel) as ListViewItem;
				if (container != null) break;
				await Task.Delay(40);
			}

			if (container != null)
			{
				FrameworkElement? folderTextBlock = null;
				for (int i = 0; i < 15; i++)
				{
					folderTextBlock = DevWinUI.DependencyObjectExtensions.FindDescendant(container, "FolderTextBlock") as FrameworkElement;
					if (folderTextBlock != null) break;
					await Task.Delay(30);
				}

				if (folderTextBlock != null && _pendingBackAnimation != null)
				{
					folderTextBlock.Visibility = Visibility.Visible;
					folderTextBlock.Opacity = 1;
					_pendingBackAnimation.TryStart(folderTextBlock);
					_pendingBackAnimation = null;
					connectedAnimation = false;
					return;
				}
			}

			_pendingBackAnimation = null;
			connectedAnimation = false;
		}
		finally
		{
			_isExecutingBackAnimation = false;
		}
	}

	private async void Page_Loaded(object sender, RoutedEventArgs e)
	{
		while (FoldersGroup == null || FoldersGroup.Count == 0)
		{
			await Task.Delay(100);
		}

		FolderTileView_SizeChanged(null, null);
		FolderTileView.ContainerContentChanging += FolderTileView_ContainerContentChanging;

		if (connectedAnimation)
		{
			_ = TryPlayBackAnimationAsync();
			var currentPlaylist = Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CurrentPlayinglist)]?.ToString() ?? string.Empty;
			if (currentPlaylist.StartsWith("FolderGroup>"))
			{
				var selectedTile = FoldersGroup.FirstOrDefault(s => s.Path == currentPlaylist.Substring("FolderGroup>".Length));
				FolderTileView.SelectedItem = selectedTile;
			}
		}
		else
			ScrollToCurrentPlayingTrack();

		await Task.Delay(100);
		FolderTileView.SizeChanged -= FolderTileView_SizeChanged;
		FolderTileView.SizeChanged += FolderTileView_SizeChanged;
	}

	private void ScrollToCurrentPlayingTrack()
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var currentPlaylist = localSettings.Values[nameof(LocalSave.CurrentPlayinglist)]?.ToString() ?? string.Empty;
		if (currentPlaylist.StartsWith("FolderGroup>"))
		{
			var selectedTile = FoldersGroup.FirstOrDefault(s => s.Path == currentPlaylist.Substring("FolderGroup>".Length));
			_ = ScrollToTile(selectedTile);
		}
	}

	private void FolderTileView_SizeChanged(object? sender, SizeChangedEventArgs? e)
	{
		const double baseContentWidth = 220;
		const double baseMargin = 10;

		double availableWidth = FolderTileView.ActualWidth;
		if (availableWidth <= 0 || FolderTileView.ItemsPanelRoot is not ItemsWrapGrid grid)
			return;

		int itemCount = FolderTileView.Items.Count;
		if (itemCount <= 0) return;

		if (availableWidth == _lastSizedWidth && itemCount == _lastSizedCount)
			return;
		_lastSizedWidth = availableWidth;
		_lastSizedCount = itemCount;

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

		_adjustedTileMargin = adjustedMargin;

		for (int i = 0; i < itemCount; i++)
		{
			var container = FolderTileView.ContainerFromItem(FolderTileView.Items[i]) as ListViewItem;
			if (container != null)
			{
				container.Margin = new Thickness(adjustedMargin, 15, adjustedMargin, 15);
			}
		}

		grid.ItemWidth = adjustedItemWidth;
	}

	private void FolderTileView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
	{
		FolderTileView_SizeChanged(null, null);

		if (args.ItemContainer is ListViewItem container)
			container.Margin = new Thickness(_adjustedTileMargin, 15, _adjustedTileMargin, 15);
	}

	private void NavigationPanelEvaluate()
	{
		var orderBy = Sort.Items.OfType<RadioMenuFlyoutItem>().Where(item => item.GroupName == "Order" && item.IsChecked).Select(item => item.Text).FirstOrDefault() ?? "Ascending";
		bool AscOrder = orderBy == "Ascending";

		var availableLetters = FoldersGroup.Select(f => FirstLetter(f.Name)).Where(c => c != "#").Distinct().OrderBy(c => c);
		bool hasSpecialCharacters = FoldersGroup.Any(f => FirstLetter(f.Name) == "#");

		PopulateAlphabetNavigation(availableLetters, AscOrder, hasSpecialCharacters);
	}

	protected override string LetterForItem(object item)
		=> item is FolderModel folder ? FirstLetter(folder.Name) : "#";

	protected override async void ScrollToSection(string letter)
	{
		FolderModel? targetFolder = letter != "#"
			? FoldersGroup.FirstOrDefault(folder => folder.Name.StartsWith(letter, StringComparison.OrdinalIgnoreCase))
			: FoldersGroup.FirstOrDefault(folder => FirstLetter(folder.Name) == "#");

		if (targetFolder != null)
		{
			try
			{
				await FolderTileView.SmoothScrollIntoViewWithItemAsync(targetFolder, itemPlacement: ScrollItemPlacement.Top, disableAnimation: false, scrollIfVisible: false);
			}
			catch (Exception)
			{
			}
			FolderTileView.SelectedItem = targetFolder;
			await Task.Delay(500);
			try
			{
				await FolderTileView.SmoothScrollIntoViewWithItemAsync(targetFolder, itemPlacement: ScrollItemPlacement.Top, disableAnimation: false, scrollIfVisible: false);
			}
			catch (Exception)
			{
			}
		}
	}

	private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
		=> _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, async () => await AdjustAlphabetSize());

	private void GotoSettigsButton_Click(object sender, RoutedEventArgs e)
	{
		App.Current.NavService.NavigateTo(typeof(SettingsPage));
	}

	private void MultiSelectButton_Click(object sender, RoutedEventArgs e)
	{
		if (MultiSelectButton.IsChecked == true)
		{
			MoreButton.Visibility = Visibility.Visible;
			MoreButton.IsEnabled = false;
			SortAndViewButtonPanel.Visibility = Visibility.Collapsed;
			ToolTipService.SetToolTip(MultiSelectButton, "Turn off multi-select mode");
			FolderTileView.SelectionMode = ListViewSelectionMode.Multiple;
			FolderTileView.IsItemClickEnabled = false;
			FolderTileView.IsMultiSelectCheckBoxEnabled = true;
			FolderTileView.IsRightTapEnabled = false;

			var itemGrids = DevWinUI.DependencyObjectExtensions.FindDescendants(FolderTileView);
			foreach (var item in itemGrids)
			{
				if (item is UIElement uiElement)
					uiElement.IsRightTapEnabled = false;
			}
		}
		else
		{
			MoreButton.Visibility = Visibility.Collapsed;
			SortAndViewButtonPanel.Visibility = Visibility.Visible;
			ToolTipService.SetToolTip(MultiSelectButton, "Turn on multi-select mode");
			FolderTileView.SelectionMode = ListViewSelectionMode.Single;
			FolderTileView.IsItemClickEnabled = true;
			FolderTileView.IsMultiSelectCheckBoxEnabled = false;
			FolderTileView.IsRightTapEnabled = true;

			var itemGrids = DevWinUI.DependencyObjectExtensions.FindDescendants(FolderTileView);
			foreach (var item in itemGrids)
			{
				if (item is UIElement uiElement)
					uiElement.IsRightTapEnabled = true;
			}
		}
	}

	private async void MenuFlyoutItemPlay_OnClick(object sender, RoutedEventArgs e)
	{
		if ((sender as MenuFlyoutItem)?.DataContext is FolderModel folder)
		{
			bool isHierarchy = FolderHierarchyViewStyle.IsChecked;
			var songs = await FolderOperations.GetSongsInFolderAsync(folder.Path, includeSubfolders: isHierarchy);
			if (songs.Count > 0)
			{
				var songPaths = songs.Select(s => s.Path).ToList();
				MusicPlayer.Instance.LoadPlaylist(songPaths, songPaths[0]);
				Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CurrentPlayinglist)] = $"FolderGroup>{folder.Path}";
			}
		}
	}

	private async void MenuFlyoutItemAddToQueue_OnClick(object sender, RoutedEventArgs e)
	{
		if ((sender as MenuFlyoutItem)?.DataContext is FolderModel folder)
		{
			bool isHierarchy = FolderHierarchyViewStyle.IsChecked;
			var songs = await FolderOperations.GetSongsInFolderAsync(folder.Path, includeSubfolders: isHierarchy);
			if (songs.Count > 0)
			{
				await DatabaseHelper.Instance.AddSongsToQueuedPlayingList(songs.Select(s => s.Path).ToList());
				GlobalNotification.Info($"Added {songs.Count} songs from '{folder.Name}' to queue.");
			}
		}
	}

	private async void MenuFlyoutItemDelete_OnClick(object sender, RoutedEventArgs e)
	{
		if ((sender as MenuFlyoutItem)?.DataContext is FolderModel folder)
		{
			bool isHierarchy = FolderHierarchyViewStyle.IsChecked;
			var songs = await FolderOperations.GetSongsInFolderAsync(folder.Path, includeSubfolders: isHierarchy);
			if (songs.Count == 0) return;

			DeleteDialog.Visibility = Visibility.Visible;
			DeleteDialogText.Text = $"Are you sure you want to delete {songs.Count} songs in '{folder.Name}' from your system?";

			MainWindow._instance.WindowResizePermission(false);
			var result = await DeleteDialog.ShowAsync();
			MainWindow._instance.WindowResizePermission(true);

			if (result == ContentDialogResult.Primary)
			{
				foreach (var s in songs)
				{
					if (File.Exists(s.Path))
					{
						LibraryWatcherService.MarkSelfInitiated(s.Path);
						File.Delete(s.Path);
						await DatabaseHelper.Instance.DeleteSongFromDB(s.Path);
					}
				}
				FolderOperations.InvalidateCache();
				FoldersGroup.Remove(folder);
				MusicPlayer.Instance.HandleAfterDelete();
				GlobalNotification.Success($"Deleted {songs.Count} songs from '{folder.Name}'.");
			}
		}
	}

	private async void MenuFlyoutMultiItemAddToQueue_OnClick(object sender, RoutedEventArgs e)
	{
		var selectedFolders = FolderTileView.SelectedItems.OfType<FolderModel>().ToList();
		bool isHierarchy = FolderHierarchyViewStyle.IsChecked;
		var allPaths = new List<string>();

		foreach (var folder in selectedFolders)
		{
			var songs = await FolderOperations.GetSongsInFolderAsync(folder.Path, includeSubfolders: isHierarchy);
			allPaths.AddRange(songs.Select(s => s.Path));
		}

		if (allPaths.Count > 0)
		{
			await DatabaseHelper.Instance.AddSongsToQueuedPlayingList(allPaths);
			GlobalNotification.Info($"Added {allPaths.Count} songs from {selectedFolders.Count} folders to queue.");
		}
	}

	private async void MenuFlyoutMultiItemDelete_OnClick(object sender, RoutedEventArgs e)
	{
		var selectedFolders = FolderTileView.SelectedItems.OfType<FolderModel>().ToList();
		bool isHierarchy = FolderHierarchyViewStyle.IsChecked;
		var allSongs = new List<Song>();

		foreach (var folder in selectedFolders)
		{
			var songs = await FolderOperations.GetSongsInFolderAsync(folder.Path, includeSubfolders: isHierarchy);
			allSongs.AddRange(songs);
		}

		if (allSongs.Count == 0) return;

		DeleteDialog.Visibility = Visibility.Visible;
		DeleteDialogText.Text = $"Are you sure you want to delete {allSongs.Count} songs from {selectedFolders.Count} folders from your system?";

		MainWindow._instance.WindowResizePermission(false);
		var result = await DeleteDialog.ShowAsync();
		MainWindow._instance.WindowResizePermission(true);

		if (result == ContentDialogResult.Primary)
		{
			foreach (var s in allSongs)
			{
				if (File.Exists(s.Path))
				{
					LibraryWatcherService.MarkSelfInitiated(s.Path);
					File.Delete(s.Path);
					await DatabaseHelper.Instance.DeleteSongFromDB(s.Path);
				}
			}
			FolderOperations.InvalidateCache();
			foreach (var f in selectedFolders)
			{
				FoldersGroup.Remove(f);
			}
			MusicPlayer.Instance.HandleAfterDelete();
			GlobalNotification.Success($"Deleted {allSongs.Count} songs.");
		}
	}

	/// <inheritdoc/>
	protected override async void AddToPlaylist_Click(object sender, RoutedEventArgs e)
	{
		var playlist = (sender as MenuFlyoutItem)?.Text;
		if (playlist == null) return;

		bool isHierarchy = FolderHierarchyViewStyle.IsChecked;
		var allPaths = new List<string>();

		if (FolderTileView.IsMultiSelectCheckBoxEnabled)
		{
			var selectedFolders = FolderTileView.SelectedItems.OfType<FolderModel>().ToList();
			foreach (var f in selectedFolders)
			{
				var songs = await FolderOperations.GetSongsInFolderAsync(f.Path, includeSubfolders: isHierarchy);
				allPaths.AddRange(songs.Select(s => s.Path));
			}
		}
		else
		{
			var folder = (sender as MenuFlyoutItem)?.DataContext as FolderModel;
			if (folder != null)
			{
				var songs = await FolderOperations.GetSongsInFolderAsync(folder.Path, includeSubfolders: isHierarchy);
				allPaths.AddRange(songs.Select(s => s.Path));
			}
		}

		if (allPaths.Count > 0)
		{
			await DatabaseHelper.Instance.AddSongsToPlaylist(playlist, allPaths);
			GlobalNotification.Info($"Added {allPaths.Count} songs to playlist '{playlist}'.");
		}
	}

	/// <inheritdoc/>
	protected override async void NewPlaylist_Click(object sender, RoutedEventArgs e)
	{
		var result = await MainPage._instance!.ShowAddPlaylistDialog(songsToAddAfterCreate: true);
		if (result.Result != AddPlaylistResult.Cancelled)
		{
			string playlistName = result.Name!;
			bool isHierarchy = FolderHierarchyViewStyle.IsChecked;
			var allPaths = new List<string>();

			if (FolderTileView.IsMultiSelectCheckBoxEnabled)
			{
				var selectedFolders = FolderTileView.SelectedItems.OfType<FolderModel>().ToList();
				foreach (var f in selectedFolders)
				{
					var songs = await FolderOperations.GetSongsInFolderAsync(f.Path, includeSubfolders: isHierarchy);
					allPaths.AddRange(songs.Select(s => s.Path));
				}
			}
			else
			{
				var folder = (sender as MenuFlyoutItem)?.DataContext as FolderModel;
				if (folder != null)
				{
					var songs = await FolderOperations.GetSongsInFolderAsync(folder.Path, includeSubfolders: isHierarchy);
					allPaths.AddRange(songs.Select(s => s.Path));
				}
			}

			if (allPaths.Count > 0)
			{
				await DatabaseHelper.Instance.AddSongsToPlaylist(playlistName, allPaths);
				GlobalNotification.Info($"Added {allPaths.Count} songs to '{playlistName}' playlist.");
			}
		}
	}
}
