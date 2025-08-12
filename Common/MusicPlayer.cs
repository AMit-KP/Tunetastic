using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace Tunetastic.Common;

/// <summary>
/// Represents a singleton music player class that provides functionality to manage and play audio tracks.
/// It supports features like play, pause, next, previous, queue management,
/// shuffle, repeat modes, and playlist loading.
/// </summary>
public class MusicPlayer
{
	/// <summary>
	/// A private static field representing the single instance of the MusicPlayer class.
	/// This field ensures the MusicPlayer follows the singleton design pattern, where only one instance
	/// of the class exists throughout the application.
	/// It is initialized lazily when the <see cref="Instance"/> property is accessed for the first time.
	/// </summary>
	private static MusicPlayer? _instance;

	/// <summary>
	/// Provides access to the core functionality for media playback within the <see cref="MusicPlayer"/> class.
	/// This property is the main interface for controlling audio playback, supporting operations such as play, pause,
	/// stop, track transitions, and playback session management.
	/// It is initialized in the singleton class constructor and does not allow external modification.
	/// </summary>
	public MediaPlayer MediaPlayer { get; private set; }

	/// <summary>
	/// A private field that stores the original list of songs in the playlist, as loaded by the user.
	/// This list represents the playlist in its unaltered order, which can be used for features like
	/// resetting the order or toggling shuffle modes.
	/// It is set when a playlist is loaded and is referenced for operations that require the original song order.
	/// </summary>
	private List<string>? OriginalPlaylist;

	/// <summary>
	/// A private field that stores the currently active playlist for the music player.
	/// This playlist is a dynamically maintained collection of song paths that represents
	/// the order of songs to be played. It can be updated based on shuffle and repeat modes.
	/// </summary>
	private List<string>? ActualPlaylist;

	/// <summary>
	/// A private boolean field used to indicate whether the next song in the playback
	/// is part of a queued list. This field manages the playback flow depending on whether
	/// songs are played in sequence from the primary playlist or from a user-defined queue.
	/// </summary>
	private bool SongQueue = false;

	/// <summary>
	/// A private integer field representing the index of the currently playing song
	/// in the playlist. This index is used to manage playback progression, allowing
	/// operations such as playing the next or previous track within the playlist.
	/// The value of this field is updated during song navigation or playlist changes.
	/// </summary>
	private int currentIndex = 0;

	/// <summary>
	/// A private boolean field used to track whether the current song has already been played
	/// when the repeat mode is set to "Repeat One".
	/// This helps manage the behavior of playback in specific repeat scenarios,
	/// ensuring proper functionality when revisiting or reloading the current track.
	/// </summary>
	private bool alreadyPlayed = false;

	/// <summary>
	/// Event triggered when the currently playing song changes.
	/// Subscribers to this event will be notified with the new song's identifier or name
	/// whenever the <see cref="CurrentSong"/> property is updated.
	/// </summary>
	public event EventHandler<string>? CurrentSongChanged;

	private string _currentSong = "";

	/// <summary>
	/// Gets or sets the identifier or name of the currently playing song.
	/// When this property is updated, the <see cref="CurrentSongChanged"/> event is triggered
	/// to notify subscribers of the change.
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

	/// <summary>
	/// An event triggered whenever the shuffle status of the music player changes.
	/// Subscribers can listen to this event to respond to changes in the shuffle mode,
	/// such as updating the UI or internal logic related to playback.
	/// The event provides the updated <see cref="ShuffleMode"/> as an argument.
	/// </summary>
	public event EventHandler<ShuffleMode>? ShuffleStatusChanged;

	private ShuffleMode _shuffleStatus = ShuffleMode.Off;

