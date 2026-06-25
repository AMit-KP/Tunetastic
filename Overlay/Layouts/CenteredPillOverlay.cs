using Microsoft.UI.Xaml.Media.Imaging;

namespace Tunetastic.Overlay.Layouts;

// ══════════════════════════════════════════════════════════════════════
// CENTERED PILL
// Pill centred in the taskbar · controls left · art · title + artist
// ══════════════════════════════════════════════════════════════════════
public class CenteredPillOverlay : OverlayBase
{
	private TextBlock? _titleText;
	private TextBlock? _artistText;
	private Border? _artBox;

	public CenteredPillOverlay(OverlayTheme theme)
	{
		Theme = theme;
		RootGrid = Build();
	}

	private Grid Build()
	{
		// Root stretches full width so the pill can centre
		var root = new Grid
		{
			Height = TaskbarHeight,
			HorizontalAlignment = HorizontalAlignment.Stretch,
		};

		var pill = MakePillBorder(34, 20);
		pill.HorizontalAlignment = HorizontalAlignment.Center;
		pill.VerticalAlignment = VerticalAlignment.Center;

		var inner = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			VerticalAlignment = VerticalAlignment.Center,
			Spacing = 5,
		};

		_artBox = MakeArtBox(24, 12, AccentRose);
		inner.Children.Add(_artBox);

		inner.Children.Add(MakePrevButton(12));
		inner.Children.Add(MakePlayPauseButton(15));
		inner.Children.Add(MakeNextButton(12));
		inner.Children.Add(MakeDivider());

		var info = new StackPanel
		{
			Orientation = Orientation.Vertical,
			Spacing = 2,
			MaxWidth = 90,
			VerticalAlignment = VerticalAlignment.Center,
		};
		_titleText = MakeTitleText("As It Was");
		_artistText = MakeSubText("Harry Styles");
		info.Children.Add(_titleText);
		info.Children.Add(_artistText);
		inner.Children.Add(info);

		pill.Child = inner;
		root.Children.Add(pill);
		return root;
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
