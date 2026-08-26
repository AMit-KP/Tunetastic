using System.Collections.ObjectModel;

namespace Tunetastic.ViewModels;

/// <summary>
/// View model backing the shared song-list pages. Owns the displayed song collection and
/// selection state, plus the Play All / Shuffle &amp; Play playback logic. View mechanics
/// (scroll-into-view, animations, menus) remain in the page code-behind.
/// </summary>
public partial class SongListViewModel : ObservableObject
{
	/// <summary>The songs the page currently displays, in display order.</summary>
	public ObservableCollection<Song> Songs { get; } = new();

#pragma warning disable MVVMTK0045 // Using [ObservableProperty] on fields is not AOT compatible for WinRT
	/// <summary>Currently selected song in single-select mode.</summary>
	[ObservableProperty]
	public Song? selectedSong;
#pragma warning restore MVVMTK0045 // Using [ObservableProperty] on fields is not AOT compatible for WinRT

	/// <summary>
	/// Disables shuffle, stores <paramref name="playlistKey"/> as the current playing list and loads
	/// every displayed song into the player. Returns the first song for the view to scroll to.
	/// </summary>
	public Song PlayAll(string playlistKey)
	{
		MusicPlayer.Instance.ToggleShuffle(ShuffleMode.Off);
		List<string> songPaths = Songs.Select(s => s.Path).ToList();
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CurrentPlayinglist)] = playlistKey;
		MusicPlayer.Instance.LoadPlaylist(songPaths);
		return Songs[0];
	}

	/// <summary>
	/// Enables shuffle, stores <paramref name="playlistKey"/> as the current playing list and loads
	/// every displayed song into the player starting from a randomly chosen song. Returns that song
	/// for the view to scroll to.
	/// </summary>
	public Song? ShuffleAndPlay(string playlistKey)
	{
		MusicPlayer.Instance.ToggleShuffle(ShuffleMode.On);
		List<string> songPaths = Songs.Select(s => s.Path).ToList();
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CurrentPlayinglist)] = playlistKey;
		var startingSong = songPaths[new Random().Next(songPaths.Count)];
		MusicPlayer.Instance.LoadPlaylist(songPaths, startingSong);
		return Songs.Select(s => s).Where(s => s.Path == startingSong).FirstOrDefault();
	}
}
