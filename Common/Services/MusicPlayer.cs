using FlyleafLib;
using FlyleafLib.MediaPlayer;
using TagLib;
using Windows.Media;
using Windows.Media.Playback;
using File = System.IO.File;

namespace Tunetastic.Common.Services;

/// <summary>
/// Provides event arguments for playback state changes.
/// </summary>
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
	/// <summary>
	/// Gets the System Media Transport Controls player used for SMTC functionality.
	/// </summary>
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
	/// <summary>Fires when the currently playing song changes.</summary>
	public event EventHandler<string>? CurrentSongChanged;

	/// <summary>Fires when the shuffle status changes.</summary>
	public event EventHandler<ShuffleMode>? ShuffleStatusChanged;

	private string _currentSong = string.Empty;
	/// <summary>
	/// Gets or sets the path of the currently playing song.
	/// </summary>
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
	/// <summary>
	/// Gets or sets the current shuffle mode.
	/// </summary>
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

	/// <summary>
	/// Gets or sets the current repeat mode.
	/// </summary>
	public RepeatMode RepeatStatus { get; private set; } = RepeatMode.All;

	/// <summary>
	/// Gets the System Media Transport Controls instance for SMTC functionality.
	/// </summary>
	public SystemMediaTransportControls? SMTC { get; private set; }

	private bool isFading = false;
	private const int initialVolume = 100;

	// ── playlist state ────────────────────────────────────────
	private List<string>? OriginalPlaylist;
	private List<string>? ActualPlaylist;
	private bool SongQueue = false;
	private int currentIndex = 0;

	// ─────────────────────────────────────────────────────────
	//  Construction
	// ─────────────────────────────────────────────────────────
	/// <summary>
	/// Initializes a new instance of the MusicPlayer class.
	/// </summary>
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

	/// <summary>
	/// Gets the singleton instance of the MusicPlayer.
	/// </summary>
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
	/// <summary>
	/// Activates the specified backend type for audio playback.
	/// </summary>
	/// <param name="type">The backend type to activate.</param>
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

	/// <summary>
	/// Handles state changes from the active backend.
	/// </summary>
	/// <param name="s">The event sender.</param>
	/// <param name="e">The playback state change arguments.</param>
	private void OnBackendStateChanged(object? s, PlaybackStateChangedArgs e) => PlaybackStateChanged?.Invoke(this, e);

	/// <summary>
	/// Handles completion of song opening from the active backend.
	/// </summary>
	/// <param name="s">The event sender.</param>
	/// <param name="e">The event arguments.</param>
	private void OnBackendOpenCompleted(object? s, EventArgs e) => OpenCompleted?.Invoke(this, e);

	/// <summary>
	/// Handles position changes from the active backend.
	/// </summary>
	/// <param name="s">The event sender.</param>
	/// <param name="ticks">The current position in ticks.</param>
	private void OnBackendPositionChanged(object? s, long ticks) => PositionChanged?.Invoke(this, ticks);

	// ─────────────────────────────────────────────────────────
	//  SMTC setup
	// ─────────────────────────────────────────────────────────
	/// <summary>
	/// Sets up the System Media Transport Controls for media playback.
	/// </summary>
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
	/// <param name="startup">Indicates whether this is a startup load operation. Default is false.</param>
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
					artistName: artist!["ArtistGroup>".Length..] == "Unknown" ? "Unknown Artist" : artist!["ArtistGroup>".Length..],
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
	/// <summary>
	/// Loads a song into the music player and optionally starts playback.
	/// </summary>
	/// <param name="songPath">The path of the song to load.</param>
	/// <param name="play">Indicates whether to start playback after loading. Default is true.</param>
	/// <param name="fadeType">The type of fade to apply during transition. Default is null.</param>
	/// <param name="dontReloadCurrent">If true, prevents reloading the current song if it's already loaded. Default is false.</param>
	/// <param name="startup">Indicates whether this is a startup load operation. Default is false.</param>
	public async Task LoadSong(string? songPath, bool play = true, FadeType? fadeType = null, bool dontReloadCurrent = false, bool startup = false)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(songPath)) return;

			var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
			var position = MusicControl._instance?.ViewModel?.ProgressBarValue ?? 0;

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

			/* NOTE When crossfade works then use this
				double selectedFadeTime = fadeType switch
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

			await _activeBackend.OpenAsync(capturedSong);

			if (capturedPlay) Play(capturedPosition);

			CurrentSong = capturedSong;
			localSettings.Values[nameof(LocalSave.LastPlayedTrack)] = CurrentSong;
		}
		catch (Exception)
		{
			GlobalNotification.Error($"Could not load song:\n{songPath}");
			Next();
		}
	}

	// ─────────────────────────────────────────────────────────
	//  Play / Pause (with fade support)
	// ─────────────────────────────────────────────────────────
	/// <summary>
	/// Pauses the currently playing audio.
	/// </summary>
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

	/// <summary>
	/// Plays the currently loaded audio.
	/// </summary>
	/// <param name="playBackPosition">The position to start playback from in seconds. Default is 0.</param>
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
	/// <summary>
	/// Plays the previous song in the playlist.
	/// </summary>
	public async void Previous()
	{
		if (LibraryScanner.IsScanning) return;
		try
		{
			if (ActualPlaylist?.Count > 0)
			{
				var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
				bool restartOnPrevious = bool.Parse(localSettings.Values[nameof(LocalSave.PreviousResetStatus)]?.ToString() ?? "false");

				string? songToPlay = null;

				if (!restartOnPrevious || TimeSpan.FromTicks(CurTimeTicks).TotalSeconds < 5)
				{
					currentIndex = !SongQueue ? currentIndex == 0 ? RepeatStatus == RepeatMode.None ? 0 : OriginalPlaylist!.Count - 1 : currentIndex - 1 : currentIndex;
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

	/// <summary>
	/// Plays the next song in the playlist.
	/// </summary>
	/// <param name="autoChange">Indicates whether this is an automatic change. Default is false.</param>
	public async void Next(bool autoChange = false)
	{
		if (LibraryScanner.IsScanning) return;
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
				if (autoChange && RepeatStatus == RepeatMode.One)
				{
					// Song ended naturally + Repeat One -> replay the same song, don't advance.
				}
				else if (currentIndex < OriginalPlaylist.Count - 1)
				{
					currentIndex++;
				}
				else
				{
					switch (RepeatStatus)
					{
						case RepeatMode.One:
						case RepeatMode.All:
							if (OriginalPlaylist != null && ActualPlaylist != null && ActualPlaylist.Count > 0)
								LoadPlaylist(OriginalPlaylist, ActualPlaylist[0], false);
							currentIndex = 0;
							break;
						case RepeatMode.None:
							if (autoChange) Pause();
							return;
					}
				}
				if (ActualPlaylist != null)
					await LoadSong(ActualPlaylist[currentIndex], isPlaying, fadeType);
			}
		}
		catch (Exception)
		{
			GlobalNotification.Error("Could not load next song.");
			Next(autoChange);
		}
	}

	/// <summary>
	/// Gets a list of upcoming songs in the playlist.
	/// </summary>
	/// <param name="count">The number of upcoming songs to retrieve. Default is 2.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains a list of upcoming songs or null if an error occurs.</returns>
	public async Task<List<Song>?> GetUpcomingSongs(int count = 2)
	{
		if (LibraryScanner.IsScanning) return null;

		try
		{
			async Task<List<string>?> BuildPathsAsync()
			{
				var paths = new List<string>();

				var queuedList = await DatabaseHelper.Instance.GetQueuedPlayingList();
				if (queuedList?.Count > 0)
				{
					foreach (var item in queuedList)
					{
						if (paths.Count >= count) break;
						paths.Add(item.Path);
					}
				}

				if (paths.Count < count && OriginalPlaylist?.Count > 0 && ActualPlaylist?.Count > 0)
				{
					int simulatedIndex = currentIndex;

					while (paths.Count < count)
					{
						if (simulatedIndex < OriginalPlaylist.Count - 1)
						{
							simulatedIndex++;
						}
						else
						{
							switch (RepeatStatus)
							{
								case RepeatMode.All:
								case RepeatMode.One:
									simulatedIndex = 0;
									break;

								case RepeatMode.None:
								default:
									return paths.Count > 0 ? paths : null;
							}
						}

						if (simulatedIndex >= 0 && simulatedIndex < ActualPlaylist.Count)
						{
							paths.Add(ActualPlaylist[simulatedIndex]);
						}
						else
						{
							break;
						}
					}
				}

				return paths.Count > 0 ? paths : null;
			}

			var paths = await BuildPathsAsync();
			if (paths == null || paths.Count == 0) return null;

			var songs = new List<Song>();
			foreach (var songPath in paths)
			{
				var song = await DatabaseHelper.Instance.GetSongByPath(songPath!);
				if (song != null) songs.Add(song);
			}

			return songs.Count > 0 ? songs : null;
		}
		catch (Exception)
		{
			return null;
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
		var selectedIndex = ActualPlaylist?.IndexOf(startingSong) ?? -1;
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
		currentIndex = startingSong != null ? OriginalPlaylist?.IndexOf(startingSong) ?? 0 : 0;

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
	/// Saves the current playback position and processes all pending tag writes.
	/// This method is called on app exit to persist playback state and apply any deferred tag changes.
	/// </summary>
	public async Task SaveOnExitActionsAsync()
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		localSettings.Values[nameof(LocalSave.PlayBackPosition)] = MusicControl._instance?.ViewModel?.ProgressBarValue ?? 0;

		await ProcessPendingTagWritesAsync();
	}

	/// <summary>
	/// Processes all pending tag writes sequentially at app exit.
	/// For each entry, verifies the file exists, fetches the latest tag data from DB, and writes tags.
	/// Playback is stopped and the backend is disposed before writing to ensure file access.
	/// </summary>
	private async Task ProcessPendingTagWritesAsync()
	{
		var pendingPaths = await DatabaseHelper.Instance.GetAllPendingTagWrites();
		if (pendingPaths.Count == 0)
			return;

		Pause();
		_activeBackend.Stop();
		_activeBackend.Dispose();

		SMTCPlayer.Source = null;
		FlyleafPlayer.Dispose();

		foreach (var path in pendingPaths)
		{
			if (!File.Exists(path))
			{
				await DatabaseHelper.Instance.DeletePendingTagWrite(path);
				continue;
			}

			var track = await DatabaseHelper.Instance.GetSongByPath(path);
			if (track == null)
			{
				await DatabaseHelper.Instance.DeletePendingTagWrite(path);
				continue;
			}

			bool success = false;
			for (int attempt = 0; attempt < 3; attempt++)
			{
				try
				{
					await AudioTagSaveToFile(path, track);
					success = true;
					break;
				}
				catch (IOException) when (attempt < 4)
				{
					await Task.Delay(500);
				}
				catch (Exception)
				{
					break;
				}
			}
			if (success)
				GlobalNotification.Info($"Updated metadata for:\n{path}");
			else
				GlobalNotification.Error($"Failed to update metadata for:\n{path}");

			await DatabaseHelper.Instance.DeletePendingTagWrite(path);
		}
	}

	/// <summary>
	/// Saves audio tags to a file.
	/// </summary>
	/// <param name="path">The path of the audio file.</param>
	/// <param name="track">The song data containing tag information.</param>
	private static async Task AudioTagSaveToFile(string path, Song track)
	{
		var DB = DatabaseHelper.Instance;
		using var audioModel = TagLib.File.Create(path);

		if (await DB.PendingTagWritesExist(path, "Cover") is int pendingTrack && pendingTrack > 0)
		{
			if (pendingTrack == 1)
			{
				var coverArtTempPath = Path.Combine(Constants.TemporaryFolder, Path.GetFileName(track.Cover));
				if (System.IO.File.Exists(coverArtTempPath))
				{
					var picture = new TagLib.Picture(coverArtTempPath)
					{
						Type = TagLib.PictureType.FrontCover,
					};

					audioModel.Tag.Pictures = new IPicture[] { picture };

					System.IO.File.Delete(coverArtTempPath);
				}
			}
			else
				audioModel.Tag.Pictures = Array.Empty<IPicture>();
		}

		if (await DB.PendingTagWritesExist(path, "Title") is int pendingTitle && pendingTitle > 0)
			audioModel.Tag.Title = pendingTitle == 1 ? track.Title : null;

		if (await DB.PendingTagWritesExist(path, "Artist") is int pendingArtist && pendingArtist > 0)
			audioModel.Tag.Performers = pendingArtist == 1 ? new[] { track.Artists } : Array.Empty<string>();

		if (await DB.PendingTagWritesExist(path, "Album") is int pendingAlbum && pendingAlbum > 0)
			audioModel.Tag.Album = pendingAlbum == 1 ? track.Album : null;

		if (await DB.PendingTagWritesExist(path, "Genre") is int pendingGenre && pendingGenre > 0)
			audioModel.Tag.Genres = pendingGenre == 1 ? new[] { track.Genre } : Array.Empty<string>();

		if (await DB.PendingTagWritesExist(path, "Year") is int pendingYear && pendingYear > 0)
			audioModel.Tag.Year = pendingYear == 1 ? uint.Parse(track.Year) : 0;

		if (await DB.PendingTagWritesExist(path, "Lyrics") > 0)
			audioModel.Tag.Lyrics = track.Lyrics;

		audioModel.Save();
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
	/// <param name="song">The song to reset to, or null to reset to default state.</param>
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

			MusicControl._instance?.ViewModel?.ResetCurrentSongFloatingWindow();
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

	/// <summary>
	/// Performs a crossfade transition between songs.
	/// </summary>
	/// <param name="songPath">The path of the song to transition to.</param>
	/// <param name="fadeTime">The duration of the fade in milliseconds.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
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
