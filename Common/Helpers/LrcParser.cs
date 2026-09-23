using System.Text;
using System.Text.RegularExpressions;

namespace Tunetastic.Common.Helpers;

/// <summary>
/// Parses LRC-format synced lyrics into a sorted list of <see cref="LrcLine"/> objects.
/// </summary>
public static class LrcParser
{
	/// <summary>
	/// Matches [mm:ss.xx] or [mm:ss.xxx] timestamps with 2 or 3 decimal places, supporting both dot and colon separators.
	/// </summary>
	private static readonly Regex TimestampRegex = new(@"\[(\d{1,2}):(\d{2})[.:](\d{1,3})\]", RegexOptions.Compiled);

	/// <summary>
	/// Matches metadata lines such as [ar:Artist], [ti:Title], [al:Album], [by:Lyricist], [offset:Offset], [re:Revision], [ve:Version], [au:Author], [length:Length], [id:ID].
	/// </summary>
	private static readonly Regex MetadataLineRegex = new(@"^\[(ar|ti|al|by|offset|re|ve|au|length|id):", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	/// <summary>
	/// Matches karaoke word-by-word highlighting tags like &lt;mm:ss.xx&gt;.
	/// </summary>
	private static readonly Regex KaraokeTagRegex = new(@"<\d{1,2}:\d{2}[.:]\d{1,3}>", RegexOptions.Compiled);

	/// <summary>
	/// Matches offset metadata lines in the format [offset:±number].
	/// </summary>
	private static readonly Regex OffsetRegex = new(@"^\[offset:([+-]?\d+)\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	/// <summary>
	/// Matches synced lyric lines with timestamps to determine if content contains synced lyrics.
	/// </summary>
	private static readonly Regex SyncedLinePattern = new(@"^\[\d{1,2}:\d{2}([.:]\d{1,3})?\]", RegexOptions.Multiline | RegexOptions.Compiled);

	/// <summary>
	/// Parses LRC-formatted lyrics text into a sorted list of <see cref="LrcLine"/>.
	/// </summary>
	/// <param name="lrcContent">The raw LRC lyrics string.</param>
	/// <returns>A list of LrcLine sorted by time. Empty/metadata lines are excluded.</returns>
	public static List<LrcLine> Parse(string lrcContent)
	{
		var lines = new List<LrcLine>();

		if (string.IsNullOrWhiteSpace(lrcContent))
			return lines;

		// First pass: extract offset from metadata
		var offsetMs = GetOffset(lrcContent);

		// Second pass: parse lyric lines
		foreach (var rawLine in lrcContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
		{
			var line = rawLine.Trim();
			if (string.IsNullOrEmpty(line))
				continue;

			if (MetadataLineRegex.IsMatch(line))
				continue;

			var matches = TimestampRegex.Matches(line);
			if (matches.Count == 0)
				continue;

			string text = TimestampRegex.Replace(line, "").Trim();

			// TODO: Karaoke word-by-word highlighting
			// The <mm:ss.xx> inline tags below are stripped for now, but the original
			// tagged text could be parsed to create per-word LrcLine entries.
			// For future karaoke: add a List<KaraokeWord> to LrcLine where
			// KaraokeWord = { string Word, TimeSpan TimeOffset }.
			// Then populate the button content with a WrapPanel of individual TextBlock
			// runs, each with its own timer-driven opacity/scale animation.
			text = KaraokeTagRegex.Replace(text, "").Trim();

			foreach (Match match in matches)
			{
				int minutes = int.Parse(match.Groups[1].Value);
				int seconds = int.Parse(match.Groups[2].Value);
				int frac = int.Parse(match.Groups[3].Value);

				int milliseconds = match.Groups[3].Value.Length == 2 ? frac * 10 : frac;

				var time = TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds) + TimeSpan.FromMilliseconds(milliseconds);
				lines.Add(new LrcLine(time, text));
			}
		}

		lines.Sort((a, b) => a.Time.CompareTo(b.Time));

		// Apply offset to all lines, clamping to 0 so no line goes negative
		ApplyOffset(lines, offsetMs);

		return lines;
	}

	/// <summary>
	/// Shifts every lyricline's time by <paramref name="offsetMs"/> milliseconds, mutating <paramref name="LyricsLines"/> in place.
	/// Shifted times are clamped to 0 so no line goes negative.
	/// </summary>
	/// <param name="LyricsLines">
	/// The lines to shift. Since <see cref="List{T}"/> is a reference type, this method modifies the
	/// caller's list directly (each element is replaced in place) — there is nothing to return.
	/// </param>
	/// <param name="offsetMs">
	/// The offset, in milliseconds, to apply. Positive values delay lines (push them later);
	/// negative values advance them (pull them earlier). A value of 0 is a no-op.
	/// </param>
	public static void ApplyOffset(List<LrcLine> LyricsLines, int offsetMs)
	{
		if (offsetMs == 0)
			return;

		var effectiveOffset = bool.Parse(Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.LRCOffsetSOfficialtandard)]?.ToString() ?? "false") ? -offsetMs : offsetMs;

		for (int i = 0; i < LyricsLines.Count; i++)
		{
			var shifted = LyricsLines[i].Time.Add(TimeSpan.FromMilliseconds(effectiveOffset));
			LyricsLines[i] = new LrcLine(shifted < TimeSpan.Zero ? TimeSpan.Zero : shifted, LyricsLines[i].Text);
		}
	}

