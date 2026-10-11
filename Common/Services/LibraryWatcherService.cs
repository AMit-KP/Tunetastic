using System.Collections.Concurrent;

namespace Tunetastic.Common.Services;

/// <summary>
/// Watches the tracked library folders and applies file system changes to the database as they happen.
/// Changes are debounced into batches, matched against the scan metadata so a rename or a move keeps the
/// existing row (play counts, dates and playlist membership), and handed back to the caller for a full
/// scan when a burst is too large to be processed incrementally.
/// </summary>
public static class LibraryWatcherService
{
	/// <summary>
	/// A change observed for one path that has not been flushed yet, together with the time of its last event.
	/// </summary>
	private class PendingChange
	{
		public WatcherChangeTypes ChangeType;
		public DateTime LastEventUtc;
	}

	private static readonly Dictionary<string, FileSystemWatcher> _watchers = new(StringComparer.OrdinalIgnoreCase);
	private static readonly ConcurrentDictionary<string, PendingChange> _pending = new(StringComparer.OrdinalIgnoreCase);
	private static readonly ConcurrentDictionary<string, DateTime> _selfInitiatedPaths = new(StringComparer.OrdinalIgnoreCase);

	// Deletions are held back for a short grace period, so a move that reaches us as a delete plus a
	// create (copy + delete across volumes, or a create flushed before its delete) is still handled as a
	// rename that keeps play counts, dates and playlist membership.
	private static readonly ConcurrentDictionary<string, DateTime> _deferredDeletes = new(StringComparer.OrdinalIgnoreCase);

	// Files the watcher added itself, with the fingerprint needed to recognise a later delete as the
	// source half of that same move.
	private static readonly ConcurrentDictionary<string, (long FileSizeBytes, long LastModifiedUtc, DateTime CreatedUtc)> _recentlyCreated = new(StringComparer.OrdinalIgnoreCase);

	private static System.Threading.Timer? _debounceTimer;
	private static List<string> _enabledExtensions = new();
	private static int _flushInProgress = 0;

	private static readonly TimeSpan QuietWindow = TimeSpan.FromMilliseconds(800);
	// One second covers the few flush ticks (300 ms each) a delete can precede its matching create by;
	// anything longer only delays genuinely deleted entries from being removed.
	private static readonly TimeSpan DeleteGraceWindow = TimeSpan.FromSeconds(1);
	private static readonly TimeSpan StitchWindow = TimeSpan.FromMinutes(2);
	private const int DebounceTickMs = 300;

	/// <summary>
	/// Raised when too many changes are pending at once, or when the watcher loses events, so the caller can
	/// offer a full scan instead of an incremental update.
	/// </summary>
	public static event Action? BulkChangeDetected;

