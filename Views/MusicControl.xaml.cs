using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace Tunetastic.Views;

/// <summary>
/// The MusicControl class provides functionality to control the playback of audio tracks.
/// It allows operations such as play, pause, stop, fast forward, rewind, and volume adjustments.
/// </summary>
public sealed partial class MusicControl : Page
{
	public MusicControl()
	{
		ViewModel = App.GetService<MusicControlViewModel>();
		InitializeComponent();

		MainPage._instance.MainPlayerPageOpened += MainPage_MainPlayerPageOpened;

		ViewModel.UpdateStoryBoard(CreateStoryBoard());
	}
	public MusicControlViewModel ViewModel
	{
		get;
	}

	/// <summary>
	/// Creates and initializes a storyboard for animating the album cover rotation.
	/// The storyboard rotates the album cover continuously in a circular motion.
	/// </summary>
	/// <returns>
	/// A <see cref="Storyboard"/> instance configured to perform a rotation animation on the album cover,
	/// or null if the animation setup failed.
	/// </returns>
	private Storyboard? CreateStoryBoard()
	{
		var rotation = new RotateTransform();
		rotation.Angle = 0;
		rotation.CenterX = 25;
		rotation.CenterY = 25;
		AlbumCover.RenderTransform = rotation;

		if (rotation is RotateTransform)
		{
			var rotateAnimation = new DoubleAnimation()
			{
				From = 0,
				To = 360,
				Duration = TimeSpan.FromSeconds(6),
				RepeatBehavior = RepeatBehavior.Forever
			};
			var storyboard = new Storyboard();
			storyboard.Children.Clear();
			storyboard.Children.Add(rotateAnimation);
			Storyboard.SetTarget(rotateAnimation, rotation);
			Storyboard.SetTargetProperty(rotateAnimation, "Angle");
			storyboard.Begin();
			return storyboard;
		}
		return null;
	}

	/// <summary>
	/// Handles the <see cref="MainPage.MainPlayerPageOpened"/> event to manage the visibility of the music control.
	/// Fades out the music control if the main player page is opened, and fades it in if it is closed.
	/// </summary>
	/// <param name="sender">
	/// The source of the event, typically the <see cref="MainPage"/> instance.
	/// </param>
	/// <param name="e">
	/// A boolean indicating whether the main player page is opened (<c>true</c>) or closed (<c>false</c>).
	/// </param>
	private void MainPage_MainPlayerPageOpened(object? sender, bool e)
	{
		if (e)
			FadeOutMusicControl();
		else
			FadeInMusicControl();
	}

	/// <summary>
	/// Animates the "TrackInfo" UI element to fade in by smoothly transitioning its opacity
	/// from 0 to 1 over a specified duration. This creates a visually appealing effect for
	/// displaying the music control overlay.
	/// </summary>
	private void FadeInMusicControl()
	{
		Storyboard storyboard = new Storyboard();
		DoubleAnimation fadeIn = new DoubleAnimation
		{
			From = 0,
			To = 1,
			Duration = TimeSpan.FromMilliseconds(500),
			EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
		};

		Storyboard.SetTarget(fadeIn, TrackInfo);
		Storyboard.SetTargetProperty(fadeIn, "Opacity");

		storyboard.Children.Add(fadeIn);
		storyboard.Begin();
	}

	/// <summary>
	/// Executes a fade-out animation on the TrackInfo UI element by transitioning its opacity
	/// from fully visible (1) to fully transparent (0).
	/// The animation uses a cubic easing function for a smooth transition over 300 milliseconds.
	/// </summary>
	private void FadeOutMusicControl()
	{
		Storyboard storyboard = new Storyboard();
		DoubleAnimation fadeOut = new DoubleAnimation
		{
			From = 1,
			To = 0,
			Duration = TimeSpan.FromMilliseconds(300),
			EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
		};

		Storyboard.SetTarget(fadeOut, TrackInfo);
		Storyboard.SetTargetProperty(fadeOut, "Opacity");

		storyboard.Children.Add(fadeOut);
		if (TrackInfo.Opacity != 0) storyboard.Begin();
	}
}
