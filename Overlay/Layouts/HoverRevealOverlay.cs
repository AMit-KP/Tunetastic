using Microsoft.UI.Xaml.Documents;
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
	private TextBlock? _toolTipText;

	public HoverRevealOverlay(OverlayTheme theme)
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

		var pill = MakeRectBorder(height: 40);

		// Outer layout: [art box | content area]
		var inner = new Grid
		{
			Width = 145,
			VerticalAlignment = VerticalAlignment.Center,
		};
		inner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });           // col 0: art
		inner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });         // col 1: gap
		inner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // col 2: content

		// Art box — col 0
		_artBox = MakeArtBox(30, 8, AccentGreen);
		Grid.SetColumn(_artBox, 0);
		inner.Children.Add(_artBox);

		// Content area: info and controls overlap in the same cell (col 2)
		// They share a single-cell grid so they sit directly on top of each other.
		var contentCell = new Grid();
		contentCell.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // single row

		// Info panel — fills full width, row 0
		var info = new Grid
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Center,
		};
		info.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // row 0: title
		info.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // row 2: artist

		_titleText = MakeTitleText(maxWidth: 125);
		_titleText.HorizontalAlignment = HorizontalAlignment.Left;
		Grid.SetRow(_titleText, 0);
		info.Children.Add(_titleText);

		_artistText = MakeSubText(maxWidth: 125);
		_artistText.HorizontalAlignment = HorizontalAlignment.Left;
		Grid.SetRow(_artistText, 1);
		info.Children.Add(_artistText);

		Grid.SetRow(info, 0);
		contentCell.Children.Add(info);

		// Controls panel — same cell, stacked on top, invisible at rest
		var _controlsPanel = new Grid
		{
			Opacity = 0,
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Right,
		};
		_controlsPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // col 0: divider
		_controlsPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2) }); // col 1: gap
		_controlsPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // col 2: prev
		_controlsPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2) }); // col 3: gap
		_controlsPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // col 4: play/pause
		_controlsPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2) }); // col 5: gap
		_controlsPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // col 6: next

		var divider = MakeDivider();
		Grid.SetColumn(divider, 0);
		_controlsPanel.Children.Add(divider);

		var prevBtn = MakePrevButton();
		Grid.SetColumn(prevBtn, 2);
		_controlsPanel.Children.Add(prevBtn);

		var playPauseBtn = MakePlayPauseButton(16);
		Grid.SetColumn(playPauseBtn, 4);
		_controlsPanel.Children.Add(playPauseBtn);

		var nextBtn = MakeNextButton();
		Grid.SetColumn(nextBtn, 6);
		_controlsPanel.Children.Add(nextBtn);

		Grid.SetRow(_controlsPanel, 0);
		contentCell.Children.Add(_controlsPanel); // overlaps info in same row/col

		Grid.SetColumn(contentCell, 2);
		inner.Children.Add(contentCell);

		pill.Child = inner;
		root.Children.Add(pill);

		// Hover events on the pill
		pill.PointerEntered += (_, _) =>
		{
			_titleText.MaxWidth = 35;
			_artistText.MaxWidth = 35;
			FadeIn(_controlsPanel, 150);
		};
		pill.PointerExited += (_, _) =>
		{
			FadeOut(_controlsPanel, 125);
			_titleText.MaxWidth = 125;
			_artistText.MaxWidth = 180;
		};

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

	/// <param name="title">Track title.</param>
	/// <param name="artist">Artist name.</param>
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
