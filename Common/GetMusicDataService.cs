using Google.Protobuf.Collections;
using Tunetastic.Generated.Protos;

namespace Tunetastic.Services;

internal class GetMusicDataService
{
    public async Task UpdateMetaData(bool onRequest = false)
    {
        bool scanAtStartup;
        try
        {
            scanAtStartup = LibrarySettingsSaver.Instance.LibrarySaveSettings.ScanAtStartup != null && LibrarySettingsSaver.Instance.LibrarySaveSettings.ScanAtStartup;
        }
        catch (Exception)
        {
            scanAtStartup = false;
        }
        if (onRequest || scanAtStartup)
        {
            await ScanLibraries();
            await Task.CompletedTask;
        }
    }
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

    private async Task ScanLibraries()
    {
        var audioFiles = new HashSet<string>();

        var libraries = new List<string>();

        foreach (var library in await GetAllLibrariesAsync())
        {
            libraries.Add(library.Path);
        }

        double ignoreTrackDuration;
        bool ignoreDuplicates;
        try
        {
            ignoreTrackDuration = LibrarySettingsSaver.Instance.LibrarySaveSettings.ignoreTracksBelowDuration == null ? 0 : LibrarySettingsSaver.Instance.LibrarySaveSettings.ignoreTracksBelowDuration;
            ignoreDuplicates = LibrarySettingsSaver.Instance.LibrarySaveSettings.IgnoreDuplicateEnabled == null ? false : LibrarySettingsSaver.Instance.LibrarySaveSettings.IgnoreDuplicateEnabled;
        }
        catch (Exception)
        {
            ignoreTrackDuration = 0;
            ignoreDuplicates = false;
        }

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
                    throw new DirectoryNotFoundException($"Directory '{folder}' not found.");   //TODO add notification
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
                            Title = audioModel.Tag.Title ?? audioModel.Name,
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
                    //TODO add notification
                }
            }

            try
            {
                ProtobufData.SaveToBin(DataFile.AllSongsMetaData, songsContainer);
            }
            catch (Exception)
            {
                LibrarySettingsSaver.Instance.LibrarySaveSettings.ScanResult = "No tracks could be added";
                //TODO add notification
            }

            LibrarySettingsSaver.Instance.LibrarySaveSettings.ScanResult = $"Libraries: {libraries.Count} Songs: {songsContainer.Songs.Count}";
            LibrarySettingsSaver.Instance.LibrarySaveSettings.totalTracks = songsContainer.Songs.Count;
        }
        else
        {
            LibrarySettingsSaver.Instance.LibrarySaveSettings.ScanResult = "No libraries found";
            //TODO add notification
        }
        LibrarySettingsSaver.Instance.SaveSettings();
    }

}
