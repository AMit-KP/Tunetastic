using FlyleafLib;
using FlyleafLib.MediaPlayer;
using Tunetastic.Common;

namespace Tunetastic.Common.Services.Backends;

// ─────────────────────────────────────────────────────────────
//  Flyleaf backend
// ─────────────────────────────────────────────────────────────
internal sealed class FlyleafMediaBackend : IMediaBackend
{
	private readonly Player _player;
	private bool _disposed;

	// Throttle: fire PositionChanged at most ~once per second
	private long _lastPosTick;
	private static readonly long _posIntervalTicks = TimeSpan.FromMilliseconds(950).Ticks;

	public event EventHandler<PlaybackStateChangedArgs>? StateChanged;
	public event EventHandler? OpenCompleted;
	public event EventHandler<long>? PositionChanged;

	public FlyleafMediaBackend(Player player)
	{
		_player = player;
		_player.OpenCompleted += OnOpenCompleted;
		_player.PropertyChanged += OnPropertyChanged;
	}

	public bool IsPlaying => _player.IsPlaying;
	public long CurTimeTicks
	{
		get => _player.CurTime;
		set => _player.CurTime = value;
	}

	public int Volume
	{
		get => _player.Audio.Volume;
		set => _player.Audio.Volume = value;
	}

	public bool IsMuted
	{
		get => _player.Audio.Mute;
		set => _player.Audio.Mute = value;
	}

	public async Task OpenAsync(string path)
		=> await Task.Run(() => _player.Open(path));

	public void Play() => _player.Play();
	public void Pause() => _player.Pause();
	public void Stop()
	{
		_player.Stop();
	}

	private void OnOpenCompleted(object? s, OpenCompletedArgs e)
		=> OpenCompleted?.Invoke(this, EventArgs.Empty);

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

	public void Dispose()
	{
		if (_disposed) return;
		_disposed = true;
		_player.OpenCompleted -= OnOpenCompleted;
		_player.PropertyChanged -= OnPropertyChanged;
	}
}
