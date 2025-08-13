using System.IO.Pipes;

namespace Tunetastic;

/// <summary>
/// Provides the entry point configuration for the application startup process.
/// </summary>
public static class Startup
{
	/// <summary>
	/// The main entry point for the Tunetastic application that enforces single instance behavior.
	/// </summary>
	/// <param name="args">Command-line arguments passed to the application.</param>
	/// <remarks>
	/// Uses a mutex to prevent multiple instances from running simultaneously.
	/// If an existing instance is detected, it attempts to communicate with it via a named pipe
	/// by sending a PING message before terminating the current instance.
	/// If no existing instance is found, it proceeds to start a new application instance.
	/// </remarks>
	[STAThread]
	static void Main(string[] args)
	{
		bool createdNew;
		using var mutex = new Mutex(true, "Tunetastic.Mutex", out createdNew);

		if (!createdNew)
		{
			try
			{
				using var client = new NamedPipeClientStream(".", "Tunetastic.InstancePing", PipeDirection.Out);
				client.Connect(200);
				using var writer = new StreamWriter(client) { AutoFlush = true };
				writer.WriteLine("PING");
			}
			catch
			{
				//ignore
			}
			return;
		}

		Application.Start(p => new App());
	}
}
