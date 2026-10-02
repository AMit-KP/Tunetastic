using System.Diagnostics;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Tunetastic.Common.Controls;

/// <summary>
/// Android style A–Z fast scroll rail for the library pages: press and drag the letter strip to scrub
/// the bound list continuously like a scrollbar, tap a letter to jump to its section, with a proximity
/// wave over the letters and a thin auto-fading scroll thumb.
/// </summary>
/// <remarks>
/// The control is purely visual and gestural — it never sees the data model. The hosting page (via the
/// shared page bases) feeds it the ordered letter set, points it at the active <see cref="ListViewBase"/>,
/// converts <see cref="ScrollProgress"/> offsets into highlighted letters / bubble text and performs the
/// smooth section jump when <see cref="LetterTapped"/> fires.
/// </remarks>
public sealed partial class AlphabetScrubber : UserControl
{
	private const double DragThreshold = 5.0;       // px of vertical travel before a press becomes a drag
	private const double WaveReach = 4.5;           // wave falloff spread, in letter rows (bigger = wider bulge)
	private const double WaveStrength = 1.25;       // extra scale at the finger (scale = 1 + WaveStrength·f)
	private const double WavePush = 8.0;            // px a letter slides left out of the rail at full falloff
	private const double IdleLetterOpacity = 0.38;  // prototype idle letter alpha
	private const double EmptyLetterOpacity = 0.12;
	private const double EmptyBoost = 0.35;         // empty letters brighten less under the wave
	private const double WaveEaseTau = 58.0;        // ms — equals the prototype's 0.25-per-frame ease at 60fps
	private const double PointerEaseTau = 55.0;     // ms — wave peak glide toward the finger (bigger = more trailing)
	private const double ThumbEaseTau = 75.0;       // ms — thumb fade ease
	private const double ThumbFadeMs = 700;

	private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
	private readonly DispatcherTimer _thumbTimer = new() { Interval = TimeSpan.FromMilliseconds(ThumbFadeMs) };
	private bool _ticking;
	private long _lastTickTimestamp;
	private double _waveY;                          // eased wave peak — glides toward the finger
	private bool _waveYValid;
	private readonly List<string> _letters = [];
	private readonly List<TextBlock> _blocks = [];
	private readonly List<TranslateTransform> _translates = [];
	private readonly List<Visual?> _visuals = [];
	private readonly List<double> _scales = [];
	private readonly List<double> _opacities = [];
	private readonly List<double> _offsets = [];
	private readonly List<double> _restOpacity = [];
	private readonly List<bool> _available = [];
	private readonly HashSet<string> _highlighted = [];
	private readonly SolidColorBrush _letterBrush;
	private bool _geometryStale;                    // letter bounds changed → CenterPoint needs a re-apply

	private ListViewBase? _targetList;
	private ScrollViewer? _scrollViewer;

	private bool _railVisible = true;
	private bool _pressed, _moved;
	private double _pressY;
	private double _pointerY = double.NaN;          // NaN → pointer not on the rail; wave at rest
	private double _thumbOpacity;
	private double _thumbTarget;

	/// <summary>Raised for a quick press (no drag) with the nearest letter that has content.</summary>
	public event EventHandler<string>? LetterTapped;

	/// <summary>Raised on every scroll change of the target list with the current offset and viewport height.</summary>
	public event EventHandler<(double Offset, double Viewport)>? ScrollProgress;

	/// <summary>True while the user is actively dragging the rail (press + move beyond the threshold).</summary>
	public bool IsDragging => _pressed && _moved;

	/// <summary>
	/// Initializes a new instance of the <see cref="AlphabetScrubber"/> class.
	/// </summary>
	public AlphabetScrubber()
	{
		InitializeComponent();
		HorizontalAlignment = HorizontalAlignment.Right;
		VerticalAlignment = VerticalAlignment.Stretch;

		_letterBrush = new SolidColorBrush(Colors.White);

		ApplyTheme();

		ActualThemeChanged += (_, _) => ApplyTheme();
		Rail.PointerPressed += Rail_PointerPressed;
		Rail.PointerMoved += Rail_PointerMoved;
		Rail.PointerReleased += Rail_PointerReleased;
		Rail.PointerCanceled += Rail_PointerReleased;
		Rail.PointerExited += Rail_PointerExited;
		Rail.SizeChanged += (_, _) => { ApplyLetterGeometry(); RetryScrollViewer(); };
		_thumbTimer.Tick += (_, _) => { _thumbTarget = 0; EnsureTicking(); };
		Unloaded += (_, _) =>
		{
			if (_ticking)
			{
				_ticking = false;
				CompositionTarget.Rendering -= OnRenderingTick;
			}
			_thumbTimer.Stop();
			_pressed = false;
			_moved = false;
			_pointerY = double.NaN;
			_waveYValid = false;
		};
	}

