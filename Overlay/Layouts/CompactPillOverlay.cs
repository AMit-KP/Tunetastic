using Microsoft.UI.Xaml.Media.Imaging;

namespace Tunetastic.Overlay.Layouts;

// ══════════════════════════════════════════════════════════════════════
// COMPACT PILL
// Rounded pill · art + title + artist · prev/play/next always visible
// ══════════════════════════════════════════════════════════════════════
public class CompactPillOverlay : OverlayBase
{
	private TextBlock? _titleText;
	private TextBlock? _artistText;
	private Border? _artBox;

	public CompactPillOverlay(OverlayTheme theme)
	{
		Theme = theme;
		RootGrid = Build();
	}

	private Grid Build()
	{
		// Root grid — same height as taskbar
		var root = new Grid
		{
			Height = TaskbarHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Center,
		};

		// Pill container
		var pill = MakePillBorder(height: 36, radius: 24);

		var inner = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			VerticalAlignment = VerticalAlignment.Center,
			Spacing = 6,
		};

		// Album art circle
		_artBox = MakeArtBox(26, 13, AccentPurple);
		inner.Children.Add(_artBox);

		// Title + artist stacked
		var info = new StackPanel
		{
			Orientation = Orientation.Vertical,
			Spacing = 2,
			MaxWidth = 100,
			VerticalAlignment = VerticalAlignment.Center,
		};
		_titleText = MakeTitleText("Neon Pulse");
		_artistText = MakeSubText("Synthwave Era");
		info.Children.Add(_titleText);
		info.Children.Add(_artistText);
		inner.Children.Add(info);

		// Divider
		inner.Children.Add(MakeDivider());

		// Controls
		inner.Children.Add(MakePrevButton());
		inner.Children.Add(MakePlayPauseButton(16));
		inner.Children.Add(MakeNextButton());

		pill.Child = inner;
		root.Children.Add(pill);
		return root;
	}

	/// <param name="title">Track title displayed in the pill.</param>
	/// <param name="artist">Artist name displayed in the pill.</param>
	/// <param name="art">Optional album art bitmap.</param>
	public void UpdateTrack(string title, string artist, BitmapImage? art = null)
	{
		_titleText?.Text = title ?? string.Empty;
		_artistText?.Text = artist ?? string.Empty;

		if (art != null)
		{
			_artBox?.Background = null;
			_artBox?.Child = new Microsoft.UI.Xaml.Controls.Image
			{
				Source = art,
				Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill
			};
		}
	}
}
