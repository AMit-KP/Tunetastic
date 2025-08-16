using System.Text.RegularExpressions;
using SQLite;

namespace Tunetastic.Common;

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
	/// <pre>
	/// Initializes the database by creating all required SQLite tables if they do not already exist.
	/// The following tables are created, each with their purpose and column details:
	/// <br/>
	/// <br/>
	/// <b>Library</b>: Stores user-added music library locations.<br/>
	///   Columns:<br/>
	///     - Name (TEXT, NOT NULL): The display name of the library.<br/>
	///     - Path (TEXT, NOT NULL, COLLATE NOCASE, UNIQUE): The file system path to the library. Uniqueness ensures no duplicate libraries.<br/>
	/// <br/>
	/// <b>MusicFormats</b>: Stores supported music file formats.<br/>
	///   Columns:<br/>
	///     - Extension (TEXT, PRIMARY KEY, COLLATE NOCASE): File extension, unique and case-insensitive.<br/>
	///     - Description (TEXT, NOT NULL): Description of the format.<br/>
	///     - Enabled (INTEGER, NOT NULL): Indicates if the format is enabled (1) or not (0).<br/>
	/// <br/>
	/// <b>Songs</b>: Stores metadata for each song.<br/>
	///   Columns:<br/>
	///     - Path (TEXT, PRIMARY KEY): Unique file path for the song.<br/>
	///     - Title (TEXT): Song title.<br/>
	///     - Artists (TEXT): Raw artist string.<br/>
	///     - Album (TEXT): Album name.<br/>
	///     - Genre (TEXT): Genre name.<br/>
	///     - Year (TEXT): Year of release.<br/>
	///     - PlayCount (INTEGER): Number of times played.<br/>
	///     - Cover (TEXT): Path or URI to cover image.<br/>
	///     - Duration (REAL): Song duration in seconds.<br/>
	///     - DateAdded (DATETIME): When the song was added.<br/>
	///     - DateLastPlayed (DATETIME, DEFAULT NULL): Last played timestamp.<br/>
	///     - Extension (TEXT): File extension.<br/>
	/// <br/>
	/// <b>Playlists</b>: Stores user playlists.<br/>
	///   Columns:<br/>
	///     - Id (INTEGER, PRIMARY KEY AUTOINCREMENT): Unique playlist ID.<br/>
	///     - Name (TEXT): Playlist name.<br/>
	/// <br/>
	/// <b>PlaylistSongs</b>: Maps songs to playlists and their order.<br/>
	///   Columns:<br/>
	///     - PlaylistId (INTEGER): Foreign key to Playlists.Id.<br/>
	///     - SongPath (TEXT): Foreign key to Songs.Path.<br/>
	///     - Position (INTEGER, DEFAULT 0): Order of the song in the playlist.<br/>
	///   Primary key is (PlaylistId, SongPath). Foreign keys ensure referential integrity and cascade deletes.<br/>
	/// <br/>
	/// <b>QueuedPlayingList</b>: Stores the current play queue.<br/>
	///   Columns:<br/>
	///     - Id (INTEGER, PRIMARY KEY AUTOINCREMENT): Unique queue entry ID.<br/>
	///     - Path (TEXT, NOT NULL): Foreign key to Songs.Path.<br/>
	///     - Position (INTEGER): Order in the queue.<br/>
	///   Foreign key ensures only valid songs are queued and cascades on delete.<br/>
	/// <br/>
	/// <b>Artists</b>: Stores unique artist metadata.<br/>
	///   Columns:<br/>
	///     - Id (INTEGER, PRIMARY KEY AUTOINCREMENT): Unique artist ID.<br/>
	///     - Name (TEXT, NOT NULL, COLLATE NOCASE, UNIQUE): Artist name, unique and case-insensitive.<br/>
	///     - ArtistImage (TEXT): Path or URI to artist image.<br/>
	///     - ArtistDescription (TEXT): Artist description.<br/>
	/// <br/>
	/// <b>SongArtists</b>: Maps songs to artists.<br/>
	///   Columns:<br/>
	///     - SongPath (TEXT, NOT NULL): Foreign key to Songs.Path.<br/>
	///     - ArtistId (INTEGER, NOT NULL): Foreign key to Artists.Id.<br/>
	///   Primary key is (SongPath, ArtistId). Foreign keys ensure referential integrity and cascade deletes.<br/>
	///   Indexes on ArtistId and SongPath for efficient lookups.<br/>
	/// <br/>
	/// <b>ArtistSplitRules</b>: Stores rules for splitting or preserving artist names.<br/>
	///   Columns:<br/>
	///     - Id (INTEGER, PRIMARY KEY AUTOINCREMENT): Unique rule ID.<br/>
	///     - Type (TEXT, NOT NULL, CHECK IN ('Splitter','Exception')): Rule type.<br/>
	///     - Pattern (TEXT, NOT NULL): Pattern to match.<br/>
	///     - IsRegex (INTEGER, NOT NULL, DEFAULT 0): Whether the pattern is a regex.<br/>
	///     - Active (INTEGER, NOT NULL, DEFAULT 1): Whether the rule is active.<br/>
	///     - IsBuiltIn (INTEGER, NOT NULL, DEFAULT 0): Whether the rule is built-in.<br/>
	///   Unique constraint on (Type, Pattern, IsRegex). Index on (Active, Type) for fast filtering.<br/>
	/// <br/>
	/// The database is located in the application's local storage folder.
	/// </pre>
	/// </summary>
	/// <returns>
	/// A task that represents the asynchronous operation of initializing the database.
	/// </returns>
	public async Task InitializeDatabase()
	{
		var dbPath = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "tunetastic.db3");
		_database = new SQLiteAsyncConnection(dbPath);

		await _database.ExecuteAsync("PRAGMA foreign_keys = ON");

		await _database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Library (
									   Name TEXT NOT NULL,
									   Path TEXT NOT NULL COLLATE NOCASE UNIQUE)");

		await _database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS MusicFormats (
									   Extension TEXT PRIMARY KEY COLLATE NOCASE,
									   Description TEXT NOT NULL,
									   Enabled INTEGER NOT NULL)");

		await _database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS Songs (
									   Path TEXT PRIMARY KEY,
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
									   Extension TEXT)");

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

		await _database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS QueuedPlayingList (
									   Id INTEGER PRIMARY KEY AUTOINCREMENT,
									   Path TEXT NOT NULL,
									   Position INTEGER,
									   FOREIGN KEY (Path) REFERENCES Songs(Path) ON DELETE CASCADE)");

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
	/// Adds new libraries or updates existing ones in the database based on their paths.
	/// This method ensures that libraries with the same path are updated with new names,
	/// while new libraries are inserted.
	/// </summary>
	/// <param name="libraries">A collection of libraries to add or update, each represented by a <see cref="LibraryModel"/>.</param>
	/// <returns>
	/// A task that represents the asynchronous operation of adding or updating libraries in the database.
	/// </returns>
	public async Task AddOrUpdateLibraries(IEnumerable<LibraryModel> libraries)
	{
		if (libraries == null) return;

		const string sql = @"INSERT INTO Library (Name, Path)
							 VALUES (?, ?)
							 ON CONFLICT(Path) DO UPDATE SET
							 Name = excluded.Name;";

		await _database.RunInTransactionAsync(conn =>
		{
			foreach (var lib in libraries)
			{
				if (lib == null) continue;
				conn.Execute(sql, lib.Name, lib.Path);
			}
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
							(Path, Title, Artists, Album, Genre, Year, PlayCount, Cover, Duration, DateAdded, DateLastPlayed, Extension)
							VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
							song.Path, song.Title, song.Artists, song.Album, song.Genre, song.Year,
							0, song.Cover, song.Duration, song.DateAdded, null, song.Extension);

				SyncSongArtistsForSong(conn, song);
			}
		});
		await PruneUnusedArtists();
	}

	/// <summary>
	/// Updates the database with the provided list of songs. This method performs the following operations:
	/// <br/>
	/// - Retrieves existing song data in the database, including play count and last played date.
	/// <br/>
	/// - Retrieves existing playlist-to-song links from the database.
	/// <br/>
	/// - Deletes all records from the `Songs` table.
	/// <br/>
	/// - Inserts the provided songs into the `Songs` table, preserving the play count and last played date if a song already existed.
	/// <br/>
	/// - Restores the playlist-to-song relationships in the `PlaylistSongs` table.
	/// </summary>
	/// <param name="songs">A list of songs to be added to or updated in the database.</param>
	/// <returns>A task that represents the asynchronous operation of updating the songs in the database.</returns>
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
							(Path, Title, Artists, Album, Genre, Year, PlayCount, Cover, Duration, DateAdded, DateLastPlayed, Extension)
							VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
							song.Path, song.Title, song.Artists, song.Album, song.Genre, song.Year,
							existingPlayCount, song.Cover, song.Duration, song.DateAdded, lastPlayed, song.Extension);

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
		await PruneUnusedArtists();
	}

	/// <summary>
	/// Deletes a song entry from the database using the specified file path.
	/// This method removes the corresponding record from the `Songs` table.
	/// </summary>
	/// <param name="path">
	/// The file path of the song to be deleted from the database.
	/// </param>
	/// <returns>
	/// A task that represents the asynchronous operation of deleting the song from the database.
	/// </returns>
	public async Task DeleteSongFromDB(string path)
	{
		await _database.ExecuteAsync("DELETE FROM Songs WHERE Path = ?", path);
		await PruneUnusedArtists();
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
	}

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
			(@"\bfeat\.?\b", true),
			(@"\bft\.?\b", true),
			(@"\bx\b", true),
		};

		await _database.RunInTransactionAsync(conn =>
		{
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
	/// Removes any artist entries from the database that are no longer linked to any songs.
	/// This ensures that the `Artists` table remains consistent and free from unused entries,
	/// maintaining database integrity and reducing unnecessary storage.
	/// </summary>
	/// <returns>
	/// A task that represents the asynchronous operation of pruning unused artist entries
	/// from the `Artists` table.
	/// </returns>
	private async Task PruneUnusedArtists()
	{
		await _database.ExecuteAsync(@"DELETE FROM Artists WHERE Id NOT IN (SELECT DISTINCT ArtistId FROM SongArtists)");
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
						 WHERE LOWER(A.Name) IN ({placeholders})
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
						 WHERE LOWER(A.Name) IN ({placeholders})
						 GROUP BY S.Path
						 HAVING COUNT(DISTINCT LOWER(A.Name)) = ?
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
	/// This method validates the pattern for existing anchors, boundaries, or special characters and avoids
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
}

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
/// for that year, and the total playback duration of those songs.
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
/// Represents the properties of a song that can be used for sorting or filtering operations.
/// </summary>
public enum SongProperty
{
	Title,
	Artists,
	Album,
	Path,
	Genre,
	Year,
	PlayCount,
	Cover,
	Duration,
	DateAdded,
	DateLastPlayed,
	Extension
}

/// <summary>
/// Represents the types of rules that can be applied to manage artist-related data processing and categorization.
/// This enumeration is used within the application to distinguish between different types of artist handling rules,
/// such as splitting artists or marking exceptions in the processing workflow.
/// </summary>
public enum ArtistRuleType
{
	Splitter,
	Exception
}
