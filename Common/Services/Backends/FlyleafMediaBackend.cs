using FlyleafLib.MediaPlayer;

namespace Tunetastic.Common.Services.Backends;

/// <summary>
/// A media backend implementation using Flyleaf for audio playback.
/// </summary>
internal sealed class FlyleafMediaBackend : IMediaBackend
{
	private readonly Player _player;
	private bool _disposed;
	/// <summary>
	/// Gets or sets the last position tick value for throttling position updates.
	/// </summary>
	private long _lastPosTick;
	/// <summary>
	/// Gets or sets the interval in ticks between position updates.
	/// </summary>
	private static readonly long _posIntervalTicks = TimeSpan.FromMilliseconds(950).Ticks;

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
	/// Initializes a new instance of the <see cref="FlyleafMediaBackend"/> class.
	/// </summary>
	/// <param name="player">The Player instance to use for playback.</param>
	public FlyleafMediaBackend(Player player)
	{
		_player = player;
		_player.OpenCompleted += OnOpenCompleted;
		_player.PropertyChanged += OnPropertyChanged;
	}

	/// <summary>
	/// Gets a value indicating whether the media is currently playing.
	/// </summary>
	public bool IsPlaying => _player.IsPlaying;
	/// <summary>
	/// Gets or sets the current position in ticks.
	/// </summary>
	public long CurTimeTicks
	{
		get => _player.CurTime;
		set => _player.CurTime = value;
	}
	/// <summary>
	/// Gets or sets the volume level (0-100).
	/// </summary>
	public int Volume
	{
		get => _player.Audio.Volume;
		set => _player.Audio.Volume = value;
	}
	/// <summary>
	/// Gets or sets a value indicating whether the media is muted.
	/// </summary>
	public bool IsMuted
	{
		get => _player.Audio.Mute;
		set => _player.Audio.Mute = value;
	}

	/// <summary>
	/// Asynchronously opens the specified media file for playback.
	/// </summary>
	/// <param name="path">The path to the media file.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public async Task OpenAsync(string path)
		=> await Task.Run(() => _player.Open(path));

	/// <summary>
	/// Starts playing the media.
	/// </summary>
	public void Play() => _player.Play();
	/// <summary>
	/// Pauses the media playback.
	/// </summary>
	public void Pause() => _player.Pause();
	/// <summary>
	/// Stops the media playback.
	/// </summary>
	public void Stop()
	{
		_player.Stop();
	}

	/// <summary>
	/// Handles the open completed event.
	/// </summary>
	/// <param name="s">The sender of the event.</param>
	/// <param name="e">The event arguments.</param>
	private void OnOpenCompleted(object? s, OpenCompletedArgs e)
		=> OpenCompleted?.Invoke(this, EventArgs.Empty);

	/// <summary>
	/// Handles property changed events.
	/// </summary>
	/// <param name="sender">The sender of the event.</param>
	/// <param name="e">The event arguments.</param>
	private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(_player.Status):
				var state = _player.Status switch
				{
					Status.Playing => PlaybackState.Playing,
					Status.Paused => PlaybackState.Paused,
					Status.Ended => PlaybackState.Ended,
					_ => PlaybackState.Stopped
				};
				StateChanged?.Invoke(this, new PlaybackStateChangedArgs(state));
				break;

			case nameof(_player.CurTime):
				long now = System.Diagnostics.Stopwatch.GetTimestamp();
				long elapsed = (now - _lastPosTick) *
							   (TimeSpan.TicksPerSecond / System.Diagnostics.Stopwatch.Frequency);
				if (elapsed < _posIntervalTicks) break;
				_lastPosTick = now;
				PositionChanged?.Invoke(this, _player.CurTime);
				break;
		}
	}

	/// <summary>
	/// Disposes the resources used by this backend.
	/// </summary>
	public void Dispose()
	{
		if (_disposed) return;
		_disposed = true;
		_player.OpenCompleted -= OnOpenCompleted;
		_player.PropertyChanged -= OnPropertyChanged;
	}
}
