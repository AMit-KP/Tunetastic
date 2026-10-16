using SQLite;

namespace Tunetastic.Common.Cli;

/// <summary>
/// Options for <see cref="CliPlaylistAdd.AddAsync"/>.
/// </summary>
public sealed class CliPlaylistAddOptions
{
	/// <summary>Gets whether a missing playlist is created. Defaults to true.</summary>
	public bool CreateIfMissing { get; init; } = true;

	/// <summary>Gets whether to only report what would happen without writing anything. Defaults to false.</summary>
	public bool DryRun { get; init; } = false;
}

/// <summary>
/// Thrown when the command cannot run at all (bad playlist name, unreachable database, ...).
/// Maps to exit code 2 in <see cref="CliRunner"/>.
/// </summary>
public sealed class CliFatalException : Exception
{
	public CliFatalException(string message) : base(message) { }
}

/// <summary>
/// Imports local audio files into the library and adds them to a playlist.
/// Reuses the library scan's metadata routine and playlist storage, so CLI imports behave exactly
/// like tracks added through the UI. Runs in-process against the shared app database, which means it
/// works whether or not the Tunetastic window is open; views that are already open refresh the next
/// time they load.
/// </summary>
public static class CliPlaylistAdd
{
	/// <summary>
	/// Adds <paramref name="inputs"/> (files, wildcards and/or folders) to <paramref name="playlistName"/>.
	/// </summary>
	/// <param name="playlistName">The target playlist. Created when missing unless disabled in <paramref name="options"/>.</param>
	/// <param name="inputs">Files, wildcard patterns and/or folders. Folders are scanned recursively.</param>
	/// <param name="options">Import options. Defaults apply when null.</param>
	/// <param name="cancellationToken">Stops after the current file.</param>
	/// <returns>The per-track report. Fatal problems throw <see cref="CliFatalException"/>.</returns>
	public static async Task<PlaylistAddReport> AddAsync(string playlistName, IReadOnlyList<string> inputs, CliPlaylistAddOptions? options = null, CancellationToken cancellationToken = default)
	{
		options ??= new CliPlaylistAddOptions();

		var nameError = ValidatePlaylistName(playlistName);
		if (nameError != null)
			throw new CliFatalException($"Invalid playlist name '{playlistName}': {nameError}");

		if (inputs == null || inputs.Count == 0)
			throw new CliFatalException("No input files given. Pass at least one file, wildcard or folder.");

		try
		{
			await DatabaseHelper.Instance.InitializeDatabase();
		}
		catch (Exception ex)
		{
			throw new CliFatalException($"Could not open the Tunetastic library database: {ex.Message}");
		}

		var (ignoreDuration, ignoreDuplicates) = ReadPreferences();
		var enabledExtensions = await WithBusyRetry(() => LibraryScanner.GetEnabledExtensions());

		var failures = new List<InputFailure>();
		var candidateFiles = ResolveInputs(inputs, enabledExtensions, failures);

		var report = new PlaylistAddReport { Playlist = playlistName, DryRun = options.DryRun };

		var candidates = new List<CliMetadataReader.TrackImport>();
		foreach (var file in candidateFiles)
		{
			cancellationToken.ThrowIfCancellationRequested();

			CliMetadataReader.TrackImport import;
			try
			{
				import = await CliMetadataReader.ReadAsync(file);
			}
			catch (Exception ex)
			{
				failures.Add(new InputFailure { Input = file, Reason = $"Could not read file: {ex.Message}" });
				continue;
			}

			if (import.Song.Duration <= ignoreDuration && ignoreDuration > 0)
			{
				report.Tracks.Add(SkippedEntry(import, $"Duration {import.Song.Duration:0.#}s is at or below the 'ignore tracks below duration' setting ({ignoreDuration:0.#}s)."));
				continue;
			}

			if (ignoreDuplicates && await WithBusyRetry(() => DatabaseHelper.Instance.SongMetadataExists(import.Song.Title, import.Song.Artists, import.Song.Album)))
			{
				report.Tracks.Add(SkippedEntry(import, "A track with the same title, artist and album is already in the library (duplicate detection is on)."));
				continue;
			}

			candidates.Add(import);
		}

		report.Failures.AddRange(failures);

		if (candidates.Count == 0)
			return report;

		bool created = false;
		bool exists = (await WithBusyRetry(() => DatabaseHelper.Instance.GetAllPlaylistNames())).Contains(playlistName);
		if (!exists)
		{
			if (!options.CreateIfMissing)
				throw new CliFatalException($"Playlist '{playlistName}' does not exist. Re-run without --no-create to create it.");

			if (!options.DryRun)
			{
				await WithBusyRetry<bool>(async () =>
				{
					await DatabaseHelper.Instance.CreatePlaylist(playlistName);
					return true;
				});
				exists = true;
				created = true;
			}
		}

		HashSet<string> alreadyInPlaylist = new(StringComparer.OrdinalIgnoreCase);
		if (exists && !options.DryRun)
		{
			var inPlaylist = await WithBusyRetry(() => DatabaseHelper.Instance.GetSongsInPlaylist(playlistName));
			alreadyInPlaylist = inPlaylist.Select(s => s.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
		}

		if (!options.DryRun)
		{
			var songs = candidates.Select(c => c.Song).ToList();
			var metas = candidates.Select(c => LibraryScanner.BuildFileScanMeta(c.Path)).ToList();

			await WithBusyRetry<bool>(async () =>
			{
				await DatabaseHelper.Instance.InsertMultipleSongs(songs);
				return true;
			});
			await WithBusyRetry<bool>(async () =>
			{
				await DatabaseHelper.Instance.UpdateFileScanMeta(metas);
				return true;
			});
			await WithBusyRetry<bool>(async () =>
			{
				await DatabaseHelper.Instance.AddSongsToPlaylist(playlistName, songs.Select(s => s.Path).ToList());
				return true;
			});
		}

		foreach (var import in candidates)
		{
			if (options.DryRun)
			{
				report.Tracks.Add(Entry(import, TrackStatus.WouldAdd, null));
			}
			else if (alreadyInPlaylist.Contains(import.Path))
			{
				report.Tracks.Add(Entry(import, TrackStatus.AlreadyPresent, "Already in this playlist; kept the existing position."));
			}
			else
			{
				report.Tracks.Add(Entry(import, TrackStatus.Added, null));
			}
		}

		return new PlaylistAddReport
		{
			Playlist = playlistName,
			PlaylistCreated = created,
			DryRun = options.DryRun,
			Tracks = report.Tracks,
			Failures = report.Failures,
		};
	}

	/// <summary>
	/// Validates a playlist name with the same rules as the "Add New Playlist" dialog.
	/// </summary>
	/// <returns>The problem, or null when the name is valid.</returns>
	internal static string? ValidatePlaylistName(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
			return "Playlist name cannot be empty.";
		if (new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" }.Contains(name.ToUpperInvariant()))
			return "This name is reserved by the system.";
		if (name.Any(c => Path.GetInvalidFileNameChars().Contains(c)))
			return "Name contains invalid characters: \\ / : * ? \" < > |";
		if (name.EndsWith(" ") || name.EndsWith("."))
			return "Name cannot end with a space or period.";
		if (name.Length > 255)
			return "Name is too long (max 255 characters).";
		return null;
	}

	/// <summary>
	/// Reads the user's scan preferences, falling back to permissive defaults when settings are unavailable.
	/// </summary>
	private static (double ignoreDuration, bool ignoreDuplicates) ReadPreferences()
	{
		try
		{
			var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
			double duration = double.Parse(settings.Values[nameof(LocalSave.IgnoreTracksBelowDuration)]?.ToString() ?? "0");
			bool duplicates = bool.Parse(settings.Values[nameof(LocalSave.IgnoreDuplicateEnabled)]?.ToString() ?? "false");
			return (duration, duplicates);
		}
		catch
		{
			return (0, false);
		}
	}

	/// <summary>
	/// Expands the CLI inputs to an ordered, de-duplicated file list. Folders are scanned recursively.
	/// Files whose extension is disabled in Settings are reported as input failures; the rest is returned.
	/// </summary>
	private static List<string> ResolveInputs(IReadOnlyList<string> inputs, List<string> enabledExtensions, List<InputFailure> failures)
	{
		var files = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var input in inputs)
		{
			foreach (var file in ExpandInput(input, failures))
			{
				if (!enabledExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
				{
					failures.Add(new InputFailure { Input = file, Reason = $"File type '{Path.GetExtension(file)}' is disabled in Settings > File Extensions." });
					continue;
				}
				files.Add(Path.GetFullPath(file));
			}
		}

		return files.OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();
	}

	/// <summary>
	/// Expands one input to files: a literal file, a folder (recursive), or a wildcard pattern.
	/// </summary>
	private static IEnumerable<string> ExpandInput(string input, List<InputFailure> failures)
	{
		if (File.Exists(input))
		{
			yield return input;
			yield break;
		}

		if (Directory.Exists(input))
		{
			string[] found;
			try
			{
				found = Directory.GetFiles(input, "*.*", SearchOption.AllDirectories);
			}
			catch (Exception ex)
			{
				failures.Add(new InputFailure { Input = input, Reason = $"Could not list folder: {ex.Message}" });
				yield break;
			}

			if (found.Length == 0)
				failures.Add(new InputFailure { Input = input, Reason = "Folder contains no files." });

			foreach (var file in found)
				yield return file;
			yield break;
		}

		if (input.IndexOfAny(['*', '?', '[']) >= 0)
		{
			var directory = Path.GetDirectoryName(input);
			var pattern = Path.GetFileName(input);
			if (string.IsNullOrEmpty(directory))
				directory = Directory.GetCurrentDirectory();

			string[] found;
			try
			{
				found = Directory.Exists(directory) ? Directory.GetFiles(directory, pattern) : [];
			}
			catch (Exception ex)
			{
				failures.Add(new InputFailure { Input = input, Reason = $"Could not expand pattern: {ex.Message}" });
				yield break;
			}

			if (found.Length == 0)
				failures.Add(new InputFailure { Input = input, Reason = "Pattern matched no files." });

			foreach (var file in found)
				yield return file;
			yield break;
		}

		failures.Add(new InputFailure { Input = input, Reason = "File not found." });
		yield break;
	}

	private static TrackEntry Entry(CliMetadataReader.TrackImport import, TrackStatus status, string? reason)
	{
		return new TrackEntry
		{
			Path = import.Path,
			Title = string.IsNullOrWhiteSpace(import.Song.Title) ? System.IO.Path.GetFileNameWithoutExtension(import.Path) : import.Song.Title,
			Status = status,
			Reason = reason,
			Imported = import.Imported,
			Defaulted = import.Defaulted,
			Skipped = import.Skipped,
			Warnings = import.Warnings,
		};
	}

	private static TrackEntry SkippedEntry(CliMetadataReader.TrackImport import, string reason)
	{
		return Entry(import, TrackStatus.Skipped, reason);
	}

	/// <summary>
	/// Retries a database operation when the app process holds the database lock, so CLI runs work
	/// while Tunetastic is open. Other errors propagate immediately.
	/// </summary>
	private static async Task<T> WithBusyRetry<T>(Func<Task<T>> operation, int attempts = 15)
	{
		for (int attempt = 1; ; attempt++)
		{
			try
			{
				return await operation();
			}
			catch (Exception ex) when (IsLocked(ex) && attempt < attempts)
			{
				await Task.Delay(200 * attempt);
			}
		}
	}

	private static bool IsLocked(Exception ex)
	{
		if (ex is SQLiteException sqliteEx && (sqliteEx.Result == SQLite3.Result.Busy || sqliteEx.Result == SQLite3.Result.Locked))
			return true;

		return ex.Message.Contains("busy", StringComparison.OrdinalIgnoreCase)
			|| ex.Message.Contains("locked", StringComparison.OrdinalIgnoreCase);
	}
}
