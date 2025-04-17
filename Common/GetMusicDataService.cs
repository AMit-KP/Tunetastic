using Tunetastic.Generated.Protos;
using Windows.Storage;

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
    private Task<List<string>> GetAllLibrariesAsync()
    {
        try
        {
            List<string> LibrariesData = new();
            foreach (var item in LibrarySettingsSaver.Instance.LibrarySaveSettings.LibraryPaths)
            {
                LibrariesData.Add(item.Path);
            }

            return Task.FromResult(LibrariesData);
        }
        catch (Exception)
        {
            return Task.FromResult(new List<string>());
        }
    }

    private async Task ScanLibraries()
    {
        var audioFiles = new HashSet<string>();

        var libraries = await GetAllLibrariesAsync();

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

        List<string> extensions = new();        //TODO extension list
        extensions.Add(".mp3");
        extensions.Add(".m4a");
        extensions.Add(".flac");
        extensions.Add(".wav");
        extensions.Add(".wma");
        extensions.Add(".aac");

        StorageFolder localFolder = ApplicationData.Current.LocalFolder;
        StorageFolder thumbnailFolder = await localFolder.CreateFolderAsync("AllSongViewThumbnails", CreationCollisionOption.OpenIfExists);

        if (thumbnailFolder != null)
            await thumbnailFolder.DeleteAsync(StorageDeleteOption.PermanentDelete);

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

            List<Song> musicDatas = new();
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
                            Duration = audioModel.Properties.Duration.TotalSeconds,
                            Path = filePath,
                            Year = audioModel.Tag.Year.ToString() ?? "Unknown Year",
                            Genre = audioModel.Tag.Genres.Length > 0 ? audioModel.Tag.Genres[0] : "Unknown Genre"
                        };
                        song.Artists.Add((audioModel.Tag.Performers.Length > 0 ? audioModel.Tag.Performers[0] : audioModel.Tag.FirstAlbumArtist) ?? "Unknown Artist");

                        if (song.Duration > ignoreTrackDuration && (!ignoreDuplicates || uniqueMetadata.Add((song.Title, song.Artists[0], song.Album))))
                            musicDatas.Add(song);
                    }
                }
                catch (Exception)
                {
                    //TODO add notification
                }
            }

            ProtobufData.SaveDataInBin(musicDatas);

            LibrarySettingsSaver.Instance.LibrarySaveSettings.ScanResult = $"Libraries: {libraries.Count} Songs: {musicDatas.Count}";
            LibrarySettingsSaver.Instance.LibrarySaveSettings.totalTracks = musicDatas.Count;
        }
        else
        {
            LibrarySettingsSaver.Instance.LibrarySaveSettings.ScanResult = "No libraries found";
            //TODO add notification
        }
        LibrarySettingsSaver.Instance.SaveSettings();
    }

}
