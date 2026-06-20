using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Tunetastic.Overlay.Layouts;

// ══════════════════════════════════════════════════════════════════════
// WAVEFORM ONLY
// No text at all · animated decorative waveform bars as progress ·
// icon controls only
// ══════════════════════════════════════════════════════════════════════
public class WaveformOnlyOverlay : OverlayBase
{
	private List<Rectangle> _waveBars = new();
	private DispatcherTimer? _waveTimer;
	private static readonly Random Rng = new();

	public WaveformOnlyOverlay(OverlayTheme theme)
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

		var pill = MakeRectBorder(36, 8);

		var inner = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			VerticalAlignment = VerticalAlignment.Center,
			Spacing = 6,
			Padding = new Thickness(4, 0, 4, 0),
		};

		// Waveform zone
		var wavePanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			VerticalAlignment = VerticalAlignment.Center,
			Spacing = 2,
			Height = 28,
		};

		double[] heights = { 8, 16, 22, 14, 26, 20, 10, 8, 12, 7, 18, 14 };
		for (int i = 0; i < heights.Length; i++)
		{
			bool active = i > 3 && i < 8;
			var bar = new Rectangle
			{
				Width = 3,
				Height = heights[i],
				RadiusX = 1.5,
				RadiusY = 1.5,
				Fill = new SolidColorBrush(active
					? (Theme == OverlayTheme.Dark ? Colors.White : Windows.UI.Color.FromArgb(220, 15, 15, 20))
					: (Theme == OverlayTheme.Dark
						? Windows.UI.Color.FromArgb(55, 255, 255, 255)
						: Windows.UI.Color.FromArgb(55, 0, 0, 0))),
				VerticalAlignment = VerticalAlignment.Center,
			};
			_waveBars.Add(bar);
			wavePanel.Children.Add(bar);
		}
		inner.Children.Add(wavePanel);

		inner.Children.Add(MakeDivider());
		inner.Children.Add(MakePrevButton());
		inner.Children.Add(MakePlayPauseButton(16));
		inner.Children.Add(MakeNextButton());

		pill.Child = inner;
		root.Children.Add(pill);
		return root;
	}

	private void StartWaveAnimation()
	{
		_waveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(160) };
		_waveTimer.Tick += (_, _) =>
		{
			foreach (var bar in _waveBars)
				bar.Height = 4 + Rng.NextDouble() * 22;
		};
		_waveTimer.Start();
	}

	public void StopWaveAnimation() => _waveTimer?.Stop();
	public void ResumeWaveAnimation() => _waveTimer?.Start();
}