	/// <summary>
	/// Represents the current shuffle status of the music player.
	/// This property determines whether shuffle mode is enabled or disabled
	/// by holding a value from the <see cref="ShuffleMode"/> enumeration.
	/// A change to this property triggers the <see cref="ShuffleStatusChanged"/> event,
	/// allowing subscribers to track updates to the shuffle mode.
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
	/// A property representing the current repeat mode for the music player.
	/// Determines the playback loop behavior, such as repeating a single song,
	/// repeating the entire playlist, or no repetition at all.
	/// </summary>
	public RepeatMode RepeatStatus { get; private set; } = RepeatMode.All;

	/// <summary>
	/// Represents the System Media Transport Controls (SMTC) associated with the MediaPlayer instance.
	/// SMTC provides integration with system-level media controls, allowing users to interact with the
	/// music player using hardware or software controls such as play, pause, next, and previous buttons.
	/// It is configured to handle button press events and update playback status.
	/// </summary>
	public SystemMediaTransportControls? SMTC;

	/// <summary>
	/// A private field indicating whether the music player is currently performing a fade operation
	/// (e.g., volume fade during play/pause or stop transitions).
	/// This field is used to prevent simultaneous fade processes and manage transitions smoothly.
	/// </summary>
	private bool isFading = false;

	/// <summary>
	/// A private constant field representing the initial default volume for the media player.
	/// This value is used as the base volume level when fading in or out during playback transitions.
	/// It ensures a consistent starting point for audio volume across various playback scenarios.
	/// </summary>
	private const double initialVolume = 1.0;

	private MusicPlayer()
	{
		MediaPlayer = new MediaPlayer();
		MediaPlayer.AutoPlay = false;
		MediaPlayer.AudioCategory = MediaPlayerAudioCategory.Media;
		MediaPlayer.MediaEnded += (s, e) => HandleTrackEnd();
		SMTCSetup();
	}

	/// <summary>
	/// Gets the single instance of the <see cref="MusicPlayer"/> class, adhering to the singleton design pattern.
	/// This property ensures a globally accessible and consistent instance of the MusicPlayer is available within the application.
	/// The instance is lazily instantiated upon first access.
	/// </summary>
	public static MusicPlayer Instance
	{
		get
		{
			_instance ??= new MusicPlayer();
			return _instance;
		}
	}

