using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;

namespace Tunetastic.Overlay.Layouts;

// ══════════════════════════════════════════════════════════════════════
// FULL ART BAR
// Art bleeds full 48px taskbar height · title + artist + progress bar
// beside it · prev/play/next controls
// ══════════════════════════════════════════════════════════════════════
public class FullArtBarOverlay : OverlayBase
{
	private TextBlock? _titleText;
	private TextBlock? _artistText;
	private Border? _artBox;
	private Rectangle? _progressFill;
	private TextBlock? _toolTipText;
	private const double ProgressBarWidth = 100;

	public FullArtBarOverlay(OverlayTheme theme)
	{
		Theme = theme;
		RootGrid = Build();
	}

	private Grid Build()
	{
		var root = new Grid
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Center,
		};

		// Outer grid: col 0 = art, col 1 = body
		var outer = new Grid
		{
			Height = 48,
			//Width = 250,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Center
		};
		outer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		outer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		// Full-height art (radius only on left)
		_artBox = new Border
		{
			VerticalAlignment = VerticalAlignment.Center,
			Width = 48,
			CornerRadius = new CornerRadius(8, 0, 0, 8),
			Background = new SolidColorBrush(AccentGreen),
		};

		Grid.SetColumn(_artBox, 0);
		outer.Children.Add(_artBox);

		// Right body
		var body = new Border
		{
			CornerRadius = new CornerRadius(0, 8, 8, 0),
			VerticalAlignment = VerticalAlignment.Center,
			Height = 48,
			Background = new SolidColorBrush(Surface),
			BorderBrush = new SolidColorBrush(Border),
			BorderThickness = new Thickness(0.5, 0, 0, 0),
			Padding = new Thickness(10, 0, 10, 0),
		};

		// Body inner grid: col 0 = info+progress, col 1 = divider, col 2 = prev, col 3 = play/pause, col 4 = next
		var bodyInner = new Grid
		{
			VerticalAlignment = VerticalAlignment.Center,
		};
		bodyInner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // info stack
		bodyInner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3) }); // spacer
		bodyInner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // divider
		bodyInner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3) }); // spacer
		bodyInner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // prev
		bodyInner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // play/pause
		bodyInner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // next

		// Info + progress stacked: row 0 = title, row 1 = artist, row 2 = progress bar
		var infoStack = new Grid
		{
			MaxWidth = ProgressBarWidth,
			VerticalAlignment = VerticalAlignment.Center,
		};
		infoStack.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // title
		infoStack.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // artist
		infoStack.RowDefinitions.Add(new RowDefinition { Height = new GridLength(3) }); // spacer
		infoStack.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // progress

		_titleText = MakeTitleText();
		Grid.SetRow(_titleText, 0);
		infoStack.Children.Add(_titleText);

		_artistText = MakeSubText();
		Grid.SetRow(_artistText, 1);
		infoStack.Children.Add(_artistText);

		// Progress bar
		var progGrid = new Grid { Width = ProgressBarWidth, Height = 3 };
		var progTrack = new Rectangle
		{
			Fill = new SolidColorBrush(ProgressTrack),
			RadiusX = 1, RadiusY = 1,
			HorizontalAlignment = HorizontalAlignment.Stretch,
		};
		_progressFill = new Rectangle
		{
			Fill = new SolidColorBrush(Theme == OverlayTheme.Dark ? AccentGreen : NeonGreen),
			RadiusX = 1, RadiusY = 1,
			Width = 35,
			HorizontalAlignment = HorizontalAlignment.Left,
		};
		progGrid.Children.Add(progTrack);
		progGrid.Children.Add(_progressFill);

		Grid.SetRow(progGrid, 3);
		infoStack.Children.Add(progGrid);

		Grid.SetColumn(infoStack, 0);
		bodyInner.Children.Add(infoStack);

		var divider = MakeDivider();
		Grid.SetColumn(divider, 2);
		bodyInner.Children.Add(divider);

		var prevButton = MakePrevButton();
		Grid.SetColumn(prevButton, 4);
		bodyInner.Children.Add(prevButton);

		var playPauseButton = MakePlayPauseButton(16);
		Grid.SetColumn(playPauseButton, 5);
		bodyInner.Children.Add(playPauseButton);

		var nextButton = MakeNextButton();
		Grid.SetColumn(nextButton, 6);
		bodyInner.Children.Add(nextButton);

		body.Child = bodyInner;
		Grid.SetColumn(body, 1);
		outer.Children.Add(body);

		root.Children.Add(outer);

		_toolTipText = new TextBlock
		{
			TextWrapping = TextWrapping.WrapWholeWords,
			TextTrimming = TextTrimming.CharacterEllipsis
		};

		ToolTipService.SetToolTip(root, _toolTipText);

		UpdateToolTipText();
		root.PointerPressed += (s, e) => MainWindow._instance.RestoreFromTrayOrTaskbar();

		return root;
	}

	public override void UpdateProgress(double value)
	{
		value = Math.Clamp(value, 0, 1);
		_progressFill?.Width = ProgressBarWidth * value;
	}

	public void UpdateTrack(string title, string artist, string album, BitmapImage? art = null)
	{
		_titleText?.Text = title ?? string.Empty;
		_artistText?.Text = artist ?? string.Empty;
		if (art != null)
		{
			_artBox?.Background = null;
			_artBox?.Child = new Microsoft.UI.Xaml.Controls.Image
			{ Source = art, Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill };
		}

		UpdateToolTipText(title ?? string.Empty, artist ?? string.Empty, album ?? string.Empty);
	}

	private void UpdateToolTipText(string title = "Song/Track Title", string artist = "Artists", string album = "Album")
	{
		if (_toolTipText is null) return;

		_toolTipText.Inlines.Clear();
		_toolTipText.Inlines.Add(new Run { Text = "Title: ", FontWeight = Microsoft.UI.Text.FontWeights.Bold });
		_toolTipText.Inlines.Add(new Run { Text = title, FontStyle = Windows.UI.Text.FontStyle.Italic });
		_toolTipText.Inlines.Add(new LineBreak());
		_toolTipText.Inlines.Add(new LineBreak());

		_toolTipText.Inlines.Add(new Run { Text = "Artists: ", FontWeight = Microsoft.UI.Text.FontWeights.Bold });
		_toolTipText.Inlines.Add(new Run { Text = artist, FontStyle = Windows.UI.Text.FontStyle.Italic });
		_toolTipText.Inlines.Add(new LineBreak());
		_toolTipText.Inlines.Add(new LineBreak());

		_toolTipText.Inlines.Add(new Run { Text = "Album: ", FontWeight = Microsoft.UI.Text.FontWeights.Bold });
		_toolTipText.Inlines.Add(new Run { Text = album, FontStyle = Windows.UI.Text.FontStyle.Italic });
	}
}

