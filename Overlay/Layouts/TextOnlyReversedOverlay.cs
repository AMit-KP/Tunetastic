using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

namespace Tunetastic.Overlay.Layouts;

// ══════════════════════════════════════════════════════════════════════
// TEXT ONLY REVERSED
// Zero art or icons · track name · artist label
// prev/play/next controls
// ══════════════════════════════════════════════════════════════════════
public class TextOnlyReversedOverlay : OverlayBase
{
	private TextBlock? _titleText;
	private TextBlock? _artistText;
	private TextBlock? _toolTipText;

	public TextOnlyReversedOverlay(OverlayTheme theme)
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
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0)),
		};

		var pill = MakeRectBorder(36, 8);

		var inner = new Grid
		{
			VerticalAlignment = VerticalAlignment.Center,
			ColumnSpacing = 2,
			ColumnDefinitions =
			{
				new ColumnDefinition { Width = new GridLength(100) }, // info
				new ColumnDefinition { Width = GridLength.Auto },	  // divider
				new ColumnDefinition { Width = GridLength.Auto },	  // prev
				new ColumnDefinition { Width = GridLength.Auto },	  // play/pause
				new ColumnDefinition { Width = GridLength.Auto },	  // next
			},
		};

		// Track + artist stacked
		var infoStack = new Grid
		{
			RowSpacing = 2,
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Auto },
			},
			Margin = new Thickness(0, 0, 2, 0),
		};
		_titleText = MakeTitleText();
		_titleText.FontSize = 12;
		_titleText.HorizontalAlignment = HorizontalAlignment.Right;
		Grid.SetRow(_titleText, 0);
		infoStack.Children.Add(_titleText);

		_artistText = MakeSubText();
		_artistText.FontSize = 11;
		_artistText.HorizontalAlignment = HorizontalAlignment.Right;
		Grid.SetRow(_artistText, 1);
		infoStack.Children.Add(_artistText);

		Grid.SetColumn(infoStack, 0);
		inner.Children.Add(infoStack);

		var divider = MakeDivider();
		Grid.SetColumn(divider, 1);
		inner.Children.Add(divider);

		var prevButton = MakePrevButton();
		Grid.SetColumn(prevButton, 2);
		inner.Children.Add(prevButton);

		var playPauseButton = MakePlayPauseButton(16);
		Grid.SetColumn(playPauseButton, 3);
		inner.Children.Add(playPauseButton);

		var nextButton = MakeNextButton();
		Grid.SetColumn(nextButton, 4);
		inner.Children.Add(nextButton);

		//pill.Child = inner;
		root.Children.Add(inner);

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

	public void UpdateTrack(string title, string artist, string album)
	{
		_titleText?.Text = title ?? string.Empty;
		_artistText?.Text = artist ?? string.Empty;

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
