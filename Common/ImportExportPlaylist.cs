using System.Text;
using System.Xml.Linq;

namespace Tunetastic.Common;

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
}