	/// <summary>
	/// Scans the raw LRC content for an <c>[offset:±number]</c> metadata line and returns its value.
	/// </summary>
	/// <param name="lrcContent">The raw LRC lyrics string to scan.</param>
	/// <returns>
	/// The offset in milliseconds specified by the first <c>[offset:...]</c> line found, or 0 if the
	/// content is empty or contains no offset metadata line.
	/// </returns>
	public static int GetOffset(string lrcContent)
	{
		int offsetMs = 0;
		foreach (var rawLine in lrcContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
		{
			var line = rawLine.Trim();
			var offsetMatch = OffsetRegex.Match(line);
			if (offsetMatch.Success)
			{
				offsetMs = int.Parse(offsetMatch.Groups[1].Value);
				break;
			}
		}

		return offsetMs;
	}

	/// <summary>
	/// Determines whether the provided lyrics content contains synced timestamps.
	/// </summary>
	/// <param name="lyrics">The lyrics string to check.</param>
	/// <returns>True if the lyrics contain at least 2 synced timestamp lines; otherwise, false.</returns>
	public static bool IsSyncedLyrics(string? lyrics)
	{
		if (string.IsNullOrWhiteSpace(lyrics))
			return false;

		int matchCount = 0;
		foreach (Match match in SyncedLinePattern.Matches(lyrics))
		{
			if (++matchCount >= 2)
				return true;
		}

		return false;
	}

	/// <summary>
	/// Shifts every timestamp in the given LRC content by the specified offset,
	/// returning the modified LRC content as a string. Metadata lines (e.g. [ar:], [ti:])
	/// are left untouched. Timestamps are clamped to a minimum of 00:00.00 so they never go negative.
	/// </summary>
	/// <param name="lrcContent">The raw LRC file content to modify.</param>
	/// <param name="offsetMs">
	/// The offset, in milliseconds, to apply to each timestamp. Positive values shift
	/// timestamps later; negative values shift them earlier.
	/// </param>
	/// <returns>
	/// The LRC content with all line timestamps shifted by
	/// <paramref name="offsetMs"/>. Returns the input unchanged if it is null, empty, or whitespace.
	/// </returns>
	public static string? SaveOffsetByChangingTimestamp(string? lrcContent, int offsetMs)
	{
		if (string.IsNullOrWhiteSpace(lrcContent))
			return lrcContent;

		var rawLines = lrcContent.Split('\n');
		var sb = new StringBuilder();

		var effectiveOffset = bool.Parse(Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.LRCOffsetSOfficialtandard)]?.ToString() ?? "false") ? -offsetMs : offsetMs;

