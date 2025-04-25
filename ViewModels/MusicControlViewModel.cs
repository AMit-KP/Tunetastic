using Microsoft.UI.Dispatching;
using Windows.Media.Playback;


namespace Tunetastic.ViewModels;

public partial class MusicControlViewModel : ObservableRecipient
{
    private readonly DispatcherQueue _dispatcherQueue;

    private bool isUpdatingProgressBar = false;

    private readonly MusicPlayer _musicPlayer = MusicPlayer.Instance;

    private string _fontIconPlayPause = "\uE768";
    public string FontIconPlayPause
    {
        get => _fontIconPlayPause;
        set => SetProperty(ref _fontIconPlayPause, value);
    }

    private string _repeatButtonFontIcon = "\uE8EE";
    public string RepeatButtonFontIcon
    {
        get => _repeatButtonFontIcon;
        set => SetProperty(ref _repeatButtonFontIcon, value);
    }

    private double _progressBarValue;
    public double ProgressBarValue
    {
        get => _progressBarValue;
        set
        {
            if (_progressBarValue != value)
            {
                _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () =>
                {
                    _progressBarValue = value;
                    try
                    {
                        OnPropertyChanged(nameof(ProgressBarValue));
                    }
                    catch (Exception)
                    {
                    }
                });

                if (!isUpdatingProgressBar)
                {
                    UpdatePlaybackPosition();
                }
            }
        }
    }

    private double _durationOfSong;
    public double DurationOfSong
    {
        get => _durationOfSong;
        set => SetProperty(ref _durationOfSong, value);
    }

    private bool _isShuffleToggled = false;
    public bool IsShuffleToggled
    {
        get => _isShuffleToggled;
        set => SetProperty(ref _isShuffleToggled, value);
    }

    private Style _repeatButtonStyle = (Style)Application.Current.Resources["AccentButtonStyle"];
    public Style RepeatButtonStyle
    {
        get => _repeatButtonStyle;
        set => SetProperty(ref _repeatButtonStyle, value);
    }

    private string _toolTipTextPlayPause = "Play";
    public string ToolTipTextPlayPause
    {
        get => _toolTipTextPlayPause;
        set => SetProperty(ref _toolTipTextPlayPause, value);
    }

    private string _toolTipTextShuffleButton = "Shuffle Off";
    public string ToolTipTextShuffleButton
    {
        get => _toolTipTextShuffleButton;
        set => SetProperty(ref _toolTipTextShuffleButton, value);
    }

    private string _toolTipTextRepeatButton = "Repeat All";
    public string ToolTipTextRepeatButton
    {
        get => _toolTipTextRepeatButton;
        set => SetProperty(ref _toolTipTextRepeatButton, value);
    }

    public MusicControlViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        _musicPlayer.MediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;

        _musicPlayer.MediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;

        _musicPlayer.MediaPlayer.MediaOpened += PlaybackSession_MediaOpenedAsync;
        //_musicPlayer.MediaPlayer.VolumeChanged += PlaybackSession_VolumeChanged;  //TODO pause on mute

        var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        if (localSettings.Values.ContainsKey("ShuffleStatus"))
            ShuffleToggle((bool)localSettings.Values["ShuffleStatus"]);

        if (localSettings.Values.ContainsKey("RepeatStatus"))
            RepeatButtonToggle(Enum.Parse<RepeatMode>(localSettings.Values["RepeatStatus"]?.ToString()));

        if (localSettings.Values.ContainsKey("LastPlayed"))
        {
            _musicPlayer.LoadSong(localSettings.Values["LastPlayed"].ToString(), play: false);          //TODO get settings

            if (localSettings.Values.ContainsKey("PlayBackPosition"))
                ProgressBarValue = double.Parse(localSettings.Values["PlayBackPosition"].ToString());
        }
    }

    [RelayCommand]
    private void TogglePlayPause()
    {
        if (_musicPlayer.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
        {
            _musicPlayer.Pause();
        }
        else if (_musicPlayer.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Paused)
        {
            _musicPlayer.Play();
        }
    }

    private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args) => _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () =>
    {
        switch (_musicPlayer.MediaPlayer.PlaybackSession.PlaybackState)
        {
            case MediaPlaybackState.Paused:
            case MediaPlaybackState.None:
                FontIconPlayPause = "\uE768";
                ToolTipTextPlayPause = "Play";
                break;

            case MediaPlaybackState.Playing:
            case MediaPlaybackState.Buffering:
            case MediaPlaybackState.Opening:
                FontIconPlayPause = "\uE769";
                ToolTipTextPlayPause = "Pause";
                break;
        }
    });


    [RelayCommand]
    private void NextSong() => _musicPlayer.Next();

    [RelayCommand]
    private void PreviousSong() => _musicPlayer.Previous();


    [RelayCommand]
    private void ForwardSong() => ProgressBarValue++;

    [RelayCommand]
    private void RewindSong() => ProgressBarValue--;


    [RelayCommand]
    private void ShuffleToggle(bool? shuffleSaved = null)
    {
        IsShuffleToggled = shuffleSaved ?? IsShuffleToggled;
        _musicPlayer.ToggleShuffle(IsShuffleToggled ? ShuffleMode.On : ShuffleMode.Off);
        ToolTipTextShuffleButton = IsShuffleToggled ? "Shuffle On" : "Shuffle Off";
        Windows.Storage.ApplicationData.Current.LocalSettings.Values["ShuffleStatus"] = IsShuffleToggled;
    }

    [RelayCommand]
    private void RepeatButtonToggle(RepeatMode? repeatSaved = null)
    {
        RepeatMode repeatMode = RepeatMode.All;
        if (repeatSaved != null)
            repeatMode = repeatSaved.Value;
        else
        {
            if (_musicPlayer.RepeatStatus == RepeatMode.All)
            {
                repeatMode = RepeatMode.One;
            }
            else if (_musicPlayer.RepeatStatus == RepeatMode.One)
            {
                repeatMode = RepeatMode.None;
            }
            else if (_musicPlayer.RepeatStatus == RepeatMode.None)
            {
                repeatMode = RepeatMode.All;
            }
        }

        switch (repeatMode)
        {
            case RepeatMode.All:
                _musicPlayer.SetRepeatMode(RepeatMode.All);
                RepeatButtonFontIcon = "\uE8EE";
                ToolTipTextRepeatButton = "Repeat All";
                RepeatButtonStyle = (Style)Application.Current.Resources["AccentButtonStyle"];
                break;
            case RepeatMode.One:
                _musicPlayer.SetRepeatMode(RepeatMode.One);
                RepeatButtonFontIcon = "\uE8ED";
                ToolTipTextRepeatButton = "Repeat One";
                RepeatButtonStyle = (Style)Application.Current.Resources["AccentButtonStyle"];
                break;
            case RepeatMode.None:
                _musicPlayer.SetRepeatMode(RepeatMode.None);
                RepeatButtonFontIcon = "\uF5E7";
                ToolTipTextRepeatButton = "Repeat Off";
                RepeatButtonStyle = (Style)Application.Current.Resources["DefaultButtonStyle"];
                break;
        }
        Windows.Storage.ApplicationData.Current.LocalSettings.Values["RepeatStatus"] = _musicPlayer.RepeatStatus.ToString();
    }

    private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args) => _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, async () =>
    {
        isUpdatingProgressBar = true;
        ProgressBarValue = sender.Position.TotalSeconds;
        await Task.Delay(1);
        isUpdatingProgressBar = false;
    });

    public async void UpdatePlaybackPosition()
    {
        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, async () =>
        {
            _musicPlayer.MediaPlayer.IsMuted = true;
            _musicPlayer.MediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(ProgressBarValue);
            await Task.Delay(500);
            _musicPlayer.MediaPlayer.IsMuted = false;
        });
    }

    private async void PlaybackSession_MediaOpenedAsync(MediaPlayer sender, object args)
    {
        await Task.Delay(100);
        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
        {
            DurationOfSong = _musicPlayer.MediaPlayer.PlaybackSession.NaturalDuration.TotalSeconds;
        });
    }


}

