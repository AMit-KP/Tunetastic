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
			GlobalNotification.Error("Do a Full Scan atleast once with a folder that contains music.");
			return false;
		}

		await AutoScanReconciler.RunCatchUpDiff();

		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.AutoScanEnabled)] = true;

		await LibraryWatcherService.StartWatching();

		BulkChangeDetected -= AutoScanService_BulkChangeDetected;
		BulkChangeDetected += AutoScanService_BulkChangeDetected;

		return true;
	}

	public static async Task DisableAutoScan()
	{
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.AutoScanEnabled)] = false;

		await LibraryWatcherService.StopWatching(drainPending: true);

		AutoScanService.BulkChangeDetected -= AutoScanService_BulkChangeDetected;
	}

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

		await AutoScanReconciler.RunCatchUpDiff();
		await LibraryWatcherService.StartWatching();
	}

	private static async void AutoScanService_BulkChangeDetected()
	{
		App.MainWindow.DispatcherQueue.TryEnqueue(async () =>
		{
			ContentDialog dialog = new ContentDialog()
			{
				Title = "Auto Sync",
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

			if(result == ContentDialogResult.Primary)
				App.Current.NavService.NavigateTo(typeof(SettingsPage));
		});
	}
}
