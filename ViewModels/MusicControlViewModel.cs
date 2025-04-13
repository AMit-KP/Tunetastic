using Windows.Media.Playback;
using Microsoft.UI.Dispatching;


namespace Tunetastic.ViewModels;

public partial class MusicControlViewModel : ObservableRecipient
{
    private readonly DispatcherQueue _dispatcherQueue;

    [ObservableProperty]
    public string fontIconPlayPause;

    [ObservableProperty]
    private bool isShuffleToggled;

    [ObservableProperty]
    public MediaPlaybackState playbackState;

    [ObservableProperty]
    public string repeatButtonFontIcon;

    private RepeatStates repeatState = RepeatStates.Off;

    [ObservableProperty]
    public string toolTipTextPlayPause;

    [ObservableProperty]
    public string toolTipTextRepeatButton;

    [ObservableProperty]
    public string toolTipTextShuffleButton;

    [ObservableProperty]
    public double durationOfSong;

    private bool isUpdatingProgressBar = false;

    public double progressBarValue;

    public double ProgressBarValue
    {
        get => progressBarValue;
        set
        {
            if (progressBarValue != value)
            {
                progressBarValue = value;
                OnPropertyChanged(nameof(ProgressBarValue));
                if (!isUpdatingProgressBar)
                {
                    UpdatePlaybackPosition();
                }
            }
        }
    }

    public MusicControlViewModel()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        //NowPlaying.Instance.CreateAudioInstance(@"C:\Users\amitp\Music\Soundtracks and Instrumentals\Avatar's Love - Samuel Kim.mp3");
        //MediaList.Instance.AddFiles(new List<string> { @"C:\Users\amitp\Music\Soundtracks and Instrumentals\Avatar's Love - Samuel Kim.mp3" });
        //MediaPlayerManager.Instance.MediaPlayerInstance.Source = MediaSource.CreateFromUri(new Uri(@"C:\Users\amitp\Music\Soundtracks and Instrumentals\Avatar's Love - Samuel Kim.mp3"));
        //MediaPlayerManager.Instance.MediaPlayerInstance.Source = MediaList.Instance.MediaPlaybackList;

        //playbackState = MediaPlayerManager.Instance.MediaPlayerInstance.PlaybackSession.PlaybackState;


        //new MediaPlayerElement().SetMediaPlayer(MediaPlayerManager.Instance.MediaPlayerInstance);
        
        //MediaPlayerManager.Instance.MediaPlayerInstance.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
        //MediaPlayerManager.Instance.MediaPlayerInstance.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
        
        RepeatButtonFontIcon = "\uF5E7";
        FontIconPlayPause = "\uE768";
        ToolTipTextRepeatButton = "Repeat Off";
        IsShuffleToggled = false;
        ToolTipTextShuffleButton = "Shuffle Off";
        
        //MediaList.Instance.MediaPlaybackList.ItemOpened += MediaPlaybackList_ItemOpened;
        //MediaList.Instance.MediaPlaybackList.ItemFailed += MediaPlaybackList_ItemFailed;
    }

    private void MediaPlaybackList_ItemFailed(MediaPlaybackList sender, MediaPlaybackItemFailedEventArgs args)
    {
        throw new NotImplementedException();    //TODO
    }

    private void MediaPlaybackList_ItemOpened(MediaPlaybackList sender, MediaPlaybackItemOpenedEventArgs args) => _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, async () =>
    {
        await UpdateUI();
    });

    private void MediaPlaybackList_CurrentItemChanged(MediaPlaybackList sender, CurrentMediaPlaybackItemChangedEventArgs args)
    {
        throw new NotImplementedException();
    }

    private async Task UpdateUI()
    {
        await Task.Delay(500);
        //DurationOfSong = MediaPlayerManager.Instance.MediaPlayerInstance.PlaybackSession.NaturalDuration.TotalSeconds;
    }

    private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args) => _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, async () =>
    {
        isUpdatingProgressBar = true;
        ProgressBarValue = sender.Position.TotalSeconds;
        await Task.Delay(1);
        isUpdatingProgressBar = false;
    });

    public async void UpdatePlaybackPosition()
    {
        //MediaPlayerManager.Instance.MediaPlayerInstance.IsMuted = true;
        //MediaPlayerManager.Instance.MediaPlayerInstance.PlaybackSession.Position = TimeSpan.FromSeconds(ProgressBarValue);
        //await Task.Delay(500);
        //MediaPlayerManager.Instance.MediaPlayerInstance.IsMuted = false;
    }

    private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args) => _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
    {
        //switch (MediaPlayerManager.Instance.MediaPlayerInstance.PlaybackSession.PlaybackState)
        //{
        //    case MediaPlaybackState.Paused:
        //        FontIconPlayPause    = "\uE768";
        //        ToolTipTextPlayPause = "Play";
        //        break;

        //    case MediaPlaybackState.Playing:
        //        FontIconPlayPause    = "\uE769";
        //        ToolTipTextPlayPause = "Pause";
        //        break;

        //    case MediaPlaybackState.Buffering:
        //        FontIconPlayPause    = "\uE768";
        //        ToolTipTextPlayPause = "Play";
        //        break;

        //    case MediaPlaybackState.None:
        //    case MediaPlaybackState.Opening:
        //        break;
        //}
    });

    [RelayCommand]
    private void TogglePlayPause()
    {
        //if (MediaPlayerManager.Instance.MediaPlayerInstance.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
        //{
        //    MediaPlayerManager.Instance.MediaPlayerInstance.Pause();
        //}
        //else if (MediaPlayerManager.Instance.MediaPlayerInstance.PlaybackSession.PlaybackState == MediaPlaybackState.Paused)
        //{
        //    MediaPlayerManager.Instance.MediaPlayerInstance.Play();
        //}
    }

    [RelayCommand]
    private void Shuffle()
    {
        if (IsShuffleToggled)
        {
            ToolTipTextShuffleButton = "Shuffle On";
            //MediaList.Instance.MediaPlaybackList.ShuffleEnabled = true;
        }
        else
        {
            ToolTipTextShuffleButton = "Shuffle Off";
            //MediaList.Instance.MediaPlaybackList.ShuffleEnabled = false;
        }
    }

    [RelayCommand]
    private void NextSong()
    {
        //MediaList.Instance.MediaPlaybackList.MoveNext();
    }

    [RelayCommand]
    private void PreviousSong()
    {
        //MediaList.Instance.MediaPlaybackList.MovePrevious();
    }

    [RelayCommand]
    private void ForwardSong() => ProgressBarValue++;

    [RelayCommand]
    private void RewindSong() => ProgressBarValue--;

    [RelayCommand]
    private void RepeatButtonToggle()
    {
        if (repeatState == RepeatStates.Off)
        {
            repeatState = RepeatStates.All;
        }
        else if (repeatState == RepeatStates.All)
        {
            repeatState = RepeatStates.One;
        }
        else if (repeatState == RepeatStates.One)
        {
            repeatState = RepeatStates.Off;
        }

        switch (repeatState)
        {
            case RepeatStates.Off:
                RepeatButtonFontIcon    = "\uF5E7";
                ToolTipTextRepeatButton = "Repeat Off";
                break;

            case RepeatStates.All:
                RepeatButtonFontIcon    = "\uE8EE";
                ToolTipTextRepeatButton = "Repeat All";
                break;

            case RepeatStates.One:
                RepeatButtonFontIcon    = "\uE8ED";
                ToolTipTextRepeatButton = "Repeat One";
                break;
        }
    }

    private enum RepeatStates
    {
        Off,
        All,
        One
    }

}
