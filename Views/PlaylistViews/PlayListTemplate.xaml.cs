using System.Text.RegularExpressions;
using Tunetastic.Generated.Protos;

namespace Tunetastic.Views.PlaylistViews;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class PlayListTemplate : Page
{
	public PlayListTemplate()
	{
		this.InitializeComponent();
	}

	/// <summary>
	/// Invoked when the page is navigated to, allowing for parameters to be passed and handled during navigation.
	/// </summary>
	/// <param name="e">An object that provides data about the navigation event, including the navigation parameter.</param>
	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		if (e.Parameter is DataGroup dataGroup)
		{
			PlaylistHeader.Text = dataGroup.Title;
		}
	}

	/// <summary>
	/// Handles the click event for deleting a playlist, removing it from the navigation service,
	/// internal data storage, and the UI navigation menu.
	/// </summary>
	/// <param name="sender">The source of the event, typically the button that was clicked.</param>
	/// <param name="e">An object containing event data.</param>
	private void DeletePlayList_Click(object sender, RoutedEventArgs e)
	{
		App.Current.NavService.GoBack();
		var playListTag = "Tunetastic.Views.PlaylistViews." + Regex.Replace(PlaylistHeader.Text, @"\s+", "_") + "CustomPlaylist";

		var playLists = ProtobufData.LoadFromBin<PlayListsList>(DataFile.CustomPlayLists);
		playLists.PlayListName.Remove(PlaylistHeader.Text);
		ProtobufData.SaveToBin<PlayListsList>(DataFile.CustomPlayLists, playLists);

		var a = NavigationPageMappings.PageDictionary.Remove(playListTag);

		var playlistsGroup = App.Current.NavService.MenuItems[2] as NavigationViewItem;
		foreach (NavigationViewItem item in playlistsGroup.MenuItems)
		{
			if (item.Tag.ToString() == playListTag)
			{
				playlistsGroup.MenuItems.Remove(item);
				break;
			}
		}
	}
}
