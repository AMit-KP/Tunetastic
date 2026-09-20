using System.Diagnostics;
using System.Globalization;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace Tunetastic.Common.Controls;

/// <summary>
/// A label that shows the latest library scan results (library count, songs count and last scan time)
/// with an odometer style animation. Digit columns roll only when their digit actually changes and every
/// column rests exactly on its digit, and every character of the scan time rolls through random letters
/// like a slot machine before landing on its target. When a value changes, only the changed characters roll.
/// </summary>
/// <remarks>
/// The values live in <see cref="ApplicationData"/>.LocalSettings under the <see cref="LocalSave"/>.ScanResult_*
/// keys, which <see cref="LibraryScanner"/> refreshes after every scan. The label subscribes to the property
/// set's MapChanged event and additionally polls every 2 seconds, so it always reflects the latest scan result.
/// </remarks>
public sealed partial class AnimatedScanResultLabel : UserControl
{
	private const double SlotHeight = 24;
	private const double CountDurationMs = 3000;
	private const double LetterDurationMs = 950;
	private const double LetterStaggerMs = 40;
	private const double BounceAmplitude = 1.70158;
	private const string ScrambleCharset = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

	private readonly DispatcherTimer _ticker = new() { Interval = TimeSpan.FromMilliseconds(16) };
	private readonly DispatcherTimer _pollTimer = new() { Interval = TimeSpan.FromSeconds(2) };
	private readonly List<CountAnimation> _activeCounts = [];
	private readonly Dictionary<char, double> _charWidthCache = [];
	private List<(char Ch, double Width)>? _charsetWidths;

	private Storyboard? _timeStoryboard;
	private int _timeGeneration;

	private long _libraries;
	private long _songs;
	private long _folders;
	private string? _time;
	private string? _message;
	private bool _showingMessage;

	private bool _loaded;
	private IPropertySet? _trackedValues;

	/// <summary>
	/// Initializes a new instance of the <see cref="AnimatedScanResultLabel"/> class.
	/// </summary>
	public AnimatedScanResultLabel()
	{
		InitializeComponent();

		_ticker.Tick += (_, _) => TickCounts();
		_pollTimer.Tick += (_, _) => UpdateStatValues(false);
		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
	}

	// ── Lifecycle ─────────────────────────────────────────────────

	/// <summary>
	/// Subscribes to scan result changes and plays the entry animation.
	/// </summary>
	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		if (_loaded) return;
		_loaded = true;

		_trackedValues = ApplicationData.Current.LocalSettings.Values;
		if (_trackedValues != null) _trackedValues.MapChanged += OnValuesChanged;

