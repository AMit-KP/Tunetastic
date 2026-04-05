using FlyleafLib.MediaPlayer;
using System.Collections.Concurrent;

namespace Tunetastic.Common;

/// <summary>
/// Provides services for managing and updating metadata related to music libraries in the application.
/// </summary>
public class GetMusicData
{
	/// <summary>
	/// Represents the asynchronous task that is responsible for scanning and updating the music libraries.
	/// </summary>
	/// <remarks>
	/// This field is used to manage the lifecycle and execution state of the scan operation,
	/// ensuring that the task can be awaited properly and no simultaneous scans occur.
	/// </remarks>
	private static Task? _scanTask;
	private static bool _isScanning = false;

	/// <summary>
	/// Indicates whether a music library scan is currently in progress.
	/// </summary>
	/// <remarks>
	/// This property returns a boolean value to check the scanning state of the music library.
	/// Useful for preventing overlapping scan operations or triggering UI updates based
	/// on the scanning state.
	/// </remarks>
	public static bool IsScanning => _isScanning;

	/// <summary>
	/// Represents the progress of the library scanning operation as a percentage.
	/// </summary>
	/// <remarks>
	/// This property indicates the current state of the scan process, ranging from 0 to 100,
	/// where 0 represents the beginning and 100 signifies completion. It is updated dynamically
	/// during the scanning of music libraries and can be used to provide feedback to the user
	/// about the scan's progress.
	/// </remarks>
	public static double ScanProgress { get; private set; } = 0;

	/// <summary>
	/// Performs an asynchronous metadata update operation on the music libraries stored in the system.
	/// Ensures that simultaneous scanning operations are avoided and updates the global notifications
	/// based on the outcome of the scanning process.
	/// </summary>
	/// <returns>
	/// A <see cref="Task"/> that represents the asynchronous operation of updating metadata.
	/// The operation updates notification messages such as "Info," "Warning," or "Error" and resets
	/// the music player state upon completion.
	/// </returns>
	public async Task UpdateMetaData()
	{
		if (IsScanning) return;

		_isScanning = true;
		string type = "";
		string message = "";
		_scanTask = Task.Run(async () =>
		{
			(type, message) = await ScanLibraries();
		});

		await _scanTask;

		switch (type)
		{
			case "Info":
				GlobalNotification.Info(message);
				break;
			case "Warning":
				GlobalNotification.Warning(message);
				break;
			case "Error":
				GlobalNotification.Error(message);
				break;
			default:
				break;
		}

		_isScanning = false;
		MusicPlayer.Instance.ResetOrReloadPlayer();
		TaskbarHelper.SetProgressState(App.Hwnd, TaskbarStates.NoProgress);
	}

