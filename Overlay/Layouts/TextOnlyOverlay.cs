using Microsoft.UI.Xaml.Media;

namespace Tunetastic.Overlay.Layouts;

// ══════════════════════════════════════════════════════════════════════
// TEXT ONLY
// Zero art or icons · track name · UPPERCASE artist label · timestamp
// prev/play/next controls
// ══════════════════════════════════════════════════════════════════════
public class TextOnlyOverlay : OverlayBase
{
	private TextBlock? _titleText;
	private TextBlock? _artistText;
	private TextBlock? _timestampText;

	public TextOnlyOverlay(OverlayTheme theme)
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
			Spacing = 8,
		};

		// Track + artist stacked
		var infoStack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 2 };
		_titleText = new TextBlock
		{
			Text = "Ghost Town",
			FontSize = 12,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = new SolidColorBrush(Text),
			VerticalAlignment = VerticalAlignment.Center,
		};
		_artistText = new TextBlock
		{
			Text = "KANYE WEST",
			FontSize = 8,
			CharacterSpacing = 60,  // letter-spacing via CharacterSpacing (1/1000 em units)
			Foreground = new SolidColorBrush(SubText),
			VerticalAlignment = VerticalAlignment.Center,
		};
		infoStack.Children.Add(_titleText);
		infoStack.Children.Add(_artistText);
		inner.Children.Add(infoStack);

		inner.Children.Add(MakeDivider());

		// Timestamp
		_timestampText = new TextBlock
		{
			Text = "2:14 / 4:03",
			FontSize = 9,
			Foreground = new SolidColorBrush(SubText),
			VerticalAlignment = VerticalAlignment.Center,
		};
		inner.Children.Add(_timestampText);

		inner.Children.Add(MakeDivider());
		inner.Children.Add(MakePrevButton());
		inner.Children.Add(MakePlayPauseButton(16));
		inner.Children.Add(MakeNextButton());

		pill.Child = inner;
		root.Children.Add(pill);
		return root;
	}

	/// <param name="artistUppercase">Pass artist name — it will display as-is. Uppercase it before passing if desired.</param>
	/// <param name="timestamp">e.g. "2:14 / 4:03"</param>
	public void UpdateTrack(string title, string artist, string timestamp = "")
	{
		_titleText?.Text = title ?? string.Empty;
		_artistText?.Text = (artist ?? string.Empty).ToUpper();
		_timestampText?.Text = timestamp ?? string.Empty;
	}
}
