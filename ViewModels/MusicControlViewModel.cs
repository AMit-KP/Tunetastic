using System.Drawing;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.WindowsAPICodePack.Taskbar;
using Tunetastic.Common.Services.TaskbarOverlay;
using Tunetastic.Overlay;
using Tunetastic.Overlay.Layouts;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.ViewManagement;


namespace Tunetastic.ViewModels;

/// <summary>
/// ViewModel for music playback control. All MediaPlayer/FlyleafLib references
/// have been replaced with the unified MusicPlayer surface (IsPlaying, CurTimeTicks,
/// PlaybackStateChanged, OpenCompleted, PositionChanged).
/// </summary>
public partial class MusicControlViewModel : ObservableRecipient
{
	private readonly DispatcherQueue _dispatcherQueue;

	private bool isUpdatingProgressBar = false;
	private bool _isRainbowActive = false;

	private readonly MusicPlayer _musicPlayer = MusicPlayer.Instance;
	private PlaybackTracker _playbackTracker = new();
	private DispatcherTimer? _midpointTimer;
	private double _startupPosition = 0;
	private ThumbnailToolBarButton Play_Pause_Button = null!;
	private OverlayBase? _overlayGrid = null;
	private readonly UISettings _uiSettings = new();

	private SmoothProgressBar? _progressBar;
	public SmoothProgressBar? ProgressBar
	{
		get => _progressBar;
		set
		{
			if (_progressBar != null)
				_progressBar.Seeked -= OnProgressBarSeeked;
			_progressBar = value;
			if (_progressBar != null)
				_progressBar.Seeked += OnProgressBarSeeked;
		}
	}

	private void OnProgressBarSeeked(object? sender, double seconds)
	{
		isUpdatingProgressBar = true;
		_progressBarValue = seconds;
		isUpdatingProgressBar = false;
		_musicPlayer.CurTimeTicks = TimeSpan.FromSeconds(seconds).Ticks;
		_overlayGrid?.UpdateProgress(seconds / DurationOfSong);
	}

	private TimeSpan _thresoldDuration = TimeSpan.Zero;

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
				_progressBarValue = value;
				try { OnPropertyChanged(nameof(ProgressBarValue)); } catch (Exception) { }

				if (!isUpdatingProgressBar)
					UpdatePlaybackPosition();
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

	private string _title = "Please select a song";
	public string Title
	{
		get => _title;
		set => SetProperty(ref _title, value);
	}

	private string _artist = "";
	public string Artist
	{
		get => _artist;
		set => SetProperty(ref _artist, value);
	}

	private string _cover = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.png");
	public string Cover
	{
		get => _cover;
		set => SetProperty(ref _cover, value);
	}

	private Visibility _forward_rewind_visibility = Visibility.Visible;
	public Visibility Forward_Rewind_Visibility
	{
		get => _forward_rewind_visibility;
		set => SetProperty(ref _forward_rewind_visibility, value);
	}

	public Storyboard? _vinylEffect { get; set; }

