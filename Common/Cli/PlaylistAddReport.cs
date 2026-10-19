using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tunetastic.Common.Cli;

/// <summary>
/// The outcome of one <c>playlist add</c> run: one entry per track plus input-level failures.
/// <see cref="WriteHuman"/> renders a human-readable report (ordinary lines to one writer, failed
/// inputs to a second), same data as <see cref="ToJson"/> serializes for scripting.
/// </summary>
public sealed class PlaylistAddReport
{
	/// <summary>Gets the playlist the tracks were added to.</summary>
	public string Playlist { get; init; } = string.Empty;

	/// <summary>Gets whether the playlist was created by this run.</summary>
	public bool PlaylistCreated { get; init; }

	/// <summary>Gets whether a dry run would create the playlist.</summary>
	public bool PlaylistWouldBeCreated { get; init; }

	/// <summary>Gets whether this was a dry run (the library and playlists were not changed).</summary>
	public bool DryRun { get; init; }

	/// <summary>Gets the per-track outcomes.</summary>
	public List<TrackEntry> Tracks { get; init; } = new();

	/// <summary>Gets the inputs that failed before any metadata could be read (missing files, unreadable files, ...).</summary>
	public List<InputFailure> Failures { get; init; } = new();

	/// <summary>Gets run-level notes that are not tied to a single track (settings fallbacks, ...).</summary>
	public List<string> Notices { get; init; } = new();

	/// <summary>Gets the number of tracks added to the playlist.</summary>
	public int AddedCount => Tracks.Count(t => t.Status == TrackStatus.Added || t.Status == TrackStatus.WouldAdd);

	/// <summary>Gets the number of tracks skipped by policy (duplicate, too short, disabled format, ...).</summary>
	public int SkippedCount => Tracks.Count(t => t.Status == TrackStatus.Skipped || t.Status == TrackStatus.AlreadyPresent);

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
		Converters = { new JsonStringEnumConverter() },
	};

	/// <summary>Serializes the report to indented JSON for stdout.</summary>
	public string ToJson()
	{
		return JsonSerializer.Serialize(this, JsonOptions);
	}

	private static class Ansi
	{
		public static readonly bool Enabled = Environment.GetEnvironmentVariable("NO_COLOR") == null;

		public static string Reset => Enabled ? "\x1b[0m" : "";
		public static string Bold => Enabled ? "\x1b[1m" : "";
		public static string Dim => Enabled ? "\x1b[90m" : "";
		public static string Red => Enabled ? "\x1b[31m" : "";
		public static string BoldRed => Enabled ? "\x1b[1;31m" : "";
		public static string Green => Enabled ? "\x1b[32m" : "";
		public static string BoldGreen => Enabled ? "\x1b[1;32m" : "";
		public static string Yellow => Enabled ? "\x1b[33m" : "";
		public static string BoldYellow => Enabled ? "\x1b[1;33m" : "";
		public static string BrightYellow => Enabled ? "\x1b[1;93m" : "";
		public static string Blue => Enabled ? "\x1b[34m" : "";
		public static string BoldBlue => Enabled ? "\x1b[1;34m" : "";
		public static string Cyan => Enabled ? "\x1b[36m" : "";
		public static string BoldCyan => Enabled ? "\x1b[1;36m" : "";
		public static string BoldWhite => Enabled ? "\x1b[1;37m" : "";
		public static string BrightWhite => Enabled ? "\x1b[97m" : "";
		public static string CodeBg => Enabled ? "\x1b[48;5;236m" : "";
	}

	/// <summary>
	/// Formats a tag line so that quoted values ('value') are rendered in a terminal code-block badge
	/// (dark background + bright text), mimicking inline markdown `code` blocks.
	/// </summary>
	private static string FormatTagLine(string line)
	{
		if (!Ansi.Enabled)
			return Clean(line);

		int firstQuote = line.IndexOf('\'');
		if (firstQuote >= 0)
		{
			int lastQuote = line.IndexOf('\'', firstQuote + 1);
			if (lastQuote > firstQuote)
			{
				string prefix = Clean(line.Substring(0, firstQuote));
				string value = Clean(line.Substring(firstQuote + 1, lastQuote - firstQuote - 1));
				string suffix = Clean(line.Substring(lastQuote + 1));

				string codeBadge = $"{Ansi.CodeBg}{Ansi.BrightWhite} {value} {Ansi.Reset}";
				return $"{prefix}{codeBadge}{Ansi.Dim}{suffix}{Ansi.Reset}";
			}
		}

		return Clean(line);
	}

	/// <summary>
	/// Writes a human-readable rendering of the report: header, tracks, notices and the final tally
	/// go to <paramref name="standard"/>; failed inputs also go to <paramref name="errors"/>.
	/// Tag-derived text is sanitized so control characters cannot corrupt the terminal.
	/// </summary>
	public void WriteHuman(TextWriter standard, TextWriter errors)
	{
		if (DryRun)
		{
			var wouldCreate = PlaylistWouldBeCreated ? $" {Ansi.Yellow}(would be created){Ansi.Reset}" : "";
			standard.WriteLine($"{Ansi.Cyan}Dry run for playlist '{Ansi.BoldWhite}{Clean(Playlist)}{Ansi.Reset}{Ansi.Cyan}'{wouldCreate}:{Ansi.Reset}");
		}
		else
		{
			var created = PlaylistCreated ? $" {Ansi.Green}(created){Ansi.Reset}" : "";
			standard.WriteLine($"{Ansi.Cyan}Playlist '{Ansi.BoldWhite}{Clean(Playlist)}{Ansi.Reset}{Ansi.Cyan}':{created}{Ansi.Reset}");
		}

		foreach (var track in Tracks)
		{
			var (label, color) = track.Status switch
			{
				TrackStatus.Added => ("Added", Ansi.BoldGreen),
				TrackStatus.WouldAdd => ("Would add", Ansi.BoldCyan),
				TrackStatus.AlreadyPresent => ("Already in playlist", Ansi.BoldBlue),
				_ => ("Skipped", Ansi.BoldYellow),
			};

			standard.WriteLine($"  [{color}{label}{Ansi.Reset}] {Ansi.BoldWhite}{Clean(track.Title)}{Ansi.Reset} {Ansi.Dim}({Clean(track.Path)}){Ansi.Reset}");
			if (!string.IsNullOrWhiteSpace(track.Reason))
				standard.WriteLine($"    {Ansi.Yellow}Reason:{Ansi.Reset} {Clean(track.Reason)}");
			foreach (var line in track.Imported)
				standard.WriteLine($"    {Ansi.Green}+{Ansi.Reset} {FormatTagLine(line)}");
			foreach (var line in track.Defaulted)
				standard.WriteLine($"    {Ansi.Dim}~{Ansi.Reset} {FormatTagLine(line)}");
			foreach (var line in track.Skipped)
				standard.WriteLine($"    {Ansi.Yellow}-{Ansi.Reset} {FormatTagLine(line)}");
			foreach (var line in track.Warnings)
				standard.WriteLine($"    {Ansi.BrightYellow}! {Clean(line)}{Ansi.Reset}");
		}

		foreach (var notice in Notices)
			standard.WriteLine($"  {Ansi.Cyan}Note:{Ansi.Reset} {Clean(notice)}");

		foreach (var failure in Failures)
			errors.WriteLine($"  [{Ansi.BoldRed}Failed{Ansi.Reset}] {Clean(failure.Input)}: {Clean(failure.Reason)}");

		int skipped = Tracks.Count(t => t.Status == TrackStatus.Skipped);
		int present = Tracks.Count(t => t.Status == TrackStatus.AlreadyPresent);
		int failed = Failures.Count;

		var addedText = DryRun
			? (AddedCount > 0 ? $"{Ansi.BoldCyan}{AddedCount} would be added{Ansi.Reset}" : $"{Ansi.Dim}0 would be added{Ansi.Reset}")
			: (AddedCount > 0 ? $"{Ansi.BoldGreen}{AddedCount} added{Ansi.Reset}" : $"{Ansi.Dim}0 added{Ansi.Reset}");

		var skippedText = skipped > 0 ? $"{Ansi.Yellow}{skipped} skipped{Ansi.Reset}" : $"{Ansi.Dim}0 skipped{Ansi.Reset}";
		var presentText = present > 0 ? $"{Ansi.Blue}{present} already in playlist{Ansi.Reset}" : $"{Ansi.Dim}0 already in playlist{Ansi.Reset}";
		var failedText = failed > 0 ? $"{Ansi.BoldRed}{failed} failed{Ansi.Reset}" : $"{Ansi.Dim}0 failed{Ansi.Reset}";

		standard.WriteLine($"{Ansi.Bold}Done:{Ansi.Reset} {addedText}, {skippedText}, {presentText}, {failedText}.");
	}

	/// <summary>
	/// Replaces control and invisible format characters (including bidi marks) with spaces so hostile
	/// tag content cannot spoof terminal output.
	/// </summary>
	private static string Clean(string? value)
	{
		if (string.IsNullOrEmpty(value))
			return string.Empty;
		return new string(value.Select(c => char.IsControl(c) || char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.Format ? ' ' : c).ToArray());
	}
}

