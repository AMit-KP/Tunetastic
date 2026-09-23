using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;

namespace Tunetastic.Overlay.Layouts;

// ══════════════════════════════════════════════════════════════════════
// ACCENT ANCIENT SCROLL
// Ancient scroll with accent-coloured rods, comtrols and track info.
// ══════════════════════════════════════════════════════════════════════
public class AccentAncientScrollOverlay : OverlayBase
{
	private TextBlock? _titleText;
	private TextBlock? _artistText;
	private Border? _artBox;
	private Image? _artImage;
	private Color _scrollAccentColor = AccentGold;
	private LinearGradientBrush? _leftRodBrush;
	private LinearGradientBrush? _rightRodBrush;
	private SolidColorBrush? _bodyBorderBrush;
	private ColorAnalyzer? _colorAnalyzer;
	private AccentColorAnalyzer? _accentColorAnalyzer;
	private TextBlock? _toolTipText;

	/// <summary>
	/// Initializes a new instance of the <see cref="AccentAncientScrollOverlay"/> class.
	/// </summary>
	/// <param name="theme">The theme to use for the overlay.</param>
	public AccentAncientScrollOverlay(OverlayTheme theme)
	{
		Theme = theme;
		RootGrid = Build();
	}

	/// <summary>
	/// Builds the visual structure of the ancient scroll overlay.
	/// </summary>
	/// <returns>A Grid representing the overlay structure.</returns>
	private Grid Build()
	{
		var root = new Grid
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Center,
		};

		const double rollWidth = 16;
		const double capExtra = 6;
		const double bodyHeight = 40;

