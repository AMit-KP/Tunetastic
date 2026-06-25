using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;

namespace Tunetastic.Overlay.Layouts;

// ══════════════════════════════════════════════════════════════════════
// ARTIST BADGE
// Artist name rendered as a tinted pill-badge below the track title.
// Badge colour adapts to the accent colour you pass.
// ══════════════════════════════════════════════════════════════════════
public class ArtistBadgeOverlay : OverlayBase
{
	private TextBlock? _titleText;
	private TextBlock? _artistBadgeText;
	private Border? _artistBadge;
	private Border? _artBox;

	public ArtistBadgeOverlay(OverlayTheme theme)
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
			Spacing = 7,
		};

		_artBox = MakeArtBox(30, 6, AccentRose);
		inner.Children.Add(_artBox);

		// Title + badge stacked
		var infoStack = new StackPanel
		{
			Orientation = Orientation.Vertical,
			Spacing = 4,
			VerticalAlignment = VerticalAlignment.Center,
		};

		_titleText = MakeTitleText("Levitating");
		_titleText.MaxWidth = 100;

		// Artist badge pill
		_artistBadge = new Border
		{
			CornerRadius = new CornerRadius(20),
			Background = new SolidColorBrush(Color.FromArgb(76, 153, 53, 86)),
			BorderBrush = new SolidColorBrush(Color.FromArgb(100, 212, 83, 126)),
			BorderThickness = new Thickness(0.5),
			Padding = new Thickness(7, 2, 7, 2),
		};
		_artistBadgeText = new TextBlock
		{
			Text = "Dua Lipa",
			FontSize = 9,
			Foreground = new SolidColorBrush(Color.FromArgb(230, 237, 147, 177)),
		};
		_artistBadge.Child = _artistBadgeText;

		infoStack.Children.Add(_titleText);
		infoStack.Children.Add(_artistBadge);
		inner.Children.Add(infoStack);

		inner.Children.Add(MakeDivider());
		inner.Children.Add(MakePrevButton());
		inner.Children.Add(MakePlayPauseButton(16));
		inner.Children.Add(MakeNextButton());

		pill.Child = inner;
		root.Children.Add(pill);
		return root;
	}

	/// <param name="badgeColor">Dominant colour used for the artist badge tint.</param>
	public void UpdateTrack(string title, string artist, Color badgeColor, BitmapImage? art = null)
	{
		_titleText?.Text = title ?? string.Empty;
		_artistBadgeText?.Text = artist ?? string.Empty;

		byte r = badgeColor.R, g = badgeColor.G, b = badgeColor.B;
		_artistBadge?.Background = new SolidColorBrush(Color.FromArgb(76, r, g, b));
		_artistBadge?.BorderBrush = new SolidColorBrush(Color.FromArgb(110, r, g, b));
		_artistBadgeText?.Foreground = new SolidColorBrush(
			Color.FromArgb(230, (byte)Math.Min(r + 80, 255),
								(byte)Math.Min(g + 60, 255),
								(byte)Math.Min(b + 80, 255)));

		_artBox?.Background = new SolidColorBrush(badgeColor);
		if (art != null)
		{
			_artBox?.Background = null;
			_artBox?.Child = new Microsoft.UI.Xaml.Controls.Image
			{ Source = art, Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill };
		}
	}
}