	/// <summary>
	/// Scans the music libraries to identify and process audio files, applying filters such as
	/// file format and optional configurations for ignoring duplicates or tracks below a certain duration.
	/// Updates the local settings and notifies the user with the scan results.
	/// Processes files in parallel (DOP=4) to fully utilize multi-core CPUs and SSDs.
	/// </summary>
	/// <returns>
	/// A <see cref="Task"/> that represents the asynchronous operation of scanning the music libraries.
	/// </returns>
	private async Task<(string, string)> ScanLibraries()
	{
		TaskbarHelper.SetProgressState(App.Hwnd, TaskbarStates.Normal);
		ScanProgress = 0;
		TaskbarHelper.SetProgressValue(App.Hwnd, ScanProgress, 100);
		var audioFiles = new HashSet<string>();

		var libraries = new List<string>();

		foreach (LibraryModel library in await DatabaseHelper.Instance.GetAllLibraries())
		{
			libraries.Add(library.Path);
		}

		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var ignoreTrackDuration = double.Parse(localSettings.Values[nameof(LocalSave.IgnoreTracksBelowDuration)]?.ToString() ?? "0");
		var ignoreDuplicates = bool.Parse(localSettings.Values[nameof(LocalSave.IgnoreDuplicateEnabled)]?.ToString() ?? "false");

		var formatList = await DatabaseHelper.Instance.GetAllMusicFormats();

		List<string>? extensions = new();

		foreach (var format in formatList)
			if (format.Enabled) extensions.Add(format.Extension);

		if (extensions?.Count == 0) extensions.Add(".mp3");

		var path = Path.Combine(Constants.ThumbnailsFolder);
		if (Directory.Exists(path)) Directory.Delete(path, true);

		ScanProgress = 0.5;

		if (libraries?.Count > 0)
		{
			libraries = libraries.OrderBy(f => f.Length).ToList();

			var uniqueFolders = new List<string>();
			foreach (var folder in libraries)
			{
				if (!Directory.Exists(folder))
				{
					GlobalNotification.Error("Library folder not found: " + folder + "\n Folder might be removed/renamed from system.");
				}
				else
				{
					if (!uniqueFolders.Any(parent => folder.StartsWith(parent, StringComparison.OrdinalIgnoreCase)))
						uniqueFolders.Add(folder);
				}
			}

			var options = new EnumerationOptions { RecurseSubdirectories = true };

			foreach (var folder in uniqueFolders)
			{
				var files = Directory.EnumerateFiles(folder, "*.*", options)
									 .Where(file => extensions.Contains(Path.GetExtension(file).ToLower()));

				foreach (var file in files)
				{
					audioFiles.Add(file);
				}
			}

			// Thread-safe collections for parallel processing
			var songsContainer = new ConcurrentBag<Song>();
			// ConcurrentDictionary used as a concurrent HashSet for duplicate detection
			var uniqueMetadata = new ConcurrentDictionary<(string Title, string Artist, string Album), byte>();

			ScanProgress = 1;
			TaskbarHelper.SetProgressValue(App.Hwnd, ScanProgress, 100);
			int processedFiles = 0;
			int totalFiles = audioFiles.Count;

			// Detect the storage type of the first library's drive and pick an
			// appropriate DOP. HDD must stay sequential (DOP=1) to avoid thrashing;
			// SATA SSD can saturate its queue at DOP=4; NVMe benefits from DOP=8.
			string probePath = uniqueFolders.Count > 0 ? uniqueFolders[0] : audioFiles.First();
			DiskKind diskKind = DiskSpeedDetector.GetDiskKind(probePath);
			int dop = DiskSpeedDetector.DopForKind(diskKind);

			// Parallel.ForEachAsync: DOP is chosen per detected storage type.
			// Each file is independent — no shared mutable state except the
			// thread-safe collections above.
			await Parallel.ForEachAsync(
				audioFiles,
				new ParallelOptions { MaxDegreeOfParallelism = dop },
				async (filePath, ct) =>
				{
					try
					{
						using (var audioModel = TagLib.File.Create(filePath))
						{
							var fileInfo = new FileInfo(filePath);

							var song = new Song
							{
								Title = audioModel.Tag.Title ?? Path.GetFileNameWithoutExtension(filePath),
								Album = audioModel.Tag.Album ?? "Unknown Album",
								Artists = (audioModel.Tag.Performers != null && audioModel.Tag.Performers.Length > 0 ? audioModel.Tag.Performers[0] : audioModel.Tag.FirstAlbumArtist) ?? "Unknown Artist",
								Duration = audioModel.Properties.Duration.TotalSeconds,
								Path = filePath,
								Year = audioModel.Tag.Year <= 0 ? "Unknown Year" : audioModel.Tag.Year.ToString(),
								Genre = (audioModel.Tag.Genres != null && audioModel.Tag.Genres.Length > 0 ? audioModel.Tag.Genres[0] : "Unknown Genre"),
								Cover = ImageResizer.CreateThumbnailImage(ThumbnailFolder.AllSongView, audioModel.Tag.Pictures, 300),
								Lyrics = audioModel.Tag.Lyrics,
								DateAdded = fileInfo.LastWriteTime,
								Extension = fileInfo.Extension,
								AudioCodecDescription = audioModel.Properties.Description,
								AudioSampleRate = audioModel.Properties.AudioSampleRate != 0 ? audioModel.Properties.AudioSampleRate.ToString() + " Hz" : null,
								AudioBitrate = audioModel.Properties.AudioBitrate != 0 ? audioModel.Properties.AudioBitrate.ToString() + " kbps" : null,
								AudioChannels = audioModel.Properties.AudioChannels switch
								{
									1 => "Mono",
									2 => "Stereo",
									4 => "Quadraphonic",
									5 => "Surround 5.0",
									6 => "Surround 5.1",
									7 => "Surround 6.1",
									8 => "Surround 7.1",
									>= 9 => "Immersive",
									_ => null
								},
								FileSize = fileInfo.Length switch
								{
									>= 1L << 40 => $"{fileInfo.Length / Math.Pow(1024, 4):0.##} TB",
									>= 1L << 30 => $"{fileInfo.Length / Math.Pow(1024, 3):0.##} GB",
									>= 1L << 20 => $"{fileInfo.Length / Math.Pow(1024, 2):0.##} MB",
									>= 1L << 10 => $"{fileInfo.Length / 1024d:0.##} KB",
									_ => $"{fileInfo.Length} B"
								}
							};

							song.PlayerType = DeterminePlayerType(song.AudioCodecDescription, filePath);

							if (song.Duration <= 0)
							{
								FlyleafLib.Config config = new FlyleafLib.Config();
								config.Video.Enabled = false;
								config.Audio.Enabled = true;
								config.Player.AutoPlay = false;
								var tempPlayer = new Player(config);
								tempPlayer.Open(filePath);
								song.Duration = TimeSpan.FromTicks(tempPlayer.Duration).TotalSeconds;
								tempPlayer.Dispose();                           // Flyleaf opened it — it owns this file regardless of extension
								song.PlayerType = "Flyleaf";
							}

							if (song.Duration > ignoreTrackDuration &&
								(!ignoreDuplicates || uniqueMetadata.TryAdd((song.Title, song.Artists, song.Album), 0)))
							{
								songsContainer.Add(song);
							}
						}
					}
					catch (Exception)
					{
						GlobalNotification.Error($"Failed to read metadata for:\n{filePath}");
						double duration = 0;
						try
						{
							FlyleafLib.Config config = new FlyleafLib.Config();
							config.Video.Enabled = false;
							config.Audio.Enabled = true;
							config.Player.AutoPlay = false;
							var tempPlayer = new Player(config);
							tempPlayer.Open(filePath);
							duration = TimeSpan.FromTicks(tempPlayer.Duration).TotalSeconds;
							tempPlayer.Dispose();
						}
						catch (Exception)
						{
							duration = 0;
						}
						var fileInfo = new FileInfo(filePath);
						var song = new Song
						{
							Title = Path.GetFileNameWithoutExtension(filePath),
							Album = "Unknown Album",
							Artists = "Unknown Artist",
							Duration = duration,
							Path = filePath,
							Year = "Unknown Year",
							Genre = "Unknown Genre",
							Cover = ImageResizer.CreateThumbnailImage(ThumbnailFolder.AllSongView, null, 300),
							DateAdded = fileInfo.LastWriteTime,
							Extension = fileInfo.Extension
						};
						if (song.Duration > ignoreTrackDuration &&
							(!ignoreDuplicates || uniqueMetadata.TryAdd((song.Title, song.Artists, song.Album), 0)))
						{
							songsContainer.Add(song);
						}
					}

					// Atomically increment counter; update taskbar every 10 files to
					// avoid hammering the UI thread from 4 concurrent workers.
					int current = Interlocked.Increment(ref processedFiles);
					if (current % 10 == 0 || current == totalFiles)
					{
						ScanProgress = Math.Round(2 + ((double)(current * 97) / totalFiles), 2);
						TaskbarHelper.SetProgressValue(App.Hwnd, ScanProgress, 100);
					}
				}
			);

			try
			{
				await DatabaseHelper.Instance.UpdateSongsDatabase(songsContainer.ToList());
			}
			catch (Exception)
			{
				localSettings.Values[nameof(LocalSave.ScanResult)] = "No tracks could be added";
				await DatabaseHelper.Instance.DeleteAllSongsFromDB();
				TaskbarHelper.SetProgressState(App.Hwnd, TaskbarStates.Error);
				return ("Error", "No tracks could be added");
			}

			var librariesCount = libraries.Count;
			var songsCount = songsContainer.Count;
			extensions = null!;
			formatList = null!;
			uniqueFolders = null!;
			libraries = null!;

			localSettings.Values[nameof(LocalSave.ScanResult)] = $"Last Scanned Libraries: {librariesCount} Songs/Tracks: {songsCount} on {new DateFormatConverter().Convert(DateTime.Now, null, "F", null).ToString()}";
			ScanProgress = 100;
			TaskbarHelper.SetProgressValue(App.Hwnd, ScanProgress, 100);
			await Task.Delay(10);
			return ("Info", "Library scan completed.\nLibraries: " + librariesCount + "\nSongs/Tracks: " + songsCount);
		}
		else
		{
			await DatabaseHelper.Instance.DeleteAllSongsFromDB();
			localSettings.Values[nameof(LocalSave.ScanResult)] = "No libraries found";
			TaskbarHelper.SetProgressState(App.Hwnd, TaskbarStates.Error);
			return ("Warning", "No libraries found. Please add atleast one library.");
		}
	}

