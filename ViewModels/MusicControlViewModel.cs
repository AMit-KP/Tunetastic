using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Media;
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

	private PlaybackTracker _playbackTracker = new();

	private DispatcherTimer? _midpointTimer;

	private TimeSpan _thresoldDuration = TimeSpan.Zero;

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

	private string _title = "Please select a song";

	/// <summary>
	/// Gets or sets the title of the currently playing track.
	/// </summary>
	/// <remarks>
	/// This property reflects the name of the song that is being played in the media player.
	/// It is automatically updated based on the media loaded into the playback session,
	/// ensuring the displayed title in the user interface is always in sync with the current track.
	/// </remarks>
	public string Title
	{
		get => _title;
		set => SetProperty(ref _title, value);
	}

	private string _artist = "";

	/// <summary>
	/// Gets or sets the artist information of the currently playing song.
	/// </summary>
	/// <remarks>
	/// This property holds the name(s) of the artist(s) associated with the currently playing track.
	/// It is automatically updated when the media player's playback session opens a new song.
	/// The value is displayed in the user interface to provide context about the current track.
	/// </remarks>
	public string Artist
	{
		get => _artist;
		set => SetProperty(ref _artist, value);
	}

	private string _cover = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.png");

	/// <summary>
	/// Gets or sets the file path or URI of the album cover image.
	/// </summary>
	/// <remarks>
	/// This property determines the visual representation of the album artwork displayed in the user interface.
	/// The value can be a local file path or a URI. Updates to this property dynamically change the displayed
	/// album cover to match the currently playing song. When no specific album cover is available, it defaults
	/// to a predefined image path.
	/// </remarks>
	public string Cover
	{
		get => _cover;
		set => SetProperty(ref _cover, value);
	}

	public Storyboard? _vinylEffect { get; set; }

	/// <summary>
	/// Represents the ViewModel for music control functionality of the application.
	/// Maintains the state and behavior of media playback, including play/pause, shuffle, repeat, and progress bar controls.
	/// Interacts with the MusicPlayer instance to handle playback events and exposes properties for UI binding to reflect current playback state.
	/// </summary>
	public MusicControlViewModel()
	{
		_dispatcherQueue = DispatcherQueue.GetForCurrentThread();

		_musicPlayer.MediaPlayer.PlaybackStateChanged += (s, e) => PlaybackSession_PlaybackStateChanged();

		_musicPlayer.MediaPlayer.PositionChanged += PlaybackSession_PositionChanged;

		_musicPlayer.MediaPlayer.MediaOpened += (s, e) => PlaybackSession_MediaOpenedAsync();

		//TODO pause on mute

		_musicPlayer.ShuffleStatusChanged += _musicPlayer_ShuffleStatusChanged;

		SetShuffleAndRepeat();

		_ = LoadLastPlayedTrack();

		App.TrayIcon.MouseClick += (s, e) =>
		{
			if (e.Button == System.Windows.Forms.MouseButtons.Left)
			{
				TogglePlayPause();
			}
		};

		if (_musicPlayer.SMTC != null)
		{
			_musicPlayer.SMTC.ButtonPressed += (s, e) =>
			{
				switch (e.Button)
				{
					case SystemMediaTransportControlsButton.Play:
					case SystemMediaTransportControlsButton.Pause:
						_dispatcherQueue.TryEnqueue(() => TogglePlayPause());
						break;
					case SystemMediaTransportControlsButton.Next:
						_dispatcherQueue.TryEnqueue(() => NextSong());
						break;
					case SystemMediaTransportControlsButton.Previous:
						_dispatcherQueue.TryEnqueue(() => PreviousSong());
						break;
				}
			};
		}

		MainWindow._instance.Content.PreviewKeyDown += PreviewKeyDownMusicControl;
		MainWindow._instance.Content.ProcessKeyboardAccelerators += keyboardInput;
	}

	/// <summary>
	/// Handles keyboard input events for processing global keyboard shortcuts in the application,
	/// such as navigating between tracks or executing playback-related commands.
	/// </summary>
	/// <param name="sender">The UI element that is the source of the event.</param>
	/// <param name="args">The event arguments containing details about the keyboard input,
	/// such as the key pressed and modifier keys.</param>
	private void keyboardInput(UIElement sender, ProcessKeyboardAcceleratorEventArgs args)
	{
		switch (args.Modifiers)
		{
			case Windows.System.VirtualKeyModifiers.Control when args.Key == Windows.System.VirtualKey.N:
				NextSong();
				break;
			case Windows.System.VirtualKeyModifiers.Control when args.Key == Windows.System.VirtualKey.P:
				PreviousSong();
				break;
		}
	}

	/// <summary>
	/// Handles the PreviewKeyDown event for the music control functionality.
	/// Intercepts specific key inputs such as the Space key to toggle play/pause functionality
	/// or the Tab key to prevent unintended default behavior in the application.
	/// </summary>
	/// <param name="sender">The source of the event.</param>
	/// <param name="e">The event data containing information about the key event.</param>
	private void PreviewKeyDownMusicControl(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
	{
		if (!MainPage._instance.searchBoxFocused && e.Key == Windows.System.VirtualKey.Space)
		{
			e.Handled = true;
			TogglePlayPause();
		}
		else if (e.Key == Windows.System.VirtualKey.Tab)
		{
			e.Handled = true;
		}
	}

	/// <summary>
	/// Initializes the shuffle and repeat button states based on the saved application settings.
	/// Retrieves the saved shuffle and repeat statuses from local settings and updates the respective toggle and button states accordingly.
	/// </summary>
	private void SetShuffleAndRepeat()
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
	private async Task LoadLastPlayedTrack()
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		if (localSettings.Values.ContainsKey(nameof(LocalSave.LastPlayedTrack)))
		{
			var song = localSettings.Values[nameof(LocalSave.LastPlayedTrack)]?.ToString();
			if (string.IsNullOrEmpty(song)) return;
			var track = await DatabaseHelper.Instance.GetSongByPath(song!);

			if (track == null)
			{
				localSettings.Values.Remove(nameof(LocalSave.LastPlayedTrack));
				localSettings.Values.Remove(nameof(LocalSave.PlayBackPosition));
				localSettings.Values.Remove(nameof(LocalSave.CurrentPlayinglist));
				return;
			}

			_musicPlayer.LoadPlaylist(song, play: bool.Parse(localSettings.Values[nameof(LocalSave.AutoStartStatus)]?.ToString() ?? "false"), startup: true);

			var position = double.Parse(localSettings.Values[nameof(LocalSave.PlayBackPosition)]?.ToString() ?? "0");

			DurationOfSong = double.Parse(track.Duration.ToString());
			ProgressBarValue = position;
		}
	}

	/// <summary>
	/// Toggles the play and pause state of the music player.
	/// If the current playback state is 'Playing', the method pauses the music.
	/// If the current playback state is 'Paused', the method resumes playback.
	/// </summary>
	[RelayCommand]
	private async Task TogglePlayPause()
	{
		switch (_musicPlayer.MediaPlayer.PlaybackState)
		{
			case MediaPlaybackState.Playing:
				MusicPlayer.Instance.SMTC.PlaybackStatus = MediaPlaybackStatus.Paused;
				_musicPlayer.Pause();
				_playbackTracker.PausePlayback();
				break;
			case MediaPlaybackState.Paused:
			case MediaPlaybackState.None:
				MusicPlayer.Instance.SMTC.PlaybackStatus = MediaPlaybackStatus.Playing;
				_musicPlayer.MediaPlayer.PositionChanged -= PlaybackSession_PositionChanged;
				await Task.Delay(10);
				_musicPlayer.Play(playBackPosition: ProgressBarValue);
				_playbackTracker.StartPlayback();
				await Task.Delay(400);
				_musicPlayer.MediaPlayer.PositionChanged += PlaybackSession_PositionChanged;
				break;
		}
	}

	/// <summary>
	/// Handles the playback state changes of the media playback session.
	/// Updates relevant UI elements, like play/pause icon and tooltip, and synchronizes playback status with system media transport controls.
	/// Also manages visual effects like rainbow animations based on the playback state.
	/// </summary>
	/// <param name="sender">The MediaPlaybackSession that triggered the state change event.</param>
	/// <param name="args">An object containing event data for the playback state change.</param>
	private void PlaybackSession_PlaybackStateChanged()
	{
		_dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, async () =>
		{
			switch (_musicPlayer.MediaPlayer.PlaybackState)
			{
				case MediaPlaybackState.Paused:
				case MediaPlaybackState.None:
					FontIconPlayPause = "\uE768";
					ToolTipTextPlayPause = "Play";

					_playbackTracker.PausePlayback();

					_vinylEffect?.Pause();

					MusicPlayer.Instance.SMTC.PlaybackStatus = MediaPlaybackStatus.Paused;
					MainPage._instance.AnimateTitle(startAnimation: false);
					TaskbarHelper.SetProgressState(App.Hwnd, TaskbarStates.Paused);

					await Task.Delay(500);

					if (_musicPlayer.MediaPlayer.PlaybackState != MediaPlaybackState.Playing)
					{
						StopRainbow();
						_isRainbowActive = false;
					}
					break;

				case MediaPlaybackState.Playing:
					FontIconPlayPause = "\uE769";
					ToolTipTextPlayPause = "Pause";

					_playbackTracker.StartPlayback();

					_vinylEffect?.Resume();

					MusicPlayer.Instance.SMTC.PlaybackStatus = MediaPlaybackStatus.Playing;
					MainPage._instance.AnimateTitle(startAnimation: true);
					TaskbarHelper.SetProgressState(App.Hwnd, TaskbarStates.Normal);

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
	/// Advances playback to the next song in the playlist or queue. And reset the playback position.
	/// If the current song is the last in the queue, behavior depends on the playback settings
	/// (e.g., loop or stop after the last song).
	/// </summary>
	[RelayCommand]
	private void NextSong()
	{
		ProgressBarValue = 0;
		_midpointTimer?.Stop();
		_playbackTracker.Reset();
		_musicPlayer.Next();
	}

	/// <summary>
	/// Switches the currently playing track to the previous song in the playlist. And reset the playback position.
	/// If the player is at the beginning of the playlist, it may either stop playback or loop based on player settings.
	/// </summary>
	[RelayCommand]
	private void PreviousSong()
	{
		ProgressBarValue = 0;
		_midpointTimer?.Stop();
		_playbackTracker.Reset();
		_musicPlayer.Previous();
	}

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
	private void PlaybackSession_PositionChanged(object? sender, TimeSpan e) => _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, async () =>
	{
		isUpdatingProgressBar = true;
		ProgressBarValue = _musicPlayer.MediaPlayer.Position.TotalSeconds;
		//await Task.Delay(1);
		//TODO smooth progress
		TaskbarHelper.SetProgressValue(App.Hwnd, ProgressBarValue / DurationOfSong * 100, 100);
		isUpdatingProgressBar = false;
	});

	/// <summary>
	/// Updates the playback position of the media player to reflect the current value of the progress bar.
	/// This method mutes the audio briefly while updating the playback position to prevent playback artifacts,
	/// then resumes audio playback once the update is complete.
	/// </summary>
	private void UpdatePlaybackPosition()
	{
		_dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, async () =>
		{
			_musicPlayer.MediaPlayer.Volume = 0;
			_musicPlayer.MediaPlayer.Position = TimeSpan.FromSeconds(ProgressBarValue);
			await Task.Delay(500);
			_musicPlayer.MediaPlayer.Volume = 0.7;
		});
	}

	/// <summary>
	/// Handles the MediaOpened event of the PlaybackSession.
	/// Initializes the playback session by setting the duration of the song and updating song metadata,
	/// including title, artist, and cover details, fetched from stored song metadata.
	/// </summary>
	/// <param name="sender">The MediaPlayer instance that triggered the event.</param>
	/// <param name="args">Event data associated with the MediaOpened event.</param>
	private async void PlaybackSession_MediaOpenedAsync()
	{
		await Task.Delay(50);
		_dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, async () =>
		{
			var songPath = _musicPlayer.CurrentSong;
			if (string.IsNullOrEmpty(songPath)) return;
			var track = await DatabaseHelper.Instance.GetSongByPath(songPath!);
			if (track != null)
			{
				DurationOfSong = double.Parse(track.Duration.ToString());

				_playbackTracker.Reset();
				_thresoldDuration = TimeSpan.FromSeconds(DurationOfSong * 0.6);
				_midpointTimer?.Stop();

				if (_musicPlayer.MediaPlayer.PlaybackState == MediaPlaybackState.Playing)
				{
					_playbackTracker.StartPlayback();
				}

				_midpointTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
				_midpointTimer.Tick += MidpointTimer_Tick;
				_midpointTimer.Start();

				Title = track.Title;
				Artist = track.Artists;
				Cover = track.Cover;
			}
			//track = null; // not needed

			_vinylEffect?.Begin();
			if (MusicPlayer.Instance.MediaPlayer.PlaybackState != MediaPlaybackState.Playing) _vinylEffect?.Pause();
			MusicControl._instance?.FloatingPlayer(null, MainPage._instance?.IsMainPlayerPageOpened ?? false);
			MusicControl._instance?.SlideInDown();
		});
	}

	private async void MidpointTimer_Tick(object? sender, object e)
	{
		var session = _musicPlayer.MediaPlayer;

		if (_playbackTracker.AlreadyCounted)
		{
			_midpointTimer?.Stop();
			return;
		}

		if (session.PlaybackState == MediaPlaybackState.Playing &&
			session.Position >= _thresoldDuration &&
			_playbackTracker.GetTotalPlayTime() >= _thresoldDuration)
		{
			_midpointTimer?.Stop();
			_playbackTracker.MarkPlayCountRecorded();
			await DatabaseHelper.Instance.IncrementPlayCount(_musicPlayer.CurrentSong);
			await DatabaseHelper.Instance.UpdateDateLastPlayed(_musicPlayer.CurrentSong);
		}
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

	/// <summary>
	/// Updates the storyboard instance used to animate UI elements related to music playback control.
	/// </summary>
	/// <param name="storyboard">
	/// The storyboard instance to be set for managing animations, or null to reset it.
	/// </param>
	public void UpdateStoryBoard(Storyboard? storyboard)
	{
		_vinylEffect = storyboard;
	}

	/// <summary>
	/// Resets the properties of the floating music control window to their default state.
	/// This includes updating the title, artist, and cover image to indicate no song is currently selected.
	/// Primarily used to clear the current song display when no valid song is being played or after certain operations like playlist resets.
	/// </summary>
	public async void ResetCurrentSongFloatingWindow()
	{
		Title = "Please select a song";
		Artist = string.Empty;
		Cover = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.png");
		await Task.Delay(50);
		_vinylEffect?.Stop();
		await Task.Delay(50);
		DurationOfSong = 0;
		MusicControl._instance?.FloatingPlayer(null, MainPage._instance?.IsMainPlayerPageOpened ?? false);
	}
}