		// outer: 3 columns (left rod, body, right rod) x 3 rows (top cap, middle, bottom cap)
		var outer = new Grid
		{
			VerticalAlignment = VerticalAlignment.Center,
		};
		outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(rollWidth) });
		outer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(rollWidth) });

		outer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(capExtra) });
		outer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(bodyHeight) });
		outer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(capExtra) });

		// --- Left rod ---
		_leftRodBrush = BuildRodBrush(mirrored: false);
		var _accentBar = new Border
		{
			Width = rollWidth,
			Height = bodyHeight + capExtra * 2,
			CornerRadius = new CornerRadius(rollWidth / 2),
			Background = _leftRodBrush,
		};
		Grid.SetColumn(_accentBar, 0);
		Grid.SetRow(_accentBar, 0);
		Grid.SetRowSpan(_accentBar, 3);
		outer.Children.Add(_accentBar);

		// --- Right rod (mirrored gradient direction) ---
		_rightRodBrush = BuildRodBrush(mirrored: true);
		var rightRod = new Border
		{
			Width = rollWidth,
			Height = bodyHeight + capExtra * 2,
			CornerRadius = new CornerRadius(rollWidth / 2),
			Background = _rightRodBrush,
		};
		Grid.SetColumn(rightRod, 2);
		Grid.SetRow(rightRod, 0);
		Grid.SetRowSpan(rightRod, 3);
		outer.Children.Add(rightRod);

		// --- Body (parchment) — top/bottom fold lines tinted with the same swappable color ---
		_bodyBorderBrush = new SolidColorBrush(_scrollAccentColor);
		var body = new Border
		{
			CornerRadius = new CornerRadius(0),
			Background = new SolidColorBrush(Surface),
			BorderBrush = _bodyBorderBrush,
			BorderThickness = new Thickness(0, 2, 0, 2),
			Height = bodyHeight,
			Padding = new Thickness(8, 0, 10, 0),
		};
		Grid.SetColumn(body, 1);
		Grid.SetRow(body, 1);

		// --- inner: content grid (art box, info, divider, buttons) — no curl layers anymore ---
		var inner = new Grid
		{
			VerticalAlignment = VerticalAlignment.Center,
		};
		inner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // art box
		inner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) });
		inner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) }); // info
		inner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2) });
		inner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // divider
		inner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2) });
		inner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // prev
		inner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // play/pause
		inner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // next

		_artBox = MakeArtBox(26, 6, AccentOrange);
		Grid.SetColumn(_artBox, 0);
		inner.Children.Add(_artBox);

		var info = new Grid { };
		info.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		info.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		_titleText = MakeTitleText();
		Grid.SetRow(_titleText, 0);
		info.Children.Add(_titleText);

		_artistText = MakeSubText();
		Grid.SetRow(_artistText, 1);
		info.Children.Add(_artistText);

		Grid.SetColumn(info, 2);
		inner.Children.Add(info);

		var divider = MakeDivider();
		Grid.SetColumn(divider, 4);
		inner.Children.Add(divider);

		var prevButton = MakePrevButton();
		Grid.SetColumn(prevButton, 6);
		inner.Children.Add(prevButton);

		var playPauseButton = MakePlayPauseButton(16);
		Grid.SetColumn(playPauseButton, 7);
		inner.Children.Add(playPauseButton);

		var nextButton = MakeNextButton();
		Grid.SetColumn(nextButton, 9);
		inner.Children.Add(nextButton);

		body.Child = inner;
		outer.Children.Add(body);
		root.Children.Add(outer);

		_artImage = new Image
		{
			Stretch = Stretch.UniformToFill,
		};
		_artBox.Background = null;
		_artBox.Child = _artImage;
		_artImage.ImageOpened += CoverArtImage_Opened;

		_colorAnalyzer = new ColorAnalyzer();
		_colorAnalyzer.Source = _artImage;

		_accentColorAnalyzer = new AccentColorAnalyzer() { MinColorCount = 1 };
		_colorAnalyzer.Analyzers.Add(_accentColorAnalyzer);

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
	/// Creates a gradient brush for the scroll rods with specified mirroring.
	/// </summary>
	/// <param name="mirrored">Indicates whether the gradient should be mirrored.</param>
	/// <returns>A LinearGradientBrush configured for the rod appearance.</returns>
	private LinearGradientBrush BuildRodBrush(bool mirrored)
	{
		var stops = mirrored
			? new[]
			{
			new GradientStop { Color = Color.FromArgb(220, _scrollAccentColor.R, _scrollAccentColor.G, _scrollAccentColor.B), Offset = 0.0 },
			new GradientStop { Color = _scrollAccentColor, Offset = 0.35 },
			new GradientStop { Color = Color.FromArgb(160, _scrollAccentColor.R, _scrollAccentColor.G, _scrollAccentColor.B), Offset = 0.65 },
			new GradientStop { Color = _scrollAccentColor, Offset = 1.0 },
			}
			: new[]
			{
			new GradientStop { Color = _scrollAccentColor, Offset = 0.0 },
			new GradientStop { Color = Color.FromArgb(160, _scrollAccentColor.R, _scrollAccentColor.G, _scrollAccentColor.B), Offset = 0.35 },
			new GradientStop { Color = _scrollAccentColor, Offset = 0.65 },
			new GradientStop { Color = Color.FromArgb(220, _scrollAccentColor.R, _scrollAccentColor.G, _scrollAccentColor.B), Offset = 1.0 },
			};

		var brush = new LinearGradientBrush
		{
			StartPoint = new Point(0, 0.5),
			EndPoint = new Point(1, 0.5),
		};
		foreach (var stop in stops)
			brush.GradientStops.Add(stop);

		return brush;
	}

	/// <summary>
	/// Applies new gradient stops to an existing rod brush.
	/// </summary>
	/// <param name="brush">The brush to update.</param>
	/// <param name="mirrored">Indicates whether the gradient should be mirrored.</param>
	private void ApplyRodStops(LinearGradientBrush brush, bool mirrored)
	{
		brush.GradientStops.Clear();

		if (mirrored)
		{
			brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(220, _scrollAccentColor.R, _scrollAccentColor.G, _scrollAccentColor.B), Offset = 0.0 });
			brush.GradientStops.Add(new GradientStop { Color = _scrollAccentColor, Offset = 0.35 });
			brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(160, _scrollAccentColor.R, _scrollAccentColor.G, _scrollAccentColor.B), Offset = 0.65 });
			brush.GradientStops.Add(new GradientStop { Color = _scrollAccentColor, Offset = 1.0 });
		}
		else
		{
			brush.GradientStops.Add(new GradientStop { Color = _scrollAccentColor, Offset = 0.0 });
			brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(160, _scrollAccentColor.R, _scrollAccentColor.G, _scrollAccentColor.B), Offset = 0.35 });
			brush.GradientStops.Add(new GradientStop { Color = _scrollAccentColor, Offset = 0.65 });
			brush.GradientStops.Add(new GradientStop { Color = Color.FromArgb(220, _scrollAccentColor.R, _scrollAccentColor.G, _scrollAccentColor.B), Offset = 1.0 });
		}
	}

	/// <summary>
	/// Updates the accent color of the scroll elements.
	/// </summary>
	/// <param name="newColor">The new accent color to apply.</param>
	private void UpdateScrollAccentColor(Color newColor)
	{
		_scrollAccentColor = newColor;

		if (_leftRodBrush != null) ApplyRodStops(_leftRodBrush, mirrored: false);
		if (_rightRodBrush != null) ApplyRodStops(_rightRodBrush, mirrored: true);
		if (_bodyBorderBrush != null) _bodyBorderBrush.Color = newColor;
	}

	/// <summary>
	/// Updates the track information displayed in the overlay.
	/// </summary>
	/// <param name="title">The track title.</param>
	/// <param name="artist">The artist name.</param>
	/// <param name="album">The album name.</param>
	/// <param name="art">The path to the album art image.</param>
	public async void UpdateTrack(string title, string artist, string album, string art)
	{
		_titleText?.Text = title ?? string.Empty;
		_artistText?.Text = artist ?? string.Empty;

		try
		{
			StorageFile file = await StorageFile.GetFileFromPathAsync(art);
			using IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read);
			var albumArt = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
			_artImage?.Source = albumArt;
			await albumArt.SetSourceAsync(stream);
		}
		catch (Exception)
		{
			return;
		}

		UpdateToolTipText(title ?? string.Empty, artist ?? string.Empty, album ?? string.Empty);
	}

	/// <summary>
	/// Handles the image opened event for cover art, updating the accent color based on the image.
	/// </summary>
	/// <param name="sender">The event sender.</param>
	/// <param name="e">The event arguments.</param>
	private async void CoverArtImage_Opened(object sender, RoutedEventArgs e)
	{
		if (_colorAnalyzer != null)
		{
			await _colorAnalyzer.UpdateAnalyzerAsync();
			await Task.Delay(10);
			var accentColor = _accentColorAnalyzer != null && _accentColorAnalyzer.SelectedColors != null ? _accentColorAnalyzer.SelectedColors[0] : Surface;
			UpdateScrollAccentColor(accentColor);
		}
	}

	/// <summary>
	/// Updates the tooltip text with track information.
	/// </summary>
	/// <param name="title">The track title.</param>
	/// <param name="artist">The artist name.</param>
	/// <param name="album">The album name.</param>
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
