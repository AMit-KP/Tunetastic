using System.Runtime.InteropServices;
using System.Text;

namespace Tunetastic.Common.Cli;

/// <summary>
/// Entry point for the Tunetastic command line, called from <c>Startup.Main</c> before the GUI starts.
/// When the arguments form a CLI command they are executed and the process exits with a scriptable
/// exit code; otherwise control returns to the normal GUI startup.
/// </summary>
public static class CliRunner
{
	private static string GetUsage()
	{
		bool color = PlaylistAddReport.IsColorOutputEnabled();
		if (!color)
		{
			return
				"Usage:\n" +
				"  tunetastic playlist add \"<playlist>\" <file | pattern | folder> [more ...] [options]\n" +
				"\n" +
				"Adds local audio files to a playlist, importing the metadata the library can store (title, artist,\n" +
				"album, year, genre, track number, lyrics, cover art, ...). The playlist is created when the\n" +
				"first track is added and it does not exist yet. Folders are scanned recursively; wildcards\n" +
				"(C:\\Music\\*.mp3) work in any shell. Files outside the configured library folders are skipped\n" +
				"unless --add-external is passed.\n" +
				"\n" +
				"Output: a human-readable report on stdout, with failed inputs also on stderr. Pass --json for\n" +
				"a single machine-readable JSON report on stdout (nothing else is printed).\n" +
				"\n" +
				"Options:\n" +
				"  --no-create       Fail when the playlist does not exist instead of creating it.\n" +
				"  --dry-run         Report what would happen without changing the library or playlists.\n" +
				"  --add-external    Also add files outside the configured library folders.\n" +
				"  --json            Print one JSON report on stdout instead of the human report.\n" +
				"  --                Treat all following arguments as inputs, even when they start with --.\n" +
				"  --help, -h        Show this help.\n" +
				"\n" +
				"Exit codes:\n" +
				"  0  Success: no inputs failed (check the added/skipped counts in the report).\n" +
				"  1  Completed, but some inputs failed.\n" +
				"  2  Usage error, the command could not run, or it was cancelled (Ctrl+C).\n" +
				"\n" +
				"Examples:\n" +
				"  tunetastic playlist add \"My Playlist\" \"C:\\Music\\song.mp3\"\n" +
				"  tunetastic playlist add \"My Playlist\" \"C:\\Music\\*.mp3\"\n" +
				"  tunetastic playlist add \"My Playlist\" \"C:\\Music\\Album\" --dry-run\n" +
				"  tunetastic playlist add \"My Playlist\" \"C:\\Music\\Album\" --dry-run --json";
		}

		return
			"\x1b[1;33mUsage:\x1b[0m\n" +
			"  \x1b[1;32mtunetastic playlist add\x1b[0m \x1b[36m\"<playlist>\"\x1b[0m \x1b[35m<file | pattern | folder>\x1b[0m \x1b[90m[more ...]\x1b[0m \x1b[33m[options]\x1b[0m\n" +
			"\n" +
			"Adds local audio files to a playlist, importing the metadata the library can store (title, artist,\n" +
			"album, year, genre, track number, lyrics, cover art, ...). The playlist is created when the\n" +
			"first track is added and it does not exist yet. Folders are scanned recursively; wildcards\n" +
			"(\x1b[35mC:\\Music\\*.mp3\x1b[0m) work in any shell. Files outside the configured library folders are skipped\n" +
			"unless \x1b[36m--add-external\x1b[0m is passed.\n" +
			"\n" +
			"\x1b[1;33mOutput:\x1b[0m a human-readable report on stdout, with failed inputs also on stderr. Pass \x1b[36m--json\x1b[0m for\n" +
			"a single machine-readable JSON report on stdout (nothing else is printed).\n" +
			"\n" +
			"\x1b[1;33mOptions:\x1b[0m\n" +
			"  \x1b[36m--no-create\x1b[0m       Fail when the playlist does not exist instead of creating it.\n" +
			"  \x1b[36m--dry-run\x1b[0m         Report what would happen without changing the library or playlists.\n" +
			"  \x1b[36m--add-external\x1b[0m    Also add files outside the configured library folders.\n" +
			"  \x1b[36m--json\x1b[0m            Print one JSON report on stdout instead of the human report.\n" +
			"  \x1b[36m--\x1b[0m                Treat all following arguments as inputs, even when they start with --.\n" +
			"  \x1b[36m--help, -h\x1b[0m        Show this help.\n" +
			"\n" +
			"\x1b[1;33mExit codes:\x1b[0m\n" +
			"  \x1b[1;32m0\x1b[0m  Success: no inputs failed (check the added/skipped counts in the report).\n" +
			"  \x1b[1;33m1\x1b[0m  Completed, but some inputs failed.\n" +
			"  \x1b[1;31m2\x1b[0m  Usage error, the command could not run, or it was cancelled (Ctrl+C).\n" +
			"\n" +
			"\x1b[1;33mExamples:\x1b[0m\n" +
			"  \x1b[1;32mtunetastic playlist add\x1b[0m \"My Playlist\" \"C:\\Music\\song.mp3\"\n" +
			"  \x1b[1;32mtunetastic playlist add\x1b[0m \"My Playlist\" \"C:\\Music\\*.mp3\"\n" +
			"  \x1b[1;32mtunetastic playlist add\x1b[0m \"My Playlist\" \"C:\\Music\\Album\" \x1b[36m--dry-run\x1b[0m\n" +
			"  \x1b[1;32mtunetastic playlist add\x1b[0m \"My Playlist\" \"C:\\Music\\Album\" \x1b[36m--dry-run --json\x1b[0m";
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool AttachConsole(int dwProcessId);

	private const int AttachParentProcess = -1;

	/// <summary>
	/// Runs the CLI command when <paramref name="args"/> starts one.
	/// </summary>
	/// <param name="args">The process command-line arguments (without the executable).</param>
	/// <returns>True when the arguments were a CLI command and the caller should exit.</returns>
	public static bool TryRun(string[] args)
	{
		if (args.Length == 0)
			return false;

		if (string.Equals(args[0], "--help", StringComparison.OrdinalIgnoreCase) || string.Equals(args[0], "-h", StringComparison.OrdinalIgnoreCase))
		{
			EnsureConsole();
			Console.Out.WriteLine(GetUsage());
			Environment.ExitCode = 0;
			return true;
		}

		if (!string.Equals(args[0], "playlist", StringComparison.OrdinalIgnoreCase))
			return false;

		EnsureConsole();

		int exitCode;
		try
		{
			if (args.Length >= 2 && string.Equals(args[1], "add", StringComparison.OrdinalIgnoreCase))
				exitCode = RunPlaylistAdd(args.Skip(2).ToList());
			else if (args.Length >= 2 && (string.Equals(args[1], "--help", StringComparison.OrdinalIgnoreCase) || string.Equals(args[1], "-h", StringComparison.OrdinalIgnoreCase)))
			{
				Console.Out.WriteLine(GetUsage());
				exitCode = 0;
			}
			else
				exitCode = UsageError("Unknown playlist command. Did you mean 'tunetastic playlist add ...'?");
		}
		catch (IOException)
		{
			// Headless launch without usable console handles: nothing can be reported.
			exitCode = 2;
		}

		Environment.ExitCode = exitCode;
		return true;
	}

	/// <summary>
	/// Attaches the parent console (when launched from one with a visible console) and selects UTF-8
	/// output so tag content renders correctly. Best effort: never fails the command.
	/// </summary>
	private static void EnsureConsole()
	{
		try
		{
			Console.OutputEncoding = Encoding.UTF8;
			if (!Console.IsErrorRedirected)
				AttachConsole(AttachParentProcess);
		}
		catch
		{
			// Headless launch or no console: output handles simply go nowhere.
		}
	}

	private static int RunPlaylistAdd(List<string> args)
	{
		bool createIfMissing = true;
		bool dryRun = false;
		bool addExternal = false;
		bool jsonOutput = false;
		var positional = new List<string>();

		for (int i = 0; i < args.Count; i++)
		{
			var arg = args[i];
			if (arg == "--")
			{
				positional.AddRange(args.Skip(i + 1));
				break;
			}
			if (string.Equals(arg, "--no-create", StringComparison.OrdinalIgnoreCase))
				createIfMissing = false;
			else if (string.Equals(arg, "--dry-run", StringComparison.OrdinalIgnoreCase))
				dryRun = true;
			else if (string.Equals(arg, "--add-external", StringComparison.OrdinalIgnoreCase))
				addExternal = true;
			else if (string.Equals(arg, "--json", StringComparison.OrdinalIgnoreCase))
				jsonOutput = true;
			else if (string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase) || string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase))
			{
				Console.Out.WriteLine(GetUsage());
				return 0;
			}
			else if (arg.StartsWith("--", StringComparison.Ordinal))
				return UsageError($"Unknown option '{arg}'.");
			else
				positional.Add(arg);
		}

