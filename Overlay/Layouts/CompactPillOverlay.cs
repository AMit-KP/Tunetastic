using Microsoft.UI.Xaml.Documents;
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
	private TextBlock? _toolTipText;

	public CompactPillOverlay(OverlayTheme theme)
	{
		Theme = theme;
		RootGrid = Build();
	}

	private Grid Build()
	{
		var root = new Grid
		{
			VerticalAlignment = VerticalAlignment.Center,
		};

		//Pill container
		var pill = MakePillBorder(height: 36, radius: 20);

		var inner = new Grid
		{
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Left,
			Width = 200
		};

		// Columns: art | info | divider | prev | play | next
		inner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 0: art
		inner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) }); // 1: info
		inner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 2: divider
		inner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 3: prev
		inner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 4: play
		inner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 5: next

		// Album art
		_artBox = MakeArtBox(26, 13, AccentPurple);
		_artBox.Margin = new Thickness(0, 0, 6, 0);
		Grid.SetColumn(_artBox, 0);
		inner.Children.Add(_artBox);

		// Title + artist stacked
		var info = new Grid();
		info.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: title
		info.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1: artist
		info.Margin = new Thickness(0, 0, 6, 0);
		info.VerticalAlignment = VerticalAlignment.Center;

		_titleText = MakeTitleText();
		Grid.SetRow(_titleText, 0);
		info.Children.Add(_titleText);

		_artistText = MakeSubText();
		Grid.SetRow(_artistText, 1);
		info.Children.Add(_artistText);

		Grid.SetColumn(info, 1);
		inner.Children.Add(info);

		// Divider
		var divider = MakeDivider();
		Grid.SetColumn(divider, 2);
		inner.Children.Add(divider);

		// Prev
		var prev = MakePrevButton();
		Grid.SetColumn(prev, 3);
		inner.Children.Add(prev);

		// Play/Pause
		var play = MakePlayPauseButton(16);
		Grid.SetColumn(play, 4);
		inner.Children.Add(play);

		// Next
		var next = MakeNextButton();
		Grid.SetColumn(next, 5);
		inner.Children.Add(next);

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

	/// <param name="title">Track title displayed in the pill.</param>
	/// <param name="artist">Artist name displayed in the pill.</param>
	/// <param name="art">Optional album art bitmap.</param>
	public void UpdateTrack(string title, string artist, string album, BitmapImage? art = null)
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
