using Microsoft.UI.Dispatching;
using Tunetastic.Views.LibraryViews;
using Windows.Media.Playback;


namespace Tunetastic.ViewModels;

/// <summary>
/// Represents the ViewModel for controlling music playback in the application.
/// </summary>
/// <remarks>
/// This class provides properties and methods to manage playback state, user interface updates,
/// and interaction with the underlying media player. It handles playback controls such as play,
/// pause, shuffle, repeat, and also manages progress and duration of the currently playing song.
/// </remarks>
public partial class MusicControlViewModel : ObservableRecipient
{
    private readonly DispatcherQueue _dispatcherQueue;

    private bool isUpdatingProgressBar = false;

    private bool _isRainbowActive = false;

    private readonly MusicPlayer _musicPlayer = MusicPlayer.Instance;

    private string _fontIconPlayPause = "\uE768";

    /// <summary>
    /// Gets or sets the glyph representation for the play/pause icon.
    /// </summary>
    /// <remarks>
    /// This property updates the visual icon displayed in the user interface to represent
    /// the current playback state. The value is mapped to specific Unicode glyphs
    /// ("\uE768" for play and "\uE769" for pause). Updates to this property occur
    /// dynamically based on the media playback state changes, ensuring synchronization between
    /// the displayed icon and the player state.
    /// </remarks>
    public string FontIconPlayPause
    {
        get => _fontIconPlayPause;
        set => SetProperty(ref _fontIconPlayPause, value);
    }

    private string _repeatButtonFontIcon = "\uE8EE";

    /// <summary>
    /// Gets or sets the glyph representation for the repeat button icon.
    /// </summary>
    /// <remarks>
    /// This property determines the visual representation of the repeat button in the user interface,
    /// dynamically updating the displayed glyph based on the current repeat mode. The property maps
    /// specific Unicode glyphs ("\uE8EE" for repeat all, "\uE8ED" for repeat one, and "\uF5E7" for repeat off)
    /// to visually indicate the active repeat state. Changes to this property ensure synchronization
    /// with the application's repeat functionality.
    /// </remarks>
    public string RepeatButtonFontIcon
    {
        get => _repeatButtonFontIcon;
        set => SetProperty(ref _repeatButtonFontIcon, value);
    }

    private double _progressBarValue;

    /// <summary>
    /// Gets or sets the current position of the progress bar, representing the playback position in seconds.
    /// </summary>
    /// <remarks>
    /// This property is dynamically updated to reflect the real-time playback position of the media content.
    /// Changes to this value trigger corresponding updates in the user interface. Modifications to the
    /// property also initiate logic to synchronize the playback position with the media playback session.
    /// Additionally, it ensures thread-safe updates by utilizing a dispatcher queue.
    /// </remarks>
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

    /// <summary>
    /// Gets or sets the total duration of the currently playing song in seconds.
    /// </summary>
    /// <remarks>
    /// This property represents the length of the song being played, expressed as a double value in seconds.
    /// It is typically used to calculate playback progress, update UI elements like progress bars,
    /// and display the song's duration in a human-readable format.
    /// The value is updated dynamically when a new media track is loaded or when the playback session initializes.
    /// </remarks>
    public double DurationOfSong
    {
        get => _durationOfSong;
        set => SetProperty(ref _durationOfSong, value);
    }

    private bool _isShuffleToggled = false;

    /// <summary>
    /// Gets or sets a value indicating whether shuffle mode is enabled for music playback.
    /// </summary>
    /// <remarks>
    /// When set to true, the playback order of the songs is randomized, activating the shuffle mode.
    /// Setting this property also updates the underlying music player's shuffle state and manages
    /// the application's user interface, such as updating tooltips and saving the shuffle status
    /// to local application settings. Changes to this value are reflected in real time
    /// to ensure synchronization between the application controls and playback behavior.
    /// </remarks>
    public bool IsShuffleToggled
    {
        get => _isShuffleToggled;
        set => SetProperty(ref _isShuffleToggled, value);
    }

