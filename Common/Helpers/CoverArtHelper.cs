using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using TagLib;
using File = System.IO.File;
using Image = SixLabors.ImageSharp.Image;

namespace Tunetastic.Common.Helpers;

/// <summary>
/// Provides utilities for managing, resizing, and caching cover art thumbnails.
/// </summary>
public static class CoverArtHelper
{
	private static readonly object _defaultCoverLock = new();

	/// <summary>
	/// Returns the standardized file path for the default cover thumbnail within the specified folder.
	/// </summary>
	public static string GetDefaultCoverPath(ThumbnailFolder thumbnailFolder)
		=> Path.Combine(Constants.ThumbnailsFolder, thumbnailFolder.ToString(), Constants.DefaultCoverFileName);

	/// <summary>
	/// Determines whether the specified cover path points to the default application cover art.
	/// </summary>
	public static bool IsDefaultCover(string? coverPath)
		=> !string.IsNullOrEmpty(coverPath) && string.Equals(Path.GetFileName(coverPath), Constants.DefaultCoverFileName, StringComparison.OrdinalIgnoreCase);

	/// <summary>
	/// Determines whether the specified cover path points to a genuine embedded or custom cover (i.e. non-empty and not default).
	/// </summary>
	public static bool HasRealCover(string? coverPath)
		=> !string.IsNullOrEmpty(coverPath) && !IsDefaultCover(coverPath);

	/// <summary>
	/// Returns the file path of the default cover thumbnail for the given folder, creating it lazily from the application icon if it does not already exist.
	/// </summary>
	/// <param name="thumbnailFolder">The thumbnail folder where the default cover is stored.</param>
	/// <param name="size">Optional square dimension to resize the image to. When omitted, the original application icon dimensions are preserved.</param>
	/// <returns>The file path of the default cover image.</returns>
	public static string GetOrCreateDefaultCover(ThumbnailFolder thumbnailFolder, int? size = null)
	{
		var targetPath = GetDefaultCoverPath(thumbnailFolder);
		if (File.Exists(targetPath))
			return targetPath;

		lock (_defaultCoverLock)
		{
			if (File.Exists(targetPath))
				return targetPath;

			var dir = Path.GetDirectoryName(targetPath);
			if (dir != null) Directory.CreateDirectory(dir);

			var appIconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.png");
			if (!File.Exists(appIconPath))
				return targetPath;

			var imageData = File.ReadAllBytes(appIconPath);
			using var image = Image.Load(imageData);

			if (size.HasValue)
			{
				image.Mutate(x => x.Resize(new ResizeOptions
				{
					Size = new Size(size.Value, size.Value),
					Mode = ResizeMode.Max
				}));
			}

			image.Save(targetPath, new PngEncoder());
			return targetPath;
		}
	}

	/// <summary>
	/// Safely deletes custom thumbnail files from disk when cover art is replaced or removed.
	/// Never deletes the shared default cover art.
	/// </summary>
	/// <param name="coverPath">The cover path to delete.</param>
	public static void DeleteThumbnailFiles(string? coverPath)
	{
		if (string.IsNullOrEmpty(coverPath) || IsDefaultCover(coverPath))
			return;

		try
		{
			var fileName = Path.GetFileName(coverPath);
			var allSongPath = Path.Combine(Constants.ThumbnailsFolder, ThumbnailFolder.AllSongView.ToString(), fileName);
			var mainPlayerPath = Path.Combine(Constants.ThumbnailsFolder, ThumbnailFolder.MainPlayer.ToString(), fileName);

			if (File.Exists(allSongPath)) File.Delete(allSongPath);
			if (File.Exists(mainPlayerPath)) File.Delete(mainPlayerPath);
		}
		catch
		{
			// Best effort; ignore locked or non-existent files
		}
	}

