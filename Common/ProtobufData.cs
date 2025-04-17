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

    public static SongList LoadDataFromBin(DataFile dataFile)
    {
        if (!File.Exists(Constants.RootDirectoryPath + $"\\{dataFile}.bin")) return new SongList();

        using (FileStream input = File.OpenRead(Constants.RootDirectoryPath + $"\\{dataFile}.bin"))
        {
            return SongList.Parser.ParseFrom(input);
        }
    }

}

public enum DataFile
{
    AllSongsMetaData
}
