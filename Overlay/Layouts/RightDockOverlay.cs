using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Tunetastic.Overlay.Layouts;

// ══════════════════════════════════════════════════════════════════════
// RIGHT DOCK
// Right-anchored · controls on the LEFT of the art+info block
// ══════════════════════════════════════════════════════════════════════
public class RightDockOverlay : OverlayBase
{
	private TextBlock? _titleText;
	private TextBlock? _artistText;
	private Border? _artBox;
	private TextBlock? _toolTipText;

	/// <summary>
	/// Initializes a new instance of the <see cref="RightDockOverlay"/> class.
	/// </summary>
	/// <param name="theme">The theme to use for the overlay.</param>
	public RightDockOverlay(OverlayTheme theme)
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
			VerticalAlignment = VerticalAlignment.Center,
		};

		//Pill container
		var pill = MakePillBorder(height: 36, radius: 20);

		var inner = new Grid
		{
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Left,
		};

		// Columns: art | info | divider | prev | play | next
		inner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 0: prev
		inner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 1: play
		inner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 2: next
		inner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 3: divider
		inner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 4: art
		inner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) }); // 5: info

		// Prev
		var prev = MakePrevButton();
		Grid.SetColumn(prev, 0);
		inner.Children.Add(prev);

		// Play/Pause
		var play = MakePlayPauseButton(16);
		Grid.SetColumn(play, 1);
		inner.Children.Add(play);

		// Next
		var next = MakeNextButton();
		Grid.SetColumn(next, 2);
		inner.Children.Add(next);

		// Divider
		var divider = MakeDivider();
		Grid.SetColumn(divider, 3);
		inner.Children.Add(divider);

		// Album art
		_artBox = MakeArtBox(26, 13, AccentPurple);
		_artBox.Margin = new Thickness(2, 0, 2, 0);
		Grid.SetColumn(_artBox, 4);
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

	/// <summary>
	/// Updates the track information displayed in the overlay.
	/// </summary>
	/// <param name="title">The title of the track.</param>
	/// <param name="artist">The artist of the track.</param>
	/// <param name="album">The album of the track.</param>
	/// <param name="art">Optional bitmap image for the track art.</param>
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
