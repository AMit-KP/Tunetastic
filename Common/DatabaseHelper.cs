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
										Path TEXT PRIMARY KEY,
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
	/// Loads all songs from the database and sorts them based on the specified property and order.
	/// </summary>
	/// <param name="songProperty">The property to sort the songs by, such as Title, Artists, or Album. Defaults to Title.</param>
	/// <param name="ascending">A boolean indicating whether the songs should be sorted in ascending order. Defaults to true.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains a list of songs ordered by the specified property and order.
	/// If an exception occurs, an empty list is returned.</returns>
	public async Task<List<Song>> LoadSongsFromDB(SongProperty songProperty = SongProperty.Title, bool ascending = true)
	{
		try
		{
			return await _database.QueryAsync<Song>($"SELECT * FROM Songs ORDER BY {songProperty.ToString()} {(ascending ? "ASC" : "DESC")}");
		}
		catch (Exception)
		{
			return new List<Song>();
		}
	}

	/// <summary>
	/// Loads the paths of all songs from the database, ordered by the specified song property and sort direction.
	/// </summary>
	/// <param name="songProperty">The property of the song used for sorting, such as Title, Artists, or Album. Defaults to Title.</param>
	/// <param name="ascending">Indicates whether the sorting should be in ascending order. Defaults to true.</param>
	/// <returns>
	/// A task that represents the asynchronous operation, returning a list of strings containing the paths of the songs.
	/// If the operation fails, an empty list is returned.
	/// </returns>
	public async Task<List<string>> LoadSongPathsFromDB(SongProperty songProperty = SongProperty.Title, bool ascending = true)
	{
		try
		{
			return (await _database.QueryAsync<Song>($"SELECT Path FROM Songs ORDER BY {songProperty.ToString()} {(ascending ? "ASC" : "DESC")}")).Select(s => s.Path).ToList();
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

	public async Task CleanupOrphanedPlaylistEntriesAsync()
	{
		await _database.ExecuteAsync("DELETE FROM PlaylistSongs WHERE SongPath NOT IN (SELECT Path FROM Songs)");
	}

	public async Task IncrementPlayCountAsync(string songPath)
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
	public async Task RemovePlaylistAsync(string playlistName)
	{
		await _database.ExecuteAsync("DELETE FROM Playlists WHERE Name = ?", playlistName);
		await _database.ExecuteAsync("DELETE FROM PlaylistSongs WHERE PlaylistId IN (SELECT Id FROM Playlists WHERE Name = ?)", playlistName);
	}

	public async Task AddSongsToQueuedPlayingList(string songPath)
	{
		await _database.ExecuteAsync("INSERT OR REPLACE INTO QueuedPlayingList (Path) VALUES (?)", songPath);
	}

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

	public async Task<List<string>> GetQueuedPlayingList()
	{
		var result = await _database.QueryAsync<Song>("SELECT Path FROM QueuedPlayingList ORDER BY rowid");
		return result.Select(song => song.Path).ToList();
	}

	public async Task RemoveFromQueuedPlayingList(string songPath)
	{
		await _database.ExecuteAsync("DELETE FROM QueuedPlayingList WHERE Path = ?", songPath);
	}

	public async Task AddSongToPlaylistAsync(int playlistId, string songPath)
	{
		await _database.ExecuteAsync("INSERT INTO PlaylistSongs (PlaylistId, SongPath) VALUES (?, ?)", playlistId, songPath);
	}

	public async Task RemoveSongFromPlaylistAsync(int playlistId, string songPath)
	{
		await _database.ExecuteAsync("DELETE FROM PlaylistSongs WHERE PlaylistId = ? AND SongPath = ?", playlistId, songPath);
	}

	public async Task<List<Song>> GetSongsInPlaylistAsync(int playlistId)
	{
		return await _database.QueryAsync<Song>("SELECT S.* FROM Songs S INNER JOIN PlaylistSongs P ON S.Path = P.SongPath WHERE P.PlaylistId = ?", playlistId);
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
