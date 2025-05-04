using System.Collections.ObjectModel;
using Tunetastic.Generated.Protos;

namespace Tunetastic.Views.LibraryViews;

/// <summary>
/// Represents a page in the application that displays a collection of all available songs.
/// </summary>
/// <remarks>
/// This class is a part of the library view system in the application and is designed to work with the application's song data.
/// It initializes a collection of songs upon construction and provides functionality to load the song collection as a playlist for playback.
/// </remarks>
public sealed partial class AllSongsViewPage : Page
{
    /// <summary>
    /// Gets or sets the collection of all songs available in the application.
    /// </summary>
    /// <remarks>
    /// The <c>AllSongs</c> property holds an observable collection of <c>Song</c> objects, representing the full list
    /// of songs loaded from the application's data source. This property is primarily used to populate the user interface
    /// and manage interactions with the song list.
    /// The collection is initialized and populated when the page instance is created. This property is also bound to
    /// the <c>ListView</c> in the associated XAML to display the songs in the UI, allowing users to interact with
    /// individual items.
    /// </remarks>
    public ObservableCollection<Song> AllSongs
    {
        get; set;
    } = new();

    /// <summary>
    /// Represents a page for displaying and managing all available songs in the application.
    /// </summary>
    /// <remarks>
    /// This page initializes a list of all available songs by reading metadata from a binary data file.
    /// It provides features to interact with the song collection, such as loading the songs as a playlist for playback.
    /// </remarks>
    public AllSongsViewPage()
    {
        this.InitializeComponent();
        AllSongs.AddRange(ProtobufData.LoadFromBin<SongList>(DataFile.AllSongsMetaData).Songs);
    }

    /// <summary>
    /// Handles the ItemClick event for the ListView control in the AllSongsViewPage.
    /// </summary>
    /// <param name="sender">The source of the event, typically the ListView control.</param>
    /// <param name="e">Provides data for the ItemClick event, including the clicked item.</param>
    /// <remarks>
    /// This method is triggered when a user clicks an item in the song list. It retrieves the clicked song,
    /// generates a playlist from the current collection of songs, and loads the clicked song into the music player for playback.
    /// The playlist is also saved as the current playlist in the application's local settings.
    /// </remarks>
    private void ListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        var track = e.ClickedItem as Song;
        List<string> songPaths = AllSongs.Select(s => s.Path).ToList();
        Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CurrentPlaylist)] = "AllSongsViewPage";
        MusicPlayer.Instance.LoadPlaylist(songPaths, track?.Path);
    }

    /// <summary>
    /// Loads the collection of all available songs as a playlist and sets the active song at the specified index.
    /// </summary>
    /// <param name="index">The index of the song to be set as the currently active track in the playlist.</param>
    /// <remarks>
    /// This method retrieves the paths of all songs currently available in the collection
    /// and initializes the playback with the specified active song index.
    /// </remarks>
    public void LoadAsPlayList(int index)
    {
        List<string> songPaths = AllSongs.Select(s => s.Path).ToList();
        MusicPlayer.Instance.LoadLastPlayed(songPaths, index);
    }
}
