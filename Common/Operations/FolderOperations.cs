using System.IO;
using Tunetastic.Common;
using Tunetastic.Common.Services;

namespace Tunetastic.Common.Operations;

/// <summary>
/// Provides operations for discovering, grouping, and navigating music folders across libraries.
/// Supports both flat directory listings and explorer-style hierarchical folder browsing.
/// </summary>
public static class FolderOperations
{
	/// <summary>
	/// Normalizes a directory path by trimming leading/trailing whitespace, removing trailing slashes,
	/// and standardizing directory separators.
	/// </summary>
	/// <param name="path">The directory path to normalize.</param>
	/// <returns>A normalized directory path string.</returns>
	public static string NormalizeFolderPath(string path)
	{
		if (string.IsNullOrWhiteSpace(path)) return string.Empty;
		var trimmed = path.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		return trimmed.Replace('/', '\\');
	}

	/// <summary>
	/// Determines whether any ancestor directory of <paramref name="dir"/> is present in the provided set of music folders.
	/// </summary>
	/// <param name="dir">The normalized directory path to check.</param>
	/// <param name="musicDirs">The set of all normalized directories that contain music.</param>
	/// <returns><c>true</c> if an ancestor directory contains music files; otherwise, <c>false</c>.</returns>
	public static bool HasAncestorFolder(string dir, HashSet<string> musicDirs)
	{
		var parent = Path.GetDirectoryName(dir);
		while (!string.IsNullOrEmpty(parent))
		{
			var normalizedParent = NormalizeFolderPath(parent);
			if (musicDirs.Contains(normalizedParent))
				return true;
			parent = Path.GetDirectoryName(parent);
		}
		return false;
	}

