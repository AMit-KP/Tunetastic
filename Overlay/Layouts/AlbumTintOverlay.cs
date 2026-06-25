using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;

namespace Tunetastic.Overlay.Layouts;

// ══════════════════════════════════════════════════════════════════════
// ALBUM TINT
// Background and border tint adapts to the album's dominant colour.
// Pass dominantColor from your art palette extractor.
// ══════════════════════════════════════════════════════════════════════
public class AlbumTintOverlay : OverlayBase
{
	private TextBlock? _titleText;
	private TextBlock? _artistText;
	private Border? _artBox;
	private Border? _pill;

	public AlbumTintOverlay(OverlayTheme theme)
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

		// Default tint — purple
		var tintBg = Color.FromArgb(45, 127, 119, 221);
		var tintBorder = Color.FromArgb(90, 127, 119, 221);

		_pill = new Border
		{
			Height = 36,
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(tintBg),
			BorderBrush = new SolidColorBrush(tintBorder),
			BorderThickness = new Thickness(0.5),
			VerticalAlignment = VerticalAlignment.Center,
			Padding = new Thickness(6, 0, 10, 0),
		};

		var inner = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			VerticalAlignment = VerticalAlignment.Center,
			Spacing = 6,
		};

		_artBox = MakeArtBox(26, 5, AccentPurple);
		inner.Children.Add(_artBox);

		var info = new StackPanel { Orientation = Orientation.Vertical, Spacing = 2, MaxWidth = 90 };
		_titleText = MakeTitleText("Blinding Lights");
		_artistText = MakeSubText("The Weeknd");
		info.Children.Add(_titleText);
		info.Children.Add(_artistText);
		inner.Children.Add(info);

		inner.Children.Add(MakeDivider());
		inner.Children.Add(MakePrevButton());
		inner.Children.Add(MakePlayPauseButton(16));
		inner.Children.Add(MakeNextButton());

		_pill.Child = inner;
		root.Children.Add(_pill);
		return root;
	}

	/// <param name="dominantColor">The album art's dominant colour. Used for background and border tint.</param>
	public void UpdateTrack(string title, string artist, Color dominantColor, BitmapImage? art = null)
	{
		_titleText?.Text = title ?? string.Empty;
		_artistText?.Text = artist ?? string.Empty;

		_pill?.Background = new SolidColorBrush(Color.FromArgb(45,
			dominantColor.R, dominantColor.G, dominantColor.B));
		_pill?.BorderBrush = new SolidColorBrush(Color.FromArgb(90,
			dominantColor.R, dominantColor.G, dominantColor.B));
		_artBox?.Background = new SolidColorBrush(dominantColor);

		if (art != null)
		{
			_artBox?.Background = null;
			_artBox?.Child = new Microsoft.UI.Xaml.Controls.Image
			{ Source = art, Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill };
		}
	}
}
