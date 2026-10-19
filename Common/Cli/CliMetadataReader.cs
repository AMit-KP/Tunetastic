namespace Tunetastic.Common.Cli;

/// <summary>
/// Reads the importable metadata of a single audio file for the CLI playlist command.
/// The returned <see cref="Song"/> is built with the same <see cref="LibraryScanner"/> routine the
/// library scan uses, so CLI imports behave exactly like scanned tracks. Fields the embedded tags
/// contain but the library has no column for (album artist, disc number, composer, ...) are listed
/// explicitly instead of being silently dropped.
/// </summary>
public static class CliMetadataReader
{
	/// <summary>
	/// The metadata read result for one audio file.
	/// </summary>
	public sealed class TrackImport
	{
		/// <summary>Gets the full path of the file that was read.</summary>
		public string Path { get; init; } = string.Empty;

		/// <summary>Gets the library-ready song built from the file.</summary>
		public Song Song { get; init; } = new();

		/// <summary>Gets whether the embedded tags could be read at all.</summary>
		public bool TagsReadable { get; init; }

		/// <summary>Gets the fields taken from the embedded tags (or derived from the file).</summary>
		public List<string> Imported { get; init; } = new();

		/// <summary>Gets the fields that fell back to a default because the tags had no value.</summary>
		public List<string> Defaulted { get; init; } = new();

		/// <summary>Gets the embedded values that have no matching library column and were not stored.</summary>
		public List<string> Skipped { get; init; } = new();

		/// <summary>Gets non-fatal warnings encountered while reading the file.</summary>
		public List<string> Warnings { get; init; } = new();
	}

	/// <summary>
	/// Reads <paramref name="path"/> into a library-ready <see cref="Song"/> and reports which
	/// embedded values were imported, defaulted or skipped. The imported/defaulted decisions mirror
	/// the <see cref="LibraryScanner"/> fallbacks exactly (null means missing; empty is stored as-is).
	/// </summary>
	/// <param name="path">The full path of the audio file to read.</param>
	/// <returns>The read result. Never returns null; unreadable files yield a filename-based fallback song.</returns>
	public static async Task<TrackImport> ReadAsync(string path)
	{
		var (song, succeeded) = await LibraryScanner.ExtractSongMetadata(path, 0);

		var imported = new List<string>();
		var defaulted = new List<string>();
		var skipped = new List<string>();
		var warnings = new List<string>();

		TagFields tag = new();
		bool diagnosticsOk = false;
		if (succeeded)
		{
			try
			{
				using var file = TagLib.File.Create(path);
				tag = TagFields.FromTag(file.Tag);
				diagnosticsOk = true;
			}
			catch (Exception ex)
			{
				warnings.Add($"Could not re-open tags for diagnostics: {ex.Message}");
			}
		}

		if (!succeeded)
		{
			defaulted.Add("Title (file name fallback)");
			defaulted.Add("Artist (Unknown Artist)");
			defaulted.Add("Album (Unknown Album)");
			defaulted.Add("Genre (Unknown Genre)");
			defaulted.Add("Year (Unknown Year)");
			defaulted.Add("Cover art (app icon fallback)");
			skipped.Add("All embedded tags (file tags could not be read)");
			if (song.Duration <= 0)
				warnings.Add("Duration could not be determined; the track may not play.");

			return new TrackImport
			{
				Path = path,
				Song = song,
				TagsReadable = false,
				Imported = imported,
				Defaulted = defaulted,
				Skipped = skipped,
				Warnings = warnings,
			};
		}

		if (!diagnosticsOk)
		{
			// The song itself was read fine; only the second diagnostic pass failed.
			// Report the stored values instead of claiming defaults that were never used.
			warnings.Add("Detailed tag diagnostics unavailable; the values below are the stored song values.");
			ReportStoredSong(song, imported, defaulted, skipped);
			return new TrackImport
			{
				Path = path,
				Song = song,
				TagsReadable = true,
				Imported = imported,
				Defaulted = defaulted,
				Skipped = skipped,
				Warnings = warnings,
			};
		}

		if (tag.Title != null)
			imported.Add($"Title '{tag.Title}'");
		else
			defaulted.Add("Title (file name fallback)");

		var performers = tag.Performers.Where(p => !string.IsNullOrEmpty(p)).ToList();
		if (performers.Count > 0)
		{
			imported.Add($"Artist '{performers[0]}'");
			foreach (var extra in performers.Skip(1))
				skipped.Add($"Artist '{extra}' (only the first performer is stored)");
		}
		else
		{
			defaulted.Add("Artist (Unknown Artist)");
		}

		foreach (var albumArtist in tag.AlbumArtists.Where(a => !string.IsNullOrEmpty(a)))
			skipped.Add($"Album artist '{albumArtist}' (library has no album-artist column)");

		if (tag.Album != null)
			imported.Add($"Album '{tag.Album}'");
		else
			defaulted.Add("Album (Unknown Album)");

		var genres = tag.Genres.Where(g => !string.IsNullOrEmpty(g)).ToList();
		if (genres.Count > 0)
		{
			imported.Add($"Genre '{genres[0]}'");
			foreach (var extra in genres.Skip(1))
				skipped.Add($"Genre '{extra}' (only the first genre is stored)");
		}
		else
		{
			defaulted.Add("Genre (Unknown Genre)");
		}

		if (tag.Year > 0)
			imported.Add($"Year '{tag.Year}'");
		else
			defaulted.Add("Year (Unknown Year)");

		if (tag.Track > 0)
			imported.Add($"Track number '{tag.Track}'");
		else
			defaulted.Add("Track number (none in tags)");
		if (tag.TrackCount > 0)
			skipped.Add($"Track count '{tag.TrackCount}' (library has no track-count column)");
		if (tag.Disc > 0)
			skipped.Add($"Disc number '{tag.Disc}' (library has no disc-number column)");
		if (tag.DiscCount > 0)
			skipped.Add($"Disc count '{tag.DiscCount}' (library has no disc-count column)");

		foreach (var composer in tag.Composers.Where(c => !string.IsNullOrEmpty(c)))
			skipped.Add($"Composer '{composer}' (library has no composer column)");

		if (!string.IsNullOrWhiteSpace(tag.Lyrics))
		{
			song.Lyrics = tag.Lyrics;
			imported.Add("Lyrics (embedded)");
		}

		if (tag.PictureCount > 0)
		{
			imported.Add(string.IsNullOrWhiteSpace(tag.PictureMimeType)
				? "Cover art (embedded)"
				: $"Cover art (embedded {tag.PictureMimeType})");
			if (tag.PictureCount > 1)
				skipped.Add($"{tag.PictureCount - 1} additional embedded picture(s) (only the first is stored)");
		}
		else
		{
			defaulted.Add("Cover art (app icon fallback)");
		}

		if (!string.IsNullOrWhiteSpace(tag.Comment))
			skipped.Add("Comment (library has no comment column)");
		if (!string.IsNullOrWhiteSpace(tag.Copyright))
			skipped.Add("Copyright (library has no copyright column)");

		ReportProbedValues(song, imported, defaulted);

		if (song.Duration <= 0)
			warnings.Add("Duration could not be determined; the track may not play.");

		return new TrackImport
		{
			Path = path,
			Song = song,
			TagsReadable = true,
			Imported = imported,
			Defaulted = defaulted,
			Skipped = skipped,
			Warnings = warnings,
		};
	}