	public MusicControlViewModel()
	{
		_dispatcherQueue = DispatcherQueue.GetForCurrentThread();

		_musicPlayer.PlaybackStateChanged += OnPlaybackStateChanged;
		_musicPlayer.OpenCompleted += (s, e) => PlaybackSession_MediaOpenedAsync();
		_musicPlayer.PositionChanged += OnPositionChanged;

		_musicPlayer.ShuffleStatusChanged += _musicPlayer_ShuffleStatusChanged;

		SetShuffleAndRepeat();
		_ = LoadLastPlayedTrack();

		if (App.TrayIcon != null)
			App.TrayIcon.LeftClick += OnTrayIconLeftClick;

		if (_musicPlayer.SMTC != null)
		{
			_musicPlayer.SMTC.ButtonPressed += (s, e) =>
			{
				switch (e.Button)
				{
					case SystemMediaTransportControlsButton.Play:
					case SystemMediaTransportControlsButton.Pause:
						_dispatcherQueue.TryEnqueue(async () => await TogglePlayPause());
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

		if (MainWindow._instance?.Content != null)
		{
			MainWindow._instance.Content.PreviewKeyDown += PreviewKeyDownMusicControl;
			MainWindow._instance.Content.ProcessKeyboardAccelerators += keyboardInput;
		}

		SetupTaskbarThumbnailToolBar();

		SetupTaskbarOverlay();
	}

	// ─────────────────────────────────────────────────────────
	//  Tray click Event handlers
	// ─────────────────────────────────────────────────────────
	private async void OnTrayIconLeftClick(SystemTrayIcon sender, SystemTrayIconEventArgs args)
	{
		await TogglePlayPause();
	}

	// ─────────────────────────────────────────────────────────
	//  Event handlers from unified MusicPlayer surface
	// ─────────────────────────────────────────────────────────

	/// <summary>
	/// Handles changes in playback state and updates the user interface and playback controls accordingly.
	/// </summary>
	/// <remarks>This method synchronizes UI elements and playback-related features with the current playback state.
	/// It should be used as an event handler for playback state change events.</remarks>
	/// <param name="sender">The source of the event, typically the music player instance.</param>
	/// <param name="e">An object containing data about the playback state change.</param>
	private void OnPlaybackStateChanged(object? sender, PlaybackStateChangedArgs e)
	{
		_dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, async () =>
		{
			switch (e.State)
			{
				case PlaybackState.Paused:
				case PlaybackState.Stopped:
					FontIconPlayPause = "\uE768";
					ToolTipTextPlayPause = "Play";

					_playbackTracker.PausePlayback();
					_vinylEffect?.Pause();
					ProgressBar?.NotifyPaused();

					if (MusicPlayer.Instance.SMTC != null)
						MusicPlayer.Instance.SMTC.PlaybackStatus = MediaPlaybackStatus.Paused;
					MainPage._instance?.AnimateTitle(startAnimation: false);
					TaskbarHelper.SetProgressState(App.Hwnd, TaskbarStates.Paused);
					Play_Pause_Button.Icon = new Icon(Path.Combine(AppContext.BaseDirectory, "Assets", "Fluent", $"play_{(App.Current.ThemeService.IsDark ? "light" : "dark")}.ico"));
					_overlayGrid?.SetPlayingState(isPlaying: false);

					await Task.Delay(500);

					if (_musicPlayer.IsPlaying)
					{
						StopRainbow();
						_isRainbowActive = false;
					}
					break;

				case PlaybackState.Playing:
					FontIconPlayPause = "\uE769";
					ToolTipTextPlayPause = "Pause";

					_playbackTracker.StartPlayback();
					_vinylEffect?.Resume();
					ProgressBar?.NotifyPlaying();

					if (MusicPlayer.Instance.SMTC != null)
						MusicPlayer.Instance.SMTC.PlaybackStatus = MediaPlaybackStatus.Playing;
					MainPage._instance?.AnimateTitle(startAnimation: true);
					TaskbarHelper.SetProgressState(App.Hwnd, TaskbarStates.Normal);
					Play_Pause_Button.Icon = new Icon(Path.Combine(AppContext.BaseDirectory, "Assets", "Fluent", $"pause_{(App.Current.ThemeService.IsDark ? "light" : "dark")}.ico"));
					_overlayGrid?.SetPlayingState(isPlaying: true);

					if (!_isRainbowActive)
					{
						StartRainbow();
						_isRainbowActive = true;
					}
					break;

				case PlaybackState.Ended:
					_musicPlayer.Next(autoChange: true);
					break;
			}
		});
	}

	/// <summary>
	/// Handles position change events by updating the progress bar and taskbar progress indicator to reflect the current
	/// playback position.
	/// </summary>
	/// <remarks>This method is typically called in response to playback position updates, ensuring that the user
	/// interface remains synchronized with the current state of playback.</remarks>
	/// <param name="sender">The source of the event. This parameter is not used.</param>
	/// <param name="ticks">The current playback position, in ticks. Represents the number of 100-nanosecond intervals elapsed.</param>
	private void OnPositionChanged(object? sender, long ticks)
	{
		_dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
		{
			var seconds = TimeSpan.FromTicks(ticks).TotalSeconds;
			isUpdatingProgressBar = true;
			ProgressBarValue = seconds;
			isUpdatingProgressBar = false;
			// Drive the smooth control — it advances itself between these ticks
			ProgressBar?.SyncPosition(seconds);
			TaskbarHelper.SetProgressValue(App.Hwnd, seconds / DurationOfSong * 100, 100);
			_overlayGrid?.UpdateProgress(seconds / DurationOfSong);
		});
	}

	// ─────────────────────────────────────────────────────────
	//  Keyboard input
	// ─────────────────────────────────────────────────────────

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

	private void PreviewKeyDownMusicControl(object sender, KeyRoutedEventArgs e)
	{
		if (!(MainPage._instance?.searchBoxFocused ?? false) && e.Key == Windows.System.VirtualKey.Space)
		{
			e.Handled = true;
			_ = TogglePlayPause();
		}
		else if (e.Key == Windows.System.VirtualKey.Tab)
		{
			e.Handled = true;
		}
	}

	// ─────────────────────────────────────────────────────────
	//  Shuffle / Repeat init
	// ─────────────────────────────────────────────────────────
	private void SetShuffleAndRepeat()
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		ShuffleToggle(bool.Parse(localSettings.Values[nameof(LocalSave.ShuffleStatus)]?.ToString() ?? "false"));
		RepeatButtonToggle(Enum.Parse<RepeatMode>(localSettings.Values[nameof(LocalSave.RepeatStatus)]?.ToString() ?? "All"));
	}

