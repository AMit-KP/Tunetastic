using Windows.Media.Core;
using Windows.Media.Playback;

namespace Tunetastic.Common;

public class MusicPlayer
{
    private static MusicPlayer? _instance;
    public MediaPlayer MediaPlayer { get; private set; }

    private List<string>? OriginalPlaylist;
    private List<string>? ActualPlaylist;
    private Queue<string>? SongQueue;
    private int currentIndex = 0;
    private bool alreadyPlayed = false;

    public event EventHandler<string>? CurrentSongChanged;

    private string _currentSong = "";
    public string CurrentSong
    {
        get => _currentSong;
        set
        {
            if (_currentSong != value)
            {
                _currentSong = value;
                CurrentSongChanged?.Invoke(this, _currentSong);
            }
        }
    }

    public ShuffleMode ShuffleStatus { get; private set; } = ShuffleMode.Off;
    public RepeatMode RepeatStatus { get; private set; } = RepeatMode.All;

    private MusicPlayer()
    {
        MediaPlayer = new MediaPlayer();
        MediaPlayer.AutoPlay = false;
        SongQueue = new Queue<string>();
        MediaPlayer.MediaEnded += (s, e) => HandleTrackEnd();
    }

    public static MusicPlayer Instance
    {
        get
        {
            _instance ??= new MusicPlayer();
            return _instance;
        }
    }

    public async void LoadPlaylist(List<string> songPaths, string? startingSong = null)
    {
        await LoadSong(startingSong ?? songPaths[0]);
        _ = Task.Run(() =>
        {

            OriginalPlaylist = new List<string>(songPaths);

            ShuffleSongs(startingSong);
        });
    }

    public async void LoadLastPlayed(List<string> songPaths, int index)
    {
        _ = Task.Run(() =>
        {
            OriginalPlaylist = new List<string>(songPaths);

            ShuffleSongs();
            currentIndex = index;
        });
    }

    public void ToggleShuffle(ShuffleMode mode)
    {
        ShuffleStatus = mode;
        if (ActualPlaylist?.Count > 0) ShuffleSongs(ActualPlaylist[currentIndex]);
    }

    public void SetRepeatMode(RepeatMode mode)
    {
        RepeatStatus = mode;
    }

    public void AddToQueue(string songPath) => SongQueue?.Enqueue(songPath);        //TODO queue system

    public async void LoadSong()
    {
        if (ActualPlaylist?.Count > 0)
        {
            await LoadSong(ActualPlaylist[currentIndex]);
        }
    }

    public async Task LoadSong(string songPath, bool play = true)
    {
        try
        {
            //if (MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing) return; // Prevent restarting      //TODO add settings for this

            //await CrossfadeTransition(ActualPlaylist[currentIndex]);          //TODO get settings
            MediaPlayer.Source = MediaSource.CreateFromUri(new Uri(songPath));
            if (play) MediaPlayer.Play();
            CurrentSong = songPath;
            Windows.Storage.ApplicationData.Current.LocalSettings.Values["LastPlayed"] = CurrentSong;
        }
        catch (Exception)
        {
            GlobalNotification.Error("Could not load song:\n" + songPath);
        }
    }


    public async void Pause()
    {
        //TODO get settings for this
        //await CrossfadePause();
        MediaPlayer.Pause();
    }

    public async void Play()
    {
        //TODO get settings for this
        //await CrossfadePause();
        MediaPlayer.Play();
    }


    public async void Previous()
    {
        //TODO get settings for this restart or prev
        try
        {
            currentIndex = currentIndex == 0 ? ActualPlaylist.Count - 1 : currentIndex - 1;
            LoadSong();
        }
        catch (Exception)
        {
            GlobalNotification.Error("Could not load previous song");
        }
    }

    public async void Next()
    {
        try
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
                        await CrossfadePause();
                        return;
                    }
                }
                else if (RepeatStatus == RepeatMode.All)
                {
                    LoadPlaylist(OriginalPlaylist);
                }
                else if (RepeatStatus == RepeatMode.None)
                {
                    await CrossfadePause();
                    return;
                }
            }

            LoadSong();
        }
        catch (Exception)
        {
            GlobalNotification.Error("Could not load next song");
        }
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

    private MediaPlayer MediaPlayerNext = new MediaPlayer(); // Second player for blending

    private async Task CrossfadeTransition(string songPath, double fadeTime)
    {
        double initialVolume = MediaPlayer.Volume;

        // Start next track on second player, but muted
        MediaPlayerNext.Source = MediaSource.CreateFromUri(new Uri(songPath));
        MediaPlayerNext.Volume = 0;
        MediaPlayerNext.Play();

        // Gradually lower the volume of the current track while raising the new one
        for (double i = 0; i < fadeTime; i += 0.05)
        {
            MediaPlayer.Volume = initialVolume * (1 - (i / fadeTime));  // Fade out current song
            MediaPlayerNext.Volume = initialVolume * (i / fadeTime);  // Fade in new song
            await Task.Delay(50);
        }

        // Stop previous player and transfer control to the new one
        MediaPlayer.Pause();
        MediaPlayer = MediaPlayerNext;
        MediaPlayerNext = new MediaPlayer(); // Reset second player
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

        try
        {
            MediaPlayer.Source = MediaSource.CreateFromUri(new Uri(songPath));
            MediaPlayer.Play();
        }
        catch (Exception)
        {
            //TODO error notification
            MediaPlayer.Volume = volume;
            Next();

        }

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

    private void HandleTrackEnd()
    {
        Next();
    }

    public void SavePlayBackPosition()
    {
        Windows.Storage.ApplicationData.Current.LocalSettings.Values["PlayBackPosition"] = MediaPlayer.PlaybackSession.Position.TotalSeconds.ToString();
        Windows.Storage.ApplicationData.Current.LocalSettings.Values["CurrentIndex"] = currentIndex.ToString();
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
