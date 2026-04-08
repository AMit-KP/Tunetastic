using FlyleafLib;
using FlyleafLib.MediaPlayer;
using Windows.Media;
using Windows.Media.Playback;

namespace Tunetastic.Common;

// ─────────────────────────────────────────────────────────────
//  Unified playback state (replaces FlyleafLib.MediaPlayer.Status)
// ─────────────────────────────────────────────────────────────
public enum PlaybackState { Playing, Paused, Stopped, Ended }


public class PlaybackStateChangedArgs : EventArgs
{
	/// <summary>
	/// Gets the current playback state.
	/// </summary>
	public PlaybackState State { get; }
	/// <summary>
	/// Initializes a new instance of the <see cref="PlaybackStateChangedArgs"/> class with the specified playback state.
	/// </summary>
	/// <param name="state">The new playback state.</param>
	public PlaybackStateChangedArgs(PlaybackState state) => State = state;
}

// ─────────────────────────────────────────────────────────────
//  Internal backend abstraction
// ─────────────────────────────────────────────────────────────
internal interface IMediaBackend : IDisposable
{
	bool IsPlaying { get; }
	long CurTimeTicks { get; set; }
	int Volume { get; set; }
	bool IsMuted { get; set; }

	Task OpenAsync(string path);
	void Play();
	void Pause();
	void Stop();

	event EventHandler<PlaybackStateChangedArgs>? StateChanged;
	event EventHandler? OpenCompleted;
	event EventHandler<long>? PositionChanged;
}

// ─────────────────────────────────────────────────────────────
//  Windows MediaPlayer backend
// ─────────────────────────────────────────────────────────────
internal sealed class WindowsMediaBackend : IMediaBackend
{
	private readonly Windows.Media.Playback.MediaPlayer _player;
	private System.Threading.Timer? _positionTimer;
	private bool _disposed;
	private volatile bool _isLoading = false;
	public double PendingStartPosition = 0.0;

	public event EventHandler<PlaybackStateChangedArgs>? StateChanged;
	public event EventHandler? OpenCompleted;
	public event EventHandler<long>? PositionChanged;

	public WindowsMediaBackend(Windows.Media.Playback.MediaPlayer player)
	{
		_player = player;
		_player.MediaOpened += OnMediaOpened;
		_player.MediaEnded += OnMediaEnded;
		_player.PlaybackSession.PlaybackStateChanged += OnPlaybackStateChanged;

		_positionTimer = new System.Threading.Timer(_ =>
		{
			if (_player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
				PositionChanged?.Invoke(this, _player.PlaybackSession.Position.Ticks);
		}, null, Timeout.Infinite, Timeout.Infinite);
	}

	public bool IsPlaying =>
		_player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing;

	public long CurTimeTicks
	{
		get => _player.PlaybackSession.Position.Ticks;
		set => _player.PlaybackSession.Position = TimeSpan.FromTicks(value);
	}

	public int Volume
	{
		get => (int)(_player.Volume * 100);
		set => _player.Volume = Math.Clamp(value, 0, 100) / 100.0;
	}

	public bool IsMuted
	{
		get => _player.IsMuted;
		set => _player.IsMuted = value;
	}

	public Task OpenAsync(string path)
	{
		_isLoading = true;
		_player.Source = Windows.Media.Core.MediaSource.CreateFromUri(new Uri(path));
		return Task.CompletedTask;
	}

	public void Play()
	{
		_player.Play();
		_positionTimer?.Change(0, 1000);
	}

	public void Pause()
	{
		_player.Pause();
		_positionTimer?.Change(Timeout.Infinite, Timeout.Infinite);
	}

	public void Stop()
	{
		_player.Source = null;
		_positionTimer?.Change(Timeout.Infinite, Timeout.Infinite);
	}

	private void OnMediaOpened(Windows.Media.Playback.MediaPlayer sender, object args)
	{
		_isLoading = false;
		if (PendingStartPosition > 0)
		{
			_player.PlaybackSession.Position = TimeSpan.FromSeconds(PendingStartPosition);
			PendingStartPosition = 0.0;
		}
		OpenCompleted?.Invoke(this, EventArgs.Empty);
	}

	private void OnMediaEnded(Windows.Media.Playback.MediaPlayer sender, object args)
	{
		_positionTimer?.Change(Timeout.Infinite, Timeout.Infinite);
		StateChanged?.Invoke(this, new PlaybackStateChangedArgs(PlaybackState.Ended));
	}

	private void OnPlaybackStateChanged(MediaPlaybackSession sender, object args)
	{
		if (_isLoading) return;

		var state = sender.PlaybackState switch
		{
			MediaPlaybackState.Playing => PlaybackState.Playing,
			MediaPlaybackState.Paused => PlaybackState.Paused,
			_ => PlaybackState.Stopped
		};
		StateChanged?.Invoke(this, new PlaybackStateChangedArgs(state));
	}

	public void Dispose()
	{
		if (_disposed) return;
		_disposed = true;
		_positionTimer?.Dispose();
		_player.MediaOpened -= OnMediaOpened;
		_player.MediaEnded -= OnMediaEnded;
		_player.PlaybackSession.PlaybackStateChanged -= OnPlaybackStateChanged;
	}
}

// ─────────────────────────────────────────────────────────────
//  Flyleaf backend
// ─────────────────────────────────────────────────────────────
internal sealed class FlyleafMediaBackend : IMediaBackend
{
	private readonly Player _player;
	private bool _disposed;

