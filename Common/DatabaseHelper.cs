using SQLite;

namespace Tunetastic.Common;

/// <summary>
/// Provides utility methods for managing database interactions within the Tunetastic application.
/// This class includes functionality to initialize the database, manage songs, playlists, and related entries.
/// It ensures efficient operations like insertion, query, update, and deletion of song and playlist data.
/// </summary>
public class DatabaseHelper
{
	private static DatabaseHelper _instance;
	private SQLiteAsyncConnection _database;

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
	/// Initializes the database by setting up the required SQLite tables if they do not already exist.
	/// This method creates the following tables:
	/// <br/>
	/// - `Songs`: Stores information related to songs, such as path, title, artists, album, and more.
	/// <br/>
	/// - `Playlists`: Stores playlist names and their associated IDs.
	/// <br/>
	/// - `PlaylistSongs`: Stores the relationship between playlists and songs, along with foreign key constraints.
	/// <br/>
	/// The database is located in the local folder of the application's storage space.
	/// </summary>
	/// <returns>
	/// A task that represents the asynchronous operation of initializing the database.
	/// </returns>
	public async Task InitializeDatabase()
	{
		var dbPath = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "tunetastic.db3");
		_database = new SQLiteAsyncConnection(dbPath);

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
										PRIMARY KEY (PlaylistId, SongPath),
										FOREIGN KEY (PlaylistId) REFERENCES Playlists(Id) ON DELETE CASCADE,
										FOREIGN KEY (SongPath) REFERENCES Songs(Path) ON DELETE CASCADE)");

		await _database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS QueuedPlayingList (
										Id INTEGER PRIMARY KEY AUTOINCREMENT,
										Path TEXT,
										FOREIGN KEY (Path) REFERENCES Songs(Path) ON DELETE CASCADE)");
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
			}
		});
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
			}
		});

		await _database.RunInTransactionAsync(conn =>
		{
			foreach (var link in existingPlaylistLinks)
			{
				conn.Execute("INSERT INTO PlaylistSongs (PlaylistId, SongPath) VALUES (?, ?)", link.PlaylistId, link.SongPath);
			}
		});
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
	}

	/// <summary>
	/// Loads all songs from the database and sorts them based on the specified property, order and limit.
	/// </summary>
	/// <param name="songProperty">The property to sort the songs by, such as Title, Artists, or Album. Defaults to Title.</param>
	/// <param name="ascending">A boolean indicating whether the songs should be sorted in ascending order. Defaults to true.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains a list of songs ordered by the specified property, order and limit.
	/// If an exception occurs, an empty list is returned.</returns>
	public async Task<List<Song>> LoadSongsFromDB(SongProperty songProperty = SongProperty.Title, bool ascending = true, int limit = 0, string? whereCondition = null)
	{
		try
		{
			return await _database.QueryAsync<Song>($"SELECT * FROM Songs {(whereCondition == null ? "" : $" WHERE {whereCondition}")} ORDER BY {songProperty.ToString()} {(ascending ? "ASC" : "DESC")}{(limit > 0 ? $" LIMIT {limit}" : "")}");
		}
		catch (Exception)
		{
			return new List<Song>();
		}
	}

	/// <summary>
	/// Loads the paths of all songs from the database, ordered by the specified song property and sort direction.
	/// </summary>
	/// <param name="SortBySongProperty">The property of the song used for sorting, such as Title, Artists, or Album. Defaults to Title.</param>
	/// <param name="ascending">Indicates whether the sorting should be in ascending order. Defaults to true.</param>
	/// <returns>
	/// A task that represents the asynchronous operation, returning a list of strings containing the paths of the songs.
	/// If the operation fails, an empty list is returned.
	/// </returns>
	public async Task<List<string>> LoadSongPathsFromDB(SongProperty SortBySongProperty = SongProperty.Title, bool ascending = true)
	{
		try
		{
			return (await _database.QueryAsync<Song>($"SELECT Path FROM Songs ORDER BY {SortBySongProperty.ToString()} {(ascending ? "ASC" : "DESC")}")).Select(s => s.Path).ToList();
		}
		catch (Exception)
		{
			return new List<string>();
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
	/// Adds a song to the specified playlist in the database by creating an entry in the `PlaylistSongs` table.
	/// </summary>
	/// <param name="playlistName">The name of the playlist to which the song should be added. Must correspond to an existing playlist in the database.</param>
	/// <param name="songPath">The path of the song to add to the playlist. Must correspond to an existing song in the `Songs` table.</param>
	/// <returns>
	/// A task that represents the asynchronous operation of adding the song to the playlist.
	/// </returns>
	public async Task AddSongToPlaylist(string playlistName, string songPath)
	{
		await _database.ExecuteAsync("INSERT INTO PlaylistSongs (PlaylistId, SongPath) VALUES ((SELECT Id FROM Playlists WHERE Name = ?), ?)", playlistName, songPath);
	}

	/// <summary>
	/// Adds multiple songs to a specified playlist in the database.
	/// This operation creates entries in the `PlaylistSongs` table by associating the songs
	/// with the provided playlist name and their respective paths.
	/// </summary>
	/// <param name="playlistName">
	/// The name of the playlist to which the songs will be added.
	/// It is used to identify the playlist in the database.
	/// </param>
	/// <param name="songPaths">
	/// A list of file paths representing the songs to be added to the playlist.
	/// Each path corresponds to a song entry in the database.
	/// </param>
	/// <returns>
	/// A task that represents the asynchronous operation of adding multiple songs to the playlist.
	/// </returns>
	public async Task AddSongsToPlaylist(string playlistName, List<string> songPaths)
	{
		await _database.RunInTransactionAsync(conn =>
		{
			foreach (var songPath in songPaths)
			{
				conn.Execute("INSERT INTO PlaylistSongs (PlaylistId, SongPath) VALUES ((SELECT Id FROM Playlists WHERE Name = ?), ?)", playlistName, songPath);
			}
		});
	}

	/// <summary>
	/// Removes a specific song from a playlist in the database.
	/// This method ensures that the entry linking the given song to the specified playlist
	/// is deleted if it exists.
	/// </summary>
	/// <param name="playlistName">The name of the playlist from which the song will be removed.</param>
	/// <param name="songPath">The path of the song to be removed from the playlist.</param>
	/// <returns>
	/// A task representing the asynchronous operation of removing the song from the playlist.
	/// </returns>
	public async Task RemoveSongFromPlaylist(string playlistName, string songPath)
	{
		await _database.ExecuteAsync("DELETE FROM PlaylistSongs WHERE PlaylistId IN (SELECT Id FROM Playlists WHERE Name = ?) AND SongPath = ?", playlistName, songPath);
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
	/// Retrieves a list of songs that belong to the specified playlist.
	/// This method queries the `Songs` table and joins it with the `PlaylistSongs` table
	/// to fetch all the songs associated with the given playlist name.
	/// </summary>
	/// <param name="playlistName">
	/// The name of the playlist for which songs need to be retrieved.
	/// </param>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result contains
	/// a list of <see cref="Song"/> objects representing the songs in the specified playlist.
	/// </returns>
	public async Task<List<Song>> GetSongsInPlaylist(string playlistName)
	{
		return await _database.QueryAsync<Song>("SELECT S.* FROM Songs S INNER JOIN PlaylistSongs P ON S.Path = P.SongPath WHERE P.PlaylistId IN (SELECT Id FROM Playlists WHERE Name = ?)", playlistName);
	}

	/// <summary>
	/// Adds a list of song paths to the queued playing list in the database.
	/// If a song already exists in the queued playing list, it is replaced with the new entry.
	/// </summary>
	/// <param name="songPaths">A list of strings representing the paths of the songs to be added to the queued playing list.</param>
	/// <returns>
	/// A task that represents the asynchronous operation of adding songs to the queued playing list.
	/// </returns>
	public async Task AddSongsToQueuedPlayingList(List<string> songPaths)
	{
		await _database.RunInTransactionAsync(conn =>
		{
			foreach (var songPath in songPaths)
			{
				conn.Execute("INSERT OR REPLACE INTO QueuedPlayingList (Path) VALUES (?)", songPath);
			}
		});
	}

	/// <summary>
	/// Retrieves the list of songs currently in the queued playing list.
	/// The queued playing list is managed within the database and includes songs
	/// ordered by their position in the queue.
	/// </summary>
	/// <returns>
	/// A task that represents the asynchronous operation of fetching the queued playing list.
	/// The task result contains a list of <see cref="Song"/> objects retrieved from the database,
	/// ordered by their queue position.
	/// </returns>
	public async Task<List<Song>> GetQueuedPlayingList()
	{
		return await _database.QueryAsync<Song>("SELECT S.* FROM Songs S JOIN QueuedPlayingList Q ON S.Path = Q.Path ORDER BY Q.Id");
	}

	/// <summary>
	/// Removes a song from the queued playing list in the database.
	/// This operation deletes the specified song entry from the `QueuedPlayingList` table.
	/// </summary>
	/// <param name="songPath">
	/// The file path of the song to be removed from the queued playing list.
	/// </param>
	/// <returns>
	/// A task that represents the asynchronous operation of removing the song from the queued playing list.
	/// </returns>
	public async Task RemoveFromQueuedPlayingList(string songPath)
	{
		await _database.ExecuteAsync("DELETE FROM QueuedPlayingList WHERE Path = ?", songPath);
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
	public string Path { get; set; }
	public string Title { get; set; }
	public string Artists { get; set; }
	public string Album { get; set; }
	public string Genre { get; set; }
	public string Year { get; set; }
	public int PlayCount { get; set; }
	public string Cover { get; set; }
	public double Duration { get; set; }
	public DateTime DateAdded { get; set; }
	public DateTime? DateLastPlayed { get; set; }
	public string Extension { get; set; }
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
	public string Name { get; set; }
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
