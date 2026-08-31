using System.Text.RegularExpressions;
using SQLite;

namespace Tunetastic.Common.Services;

/// <summary>
/// Provides utility methods for managing database interactions within the Tunetastic application.
/// This class includes functionality to initialize the database, manage songs, playlists, and related entries.
/// It ensures efficient operations like insertion, query, update, and deletion of song and playlist data.
/// </summary>
public class DatabaseHelper
{
	private static DatabaseHelper _instance = null!;
	private SQLiteAsyncConnection _database = null!;

	private DatabaseHelper() { }

	/// <summary>
	/// A static property that provides a singleton instance of the `DatabaseHelper` class.
	/// This property ensures that only one instance of `DatabaseHelper` is created and shared across the application.
	/// </summary>
	public static DatabaseHelper Instance
	{
		get
		{
			return _instance ??= new DatabaseHelper();
		}
	}

	/// <summary>
	/// Opens (or creates) the app's local SQLite database and ensures the full schema exists.
	/// Creates the following tables:
	/// <list type="bullet">
	/// <item>
	/// <description><c>Library</c>
	/// <list type="bullet">
	/// <item><description><c>Name</c> (TEXT, NOT NULL) — Display name for the library entry.</description></item>
	/// <item><description><c>Path</c> (TEXT, NOT NULL, UNIQUE, COLLATE NOCASE) — Filesystem path to the library root.</description></item>
	/// </list>
	/// </description>
	/// </item>
	/// <item>
	/// <description><c>MusicFormats</c>
	/// <list type="bullet">
	/// <item><description><c>Extension</c> (TEXT, PRIMARY KEY, COLLATE NOCASE) — File extension, e.g. mp3, flac.</description></item>
	/// <item><description><c>Description</c> (TEXT, NOT NULL) — Human-readable format name.</description></item>
	/// <item><description><c>Enabled</c> (INTEGER, NOT NULL) — 0/1 flag for whether this format is scanned.</description></item>
	/// </list>
	/// </description>
	/// </item>
	/// <item>
	/// <description><c>Songs</c>
	/// <list type="bullet">
	/// <item><description><c>Id</c> (INTEGER, PRIMARY KEY AUTOINCREMENT) — Surrogate key.</description></item>
	/// <item><description><c>Path</c> (TEXT, UNIQUE) — Filesystem path to the audio file.</description></item>
	/// <item><description><c>Title</c> (TEXT) — Track title.</description></item>
	/// <item><description><c>Artists</c> (TEXT) — Raw/unsplit artist string as read from tags.</description></item>
	/// <item><description><c>Album</c> (TEXT) — Album name.</description></item>
	/// <item><description><c>Genre</c> (TEXT) — Genre tag.</description></item>
	/// <item><description><c>Year</c> (TEXT) — Release year.</description></item>
	/// <item><description><c>PlayCount</c> (INTEGER) — Number of times played.</description></item>
	/// <item><description><c>Cover</c> (TEXT) — Path or reference to cached cover art.</description></item>
	/// <item><description><c>Duration</c> (REAL) — Track length.</description></item>
	/// <item><description><c>DateAdded</c> (DATETIME) — When the row was inserted.</description></item>
	/// <item><description><c>DateLastPlayed</c> (DATETIME, DEFAULT NULL) — Last played timestamp.</description></item>
	/// <item><description><c>Extension</c> (TEXT) — File extension.</description></item>
	/// <item><description><c>AudioBitrate</c> (TEXT, DEFAULT NULL) — Bitrate metadata.</description></item>
	/// <item><description><c>AudioChannels</c> (TEXT, DEFAULT NULL) — Channel count/layout metadata.</description></item>
	/// <item><description><c>AudioSampleRate</c> (TEXT, DEFAULT NULL) — Sample rate metadata.</description></item>
	/// <item><description><c>AudioCodecDescription</c> (TEXT, DEFAULT NULL) — Human-readable codec description.</description></item>
	/// <item><description><c>FileSize</c> (TEXT) — File size.</description></item>
	/// <item><description><c>Lyrics</c> (TEXT, DEFAULT NULL) — Cached lyrics text.</description></item>
	/// <item><description><c>PlayerType</c> (TEXT, NOT NULL, DEFAULT 'Flyleaf') — Playback engine for this file.</description></item>
	/// </list>
	/// </description>
	/// </item>
	/// <item>
	/// <description><c>Playlists</c>
	/// <list type="bullet">
	/// <item><description><c>Id</c> (INTEGER, PRIMARY KEY AUTOINCREMENT) — Surrogate key.</description></item>
	/// <item><description><c>Name</c> (TEXT) — Playlist name.</description></item>
	/// </list>
	/// </description>
	/// </item>
	/// <item>
	/// <description><c>PlaylistSongs</c>
	/// <list type="bullet">
	/// <item><description><c>PlaylistId</c> (INTEGER, PK composite, FK → Playlists.Id ON DELETE CASCADE) — Owning playlist.</description></item>
	/// <item><description><c>SongPath</c> (TEXT, PK composite, FK → Songs.Path ON DELETE CASCADE) — Referenced song.</description></item>
	/// <item><description><c>Position</c> (INTEGER, DEFAULT 0) — Sort order within the playlist.</description></item>
	/// </list>
	/// </description>
	/// </item>
	/// <item>
	/// <description><c>QueuedPlayingList</c>
	/// <list type="bullet">
	/// <item><description><c>Id</c> (INTEGER, PRIMARY KEY AUTOINCREMENT) — Surrogate key.</description></item>
	/// <item><description><c>Path</c> (TEXT, NOT NULL, FK → Songs.Path ON DELETE CASCADE) — Queued song.</description></item>
	/// <item><description><c>Position</c> (INTEGER) — Order within the queue.</description></item>
	/// </list>
	/// </description>
	/// </item>
	/// <item>
	/// <description><c>Artists</c>
	/// <list type="bullet">
	/// <item><description><c>Id</c> (INTEGER, PRIMARY KEY AUTOINCREMENT) — Surrogate key.</description></item>
	/// <item><description><c>Name</c> (TEXT, NOT NULL, UNIQUE, COLLATE NOCASE) — Artist name.</description></item>
	/// <item><description><c>ArtistImage</c> (TEXT) — Path or reference to cached artist image.</description></item>
	/// <item><description><c>ArtistDescription</c> (TEXT) — Bio/description text.</description></item>
	/// </list>
	/// </description>
	/// </item>
	/// <item>
	/// <description><c>SongArtists</c>
	/// <list type="bullet">
	/// <item><description><c>SongPath</c> (TEXT, PK composite, NOT NULL, FK → Songs.Path ON DELETE CASCADE) — Song side of the link.</description></item>
	/// <item><description><c>ArtistId</c> (INTEGER, PK composite, NOT NULL, FK → Artists.Id ON DELETE CASCADE) — Artist side of the link.</description></item>
	/// </list>
	/// </description>
	/// </item>
	/// <item>
	/// <description><c>ArtistSplitRules</c>
	/// <list type="bullet">
	/// <item><description><c>Id</c> (INTEGER, PRIMARY KEY AUTOINCREMENT) — Surrogate key.</description></item>
	/// <item><description><c>Type</c> (TEXT, NOT NULL, CHECK IN 'Splitter'/'Exception') — Rule type.</description></item>
	/// <item><description><c>Pattern</c> (TEXT, NOT NULL) — Literal string or regex pattern to match.</description></item>
	/// <item><description><c>IsRegex</c> (INTEGER, NOT NULL, DEFAULT 0) — 0/1 whether Pattern is a regex.</description></item>
	/// <item><description><c>Active</c> (INTEGER, NOT NULL, DEFAULT 1) — 0/1 whether the rule is enabled.</description></item>
	/// <item><description><c>IsBuiltIn</c> (INTEGER, NOT NULL, DEFAULT 0) — 0/1 whether this is an app-shipped default rule.</description></item>
	/// </list>
	/// </description>
	/// </item>
	/// <item>
	/// <description><c>PendingTagWrites</c>
	/// <list type="bullet">
	/// <item><description><c>Path</c> (TEXT, PRIMARY KEY, FK → Songs.Path ON DELETE CASCADE) — Song with pending changes.</description></item>
	/// <item><description><c>Cover</c> (INTEGER, NOT NULL, DEFAULT 0) — Dirty flag for cover art.</description></item>
	/// <item><description><c>Title</c> (INTEGER, NOT NULL, DEFAULT 0) — Dirty flag for title.</description></item>
	/// <item><description><c>Artist</c> (INTEGER, NOT NULL, DEFAULT 0) — Dirty flag for artist.</description></item>
	/// <item><description><c>Album</c> (INTEGER, NOT NULL, DEFAULT 0) — Dirty flag for album.</description></item>
	/// <item><description><c>Genre</c> (INTEGER, NOT NULL, DEFAULT 0) — Dirty flag for genre.</description></item>
	/// <item><description><c>Year</c> (INTEGER, NOT NULL, DEFAULT 0) — Dirty flag for year.</description></item>
	/// <item><description><c>Lyrics</c> (INTEGER, NOT NULL, DEFAULT 0) — Dirty flag for lyrics.</description></item>
	/// </list>
	/// </description>
	/// </item>
	/// <item>
	/// <description><c>SongFTS</c> (FTS5 virtual table, content='Songs', content_rowid='Id')
	/// <list type="bullet">
	/// <item><description><c>Title</c> — Indexed, mirrors Songs.Title.</description></item>
	/// <item><description><c>Album</c> — Indexed, mirrors Songs.Album.</description></item>
	/// <item><description><c>Genre</c> — Indexed, mirrors Songs.Genre.</description></item>
	/// <item><description><c>Year</c> — Indexed, mirrors Songs.Year.</description></item>
	/// <item><description><c>Artists</c> — Indexed, mirrors Songs.Artists.</description></item>
	/// <item><description><c>Path</c> — UNINDEXED, carried through for row identification.</description></item>
	/// </list>
	/// </description>
	/// </item>
	/// <item>
	/// <description><c>ArtistFTS</c> (FTS5 virtual table, content='Artists', content_rowid='Id')
	/// <list type="bullet">
	/// <item><description><c>Name</c> — Indexed, mirrors Artists.Name.</description></item>
	/// </list>
	/// </description>
	/// </item>
	/// </list>
	/// </summary>
	/// <returns>A <see cref="Task"/> that completes once schema creation, migration, and seeding have finished.</returns>
	public async Task InitializeDatabase()
	{
		var dbPath = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "tunetastic.db3");
		_database = new SQLiteAsyncConnection(dbPath);

		await _database.ExecuteAsync("PRAGMA foreign_keys = ON");
		await _database.ExecuteAsync("ANALYZE");
		await _database.ExecuteAsync("PRAGMA optimize");

		await _database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Library (
									   Name TEXT NOT NULL,
									   Path TEXT NOT NULL COLLATE NOCASE UNIQUE)");

		await _database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS MusicFormats (
									   Extension TEXT PRIMARY KEY COLLATE NOCASE,
									   Description TEXT NOT NULL,
									   Enabled INTEGER NOT NULL)");

		await _database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Songs (
									   Id INTEGER PRIMARY KEY AUTOINCREMENT,
									   Path TEXT UNIQUE,
									   Title TEXT,
									   Artists TEXT,
									   Album TEXT,
									   Genre TEXT,
									   Year TEXT,
									   PlayCount INTEGER,
									   Cover TEXT,
									   Duration REAL,
									   DateAdded DATETIME,
									   DateLastPlayed DATETIME DEFAULT NULL,
									   Extension TEXT,
									   AudioBitrate TEXT DEFAULT NULL,
									   AudioChannels TEXT DEFAULT NULL,
									   AudioSampleRate TEXT DEFAULT NULL,
									   AudioCodecDescription TEXT DEFAULT NULL,
									   FileSize TEXT,
									   Lyrics TEXT DEFAULT NULL,
									   PlayerType TEXT NOT NULL DEFAULT 'Flyleaf')");

		await _database.ExecuteAsync(@"CREATE INDEX IF NOT EXISTS idx_Songs_Title_nocase ON Songs(Title COLLATE NOCASE)");
		await _database.ExecuteAsync(@"CREATE INDEX IF NOT EXISTS idx_Songs_Album_nonempty ON Songs(Album) WHERE Album IS NOT NULL AND TRIM(Album) != ''");
		await _database.ExecuteAsync(@"CREATE INDEX IF NOT EXISTS idx_Songs_Genre_nonempty ON Songs(Genre) WHERE Genre IS NOT NULL AND TRIM(Genre) != ''");
		await _database.ExecuteAsync(@"CREATE INDEX IF NOT EXISTS idx_Songs_Year_nonempty  ON Songs(Year)  WHERE Year  IS NOT NULL AND TRIM(Year)  != ''");

		await _database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Playlists (
									   Id INTEGER PRIMARY KEY AUTOINCREMENT,
									   Name TEXT)");

		await _database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS PlaylistSongs (
									   PlaylistId INTEGER,
									   SongPath TEXT,
									   Position INTEGER DEFAULT 0,
									   PRIMARY KEY (PlaylistId, SongPath),
									   FOREIGN KEY (PlaylistId) REFERENCES Playlists(Id) ON DELETE CASCADE,
									   FOREIGN KEY (SongPath) REFERENCES Songs(Path) ON DELETE CASCADE)");

		await _database.ExecuteAsync(@"CREATE INDEX IF NOT EXISTS idx_PlaylistSongs_PlaylistId_Position ON PlaylistSongs(PlaylistId, Position)");

		await _database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS QueuedPlayingList (
									   Id INTEGER PRIMARY KEY AUTOINCREMENT,
									   Path TEXT NOT NULL,
									   Position INTEGER,
									   FOREIGN KEY (Path) REFERENCES Songs(Path) ON DELETE CASCADE)");

		await _database.ExecuteAsync(@"CREATE INDEX IF NOT EXISTS idx_QueuedPlayingList_Path_Position ON QueuedPlayingList(Path, Position)");
		await _database.ExecuteAsync(@"CREATE INDEX IF NOT EXISTS idx_QueuedPlayingList_Position ON QueuedPlayingList(Position)");

