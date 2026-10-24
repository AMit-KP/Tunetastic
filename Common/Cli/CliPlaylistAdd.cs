using SQLite;

namespace Tunetastic.Common.Cli;

/// <summary>
/// Options for <see cref="CliPlaylistAdd.AddAsync"/>.
/// </summary>
public sealed class CliPlaylistAddOptions
{
	/// <summary>Gets whether a missing playlist is created. Defaults to true.</summary>
	public bool CreateIfMissing { get; init; } = true;

	/// <summary>Gets whether to only report what would happen without changing the library or playlists. Defaults to false.</summary>
	public bool DryRun { get; init; } = false;

	/// <summary>
	/// Gets whether files outside the configured library folders may be added. Defaults to false:
	/// they are skipped with an explanation instead, because the next full library scan only keeps
	/// tracks inside the configured libraries.
	/// </summary>
	public bool AddExternal { get; init; } = false;
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
/// like tracks added through the UI. Opens the shared app database directly, which means it works
/// whether or not the Tunetastic window is open; views that are already open refresh the next
/// time they load.
/// </summary>
public static class CliPlaylistAdd
{
	/// <summary>
	/// Adds <paramref name="inputs"/> (files, wildcards and/or folders) to <paramref name="playlistName"/>.
	/// Input order is preserved and defines the playlist order.
	/// </summary>
	/// <param name="playlistName">The target playlist. Created when missing unless disabled in <paramref name="options"/>.</param>
	/// <param name="inputs">Files, wildcard patterns and/or folders. Folders are scanned recursively.</param>
	/// <param name="options">Import options. Defaults apply when null.</param>
	/// <param name="cancellationToken">Stops after the current file.</param>
	/// <returns>The per-track report. Fatal problems throw <see cref="CliFatalException"/>.</returns>
	public static async Task<PlaylistAddReport> AddAsync(string playlistName, IReadOnlyList<string> inputs, CliPlaylistAddOptions? options = null, CancellationToken cancellationToken = default)
	{
		options ??= new CliPlaylistAddOptions();
		playlistName = playlistName.Trim();

		var nameError = ValidatePlaylistName(playlistName);
		if (nameError != null)
			throw new CliFatalException($"Invalid playlist name '{playlistName}': {nameError}");

		if (inputs == null || inputs.Count == 0)
			throw new CliFatalException("No input files given. Pass at least one file, wildcard or folder.");

		string? dbOverride = Environment.GetEnvironmentVariable("TUNETASTIC_DB_PATH");
		if (string.IsNullOrWhiteSpace(dbOverride) && !HasPackageIdentity())
			throw new CliFatalException("The CLI cannot reach the library from an unpackaged build. Run the installed (packaged) Tunetastic app, or set TUNETASTIC_DB_PATH to a database file (testing only).");

		try
		{
			await WithBusyRetry(() => DatabaseHelper.Instance.InitializeDatabase());
		}
		catch (Exception ex)
		{
			throw new CliFatalException($"Could not open the Tunetastic library database: {ex.Message}");
		}

		var (ignoreDuration, ignoreDuplicates, prefsNotice) = ReadPreferences();
		var enabledExtensions = await WithBusyRetry(() => LibraryScanner.GetEnabledExtensions());

		// Fail fast on --no-create so scripting gets a deterministic error even with no usable inputs.
		// An existing playlist is resolved to its stored casing: the database compares names
		// case-sensitively, so all later operations must use the stored form.
		string? storedName = (await WithBusyRetry(() => DatabaseHelper.Instance.GetAllPlaylistNames()))
			.FirstOrDefault(n => string.Equals(n, playlistName, StringComparison.OrdinalIgnoreCase));
		if (storedName == null && !options.CreateIfMissing)
			throw new CliFatalException($"Playlist '{playlistName}' does not exist. Re-run without --no-create to create it.");
		bool playlistExists = storedName != null;
		if (playlistExists)
			playlistName = storedName!;

		var report = new PlaylistAddReport { Playlist = playlistName, DryRun = options.DryRun };
		if (prefsNotice != null)
			report.Notices.Add(prefsNotice);

		var libraryRoots = await GetEffectiveLibraryRoots(report.Notices);

		var failures = new List<InputFailure>();
		var candidateFiles = ResolveInputs(inputs, enabledExtensions, failures, report.Notices);

		var seenInRun = new HashSet<(string Title, string Artist, string Album)>();
		var addedPaths = new HashSet<string>(StringComparer.Ordinal);
		var candidates = new List<CliMetadataReader.TrackImport>();
		foreach (var file in candidateFiles)
		{
			cancellationToken.ThrowIfCancellationRequested();

			// Files outside the configured library folders are skipped unless the user explicitly
			// opted in with --add-external: the next full library scan only keeps tracks inside
			// those folders, so an external add without awareness would silently vanish later.
			if (!options.AddExternal && !IsUnderLibraryRoot(file, libraryRoots))
			{
				report.Tracks.Add(SkippedFileEntry(file, "Lives outside your configured library folders. Pass --add-external to add it anyway; note a later full library scan only keeps tracks inside the configured libraries."));
				continue;
			}

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

			string? readWarning = null;
			if (!import.TagsReadable)
			{
				// The scan imports such files with fallback values; the CLI does the same instead of
				// dropping a playable track. The fallback is reported explicitly, never silently,
				// but only when the track is actually imported (see below).
				readWarning = await DescribeReadFailure(file);
			}

			// Resolve to the stored row (case-insensitive): re-imports keep their history and casing.
			// The exact match uses the indexed key; the NOCASE fallback only runs for new casings.
			// Both lookups surface their own friendly error paths where possible (see DatabaseHelper);
			// when the database stays busy the result is null and the caller treats it as "unknown".
			var stored = await DatabaseHelper.Instance.GetSongByPath(file);
			stored ??= await DatabaseHelper.Instance.GetSongByPathNoCase(file);
			string effectivePath = stored?.Path ?? file;
			if (stored != null)
			{
				import.Song.PlayCount = stored.PlayCount;
				import.Song.DateLastPlayed = stored.DateLastPlayed;
				import.Song.Path = stored.Path;
			}

			if (import.Song.Duration <= ignoreDuration)
			{
				report.Tracks.Add(SkippedEntry(import, effectivePath, $"Duration {import.Song.Duration:0.#}s is at or below the 'ignore tracks below duration' setting ({ignoreDuration:0.#}s)."));
				continue;
			}

			var identity = (import.Song.Title, import.Song.Artists, import.Song.Album);
			bool duplicateInRun = !seenInRun.Add(identity);
			bool duplicateInDb = ignoreDuplicates && await WithBusyRetry(() => DatabaseHelper.Instance.SongMetadataExists(import.Song.Title, import.Song.Artists, import.Song.Album, excludePath: effectivePath));
			if (ignoreDuplicates && (duplicateInRun || duplicateInDb))
			{
				report.Tracks.Add(SkippedEntry(import, effectivePath, duplicateInDb
					? "A track with the same title, artist and album is already in the library (duplicate detection is on)."
					: "The same track appears more than once in this run (duplicate detection is on)."));
				continue;
			}

			candidates.Add(import);
			if (readWarning != null)
				import.Warnings.Add(readWarning);
		}

		report.Failures.AddRange(failures);

		if (candidates.Count == 0)
			return FinalReport(report, created: false, wouldCreate: false);

		bool created = false;
		if (!playlistExists && !options.DryRun)
		{
			cancellationToken.ThrowIfCancellationRequested();
			try
			{
				await WithBusyRetry(() => DatabaseHelper.Instance.CreatePlaylist(playlistName));
			}
			catch (Exception ex)
			{
				foreach (var c in candidates)
					report.Failures.Add(new InputFailure { Input = c.Song.Path, Reason = $"Playlist '{playlistName}' could not be created: {ex.Message}" });
				return FinalReport(report, created, wouldCreate: false);
			}
			playlistExists = true;
			created = true;
		}

		// Read-only snapshot; always computed so dry runs report membership truthfully.
		var inPlaylist = await WithBusyRetry(() => DatabaseHelper.Instance.GetSongsInPlaylist(playlistName));
		var alreadyInPlaylist = inPlaylist.Select(s => s.Path).ToHashSet(StringComparer.Ordinal);

		if (!options.DryRun)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var songs = candidates.Select(c => c.Song).ToList();
			// Scan-meta is only tracked for files inside the libraries: the auto-scan reconciler
			// deletes tracked paths it cannot find under a library root, so out-of-library tracks
			// must not be registered there.
			var metas = candidates
				.Where(c => IsUnderLibraryRoot(c.Song.Path, libraryRoots))
				.Select(c => LibraryScanner.BuildFileScanMeta(c.Song.Path))
				.ToList();

			try
			{
				await WithBusyRetry(() => DatabaseHelper.Instance.InsertMultipleSongs(songs));
				await WithBusyRetry(() => DatabaseHelper.Instance.UpdateFileScanMeta(metas));
				await WithBusyRetry(() => DatabaseHelper.Instance.AddSongsToPlaylist(playlistName, songs.Select(s => s.Path).ToList()));
			}
			catch (Exception ex)
			{
				// The three steps are each transactional and idempotent, so a retry completes the
				// job without duplicating rows or links. A mid-run failure can leave a partial state
				// (songs already inserted, scan-meta and/or playlist links still pending); the
				// message must say that honestly rather than claiming a clean failure.
				foreach (var c in candidates)
					report.Failures.Add(new InputFailure { Input = c.Song.Path, Reason = $"Database write failed partway: {ex.Message} Some files may already be in the library or playlist; the remaining steps were not completed. Re-run the same command to finish." });
				return FinalReport(report, created, wouldCreate: false);
			}
		}

