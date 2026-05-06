using Windows.Media.Playback;

namespace Tunetastic.Common.Services.Backends;

// ─────────────────────────────────────────────────────────────
//  Windows MediaPlayer backend
// ─────────────────────────────────────────────────────────────
internal sealed class WindowsMediaBackend : IMediaBackend
{
	private readonly MediaPlayer _player;
	private System.Threading.Timer? _positionTimer;
	private bool _disposed;
	private volatile bool _isLoading = false;
	public double PendingStartPosition = 0.0;

	public event EventHandler<PlaybackStateChangedArgs>? StateChanged;
	public event EventHandler? OpenCompleted;
	public event EventHandler<long>? PositionChanged;

	public WindowsMediaBackend(MediaPlayer player)
	{
		_player = player;
		_player.MediaOpened += OnMediaOpened;
		_player.MediaEnded += OnMediaEnded;
		_player.PlaybackSession.PlaybackStateChanged += OnPlaybackStateChanged;

		_positionTimer = new System.Threading.Timer(_ =>
		{
			if (_player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
				PositionChanged?.Invoke(this, _player.PlaybackSession.Position.Ticks);
		}, null, Timeout.Infinite, Timeout.Infinite);
	}

	public bool IsPlaying =>
		_player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing;

	public long CurTimeTicks
	{
		get => _player.PlaybackSession.Position.Ticks;
		set => _player.PlaybackSession.Position = TimeSpan.FromTicks(value);
	}

	public int Volume
	{
		get => (int)(_player.Volume * 100);
		set => _player.Volume = Math.Clamp(value, 0, 100) / 100.0;
	}

	public bool IsMuted
	{
		get => _player.IsMuted;
		set => _player.IsMuted = value;
	}

	public Task OpenAsync(string path)
	{
		_isLoading = true;
		_player.Source = Windows.Media.Core.MediaSource.CreateFromUri(new Uri(path));
		return Task.CompletedTask;
	}

	public void Play()
	{
		_player.Play();
		_positionTimer?.Change(0, 1000);
	}

	public void Pause()
	{
		_player.Pause();
		_positionTimer?.Change(Timeout.Infinite, Timeout.Infinite);
	}

	public void Stop()
	{
		_player.Source = null;
		_positionTimer?.Change(Timeout.Infinite, Timeout.Infinite);
	}

	private void OnMediaOpened(MediaPlayer sender, object args)
	{
		_isLoading = false;
		if (PendingStartPosition > 0)
		{
			_player.PlaybackSession.Position = TimeSpan.FromSeconds(PendingStartPosition);
			PendingStartPosition = 0.0;
		}
		OpenCompleted?.Invoke(this, EventArgs.Empty);
	}

	private void OnMediaEnded(MediaPlayer sender, object args)
	{
		_positionTimer?.Change(Timeout.Infinite, Timeout.Infinite);
		StateChanged?.Invoke(this, new PlaybackStateChangedArgs(PlaybackState.Ended));
	}

	private void OnPlaybackStateChanged(MediaPlaybackSession sender, object args)
	{
		if (_isLoading) return;

		var state = sender.PlaybackState switch
		{
			MediaPlaybackState.Playing => PlaybackState.Playing,
			MediaPlaybackState.Paused => PlaybackState.Paused,
			_ => PlaybackState.Stopped
		};
		StateChanged?.Invoke(this, new PlaybackStateChangedArgs(state));
	}

	public void Dispose()
	{
		if (_disposed) return;
		_disposed = true;
		_positionTimer?.Dispose();
		_player.MediaOpened -= OnMediaOpened;
		_player.MediaEnded -= OnMediaEnded;
		_player.PlaybackSession.PlaybackStateChanged -= OnPlaybackStateChanged;
	}
}