	/// <summary>
	/// Mode 1 (All Music Folders): Retrieves all folders directly containing music files, with no hierarchy checking.
	/// </summary>
	/// <param name="ascending">Specifies whether folders should be sorted in ascending or descending alphabetical order.</param>
	/// <returns>A list of <see cref="FolderModel"/> objects representing all folders directly containing music files.</returns>
	public static async Task<List<FolderModel>> GetFoldersFlatAsync(bool ascending = true)
	{
		try
		{
			var songs = await DatabaseHelper.Instance.GetSongPathsWithMetadataAsync();
			var groups = songs
				.Where(s => !string.IsNullOrEmpty(s.Path))
				.GroupBy(s => NormalizeFolderPath(Path.GetDirectoryName(s.Path) ?? string.Empty), StringComparer.OrdinalIgnoreCase)
				.Where(g => !string.IsNullOrEmpty(g.Key));

			var folders = new List<FolderModel>();
			foreach (var g in groups)
			{
				var dirPath = g.Key;
				var dirName = Path.GetFileName(dirPath);
				if (string.IsNullOrEmpty(dirName)) dirName = dirPath;

				var cover = g.FirstOrDefault(s => !string.IsNullOrEmpty(s.Cover) && !s.Cover.EndsWith(Constants.DefaultCoverFileName, StringComparison.OrdinalIgnoreCase))?.Cover
							?? g.FirstOrDefault(s => !string.IsNullOrEmpty(s.Cover))?.Cover ?? string.Empty;

				folders.Add(new FolderModel
				{
					Name = dirName,
					Path = dirPath,
					Count = g.Count(),
					TotalSongCount = g.Count(),
					SubFolderCount = 0,
					TotalDuration = g.Sum(s => s.Duration),
					Cover = cover
				});
			}

			return ascending
				? folders.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase).ToList()
				: folders.OrderByDescending(f => f.Name, StringComparer.OrdinalIgnoreCase).ToList();
		}
		catch (Exception)
		{
			return new List<FolderModel>();
		}
	}

	/// <summary>
	/// Mode 2 (Folder Hierarchy): Retrieves root folders that contain music files (where no ancestor contains music files).
	/// Empty intermediate container folders that contain no direct or descendant music files are excluded.
	/// </summary>
	/// <param name="ascending">Specifies whether folders should be sorted in ascending or descending alphabetical order.</param>
	/// <returns>A list of top-level <see cref="FolderModel"/> root folders.</returns>
	public static async Task<List<FolderModel>> GetRootFoldersHierarchicalAsync(bool ascending = true)
	{
		try
		{
			var songs = await DatabaseHelper.Instance.GetSongPathsWithMetadataAsync();
			var directGroups = songs
				.Where(s => !string.IsNullOrEmpty(s.Path))
				.GroupBy(s => NormalizeFolderPath(Path.GetDirectoryName(s.Path) ?? string.Empty), StringComparer.OrdinalIgnoreCase)
				.Where(g => !string.IsNullOrEmpty(g.Key))
				.ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

			var musicDirs = directGroups.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
			var rootDirs = musicDirs.Where(d => !HasAncestorFolder(d, musicDirs)).ToList();

			var folders = new List<FolderModel>();
			foreach (var rootDir in rootDirs)
			{
				var songsInTree = songs.Where(s =>
				{
					var sDir = NormalizeFolderPath(Path.GetDirectoryName(s.Path) ?? string.Empty);
					return LibraryScanner.IsSameOrNestedPath(sDir, rootDir);
				}).ToList();

				var immediateSubfolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (var md in musicDirs)
				{
					if (!md.Equals(rootDir, StringComparison.OrdinalIgnoreCase) && LibraryScanner.IsSameOrNestedPath(md, rootDir))
					{
						var relative = md.Substring(rootDir.Length).TrimStart('\\', '/');
						var firstSegment = relative.Split('\\', '/')[0];
						if (!string.IsNullOrEmpty(firstSegment))
							immediateSubfolders.Add(firstSegment);
					}
				}

				var dirName = Path.GetFileName(rootDir);
				if (string.IsNullOrEmpty(dirName)) dirName = rootDir;

				var cover = songsInTree.FirstOrDefault(s => !string.IsNullOrEmpty(s.Cover) && !s.Cover.EndsWith(Constants.DefaultCoverFileName, StringComparison.OrdinalIgnoreCase))?.Cover
							?? songsInTree.FirstOrDefault(s => !string.IsNullOrEmpty(s.Cover))?.Cover ?? string.Empty;

				folders.Add(new FolderModel
				{
					Name = dirName,
					Path = rootDir,
					Count = directGroups.TryGetValue(rootDir, out var dg) ? dg.Count : 0,
					TotalSongCount = songsInTree.Count,
					SubFolderCount = immediateSubfolders.Count,
					TotalDuration = songsInTree.Sum(s => s.Duration),
					Cover = cover
				});
			}

			return ascending
				? folders.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase).ToList()
				: folders.OrderByDescending(f => f.Name, StringComparer.OrdinalIgnoreCase).ToList();
		}
		catch (Exception)
		{
			return new List<FolderModel>();
		}
	}

	/// <summary>
	/// In Mode 2 (Folder Hierarchy): Retrieves immediate child subfolders under <paramref name="parentFolderPath"/> that lead to music tracks.
	/// </summary>
	/// <param name="parentFolderPath">The parent directory path whose subfolders should be retrieved.</param>
	/// <param name="ascending">Specifies whether folders should be sorted in ascending or descending alphabetical order.</param>
	/// <returns>A list of <see cref="FolderModel"/> objects representing immediate child subfolders.</returns>
	public static async Task<List<FolderModel>> GetSubFoldersAsync(string parentFolderPath, bool ascending = true)
	{
		try
		{
			var normalizedParent = NormalizeFolderPath(parentFolderPath);

			var songs = await DatabaseHelper.Instance.GetSongPathsWithMetadataAsync();
			var directGroups = songs
				.Where(s => !string.IsNullOrEmpty(s.Path))
				.GroupBy(s => NormalizeFolderPath(Path.GetDirectoryName(s.Path) ?? string.Empty), StringComparer.OrdinalIgnoreCase)
				.Where(g => !string.IsNullOrEmpty(g.Key))
				.ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

			var musicDirs = directGroups.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

			var subfolderSegments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (var md in musicDirs)
			{
				if (!md.Equals(normalizedParent, StringComparison.OrdinalIgnoreCase) && LibraryScanner.IsSameOrNestedPath(md, normalizedParent))
				{
					var relative = md.Substring(normalizedParent.Length).TrimStart('\\', '/');
					var firstSegment = relative.Split('\\', '/')[0];
					if (!string.IsNullOrEmpty(firstSegment) && !subfolderSegments.ContainsKey(firstSegment))
					{
						subfolderSegments[firstSegment] = Path.Combine(normalizedParent, firstSegment);
					}
				}
			}

			var subfolders = new List<FolderModel>();
			foreach (var kvp in subfolderSegments)
			{
				var subName = kvp.Key;
				var subPath = kvp.Value;

				var songsInSubTree = songs.Where(s =>
				{
					var sDir = NormalizeFolderPath(Path.GetDirectoryName(s.Path) ?? string.Empty);
					return LibraryScanner.IsSameOrNestedPath(sDir, subPath);
				}).ToList();

				var nestedSubfolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (var md in musicDirs)
				{
					if (!md.Equals(subPath, StringComparison.OrdinalIgnoreCase) && LibraryScanner.IsSameOrNestedPath(md, subPath))
					{
						var relative = md.Substring(subPath.Length).TrimStart('\\', '/');
						var first = relative.Split('\\', '/')[0];
						if (!string.IsNullOrEmpty(first))
							nestedSubfolders.Add(first);
					}
				}

				var cover = songsInSubTree.FirstOrDefault(s => !string.IsNullOrEmpty(s.Cover) && !s.Cover.EndsWith(Constants.DefaultCoverFileName, StringComparison.OrdinalIgnoreCase))?.Cover
							?? songsInSubTree.FirstOrDefault(s => !string.IsNullOrEmpty(s.Cover))?.Cover ?? string.Empty;

				subfolders.Add(new FolderModel
				{
					Name = subName,
					Path = subPath,
					Count = directGroups.TryGetValue(subPath, out var dg) ? dg.Count : 0,
					TotalSongCount = songsInSubTree.Count,
					SubFolderCount = nestedSubfolders.Count,
					TotalDuration = songsInSubTree.Sum(s => s.Duration),
					Cover = cover
				});
			}

			return ascending
				? subfolders.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase).ToList()
				: subfolders.OrderByDescending(f => f.Name, StringComparer.OrdinalIgnoreCase).ToList();
		}
		catch (Exception)
		{
			return new List<FolderModel>();
		}
	}

	/// <summary>
	/// Retrieves songs located in a specific folder (optionally including all subfolders).
	/// </summary>
	/// <param name="folderPath">The directory path to query songs for.</param>
	/// <param name="includeSubfolders"><c>true</c> to include songs from subdirectories; <c>false</c> for direct contents only.</param>
	/// <param name="orderBy">The song property to order by.</param>
	/// <param name="ascending">Specifies whether sorting is ascending or descending.</param>
	/// <returns>A list of <see cref="Song"/> objects matching the folder criteria.</returns>
	public static async Task<List<Song>> GetSongsInFolderAsync(string folderPath, bool includeSubfolders = false, SongProperty orderBy = SongProperty.Title, bool ascending = true)
	{
		try
		{
			var normalized = NormalizeFolderPath(folderPath);

			var songs = await DatabaseHelper.Instance.LoadSongsFromDB(orderBy: orderBy, ascending: ascending);
			if (includeSubfolders)
			{
				return songs.Where(s =>
				{
					var dir = NormalizeFolderPath(Path.GetDirectoryName(s.Path) ?? string.Empty);
					return LibraryScanner.IsSameOrNestedPath(dir, normalized);
				}).ToList();
			}
			else
			{
				return songs.Where(s =>
				{
					var dir = NormalizeFolderPath(Path.GetDirectoryName(s.Path) ?? string.Empty);
					return dir.Equals(normalized, StringComparison.OrdinalIgnoreCase);
				}).ToList();
			}
		}
		catch (Exception)
		{
			return new List<Song>();
		}
	}
}

