using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Media;


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

	public Storyboard? _vinylEffect { get; set; }

	public MusicControlViewModel()
	{
		_dispatcherQueue = DispatcherQueue.GetForCurrentThread();

		_musicPlayer.PlaybackStateChanged += OnPlaybackStateChanged;
		_musicPlayer.OpenCompleted += (s, e) => PlaybackSession_MediaOpenedAsync();
		_musicPlayer.PositionChanged += OnPositionChanged;

		//TODO pause on mute
		_musicPlayer.ShuffleStatusChanged += _musicPlayer_ShuffleStatusChanged;

		SetShuffleAndRepeat();
		_ = LoadLastPlayedTrack();

		App.TrayIcon.MouseClick += (s, e) =>
		{
			if (e.Button == System.Windows.Forms.MouseButtons.Left)
				TogglePlayPause();
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

	// ─────────────────────────────────────────────────────────
	//  Event handlers from unified MusicPlayer surface
	// ─────────────────────────────────────────────────────────

	/// <summary>
	/// Handles unified PlaybackState changes. Replaces the old
	/// MediaPlayer_PropertyChanged / FlyleafLib.MediaPlayer.Status switch.
	/// </summary>
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

					MusicPlayer.Instance.SMTC.PlaybackStatus = MediaPlaybackStatus.Paused;
					MainPage._instance.AnimateTitle(startAnimation: false);
					TaskbarHelper.SetProgressState(App.Hwnd, TaskbarStates.Paused);

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

					MusicPlayer.Instance.SMTC.PlaybackStatus = MediaPlaybackStatus.Playing;
					MainPage._instance.AnimateTitle(startAnimation: true);
					TaskbarHelper.SetProgressState(App.Hwnd, TaskbarStates.Normal);

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
	/// Handles position ticks from the active backend (~once per second).
	/// Replaces the throttled PlaybackSession_PositionChanged from before.
	/// Throttling is now done inside each backend — no extra throttle needed here.
	/// </summary>
	private void OnPositionChanged(object? sender, long ticks)
	{
		_dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
		{
			isUpdatingProgressBar = true;
			ProgressBarValue = TimeSpan.FromTicks(ticks).TotalSeconds;
			TaskbarHelper.SetProgressValue(App.Hwnd, ProgressBarValue / DurationOfSong * 100, 100);
			isUpdatingProgressBar = false;
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
			DurationOfSong = track.Duration;
			ProgressBarValue = position;
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
			MusicPlayer.Instance.SMTC.PlaybackStatus = MediaPlaybackStatus.Paused;
			_musicPlayer.Pause();
			_playbackTracker.PausePlayback();
		}
		else
		{
			MusicPlayer.Instance.SMTC.PlaybackStatus = MediaPlaybackStatus.Playing;
			_musicPlayer.Play(playBackPosition: ProgressBarValue);
			_playbackTracker.StartPlayback();
		}
	}

	// ─────────────────────────────────────────────────────────
	//  Rainbow frame helpers
	// ─────────────────────────────────────────────────────────
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
	[RelayCommand]
	private void NextSong()
	{
		ProgressBarValue = 0;
		_midpointTimer?.Stop();
		_playbackTracker.Reset();
		_musicPlayer.Next();
	}

	[RelayCommand]
	private void PreviousSong()
	{
		ProgressBarValue = 0;
		_midpointTimer?.Stop();
		_playbackTracker.Reset();
		_musicPlayer.Previous();
	}

	[RelayCommand]
	private void ForwardSong() => ProgressBarValue++;

	[RelayCommand]
	private void RewindSong() => ProgressBarValue--;

	// ─────────────────────────────────────────────────────────
	//  Shuffle / Repeat
	// ─────────────────────────────────────────────────────────
	[RelayCommand]
	public void ShuffleToggle(bool? shuffleSaved = null)
	{
		IsShuffleToggled = shuffleSaved ?? IsShuffleToggled;
		_musicPlayer.ToggleShuffle(IsShuffleToggled ? ShuffleMode.On : ShuffleMode.Off);
		ToolTipTextShuffleButton = IsShuffleToggled ? "Shuffle On" : "Shuffle Off";
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.ShuffleStatus)] = IsShuffleToggled;
	}

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
			_musicPlayer.Volume = 75;
		});
	}

	// ─────────────────────────────────────────────────────────
	//  Media opened — update UI metadata
	// ─────────────────────────────────────────────────────────
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

				if (_musicPlayer.IsPlaying)
					_playbackTracker.StartPlayback();

				_midpointTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
				_midpointTimer.Tick += MidpointTimer_Tick;
				_midpointTimer.Start();

				Title = track.Title;
				Artist = track.Artists;
				Cover = track.Cover;
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
			return;
		}

		// Use the unified MusicPlayer surface — no direct Flyleaf Player reference
		if (_musicPlayer.IsPlaying &&
			TimeSpan.FromTicks(_musicPlayer.CurTimeTicks) >= _thresoldDuration &&
			_playbackTracker.GetTotalPlayTime() >= _thresoldDuration)
		{
			_midpointTimer?.Stop();
			_playbackTracker.MarkPlayCountRecorded();
			await DatabaseHelper.Instance.IncrementPlayCount(_musicPlayer.CurrentSong);
			await DatabaseHelper.Instance.UpdateDateLastPlayed(_musicPlayer.CurrentSong);
		}
	}

	// ─────────────────────────────────────────────────────────
	//  Shuffle event from MusicPlayer
	// ─────────────────────────────────────────────────────────
	private void _musicPlayer_ShuffleStatusChanged(object? sender, ShuffleMode e)
		=> ShuffleToggle(e == ShuffleMode.On);

	// ─────────────────────────────────────────────────────────
	//  Storyboard / vinyl effect
	// ─────────────────────────────────────────────────────────
	public void UpdateStoryBoard(Storyboard? storyboard) => _vinylEffect = storyboard;

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
