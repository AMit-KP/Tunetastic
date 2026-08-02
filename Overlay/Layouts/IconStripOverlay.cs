using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Tunetastic.Overlay.Layouts;

// ══════════════════════════════════════════════════════════════════════
// ICON STRIP
// Zero text · art thumbnail · prev / play / next
// ══════════════════════════════════════════════════════════════════════
public class IconStripOverlay : OverlayBase
{
	private Border? _artBox;
	private TextBlock? _toolTipText;

	public IconStripOverlay(OverlayTheme theme)
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

		var pill = MakeRectBorder(height: 45);

		var inner = new Grid
		{
			VerticalAlignment = VerticalAlignment.Center,
			ColumnDefinitions =
			{
				new ColumnDefinition { Width = new GridLength(42) }, // 0: art box
				new ColumnDefinition { Width = GridLength.Auto }, // 1: prev button
				new ColumnDefinition { Width = GridLength.Auto }, // 2: prev button
				new ColumnDefinition { Width = GridLength.Auto }, // 3: play/pause button
				new ColumnDefinition { Width = GridLength.Auto }, // 4: next button
			},
		};

		_artBox = MakeArtBox(32, 5, AccentBlue);
		_artBox.Margin = new Thickness(4, 0, 4, 0);
		Grid.SetColumn(_artBox, 0);
		inner.Children.Add(_artBox);

		var divider = MakeDivider();
		Grid.SetColumn(divider, 1);
		inner.Children.Add(divider);

		var prevButton = MakePrevButton(13);
		Grid.SetColumn(prevButton, 2);
		inner.Children.Add(prevButton);

		var playPauseButton = MakePlayPauseButton(16);
		Grid.SetColumn(playPauseButton, 3);
		inner.Children.Add(playPauseButton);

		var nextButton = MakeNextButton(13);
		Grid.SetColumn(nextButton, 4);
		inner.Children.Add(nextButton);

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

	/// <param name="art">Album art bitmap. No text fields in this layout.</param>
	public void UpdateTrack(string title, string artist, string album, BitmapImage? art = null)
	{
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