/// <summary>
/// The outcome for a single track.
/// </summary>
public sealed class TrackEntry
{
	/// <summary>Gets the full path of the track file.</summary>
	public string Path { get; init; } = string.Empty;

	/// <summary>Gets the track title (or file name when the tags had none).</summary>
	public string Title { get; init; } = string.Empty;

	/// <summary>Gets what happened to the track.</summary>
	public TrackStatus Status { get; init; }

	/// <summary>Gets why the track was skipped, when <see cref="Status"/> is <see cref="TrackStatus.Skipped"/>.</summary>
	public string? Reason { get; init; }

	/// <summary>Gets the embedded values that were imported.</summary>
	public List<string> Imported { get; init; } = new();

	/// <summary>Gets the fields that fell back to a default.</summary>
	public List<string> Defaulted { get; init; } = new();

	/// <summary>Gets the embedded values that have no library column and were not stored.</summary>
	public List<string> Skipped { get; init; } = new();

	/// <summary>Gets non-fatal warnings for the track.</summary>
	public List<string> Warnings { get; init; } = new();
}

/// <summary>
/// What happened to a single track during <c>playlist add</c>.
/// </summary>
public enum TrackStatus
{
	/// <summary>The track was imported and added to the playlist.</summary>
	Added,
	/// <summary>Dry run: the track would have been added.</summary>
	WouldAdd,
	/// <summary>The track was already in the playlist.</summary>
	AlreadyPresent,
	/// <summary>The track was skipped by policy (duplicate, too short, disabled format).</summary>
	Skipped,
}

/// <summary>
/// An input that failed before any metadata could be read.
/// </summary>
public sealed class InputFailure
{
	/// <summary>Gets the input (file path or pattern) that failed.</summary>
	public string Input { get; init; } = string.Empty;

	/// <summary>Gets why it failed.</summary>
	public string Reason { get; init; } = string.Empty;
}