	/// <summary>
	/// Configures the System Media Transport Controls (SMTC) for the music player to enable integration with system media controls.
	/// This setup includes enabling play, pause, next, and previous buttons and attaching handlers for button press events.
	/// </summary>
	private void SMTCSetup()
	{
		MediaPlayer.CommandManager.IsEnabled = false;
		SMTC = MediaPlayer.SystemMediaTransportControls;
		SMTC.IsPlayEnabled = true;
		SMTC.IsPauseEnabled = true;
		SMTC.IsNextEnabled = true;
		SMTC.IsPreviousEnabled = true;
		SMTC.IsEnabled = true;
	}

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
	public async void LoadPlaylist(string? startingSong, bool play = true, bool dontReloadCurrent = false)
	{
		await LoadSong(startingSong, play, dontReloadCurrent: dontReloadCurrent);

		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		List<string> list = new();

		switch (localSettings.Values[nameof(LocalSave.CurrentPlayinglist)]?.ToString())
		{
			case "AllSongsViewPage":
				list = (await DatabaseHelper.Instance.LoadSongsFromDB(orderBy: Enum.Parse<SongProperty>(localSettings.Values[nameof(LocalSave.AllSongViewSortBy)]?.ToString() ?? "Title"),
																	  ascending: (localSettings.Values[nameof(LocalSave.AllSongViewSortOrder)]?.ToString() ?? "Ascending") == "Ascending")).Select(s => s.Path).ToList();
				break;

			case var artist when artist?.StartsWith("ArtistGroup>") == true:
				list = (await DatabaseHelper.Instance.GetSongsByArtist(artistName: artist?.Substring("ArtistGroup>".Length) == "Unknown" ? "Unknown Artist" : artist?.Substring("ArtistGroup>".Length),
																	   orderBy: Enum.Parse<SongProperty>(localSettings.Values[nameof(LocalSave.ArtistDetailViewSortBy)]?.ToString() ?? "Title"),
																	   ascending: (localSettings.Values[nameof(LocalSave.ArtistDetailViewSortOrder)]?.ToString() ?? "Ascending") == "Ascending")).Select(s => s.Path).ToList();
				break;

			case var album when album?.StartsWith("AlbumGroup>") == true:
				list = (await DatabaseHelper.Instance.LoadSongsFromDB(orderBy: Enum.Parse<SongProperty>(localSettings.Values[nameof(LocalSave.AlbumDetailViewSortBy)]?.ToString() ?? "Title"),
																	  ascending: (localSettings.Values[nameof(LocalSave.AlbumDetailViewSortOrder)]?.ToString() ?? "Ascending") == "Ascending",
																	  whereCondition: $"{SongProperty.Album.ToString()} = '{(album?.Substring("AlbumGroup>".Length) == "Unknown" ? "Unknown Album" : album?.Substring("AlbumGroup>".Length))?.Replace("'", "''").Replace("\\", "\\\\").Replace("\"", "\\\"")}'")).Select(s => s.Path).ToList();
				break;

			case var genre when genre?.StartsWith("GenreGroup>") == true:
				list = (await DatabaseHelper.Instance.LoadSongsFromDB(orderBy: Enum.Parse<SongProperty>(localSettings.Values[nameof(LocalSave.GenreDetailViewSortBy)]?.ToString() ?? "Title"),
																	  ascending: (localSettings.Values[nameof(LocalSave.GenreDetailViewSortOrder)]?.ToString() ?? "Ascending") == "Ascending",
																	  whereCondition: $"{SongProperty.Genre.ToString()} = '{(genre?.Substring("GenreGroup>".Length) == "Unknown" ? "Unknown Genre" : genre?.Substring("GenreGroup>".Length))?.Replace("'", "''").Replace("\\", "\\\\").Replace("\"", "\\\"")}'")).Select(s => s.Path).ToList();
				break;

			case var year when year?.StartsWith("YearGroup>") == true:
				list = (await DatabaseHelper.Instance.LoadSongsFromDB(orderBy: Enum.Parse<SongProperty>(localSettings.Values[nameof(LocalSave.YearDetailViewSortBy)]?.ToString() ?? "Title"),
																	  ascending: (localSettings.Values[nameof(LocalSave.YearDetailViewSortOrder)]?.ToString() ?? "Ascending") == "Ascending",
																	  whereCondition: $"{SongProperty.Year.ToString()} = '{(year?.Substring("YearGroup>".Length) == "Unknown" ? "Unknown Year" : year?.Substring("YearGroup>".Length))}'")).Select(s => s.Path).ToList();
				break;

			case "MostPlayed":
				var mostPlayedMaxLimit = localSettings.Values[nameof(LocalSave.MostPlayedMaxLimit)]?.ToString() ?? "100";
				list = (await DatabaseHelper.Instance.LoadSongsFromDB(orderBy: SongProperty.PlayCount,
																	  ascending: false,
																	  limit: mostPlayedMaxLimit == "Unlimited" ? 0 : int.Parse(mostPlayedMaxLimit),
																	  whereCondition: $"{SongProperty.PlayCount.ToString()} > 0")).Select(s => s.Path).ToList();
				break;

			case "RecentlyPlayed":
				var recentlyPlayedMaxLimit = localSettings.Values[nameof(LocalSave.RecentlyPlayedMaxLimit)]?.ToString() ?? "100";
				list = (await DatabaseHelper.Instance.LoadSongsFromDB(orderBy: SongProperty.DateLastPlayed,
																	  ascending: false,
																	  limit: recentlyPlayedMaxLimit == "Unlimited" ? 0 : int.Parse(recentlyPlayedMaxLimit),
																	  whereCondition: $"{SongProperty.DateLastPlayed.ToString()} NOT NULL")).Select(s => s.Path).ToList();
				break;

			case "RecentlyAdded":
				var recentlyAddedMaxLimit = Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.RecentlyAddedMaxLimit)]?.ToString() ?? "100";
				list = (await DatabaseHelper.Instance.LoadSongsFromDB(orderBy: SongProperty.DateAdded,
																	  ascending: false,
																	  limit: recentlyAddedMaxLimit == "Unlimited" ? 0 : int.Parse(recentlyAddedMaxLimit))).Select(s => s.Path).ToList();
				break;

			case var playlist when playlist?.StartsWith("CustomPlaylist__") == true:
				list = (await DatabaseHelper.Instance.GetSongsInPlaylist(playlist.Substring("CustomPlaylist__".Length))).Select(s => s.Path).ToList();
				break;
		}
		_ = Task.Run(() =>
		{
			OriginalPlaylist = new List<string>(list);

			ShuffleSongs(startingSong);
		});
	}

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

	/// <summary>
	/// Loads a specified song into the media player and optionally starts playback. Supports fade transitions during the song change if specified.
	/// </summary>
	/// <param name="songPath">The file path or URL of the song to be loaded. If null or empty, no song is loaded.</param>
	/// <param name="play">Indicates whether playback should start immediately after loading the song. Defaults to true.</param>
	/// <param name="fadeType">Defines the fade transition behavior during song loading. Options include None, Manual, or AutoAdvance. Defaults to null.</param>
	/// <param name="dontReloadCurrent">If true, the current song is not reloaded if it matches the one being loaded. Defaults to false.</param>
	/// <returns>A task that represents the asynchronous operation of loading and potentially playing the song.</returns>
	public async Task LoadSong(string? songPath, bool play = true, FadeType? fadeType = null, bool dontReloadCurrent = false)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(songPath)) return;

			var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

			bool isPlaying = MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing;

			if (songPath == CurrentSong)
			{
				if (dontReloadCurrent)
				{
					if (!isPlaying && play) Play();
					return;
				}

				if (bool.Parse(localSettings.Values[nameof(LocalSave.RestartTrackOnSelectionStatus)]?.ToString() ?? "false"))
				{
					//fadeType = bool.Parse(localSettings.Values[nameof(LocalSave.ManualTrackChangeStatus)]?.ToString() ?? "false") ? FadeType.Manual : FadeType.None;
				}
				else
				{
					if (!isPlaying && play) Play();
					return;
				}
			}

			#region When crossfade works then use this
			/*double selectedFadeTime = fadeType switch
				{
					FadeType.Manual => double.Parse(localSettings.Values[nameof(LocalSave.ManualTrackChangeValue)]?.ToString() ?? "1000"),
					FadeType.AutoAdvance => double.Parse(localSettings.Values[nameof(LocalSave.AutoAdvanceValue)]?.ToString() ?? "1000"),
					_ => 0
				};

				if (!play)
				{
					MediaPlayer.Source = MediaSource.CreateFromUri(new Uri(songPath));
				}
				else if (fadeType == FadeType.None)
				{
					MediaPlayer.Source = MediaSource.CreateFromUri(new Uri(songPath));
					MediaPlayer.Play();
				}
				else
				{
					await (fadeType == null && !isPlaying
						? Task.Run(() => { MediaPlayer.Source = MediaSource.CreateFromUri(new Uri(songPath)); Play(); })
						: CrossfadeTransition(songPath, selectedFadeTime));
				}*/
			#endregion

			MediaPlayer.Source = MediaSource.CreateFromUri(new Uri(songPath));
			if (play) Play();

			CurrentSong = songPath;
			localSettings.Values[nameof(LocalSave.LastPlayedTrack)] = CurrentSong;
		}
		catch (Exception)
		{
			GlobalNotification.Error($"Could not load song:\n{songPath}");
			Next(autoChange: play);
			//TODO handle when folder renamed/removed
		}
	}

	/// <summary>
	/// Pauses the playback of the current song and optionally performs a fade-out effect by gradually reducing the volume if enabled in settings.
	/// If the fade-out effect is enabled, the volume decreases smoothly over a configured duration before pausing the MediaPlayer.
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
					MediaPlayer.Volume = initialVolume * Math.Pow((1 - progress), 2);
					await Task.Delay(10);
				}
			}
			catch (Exception)
			{
				//ignored
			}
			finally
			{
				isFading = false;
			}
		}
		MediaPlayer.Pause();
	}

	/// <summary>
	/// Initiates playback of the current song in the music player.
	/// If fade-in is enabled in the application settings, the volume is gradually increased to the configured level.
	/// Otherwise, playback begins immediately at the default volume.
	/// </summary>
	public async void Play()
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

		if (bool.Parse(localSettings.Values[nameof(LocalSave.PlayPauseStopFadeStatus)]?.ToString() ?? "false") && !isFading)
		{
			isFading = true;
			try
			{
				MediaPlayer.Volume = 0;
				MediaPlayer.Play();

				var fadeTime = int.Parse(localSettings.Values[nameof(LocalSave.PlayPauseStopFadeValue)]?.ToString() ?? "700");
				int steps = fadeTime / 10;

				for (int i = 0; i <= steps; i++)
				{
					double progress = (double)i / steps;
					MediaPlayer.Volume = initialVolume * Math.Pow(progress, 2);
					await Task.Delay(10);
				}
			}
			catch (Exception)
			{
			}
			finally
			{
				MediaPlayer.Volume = initialVolume;
				isFading = false;
			}
		}
		else
		{
			MediaPlayer.Volume = initialVolume;
			MediaPlayer.Play();
		}
	}

	/// <summary>
	/// Plays the previous track in the playlist or restarts the current track based on user settings and playback position.
	/// </summary>
	/// <remarks>
	/// This method handles several scenarios:
	/// <list type="bullet">
	/// <item>If music scanning is in progress (<see cref="GetMusicData.IsScanning"/>), the method returns without action</item>
	/// <item>If the playlist is empty, the method returns without action</item>
	/// <item>The behavior depends on the PreviousResetStatus setting and current playback position:
	///   <list type="bullet">
	///     <item>If PreviousResetStatus is false or playback position is less than 5 seconds: moves to the previous track</item>
	///     <item>If PreviousResetStatus is true and playback position is 5 seconds or more: restarts the current track</item>
	///   </list>
	/// </item>
	/// <item>When moving to previous track:
	///   <list type="bullet">
	///     <item>In normal playlist mode: moves to the previous track, or to the last track if at the beginning</item>
	///     <item>In queue mode: stays on the current track</item>
	///   </list>
	/// </item>
	/// </list>
	/// </remarks>
	/// <exception cref="Exception">Throws if unable to load the previous song, triggering Next() as fallback</exception>
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

				if (!restartOnPrevious || MediaPlayer.PlaybackSession.Position.TotalSeconds < 5)
				{
					currentIndex = !SongQueue ? currentIndex == 0 ? OriginalPlaylist.Count - 1 : currentIndex - 1 : currentIndex;
					songToPlay = ActualPlaylist[currentIndex];
				}
				else
				{
					songToPlay = CurrentSong;
				}

				bool isPlaying = MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing;
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
	/// Advances the current song index to the next song in the playlist.
	/// Depending on the repeat mode, it will either loop the playlist, play the same song,
	/// or stop playback if there is no further song to play. It also checks whether there's any queued songs present to play.
	/// </summary>
	/// <param name="autoChange">If set to true, the playback will automatically advance to the next song; otherwise, playback state will be maintained based on user action.</param>
	/// <Remark>
	/// If scanning is in progress, then the function doesn't work
	/// </Remark>
	public async void Next(bool autoChange = false)
	{
		if (GetMusicData.IsScanning) return;
		try
		{
			bool isPlaying = autoChange ? autoChange : MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing;

			var queuedList = await DatabaseHelper.Instance.GetQueuedPlayingList();

			var fadeType = isPlaying ? bool.Parse(Windows.Storage.ApplicationData.Current.LocalSettings.Values[autoChange ? nameof(LocalSave.AutoAdvanceStatus) : nameof(LocalSave.ManualTrackChangeStatus)]?.ToString() ?? "false") ? (autoChange ? FadeType.AutoAdvance : FadeType.Manual) : FadeType.None : FadeType.None;

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
							if (!alreadyPlayed)
							{
								currentIndex = 0;
								alreadyPlayed = true;
							}
							else
							{
								if (autoChange) Pause();
								return;
							}
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
			else return;
		}
		catch (Exception)
		{
			GlobalNotification.Error("Could not load next song.");
			Next(autoChange);
		}
	}

	/// <summary>
	/// Handles the end of the current track playback by advancing to the next track in the queue.
	/// This method is triggered when the MediaPlayer's MediaEnded event occurs,
	/// ensuring a seamless transition to the next song when auto-change is enabled.
	/// </summary>
	private void HandleTrackEnd()
	{
		Next(autoChange: true);
	}

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
			if (startingSong != null)
				ReorderPlaylist(startingSong);
		}
		else
		{
			ActualPlaylist = OriginalPlaylist?.ToList();
		}
	}

	//Do not use for now as it causes other issues
	private async Task CrossfadeTransition(string songPath, double fadeTime)
	{
		MediaPlayer MediaPlayerNext = new();
		MediaPlayerNext.Source = MediaSource.CreateFromUri(new Uri(songPath));
		MediaPlayerNext.Volume = 0;
		MediaPlayerNext.Play();

		var steps = fadeTime / 10;
		for (int i = 0; i <= steps; i++)
		{
			double progress = (double)i / steps;
			MediaPlayerNext.Volume = initialVolume * Math.Pow(progress, 2);
			MediaPlayer.Volume = initialVolume * Math.Pow((1 - progress), 2);
			await Task.Delay(10);
		}

		MediaPlayer.Pause();
		MediaPlayer.Volume = initialVolume;
		MediaPlayer.Source = MediaSource.CreateFromUri(new Uri(songPath));
		MediaPlayer.PlaybackSession.Position = MediaPlayerNext.PlaybackSession.Position;
		MediaPlayer.Play();
		MediaPlayerNext.Pause();
		MediaPlayerNext.Dispose();
	}

	/// <summary>
	/// Saves the current playback position and the current song index of the player.
	/// This method stores the playback position and index in application settings for persistence,
	/// allowing the playback to resume from the saved state the next time the application is launched.
	/// </summary>
	public void SavePlayBackPosition()
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		localSettings.Values[nameof(LocalSave.PlayBackPosition)] = MediaPlayer.PlaybackSession.Position.TotalSeconds.ToString();
	}

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
			MediaPlayer.Position = TimeSpan.Zero;
			MediaPlayer.Source = null;
			localSettings.Values.Remove(nameof(LocalSave.LastPlayedTrack));
			localSettings.Values.Remove(nameof(LocalSave.PlayBackPosition));
			localSettings.Values.Remove(nameof(LocalSave.CurrentPlayinglist));
			currentIndex = 0;

			MusicControl._instance.ViewModel.ResetCurrentSongFloatingWindow();
		}
		else
		{
			LoadPlaylist(track.Path, MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing, dontReloadCurrent: true);
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
					if (++initialIndex == ActualPlaylist.Count)
						break;

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
}

/// <summary>
/// Specifies the repeat modes that can be used by the music player.
/// This enum controls how playback behaves when the end of the playlist is reached.
/// It includes options for disabling repeat, repeating a single track, or repeating the entire playlist.
/// </summary>
public enum RepeatMode
{
	None,
	One,
	All
}

/// <summary>
/// Specifies the shuffle modes available for the music player.
/// This enum defines whether the playlist should be played in sequential order or in a randomized order.
/// The shuffle mode impacts the playback sequence when enabled.
/// </summary>
public enum ShuffleMode
{
	Off,
	On
}

public enum FadeType
{
	None,
	Manual,
	AutoAdvance
}
