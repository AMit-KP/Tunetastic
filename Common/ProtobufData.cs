using Google.Protobuf;

namespace Tunetastic.Common;

/// <summary>
/// Provides methods for serializing and deserializing Protocol Buffer data to and from binary files.
/// </summary>
public class ProtobufData
{
	/// <summary>
	/// Saves the provided data to a binary file.
	/// </summary>
	/// <typeparam name="T">The type of data to be saved, which must implement IMessage&lt;T&gt;.</typeparam>
	/// <param name="FileName">The enum value representing the binary file to save the data into.</param>
	/// <param name="data">The data object of type T to save.</param>
	public static void SaveToBin<T>(DataFile FileName, T data) where T : IMessage<T>
	{
		using FileStream fileStream = new FileStream(Constants.RootDirectoryPath + $"\\{FileName}.bin", FileMode.Create, FileAccess.Write, FileShare.None);
		using BufferedStream bufferedStream = new BufferedStream(fileStream);
		data.WriteTo(bufferedStream);
	}

	/// <summary>
	/// Loads data from a binary file using Protocol Buffers serialization.
	/// This method reads from the binary file associated with the specified
	/// `DataFile` enumeration value and parses it into an object of type `T`.
	/// </summary>
	/// <typeparam name="T">The type of message to load. Must implement `IMessage&lt;T&gt;` and have a parameterless constructor.</typeparam>
	/// <param name="FileName">The name of the binary file to load, specified as a `DataFile` enum value.</param>
	/// <returns>An instance of type `T` parsed from the binary file. Returns a new instance of `T` if the file is missing, empty, or an error occurs during parsing.</returns>
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

	/// <summary>
	/// Deletes the specified binary data file if it exists.
	/// </summary>
	/// <param name="FileName">The enum value representing the binary file to be deleted.</param>
	public static void DeleteBinFile(DataFile FileName)
	{
		try
		{
			File.Delete(Constants.RootDirectoryPath + $"\\{FileName}.bin");
		}
		catch (Exception)
		{
			//ignored
		}
	}

}

/// <summary>
/// Enumerates the types of binary data files managed by the application.
/// Each value represents a specific category of data stored and retrieved
/// using Protocol Buffers serialization.
/// </summary>
public enum DataFile
{
	AllSongsMetaData,
	AllLibraries,
	FormatsAllowed,
	CustomPlayLists
}
