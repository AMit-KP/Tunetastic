using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Media.Playback;

namespace Tunetastic.Views;

/// <summary>
/// The MusicControl class provides functionality to control the playback of audio tracks.
/// It allows operations such as play, pause, stop, fast forward, rewind, and volume adjustments.
/// </summary>
public sealed partial class MusicControl : Page
{
	public static MusicControl? _instance;
	public MusicControl()
	{
		ViewModel = App.GetService<MusicControlViewModel>();
		InitializeComponent();

		_instance = this;

		MainPage._instance.MainPlayerPageOpened += FloatingPlayer;
		VinylEffectStoryBoard();
	}

	/// <summary>
	/// Configures and starts the storyboard animation for spinning the vinyl record effect.
	/// This method initializes the rotation animation and syncs its state with the playback status.
	/// </summary>
	private void VinylEffectStoryBoard()
	{
		var storyboard = CreateStoryBoard();
		ViewModel.UpdateStoryBoard(storyboard);
		if (MusicPlayer.Instance.MediaPlayer.PlaybackSession.PlaybackState != MediaPlaybackState.Playing) storyboard?.Pause();
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
	public void FloatingPlayer(object? sender, bool e)
	{
		if (e || ViewModel.Title == "Please select a song")
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
		if (TrackInfo.Opacity != 1) storyboard.Begin();
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
			Duration = TimeSpan.FromMilliseconds(200),
			EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
		};

		Storyboard.SetTarget(fadeOut, TrackInfo);
		Storyboard.SetTargetProperty(fadeOut, "Opacity");

		storyboard.Children.Add(fadeOut);
		if (TrackInfo.Opacity != 0) storyboard.Begin();
	}

	/// <summary>
	/// Initiates an animation that slides the song information panel into view
	/// from the top and fades it in simultaneously. This method checks the
	/// current opacity of the panel and applies appropriate storyboard animations
	/// for both the vertical translation and opacity change.
	/// </summary>
	public async void SlideInDown()
	{
		if (TrackInfo.Opacity == 1)
		{
			SongInfoTransform.Y = -100;
			TrackInfo.Opacity = 0;
			Storyboard storyboard = new Storyboard();
			DoubleAnimation slideRight = new DoubleAnimation
			{
				To = 0,
				Duration = TimeSpan.FromMilliseconds(400),
				EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
			};

			DoubleAnimation fadeIn = new DoubleAnimation
			{
				From = 0,
				To = 1,
				Duration = TimeSpan.FromMilliseconds(400),
				EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
			};

			Storyboard.SetTarget(slideRight, SongInfoTransform);
			Storyboard.SetTargetProperty(slideRight, "Y");
			storyboard.Children.Add(slideRight);
			Storyboard.SetTarget(fadeIn, TrackInfo);
			Storyboard.SetTargetProperty(fadeIn, "Opacity");
			storyboard.Children.Add(fadeIn);
			storyboard.Begin();
		}
	}
}
