using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tunetastic.Common.Cli;

/// <summary>
/// The outcome of one <c>playlist add</c> run: one entry per track plus input-level failures.
/// Serializes to JSON on stdout for scripting; <see cref="WriteHuman"/> renders the same data for humans on stderr.
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

	/// <summary>
	/// Writes a human-readable rendering of the report to <paramref name="writer"/>.
	/// Tag-derived text is sanitized so control characters cannot corrupt the terminal.
	/// </summary>
	public void WriteHuman(TextWriter writer)
	{
		if (DryRun)
			writer.WriteLine($"Dry run for playlist '{Clean(Playlist)}'{(PlaylistWouldBeCreated ? " (would be created)" : "")}:");
		else
			writer.WriteLine($"Playlist '{Clean(Playlist)}':{(PlaylistCreated ? " (created)" : "")}");

		foreach (var track in Tracks)
		{
			var label = track.Status switch
			{
				TrackStatus.Added => "Added",
				TrackStatus.WouldAdd => "Would add",
				TrackStatus.AlreadyPresent => "Already in playlist",
				_ => "Skipped",
			};
			writer.WriteLine($"  [{label}] {Clean(track.Title)} ({Clean(track.Path)})");
			if (!string.IsNullOrWhiteSpace(track.Reason))
				writer.WriteLine($"    Reason: {Clean(track.Reason)}");
			foreach (var line in track.Imported)
				writer.WriteLine($"    + {Clean(line)}");
			foreach (var line in track.Defaulted)
				writer.WriteLine($"    ~ {Clean(line)}");
			foreach (var line in track.Skipped)
				writer.WriteLine($"    - {Clean(line)}");
			foreach (var line in track.Warnings)
				writer.WriteLine($"    ! {Clean(line)}");
		}

		foreach (var failure in Failures)
			writer.WriteLine($"  [Failed] {Clean(failure.Input)}: {Clean(failure.Reason)}");

		foreach (var notice in Notices)
			writer.WriteLine($"  Note: {Clean(notice)}");

		writer.WriteLine(DryRun
			? $"Done: {AddedCount} would be added, {SkippedCount} skipped, {Failures.Count} failed."
			: $"Done: {AddedCount} added, {SkippedCount} skipped, {Failures.Count} failed.");
	}

	/// <summary>
	/// Replaces control characters with spaces so hostile tag content cannot spoof terminal output.
	/// </summary>
	private static string Clean(string? value)
	{
		if (string.IsNullOrEmpty(value))
			return string.Empty;
		return new string(value.Select(c => char.IsControl(c) ? ' ' : c).ToArray());
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