	// Throttle: fire PositionChanged at most ~once per second
	private long _lastPosTick;
	private static readonly long _posIntervalTicks = TimeSpan.FromMilliseconds(950).Ticks;

	public event EventHandler<PlaybackStateChangedArgs>? StateChanged;
	public event EventHandler? OpenCompleted;
	public event EventHandler<long>? PositionChanged;

	public FlyleafMediaBackend(Player player)
	{
		_player = player;
		_player.OpenCompleted += OnOpenCompleted;
		_player.PropertyChanged += OnPropertyChanged;
	}

	public bool IsPlaying => _player.IsPlaying;
	public long CurTimeTicks
	{
		get => _player.CurTime;
		set => _player.CurTime = value;
	}

	public int Volume
	{
		get => _player.Audio.Volume;
		set => _player.Audio.Volume = value;
	}

	public bool IsMuted
	{
		get => _player.Audio.Mute;
		set => _player.Audio.Mute = value;
	}

	public async Task OpenAsync(string path)
		=> await Task.Run(() => _player.Open(path));

	public void Play() => _player.Play();
	public void Pause() => _player.Pause();
	public void Stop()
	{
		_player.Stop();
	}

	private void OnOpenCompleted(object? s, OpenCompletedArgs e)
		=> OpenCompleted?.Invoke(this, EventArgs.Empty);

	private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(_player.Status):
				var state = _player.Status switch
				{
					FlyleafLib.MediaPlayer.Status.Playing => PlaybackState.Playing,
					FlyleafLib.MediaPlayer.Status.Paused => PlaybackState.Paused,
					FlyleafLib.MediaPlayer.Status.Ended => PlaybackState.Ended,
					_ => PlaybackState.Stopped
				};
				StateChanged?.Invoke(this, new PlaybackStateChangedArgs(state));
				break;

			case nameof(_player.CurTime):
				long now = System.Diagnostics.Stopwatch.GetTimestamp();
				long elapsed = (now - _lastPosTick) *
							   (TimeSpan.TicksPerSecond / System.Diagnostics.Stopwatch.Frequency);
				if (elapsed < _posIntervalTicks) break;
				_lastPosTick = now;
				PositionChanged?.Invoke(this, _player.CurTime);
				break;
		}
	}

	public void Dispose()
	{
		if (_disposed) return;
		_disposed = true;
		_player.OpenCompleted -= OnOpenCompleted;
		_player.PropertyChanged -= OnPropertyChanged;
	}
}

// ─────────────────────────────────────────────────────────────
//  Backend type enum
// ─────────────────────────────────────────────────────────────
public enum BackendType { Windows, Flyleaf, Unsupported }

// ─────────────────────────────────────────────────────────────
//  MusicPlayer singleton
// ─────────────────────────────────────────────────────────────
/// <summary>
/// Singleton music player that routes audio to the Windows MediaPlayer backend
/// for common formats (smooth UI, no stutter) and falls back to FlyleafLib for
/// formats not natively supported by Windows Media Foundation.
/// </summary>
public class MusicPlayer
{
	private static MusicPlayer? _instance;

	// ── backends ─────────────────────────────────────────────
	// The Windows.Media.Playback.MediaPlayer serves dual purpose:
	//   • Always: provides SMTC (system media transport controls)
	//   • When Windows backend active: also outputs audio at full volume
	//   • When Flyleaf backend active: kept at Volume=0 for SMTC only
	public MediaPlayer SMTCPlayer { get; private set; } = null!;

	/// <summary>
	/// The underlying Flyleaf Player. Kept internal; external code should use
	/// the MusicPlayer surface (IsPlaying, CurTimeTicks, etc.).
	/// Exposed only for legacy compatibility — prefer the abstracted surface.
	/// </summary>
	internal Player FlyleafPlayer { get; private set; } = null!;

	private WindowsMediaBackend _windowsBackend = null!;
	private FlyleafMediaBackend _flyleafBackend = null!;
	private IMediaBackend _activeBackend = null!;
	private BackendType _activeBackendType = BackendType.Windows;

	// ── public surface replacing direct MediaPlayer access ───

	/// <summary>Whether audio is currently playing on the active backend.</summary>
	public bool IsPlaying => _activeBackend?.IsPlaying ?? false;

	/// <summary>Current playback position in ticks (same unit as TimeSpan.Ticks).</summary>
	public long CurTimeTicks
	{
		get => _activeBackend?.CurTimeTicks ?? 0;
		set { if (_activeBackend != null) _activeBackend.CurTimeTicks = value; }
	}

	/// <summary>Playback volume 0–100.</summary>
	public int Volume
	{
		get => _activeBackend?.Volume ?? 0;
		set { if (_activeBackend != null) _activeBackend.Volume = value; }
	}

