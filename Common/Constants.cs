namespace Tunetastic.Common;

/// <summary>
/// Provides application-wide constants such as directory paths and commonly used file paths.
/// </summary>
/// <remarks>
/// This class contains pre-defined file paths and directory locations used across the application.
/// The paths are generated dynamically at runtime based on application-specific configurations and environment settings.
/// </remarks>
public static partial class Constants
{
	/// <summary>
	/// Represents the root directory path where the application's data and configuration files are stored.
	/// </summary>
	/// <remarks>
	/// This directory serves as the base location for various application resources, such as configuration files,
	/// cached data, and other necessary files. It is dynamically determined at runtime based on the application-specific
	/// data folder and product name.
	/// </remarks>
	public static readonly string RootDirectoryPath = Path.Combine(PathHelper.GetAppDataFolderPath(), ProcessInfoHelper.ProductName);

	/// <summary>
	/// Represents the file path to the application's main configuration file.
	/// </summary>
	/// <remarks>
	/// This path points to the primary configuration file (AppConfig.json) used for storing application settings.
	/// It is located within the application's root directory and is constructed dynamically at runtime based on
	/// the application-specific root directory path.
	/// </remarks>
	public static readonly string AppConfigPath = Path.Combine(RootDirectoryPath, "AppConfig.json");

	/// <summary>
	/// Represents the directory path where all thumbnail images are stored.
	/// </summary>
	/// <remarks>
	/// This folder is used to organize and store generated thumbnail images for application resources,
	/// such as album covers or other visual elements. The folder path is created as a subdirectory
	/// under the root directory defined by <see cref="Constants.RootDirectoryPath"/>.
	/// </remarks>
	public static readonly string ThumbnailsFolder = Path.Combine(RootDirectoryPath, "Thumbnails");

	/// <summary>
	/// Represents the directory path where temporary files are stored.
	/// </summary>
	public static readonly string TemporaryFolder = Path.Combine(RootDirectoryPath, "Temporary");
}
