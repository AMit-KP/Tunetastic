using System.Diagnostics;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Tunetastic.Common.Controls;

/// <summary>
/// A smooth progress bar control that provides visual feedback for media playback position.
/// </summary>
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

	private const double ThumbSize = 15;
	private const double TrackHeight = 4;

	// ── Dependency Properties ─────────────────────────────────────

	/// <summary>
	/// Identifies the <see cref="Position"/> dependency property.
	/// </summary>
	public static readonly DependencyProperty PositionProperty =
		DependencyProperty.Register(nameof(Position), typeof(double),
			typeof(SmoothProgressBar), new PropertyMetadata(0d));

	/// <summary>
	/// Gets the current position in seconds at 60 fps. This property is bound to a TextBlock for display.
	/// </summary>
	public double Position
	{
		get => (double)GetValue(PositionProperty);
		private set => SetValue(PositionProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="ProgressColor"/> dependency property.
	/// </summary>
	public static readonly DependencyProperty ProgressColorProperty =
		DependencyProperty.Register(nameof(ProgressColor), typeof(Brush),
			typeof(SmoothProgressBar), new PropertyMetadata(null, OnColorChanged));

	/// <summary>
	/// Gets or sets the custom fill/thumb color. Only used when UseAccentColor="False".
	/// </summary>
	public Brush ProgressColor
	{
		get => (Brush)GetValue(ProgressColorProperty);
		set => SetValue(ProgressColorProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="UseAccentColor"/> dependency property.
	/// </summary>
	public static readonly DependencyProperty UseAccentColorProperty =
		DependencyProperty.Register(nameof(UseAccentColor), typeof(bool),
			typeof(SmoothProgressBar), new PropertyMetadata(true, OnColorChanged));

	/// <summary>
	/// Gets or sets a value indicating whether to use the system accent color (true) or ProgressColor (false).
	/// </summary>
	public bool UseAccentColor
	{
		get => (bool)GetValue(UseAccentColorProperty);
		set => SetValue(UseAccentColorProperty, value);
	}

	/// <summary>
	/// Gets the duration of the current track in seconds.
	/// </summary>
	public double Duration { get; private set; } = 1;

	/// <summary>
	/// Occurs when the user scrubs the progress bar — value is seconds.
	/// </summary>
	public event EventHandler<double>? Seeked;

	// ── Constructor ───────────────────────────────────────────────

	/// <summary>
	/// Initializes a new instance of the <see cref="SmoothProgressBar"/> class.
	/// </summary>
	public SmoothProgressBar()
	{
		// MinHeight = ThumbSize so the entire thumb area is tappable,
		// but the visual track stays at TrackHeight via its own Height.
		MinHeight = ThumbSize + 2;

		// Transparent background on the root captures pointer events across
		// the full MinHeight hit area, not just the 4px track.
		_root.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));

		_trackBg.Height = TrackHeight;
		_trackBg.RadiusX = 2; _trackBg.RadiusY = 2;
		_trackBg.VerticalAlignment = VerticalAlignment.Center;
		_trackBg.Fill = GetTrackBackground();

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

	/// <summary>
	/// Called when the color property changes.
	/// </summary>
	/// <param name="d">The dependency object.</param>
	/// <param name="e">The event arguments.</param>
	private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		=> ((SmoothProgressBar)d).ApplyColor();

	/// <summary>
	/// Applies the appropriate color to the progress bar elements.
	/// </summary>
	private void ApplyColor()
	{
		var brush = (!UseAccentColor && ProgressColor != null)
			? ProgressColor
			: (Brush)Application.Current.Resources["AccentAAFillColorDefaultBrush"];
		_trackFill.Fill = brush;
		_thumb.Fill = brush;
		_trackBg.Fill = GetTrackBackground();
	}

	/// <summary>
	/// Gets the background color for the track based on the current theme.
	/// </summary>
	/// <returns>A solid color brush representing the track background.</returns>
	private static SolidColorBrush GetTrackBackground()
	{
		var uiSettings = new Windows.UI.ViewManagement.UISettings();
		var color = uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);

		// Calculate a visible track color based on theme
		// In light theme: darker grey; in dark theme: lighter grey
		byte alpha = 60; // Semi-transparent for subtlety
		byte grey = color.R > 128 ? (byte)180 : (byte)80;

		return new SolidColorBrush(Windows.UI.Color.FromArgb(alpha, grey, grey, grey));
	}

	// ── ViewModel API ─────────────────────────────────────────────

	/// <summary>
	/// Synchronizes the position with the current playback state.
	/// </summary>
	/// <param name="seconds">The current position in seconds.</param>
	/// <remarks>
	/// Only hard-snaps on large drift — never interrupts sub-second motion.
	/// </remarks>
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

	/// <summary>
	/// Notifies the progress bar that playback has started or resumed.
	/// </summary>
	public void NotifyPlaying()
	{
		_isPlaying = true;
		if (!_watch.IsRunning) _watch.Start();
		if (!_timer.IsEnabled) _timer.Start();
	}

	/// <summary>
	/// Notifies the progress bar that playback has paused or stopped.
	/// </summary>
	public void NotifyPaused()
	{
		_isPlaying = false;
		_anchorSeconds += _watch.Elapsed.TotalSeconds;
		_watch.Reset();
		_timer.Stop();
		UpdateUI(_anchorSeconds);
	}

	/// <summary>
	/// Notifies the progress bar that a new track has loaded.
	/// </summary>
	/// <param name="durationSeconds">The duration of the new track in seconds.</param>
	/// <remarks>
	/// Does NOT stop timer/watch when already playing because
	/// NotifyPlaying fires before this on auto-change.
	/// </remarks>
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
	/// </summary>
	/// <param name="seconds">The position to set in seconds.</param>
	/// <remarks>
	/// Call this after NotifyTrackChanged when restoring last playback position.
	/// </remarks>
	public void SetInitialPosition(double seconds)
	{
		_anchorSeconds = Math.Clamp(seconds, 0, Duration);
		_watch.Reset();
		if (_isPlaying) _watch.Start();
		UpdateUI(_anchorSeconds);
	}

	// ── Tick ──────────────────────────────────────────────────────

	/// <summary>
	/// Handles the timer tick event for smooth animation.
	/// </summary>
	/// <param name="sender">The sender of the event.</param>
	/// <param name="e">The event arguments.</param>
	private void OnTick(object? sender, object e)
	{
		if (_isDragging || !_isPlaying) return;
		var live = _anchorSeconds + _watch.Elapsed.TotalSeconds;
		if (live >= Duration) { live = Duration; _timer.Stop(); }
		UpdateUI(live);
	}

	/// <summary>
	/// Redraws the progress bar at the current position.
	/// </summary>
	private void RedrawAtCurrentPosition()
		=> UpdateUI(_anchorSeconds + (_isPlaying ? _watch.Elapsed.TotalSeconds : 0));

	/// <summary>
	/// Updates the UI to reflect the specified position.
	/// </summary>
	/// <param name="seconds">The position in seconds to update to.</param>
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

	/// <summary>
	/// Handles pointer pressed events for scrubbing.
	/// </summary>
	/// <param name="sender">The sender of the event.</param>
	/// <param name="e">The pointer routed event arguments.</param>
	private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
	{
		_isDragging = true;
		_isPointerCaptured = CapturePointer(e.Pointer);
		SeekToPointer(e);
		e.Handled = true;
	}

	/// <summary>
	/// Handles pointer moved events for scrubbing.
	/// </summary>
	/// <param name="sender">The sender of the event.</param>
	/// <param name="e">The pointer routed event arguments.</param>
	private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
	{
		if (!_isDragging) return;
		SeekToPointer(e);
		e.Handled = true;
	}

	/// <summary>
	/// Handles pointer released events for scrubbing.
	/// </summary>
	/// <param name="sender">The sender of the event.</param>
	/// <param name="e">The pointer routed event arguments.</param>
	private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
	{ FinishDrag(e); e.Handled = true; }

	/// <summary>
	/// Handles pointer capture lost events for scrubbing.
	/// </summary>
	/// <param name="sender">The sender of the event.</param>
	/// <param name="e">The pointer routed event arguments.</param>
	private void OnPointerCaptureLost(object sender, PointerRoutedEventArgs e)
		=> FinishDrag(e);

	/// <summary>
	/// Seeks to the position indicated by the pointer.
	/// </summary>
	/// <param name="e">The pointer routed event arguments.</param>
	private void SeekToPointer(PointerRoutedEventArgs e)
	{
		var seconds = Math.Clamp(e.GetCurrentPoint(this).Position.X / ActualWidth, 0, 1) * Duration;
		_anchorSeconds = seconds;
		_watch.Reset();
		if (_isPlaying) _watch.Start();
		UpdateUI(seconds);
	}

	/// <summary>
	/// Finishes the drag operation and fires the seeked event.
	/// </summary>
	/// <param name="e">The pointer routed event arguments.</param>
	private void FinishDrag(PointerRoutedEventArgs e)
	{
		if (!_isDragging) return;
		SeekToPointer(e);
		_isDragging = false;
		if (_isPointerCaptured) { ReleasePointerCapture(e.Pointer); _isPointerCaptured = false; }
		Seeked?.Invoke(this, _anchorSeconds);
	}
}
