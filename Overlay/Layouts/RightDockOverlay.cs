using Microsoft.UI.Xaml.Media.Imaging;

namespace Tunetastic.Overlay.Layouts;

// ══════════════════════════════════════════════════════════════════════
// RIGHT DOCK
// Right-anchored · controls on the LEFT of the art+info block
// ══════════════════════════════════════════════════════════════════════
public class RightDockOverlay : OverlayBase
{
	private TextBlock? _titleText;
	private TextBlock? _artistText;
	private Border? _artBox;

	public RightDockOverlay(OverlayTheme theme)
	{
		Theme = theme;
		RootGrid = Build();
	}

	private Grid Build()
	{
		var root = new Grid
		{
			Height = TaskbarHeight,
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Center,
		};

		var pill = MakeRectBorder(36, 8);

		var inner = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			VerticalAlignment = VerticalAlignment.Center,
			Spacing = 5,
		};

		// Controls first
		inner.Children.Add(MakePrevButton());
		inner.Children.Add(MakePlayPauseButton(16));
		inner.Children.Add(MakeNextButton());
		inner.Children.Add(MakeDivider());

		// Art + info
		_artBox = MakeArtBox(26, 6, AccentOrange);
		inner.Children.Add(_artBox);

		var info = new StackPanel { Orientation = Orientation.Vertical, Spacing = 2, MaxWidth = 90 };
		_titleText = MakeTitleText("Ghost Town");
		_artistText = MakeSubText("Kanye West");
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
