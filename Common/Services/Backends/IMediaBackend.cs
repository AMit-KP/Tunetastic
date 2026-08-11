namespace Tunetastic.Common.Services.Backends;

/// <summary>
/// Defines the interface for media playback backends.
/// </summary>
internal interface IMediaBackend : IDisposable
{
	/// <summary>
	/// Gets a value indicating whether the media is currently playing.
	/// </summary>
	bool IsPlaying { get; }

	/// <summary>
	/// Gets or sets the current position in ticks.
	/// </summary>
	long CurTimeTicks { get; set; }

	/// <summary>
	/// Gets or sets the volume level (0-100).
	/// </summary>
	int Volume { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the media is muted.
	/// </summary>
	bool IsMuted { get; set; }

	/// <summary>
	/// Asynchronously opens the specified media file for playback.
	/// </summary>
	/// <param name="path">The path to the media file.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	Task OpenAsync(string path);

	/// <summary>
	/// Starts playing the media.
	/// </summary>
	void Play();

	/// <summary>
	/// Pauses the media playback.
	/// </summary>
	void Pause();

	/// <summary>
	/// Stops the media playback.
	/// </summary>
	void Stop();


	/// <summary>
	/// Occurs when the playback state of the media changes.
	/// </summary>
	event EventHandler<PlaybackStateChangedArgs>? StateChanged;

	/// <summary>
	/// Occurs when the media opening process is completed.
	/// </summary>
	event EventHandler? OpenCompleted;

	/// <summary>
	/// Occurs when the position of the media changes.
	/// </summary>
	event EventHandler<long>? PositionChanged;
}
