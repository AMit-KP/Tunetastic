namespace Tunetastic.Common.Services;

public static class AutoScanService
{
	public static event Action? BulkChangeDetected
	{
		add => LibraryWatcherService.BulkChangeDetected += value;
		remove => LibraryWatcherService.BulkChangeDetected -= value;
	}

	public static async Task<bool> EnableAutoScan()
	{
		var trackedMeta = await DatabaseHelper.Instance.GetAllFileScanMeta();

		if (trackedMeta.Count == 0)
		{
			GlobalNotification.Error("Do a Full Scan atleast once.");
			return false;
		}

		await AutoScanReconciler.RunCatchUpDiff();

		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.AutoScanEnabled)] = true;

		await LibraryWatcherService.StartWatching();
		return true;
	}

	public static async Task DisableAutoScan()
	{
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.AutoScanEnabled)] = false;

		await LibraryWatcherService.StopWatching(drainPending: true);
	}

	public static async Task OnLibraryFolderAdded()
	{
		// Assumes DatabaseHelper.Instance.AddOrUpdateLibrary(...) already ran for the new folder.
		if (bool.Parse(Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.AutoScanEnabled)]?.ToString() ?? "false"))
			return;

		await AutoScanReconciler.RunCatchUpDiff();
		await LibraryWatcherService.StartWatching();
	}

	public static async Task OnLibraryFolderRemoved(string removedFolderPath)
	{
		// Assumes DatabaseHelper.Instance.RemoveLibrary(...) already ran for the removed folder.
		if (bool.Parse(Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.AutoScanEnabled)]?.ToString() ?? "false"))
			return;

		var removedPaths = await DatabaseHelper.Instance.DeleteSongsUnderFolder(removedFolderPath);
		await DatabaseHelper.Instance.DeleteFileScanMeta(removedPaths);

		await LibraryWatcherService.StartWatching();
	}

	public static async Task ResumeIfEnabled()
	{
		if (bool.Parse(Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.AutoScanEnabled)]?.ToString() ?? "false"))
			return;

		var trackedMeta = await DatabaseHelper.Instance.GetAllFileScanMeta();
		if (trackedMeta.Count == 0)
		{
			GlobalNotification.Warning("Auto scan was enabled but no scan data was found. Please run a full scan.");
			return;
		}

		await AutoScanReconciler.RunCatchUpDiff();
		await LibraryWatcherService.StartWatching();
	}
}
