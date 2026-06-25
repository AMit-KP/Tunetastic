using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace Tunetastic.Overlay.Layouts;

// ══════════════════════════════════════════════════════════════════════
// ACCENT EDGE
// 3px coloured vertical bar on left edge · art + info + controls
// ══════════════════════════════════════════════════════════════════════
public class AccentEdgeOverlay : OverlayBase
{
	private TextBlock? _titleText;
	private TextBlock? _artistText;
	private Border? _artBox;
	private Rectangle? _accentBar;

	public AccentEdgeOverlay(OverlayTheme theme)
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

		var outer = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			VerticalAlignment = VerticalAlignment.Center,
			Spacing = 0,
		};

		// Accent bar
		_accentBar = new Rectangle
		{
			Width = 3,
			Height = 36,
			RadiusX = 1.5,
			RadiusY = 1.5,
			Fill = new SolidColorBrush(AccentOrange),
			VerticalAlignment = VerticalAlignment.Center,
		};
		outer.Children.Add(_accentBar);

		// Body — no left radius (joins the accent bar)
		var body = new Border
		{
			CornerRadius = new CornerRadius(0, 8, 8, 0),
			Background = new SolidColorBrush(Surface),
			BorderBrush = new SolidColorBrush(Border),
			BorderThickness = new Thickness(0, 0.5, 0.5, 0.5),
			Height = 36,
			Padding = new Thickness(8, 0, 10, 0),
		};

		var inner = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			VerticalAlignment = VerticalAlignment.Center,
			Spacing = 6,
		};

		_artBox = MakeArtBox(26, 6, AccentOrange);
		inner.Children.Add(_artBox);

		var info = new StackPanel { Orientation = Orientation.Vertical, Spacing = 2, MaxWidth = 90 };
		_titleText = MakeTitleText("Levitating");
		_artistText = MakeSubText("Dua Lipa");
		info.Children.Add(_titleText);
		info.Children.Add(_artistText);
		inner.Children.Add(info);

		inner.Children.Add(MakeDivider());
		inner.Children.Add(MakePrevButton());
		inner.Children.Add(MakePlayPauseButton(16));
		inner.Children.Add(MakeNextButton());

		body.Child = inner;
		outer.Children.Add(body);
		root.Children.Add(outer);
		return root;
	}

	/// <param name="accentColor">Sets the left accent bar colour — use your album dominant colour.</param>
	public void UpdateTrack(string title, string artist, Color accentColor, BitmapImage? art = null)
	{
		_titleText?.Text = title ?? string.Empty;
		_artistText?.Text = artist ?? string.Empty;
		_accentBar?.Fill = new SolidColorBrush(accentColor);
		_artBox?.Background = new SolidColorBrush(accentColor);

		if (art != null)
		{
			_artBox?.Background = null;
			_artBox?.Child = new Microsoft.UI.Xaml.Controls.Image
			{ Source = art, Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill };
		}
	}
}
