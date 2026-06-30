using Microsoft.UI.Xaml.Documents;
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
	private TextBlock? _toolTipText;

	public CenteredPillOverlay(OverlayTheme theme)
	{
		Theme = theme;
		RootGrid = Build();
	}

	private Grid Build()
	{
		var root = new Grid
		{
			VerticalAlignment = VerticalAlignment.Center
		};

		var pill = MakePillBorder(34, 20);
		pill.HorizontalAlignment = HorizontalAlignment.Left;
		pill.VerticalAlignment = VerticalAlignment.Center;

		var inner = new Grid
		{
			VerticalAlignment = VerticalAlignment.Center,
			ColumnDefinitions =
			{
				new ColumnDefinition { Width = GridLength.Auto }, // art
				new ColumnDefinition { Width = GridLength.Auto }, // prev
				new ColumnDefinition { Width = GridLength.Auto }, // play/pause
				new ColumnDefinition { Width = GridLength.Auto }, // next
				new ColumnDefinition { Width = GridLength.Auto }, // divider
				new ColumnDefinition { Width = new GridLength(100) }, // info
			},
		};

		_artBox = MakeArtBox(24, 12, AccentRose);
		Grid.SetColumn(_artBox, 0);
		inner.Children.Add(_artBox);

		var prevButton = MakePrevButton(12);
		Grid.SetColumn(prevButton, 1);
		inner.Children.Add(prevButton);

		var playPauseButton = MakePlayPauseButton(15);
		Grid.SetColumn(playPauseButton, 2);
		inner.Children.Add(playPauseButton);

		var nextButton = MakeNextButton(12);
		Grid.SetColumn(nextButton, 3);
		inner.Children.Add(nextButton);

		var divider = MakeDivider();
		Grid.SetColumn(divider, 4);
		inner.Children.Add(divider);

		var info = new Grid
		{
			VerticalAlignment = VerticalAlignment.Center,
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Auto },
			},
			Margin = new Thickness(4, 0, 0, 0)
		};

		_titleText = MakeTitleText();
		Grid.SetRow(_titleText, 0);
		info.Children.Add(_titleText);

		_artistText = MakeSubText();
		Grid.SetRow(_artistText, 1);
		info.Children.Add(_artistText);

		Grid.SetColumn(info, 5);
		inner.Children.Add(info);

		pill.Child = inner;
		root.Children.Add(pill);

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