	/// <summary>
	/// Number of pending changes above which the watcher stops processing them one by one and asks for a full scan instead.
	/// </summary>
	private static int BulkChangeThreshold => int.Parse(Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.AutoScanBulkThreshold)]?.ToString() ?? "50");

	/// <summary>
	/// Marks a path as written by the app itself, so the watcher event it produces only refreshes the scan
	/// metadata instead of being treated as an external change.
	/// </summary>
	/// <param name="path">The full path of the file the app is about to write to.</param>
	public static void MarkSelfInitiated(string path)
	{
		_selfInitiatedPaths[path] = DateTime.UtcNow;
	}

	/// <summary>
	/// Checks whether a path carries a self-initiated mark and clears it in the same step, so a single mark is
	/// consumed at most once.
	/// </summary>
	/// <param name="path">The full path of the file to check.</param>
	/// <returns>
	/// <see langword="true"/> when the path carried a mark that is still within <see cref="StitchWindow"/>;
	/// otherwise, <see langword="false"/>.
	/// </returns>
	private static bool ConsumeSelfInitiated(string path)
	{
		if (!_selfInitiatedPaths.TryRemove(path, out var markedUtc))
			return false;

		// A stale mark (the app's own write never happened, e.g. a tag save failed because the file was
		// in use) must not swallow a real change that arrives later on.
		return DateTime.UtcNow - markedUtc <= StitchWindow;
	}

	/// <summary>
	/// Starts watching every effective library root. Any watch that is already running is stopped first, and the
	/// tracked extensions are reloaded so the watchers always reflect the current library configuration.
	/// </summary>
	/// <returns>A task that represents the asynchronous operation of creating the watchers and the debounce timer.</returns>
	public static async Task StartWatching()
	{
		await StopWatching(drainPending: false);

		var libraries = new List<string>();
		foreach (var lib in await DatabaseHelper.Instance.GetAllLibraries())
			libraries.Add(lib.Path);

		var roots = LibraryScanner.ComputeEffectiveRoots(libraries);
		_enabledExtensions = await LibraryScanner.GetEnabledExtensions();

		foreach (var root in roots)
		{
			var watcher = new FileSystemWatcher(root)
			{
				IncludeSubdirectories = true,
				InternalBufferSize = 65536,
				NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Size
			};

			watcher.Created += OnCreated;
			watcher.Changed += OnChanged;
			watcher.Deleted += OnDeleted;
			watcher.Renamed += OnRenamed;
			watcher.Error += OnError;
			watcher.EnableRaisingEvents = true;

			_watchers[root] = watcher;
		}

		_debounceTimer = new System.Threading.Timer(_ => { _ = FlushPendingChanges(); }, null, DebounceTickMs, DebounceTickMs);
	}

	/// <summary>
	/// Stops and disposes every watcher together with the debounce timer, optionally applying the changes that
	/// are still pending.
	/// </summary>
	/// <param name="drainPending">
	/// <see langword="true"/> to apply the pending changes before the queue is cleared;
	/// <see langword="false"/> to discard them, for example when the watcher is restarted right away.
	/// </param>
	/// <returns>A task that represents the asynchronous operation of stopping the watch.</returns>
	/// <remarks>
	/// The pending queue is cleared in either case. A drain that exceeds <see cref="BulkChangeThreshold"/>
	/// raises <see cref="BulkChangeDetected"/> instead of applying the changes itself.
	/// </remarks>
	public static async Task StopWatching(bool drainPending = true)
	{
		_debounceTimer?.Dispose();
		_debounceTimer = null;

		foreach (var watcher in _watchers.Values)
		{
			watcher.EnableRaisingEvents = false;
			watcher.Created -= OnCreated;
			watcher.Changed -= OnChanged;
			watcher.Deleted -= OnDeleted;
			watcher.Renamed -= OnRenamed;
			watcher.Error -= OnError;
			watcher.Dispose();
		}
		_watchers.Clear();

		if (drainPending)
			await DrainPendingImmediately();

		_pending.Clear();
	}

	/// <summary>
	/// Applies the pending changes immediately instead of waiting for the debounce quiet window to expire.
	/// </summary>
	/// <returns>A task that represents the asynchronous flush of the pending changes.</returns>
	/// <remarks>
	/// When more changes are waiting than <see cref="BulkChangeThreshold"/>, processing them one by one would be
	/// slower than a fresh scan, so <see cref="BulkChangeDetected"/> is raised and the queue is left untouched.
	/// </remarks>
	public static async Task DrainPendingImmediately()
	{
		if (_pending.Count >= BulkChangeThreshold)
		{
			BulkChangeDetected?.Invoke();
			return;
		}

		await FlushPendingChanges(forceAll: true);
	}

	/// <summary>
	/// Records a change for a path so the next flush can process it as part of a batch. Writes that the app
	/// initiated itself are handled right away and never enter the batch.
	/// </summary>
	/// <param name="path">The full path of the file that changed.</param>
	/// <param name="changeType">The kind of change reported by the watcher.</param>
	private static void EnqueueChange(string path, WatcherChangeTypes changeType)
	{
		if (ConsumeSelfInitiated(path))
		{
			_ = HandleSelfInitiatedRefresh(path, changeType);
			return;
		}

		_pending.AddOrUpdate(
			path,
			_ => new PendingChange { ChangeType = changeType, LastEventUtc = DateTime.UtcNow },
			(_, existing) =>
			{
				// Don't downgrade Created -> Changed; a burst of writes right after
				// creation is still "this file is new", not "this file was modified".
				if (existing.ChangeType != WatcherChangeTypes.Created)
					existing.ChangeType = changeType;

				existing.LastEventUtc = DateTime.UtcNow;
				return existing;
			});
	}

	/// <summary>
	/// Refreshes the scan metadata of a file the app wrote to itself, bypassing the rename and delete matching
	/// because the app already knows what it changed. Deletions are ignored, as the app removes those rows in
	/// its own delete flow.
	/// </summary>
	/// <param name="path">The full path of the file the app wrote to.</param>
	/// <param name="changeType">The kind of change reported by the watcher.</param>
	/// <returns>A task that represents the asynchronous metadata refresh.</returns>
	private static async Task HandleSelfInitiatedRefresh(string path, WatcherChangeTypes changeType)
	{
		if (changeType == WatcherChangeTypes.Deleted)
			return; // DB row already removed by the app's own delete flow — nothing to do

		try
		{
			var meta = LibraryScanner.BuildFileScanMeta(path);
			await DatabaseHelper.Instance.UpdateFileScanMeta(new List<FileScanMeta> { meta });
		}
		catch (Exception)
		{
			// file may already be gone by the time we get here — ignore
		}
	}

	/// <summary>
	/// Determines whether a path ends in one of the audio extensions that are currently enabled.
	/// </summary>
	/// <param name="path">The full path of the file to test.</param>
	/// <returns>
	/// <see langword="true"/> when the file extension is tracked; otherwise, <see langword="false"/>, which also
	/// covers paths whose extension cannot be read.
	/// </returns>
	private static bool IsTrackedExtension(string path)
	{
		try
		{
			return _enabledExtensions.Contains(Path.GetExtension(path).ToLower());
		}
		catch (Exception)
		{
			return false;
		}
	}

	/// <summary>
	/// Queues a newly created tracked file for the next batch.
	/// </summary>
	/// <param name="sender">The <see cref="FileSystemWatcher"/> that raised the event.</param>
	/// <param name="e">The event data holding the full path of the created file.</param>
	private static void OnCreated(object sender, FileSystemEventArgs e)
	{
		if (!IsTrackedExtension(e.FullPath)) return;
		EnqueueChange(e.FullPath, WatcherChangeTypes.Created);
	}

	/// <summary>
	/// Queues a modified tracked file for the next batch.
	/// </summary>
	/// <param name="sender">The <see cref="FileSystemWatcher"/> that raised the event.</param>
	/// <param name="e">The event data holding the full path of the modified file.</param>
	private static void OnChanged(object sender, FileSystemEventArgs e)
	{
		if (!IsTrackedExtension(e.FullPath)) return;
		EnqueueChange(e.FullPath, WatcherChangeTypes.Changed);
	}

	/// <summary>
	/// Queues a deleted tracked file so its own row can be removed once the deletion grace window has passed.
	/// </summary>
	/// <param name="sender">The <see cref="FileSystemWatcher"/> that raised the event.</param>
	/// <param name="e">The event data holding the full path of the deleted file.</param>
	private static void OnDeleted(object sender, FileSystemEventArgs e)
	{
		if (!IsTrackedExtension(e.FullPath)) return;
		EnqueueChange(e.FullPath, WatcherChangeTypes.Deleted);
	}

	/// <summary>
	/// Handles a rename reported directly by the watcher. The database work is started off the event thread so
	/// the watcher keeps receiving events while the update runs.
	/// </summary>
	/// <param name="sender">The <see cref="FileSystemWatcher"/> that raised the event.</param>
	/// <param name="e">The event data holding the old and the new full path.</param>
	private static void OnRenamed(object sender, RenamedEventArgs e)
	{
		_ = HandleRenamedEvent(e.OldFullPath, e.FullPath);
	}

	/// <summary>
	/// Applies a rename or a move: the existing row is kept when both paths are tracked, removed when the file
	/// left the library, and created when the file entered it.
	/// </summary>
	/// <param name="oldPath">The full path the file had before the rename.</param>
	/// <param name="newPath">The full path the file has after the rename.</param>
	/// <returns>A task that represents the asynchronous update of the database.</returns>
	private static async Task HandleRenamedEvent(string oldPath, string newPath)
	{
		// A mark only ever means the app wrote to the file itself (tag save), and those writes never change
		// a path. So a rename/move is always a real change: clear any mark — including a stale one left
		// behind by a tag save that failed — and update the entry instead of only refreshing its scan meta.
		ConsumeSelfInitiated(oldPath);
		ConsumeSelfInitiated(newPath);

		try
		{
			bool oldTracked = IsTrackedExtension(oldPath);
			bool newTracked = IsTrackedExtension(newPath);

			if (!oldTracked && !newTracked) return;

			if (oldTracked && !newTracked)
			{
				await DatabaseHelper.Instance.DeleteSongFromDB(oldPath);
				await DatabaseHelper.Instance.DeleteFileScanMeta(new List<string> { oldPath });
				_deferredDeletes.TryRemove(oldPath, out _);
				_recentlyCreated.TryRemove(oldPath, out _);

				// Deleted from the database — refresh whichever library/playlist page is visible
				MainPage._instance?.RefreshVisibleLibraryPage();
				return;
			}

			if (!oldTracked && newTracked)
			{
				await Tunetastic.Common.Operations.FileChangeProcessor.ProcessFileChange(newPath, FileChangeType.Created);
				RememberCreated(newPath);

				// A new entry was added — refresh whichever library/playlist page is visible
				MainPage._instance?.RefreshVisibleLibraryPage();
				return;
			}

			await Tunetastic.Common.Operations.FileChangeProcessor.ProcessFileChange(oldPath, FileChangeType.Renamed, newPath);

			// The old entry was repointed, so nothing is left to delete for it.
			_pending.TryRemove(oldPath, out _);
			_deferredDeletes.TryRemove(oldPath, out _);

			// The visible page still holds the entry under its old path — without this the stale row
			// points at a file that no longer exists until the next navigation.
			MainPage._instance?.RefreshVisibleLibraryPage();
		}
		catch (Exception ex)
		{
			GlobalNotification.Error($"Couldn't track the moved file:\n{newPath}\n{ex.Message}");
		}
	}

	/// <summary>
	/// Reacts to a watcher error, typically a buffer overflow, by asking for a full scan because the events that
	/// were lost while the buffer was full can no longer be recovered.
	/// </summary>
	/// <param name="sender">The <see cref="FileSystemWatcher"/> that raised the error.</param>
	/// <param name="e">The event data describing the error.</param>
	private static void OnError(object sender, ErrorEventArgs e)
	{
		// Buffer overflow or similar — treat exactly like a bulk-change burst.
		BulkChangeDetected?.Invoke();
	}

	/// <summary>
	/// Remembers a file that was just added, with the size and last write time that identify its content.
	/// </summary>
	private static void RememberCreated(string path)
	{
		try
		{
			var fi = new FileInfo(path);
			if (fi.Exists)
				_recentlyCreated[path] = (fi.Length, fi.LastWriteTimeUtc.Ticks, DateTime.UtcNow);
		}
		catch (Exception) { }
	}

	/// <summary>
	/// Applies the deletions whose grace window has passed and forgets creates that are too old to be
	/// matched to anything.
	/// </summary>
	/// <returns><see langword="true"/> when at least one entry was removed from the database.</returns>
	private static async Task<bool> ApplyExpiredDeletes()
	{
		var now = DateTime.UtcNow;
		bool applied = false;

		foreach (var entry in _deferredDeletes.ToList())
		{
			if (now - entry.Value < DeleteGraceWindow)
				continue;

			_deferredDeletes.TryRemove(entry.Key, out _);

			// The file came back before the window ended — the row stayed, so there is nothing to remove.
			if (File.Exists(entry.Key))
				continue;

			var meta = await DatabaseHelper.Instance.GetFileScanMeta(entry.Key);
			if (meta == null)
				continue;

			if (await TryStitchDeleteToRecentCreate(entry.Key, meta))
				continue;

			await DatabaseHelper.Instance.DeleteSongsFromDB(new List<string> { entry.Key });
			await DatabaseHelper.Instance.DeleteFileScanMeta(new List<string> { entry.Key });
			applied = true;
		}

		foreach (var entry in _recentlyCreated.ToList())
		{
			if (now - entry.Value.CreatedUtc > StitchWindow)
				_recentlyCreated.TryRemove(entry.Key, out _);
		}

		return applied;
	}

	/// <summary>
	/// Looks for a file that was added recently and carries the same size and last write time as the entry
	/// that is about to be removed. When one is found, this delete is really the source half of a move.
	/// </summary>
	/// <returns><see langword="true"/> when the deletion was handled as a move.</returns>
	private static async Task<bool> TryStitchDeleteToRecentCreate(string deletedPath, FileScanMeta meta)
	{
		foreach (var candidate in _recentlyCreated.ToList())
		{
			if (candidate.Value.FileSizeBytes != meta.FileSizeBytes || candidate.Value.LastModifiedUtc != meta.LastModifiedUtc)
				continue;

			if (!_recentlyCreated.TryRemove(candidate.Key, out _))
				continue;

			await DatabaseHelper.Instance.TransferSongData(deletedPath, candidate.Key);
			_pending.TryRemove(deletedPath, out _);
			return true;
		}

		return false;
	}

	/// <summary>
	/// Applies the changes that are ready as one batch: deletions whose grace window has passed are finalised
	/// first, then the remaining paths are split into deleted, created and modified sets, matched as renames and
	/// moves against the scan metadata, processed one by one and finally reported through
	/// <see cref="RefreshAfterBatch"/>.
	/// </summary>
	/// <param name="forceAll">
	/// <see langword="true"/> to process every pending change regardless of its quiet window;
	/// <see langword="false"/> to process only the paths that have been quiet for long enough.
	/// </param>
	/// <returns>A task that represents the asynchronous flush.</returns>
	/// <remarks>
	/// Reentrancy is guarded by <c>_flushInProgress</c>, so a debounce tick that fires while a flush is running
	/// returns immediately. Failures inside a pass are reported and never stop the debounce loop.
	/// </remarks>
	private static async Task FlushPendingChanges(bool forceAll = false)
	{
		if (Interlocked.CompareExchange(ref _flushInProgress, 1, 0) != 0)
			return;

		try
		{
			bool deletionsApplied = await ApplyExpiredDeletes();

			var now = DateTime.UtcNow;
			var readyPaths = _pending
				.Where(kv => forceAll || (now - kv.Value.LastEventUtc) >= QuietWindow)
				.Select(kv => kv.Key)
				.ToList();

			if (readyPaths.Count == 0)
			{
				if (deletionsApplied)
					await RefreshAfterBatch();
				return;
			}

			if (readyPaths.Count > BulkChangeThreshold)
			{
				foreach (var p in readyPaths) _pending.TryRemove(p, out _);
				BulkChangeDetected?.Invoke();
				return;
			}

			var readyEntries = new Dictionary<string, WatcherChangeTypes>(StringComparer.OrdinalIgnoreCase);
			foreach (var p in readyPaths)
			{
				if (_pending.TryRemove(p, out var change))
					readyEntries[p] = change.ChangeType;
			}

			var deletedPaths = readyEntries.Where(kv => kv.Value == WatcherChangeTypes.Deleted).Select(kv => kv.Key).ToList();
			var createdPaths = readyEntries.Where(kv => kv.Value == WatcherChangeTypes.Created).Select(kv => kv.Key).ToList();
			var modifiedPaths = readyEntries.Where(kv => kv.Value == WatcherChangeTypes.Changed).Select(kv => kv.Key).ToList();

			var disappeared = new Dictionary<string, FileScanMeta>(StringComparer.OrdinalIgnoreCase);
			foreach (var path in deletedPaths)
			{
				var meta = await DatabaseHelper.Instance.GetFileScanMeta(path);
				if (meta != null) disappeared[path] = meta;
			}

			// Deletes that are known but not flushed yet, and deletions still inside their grace window:
			// a create that lands after its source delete was observed still pairs up with them, which is
			// what keeps a move across folders or libraries a rename instead of delete plus new.
			foreach (var kv in _pending)
			{
				if (kv.Value.ChangeType != WatcherChangeTypes.Deleted || disappeared.ContainsKey(kv.Key))
					continue;

				var meta = await DatabaseHelper.Instance.GetFileScanMeta(kv.Key);
				if (meta != null) disappeared[kv.Key] = meta;
			}

			foreach (var path in _deferredDeletes.Keys)
			{
				if (disappeared.ContainsKey(path))
					continue;

				if (File.Exists(path))
				{
					// Back on disk before the grace window ended — keep the row and stop tracking it.
					_deferredDeletes.TryRemove(path, out _);
					continue;
				}

				var meta = await DatabaseHelper.Instance.GetFileScanMeta(path);
				if (meta != null) disappeared[path] = meta;
			}

			var appeared = new Dictionary<string, (long FileSizeBytes, long LastModifiedUtc, long CreationTimeUtc)>(StringComparer.OrdinalIgnoreCase);
			foreach (var path in createdPaths)
			{
				try
				{
					var fi = new FileInfo(path);
					if (fi.Exists)
						appeared[path] = (fi.Length, fi.LastWriteTimeUtc.Ticks, fi.CreationTimeUtc.Ticks);
				}
				catch (Exception) { }
			}

			var matchResult = Tunetastic.Common.Operations.RenameDetector.DetectRenamesAndMoves(disappeared, appeared);

			foreach (var (oldPath, newPath) in matchResult.Renames)
			{
				try
				{
					await Tunetastic.Common.Operations.FileChangeProcessor.ProcessFileChange(oldPath, FileChangeType.Renamed, newPath);

					// The old entry was repointed, so there is nothing left to delete for it.
					_pending.TryRemove(oldPath, out _);
					_deferredDeletes.TryRemove(oldPath, out _);
				}
				catch (Exception ex)
				{
					GlobalNotification.Error($"Couldn't track the moved file:\n{newPath}\n{ex.Message}");
				}
			}

			foreach (var path in matchResult.UnmatchedDisappeared)
			{
				// Held back briefly (see DeleteGraceWindow) so a move that arrives as create plus delete
				// can still be stitched together with its play data intact.
				_deferredDeletes.TryAdd(path, DateTime.UtcNow);
			}

			foreach (var path in matchResult.UnmatchedAppeared)
			{
				try
				{
					await Tunetastic.Common.Operations.FileChangeProcessor.ProcessFileChange(path, FileChangeType.Created);
					RememberCreated(path);
				}
				catch (Exception ex)
				{
					GlobalNotification.Error($"Couldn't add the new file to the library:\n{path}\n{ex.Message}");
				}
			}

			foreach (var path in modifiedPaths)
			{
				try
				{
					await Tunetastic.Common.Operations.FileChangeProcessor.ProcessFileChange(path, FileChangeType.Modified);
				}
				catch (Exception ex)
				{
					GlobalNotification.Error($"Couldn't refresh the library entry for:\n{path}\n{ex.Message}");
				}
			}

			await RefreshAfterBatch();
		}
		catch (Exception ex)
		{
			// A failing pass must never kill the debounce loop silently.
			GlobalNotification.Error($"An automatic scan pass failed.\n{ex.Message}");
		}
		finally
		{
			Interlocked.Exchange(ref _flushInProgress, 0);
		}
	}

	/// <summary>
	/// Recounts the tracked folders and refreshes the visible page after a batch was applied.
	/// </summary>
	private static async Task RefreshAfterBatch()
	{
		// The flush only sees the changed batch, so recount folders from the tracked inventory after the
		// batch was applied — same consistency model as the songs count between full scans.
		await LibraryScanner.RefreshAutoScanResultMessage(LibraryScanner.CountFoldersFromPaths((await DatabaseHelper.Instance.GetAllFileScanMeta()).Select(m => m.Path)));

		// The processed batch changed the database — refresh whichever library/playlist page is visible
		MainPage._instance?.RefreshVisibleLibraryPage();
	}
}
