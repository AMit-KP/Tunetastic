using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace Tunetastic.Overlay;

/// <summary>
/// Base class for every overlay layout.
/// Exposes the root Grid to pass to SetOverlayContent(),
/// plus common button references and shared control methods.
/// </summary>
public abstract class OverlayBase
{
	// ── Public surface ────────────────────────────────────────────────

	/// <summary>Pass this to your SetOverlayContent() call.</summary>
	public Grid? RootGrid { get; protected set; }

	/// <summary>Which theme this instance was created with.</summary>
	public OverlayTheme Theme { get; protected set; }

	// Common button references — not every layout uses all of them.
	// Null-check before subscribing if you are unsure.
	public Button? PlayPauseButton { get; protected set; }
	public Button? PreviousButton { get; protected set; }
	public Button? NextButton { get; protected set; }

	// ── Play/Pause icon toggle ────────────────────────────────────────

	private bool _isPlaying = false;

	/// <summary>
	/// Call this from outside to flip the play/pause icon.
	/// e.g. overlay.SetPlayingState(player.IsPlaying);
	/// </summary>
	public void SetPlayingState(bool isPlaying)
	{
		_isPlaying = isPlaying;
		if (PlayPauseButton?.Content is FontIcon icon)
		{
			icon.Glyph = isPlaying ? "\uE769" : "\uE768"; // Pause : Play
			ToolTipService.SetToolTip(PlayPauseButton, isPlaying ? "Pause" : "Play");
		}
	}

	// ── Progress update — layouts that have a progress indicator ──────

	/// <summary>
	/// Override in layouts that expose a progress bar or stripe.
	/// value is 0.0 – 1.0.
	/// </summary>
	public virtual void UpdateProgress(double value) { }

	// ── Abstract track update — each layout defines its own params ────

	// Layouts call their own strongly-typed UpdateTrack() overload.
	// No single base signature is enforced here because parameters
	// differ per layout (see each layout class).

	// ── Protected helpers ─────────────────────────────────────────────

	protected static Color DarkBg => Color.FromArgb(255, 32, 32, 40);
	protected static Color DarkSurface => Color.FromArgb(180, 48, 48, 60);
	protected static Color DarkBorder => Color.FromArgb(40, 255, 255, 255);
	protected static Color DarkText => Color.FromArgb(255, 255, 255, 255);
	protected static Color DarkSubText => Color.FromArgb(155, 255, 255, 255);
	protected static Color DarkDivider => Color.FromArgb(38, 255, 255, 255);

	protected static Color LightBg => Color.FromArgb(255, 243, 243, 248);
	protected static Color LightSurface => Color.FromArgb(200, 255, 255, 255);
	protected static Color LightBorder => Color.FromArgb(60, 0, 0, 0);
	protected static Color LightText => Color.FromArgb(255, 15, 15, 20);
	protected static Color LightSubText => Color.FromArgb(155, 15, 15, 20);
	protected static Color LightDivider => Color.FromArgb(38, 0, 0, 0);

	protected Color Bg => Theme == OverlayTheme.Dark ? DarkBg : LightBg;
	protected Color Surface => Theme == OverlayTheme.Dark ? DarkSurface : LightSurface;
	protected Color Border => Theme == OverlayTheme.Dark ? DarkBorder : LightBorder;
	protected Color Text => Theme == OverlayTheme.Dark ? DarkText : LightText;
	protected Color SubText => Theme == OverlayTheme.Dark ? DarkSubText : LightSubText;
	protected Color Divider => Theme == OverlayTheme.Dark ? DarkDivider : LightDivider;

	protected Color ProgressTrack => Theme == OverlayTheme.Dark
		? Color.FromArgb(30, 255, 255, 255)
		: Color.FromArgb(40, 0, 0, 0);

	/// <summary>Taskbar height constant — 48px.</summary>
	protected const double TaskbarHeight = 48d;

	// ── Factory helpers ───────────────────────────────────────────────

	/// <summary>Creates a standard icon button with a Segoe Fluent Icon glyph.</summary>
	protected Button MakeIconButton(string glyph, double size = 14, Color? foreground = null)
	{
		var fg = foreground ?? (Theme == OverlayTheme.Dark
			? Color.FromArgb(153, 255, 255, 255)
			: Color.FromArgb(153, 0, 0, 0));

		return new Button
		{
			Content = new FontIcon
			{
				Glyph = glyph,
				FontSize = size,
				Foreground = new SolidColorBrush(fg)
			},
			Background = new SolidColorBrush(Colors.Transparent),
			BorderBrush = new SolidColorBrush(Colors.Transparent),
			BorderThickness = new Thickness(0),
			Padding = new Thickness(4),
			MinWidth = 0,
			MinHeight = 0,
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center,
		};
	}

	/// <summary>Creates the play/pause button and stores it in PlayPauseButton.</summary>
	protected Button MakePlayPauseButton(double size = 16, Color? foreground = null)
	{
		var fg = foreground ?? (Theme == OverlayTheme.Dark
			? Colors.White
			: Color.FromArgb(255, 15, 15, 20));

		PlayPauseButton = MakeIconButton("\uE768", size, fg); // Play glyph default
		ToolTipService.SetToolTip(PlayPauseButton, "Play");
		return PlayPauseButton;
	}

	/// <summary>Creates the previous button and stores it in PreviousButton.</summary>
	protected Button MakePrevButton(double size = 13, Color? foreground = null)
	{
		PreviousButton = MakeIconButton("\uE892", size, foreground); // Previous track
		ToolTipService.SetToolTip(PreviousButton, "Previous song/track");
		return PreviousButton;
	}

