namespace Tunetastic.Common;

/// <summary>
/// Provides utility methods for displaying global notifications to the user.
/// </summary>
public static class GlobalNotification
{
    /// <summary>
    /// Displays an informational notification to the user with the specified message.
    /// </summary>
    /// <param name="message">The message content to display in the notification.</param>
    public static void Info(string message)
    {
        Growl.InfoGlobal(new GrowlInfo
        {
            ShowDateTime = false,
            StaysOpen = false,
            IsClosable = true,
            Title = "Tunetastic",
            Message = message
        });
    }

    /// <summary>
    /// Displays an error notification to the user with the specified message.
    /// </summary>
    /// <param name="message">The error message content to display in the notification.</param>
    public static void Error(string message)
    {
        Growl.ErrorGlobal(new GrowlInfo
        {
            ShowDateTime = false,
            StaysOpen = true,
            IsClosable = true,
            Title = "Tunetastic",
            Message = message
        });
    }

    /// <summary>
    /// Displays a warning notification to the user with the specified message.
    /// </summary>
    /// <param name="message">The message content to display in the warning notification.</param>
    public static void Warning(string message)
    {
        Growl.WarningGlobal(new GrowlInfo
        {
            ShowDateTime = false,
            StaysOpen = false,
            IsClosable = true,
            Title = "Tunetastic",
            Message = message
        });
    }
}