	// ── events replacing MediaPlayer.PropertyChanged ─────────

	/// <summary>Fires when playback state changes (Playing / Paused / Stopped / Ended).</summary>
	public event EventHandler<PlaybackStateChangedArgs>? PlaybackStateChanged;

	/// <summary>Fires when a new song finishes opening and is ready.</summary>
	public event EventHandler? OpenCompleted;

	/// <summary>Fires ~once per second with the current position in ticks while playing.</summary>
	public event EventHandler<long>? PositionChanged;

	// ── existing public events / properties ──────────────────
	public event EventHandler<string>? CurrentSongChanged;
	public event EventHandler<ShuffleMode>? ShuffleStatusChanged;

	private string _currentSong = string.Empty;
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

	private ShuffleMode _shuffleStatus = ShuffleMode.Off;
	public ShuffleMode ShuffleStatus
	{
		get => _shuffleStatus;
		set
		{
			if (_shuffleStatus != value)
			{
				_shuffleStatus = value;
				ShuffleStatusChanged?.Invoke(this, _shuffleStatus);
			}
		}
	}

	public RepeatMode RepeatStatus { get; private set; } = RepeatMode.All;

	/// <summary>SMTC controls, always available via SMTCPlayer.</summary>
	public SystemMediaTransportControls? SMTC { get; private set; }

	private bool isFading = false;
	private const int initialVolume = 100;

	// ── playlist state ────────────────────────────────────────
	private List<string>? OriginalPlaylist;
	private List<string>? ActualPlaylist;
	private bool SongQueue = false;
	private int currentIndex = 0;
	private bool alreadyPlayed = false;

	// ─────────────────────────────────────────────────────────
	//  Construction
	// ─────────────────────────────────────────────────────────
	private MusicPlayer()
	{
		var ffmpegPath = Path.Combine(AppContext.BaseDirectory, "Assets", "FFmpeg");
		Engine.Start(new EngineConfig
		{
			UIRefresh = false,
			FFmpegPath = ffmpegPath,
		});

		var config = new FlyleafLib.Config();
		config.Video.Enabled = false;
		config.Audio.Enabled = true;
		config.Player.AutoPlay = false;
		config.Player.ThreadPriority = System.Threading.ThreadPriority.AboveNormal;
		config.Player.MinBufferDuration = TimeSpan.FromSeconds(6).Ticks;
		config.Demuxer.BufferDuration = TimeSpan.FromSeconds(10).Ticks;
		FlyleafPlayer = new Player(config);

		SMTCSetup();

		_windowsBackend = new WindowsMediaBackend(SMTCPlayer);
		_flyleafBackend = new FlyleafMediaBackend(FlyleafPlayer);

		ActivateBackend(BackendType.Windows);
	}

	public static MusicPlayer Instance
	{
		get
		{
			_instance ??= new MusicPlayer();
			return _instance;
		}
	}

	// ─────────────────────────────────────────────────────────
	//  Backend switching
	// ─────────────────────────────────────────────────────────
	private void ActivateBackend(BackendType type)
	{
		if (_activeBackend != null)
		{
			_activeBackend.StateChanged -= OnBackendStateChanged;
			_activeBackend.OpenCompleted -= OnBackendOpenCompleted;
			_activeBackend.PositionChanged -= OnBackendPositionChanged;
		}

		_activeBackendType = type;
		_activeBackend = type == BackendType.Flyleaf ? (IMediaBackend)_flyleafBackend : _windowsBackend;

		// When Flyleaf is active, keep SMTCPlayer silent (SMTC-only dummy)
		// When Windows is active, SMTCPlayer outputs real audio — volume handled by Play/Pause
		if (type == BackendType.Flyleaf)
			SMTCPlayer.Volume = 0;

		_activeBackend.StateChanged += OnBackendStateChanged;
		_activeBackend.OpenCompleted += OnBackendOpenCompleted;
		_activeBackend.PositionChanged += OnBackendPositionChanged;
	}

	private void OnBackendStateChanged(object? s, PlaybackStateChangedArgs e) => PlaybackStateChanged?.Invoke(this, e);

	private void OnBackendOpenCompleted(object? s, EventArgs e) => OpenCompleted?.Invoke(this, e);

	private void OnBackendPositionChanged(object? s, long ticks) => PositionChanged?.Invoke(this, ticks);

	// ─────────────────────────────────────────────────────────
	//  SMTC setup
	// ─────────────────────────────────────────────────────────
	private void SMTCSetup()
	{
		SMTCPlayer = new MediaPlayer();
		SMTCPlayer.AutoPlay = false;
		SMTCPlayer.AudioCategory = MediaPlayerAudioCategory.Media;
		SMTCPlayer.Volume = 0;
		SMTCPlayer.CommandManager.IsEnabled = false;
		SMTC = SMTCPlayer.SystemMediaTransportControls;
		SMTC.IsPlayEnabled = true;
		SMTC.IsPauseEnabled = true;
		SMTC.IsNextEnabled = true;
		SMTC.IsPreviousEnabled = true;
		SMTC.IsEnabled = true;
	}

