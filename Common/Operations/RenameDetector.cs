namespace Tunetastic.Common.Operations;
/// <summary>
/// Represents the outcome of detecting renamed or moved files by comparing
/// disappeared and appeared file entries from a library scan.
/// </summary>
/// <remarks>
/// Entries that could not be confidently paired across scans are reported in
/// <see cref="UnmatchedDisappeared"/> and <see cref="UnmatchedAppeared"/> so that
/// callers can handle them as removals and additions respectively.
/// </remarks>
public class RenameMatchResult
{
	/// <summary>
	/// Pairs of old and new file paths identified as renames or moves.
	/// </summary>
	public List<(string OldPath, string NewPath)> Renames { get; set; } = [];

	/// <summary>
	/// Paths of files from the previous scan that disappeared but could not be
	/// matched to any newly appeared file.
	/// </summary>
	public List<string> UnmatchedDisappeared { get; set; } = [];

	/// <summary>
	/// Paths of newly appeared files that could not be matched to any file
	/// that disappeared from the previous scan.
	/// </summary>
	public List<string> UnmatchedAppeared { get; set; } = [];
}

/// <summary>
/// Provides rename/move detection logic for files tracked between library scans.
/// </summary>
public static class RenameDetector
{
	/// <summary>
	/// Detects renamed or moved files by matching files that disappeared since the
	/// previous scan against files that newly appeared.
	/// </summary>
	/// <remarks>
	/// Matching is performed in two tiers, from most to least confident:
	/// <list type="number">
	/// <item><description>Same file size and same last-modified time (UTC).</description></item>
	/// <item><description>Same file size and same creation time (UTC), only when the pairing is unique on both sides.</description></item>
	/// </list>
	/// All comparisons are case-insensitive on file paths. Entries left unmatched
	/// after both tiers are returned as removals and additions respectively.
	/// </remarks>
	/// <param name="disappeared">
	/// Files from the previous scan that no longer exist, keyed by path.
	/// </param>
	/// <param name="appeared">
	/// Newly discovered files, keyed by path, with their size in bytes and
	/// their last-modified and creation timestamps in UTC.
	/// </param>
	/// <returns>
	/// A <see cref="RenameMatchResult"/> containing detected rename pairs and the
	/// unmatched disappeared/appeared paths.
	/// </returns>
	public static RenameMatchResult DetectRenamesAndMoves(Dictionary<string, FileScanMeta> disappeared, Dictionary<string, (long FileSizeBytes, long LastModifiedUtc, long CreationTimeUtc)> appeared)
	{
		var result = new RenameMatchResult();

		var remainingDisappeared = new Dictionary<string, FileScanMeta>(disappeared, StringComparer.OrdinalIgnoreCase);
		var remainingAppeared = new Dictionary<string, (long FileSizeBytes, long LastModifiedUtc, long CreationTimeUtc)>(appeared, StringComparer.OrdinalIgnoreCase);

		// Tier 1: same FileSizeBytes + same LastModifiedUtc
		var tier1DisappearedLookup = remainingDisappeared
			.GroupBy(kv => (kv.Value.FileSizeBytes, kv.Value.LastModifiedUtc))
			.ToDictionary(g => g.Key, g => g.Select(kv => kv.Key).ToList());

		foreach (var appearedEntry in remainingAppeared.ToList())
		{
			var key = (appearedEntry.Value.FileSizeBytes, appearedEntry.Value.LastModifiedUtc);

			if (tier1DisappearedLookup.TryGetValue(key, out var candidates) && candidates.Count > 0)
			{
				var oldPath = candidates[0];
				candidates.RemoveAt(0);

				result.Renames.Add((oldPath, appearedEntry.Key));
				remainingDisappeared.Remove(oldPath);
				remainingAppeared.Remove(appearedEntry.Key);
			}
		}

		// Tier 2: same FileSizeBytes + same CreationTimeUtc, only if pairing is unique
		var tier2DisappearedLookup = remainingDisappeared
			.GroupBy(kv => (kv.Value.FileSizeBytes, kv.Value.CreationTimeUtc))
			.Where(g => g.Count() == 1)
			.ToDictionary(g => g.Key, g => g.Single().Key);

		var tier2AppearedLookup = remainingAppeared
			.GroupBy(kv => (kv.Value.FileSizeBytes, kv.Value.CreationTimeUtc))
			.Where(g => g.Count() == 1)
			.ToDictionary(g => g.Key, g => g.Single().Key);

		foreach (var key in tier2DisappearedLookup.Keys.ToList())
		{
			if (tier2AppearedLookup.TryGetValue(key, out var newPath))
			{
				var oldPath = tier2DisappearedLookup[key];

				if (remainingDisappeared.ContainsKey(oldPath) && remainingAppeared.ContainsKey(newPath))
				{
					result.Renames.Add((oldPath, newPath));
					remainingDisappeared.Remove(oldPath);
					remainingAppeared.Remove(newPath);
				}
			}
		}

		result.UnmatchedDisappeared = [.. remainingDisappeared.Keys];
		result.UnmatchedAppeared = [.. remainingAppeared.Keys];

		return result;
	}
}
