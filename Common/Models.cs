using SQLite;

namespace Tunetastic.Common;

/// <summary>
/// Represents a song entity that is stored in the `Songs` table of the database.
/// This class includes metadata and properties associated with a song, such as title, artists, album, genre, duration, and more.
/// </summary>
[Table("Songs")]
public class Song
{
	[PrimaryKey]
	public string Path { get; set; } = string.Empty;
	public string Title { get; set; } = string.Empty;
	public string Artists { get; set; } = string.Empty;
	public string Album { get; set; } = string.Empty;
	public string Genre { get; set; } = string.Empty;
	public string Year { get; set; } = string.Empty;
	public int PlayCount { get; set; }
	public string Cover { get; set; } = string.Empty;
	public double Duration { get; set; }
	public DateTime DateAdded { get; set; }
	public DateTime? DateLastPlayed { get; set; }
	public string Extension { get; set; } = string.Empty;
	public string? AudioBitrate { get; set; }
	public string? AudioChannels { get; set; }
	public string? AudioSampleRate { get; set; }
	public string? AudioCodecDescription { get; set; }
	public string? Lyrics { get; set; }
	public string? FileSize { get; set; }
	public string PlayerType { get; set; } = "Flyleaf";
}

/// <summary>
/// Represents a library resource within the Tunetastic application.
/// This model is mapped to the "Library" database table and is used to store information
/// such as the library's name and its file path.
/// </summary>
[Table("Library")]
public class LibraryModel
{
	public string Name { get; set; } = string.Empty;
	[PrimaryKey]
	public string Path { get; set; } = string.Empty;
}

/// <summary>
/// Represents a data model for storing music format information in the database.
/// This class is mapped to the 'MusicFormats' table and is used to manage and retrieve information
/// about supported music file formats, including their extensions, descriptions, and whether they are enabled.
/// </summary>
[Table("MusicFormats")]
public class MusicFormatModel
{
	[PrimaryKey]
	public string Extension { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public bool Enabled { get; set; }
}

/// <summary>
/// Represents a playlist within the Tunetastic application.
/// This class models the structure of a playlist entry in the database, including its unique identifier and name.
/// Used in conjunction with database interactions to manage user-created playlists.
/// </summary>
[Table("Playlists")]
public class PlaylistName
{
	[PrimaryKey, AutoIncrement]
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Represents a row in the PendingTagWrites table, used for deferred tag write processing.
/// </summary>
public class PendingTagWriteRow
{
	public string Path { get; set; } = string.Empty;
}

/// <summary>
/// Represents an artist entity within the music database of the Tunetastic application.
/// This class provides properties to store details about an artist,
/// including their name, description, and associated image.
/// </summary>
[Table("Artists")]
public class Artist
{
	[PrimaryKey, AutoIncrement]
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string ArtistImage { get; set; } = string.Empty;
	public string ArtistDescription { get; set; } = string.Empty;
}

/// <summary>
/// Represents data aggregation for a specific year, used for displaying and managing
/// year-based song statistics within the Tunetastic application.
/// This model includes properties to represent the year, the number of songs recorded
/// for that year and the total playback duration of those songs.
/// </summary>
public class YearModel
{
	public string Year { get; set; } = string.Empty;
	public int Count { get; set; }
	public double TotalDuration { get; set; }
}

/// <summary>
/// Represents an album entity with its associated metadata.
/// This model is used to group songs by album in the application and contains relevant
/// information, including the name of the album, the number of songs in the album,
/// the total duration of all songs in the album, and the cover image associated with the album.
/// </summary>
public class AlbumModel
{
	public string Album { get; set; } = string.Empty;
	public int Count { get; set; }
	public double TotalDuration { get; set; }
	public string Cover { get; set; } = string.Empty;
}

/// <summary>
/// Represents a data model for a music genre. This model includes information about
/// the genre name, total count of songs in the genre, and the cumulative duration of all songs
/// within the genre. It is commonly used in operations or views related to genre-based song grouping in the application.
/// </summary>
public class GenreModel
{
	public string Genre { get; set; } = string.Empty;
	public int Count { get; set; }
	public double TotalDuration { get; set; }
}

/// <summary>
/// Represents a model for an artist and associated metadata within the Tunetastic application.
/// This class encapsulates the artist's name, the count of songs associated with the artist,
/// and the total duration of those songs.
/// </summary>
public class ArtistModel
{
	public string Artist { get; set; } = string.Empty;
	public int Count { get; set; }
	public double TotalDuration { get; set; }
}

/// <summary>
/// Represents a rule for splitting or categorizing artist names in the Tunetastic application. <br/>
/// The rule can be configured as either a "Splitter" to break apart artist names, or an "Exception" to preserve specific artist names intact. <br/>
/// The Pattern field can be treated as a literal string or regular expression based on IsRegex. <br/>
/// Rules can be temporarily disabled by setting Active to false. <br/>
/// Built-in rules (IsBuiltIn=true) are preserved during resets.
/// </summary>
[Table("ArtistSplitRules")]
public class ArtistSplitRule
{
	[PrimaryKey, AutoIncrement]
	public int Id { get; set; }
	public string Type { get; set; } = string.Empty;
	public string Pattern { get; set; } = string.Empty;
	public bool IsRegex { get; set; }
	public bool Active { get; set; } = true;
	public bool IsBuiltIn { get; set; } = false;
}

/// <summary>
/// Represents an item within a search result, categorized by type.
/// The type determines whether the item corresponds to a song title, artist, or album.
/// </summary>
public sealed class SearchItem
{
	public SearchItemType Type { get; set; }

	public Song? Title { get; set; }
	public string? Artist { get; set; }
	public AlbumModel? Album { get; set; }
}

/// <summary>
/// Encapsulates categorized search results returned from the database.
/// </summary>
public class SearchResults
{
	/// <summary>
	/// Songs matched by the query (titles), ordered by relevance or other criteria.
	/// </summary>
	public List<Song> Titles { get; set; } = new();

	/// <summary>
	/// Matched artist names (string only for lighter payload).
	/// </summary>
	public List<string> Artists { get; set; } = new();

	/// <summary>
	/// Albums matched by the query with metadata.
	/// </summary>
	public List<AlbumModel> Albums { get; set; } = new();

	/// <summary>
	/// Indicates which category is most relevant for prioritizing in the UI.
	/// </summary>
	public string PrimaryCategory { get; set; } = string.Empty;

	/// <summary>
	/// True if there were no matches across any category.
	/// </summary>
	public bool IsEmpty =>
		(Titles == null || Titles.Count == 0) &&
		(Artists == null || Artists.Count == 0) &&
		(Albums == null || Albums.Count == 0);

	/// <summary>
	/// A collection of search items representing the combined result set from the search query.
	/// This property aggregates items such as songs, artists, and albums into a unified list
	/// for easier handling and display.
	/// </summary>
	public List<SearchItem> Items { get; set; } = new();
}


/// <summary>
/// Metadata for each layout, used to populate user-facing dropdowns.
/// </summary>
public class OverlayLayoutInfo
{
	/// <summary>The enum value this info describes.</summary>
	public OverlayLayout Layout { get; init; }

	/// <summary>Human-readable name shown in the dropdown.</summary>
	public string? DisplayName { get; init; }

	/// <summary>Short description shown as a subtitle or tooltip.</summary>
	public string? Description { get; init; }
}