	// ─────────────────────────────────────────────────────────
	//  Startup: restore last played track
	// ─────────────────────────────────────────────────────────
	private async Task LoadLastPlayedTrack()
	{
		await Task.Delay(200);
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

			var position = double.Parse(localSettings.Values[nameof(LocalSave.PlayBackPosition)]?.ToString() ?? "0");
			DurationOfSong = track.Duration;
			ProgressBarValue = position;
			_startupPosition = position;

			_musicPlayer.LoadPlaylist(song, play: bool.Parse(localSettings.Values[nameof(LocalSave.AutoStartStatus)]?.ToString() ?? "false"), startup: true);
		}
	}

	// ─────────────────────────────────────────────────────────
	//  Play / Pause toggle
	// ─────────────────────────────────────────────────────────
	[RelayCommand]
	private async Task TogglePlayPause()
	{
		if (_musicPlayer.IsPlaying)
		{
			if (MusicPlayer.Instance.SMTC != null)
				MusicPlayer.Instance.SMTC.PlaybackStatus = MediaPlaybackStatus.Paused;
			_musicPlayer.Pause();
			_playbackTracker.PausePlayback();
		}
		else
		{
			if (MusicPlayer.Instance.SMTC != null)
				MusicPlayer.Instance.SMTC.PlaybackStatus = MediaPlaybackStatus.Playing;
			_musicPlayer.Play(playBackPosition: ProgressBarValue);
			_playbackTracker.StartPlayback();
		}
	}

	// ─────────────────────────────────────────────────────────
	//  Rainbow frame helpers
	// ─────────────────────────────────────────────────────────

	/// <summary>
	/// Initializes and starts the rainbow frame effect if the relevant user settings are enabled.
	/// </summary>
	/// <remarks>This method checks application settings to determine whether the rainbow frame effect should be
	/// activated and configures its speed accordingly. It is intended to be called when the application needs to update or
	/// start the rainbow frame based on user preferences.</remarks>
	private void StartRainbow()
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		if (bool.Parse(localSettings.Values[nameof(LocalSave.RainbowFrameStatus)]?.ToString() ?? "false") &&
			bool.Parse(localSettings.Values[nameof(LocalSave.RainbowOnlyDuringPlayback)]?.ToString() ?? "false"))
		{
			App.Current.RainbowFrame.StartRainbowFrame();
			App.Current.RainbowFrame.UpdateEffectSpeed(51 - int.Parse(localSettings.Values[nameof(LocalSave.RainbowFrameSpeed)]?.ToString() ?? "31"));
		}
	}

	/// <summary>
	/// Stops the rainbow frame effect and resets the frame color to its default state if the relevant settings are
	/// enabled.
	/// </summary>
	/// <remarks>This method checks application settings to determine whether the rainbow frame effect should be
	/// stopped and the frame color reset. It has no effect if the required settings are not enabled.</remarks>
	private void StopRainbow()
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		if (bool.Parse(localSettings.Values[nameof(LocalSave.RainbowFrameStatus)]?.ToString() ?? "false") &&
			bool.Parse(localSettings.Values[nameof(LocalSave.RainbowOnlyDuringPlayback)]?.ToString() ?? "false"))
		{
			App.Current.RainbowFrame.StopRainbowFrame();
			App.Current.RainbowFrame.ResetFrameColorToDefault();
		}
	}

	// ─────────────────────────────────────────────────────────
	//  Transport commands
	// ─────────────────────────────────────────────────────────

	/// <summary>
	/// Advances playback to the next song in the playlist and resets playback progress.
	/// </summary>
	/// <remarks>This method stops any active midpoint timer, resets the playback tracker, and updates the progress
	/// bar to the beginning of the next song. Use this command to skip to the next track during playback.</remarks>
	[RelayCommand]
	private void NextSong()
	{
		ProgressBarValue = 0;
		_midpointTimer?.Stop();
		_midpointTimer?.Tick -= MidpointTimer_Tick;
		_playbackTracker.Reset();
		_musicPlayer.Next();
	}

	/// <summary>
	/// Skips to the previous song in the playback queue and resets playback progress.
	/// </summary>
	/// <remarks>Calling this method resets the progress bar and playback tracker to the beginning of the previous
	/// song. If the current song is the first in the queue, behavior depends on the implementation of the underlying music
	/// player.</remarks>
	[RelayCommand]
	private void PreviousSong()
	{
		ProgressBarValue = 0;
		_midpointTimer?.Stop();
		_midpointTimer?.Tick -= MidpointTimer_Tick;
		_playbackTracker.Reset();
		_musicPlayer.Previous();
	}

	/// <summary>
	/// Advances the current song progress by one unit.
	/// </summary>
	/// <remarks>This method is typically used to move the playback position forward, such as when handling a user
	/// action to skip ahead. The actual effect depends on how the progress bar value is interpreted in the
	/// application.</remarks>
	[RelayCommand]
	private void ForwardSong() => ProgressBarValue++;

	/// <summary>
	/// Moves the current song position backward by one unit.
	/// </summary>
	/// <remarks>This method is typically used to rewind playback in a media player interface. If the song is
	/// already at the beginning, further calls may have no effect depending on the implementation of the progress
	/// bar.</remarks>
	[RelayCommand]
	private void RewindSong() => ProgressBarValue--;

	// ─────────────────────────────────────────────────────────
	//  Shuffle / Repeat
	// ─────────────────────────────────────────────────────────
	/// <summary>
	/// Toggles the shuffle mode for music playback, optionally setting the shuffle state explicitly.
	/// </summary>
	/// <remarks>The shuffle state is persisted to local application settings. This method updates the shuffle
	/// button tooltip to reflect the current state.</remarks>
	/// <param name="shuffleSaved">If specified, determines whether shuffle mode is enabled. If null, the current shuffle state is toggled.</param>
	[RelayCommand]
	public void ShuffleToggle(bool? shuffleSaved = null)
	{
		IsShuffleToggled = shuffleSaved ?? IsShuffleToggled;
		_musicPlayer.ToggleShuffle(IsShuffleToggled ? ShuffleMode.On : ShuffleMode.Off);
		ToolTipTextShuffleButton = IsShuffleToggled ? "Shuffle On" : "Shuffle Off";
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.ShuffleStatus)] = IsShuffleToggled;

		if (_overlayGrid is not null && _overlayGrid is QueuePreviewOverlay)
			CurrentSongInfoForUpdateOverlay();

	}

	/// <summary>
	/// Toggles the repeat mode of the music player or sets it to a specified mode.
	/// </summary>
	/// <remarks>This method updates the music player's repeat mode and synchronizes the UI to reflect the current
	/// state. The selected repeat mode is also saved to local application settings for persistence across
	/// sessions.</remarks>
	/// <param name="repeatSaved">The repeat mode to set. If null, the repeat mode cycles through All, One, and None in sequence.</param>
	[RelayCommand]
	private void RepeatButtonToggle(RepeatMode? repeatSaved = null)
	{
		RepeatMode repeatMode = RepeatMode.All;
		if (repeatSaved != null)
			repeatMode = repeatSaved.Value;
		else
		{
			if (_musicPlayer.RepeatStatus == RepeatMode.All) repeatMode = RepeatMode.One;
			else if (_musicPlayer.RepeatStatus == RepeatMode.One) repeatMode = RepeatMode.None;
			else if (_musicPlayer.RepeatStatus == RepeatMode.None) repeatMode = RepeatMode.All;
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

		if (_overlayGrid is not null && _overlayGrid is QueuePreviewOverlay)
			CurrentSongInfoForUpdateOverlay();
	}

	// ─────────────────────────────────────────────────────────
	//  Seek / scrub
	// ─────────────────────────────────────────────────────────
	/// <summary>
	/// Seeks the active backend to ProgressBarValue (in seconds).
	/// Uses Normal priority and briefly mutes to avoid audio artifacts.
	/// </summary>
	private void UpdatePlaybackPosition()
	{
		_dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, async () =>
		{
			_musicPlayer.Volume = 0;
			_musicPlayer.CurTimeTicks = TimeSpan.FromSeconds(ProgressBarValue).Ticks;
			await Task.Delay(100);
			_musicPlayer.Volume = 100;
		});
	}

	// ─────────────────────────────────────────────────────────
	//  Media opened — update UI metadata
	// ─────────────────────────────────────────────────────────
	/// <summary>
	/// Handles the media opened event for the playback session and updates the user interface with the current song's
	/// metadata asynchronously.
	/// </summary>
	/// <remarks>This method retrieves the current song information, updates playback tracking, and refreshes UI
	/// elements such as the title, artist, and cover art. It also manages playback-related timers and effects. This method
	/// is intended to be called when a new media file is successfully opened for playback.</remarks>
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
				ProgressBar?.NotifyTrackChanged(DurationOfSong);
				if (_musicPlayer.IsPlaying)
					ProgressBar?.NotifyPlaying();
				ProgressBar?.SetInitialPosition(_startupPosition);

				if (_startupPosition > 0)
					_startupPosition = 0;

				_playbackTracker.Reset();
				_thresoldDuration = TimeSpan.FromSeconds(DurationOfSong * 0.6);
				_midpointTimer?.Stop();

				if (_musicPlayer.IsPlaying)
					_playbackTracker.StartPlayback();

				_midpointTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
				_midpointTimer.Tick -= MidpointTimer_Tick;
				_midpointTimer.Tick += MidpointTimer_Tick;
				_midpointTimer.Start();

				Title = track.Title;
				Artist = track.Artists;
				Cover = track.Cover;
				UpdateInfoOnTaskbarOverlay(track);
			}

			_vinylEffect?.Begin();
			if (!_musicPlayer.IsPlaying) _vinylEffect?.Pause();
			MusicControl._instance?.FloatingPlayer(null, MainPage._instance?.IsMainPlayerPageOpened ?? false);
			MusicControl._instance?.SlideInDown();
		});
	}

	// ─────────────────────────────────────────────────────────
	//  Midpoint play-count timer
	// ─────────────────────────────────────────────────────────
	private async void MidpointTimer_Tick(object? sender, object e)
	{
		if (_playbackTracker.AlreadyCounted)
		{
			_midpointTimer?.Stop();
			_midpointTimer?.Tick -= MidpointTimer_Tick;
			return;
		}

		// Use the unified MusicPlayer surface — no direct Flyleaf Player reference
		if (_musicPlayer.IsPlaying &&
			TimeSpan.FromTicks(_musicPlayer.CurTimeTicks) >= _thresoldDuration &&
			_playbackTracker.GetTotalPlayTime() >= _thresoldDuration)
		{
			_midpointTimer?.Stop();
			_midpointTimer?.Tick -= MidpointTimer_Tick;
			_playbackTracker.MarkPlayCountRecorded();
			await DatabaseHelper.Instance.IncrementPlayCount(_musicPlayer.CurrentSong);
			await DatabaseHelper.Instance.UpdateDateLastPlayed(_musicPlayer.CurrentSong);
		}
	}

	// ─────────────────────────────────────────────────────────
	//  Shuffle event from MusicPlayer
	// ─────────────────────────────────────────────────────────
	/// <summary>
	/// Handles the event that occurs when the shuffle mode of the music player changes.
	/// </summary>
	/// <param name="sender">The source of the event, typically the music player instance.</param>
	/// <param name="e">The new shuffle mode value indicating whether shuffle is enabled or disabled.</param>
	private void _musicPlayer_ShuffleStatusChanged(object? sender, ShuffleMode e)
		=> ShuffleToggle(e == ShuffleMode.On);

	// ─────────────────────────────────────────────────────────
	//  Storyboard / vinyl effect
	// ─────────────────────────────────────────────────────────
	/// <summary>
	/// Updates the current storyboard used for the vinyl effect.
	/// </summary>
	/// <param name="storyboard">The storyboard to apply to the vinyl effect. Can be null to remove the current storyboard.</param>
	public void UpdateStoryBoard(Storyboard? storyboard) => _vinylEffect = storyboard;

	/// <summary>
	/// Resets the floating window displaying the current song to its default state.
	/// </summary>
	/// <remarks>This method clears the song information, resets the cover image to the default application icon,
	/// and stops any active vinyl effect. It also updates the floating player UI to prompt the user to select a song. This
	/// method is asynchronous but returns void, so exceptions may not be observed by callers.</remarks>
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

	private void SetupTaskbarThumbnailToolBar()
	{
		var prevButton = new ThumbnailToolBarButton(
		new Icon(Path.Combine(AppContext.BaseDirectory, "Assets", "Fluent", $"prev_{(App.Current.ThemeService.IsDark ? "light" : "dark")}.ico")), "Previous");

		Play_Pause_Button = new ThumbnailToolBarButton(
		new Icon(Path.Combine(AppContext.BaseDirectory, "Assets", "Fluent", $"{(_musicPlayer.IsPlaying ? "pause" : "play")}_{(App.Current.ThemeService.IsDark ? "light" : "dark")}.ico")), "Play");

		var nextButton = new ThumbnailToolBarButton(
			new Icon(Path.Combine(AppContext.BaseDirectory, "Assets", "Fluent", $"next_{(App.Current.ThemeService.IsDark ? "light" : "dark")}.ico")), "Next");


		prevButton.Click += (s, e) => PreviousSong();
		Play_Pause_Button.Click += async (s, e) => await TogglePlayPause();
		nextButton.Click += (s, e) => NextSong();

		TaskbarManager.Instance.ThumbnailToolBars.AddButtons(App.Hwnd, prevButton, Play_Pause_Button, nextButton);

		App.Current.ThemeService.ThemeChanged += (s, e) =>
		{
			prevButton.Icon = new Icon(Path.Combine(AppContext.BaseDirectory, "Assets", "Fluent", $"prev_{(App.Current.ThemeService.IsDark ? "light" : "dark")}.ico"));
			Play_Pause_Button.Icon = new Icon(Path.Combine(AppContext.BaseDirectory, "Assets", "Fluent", $"{(_musicPlayer.IsPlaying ? "pause" : "play")}_{(App.Current.ThemeService.IsDark ? "light" : "dark")}.ico"));
			nextButton.Icon = new Icon(Path.Combine(AppContext.BaseDirectory, "Assets", "Fluent", $"next_{(App.Current.ThemeService.IsDark ? "light" : "dark")}.ico"));
		};
	}

	// ─────────────────────────────────────────────────────────
	//  Taskbar Overlay
	// ─────────────────────────────────────────────────────────

	public async void SetupTaskbarOverlay()
	{
		var theme = Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.TaskBarOverlayTheme)]?.ToString() ?? "LightTBOL";

		_uiSettings.ColorValuesChanged -= _uiSettings_ColorValuesChanged;

		switch (theme)
		{
			case "LightTBOL":
				OverlayGridCreation(OverlayTheme.Light);
				break;

			case "DarkTBOL":
				OverlayGridCreation(OverlayTheme.Dark);
				break;

			default:
			case "DefaultTBOL":
				_uiSettings.ColorValuesChanged += _uiSettings_ColorValuesChanged;
				_uiSettings_ColorValuesChanged(_uiSettings, null);
				return;
		}

		SetContentAndUpdateLayoutWithData();
	}

	private void _uiSettings_ColorValuesChanged(UISettings sender, object? args)
	{
		_dispatcherQueue.TryEnqueue(() =>
		{
			bool isDark = sender.GetColorValue(UIColorType.Background) == Windows.UI.Color.FromArgb(255, 0, 0, 0);
			OverlayGridCreation(isDark ? OverlayTheme.Dark : OverlayTheme.Light);
			SetContentAndUpdateLayoutWithData();
		});
	}

	private void OverlayGridCreation(OverlayTheme actualTheme)
	{
		var overlay = OverlayLayoutCatalog.All.FirstOrDefault(item => item.DisplayName == (Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.TaskBarOverlayDesign)]?.ToString() ?? "Compact Pill"))?.Layout;
		_overlayGrid = OverlayFactory.Create(overlay, actualTheme);

		if (_overlayGrid.RootGrid is not null)
		{
			_overlayGrid.PlayPauseButton?.Click += async (_, _) => await TogglePlayPause();
			_overlayGrid.PreviousButton?.Click += (_, _) => PreviousSong();
			_overlayGrid.NextButton?.Click += (_, _) => NextSong();
		}
	}

	public async void CurrentSongInfoForUpdateOverlay()
	{
		var song = Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.LastPlayedTrack)]?.ToString();
		if (string.IsNullOrEmpty(song)) return;

		var track = await DatabaseHelper.Instance.GetSongByPath(song);
		if (track is not null)
			UpdateInfoOnTaskbarOverlay(track);
	}

	private void SetContentAndUpdateLayoutWithData()
	{
		if (_overlayGrid is not null && _overlayGrid.RootGrid is not null)
			TaskbarOverlayManager.SetContent(_overlayGrid.RootGrid);

		CurrentSongInfoForUpdateOverlay();
	}

	private async void UpdateInfoOnTaskbarOverlay(Song track)
	{
		if (_overlayGrid is null) return;

		var albumArt = await GetAlbumArt(track.Cover);

		switch (_overlayGrid)
		{
			case CompactPillOverlay:
			case HoverRevealOverlay:
			case RightDockOverlay:
			case FullArtBarOverlay:
			case CenteredPillOverlay:
			case TopAccentStripeOverlay:
			case BottomAccentStripeOverlay:
			case ArcRingOverlay:
			case IconStripOverlay:
				((dynamic)_overlayGrid).UpdateTrack(track.Title, track.Artists, track.Album, albumArt);
				break;

			case TextOnlyOverlay:
			case TextOnlyReversedOverlay:
			case MarqueeTickerOverlay:
				((dynamic)_overlayGrid).UpdateTrack(track.Title, track.Artists, track.Album);
				break;

			case QueuePreviewOverlay qpo:
				var nextSongs = await _musicPlayer.GetUpcomingSongs();

				BitmapImage? nextSongArt1 = null, nextSongArt2 = null;

				if (nextSongs is not null && nextSongs.Count > 0)
				{
					nextSongArt1 = await GetAlbumArt(nextSongs[0].Cover);

					if (nextSongs.Count > 1)
						nextSongArt2 = await GetAlbumArt(nextSongs[1].Cover);
				}

				qpo.UpdateTrack(track.Title, track.Artists, track.Album, albumArt, nextSongArt1, nextSongArt2);
				break;

			case AccentAncientScrollOverlay:
			case AlbumTintOverlay:
			case TopAlbumAccentStripeOverlay:
				((dynamic)_overlayGrid).UpdateTrack(track.Title, track.Artists, track.Album, track.Cover);
				break;
		}
		_overlayGrid.UpdateProgress(ProgressBarValue / DurationOfSong);

		static async Task<BitmapImage?> GetAlbumArt(string coverArt)
		{
			BitmapImage? albumArt = null;
			try
			{
				StorageFile file = await StorageFile.GetFileFromPathAsync(coverArt);
				using IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read);
				albumArt = new BitmapImage();
				await albumArt.SetSourceAsync(stream);
			}
			catch (Exception)
			{
				albumArt = null;
			}

			return albumArt;
		}
	}
}
