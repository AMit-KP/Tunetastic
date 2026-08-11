using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;

namespace Tunetastic.Overlay.Layouts;

// ══════════════════════════════════════════════════════════════════════
// TOP STRIPE
// 3px progress stripe along the TOP edge · art + title + artist + controls
// ══════════════════════════════════════════════════════════════════════
public class TopAccentStripeOverlay : OverlayBase
{
	private TextBlock? _titleText;
	private TextBlock? _artistText;
	private Border? _artBox;
	private Rectangle? _stripeFill;
	private TextBlock? _toolTipText;
	private double _stripeContainerWidth = 0;

	/// <summary>
	/// Initializes a new instance of the <see cref="TopAccentStripeOverlay"/> class.
	/// </summary>
	/// <param name="theme">The theme to use for the overlay.</param>
	public TopAccentStripeOverlay(OverlayTheme theme)
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
			Fill = new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemAccentColor"]),
			RadiusX = 2, RadiusY = 2,
			Width = 0,
			Height = 3,
			HorizontalAlignment = HorizontalAlignment.Left,
		};
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
			{ Source = art, Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill };
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
