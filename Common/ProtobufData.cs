using Google.Protobuf;

namespace Tunetastic.Common;
public class ProtobufData
{
    public static void SaveToBin<T>(DataFile FileName, T data) where T : IMessage<T>
    {
        using FileStream fileStream = new FileStream(Constants.RootDirectoryPath + $"\\{FileName}.bin", FileMode.Create, FileAccess.Write, FileShare.None);
        using BufferedStream bufferedStream = new BufferedStream(fileStream);
        data.WriteTo(bufferedStream);
    }

    public static T LoadFromBin<T>(DataFile FileName) where T : IMessage<T>, new()
    {
        try
        {
            using FileStream fileStream = new FileStream(Constants.RootDirectoryPath + $"\\{FileName}.bin", FileMode.Open, FileAccess.Read, FileShare.Read);
            using BufferedStream bufferedStream = new BufferedStream(fileStream);
            return new MessageParser<T>(() => new T()).ParseFrom(bufferedStream);
        }
        catch (Exception)
        {
            return new T();
        }
    }

}

public enum DataFile
{
    AllSongsMetaData,
    AllLibraries
}
