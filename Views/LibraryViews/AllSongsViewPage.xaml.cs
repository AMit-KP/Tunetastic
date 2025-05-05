using System.Collections.ObjectModel;
using CommunityToolkit.WinUI;
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
    /// Loads the collection of all available songs as a playlist and starts preparing them for playback.
    /// </summary>
    /// <remarks>
    /// This method retrieves the file paths of all songs in the collection and initializes a playlist within the music player.
    /// It ensures that the songs are ready for playback and begins with the specified track as the currently active item.
    /// </remarks>
    public void LoadAsPlayList()
    {
        List<string> songPaths = AllSongs.Select(s => s.Path).ToList();
        MusicPlayer.Instance.LoadLastPlayed(songPaths);
    }

    /// <summary>
    /// Scrolls to a specific song in the `AllSongsListView`.
    /// </summary>
    /// <param name="song">The song object to scroll to. If null, no action is performed.</param>
    /// <returns>A task representing the asynchronous operation of scrolling to the specified song.</returns>
    private async Task ScrollToSong(Song? song)
    {
        if (song != null)
        {
            await AllSongsListView.SmoothScrollIntoViewWithItemAsync(song, itemPlacement: ScrollItemPlacement.Center, disableAnimation: false, scrollIfVisible: false);
            AllSongsListView.SelectedItem = song;
        }

    }

    /// <summary>
    /// Handles the Loaded event for the AllSongsViewPage.
    /// </summary>
    /// <param name="sender">The source of the event, typically the page itself.</param>
    /// <param name="e">The event data associated with the Loaded event.</param>
    /// <remarks>
    /// This method verifies if the current playlist corresponds to "AllSongsViewPage" by accessing the application's
    /// local settings. If the last played song is found in the local settings, it attempts to scroll to that song
    /// within the songs list. The scrolling operation is performed asynchronously with a slight delay.
    /// </remarks>
    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        if (localSettings.Values[nameof(LocalSave.CurrentPlaylist)]?.ToString() == "AllSongsViewPage")
        {
            var SelectedSong = AllSongs.Select(s => s).Where(s => s.Path == localSettings.Values[nameof(LocalSave.LastPlayedTrack)]?.ToString()).FirstOrDefault();
            _ = ScrollToSong(SelectedSong);
        }
    }

    private void SortButton_OnClick(object sender, RoutedEventArgs e)
    {

    }

    private void GroupButton_OnClick(object sender, RoutedEventArgs e)
    {

    }

    private void ViewButton_OnClick(object sender, RoutedEventArgs e)
    {

    }

    /// <summary>
    /// Handles the click event of the "Shuffle and Play" button to shuffle the song list
    /// and begin playback from a randomly selected song.
    /// </summary>
    /// <param name="sender">The source of the click event, typically the "Shuffle and Play" button.</param>
    /// <param name="e">Provides data about the click event.</param>
    /// <remarks>
    /// This method disables the button to prevent repeated triggers, enables shuffle mode on the music player,
    /// and retrieves the list of song paths to shuffle and load as a playlist. It then randomly selects a starting song
    /// from the playlist and scrolls to that song in the user interface. After a brief delay, it ensures that the song
    /// is properly scrolled into view and re-enables the button.
    /// </remarks>
    private async void ShuffleAndPlayButton_OnClick(object sender, RoutedEventArgs e)
    {
        ShuffleAndPlay.IsEnabled = false;
        MusicPlayer.Instance.ToggleShuffle(ShuffleMode.On);
        List<string> songPaths = AllSongs.Select(s => s.Path).ToList();

        var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        localSettings.Values[nameof(LocalSave.CurrentPlaylist)] = "AllSongsViewPage";

        var startingSong = songPaths[new Random().Next(songPaths.Count)];
        MusicPlayer.Instance.LoadPlaylist(songPaths, startingSong);
        var SelectedSong = AllSongs.Select(s => s).Where(s => s.Path == startingSong).FirstOrDefault();
        await ScrollToSong(SelectedSong);       //somehow this doesn't work
        await Task.Delay(500);
        await ScrollToSong(SelectedSong);
        ShuffleAndPlay.IsEnabled = true;
    }

    /// <summary>
    /// Handles the click event for the "Play All" button and initiates playback of all songs in the current view.
    /// </summary>
    /// <param name="sender">The source of the event, typically the "Play All" button.</param>
    /// <param name="e">Provides data for the routed event that triggered the method.</param>
    /// <remarks>
    /// This method disables shuffle mode, creates a playlist from all songs in the current view,
    /// stores the name of the current playlist in application settings, and starts playing the songs in order.
    /// It also scrolls to the first song in the playlist after initiating playback.
    /// </remarks>
    private async void PlayAllButton_OnClick(object sender, RoutedEventArgs e)
    {
        ShuffleAndPlay.IsEnabled = false;
        MusicPlayer.Instance.ToggleShuffle(ShuffleMode.Off);
        List<string> songPaths = AllSongs.Select(s => s.Path).ToList();
        var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        localSettings.Values[nameof(LocalSave.CurrentPlaylist)] = "AllSongsViewPage";
        MusicPlayer.Instance.LoadPlaylist(songPaths);
        await ScrollToSong(AllSongs[0]);
        ShuffleAndPlay.IsEnabled = true;
    }
}
