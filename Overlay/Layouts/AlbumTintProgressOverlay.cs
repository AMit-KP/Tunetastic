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
// ALBUM TINT PROGRESS
// The album-colour gradient IS the progress fill — revealed left-to-right
// as the track plays. 
// ══════════════════════════════════════════════════════════════════════
public class AlbumTintProgressOverlay : OverlayBase
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
	private Border? _gradientLayer;
	private RectangleGeometry? _gradientClip;
	private double _lastProgress = 0;

	private Color _accentColor1 = AccentBlue;
	private Color _accentColor2 = AccentPink;

	private TextBlock? _toolTipText;

	private ColorAnalyzer? _colorAnalyzer;
	private AccentColorAnalyzer? _accentColorAnalyzer;
	private BaseColorAnalyzer? _baseColorAnalyzer;
	private ColorWeightAnalyzer? _colorWeightAnalyzer;

	private Border? _titlePillBorder;
	private Border? _artistPillBorder;
	private Border? _prevPillBorder;
	private Border? _playPillBorder;
	private Border? _nextPillBorder;
	private Brush? _chipBrush;

	public AlbumTintProgressOverlay(OverlayTheme theme)
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
			BorderBrush = new SolidColorBrush(Border),
			Background = new SolidColorBrush(Surface),
			VerticalAlignment = VerticalAlignment.Center,
		};

		_chipBrush = new SolidColorBrush(Theme == OverlayTheme.Dark ? DarkSurface : LightSurface);

		// All layers stack inside one Grid so they can overlap.
		var stack = new Grid();

		_gradientLayer = new Border
		{
			CornerRadius = new CornerRadius(8),
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
		};

		_gradientClip = new RectangleGeometry { Rect = new Rect(0, 0, 0, 0) };
		_gradientLayer.Clip = _gradientClip;

		ResetToDefaultBackground();

		stack.Children.Add(_gradientLayer);

		_pill.SizeChanged += (s, e) =>
		{
			if (e.NewSize.Width > 0 && e.NewSize.Height > 0)
			{
				SetGradientReveal(_lastProgress);
			}
		};

		// Layer 2 — content (art, title/artist, divider, controls)
		var inner = new Grid
		{
			VerticalAlignment = VerticalAlignment.Center,
			ColumnSpacing = 6,
			Padding = new Thickness(6, 0, 10, 0),
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

		_artBox = MakeArtBox(32, 4, AccentPurple);
		Grid.SetColumn(_artBox, 0);
		inner.Children.Add(_artBox);

		var info = new Grid
		{
			RowSpacing = 3,
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto },
				new RowDefinition { Height = GridLength.Auto },
			},
		};

		// --- Title chip ---
		var titleBgTemplate = MakePillBorder(height: 18, radius: 4);
		titleBgTemplate.Background = _chipBrush;
		titleBgTemplate.Padding = new Thickness(2, 0, 2, 2);
		titleBgTemplate.HorizontalAlignment = HorizontalAlignment.Left;
		titleBgTemplate.VerticalAlignment = VerticalAlignment.Center;

		_titleText = MakeTitleText();
		_titleText.FontSize = 13;
		_titleText.Padding = new Thickness(2, 0, 2, 1);

		var titleChip = CreateRevealingChip(_titleText, titleBgTemplate, out var titleBg);
		_titlePillBorder = titleBg;

		Grid.SetRow(titleChip, 0);
		info.Children.Add(titleChip);

		// --- Artist chip ---
		var artistBgTemplate = MakePillBorder(height: 16, radius: 4);
		artistBgTemplate.Background = _chipBrush;
		artistBgTemplate.Padding = new Thickness(2, 0, 2, 2);
		artistBgTemplate.HorizontalAlignment = HorizontalAlignment.Left;
		artistBgTemplate.VerticalAlignment = VerticalAlignment.Center;

		_artistText = MakeSubText();
		_artistText.FontSize = 11;
		_artistText.Padding = new Thickness(2, 0, 2, 1);

		var artistChip = CreateRevealingChip(_artistText, artistBgTemplate, out var artistBg);
		_artistPillBorder = artistBg;

		Grid.SetRow(artistChip, 1);
		info.Children.Add(artistChip);

		Grid.SetColumn(info, 1);
		inner.Children.Add(info);

		_divider = MakeDivider();
		Grid.SetColumn(_divider, 2);
		inner.Children.Add(_divider);

		// --- Transport button chips ---
		var prevBgTemplate = MakePillBorder(height: 20, radius: 4);
		prevBgTemplate.Background = _chipBrush;
		prevBgTemplate.Width = 20;
		prevBgTemplate.Padding = new Thickness(0);
		prevBgTemplate.HorizontalAlignment = HorizontalAlignment.Center;

		var playBgTemplate = MakePillBorder(height: 25, radius: 4);
		playBgTemplate.Background = _chipBrush;
		playBgTemplate.Width = 25;
		playBgTemplate.Padding = new Thickness(0);
		playBgTemplate.HorizontalAlignment = HorizontalAlignment.Center;

		var nextBgTemplate = MakePillBorder(height: 20, radius: 4);
		nextBgTemplate.Background = _chipBrush;
		nextBgTemplate.Width = 20;
		nextBgTemplate.Padding = new Thickness(0);
		nextBgTemplate.HorizontalAlignment = HorizontalAlignment.Center;

		_prevButton = MakePrevButton();
		_playPauseButton = MakePlayPauseButton(16);
		_nextButton = MakeNextButton();

		_prevButton.Padding = new Thickness(0);
		_playPauseButton.Padding = new Thickness(0);
		_nextButton.Padding = new Thickness(0);

		var prevChip = CreateRevealingChip(_prevButton, prevBgTemplate, out var prevBg);
		_prevPillBorder = prevBg;

		var playChip = CreateRevealingChip(_playPauseButton, playBgTemplate, out var playBg);
		_playPillBorder = playBg;

		var nextChip = CreateRevealingChip(_nextButton, nextBgTemplate, out var nextBg);
		_nextPillBorder = nextBg;

		Grid.SetColumn(prevChip, 3);
		inner.Children.Add(prevChip);

		Grid.SetColumn(playChip, 4);
		inner.Children.Add(playChip);

		Grid.SetColumn(nextChip, 5);
		inner.Children.Add(nextChip);

		stack.Children.Add(inner);
		_pill.Child = stack;
		root.Children.Add(_pill);

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

	private void ResetToDefaultBackground()
	{
		_gradientLayer?.Background = new SolidColorBrush(Surface);
		_accentColor1 = AccentBlue;
		_accentColor2 = AccentPink;
		_lastProgress = 0;

		SetGradientReveal(0);
	}

	private void SetGradientReveal(double value)
	{
		if (_gradientClip is null || _pill is null) return;

		double width = _pill.ActualWidth;
		double height = _pill.ActualHeight;

		_gradientClip.Rect = (width <= 0 || height <= 0)
			? new Rect(0, 0, 0, 0)
			: new Rect(0, 0, width * value, height);

		RefreshChipReveal();
	}

	private void ApplyGradient(Color color1, Color color2)
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

		_gradientLayer?.Background = gradientBrush;
	}

	private async void CoverArtImage_Opened(object sender, RoutedEventArgs e)
	{
		if (_colorAnalyzer is null) return;

		await _colorAnalyzer.UpdateAnalyzerAsync();
		await Task.Delay(10);

		if (_accentColorAnalyzer?.SelectedColors != null && _baseColorAnalyzer?.SelectedColors != null && _colorWeightAnalyzer?.SelectedColors != null)
		{
			_accentColor1 = _accentColorAnalyzer.SelectedColors[0];

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

			int index = Array.FindIndex(candidates, c => !ColorHelper.AreColorsTooSimilar(_accentColor1, c, 40));
			_accentColor2 = index >= 0 ? candidates[index] : candidates[^1];
		}
		else
		{
			_accentColor1 = _accentColor2 = Surface;
		}

		ApplyGradient(_accentColor1, _accentColor2);

		SetGradientReveal(_lastProgress);
	}

	private Grid CreateRevealingChip(UIElement content, Border bgTemplate, out Border backgroundLayer)
	{
		var chipGrid = new Grid
		{
			HorizontalAlignment = bgTemplate.HorizontalAlignment,
			VerticalAlignment = bgTemplate.VerticalAlignment,
			Width = bgTemplate.Width,
			Height = bgTemplate.Height,
		};

		backgroundLayer = new Border
		{
			CornerRadius = bgTemplate.CornerRadius,
			Background = bgTemplate.Background,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
		};

		var contentHost = new Border
		{
			Padding = bgTemplate.Padding,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			Child = content,
		};

		chipGrid.Children.Add(backgroundLayer);
		chipGrid.Children.Add(contentHost);

		return chipGrid;
	}

	private void RefreshChipReveal()
	{
		if (_pill is null || _gradientClip is null) return;

		double revealedWidth = _gradientClip.Rect.Width;

		SetChipRevealed(_titlePillBorder, revealedWidth);
		SetChipRevealed(_artistPillBorder, revealedWidth);
		SetChipRevealed(_prevPillBorder, revealedWidth);
		SetChipRevealed(_playPillBorder, revealedWidth);
		SetChipRevealed(_nextPillBorder, revealedWidth);
	}

	private void SetChipRevealed(Border? chip, double revealedWidth)
	{
		if (chip is null || _pill is null) return;

		if (chip.Clip is not RectangleGeometry clip)
		{
			clip = new RectangleGeometry { Rect = new Rect(0, 0, 0, 0) };
			chip.Clip = clip;
		}

		double chipWidth = chip.ActualWidth;
		double chipHeight = chip.ActualHeight;

		if (chipWidth <= 0 || chipHeight <= 0)
		{
			clip.Rect = new Rect(0, 0, 0, 0);
			return;
		}

		Point point;
		try
		{
			point = chip.TransformToVisual(_pill).TransformPoint(new Point(0, 0));
		}
		catch (Exception)
		{
			return;
		}

		double localReveal = revealedWidth - point.X;
		localReveal = Math.Clamp(localReveal, 0, chipWidth);

		clip.Rect = new Rect(0, 0, localReveal, chipHeight);
	}

	public override void UpdateProgress(double value)
	{
		value = Math.Clamp(value, 0, 1);
		_lastProgress = value;
		SetGradientReveal(value);
	}

	public async void UpdateTrack(string title, string artist, string album, string art)
	{
		_titleText?.Text = title ?? string.Empty;
		_artistText?.Text = artist ?? string.Empty;

		ResetToDefaultBackground();

		try
		{
			StorageFile file = await StorageFile.GetFileFromPathAsync(art);
			using IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read);
			var albumArt = new BitmapImage();
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
