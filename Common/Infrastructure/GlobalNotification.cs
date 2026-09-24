namespace Tunetastic.Common.Infrastructure;

/// <summary>
/// Provides utility methods for displaying global notifications to the user.
/// This class offers static methods to show different types of notifications
/// such as information, error, and warning messages to the user in a global context.
/// </summary>
public static class GlobalNotification
{
	/// <summary>
	/// Displays an informational notification to the user with the specified message.
	/// The notification will be shown with a blue color and will automatically close.
	/// </summary>
	/// <param name="message">The message content to display in the notification.</param>
	public static void Info(string message)
	{
		RunOnUIThread(async () =>
		{
			Growl.InfoGlobal(new GrowlInfo
			{
				ShowDateTime = false,
				UseBlueColorForInfo = true,
				StaysOpen = false,
				IsClosable = true,
				Title = "Tunetastic",
				Message = message
			});

			await Task.Delay(20);

			MainWindow._instance.BringToFront();
		});
	}

	/// <summary>
	/// Displays an error notification to the user with the specified message.
	/// The notification will be shown with an error color and will automatically close.
	/// </summary>
	/// <param name="message">The error message content to display in the notification.</param>
	public static void Error(string message)
	{
		RunOnUIThread(async () =>
		{
			Growl.ErrorGlobal(new GrowlInfo
			{
				ShowDateTime = false,
				StaysOpen = false,
				IsClosable = true,
				Title = "Tunetastic",
				Message = message
			});

			await Task.Delay(20);

			MainWindow._instance.BringToFront();
		});
	}

	/// <summary>
	/// Displays a warning notification to the user with the specified message.
	/// The notification will be shown with a warning color and will automatically close.
	/// </summary>
	/// <param name="message">The message content to display in the warning notification.</param>
	public static void Warning(string message)
	{
		RunOnUIThread(async () =>
		{
			Growl.WarningGlobal(new GrowlInfo
			{
				ShowDateTime = false,
				StaysOpen = false,
				IsClosable = true,
				Title = "Tunetastic",
				Message = message
			});

			await Task.Delay(20);

			MainWindow._instance.BringToFront();
		});
	}

	/// <summary>
	/// Runs the given action on the UI thread. Growl notifications create WinUI controls, which
	/// can only be created on the UI thread; callers here include background work (e.g. the
	/// library scanner), so this can't assume it's already there.
	/// </summary>
	/// <param name="action">The action to run.</param>
	private static void RunOnUIThread(Action action)
	{
		var dispatcherQueue = App.MainWindow?.DispatcherQueue;
		if (dispatcherQueue == null || dispatcherQueue.HasThreadAccess)
			action();
		else
			dispatcherQueue.TryEnqueue(() => action());
	}
}