		UpdateStatValues(true);
		_pollTimer.Start();
	}

	/// <summary>
	/// Stops all animations and unsubscribes from scan result changes.
	/// </summary>
	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		if (!_loaded) return;
		_loaded = false;

		if (_trackedValues != null)
		{
			_trackedValues.MapChanged -= OnValuesChanged;
			_trackedValues = null;
		}
		_pollTimer.Stop();
		_ticker.Stop();
		_activeCounts.Clear();
		_timeStoryboard?.Stop();
		_timeStoryboard = null;
	}

	// ── Value updates ─────────────────────────────────────────────

	/// <summary>
	/// Handles changes on the LocalSettings property set. Marshalled to the UI thread, then the four
	/// values are re-read and only the ones that actually changed get animated.
	/// </summary>
	private void OnValuesChanged(IObservableMap<string, object> sender, IMapChangedEventArgs<string> args)
		=> DispatcherQueue.TryEnqueue(() => UpdateStatValues(false));

	/// <summary>
	/// Reads the four scan result values and the situational message, then animates whatever changed.
	/// </summary>
	/// <param name="initial">When true the counts start from zero and the time rolls in completely.</param>
	private void UpdateStatValues(bool initial)
	{
		var values = ApplicationData.Current.LocalSettings.Values;
		if (values == null) return;

		ApplyStatValues(initial,
			ReadLong(values, nameof(LocalSave.ScanResult_LibraryCount)),
			ReadLong(values, nameof(LocalSave.ScanResult_SongsCount)),
			ReadLong(values, nameof(LocalSave.ScanResult_FolderCount)),
			ReadString(values, nameof(LocalSave.ScanResult_Time)) ?? "Never",
			ReadString(values, nameof(LocalSave.ScanResult_Message)));
	}

	/// <summary>
	/// Applies a set of scan result values to the label. When a scan result message is present (the last
	/// scan failed, e.g. "No libraries found"), it replaces the library/songs/folders counts while the
	/// last scanned stat hides; a cleared message brings the counts back.
	/// </summary>
	private void ApplyStatValues(bool initial, long libraries, long songs, long folders, string time, string? message)
	{
		var hasMessage = !string.IsNullOrEmpty(message);
		if (hasMessage != _showingMessage)
		{
			_showingMessage = hasMessage;
			LibrariesStat.Visibility = hasMessage ? Visibility.Collapsed : Visibility.Visible;
			CountsDot.Visibility = hasMessage ? Visibility.Collapsed : Visibility.Visible;
			SongsStat.Visibility = hasMessage ? Visibility.Collapsed : Visibility.Visible;
			FoldersStat.Visibility = hasMessage ? Visibility.Collapsed : Visibility.Visible;
			TimeDot.Visibility = hasMessage ? Visibility.Collapsed : Visibility.Visible;
			TimeStat.Visibility = hasMessage ? Visibility.Collapsed : Visibility.Visible;
			MessageValue.Visibility = hasMessage ? Visibility.Visible : Visibility.Collapsed;
		}

		if (initial)
		{
			AnimateCount(LibrariesValue, 0, libraries);
			AnimateCount(SongsValue, 0, songs);
			AnimateCount(FoldersValue, 0, folders);
			AnimateScramble(TimeValue, time, string.Empty);
		}
		else
		{
			if (libraries != _libraries) AnimateCount(LibrariesValue, _libraries, libraries);
			if (songs != _songs) AnimateCount(SongsValue, _songs, songs);
			if (folders != _folders) AnimateCount(FoldersValue, _folders, folders);
			if (time != _time) AnimateScramble(TimeValue, time, _time ?? string.Empty);
		}

		if (message != _message)
		{
			if (string.IsNullOrEmpty(message))
				MessageValue.Children.Clear();
			else
				AnimateScramble(MessageValue, message, _message ?? string.Empty);
		}

		_libraries = libraries;
		_songs = songs;
		_folders = folders;
		_time = time;
		_message = message;
	}

	// ── Odometer counts ───────────────────────────────────────────

	/// <summary>
	/// Animates a numeric value from <paramref name="from"/> to <paramref name="to"/>. Each digit column
	/// rolls only when its own digit changes, taking the short path (plus a few extra full spins for large
	/// jumps so big changes still feel like a spinning odometer). Columns whose digit is the same in both
	/// values never move, so nothing is ever shown half-scrolled.
	/// </summary>
	private void AnimateCount(StackPanel panel, long from, long to)
	{
		if (from == to)
		{
			RenderStatic(panel, to.ToString("N0", CultureInfo.CurrentCulture));
			return;
		}

		_activeCounts.RemoveAll(a => a.Panel == panel);

		panel.Children.Clear();
		var text = to.ToString("N0", CultureInfo.CurrentCulture);
		var columns = new List<RollingColumn>();
		var places = new long[text.Length];
		var place = 1L;
		for (var i = text.Length - 1; i >= 0; i--)
		{
			if (char.IsDigit(text[i]))
			{
				places[i] = place;
				place *= 10;
			}
		}

		var direction = to > from ? 1 : -1;
		var digitSlotWidth = CharWidth('0');

		for (var i = 0; i < text.Length; i++)
		{
			if (places[i] > 0)
			{
				var digitFrom = (int)((from / places[i]) % 10);
				var digitTo = (int)((to / places[i]) % 10);

				// MakeSlot already parents the strip inside the clipped slot grid.
				var slot = MakeSlot(digitSlotWidth, out var strip, out var translate);
				for (var copy = 0; copy < 3; copy++)
					for (var digit = 0; digit <= 9; digit++)
						strip.Children.Add(MakeCharText((char)('0' + digit), digitSlotWidth));
				panel.Children.Add(slot);

				var steps = digitTo - digitFrom;
				if (to > from && steps < 0) steps += 10;
				if (to < from && steps > 0) steps -= 10;
				var extraSpins = Math.Min((int)(Math.Abs(to - from) / (places[i] * 10)), 4);
				var total = steps + direction * 10 * extraSpins;

				if (total == 0)
				{
					translate.Y = -digitTo * SlotHeight;
					continue;
				}
				columns.Add(new RollingColumn
				{
					Translate = translate,
					Start = digitFrom,
					Total = total,
					DigitTo = digitTo,
					Amplitude = Math.Min(BounceAmplitude, Math.Cbrt(8.1 / Math.Abs(total)))
				});
			}
			else
			{
				panel.Children.Add(MakeCharText(text[i], CharWidth(text[i])));
			}
		}

		if (columns.Count == 0) return;
		_activeCounts.Add(new CountAnimation { Panel = panel, Columns = columns, From = from, To = to, Watch = Stopwatch.StartNew() });
		if (!_ticker.IsEnabled) _ticker.Start();
	}

	/// <summary>
	/// Advances every running count animation by one frame. Each column rides its own eased distance and
	/// snaps exactly onto its final digit when the animation completes.
	/// </summary>
	private void TickCounts()
	{
		for (var i = _activeCounts.Count - 1; i >= 0; i--)
		{
			var animation = _activeCounts[i];
			var progress = Math.Clamp(animation.Watch.Elapsed.TotalMilliseconds / CountDurationMs, 0, 1);

			foreach (var column in animation.Columns)
			{
				var eased = EaseOutBack(progress, column.Amplitude);
				var steps = column.Start + column.Total * eased;
				var offset = (steps % 10 + 10) % 10;
				column.Translate.Y = -offset * SlotHeight;
			}

			if (progress < 1) continue;

			foreach (var column in animation.Columns)
				column.Translate.Y = -column.DigitTo * SlotHeight;
			_activeCounts.RemoveAt(i);
		}

		if (_activeCounts.Count == 0) _ticker.Stop();
	}

	// ── Slot machine letters ──────────────────────────────────────

	/// <summary>
	/// Rolls every character of <paramref name="next"/> through random letters and settles on the target
	/// with a small bounce. Characters identical to <paramref name="previous"/> stay still.
	/// </summary>
	private void AnimateScramble(StackPanel panel, string next, string previous)
	{
		if (next == previous)
		{
			RenderStatic(panel, next);
			return;
		}

		var generation = ++_timeGeneration;
		_timeStoryboard?.Stop();

		panel.Children.Clear();
		var storyboard = new Storyboard();
		for (var i = 0; i < next.Length; i++)
		{
			var ch = next[i];
			if (ch == ' ')
			{
				panel.Children.Add(MakeCharText('\u00A0', CharWidth('\u00A0')));
				continue;
			}

			var previousChar = i < previous.Length ? previous[i] : (char?)null;
			if (previousChar == ch)
			{
				panel.Children.Add(MakeCharText(ch, CharWidth(ch)));
				continue;
			}

			// The slot only ever gets as wide as the wider of the old and new characters, and the random
			// roll characters are picked from the ones that fit inside it, so the line never expands
			// while the letters are spinning.
			var rolls = 6 + i % 4;
			var slotWidth = Math.Max(CharWidth(ch), previousChar.HasValue ? CharWidth(previousChar.Value) : 0);
			var start = previousChar.HasValue && previousChar.Value != ' ' ? previousChar.Value : RandomChar(slotWidth);
			var sequence = new List<char> { start };
			for (var k = 0; k < rolls; k++)
				sequence.Add(RandomChar(slotWidth));
			sequence.Add(ch);
			sequence.Add(RandomChar(slotWidth));
			sequence.Add(RandomChar(slotWidth));

			var slot = MakeSlot(slotWidth, out var strip, out var translate);
			foreach (var c in sequence)
				strip.Children.Add(MakeCharText(c, slotWidth));
			panel.Children.Add(slot);

			var animation = new DoubleAnimation
			{
				From = 0,
				To = -(sequence.Count - 3) * SlotHeight,
				Duration = new Duration(TimeSpan.FromMilliseconds(LetterDurationMs + i * LetterStaggerMs)),
				BeginTime = TimeSpan.FromMilliseconds(i * LetterStaggerMs),
				EasingFunction = new BackEase { Amplitude = BounceAmplitude, EasingMode = EasingMode.EaseOut },
				EnableDependentAnimation = true
			};
			Storyboard.SetTarget(animation, translate);
			Storyboard.SetTargetProperty(animation, "Y");
			storyboard.Children.Add(animation);
		}

		storyboard.Completed += (_, _) =>
		{
			if (generation != _timeGeneration) return;
			_timeStoryboard = null;
			RenderStatic(panel, next);
		};
		_timeStoryboard = storyboard;
		storyboard.Begin();
	}

	// ── Slot building blocks ──────────────────────────────────────

	/// <summary>
	/// Replaces the panel content with plain, non-animated character slots.
	/// </summary>
	private void RenderStatic(StackPanel panel, string text)
	{
		panel.Children.Clear();
		foreach (var ch in text)
		{
			var c = ch == ' ' ? '\u00A0' : ch;
			panel.Children.Add(MakeCharText(c, CharWidth(c)));
		}
	}

	/// <summary>
	/// Creates one character window: a fixed width grid clipping a vertical strip of characters, with the
	/// strip already parented inside it.
	/// </summary>
	private Grid MakeSlot(double slotWidth, out StackPanel strip, out TranslateTransform translate)
	{
		var slot = new Grid { Width = slotWidth, Height = SlotHeight };
		slot.Clip = new RectangleGeometry { Rect = new Windows.Foundation.Rect(0, 0, slotWidth, SlotHeight) };

		strip = new StackPanel { Orientation = Orientation.Vertical };
		translate = new TranslateTransform();
		strip.RenderTransform = translate;
		slot.Children.Add(strip);
		return slot;
	}

	/// <summary>
	/// Creates one character cell for a strip: a bold text block at its natural line height, vertically
	/// centered inside a fixed height cell so resting characters line up with the surrounding labels.
	/// </summary>
	private Grid MakeCharText(char ch, double slotWidth)
	{
		var cell = new Grid { Height = SlotHeight };
		cell.Children.Add(new TextBlock
		{
			Text = ch.ToString(),
			FontSize = 15,
			FontWeight = FontWeights.Bold,
			Width = slotWidth,
			TextAlignment = TextAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
		});
		return cell;
	}

	/// <summary>
	/// Measures the advance width of a character in the default font with the bold value weight, cached
	/// per character.
	/// </summary>
	private double CharWidth(char ch)
	{
		if (!_charWidthCache.TryGetValue(ch, out var width))
		{
			try
			{
				var probe = new TextBlock { FontSize = 15, FontWeight = FontWeights.Bold, Text = ch.ToString() };
				probe.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
				width = probe.DesiredSize.Width > 0 ? Math.Ceiling(probe.DesiredSize.Width) : 9;
			}
			catch
			{
				width = 9;
			}
			_charWidthCache[ch] = width;
		}
		return width;
	}

	/// <summary>
	/// Picks a random roll character whose width fits inside the given slot width, so wide letters never
	/// force a narrow slot (and the whole line) to expand mid-roll.
	/// </summary>
	private char RandomChar(double slotWidth)
	{
		_charsetWidths ??= ScrambleCharset.Select(c => (Ch: c, Width: CharWidth(c))).OrderBy(x => x.Width).ToList();
		var fitting = _charsetWidths.Where(x => x.Width <= slotWidth).ToList();
		return fitting.Count == 0
			? ScrambleCharset[Random.Shared.Next(ScrambleCharset.Length)]
			: fitting[Random.Shared.Next(fitting.Count)].Ch;
	}

	/// <summary>
	/// Ease-out-with-bounce where the overshoot stays within about one character regardless of how far
	/// a column travels, so long spins don't end in a big reverse flick.
	/// </summary>
	private static double EaseOutBack(double t, double amplitude)
	{
		var c3 = amplitude + 1;
		var x = t - 1;
		return 1 + c3 * x * x * x + amplitude * x * x;
	}

	private static long ReadLong(IPropertySet values, string key)
		=> values.TryGetValue(key, out var value) && value is IConvertible convertible
			? Convert.ToInt64(convertible, CultureInfo.InvariantCulture)
			: 0;

	private static string? ReadString(IPropertySet values, string key)
		=> values.TryGetValue(key, out var value) && value is string text ? text : null;

	private sealed class RollingColumn
	{
		public required TranslateTransform Translate { get; init; }
		public required int Start { get; init; }
		public required int Total { get; init; }
		public required int DigitTo { get; init; }
		public required double Amplitude { get; init; }
	}

	private sealed class CountAnimation
	{
		public required StackPanel Panel { get; init; }
		public required List<RollingColumn> Columns { get; init; }
		public required long From { get; init; }
		public required long To { get; init; }
		public required Stopwatch Watch { get; init; }
	}
}
