using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace Tunetastic.Overlay.Layouts;

// ══════════════════════════════════════════════════════════════════════
// ARC RING
// Circular disc art with a PathGeometry arc progress ring around it.
// UpdateProgress() redraws the arc. No separate progress bar needed.
// ══════════════════════════════════════════════════════════════════════
public class ArcRingOverlay : OverlayBase
{
	private TextBlock? _titleText;
	private TextBlock? _artistText;
	private Border? _discArt;
	private Microsoft.UI.Xaml.Shapes.Path? _arcPath;
	private const double RingSize = 38d;
	private const double RingRadius = 17d;  // just inside the 38px disc
	private const double StrokeW = 2.5d;
	private TextBlock? _toolTipText;

	/// <summary>
	/// Initializes a new instance of the <see cref="ArcRingOverlay"/> class.
	/// </summary>
	/// <param name="theme">The theme to use for the overlay.</param>
	public ArcRingOverlay(OverlayTheme theme)
	{
		Theme = theme;
		RootGrid = Build();
	}

	/// <summary>
	/// Builds the visual structure of the arc ring overlay.
	/// </summary>
	/// <returns>A Grid representing the overlay structure.</returns>
	private Grid Build()
	{
		var root = new Grid
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Center,
		};

		var pill = MakeRectBorder(height: 45);

		var inner = new Grid
		{
			VerticalAlignment = VerticalAlignment.Center,
			ColumnDefinitions =
			{
				new ColumnDefinition { Width = GridLength.Auto },	  // 0: disc + ring
				new ColumnDefinition { Width = new GridLength(100) }, // 1: track info
				new ColumnDefinition { Width = GridLength.Auto },	  // 2: divider
				new ColumnDefinition { Width = GridLength.Auto },	  // 3: prev button
				new ColumnDefinition { Width = GridLength.Auto },	  // 4: play/pause button
				new ColumnDefinition { Width = GridLength.Auto },	  // 5: next button
			},
		};

		// ── Disc + ring (overlay them in a Grid) ──────────────────────
		var discGrid = new Grid
		{
			Margin = new Thickness(4, 0, 0, 0),
			Width = RingSize,
			Height = RingSize,
			VerticalAlignment = VerticalAlignment.Center,
		};
		Grid.SetColumn(discGrid, 0);

		// Track circle (background of the arc)
		var arcTrack = new Ellipse
		{
			Width = RingSize,
			Height = RingSize,
			Stroke = new SolidColorBrush(ProgressTrack),
			StrokeThickness = StrokeW,
			Fill = new SolidColorBrush(Colors.Transparent),
		};
		discGrid.Children.Add(arcTrack);

		// Arc fill — drawn as a Path using ArcSegment
		_arcPath = new Microsoft.UI.Xaml.Shapes.Path
		{
			Stroke = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemAccentColor"]),
			StrokeThickness = StrokeW,
			StrokeStartLineCap = PenLineCap.Round,
			StrokeEndLineCap = PenLineCap.Round,
			Fill = new SolidColorBrush(Colors.Transparent),
		};
		DrawArc(0.38); // initial 38% position
		discGrid.Children.Add(_arcPath);

		// Disc art in the centre (slightly smaller than the ring)
		_discArt = new Border
		{
			Width = RingSize - StrokeW * 3,
			Height = RingSize - StrokeW * 3,
			CornerRadius = new CornerRadius((RingSize - StrokeW * 3) / 2),
			Background = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemAccentColor"]),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
		};
		_discArt.Child = new FontIcon
		{
			Glyph = "\uEC4F",
			FontSize = 11,
			Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
		};
		discGrid.Children.Add(_discArt);

		inner.Children.Add(discGrid);

		// ── Track info ──────────────────────────────────────────────
		var info = new Grid
		{
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(6, 0, 0, 0),
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto }, // 0: title
				new RowDefinition { Height = GridLength.Auto }, // 1: artist
			},
		};
		Grid.SetColumn(info, 1);

		_titleText = MakeTitleText();
		Grid.SetRow(_titleText, 0);
		info.Children.Add(_titleText);

		_artistText = MakeSubText();
		_artistText.Margin = new Thickness(0, 2, 0, 0);
		Grid.SetRow(_artistText, 1);
		info.Children.Add(_artistText);

		inner.Children.Add(info);

		var divider = MakeDivider();
		divider.Margin = new Thickness(4, 0, 4, 0);
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
	public override void UpdateProgress(double value)
	{
		value = Math.Clamp(value, 0.001, 0.999); // avoid degenerate full-circle case
		DrawArc(value);
	}

	/// <summary>
	/// Draws an arc segment representing the progress on the ring.
	/// </summary>
	/// <param name="progress">The progress value between 0 and 1.</param>
	private void DrawArc(double progress)
	{
		if (_arcPath == null) return;

		double cx = RingSize / 2;
		double cy = RingSize / 2;
		double angle = progress * 360d;
		double rad = (angle - 90) * Math.PI / 180d; // start from top

		double startX = cx + RingRadius * Math.Cos(-Math.PI / 2);
		double startY = cy + RingRadius * Math.Sin(-Math.PI / 2);
		double endX = cx + RingRadius * Math.Cos(rad);
		double endY = cy + RingRadius * Math.Sin(rad);

		var figure = new PathFigure
		{
			StartPoint = new Windows.Foundation.Point(startX, startY),
			IsClosed = false,
		};
		figure.Segments.Add(new ArcSegment
		{
			Point = new Windows.Foundation.Point(endX, endY),
			Size = new Windows.Foundation.Size(RingRadius, RingRadius),
			IsLargeArc = angle > 180,
			SweepDirection = SweepDirection.Clockwise,
			RotationAngle = 0,
		});

		var geo = new PathGeometry();
		geo.Figures.Add(figure);
		_arcPath?.Data = geo;
	}

	/// <summary>
	/// Updates the track information displayed in the overlay.
	/// </summary>
	/// <param name="title">The track title.</param>
	/// <param name="artist">The artist name.</param>
	/// <param name="album">The album name.</param>
	/// <param name="art">The bitmap image for the album art (optional).</param>
	public void UpdateTrack(string title, string artist, string album, BitmapImage? art = null)
	{
		_titleText?.Text = title ?? string.Empty;
		_artistText?.Text = artist ?? string.Empty;
		if (art != null)
		{
			_discArt?.Background = null;
			_discArt?.Child = new Microsoft.UI.Xaml.Controls.Image
			{ Source = art, Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill };
		}

		UpdateToolTipText(title ?? string.Empty, artist ?? string.Empty, album ?? string.Empty);
	}

	/// <summary>
	/// Updates the tooltip text with track information.
	/// </summary>
	/// <param name="title">The track title.</param>
	/// <param name="artist">The artist name.</param>
	/// <param name="album">The album name.</param>
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
