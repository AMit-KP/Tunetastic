namespace Tunetastic.Common.Services.Backends;

// ─────────────────────────────────────────────────────────────
//  Internal backend abstraction
// ─────────────────────────────────────────────────────────────
internal interface IMediaBackend : IDisposable
{
	bool IsPlaying { get; }
	long CurTimeTicks { get; set; }
	int Volume { get; set; }
	bool IsMuted { get; set; }

	Task OpenAsync(string path);
	void Play();
	void Pause();
	void Stop();

	event EventHandler<PlaybackStateChangedArgs>? StateChanged;
	event EventHandler? OpenCompleted;
	event EventHandler<long>? PositionChanged;
}
