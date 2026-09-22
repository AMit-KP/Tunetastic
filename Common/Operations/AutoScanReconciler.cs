using System.Collections.Concurrent;

namespace Tunetastic.Common.Operations;

public static class AutoScanReconciler
{
	public static async Task RunCatchUpDiff(bool showNotification)
	{
		var libraries = new List<string>();
		foreach (LibraryModel library in await DatabaseHelper.Instance.GetAllLibraries())
			libraries.Add(library.Path);

		if (libraries.Count == 0)
			return;

		if (showNotification)
			GlobalNotification.Info("Scanning for changes please wait...");

		var effectiveRoots = LibraryScanner.ComputeEffectiveRoots(libraries);
		var extensions = await LibraryScanner.GetEnabledExtensions();

		var options = new EnumerationOptions { RecurseSubdirectories = true };
		var onDisk = new Dictionary<string, (long FileSizeBytes, long LastModifiedUtc, long CreationTimeUtc)>(StringComparer.OrdinalIgnoreCase);

		foreach (var folder in effectiveRoots)
		{
			var files = Directory.EnumerateFiles(folder, "*.*", options)
								 .Where(file => extensions.Contains(Path.GetExtension(file).ToLower()));

			foreach (var file in files)
			{
				var fileInfo = new FileInfo(file);
				onDisk[file] = (fileInfo.Length, fileInfo.LastWriteTimeUtc.Ticks, fileInfo.CreationTimeUtc.Ticks);
			}
		}

		var trackedMeta = await DatabaseHelper.Instance.GetAllFileScanMeta();
		var tracked = trackedMeta.ToDictionary(m => m.Path, m => m, StringComparer.OrdinalIgnoreCase);

		var disappearedPaths = tracked.Keys.Where(p => !onDisk.ContainsKey(p)).ToList();
		var appearedPaths = onDisk.Keys.Where(p => !tracked.ContainsKey(p)).ToList();

		var disappeared = disappearedPaths.ToDictionary(p => p, p => tracked[p], StringComparer.OrdinalIgnoreCase);
		var appeared = appearedPaths.ToDictionary(p => p, p => onDisk[p], StringComparer.OrdinalIgnoreCase);

		var matchResult = RenameDetector.DetectRenamesAndMoves(disappeared, appeared);

		int failedChanges = 0;

		foreach (var (oldPath, newPath) in matchResult.Renames)
		{
			try
			{
				await FileChangeProcessor.ProcessFileChange(oldPath, FileChangeType.Renamed, newPath);
			}
			catch (Exception ex)
			{
				// A single failing file must not abort the pass: that would also skip the deletions,
				// the new files, the folder recount and the notification below.
				failedChanges++;
				GlobalNotification.Error($"Couldn't update the library entry for:\n{oldPath}\n{ex.Message}");
			}
		}

		if (matchResult.UnmatchedDisappeared.Count > 0)
		{
			try
			{
				await DatabaseHelper.Instance.DeleteSongsFromDB(matchResult.UnmatchedDisappeared);
				await DatabaseHelper.Instance.DeleteFileScanMeta(matchResult.UnmatchedDisappeared);
			}
			catch (Exception ex)
			{
				failedChanges++;
				GlobalNotification.Error($"Couldn't remove the missing tracks from the library.\n{ex.Message}");
			}
		}

		var modifiedPaths = onDisk.Keys
			.Where(p => tracked.ContainsKey(p))
			.Where(p => tracked[p].FileSizeBytes != onDisk[p].FileSizeBytes || tracked[p].LastModifiedUtc != onDisk[p].LastModifiedUtc)
			.ToList();

		try
		{
			await BatchProcessCreatedAndModified(matchResult.UnmatchedAppeared, modifiedPaths);
		}
		catch (Exception ex)
		{
			failedChanges++;
			GlobalNotification.Error($"Couldn't add the new or changed tracks to the library.\n{ex.Message}");
		}

		// onDisk holds every extension-matching file currently on disk — the same set a full scan counts —
		// so the folder stat can be recounted exactly, including folders emptied by deletions.
		await LibraryScanner.RefreshAutoScanResultMessage(LibraryScanner.CountFoldersFromPaths(onDisk.Keys));

		if (showNotification)
		{
			if (failedChanges > 0)
				GlobalNotification.Warning($"{failedChanges} change(s) could not be applied. See the messages above.");
			else
				GlobalNotification.Success("All libraries are in sync");
		}
	}

	private static async Task BatchProcessCreatedAndModified(List<string> createdPaths, List<string> modifiedPaths)
	{
		var allPaths = createdPaths.Concat(modifiedPaths).ToList();
		if (allPaths.Count == 0)
			return;

		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var ignoreTrackDuration = double.Parse(localSettings.Values[nameof(LocalSave.IgnoreTracksBelowDuration)]?.ToString() ?? "0");
		var ignoreDuplicates = bool.Parse(localSettings.Values[nameof(LocalSave.IgnoreDuplicateEnabled)]?.ToString() ?? "false");

		var existingSongs = (await DatabaseHelper.Instance.LoadSongsFromDB())
			.ToDictionary(s => s.Path, s => s, StringComparer.OrdinalIgnoreCase);

		var songsToUpsert = new ConcurrentBag<Song>();
		var metaToUpsert = new ConcurrentBag<FileScanMeta>();
		var pathsToDelete = new ConcurrentBag<string>();
		var uniqueMetadata = new ConcurrentDictionary<(string Title, string Artist, string Album), byte>();

		await Parallel.ForEachAsync(allPaths, async (filePath, ct) =>
		{
			var (song, succeeded) = await LibraryScanner.ExtractSongMetadata(filePath, ignoreTrackDuration);
			if (!succeeded) return;

			if (song.Duration <= ignoreTrackDuration)
			{
				if (existingSongs.ContainsKey(filePath))
					pathsToDelete.Add(filePath);
				return;
			}

			if (ignoreDuplicates)
			{
				bool dupInDb = await DatabaseHelper.Instance.SongMetadataExists(song.Title, song.Artists, song.Album, excludePath: filePath);
				bool dupInBatch = !uniqueMetadata.TryAdd((song.Title, song.Artists, song.Album), 0);
				if (dupInDb || dupInBatch) return;
			}

			if (existingSongs.TryGetValue(filePath, out var existingSong))
			{
				song.PlayCount = existingSong.PlayCount;
				song.DateLastPlayed = existingSong.DateLastPlayed;
			}

			songsToUpsert.Add(song);
			metaToUpsert.Add(LibraryScanner.BuildFileScanMeta(filePath));
		});

		if (pathsToDelete.Count > 0)
		{
			var deleteList = pathsToDelete.ToList();
			await DatabaseHelper.Instance.DeleteSongsFromDB(deleteList);
			await DatabaseHelper.Instance.DeleteFileScanMeta(deleteList);
		}

		if (songsToUpsert.Count > 0)
		{
			await DatabaseHelper.Instance.InsertMultipleSongs([.. songsToUpsert]);
			await DatabaseHelper.Instance.UpdateFileScanMeta([.. metaToUpsert]);
		}
	}
}
