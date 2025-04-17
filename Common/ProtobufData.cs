using Google.Protobuf;
using Tunetastic.Generated.Protos;

namespace Tunetastic.Common;
public class ProtobufData
{
    public static void SaveDataInBin(List<Song> songList)
    {
        SongList songsContainer = new SongList();
        songsContainer.Songs.AddRange(songList);

        using (FileStream output = File.Create(Constants.RootDirectoryPath + $"\\{DataFile.AllSongsMetaData}.bin"))
        {
            songsContainer.WriteTo(output);
        }
    }

    public static SongList LoadSongMetaDataFromBin(DataFile dataFile)
    {
        try
        {
            using (FileStream fileStream = new FileStream(Constants.RootDirectoryPath + $"\\{dataFile}.bin", FileMode.Open, FileAccess.Read, FileShare.Read))
            using (BufferedStream bufferedStream = new BufferedStream(fileStream))
                return SongList.Parser.ParseFrom(bufferedStream);
        }
        catch (Exception)
        {
            return new SongList();
        }
    }

}

public enum DataFile
{
    AllSongsMetaData
}