		var options = new CliPlaylistAddOptions { CreateIfMissing = createIfMissing, DryRun = dryRun, AddExternal = addExternal };

		if (positional.Count < 2)
			return UsageError("Missing arguments. Expected: tunetastic playlist add \"<playlist>\" <file | pattern | folder> [...]");

		var playlistName = positional[0];
		var inputs = positional.Skip(1).ToList();

		PlaylistAddReport report;
		using var cts = new CancellationTokenSource();
		int cancelPresses = 0;
		void CancelHandler(object? sender, ConsoleCancelEventArgs e)
		{
			// First Ctrl+C cancels gracefully; a second one lets the process die.
			if (++cancelPresses > 1)
				return;
			e.Cancel = true;
			cts.Cancel();
		}
		Console.CancelKeyPress += CancelHandler;
		try
		{
			report = CliPlaylistAdd.AddAsync(playlistName, inputs, options, cts.Token).GetAwaiter().GetResult();
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
		finally
		{
			Console.CancelKeyPress -= CancelHandler;
		}

		// Success output: either one clean JSON document on stdout, or the human report with its
		// ordinary lines on stdout and the failed inputs (also) on stderr.
		if (jsonOutput)
		{
			Console.Out.WriteLine(report.ToJson());
		}
		else
		{
			report.WriteHuman(Console.Out, Console.Error);
		}

		return report.Failures.Count > 0 ? 1 : 0;
	}

	private static int UsageError(string message)
	{
		Console.Error.WriteLine($"Error: {message}");
		Console.Error.WriteLine();
		Console.Error.WriteLine(GetUsage());
		return 2;
	}
}
