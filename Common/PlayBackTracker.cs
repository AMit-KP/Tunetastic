namespace Tunetastic.Common;

/// <summary>
/// Tracks playback activity of a media item, including play time, playback position, and playback count state.
/// </summary>
public class PlaybackTracker
{
	/// <summary>
	/// Stores the cumulative duration for which the current media item has been played.
	/// This value is updated when playback is paused or stopped and represents the total
	/// play time across all playback sessions. The value is reset when the playback
	/// tracker is reset.
	/// </summary>
	private TimeSpan _totalPlayTime = TimeSpan.Zero;

	/// <summary>
	/// Indicates whether the play count for the current media item has already been incremented
	/// during this playback session. Prevents multiple increments for the same playback period.
	/// The value is reset when the playback tracker is reset.
	/// </summary>
	public bool AlreadyCounted { get; private set; } = false;


	/// <summary>
	/// Tracks the timestamp marking the start of the most recent playback session.
	/// This value is set when playback begins and cleared when the session ends
	/// (e.g., when paused or stopped). It is used to calculate the elapsed playback
	/// time for the current session.
	/// </summary>
	private DateTime? _lastPlayStart = null;

	/// <summary>
	/// Starts playback for the current media item by tracking the beginning of the playback period.
	/// If playback has not started before, the start time is recorded.
	/// </summary>
	public void StartPlayback()
	{
		_lastPlayStart ??= DateTime.UtcNow;
	}

	/// <summary>
	/// Pauses the current playback session and updates the total play time by adding the elapsed time
	/// since the playback was last started. If playback has already been paused or not started, this
	/// method has no effect.
	/// </summary>
	public void PausePlayback()
	{
		if (_lastPlayStart != null)
		{
			_totalPlayTime += DateTime.UtcNow - _lastPlayStart.Value;
			_lastPlayStart = null;
		}
	}

	/// <summary>
	/// Calculates and returns the total playback time accumulated, including the active playback session if applicable.
	/// If playback is currently ongoing, the elapsed time since playback started is added to the previously tracked total time.
	/// </summary>
	/// <returns>The total playback time as a TimeSpan object.</returns>
	public TimeSpan GetTotalPlayTime()
	{
		if (_lastPlayStart != null)
		{
			var currentPlay = DateTime.UtcNow - _lastPlayStart.Value;
			return _totalPlayTime + currentPlay;
		}

		return _totalPlayTime;
	}

	/// <summary>
	/// Marks the play count as recorded for the current media playback session.
	/// This indicates that the play count should no longer be incremented
	/// for the current playback session to avoid duplicate updates.
	/// </summary>
	public void MarkPlayCountRecorded()
	{
		AlreadyCounted = true;
	}

	/// <summary>
	/// Resets the playback tracker by clearing all tracking data, including total play time,
	/// maximum playback position, playback count state, and playback start timestamp.
	/// </summary>
	public void Reset()
	{
		_totalPlayTime = TimeSpan.Zero;
		_lastPlayStart = null;
		AlreadyCounted = false;
	}
}
