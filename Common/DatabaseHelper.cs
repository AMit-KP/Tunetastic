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
	/// - `QueuedPlayingList`: A table for managing a queue of songs to be played.
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
									   Position INTEGER DEFAULT 0,
									   PRIMARY KEY (PlaylistId, SongPath),
									   FOREIGN KEY (PlaylistId) REFERENCES Playlists(Id) ON DELETE CASCADE,
									   FOREIGN KEY (SongPath) REFERENCES Songs(Path) ON DELETE CASCADE)");

		await _database.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS QueuedPlayingList (
									   Id INTEGER PRIMARY KEY AUTOINCREMENT,
									   Path TEXT NOT NULL,
									   Position INTEGER,
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
	/// <c>YearModel</c> objects, where each object contains the year, a count
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
/// Represents data aggregation for a specific year, used for displaying and managing
/// year-based song statistics within the Tunetastic application.
/// This model includes properties to represent the year, the number of songs recorded
/// for that year, and the total playback duration of those songs.
/// </summary>
public class YearModel
{
	public string Year { get; set; }
	public int Count { get; set; }
	public double TotalDuration { get; set; }
}

/// <summary>
/// Represents a data model for a music genre. This model includes information about
/// the genre name, total count of songs in the genre, and the cumulative duration of all songs
/// within the genre. It is commonly used in operations or views related to genre-based song grouping in the application.
/// </summary>
public class GenreModel
{
	public string Genre { get; set; }
	public int Count { get; set; }
	public double TotalDuration { get; set; }
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
