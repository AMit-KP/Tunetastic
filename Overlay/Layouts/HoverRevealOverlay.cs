using Microsoft.UI.Xaml.Media.Imaging;

namespace Tunetastic.Overlay.Layouts;
// ══════════════════════════════════════════════════════════════════════
// HOVER REVEAL
// Track info at rest · controls fade in on pointer enter, out on leave
// ══════════════════════════════════════════════════════════════════════
public class HoverRevealOverlay : OverlayBase
{
	private TextBlock? _titleText;
	private TextBlock? _artistText;
	private Border? _artBox;
	private StackPanel? _controlsPanel;

	public HoverRevealOverlay(OverlayTheme theme)
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

		var pill = MakeRectBorder(height: 36, radius: 8);

		var inner = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			VerticalAlignment = VerticalAlignment.Center,
			Spacing = 6,
		};

		// Circular art (spin-disc feel)
		_artBox = MakeArtBox(26, 13, AccentGreen);
		inner.Children.Add(_artBox);

		// Info
		var info = new StackPanel
		{
			Orientation = Orientation.Vertical,
			Spacing = 2,
			MaxWidth = 110,
			VerticalAlignment = VerticalAlignment.Center,
		};
		_titleText = MakeTitleText("Midnight Drive");
		_artistText = MakeSubText("The Glitch Mob");
		info.Children.Add(_titleText);
		info.Children.Add(_artistText);
		inner.Children.Add(info);

		// Controls panel — hidden at rest
		_controlsPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 2,
			Opacity = 0,   // hidden by default
			VerticalAlignment = VerticalAlignment.Center,
		};
		_controlsPanel.Children.Add(MakeDivider());
		_controlsPanel.Children.Add(MakePrevButton());
		_controlsPanel.Children.Add(MakePlayPauseButton(16));
		_controlsPanel.Children.Add(MakeNextButton());
		inner.Children.Add(_controlsPanel);

		pill.Child = inner;
		root.Children.Add(pill);

		// Hover events on the pill
		pill.PointerEntered += (_, _) => FadeIn(_controlsPanel, 150);
		pill.PointerExited += (_, _) => FadeOut(_controlsPanel, 150);

		return root;
	}

	/// <param name="title">Track title.</param>
	/// <param name="artist">Artist name.</param>
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
