using System.Collections.ObjectModel;
using Tunetastic.Generated.Protos;

namespace Tunetastic.Views.LibraryViews;
public sealed partial class AllSongsViewPage : Page
{
    public ObservableCollection<Song> AllSongs
    {
        get; set;
    } = new();

    public AllSongsViewPage()
    {
        this.InitializeComponent();
        AllSongs.AddRange(ProtobufData.LoadFromBin<SongList>(DataFile.AllSongsMetaData).Songs);
    }

    private void ListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        var track = e.ClickedItem as Song;
        List<string> songPaths = AllSongs.Select(s => s.Path).ToList();
        Windows.Storage.ApplicationData.Current.LocalSettings.Values["CurrentPlaylist"] = "AllSongsViewPage";
        MusicPlayer.Instance.LoadPlaylist(songPaths, track?.Path);
    }

    public void LoadAsPlayList(int index)
    {
        List<string> songPaths = AllSongs.Select(s => s.Path).ToList();
        MusicPlayer.Instance.LoadLastPlayed(songPaths, index);
    }
}
