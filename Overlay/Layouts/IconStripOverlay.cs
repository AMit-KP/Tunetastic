using Microsoft.UI.Xaml.Media.Imaging;

namespace Tunetastic.Overlay.Layouts;

// ══════════════════════════════════════════════════════════════════════
// ICON STRIP
// Zero text · art thumbnail · prev / play / next / like / volume
// ══════════════════════════════════════════════════════════════════════
public class IconStripOverlay : OverlayBase
{
	private Border? _artBox;

	public IconStripOverlay(OverlayTheme theme)
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

		var pill = MakeRectBorder(36, 8);

		var inner = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			VerticalAlignment = VerticalAlignment.Center,
			Spacing = 2,
		};

		_artBox = MakeArtBox(28, 5, AccentBlue);
		inner.Children.Add(_artBox);

		inner.Children.Add(MakePrevButton(13));
		inner.Children.Add(MakePlayPauseButton(16));
		inner.Children.Add(MakeNextButton(13));

		pill.Child = inner;
		root.Children.Add(pill);
		return root;
	}

	/// <param name="art">Album art bitmap. No text fields in this layout.</param>
	public void UpdateTrack(BitmapImage? art = null)
	{
		if (art != null)
		{
			_artBox?.Background = null;
			_artBox?.Child = new Microsoft.UI.Xaml.Controls.Image
			{ Source = art, Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill };
		}
	}
}
