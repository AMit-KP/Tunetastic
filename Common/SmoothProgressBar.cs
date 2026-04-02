using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Tunetastic.Common;

public sealed class SmoothProgressBar : UserControl
{
	private readonly Rectangle _trackBg = new();
	private readonly Rectangle _trackFill = new();
	private readonly Ellipse _thumb = new();
	private readonly Grid _root = new();

	private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(16) };
	private readonly Stopwatch _watch = new();

	private double _anchorSeconds = 0;
	private bool _isPlaying = false;
	private bool _isDragging = false;
	private bool _isPointerCaptured = false;

	private const double ThumbSize = 14;
	private const double TrackHeight = 4;

	// ── Dependency Properties ─────────────────────────────────────

	public static readonly DependencyProperty PositionProperty =
		DependencyProperty.Register(nameof(Position), typeof(double),
			typeof(SmoothProgressBar), new PropertyMetadata(0d));

	/// <summary>Live position in seconds at 60 fps. Bind a TextBlock here.</summary>
	public double Position
	{
		get => (double)GetValue(PositionProperty);
		private set => SetValue(PositionProperty, value);
	}

	public static readonly DependencyProperty ProgressColorProperty =
		DependencyProperty.Register(nameof(ProgressColor), typeof(Brush),
			typeof(SmoothProgressBar), new PropertyMetadata(null, OnColorChanged));

	/// <summary>Custom fill/thumb color. Only used when UseAccentColor="False".</summary>
	public Brush ProgressColor
	{
		get => (Brush)GetValue(ProgressColorProperty);
		set => SetValue(ProgressColorProperty, value);
	}

	public static readonly DependencyProperty UseAccentColorProperty =
		DependencyProperty.Register(nameof(UseAccentColor), typeof(bool),
			typeof(SmoothProgressBar), new PropertyMetadata(true, OnColorChanged));

	/// <summary>True (default) = system accent. False = use ProgressColor.</summary>
	public bool UseAccentColor
	{
		get => (bool)GetValue(UseAccentColorProperty);
		set => SetValue(UseAccentColorProperty, value);
	}

	public double Duration { get; private set; } = 1;

	/// <summary>Fired when the user scrubs — value is seconds.</summary>
	public event EventHandler<double>? Seeked;

	// ── Constructor ───────────────────────────────────────────────

	public SmoothProgressBar()
	{
		// MinHeight = ThumbSize so the entire thumb area is tappable,
		// but the visual track stays at TrackHeight via its own Height.
		MinHeight = ThumbSize;

		// Transparent background on the root captures pointer events across
		// the full MinHeight hit area, not just the 4px track.
		_root.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));

		_trackBg.Height = TrackHeight;
		_trackBg.RadiusX = 2; _trackBg.RadiusY = 2;
		_trackBg.VerticalAlignment = VerticalAlignment.Center;
		_trackBg.Fill = (Brush)Application.Current.Resources["SystemFillColorNeutralBackgroundBrush"];

		_trackFill.Height = TrackHeight;
		_trackFill.RadiusX = 2; _trackFill.RadiusY = 2;
		_trackFill.HorizontalAlignment = HorizontalAlignment.Left;
		_trackFill.VerticalAlignment = VerticalAlignment.Center;
		_trackFill.Width = 0;

		_thumb.Width = ThumbSize; _thumb.Height = ThumbSize;
		_thumb.HorizontalAlignment = HorizontalAlignment.Left;
		_thumb.VerticalAlignment = VerticalAlignment.Center;
		_thumb.Margin = new Thickness(-ThumbSize / 2, 0, 0, 0);
		_thumb.RenderTransform = new TranslateTransform();

		_root.Children.Add(_trackBg);
		_root.Children.Add(_trackFill);
		_root.Children.Add(_thumb);
		Content = _root;

		_timer.Tick += OnTick;
		SizeChanged += (_, _) => RedrawAtCurrentPosition();
		Loaded += (_, _) => ApplyColor();
		PointerPressed += OnPointerPressed;
		PointerMoved += OnPointerMoved;
		PointerReleased += OnPointerReleased;
		PointerCaptureLost += OnPointerCaptureLost;
	}

	// ── Color ─────────────────────────────────────────────────────

	private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		=> ((SmoothProgressBar)d).ApplyColor();

	private void ApplyColor()
	{
		var brush = (!UseAccentColor && ProgressColor != null)
			? ProgressColor
			: (Brush)Application.Current.Resources["AccentAAFillColorDefaultBrush"];
		_trackFill.Fill = brush;
		_thumb.Fill = brush;
	}

	// ── ViewModel API ─────────────────────────────────────────────

	/// <summary>
	/// Call every time MusicPlayer.PositionChanged fires.
	/// Only hard-snaps on large drift — never interrupts sub-second motion.
	/// </summary>
	public void SyncPosition(double seconds)
	{
		if (_isDragging) return;

		var predicted = _anchorSeconds + _watch.Elapsed.TotalSeconds;
		var drift = Math.Abs(predicted - seconds);

		if (drift > 1.5)
		{
			_anchorSeconds = seconds;
			if (_isPlaying) _watch.Restart();
			else { _watch.Reset(); UpdateUI(seconds); }
		}
	}

	/// <summary>Call when playback starts or resumes.</summary>
	public void NotifyPlaying()
	{
		_isPlaying = true;
		if (!_watch.IsRunning) _watch.Start();
		if (!_timer.IsEnabled) _timer.Start();
	}

	/// <summary>Call when playback pauses or stops.</summary>
	public void NotifyPaused()
	{
		_isPlaying = false;
		_anchorSeconds += _watch.Elapsed.TotalSeconds;
		_watch.Reset();
		_timer.Stop();
		UpdateUI(_anchorSeconds);
	}

	/// <summary>
	/// Call when a new track loads (including auto next/prev).
	/// Does NOT stop timer/watch when already playing because
	/// NotifyPlaying fires before this on auto-change.
	/// </summary>
	public void NotifyTrackChanged(double durationSeconds)
	{
		Duration = Math.Max(durationSeconds, 1);
		_anchorSeconds = 0;

		if (_isPlaying)
		{
			_watch.Restart();
		}
		else
		{
			_watch.Reset();
			_timer.Stop();
			UpdateUI(0);
		}
	}

	/// <summary>
	/// Sets the initial position on app startup without triggering a seek.
	/// Call this after NotifyTrackChanged when restoring last playback position.
	/// </summary>
	public void SetInitialPosition(double seconds)
	{
		_anchorSeconds = Math.Clamp(seconds, 0, Duration);
		_watch.Reset();
		if (_isPlaying) _watch.Start();
		UpdateUI(_anchorSeconds);
	}

	// ── Tick ──────────────────────────────────────────────────────

	private void OnTick(object? sender, object e)
	{
		if (_isDragging || !_isPlaying) return;
		var live = _anchorSeconds + _watch.Elapsed.TotalSeconds;
		if (live >= Duration) { live = Duration; _timer.Stop(); }
		UpdateUI(live);
	}

	private void RedrawAtCurrentPosition()
		=> UpdateUI(_anchorSeconds + (_isPlaying ? _watch.Elapsed.TotalSeconds : 0));

	private void UpdateUI(double seconds)
	{
		var w = ActualWidth;
		if (w <= 0 || Duration <= 0) return;
		var fillWidth = Math.Clamp(seconds / Duration, 0, 1) * w;
		_trackFill.Width = fillWidth;
		((TranslateTransform)_thumb.RenderTransform).X = fillWidth;
		Position = seconds;
	}

	// ── Scrub ─────────────────────────────────────────────────────

	private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
	{
		_isDragging = true;
		_isPointerCaptured = CapturePointer(e.Pointer);
		SeekToPointer(e);
		e.Handled = true;
	}

	private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
	{
		if (!_isDragging) return;
		SeekToPointer(e);
		e.Handled = true;
	}

	private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
	{ FinishDrag(e); e.Handled = true; }

	private void OnPointerCaptureLost(object sender, PointerRoutedEventArgs e)
		=> FinishDrag(e);

	private void SeekToPointer(PointerRoutedEventArgs e)
	{
		var seconds = Math.Clamp(e.GetCurrentPoint(this).Position.X / ActualWidth, 0, 1) * Duration;
		_anchorSeconds = seconds;
		_watch.Reset();
		if (_isPlaying) _watch.Start();
		UpdateUI(seconds);
	}

	private void FinishDrag(PointerRoutedEventArgs e)
	{
		if (!_isDragging) return;
		SeekToPointer(e);
		_isDragging = false;
		if (_isPointerCaptured) { ReleasePointerCapture(e.Pointer); _isPointerCaptured = false; }
		Seeked?.Invoke(this, _anchorSeconds);
	}
}
