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

	public ArcRingOverlay(OverlayTheme theme)
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
			VerticalAlignment = VerticalAlignment.Center,
		};

		var pill = MakeRectBorder(44, 8);

		var inner = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			VerticalAlignment = VerticalAlignment.Center,
			Spacing = 8,
		};

		// ── Disc + ring (overlay them in a Grid) ──────────────────────
		var discGrid = new Grid
		{
			Width = RingSize,
			Height = RingSize,
			VerticalAlignment = VerticalAlignment.Center,
		};

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
			Stroke = new SolidColorBrush(AccentGold),
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
			Background = new SolidColorBrush(AccentGold),
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
		var info = new StackPanel { Orientation = Orientation.Vertical, Spacing = 2, MaxWidth = 100 };
		_titleText = MakeTitleText("Save Your Tears");
		_artistText = MakeSubText("The Weeknd");
		info.Children.Add(_titleText);
		info.Children.Add(_artistText);
		inner.Children.Add(info);

		inner.Children.Add(MakeDivider());
		inner.Children.Add(MakePrevButton());
		inner.Children.Add(MakePlayPauseButton(16));
		inner.Children.Add(MakeNextButton());

		pill.Child = inner;
		root.Children.Add(pill);
		return root;
	}

	/// <summary>Redraws the arc for the given progress value (0.0–1.0).</summary>
	public override void UpdateProgress(double value)
	{
		value = Math.Clamp(value, 0.001, 0.999); // avoid degenerate full-circle case
		DrawArc(value);
	}

	private void DrawArc(double progress)
	{
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

	public void UpdateTrack(string title, string artist, BitmapImage? art = null)
	{
		_titleText?.Text = title ?? string.Empty;
		_artistText?.Text = artist ?? string.Empty;
		if (art != null)
		{
			_discArt?.Background = null;
			_discArt?.Child = new Microsoft.UI.Xaml.Controls.Image
			{ Source = art, Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill };
		}
	}
}