		for (int i = 0; i < rawLines.Length; i++)
		{
			var line = rawLines[i];
			var trimmed = line.TrimEnd('\r').Trim();

			if (!MetadataLineRegex.IsMatch(trimmed))
			{
				line = TimestampRegex.Replace(line, m => ShiftTimestamp(m, effectiveOffset));

				// TODO: Apply offset to Karaoke tags
				//line = KaraokeTagRegex.Replace(line, m => ShiftTimestamp(m, effectiveOffset));
			}

			sb.Append(line);
			if (i < rawLines.Length - 1)
				sb.Append('\n');
		}

		return sb.ToString();
	}

	/// <summary>
	/// Parses a single timestamp regex match (e.g. "[01:23.45]" or "&lt;01:23.45&gt;"),
	/// applies the given offset in milliseconds, and returns the timestamp re-formatted
	/// as a string with the same delimiters and fractional-digit precision as the original match.
	/// </summary>
	/// <param name="match">
	/// A regex match with capture groups: 1 = minutes, 2 = seconds, 3 = fractional seconds
	/// (2 or 3 digits), and the full match including its opening/closing delimiter characters.
	/// </param>
	/// <param name="offsetMs">The offset, in milliseconds, to apply to the parsed timestamp.</param>
	/// <returns>
	/// The shifted timestamp as a string, wrapped in the same delimiter characters as the
	/// original match, clamped to a minimum of 00:00.00.
	/// </returns>
	private static string ShiftTimestamp(Match match, int offsetMs)
	{
		int minutes = int.Parse(match.Groups[1].Value);
		int seconds = int.Parse(match.Groups[2].Value);
		string fracStr = match.Groups[3].Value;
		int frac = int.Parse(fracStr);
		int ms = fracStr.Length == 2 ? frac * 10 : frac;

		var time = TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds) + TimeSpan.FromMilliseconds(ms);
		time += TimeSpan.FromMilliseconds(offsetMs);
		if (time < TimeSpan.Zero)
			time = TimeSpan.Zero;

		int totalMinutes = (int)time.TotalMinutes;
		string fracOut = fracStr.Length == 2
			? (time.Milliseconds / 10).ToString("D2")
			: time.Milliseconds.ToString("D3");

		char open = match.Value[0];
		char close = match.Value[^1];

		return $"{open}{totalMinutes:D2}:{time.Seconds:D2}.{fracOut}{close}";
	}

	/// <summary>
	/// Adds or updates the <c>[offset:±number]</c> metadata line in the given LRC content.
	/// If an offset line already exists, its value is overwritten; if none exists, a new one is
	/// inserted after any other metadata lines (or at the top if there are none).
	/// </summary>
	/// <param name="lrcContent">The raw LRC file content to modify.</param>
	/// <param name="offsetMs">The offset, in milliseconds, to write into the <c>[offset:]</c> line.</param>
	/// <returns>
	/// The LRC content with the <c>[offset:]</c> metadata line added or updated. If
	/// <paramref name="lrcContent"/> is null, empty, or whitespace, returns a new string
	/// containing only the <c>[offset:]</c> line.
	/// </returns>
	public static string? SetOffsetMetadata(string? lrcContent, int offsetMs)
	{
		if (string.IsNullOrWhiteSpace(lrcContent))
			return $"[offset:{offsetMs}]";

		var rawLines = lrcContent.Split('\n').ToList();

		for (int i = 0; i < rawLines.Count; i++)
		{
			var trimmed = rawLines[i].TrimEnd('\r').Trim();
			if (OffsetRegex.IsMatch(trimmed))
			{
				rawLines[i] = $"[offset:{offsetMs}]";
				return string.Join('\n', rawLines);
			}
		}

		int insertIndex = 0;
		for (int i = 0; i < rawLines.Count; i++)
		{
			var trimmed = rawLines[i].TrimEnd('\r').Trim();
			if (MetadataLineRegex.IsMatch(trimmed))
				insertIndex = i + 1;
			else if (!string.IsNullOrEmpty(trimmed))
				break;
		}

		rawLines.Insert(insertIndex, $"[offset:{offsetMs}]");
		return string.Join('\n', rawLines);
	}
}
