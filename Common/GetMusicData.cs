using LibVLCSharp.Shared;

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
	}

	/// <summary>
	/// Scans the music libraries to identify and process audio files, applying filters such as
	/// file format and optional configurations for ignoring duplicates or tracks below a certain duration.
	/// Updates the local settings and notifies the user with the scan results.
	/// </summary>
	/// <returns>
	/// A <see cref="Task"/> that represents the asynchronous operation of scanning the music libraries.
	/// </returns>
	private async Task<(string, string)> ScanLibraries()
	{
		ScanProgress = 0;
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

			List<Song>? songsContainer = new();
			HashSet<(string Title, string Artist, string Album)>? uniqueMetadata = new HashSet<(string, string, string)>();

			ScanProgress = 1;
			int processedFiles = 0;

			foreach (var filePath in audioFiles)
			{
				try
				{
					//TODO some songs have unsupported codecs
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
							DateAdded = fileInfo.LastWriteTime,
							Extension = fileInfo.Extension
						};

						if (song.Duration <= 0)
						{
							Core.Initialize();
							var _libVLC = new LibVLC();
							var media = new Media(_libVLC, filePath, FromType.FromPath);
							var VlcMediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);
							VlcMediaPlayer.Media = media;
							VlcMediaPlayer.Volume = 0;
							VlcMediaPlayer.Mute = true;
							VlcMediaPlayer.Play();
							song.Duration = VlcMediaPlayer.Length / 1000.0;
							VlcMediaPlayer.Dispose();
							_libVLC.Dispose();
						}

						if (song.Duration > ignoreTrackDuration && (!ignoreDuplicates || uniqueMetadata.Add((song.Title, song.Artists, song.Album))))
							songsContainer.Add(song);
					}
				}
				catch (Exception)
				{
					GlobalNotification.Error($"Failed to read metadata for:\n{filePath}");
					double duration = 0;
					try
					{
						Core.Initialize();
						var _libVLC = new LibVLC();
						var media = new Media(_libVLC, filePath, FromType.FromPath);
						var VlcMediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);
						VlcMediaPlayer.Media = media;
						VlcMediaPlayer.Volume = 0;
						VlcMediaPlayer.Mute = true;
						VlcMediaPlayer.Play();
						duration = VlcMediaPlayer.Length / 1000.0;
						VlcMediaPlayer.Dispose();
						_libVLC.Dispose();
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
					if (song.Duration > ignoreTrackDuration && (!ignoreDuplicates || uniqueMetadata.Add((song.Title, song.Artists, song.Album))))
						songsContainer.Add(song);
				}

				processedFiles++;
				ScanProgress = Math.Round((2 + ((double)(processedFiles * 97) / audioFiles.Count)), 2);
				await Task.Delay(10);
			}

			try
			{
				await DatabaseHelper.Instance.UpdateSongsDatabase(songsContainer);
			}
			catch (Exception)
			{
				localSettings.Values[nameof(LocalSave.ScanResult)] = "No tracks could be added";
				await DatabaseHelper.Instance.DeleteAllSongsFromDB();
				return ("Error", "No tracks could be added");
			}


			var librariesCount = libraries.Count;
			var songsCount = songsContainer.Count;
			extensions = null!;
			formatList = null!;
			uniqueFolders = null!;
			songsContainer = null!;
			uniqueMetadata = null!;
			libraries = null!;

			localSettings.Values[nameof(LocalSave.ScanResult)] = $"Last Scanned Libraries: {librariesCount} Songs/Tracks: {songsCount} on {DateTime.Now}";
			ScanProgress = 100;
			await Task.Delay(10);
			return ("Info", "Library scan completed.\nLibraries: " + librariesCount + "\nSongs/Tracks: " + songsCount);
		}
		else
		{
			await DatabaseHelper.Instance.DeleteAllSongsFromDB();
			localSettings.Values[nameof(LocalSave.ScanResult)] = "No libraries found";
			return ("Warning", "No libraries found. Please add atleast one library.");
		}
	}

}