	/// <summary>
	/// Determines the appropriate player type ("Windows" or "Flyleaf") for a given audio file,
	/// based on its file extension and codec description.
	/// </summary>
	/// <param name="codecDescription">
	/// The codec description string, typically obtained from TagLib, which provides information about the audio codec.
	/// </param>
	/// <param name="filePath">
	/// The full path to the audio file whose player type is to be determined.
	/// </param>
	/// <returns>
	/// Returns "Windows" if the file is best handled by the Windows-native player, or "Flyleaf" if it requires the Flyleaf player.
	/// </returns>
	private static string DeterminePlayerType(string? codecDescription, string filePath)
	{
		var ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();

		switch (ext)
		{
			// Unambiguous Windows-native extensions
			case ".mp3":
			case ".mp2":
			case ".wma":
			case ".asf":
			case ".mid":
			case ".midi":
			case ".kar":
			case ".rmi":
				return "Windows";

			// Unambiguous Flyleaf-only extensions
			case ".ogg":
			case ".oga":
			case ".ogx":
			case ".opus":
			case ".ape":
			case ".wv":
			case ".tta":
			case ".mka":
			case ".webm":
			case ".ac3":
			case ".dts":
			case ".ra":
			case ".rm":
			case ".rmvb":
			case ".flac":
				return "Flyleaf";
		}

		// Ambiguous extensions — resolve using codec description from TagLib
		if (!string.IsNullOrWhiteSpace(codecDescription))
		{
			var codec = codecDescription.ToLowerInvariant();

			// Flyleaf-only codecs
			if (codec.Contains("apple lossless") ||
				codec.Contains("alac") ||
				codec.Contains("opus") ||
				codec.Contains("vorbis") ||
				codec.Contains("wavpack") ||
				codec.Contains("monkey") ||
				codec.Contains("g.711") ||
				codec.Contains("g711") ||
				codec.Contains("g.726") ||
				codec.Contains("g726") ||
				codec.Contains("rf64") ||
				codec.Contains("dolby") ||
				codec.Contains("xhe") ||
				codec.Contains("eld") ||
				codec.Contains("usac"))
			{
				return "Flyleaf";
			}

			// Windows-native codecs
			if (codec.Contains("mpeg audio") ||
				codec.Contains("aac") ||
				codec.Contains("mpeg-4 audio") ||
				codec.Contains("pcm") ||
				codec.Contains("windows media audio") ||
				codec.Contains("wma") ||
				codec.Contains("he-aac") ||
				codec.Contains("he aac"))
			{
				return "Windows";
			}
		}

		// No codec info or unrecognized — extension last resort
		switch (ext)
		{
			case ".m4a":
			case ".m4b":
			case ".m4r":
			case ".mp4":
			case ".aac":
			case ".wav":
			case ".bwf":
				// The Duration=0 path above will override to Flyleaf if needed.
				return "Windows";
			default:
				return "Flyleaf";
		}
	}

}
