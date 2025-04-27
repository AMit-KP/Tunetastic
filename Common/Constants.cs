namespace Tunetastic.Common;

public static partial class Constants
{
    public static readonly string RootDirectoryPath = Path.Combine(PathHelper.GetAppDataFolderPath(), ProcessInfoHelper.ProductName);
    public static readonly string AppConfigPath = Path.Combine(RootDirectoryPath, "AppConfig.json");
    public static readonly string LibrariesSettingsDataPath = Path.Combine(RootDirectoryPath, "LibrariesData.json");
    public static readonly string ThumbnailsFolder = Path.Combine(RootDirectoryPath, "Thumbnails");
}
