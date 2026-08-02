using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using ColorHelper = Tunetastic.Common.Helpers.ColorHelper;

namespace Tunetastic.Overlay.Layouts;

// ══════════════════════════════════════════════════════════════════════
// ALBUM TINT
// Background and border tint adapts to the album's colours.
// ══════════════════════════════════════════════════════════════════════
public class AlbumTintOverlay : OverlayBase
{
	private TextBlock? _titleText;
	private TextBlock? _artistText;
	private Rectangle? _divider;
	private Button? _prevButton;
	private Button? _nextButton;
	private Button? _playPauseButton;
	private Border? _artBox;
	private Image? _artImage;
	private Border? _pill;
	private TextBlock? _toolTipText;
	private ColorAnalyzer? _colorAnalyzer;
	private AccentColorAnalyzer? _accentColorAnalyzer;
	private BaseColorAnalyzer? _baseColorAnalyzer;
	private ColorWeightAnalyzer? _colorWeightAnalyzer;

	public AlbumTintOverlay(OverlayTheme theme)
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

		_pill = new Border
		{
			Height = 45,
			CornerRadius = new CornerRadius(8),
			BorderThickness = new Thickness(0.5),
			VerticalAlignment = VerticalAlignment.Center,
			Padding = new Thickness(6, 0, 10, 0),
		};

		// Set default gradient (purple)
		ApplyPillGradient(AccentBlue, AccentPink);

		// --- inner grid: replaces the horizontal StackPanel ---
		var inner = new Grid
		{
			VerticalAlignment = VerticalAlignment.Center,
			ColumnSpacing = 6,
			ColumnDefinitions =
			{
				new ColumnDefinition { Width = GridLength.Auto }, // art box
				new ColumnDefinition { Width = new GridLength(100) }, // title/artist
				new ColumnDefinition { Width = GridLength.Auto }, // divider
				new ColumnDefinition { Width = GridLength.Auto }, // prev
				new ColumnDefinition { Width = GridLength.Auto }, // play/pause
				new ColumnDefinition { Width = GridLength.Auto }, // next
			},
		};

		_artBox = MakeArtBox(26, 5, AccentPurple);
		Grid.SetColumn(_artBox, 0);
		inner.Children.Add(_artBox);

		// --- info grid: replaces the vertical StackPanel ---
		var info = new Grid
		{
			RowSpacing = 2,
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Auto },
			},
		};

		_titleText = MakeTitleText();
		_artistText = MakeSubText();

		Grid.SetRow(_titleText, 0);
		Grid.SetRow(_artistText, 1);
		info.Children.Add(_titleText);
		info.Children.Add(_artistText);

		Grid.SetColumn(info, 1);
		inner.Children.Add(info);

		_divider = MakeDivider();
		Grid.SetColumn(_divider, 2);
		inner.Children.Add(_divider);

		_prevButton = MakePrevButton();
		Grid.SetColumn(_prevButton, 3);
		inner.Children.Add(_prevButton);

		_playPauseButton = MakePlayPauseButton(16);
		Grid.SetColumn(_playPauseButton, 4);
		inner.Children.Add(_playPauseButton);

		_nextButton = MakeNextButton();
		Grid.SetColumn(_nextButton, 5);
		inner.Children.Add(_nextButton);

		_pill.Child = inner;
		root.Children.Add(_pill);

		_artImage = new Image
		{
			Stretch = Stretch.UniformToFill,
		};
		_artBox.Background = null;
		_artBox.Child = _artImage;
		_artImage.ImageOpened += CoverArtImage_Opened;

		_colorAnalyzer = new ColorAnalyzer();
		_colorAnalyzer.Source = _artImage;

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

	private void ApplyPillGradient(Color color1, Color color2)
	{
		var gradientBrush = new LinearGradientBrush
		{
			StartPoint = new Point(0, 0),
			EndPoint = new Point(1, 1),
			GradientStops =
			{
				new GradientStop { Color = color1, Offset = 0.0 },
				new GradientStop { Color = color2, Offset = 1.0 },
			}
		};

		_pill?.Background = gradientBrush;
	}

	private async void CoverArtImage_Opened(object sender, RoutedEventArgs e)
	{
		if (_colorAnalyzer != null)
		{
			await _colorAnalyzer.UpdateAnalyzerAsync();
			await Task.Delay(10);

			Color accentColor1, accentColor2;
			if (_accentColorAnalyzer != null && _accentColorAnalyzer?.SelectedColors != null && _baseColorAnalyzer != null && _baseColorAnalyzer?.SelectedColors != null && _colorWeightAnalyzer != null && _colorWeightAnalyzer?.SelectedColors != null)
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

			ApplyPillGradient(accentColor1, accentColor2);

			var bgColor1 = ColorHelper.IsItDarkOrLight(accentColor1);
			var bgColor2 = ColorHelper.IsItDarkOrLight(accentColor2);

			_titleText?.Foreground = new SolidColorBrush(bgColor1 == OverlayTheme.Dark ? DarkText : LightText);
			_artistText?.Foreground = new SolidColorBrush(bgColor1 == OverlayTheme.Dark ? DarkSubText : LightSubText);

			var buttonColor = bgColor2 == OverlayTheme.Dark ? Color.FromArgb(153, 255, 255, 255) : Color.FromArgb(153, 0, 0, 0);

			if (_prevButton?.Content is FontIcon icon1)
				icon1.Foreground = new SolidColorBrush(buttonColor);

			if (_playPauseButton?.Content is FontIcon icon)
				icon.Foreground = new SolidColorBrush(buttonColor);

			if (_nextButton?.Content is FontIcon icon2)
				icon2.Foreground = new SolidColorBrush(buttonColor);
		}
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
