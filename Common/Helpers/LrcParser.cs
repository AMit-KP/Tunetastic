using System.Text.RegularExpressions;

namespace Tunetastic.Common.Helpers;

/// <summary>
/// Parses LRC-format synced lyrics into a sorted list of <see cref="LrcLine"/> objects.
/// </summary>
public static class LrcParser
{
    // Matches [mm:ss.xx] or [mm:ss.xxx] timestamps (2 or 3 decimal places, dot or colon separator)
    private static readonly Regex TimestampRegex = new(@"\[(\d{1,2}):(\d{2})[.:](\d{1,3})\]", RegexOptions.Compiled);
    private static readonly Regex MetadataLineRegex = new(@"^\[(ar|ti|al|by|offset|re|ve|au|length|id):", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex KaraokeTagRegex = new(@"<\d{1,2}:\d{2}[.:]\d{1,3}>", RegexOptions.Compiled);
    private static readonly Regex OffsetRegex = new(@"^\[offset:(-?\d+)\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
        if (offsetMs != 0)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                var shifted = lines[i].Time.Add(TimeSpan.FromMilliseconds(offsetMs));
                lines[i] = new LrcLine(shifted < TimeSpan.Zero ? TimeSpan.Zero : shifted, lines[i].Text);
            }
        }

        return lines;
    }
}
