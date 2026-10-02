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
/// wave over the letters, a large letter bubble that follows the finger and a thin auto-fading scroll thumb.
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
	private const double WaveReach = 3.0;           // wave falloff spread, in letter rows
	private const double WaveStrength = 0.8;        // extra scale at the finger (scale = 1 + WaveStrength·f)
	private const double BubbleSize = 86;           // side of the letter bubble
	private const double BubbleGap = 14;            // gap between bubble and the rail
	private const double BubbleHalf = BubbleSize / 2;
	private const double IdleLetterOpacity = 0.38;  // prototype idle letter alpha
	private const double EmptyLetterOpacity = 0.12;
	private const double EmptyBoost = 0.35;         // empty letters brighten less under the wave
	private const double ThumbFadeMs = 700;

	private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
	private readonly DispatcherTimer _ticker = new() { Interval = TimeSpan.FromMilliseconds(16) };
	private readonly DispatcherTimer _thumbTimer = new() { Interval = TimeSpan.FromMilliseconds(ThumbFadeMs) };
	private readonly List<string> _letters = [];
	private readonly List<TextBlock> _blocks = [];
	private readonly List<Visual?> _visuals = [];
	private readonly List<double> _scales = [];
	private readonly List<double> _opacities = [];
	private readonly List<double> _restOpacity = [];
	private readonly List<bool> _available = [];
	private readonly HashSet<string> _highlighted = [];
	private readonly SolidColorBrush _letterBrush;

	private ListViewBase? _targetList;
	private ScrollViewer? _scrollViewer;
	private Popup? _bubblePopup;
	private TextBlock _bubbleText = null!;
	private Border _bubbleBorder = null!;

	private bool _railVisible = true;
	private bool _pressed, _moved;
	private double _pressY;
	private double _pointerY = double.NaN;          // NaN → pointer not on the rail; wave at rest
	private bool _bubbleOpen;
	private double _bubbleVisibility;               // 0..1 animated popup visibility
	private double _bubblePulse = 1;                // 1.14 → 1 pulse on letter change
	private string? _bubbleLetter;
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

		BuildBubble();
		ApplyTheme();

		ActualThemeChanged += (_, _) => ApplyTheme();
		Rail.PointerPressed += Rail_PointerPressed;
		Rail.PointerMoved += Rail_PointerMoved;
		Rail.PointerReleased += Rail_PointerReleased;
		Rail.PointerCanceled += Rail_PointerReleased;
		Rail.PointerExited += Rail_PointerExited;
		Rail.SizeChanged += (_, _) => { ApplyLetterGeometry(); RetryScrollViewer(); };
		_ticker.Tick += (_, _) => Tick();
		_thumbTimer.Tick += (_, _) => { _thumbTarget = 0; EnsureTicking(); };
		Unloaded += (_, _) =>
		{
			_ticker.Stop();
			_thumbTimer.Stop();
			_pressed = false;
			_moved = false;
			_pointerY = double.NaN;
			_bubbleOpen = false;
			if (_bubblePopup != null) _bubblePopup.IsOpen = false;
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
		_visuals.Clear();
		_scales.Clear();
		_opacities.Clear();
		_restOpacity.Clear();
		_available.Clear();
		Rail.Children.Clear();
		Rail.RowDefinitions.Clear();

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
				Margin = new Thickness(0, 0, 14, 0)
			};
		ToolTipService.SetPlacement(block, PlacementMode.Left);
		ToolTipService.SetToolTip(block, letter);
		AutomationProperties.SetName(block, letter);
			Grid.SetRow(block, i);
			Rail.Children.Add(block);

			_blocks.Add(block);
			_visuals.Add(TryGetVisual(block));
			_available.Add(isAvailable);
			_restOpacity.Add(isAvailable ? IdleLetterOpacity : EmptyLetterOpacity);
			_scales.Add(1);
			_opacities.Add(_restOpacity[i]);
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

	/// <summary>Updates the bubble letter while the user is dragging (the host resolves the real top letter).</summary>
	public void SetBubbleLetter(string letter)
	{
		if (!_bubbleOpen || _bubbleLetter == letter) return;
		_bubbleLetter = letter;
		_bubbleText.Text = letter;
		_bubblePulse = 1.14;
		EnsureTicking();
	}

	// ── Bubble ────────────────────────────────────────────────────

	/// <summary>Creates the letter bubble as a code-built popup anchored to the control's root grid.</summary>
	private void BuildBubble()
	{
		_bubbleText = new TextBlock { FontSize = 40, FontWeight = FontWeights.SemiBold };
		_bubbleBorder = new Border
		{
			Width = BubbleSize,
			Height = BubbleSize,
			CornerRadius = new CornerRadius(26),
			Child = _bubbleText,
			IsHitTestVisible = false,
			Opacity = 0,
			RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5),
			RenderTransform = new ScaleTransform { ScaleX = 0.55, ScaleY = 0.55 }
		};
		_bubblePopup = new Popup
		{
			Child = _bubbleBorder,
			IsLightDismissEnabled = false,
			HorizontalOffset = -(BubbleSize + BubbleGap)
		};
		Root.Children.Add(_bubblePopup);
	}

	private void ShowBubbleAt(double y)
	{
		_bubbleLetter = NearestAvailableLetter(IndexFromY(y));
		if (_bubbleLetter == null) return;
		_bubbleText.Text = _bubbleLetter;
		MoveBubble(y);
		_bubblePopup!.IsOpen = true;
		_bubbleOpen = true;
		EnsureTicking();
	}

	/// <summary>Centers the bubble on the finger, clamped so it never leaves the window vertically.</summary>
	private void MoveBubble(double y)
	{
		if (_bubblePopup == null) return;
		var rootTop = TransformToVisual(null).TransformPoint(new Windows.Foundation.Point(0, 0)).Y;
		var window = XamlRoot?.Content as FrameworkElement;
		var windowHeight = window?.ActualHeight ?? ActualHeight;
		var clamped = Math.Clamp(rootTop + y, BubbleHalf + 4, Math.Max(BubbleHalf + 4, windowHeight - BubbleHalf - 4));
		_bubblePopup.VerticalOffset = clamped - rootTop - BubbleHalf;
	}

	private void CloseBubble()
	{
		_bubbleOpen = false;
		EnsureTicking();   // the tick closes the popup once the fade-out finishes
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
		ShowBubbleAt(_pressY);
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
			{
				DragTo(y);
				MoveBubble(y);
			}
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
		CloseBubble();
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
	/// Single per-frame tick (house DispatcherTimer pattern) driving all three animated channels: the
	/// letter wave (eased toward gaussian targets), the bubble scale/fade and the thumb fade. Stops
	/// itself when everything has settled.
	/// </summary>
	private void Tick()
	{
		var active = false;

		var rowCount = Math.Max(1, _blocks.Count);
		var rowHeight = Math.Max(1, Rail.ActualHeight) / rowCount;
		for (var i = 0; i < _blocks.Count; i++)
		{
			double falloff = 0;
			if (!double.IsNaN(_pointerY))
			{
				var distance = (_pointerY - (i + 0.5) * rowHeight) / rowHeight;
				falloff = Math.Exp(-distance * distance / WaveReach);
			}

			var targetScale = 1 + WaveStrength * falloff;
			var targetOpacity = _restOpacity[i] + (1 - _restOpacity[i]) * falloff * (_available[i] ? 1 : EmptyBoost);

			_scales[i] += (targetScale - _scales[i]) * 0.25;
			_opacities[i] += (targetOpacity - _opacities[i]) * 0.25;
			if (Math.Abs(targetScale - _scales[i]) > 0.002 || Math.Abs(targetOpacity - _opacities[i]) > 0.002) active = true;

			if (_visuals[i] is not { } visual) continue;
			var scale = (float)_scales[i];
			visual.Scale = new Vector3(scale, scale, 1);
			visual.Opacity = (float)_opacities[i];
		}

		var bubbleTarget = _bubbleOpen ? 1 : 0;
		_bubbleVisibility += (bubbleTarget - _bubbleVisibility) * 0.35;
		_bubblePulse += (1 - _bubblePulse) * 0.3;
		if (Math.Abs(bubbleTarget - _bubbleVisibility) > 0.01 || Math.Abs(1 - _bubblePulse) > 0.005) active = true;

		if (_bubbleBorder.RenderTransform is ScaleTransform transform)
		{
			var bubbleScale = (0.55 + 0.45 * _bubbleVisibility) * _bubblePulse;
			transform.ScaleX = bubbleScale;
			transform.ScaleY = bubbleScale;
		}
		_bubbleBorder.Opacity = _bubbleVisibility;
		if (!_bubbleOpen && _bubbleVisibility <= 0.02 && _bubblePopup!.IsOpen) _bubblePopup.IsOpen = false;

		_thumbOpacity += (_thumbTarget - _thumbOpacity) * 0.2;
		if (Math.Abs(_thumbTarget - _thumbOpacity) > 0.01) active = true;
		Thumb.Opacity = _thumbOpacity;

		if (!active) _ticker.Stop();
	}

	private void EnsureTicking()
	{
		if (!_ticker.IsEnabled) _ticker.Start();
	}

	/// <summary>Scales letters around their right-center, matching the prototype's transform-origin.</summary>
	private void ApplyLetterGeometry()
	{
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
	/// Theme-aware colors: letters White/Black with dimming carried purely by opacity, bubble surfaces
	/// per theme. Called on construction, theme changes and rail rebuilds.
	/// </summary>
	private void ApplyTheme()
	{
		var dark = ActualTheme == ElementTheme.Dark;
		_letterBrush.Color = dark ? Colors.White : Colors.Black;

		Thumb.Background = new SolidColorBrush(dark ? Color.FromArgb(72, 255, 255, 255) : Color.FromArgb(64, 0, 0, 0));
		_bubbleBorder.Background = new SolidColorBrush(dark ? Color.FromArgb(255, 61, 61, 61) : Color.FromArgb(255, 243, 243, 243));
		_bubbleBorder.BorderBrush = new SolidColorBrush(dark ? Colors.Transparent : Color.FromArgb(26, 0, 0, 0));
		_bubbleBorder.BorderThickness = new Thickness(1);
		_bubbleText.Foreground = _letterBrush;
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