		await _database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Artists (
									   Id INTEGER PRIMARY KEY AUTOINCREMENT,
									   Name TEXT NOT NULL COLLATE NOCASE UNIQUE,
									   ArtistImage TEXT,
									   ArtistDescription TEXT)");

		await _database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS SongArtists (
									   SongPath TEXT NOT NULL,
									   ArtistId INTEGER NOT NULL,
									   PRIMARY KEY (SongPath, ArtistId),
									   FOREIGN KEY (SongPath) REFERENCES Songs(Path) ON DELETE CASCADE,
									   FOREIGN KEY (ArtistId) REFERENCES Artists(Id) ON DELETE CASCADE)");

		await _database.ExecuteAsync(@"CREATE INDEX IF NOT EXISTS idx_SongArtists_ArtistId ON SongArtists(ArtistId)");
		await _database.ExecuteAsync(@"CREATE INDEX IF NOT EXISTS idx_SongArtists_SongPath ON SongArtists(SongPath)");

		await _database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS ArtistSplitRules (
									   Id INTEGER PRIMARY KEY AUTOINCREMENT,
									   Type TEXT NOT NULL CHECK (Type IN ('Splitter','Exception')),
									   Pattern TEXT NOT NULL,
									   IsRegex INTEGER NOT NULL DEFAULT 0,
									   Active INTEGER NOT NULL DEFAULT 1,
									   IsBuiltIn INTEGER NOT NULL DEFAULT 0,
									   UNIQUE(Type, Pattern, IsRegex))");

		await _database.ExecuteAsync(@"CREATE INDEX IF NOT EXISTS idx_ArtistSplitRules_ActiveType ON ArtistSplitRules(Active, Type)");

		await _database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS PendingTagWrites (
									   Path TEXT PRIMARY KEY,
									   FOREIGN KEY (Path) REFERENCES Songs(Path) ON DELETE CASCADE)");

		foreach (var col in new[] { "Cover", "Title", "Artist", "Album", "Genre", "Year", "Lyrics" })
		{
			try
			{
				await _database.ExecuteAsync($"ALTER TABLE PendingTagWrites ADD COLUMN {col} INTEGER NOT NULL DEFAULT 0");
			}
			catch { }
		}

