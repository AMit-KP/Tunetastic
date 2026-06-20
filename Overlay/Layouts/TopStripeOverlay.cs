using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;

namespace Tunetastic.Overlay.Layouts;

// ══════════════════════════════════════════════════════════════════════
// TOP STRIPE
// 2px progress stripe along the TOP edge · art + title + artist + controls
// ══════════════════════════════════════════════════════════════════════
public class TopStripeOverlay : OverlayBase
{
	private TextBlock? _titleText;
	private TextBlock? _artistText;
	private Border? _artBox;
	private Rectangle? _stripeFill;
	private Grid? _stripeGrid;
	private double _stripeContainerWidth = 0; // resolved at layout time

	public TopStripeOverlay(OverlayTheme theme)
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

		// Outer border with no padding (stripe bleeds to edge)
		var outer = new Border
		{
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(Surface),
			BorderBrush = new SolidColorBrush(Border),
			BorderThickness = new Thickness(0.5),
			Height = TaskbarHeight,
		};

		var rootStack = new Grid();
		rootStack.RowDefinitions.Add(new RowDefinition { Height = new GridLength(2) });
		rootStack.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

		// Stripe row
		_stripeGrid = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
		var stripeTrack = new Rectangle
		{
			Fill = new SolidColorBrush(ProgressTrack),
			RadiusX = 2, RadiusY = 2,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			Height = 2,
		};
		_stripeFill = new Rectangle
		{
			Fill = new SolidColorBrush(AccentPurple),
			RadiusX = 2, RadiusY = 2,
			Width = 0,
			Height = 2,
			HorizontalAlignment = HorizontalAlignment.Left,
		};
		_stripeGrid.Children.Add(stripeTrack);
		_stripeGrid.Children.Add(_stripeFill);
		Grid.SetRow(_stripeGrid, 0);

		// Register for size change so we can compute stripe fill width
		_stripeGrid.SizeChanged += (s, e) =>
		{
			_stripeContainerWidth = e.NewSize.Width;
		};

		// Content row
		var content = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			VerticalAlignment = VerticalAlignment.Center,
			Spacing = 6,
			Padding = new Thickness(6, 0, 10, 0),
		};
		_artBox = MakeArtBox(26, 6, AccentPurple);
		content.Children.Add(_artBox);

		var info = new StackPanel { Orientation = Orientation.Vertical, Spacing = 2, MaxWidth = 90 };
		_titleText = MakeTitleText("Neon Pulse");
		_artistText = MakeSubText("Synthwave Era");
		info.Children.Add(_titleText);
		info.Children.Add(_artistText);
		content.Children.Add(info);

		content.Children.Add(MakeDivider());
		content.Children.Add(MakePrevButton());
		content.Children.Add(MakePlayPauseButton(16));
		content.Children.Add(MakeNextButton());

		Grid.SetRow(content, 1);

		rootStack.Children.Add(_stripeGrid);
		rootStack.Children.Add(content);
		outer.Child = rootStack;
		root.Children.Add(outer);
		return root;
	}

	public override void UpdateProgress(double value)
	{
		value = Math.Clamp(value, 0, 1);
		_stripeFill?.Width = _stripeContainerWidth * value;
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