	// ── Public API ────────────────────────────────────────────────

	/// <summary>
	/// The list the scrubber drives: its inner <see cref="ScrollViewer"/> is resolved for drag scrolling
	/// and scroll reporting. Re-set when the page switches between views (e.g. List ↔ Compact).
	/// </summary>
	public ListViewBase? TargetList
	{
		get => _targetList;
		set
		{
			if (ReferenceEquals(_targetList, value)) return;
			if (_scrollViewer != null)
			{
				_scrollViewer.ViewChanged -= OnTargetViewChanged;
				_scrollViewer = null;
			}
			_targetList = value;
			ResolveScrollViewer();
		}
	}

	/// <summary>
	/// Rebuilds the rail. <paramref name="lettersInOrder"/> is the full ordered letter set (# placement and
	/// descending reversal already applied by the caller); <paramref name="availableLetters"/> marks the
	/// letters that actually have content — the rest render dimmed and are skipped by taps and the bubble.
	/// With <paramref name="railVisible"/> false (sorts without letters, e.g. Duration) the rail hides but
	/// the thumb keeps working.
	/// </summary>
	public void SetLetters(IReadOnlyList<string> lettersInOrder, IEnumerable<string> availableLetters, bool railVisible)
	{
		var available = new HashSet<string>(availableLetters);
		_letters.Clear();
		_letters.AddRange(lettersInOrder);
		_highlighted.Clear();
		_blocks.Clear();
		_translates.Clear();
		_visuals.Clear();
		_scales.Clear();
		_opacities.Clear();
		_offsets.Clear();
		_restOpacity.Clear();
		_available.Clear();
		Rail.Children.Clear();
		Rail.RowDefinitions.Clear();
		_geometryStale = true;

		for (var i = 0; i < _letters.Count; i++)
		{
			var letter = _letters[i];
			var isAvailable = available.Contains(letter);

			Rail.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
			var block = new TextBlock
			{
				Text = letter,
				FontSize = 11.5,
				FontWeight = FontWeights.SemiBold,
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(0, 0, 14, 0)
			};
			// the slide-out is a plain XAML translate: an element visual's Offset is layout-managed by
			// XAML and writing it would throw every letter to the top of the rail
			var translate = new TranslateTransform();
			block.RenderTransform = translate;
		ToolTipService.SetPlacement(block, PlacementMode.Left);
		ToolTipService.SetToolTip(block, letter);
		AutomationProperties.SetName(block, letter);
			Grid.SetRow(block, i);
			Rail.Children.Add(block);

			_blocks.Add(block);
			_translates.Add(translate);
			_visuals.Add(TryGetVisual(block));
			_available.Add(isAvailable);
			_restOpacity.Add(isAvailable ? IdleLetterOpacity : EmptyLetterOpacity);
			_scales.Add(1);
			_opacities.Add(_restOpacity[i]);
			_offsets.Add(0);
		}

		_railVisible = railVisible && _letters.Count > 0;
		Rail.Visibility = _railVisible ? Visibility.Visible : Visibility.Collapsed;
		ApplyTheme();
		_dispatcherQueue.TryEnqueue(ApplyLetterGeometry);
		EnsureTicking();
	}

	/// <summary>Lights up every letter that currently has items on screen (computed by the host).</summary>
	public void SetHighlightedLetters(IEnumerable<string> letters)
	{
		_highlighted.Clear();
		foreach (var letter in letters)
			_highlighted.Add(letter);
		for (var i = 0; i < _letters.Count; i++)
			_restOpacity[i] = _highlighted.Contains(_letters[i]) ? 1 : _available[i] ? IdleLetterOpacity : EmptyLetterOpacity;
		EnsureTicking();
	}

	// ── Pointer handling ──────────────────────────────────────────

	private void Rail_PointerPressed(object sender, PointerRoutedEventArgs e)
	{
		if (!_railVisible) return;
		var position = e.GetCurrentPoint(Rail).Position;
		_pressed = true;
		_moved = false;
		_pressY = position.Y;
		_pointerY = position.Y;
		Rail.CapturePointer(e.Pointer);
		EnsureTicking();
		e.Handled = true;
	}

	private void Rail_PointerMoved(object sender, PointerRoutedEventArgs e)
	{
		var y = e.GetCurrentPoint(Rail).Position.Y;
		_pointerY = y;
		if (_pressed)
		{
			if (!_moved && Math.Abs(y - _pressY) > DragThreshold) _moved = true;
			if (_moved)
				DragTo(y);
		}
		EnsureTicking();
		e.Handled = true;
	}

