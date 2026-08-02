using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using ColorHelper = Tunetastic.Common.Helpers.ColorHelper;

namespace Tunetastic.Overlay.Layouts;

// ══════════════════════════════════════════════════════════════════════
// TOP ALBUM ACCENT STRIPE
// 3px progress stripe along the TOP edge · art + title + artist + controls
// ══════════════════════════════════════════════════════════════════════
public class TopAlbumAccentStripeOverlay : OverlayBase
{
	private TextBlock? _titleText;
	private TextBlock? _artistText;
	private Border? _artBox;
	private Image? _artImage;
	private Rectangle? _stripeFill;
	private TextBlock? _toolTipText;
	private double _stripeContainerWidth = 0;

	private ColorAnalyzer? _colorAnalyzer;
	private AccentColorAnalyzer? _accentColorAnalyzer;
	private BaseColorAnalyzer? _baseColorAnalyzer;
	private ColorWeightAnalyzer? _colorWeightAnalyzer;

	public TopAlbumAccentStripeOverlay(OverlayTheme theme)
	{
		Theme = theme;
		RootGrid = Build();
	}

	private Grid Build()
	{
		var root = new Grid
		{
			HorizontalAlignment = HorizontalAlignment.Left,
		};

		// Outer border with no padding (stripe bleeds to edge)
		var outer = new Border
		{
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(Surface),
			BorderBrush = new SolidColorBrush(Border),
			BorderThickness = new Thickness(0.5),
		};

		var rootStack = new Grid();
		rootStack.RowDefinitions.Add(new RowDefinition { Height = new GridLength(3) });
		rootStack.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

		// Stripe row
		var stripeGrid = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
		var stripeTrack = new Rectangle
		{
			Fill = new SolidColorBrush(ProgressTrack),
			RadiusX = 2, RadiusY = 2,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			Height = 3,
		};
		_stripeFill = new Rectangle
		{
			RadiusX = 2, RadiusY = 2,
			Width = 0,
			Height = 3,
			HorizontalAlignment = HorizontalAlignment.Left,
		};

		// Default gradient (matches AlbumTintOverlay's default) until album art loads
		ApplyStripeGradient(AccentBlue, AccentPink);

		stripeGrid.Children.Add(stripeTrack);
		stripeGrid.Children.Add(_stripeFill);
		Grid.SetRow(stripeGrid, 0);

		// Register for size change so we can compute stripe fill width
		stripeGrid.SizeChanged += (s, e) =>
		{
			_stripeContainerWidth = e.NewSize.Width;
		};

		// Content row
		var content = new Grid
		{
			VerticalAlignment = VerticalAlignment.Center,
			ColumnSpacing = 2,
			Padding = new Thickness(6, 0, 10, 0),
			ColumnDefinitions =
			{
				new ColumnDefinition { Width = GridLength.Auto },	  // art
				new ColumnDefinition { Width = new GridLength(100) }, // info
				new ColumnDefinition { Width = GridLength.Auto },	  // divider
				new ColumnDefinition { Width = GridLength.Auto },	  // prev
				new ColumnDefinition { Width = GridLength.Auto },	  // play/pause
				new ColumnDefinition { Width = GridLength.Auto },	  // next
			},
		};

		_artBox = MakeArtBox(26, 6, AccentTeal);
		Grid.SetColumn(_artBox, 0);
		content.Children.Add(_artBox);

		var info = new Grid
		{
			RowSpacing = 2,
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Auto },
			},
			Margin = new Thickness(3, 0, 0, 0),
		};
		_titleText = MakeTitleText();
		Grid.SetRow(_titleText, 0);
		info.Children.Add(_titleText);

		_artistText = MakeSubText();
		Grid.SetRow(_artistText, 1);
		info.Children.Add(_artistText);

		Grid.SetColumn(info, 1);
		content.Children.Add(info);

		var divider = MakeDivider();
		Grid.SetColumn(divider, 2);
		content.Children.Add(divider);

		var prevButton = MakePrevButton();
		Grid.SetColumn(prevButton, 3);
		content.Children.Add(prevButton);

		var playPauseButton = MakePlayPauseButton(16);
		Grid.SetColumn(playPauseButton, 4);
		content.Children.Add(playPauseButton);

		var nextButton = MakeNextButton();
		Grid.SetColumn(nextButton, 5);
		content.Children.Add(nextButton);

		Grid.SetRow(content, 1);

		rootStack.Children.Add(stripeGrid);
		rootStack.Children.Add(content);
		outer.Child = rootStack;
		root.Children.Add(outer);

		// --- persistent art image + color analyzer chain ---
		_artImage = new Image { Stretch = Stretch.UniformToFill };
		_artBox.Background = null;
		_artBox.Child = _artImage;
		_artImage.ImageOpened += CoverArtImage_Opened;

		_colorAnalyzer = new ColorAnalyzer { Source = _artImage };
		_accentColorAnalyzer = new AccentColorAnalyzer() { MinColorCount = 3 };
		_baseColorAnalyzer = new BaseColorAnalyzer() { MinColorCount = 3 };
		_colorWeightAnalyzer = new ColorWeightAnalyzer() { MinColorCount = 3 };
		_colorAnalyzer.Analyzers.Add(_accentColorAnalyzer);
		_colorAnalyzer.Analyzers.Add(_baseColorAnalyzer);
		_colorAnalyzer.Analyzers.Add(_colorWeightAnalyzer);

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

	private void ApplyStripeGradient(Color color1, Color color2)
	{
		var gradientBrush = new LinearGradientBrush
		{
			StartPoint = new Point(0, 0),
			EndPoint = new Point(1, 0), // horizontal, matches the stripe's direction
			GradientStops =
			{
				new GradientStop { Color = color1, Offset = 0.0 },
				new GradientStop { Color = color2, Offset = 1.0 },
			}
		};

		_stripeFill?.Fill = gradientBrush;
	}

	private async void CoverArtImage_Opened(object sender, RoutedEventArgs e)
	{
		if (_colorAnalyzer is null) return;

		await _colorAnalyzer.UpdateAnalyzerAsync();
		await Task.Delay(10);

		Color accentColor1, accentColor2;
		if (_accentColorAnalyzer?.SelectedColors != null && _baseColorAnalyzer?.SelectedColors != null && _colorWeightAnalyzer?.SelectedColors != null)
		{
			accentColor1 = _accentColorAnalyzer.SelectedColors[0];

			var candidates = new[]
			{
				_colorWeightAnalyzer.SelectedColors[0],
				_colorWeightAnalyzer.SelectedColors[1],
				_accentColorAnalyzer.SelectedColors[1],
				_colorWeightAnalyzer.SelectedColors[2],
				_accentColorAnalyzer.SelectedColors[2],
				_baseColorAnalyzer.SelectedColors[1],
				_baseColorAnalyzer.SelectedColors[2],
			};

			int index = Array.FindIndex(candidates, c => !ColorHelper.AreColorsTooSimilar(accentColor1, c, 40));
			accentColor2 = index >= 0 ? candidates[index] : candidates[^1];
		}
		else
		{
			accentColor1 = accentColor2 = Surface;
		}

		ApplyStripeGradient(accentColor1, accentColor2);
	}

	public override void UpdateProgress(double value)
	{
		value = Math.Clamp(value, 0, 1);
		_stripeFill?.Width = _stripeContainerWidth * value;
	}

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
