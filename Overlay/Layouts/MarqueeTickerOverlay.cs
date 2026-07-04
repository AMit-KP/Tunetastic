using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Tunetastic.Overlay.Layouts;

// ══════════════════════════════════════════════════════════════════════
// MARQUEE TICKER 
// Music-note icon · scrolling track+artist+album ticker · micro progress bar
// prev/play/next controls
// ══════════════════════════════════════════════════════════════════════
public class MarqueeTickerOverlay : OverlayBase
{
	private TextBlock? _tickerText;
	private Rectangle? _progressFill;
	private TextBlock? _toolTipText;
	private double _progressWidth = 40;

	public MarqueeTickerOverlay(OverlayTheme theme)
	{
		Theme = theme;
		RootGrid = Build();
	}

	private Grid Build()
	{
		var root = new Grid
		{
			HorizontalAlignment = HorizontalAlignment.Left,
		};

		var pill = MakeRectBorder();

		var inner = new Grid
		{
			VerticalAlignment = VerticalAlignment.Center,
			ColumnDefinitions =
			{
				new ColumnDefinition { Width = GridLength.Auto }, // 1: ticker clip
				new ColumnDefinition { Width = GridLength.Auto }, // 2: progress bar
				new ColumnDefinition { Width = GridLength.Auto }, // 3: divider
				new ColumnDefinition { Width = GridLength.Auto }, // 4: prev button
				new ColumnDefinition { Width = GridLength.Auto }, // 5: play/pause button
				new ColumnDefinition { Width = GridLength.Auto }, // 6: next button
			},
		};

		// Ticker text (clipped container)
		var tickerClip = new Border
		{
			Width = 120,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(4, 0, 4, 0),
			//Clip = new Microsoft.UI.Xaml.Media.RectangleGeometry
			//{
			//	Rect = new Windows.Foundation.Rect(0, 0, 120, 16)
			//},
		};

		var marquee = new AutoScrollView
		{
			IsPlaying = true,
			VerticalAlignment = VerticalAlignment.Center
		};

		_tickerText = new TextBlock
		{
			Text = "Track Name · Artist",
			FontSize = 13,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = new SolidColorBrush(Text),
			VerticalAlignment = VerticalAlignment.Center,
		};

		marquee.Child = _tickerText;
		tickerClip.Child = marquee;
		Grid.SetColumn(tickerClip, 0);
		inner.Children.Add(tickerClip);

		// Micro progress bar
		var progContainer = new Grid
		{
			Width = _progressWidth,
			Height = 3,
			Margin = new Thickness(4, 0, 8, 0),
			VerticalAlignment = VerticalAlignment.Center,
		};
		var progTrack = new Rectangle
		{
			Fill = new SolidColorBrush(ProgressTrack),
			RadiusX = 1,
			RadiusY = 1,
			Height = 3,
			HorizontalAlignment = HorizontalAlignment.Stretch,
		};
		_progressFill = new Rectangle
		{
			Fill = new SolidColorBrush(Text),
			RadiusX = 1,
			RadiusY = 1,
			Height = 3,
			Width = 0,
			HorizontalAlignment = HorizontalAlignment.Left,
		};
		progContainer.Children.Add(progTrack);
		progContainer.Children.Add(_progressFill);
		Grid.SetColumn(progContainer, 1);
		inner.Children.Add(progContainer);

		var divider = MakeDivider();
		divider.Margin = new Thickness(0, 0, 4, 0);
		Grid.SetColumn(divider, 2);
		inner.Children.Add(divider);

		var prevButton = MakePrevButton();
		Grid.SetColumn(prevButton, 3);
		inner.Children.Add(prevButton);

		var playPauseButton = MakePlayPauseButton(16);
		Grid.SetColumn(playPauseButton, 4);
		inner.Children.Add(playPauseButton);

		var nextButton = MakeNextButton();
		Grid.SetColumn(nextButton, 5);
		inner.Children.Add(nextButton);

		pill.Child = inner;
		root.Children.Add(pill);

		_toolTipText = new TextBlock
		{
			TextWrapping = TextWrapping.WrapWholeWords,
			TextTrimming = TextTrimming.CharacterEllipsis
		};

		ToolTipService.SetToolTip(root, _toolTipText);

		UpdateToolTipText();
		root.PointerPressed += (s, e) => MainWindow._instance.RestoreFromTrayOrTaskbar();

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
	public void UpdateTrack(string title, string artist, string album)
	{
		_tickerText?.Text = $"{title} • {artist} • {album}";

		UpdateToolTipText(title ?? string.Empty, artist ?? string.Empty, album ?? string.Empty);
	}

	private void UpdateToolTipText(string title = "Song/Track Title", string artist = "Artists", string album = "Album")
	{
		if (_toolTipText is null) return;

		_toolTipText.Inlines.Clear();
		_toolTipText.Inlines.Add(new Run { Text = "Title: ", FontWeight = Microsoft.UI.Text.FontWeights.Bold });
		_toolTipText.Inlines.Add(new Run { Text = title, FontStyle = Windows.UI.Text.FontStyle.Italic });
		_toolTipText.Inlines.Add(new LineBreak());
		_toolTipText.Inlines.Add(new LineBreak());

		_toolTipText.Inlines.Add(new Run { Text = "Artists: ", FontWeight = Microsoft.UI.Text.FontWeights.Bold });
		_toolTipText.Inlines.Add(new Run { Text = artist, FontStyle = Windows.UI.Text.FontStyle.Italic });
		_toolTipText.Inlines.Add(new LineBreak());
		_toolTipText.Inlines.Add(new LineBreak());

		_toolTipText.Inlines.Add(new Run { Text = "Album: ", FontWeight = Microsoft.UI.Text.FontWeights.Bold });
		_toolTipText.Inlines.Add(new Run { Text = album, FontStyle = Windows.UI.Text.FontStyle.Italic });
	}
}
