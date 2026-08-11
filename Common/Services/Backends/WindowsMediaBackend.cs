using Windows.Media.Playback;

namespace Tunetastic.Common.Services.Backends;

/// <summary>
/// A media backend implementation using Windows MediaPlayer for audio playback.
/// </summary>
internal sealed class WindowsMediaBackend : IMediaBackend
{
	private readonly MediaPlayer _player;
	private System.Threading.Timer? _positionTimer;
	private bool _disposed;
	private volatile bool _isLoading = false;
	/// <summary>
	/// Gets or sets the pending start position for media playback.
	/// </summary>
	public double PendingStartPosition = 0.0;

	/// <summary>
	/// Occurs when the playback state of the media changes.
	/// </summary>
	public event EventHandler<PlaybackStateChangedArgs>? StateChanged;
	/// <summary>
	/// Occurs when the media opening process is completed.
	/// </summary>
	public event EventHandler? OpenCompleted;
	/// <summary>
	/// Occurs when the position of the media changes.
	/// </summary>
	public event EventHandler<long>? PositionChanged;

	/// <summary>
	/// Initializes a new instance of the <see cref="WindowsMediaBackend"/> class.
	/// </summary>
	/// <param name="player">The MediaPlayer instance to use for playback.</param>
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

	/// <summary>
	/// Gets a value indicating whether the media is currently playing.
	/// </summary>
	public bool IsPlaying =>
		_player.PlaybackSession.PlaybackState == MediaPlaybackState.Playing;

	/// <summary>
	/// Gets or sets the current position in ticks.
	/// </summary>
	public long CurTimeTicks
	{
		get => _player.PlaybackSession.Position.Ticks;
		set => _player.PlaybackSession.Position = TimeSpan.FromTicks(value);
	}

	/// <summary>
	/// Gets or sets the volume level (0-100).
	/// </summary>
	public int Volume
	{
		get => (int)(_player.Volume * 100);
		set => _player.Volume = Math.Clamp(value, 0, 100) / 100.0;
	}

	/// <summary>
	/// Gets or sets a value indicating whether the media is muted.
	/// </summary>
	public bool IsMuted
	{
		get => _player.IsMuted;
		set => _player.IsMuted = value;
	}

	/// <summary>
	/// Asynchronously opens the specified media file for playback.
	/// </summary>
	/// <param name="path">The path to the media file.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public Task OpenAsync(string path)
	{
		_isLoading = true;
		_player.Source = Windows.Media.Core.MediaSource.CreateFromUri(new Uri(path));
		return Task.CompletedTask;
	}

	/// <summary>
	/// Starts playing the media.
	/// </summary>
	public void Play()
	{
		_player.Play();
		_positionTimer?.Change(0, 1000);
	}

	/// <summary>
	/// Pauses the media playback.
	/// </summary>
	public void Pause()
	{
		_player.Pause();
		_positionTimer?.Change(Timeout.Infinite, Timeout.Infinite);
	}

	/// <summary>
	/// Stops the media playback and clears the source.
	/// </summary>
	public void Stop()
	{
		_player.Source = null;
		_positionTimer?.Change(Timeout.Infinite, Timeout.Infinite);
	}

	/// <summary>
	/// Handles the media opened event.
	/// </summary>
	/// <param name="sender">The sender of the event.</param>
	/// <param name="args">The event arguments.</param>
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

	/// <summary>
	/// Handles the media ended event.
	/// </summary>
	/// <param name="sender">The sender of the event.</param>
	/// <param name="args">The event arguments.</param>
	private void OnMediaEnded(MediaPlayer sender, object args)
	{
		_positionTimer?.Change(Timeout.Infinite, Timeout.Infinite);
		StateChanged?.Invoke(this, new PlaybackStateChangedArgs(PlaybackState.Ended));
	}

	/// <summary>
	/// Handles the playback state changed event.
	/// </summary>
	/// <param name="sender">The sender of the event.</param>
	/// <param name="args">The event arguments.</param>
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

	/// <summary>
	/// Disposes the resources used by this backend.
	/// </summary>
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
