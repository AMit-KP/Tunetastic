using System.IO.Pipes;

namespace Tunetastic;

/// <summary>
/// Provides the entry point configuration for the application startup process.
/// </summary>
public static class Startup
{
	/// <summary>
	/// The main entry point for the Tunetastic application that enforces single instance behavior
	/// for the GUI, or runs a CLI command (<c>tunetastic playlist add ...</c>) and exits.
	/// </summary>
	/// <param name="args">Command-line arguments passed to the application.</param>
	/// <remarks>
	/// CLI arguments are handled first by <see cref="CliRunner.TryRun"/>: the command runs against
	/// the shared database in this process (no GUI starts) and the exit code is set for scripting.
	/// Otherwise a mutex prevents multiple GUI instances from running simultaneously.
	/// If an existing instance is detected, it attempts to communicate with it via a named pipe
	/// by sending a PING message before terminating the current instance.
	/// If no existing instance is found, it proceeds to start a new application instance.
	/// </remarks>
	[STAThread]
	static void Main(string[] args)
	{
		if (CliRunner.TryRun(args))
			return;

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
