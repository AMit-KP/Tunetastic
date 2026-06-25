using Microsoft.UI.Xaml.Media.Imaging;

namespace Tunetastic.Overlay.Layouts;

// ══════════════════════════════════════════════════════════════════════
// LEFT PILL
// Left-anchored pill · circular art · title · artist · always-on controls
// ══════════════════════════════════════════════════════════════════════
public class LeftPillOverlay : OverlayBase
{
	private TextBlock? _titleText;
	private TextBlock? _artistText;
	private Border? _artBox;

	public LeftPillOverlay(OverlayTheme theme)
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

		var pill = MakePillBorder(36, 24);

		var inner = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			VerticalAlignment = VerticalAlignment.Center,
			Spacing = 6,
		};

		_artBox = MakeArtBox(26, 13, AccentPurple);
		inner.Children.Add(_artBox);

		var info = new StackPanel { Orientation = Orientation.Vertical, Spacing = 2, MaxWidth = 90 };
		_titleText = MakeTitleText("Neon Pulse");
		_artistText = MakeSubText("Synthwave Era");
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
