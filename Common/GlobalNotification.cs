namespace Tunetastic.Common;
public static class GlobalNotification
{
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
