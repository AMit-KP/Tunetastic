using System.Text.RegularExpressions;

namespace Tunetastic.Common;

/// <summary>
/// Parses LRC-format synced lyrics into a sorted list of <see cref="LrcLine"/> objects.
/// </summary>
public static class LrcParser
{
    // Matches [mm:ss.xx] or [mm:ss.xxx] timestamps (2 or 3 decimal places, dot or colon separator)
    private static readonly Regex TimestampRegex = new(@"\[(\d{1,2}):(\d{2})[.:](\d{1,3})\]", RegexOptions.Compiled);
    private static readonly Regex MetadataLineRegex = new(@"^\[(ar|ti|al|by|offset|re|ve):", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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

            if (string.IsNullOrEmpty(text))
                continue;

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
        return lines;
    }
}
