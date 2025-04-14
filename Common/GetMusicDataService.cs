using Nucs.JsonSettings;
using Windows.Storage;

namespace Tunetastic.Services;

internal class GetMusicDataService
{
    public async Task UpdateMetaData(bool onRequest = false)
    {
        bool scanAtStartup;
        try
        {
            scanAtStartup = LibrarySettingsSaver.Instance.LibrarySaveSettings.ScanAtStartup == null ? false : LibrarySettingsSaver.Instance.LibrarySaveSettings.ScanAtStartup;
        }
        catch (Exception)
        {
            scanAtStartup = false;
        }
        if (onRequest || scanAtStartup)
        {
            var AllSongs = await ScanLibraries();

            var songlist = JsonSettings.Load<AllSongList>();
            songlist.SongPaths?.Clear();
            songlist.Save();
            songlist.SongPaths = AllSongs;
            songlist.Save();
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

    private async Task<List<string>> ScanLibraries()
    {
        List<string> allSongs = new();
        int totalfolders = 0;
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

        StorageFolder localFolder = ApplicationData.Current.LocalFolder;
        StorageFolder thumbnailFolder = await localFolder.CreateFolderAsync("AllSongViewThumbnails", CreationCollisionOption.OpenIfExists);

        if (thumbnailFolder != null)
            await thumbnailFolder.DeleteAsync(StorageDeleteOption.PermanentDelete);

        if (libraries != null && libraries.Count > 0)
        {
            var filteredFolders = FilterSubfolders(libraries);

            if (filteredFolders.Count > 0)
            {
                foreach (var paths in filteredFolders)
                {
                    var (allsongs, foldercount) = CountFilesAndSubfolders(paths, ignoreTrackDuration);
                    totalfolders += foldercount;
                    allSongs.AddRange(allsongs);
                    allsongs = null;
                }
            }


            if (ignoreDuplicates)
            {
                HashSet<(string Title, string Artist, string Album)>? uniqueMetadata = new HashSet<(string, string, string)>();

                List<string>? distinctSongs = new();

                foreach (var songPath in allSongs)
                {
                    using (var audioModel = TagLib.File.Create(songPath))
                    {
                        var metadata = (audioModel.Tag.Title ?? audioModel.Name
                            , audioModel.Tag.Performers.Length > 0 ? audioModel.Tag.Performers[0] : audioModel.Tag.FirstAlbumArtist
                            , audioModel.Tag.Album ?? "Unknown Album");

                        if (uniqueMetadata.Add(metadata))
                        {
                            distinctSongs.Add(songPath);
                        }
                    }
                }

                allSongs = distinctSongs;
                distinctSongs = null;
                uniqueMetadata = null;
            }
            allSongs = allSongs.OrderBy(path => TagLib.File.Create(path).Tag.Title ?? Path.GetFileNameWithoutExtension(path)).ToList();
            LibrarySettingsSaver.Instance.LibrarySaveSettings.ScanResult = $"Libraries: {libraries.Count} Folders: {totalfolders} Songs: {allSongs.Count}";
            LibrarySettingsSaver.Instance.LibrarySaveSettings.totalTracks = allSongs.Count;
        }
        else
        {
            LibrarySettingsSaver.Instance.LibrarySaveSettings.ScanResult = "No libraries found";
        }
        LibrarySettingsSaver.Instance.SaveSettings();
        return allSongs;
    }

    private (List<string> files, int subfolderCount) CountFilesAndSubfolders(string folderPath, double ignoreTrackDuration)
    {
        List<string> extensions = new();        //TODO extension list
        extensions.Add("mp3");
        extensions.Add("m4a");
        extensions.Add("flac");
        extensions.Add("wav");
        extensions.Add("wma");
        extensions.Add("aac");

        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"Directory '{folderPath}' not found.");   //TODO add notification
        }

        int FolderCount = 0;
        List<string> files = new();

        foreach (string extension in extensions)
        {
            var x = from file in Directory.GetFiles(folderPath)
                    where Path.GetExtension(file).ToLower().Equals($".{extension}", StringComparison.OrdinalIgnoreCase) && TagLib.File.Create(file).Properties.Duration.TotalSeconds > ignoreTrackDuration
                    select file;

            files.AddRange(x.ToList());
        }

        if (files.Count > 0)
            FolderCount++;

        var subDirectories = Directory.GetDirectories(folderPath).ToList();

        if (subDirectories.Count > 0)
        {
            foreach (string subDir in subDirectories)
            {
                var (subFolderFiles, subSubfolderCount) = CountFilesAndSubfolders(subDir, ignoreTrackDuration);
                files.AddRange(subFolderFiles);
                FolderCount += subSubfolderCount;
            }
        }

        return (files, FolderCount);
    }

    private List<string> FilterSubfolders(List<string> folders)
    {
        List<string> filteredFolders = new(folders);

        foreach (var folder in folders)
        {
            foreach (var subfolder in folders)
            {
                if (subfolder.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && subfolder != folder)
                {
                    filteredFolders.Remove(subfolder);
                }
            }
        }

        return filteredFolders;
    }

}
