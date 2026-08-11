using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;

namespace Tunetastic.Overlay.Layouts;

// ══════════════════════════════════════════════════════════════════════
// BOTTOM STRIPE
// 2px progress stripe along the BOTTOM edge · same content layout
// ══════════════════════════════════════════════════════════════════════
public class BottomAccentStripeOverlay : OverlayBase
{
	private TextBlock? _titleText;
	private TextBlock? _artistText;
	private Border? _artBox;
	private Rectangle? _stripeFill;
	private TextBlock? _toolTipText;
	private double _stripeContainerWidth = 0;

	/// <summary>
	/// Initializes a new instance of the <see cref="BottomAccentStripeOverlay"/> class.
	/// </summary>
	/// <param name="theme">The theme to use for the overlay.</param>
	public BottomAccentStripeOverlay(OverlayTheme theme)
	{
		Theme = theme;
		RootGrid = Build();
	}

	/// <summary>
	/// Builds the visual structure of the bottom accent stripe overlay.
	/// </summary>
	/// <returns>A Grid representing the overlay structure.</returns>
	private Grid Build()
	{
		var root = new Grid
		{
			HorizontalAlignment = HorizontalAlignment.Left,
		};

		var outer = new Border
		{
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(Surface),
			BorderBrush = new SolidColorBrush(Border),
			BorderThickness = new Thickness(0.5),
		};

		var rootStack = new Grid();
		rootStack.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		rootStack.RowDefinitions.Add(new RowDefinition { Height = new GridLength(3) });

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

		_artBox = MakeArtBox(26, 6, AccentGreen);
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

		Grid.SetRow(content, 0);

		// Stripe row
		var stripeGrid = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
		stripeGrid.SizeChanged += (s, e) => _stripeContainerWidth = e.NewSize.Width;

		var stripeTrack = new Rectangle
		{
			Fill = new SolidColorBrush(ProgressTrack),
			RadiusX = 2, RadiusY = 2,
			Height = 3,
			HorizontalAlignment = HorizontalAlignment.Stretch,
		};
		_stripeFill = new Rectangle
		{
			Fill = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemAccentColor"]),
			RadiusX = 2, RadiusY = 2,
			Width = 0,
			Height = 3,
			HorizontalAlignment = HorizontalAlignment.Left,
		};
		stripeGrid.Children.Add(stripeTrack);
		stripeGrid.Children.Add(_stripeFill);
		Grid.SetRow(stripeGrid, 1);

		rootStack.Children.Add(content);
		rootStack.Children.Add(stripeGrid);
		outer.Child = rootStack;
		root.Children.Add(outer);

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

	/// <inheritdoc/>
	public override void UpdateProgress(double value)
	{
		value = Math.Clamp(value, 0, 1);
		_stripeFill?.Width = _stripeContainerWidth * value;
	}

	/// <summary>
	/// Updates the track information displayed in the overlay.
	/// </summary>
	/// <param name="title">The track title.</param>
	/// <param name="artist">The artist name.</param>
	/// <param name="album">The album name.</param>
	/// <param name="art">The bitmap image for the album art (optional).</param>
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
