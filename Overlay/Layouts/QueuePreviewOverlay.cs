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

	// Default accent colours for queue slots
	private static readonly Color[] QueueColors = { AccentIndigo, AccentGreen, AccentOrange };

	public QueuePreviewOverlay(OverlayTheme theme)
	{
		Theme = theme;
		RootGrid = Build();
	}

	private Grid Build()
	{
		var root = new Grid
		{
			Height = TaskbarHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
		};

		var pill = MakeRectBorder(TaskbarHeight, 8);

		var inner = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			VerticalAlignment = VerticalAlignment.Center,
			Spacing = 5,
			Padding = new Thickness(6, 0, 10, 0),
		};

		// ── Diminishing art stack ───────────────────────────────────
		// Use a relative panel or just a horizontal stack with decreasing sizes
		var artStack = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			VerticalAlignment = VerticalAlignment.Center,
			Spacing = 2,
		};

		_currentArt = MakeArtBox(30, 6, QueueColors[0]);
		_currentArt.BorderBrush = new SolidColorBrush(Theme == OverlayTheme.Dark
			? Color.FromArgb(80, 255, 255, 255)
			: Color.FromArgb(80, 0, 0, 0));
		_currentArt.BorderThickness = new Thickness(1.5);

		_nextArt1 = MakeArtBox(22, 4, QueueColors[1]);
		_nextArt1.Opacity = 0.60;

		_nextArt2 = MakeArtBox(16, 3, QueueColors[2]);
		_nextArt2.Opacity = 0.35;

		artStack.Children.Add(_currentArt);
		artStack.Children.Add(_nextArt1);
		artStack.Children.Add(_nextArt2);
		inner.Children.Add(artStack);

		// ── Info ────────────────────────────────────────────────────
		var info = new StackPanel { Orientation = Orientation.Vertical, Spacing = 2, MaxWidth = 90 };
		_titleText = MakeTitleText("Superhero");
		_artistText = MakeSubText("Metro Boomin");
		info.Children.Add(_titleText);
		info.Children.Add(_artistText);
		inner.Children.Add(info);

		inner.Children.Add(MakeDivider());
		inner.Children.Add(MakePrevButton());
		inner.Children.Add(MakePlayPauseButton(16));
		inner.Children.Add(MakeNextButton());

		pill.Child = inner;
		root.Children.Add(pill);
		return root;
	}

	/// <param name="title">Current track title.</param>
	/// <param name="artist">Current track artist.</param>
	/// <param name="currentArt">Album art for the current track.</param>
	/// <param name="nextArt1">Album art for the next track in queue.</param>
	/// <param name="nextArt2">Album art for the track after that.</param>
	public void UpdateTrack(
		string title, string artist, BitmapImage? currentArt = null, BitmapImage? nextArt1 = null, BitmapImage? nextArt2 = null)
	{
		_titleText?.Text = title ?? string.Empty;
		_artistText?.Text = artist ?? string.Empty;

		if (_currentArt != null && currentArt != null)
			SetArtBox(_currentArt, currentArt, QueueColors[0]);

		if (_nextArt1 != null && nextArt1 != null)
			SetArtBox(_nextArt1, nextArt1, QueueColors[1]);

		if (_nextArt2 != null && nextArt2 != null)
			SetArtBox(_nextArt2, nextArt2, QueueColors[2]);
	}

	private static void SetArtBox(Border box, BitmapImage img, Color fallback)
	{
		if (img != null)
		{
			box.Background = null;
			box.Child = new Microsoft.UI.Xaml.Controls.Image
			{ Source = img, Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill };
		}
		else
		{
			box.Background = new SolidColorBrush(fallback);
		}
	}
}
