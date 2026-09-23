using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

namespace Tunetastic.Overlay.Layouts;

// ══════════════════════════════════════════════════════════════════════
// TEXT ONLY
// prev/play/next controls
// Zero art or icons · track name · artist label
// ══════════════════════════════════════════════════════════════════════
public class TextOnlyOverlay : OverlayBase
{
	private TextBlock? _titleText;
	private TextBlock? _artistText;
	private TextBlock? _toolTipText;

	/// <summary>
	/// Initializes a new instance of the <see cref="TextOnlyOverlay"/> class.
	/// </summary>
	/// <param name="theme">The theme to use for the overlay.</param>
	public TextOnlyOverlay(OverlayTheme theme)
	{
		Theme = theme;
		RootGrid = Build();
	}

	/// <summary>
	/// Builds the UI layout for the hover reveal overlay.
	/// </summary>
	/// <returns>A Grid representing the root of the overlay layout.</returns>
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
				new ColumnDefinition { Width = GridLength.Auto },	  // prev
				new ColumnDefinition { Width = GridLength.Auto },	  // play/pause
				new ColumnDefinition { Width = GridLength.Auto },	  // next
				new ColumnDefinition { Width = GridLength.Auto },	  // divider
				new ColumnDefinition { Width = new GridLength(100) }, // info
			},
		};

		var prevButton = MakePrevButton();
		Grid.SetColumn(prevButton, 0);
		inner.Children.Add(prevButton);

		var playPauseButton = MakePlayPauseButton(16);
		Grid.SetColumn(playPauseButton, 1);
		inner.Children.Add(playPauseButton);

		var nextButton = MakeNextButton();
		Grid.SetColumn(nextButton, 2);
		inner.Children.Add(nextButton);

		var divider = MakeDivider();
		Grid.SetColumn(divider, 3);
		inner.Children.Add(divider);

		// Track + artist stacked
		var infoStack = new Grid
		{
			RowSpacing = 2,
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Auto },
			},
			Margin = new Thickness(2, 0, 0, 0),
		};
		_titleText = MakeTitleText();
		_titleText.FontSize = 12;
		Grid.SetRow(_titleText, 0);
		infoStack.Children.Add(_titleText);

		_artistText = MakeSubText();
		_artistText.FontSize = 11;
		Grid.SetRow(_artistText, 1);
		infoStack.Children.Add(_artistText);

		Grid.SetColumn(infoStack, 4);
		inner.Children.Add(infoStack);

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

	/// <summary>
	/// Updates the track information displayed in the overlay.
	/// </summary>
	/// <param name="title">The title of the track.</param>
	/// <param name="artist">The artist of the track.</param>
	/// <param name="album">The album of the track.</param>
	public void UpdateTrack(string title, string artist, string album)
	{
		_titleText?.Text = title ?? string.Empty;
		_artistText?.Text = artist ?? string.Empty;

		UpdateToolTipText(title ?? string.Empty, artist ?? string.Empty, album ?? string.Empty);
	}

	/// <summary>
	/// Updates the tooltip text with track information.
	/// </summary>
	/// <param name="title">Track title.</param>
	/// <param name="artist">Artist name.</param>
	/// <param name="album">Album name.</param>
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
