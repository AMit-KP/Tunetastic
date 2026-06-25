using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;

namespace Tunetastic.Overlay.Layouts;

// ══════════════════════════════════════════════════════════════════════
// STACKED INFO
// Two-row block: title / artist · timestamp · thin inline progress bar
// ══════════════════════════════════════════════════════════════════════
public class StackedInfoOverlay : OverlayBase
{
	private TextBlock? _titleText;
	private TextBlock? _artistText;
	private TextBlock? _timestampText;
	private Border? _artBox;
	private Rectangle? _progressFill;
	private const double ProgressWidth = 90;

	public StackedInfoOverlay(OverlayTheme theme)
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

		var pill = MakeRectBorder(42, 8);

		var inner = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			VerticalAlignment = VerticalAlignment.Center,
			Spacing = 7,
		};

		_artBox = MakeArtBox(30, 6, AccentGold);
		inner.Children.Add(_artBox);

		// Stacked info block
		var infoStack = new StackPanel
		{
			Orientation = Orientation.Vertical,
			Spacing = 2,
			VerticalAlignment = VerticalAlignment.Center,
		};
		_titleText = MakeTitleText("Save Your Tears");
		_titleText.MaxWidth = 100;

		// Artist + timestamp on same row
		var sub = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
		_artistText = MakeSubText("The Weeknd");
		_timestampText = new TextBlock
		{
			Text = "· 2:14 / 3:35",
			FontSize = 9,
			Foreground = new SolidColorBrush(SubText),
			VerticalAlignment = VerticalAlignment.Center,
		};
		sub.Children.Add(_artistText);
		sub.Children.Add(_timestampText);

		// Progress bar
		var progGrid = new Grid { Width = ProgressWidth, Height = 2 };
		var progTrack = new Rectangle
		{
			Fill = new SolidColorBrush(ProgressTrack),
			RadiusX = 1, RadiusY = 1,
			HorizontalAlignment = HorizontalAlignment.Stretch,
		};
		_progressFill = new Rectangle
		{
			Fill = new SolidColorBrush(AccentGold),
			RadiusX = 1, RadiusY = 1,
			Width = ProgressWidth * 0.60,
			HorizontalAlignment = HorizontalAlignment.Left,
		};
		progGrid.Children.Add(progTrack);
		progGrid.Children.Add(_progressFill);

		infoStack.Children.Add(_titleText);
		infoStack.Children.Add(sub);
		infoStack.Children.Add(progGrid);
		inner.Children.Add(infoStack);

		inner.Children.Add(MakeDivider());
		inner.Children.Add(MakePrevButton());
		inner.Children.Add(MakePlayPauseButton(16));
		inner.Children.Add(MakeNextButton());

		pill.Child = inner;
		root.Children.Add(pill);
		return root;
	}

	public override void UpdateProgress(double value)
	{
		value = Math.Clamp(value, 0, 1);
		_progressFill?.Width = ProgressWidth * value;
	}

	/// <param name="timestamp">e.g. "· 2:14 / 3:35"</param>
	public void UpdateTrack(string title, string artist, string timestamp = "", BitmapImage? art = null)
	{
		_titleText?.Text = title ?? string.Empty;
		_artistText?.Text = artist ?? string.Empty;
		_timestampText?.Text = timestamp ?? string.Empty;
		if (art != null)
		{
			_artBox?.Background = null;
			_artBox?.Child = new Microsoft.UI.Xaml.Controls.Image
			{ Source = art, Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill };
		}
	}
}