	// ─────────────────────────────────────────────────────────
	//  Playlist loading
	// ─────────────────────────────────────────────────────────
	/// <summary>
	/// Loads a playlist into the music player from a list of song file paths and optionally starts playback.
	/// This method also allows resuming from the currently playing song without reloading, or specifying a song to start playback.
	/// </summary>
	/// <param name="songPaths">A list of file paths representing the songs in the playlist.</param>
	/// <param name="startingSong">
	/// An optional parameter specifying the file path of the song to start playback.
	/// If null, playback starts from the first song in the playlist.
	/// </param>
	/// <param name="play">
	/// A boolean indicating whether playback should start immediately after loading the playlist.
	/// Defaults to true.
	/// </param>
	/// <param name="dontReloadCurrent">
	/// A boolean indicating whether to avoid reloading the currently playing song if it's part of the new playlist.
	/// Defaults to false.
	/// </param>
	public async void LoadPlaylist(List<string> songPaths, string? startingSong = null, bool play = true, bool dontReloadCurrent = false)
	{
		await LoadSong(startingSong ?? songPaths[0], play, dontReloadCurrent: dontReloadCurrent);
		_ = Task.Run(() =>
		{
			OriginalPlaylist = new List<string>(songPaths);
			ShuffleSongs(startingSong);
		});
	}

	/// <summary>
	/// Loads a music playlist based on the user's configuration and preferences.
	/// The method determines the appropriate playlist or custom playlists and prepares it for playback. Optionally, starts playback from the specified starting song.
	/// </summary>
	/// <param name="startingSong">The path of the song to start playback from, or null to start with the default song.</param>
	/// <param name="play">Indicates whether to start playback automatically after loading the playlist. Default is true.</param>
	/// <param name="dontReloadCurrent">If true, prevents reloading the current playing song if it is already loaded. Default is false.</param>
	public async void LoadPlaylist(string? startingSong, bool play = true, bool dontReloadCurrent = false, bool startup = false)
	{
		await LoadSong(startingSong, play, dontReloadCurrent: dontReloadCurrent, startup: startup);

		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		List<string> list = new();

		switch (localSettings.Values[nameof(LocalSave.CurrentPlayinglist)]?.ToString())
		{
			case "AllSongsViewPage":
				list = (await DatabaseHelper.Instance.LoadSongsFromDB(
					orderBy: Enum.Parse<SongProperty>(localSettings.Values[nameof(LocalSave.AllSongViewSortBy)]?.ToString() ?? "Title"),
					ascending: (localSettings.Values[nameof(LocalSave.AllSongViewSortOrder)]?.ToString() ?? "Ascending") == "Ascending"))
					.Select(s => s.Path).ToList();
				break;

			case var artist when artist?.StartsWith("ArtistGroup>") == true:
				list = (await DatabaseHelper.Instance.GetSongsByArtist(
					artistName: artist?["ArtistGroup>".Length..] == "Unknown" ? "Unknown Artist" : artist?["ArtistGroup>".Length..],
					orderBy: Enum.Parse<SongProperty>(localSettings.Values[nameof(LocalSave.ArtistDetailViewSortBy)]?.ToString() ?? "Title"),
					ascending: (localSettings.Values[nameof(LocalSave.ArtistDetailViewSortOrder)]?.ToString() ?? "Ascending") == "Ascending"))
					.Select(s => s.Path).ToList();
				break;

			case var album when album?.StartsWith("AlbumGroup>") == true:
				list = (await DatabaseHelper.Instance.LoadSongsFromDB(
					orderBy: Enum.Parse<SongProperty>(localSettings.Values[nameof(LocalSave.AlbumDetailViewSortBy)]?.ToString() ?? "Title"),
					ascending: (localSettings.Values[nameof(LocalSave.AlbumDetailViewSortOrder)]?.ToString() ?? "Ascending") == "Ascending",
					whereCondition: $"{SongProperty.Album.ToString()} = '{(album?.Substring("AlbumGroup>".Length) == "Unknown" ? "Unknown Album" : album?.Substring("AlbumGroup>".Length))?.Replace("'", "''").Replace("\\", "\\\\").Replace("\"", "\\\"")}'"
				)).Select(s => s.Path).ToList();
				break;

			case var genre when genre?.StartsWith("GenreGroup>") == true:
				list = (await DatabaseHelper.Instance.LoadSongsFromDB(
					orderBy: Enum.Parse<SongProperty>(localSettings.Values[nameof(LocalSave.GenreDetailViewSortBy)]?.ToString() ?? "Title"),
					ascending: (localSettings.Values[nameof(LocalSave.GenreDetailViewSortOrder)]?.ToString() ?? "Ascending") == "Ascending",
					whereCondition: $"{SongProperty.Genre.ToString()} = '{(genre?.Substring("GenreGroup>".Length) == "Unknown" ? "Unknown Genre" : genre?.Substring("GenreGroup>".Length))?.Replace("'", "''").Replace("\\", "\\\\").Replace("\"", "\\\"")}'"
				)).Select(s => s.Path).ToList();
				break;

			case var year when year?.StartsWith("YearGroup>") == true:
				list = (await DatabaseHelper.Instance.LoadSongsFromDB(
					orderBy: Enum.Parse<SongProperty>(localSettings.Values[nameof(LocalSave.YearDetailViewSortBy)]?.ToString() ?? "Title"),
					ascending: (localSettings.Values[nameof(LocalSave.YearDetailViewSortOrder)]?.ToString() ?? "Ascending") == "Ascending",
					whereCondition: $"{SongProperty.Year.ToString()} = '{(year?.Substring("YearGroup>".Length) == "Unknown" ? "Unknown Year" : year?.Substring("YearGroup>".Length))}'"
				)).Select(s => s.Path).ToList();
				break;

			case "MostPlayed":
				var mostPlayedMaxLimit = localSettings.Values[nameof(LocalSave.MostPlayedMaxLimit)]?.ToString() ?? "100";
				list = (await DatabaseHelper.Instance.LoadSongsFromDB(
					orderBy: SongProperty.PlayCount, ascending: false,
					limit: mostPlayedMaxLimit == "Unlimited" ? 0 : int.Parse(mostPlayedMaxLimit),
					whereCondition: $"{SongProperty.PlayCount.ToString()} > 0"))
					.Select(s => s.Path).ToList();
				break;

			case "RecentlyPlayed":
				var recentlyPlayedMaxLimit = localSettings.Values[nameof(LocalSave.RecentlyPlayedMaxLimit)]?.ToString() ?? "100";
				list = (await DatabaseHelper.Instance.LoadSongsFromDB(
					orderBy: SongProperty.DateLastPlayed, ascending: false,
					limit: recentlyPlayedMaxLimit == "Unlimited" ? 0 : int.Parse(recentlyPlayedMaxLimit),
					whereCondition: $"{SongProperty.DateLastPlayed.ToString()} NOT NULL"))
					.Select(s => s.Path).ToList();
				break;

			case "RecentlyAdded":
				var recentlyAddedMaxLimit = Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.RecentlyAddedMaxLimit)]?.ToString() ?? "100";
				list = (await DatabaseHelper.Instance.LoadSongsFromDB(
					orderBy: SongProperty.DateAdded, ascending: false,
					limit: recentlyAddedMaxLimit == "Unlimited" ? 0 : int.Parse(recentlyAddedMaxLimit)))
					.Select(s => s.Path).ToList();
				break;

