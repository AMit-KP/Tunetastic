using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;

namespace Tunetastic.Overlay.Layouts;

// ══════════════════════════════════════════════════════════════════════
// FULL ART BAR
// Art bleeds full 48px taskbar height · title + artist + progress bar
// beside it · prev/play/next controls
// ══════════════════════════════════════════════════════════════════════
public class FullArtBarOverlay : OverlayBase
{
	private TextBlock? _titleText;
	private TextBlock? _artistText;
	private Border? _artBox;
	private Rectangle? _progressFill;
	private const double ProgressBarWidth = 100;

	public FullArtBarOverlay(OverlayTheme theme)
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

		var outer = new StackPanel { Orientation = Orientation.Horizontal };

		// Full-height art (radius only on left)
		_artBox = new Border
		{
			Width = TaskbarHeight,
			Height = TaskbarHeight,
			CornerRadius = new CornerRadius(8, 0, 0, 8),
			Background = new SolidColorBrush(AccentGreen),
		};
		_artBox.Child = new FontIcon
		{
			Glyph = "\uEC4F",
			FontSize = 18,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(180, 255, 255, 255)),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
		};
		outer.Children.Add(_artBox);

		// Right body
		var body = new Border
		{
			CornerRadius = new CornerRadius(0, 8, 8, 0),
			Background = new SolidColorBrush(Surface),
			BorderBrush = new SolidColorBrush(Border),
			BorderThickness = new Thickness(0.5),
			Height = TaskbarHeight,
			Padding = new Thickness(10, 0, 10, 0),
		};

		var bodyInner = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			VerticalAlignment = VerticalAlignment.Center,
			Spacing = 8,
		};

		// Info + progress stacked
		var infoStack = new StackPanel
		{
			Orientation = Orientation.Vertical,
			Spacing = 3,
			MaxWidth = ProgressBarWidth,
			VerticalAlignment = VerticalAlignment.Center,
		};
		_titleText = MakeTitleText("Midnight Drive");
		_artistText = MakeSubText("The Glitch Mob");

		// Progress bar
		var progGrid = new Grid { Width = ProgressBarWidth, Height = 2 };
		var progTrack = new Rectangle
		{
			Fill = new SolidColorBrush(ProgressTrack),
			RadiusX = 1, RadiusY = 1,
			HorizontalAlignment = HorizontalAlignment.Stretch,
		};
		_progressFill = new Rectangle
		{
			Fill = new SolidColorBrush(AccentGreen),
			RadiusX = 1, RadiusY = 1,
			Width = 35,
			HorizontalAlignment = HorizontalAlignment.Left,
		};
		progGrid.Children.Add(progTrack);
		progGrid.Children.Add(_progressFill);

		infoStack.Children.Add(_titleText);
		infoStack.Children.Add(_artistText);
		infoStack.Children.Add(progGrid);
		bodyInner.Children.Add(infoStack);

		bodyInner.Children.Add(MakeDivider());
		bodyInner.Children.Add(MakePrevButton());
		bodyInner.Children.Add(MakePlayPauseButton(16));
		bodyInner.Children.Add(MakeNextButton());

		body.Child = bodyInner;
		outer.Children.Add(body);
		root.Children.Add(outer);
		return root;
	}

	public override void UpdateProgress(double value)
	{
		value = Math.Clamp(value, 0, 1);
		_progressFill?.Width = ProgressBarWidth * value;
	}

	public void UpdateTrack(string title, string artist, BitmapImage? art = null)
	{
		_titleText?.Text = title ?? string.Empty;
		_artistText?.Text = artist ?? string.Empty;
		if (art != null)
		{
			_artBox?.Background = null;
			_artBox?.Child = new Microsoft.UI.Xaml.Controls.Image
			{ Source = art, Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill };
		}
	}
}

