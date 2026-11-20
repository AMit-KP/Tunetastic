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
	/// Manages an atomic, transactional backup session for thumbnail images during library rescans.
	/// Moves the existing thumbnails folder to a temporary backup in ~1-4ms so that the rescan can
	/// immediately create new thumbnails without delay. If the rescan or database update succeeds,
	/// the old backup is purged in the background. If it fails, the previous thumbnails are restored instantly.
	/// </summary>
	public sealed class ThumbnailBackupSession : IDisposable
	{
		private readonly string? _backupPath;
		private readonly string _thumbnailsPath;
		private bool _committed;

		internal ThumbnailBackupSession(string? backupPath, string thumbnailsPath)
		{
			_backupPath = backupPath;
			_thumbnailsPath = thumbnailsPath;
		}

		/// <summary>
		/// Commits the session upon successful scan and database update. Deletes the previous backup
		/// folder asynchronously in the background.
		/// </summary>
		public void Commit()
		{
			if (_committed) return;
			_committed = true;

			if (_backupPath != null && Directory.Exists(_backupPath))
			{
				var folderToDelete = _backupPath;
				_ = Task.Run(() =>
				{
					DeleteDirectoryParallel(folderToDelete, deleteRoot: true);
					CleanOrphanedThumbnailFolders();
				});
			}
		}

		/// <summary>
		/// Rolls back the session upon a failed scan or database error. Restores the previous thumbnails
		/// folder instantly (~1-4ms), ensuring database rows never lose their corresponding cover art files.
		/// </summary>
		public void Rollback()
		{
			if (_committed) return;
			_committed = true;

			if (_backupPath == null || !Directory.Exists(_backupPath))
				return;

			try
			{
				if (Directory.Exists(_thumbnailsPath))
				{
					var failedTrash = Path.Combine(Constants.TemporaryFolder, "Thumbnails_failed_" + Guid.NewGuid().ToString("N"));
					try
					{
						Directory.Move(_thumbnailsPath, failedTrash);
						_ = Task.Run(() => DeleteDirectoryParallel(failedTrash, deleteRoot: true));
					}
					catch
					{
						_ = Task.Run(() => DeleteDirectoryParallel(_thumbnailsPath, deleteRoot: false));
					}
				}

				if (!Directory.Exists(_thumbnailsPath))
				{
					Directory.Move(_backupPath, _thumbnailsPath);
				}
			}
			catch
			{
				// Best-effort restoration
			}
		}

		/// <summary>
		/// Automatically rolls back if the session finishes without being committed.
		/// </summary>
		public void Dispose()
		{
			if (!_committed)
			{
				Rollback();
			}
		}
	}

	/// <summary>
	/// Initiates a transactional thumbnail backup session. Instantly renames the existing
	/// <see cref="Constants.ThumbnailsFolder"/> to a temporary backup on the same volume (~1-4ms),
	/// allowing new thumbnails to be written cleanly while protecting existing covers against database update failures.
	/// </summary>
	public static ThumbnailBackupSession CreateThumbnailBackupSession()
	{
		var thumbnailsPath = Constants.ThumbnailsFolder;
		if (!Directory.Exists(thumbnailsPath))
			return new ThumbnailBackupSession(null, thumbnailsPath);

		var backupPath = Path.Combine(Constants.TemporaryFolder, "Thumbnails_backup_" + Guid.NewGuid().ToString("N"));

		try
		{
			var tempRoot = Path.GetDirectoryName(backupPath);
			if (tempRoot != null && !Directory.Exists(tempRoot))
				Directory.CreateDirectory(tempRoot);

			Directory.Move(thumbnailsPath, backupPath);
			return new ThumbnailBackupSession(backupPath, thumbnailsPath);
		}
		catch
		{
			// If moving fails (e.g. active folder lock), proceed without backup
			return new ThumbnailBackupSession(null, thumbnailsPath);
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
	internal static void CleanOrphanedThumbnailFolders()
	{
		try
		{
			if (!Directory.Exists(Constants.TemporaryFolder))
				return;

			// If the main thumbnails folder is missing, attempt restoring the most recent orphaned backup
			if (!Directory.Exists(Constants.ThumbnailsFolder))
			{
				var backupFolder = Directory.EnumerateDirectories(Constants.TemporaryFolder, "Thumbnails_backup_*").FirstOrDefault();
				if (backupFolder != null)
				{
					try
					{
						Directory.Move(backupFolder, Constants.ThumbnailsFolder);
					}
					catch
					{
						// Best effort
					}
				}
			}

			var patterns = new[] { "Thumbnails_old_*", "Thumbnails_backup_*", "Thumbnails_failed_*" };
			foreach (var pattern in patterns)
			{
				foreach (var folder in Directory.EnumerateDirectories(Constants.TemporaryFolder, pattern))
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
		}
		catch
		{
			// Best effort
		}
	}

}