			case var playlist when playlist?.StartsWith("CustomPlaylist__") == true:
				list = (await DatabaseHelper.Instance.GetSongsInPlaylist(playlist.Substring("CustomPlaylist__".Length)))
					.Select(s => s.Path).ToList();
				break;
		}
		_ = Task.Run(() =>
		{
			OriginalPlaylist = new List<string>(list);
			ShuffleSongs(startingSong);
		});
	}

	// ─────────────────────────────────────────────────────────
	//  Shuffle / Repeat
	// ─────────────────────────────────────────────────────────
	/// <summary>
	/// Toggles the shuffle mode of the music player by setting the shuffle status.
	/// If the music player has an active playlist, it reshuffles the songs starting from the current song.
	/// </summary>
	/// <param name="mode">
	/// The desired shuffle mode to set. Use <see cref="ShuffleMode.On"/> to enable shuffle mode,
	/// or <see cref="ShuffleMode.Off"/> to disable it.
	/// </param>
	public void ToggleShuffle(ShuffleMode mode)
	{
		ShuffleStatus = mode;
		if (ActualPlaylist?.Count > 0) ShuffleSongs(ActualPlaylist[currentIndex]);
	}

	/// <summary>
	/// Sets the repeat mode of the music player to the specified value.
	/// Updates the player's repeat status according to the provided mode.
	/// </summary>
	/// <param name="mode">
	/// The repeat mode to apply. Possible values are:
	/// None - Repeat mode is turned off.
	/// One - The first track of playlist will repeat after the playlists ends.
	/// All - The entire playlist will repeat after the playlists ends.
	/// </param>
	public void SetRepeatMode(RepeatMode mode)
	{
		RepeatStatus = mode;
	}

	// ─────────────────────────────────────────────────────────
	//  Core song loading
	// ─────────────────────────────────────────────────────────
	public async Task LoadSong(string? songPath, bool play = true, FadeType? fadeType = null, bool dontReloadCurrent = false, bool startup = false)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(songPath)) return;

			var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
			var position = MusicControl._instance.ViewModel.ProgressBarValue;

			var playerType = await DatabaseHelper.Instance.GetPlayerTypeByPath(songPath);
			var requiredBackend = playerType == "Windows" ? BackendType.Windows : BackendType.Flyleaf;

			if (songPath == CurrentSong)
			{
				if (dontReloadCurrent)
				{
					if (!IsPlaying && play) Play(playBackPosition: position);
					return;
				}
				if (bool.Parse(localSettings.Values[nameof(LocalSave.RestartTrackOnSelectionStatus)]?.ToString() ?? "false"))
				{
					//fadeType = bool.Parse(localSettings.Values[nameof(LocalSave.ManualTrackChangeStatus)]?.ToString() ?? "false") ? FadeType.Manual : FadeType.None;
				}
				else
				{
					if (!IsPlaying && play) Play();
					return;
				}
			}

			if (requiredBackend != _activeBackendType)
			{
				_activeBackend.Volume = 0;
				_activeBackend.Stop();
				ActivateBackend(requiredBackend);
			}

			if (requiredBackend == BackendType.Flyleaf)
				SMTCPlayer.Source = Windows.Media.Core.MediaSource.CreateFromUri(new Uri(songPath));

			var capturedSong = songPath;
			var capturedPlay = play;
			var capturedPosition = startup ? position : 0.0;

			// Windows backend: media isn't seekable until MediaOpened fires.
			// Store the startup position and apply it in OnMediaOpened instead.
			if (_activeBackendType == BackendType.Windows && capturedPosition > 0)
			{
				_windowsBackend.PendingStartPosition = capturedPosition;
				capturedPosition = 0.0;
			}

			#region When crossfade works then use this
			/*double selectedFadeTime = fadeType switch
				{
					FadeType.Manual      => double.Parse(localSettings.Values[nameof(LocalSave.ManualTrackChangeValue)]?.ToString() ?? "1000"),
					FadeType.AutoAdvance => double.Parse(localSettings.Values[nameof(LocalSave.AutoAdvanceValue)]?.ToString()    ?? "1000"),
					_ => 0
				};

				if (!capturedPlay)
				{
					await _activeBackend.OpenAsync(capturedSong);
				}
				else if (fadeType == FadeType.None)
				{
					await _activeBackend.OpenAsync(capturedSong);
					Play(capturedPosition);
				}
				else
				{
					await (fadeType == null && !IsPlaying
						? Task.Run(async () => { await _activeBackend.OpenAsync(capturedSong); Play(capturedPosition); })
						: CrossfadeTransition(capturedSong, selectedFadeTime));
				}*/
			#endregion

			await _activeBackend.OpenAsync(capturedSong);

			if (capturedPlay) Play(capturedPosition);

			CurrentSong = capturedSong;
			localSettings.Values[nameof(LocalSave.LastPlayedTrack)] = CurrentSong;
		}
		catch (Exception)
		{
			GlobalNotification.Error($"Could not load song:\n{songPath}");
			Next(autoChange: play);
		}
	}

	// ─────────────────────────────────────────────────────────
	//  Play / Pause (with fade support)
	// ─────────────────────────────────────────────────────────
	public async void Pause()
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

		if (bool.Parse(localSettings.Values[nameof(LocalSave.PlayPauseStopFadeStatus)]?.ToString() ?? "false") && !isFading)
		{
			isFading = true;
			try
			{
				var fadeTime = int.Parse(localSettings.Values[nameof(LocalSave.PlayPauseStopFadeValue)]?.ToString() ?? "700");
				int steps = fadeTime / 10;
				for (int i = 0; i <= steps; i++)
				{
					double progress = (double)i / steps;
					_activeBackend.Volume = (int)(initialVolume * Math.Pow((1 - progress), 2));
					await Task.Delay(10);
				}
			}
			catch (Exception) { }
			finally { isFading = false; }
		}

		_activeBackend.Pause();

		if (_activeBackendType == BackendType.Flyleaf && SMTC != null)
			SMTC.PlaybackStatus = Windows.Media.MediaPlaybackStatus.Paused;
	}

	public async void Play(double playBackPosition = 0)
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		_activeBackend.IsMuted = false;

		if (bool.Parse(localSettings.Values[nameof(LocalSave.PlayPauseStopFadeStatus)]?.ToString() ?? "false") && !isFading)
		{
			isFading = true;
			try
			{
				_activeBackend.Volume = 0;
				// Windows: position BEFORE Play() to avoid blip. Flyleaf: position AFTER Play().
				if (_activeBackendType == BackendType.Windows && playBackPosition > 0)
					_activeBackend.CurTimeTicks = TimeSpan.FromSeconds(playBackPosition).Ticks;
				_activeBackend.Play();
				if (_activeBackendType == BackendType.Flyleaf && playBackPosition > 0)
					_activeBackend.CurTimeTicks = TimeSpan.FromSeconds(playBackPosition).Ticks;

				// Update SMTC without SMTCPlayer.Play() — avoids double-decode CPU waste
				if (_activeBackendType == BackendType.Flyleaf && SMTC != null)
					SMTC.PlaybackStatus = Windows.Media.MediaPlaybackStatus.Playing;

				var fadeTime = int.Parse(localSettings.Values[nameof(LocalSave.PlayPauseStopFadeValue)]?.ToString() ?? "700");
				int steps = fadeTime / 10;
				for (int i = 0; i <= steps; i++)
				{
					double progress = (double)i / steps;
					_activeBackend.Volume = (int)(initialVolume * Math.Pow(progress, 2));
					await Task.Delay(10);
				}
			}
			catch (Exception) { }
			finally
			{
				_activeBackend.Volume = initialVolume;
				isFading = false;
			}
		}
		else
		{
			_activeBackend.Volume = initialVolume;
			// Windows: position BEFORE Play() to avoid blip. Flyleaf: position AFTER Play().
			if (_activeBackendType == BackendType.Windows && playBackPosition > 0)
				_activeBackend.CurTimeTicks = TimeSpan.FromSeconds(playBackPosition).Ticks;
			_activeBackend.Play();
			if (_activeBackendType == BackendType.Flyleaf && playBackPosition > 0)
				_activeBackend.CurTimeTicks = TimeSpan.FromSeconds(playBackPosition).Ticks;

			// Update SMTC without SMTCPlayer.Play() — avoids double-decode CPU waste
			if (_activeBackendType == BackendType.Flyleaf && SMTC != null)
				SMTC.PlaybackStatus = Windows.Media.MediaPlaybackStatus.Playing;
		}
	}

	// ─────────────────────────────────────────────────────────
	//  Previous / Next (logic unchanged, use new surface)
	// ─────────────────────────────────────────────────────────
	public async void Previous()
	{
		if (GetMusicData.IsScanning) return;
		try
		{
			if (ActualPlaylist?.Count > 0)
			{
				var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
				bool restartOnPrevious = bool.Parse(localSettings.Values[nameof(LocalSave.PreviousResetStatus)]?.ToString() ?? "false");

				string? songToPlay = null;

				if (!restartOnPrevious || TimeSpan.FromTicks(CurTimeTicks).TotalSeconds < 5)
				{
					currentIndex = !SongQueue ? currentIndex == 0 ? OriginalPlaylist.Count - 1 : currentIndex - 1 : currentIndex;
					songToPlay = ActualPlaylist[currentIndex];
				}
				else
				{
					songToPlay = CurrentSong;
				}

				bool isPlaying = IsPlaying;
				bool manualCrossfadeEnabled = bool.Parse(localSettings.Values[nameof(LocalSave.ManualTrackChangeStatus)]?.ToString() ?? "false");
				await LoadSong(songToPlay, isPlaying, isPlaying && manualCrossfadeEnabled ? FadeType.Manual : FadeType.None);
			}
			else return;
		}
		catch (Exception)
		{
			GlobalNotification.Error("Could not load previous song.");
			Next();
		}
	}

	public async void Next(bool autoChange = false)
	{
		if (GetMusicData.IsScanning) return;
		try
		{
			bool isPlaying = autoChange ? autoChange : IsPlaying;

			var queuedList = await DatabaseHelper.Instance.GetQueuedPlayingList();
			var fadeType = isPlaying
				? bool.Parse(Windows.Storage.ApplicationData.Current.LocalSettings.Values[autoChange ? nameof(LocalSave.AutoAdvanceStatus) : nameof(LocalSave.ManualTrackChangeStatus)]?.ToString() ?? "false")
					? (autoChange ? FadeType.AutoAdvance : FadeType.Manual) : FadeType.None
				: FadeType.None;

			if (queuedList?.Count > 0)
			{
				await LoadSong(queuedList[0].Path, isPlaying, fadeType);
				await DatabaseHelper.Instance.ClearFromQueue();
				if (!SongQueue) GlobalNotification.Info("Queue started.");
				SongQueue = true;
				return;
			}
			else
			{
				if (SongQueue) GlobalNotification.Info("Queue ended.");
				SongQueue = false;
			}

			if (OriginalPlaylist?.Count > 0)
			{
				if (currentIndex < OriginalPlaylist.Count - 1)
				{
					currentIndex++;
				}
				else
				{
					switch (RepeatStatus)
					{
						case RepeatMode.One:
							if (!alreadyPlayed) { currentIndex = 0; alreadyPlayed = true; }
							else { if (autoChange) Pause(); return; }
							break;
						case RepeatMode.All:
							LoadPlaylist(OriginalPlaylist, ActualPlaylist[0], false);
							currentIndex = 0;
							break;
						case RepeatMode.None:
							if (autoChange) Pause();
							return;
					}
				}
				await LoadSong(ActualPlaylist[currentIndex], isPlaying, fadeType);
			}
		}
		catch (Exception)
		{
			GlobalNotification.Error("Could not load next song.");
			Next(autoChange);
		}
	}

	// ─────────────────────────────────────────────────────────
	//  Playlist helpers
	// ─────────────────────────────────────────────────────────
	/// <summary>
	/// Reorders the playlist so that the specified starting song becomes the first song,
	/// followed by the remaining songs in their original order.
	/// </summary>
	/// <param name="startingSong">
	/// The path of the song that should be at the beginning of the reordered playlist.
	/// </param>
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

	/// <summary>
	/// Performs the shuffling of songs in the playlist based on the current shuffle mode,
	/// while optionally maintaining a specified starting song at the beginning of the playlist.
	/// This method updates the playback order dynamically.
	/// </summary>
	/// <param name="startingSong">
	/// An optional parameter specifying the song path that should remain as the first track
	/// in the shuffled playlist. If null, the shuffling starts from the current order.
	/// </param>
	private void ShuffleSongs(string? startingSong = null)
	{
		currentIndex = startingSong != null ? OriginalPlaylist.IndexOf(startingSong) : 0;

		if (ShuffleStatus == ShuffleMode.On && OriginalPlaylist?.Count > 2)
		{
			Random rng = new Random();
			ActualPlaylist = OriginalPlaylist?.OrderBy(_ => rng.Next()).ToList();
			if (startingSong != null) ReorderPlaylist(startingSong);
		}
		else
		{
			ActualPlaylist = OriginalPlaylist?.ToList();
		}
	}

	// ─────────────────────────────────────────────────────────
	//  Persistence
	// ─────────────────────────────────────────────────────────
	/// <summary>
	/// Saves the current playback position and the current song index of the player.
	/// This method stores the playback position and index in application settings for persistence,
	/// allowing the playback to resume from the saved state the next time the application is launched.
	/// </summary>
	public void SavePlayBackPosition()
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		localSettings.Values[nameof(LocalSave.PlayBackPosition)] = TimeSpan.FromTicks(CurTimeTicks).TotalSeconds.ToString();
	}

	// ─────────────────────────────────────────────────────────
	//  Reset / reload after scan or delete
	// ─────────────────────────────────────────────────────────
	/// <summary>
	/// Resets the music player state after a library scan. This includes determining if the current song
	/// is still valid, pausing playback if necessary, resetting playlists, and clearing playback-related
	/// settings and data. If the current song is identified, it reloads the appropriate playlist or track
	/// for continued playback.
	/// </summary>
	public async void ResetOrReloadPlayer(Song? song = null)
	{
		var track = song ?? await DatabaseHelper.Instance.GetSongByPath(CurrentSong);
		if (track == null)
		{
			Pause();
			var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
			ActualPlaylist = null;
			OriginalPlaylist = null;
			ActualPlaylist = OriginalPlaylist = new List<string>();
			CurrentSong = "";
			_activeBackend.Stop();
			SMTCPlayer.Source = null;
			localSettings.Values.Remove(nameof(LocalSave.LastPlayedTrack));
			localSettings.Values.Remove(nameof(LocalSave.PlayBackPosition));
			localSettings.Values.Remove(nameof(LocalSave.CurrentPlayinglist));
			currentIndex = 0;

			MusicControl._instance.ViewModel.ResetCurrentSongFloatingWindow();
		}
		else
		{
			LoadPlaylist(track.Path, IsPlaying, dontReloadCurrent: true);
		}
	}

	/// <summary>
	/// Handles actions required after a song is deleted from the device or database.
	/// This method ensures the music player's state is updated accordingly, such as
	/// determining the next playable track in the playlist and resetting or reloading the player
	/// to maintain a consistent playback experience.
	/// </summary>
	public async void HandleAfterDelete()
	{
		var track = await DatabaseHelper.Instance.GetSongByPath(CurrentSong);
		var initialIndex = currentIndex;

		if (track == null)
		{
			if (ActualPlaylist != null && ActualPlaylist.Count > 0)
			{
				do
				{
					if (++initialIndex == ActualPlaylist.Count) break;
					track = await DatabaseHelper.Instance.GetSongByPath(ActualPlaylist[initialIndex]);
				} while (track == null);

				initialIndex = currentIndex;
				while (track == null)
				{
					if (--initialIndex < 0) break;
					track = await DatabaseHelper.Instance.GetSongByPath(ActualPlaylist[initialIndex]);
				}
			}
		}
		ResetOrReloadPlayer(track);
	}

	//Do not use for now as it causes other issues
	private async Task CrossfadeTransition(string songPath, double fadeTime)
	{
		/*
		 * Crossfade: fade out the current backend while fading in a second instance.
		 * For same-backend transitions this would need a second Player/MediaPlayer instance.
		 * Keeping the original logic here updated for the dual-backend surface:
		 *
		 * var steps = fadeTime / 10;
		 * // Start next song at volume 0
		 * // (would need a second IMediaBackend instance — not yet wired up)
		 * for (int i = 0; i <= steps; i++)
		 * {
		 *     double progress = (double)i / steps;
		 *     // nextBackend.Volume = (int)(initialVolume * Math.Pow(progress, 2));
		 *     _activeBackend.Volume = (int)(initialVolume * Math.Pow((1 - progress), 2));
		 *     await Task.Delay(10);
		 * }
		 * _activeBackend.Pause();
		 * _activeBackend.Volume = initialVolume;
		 * await _activeBackend.OpenAsync(songPath);
		 * // _activeBackend.CurTimeTicks = nextBackend position;
		 * Play();
		 * // nextBackend.Pause(); nextBackend.Dispose();
		 */
		await Task.CompletedTask;
	}
}

// ─────────────────────────────────────────────────────────────
//  Enums
// ─────────────────────────────────────────────────────────────
/// <summary>
/// Specifies the repeat modes that can be used by the music player.
/// This enum controls how playback behaves when the end of the playlist is reached.
/// It includes options for disabling repeat, repeating a single track, or repeating the entire playlist.
/// </summary>
public enum RepeatMode { None, One, All }

/// <summary>
/// Specifies the shuffle modes available for the music player.
/// This enum defines whether the playlist should be played in sequential order or in a randomized order.
/// The shuffle mode impacts the playback sequence when enabled.
/// </summary>
public enum ShuffleMode { Off, On }

public enum FadeType { None, Manual, AutoAdvance }
