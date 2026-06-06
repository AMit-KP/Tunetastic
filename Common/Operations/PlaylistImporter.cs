using System.Text;
using System.Xml.Linq;

namespace Tunetastic.Common.Operations;

/// <summary>
/// Provides functionality for importing and exporting playlists in various formats.
/// </summary>
public static class ImportExportPlaylist
{
	/// <summary>
	/// Exports a playlist to a specified file format in the user's "Downloads" directory.
	/// </summary>
	/// <param name="playlistFileName">The desired name of the playlist file, without the extension.</param>
	/// <param name="format">The file format to export the playlist in (e.g., "m3u", "m3u8", "pls", "wpl", "zpl").</param>
	/// <param name="trackPaths">A collection of file paths representing the tracks in the playlist.</param>
	public static async Task Export(string playlistFileName, string format, List<string> trackPaths)
	{
		var downloadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
		var fullPath = GetUniqueFilePath(downloadPath, playlistFileName, format.ToLower());

		try
		{
			await Task.Run(() =>
			{
				switch (format.ToLower())
				{
					case "m3u":
						ExportM3U(fullPath, trackPaths);
						break;
					case "m3u8":
						ExportM3U8(fullPath, trackPaths);
						break;
					case "pls":
						ExportPLS(fullPath, trackPaths);
						break;
					case "wpl":
						ExportWPL(fullPath, trackPaths);
						break;
					case "zpl":
						ExportZPL(fullPath, trackPaths);
						break;
					default:
						throw new NotSupportedException($"Unsupported playlist format: {format}");
				}
			});
			if (!File.Exists(fullPath))
				throw new Exception("Failed to export playlist.");
		}
		catch (NotSupportedException)
		{
			GlobalNotification.Error("Unsupported playlist format.");
			return;
		}
		catch (Exception)
		{
			GlobalNotification.Error("Failed to export playlist.");
			return;
		}

		GlobalNotification.Info($"Playlist exported to: {fullPath}");
	}

	/// <summary>
	/// Generates a unique file path by appending an incremented index to the base file name if a file with the same name already exists.
	/// </summary>
	/// <param name="directory">The directory in which the file will be saved.</param>
	/// <param name="baseName">The base name of the file without the extension.</param>
	/// <param name="extension">The extension of the file (without a leading dot).</param>
	/// <returns>A unique file path that does not conflict with any existing file in the specified directory.</returns>
	private static string GetUniqueFilePath(string directory, string baseName, string extension)
	{
		string fullPath = Path.Combine(directory, $"{baseName}.{extension}");
		int count = 1;

		while (File.Exists(fullPath))
		{
			fullPath = Path.Combine(directory, $"{baseName}_{count}.{extension}");
			count++;
		}

		return fullPath;
	}

	/// <summary>
	/// Exports a playlist in M3U format to the specified file path.
	/// </summary>
	/// <param name="path">The file path where the M3U playlist will be saved.</param>
	/// <param name="tracks">The list of track file paths to include in the playlist.</param>
	private static void ExportM3U(string path, List<string> tracks)
	{
		File.WriteAllLines(path, new[] { "#EXTM3U" }.Concat(tracks));
	}

	/// <summary>
	/// Exports a playlist in the M3U8 format to the specified file path with the given track paths.
	/// </summary>
	/// <param name="path">The full file path where the M3U8 playlist will be saved.</param>
	/// <param name="tracks">A list of file paths representing the tracks to include in the playlist.</param>
	private static void ExportM3U8(string path, List<string> tracks)
	{
		File.WriteAllLines(path, new[] { "#EXTM3U" }.Concat(tracks), Encoding.UTF8);
	}

	/// <summary>
	/// Exports a playlist in PLS format to the specified file path with the provided track list.
	/// </summary>
	/// <param name="path">The file path where the playlist will be exported.</param>
	/// <param name="tracks">The list of track file paths to include in the playlist.</param>
	private static void ExportPLS(string path, List<string> tracks)
	{
		using var writer = new StreamWriter(path, false, Encoding.UTF8);
		writer.WriteLine("[playlist]");
		for (int i = 0; i < tracks.Count; i++)
		{
			writer.WriteLine($"File{i + 1}={tracks[i]}");
			writer.WriteLine($"Title{i + 1}=Track {i + 1}");
			writer.WriteLine($"Length{i + 1}=-1");
		}
		writer.WriteLine($"NumberOfEntries={tracks.Count}");
		writer.WriteLine("Version=2");
	}

	/// <summary>
	/// Exports a playlist to the WPL (Windows Media Player Playlist) format.
	/// </summary>
	/// <param name="path">The full file path where the WPL file will be saved.</param>
	/// <param name="tracks">A list of file paths representing the tracks to include in the playlist.</param>
	private static void ExportWPL(string path, List<string> tracks)
	{
		var doc = new XDocument(
			new XElement("smil",
				new XElement("head",
					new XElement("meta", new XAttribute("name", "Generator"), new XAttribute("content", "YourMusicPlayer")),
					new XElement("title", Path.GetFileNameWithoutExtension(path))
				),
				new XElement("body",
					new XElement("seq",
						tracks.Select(p => new XElement("media", new XAttribute("src", p)))
					)
				)
			)
		);
		doc.Save(path);
	}