    private Style _repeatButtonStyle = (Style)Application.Current.Resources["AccentButtonStyle"];

    /// <summary>
    /// Gets or sets the style applied to the Repeat button in the music control interface.
    /// </summary>
    /// <remarks>
    /// This property determines the visual appearance of the Repeat button, dynamically updating
    /// its style based on the current repeat mode. It corresponds to predefined styles such as
    /// "AccentButtonStyle" for active repeat modes (All or One) and "DefaultButtonStyle" when
    /// repeat is turned off. This allows the button's visual state to reflect the repeat functionality
    /// accurately, enhancing user interaction and experience.
    /// </remarks>
    public Style RepeatButtonStyle
    {
        get => _repeatButtonStyle;
        set => SetProperty(ref _repeatButtonStyle, value);
    }

    private string _toolTipTextPlayPause = "Play";

    /// <summary>
    /// Gets or sets the tooltip text for the play/pause button.
    /// </summary>
    /// <remarks>
    /// This property dynamically updates the text displayed in the tooltip of the play/pause button,
    /// reflecting the current state of media playback. The value is set to "Play" when the media is paused or stopped,
    /// and "Pause" when the media is playing, ensuring contextual guidance for the user. Changes are synchronized
    /// with the playback state managed by the media player.
    /// </remarks>
    public string ToolTipTextPlayPause
    {
        get => _toolTipTextPlayPause;
        set => SetProperty(ref _toolTipTextPlayPause, value);
    }

    private string _toolTipTextShuffleButton = "Shuffle Off";

    /// <summary>
    /// Gets or sets the tooltip text displayed for the shuffle toggle button.
    /// </summary>
    /// <remarks>
    /// This property determines the tooltip message shown to the user when hovering over the shuffle button.
    /// The text dynamically updates based on the shuffle state, indicating "Shuffle On" when shuffle mode is activated
    /// and "Shuffle Off" when it is deactivated. It ensures that the user has clear feedback on the current shuffle status.
    /// </remarks>
    public string ToolTipTextShuffleButton
    {
        get => _toolTipTextShuffleButton;
        set => SetProperty(ref _toolTipTextShuffleButton, value);
    }

    private string _toolTipTextRepeatButton = "Repeat All";

    /// <summary>
    /// Gets or sets the tooltip text for the repeat button.
    /// </summary>
    /// <remarks>
    /// This property defines the descriptive text displayed when the user hovers over the repeat button
    /// in the music control interface. It dynamically updates based on the current repeat mode:
    /// "Repeat All", "Repeat One", or "Repeat Off". These updates provide users with contextual information
    /// about the button's current function.
    /// </remarks>
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

        _musicPlayer.ShuffleStatusChanged += _musicPlayer_ShuffleStatusChanged;

        SetToggleAndRepeat();

