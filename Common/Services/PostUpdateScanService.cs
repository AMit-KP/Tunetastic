namespace Tunetastic.Common.Services;

/// <summary>
/// Triggers a one-time full library scan on the first launch of the app after it was updated to a targeted version.
/// </summary>
/// <remarks>
/// The newest targeted version the scan has already handled is tracked in local settings
/// (<see cref="LocalSave.PostUpdateScanDoneVersion"/>):
/// <list type="bullet">
/// <item>A version absent from <see cref="TargetVersions"/> is never scanned and any recorded state is left untouched.</item>
/// <item>An install without songs in the database is sealed immediately, so a fresh install never triggers the scan, even after songs are added later.</item>
/// <item>Every other install runs the scan once for the newest targeted version it has reached, including one whose targeted release was skipped (e.g. 1.50 -> 1.52 with 1.51 listed).</item>
/// <item>Add a version to force a repeat for a later release; remove a listed version to stop backfilling it once that release is no longer relevant.</item>
/// </list>
/// </remarks>
public static class PostUpdateScanService
{
	/// <summary>
	/// The versions that trigger a one-time full library scan on the first launch after updating to them.
	/// Leave empty to never scan after an update. Uses the 3-part file version form (e.g. "1.56.47").
	/// </summary>
	private static readonly HashSet<string> TargetVersions = new(StringComparer.OrdinalIgnoreCase)
	{
		// NOTE: Update the version for rescan
		"1.56.49"
	};

	/// <summary>
	/// Runs a full library scan once when a targeted version is running (including a newer version reached
	/// after skipping the targeted release) and the database already contains songs; then records the
	/// highest version handled.
	/// </summary>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result is <see langword="true"/>
	/// when the post-update scan was executed, otherwise <see langword="false"/>.
	/// </returns>
	public static async Task<bool> RunIfEligible()
	{
		var currentVersion = ParseVersion(ProcessInfoHelper.GetFileVersionInfo().FileVersion);
		if (currentVersion is null)
			return false;

		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var lastDoneVersion = ParseVersion(localSettings.Values[nameof(LocalSave.PostUpdateScanDoneVersion)]?.ToString());

		var pendingVersions = TargetVersions
			.Select(ParseVersion)
			.Where(version => version is not null && version.CompareTo(currentVersion) <= 0 && (lastDoneVersion is null || version.CompareTo(lastDoneVersion) > 0))
			.Select(version => version!)
			.ToList();

		if (pendingVersions.Count == 0)
			return false;

		var songsCount = await DatabaseHelper.Instance.TryGetSongsCount();
		if (songsCount is null)
			return false;

		if (songsCount.Value <= 0)
		{
			localSettings.Values[nameof(LocalSave.PostUpdateScanDoneVersion)] = currentVersion.ToString(3);
			return false;
		}

		if (LibraryScanner.IsScanning)
			return false;

		await new LibraryScanner().UpdateMetaData();
		localSettings.Values[nameof(LocalSave.PostUpdateScanDoneVersion)] = pendingVersions.Max()!.ToString(3);
		return true;
	}

	/// <summary>
	/// Parses a version string such as "1.56.47" or "1.56.47.3494" into a comparable 3-part <see cref="Version"/>.
	/// </summary>
	/// <param name="text">The version text to parse.</param>
	/// <returns>The parsed version, or <see langword="null"/> when the text is missing or invalid.</returns>
	private static Version? ParseVersion(string? text)
	{
		if (string.IsNullOrWhiteSpace(text))
			return null;

		var numbers = text.Split('.').Take(3).ToList();
		while (numbers.Count < 3)
			numbers.Add("0");

		return Version.TryParse(string.Join('.', numbers), out var version) ? version : null;
	}
}