		foreach (var import in candidates)
		{
			string effectivePath = import.Song.Path;
			if (!IsUnderLibraryRoot(effectivePath, libraryRoots))
				import.Warnings.Add("Lives outside your configured libraries: excluded from auto-scan tracking and dropped by the next full re-scan.");
			if (options.DryRun)
			{
				report.Tracks.Add(Entry(import, effectivePath, alreadyInPlaylist.Contains(effectivePath) ? TrackStatus.AlreadyPresent : TrackStatus.WouldAdd, null));
			}
			else if (alreadyInPlaylist.Contains(effectivePath) || !addedPaths.Add(effectivePath))
			{
				report.Tracks.Add(Entry(import, effectivePath, TrackStatus.AlreadyPresent, "Already in this playlist; kept the existing position."));
			}
			else
			{
				report.Tracks.Add(Entry(import, effectivePath, TrackStatus.Added, null));
			}
		}

		return FinalReport(report, created, wouldCreate: options.DryRun && !playlistExists);
	}

	/// <summary>
	/// Copies the accumulated tracks and failures into the returned report with its creation flags.
	/// </summary>
	private static PlaylistAddReport FinalReport(PlaylistAddReport report, bool created, bool wouldCreate)
	{
		return new PlaylistAddReport
		{
			Playlist = report.Playlist,
			PlaylistCreated = created,
			PlaylistWouldBeCreated = wouldCreate,
			DryRun = report.DryRun,
			Tracks = report.Tracks,
			Failures = report.Failures,
			Notices = report.Notices,
		};
	}

