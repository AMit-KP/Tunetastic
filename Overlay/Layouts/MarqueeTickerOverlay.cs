using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Tunetastic.Overlay.Layouts;

// ══════════════════════════════════════════════════════════════════════
// MARQUEE TICKER 
// Music-note icon · scrolling track+artist ticker · micro progress bar
// prev/play/next controls
// NOTE: Uses DevWinUI MarqueeText — see comment below for setup.
// ══════════════════════════════════════════════════════════════════════
public class MarqueeTickerOverlay : OverlayBase
{
	// Replace TextBlock below with DevWinUI MarqueeText once set up.
	// Example:
	//   var marquee = new DevWinUI.MarqueeText
	//   {
	//       Text     = "Track · Artist",
	//       Speed    = 40,
	//       Behavior = MarqueeBehavior.Ticker,
	//   };
	// Then swap _tickerText references for marquee.

	private TextBlock? _tickerText;   // TODO: replace with DevWinUI MarqueeText
	private Rectangle? _progressFill;
	private double _progressWidth = 80; // container width for progress calc

	public MarqueeTickerOverlay(OverlayTheme theme)
	{
		Theme = theme;
		RootGrid = Build();
	}

	private Grid Build()
	{
		var root = new Grid
		{
			Height = TaskbarHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
		};

		var pill = MakeRectBorder(36, 8);

		var inner = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			VerticalAlignment = VerticalAlignment.Center,
			Spacing = 8,
		};

		// Music note icon
		inner.Children.Add(new FontIcon
		{
			Glyph = "\uEC4F",
			FontSize = 14,
			Foreground = new SolidColorBrush(SubText),
			VerticalAlignment = VerticalAlignment.Center,
		});

		// Ticker text (clipped container)
		var tickerClip = new Border
		{
			Width = 120,
			Height = 16,
			Clip = new Microsoft.UI.Xaml.Media.RectangleGeometry
			{
				Rect = new Windows.Foundation.Rect(0, 0, 120, 16)
			},
		};
		// TODO: Replace _tickerText with DevWinUI MarqueeText (see comment above)
		_tickerText = new TextBlock
		{
			Text = "Track Name · Artist",
			FontSize = 11,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = new SolidColorBrush(Text),
			VerticalAlignment = VerticalAlignment.Center,
		};
		tickerClip.Child = _tickerText;
		inner.Children.Add(tickerClip);

		// Micro progress bar
		var progContainer = new Grid
		{
			Width = 60,
			Height = 2,
			VerticalAlignment = VerticalAlignment.Center,
		};
		var progTrack = new Rectangle
		{
			Fill = new SolidColorBrush(ProgressTrack),
			RadiusX = 1,
			RadiusY = 1,
			Height = 2,
			HorizontalAlignment = HorizontalAlignment.Stretch,
		};
		_progressFill = new Rectangle
		{
			Fill = new SolidColorBrush(Text),
			RadiusX = 1,
			RadiusY = 1,
			Height = 2,
			Width = 0,
			HorizontalAlignment = HorizontalAlignment.Left,
		};
		_progressWidth = 60;
		progContainer.Children.Add(progTrack);
		progContainer.Children.Add(_progressFill);
		inner.Children.Add(progContainer);

		inner.Children.Add(MakeDivider());
		inner.Children.Add(MakePrevButton());
		inner.Children.Add(MakePlayPauseButton(16));
		inner.Children.Add(MakeNextButton());

		pill.Child = inner;
		root.Children.Add(pill);
		return root;
	}

	/// <inheritdoc/>
	/// <param name="value">0.0 – 1.0 playback position.</param>
	public override void UpdateProgress(double value)
	{
		value = Math.Clamp(value, 0, 1);
		_progressFill?.Width = _progressWidth * value;
	}

	/// <param name="title">Track title shown in the ticker.</param>
	/// <param name="artist">Artist name shown in the ticker.</param>
	public void UpdateTrack(string title, string artist)
	{
		// TODO: If using DevWinUI MarqueeText, set .Text on that control instead.
		_tickerText?.Text = $"{title} · {artist}";
	}
}
