using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Tunetastic.Common;
using Tunetastic.Common.Operations;
using Tunetastic.Common.Services;
using Tunetastic.Views.Common;

namespace Tunetastic.Views.LibraryViews;

/// <summary>
/// Mode 2 (Folder Hierarchy): Displays root music folders and allows hierarchical browsing,
/// showing subfolders 1st and music tracks 2nd like a file explorer.
/// </summary>
public sealed partial class FolderHierarchyPage : FolderPageBase
{
	public override string PageModeName => "Folder Hierarchy";

	/// <inheritdoc/>
	protected override Button ShuffleAndPlayControl => ShuffleAndPlay;

	/// <inheritdoc/>
	protected override Button MoreButtonControl => MoreButton;

	/// <summary>
	/// Gets or sets the collection of subfolder tiles displayed when drilled inside a folder.
	/// </summary>
	public ObservableCollection<FolderModel> SubFolders { get; set; } = new();

	public FolderHierarchyPage()
	{
		this.InitializeComponent();
		_ = RunScanningAwareInit();
	}

	/// <inheritdoc/>
	protected override Page CreateFreshPage() => new FolderHierarchyPage();

	/// <inheritdoc/>
	protected override async Task LoadRootFoldersAsync(bool ascOrder)
	{
		var folders = await FolderOperations.GetRootFoldersHierarchicalAsync(ascOrder);
		RootFolders.Clear();
		RootFolders.AddRange(folders);
	}

	/// <inheritdoc/>
	protected override async Task LoadFolderContentsAsync(string folderPath, SongProperty sortBy, bool ascOrder)
	{
		// 1st: Subfolders
		var subfolders = await FolderOperations.GetSubFoldersAsync(folderPath, ascOrder);
		SubFolders.Clear();
		SubFolders.AddRange(subfolders);
		SubFoldersSection.Visibility = SubFolders.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

		// 2nd: Songs directly in this folder
		var songs = await FolderOperations.GetSongsInFolderAsync(folderPath, includeSubfolders: false, orderBy: sortBy, ascending: ascOrder);
		FolderGroupSongs.Clear();
		FolderGroupSongs.AddRange(songs);
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

	/// <inheritdoc/>
	protected override void ModeButton_OnClick(object sender, RoutedEventArgs e)
	{
		if (AllMusicFoldersViewStyle.IsChecked)
		{
			Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.FoldersViewMode)] = "All Music Folders";
			App.Current.NavService.NavigateTo(typeof(FoldersViewPage), CurrentFolderPath);
		}
	}
}

