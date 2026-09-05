using System.Collections.Concurrent;

namespace Tunetastic.Common.Services;

public static class LibraryWatcherService
{
	private class PendingChange
	{
		public WatcherChangeTypes ChangeType;
		public DateTime LastEventUtc;
	}

	private static readonly Dictionary<string, FileSystemWatcher> _watchers = new(StringComparer.OrdinalIgnoreCase);
	private static readonly ConcurrentDictionary<string, PendingChange> _pending = new(StringComparer.OrdinalIgnoreCase);
	private static readonly HashSet<string> _selfInitiatedPaths = new(StringComparer.OrdinalIgnoreCase);
	private static readonly object _selfInitiatedLock = new();

	private static System.Threading.Timer? _debounceTimer;
	private static List<string> _enabledExtensions = new();
	private static int _flushInProgress = 0;

	private static readonly TimeSpan QuietWindow = TimeSpan.FromMilliseconds(800);
	private const int DebounceTickMs = 300;

	public static event Action? BulkChangeDetected;

	private static int BulkChangeThreshold =>
		// TODO: tune this value during testing
		int.Parse(Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.AutoScanBulkThreshold)]?.ToString() ?? "50");

	public static void MarkSelfInitiated(string path)
	{
		lock (_selfInitiatedLock)
		{
			_selfInitiatedPaths.Add(path);
		}
	}

	private static bool ConsumeSelfInitiated(string path)
	{
		lock (_selfInitiatedLock)
		{
			return _selfInitiatedPaths.Remove(path);
		}
	}

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

	public static async Task DrainPendingImmediately()
	{
		if (_pending.Count >= BulkChangeThreshold)
		{
			BulkChangeDetected?.Invoke();
			return;
		}

		await FlushPendingChanges(forceAll: true);
	}

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

	private static void OnCreated(object sender, FileSystemEventArgs e)
	{
		if (!IsTrackedExtension(e.FullPath)) return;
		EnqueueChange(e.FullPath, WatcherChangeTypes.Created);
	}

	private static void OnChanged(object sender, FileSystemEventArgs e)
	{
		if (!IsTrackedExtension(e.FullPath)) return;
		EnqueueChange(e.FullPath, WatcherChangeTypes.Changed);
	}

	private static void OnDeleted(object sender, FileSystemEventArgs e)
	{
		if (!IsTrackedExtension(e.FullPath)) return;
		EnqueueChange(e.FullPath, WatcherChangeTypes.Deleted);
	}

	private static void OnRenamed(object sender, RenamedEventArgs e)
	{
		_ = HandleRenamedEvent(e.OldFullPath, e.FullPath);
	}

	private static async Task HandleRenamedEvent(string oldPath, string newPath)
	{
		if (ConsumeSelfInitiated(oldPath))
		{
			await HandleSelfInitiatedRefresh(newPath, WatcherChangeTypes.Changed);
			return;
		}

		bool oldTracked = IsTrackedExtension(oldPath);
		bool newTracked = IsTrackedExtension(newPath);

		if (!oldTracked && !newTracked) return;

		if (oldTracked && !newTracked)
		{
			await DatabaseHelper.Instance.DeleteSongFromDB(oldPath);
			await DatabaseHelper.Instance.DeleteFileScanMeta(new List<string> { oldPath });
			return;
		}

		if (!oldTracked && newTracked)
		{
			await Tunetastic.Common.Operations.FileChangeProcessor.ProcessFileChange(newPath, FileChangeType.Created);
			return;
		}

		await Tunetastic.Common.Operations.FileChangeProcessor.ProcessFileChange(oldPath, FileChangeType.Renamed, newPath);
	}

	private static void OnError(object sender, ErrorEventArgs e)
	{
		// Buffer overflow or similar — treat exactly like a bulk-change burst.
		BulkChangeDetected?.Invoke();
	}

	private static async Task FlushPendingChanges(bool forceAll = false)
	{
		if (Interlocked.CompareExchange(ref _flushInProgress, 1, 0) != 0)
			return;

		try
		{
			var now = DateTime.UtcNow;
			var readyPaths = _pending
				.Where(kv => forceAll || (now - kv.Value.LastEventUtc) >= QuietWindow)
				.Select(kv => kv.Key)
				.ToList();

			if (readyPaths.Count == 0) return;

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
				await Tunetastic.Common.Operations.FileChangeProcessor.ProcessFileChange(oldPath, FileChangeType.Renamed, newPath);

			if (matchResult.UnmatchedDisappeared.Count > 0)
			{
				await DatabaseHelper.Instance.DeleteSongsFromDB(matchResult.UnmatchedDisappeared);
				await DatabaseHelper.Instance.DeleteFileScanMeta(matchResult.UnmatchedDisappeared);
			}

			foreach (var path in matchResult.UnmatchedAppeared)
				await Tunetastic.Common.Operations.FileChangeProcessor.ProcessFileChange(path, FileChangeType.Created);

			foreach (var path in modifiedPaths)
				await Tunetastic.Common.Operations.FileChangeProcessor.ProcessFileChange(path, FileChangeType.Modified);

			await LibraryScanner.RefreshAutoScanResultMessage();
		}
		finally
		{
			Interlocked.Exchange(ref _flushInProgress, 0);
		}
	}
}
