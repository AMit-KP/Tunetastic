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

	private void MenuFlyout_Opened(object sender, object e)
	{
		var menu = sender as MenuFlyout;
		var target = menu?.Target as FrameworkElement;
		var song = target?.DataContext as Song
			?? menu?.Items.OfType<FrameworkElement>().Select(i => i.DataContext).OfType<Song>().FirstOrDefault();

		if (song != null && song.IsFolder)
		{
			if (AddToPlaylist != null) AddToPlaylist.Visibility = Visibility.Collapsed;
			if (PlaylistSeparator != null) PlaylistSeparator.Visibility = Visibility.Collapsed;
			if (InfoTagMenuItem != null) InfoTagMenuItem.Visibility = Visibility.Collapsed;
			if (InfoSeparator != null) InfoSeparator.Visibility = Visibility.Collapsed;
			if (DeleteMenuItem != null) DeleteMenuItem.Visibility = Visibility.Collapsed;
			if (DeleteSeparator != null) DeleteSeparator.Visibility = Visibility.Collapsed;
		}
		else
		{
			if (AddToPlaylist != null) AddToPlaylist.Visibility = Visibility.Visible;
			if (PlaylistSeparator != null) PlaylistSeparator.Visibility = Visibility.Visible;
			if (InfoTagMenuItem != null) InfoTagMenuItem.Visibility = Visibility.Visible;
			if (InfoSeparator != null) InfoSeparator.Visibility = Visibility.Visible;
			if (DeleteMenuItem != null) DeleteMenuItem.Visibility = Visibility.Visible;
			if (DeleteSeparator != null) DeleteSeparator.Visibility = Visibility.Visible;
			MenuFlyoutOpened(sender, e);
		}
	}

	private void ListView_ItemClick(object sender, ItemClickEventArgs e)
	{
		if (e.ClickedItem is Song track)
		{
			if (track.IsFolder)
			{
				NavigateToFolder(track.Path);
				return;
			}

			List<string> songPaths = FolderGroupSongs.Where(s => !s.IsFolder).Select(s => s.Path).ToList();
			MusicPlayer.Instance.LoadPlaylist(songPaths, track.Path);
			Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CurrentPlayinglist)] = $"FolderGroup>{CurrentFolderPath}";
		}
	}

	private async void MenuFlyoutItemPlay_OnClick(object sender, RoutedEventArgs e)
	{
		var songData = (sender as MenuFlyoutItem)?.DataContext as Song;
		if (songData == null) return;

		if (songData.IsFolder)
		{
			var songs = await FolderOperations.GetSongsInFolderAsync(songData.Path, includeSubfolders: true);
			if (songs.Count > 0)
			{
				var songPaths = songs.Select(s => s.Path).ToList();
				MusicPlayer.Instance.LoadPlaylist(songPaths, songPaths[0]);
				Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CurrentPlayinglist)] = $"FolderGroup>{songData.Path}";
			}
		}
		else
		{
			List<string> songPaths = FolderGroupSongs.Where(s => !s.IsFolder).Select(s => s.Path).ToList();
			MusicPlayer.Instance.LoadPlaylist(songPaths, songData.Path);
			Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CurrentPlayinglist)] = $"FolderGroup>{CurrentFolderPath}";
		}
	}

	private async void MenuFlyoutItemAddToQueue_OnClick(object sender, RoutedEventArgs e)
	{
		var songData = (sender as MenuFlyoutItem)?.DataContext as Song;
		if (songData == null) return;

		if (songData.IsFolder)
		{
			var songs = await FolderOperations.GetSongsInFolderAsync(songData.Path, includeSubfolders: true);
			if (songs.Count > 0)
			{
				await DatabaseHelper.Instance.AddSongsToQueuedPlayingList(songs.Select(s => s.Path).ToList());
				GlobalNotification.Info($"Added {songs.Count} songs from '{songData.Title}' to queue.");
			}
		}
		else
		{
			List<string> songPaths = new() { songData.Path };
			await DatabaseHelper.Instance.AddSongsToQueuedPlayingList(songPaths);
			GlobalNotification.Info($"{songData.Title} added to queue.");
		}
	}

	private async void MenuFlyoutItemInfoTag_OnClick(object sender, RoutedEventArgs e)
	{
		var songData = (sender as MenuFlyoutItem)?.DataContext as Song;
		if (songData is not null && !songData.IsFolder)
			MainPage._instance?.ShowSongInfo(await DatabaseHelper.Instance.GetSongByPath(songData.Path));
	}

	private async void MenuFlyoutItemDelete_OnClick(object sender, RoutedEventArgs e)
	{
		var songData = (sender as MenuFlyoutItem)?.DataContext as Song;
		if (songData == null || songData.IsFolder) return;

		DeleteDialog.Visibility = Visibility.Visible;
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
				FolderOperations.InvalidateCache();
				FolderGroupSongs.Remove(songData);
				MusicPlayer.Instance.HandleAfterDelete();
				GlobalNotification.Success("Song/Track deleted." +
											$"\nTitle: {songData.Title}" +
											$"\nArtist: {songData.Artists}" +
											$"\nAlbum: {songData.Album}" +
											$"\nFile: {songData.Path}");
			}
		}
		if (FolderGroupSongs.Count <= 0)
		{
			App.Current.NavService.GoBack();
		}
	}

	public FolderDetailPage()
	{
		this.InitializeComponent();
		_ = RunScanningAwareInit();
	}

	private static readonly System.Reflection.FieldInfo? NavServiceLastParamField =
		typeof(JsonNavigationService).GetField("_lastParameterUsed", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

	private static readonly System.Reflection.FieldInfo? NavServiceSuppressNavField =
		typeof(JsonNavigationService).GetField("_suppressSelectionNavigation", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

	/// <inheritdoc/>
	protected override async void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);

		if (e.Parameter is string folderPath && !string.IsNullOrWhiteSpace(folderPath))
		{
			CurrentFolderPath = folderPath;
			if (App.Current.NavService is JsonNavigationService navService)
			{
				NavServiceLastParamField?.SetValue(navService, folderPath);
			}
			var folderName = Path.GetFileName(folderPath);
			if (string.IsNullOrEmpty(folderName)) folderName = folderPath;
			ActualFolderGroup.Text = folderName;
			ActualFolderGroup.Visibility = Visibility.Visible;
			ActualFolderGroup.Opacity = 1;
			ToolTipService.SetToolTip(ActualFolderGroup, folderPath);
			if (FolderOperations.CachedRootFolders != null)
			{
				UpdateBreadcrumbs(FolderOperations.CachedRootFolders);
			}
			else
			{
				_ = UpdateBreadcrumbsAsync();
			}
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
			Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.SelectedFolder)] = CurrentFolderPath;
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

	private void NavigateToFolder(string folderPath)
	{
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.SelectedFolder)] = folderPath;
		if (App.Current.NavService is JsonNavigationService nav)
		{
			NavServiceLastParamField?.SetValue(nav, null);
		}
		bool navigated = App.Current.NavService.NavigateTo(typeof(FolderDetailPage), folderPath, false, new DrillInNavigationTransitionInfo());
		if (!navigated)
		{
			App.Current.NavService.Frame?.Navigate(typeof(FolderDetailPage), folderPath, new DrillInNavigationTransitionInfo());
		}
	}

	private void UpdateBreadcrumbs(List<FolderModel>? rootFolders)
	{
		BreadcrumbSegmentsPanel.Children.Clear();
		if (string.IsNullOrWhiteSpace(CurrentFolderPath))
		{
			BreadcrumbsColDef.Width = new GridLength(1, GridUnitType.Star);
			HeaderColDef.Width = new GridLength(0);
			BreadcrumbSegmentsPanel.Visibility = Visibility.Collapsed;
			Grid.SetColumn(HeaderContainerGrid, 2);
			return;
		}

		try
		{
			var normalizedCurrent = FolderOperations.NormalizeFolderPath(CurrentFolderPath);
			var matchingRoot = rootFolders?.FirstOrDefault(r =>
				LibraryScanner.IsSameOrNestedPath(normalizedCurrent, FolderOperations.NormalizeFolderPath(r.Path)));

			var chain = new List<(string Name, string Path)>();
			string? rootPath = matchingRoot != null ? FolderOperations.NormalizeFolderPath(matchingRoot.Path) : null;

			if (!string.IsNullOrEmpty(rootPath) && !normalizedCurrent.Equals(rootPath, StringComparison.OrdinalIgnoreCase))
			{
				var parent = Path.GetDirectoryName(normalizedCurrent);
				while (!string.IsNullOrEmpty(parent))
				{
					var normParent = FolderOperations.NormalizeFolderPath(parent);
					var dirName = Path.GetFileName(normParent);
					if (string.IsNullOrEmpty(dirName)) dirName = normParent;

					chain.Add((dirName, normParent));

					if (normParent.Equals(rootPath, StringComparison.OrdinalIgnoreCase))
						break;

					parent = Path.GetDirectoryName(normParent);
				}
				chain.Reverse();
			}
			else if (string.IsNullOrEmpty(rootPath))
			{
				var parent = Path.GetDirectoryName(normalizedCurrent);
				if (!string.IsNullOrEmpty(parent))
				{
					var normParent = FolderOperations.NormalizeFolderPath(parent);
					var dirName = Path.GetFileName(normParent);
					if (string.IsNullOrEmpty(dirName)) dirName = normParent;
					chain.Add((dirName, normParent));
				}
			}

			if (chain.Count > 0)
			{
				foreach (var ancestor in chain)
				{
					var segGrid = new Grid
					{
						BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
						Tag = ancestor.Path,
						VerticalAlignment = VerticalAlignment.Center
					};
					segGrid.Tapped += AncestorBreadcrumb_Tapped;

					var textBlock = new TextBlock
					{
						Text = ancestor.Name,
						Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
						Style = (Style)Application.Current.Resources["TitleTextBlockStyle"],
						MaxWidth = 220,
						TextTrimming = TextTrimming.CharacterEllipsis,
						VerticalAlignment = VerticalAlignment.Center
					};

					ToolTipService.SetToolTip(segGrid, ancestor.Path);
					segGrid.Children.Add(textBlock);
					BreadcrumbSegmentsPanel.Children.Add(segGrid);

					var sepIcon = new FontIcon
					{
						Glyph = "\uE974",
						Margin = new Thickness(0, 4, 0, 0),
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center
					};
					BreadcrumbSegmentsPanel.Children.Add(sepIcon);
				}

				BreadcrumbsColDef.Width = GridLength.Auto;
				HeaderColDef.Width = new GridLength(1, GridUnitType.Star);
				Grid.SetColumn(HeaderContainerGrid, 3);
				BreadcrumbSegmentsPanel.Visibility = Visibility.Visible;
			}
			else
			{
				BreadcrumbsColDef.Width = new GridLength(1, GridUnitType.Star);
				HeaderColDef.Width = new GridLength(0);
				BreadcrumbSegmentsPanel.Visibility = Visibility.Collapsed;
				Grid.SetColumn(HeaderContainerGrid, 2);
			}
		}
		catch (Exception)
		{
			BreadcrumbsColDef.Width = new GridLength(1, GridUnitType.Star);
			HeaderColDef.Width = new GridLength(0);
			BreadcrumbSegmentsPanel.Visibility = Visibility.Collapsed;
			Grid.SetColumn(HeaderContainerGrid, 2);
		}
	}

	private async Task UpdateBreadcrumbsAsync()
	{
		var rootFolders = FolderOperations.CachedRootFolders ?? await FolderOperations.GetRootFoldersHierarchicalAsync();
		UpdateBreadcrumbs(rootFolders);
	}

	private void AncestorBreadcrumb_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
	{
		if ((sender as FrameworkElement)?.Tag is string targetPath && !string.IsNullOrWhiteSpace(targetPath))
		{
			Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.SelectedFolder)] = CurrentFolderPath;
			if (App.Current.NavService is JsonNavigationService nav)
			{
				NavServiceLastParamField?.SetValue(nav, null);
			}
			bool navigated = App.Current.NavService.NavigateTo(typeof(FolderDetailPage), targetPath, false, new DrillInNavigationTransitionInfo());
			if (!navigated)
			{
				App.Current.NavService.Frame?.Navigate(typeof(FolderDetailPage), targetPath, new DrillInNavigationTransitionInfo());
			}
		}
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
		{
			if (App.Current.NavService is JsonNavigationService nav)
			{
				NavServiceSuppressNavField?.SetValue(nav, true);
				try
				{
					libraryNavigationItem.IsSelected = true;
				}
				finally
				{
					NavServiceSuppressNavField?.SetValue(nav, false);
				}
			}
			else
			{
				libraryNavigationItem.IsSelected = true;
			}
		}
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

		var subfolderItems = new List<Song>();
		if (isHierarchy)
		{
			var subfolders = await FolderOperations.GetSubFoldersAsync(CurrentFolderPath, AscOrder);
			foreach (var sf in subfolders)
			{
				subfolderItems.Add(new Song
				{
					IsFolder = true,
					Title = sf.Name,
					Path = sf.Path,
					Duration = sf.TotalDuration,
					Artists = string.Empty,
					Album = string.Empty,
					Year = string.Empty,
					Cover = string.Empty
				});
			}
		}

		var songs = await FolderOperations.GetSongsInFolderAsync(
			CurrentFolderPath,
			includeSubfolders: false,
			orderBy: Enum.Parse<SongProperty>(sortBy),
			ascending: AscOrder);

		FolderGroupSongs.Clear();
		FolderGroupSongs.AddRange(subfolderItems);
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
				availableLetters = FolderGroupSongs.Where(s => !s.IsFolder).Select(s => FirstLetter(s.Artists)).Where(c => c != "#").Distinct().OrderBy(c => c);
				hasSpecialCharacters = FolderGroupSongs.Where(s => !s.IsFolder).Any(s => FirstLetter(s.Artists) == "#");
				break;
			case "Album":
				availableLetters = FolderGroupSongs.Where(s => !s.IsFolder).Select(s => FirstLetter(s.Album)).Where(c => c != "#").Distinct().OrderBy(c => c);
				hasSpecialCharacters = FolderGroupSongs.Where(s => !s.IsFolder).Any(s => FirstLetter(s.Album) == "#");
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

		GoToSettings.Visibility = FolderGroupSongs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
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
		var items = GetCurrentViewStyle().SelectedItems.OfType<Song>().ToList();
		var songPaths = new List<string>();
		foreach (var item in items)
		{
			if (item.IsFolder)
			{
				var subSongs = await FolderOperations.GetSongsInFolderAsync(item.Path, includeSubfolders: true);
				songPaths.AddRange(subSongs.Select(s => s.Path));
			}
			else
			{
				songPaths.Add(item.Path);
			}
		}

		if (songPaths.Count > 0)
		{
			await DatabaseHelper.Instance.AddSongsToQueuedPlayingList(songPaths);
			GlobalNotification.Info($"{songPaths.Count} songs added to queue.");
		}
	}

	private async void MenuFlyoutMultiItemDelete_OnClick(object sender, RoutedEventArgs e)
	{
		var songs = GetCurrentViewStyle().SelectedItems.OfType<Song>().Where(s => !s.IsFolder).ToList();
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
			FolderOperations.InvalidateCache();
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
			var songs = listView.SelectedItems.OfType<Song>().Where(s => !s.IsFolder).ToList();
			if (songs.Count > 0)
			{
				await DatabaseHelper.Instance.AddSongsToPlaylist(playlist, songs.Select(s => s.Path).ToList());
				GlobalNotification.Info($"{songs.Count} songs added to {playlist} playlist.");
			}
		}
		else
		{
			if ((sender as MenuFlyoutItem)?.DataContext is Song song && !song.IsFolder)
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
				var songs = listView.SelectedItems.OfType<Song>().Where(s => !s.IsFolder).ToList();
				if (songs.Count > 0)
				{
					await DatabaseHelper.Instance.AddSongsToPlaylist(playlistName, songs.Select(s => s.Path).ToList());
					GlobalNotification.Info($"{songs.Count} songs added to {playlistName} playlist.");
				}
			}
			else
			{
				if ((sender as MenuFlyoutItem)?.DataContext is Song song && !song.IsFolder)
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
		int maxWait = 50;
		while ((FolderGroupSongs == null || FolderGroupSongs.Count == 0) && maxWait > 0)
		{
			await Task.Delay(100);
			maxWait--;
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
		_dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, async () =>
		{
			await AdjustAlphabetSize();
		});
	}
}