	/// <summary>
	/// Determines whether this process runs with package identity (installed MSIX / VS-packaged debug).
	/// Without it, <c>Windows.Storage.ApplicationData</c> is unavailable and the shared library
	/// database cannot be reached.
	/// </summary>
	private static bool HasPackageIdentity()
	{
		try
		{
			_ = Windows.Storage.ApplicationData.Current.LocalFolder;
			return true;
		}
		catch
		{
			return false;
		}
	}

	/// <summary>
	/// Validates a playlist name with the same rules as the "Add New Playlist" dialog (which trims first).
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
			return "Name cannot end with a period";
		if (name.Length > 255)
			return "Name is too long (max 255 characters).";
		return null;
	}

	/// <summary>
	/// Reads the user's scan preferences with the same culture the scanner uses, falling back to
	/// permissive defaults when settings are unavailable.
	/// </summary>
	/// <returns>The ignore-duration, the duplicate flag, and a notice when the fallback was used.</returns>
	private static (double ignoreDuration, bool ignoreDuplicates, string? notice) ReadPreferences()
	{
		try
		{
			var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
			double duration = double.Parse(settings.Values[nameof(LocalSave.IgnoreTracksBelowDuration)]?.ToString() ?? "0");
			bool duplicates = bool.Parse(settings.Values[nameof(LocalSave.IgnoreDuplicateEnabled)]?.ToString() ?? "false");
			return (duration, duplicates, null);
		}
		catch
		{
			return (0, false, "App settings were unavailable; using no minimum duration and duplicate detection off.");
		}
	}

	/// <summary>
	/// Loads the effective library roots used to decide whether a file gets scan-meta tracking.
	/// Computed silently (unlike the scanner, which notifies): the CLI has no window for toasts.
	/// Missing folders are reported once via <paramref name="notices"/>.
	/// </summary>
	private static async Task<HashSet<string>> GetEffectiveLibraryRoots(List<string> notices)
	{
		var paths = (await WithBusyRetry(() => DatabaseHelper.Instance.GetAllLibraries()))
			.Select(l => l.Path)
			.OrderBy(f => f.Length)
			.ToList();

		var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var folder in paths)
		{
			if (!Directory.Exists(folder))
			{
				notices.Add($"Library folder not found, skipped for scan tracking: {folder}");
				continue;
			}
			if (!roots.Any(parent => IsSameOrNestedPath(folder, parent)))
				roots.Add(folder);
		}
		return roots;
	}

	/// <summary>
	/// Determines whether <paramref name="path"/> is the same folder as <paramref name="parent"/> or lives
	/// beneath it. Mirrors the scanner's separator-aware nesting check.
	/// </summary>
	private static bool IsSameOrNestedPath(string path, string parent)
	{
		var trimmedParent = parent.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

		return string.Equals(path, trimmedParent, StringComparison.OrdinalIgnoreCase)
			|| path.StartsWith(trimmedParent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Determines whether <paramref name="path"/> is the same folder as, or lives beneath, one of <paramref name="roots"/>.
	/// Mirrors the scanner's nesting check so CLI tracking agrees with auto-scan.
	/// </summary>
	private static bool IsUnderLibraryRoot(string path, HashSet<string> roots)
	{
		return roots.Any(root => IsSameOrNestedPath(path, root));
	}

	/// <summary>
	/// Expands the CLI inputs to a de-duplicated file list in input order (which defines playlist order).
	/// Expansion results within one folder or pattern are sorted by path for determinism.
	/// A literal file with a disabled extension is a failure; folder- and pattern-expanded files with
	/// disabled extensions are counted in a single notice, like the library scan filters silently.
	/// </summary>
	private static List<string> ResolveInputs(IReadOnlyList<string> inputs, List<string> enabledExtensions, List<InputFailure> failures, List<string> notices)
	{
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var files = new List<string>();
		int disabledCount = 0;

		void Add(string file)
		{
			string fullPath;
			try
			{
				fullPath = Path.GetFullPath(file);
			}
			catch (Exception ex)
			{
				failures.Add(new InputFailure { Input = file, Reason = $"Invalid path: {ex.Message}" });
				return;
			}
			if (seen.Add(fullPath))
				files.Add(fullPath);
		}

		foreach (var input in inputs)
		{
			if (File.Exists(input))
			{
				// Normalize to on-disk casing: rescan comparisons are ordinal, so a differently
				// cased literal would otherwise churn history and playlist links on re-scan.
				string literal = ResolveDiskCasing(input) ?? input;
				if (!enabledExtensions.Contains(Path.GetExtension(literal).ToLowerInvariant()))
					failures.Add(new InputFailure { Input = literal, Reason = $"File type '{Path.GetExtension(literal)}' is disabled in Settings > File Extensions." });
				else
					Add(literal);
				continue;
			}

			foreach (var file in ExpandInput(input, failures))
			{
				if (enabledExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
					Add(file);
				else
					disabledCount++;
			}
		}

		if (disabledCount > 0)
			notices.Add($"{disabledCount} file(s) skipped: extension disabled in Settings > File Extensions.");

		return files;
	}

	/// <summary>
	/// Resolves <paramref name="path"/> to its on-disk casing via the parent directory.
	/// Returns null when the casing cannot be determined; callers then keep the given path.
	/// </summary>
	private static string? ResolveDiskCasing(string path)
	{
		try
		{
			var name = Path.GetFileName(path);
			var directory = Path.GetDirectoryName(path);
			if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(directory) || name.IndexOfAny(['*', '?']) >= 0)
				return null;
			return Directory.GetFiles(directory, name).FirstOrDefault();
		}
		catch
		{
			return null;
		}
	}

	/// <summary>
	/// Expands one non-literal input to files, sorted by path: a folder (recursive, skipping
	/// inaccessible subfolders like the scanner, depth-limited against junction loops) or a wildcard
	/// pattern (<c>*</c> and <c>?</c> in the file name only).
	/// </summary>
	private static IEnumerable<string> ExpandInput(string input, List<InputFailure> failures)
	{
		if (Directory.Exists(input))
		{
			var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, MaxRecursionDepth = 128 };
			List<string> found = new();
			try
			{
				found.AddRange(Directory.EnumerateFiles(input, "*.*", options).OrderBy(f => f, StringComparer.Ordinal));
			}
			catch (Exception ex)
			{
				failures.Add(new InputFailure { Input = input, Reason = $"Could not list folder: {ex.Message}" });
				yield break;
			}

			if (found.Count == 0)
				failures.Add(new InputFailure { Input = input, Reason = "Folder contains no files." });

			foreach (var file in found)
				yield return file;
			yield break;
		}

		if (input.IndexOfAny(['*', '?']) >= 0)
		{
			var directory = Path.GetDirectoryName(input);
			var pattern = Path.GetFileName(input) ?? "*.*";
			if (string.IsNullOrEmpty(directory))
				directory = Directory.GetCurrentDirectory();

			if (directory.IndexOfAny(['*', '?']) >= 0)
			{
				failures.Add(new InputFailure { Input = input, Reason = "Wildcards are only supported in the file name; pass a folder to include subfolders." });
				yield break;
			}

			string[] found;
			try
			{
				found = Directory.Exists(directory)
					? Directory.GetFiles(directory, pattern).OrderBy(f => f, StringComparer.Ordinal).ToArray()
					: [];
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

	/// <summary>
	/// Builds the report entry for a track that was read.
	/// </summary>
	private static TrackEntry Entry(CliMetadataReader.TrackImport import, string effectivePath, TrackStatus status, string? reason)
	{
		return new TrackEntry
		{
			Path = effectivePath,
			Title = string.IsNullOrWhiteSpace(import.Song.Title) ? System.IO.Path.GetFileNameWithoutExtension(effectivePath) : import.Song.Title,
			Status = status,
			Reason = reason,
			Imported = import.Imported,
			Defaulted = import.Defaulted,
			Skipped = import.Skipped,
			Warnings = import.Warnings,
		};
	}

	/// <summary>
	/// Builds the report entry for a track skipped by policy.
	/// </summary>
	private static TrackEntry SkippedEntry(CliMetadataReader.TrackImport import, string effectivePath, string reason)
	{
		return Entry(import, effectivePath, TrackStatus.Skipped, reason);
	}

	/// <summary>
	/// Builds the report entry for a file skipped before it was read (external-path policy), so the
	/// entry shows only the path and the reason.
	/// </summary>
	private static TrackEntry SkippedFileEntry(string path, string reason)
	{
		return new TrackEntry
		{
			Path = path,
			Title = System.IO.Path.GetFileNameWithoutExtension(path),
			Status = TrackStatus.Skipped,
			Reason = reason,
		};
	}

	/// <summary>
	/// Describes why a full metadata read failed. When the tags themselves open fine, the failure
	/// came from a later step (typically an undecodable cover picture), which the message says.
	/// </summary>
	private static async Task<string> DescribeReadFailure(string file)
	{
		try
		{
			await Task.Run(() =>
			{
				using var _ = TagLib.File.Create(file);
			});
		}
		catch
		{
			return "Embedded tags could not be read; imported with file-name fallback values.";
		}
		return "Full metadata read failed (possibly undecodable cover art); imported with fallback values.";
	}

	/// <summary>
	/// Retries a database operation for transient SQLite lock contention, so CLI runs work while the
	/// Tunetastic GUI holds the database briefly. The policy is small and bounded: 3 attempts with a
	/// 40 ms delay, mirroring <see cref="DatabaseHelper.GetSongByPathNoCase"/>. Every wrapped operation
	/// is read-only, a transactional upsert, or safe to re-run (idempotent, or because a Busy/Locked
	/// error in the default rollback-journal mode means the failing statement never committed);
	/// a retry therefore cannot repeat a partial write. When the retries
	/// run out the failure propagates to the caller and is reported per input.
	/// </summary>
	private static async Task<T> WithBusyRetry<T>(Func<Task<T>> operation, int attempts = 3)
	{
		for (int attempt = 1; ; attempt++)
		{
			try
			{
				return await operation();
			}
			catch (Exception ex) when (IsLocked(ex) && attempt < attempts)
			{
				await Task.Delay(40);
			}
		}
	}

	/// <summary>
	/// Retries a database operation without a result for transient SQLite lock contention.
	/// Same bounded policy as <see cref="WithBusyRetry{T}(Func{Task{T}}, int)"/>: 3 attempts, 40 ms delay.
	/// </summary>
	private static async Task WithBusyRetry(Func<Task> operation, int attempts = 3)
	{
		for (int attempt = 1; ; attempt++)
		{
			try
			{
				await operation();
				return;
			}
			catch (Exception ex) when (IsLocked(ex) && attempt < attempts)
			{
				await Task.Delay(40);
			}
		}
	}

	/// <summary>
	/// Determines whether <paramref name="ex"/> is a database lock contention that is worth retrying.
	/// Only SQLite errors qualify; message sniffing is a fallback for provider variants of the same error.
	/// </summary>
	private static bool IsLocked(Exception ex)
	{
		if (ex is not SQLiteException sqliteEx)
			return false;
		if (sqliteEx.Result == SQLite3.Result.Busy || sqliteEx.Result == SQLite3.Result.Locked)
			return true;

		return sqliteEx.Message.Contains("busy", StringComparison.OrdinalIgnoreCase)
			|| sqliteEx.Message.Contains("locked", StringComparison.OrdinalIgnoreCase);
	}
}