	/// <summary>
	/// Creates a thumbnail image from the provided pictures and saves it as a PNG file in the specified thumbnail folder.
	/// If no pictures are provided or loading fails, returns the reusable default cover thumbnail.
	/// </summary>
	/// <param name="thumbnailFolder">The folder where the thumbnail image will be stored. This determines the subfolder under the Thumbnails directory.</param>
	/// <param name="pictures">An array of IPicture objects representing the image(s) to be resized. If no valid images are provided, the default cover is used.</param>
	/// <param name="width">The desired width of the thumbnail image in pixels.</param>
	/// <param name="height">The desired height of the thumbnail image in pixels.</param>
	/// <returns>The file path of the created thumbnail image.</returns>
	public static string CreateThumbnailImage(ThumbnailFolder thumbnailFolder, IPicture[]? pictures, int width, int height)
	{
		if (pictures == null || pictures.Length == 0)
			return GetOrCreateDefaultCover(thumbnailFolder, width);

		var thumbnailFilePath = Path.Combine(Constants.ThumbnailsFolder, thumbnailFolder.ToString(), "Cover_" + new string(Enumerable.Range(0, 10).Select(_ => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"[new Random().Next(62)]).ToArray()) + ".png");
		var dir = Path.GetDirectoryName(thumbnailFilePath);
		if (dir != null) Directory.CreateDirectory(dir);

		try
		{
			using var image = Image.Load(pictures[0].Data.Data);
			image.Mutate(x => x.Resize(new ResizeOptions
			{
				Size = new Size(width, height),
				Mode = ResizeMode.Max
			}));

			image.Save(thumbnailFilePath, new PngEncoder());
			return thumbnailFilePath;
		}
		catch (Exception)
		{
			return GetOrCreateDefaultCover(thumbnailFolder, width);
		}
	}

	/// <summary>
	/// Creates a thumbnail image from the provided pictures and saves it as a PNG file in the specified thumbnail folder.
	/// If no pictures are provided or loading fails, returns the reusable default cover thumbnail.
	/// </summary>
	/// <param name="thumbnailFolder">The folder where the thumbnail image will be stored. This defines the subfolder under the main Thumbnails directory.</param>
	/// <param name="pictures">An array of IPicture objects containing the image data to be processed. If no pictures are provided, the default cover is used.</param>
	/// <param name="fileName">An optional string specifying the name of the thumbnail file. If not provided, a randomly generated name will be used.</param>
	/// <returns>The file path of the created thumbnail image.</returns>
	public static string CreateThumbnailImage(ThumbnailFolder thumbnailFolder, IPicture[]? pictures, string? fileName = null)
	{
		if (pictures == null || pictures.Length == 0)
			return GetOrCreateDefaultCover(thumbnailFolder);

		var thumbnailFilePath = Path.Combine(Constants.ThumbnailsFolder, thumbnailFolder.ToString(), fileName ?? "Cover_" + new string(Enumerable.Range(0, 10).Select(_ => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"[new Random().Next(62)]).ToArray()) + ".png");
		var dir = Path.GetDirectoryName(thumbnailFilePath);
		if (dir != null) Directory.CreateDirectory(dir);

		try
		{
			using var image = Image.Load(pictures[0].Data.Data);
			image.Save(thumbnailFilePath, new PngEncoder());
			return thumbnailFilePath;
		}
		catch (Exception)
		{
			return GetOrCreateDefaultCover(thumbnailFolder);
		}
	}

	/// <summary>
	/// Creates a thumbnail image from the provided pictures and saves it as a PNG file in the specified thumbnail folder.
	/// </summary>
	/// <param name="thumbnailFolder">The folder where the thumbnail image will be stored. This determines the subfolder under the Thumbnails directory.</param>
	/// <param name="pictures">An array of IPicture objects representing the image(s) to be resized. If no valid images are provided, the default cover will be used.</param>
	/// <param name="size">The desired dimension for both the width and height of the square thumbnail image in pixels.</param>
	/// <returns>The file path of the created thumbnail image.</returns>
	public static string CreateThumbnailImage(ThumbnailFolder thumbnailFolder, IPicture[]? pictures, int size)
	{
		return CreateThumbnailImage(thumbnailFolder, pictures, size, size);
	}

	/// <summary>
	/// Rapidly clears the thumbnail cache folder without blocking the calling thread.
	/// </summary>
	/// <remarks>
	/// Standard recursive <see cref="Directory.Delete(string, bool)"/> runs synchronously and single-threaded,
	/// taking seconds or minutes when thousands of thumbnail files exist. This method instead instantly moves the
	/// existing directory to a temporary folder on the same volume (an atomic filesystem rename taking ~1ms), freeing
	/// up <see cref="Constants.ThumbnailsFolder"/> immediately so library scanning can proceed without delay.
	/// The old thumbnails are then deleted in the background using multi-threaded parallel I/O.
	/// </remarks>
	public static void FastPurgeThumbnails()
	{
		var thumbnailsPath = Constants.ThumbnailsFolder;
		if (!Directory.Exists(thumbnailsPath))
			return;

		var tempTrashFolder = Path.Combine(Constants.TemporaryFolder, "Thumbnails_old_" + Guid.NewGuid().ToString("N"));
		bool moved = false;

		try
		{
			var tempRoot = Path.GetDirectoryName(tempTrashFolder);
			if (tempRoot != null && !Directory.Exists(tempRoot))
				Directory.CreateDirectory(tempRoot);

			Directory.Move(thumbnailsPath, tempTrashFolder);
			moved = true;
		}
		catch
		{
			// If Directory.Move fails (e.g. open handles or volume boundary), fallback to deleting in-place
		}

		if (moved)
		{
			_ = Task.Run(() =>
			{
				DeleteDirectoryParallel(tempTrashFolder, deleteRoot: true);
				CleanOrphanedThumbnailFolders();
			});
		}
		else
		{
			List<string> existingFiles;
			try
			{
				existingFiles = Directory.EnumerateFiles(thumbnailsPath, "*", SearchOption.AllDirectories).ToList();
			}
			catch
			{
				existingFiles = new List<string>();
			}

			_ = Task.Run(() =>
			{
				DeleteFilesParallel(existingFiles);
				CleanOrphanedThumbnailFolders();
			});
		}
	}

	/// <summary>
	/// Deletes a collection of files in parallel to maximize disk and filesystem queue saturation.
	/// </summary>
	private static void DeleteFilesParallel(IEnumerable<string> files)
	{
		try
		{
			var dop = Math.Clamp(Environment.ProcessorCount * 2, 4, 16);
			Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = dop }, file =>
			{
				try
				{
					File.SetAttributes(file, FileAttributes.Normal);
					File.Delete(file);
				}
				catch
				{
					// Best-effort: ignore individual locked files
				}
			});
		}
		catch
		{
			// Best effort
		}
	}

	/// <summary>
	/// Recursively deletes all files and subdirectories inside a directory using parallel file deletion.
	/// </summary>
	private static void DeleteDirectoryParallel(string directoryPath, bool deleteRoot)
	{
		if (!Directory.Exists(directoryPath))
			return;

		try
		{
			var files = Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories);
			DeleteFilesParallel(files);

			var subDirs = Directory.EnumerateDirectories(directoryPath, "*", SearchOption.AllDirectories)
								   .OrderByDescending(d => d.Length);
			foreach (var dir in subDirs)
			{
				try
				{
					Directory.Delete(dir, false);
				}
				catch
				{
					// Best effort
				}
			}

			if (deleteRoot)
			{
				try
				{
					Directory.Delete(directoryPath, true);
				}
				catch
				{
					// Best effort
				}
			}
		}
		catch
		{
			// Best effort
		}
	}

	/// <summary>
	/// Scans the temporary folder for any leftover "Thumbnails_old_*" directories from previous interrupted sessions and deletes them.
	/// </summary>
	private static void CleanOrphanedThumbnailFolders()
	{
		try
		{
			if (!Directory.Exists(Constants.TemporaryFolder))
				return;

			var oldFolders = Directory.EnumerateDirectories(Constants.TemporaryFolder, "Thumbnails_old_*");
			foreach (var folder in oldFolders)
			{
				try
				{
					DeleteDirectoryParallel(folder, deleteRoot: true);
				}
				catch
				{
					// Best effort
				}
			}
		}
		catch
		{
			// Best effort
		}
	}
}

