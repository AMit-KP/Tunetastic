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

	/// <summary>
	/// Gets or sets the root Grid that should be passed to SetOverlayContent().
	/// </summary>
	public Grid? RootGrid { get; protected set; }

	/// <summary>
	/// Gets or sets the theme this instance was created with.
	/// </summary>
	public OverlayTheme Theme { get; protected set; }

	// Common button references — not every layout uses all of them.
	// Null-check before subscribing if you are unsure.
	
	/// <summary>
	/// Gets or sets the play/pause button reference.
	/// </summary>
	public Button? PlayPauseButton { get; protected set; }
	
	/// <summary>
	/// Gets or sets the previous button reference.
	/// </summary>
	public Button? PreviousButton { get; protected set; }
	
	/// <summary>
	/// Gets or sets the next button reference.
	/// </summary>
	public Button? NextButton { get; protected set; }

	// ── Play/Pause icon toggle ────────────────────────────────────────

	private bool _isPlaying = false;

	/// <summary>
	/// Sets the play/pause state and updates the button icon accordingly.
	/// Call this from outside to flip the play/pause icon.
	/// e.g. overlay.SetPlayingState(player.IsPlaying);
	/// </summary>
	/// <param name="isPlaying">True if playing, false if paused.</param>
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
	/// Updates the progress indicator in layouts that expose one.
	/// Override in layouts that expose a progress bar or stripe.
	/// value is 0.0 – 1.0.
	/// </summary>
	/// <param name="value">The progress value between 0.0 and 1.0.</param>
	public virtual void UpdateProgress(double value) { }

	// ── Abstract track update — each layout defines its own params ────

	// Layouts call their own strongly-typed UpdateTrack() overload.
	// No single base signature is enforced here because parameters
	// differ per layout (see each layout class).

	// ── Protected helpers ─────────────────────────────────────────────

	/// <summary>
	/// Gets the dark background color.
	/// </summary>
	protected static Color DarkBg => Color.FromArgb(255, 32, 32, 40);
	
	/// <summary>
	/// Gets the dark surface color.
	/// </summary>
	protected static Color DarkSurface => Color.FromArgb(180, 48, 48, 60);
	
	/// <summary>
	/// Gets the dark border color.
	/// </summary>
	protected static Color DarkBorder => Color.FromArgb(40, 255, 255, 255);
	
	/// <summary>
	/// Gets the dark text color.
	/// </summary>
	protected static Color DarkText => Color.FromArgb(255, 255, 255, 255);
	
	/// <summary>
	/// Gets the dark sub-text color.
	/// </summary>
	protected static Color DarkSubText => Color.FromArgb(155, 255, 255, 255);
	
	/// <summary>
	/// Gets the dark divider color.
	/// </summary>
	protected static Color DarkDivider => Color.FromArgb(38, 255, 255, 255);

	/// <summary>
	/// Gets the light background color.
	/// </summary>
	protected static Color LightBg => Color.FromArgb(255, 243, 243, 248);
	
	/// <summary>
	/// Gets the light surface color.
	/// </summary>
	protected static Color LightSurface => Color.FromArgb(200, 255, 255, 255);
	
	/// <summary>
	/// Gets the light border color.
	/// </summary>
	protected static Color LightBorder => Color.FromArgb(60, 0, 0, 0);
	
	/// <summary>
	/// Gets the light text color.
	/// </summary>
	protected static Color LightText => Color.FromArgb(255, 15, 15, 20);
	
	/// <summary>
	/// Gets the light sub-text color.
	/// </summary>
	protected static Color LightSubText => Color.FromArgb(155, 15, 15, 20);
	
	/// <summary>
	/// Gets the light divider color.
	/// </summary>
	protected static Color LightDivider => Color.FromArgb(38, 0, 0, 0);

	/// <summary>
	/// Gets the background color based on the current theme.
	/// </summary>
	protected Color Bg => Theme == OverlayTheme.Dark ? DarkBg : LightBg;
	
	/// <summary>
	/// Gets the surface color based on the current theme.
	/// </summary>
	protected Color Surface => Theme == OverlayTheme.Dark ? DarkSurface : LightSurface;
	
	/// <summary>
	/// Gets the border color based on the current theme.
	/// </summary>
	protected Color Border => Theme == OverlayTheme.Dark ? DarkBorder : LightBorder;
	
	/// <summary>
	/// Gets the text color based on the current theme.
	/// </summary>
	protected Color Text => Theme == OverlayTheme.Dark ? DarkText : LightText;
	
	/// <summary>
	/// Gets the sub-text color based on the current theme.
	/// </summary>
	protected Color SubText => Theme == OverlayTheme.Dark ? DarkSubText : LightSubText;
	
	/// <summary>
	/// Gets the divider color based on the current theme.
	/// </summary>
	protected Color Divider => Theme == OverlayTheme.Dark ? DarkDivider : LightDivider;

	/// <summary>
	/// Gets the progress track color based on the current theme.
	/// </summary>
	protected Color ProgressTrack => Theme == OverlayTheme.Dark
		? Color.FromArgb(30, 255, 255, 255)
		: Color.FromArgb(40, 0, 0, 0);

	// ── Factory helpers ───────────────────────────────────────────────

	/// <summary>
	/// Creates a standard icon button with a Segoe Fluent Icon glyph.
	/// </summary>
	/// <param name="glyph">The Segoe Fluent Icon glyph to use.</param>
	/// <param name="size">The font size of the icon. Default is 14.</param>
	/// <param name="foreground">The foreground color of the icon. Default is null (uses theme default).</param>
	/// <returns>A configured Button control with the specified icon.</returns>
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

	/// <summary>
	/// Creates the play/pause button and stores it in PlayPauseButton.
	/// </summary>
	/// <param name="size">The font size of the icon. Default is 16.</param>
	/// <param name="foreground">The foreground color of the icon. Default is null (uses theme default).</param>
	/// <returns>The created Button control.</returns>
	protected Button MakePlayPauseButton(double size = 16, Color? foreground = null)
	{
		var fg = foreground ?? (Theme == OverlayTheme.Dark
			? Colors.White
			: Color.FromArgb(255, 15, 15, 20));

		PlayPauseButton = MakeIconButton("\uE768", size, fg); // Play glyph default
		ToolTipService.SetToolTip(PlayPauseButton, "Play");
		return PlayPauseButton;
	}

	/// <summary>
	/// Creates the previous button and stores it in PreviousButton.
	/// </summary>
	/// <param name="size">The font size of the icon. Default is 13.</param>
	/// <param name="foreground">The foreground color of the icon. Default is null (uses theme default).</param>
	/// <returns>The created Button control.</returns>
	protected Button MakePrevButton(double size = 13, Color? foreground = null)
	{
		PreviousButton = MakeIconButton("\uE892", size, foreground); // Previous track
		ToolTipService.SetToolTip(PreviousButton, "Previous song/track");
		return PreviousButton;
	}

	/// <summary>
	/// Creates the next button and stores it in NextButton.
	/// </summary>
	/// <param name="size">The font size of the icon. Default is 13.</param>
	/// <param name="foreground">The foreground color of the icon. Default is null (uses theme default).</param>
	/// <returns>The created Button control.</returns>
	protected Button MakeNextButton(double size = 13, Color? foreground = null)
	{
		NextButton = MakeIconButton("\uE893", size, foreground); // Next track
		ToolTipService.SetToolTip(NextButton, "Next song/track");
		return NextButton;
	}

	/// <summary>
	/// Creates a TextBlock styled as a track title.
	/// </summary>
	/// <param name="text">The text content. Default is "Track Title".</param>
	/// <param name="maxWidth">The maximum width of the text block. Default is 100.</param>
	/// <returns>A configured TextBlock control.</returns>
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

	/// <summary>
	/// Creates a TextBlock styled as an artist / subtitle.
	/// </summary>
	/// <param name="text">The text content. Default is "Artist Name".</param>
	/// <param name="maxWidth">The maximum width of the text block. Default is 100.</param>
	/// <returns>A configured TextBlock control.</returns>
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

	/// <summary>
	/// Creates a thin vertical divider line.
	/// </summary>
	/// <returns>A configured Rectangle control.</returns>
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

	/// <summary>
	/// Creates a rounded album art placeholder square.
	/// </summary>
	/// <param name="size">The size of the border.</param>
	/// <param name="radius">The corner radius.</param>
	/// <param name="accentColor">The accent color for the background.</param>
	/// <param name="image">Optional bitmap image to display in the placeholder.</param>
	/// <returns>A configured Border control.</returns>
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

	/// <summary>
	/// Creates a pill-shaped container with surface background and border.
	/// </summary>
	/// <param name="height">The height of the border. Default is 36.</param>
	/// <param name="radius">The corner radius. Default is 24.</param>
	/// <returns>A configured Border control.</returns>
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

	/// <summary>
	/// Creates a rectangular pill container.
	/// </summary>
	/// <param name="height">The height of the border. Default is 36.</param>
	/// <param name="radius">The corner radius. Default is 8.</param>
	/// <returns>A configured Border control.</returns>
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
	/// <param name="element">The UI element to fade in.</param>
	/// <param name="durationMs">The duration of the animation in milliseconds. Default is 150.</param>
	/// <param name="startOpacity">The starting opacity value. Default is 0.</param>
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
	/// <param name="element">The UI element to fade out.</param>
	/// <param name="durationMs">The duration of the animation in milliseconds. Default is 150.</param>
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

	/// <summary>
	/// Gets the default accent purple color.
	/// </summary>
	protected static Color AccentPurple => Color.FromArgb(255, 127, 119, 221);
	
	/// <summary>
	/// Gets the default accent green color.
	/// </summary>
	protected static Color AccentGreen => Color.FromArgb(255, 29, 158, 117);
	
	/// <summary>
	/// Gets the neon green color.
	/// </summary>
	protected static Color NeonGreen => Color.FromArgb(255, 57, 255, 20);
	
	/// <summary>
	/// Gets the default accent orange color.
	/// </summary>
	protected static Color AccentOrange => Color.FromArgb(255, 216, 90, 48);
	
	/// <summary>
	/// Gets the default accent pink color.
	/// </summary>
	protected static Color AccentPink => Color.FromArgb(255, 212, 83, 126);
	
	/// <summary>
	/// Gets the default accent blue color.
	/// </summary>
	protected static Color AccentBlue => Color.FromArgb(255, 55, 138, 221);
	
	/// <summary>
	/// Gets the default accent gold color.
	/// </summary>
	protected static Color AccentGold => Color.FromArgb(255, 186, 117, 23);
	
	/// <summary>
	/// Gets the default accent teal color.
	/// </summary>
	protected static Color AccentTeal => Color.FromArgb(255, 15, 110, 86);
	
	/// <summary>
	/// Gets the default accent indigo color.
	/// </summary>
	protected static Color AccentIndigo => Color.FromArgb(255, 83, 74, 183);
	
	/// <summary>
	/// Gets the default accent rose color.
	/// </summary>
	protected static Color AccentRose => Color.FromArgb(255, 153, 53, 86);
}