	/// <summary>
	/// Exports a playlist in ZPL (Zune Playlist) format to the specified file path.
	/// </summary>
	/// <param name="path">The full file path where the playlist will be exported.</param>
	/// <param name="tracks">A list of track file paths to include in the playlist.</param>
	private static void ExportZPL(string path, List<string> tracks)
	{
		var doc = new XDocument(
			new XElement("smil",
				new XElement("head",
					new XElement("meta", new XAttribute("name", "Generator"), new XAttribute("content", "YourMusicPlayer")),
					new XElement("title", Path.GetFileNameWithoutExtension(path))
				),
				new XElement("body",
					new XElement("seq",
						tracks.Select(p => new XElement("media", new XAttribute("src", p)))
					)
				)
			)
		);
		doc.Save(path);
	}

	/// <summary>
	/// Imports a playlist from the specified file and filters its tracks to include only those
	/// available in the library.
	/// </summary>
	/// <param name="filePath">The full path to the playlist file to be imported.</param>
	/// <returns>
	/// A tuple containing the playlist name, the total number of tracks in the file,
	/// and a list of tracks that exist in the library.
	/// </returns>
	public static async Task<(string name, int totalTrackCount, List<string> trackInLibrary)> ImportData(string filePath)
	{
		try
		{
			if (!File.Exists(filePath))
				GlobalNotification.Error($"Playlist file {filePath} not found.");

			List<string> allTracks = new();
			await Task.Run(() =>
			{
				allTracks = GetTrackPaths(filePath);
			});

			if (allTracks.Count == 0)
				GlobalNotification.Error("No valid tracks found in the playlist.");

			var filteredTracks = await DatabaseHelper.Instance.FilterExistingSongs(allTracks);

			var name = Path.GetFileNameWithoutExtension(filePath);
			return (name, allTracks.Count, filteredTracks);
		}
		catch (Exception)
		{
			GlobalNotification.Error("Failed to import playlist.");
			return ("", 0, new List<string>());
		}
	}

	/// <summary>
	/// Retrieves a list of track paths from a playlist file based on its format.
	/// </summary>
	/// <param name="filePath">The file path of the playlist to parse.</param>
	/// <returns>A list of strings representing the paths of tracks contained in the playlist.</returns>
	private static List<string> GetTrackPaths(string filePath)
	{
		var ext = Path.GetExtension(filePath).ToLowerInvariant();

		return ext switch
		{
			".m3u" or ".m3u8" => ParseM3U(filePath),
			".pls" => ParsePLS(filePath),
			".wpl" => ParseWPL(filePath),
			".zpl" => ParseZPL(filePath),
			_ => new List<string>()
		};
	}

	/// <summary>
	/// Parses an M3U or M3U8 playlist file and extracts the list of track file paths.
	/// </summary>
	/// <param name="filePath">The full path to the M3U or M3U8 playlist file to be parsed.</param>
	/// <returns>A list of strings containing file paths specified in the playlist.</returns>
	private static List<string> ParseM3U(string filePath)
	{
		return File.ReadLines(filePath)
					.Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
					.Select(line => line.Trim())
					.ToList();
	}

	/// <summary>
	/// Parses a PLS file and extracts track file paths from it.
	/// </summary>
	/// <param name="filePath">The full file path of the PLS file to be parsed.</param>
	/// <returns>A list of file paths representing the tracks defined in the PLS file.</returns>
	private static List<string> ParsePLS(string filePath)
	{
		return File.ReadLines(filePath)
					.Where(line => line.StartsWith("File", StringComparison.OrdinalIgnoreCase))
					.Select(line => line.Split('=', 2)[1].Trim())
					.ToList();
	}

	/// <summary>
	/// Parses a Windows Media Player Playlist (WPL) file and retrieves the file paths of the media items within it.
	/// </summary>
	/// <param name="filePath">The full file path of the WPL file to be parsed.</param>
	/// <returns>A list of file paths representing the media items contained in the WPL playlist.</returns>
	private static List<string> ParseWPL(string filePath)
	{
		var doc = XDocument.Load(filePath);
		return doc.Descendants("media")
					.Select(x => x.Attribute("src")?.Value)
					.Where(src => !string.IsNullOrWhiteSpace(src))
					.Select(src => src!)
					.ToList();
	}

	/// <summary>
	/// Parses a ZPL playlist file and extracts the list of track file paths.
	/// </summary>
	/// <param name="filePath">The full file path of the ZPL file to be parsed.</param>
	/// <returns>A list of strings representing the file paths of the tracks contained in the playlist.</returns>
	private static List<string> ParseZPL(string filePath)
	{
		var doc = XDocument.Load(filePath);
		return doc.Descendants("media")
				.Select(x => x.Attribute("src")?.Value)
				.Where(src => !string.IsNullOrWhiteSpace(src))
				.Select(src => src!)
				.ToList();
	}
}