        LoadLastPlayedTrack();
    }

    /// <summary>
    /// Initializes the shuffle and repeat button states based on the saved application settings.
    /// Retrieves the saved shuffle and repeat statuses from local settings and updates the respective toggle and button states accordingly.
    /// </summary>
    private void SetToggleAndRepeat()
    {
        var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

        ShuffleToggle(bool.Parse(localSettings.Values[nameof(LocalSave.ShuffleStatus)]?.ToString() ?? "false"));

        RepeatButtonToggle(Enum.Parse<RepeatMode>(localSettings.Values[nameof(LocalSave.RepeatStatus)]?.ToString() ?? "All"));
    }

    /// <summary>
    /// Loads the last played track from the saved application data and resumes playback state.
    /// Retrieves the last played track information, playback position, and other related details
    /// from local settings to restore the media player's state and playlist upon application startup.
    /// </summary>
    private void LoadLastPlayedTrack()
    {
        var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        if (localSettings.Values.ContainsKey(nameof(LocalSave.LastPlayedTrack)))
        {
            using var _ = _musicPlayer.LoadSong(localSettings.Values[nameof(LocalSave.LastPlayedTrack)]?.ToString(), play: bool.Parse(localSettings.Values[nameof(LocalSave.AutoStartStatus)]?.ToString() ?? "false"));

            ProgressBarValue = double.Parse(localSettings.Values[nameof(LocalSave.PlayBackPosition)]?.ToString() ?? "0");


            switch (localSettings.Values[nameof(LocalSave.CurrentPlaylist)]?.ToString())
            {
                case "AllSongsViewPage":
                    new AllSongsViewPage().LoadAsPlayList();
                    break;

                default:
                    break;
            }

        }
    }

    /// <summary>
    /// Toggles the play and pause state of the music player.
    /// If the current playback state is 'Playing', the method pauses the music.
    /// If the current playback state is 'Paused', the method resumes playback.
    /// </summary>
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

    /// <summary>
    /// Handles changes in the playback state of the media session and updates the user interface accordingly.
    /// This method ensures UI updates are dispatched to the UI thread and adjusts the play/pause icon
    /// and tooltip text based on the current playback state.
    /// </summary>
    /// <param name="sender">The media playback session that raised the playback state changed event.</param>
    /// <param name="args">Additional event data, if any, provided by the event source.</param>
    private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
    {
        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, async () =>
        {
            switch (_musicPlayer.MediaPlayer.PlaybackSession.PlaybackState)
            {
                case MediaPlaybackState.Paused:
                case MediaPlaybackState.None:
                    FontIconPlayPause = "\uE768";
                    ToolTipTextPlayPause = "Play";

                    await Task.Delay(500);

                    if (_musicPlayer.MediaPlayer.PlaybackSession.PlaybackState != MediaPlaybackState.Playing)
                    {
                        StopRainbow();
                        _isRainbowActive = false;
                    }
                    break;

                case MediaPlaybackState.Playing:
                    FontIconPlayPause = "\uE769";
                    ToolTipTextPlayPause = "Pause";

                    if (!_isRainbowActive)
                    {
                        StartRainbow();
                        _isRainbowActive = true;
                    }
                    break;
            }
        });
    }

    /// <summary>
    /// Activates the rainbow frame effect based on user preferences and saved application settings.
    /// Checks if the rainbow frame feature and playback-dependent activation are enabled in local settings.
    /// If enabled, starts the rainbow frame effect and applies the user-defined speed configuration.
    /// </summary>
    private void StartRainbow()
    {
        var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        if (bool.Parse(localSettings.Values[nameof(LocalSave.RainbowFrameStatus)]?.ToString() ?? "false") && bool.Parse(localSettings.Values[nameof(LocalSave.RainbowOnlyDuringPlayback)]?.ToString() ?? "false"))
        {
            App.Current.RainbowFrame.StartRainbowFrame();

            App.Current.RainbowFrame.UpdateEffectSpeed(51 - int.Parse(localSettings.Values[nameof(LocalSave.RainbowFrameSpeed)]?.ToString() ?? "31"));
        }
    }

    /// <summary>
    /// Stops the rainbow frame effect and resets the frame color to its default state if certain conditions are met.
    /// Checks the saved application settings to determine whether the rainbow frame is active and if it should only operate during playback.
    /// If both conditions are true, it stops the rainbow frame and resets the frame color.
    /// </summary>
    private void StopRainbow()
    {
        var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        if (bool.Parse(localSettings.Values[nameof(LocalSave.RainbowFrameStatus)]?.ToString() ?? "false") && bool.Parse(localSettings.Values[nameof(LocalSave.RainbowOnlyDuringPlayback)]?.ToString() ?? "false"))
        {
            App.Current.RainbowFrame.StopRainbowFrame();
            App.Current.RainbowFrame.ResetFrameColorToDefault();
        }
    }


    /// <summary>
    /// Advances playback to the next song in the playlist or queue.
    /// If the current song is the last in the queue, behavior depends on the playback settings
    /// (e.g., loop or stop after the last song).
    /// </summary>
    [RelayCommand]
    private void NextSong() => _musicPlayer.Next();

    /// <summary>
    /// Switches the currently playing track to the previous song in the playlist.
    /// If the player is at the beginning of the playlist, it may either stop playback or loop based on player settings.
    /// </summary>
    [RelayCommand]
    private void PreviousSong() => _musicPlayer.Previous();

    /// <summary>
    /// Moves the playback position of the currently playing song forward by increasing the progress value.
    /// This command is used for forwarding the track to a later position in time.
    /// </summary>
    [RelayCommand]
    private void ForwardSong() => ProgressBarValue++;

    /// <summary>
    /// Moves the playback position of the currently playing song backward by reducing the progress value.
    /// This command is used for rewinding the track to an earlier position in time.
    /// </summary>
    [RelayCommand]
    private void RewindSong() => ProgressBarValue--;


    /// <summary>
    /// Toggles the shuffle mode for the music player.
    /// If shuffle is currently on, it turns it off; otherwise, it enables shuffle.
    /// Updates the tooltip text and saves the shuffle state to local settings.
    /// </summary>
    /// <param name="shuffleSaved">
    /// Optional parameter indicating the desired shuffle state. If null, the current shuffle state is toggled.
    /// </param>
    [RelayCommand]
    public void ShuffleToggle(bool? shuffleSaved = null)
    {
        IsShuffleToggled = shuffleSaved ?? IsShuffleToggled;
        _musicPlayer.ToggleShuffle(IsShuffleToggled ? ShuffleMode.On : ShuffleMode.Off);
        ToolTipTextShuffleButton = IsShuffleToggled ? "Shuffle On" : "Shuffle Off";
        Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.ShuffleStatus)] = IsShuffleToggled;
    }

    /// <summary>
    /// Toggles the repeat mode of the music player among 'None', 'One', and 'All'.
    /// Updates the repeat button's appearance and tooltip text according to the selected mode.
    /// If a saved repeat mode is provided, it is applied; otherwise, the default is used.
    /// The chosen repeat mode is saved to the local settings.
    /// </summary>
    /// <param name="repeatSaved">
    /// An optional parameter specifying the saved repeat mode to apply.
    /// If null, the method uses the default repeat mode.
    /// </param>
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
        Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.RepeatStatus)] = _musicPlayer.RepeatStatus.ToString();
    }

    /// <summary>
    /// Handles the PositionChanged event for the MediaPlaybackSession.
    /// This event occurs when the position within the currently playing media changes.
    /// Updates UI components or internal states to reflect the new playback position.
    /// </summary>
    /// <param name="sender">The MediaPlaybackSession that raised the event.</param>
    /// <param name="args">The event data associated with the PositionChanged event.</param>
    private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args) => _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, async () =>
    {
        isUpdatingProgressBar = true;
        ProgressBarValue = sender.Position.TotalSeconds;
        await Task.Delay(1);
        isUpdatingProgressBar = false;
    });

    /// <summary>
    /// Updates the playback position of the media player to reflect the current value of the progress bar.
    /// This method mutes the audio briefly while updating the playback position to prevent playback artifacts,
    /// then resumes audio playback once the update is complete.
    /// </summary>
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

    /// <summary>
    /// Handles the event when a media file is opened in the playback session.
    /// This method updates the duration of the currently playing media file.
    /// </summary>
    /// <param name="sender">The MediaPlayer instance that triggered the event.</param>
    /// <param name="args">Additional event data associated with the media opened event.</param>
    private async void PlaybackSession_MediaOpenedAsync(MediaPlayer sender, object args)
    {
        await Task.Delay(100);
        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
        {
            DurationOfSong = _musicPlayer.MediaPlayer.PlaybackSession.NaturalDuration.TotalSeconds;
        });
    }

    /// <summary>
    /// Handles the event when the shuffle status of the music player changes.
    /// Updates the shuffle toggle state based on the provided shuffle mode.
    /// </summary>
    /// <param name="sender">The source of the event, typically the music player.</param>
    /// <param name="e">The new shuffle mode, indicating whether shuffle is turned on or off.</param>
    private void _musicPlayer_ShuffleStatusChanged(object? sender, ShuffleMode e)
    {
        ShuffleToggle(e == ShuffleMode.On);
    }
}

