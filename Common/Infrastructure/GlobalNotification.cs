using System.Diagnostics;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Tunetastic.Common.Infrastructure;

/// <summary>
/// Provides utility methods for displaying global notifications to the user.
/// This class offers static methods to show different types of notifications
/// such as information, error, and warning messages to the user in a global context.
/// </summary>
public static class GlobalNotification
{
	private static IconSource GetImageIconSource()
	{
		return new ImageIconSource
		{
			ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/AppIcon.png"))
		};
	}

	/// <summary>
	/// Displays an informational notification to the user with the specified message.
	/// The notification will be shown with a blue color and will automatically close.
	/// </summary>
	/// <param name="message">The message content to display in the notification.</param>
	public static void Info(string message)
	{
		Show(() => Growl.InfoGlobal(new GrowlInfo
		{
			ShowDateTime = false,
			IsIconVisible = true,
			IconSource = GetImageIconSource(),
			ShowCloseButton = true,
			UseBlueColorForInfo = true,
			StaysOpen = false,
			WaitTime = TimeSpan.FromSeconds(10),
			IsClosable = true,
			Title = $"{ProcessInfoHelper.ProductName} ({ProcessInfoHelper.VersionWithPrefix})",
			Message = message,
		}));
	}

	/// <summary>
	/// Displays a success notification to the user with the specified message.
	/// The notification will be shown with a green color and will automatically close.
	/// </summary>
	/// <param name="message">The message content to display in the notification.</param>
	public static void Success(string message)
	{
		Show(() => Growl.SuccessGlobal(new GrowlInfo
		{
			ShowDateTime = false,
			IsIconVisible = true,
			IconSource = GetImageIconSource(),
			ShowCloseButton = true,
			UseBlueColorForInfo = true,
			StaysOpen = false,
			WaitTime = TimeSpan.FromSeconds(10),
			IsClosable = true,
			Title = $"{ProcessInfoHelper.ProductName} ({ProcessInfoHelper.VersionWithPrefix})",
			Message = message,
		}));
	}

	/// <summary>
	/// Displays an error notification to the user with the specified message.
	/// The notification will be shown with a red color and will automatically close.
	/// </summary>
	/// <param name="message">The error message content to display in the notification.</param>
	public static void Error(string message)
	{
		Show(() => Growl.ErrorGlobal(new GrowlInfo
		{
			ShowDateTime = false,
			IsIconVisible = true,
			IconSource = GetImageIconSource(),
			ShowCloseButton = true,
			StaysOpen = false,
			IsClosable = true,
			WaitTime = TimeSpan.FromSeconds(10),
			Title = "Tunetastic",
			Message = message
		}));
	}

	/// <summary>
	/// Displays a warning notification to the user with the specified message.
	/// The notification will be shown with a yellow color and will automatically close.
	/// </summary>
	/// <param name="message">The message content to display in the warning notification.</param>
	public static void Warning(string message)
	{
		Show(() => Growl.WarningGlobal(new GrowlInfo
		{
			ShowDateTime = false,
			IsIconVisible = true,
			IconSource = GetImageIconSource(),
			ShowCloseButton = true,
			StaysOpen = false,
			WaitTime = TimeSpan.FromSeconds(10),
			IsClosable = true,
			Title = "Tunetastic",
			Message = message
		}));
	}

	/// <summary>
	/// Dispatches the notification creation and display onto the UI thread safely.
	/// </summary>
	/// <param name="growlAction">The delegate creating and presenting the Growl notification.</param>
	private static void Show(Action growlAction)
	{
		RunOnUIThread(async () =>
		{
			try
			{
				growlAction();
				await Task.Delay(20);
				MainWindow._instance?.BringToFront();
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"[GlobalNotification] Failed to show notification: {ex}");
			}
		});
	}

	/// <summary>
	/// Runs the given action on the UI thread using the main window's DispatcherQueue.
	/// If no dispatcher is available (e.g. during early startup or late teardown),
	/// the action is safely dropped to avoid cross-thread COM exceptions.
	/// </summary>
	/// <param name="action">The action to run.</param>
	private static void RunOnUIThread(Action action)
	{
		var dispatcherQueue = App.MainWindow?.DispatcherQueue;
		if (dispatcherQueue == null)
		{
			Debug.WriteLine("[GlobalNotification] No UI DispatcherQueue available; notification dropped.");
			return;
		}

		if (dispatcherQueue.HasThreadAccess)
		{
			action();
		}
		else
		{
			dispatcherQueue.TryEnqueue(() => action());
		}
	}
}

