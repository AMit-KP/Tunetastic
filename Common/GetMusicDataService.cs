using Google.Protobuf.Collections;
using Tunetastic.Generated.Protos;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace Tunetastic.Services;

/// <summary>
/// Provides services for managing and updating metadata related to music libraries in the application.
/// </summary>
internal class GetMusicDataService
{
    /// <summary>
    /// Updates the metadata of the music libraries, optionally triggered by a user request.
    /// </summary>
    /// <param name="onRequest">
    /// A boolean value indicating whether the update is manually triggered by a user request. If set to true,
    /// the metadata update is executed regardless of other conditions. Defaults to false.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> that represents the asynchronous operation of updating the metadata.
    /// </returns>
    public async Task UpdateMetaData(bool onRequest = false)
    {
        bool scanAtStartup = bool.Parse(Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.ScanAtStartup)]?.ToString() ?? "false");
        if (onRequest || scanAtStartup)
        {
            await ScanLibraries();
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Retrieves all music libraries stored in the system as a collection of <see cref="Library"/> objects.
    /// </summary>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous operation and contains
    /// a <see cref="RepeatedField{Library}"/> collection of all libraries.
    /// If an exception occurs, an empty collection is returned.
    /// </returns>
    private Task<RepeatedField<Library>> GetAllLibrariesAsync()
    {
        try
        {
            var LibrariesData = ProtobufData.LoadFromBin<LibraryList>(DataFile.AllLibraries).Libraries;

            return Task.FromResult(LibrariesData);
        }
        catch (Exception)
        {
            return Task.FromResult(new RepeatedField<Library>());
        }
    }

    /// <summary>
    /// Scans the music libraries to identify and process audio files, applying filters such as
    /// file format and optional configurations for ignoring duplicates or tracks below a certain duration.
    /// Updates the local settings and notifies the user with the scan results.
    /// </summary>
    /// <returns>
    /// A <see cref="Task"/> that represents the asynchronous operation of scanning the music libraries.
    /// </returns>
    private async Task ScanLibraries()
    {
        var audioFiles = new HashSet<string>();

        var libraries = new List<string>();

        foreach (var library in await GetAllLibrariesAsync())
        {
            libraries.Add(library.Path);
        }

        var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        var ignoreTrackDuration = double.Parse(localSettings.Values[nameof(LocalSave.IgnoreTracksBelowDuration)]?.ToString() ?? "0");
        var ignoreDuplicates = bool.Parse(localSettings.Values[nameof(LocalSave.IgnoreDuplicateEnabled)]?.ToString() ?? "false");


        var formatList = ProtobufData.LoadFromBin<FormatList>(DataFile.FormatsAllowed).Formatlist;

        List<string> extensions = new();

        foreach (var format in formatList)
            if (format.Enabled) extensions.Add(format.Extension);

        if (extensions.Count == 0) extensions.Add(".mp3");

        var path = Path.Combine(Constants.ThumbnailsFolder, ThumbnailFolder.AllSongView.ToString());
        if (Directory.Exists(path)) Directory.Delete(path, true);

        if (libraries?.Count > 0)
        {
            libraries = libraries.OrderBy(f => f.Length).ToList();

            var uniqueFolders = new List<string>();
            foreach (var folder in libraries)
            {
                if (!Directory.Exists(folder))
                {
                    GlobalNotification.Error("Library folder not found: " + folder + "\n Folder might be removed/renamed from system.");
                }
                else
                {
                    if (!uniqueFolders.Any(parent => folder.StartsWith(parent, StringComparison.OrdinalIgnoreCase)))
                        uniqueFolders.Add(folder);
                }
            }

            var options = new EnumerationOptions { RecurseSubdirectories = true };

            foreach (var folder in uniqueFolders)
            {
                var files = Directory.EnumerateFiles(folder, "*.*", options)
                                     .Where(file => extensions.Contains(Path.GetExtension(file).ToLower()));

                foreach (var file in files)
                {
                    audioFiles.Add(file);
                }
            }

            SongList songsContainer = new SongList();
            HashSet<(string Title, string Artist, string Album)>? uniqueMetadata = new HashSet<(string, string, string)>();

            foreach (var filePath in audioFiles)
            {
                try
                {
                    using (var audioModel = TagLib.File.Create(filePath))
                    {
                        var song = new Song
                        {
                            Title = audioModel.Tag.Title ?? Path.GetFileNameWithoutExtension(filePath),
                            Album = audioModel.Tag.Album ?? "Unknown Album",
                            Artists = (audioModel.Tag.Performers.Length > 0 ? audioModel.Tag.Performers[0] : audioModel.Tag.FirstAlbumArtist) ?? "Unknown Artist",
                            Duration = audioModel.Properties.Duration.TotalSeconds,
                            Path = filePath,
                            Year = audioModel.Tag.Year.ToString() ?? "Unknown Year",
                            Genre = audioModel.Tag.Genres.Length > 0 ? audioModel.Tag.Genres[0] : "Unknown Genre",
                            Cover = ImageResizer.CreateThumbnailImage(ThumbnailFolder.AllSongView, audioModel.Tag.Pictures, 100)
                        };

                        if (song.Duration > ignoreTrackDuration && (!ignoreDuplicates || uniqueMetadata.Add((song.Title, song.Artists, song.Album))))
                            songsContainer.Songs.Add(song);
                    }
                }
                catch (Exception)
                {
                    GlobalNotification.Error($"Failed to read metadata for:\n{filePath}");
                    double duration = 0;
                    try
                    {
                        var mediaPlayer = new MediaPlayer();
                        mediaPlayer.AutoPlay = false;
                        mediaPlayer.Source = MediaSource.CreateFromUri(new Uri(filePath));
                        duration = mediaPlayer.PlaybackSession.NaturalDuration.TotalSeconds;
                        mediaPlayer = null;
                    }
                    catch (Exception)
                    {
                        duration = 0;
                    }

                    var song = new Song
                    {
                        Title = Path.GetFileNameWithoutExtension(filePath),
                        Album = "Unknown Album",
                        Artists = "Unknown Artist",
                        Duration = duration,
                        Path = filePath,
                        Year = "Unknown Year",
                        Genre = "Unknown Genre",
                        Cover = ImageResizer.CreateThumbnailImage(ThumbnailFolder.AllSongView, null, 100)
                    };
                    if (song.Duration > ignoreTrackDuration && (!ignoreDuplicates || uniqueMetadata.Add((song.Title, song.Artists, song.Album))))
                        songsContainer.Songs.Add(song);
                }
            }

            try
            {
                ProtobufData.SaveToBin(DataFile.AllSongsMetaData, songsContainer);
            }
            catch (Exception)
            {
                localSettings.Values[nameof(LocalSave.ScanResult)] = "No tracks could be added";
                GlobalNotification.Error("No tracks could be added");
            }

            localSettings.Values[nameof(LocalSave.ScanResult)] = $"Libraries: {libraries.Count} Songs: {songsContainer.Songs.Count}";
            GlobalNotification.Info("Library scan completed.\nLibraries: " + libraries.Count + "\nSongs: " + songsContainer.Songs.Count);
        }
        else
        {
            localSettings.Values[nameof(LocalSave.ScanResult)] = "No libraries found";
            GlobalNotification.Warning("No libraries found. Please add atleast one library.");
        }
    }

}
