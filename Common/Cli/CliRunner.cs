namespace Tunetastic.Common.Cli;

/// <summary>
/// Entry point for the Tunetastic command line, called from <c>Startup.Main</c> before the GUI starts.
/// When the arguments form a CLI command they are executed and the process exits with a scriptable
/// exit code; otherwise control returns to the normal GUI startup.
/// </summary>
public static class CliRunner
{
	private const string Usage =
		"Usage:\n" +
		"  tunetastic playlist add \"<playlist>\" <file | pattern | folder> [more ...] [options]\n" +
		"\n" +
		"Adds local audio files to a playlist, importing all embedded metadata (title, artist,\n" +
		"album, year, genre, track number, lyrics, cover art, ...). The playlist is created when\n" +
		"it does not exist. Folders are scanned recursively; wildcards (C:\\Music\\*.mp3) work in\n" +
		"any shell. Prints a human-readable summary to stderr and a JSON report to stdout.\n" +
		"\n" +
		"Options:\n" +
		"  --no-create   Fail when the playlist does not exist instead of creating it.\n" +
		"  --dry-run     Report what would happen without writing anything.\n" +
		"  --help, -h    Show this help.\n" +
		"\n" +
		"Exit codes:\n" +
		"  0  All tracks added (or dry run / already present).\n" +
		"  1  Completed, but some inputs failed.\n" +
		"  2  Usage error or the command could not run.\n" +
		"\n" +
		"Examples:\n" +
		"  tunetastic playlist add \"My Playlist\" \"C:\\Music\\song.mp3\"\n" +
		"  tunetastic playlist add \"My Playlist\" \"C:\\Music\\*.mp3\"\n" +
		"  tunetastic playlist add \"My Playlist\" \"C:\\Music\\Album\" --dry-run";

	/// <summary>
	/// Runs the CLI command when <paramref name="args"/> starts one.
	/// </summary>
	/// <param name="args">The process command-line arguments (without the executable).</param>
	/// <returns>True when the arguments were a CLI command and the caller should exit.</returns>
	public static bool TryRun(string[] args)
	{
		if (args.Length == 0 || !string.Equals(args[0], "playlist", StringComparison.OrdinalIgnoreCase))
			return false;

		int exitCode;
		if (args.Length >= 2 && string.Equals(args[1], "add", StringComparison.OrdinalIgnoreCase))
			exitCode = RunPlaylistAdd(args.Skip(2).ToList());
		else
			exitCode = UsageError("Unknown playlist command. Did you mean 'tunetastic playlist add ...'?");

		Environment.ExitCode = exitCode;
		return true;
	}

	private static int RunPlaylistAdd(List<string> args)
	{
		bool createIfMissing = true;
		bool dryRun = false;
		var positional = new List<string>();

		foreach (var arg in args)
		{
			if (string.Equals(arg, "--no-create", StringComparison.OrdinalIgnoreCase))
				createIfMissing = false;
			else if (string.Equals(arg, "--dry-run", StringComparison.OrdinalIgnoreCase))
				dryRun = true;
			else if (string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase) || string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase))
			{
				Console.Out.WriteLine(Usage);
				return 0;
			}
			else if (arg.StartsWith("--", StringComparison.Ordinal))
				return UsageError($"Unknown option '{arg}'.");
			else
				positional.Add(arg);
		}

		var options = new CliPlaylistAddOptions { CreateIfMissing = createIfMissing, DryRun = dryRun };

		if (positional.Count < 2)
			return UsageError("Missing arguments. Expected: tunetastic playlist add \"<playlist>\" <file | pattern | folder> [...]");

		var playlistName = positional[0];
		var inputs = positional.Skip(1).ToList();

		PlaylistAddReport report;
		try
		{
			report = CliPlaylistAdd.AddAsync(playlistName, inputs, options).GetAwaiter().GetResult();
		}
		catch (CliFatalException ex)
		{
			Console.Error.WriteLine($"Error: {ex.Message}");
			return 2;
		}
		catch (OperationCanceledException)
		{
			Console.Error.WriteLine("Error: Cancelled.");
			return 2;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"Error: Unexpected failure: {ex.Message}");
			return 2;
		}

		report.WriteHuman(Console.Error);
		Console.Out.WriteLine(report.ToJson());

		return report.Failures.Count > 0 ? 1 : 0;
	}

	private static int UsageError(string message)
	{
		Console.Error.WriteLine($"Error: {message}");
		Console.Error.WriteLine();
		Console.Error.WriteLine(Usage);
		return 2;
	}
}
