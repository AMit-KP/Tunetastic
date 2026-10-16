using System.Text.Json;

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

	/// <summary>Gets whether this was a dry run (nothing was written).</summary>
	public bool DryRun { get; init; }

	/// <summary>Gets the per-track outcomes.</summary>
	public List<TrackEntry> Tracks { get; init; } = new();

	/// <summary>Gets the inputs that failed before any metadata could be read (missing files, unreadable files, ...).</summary>
	public List<InputFailure> Failures { get; init; } = new();

	/// <summary>Gets the number of tracks added to the playlist.</summary>
	public int AddedCount => Tracks.Count(t => t.Status == TrackStatus.Added || t.Status == TrackStatus.WouldAdd);

	/// <summary>Gets the number of tracks skipped by policy (duplicate, too short, disabled format, ...).</summary>
	public int SkippedCount => Tracks.Count(t => t.Status == TrackStatus.Skipped || t.Status == TrackStatus.AlreadyPresent);

	/// <summary>Serializes the report to indented JSON for stdout.</summary>
	public string ToJson()
	{
		return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
	}

	/// <summary>
	/// Writes a human-readable rendering of the report to <paramref name="writer"/>.
	/// </summary>
	public void WriteHuman(TextWriter writer)
	{
		writer.WriteLine(DryRun ? $"Dry run for playlist '{Playlist}':" : $"Playlist '{Playlist}':{(PlaylistCreated ? " (created)" : "")}");

		foreach (var track in Tracks)
		{
			var label = track.Status switch
			{
				TrackStatus.Added => "Added",
				TrackStatus.WouldAdd => "Would add",
				TrackStatus.AlreadyPresent => "Already in playlist",
				_ => "Skipped",
			};
			writer.WriteLine($"  [{label}] {track.Title} ({track.Path})");
			if (!string.IsNullOrWhiteSpace(track.Reason))
				writer.WriteLine($"    Reason: {track.Reason}");
			foreach (var line in track.Imported)
				writer.WriteLine($"    + {line}");
			foreach (var line in track.Defaulted)
				writer.WriteLine($"    ~ {line}");
			foreach (var line in track.Skipped)
				writer.WriteLine($"    - {line}");
			foreach (var line in track.Warnings)
				writer.WriteLine($"    ! {line}");
		}

		foreach (var failure in Failures)
			writer.WriteLine($"  [Failed] {failure.Input}: {failure.Reason}");

		writer.WriteLine($"Done: {AddedCount} added, {SkippedCount} skipped, {Failures.Count} failed.");
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