		await _database.ExecuteAsync(@"CREATE VIRTUAL TABLE IF NOT EXISTS SongFTS
									   USING fts5(
									   Title,
									   Album,
									   Genre,
									   Year,
									   Artists,
									   Path UNINDEXED,
									   content='Songs',
									   content_rowid='Id',
									   tokenize='unicode61')");

		await _database.ExecuteAsync("INSERT INTO SongFTS(SongFTS) VALUES('rebuild')");

		await _database.ExecuteAsync(@"CREATE VIRTUAL TABLE IF NOT EXISTS ArtistFTS
									   USING fts5(
									   Name,
									   content='Artists',
									   content_rowid='Id',
									   tokenize='unicode61')");

		await _database.ExecuteAsync("INSERT INTO ArtistFTS(ArtistFTS) VALUES('rebuild')");

		await _database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS FileScanMeta (
									   Path TEXT PRIMARY KEY,
									   LastModifiedUtc INTEGER NOT NULL,
									   CreationTimeUtc INTEGER NOT NULL,
									   FileSizeBytes INTEGER NOT NULL,
									   LastScannedUtc INTEGER NOT NULL,
									   FOREIGN KEY (Path) REFERENCES Songs(Path) ON DELETE CASCADE);");

		await PopulateMusicFormatTable();

		await EnsureDefaultArtistRules();
		await ReloadArtistSplitRules();

		await EnsureArtistLinksPopulated();
	}

	/// <summary>
	/// Retrieves all saved library entries from the database.
	/// This method queries the 'Library' table and fetches the name and path of each library,
	/// sorted in ascending order based on the row ID.
	/// If an error occurs during the query execution, an empty list is returned.
	/// </summary>
	/// <returns>
	/// A task that represents the asynchronous operation of fetching all libraries.
	/// The task result contains a list of <see cref="LibraryModel"/> objects representing the retrieved library entries.
	/// </returns>
	public async Task<List<LibraryModel>> GetAllLibraries()
	{
		try
		{
			return await _database.QueryAsync<LibraryModel>("SELECT Name, Path FROM Library ORDER BY rowid ASC");
		}
		catch (Exception)
		{
			return new List<LibraryModel>();
		}
	}

	/// <summary>
	/// Adds new library or updates existing ones in the database based on its paths.
	/// This method ensures that library with the same path is updated with new names,
	/// while new library is inserted.
	/// </summary>
	/// <param name="library">A model of <see cref="LibraryModel"/> containing the library's name and path.</param>
	/// <returns>
	/// A task that represents the asynchronous operation of adding or updating library in the database.
	/// </returns>
	public async Task AddOrUpdateLibrary(LibraryModel library)
	{
		if (library == null) return;

		const string sql = @"INSERT INTO Library (Name, Path)
							 VALUES (?, ?)
							 ON CONFLICT(Path) DO UPDATE SET
							 Name = excluded.Name;";

		await _database.RunInTransactionAsync(conn =>
		{
			conn.Execute(sql, library.Name, library.Path);
		});
	}

	/// <summary>
	/// Removes a specified library from the database based on its path.
	/// This operation performs a deletion in the 'Library' table for the given library model.
	/// </summary>
	/// <param name="model">
	/// The <see cref="LibraryModel"/> object representing the library to be removed.
	/// The model must have a non-null, non-empty, and non-whitespace Path property.
	/// </param>
	/// <returns>
	/// A task that represents the asynchronous operation of removing a library from the database.
	/// </returns>
	public async Task RemoveLibrary(LibraryModel model)
	{
		if (model == null || string.IsNullOrWhiteSpace(model.Path))
			return;

		await _database.ExecuteAsync("DELETE FROM Library WHERE Path = ?", model.Path);
	}

	/// <summary>
	/// Populates the 'MusicFormat' table with predefined data containing various audio format information.
	/// This includes details such as format names, file extensions, and descriptions relevant for music files.
	/// Ensures consistency by avoiding duplicate entries and standardizing the data required for application functionality.
	/// </summary>
	/// <returns>
	/// A task that represents the asynchronous operation of populating the 'MusicFormat' table.
	/// </returns>
	private async Task PopulateMusicFormatTable()
	{
		var mfCount = await _database.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM MusicFormats");
		if (mfCount == 0)
		{
			await _database.RunInTransactionAsync(conn =>
			{
				conn.Execute(@"INSERT INTO MusicFormats (Extension, Description, Enabled) VALUES (?, ?, ?)",
					".mp3", "MPEG-1 Audio Layer 3 – The compression that saves valuable space while maintaining near-flawless quality of the original source of sound.", 1);
				conn.Execute(@"INSERT INTO MusicFormats (Extension, Description, Enabled) VALUES (?, ?, ?)",
					".m4a", "MPEG-4 Audio - An audio file format developed by Apple, designed to store high-quality sound efficiently.", 1);
				conn.Execute(@"INSERT INTO MusicFormats (Extension, Description, Enabled) VALUES (?, ?, ?)",
					".flac", "Free Lossless Audio Codec – This lossless audio format compresses audio data without losing any quality, making it perfect for preserving the original sound.", 1);
				conn.Execute(@"INSERT INTO MusicFormats (Extension, Description, Enabled) VALUES (?, ?, ?)",
					".alac", "Apple Lossless Audio Codec – Developed by Apple, this lossless audio format is designed for use on Apple devices, ensuring high-quality audio playback.", 0);
				conn.Execute(@"INSERT INTO MusicFormats (Extension, Description, Enabled) VALUES (?, ?, ?)",
					".wav", "Waveform Audio File Format – An uncompressed audio format that stores audio data in its raw waveform, offering pristine sound quality.", 0);
				conn.Execute(@"INSERT INTO MusicFormats (Extension, Description, Enabled) VALUES (?, ?, ?)",
					".wma", "Windows Media Audio – Windows audio format known for its lossless compression, retaining high audio quality throughout all types of restructuring processes.", 0);
				conn.Execute(@"INSERT INTO MusicFormats (Extension, Description, Enabled) VALUES (?, ?, ?)",
					".aac", "Advanced Audio Coding - An audio format that delivers decently high-quality sound and is enhanced using advanced coding.", 0);
				conn.Execute(@"INSERT INTO MusicFormats (Extension, Description, Enabled) VALUES (?, ?, ?)",
					".ogg", "Ogg Vorbis – An open-source digital multimedia container format designed to provide for efficient streaming and manipulation of digital multimedia.", 0);
				conn.Execute(@"INSERT INTO MusicFormats (Extension, Description, Enabled) VALUES (?, ?, ?)",
					".aiff", "Audio Interchange File Format – An uncompressed CD-quality audio format developed by Apple, commonly used in professional audio environments.", 0);
			});
		}
	}

	/// <summary>
	/// Updates the enabled status of a specific music file format in the database.
	/// This method modifies the 'MusicFormats' table, changing the 'Enabled' state of the specified file extension.
	/// </summary>
	/// <param name="extension">
	/// The file extension representing the music format (e.g., ".mp3", ".flac").
	/// If the provided extension is null, empty, or contains only whitespace, the method exits without making updates.
	/// </param>
	/// <param name="enabled">
	/// A boolean indicating the desired enabled status for the music format.
	/// Pass <c>true</c> to enable the music format, or <c>false</c> to disable it.
	/// </param>
	/// <returns>
	/// A task that represents the asynchronous operation of updating the enabled status.
	/// </returns>
	public async Task SetMusicFormatEnabled(string extension, bool enabled)
	{
		if (string.IsNullOrWhiteSpace(extension))
			return;

		var ext = extension.Trim();
		if (!ext.StartsWith(".")) ext = "." + ext;

		await _database.ExecuteAsync("UPDATE MusicFormats SET Enabled = ? WHERE Extension = ? COLLATE NOCASE", enabled ? 1 : 0, ext);
	}

	public async Task<List<MusicFormatModel>> GetAllMusicFormats()
	{
		return await _database.QueryAsync<MusicFormatModel>("SELECT Extension, Description, Enabled FROM MusicFormats ORDER BY rowid ASC");
	}

	/// <summary>
	/// Inserts multiple songs into the `Songs` table of the database.
	/// This method uses a database transaction to insert or update the given list of songs.
	/// If a song with the same path already exists, it is replaced with the provided data.
	/// </summary>
	/// <param name="songs">
	/// A list of songs to be inserted into the database. Each song should contain details such as title, path, artists, album, genre, duration, and more.
	/// </param>
	/// <returns>
	/// A task that represents the asynchronous operation of inserting the songs into the database.
	/// </returns>
	public async Task InsertMultipleSongs(List<Song> songs)
	{
		await _database.RunInTransactionAsync(conn =>
		{
			foreach (var song in songs)
			{
				conn.Execute(@"INSERT OR REPLACE INTO Songs
							   (Path, Title, Artists, Album, Genre, Year, PlayCount, Cover, Duration, DateAdded, DateLastPlayed, Extension, AudioBitrate, AudioChannels, AudioCodecDescription, AudioSampleRate, Lyrics, FileSize, PlayerType)
							   VALUES
							   (?,	  ?,	 ?,		  ?,	 ?,		?,	  ?,		 ?,		?,		  ?,		 ?,				 ?,			?,			  ?,			 ?,						?,				 ?,		 ?,		   ?)
							   ON CONFLICT(Path) DO UPDATE SET
							   Title = excluded.Title,
							   Artists = excluded.Artists,
							   Album = excluded.Album,
							   Genre = excluded.Genre,
							   Year = excluded.Year,
							   PlayCount = excluded.PlayCount,
							   Cover = excluded.Cover,
							   Duration = excluded.Duration,
							   DateAdded = excluded.DateAdded,
							   DateLastPlayed = excluded.DateLastPlayed,
							   Extension = excluded.Extension,
							   AudioBitrate = excluded.AudioBitrate,
							   AudioChannels = excluded.AudioChannels,
							   AudioCodecDescription = excluded.AudioCodecDescription,
							   AudioSampleRate = excluded.AudioSampleRate,
							   Lyrics = excluded.Lyrics,
							   FileSize = excluded.FileSize,
							   PlayerType = excluded.PlayerType;",
							   song.Path, song.Title, song.Artists, song.Album, song.Genre, song.Year,
							   song.PlayCount, song.Cover, song.Duration, song.DateAdded, song.DateLastPlayed,
							   song.Extension, song.AudioBitrate, song.AudioChannels, song.AudioCodecDescription, song.AudioSampleRate, song.Lyrics, song.FileSize, song.PlayerType);

				SyncSongArtistsForSong(conn, song);
			}
		});
		await PruneUnusedArtists();
		await RebuildFts();
	}

	/// <summary>
	/// Updates the database with the provided list of songs. This process includes:
	/// <br/>
	/// - Retrieving existing song data along with play count and last played date.
	/// <br/>
	/// - Retrieving existing playlist-to-song relationships.
	/// <br/>
	/// - Clearing all records from the `Songs` table.
	/// <br/>
	/// - Inserting the provided songs into the database, preserving play count and last played date for existing entries.
	/// <br/>
	/// - Restoring playlist-to-song relationships in the `PlaylistSongs` table.
	/// <br/>
	/// - Cleaning up unused artist records and rebuilding the full-text search (FTS) indexing.
	/// </summary>
	/// <param name="songs">A collection of song objects to be saved into or updated within the database.</param>
	/// <returns>A task representing the asynchronous operation of updating the songs database.</returns>
	public async Task UpdateSongsDatabase(List<Song> songs)
	{
		var existingSongData = await _database.QueryAsync<Song>("SELECT Path, PlayCount, DateLastPlayed FROM Songs");
		var existingPlaylistLinks = await _database.QueryAsync<(int PlaylistId, string SongPath)>("SELECT PlaylistId, SongPath FROM PlaylistSongs");

		await DeleteAllSongsFromDB();

		await _database.RunInTransactionAsync(conn =>
		{
			foreach (var song in songs)
			{
				int existingPlayCount = existingSongData.FirstOrDefault(s => s.Path == song.Path)?.PlayCount ?? 0;
				DateTime? lastPlayed = existingSongData.FirstOrDefault(s => s.Path == song.Path)?.DateLastPlayed;

				conn.Execute(@"INSERT OR REPLACE INTO Songs
							   (Path, Title, Artists, Album, Genre, Year, PlayCount, Cover, Duration, DateAdded, DateLastPlayed, Extension, AudioBitrate, AudioChannels, AudioCodecDescription, AudioSampleRate, Lyrics, FileSize, PlayerType)
							   VALUES
							   (?,	  ?,	 ?,		  ?,	 ?,		?,	  ?,		 ?,		?,		  ?,		 ?,				 ?,			?,			  ?,			 ?,						?,				 ?,		 ?,		   ?)
							   ON CONFLICT(Path) DO UPDATE SET
							   Title = excluded.Title,
							   Artists = excluded.Artists,
							   Album = excluded.Album,
							   Genre = excluded.Genre,
							   Year = excluded.Year,
							   PlayCount = excluded.PlayCount,
							   Cover = excluded.Cover,
							   Duration = excluded.Duration,
							   DateAdded = excluded.DateAdded,
							   DateLastPlayed = excluded.DateLastPlayed,
							   Extension = excluded.Extension,
							   AudioBitrate = excluded.AudioBitrate,
							   AudioChannels = excluded.AudioChannels,
							   AudioCodecDescription = excluded.AudioCodecDescription,
							   AudioSampleRate = excluded.AudioSampleRate,
							   Lyrics = excluded.Lyrics,
							   FileSize = excluded.FileSize,
							   PlayerType = excluded.PlayerType;",
							   song.Path, song.Title, song.Artists, song.Album, song.Genre, song.Year,
							   existingPlayCount, song.Cover, song.Duration, song.DateAdded, lastPlayed,
							   song.Extension, song.AudioBitrate, song.AudioChannels, song.AudioCodecDescription, song.AudioSampleRate, song.Lyrics, song.FileSize, song.PlayerType);

				SyncSongArtistsForSong(conn, song);
			}
		});

		await _database.RunInTransactionAsync(conn =>
		{
			foreach (var link in existingPlaylistLinks)
			{
				conn.Execute("INSERT INTO PlaylistSongs (PlaylistId, SongPath) VALUES (?, ?)", link.PlaylistId, link.SongPath);
			}
		});

		await PruneUnusedArtists();
		await RebuildFts();
	}

	/// <summary>
	/// Deletes all song records from the database by removing all entries in the `Songs` table.
	/// This operation permanently clears any data stored for the songs in the database.
	/// </summary>
	/// <returns>
	/// A task that represents the asynchronous operation of deleting all songs from the database.
	/// </returns>
	public async Task DeleteAllSongsFromDB()
	{
		await _database.ExecuteAsync("DELETE FROM Songs");
	}

	/// <summary>
	/// Deletes a song entry from the database using the specified file path.
	/// This method removes the corresponding record from the `Songs` table
	/// and optionally from the `SongFTS` table, if it exists.
	/// </summary>
	/// <param name="path">
	/// The file path of the song to be deleted from the database.
	/// </param>
	/// <returns>
	/// A task that represents the asynchronous operation of deleting the song entry from the database.
	/// </returns>
	public async Task DeleteSongFromDB(string path)
	{
		await _database.ExecuteAsync("DELETE FROM Songs WHERE Path = ?", path);
		await PruneUnusedArtists();
		try
		{
			await _database.ExecuteAsync("DELETE FROM SongFTS WHERE Path = ?", path);
		}
		catch (Exception)
		{
			//ignored
		}
	}

	/// <summary>
	/// Loads all songs from the database and sorts them based on the specified property, order and limit.
	/// </summary>
	/// <param name="orderBy">The property to sort the songs by, such as Title, Artists, or Album. Defaults to Title.</param>
	/// <param name="ascending">A boolean indicating whether the songs should be sorted in ascending order. Defaults to true.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains a list of songs ordered by the specified property, order and limit.
	/// If an exception occurs, an empty list is returned.</returns>
	public async Task<List<Song>> LoadSongsFromDB(SongProperty orderBy = SongProperty.Title, bool ascending = true, int limit = 0, string? whereCondition = null)
	{
		try
		{
			return await _database.QueryAsync<Song>($"SELECT * FROM Songs {(whereCondition == null ? "" : $" WHERE {whereCondition}")} ORDER BY {orderBy.ToString()} {(ascending ? "ASC" : "DESC")}{(limit > 0 ? $" LIMIT {limit}" : "")}");
		}
		catch (Exception)
		{
			return new List<Song>();
		}
	}

	/// <summary>
	/// Retrieves the total count of songs stored in the `Songs` table of the database.
	/// In case of an error during the query execution, this method returns 0.
	/// </summary>
	/// <returns>
	/// A task that represents the asynchronous operation of fetching the song count.
	/// The task result contains the count of songs as an integer, or 0 if an error occurs.
	/// </returns>
	public async Task<int> GetSongsCount()
	{
		try
		{
			return await _database.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Songs");
		}
		catch (Exception)
		{
			return 0;
		}
	}

	public async Task<bool> CheckIfTotalPlayTimeIsAbove1Hour()
	{
		try
		{
			return await _database.ExecuteScalarAsync<bool>(@"SELECT 
																	CASE 
																		WHEN SUM(CASE WHEN PlayCount > 1 THEN playcount * duration * 0.6 ELSE 0 END) > 3600
																		THEN 1
																		ELSE 0
																	END AS condition_met
																FROM Songs");
		}
		catch (Exception)
		{
			return false;
		}
	}

	/// <summary>
	/// Retrieves a song from the database by its file path.
	/// This method queries the Songs table to find a song that matches the provided path.
	/// </summary>
	/// <param name="path">The file path of the song to retrieve from the database.</param>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result contains the matching
	/// <see cref="Song"/> object if found, or null if no match is found or an error occurs.
	/// </returns>
	public async Task<Song?> GetSongByPath(string path)
	{
		try
		{
			var result = await _database.QueryAsync<Song>("SELECT * FROM Songs WHERE Path = ?", path);
			return result.Count() > 0 ? result.FirstOrDefault() : null;
		}
		catch (Exception)
		{
			return null;
		}
	}

	/// <summary>
	/// Retrieves the PlayerType value for a song based on its file path.
	/// This method queries the Songs table and returns the PlayerType associated with the specified path.
	/// </summary>
	/// <param name="path">The file path of the song whose PlayerType is to be retrieved.</param>
	/// <returns>
	/// A task representing the asynchronous operation. The task result contains the PlayerType as a string,
	/// or null if the song is not found or an error occurs.
	/// </returns>
	public async Task<string?> GetPlayerTypeByPath(string path)
	{
		try
		{
			return await _database.ExecuteScalarAsync<string>(
				"SELECT PlayerType FROM Songs WHERE Path = ?", path);
		}
		catch (Exception)
		{
			return null;
		}
	}

	/// <summary>
	/// Filters the provided list of file paths to include only those that exist in the database.
	/// This method checks the `Songs` table in the database for paths matching the given list
	/// and returns only the paths that are found in the database.
	/// </summary>
	/// <param name="paths">A list of file paths to be filtered. The list may contain paths that may or may not exist in the database.</param>
	/// <returns>
	/// A task that represents the asynchronous operation. The result contains a list of file paths that exist in the database.
	/// </returns>
	public async Task<List<string>> FilterExistingSongs(List<string> paths)
	{
		if (paths == null || paths.Count == 0)
			return new List<string>();

		string placeholders = string.Join(", ", paths.Select(_ => "?"));
		string query = $"SELECT Path FROM Songs WHERE Path IN ({placeholders})";

		var existing = await _database.QueryAsync<Song>(query, paths.Cast<object>().ToArray());
		var existingSet = existing.Select(s => s.Path).ToHashSet();

		return paths.Where(p => existingSet.Contains(p)).ToList();
	}

	public static event Action? OnPlayCountUpdated;

	/// <summary>
	/// Increments the play count of a song in the database.
	/// This method updates the `PlayCount` field of the song record
	/// corresponding to the specified file path by adding one to its current value.
	/// </summary>
	/// <param name="songPath">
	/// The file path of the song whose play count should be incremented.
	/// </param>
	/// <returns>
	/// A task that represents the asynchronous operation of updating the play count in the database.
	/// </returns>
	public async Task IncrementPlayCount(string songPath)
	{
		await _database.ExecuteAsync("UPDATE Songs SET PlayCount = PlayCount + 1 WHERE Path = ?", songPath);
		OnPlayCountUpdated?.Invoke();
	}

	/// <summary>
	/// Resets the play count of a specific song to zero in the database.
	/// </summary>
	/// <param name="songPath">The file path of the song whose play count is to be reset.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public async Task ResetPlayCount(string songPath)
	{
		await _database.ExecuteAsync("UPDATE Songs SET PlayCount = 0 WHERE Path = ?", songPath);
	}

	public static event Action? OnDateLastPlayedUpdated;

	/// <summary>
	/// Updates the `DateLastPlayed` field of a song in the database to the current date and time.
	/// This method queries the `Songs` table and modifies the corresponding row with the provided song path.
	/// </summary>
	/// <param name="songPath">
	/// The file path of the song whose `DateLastPlayed` field needs to be updated.
	/// </param>
	/// <returns>
	/// A task that represents the asynchronous operation of updating the `DateLastPlayed` field in the database.
	/// </returns>
	public async Task UpdateDateLastPlayed(string songPath)
	{
		await _database.ExecuteAsync("UPDATE Songs SET DateLastPlayed = ? WHERE Path = ?", DateTime.Now, songPath);
		OnDateLastPlayedUpdated?.Invoke();
	}

	/// <summary>
	/// Resets the date a song was last played by setting the DateLastPlayed field to NULL for the specified song in the database.
	/// </summary>
	/// <param name="songPath">The file path of the song for which the last played date needs to be reset.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	public async Task ResetDateLastPlayed(string songPath)
	{
		await _database.ExecuteAsync("UPDATE Songs SET DateLastPlayed = NULL WHERE Path = ?", songPath);
	}

	/// <summary>
	/// Inserts a new pending tag write entry. Skips silently if the path already exists (enforced by PRIMARY KEY).
	/// </summary>
	/// <param name="path">The file path of the song whose tag write is pending.</param>
	public async Task AddPendingTagWrite(string path, int pendingCover = 0, int pendingTitle = 0, int pendingArtist = 0, int pendingAlbum = 0, int pendingGenre = 0, int pendingYear = 0, int pendingLyrics = 0)
	{
		await _database.ExecuteAsync("INSERT OR REPLACE INTO PendingTagWrites (Path, Cover, Title, Artist, Album, Genre, Year, Lyrics) VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
										path, pendingCover, pendingTitle, pendingArtist, pendingAlbum, pendingGenre, pendingYear, pendingLyrics);
	}

	/// <summary>
	/// Checks if a pending tag write entry exists for the specified file path.
	/// </summary>
	/// <param name="path">The file path to check for pending tag writes.</param>
	/// <param name="attribute">The attribute to check for pending tag writes.</param>
	/// <returns></returns> 
	public async Task<int> PendingTagWritesExist(string path, string attribute)
	{
		return await _database.ExecuteScalarAsync<int>($"SELECT {attribute} FROM PendingTagWrites WHERE Path = ?", path);
	}

	/// <summary>
	/// Retrieves all pending tag write entries from the database.
	/// </summary>
	/// <returns>A list of file paths with pending tag writes.</returns>
	public async Task<List<string>> GetAllPendingTagWrites()
	{
		try
		{
			var rows = await _database.QueryAsync<PendingTagWriteRow>("SELECT Path FROM PendingTagWrites ORDER BY rowid ASC");
			return rows.Select(r => r.Path).ToList();
		}
		catch (Exception)
		{
			return new List<string>();
		}
	}

	/// <summary>
	/// Deletes a pending tag write entry for the specified file path.
	/// </summary>
	/// <param name="path">The file path of the song to remove from pending writes.</param>
	public async Task DeletePendingTagWrite(string path)
	{
		await _database.ExecuteAsync("DELETE FROM PendingTagWrites WHERE Path = ?", path);
	}

	/// <summary>
	/// Creates a new playlist with the specified name and adds it to the database.
	/// </summary>
	/// <param name="playlistName">
	/// The name of the playlist to be created. This name must be unique to avoid duplication in the database.
	/// </param>
	/// <returns>
	/// A task that represents the asynchronous operation of inserting the playlist into the database.
	/// </returns>
	public async Task CreatePlaylist(string playlistName)
	{
		await _database.ExecuteAsync("INSERT INTO Playlists (Name) VALUES (?)", playlistName);
	}

	/// <summary>
	/// Retrieves a list of all playlist names stored in the database, ordered by their associated IDs in ascending order.
	/// This method queries the `Playlists` table and extracts the names of all available playlists.
	/// </summary>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result contains a list of strings representing the playlist names.
	/// </returns>
	public async Task<List<string>> GetAllPlaylistNames()
	{
		var result = await _database.QueryAsync<PlaylistName>("SELECT * FROM Playlists ORDER BY Id ASC");
		return result.Select(p => p.Name).ToList();
	}

	/// <summary>
	/// Removes a playlist and all its associated song entries from the database based on the specified playlist name.
	/// This method ensures that both the playlist record and its related entries in the `PlaylistSongs` table are deleted.
	/// </summary>
	/// <param name="playlistName">The name of the playlist to be removed.</param>
	/// <returns>
	/// A task representing the asynchronous operation of removing the playlist and its associated data.
	/// </returns>
	public async Task RemovePlaylist(string playlistName)
	{
		await _database.ExecuteAsync("DELETE FROM Playlists WHERE Name = ?", playlistName);
	}

	/// <summary>
	/// Renames an existing playlist in the database by updating its name.
	/// </summary>
	/// <param name="oldName">The current name of the playlist to be renamed.</param>
	/// <param name="newName">The new name to assign to the playlist.</param>
	/// <returns>
	/// A task that represents the asynchronous operation of renaming the playlist.
	/// </returns>
	public async Task RenamePlaylist(string oldName, string newName)
	{
		await _database.ExecuteAsync("UPDATE Playlists SET Name = ? WHERE Name = ?", newName, oldName);
	}

	/// <summary>
	/// Adds a song to the specified playlist in the database.
	/// If the song already exists in the playlist, this method will ignore the duplicate entry.
	/// The method assigns the next available position within the playlist for the song.
	/// </summary>
	/// <param name="playlistName">The name of the playlist to which the song should be added.</param>
	/// <param name="songPath">The file path of the song to be added to the playlist.</param>
	/// <returns>
	/// A task that represents the asynchronous operation of adding the song to the playlist.
	/// </returns>
	public async Task AddSongToPlaylist(string playlistName, string songPath)
	{
		int nextPosition = await GetNextPlaylistPosition(playlistName);
		await _database.ExecuteAsync(@"INSERT OR IGNORE INTO PlaylistSongs (PlaylistId, SongPath, Position)
									   VALUES ((SELECT Id FROM Playlists WHERE Name = ?), ?, ?)", playlistName, songPath, nextPosition);
	}

	/// <summary>
	/// Adds a list of songs to the specified playlist in the database.
	/// This method inserts song paths into the `PlaylistSongs` table, associating them with the playlist
	/// name provided. Songs are added starting at the next available position within the playlist.
	/// </summary>
	/// <param name="playlistName">The name of the playlist to which the songs will be added.</param>
	/// <param name="songPaths">A list of file paths for the songs to be added to the playlist.</param>
	/// <returns>
	/// A task representing the asynchronous operation of adding songs to the playlist.
	/// </returns>
	public async Task AddSongsToPlaylist(string playlistName, List<string> songPaths)
	{
		int basePosition = await GetNextPlaylistPosition(playlistName);

		await _database.RunInTransactionAsync(conn =>
		{
			int position = basePosition;
			foreach (var songPath in songPaths)
			{
				conn.Execute(@"INSERT OR IGNORE INTO PlaylistSongs (PlaylistId, SongPath, Position)
							   VALUES ((SELECT Id FROM Playlists WHERE Name = ?), ?, ?)", playlistName, songPath, position++);
			}
		});
	}

	/// <summary>
	/// Retrieves the next available position number for a song in the specified playlist.
	/// This method determines the maximum current position in the playlist and increments it by one.
	/// It is used to assign a position to a new song being added to the playlist.
	/// </summary>
	/// <param name="playlistName">
	/// The name of the playlist for which the next position number is being calculated.
	/// </param>
	/// <returns>
	/// A task representing the asynchronous operation, containing the next available position number in the playlist.
	/// </returns>
	private async Task<int> GetNextPlaylistPosition(string playlistName)
	{
		return await _database.ExecuteScalarAsync<int>(@"SELECT COALESCE(MAX(Position), -1) + 1
														 FROM PlaylistSongs
														 WHERE PlaylistId = (SELECT Id FROM Playlists WHERE Name = ?)", playlistName);
	}

	/// <summary>
	/// Removes multiple songs from a specified playlist in the database.
	/// This method deletes entries from the `PlaylistSongs` table where the provided song paths
	/// are associated with the given playlist.
	/// </summary>
	/// <param name="playlistName">
	/// The name of the playlist from which the songs will be removed.
	/// </param>
	/// <param name="songPaths">
	/// A list of file paths representing the songs to be removed from the playlist.
	/// </param>
	/// <returns>
	/// A task that represents the asynchronous operation of removing songs from the playlist.
	/// </returns>
	public async Task RemoveSongsFromPlaylist(string playlistName, List<string> songPaths)
	{
		await _database.RunInTransactionAsync(conn =>
		{
			foreach (var songPath in songPaths)
			{
				conn.Execute("DELETE FROM PlaylistSongs WHERE PlaylistId IN (SELECT Id FROM Playlists WHERE Name = ?) AND SongPath = ?", playlistName, songPath);
			}
		});
	}

	/// <summary>
	/// Retrieves all songs associated with a given playlist, ordered by their position within the playlist.
	/// </summary>
	/// <param name="playlistName">
	/// The name of the playlist whose songs are to be retrieved.
	/// </param>
	/// <returns>
	/// A task representing the asynchronous operation, with a list of <see cref="Song"/> objects that belong to the specified playlist.
	/// </returns>
	public async Task<List<Song>> GetSongsInPlaylist(string playlistName)
	{
		return await _database.QueryAsync<Song>(@"SELECT S.* FROM Songs S
												  JOIN PlaylistSongs P ON S.Path = P.SongPath
												  WHERE P.PlaylistId = (SELECT Id FROM Playlists WHERE Name = ?)
												  ORDER BY P.Position ASC", playlistName);
	}

	/// <summary>
	/// Sorts the songs in a specified playlist based on the given column and sorting order.
	/// Updates the `Position` of each song in the playlist to reflect the new order.
	/// </summary>
	/// <param name="playlistName">
	/// The name of the playlist whose songs need to be sorted.
	/// </param>
	/// <param name="orderByColumn">
	/// The column by which the playlist songs should be sorted (e.g., Title, Artists, Album).
	/// </param>
	/// <param name="ascending">
	/// Indicates whether the sorting should be in ascending (`true`) or descending (`false`) order.
	/// </param>
	/// <returns>
	/// A task that represents the asynchronous operation of sorting songs in the playlist.
	/// </returns>
	public async Task SortPlaylistSongs(string playlistName, SongProperty orderByColumn, bool ascending)
	{
		var songPaths = (await _database.QueryAsync<Song>($@"SELECT S.Path FROM Songs S
															 JOIN PlaylistSongs P ON S.Path = P.SongPath
															 WHERE P.PlaylistId = (SELECT Id FROM Playlists WHERE Name = ?)
															 ORDER BY S.{orderByColumn.ToString()} {(ascending ? "ASC" : "DESC")}", playlistName)).Select(s => s.Path).ToList();

		await _database.RunInTransactionAsync(conn =>
		{
			int position = 0;
			foreach (var path in songPaths)
			{
				conn.Execute(@"UPDATE PlaylistSongs
							   SET Position = ?
							   WHERE PlaylistId = (SELECT Id FROM Playlists WHERE Name = ?)
							   AND SongPath = ?", position++, playlistName, path);
			}
		});
	}

	/// <summary>
	/// Adds a list of songs to the 'QueuedPlayingList' table in the database.
	/// Each song is inserted into the queue with a unique position. If duplicate entries are not allowed,
	/// it ensures songs are not repeated within the queue.
	/// </summary>
	/// <param name="songPaths">
	/// A list of file paths representing the songs to be added to the queued playing list.
	/// </param>
	/// <returns>
	/// A task that represents the asynchronous operation of adding the provided songs to the queue.
	/// </returns>
	public async Task AddSongsToQueuedPlayingList(List<string> songPaths)
	{
		int basePosition = await GetNextQueuePosition();

		await _database.RunInTransactionAsync(conn =>
		{
			int position = basePosition;
			bool duplicateAllowed = bool.Parse(Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.duplicateQueueAllowed)]?.ToString() ?? "false");

			foreach (var songPath in songPaths)
			{
				if (!duplicateAllowed)
				{
					var exists = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM QueuedPlayingList WHERE Path = ?", songPath);
					if (exists > 0) continue;
				}

				conn.Execute(@"INSERT INTO QueuedPlayingList (Path, Position) VALUES (?, ?)", songPath, position++);
			}
		});
		MusicControl._instance?.ViewModel.CurrentSongInfoForUpdateOverlay();
	}

	/// <summary>
	/// Retrieves the list of songs from the queued playing list, preserving the order
	/// in which they are meant to be played.
	/// The queue is managed within the `QueuedPlayingList` table, which references
	/// the song information stored in the `Songs` table.
	/// This method ensures that the items in the queue are returned in ascending order
	/// based on their respective positions.
	/// </summary>
	/// <returns>
	/// A task representing the asynchronous operation of retrieving the queued playing list.
	/// The result contains a list of `Song` objects representing the queued songs in their
	/// respective order.
	/// </returns>
	public async Task<List<Song>> GetQueuedPlayingList()
	{
		return await _database.QueryAsync<Song>(@"SELECT S.* FROM Songs S
												  JOIN QueuedPlayingList Q ON S.Path = Q.Path
												  ORDER BY Q.Position ASC");
	}

	/// <summary>
	/// Retrieves the next available position in the queue for the `QueuedPlayingList` table.
	/// This method calculates the maximum position value currently present in the table,
	/// increments it by one, and returns the result.
	/// </summary>
	/// <returns>
	/// A task representing the asynchronous operation that returns the next queue position as an integer.
	/// </returns>
	private async Task<int> GetNextQueuePosition()
	{
		return await _database.ExecuteScalarAsync<int>("SELECT COALESCE(MAX(Position), -1) + 1 FROM QueuedPlayingList");
	}

	/// <summary>
	/// Reorders the queued playing list based on the provided sequence of paths.
	/// Updates the positions of the items in the queue to match the new order.
	/// </summary>
	/// <param name="orderedPaths">A list of song paths that defines the new order of the queue. Each path corresponds to a song in the queue.</param>
	/// <returns>
	/// A task that represents the asynchronous operation of reordering the queued playing list.
	/// </returns>
	public async Task ReorderQueue(List<string> orderedPaths)
	{
		await _database.RunInTransactionAsync(conn =>
		{
			int position = 0;
			foreach (var path in orderedPaths)
			{
				conn.Execute(@"UPDATE QueuedPlayingList SET Position = ?
							   WHERE Id = (SELECT Id FROM QueuedPlayingList WHERE Path = ? ORDER BY Position ASC LIMIT 1)", position++, path);
			}
		});
	}

	/// <summary>
	/// Moves the specified item in the `QueuedPlayingList` table one position up in the queue.
	/// If the item is already at the top of the queue, no changes are made.
	/// </summary>
	/// <param name="path">The path of the song to be moved up in the queue.</param>
	/// <returns>
	/// A task that represents the asynchronous operation of moving the queue item up.
	/// </returns>
	public async Task MoveQueueItemUp(string path)
	{
		var currentRow = await _database.QueryAsync<(int Id, int Position)>("SELECT Id, Position FROM QueuedPlayingList WHERE Path = ? ORDER BY Position ASC LIMIT 1", path);

		if (currentRow.Count == 0) return;

		var current = currentRow[0];

		var aboveRow = await _database.QueryAsync<(int Id, int Position)>("SELECT Id, Position FROM QueuedPlayingList WHERE Position < ? ORDER BY Position DESC LIMIT 1", current.Position);

		if (aboveRow.Count == 0) return;

		var above = aboveRow[0];

		await _database.RunInTransactionAsync(conn =>
		{
			conn.Execute("UPDATE QueuedPlayingList SET Position = ? WHERE Id = ?", current.Position, above.Id);
			conn.Execute("UPDATE QueuedPlayingList SET Position = ? WHERE Id = ?", above.Position, current.Id);
		});
	}

	/// <summary>
	/// Moves a queued item in the `QueuedPlayingList` table down by one position.
	/// This method swaps the current item's position with the next item in the queue,
	/// ensuring they maintain their relative order.
	/// </summary>
	/// <param name="path">The unique file path of the queued item to be moved down in the list.</param>
	/// <returns>
	/// A task that represents the asynchronous operation of moving the item down in the queue.
	/// If the item is already at the bottom of the queue or not found, no changes are made.
	/// </returns>
	public async Task MoveQueueItemDown(string path)
	{
		var currentRow = await _database.QueryAsync<(int Id, int Position)>("SELECT Id, Position FROM QueuedPlayingList WHERE Path = ? ORDER BY Position ASC LIMIT 1", path);

		if (currentRow.Count == 0) return;
		var current = currentRow[0];

		var belowRow = await _database.QueryAsync<(int Id, int Position)>("SELECT Id, Position FROM QueuedPlayingList WHERE Position > ? ORDER BY Position ASC LIMIT 1", current.Position);

		if (belowRow.Count == 0) return;
		var below = belowRow[0];

		await _database.RunInTransactionAsync(conn =>
		{
			conn.Execute("UPDATE QueuedPlayingList SET Position = ? WHERE Id = ?", current.Position, below.Id);
			conn.Execute("UPDATE QueuedPlayingList SET Position = ? WHERE Id = ?", below.Position, current.Id);
		});
	}

	/// <summary>
	/// Moves a song in the queue to the top of the queued playing list.
	/// This operation identifies the song by its path, finds the current position, and adjusts it
	/// to ensure that the song appears at the top of the queue, preceding all other songs.
	/// </summary>
	/// <param name="path">
	/// The file path of the song to be moved to the top.
	/// </param>
	/// <returns>
	/// A task that represents the asynchronous operation of moving the song to the top of the queued playing list.
	/// </returns>
	public async Task MoveQueueItemToTop(string path)
	{
		var currentRow = await _database.QueryAsync<(int Id, int Position)>("SELECT Id, Position FROM QueuedPlayingList WHERE Path = ? ORDER BY Position ASC LIMIT 1", path);

		if (currentRow.Count == 0) return;
		var current = currentRow[0];

		var minPosition = await _database.ExecuteScalarAsync<int>("SELECT MIN(Position) FROM QueuedPlayingList");

		if (current.Position == minPosition) return;

		await _database.ExecuteAsync("UPDATE QueuedPlayingList SET Position = ? WHERE Id = ?", minPosition - 1, current.Id);
	}

	/// <summary>
	/// Moves a specific song in the queued playing list to the bottom position.
	/// This method updates the position of the song identified by its path in the `QueuedPlayingList` table.
	/// </summary>
	/// <param name="path">
	/// The file path of the song to be moved to the bottom of the queue.
	/// </param>
	/// <returns>
	/// A task representing the asynchronous operation of repositioning the song in the queue.
	/// </returns>
	public async Task MoveQueueItemToBottom(string path)
	{
		var currentRow = await _database.QueryAsync<(int Id, int Position)>("SELECT Id, Position FROM QueuedPlayingList WHERE Path = ? ORDER BY Position ASC LIMIT 1", path);

		if (currentRow.Count == 0) return;
		var current = currentRow[0];

		var maxPosition = await _database.ExecuteScalarAsync<int>("SELECT MAX(Position) FROM QueuedPlayingList");

		await _database.ExecuteAsync("UPDATE QueuedPlayingList SET Position = ? WHERE Id = ?", maxPosition + 1, current.Id);
	}

	/// <summary>
	/// Removes a song from the queued playing list. If a specific song path is provided, the first occurrence
	/// of that song in the queue, based on position, will be removed. If no path is specified, the first song
	/// in the queue will be removed.
	/// </summary>
	/// <param name="path">
	/// The file path of the song to remove from the queue. If null, the first song in the queue will be cleared.
	/// </param>
	/// <returns>
	/// A task that represents the asynchronous operation of removing a song from the queued playing list.
	/// </returns>
	public async Task ClearFromQueue(string? path = null)
	{
		if (!string.IsNullOrEmpty(path))
		{
			await _database.ExecuteAsync(@"DELETE FROM QueuedPlayingList
										   WHERE Id = (SELECT Id FROM QueuedPlayingList WHERE Path = ? ORDER BY Position ASC LIMIT 1)", path);
		}
		else
		{
			var firstId = await _database.ExecuteScalarAsync<int?>("SELECT Id FROM QueuedPlayingList ORDER BY Position ASC LIMIT 1");

			if (firstId.HasValue)
				await _database.ExecuteAsync("DELETE FROM QueuedPlayingList WHERE Id = ?", firstId.Value);
		}
	}

	/// <summary>
	/// Retrieves a list of songs grouped by year from the database.
	/// The query counts the number of songs, sums their total duration,
	/// and categorizes them by year. For records where the year is null or empty,
	/// they are categorized under 'Unknown'.
	/// </summary>
	/// <returns>
	/// A task representing the asynchronous operation that returns a list of
	/// <see cref="YearModel"/> objects, where each object contains the year, a count
	/// of songs for that year, and their total duration.
	/// </returns>
	public async Task<List<YearModel>> GetSongsGroupedByYear(bool ascending = true)
	{
		var result = await _database.QueryAsync<YearModel>(@$"SELECT CASE WHEN TRIM(Year) = 'Unknown Year' THEN 'Unknown' ELSE Year END AS Year, COUNT(*) AS Count, SUM(Duration) AS TotalDuration
															  FROM Songs
															  WHERE Year IS NOT NULL AND TRIM(Year) != ''
															  GROUP BY Year
															  ORDER BY Year {(ascending ? "ASC" : "DESC")}");

		return result.ToList();
	}

	/// <summary>
	/// Retrieves a list of songs grouped by their genres from the database.
	/// The method aggregates songs based on their genre, calculates the total count
	/// and total duration for each genre, and groups them accordingly.
	/// Genres with the value 'Unknown Genre' are handled as 'Unknown'.
	/// </summary>
	/// <param name="ascending">A boolean indicating whether the genres should be ordered alphabetically in ascending order. Defaults to true.</param>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result contains
	/// a list of <see cref="GenreModel"/> objects where each object represents a genre,
	/// its count, and the total duration of songs in that genre.
	/// </returns>
	public async Task<List<GenreModel>> GetSongsGroupedByGenre(bool ascending = true)
	{
		var result = await _database.QueryAsync<GenreModel>(@$"SELECT CASE WHEN TRIM(Genre) = 'Unknown Genre' THEN 'Unknown' ELSE Genre END AS Genre, COUNT(*) AS Count, SUM(Duration) AS TotalDuration
															   FROM Songs
															   WHERE Genre IS NOT NULL AND TRIM(Genre) != ''
															   GROUP BY Genre
															   ORDER BY Genre {(ascending ? "ASC" : "DESC")}");

		return result.ToList();
	}

	public async Task<List<AlbumModel>> GetSongsGroupedByAlbum(bool ascending = true)
	{
		var result = await _database.QueryAsync<AlbumModel>(@$"SELECT CASE WHEN TRIM(Album) = 'Unknown Album' THEN 'Unknown' ELSE Album END AS Album, COUNT(*) AS Count, SUM(Duration) AS TotalDuration, Cover
															   FROM Songs
															   WHERE Album IS NOT NULL AND TRIM(Album) != ''
															   GROUP BY Album
															   ORDER BY Album {(ascending ? "ASC" : "DESC")}");
		return result.ToList();
	}

	/// <summary>
	/// A private regex variable used to split artist names from a given string.
	/// It defines patterns for common separators such as ",", "&amp;", "and", "feat.", "ft.", "x", etc.,
	/// ensuring case-insensitivity and cultural variance while using a compiled regex for optimal performance.
	/// </summary>
	private Regex _artistSplitRegex = new(@"\s*(?:,|&|\band\b|\bfeat\.?\b|\bft\.?\b|\bx\b)\s*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

	/// <summary>
	/// A private field used to store a collection of active exception patterns for artist name splitting.
	/// This field ensures that certain artist names specified as exceptions are not split
	/// incorrectly during processing. It is initialized and updated based on database records.
	/// </summary>
	private HashSet<string> _activeExceptionNames = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Ensures that default artist splitting rules are present in the database.
	/// These rules consist of predefined patterns and configurations to handle
	/// common artist name separations (e.g., separators like "&amp;", "and", "feat.").
	/// If these rules do not already exist, they will be inserted as built-in entries.
	/// Additionally, existing rules matching the default configurations will be updated
	/// to mark them as built-in.
	/// </summary>
	/// <returns>
	/// A task representing the asynchronous operation to ensure default artist splitting rules.
	/// </returns>
	private async Task EnsureDefaultArtistRules()
	{
		var defaults = new (string Pattern, bool IsRegex)[]
		{
			(",", false),
			("&", false),
			(@"\band\b", true),
			(@"\bfeat\.?\s+", true),
			(@"\bft\.?\s+", true),
			(@"\bx\b", true),
		};

		await _database.RunInTransactionAsync(conn =>
		{
			conn.Execute(@"DELETE from ArtistSplitRules where Pattern in ('\bfeat\.?\b', '\bft\.?\b') and IsBuiltIn = 1");

			foreach (var d in defaults)
			{
				conn.Execute(@"INSERT OR IGNORE INTO ArtistSplitRules (Type, Pattern, IsRegex, Active, IsBuiltIn) VALUES ('Splitter', ?, ?, 1, 1)", d.Pattern, d.IsRegex ? 1 : 0);

				conn.Execute(@"UPDATE ArtistSplitRules SET IsBuiltIn = 1 WHERE Type = 'Splitter' AND Pattern = ? AND IsRegex = ?", d.Pattern, d.IsRegex ? 1 : 0);
			}
		});
	}

	/// <summary>
	/// Reloads and updates the artist split rules and exception patterns used for parsing artist names.
	/// This method retrieves the active rules from the database, compiles them into a regular expression
	/// pattern for splitting artist names, and updates the set of exception names.
	/// </summary>
	/// <returns>
	/// A task that represents the asynchronous operation of reloading the artist split rules and exception patterns.
	/// </returns>
	private async Task ReloadArtistSplitRules()
	{
		var splitters = await _database.QueryAsync<ArtistSplitRule>("SELECT Pattern, IsRegex FROM ArtistSplitRules WHERE Active = 1 AND Type = 'Splitter'");

		var parts = new List<string>();
		foreach (var r in splitters)
			parts.Add(r.IsRegex ? r.Pattern : Regex.Escape(r.Pattern));

		string pattern = parts.Count > 0 ? @"\s*(?:" + string.Join("|", parts) + @")\s*" : @"\s*(?:,|&|\band\b|\bfeat\.?\b|\bft\.?\b|\bx\b)\s*";

		_artistSplitRegex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		var exceptions = await _database.QueryAsync<ArtistSplitRule>("SELECT Pattern FROM ArtistSplitRules WHERE Active = 1 AND Type = 'Exception'");

		_activeExceptionNames = new HashSet<string>(exceptions.Select(e => e.Pattern).Where(p => !string.IsNullOrWhiteSpace(p)), StringComparer.OrdinalIgnoreCase);
	}


	/// <summary>
	/// Normalizes an artist's name by applying standard formatting rules, such as trimming leading and trailing
	/// whitespace or special characters and reducing multiple spaces to a single space.
	/// </summary>
	/// <param name="s">The raw artist name string to be normalized.</param>
	/// <returns>The normalized artist name as a string.</returns>
	private static string NormalizeArtist(string s)
	{
		if (string.IsNullOrWhiteSpace(s)) return string.Empty;
		s = s.Trim(' ', '"', '“', '”', '\'');
		s = Regex.Replace(s, @"\s+", " ");
		return s;
	}

	/// <summary>
	/// Splits a string of artist names into individual normalized artist names based on common delimiters
	/// such as commas, "and", "feat", "ft", "&amp;", and "x". Supports handling artist exceptions to preserve
	/// specific sequences of names as a single unit.
	/// </summary>
	/// <param name="artistsField">The string containing the names of artists to be split.</param>
	/// <returns>
	/// An enumerable collection of normalized artist names. Artist names considered "Unknown" or
	/// "Unknown Artist" are standardized to "Unknown Artist."
	/// </returns>
	private IEnumerable<string> SplitArtists(string artistsField)
	{
		if (string.IsNullOrWhiteSpace(artistsField))
			yield break;

		string source = artistsField;
		var placeholderMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		int idx = 0;

		foreach (var exc in _activeExceptionNames)
		{
			if (string.IsNullOrWhiteSpace(exc)) continue;
			string placeholder = $"§§EXC{idx++}§§";

			source = Regex.Replace(source, Regex.Escape(exc), placeholder, RegexOptions.IgnoreCase);
			placeholderMap[placeholder] = exc;
		}

		foreach (var raw in _artistSplitRegex.Split(source))
		{
			var token = NormalizeArtist(raw);
			if (token.Length == 0) continue;

			if (placeholderMap.TryGetValue(token, out var original))
				token = original;

			if (token.Equals("Unknown", StringComparison.OrdinalIgnoreCase) || token.Equals("Unknown Artist", StringComparison.OrdinalIgnoreCase))
			{
				yield return "Unknown Artist";
			}
			else
			{
				yield return token;
			}
		}
	}

	/// <summary>
	/// Synchronizes the linkage between a song and its associated artists in the database.
	/// This method ensures the `SongArtists` table accurately reflects the relationships between a specified song and its artists.
	/// If the artists do not already exist in the `Artists` table, they are added.
	/// </summary>
	/// <param name="conn">
	/// The SQLite database connection used to execute commands.
	/// </param>
	/// <param name="songPath">
	/// The file path of the song whose artist associations are to be synchronized.
	/// </param>
	/// <param name="artistsField">
	/// A string containing the names of the artists associated with the song, typically separated by delimiters.
	/// </param>
	private void SyncSongArtistsForSong(SQLiteConnection conn, string songPath, string artistsField)
	{
		conn.Execute("DELETE FROM SongArtists WHERE SongPath = ?", songPath);

		var artistNames = SplitArtists(artistsField).ToList();
		if (artistNames.Count == 0) return;

		foreach (var name in artistNames)
		{
			conn.Execute("INSERT OR IGNORE INTO Artists (Name) VALUES (?)", name);
			conn.Execute(@"INSERT OR IGNORE INTO SongArtists (SongPath, ArtistId) SELECT ?, Id FROM Artists WHERE Name = ? COLLATE NOCASE", songPath, name);
		}
	}

	/// <summary>
	/// Synchronizes the artist data for a specific song with the corresponding entries in the database.
	/// This method updates the 'Artists' and 'SongArtists' tables based on the artist field of the given song.
	/// Ensures that individual artists are split, added to the database, and mapped correctly to the provided song.
	/// </summary>
	/// <param name="conn">The SQLite connection used for database operations.</param>
	/// <param name="song">The song object containing the details, including the artist information to sync.</param>
	private void SyncSongArtistsForSong(SQLiteConnection conn, Song song) => SyncSongArtistsForSong(conn, song.Path, song.Artists);

	/// <summary>
	/// Ensures that the SongArtists table is populated with relationships between songs and their associated artists.
	/// If the table already contains data, this method exits without performing any further operations.
	/// Otherwise, it queries the Songs table for entries with non-empty artist metadata and populates the necessary links.
	/// This operation is executed within a database transaction to ensure data consistency.
	/// </summary>
	/// <returns>
	/// A task that represents the asynchronous operation of populating artist links.
	/// </returns>
	private async Task EnsureArtistLinksPopulated()
	{
		var linkCount = await _database.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM SongArtists");
		if (linkCount > 0) return;

		var songs = await _database.QueryAsync<(string Path, string Artists)>("SELECT Path, Artists FROM Songs WHERE Artists IS NOT NULL AND TRIM(Artists) != ''");

		await _database.RunInTransactionAsync(conn =>
		{
			foreach (var s in songs)
				SyncSongArtistsForSong(conn, s.Path, s.Artists);
		});
	}

	/// <summary>
	/// Removes any artist entries from the database that are no longer linked to any songs or related data.
	/// This operation ensures that the `Artists` table and its related full-text search table
	/// only contain artists currently associated with existing songs, maintaining database consistency and optimization.
	/// </summary>
	/// <returns>
	/// A task that represents the asynchronous operation of pruning unused artist entries
	/// from the `Artists` table and its associated full-text search table.
	/// </returns>
	private async Task PruneUnusedArtists()
	{
		await _database.ExecuteAsync(@"DELETE FROM Artists WHERE Id NOT IN (SELECT DISTINCT ArtistId FROM SongArtists)");
		try
		{
			await _database.ExecuteAsync(@"DELETE FROM ArtistFTS WHERE Name NOT IN (SELECT Name FROM Artists)");
		}
		catch (Exception)
		{
			//ignored
		}
	}

	/// <summary>
	/// Retrieves a list of artists, each grouped with the number of songs they are associated with and the total duration of those songs.
	/// The results are ordered by the artist's name in either ascending or descending order.
	/// </summary>
	/// <param name="ascending">
	/// A boolean indicating the order of the artist names.
	/// Set to true for ascending order, or false for descending order.
	/// </param>
	/// <returns>
	/// A task that represents the asynchronous operation.
	/// The task result contains a list of <see cref="ArtistModel"/> objects,
	/// where each object includes the artist's name, the count of their associated songs, and the total duration of their songs.
	/// </returns>
	public async Task<List<ArtistModel>> GetSongsGroupedByArtist(bool ascending = true)
	{
		var result = await _database.QueryAsync<ArtistModel>($@"SELECT CASE WHEN TRIM(A.Name) = 'Unknown Artist' THEN 'Unknown' ELSE A.Name END AS Artist, COUNT(*) AS Count, SUM(S.Duration) AS TotalDuration
																FROM Artists A
																JOIN SongArtists SA ON SA.ArtistId = A.Id
																JOIN Songs S ON S.Path = SA.SongPath
																GROUP BY A.Name
																ORDER BY A.Name {(ascending ? "ASC" : "DESC")}");
		return result.ToList();
	}

	/// <summary>
	/// Retrieves a list of songs associated with a specific artist from the database.
	/// The result can be ordered by a specified song property, either in ascending or descending order.
	/// </summary>
	/// <param name="artistName">The name of the artist whose songs are to be retrieved.</param>
	/// <param name="orderBy">The property by which the results should be ordered. Defaults to sorting by the song title.</param>
	/// <param name="ascending">Specifies whether the results should be sorted in ascending order. Defaults to true if not specified.</param>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result contains a list of songs matching the specified artist and order criteria.
	/// </returns>
	public async Task<List<Song>> GetSongsByArtist(string artistName, SongProperty orderBy = SongProperty.Title, bool ascending = true)
	{
		return await GetSongsByArtists(new[] { artistName }, orderBy, ascending, matchAll: false);
	}

	/// <summary>
	/// Retrieves a list of songs associated with the specified artist names.
	/// Supports ordering, sorting direction, and matching either any or all artists.
	/// </summary>
	/// <param name="artistNames">
	/// An enumerable collection of artist names to filter songs by. Names are normalized and deduplicated.
	/// </param>
	/// <param name="orderBy">
	/// The property by which to order the results. Defaults to <see cref="SongProperty.Title"/>.
	/// </param>
	/// <param name="ascending">
	/// If true, sorts results in ascending order; otherwise, sorts in descending order.
	/// </param>
	/// <param name="matchAll">
	/// If true, only returns songs that match all provided artist names; if false, returns songs matching any of the names.
	/// </param>
	/// <returns>
	/// A task representing the asynchronous operation. The result contains a list of <see cref="Song"/> objects matching the criteria.
	/// </returns>
	public async Task<List<Song>> GetSongsByArtists(IEnumerable<string> artistNames, SongProperty orderBy = SongProperty.Title, bool ascending = true, bool matchAll = false)
	{
		var names = (artistNames ?? Enumerable.Empty<string>()).Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

		if (names.Count == 0)
			return new List<Song>();

		var placeholders = string.Join(", ", Enumerable.Repeat("?", names.Count));

		if (!matchAll)
		{
			var sql = $@"SELECT DISTINCT S.*
						 FROM Songs S
						 JOIN SongArtists SA ON SA.SongPath = S.Path
						 JOIN Artists A ON A.Id = SA.ArtistId
						 WHERE A.Name IN ({placeholders}) COLLATE NOCASE
						 ORDER BY S.{orderBy} {(ascending ? "ASC" : "DESC")}";

			var args = names.Select(n => (object)n.ToLowerInvariant()).ToArray();
			return await _database.QueryAsync<Song>(sql, args);
		}
		else
		{
			var sql = $@"SELECT S.*
						 FROM Songs S
						 JOIN SongArtists SA ON SA.SongPath = S.Path
						 JOIN Artists A ON A.Id = SA.ArtistId
						 WHERE A.Name IN ({placeholders}) COLLATE NOCASE
						 GROUP BY S.Path
						 HAVING COUNT(DISTINCT A.Id) = ?
						 ORDER BY S.{orderBy} {(ascending ? "ASC" : "DESC")}";

			var argsList = names.Select(n => (object)n.ToLowerInvariant()).ToList();
			argsList.Add(names.Count);
			return await _database.QueryAsync<Song>(sql, argsList.ToArray());
		}
	}

	/// <summary>
	/// Inserts or updates the metadata for a given artist in the database.
	/// If the artist does not exist, a new entry is created with the provided name.
	/// If the artist already exists, the image URL and description fields are updated.
	/// </summary>
	/// <param name="artistName">
	/// The name of the artist whose metadata is being inserted or updated. This value serves as the unique identifier for the artist.
	/// </param>
	/// <param name="imagePath">
	/// The URL of the artist's image. If null, no update will be made to the image URL.
	/// </param>
	/// <param name="description">
	/// A textual description of the artist. If null, no update will be made to the description field.
	/// </param>
	/// <returns>
	/// A task representing the asynchronous operation of upserting the artist metadata.
	/// </returns>
	public async Task UpsertArtistMetadata(string artistName, string? imagePath, string? description)
	{
		await _database.ExecuteAsync("INSERT OR IGNORE INTO Artists (Name) VALUES (?)", artistName);
		await _database.ExecuteAsync("UPDATE Artists SET ArtistImage = ?, ArtistDescription = ? WHERE Name = ? COLLATE NOCASE", imagePath, description, artistName);
	}

	/// <summary>
	/// Retrieves a list of artist split rules from the database. Artist split rules are used to define how metadata
	/// related to artists (e.g., collaborations, remixes) should be handled or split based on specified patterns.
	/// The method supports filtering by active or inactive rules, depending on the parameter provided.
	/// </summary>
	/// <param name="includeInactive">
	/// If true, includes both active and inactive artist split rules in the returned list.
	/// If false, only active artist split rules are included.
	/// </param>
	/// <returns>
	/// A task that represents the asynchronous operation. The result is a list of
	/// <see cref="ArtistSplitRule"/> objects containing the retrieved artist split rules.
	/// </returns>
	public async Task<List<ArtistSplitRule>> GetArtistSplitRules(bool includeInactive = true)
	{
		if (includeInactive)
			return await _database.Table<ArtistSplitRule>().OrderBy(r => r.Id).ToListAsync();

		return await _database.QueryAsync<ArtistSplitRule>("SELECT * FROM ArtistSplitRules WHERE Active = 1 ORDER BY Id ASC");
	}

	/// <summary>
	/// Determines whether the provided string contains special characters commonly associated with regular expressions.
	/// </summary>
	/// <param name="pattern">The input string to analyze for regular expression indicators.</param>
	/// <returns>
	/// A boolean value indicating whether the input string likely represents a regular expression.
	/// Returns true if the string contains special regex characters; otherwise, false.
	/// </returns>
	private static bool IsProbablyRegex(string pattern)
	{
		if (string.IsNullOrWhiteSpace(pattern)) return false;

		return pattern.IndexOfAny(new[] { '\\', '.', '?', '*', '+', '^', '$', '|', '(', ')', '[', ']', '{', '}' }) >= 0;
	}


	/// <summary>
	/// Ensures that a provided regex pattern is properly wrapped with word boundaries (`\b`) only when applicable.
	/// This method validates the pattern for existing anchors, boundaries or special characters and avoids
	/// redundant or incorrect boundary additions.
	/// </summary>
	/// <param name="pattern">The regex pattern to evaluate and optionally modify with word boundaries.</param>
	/// <returns>
	/// A string containing the updated pattern with word boundaries if applicable, or the original pattern
	/// if no modification is needed.
	/// <br/>
	/// Examples:
	/// <br/>
	/// - "and"     -> "\band\b"
	/// <br/>
	/// - "feat\.?" -> "\bfeat\.?\b"
	/// <br/>
	/// - "\band\b" -> stays as-is
	/// <br/>
	/// - "(?i)and" -> stays as-is (already uses flags/anchors)
	/// <br/>
	/// - "x"       -> "\bx\b"
	/// </returns>
	private static string EnsureWordBoundaries(string pattern)
	{
		if (string.IsNullOrWhiteSpace(pattern)) return pattern;

		if (Regex.IsMatch(pattern, @"(^\^)|(\$$)|\\b|\\B|\(\?[imnsux\-]"))
			return pattern;

		bool startsWordChar = Regex.IsMatch(pattern, @"^\w");
		bool endsWordChar = Regex.IsMatch(pattern, @"\w$");

		if (!startsWordChar && !endsWordChar)
			return pattern;
		if (startsWordChar && endsWordChar)
			return $@"\b{pattern}\b";
		if (startsWordChar && !endsWordChar)
			return $@"\b{pattern}";
		if (!startsWordChar && endsWordChar)
			return $@"{pattern}\b";

		return pattern;
	}

	/// <summary>
	/// Adds a new artist rule or enables an existing one in the database.
	/// The rule defines how artists should be split or treated as exceptions based on the given pattern.
	/// If the rule already exists but is inactive, it will be activated.
	/// The method determines if the pattern should be treated as a regex or plain text based on provided overrides or by analyzing the pattern.
	/// </summary>
	/// <param name="type">The type of the artist rule, either "Splitter" or "Exception".</param>
	/// <param name="pattern">The pattern to be used for the rule, which can be a regex or plain text.</param>
	/// <param name="isRegexOverride">
	/// An optional boolean to explicitly indicate if the pattern should be treated as a regex.
	/// If null, the system will attempt to determine automatically if the pattern is a regex.
	/// </param>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result contains the unique ID of the rule in the database.
	/// </returns>
	public async Task<int> AddOrEnableArtistRule(ArtistRuleType type, string pattern, bool? isRegexOverride = null)
	{
		if (string.IsNullOrWhiteSpace(pattern))
			throw new ArgumentException("Pattern is required", nameof(pattern));

		pattern = pattern.Trim();
		string typeStr = type.ToString();

		int isRegexInt;
		string finalPattern = pattern;

		if (type == ArtistRuleType.Exception)
		{
			isRegexInt = 0;
		}
		else
		{
			bool useRegex = isRegexOverride ?? IsProbablyRegex(pattern);
			isRegexInt = useRegex ? 1 : 0;

			if (useRegex)
			{
				finalPattern = EnsureWordBoundaries(pattern);
			}
		}

		await _database.ExecuteAsync(@"INSERT OR IGNORE INTO ArtistSplitRules (Type, Pattern, IsRegex, Active, IsBuiltIn) VALUES ('Splitter', ?, ?, 1, 1)", finalPattern, isRegexInt);

		await _database.ExecuteAsync(@"UPDATE ArtistSplitRules SET Active = 1 WHERE Type = ? AND Pattern = ? AND IsRegex = ?", typeStr, finalPattern, isRegexInt);

		var id = await _database.ExecuteScalarAsync<int>(@"SELECT Id FROM ArtistSplitRules WHERE Type = ? AND Pattern = ? AND IsRegex = ?", typeStr, finalPattern, isRegexInt);

		await ReloadArtistSplitRules();

		return id;
	}

	/// <summary>
	/// Deactivates an artist split rule by setting its `Active` flag to `0` in the database.
	/// Also triggers a reload of the artist split rules after the update.
	/// </summary>
	/// <param name="id">The unique identifier of the artist split rule to be deactivated.</param>
	/// <returns>
	/// A task that represents the asynchronous operation of removing the artist split rule.
	/// </returns>
	public async Task RemoveArtistRule(int id)
	{
		await _database.ExecuteAsync("UPDATE ArtistSplitRules SET Active = 0 WHERE Id = ?", id);
		await ReloadArtistSplitRules();
	}

	/// <summary>
	/// Removes an active artist rule from the database by updating its status to inactive.
	/// This operation is performed for a specific rule type, pattern, and optional regular expression flag.
	/// </summary>
	/// <param name="type">
	/// The type of the artist rule to be removed. It can be either a 'Splitter' or an 'Exception'.
	/// </param>
	/// <param name="pattern">
	/// The pattern associated with the artist rule that is to be removed. This is typically a string value.
	/// </param>
	/// <param name="isRegex">
	/// Indicates whether the pattern should be treated as a regular expression.
	/// Pass 'true' if the pattern is a regex, otherwise 'false'.
	/// </param>
	/// <returns>
	/// A task that represents the asynchronous operation. The task completes once the rule has been removed from the database.
	/// </returns>
	public async Task RemoveArtistRule(ArtistRuleType type, string pattern, bool isRegex = false)
	{
		var typeStr = type.ToString();
		int isRegexInt = (type == ArtistRuleType.Splitter && isRegex) ? 1 : 0;

		await _database.ExecuteAsync("UPDATE ArtistSplitRules SET Active = 0 WHERE Type = ? AND Pattern = ? AND IsRegex = ?", typeStr, pattern.Trim(), isRegexInt);
	}


	/// <summary>
	/// Restores the default artist split rules in the database by ensuring built-in rules are active.
	/// This involves enabling all built-in artist splitter rules and reloading them into the application.
	/// </summary>
	/// <returns>
	/// A task that represents the asynchronous operation of restoring the default artist rules.
	/// </returns>
	public async Task RestoreDefaultArtistRules()
	{
		await EnsureDefaultArtistRules();
		await _database.ExecuteAsync("UPDATE ArtistSplitRules SET Active = 1 WHERE IsBuiltIn = 1 AND Type = 'Splitter'");
		await ReloadArtistSplitRules();
	}

	/// <summary>
	/// Rebuilds all song-artist links in the database based on the current data and rules.
	/// This method processes all songs in the database and regenerates the associations
	/// between songs and artists to ensure consistency and accuracy.
	/// Additionally, it prunes any unused artists to clean up the database.
	/// </summary>
	/// <returns>
	/// A task that represents the asynchronous operation of rebuilding all song-artist links.
	/// </returns>
	private async Task RebuildAllSongArtistLinks()
	{
		var songs = await _database.QueryAsync<(string Path, string Artists)>("SELECT Path, Artists FROM Songs");

		await _database.RunInTransactionAsync(conn =>
		{
			foreach (var s in songs)
				SyncSongArtistsForSong(conn, s.Path, s.Artists);
		});

		await PruneUnusedArtists();
	}

	/// <summary>
	/// Applies changes made to artist rules by updating or reloading the necessary configurations.
	/// If the rebuild parameter is set to true, this method triggers a complete rebuild of song-artist links
	/// to reflect the updated rules in the database.
	/// </summary>
	/// <param name="rebuild">A boolean flag indicating whether to rebuild all song-artist links. If true, the links will be fully rebuilt.</param>
	/// <returns>
	/// A task that represents the asynchronous operation of applying changes to artist rules.
	/// </returns>
	public async Task ApplyArtistRulesChanges(bool rebuild = false)
	{
		await ReloadArtistSplitRules();
		if (rebuild)
			await RebuildAllSongArtistLinks();
	}

	/// <summary>
	/// Previews the result of splitting an artist field into individual artist names based on specific delimiters or patterns.
	/// This method does not modify the database or persist any changes and is intended to provide a preview of the parsed output.
	/// </summary>
	/// <param name="artistField">The string containing the artist information to split. Multiple artist names are usually delimited by a specific separator, such as a comma or ampersand.</param>
	/// <returns>
	/// A list of individual artist names extracted from the input artist field.
	/// </returns>
	public List<string> PreviewSplit(string artistField)
	{
		return SplitArtists(artistField).ToList();
	}

	/// <summary>
	/// Rebuilds the Full-Text Search (FTS) indexes for the Songs and Artists tables in the database.
	/// This method ensures that the FTS indexes are up-to-date by performing the following operations:
	/// <br/>
	/// - Deletes existing entries from the SongFTS table to clear previous index data.
	/// <br/>
	/// - Deletes existing entries from the ArtistFTS table to clear previous index data.
	/// <br/>
	/// - Initiates a rebuild of the SongFTS index by inserting a special rebuild command entry.
	/// <br/>
	/// - Initiates a rebuild of the ArtistFTS index by inserting a special rebuild command entry.
	/// <br/>
	/// Errors during individual delete operations are caught and ignored to prevent interruption of the rebuild process.
	/// </summary>
	/// <returns>A task representing the asynchronous operation of rebuilding the FTS indexes.</returns>
	public async Task RebuildFts()
	{
		try
		{
			await _database.ExecuteAsync("DELETE FROM SongFTS");
		}
		catch (Exception)
		{
			//ignored
		}
		try
		{
			await _database.ExecuteAsync("DELETE FROM ArtistFTS");
		}
		catch (Exception)
		{
			//ignored
		}
		await _database.ExecuteAsync("INSERT INTO SongFTS(SongFTS) VALUES('rebuild')");
		await _database.ExecuteAsync("INSERT INTO ArtistFTS(ArtistFTS) VALUES('rebuild')");
	}

	public async Task<List<string>> GetAllArtists()
	{
		var artists = await _database.QueryAsync<Artist>("SELECT Name FROM Artists ORDER BY Name ASC");
		return artists.Select(x => x.Name).ToList();
	}

	public async Task<List<string>> GetAllAlbums()
	{
		var albums = await _database.QueryAsync<Song>("SELECT DISTINCT Album FROM Songs WHERE Album IS NOT NULL AND Album != '' ORDER BY Album ASC");
		return albums.Select(x => x.Album).ToList();
	}

	public async Task<List<string>> GetAllGenres()
	{
		var genres = await _database.QueryAsync<Song>("SELECT DISTINCT Genre FROM Songs WHERE Genre IS NOT NULL AND Genre != '' AND Genre != 'Unknown' AND Genre != 'Unknown Genre' ORDER BY Genre ASC");
		return genres.Select(x => x.Genre).ToList();
	}

	public async Task UpdateFileScanMeta(List<FileScanMeta> metas)
	{
		if (metas == null || metas.Count == 0) return;

		await _database.RunInTransactionAsync(conn =>
		{
			foreach (var meta in metas)
			{
				conn.Execute(@"INSERT INTO FileScanMeta (Path, LastModifiedUtc, CreationTimeUtc, FileSizeBytes, LastScannedUtc)
							   VALUES (?, ?, ?, ?, ?)
							   ON CONFLICT(Path) DO UPDATE SET
							   LastModifiedUtc = excluded.LastModifiedUtc,
							   CreationTimeUtc = excluded.CreationTimeUtc,
							   FileSizeBytes = excluded.FileSizeBytes,
							   LastScannedUtc = excluded.LastScannedUtc;",
							   meta.Path, meta.LastModifiedUtc, meta.CreationTimeUtc, meta.FileSizeBytes, meta.LastScannedUtc);
			}
		});
	}

	public async Task<FileScanMeta?> GetFileScanMeta(string path)
	{
		try
		{
			var result = await _database.QueryAsync<FileScanMeta>("SELECT Path, LastModifiedUtc, CreationTimeUtc, FileSizeBytes, LastScannedUtc FROM FileScanMeta WHERE Path = ?", path);
			return result.Count > 0 ? result[0] : null;
		}
		catch (Exception)
		{
			return null;
		}
	}

	public async Task DeleteFileScanMeta(string path)
	{
		await _database.ExecuteAsync("DELETE FROM FileScanMeta WHERE Path = ?", path);
	}

	public async Task WipeFileScanMeta()
	{
		await _database.ExecuteAsync("DELETE FROM FileScanMeta");
	}

	// NOTE: Below are some helper methods for advanced search functionality

	/// <summary>
	/// Parses the user-provided search input into groups of terms for advanced search functionality.
	/// The input is split into <b>OR</b> groups using the <b>';'</b> delimiter, and within each group, <b>AND</b> terms are identified using the <b>'+'</b> delimiter.
	/// This allows complex search queries to be processed for further operations, such as database lookups.
	/// </summary>
	/// <param name="input">
	/// The raw search string provided by the user. Delimiters (';' and '+') are used to define OR and AND groupings in the query.
	/// </param>
	/// <returns>
	/// A list of OR groups, where each group is a list of AND terms. Each inner list represents terms that must all match within an OR grouping.
	/// </returns>
	private static List<List<string>> ParseSearchInput(string input)
	{
		var groups = new List<List<string>>();
		foreach (var orPart in input.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
		{
			var andTerms = orPart.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
								 .Where(t => !string.IsNullOrWhiteSpace(t))
								 .ToList();
			if (andTerms.Count > 0) groups.Add(andTerms);
		}
		return groups;
	}

	/// <summary>
	/// Escapes a string for use in a Full-Text Search (FTS) query by processing special characters.
	/// Specifically, doubles any embedded quotes in the input string to maintain correct FTS query syntax
	/// while ensuring proper handling of word boundaries as defined by the Unicode61 tokenizer.
	/// </summary>
	/// <param name="s">The input string to be escaped for use in an FTS context. This string may contain special characters requiring escaping.</param>
	/// <returns>An escaped string suitable for FTS queries. If the input string is null or contains only whitespace, an empty string is returned.</returns>
	private static string EscapeFts(string s)
	{
		if (string.IsNullOrWhiteSpace(s))
			return string.Empty;

		var escaped = s.Replace("\"", "\"\"");
		return $"\"{escaped}\"";
	}

	/// <summary>
	/// Constructs a full-text search (FTS) query by processing a list of search terms
	/// and appending a wildcard '*' to each term. The terms are then combined
	/// with an "AND" logical operator, creating a query that matches all given terms.
	/// </summary>
	/// <param name="terms">A list of terms to be included in the query. These terms are first escaped and then appended with wildcards.</param>
	/// <returns>
	/// A string representing the constructed FTS query. If there is only one term, the resulting query
	/// consists of that term with the wildcard. If there are multiple terms, they are joined with "AND".
	/// </returns>
	public static string BuildAndPrefix(List<string> terms)
	{
		var escaped = terms.Select(t => $"{EscapeFts(t)}*").ToList();
		return escaped.Count == 1 ? escaped[0] : string.Join(" AND ", escaped);
	}

	/// <summary>
	/// Constructs a column-specific match query for a group of search terms, tailored to the specified search scope.
	/// This method generates a Full-Text Search (FTS) match string intended for use in querying a database for songs.
	/// Depending on the search scope, the generated match string targets specific columns (e.g., title, artist, album)
	/// or performs a broader search across multiple fields.
	/// </summary>
	/// <param name="andTerms">A list of terms to be combined with an AND logic. These terms are matched within a column or across columns based on the scope.</param>
	/// <param name="scope">The search scope determining which database columns are matched. Possible values include Title, Artist, Album, or All.</param>
	/// <returns>A string representing the match query formatted according to the specified scope and terms. For example, a scope of Title will return a match string targeting only the title column, while a scope of All matches across multiple columns.</returns>
	private static string BuildSongFtsMatchForGroup(List<string> andTerms, SearchScope scope, bool isAndQuery = false)
	{
		if (isAndQuery)
		{
			var perTerm = andTerms.Select(t =>
			{
				var e = $"{EscapeFts(t)}*";
				return $"title:({e}) OR album:({e}) OR artists:({e}) OR genre:({e}) OR year:({e})";
			});
			return string.Join(" AND ", perTerm.Select(expr => $"({expr})"));
		}

		var andExpr = BuildAndPrefix(andTerms);
		return scope switch
		{
			SearchScope.Title => $"title:({andExpr})",
			SearchScope.Artist => $"artists:({andExpr})",
			SearchScope.Album => $"album:({andExpr})",
			_ => $"title:({andExpr}) OR album:({andExpr}) OR artists:({andExpr}) OR genre:({andExpr}) OR year:({andExpr})"
		};
	}

	/// <summary>
	/// Constructs an artist-specific full-text search (FTS) query for a group of terms.
	/// This method generates a search expression targeting the "name" field in the database by combining the given terms
	/// with logical operators and applying FTS-specific formatting. If there is only one term, it is suffixed with '*'
	/// to allow prefix matching. If multiple terms are provided, they are combined as a logical conjunction within parentheses.
	/// </summary>
	/// <param name="andTerms">A list of search terms to be combined in the FTS query for the artist match.</param>
	/// <returns>A formatted string representing an FTS query for matching artist names.</returns>
	private static string BuildArtistFtsMatchForGroup(List<string> andTerms)
	{
		var tokens = andTerms
			.SelectMany(t => t.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			.Where(t => !string.IsNullOrWhiteSpace(t))
			.Select(t => $"{EscapeFts(t)}*")
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();

		var expr = tokens.Count == 1 ? tokens[0] : string.Join(" AND ", tokens);
		return $"name:({expr})";
	}


	/// <summary>
	/// Detects and returns the primary search category based on the search query structure and scope.
	/// <br/>
	/// The method analyzes the search pattern using several heuristic rules:
	/// <br/>
	/// 1. Honors explicitly provided search scope if specified (not 'All')
	/// <br/>
	/// 2. Defaults to Title category for queries with multiple AND terms, unless matching the year pattern
	/// <br/>
	/// 3. For single terms, tries to match common patterns:
	/// <br/>
	/// - Year pattern (4 digits)
	/// <br/>
	/// - Artist name existence
	/// <br/>
	/// - Album title existence
	/// <br/>
	/// - Song title existence
	/// <br/>
	/// 4. Falls back to 'All' category if no specific matches found
	/// </summary>
	/// <param name="groups">The parsed search query organized as groups of terms</param>
	/// <param name="scope">The search scope preference provided by the user</param>
	/// <returns>The detected primary <see cref="SearchCategory"/> for organizing results</returns>
	private async Task<SearchCategory> DetectPrimaryCategory(List<List<string>> groups, SearchScope scope)
	{
		if (scope != SearchScope.All)
		{
			return scope switch
			{
				SearchScope.Artist => SearchCategory.Artist,
				SearchScope.Album => SearchCategory.Album,
				SearchScope.Title => SearchCategory.Title,
				_ => SearchCategory.All
			};
		}

		if (groups.Any(g => g.Count >= 2))
		{
			bool isSingleYear =
				groups.Count == 1 &&
				groups[0].Count == 1 &&
				Regex.IsMatch(groups[0][0].Trim(), @"^\d{4}$");

			if (!isSingleYear)
				return SearchCategory.Title;
		}

		var first = groups.FirstOrDefault()?.FirstOrDefault();
		if (string.IsNullOrWhiteSpace(first)) return SearchCategory.All;

		var term = first.Trim();
		if (Regex.IsMatch(term, @"^\d{4}$")) return SearchCategory.Year;

		var artistHits = await _database.ExecuteScalarAsync<int>(
			"SELECT COUNT(*) FROM ArtistFTS WHERE ArtistFTS MATCH ? LIMIT 1", $"name:({EscapeFts(term)}*)");
		if (artistHits > 0) return SearchCategory.Artist;

		var albumHits = await _database.ExecuteScalarAsync<int>(
			"SELECT COUNT(*) FROM SongFTS WHERE SongFTS MATCH ? LIMIT 1", $"album:({EscapeFts(term)}*)");
		if (albumHits > 0) return SearchCategory.Album;

		var titleHits = await _database.ExecuteScalarAsync<int>(
			"SELECT COUNT(*) FROM SongFTS WHERE SongFTS MATCH ? LIMIT 1", $"title:({EscapeFts(term)}*)");
		if (titleHits > 0) return SearchCategory.Title;

		return SearchCategory.All;
	}

	/// <summary>
	/// Checks if all provided artist terms exist in the database.
	/// The function queries the ArtistFTS table to determine if each term has a match.
	/// </summary>
	/// <param name="terms">A collection of terms to verify against the ArtistFTS table. Each term should be non-empty and trimmed.</param>
	/// <returns>A task that represents the asynchronous operation. The task result is <c>true</c> if all terms are found in the database, otherwise <c>false</c>.</returns>
	private async Task<bool> AreAllArtistTerms(IEnumerable<string> terms)
	{
		foreach (var t in terms)
		{
			if (string.IsNullOrWhiteSpace(t)) return false;
			var hits = await _database.ExecuteScalarAsync<int>(
				"SELECT COUNT(*) FROM ArtistFTS WHERE ArtistFTS MATCH ? LIMIT 1", $"name:({EscapeFts(t.Trim())}*)");
			if (hits == 0) return false;
		}
		return true;
	}

	/// <summary>
	/// Extracts unique 4-digit numeric tokens from a given collection of strings.
	/// This method identifies and returns all distinct year-like tokens (e.g., "2011") that consist of exactly four digits.
	/// Tokens are filtered, trimmed of surrounding whitespace, and compared case-insensitively to ensure uniqueness.
	/// </summary>
	/// <param name="terms">A collection of strings to parse and extract year-like tokens.</param>
	/// <returns>A list of unique 4-digit tokens extracted from the input collection.</returns>
	private static List<string> ExtractYearTokens(IEnumerable<string> terms)
	{
		return terms
			.Select(t => t.Trim())
			.Where(t => Regex.IsMatch(t, @"^\d{4}$"))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	/// <summary>
	/// Represents a database row containing an artist's name in the Tunetastic application.
	/// This class is used for querying and retrieving artist-related data from the database.
	/// </summary>
	private sealed class ArtistNameRow
	{
		public string? Name { get; set; }
	}

	/// <summary>
	/// Represents a database row mapping for album names used within the Tunetastic application.
	/// This class serves as a lightweight data structure for querying and processing album information
	/// from the database, specifically in scenarios involving album search operations.
	/// </summary>
	private sealed class AlbumNameRow
	{
		public string? Album { get; set; }
	}

	/// <summary>
	/// Defines the various categories used for organizing and prioritizing search results
	/// in the Tunetastic application.
	/// </summary>
	/// <remarks>
	/// This enumeration represents the primary classifications to filter or group music searches.
	/// Categories include titles, artists, albums, genres, years, or encompassing all available data.
	/// It aids in structuring search algorithms and refining results for user queries.
	/// </remarks>
	private enum SearchCategory
	{
		All,
		Title,
		Artist,
		Album,
		Genre,
		Year
	}

	/// <summary>
	/// Performs an asynchronous search operation across songs, artists, and albums in the database based on user input.
	/// This comprehensive search method employs sophisticated categorization, prioritization, and filtering mechanisms to deliver relevant results.
	/// <br/><br/>
	/// Key Features:
	/// <br/>
	/// - Supports single and multi-term queries with AND/OR logic using '+' and ';' delimiters<br/>
	/// - Detects and prioritizes primary search categories (Title, Artist, Album, Year)<br/>
	/// - Handles special cases like artist conjunction searches and year-only queries<br/>
	/// - Uses full-text search (FTS) for efficient text matching<br/>
	/// - Enforces search scopes for targeted results<br/>
	/// - Implements fallback logic when primary category yields no results<br/>
	/// <br/>
	/// Implementation Details:
	/// <br/>
	/// 1. Input Validation and Parsing<br/>
	/// 2. Primary Category Detection<br/>
	/// 3. Artist Conjunction Handling<br/>
	/// 4. Full-Text Search (FTS) for Songs<br/>
	/// 5. Artist Name Matching<br/>
	/// 6. Album Search and Hydration<br/>
	/// 7. Year-Only Override Logic<br/>
	/// 8. Scope Enforcement<br/>
	/// 9. Primary Category Fallbacks<br/>
	/// 10. Results Ordering and Assembly<br/>
	/// </summary>
	/// <param name="input">The search query string. Supports AND/OR logic using '+' and ';' delimiters.</param>
	/// <param name="scope">The search scope to limit results to specific categories (All, Title, Artist, Album).</param>
	/// <param name="limitPerCategory">Maximum number of results to return per category.</param>
	/// <returns>A SearchResults object containing matched songs, artists, and albums, along with primary category information.</returns>
	public async Task<SearchResults> Search(string input, SearchScope scope = SearchScope.All, int limitPerCategory = 5)
	{
		var results = new SearchResults();
		if (string.IsNullOrWhiteSpace(input)) return results;

		var groups = ParseSearchInput(input);
		if (groups.Count == 0) return results;

		bool preferArtistConjunction = false;
		List<string> artistAndTerms = new();
		if (groups.Count == 1 && groups[0].Count >= 2)
		{
			var terms = groups[0].Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
			if (await AreAllArtistTerms(terms))
			{
				preferArtistConjunction = true;
				artistAndTerms = terms;
			}
		}

		var primary = await DetectPrimaryCategory(groups, scope);
		results.PrimaryCategory = primary.ToString();

		var songGroupMatches = groups.Select(g => BuildSongFtsMatchForGroup(g, scope, isAndQuery: g.Count > 1)).ToList();
		var artistGroupMatches = groups.Select(BuildArtistFtsMatchForGroup).ToList();
		var groupYears = groups.Select(g => ExtractYearTokens(g)).ToList();

		bool filledTitlesViaArtistConjunction = false;
		if (preferArtistConjunction)
		{
			var songs = await GetSongsByArtists(artistAndTerms, orderBy: SongProperty.Title, ascending: true, matchAll: true);
			if (songs.Count > 0)
			{
				results.Titles = songs.Take(limitPerCategory).ToList();
				results.PrimaryCategory = "Title";
				filledTitlesViaArtistConjunction = true;
			}
		}

		if (!filledTitlesViaArtistConjunction &&
			(scope == SearchScope.All || scope == SearchScope.Title || scope == SearchScope.Album || scope == SearchScope.Artist))
		{
			var songSubs = new List<string>();
			var songArgs = new List<object>();

			for (int i = 0; i < songGroupMatches.Count; i++)
			{
				var match = songGroupMatches[i];
				var years = groupYears[i];
				var yearClause = years.Count > 0
					? $" AND S.Year IN ({string.Join(", ", Enumerable.Repeat("?", years.Count))})"
					: string.Empty;

				songSubs.Add(@"SELECT S.*, bm25(SongFTS) AS Score
							   FROM SongFTS
							   JOIN Songs S ON S.Path = SongFTS.Path
							   WHERE SongFTS MATCH ?" + yearClause);

				songArgs.Add(match);
				if (years.Count > 0)
					songArgs.AddRange(years);
			}

			if (songSubs.Count > 0)
			{
				var songSql = $@"WITH matches AS MATERIALIZED (
								 {string.Join("\nUNION ALL\n", songSubs)})
								 SELECT Path, Title, Artists, Album, Genre, Year, PlayCount, Cover, Duration, DateAdded, DateLastPlayed, Extension, PlayerType
								 FROM matches
								 GROUP BY Path
								 ORDER BY MIN(Score) ASC
								 LIMIT {limitPerCategory}";

				results.Titles = await _database.QueryAsync<Song>(songSql, songArgs.ToArray());
			}
		}

		if (scope == SearchScope.All || scope == SearchScope.Artist)
		{
			var artistSubs = new List<string>();
			var artistArgs = new List<object>();
			foreach (var m in artistGroupMatches)
			{
				artistSubs.Add(@"SELECT Name, bm25(ArtistFTS) AS Score
								 FROM ArtistFTS
								 WHERE ArtistFTS MATCH ?");
				artistArgs.Add(m);
			}

			if (artistSubs.Count > 0)
			{
				var artistSql = $@"WITH matches(Name, Score) AS MATERIALIZED (
								   {string.Join("\nUNION ALL\n", artistSubs)})
								   SELECT Name
								   FROM matches
								   GROUP BY Name
								   ORDER BY MIN(Score) ASC
								   LIMIT {limitPerCategory}";

				var rows = await _database.QueryAsync<ArtistNameRow>(artistSql, artistArgs.ToArray());
				results.Artists = rows.Select(r => r.Name).ToList()!;
			}
		}

		if (scope == SearchScope.All || scope == SearchScope.Album)
		{
			var albumSubs = new List<string>();
			var albumArgs = new List<object>();

			for (int i = 0; i < songGroupMatches.Count; i++)
			{
				var match = songGroupMatches[i];
				var years = groupYears[i];
				var yearClause = years.Count > 0
					? $" AND S.Year IN ({string.Join(", ", Enumerable.Repeat("?", years.Count))})"
					: string.Empty;

				albumSubs.Add(@"SELECT SongFTS.Album AS Album, bm25(SongFTS) AS Score
								FROM SongFTS
								JOIN Songs S ON S.Path = SongFTS.Path
								WHERE SongFTS MATCH ?
								AND TRIM(SongFTS.Album) != ''" + yearClause);

				albumArgs.Add(match);
				if (years.Count > 0)
					albumArgs.AddRange(years);
			}

			if (albumSubs.Count > 0)
			{
				var albumSql = $@"WITH matches(Album, Score) AS MATERIALIZED (
								  {string.Join("\nUNION ALL\n", albumSubs)})
								  SELECT Album
								  FROM matches
								  WHERE TRIM(Album) != ''
								  GROUP BY Album
								  ORDER BY MIN(Score) ASC
								  LIMIT {limitPerCategory}";

				var rows = await _database.QueryAsync<AlbumNameRow>(albumSql, albumArgs.ToArray());
				var albumNames = rows
					.Select(r => r.Album)
					.Where(a => !string.IsNullOrWhiteSpace(a))
					.Distinct(StringComparer.OrdinalIgnoreCase)
					.ToList();

				if (albumNames.Count > 0)
				{
					var values = string.Join(", ", albumNames.Select((n, i) => $"(?, {i})"));
					var rankArgs = albumNames.Cast<object>().ToList();

					var hydrateSql = $@"WITH ranks(Name, Ord) AS (VALUES {values})
										SELECT
										CASE WHEN TRIM(S.Album) = 'Unknown Album' THEN 'Unknown' ELSE S.Album END AS Album,
										COUNT(*) AS Count,
										SUM(S.Duration) AS TotalDuration,
										COALESCE(MAX(S.Cover), '') AS Cover
										FROM Songs S
										JOIN ranks R ON R.Name = S.Album
										GROUP BY S.Album
										ORDER BY MIN(R.Ord) ASC
										LIMIT {limitPerCategory}";

					results.Albums = await _database.QueryAsync<AlbumModel>(hydrateSql, rankArgs.ToArray());
				}
			}
		}

		var singleYear = groups.Count == 1 && groups[0].Count == 1 && Regex.IsMatch(groups[0][0], @"^\d{4}$");
		if (singleYear)
		{
			var y = groups[0][0].Trim();

			results.Titles = await _database.QueryAsync<Song>("SELECT * FROM Songs WHERE Year = ? ORDER BY Title COLLATE NOCASE ASC LIMIT ?", y, limitPerCategory);

			results.Albums = await _database.QueryAsync<AlbumModel>(@"SELECT Album, COUNT(*) AS Count, SUM(Duration) AS TotalDuration, COALESCE(MAX(Cover), '') AS Cover
																	  FROM Songs
																	  WHERE Year = ? AND TRIM(Album) != ''
																	  GROUP BY Album
																	  ORDER BY Album COLLATE NOCASE ASC
																	  LIMIT ?", y, limitPerCategory);

			results.PrimaryCategory = SearchCategory.Year.ToString();
		}

		if (scope == SearchScope.Title)
		{
			results.Artists.Clear();
			results.Albums.Clear();
			results.PrimaryCategory = SearchCategory.Title.ToString();
		}
		else if (scope == SearchScope.Artist)
		{
			results.Titles.Clear();
			results.Albums.Clear();
			results.PrimaryCategory = SearchCategory.Artist.ToString();
		}
		else if (scope == SearchScope.Album)
		{
			results.Titles.Clear();
			results.Artists.Clear();
			results.PrimaryCategory = SearchCategory.Album.ToString();
		}

		if (results.PrimaryCategory == SearchCategory.Artist.ToString() && results.Artists.Count == 0)
		{
			if (results.Titles.Count > 0) results.PrimaryCategory = SearchCategory.Title.ToString();
			else if (results.Albums.Count > 0) results.PrimaryCategory = SearchCategory.Album.ToString();
			else results.PrimaryCategory = SearchCategory.All.ToString();
		}
		else if (results.PrimaryCategory == SearchCategory.Album.ToString() && results.Albums.Count == 0)
		{
			if (results.Titles.Count > 0) results.PrimaryCategory = SearchCategory.Title.ToString();
			else if (results.Artists.Count > 0) results.PrimaryCategory = SearchCategory.Artist.ToString();
			else results.PrimaryCategory = SearchCategory.All.ToString();
		}
		else if (results.PrimaryCategory == SearchCategory.Title.ToString() && results.Titles.Count == 0)
		{
			if (results.Albums.Count > 0) results.PrimaryCategory = SearchCategory.Album.ToString();
			else if (results.Artists.Count > 0) results.PrimaryCategory = SearchCategory.Artist.ToString();
			else results.PrimaryCategory = SearchCategory.All.ToString();
		}

		results.Items = BuildOrderedItems(results);
		return results;
	}

	/// <summary>
	/// Builds an ordered list of search items based on the provided search results and primary category.
	/// </summary>
	/// <param name="results">
	/// The search results containing titles, artists and albums to be ordered. Contains a PrimaryCategory
	/// property that determines the ordering of items.
	/// </param>
	/// <returns>
	/// A list of SearchItems containing the search results ordered based on the primary category:
	/// <br/>
	/// - For "Title" category: Titles first, followed by Artists then Albums
	/// <br/>
	/// - For "Artist" category: Artists first, followed by Titles then Albums
	/// <br/>
	/// - For "Album" category: Albums first, followed by Titles then Artists
	/// <br/>
	/// - For "All/Unknown": Natural ordering of Titles, Artists then Albums is maintained
	/// </returns>
	private List<SearchItem> BuildOrderedItems(SearchResults results)
	{
		var items = new List<SearchItem>();

		void AddSongs(IEnumerable<Song>? src)
		{
			if (src == null) return;
			foreach (var s in src) items.Add(new SearchItem { Type = SearchItemType.Title, Title = s });
		}
		void AddArtists(IEnumerable<string>? src)
		{
			if (src == null) return;
			foreach (var a in src) items.Add(new SearchItem { Type = SearchItemType.Artist, Artist = a });
		}
		void AddAlbums(IEnumerable<AlbumModel>? src)
		{
			if (src == null) return;
			foreach (var a in src) items.Add(new SearchItem { Type = SearchItemType.Album, Album = a });
		}

		var primary = results?.PrimaryCategory?.Trim() ?? string.Empty;
		bool Is(string name) => primary.Equals(name, StringComparison.OrdinalIgnoreCase);

		if (Is("Title"))
		{
			AddSongs(results!.Titles);
			AddArtists(results.Artists);
			AddAlbums(results.Albums);
		}
		else if (Is("Artist"))
		{
			AddArtists(results!.Artists);
			AddSongs(results.Titles);
			AddAlbums(results.Albums);
		}
		else if (Is("Album"))
		{
			AddAlbums(results!.Albums);
			AddSongs(results.Titles);
			AddArtists(results.Artists);
		}
		else
		{
			// All/Unknown: keep your natural per-category ordering
			AddSongs(results!.Titles);
			AddArtists(results.Artists);
			AddAlbums(results.Albums);
		}

		return items;
	}
}