	private void Rail_PointerReleased(object sender, PointerRoutedEventArgs e)
	{
		if (!_pressed) return;
		if (!_moved)
		{
			var letter = NearestAvailableLetter(IndexFromY(_pressY));
			if (letter != null) LetterTapped?.Invoke(this, letter);
		}
		_pressed = false;
		_moved = false;
		Rail.ReleasePointerCapture(e.Pointer);
		// keep the hover wave alive when the mouse stays on the rail
		var position = e.GetCurrentPoint(Rail).Position;
		if (position.Y < 0 || position.Y > Rail.ActualHeight) _pointerY = double.NaN;
		EnsureTicking();
		e.Handled = true;
	}

	private void Rail_PointerExited(object sender, PointerRoutedEventArgs e)
	{
		if (_pressed) return;
		_pointerY = double.NaN;
		EnsureTicking();
	}

	/// <summary>Scrollbar-style drag: finger position maps proportionally onto the full scroll range.</summary>
	private void DragTo(double y)
	{
		if (_scrollViewer == null) return;
		var scrollable = _scrollViewer.ScrollableHeight;
		if (scrollable <= 0) return;
		var fraction = Math.Clamp(y / Math.Max(1, Rail.ActualHeight), 0, 1);
		_scrollViewer.ChangeView(null, fraction * scrollable, null, disableAnimation: true);
	}

	private int IndexFromY(double y)
		=> Math.Clamp((int)(y / Math.Max(1, Rail.ActualHeight) * _letters.Count), 0, Math.Max(0, _letters.Count - 1));

	/// <summary>Nearest rail index that actually has content — empty letters are skipped.</summary>
	private string? NearestAvailableLetter(int index)
	{
		string? best = null;
		var bestDistance = int.MaxValue;
		for (var i = 0; i < _letters.Count; i++)
		{
			if (!_available[i]) continue;
			var distance = Math.Abs(i - index);
			if (distance < bestDistance)
			{
				bestDistance = distance;
				best = _letters[i];
			}
		}
		return best;
	}

	// ── Animation tick ────────────────────────────────────────────

	/// <summary>
	/// Per-rendered-frame tick (composition render loop — the WinUI equivalent of the prototype's
	/// requestAnimationFrame loop) driving both animated channels: the letter wave and the thumb fade.
	/// All easing is frame-rate independent, and the wave peak glides toward the finger so a drag reads
	/// as a traveling wave rather than letters swelling in place. Stops itself when everything settled.
	/// </summary>
	private void OnRenderingTick(object? sender, object e)
	{
		var now = Stopwatch.GetTimestamp();
		var deltaMs = Math.Min(100.0, (now - _lastTickTimestamp) * 1000.0 / Stopwatch.Frequency);
		_lastTickTimestamp = now;

		if (Tick(deltaMs)) return;
		_ticking = false;
		CompositionTarget.Rendering -= OnRenderingTick;
	}

	private bool Tick(double deltaMs)
	{
		var active = false;

		// letter bounds became measurable since the last rebuild → apply the scale anchors now
		if (_geometryStale && _blocks.Count > 0 && _blocks[0].ActualWidth > 0)
		{
			_geometryStale = false;
			ApplyLetterGeometry();
		}

		// the wave peak eases toward the finger instead of following it exactly
		if (!double.IsNaN(_pointerY))
		{
			if (!_waveYValid)
			{
				_waveY = _pointerY;
				_waveYValid = true;
			}
			_waveY += (_pointerY - _waveY) * (1 - Math.Exp(-deltaMs / PointerEaseTau));
			if (Math.Abs(_pointerY - _waveY) > 0.1) active = true;
		}
		else
		{
			_waveYValid = false;
		}

		var rowCount = Math.Max(1, _blocks.Count);
		var rowHeight = Math.Max(1, Rail.ActualHeight) / rowCount;
		var waveMove = 1 - Math.Exp(-deltaMs / WaveEaseTau);
		for (var i = 0; i < _blocks.Count; i++)
		{
			double falloff = 0;
			if (_waveYValid)
			{
				var distance = (_waveY - (i + 0.5) * rowHeight) / rowHeight;
				falloff = Math.Exp(-distance * distance / WaveReach);
			}

			var targetScale = 1 + WaveStrength * falloff;
			var targetOpacity = _restOpacity[i] + (1 - _restOpacity[i]) * falloff * (_available[i] ? 1 : EmptyBoost);
			var targetOffset = -falloff * WavePush;   // letters slide left out of the rail as the wave passes

			_scales[i] += (targetScale - _scales[i]) * waveMove;
			_opacities[i] += (targetOpacity - _opacities[i]) * waveMove;
			_offsets[i] += (targetOffset - _offsets[i]) * waveMove;
			if (Math.Abs(targetScale - _scales[i]) > 0.002 || Math.Abs(targetOpacity - _opacities[i]) > 0.002 || Math.Abs(targetOffset - _offsets[i]) > 0.05) active = true;

			_translates[i].X = _offsets[i];

			if (_visuals[i] is not { } visual) continue;
			var scale = (float)_scales[i];
			visual.Scale = new Vector3(scale, scale, 1);
			visual.Opacity = (float)_opacities[i];
		}

		_thumbOpacity += (_thumbTarget - _thumbOpacity) * (1 - Math.Exp(-deltaMs / ThumbEaseTau));
		if (Math.Abs(_thumbTarget - _thumbOpacity) > 0.01) active = true;
		Thumb.Opacity = _thumbOpacity;

		return active;
	}

