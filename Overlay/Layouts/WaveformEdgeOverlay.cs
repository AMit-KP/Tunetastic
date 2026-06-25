using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace Tunetastic.Overlay.Layouts;

// ══════════════════════════════════════════════════════════════════════
// WAVEFORM EDGE
// Full-height art flush left · animated decorative waveform bars ·
// prev/play/next icon controls
// ══════════════════════════════════════════════════════════════════════
public class WaveformEdgeOverlay : OverlayBase
{
	private Border? _artBox;
	private List<Rectangle> _waveBars = new();
	private DispatcherTimer? _waveTimer;
	//private Rectangle? _progressFill; // not exposed — decorative only

	// Bar heights used for decorative animation
	private static readonly double[] BarHeights = { 8, 14, 20, 16, 24, 18, 10, 8, 12, 7 };
	private static readonly Random Rng = new();

	public WaveformEdgeOverlay(OverlayTheme theme)
	{
		Theme = theme;
		RootGrid = Build();
		StartWaveAnimation();
	}

	private Grid Build()
	{
		var root = new Grid
		{
			Height = TaskbarHeight,
			HorizontalAlignment = HorizontalAlignment.Left,
		};

		// Outer container — no left padding so art bleeds to edge
		var outer = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			VerticalAlignment = VerticalAlignment.Center,
		};

		// Full-height art (no radius on left side, slight on right)
		_artBox = new Border
		{
			Width = TaskbarHeight,
			Height = TaskbarHeight,
			CornerRadius = new CornerRadius(8, 0, 0, 8),
			Background = new SolidColorBrush(AccentOrange),
		};
		_artBox.Child = new FontIcon
		{
			Glyph = "\uEC4F",
			FontSize = 18,
			Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
		};
		outer.Children.Add(_artBox);

		// Right body
		var body = new Border
		{
			CornerRadius = new CornerRadius(0, 8, 8, 0),
			Background = new SolidColorBrush(Surface),
			BorderBrush = new SolidColorBrush(Border),
			BorderThickness = new Thickness(0.5, 0.5, 0.5, 0.5),
			Height = TaskbarHeight,
		};

		var bodyInner = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			VerticalAlignment = VerticalAlignment.Center,
			Spacing = 6,
			Padding = new Thickness(8, 0, 10, 0),
		};

		// Waveform bars
		var wavePanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			VerticalAlignment = VerticalAlignment.Center,
			Spacing = 2,
			Height = 28,
		};

		for (int i = 0; i < BarHeights.Length; i++)
		{
			bool active = i > 2 && i < 7;
			var bar = new Rectangle
			{
				Width = 3,
				Height = BarHeights[i],
				RadiusX = 1.5,
				RadiusY = 1.5,
				Fill = new SolidColorBrush(active
					? (Theme == OverlayTheme.Dark ? Colors.White : Color.FromArgb(220, 15, 15, 20))
					: (Theme == OverlayTheme.Dark
						? Color.FromArgb(60, 255, 255, 255)
						: Color.FromArgb(60, 0, 0, 0))),
				VerticalAlignment = VerticalAlignment.Center,
			};
			_waveBars.Add(bar);
			wavePanel.Children.Add(bar);
		}
		bodyInner.Children.Add(wavePanel);

		// Controls
		bodyInner.Children.Add(MakePrevButton());
		bodyInner.Children.Add(MakePlayPauseButton(16));
		bodyInner.Children.Add(MakeNextButton());

		body.Child = bodyInner;
		outer.Children.Add(body);
		root.Children.Add(outer);
		return root;
	}

	private void StartWaveAnimation()
	{
		_waveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(180) };
		_waveTimer.Tick += (_, _) =>
		{
			foreach (var bar in _waveBars)
				bar.Height = 5 + Rng.NextDouble() * 20;
		};
		_waveTimer.Start();
	}

	/// <summary>Stop the decorative wave animation (e.g. when paused).</summary>
	public void StopWaveAnimation() => _waveTimer?.Stop();

	/// <summary>Resume the decorative wave animation.</summary>
	public void ResumeWaveAnimation() => _waveTimer?.Start();

	/// <param name="art">Album art bitmap (replaces placeholder).</param>
	public void UpdateTrack(BitmapImage? art = null)
	{
		if (art != null)
		{
			_artBox?.Background = null;
			_artBox?.Child = new Microsoft.UI.Xaml.Controls.Image
			{
				Source = art,
				Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill
			};
		}
	}
}