	/// <summary>Creates the next button and stores it in NextButton.</summary>
	protected Button MakeNextButton(double size = 13, Color? foreground = null)
	{
		NextButton = MakeIconButton("\uE893", size, foreground); // Next track
		ToolTipService.SetToolTip(NextButton, "Next song/track");
		return NextButton;
	}

	/// <summary>Creates a TextBlock styled as a track title.</summary>
	protected TextBlock MakeTitleText(string text = "Track Title", double maxWidth = 100)
	{
		return new TextBlock
		{
			Text = text,
			FontSize = 11,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = new SolidColorBrush(Text),
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalTextAlignment = TextAlignment.Left,
			MaxWidth = maxWidth,
			TextTrimming = TextTrimming.CharacterEllipsis,
		};
	}

	/// <summary>Creates a TextBlock styled as an artist / subtitle.</summary>
	protected TextBlock MakeSubText(string text = "Artist Name", double maxWidth = 100)
	{
		return new TextBlock
		{
			Text = text,
			FontSize = 9,
			Foreground = new SolidColorBrush(SubText),
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalTextAlignment = TextAlignment.Left,
			MaxWidth = maxWidth,
			TextTrimming = TextTrimming.CharacterEllipsis,
		};
	}

	/// <summary>Thin vertical divider line.</summary>
	protected Rectangle MakeDivider()
	{
		return new Rectangle
		{
			Width = 1,
			Height = 16,
			Fill = new SolidColorBrush(Divider),
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(2, 0, 2, 0)
		};
	}

	/// <summary>Rounded album art placeholder square.</summary>
	protected Border MakeArtBox(double size, double radius, Color accentColor, BitmapImage? image = null)
	{
		var border = new Border
		{
			Width = size,
			Height = size,
			CornerRadius = new CornerRadius(radius),
			Background = new SolidColorBrush(accentColor),
			VerticalAlignment = VerticalAlignment.Center,
		};

		if (image != null)
		{
			border.Child = new Microsoft.UI.Xaml.Controls.Image
			{
				Source = image,
				Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill
			};
		}
		else
		{
			// Placeholder music note icon
			border.Child = new FontIcon
			{
				Glyph = "\uEC4F",
				FontSize = size * 0.4,
				Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
			};
		}

		return border;
	}

	/// <summary>Creates a pill-shaped container with surface background and border.</summary>
	protected Border MakePillBorder(double height = 36, double radius = 24)
	{
		return new Border
		{
			Height = height,
			CornerRadius = new CornerRadius(radius),
			Background = new SolidColorBrush(Surface),
			BorderBrush = new SolidColorBrush(Border),
			BorderThickness = new Thickness(0.5),
			VerticalAlignment = VerticalAlignment.Center,
			Padding = new Thickness(6, 0, 10, 0),
		};
	}

	/// <summary>Creates a rectangular pill container.</summary>
	protected Border MakeRectBorder(double height = 36, double radius = 8)
	{
		return new Border
		{
			Height = height,
			CornerRadius = new CornerRadius(radius),
			Background = new SolidColorBrush(Surface),
			BorderBrush = new SolidColorBrush(Border),
			BorderThickness = new Thickness(0.5),
			VerticalAlignment = VerticalAlignment.Center,
			Padding = new Thickness(8, 0, 10, 0),
		};
	}

	/// <summary>
	/// Fades element in. startOpacity → 1.0 over durationMs.
	/// </summary>
	protected static void FadeIn(UIElement element, double durationMs = 150, double startOpacity = 0)
	{
		element.Opacity = startOpacity;
		var anim = new DoubleAnimation
		{
			From = startOpacity,
			To = 1.0,
			Duration = new Duration(TimeSpan.FromMilliseconds(durationMs)),
			EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
		};
		Storyboard.SetTarget(anim, element);
		Storyboard.SetTargetProperty(anim, "Opacity");
		var sb = new Storyboard();
		sb.Children.Add(anim);
		sb.Begin();
	}

	/// <summary>
	/// Fades element out. 1.0 → 0 over durationMs.
	/// </summary>
	protected static void FadeOut(UIElement element, double durationMs = 150)
	{
		var anim = new DoubleAnimation
		{
			From = element.Opacity,
			To = 0,
			Duration = new Duration(TimeSpan.FromMilliseconds(durationMs)),
			EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
		};
		Storyboard.SetTarget(anim, element);
		Storyboard.SetTargetProperty(anim, "Opacity");
		var sb = new Storyboard();
		sb.Children.Add(anim);
		sb.Begin();
	}

	/// <summary>Default accent colour used for art placeholder boxes.</summary>
	protected static Color AccentPurple => Color.FromArgb(255, 127, 119, 221);
	protected static Color AccentGreen => Color.FromArgb(255, 29, 158, 117);
	protected static Color NeonGreen => Color.FromArgb(255, 57, 255, 20);
	protected static Color AccentOrange => Color.FromArgb(255, 216, 90, 48);
	protected static Color AccentPink => Color.FromArgb(255, 212, 83, 126);
	protected static Color AccentBlue => Color.FromArgb(255, 55, 138, 221);
	protected static Color AccentGold => Color.FromArgb(255, 186, 117, 23);
	protected static Color AccentTeal => Color.FromArgb(255, 15, 110, 86);
	protected static Color AccentIndigo => Color.FromArgb(255, 83, 74, 183);
	protected static Color AccentRose => Color.FromArgb(255, 153, 53, 86);
}