	private void EnsureTicking()
	{
		if (_ticking) return;
		_ticking = true;
		_lastTickTimestamp = Stopwatch.GetTimestamp();
		CompositionTarget.Rendering += OnRenderingTick;
	}

	/// <summary>Scales letters around their right-center, matching the prototype's transform-origin.</summary>
	private void ApplyLetterGeometry()
	{
		if (_blocks.Count == 0 || _blocks[0].ActualWidth <= 0) return;   // layout not ready yet — the tick retries
		_geometryStale = false;
		for (var i = 0; i < _blocks.Count; i++)
		{
			if (_visuals[i] is not { } visual) continue;
			visual.CenterPoint = new Vector3((float)_blocks[i].ActualWidth, (float)(_blocks[i].ActualHeight / 2), 0);
		}
	}

	// ── Scroll target plumbing ────────────────────────────────────

	/// <summary>
	/// Resolves the inner ScrollViewer of the target list on the next dispatcher pass (the template needs
	/// a layout cycle after view switches) and wires the ViewChanged feed.
	/// </summary>
	private void ResolveScrollViewer()
		=> _dispatcherQueue.TryEnqueue(() =>
		{
			if (_targetList == null) return;
			_scrollViewer = _targetList.FindDescendant<ScrollViewer>();
			if (_scrollViewer == null) return;
			_scrollViewer.ViewChanged -= OnTargetViewChanged;   // guard against double wiring
			_scrollViewer.ViewChanged += OnTargetViewChanged;
			PublishScrollProgress();
		});

	/// <summary>Retries ScrollViewer resolution after size changes, e.g. right after the page loads.</summary>
	private void RetryScrollViewer()
	{
		if (_targetList != null && _scrollViewer == null) ResolveScrollViewer();
	}

	private void OnTargetViewChanged(object? sender, ScrollViewerViewChangedEventArgs args)
	{
		if (_scrollViewer == null) return;

		var viewport = _scrollViewer.ViewportHeight;
		var scrollable = _scrollViewer.ScrollableHeight;
		if (viewport <= 0 || scrollable <= 0)
		{
			_thumbTarget = 0;
			EnsureTicking();
		}
		else
		{
			var track = Math.Max(1, ActualHeight);
			var height = Math.Max(40, viewport / (viewport + scrollable) * track);
			Thumb.Height = height;
			Thumb.Margin = new Thickness(0, Math.Clamp(_scrollViewer.VerticalOffset / scrollable, 0, 1) * (track - height), 3, 0);
			ShowThumb();
		}

		PublishScrollProgress();
	}

	private void ShowThumb()
	{
		if (_thumbTarget < 1)
		{
			_thumbTarget = 1;
			EnsureTicking();
		}
		_thumbTimer.Stop();
		_thumbTimer.Start();
	}

	private void PublishScrollProgress()
	{
		if (_scrollViewer == null) return;
		ScrollProgress?.Invoke(this, (_scrollViewer.VerticalOffset, _scrollViewer.ViewportHeight));
	}

	// ── Theming ───────────────────────────────────────────────────

	/// <summary>
	/// Theme-aware colors: letters White/Black with dimming carried purely by opacity. Called on
	/// construction, theme changes and rail rebuilds.
	/// </summary>
	private void ApplyTheme()
	{
		var dark = ActualTheme == ElementTheme.Dark;
		_letterBrush.Color = dark ? Colors.White : Colors.Black;

		Thumb.Background = new SolidColorBrush(dark ? Color.FromArgb(72, 255, 255, 255) : Color.FromArgb(64, 0, 0, 0));
		foreach (var block in _blocks)
			block.Foreground = _letterBrush;
	}

	/// <summary>Gets the composition backing of an element; null when composition is unavailable.</summary>
	private static Visual? TryGetVisual(UIElement element)
	{
		try
		{
			return ElementCompositionPreview.GetElementVisual(element);
		}
		catch
		{
			return null;
		}
	}
}
