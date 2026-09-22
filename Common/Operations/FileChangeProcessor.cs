namespace Tunetastic.Common.Operations;

/// <summary>
/// Applies individual file-system changes in the music libraries (created, modified, deleted and renamed files)
/// to the song database and to the scan metadata used by incremental scans.
/// </summary>
/// <remarks>
/// Shared entry point for single-file changes: <see cref="LibraryWatcherService"/> forwards live file-system
/// events and <see cref="AutoScanReconciler"/> forwards renames found while diffing the snapshot. Both callers
/// report one path at a time, outside a full scan, so folder counts and notifications stay with the caller.
/// </remarks>
public static class FileChangeProcessor
{
	/// <summary>
	/// Applies a single file-system change to the database, dispatching to the handler that matches
	/// <paramref name="changeType"/>.
	/// </summary>
	/// <remarks>
	/// <see cref="FileChangeType.Created"/> and <see cref="FileChangeType.Modified"/> read the file's tags and
	/// upsert the song with its scan metadata, <see cref="FileChangeType.Deleted"/> removes both rows, and
	/// <see cref="FileChangeType.Renamed"/> repoints a tracked file to its new path (or treats it as newly created
	/// when the old path had no scan metadata). The ignore preferences are read once per call, and database
	/// failures propagate to the caller.
	/// </remarks>
	/// <param name="path">
	/// The full path of the affected file. For <see cref="FileChangeType.Renamed"/> this is the old path.
	/// </param>
	/// <param name="changeType">The kind of change detected on the file.</param>
	/// <param name="newPath">
	/// The full path the file now lives at, used only for <see cref="FileChangeType.Renamed"/>; when it is null,
	/// empty or whitespace the call returns without touching the database.
	/// </param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	public static async Task ProcessFileChange(string path, FileChangeType changeType, string? newPath = null)
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var ignoreTrackDuration = double.Parse(localSettings.Values[nameof(LocalSave.IgnoreTracksBelowDuration)]?.ToString() ?? "0");
		var ignoreDuplicates = bool.Parse(localSettings.Values[nameof(LocalSave.IgnoreDuplicateEnabled)]?.ToString() ?? "false");

		switch (changeType)
		{
			case FileChangeType.Created:
				await HandleCreated(path, ignoreTrackDuration, ignoreDuplicates);
				break;

			case FileChangeType.Modified:
				await HandleModified(path, ignoreTrackDuration, ignoreDuplicates);
				break;

			case FileChangeType.Deleted:
				await DatabaseHelper.Instance.DeleteSongFromDB(path);
				await DatabaseHelper.Instance.DeleteFileScanMeta(new List<string> { path });
				break;

			case FileChangeType.Renamed:
				if (string.IsNullOrWhiteSpace(newPath))
					return;

				var existingMeta = await DatabaseHelper.Instance.GetFileScanMeta(path);
				if (existingMeta == null)
				{
					// old path wasn't tracked (e.g. untracked file type) — treat as a new file
					await HandleCreated(newPath, ignoreTrackDuration, ignoreDuplicates);
					return;
				}

				await DatabaseHelper.Instance.RenameSongPath(path, newPath);

				var updatedMeta = LibraryScanner.BuildFileScanMeta(newPath);
				await DatabaseHelper.Instance.UpdateFileScanMeta(new List<FileScanMeta> { updatedMeta });
				break;
		}
	}

	/// <summary>
	/// Inserts a newly discovered file as a song and records the scan metadata snapshot for it.
	/// </summary>
	/// <remarks>
	/// Nothing is written when the metadata cannot be read, when the duration is at or below
	/// <paramref name="ignoreTrackDuration"/>, or when duplicate filtering is on and the same title, artists and
	/// album already exist.
	/// </remarks>
	/// <param name="path">The full path of the created or newly appeared file.</param>
	/// <param name="ignoreTrackDuration">
	/// Tracks of this duration or shorter (in seconds) are filtered out; 0 keeps every readable track.
	/// </param>
	/// <param name="ignoreDuplicates">
	/// When true, a file whose title, artists and album already exist in the database is not inserted.
	/// </param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	private static async Task HandleCreated(string path, double ignoreTrackDuration, bool ignoreDuplicates)
	{
		var (song, succeeded) = await LibraryScanner.ExtractSongMetadata(path, ignoreTrackDuration);
		if (!succeeded) return;
		if (song.Duration <= ignoreTrackDuration) return;

		if (ignoreDuplicates && await DatabaseHelper.Instance.SongMetadataExists(song.Title, song.Artists, song.Album))
			return;

		await DatabaseHelper.Instance.InsertMultipleSongs(new List<Song> { song });
		await DatabaseHelper.Instance.UpdateFileScanMeta(new List<FileScanMeta> { LibraryScanner.BuildFileScanMeta(path) });
	}

	/// <summary>
	/// Refreshes the stored details of an already tracked file whose content changed.
	/// </summary>
	/// <remarks>
	/// <see cref="Song.PlayCount"/> and <see cref="Song.DateLastPlayed"/> are copied onto the newly read song, so the
	/// refresh keeps the playback history. A file that now falls at or below
	/// <paramref name="ignoreTrackDuration"/> is treated as if it went away, and an unreadable file or a duplicate
	/// (the current path excluded) leaves the database untouched.
	/// </remarks>
	/// <param name="path">The full path of the modified file.</param>
	/// <param name="ignoreTrackDuration">
	/// Tracks of this duration or shorter (in seconds) are removed from the library; 0 keeps every readable track.
	/// </param>
	/// <param name="ignoreDuplicates">
	/// When true, the refresh is skipped when another song with the same title, artists and album exists.
	/// </param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	private static async Task HandleModified(string path, double ignoreTrackDuration, bool ignoreDuplicates)
	{
		var (song, succeeded) = await LibraryScanner.ExtractSongMetadata(path, ignoreTrackDuration);
		if (!succeeded) return;

		if (song.Duration <= ignoreTrackDuration)
		{
			// re-encoded/edited below threshold — treat like the file went away
			await DatabaseHelper.Instance.DeleteSongFromDB(path);
			await DatabaseHelper.Instance.DeleteFileScanMeta(new List<string> { path });
			return;
		}

		if (ignoreDuplicates && await DatabaseHelper.Instance.SongMetadataExists(song.Title, song.Artists, song.Album, excludePath: path))
			return;

		// preserve PlayCount/DateLastPlayed — this is a metadata refresh, not a new song
		var existingSong = await DatabaseHelper.Instance.GetSongByPath(path);
		song.PlayCount = existingSong?.PlayCount ?? 0;
		song.DateLastPlayed = existingSong?.DateLastPlayed;

		await DatabaseHelper.Instance.InsertMultipleSongs(new List<Song> { song });
		await DatabaseHelper.Instance.UpdateFileScanMeta(new List<FileScanMeta> { LibraryScanner.BuildFileScanMeta(path) });
	}
}
