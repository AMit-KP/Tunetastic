using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Tunetastic.Common;
using Tunetastic.Common.Operations;
using Tunetastic.Views.Common;

namespace Tunetastic.Views.LibraryViews;

/// <summary>
/// Mode 1 (All Music Folders): Displays a flat listing of all folders containing music files.
/// </summary>
public sealed partial class FoldersViewPage : FolderPageBase
{
	public override string PageModeName => "All Music Folders";

	/// <inheritdoc/>
	protected override Button ShuffleAndPlayControl => ShuffleAndPlay;

	/// <inheritdoc/>
	protected override Button MoreButtonControl => MoreButton;

	public FoldersViewPage()
	{
		this.InitializeComponent();
		_ = RunScanningAwareInit();
	}

	/// <inheritdoc/>
	protected override Page CreateFreshPage() => new FoldersViewPage();

	/// <inheritdoc/>
	protected override async Task LoadRootFoldersAsync(bool ascOrder)
	{
		var folders = await FolderOperations.GetFoldersFlatAsync(ascOrder);
		RootFolders.Clear();
		RootFolders.AddRange(folders);
	}

	/// <inheritdoc/>
	protected override async Task LoadFolderContentsAsync(string folderPath, SongProperty sortBy, bool ascOrder)
	{
		var songs = await FolderOperations.GetSongsInFolderAsync(folderPath, includeSubfolders: false, orderBy: sortBy, ascending: ascOrder);
		FolderGroupSongs.Clear();
		FolderGroupSongs.AddRange(songs);
	}

	/// <inheritdoc/>
	protected override void ModeButton_OnClick(object sender, RoutedEventArgs e)
	{
		if (FolderHierarchyViewStyle.IsChecked)
		{
			Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.FoldersViewMode)] = "Folder Hierarchy";
			NavigationPageMappings.PageDictionary["Tunetastic.Views.LibraryViews.FoldersViewPage"] = typeof(FolderHierarchyPage);
			App.Current.NavService.NavigateTo(typeof(FolderHierarchyPage), CurrentFolderPath);
		}
	}
}
