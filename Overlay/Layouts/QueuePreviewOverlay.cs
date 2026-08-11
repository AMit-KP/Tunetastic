using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;

namespace Tunetastic.Overlay.Layouts;

// ══════════════════════════════════════════════════════════════════════
// QUEUE PREVIEW
// Current + 2 upcoming album art thumbnails in a diminishing stack,
// track info, prev/play/next controls.
// ══════════════════════════════════════════════════════════════════════
public class QueuePreviewOverlay : OverlayBase
{
	private Border? _currentArt;
	private Border? _nextArt1;
	private Border? _nextArt2;
	private TextBlock? _titleText;
	private TextBlock? _artistText;
	private TextBlock? _toolTipText;

	// Default accent colours for queue slots
	private static readonly Color[] QueueColors = { AccentIndigo, AccentGreen, AccentOrange };

	/// <summary>
	/// Initializes a new instance of the <see cref="QueuePreviewOverlay"/> class.
	/// </summary>
	/// <param name="theme">The theme to use for the overlay.</param>
	public QueuePreviewOverlay(OverlayTheme theme)
	{
		Theme = theme;
		RootGrid = Build();
	}

	/// <summary>
	/// Builds the UI layout for the queue preview overlay.
	/// </summary>
	/// <returns>A Grid representing the root of the overlay layout.</returns>
	private Grid Build()
	{
		var root = new Grid
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Center,
		};

		var pill = MakeRectBorder(height: 48);

		var inner = new Grid
		{
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(4, 0, 0, 0),
			ColumnDefinitions =
			{
				new ColumnDefinition { Width = GridLength.Auto }, // 0: art stack
				new ColumnDefinition { Width = new GridLength(100) }, // 1: info
				new ColumnDefinition { Width = GridLength.Auto }, // 2: divider
				new ColumnDefinition { Width = GridLength.Auto }, // 3: prev button
				new ColumnDefinition { Width = GridLength.Auto }, // 4: play/pause button
				new ColumnDefinition { Width = GridLength.Auto }, // 5: next button
			},
		};

		// ── Diminishing art stack ───────────────────────────────────
		var artStack = new Grid
		{
			VerticalAlignment = VerticalAlignment.Center,
			ColumnDefinitions =
			{
				new ColumnDefinition { Width = GridLength.Auto }, // 0: current art
				new ColumnDefinition { Width = GridLength.Auto }, // 1: next art 1
				new ColumnDefinition { Width = GridLength.Auto }, // 2: next art 2
			},
		};
		Grid.SetColumn(artStack, 0);

		const double ArtSpacing = 2;

		_currentArt = MakeArtBox(30, 6, QueueColors[0]);
		_currentArt.BorderBrush = new SolidColorBrush(Theme == OverlayTheme.Dark
			? Color.FromArgb(80, 255, 255, 255)
			: Color.FromArgb(80, 0, 0, 0));
		_currentArt.BorderThickness = new Thickness(1.5);
		Grid.SetColumn(_currentArt, 0);

		_nextArt1 = MakeArtBox(22, 4, QueueColors[1]);
		_nextArt1.Opacity = 0.60;
		_nextArt1.Margin = new Thickness(ArtSpacing, 0, 0, 0);
		Grid.SetColumn(_nextArt1, 1);

		_nextArt2 = MakeArtBox(16, 3, QueueColors[2]);
		_nextArt2.Opacity = 0.35;
		_nextArt2.Margin = new Thickness(ArtSpacing, 0, 0, 0);
		Grid.SetColumn(_nextArt2, 2);

		artStack.Children.Add(_currentArt);
		artStack.Children.Add(_nextArt1);
		artStack.Children.Add(_nextArt2);
		inner.Children.Add(artStack);

		// ── Info ────────────────────────────────────────────────────
		var info = new Grid
		{
			VerticalAlignment = VerticalAlignment.Center,
			MaxWidth = 90,
			Margin = new Thickness(6, 0, 2, 0),
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto }, // 0: title
				new RowDefinition { Height = GridLength.Auto }, // 1: artist
			},
		};
		Grid.SetColumn(info, 1);

		_titleText = MakeTitleText();
		Grid.SetRow(_titleText, 0);
		info.Children.Add(_titleText);

		_artistText = MakeSubText();
		Grid.SetRow(_artistText, 1);
		info.Children.Add(_artistText);

		inner.Children.Add(info);

		var divider = MakeDivider();
		Grid.SetColumn(divider, 2);
		inner.Children.Add(divider);

		var prevButton = MakePrevButton();
		Grid.SetColumn(prevButton, 3);
		inner.Children.Add(prevButton);

		var playPauseButton = MakePlayPauseButton(16);
		Grid.SetColumn(playPauseButton, 4);
		inner.Children.Add(playPauseButton);

		var nextButton = MakeNextButton();
		Grid.SetColumn(nextButton, 5);
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

	/// <summary>
	/// Updates the track information displayed in the queue preview overlay.
	/// </summary>
	/// <param name="title">Current track title.</param>
	/// <param name="artist">Current track artist.</param>
	/// <param name="album">Current track album.</param>
	/// <param name="currentArt">Album art for the current track.</param>
	/// <param name="nextArt1">Album art for the next track in queue.</param>
	/// <param name="nextArt2">Album art for the track after that.</param>
	public void UpdateTrack(
		string title, string artist, string album, BitmapImage? currentArt = null, BitmapImage? nextArt1 = null, BitmapImage? nextArt2 = null)
	{
		_titleText?.Text = title ?? string.Empty;
		_artistText?.Text = artist ?? string.Empty;

		if (_currentArt != null && currentArt != null)
			SetArtBox(_currentArt, currentArt, QueueColors[0]);

		if (_nextArt1 != null && nextArt1 != null)
			SetArtBox(_nextArt1, nextArt1, QueueColors[1]);
		else
			SetArtBox(_nextArt1, null, Colors.Transparent);

		if (_nextArt2 != null && nextArt2 != null)
			SetArtBox(_nextArt2, nextArt2, QueueColors[2]);
		else
			SetArtBox(_nextArt2, null, Colors.Transparent);

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

	/// <summary>
	/// Sets the album art for a given art box.
	/// </summary>
	/// <param name="box">The border element to update.</param>
	/// <param name="img">The bitmap image to display.</param>
	/// <param name="fallback">The fallback color if no image is provided.</param>
	private static void SetArtBox(Border? box, BitmapImage? img, Color fallback)
	{
		if (img != null)
		{
			box?.Background = null;
			box?.Child = new Image
			{ Source = img, Stretch = Stretch.UniformToFill };
		}
		else
		{
			box?.Background = new SolidColorBrush(fallback);
			box?.Child = null;
		}
	}
}
