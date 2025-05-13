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
	/// A private queue that stores the list of songs to be played in the order they are added.
	/// This queue operates as part of the music player's queue management system, enabling
	/// functionality to temporarily add tracks for playback outside of the standard playlist order.
	/// Songs in the queue are played sequentially before returning to the regular playlist sequence.
	/// </summary>
	private Queue<string>? SongQueue;

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
				ShuffleStatusChanged?.Invoke(this, _shuffleStatus); // Fire event
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
	public SystemMediaTransportControls SMTC;

	private MusicPlayer()
	{
		MediaPlayer = new MediaPlayer();
		MediaPlayer.AutoPlay = false;
		MediaPlayer.AudioCategory = MediaPlayerAudioCategory.Media;
		SongQueue = new Queue<string>();
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
	/// Loads a playlist into the music player and optionally starts playback with a specified starting song.
	/// This method updates the player's playlist and reshuffles it if shuffle mode is enabled.
	/// </summary>
	/// <param name="songPaths">A list of song file paths representing the playlist to be loaded.</param>
	/// <param name="startingSong">
	/// An optional parameter specifying the path of the song to start playing.
	/// If null, the first song in the playlist is used.
	/// </param>
	public async void LoadPlaylist(List<string> songPaths, string? startingSong = null, bool play = true)
	{
		await LoadSong(startingSong ?? songPaths[0], play);
		_ = Task.Run(() =>
		{
			OriginalPlaylist = new List<string>(songPaths);

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
	/// Adds a song to the queue for playback. The song will be played after the currently playing song
	/// and before returning to the main playlist sequence.
	/// </summary>
	/// <param name="songPath">
	/// The file path of the song to be added to the queue. The path must point to a valid audio file.
	/// </param>
	public void AddToQueue(string songPath) => SongQueue?.Enqueue(songPath);        //TODO queue system


	/// <summary>
	/// Loads the current song from the active playlist based on the current index.
	/// If a playlist exists, it retrieves the song at the specified index and sets the playback state
	/// depending on whether the media player is actively playing.
	/// </summary>
	private async void LoadSong(bool? nextTrack = null)
	{
		if (ActualPlaylist?.Count > 0)
		{
			await LoadSong(ActualPlaylist[currentIndex], nextTrack ?? MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing);
		}
	}

	/// <summary>
	/// Loads and prepares the specified song for playback, updating the music player's state and optionally starting playback.
	/// This method also updates the track to be marked as the current song.
	/// </summary>
	/// <param name="songPath">The file path of the song to be loaded into the music player.</param>
	/// <param name="play">
	/// Optional parameter indicating whether playback should automatically start after loading the song.
	/// Defaults to true if not specified.
	/// </param>
	/// <returns>An asynchronous task that represents the operation of loading the song.</returns>
	public async Task LoadSong(string? songPath, bool play = true)
	{
		try
		{
			if (songPath == null || songPath == "") return;

			if (!(songPath == CurrentSong) || bool.Parse(Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.RestartTrackOnSelectionStatus)]?.ToString() ?? "false"))
			{
				//await CrossfadeTransition(ActualPlaylist[currentIndex]);          //TODO get settings
				MediaPlayer.Source = MediaSource.CreateFromUri(new Uri(songPath));
				if (play) Play();
				CurrentSong = songPath;
				Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.LastPlayedTrack)] = CurrentSong;
			}
			else
			{
				if (MediaPlayer.PlaybackSession.PlaybackState != MediaPlaybackState.Playing)
					Play();
			}
		}
		catch (Exception)
		{
			GlobalNotification.Error("Could not load song:\n" + songPath);
			Next(play);
		}
	}

	private bool isFading = false;
	private const double initialVolume = 1.0;

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
	/// Moves to the previous song in the current playlist based on saved user settings.
	/// If settings allow restarting the current song and the playback position is below a threshold,
	/// the current song restarts. Otherwise, the player navigates to the previous song.
	/// If the current song is the first song in the playlist, the playback jumps to the last song.
	/// </summary>
	/// <remarks>
	/// Ensures playlist continuity when moving backwards, either by restarting the current song or moving to the previous one.
	/// Displays an error notification and moves to next song if the previous song cannot be loaded.
	/// </remarks>
	public async void Previous()
	{
		try
		{
			var restart = bool.Parse(Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.PreviousResetStatus)]?.ToString() ?? "false");
			if (!restart || (restart && MediaPlayer.PlaybackSession.Position.TotalSeconds < 5))
			{
				if (OriginalPlaylist?.Count > 0)
					currentIndex = currentIndex == 0 ? OriginalPlaylist.Count - 1 : currentIndex - 1;
				else return;
			}
			LoadSong();
		}
		catch (Exception)
		{
			GlobalNotification.Error("Could not load previous song");
			Next();
		}
	}

	/// <summary>
	/// Skips to the next song in the playback queue or playlist, following the current shuffle and repeat modes.
	/// If the queue has songs, the next song is dequeued and played. Otherwise, the next song in the playlist is loaded.
	/// Handles transitions and repeat behaviors, such as restarting the playlist or pausing playback when the end is reached.
	/// </summary>
	/// <exception cref="Exception">
	/// Throws an exception if there is an error loading the next song.
	/// A global error notification is displayed and moves to next song when this occurs.
	/// </exception>
	public async void Next(bool? nextTrackAutoChange = null)
	{
		try
		{
			if (SongQueue?.Count > 0)
			{
				await CrossfadeTransition(SongQueue.Dequeue());
				return;
			}

			if (OriginalPlaylist != null)
			{
				if (currentIndex < OriginalPlaylist.Count - 1)
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
							Pause();
							return;
						}
					}
					else if (RepeatStatus == RepeatMode.All)
					{
						LoadPlaylist(OriginalPlaylist);
					}
					else if (RepeatStatus == RepeatMode.None)
					{
						Pause();
						return;
					}
				}
			}
			else return;

			LoadSong(nextTrackAutoChange);
		}
		catch (Exception)
		{
			GlobalNotification.Error("Could not load next song");
			Next(nextTrackAutoChange);
		}
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

	/// <summary>
	/// Handles the end of the currently playing track by initiating playback of the next song in the queue or playlist.
	/// This method respects the shuffle and repeat modes to determine the next track to play.
	/// If no more tracks are available to play, it pauses playback or loops based on the repeat mode.
	/// </summary>
	private void HandleTrackEnd()
	{
		Next(true);
	}

	/// <summary>
	/// Saves the current playback position and the current song index of the player.
	/// This method stores the playback position and index in application settings for persistence,
	/// allowing the playback to resume from the saved state the next time the application is launched.
	/// </summary>
	public void SavePlayBackPosition()
	{
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.PlayBackPosition)] = MediaPlayer.PlaybackSession.Position.TotalSeconds.ToString();
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CurrentIndex)] = currentIndex.ToString();
	}

	/// <summary>
	/// Resets the playlists of the music player after a music file scan operation is completed.
	/// This method clears the existing playlists and reinitializes them to ensure they reflect updates
	/// or changes made during the scan.
	/// If a song is currently playing, it is retained in both the actual and original playlists,
	/// and the current song index is updated accordingly.
	/// </summary>
	internal void ResetAfterScan()
	{
		ActualPlaylist = null;
		OriginalPlaylist = null;
		ActualPlaylist = OriginalPlaylist = new List<string>();
		if (CurrentSong != null && CurrentSong != "")
		{
			ActualPlaylist.Add(CurrentSong);
			OriginalPlaylist.Add(CurrentSong);
			currentIndex = 0;
		}
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
