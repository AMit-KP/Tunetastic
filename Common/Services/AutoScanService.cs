namespace Tunetastic.Common.Services;

/// <summary>
/// Owns the Auto Scan (the "Auto Sync" switch) lifecycle: it starts and stops the incremental library watching
/// and remembers the user's choice across restarts.
/// </summary>
/// <remarks>
/// While it is on, <see cref="LibraryWatcherService"/> applies file changes as they happen and
/// <see cref="AutoScanReconciler"/> covers the changes missed while the app was not running.
/// </remarks>
public static class AutoScanService
{
	/// <summary>
	/// Raised when too many file changes are pending to apply incrementally, so a full scan can be offered.
	/// </summary>
	/// <remarks>
	/// Forwards to <see cref="LibraryWatcherService.BulkChangeDetected"/>; subscribing here is the same as
	/// subscribing to the watcher directly.
	/// </remarks>
	public static event Action? BulkChangeDetected
	{
		add => LibraryWatcherService.BulkChangeDetected += value;
		remove => LibraryWatcherService.BulkChangeDetected -= value;
	}

	/// <summary>
	/// Turns Auto Scan on: applies the changes missed since the last scan, stores the choice in
	/// <see cref="LocalSave.AutoScanEnabled"/> and starts watching the library folders.
	/// </summary>
	/// <remarks>
	/// The catch-up pass runs with notifications. Nothing is stored and the watcher is not started when no
	/// baseline scan exists. Called by the Auto Sync switch in the settings.
	/// </remarks>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result is <see langword="true"/> when Auto
	/// Scan was turned on, or <see langword="false"/> when no baseline scan exists — the caller should then
	/// switch the setting back off.
	/// </returns>
	public static async Task<bool> EnableAutoScan()
	{
		var trackedMeta = await DatabaseHelper.Instance.GetAllFileScanMeta();

		if (trackedMeta.Count == 0)
		{
			GlobalNotification.Error("Do a Full Scan atleast once with a folder that contains music.");
			return false;
		}

		await AutoScanReconciler.RunCatchUpDiff(showNotification: true);

		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.AutoScanEnabled)] = true;

		await LibraryWatcherService.StartWatching();

		BulkChangeDetected -= AutoScanService_BulkChangeDetected;
		BulkChangeDetected += AutoScanService_BulkChangeDetected;

		return true;
	}

	/// <summary>
	/// Turns Auto Scan off: stores the choice in <see cref="LocalSave.AutoScanEnabled"/>, stops watching the
	/// library folders and detaches the bulk-change handler.
	/// </summary>
	/// <remarks>
	/// The setting is written first, so an interruption cannot make the app resume watching on the next start.
	/// Pending changes are drained by <see cref="LibraryWatcherService.StopWatching"/>, so files changed just
	/// before the switch was turned off are still applied.
	/// </remarks>
	/// <returns>A task that represents the asynchronous operation of draining the changes and stopping the watch.</returns>
	public static async Task DisableAutoScan()
	{
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.AutoScanEnabled)] = false;

		await LibraryWatcherService.StopWatching(drainPending: true);

		AutoScanService.BulkChangeDetected -= AutoScanService_BulkChangeDetected;
	}

	/// <summary>
	/// Restores Auto Scan for the current session when the user had left it enabled, and does nothing when it
	/// is switched off.
	/// </summary>
	/// <remarks>
	/// Called on startup and after a full scan, because a scan stops the watcher. Unlike
	/// <see cref="EnableAutoScan"/> it writes no setting and keeps the catch-up pass silent. A stale switch
	/// over a database without tracked files is cleared with <see cref="DisableAutoScan"/>.
	/// </remarks>
	/// <returns>
	/// A task that represents the asynchronous operation. Once it completes, the watcher is running when Auto
	/// Scan is enabled, or switched off when no baseline scan exists.
	/// </returns>
	public static async Task ResumeIfEnabled()
	{
		if (!bool.Parse(Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.AutoScanEnabled)]?.ToString() ?? "false"))
			return;

		var trackedMeta = await DatabaseHelper.Instance.GetAllFileScanMeta();
		if (trackedMeta.Count == 0)
		{
			GlobalNotification.Warning("Auto scan was enabled but no music was found. Please run a full scan with a folder that contains music.");
			await DisableAutoScan();
			return;
		}

		BulkChangeDetected -= AutoScanService_BulkChangeDetected;
		BulkChangeDetected += AutoScanService_BulkChangeDetected;

		await AutoScanReconciler.RunCatchUpDiff(showNotification: false);
		await LibraryWatcherService.StartWatching();
	}

	/// <summary>
	/// Handles <see cref="BulkChangeDetected"/> by offering the user a full scan on the UI thread.
	/// </summary>
	/// <remarks>
	/// The dialog is modal, so window resizing is disabled while it is open. Only "Open Settings" navigates to
	/// <see cref="SettingsPage"/>; "Later" leaves the changes to the next app start.
	/// </remarks>
	private static async void AutoScanService_BulkChangeDetected()
	{
		App.MainWindow.DispatcherQueue.TryEnqueue(async () =>
		{
			ContentDialog dialog = new ContentDialog()
			{
				Title = "Auto Sync",
				RequestedTheme = App.Current.ThemeService.ActualTheme,
				CloseButtonText = "Later",
				PrimaryButtonText = "Open Settings",
				Content = new TextBlock
				{
					Text = "A large number of file changes were detected. Please, do a full scan to sync libraries, otherwise the libraries will sync on app restart.",
					TextWrapping = TextWrapping.WrapWholeWords,
					Margin = new Thickness(10)
				},
				DefaultButton = ContentDialogButton.Primary,
				XamlRoot = App.MainWindow.Content.XamlRoot
			};

			MainWindow._instance.WindowResizePermission(false);
			var result = await dialog.ShowAsync();
			MainWindow._instance.WindowResizePermission(true);

			if (result == ContentDialogResult.Primary)
				App.Current.NavService.NavigateTo(typeof(SettingsPage));
		});
	}
}
