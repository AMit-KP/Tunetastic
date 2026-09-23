namespace Tunetastic.Common.Helpers;

/// <summary>
/// Represents a single line of parsed LRC lyrics.
/// </summary>
public class LrcLine
{
	/// <summary>
	/// The timestamp of this lyric line.
	/// </summary>
	public TimeSpan Time { get; set; }

	/// <summary>
	/// The lyric text (with all timestamp tags stripped).
	/// </summary>
	public string Text { get; set; } = string.Empty;

	/// <summary>
	/// Initializes a new instance of the <see cref="LrcLine"/> class.
	/// </summary>
	/// <param name="time">The timestamp of this lyric line.</param>
	/// <param name="text">The lyric text (with all timestamp tags stripped).</param>
	public LrcLine(TimeSpan time, string text)
	{
		Time = time;
		Text = text;
	}
}
