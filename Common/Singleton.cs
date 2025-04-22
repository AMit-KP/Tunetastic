using Nucs.JsonSettings;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace Tunetastic.Common;

public sealed class LibrarySettingsSaver
{
    private static readonly Lazy<LibrarySettingsSaver> _instance =
        new(() => new LibrarySettingsSaver());

    public static LibrarySettingsSaver Instance => _instance.Value;

    public LibrarySettings LibrarySaveSettings { get; }

    private LibrarySettingsSaver()
    {
        LibrarySaveSettings = JsonSettings.Load<LibrarySettings>();
    }

    public void SaveSettings()
    {
        LibrarySaveSettings.Save();
    }
}

public class MusicPlayer
{
    private static MusicPlayer? _instance;
    public MediaPlayer MediaPlayer { get; private set; }

    private List<string>? OriginalPlaylist;
    private List<string>? ActualPlaylist;
    private Queue<string>? SongQueue;
    private int currentIndex = 0;
    private bool alreadyPlayed = false;

    public ShuffleMode ShuffleStatus { get; private set; } = ShuffleMode.Off;
    public RepeatMode RepeatStatus { get; private set; } = RepeatMode.None;

    private MusicPlayer()
    {
        MediaPlayer = new MediaPlayer();
        SongQueue = new Queue<string>();
        MediaPlayer.MediaEnded += (s, e) => HandleTrackEnd();
    }

    public static MusicPlayer Instance
    {
        get
        {
            if (_instance == null)
                _instance = new MusicPlayer();
            return _instance;
        }
    }

    public void LoadPlaylist(List<string> songPaths, string? startingSong = null)
    {
        OriginalPlaylist = new List<string>(songPaths);

        ShuffleSongs(startingSong);

        Play();
    }

    public void ToggleShuffle(ShuffleMode mode)
    {
        ShuffleStatus = mode;
        ShuffleSongs(ActualPlaylist[currentIndex]);
    }
    public void SetRepeatMode(RepeatMode mode)
    {
        RepeatStatus = mode;
    }

    public void AddToQueue(string songPath) => SongQueue?.Enqueue(songPath);        //TODO queue system

    public async void Play()
    {
        //if (MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing) return; // Prevent restarting      //TODO add settings for this

        await CrossfadeTransition(ActualPlaylist[currentIndex]);
    }

    public async void Pause()
    {
        //get settings for this
        await CrossfadePause();
        //MediaPlayer.Pause();
    }
    public async void Previous()
    {
        //TODO get settings for this restart or prev
        currentIndex = currentIndex == 0 ? ActualPlaylist.Count - 1 : currentIndex - 1;
        Play();
    }

    public async void Next()
    {
        if (SongQueue?.Count > 0)
        {
            await CrossfadeTransition(SongQueue.Dequeue());
            return;
        }

        if (currentIndex < OriginalPlaylist?.Count - 1)
        {
            currentIndex++;
        }
        else
        {
            if (RepeatStatus == RepeatMode.One)
            {

                if (!alreadyPlayed)
                {
                    currentIndex = 0;
                    alreadyPlayed = true;
                }
                else
                {
                    await CrossfadeStop();
                    return;
                }
            }
            else if (RepeatStatus == RepeatMode.All)
            {
                LoadPlaylist(OriginalPlaylist);
            }
            else if (RepeatStatus == RepeatMode.None)
            {
                await CrossfadeStop();
                return;
            }
        }

        Play();
    }

    private void ReorderPlaylist(string startingSong)
    {
        var selectedIndex = ActualPlaylist.IndexOf(startingSong);
        if (selectedIndex > 0 && selectedIndex < ActualPlaylist?.Count)
        {
            var reordered = ActualPlaylist.Skip(selectedIndex)
                                            .Concat(ActualPlaylist.Take(selectedIndex))
                                            .ToList();
            ActualPlaylist = reordered;
        }
        currentIndex = 0;
    }

    private void ShuffleSongs(string? startingSong = null)
    {
        currentIndex = startingSong != null ? OriginalPlaylist.IndexOf(startingSong) : 0;

        if (ShuffleStatus == ShuffleMode.On && OriginalPlaylist?.Count > 2)
        {
            Random rng = new Random();
            ActualPlaylist = OriginalPlaylist?.OrderBy(_ => rng.Next()).ToList();
            if (startingSong != null)
                ReorderPlaylist(startingSong);
        }
        else
        {
            ActualPlaylist = OriginalPlaylist;
        }
    }
    private async Task CrossfadeTransition(string songPath)
    {
        //TODO get settings for time
        double volume = MediaPlayer.Volume;

        // Fade out current track
        for (double i = volume; i > 0; i -= 0.05)
        {
            MediaPlayer.Volume = i;
            await Task.Delay(50);
        }

        MediaPlayer.Source = MediaSource.CreateFromUri(new Uri(songPath));
        MediaPlayer.Play();

        // Fade in new track
        for (double i = 0; i <= volume; i += 0.05)
        {
            MediaPlayer.Volume = i;
            await Task.Delay(50);
        }
    }
    private async Task CrossfadePause()
    {
        //TODO get settings for time
        double volume = MediaPlayer.Volume;
        for (double i = volume; i > 0; i -= 0.05)
        {
            MediaPlayer.Volume = i;
            await Task.Delay(50);
        }

        MediaPlayer.Pause();
        MediaPlayer.Volume = volume;
    }
    private async Task CrossfadeStop()
    {
        //TODO get settings for time
        for (double i = MediaPlayer.Volume; i > 0; i -= 0.05)
        {
            MediaPlayer.Volume = i;
            await Task.Delay(100);
        }
        MediaPlayer.Pause();
    }

    private void HandleTrackEnd()
    {
        Next();
    }
}

public enum RepeatMode
{
    None,
    One,
    All
}

public enum ShuffleMode
{
    Off,
    On
}
