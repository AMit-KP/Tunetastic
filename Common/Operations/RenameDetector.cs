namespace Tunetastic.Common.Operations;

public class RenameMatchResult
{
	public List<(string OldPath, string NewPath)> Renames { get; set; } = new();
	public List<string> UnmatchedDisappeared { get; set; } = new();
	public List<string> UnmatchedAppeared { get; set; } = new();

	public static RenameMatchResult DetectRenamesAndMoves(
	Dictionary<string, FileScanMeta> disappeared,
	Dictionary<string, (long FileSizeBytes, long LastModifiedUtc, long CreationTimeUtc)> appeared)
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

		result.UnmatchedDisappeared = remainingDisappeared.Keys.ToList();
		result.UnmatchedAppeared = remainingAppeared.Keys.ToList();

		return result;
	}
}
