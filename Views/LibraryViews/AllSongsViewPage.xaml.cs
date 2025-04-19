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

    private void PlayNow_Click(object sender, RoutedEventArgs e)
    {

    }
}
