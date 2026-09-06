namespace Tunetastic.Common.Operations;

public static class FileChangeProcessor
{
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