	/// <summary>
	/// Reports the file-derived (probed) values of <paramref name="song"/>.
	/// </summary>
	private static void ReportProbedValues(Song song, List<string> imported, List<string> defaulted)
	{
		if (song.Duration > 0)
			imported.Add($"Duration '{TimeSpan.FromSeconds(song.Duration):m\\:ss}' (probed)");
		else
			defaulted.Add("Duration (could not be determined)");
		imported.Add($"File type '{song.Extension}' ({song.PlayerType} playback engine)");
		if (!string.IsNullOrWhiteSpace(song.AudioBitrate))
			imported.Add($"Bitrate '{song.AudioBitrate}' (probed)");
		if (!string.IsNullOrWhiteSpace(song.AudioSampleRate))
			imported.Add($"Sample rate '{song.AudioSampleRate}' (probed)");
		if (!string.IsNullOrWhiteSpace(song.AudioChannels))
			imported.Add($"Channels '{song.AudioChannels}' (probed)");
		if (!string.IsNullOrWhiteSpace(song.FileSize))
			imported.Add($"File size '{song.FileSize}'");
	}

	/// <summary>
	/// Reports the values of an already-read <paramref name="song"/> when the diagnostic tag pass failed.
	/// </summary>
	private static void ReportStoredSong(Song song, List<string> imported, List<string> defaulted, List<string> skipped)
	{
		imported.Add($"Title '{song.Title}' (stored)");
		imported.Add($"Artist '{song.Artists}' (stored)");
		imported.Add($"Album '{song.Album}' (stored)");
		imported.Add($"Genre '{song.Genre}' (stored)");
		imported.Add($"Year '{song.Year}' (stored)");
		if (song.Track > 0)
			imported.Add($"Track number '{song.Track}' (stored)");
		else
			defaulted.Add("Track number (none in tags)");
		if (!string.IsNullOrWhiteSpace(song.Lyrics))
			imported.Add("Lyrics (stored)");
		imported.Add("Cover art (stored thumbnail)");
		skipped.Add("Unmapped embedded fields (diagnostics unavailable for this file)");
		ReportProbedValues(song, imported, defaulted);
	}

	/// <summary>
	/// A detached copy of the tag values the CLI reports on, so diagnostics outlive the file they were read from.
	/// </summary>
	private sealed class TagFields
	{
		public string? Title { get; init; }
		public string[] Performers { get; init; } = [];
		public string[] AlbumArtists { get; init; } = [];
		public string? Album { get; init; }
		public string[] Genres { get; init; } = [];
		public uint Year { get; init; }
		public uint Track { get; init; }
		public uint TrackCount { get; init; }
		public uint Disc { get; init; }
		public uint DiscCount { get; init; }
		public string[] Composers { get; init; } = [];
		public string? Lyrics { get; init; }
		public int PictureCount { get; init; }
		public string? PictureMimeType { get; init; }
		public string? Comment { get; init; }
		public string? Copyright { get; init; }

		public static TagFields FromTag(TagLib.Tag tag)
		{
			return new TagFields
			{
				Title = tag.Title,
				Performers = tag.Performers ?? [],
				AlbumArtists = tag.AlbumArtists ?? [],
				Album = tag.Album,
				Genres = tag.Genres ?? [],
				Year = tag.Year,
				Track = tag.Track,
				TrackCount = tag.TrackCount,
				Disc = tag.Disc,
				DiscCount = tag.DiscCount,
				Composers = tag.Composers ?? [],
				Lyrics = tag.Lyrics,
				PictureCount = tag.Pictures?.Length ?? 0,
				PictureMimeType = tag.Pictures?.Length > 0 ? tag.Pictures[0].MimeType : null,
				Comment = tag.Comment,
				Copyright = tag.Copyright,
			};
		}
	}
}
