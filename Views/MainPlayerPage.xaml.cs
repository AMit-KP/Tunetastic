using System.Text.RegularExpressions;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Tunetastic.Views.LibraryViews;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Tunetastic.Views;

/// <summary>
/// Represents the main player page within the application.
/// This page is primarily responsible for displaying music playback and interacting with the music player.
/// </summary>
/// <remarks>
/// The <c>MainPlayerPage</c> class is registered as one of the navigation pages in the <c>NavigationPageMappings</c>.
/// It initializes the music player's song list and handles events related to song changes.
/// This page leverages a DispatcherQueue for managing UI updates.
/// </remarks>
public sealed partial class MainPlayerPage : Page
{
	private readonly MusicPlayer _musicPlayer = MusicPlayer.Instance;
	private readonly DispatcherQueue _dispatcherQueue;
	BitmapImage? BGbitmapImage = null;
	private double pageHeight = 0;
	private double pageWidth = 0;
	private double coverArtAspectRatio = 1.0;
	private double coverArtImagePixelWidth = 500;
	private double coverArtImagePixelHeight = 500;
	private bool _isTilted = false;
	private double lyricsTargetWidth;
	private double lyricsTargetHeight;
	private bool lyricsVisible = false;
	private string? lyricsText = null;

	// Synced lyrics fields
	private List<LrcLine> _lines = new();
	private List<Button> _lyricButtons = new();
	private int _activeIndex = -1;
	private DispatcherTimer? _lrcTimer;
	private long _lastKnownTicks = 0;
	private bool _centeringPaddingSet = false;
	private string? _externalLrcPath;
	private const double SyncControlsGap = 30;


	/// <summary>
	/// True if the currently displayed lyrics came from an external .lrc file.
	/// Useful for hiding/showing menu buttons.
	/// </summary>
	public bool HasExternalLrcFile => !string.IsNullOrEmpty(_externalLrcPath);

	public MainPlayerPage()
	{
		this.InitializeComponent();
		this.NavigationCacheMode = NavigationCacheMode.Required;

		BlurEffect.Amount = 50 + (double.Parse(Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.MainPlayerBGBlurValue)]?.ToString() ?? "5") * 10);
		_dispatcherQueue = DispatcherQueue.GetForCurrentThread();
		_musicPlayer.CurrentSongChanged -= OnCurrentSongChanged;
		_musicPlayer.CurrentSongChanged += OnCurrentSongChanged;
	}

	/// <summary>
	/// Handles the event triggered when the current song in the music player changes.
	/// This method updates the UI to reflect changes related to the new song.
	/// </summary>
	/// <param name="sender">The source of the event. This parameter is optional and can be null.</param>
	/// <param name="e">The string containing information about the new song, such as its identifier or name.</param>
	private void OnCurrentSongChanged(object? sender, string e)
	{
		_dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, async () =>
		{
			await Task.Delay(200);
			await UpdateUI(false);
		});
	}

	/// <summary>
	/// Updates the UI elements of the main player page to reflect the current song and player state.
	/// This method modifies <see cref="SystemMediaTransportControls"/> and various UI components such as the background image, cover art, and the title based on the current song.
	/// </summary>
	/// <param name="notify">A boolean value indicating whether a notification should be displayed after updating the UI. Default is true.</param>
	/// <returns>A <see cref="Task"/> that represents the asynchronous operation of updating the UI.</returns>
	private async Task<Task> UpdateUI(bool notify = true, bool backNavigation = false)
	{
		try
		{
			HideLyricsAndResetCoverArt();
		}
		catch (Exception)
		{ }

		CleanupSyncedLyrics();

		try
		{
			if (await DatabaseHelper.Instance.GetSongsCount() != 0)
			{
				var songPath = _musicPlayer.CurrentSong;
				if (!string.IsNullOrEmpty(songPath))
				{
					var track = await DatabaseHelper.Instance.GetSongByPath(songPath);
					if (track != null && File.Exists(track?.Path))
					{
						Title.Text = track?.Title;
						ToolTipService.SetToolTip(Title, track?.Title);

						Album.Text = track?.Album;
						ToolTipService.SetToolTip(Album, track?.Album);

						Artist.Text = track?.Artists;
						ToolTipService.SetToolTip(Artist, track?.Artists);

						Title.FontSize = Album.FontSize * 1.5;
						Artist.FontSize = Album.FontSize * 1.1;

						var thumbnailFilePath = Path.Combine(Constants.ThumbnailsFolder, ThumbnailFolder.MainPlayer.ToString(), Path.GetFileName(track?.Cover ?? ""));
						if (!string.IsNullOrEmpty(track?.Cover) && !File.Exists(thumbnailFilePath))
						{
							using var audioModel = TagLib.File.Create(track?.Path);
							ImageResizer.CreateThumbnailImage(ThumbnailFolder.MainPlayer, audioModel.Tag.Pictures, Path.GetFileName(track?.Cover ?? ""));
						}

						if (File.Exists(thumbnailFilePath))
						{
							StorageFile file = await StorageFile.GetFileFromPathAsync(thumbnailFilePath);
							using IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read);
							BitmapImage bitmapImage = new BitmapImage();
							await bitmapImage.SetSourceAsync(stream);

							BGbitmapImage = bitmapImage;
							if (!backNavigation)
								BackgroundImage.Source = bitmapImage;

							coverArtImagePixelWidth = bitmapImage.PixelWidth;
							coverArtImagePixelHeight = bitmapImage.PixelHeight;
							coverArtAspectRatio = coverArtImagePixelWidth > 0 && coverArtImagePixelHeight > 0 ? (double)coverArtImagePixelWidth / coverArtImagePixelHeight : 1.0;

							CoverArtImage.Source = bitmapImage;
						}

						if ((Title.Text + "\n" + Artist.Text).Length > 128)
							App.TrayIcon?.Tooltip = Title.Text;
						else
							App.TrayIcon?.Tooltip = Title.Text + "\n" + Artist.Text;

						if (MusicPlayer.Instance.SMTC != null)
						{
							var updater = MusicPlayer.Instance.SMTC.DisplayUpdater;
							updater.Type = MediaPlaybackType.Music;
							if (!string.IsNullOrEmpty(track?.Cover))
								updater.Thumbnail = RandomAccessStreamReference.CreateFromFile(await StorageFile.GetFileFromPathAsync(track?.Cover));
							updater.MusicProperties.Title = Title.Text;
							updater.MusicProperties.Artist = Artist.Text;
							updater.MusicProperties.AlbumTitle = Album.Text;
							updater.MusicProperties.AlbumArtist = Artist.Text;
							updater.MusicProperties.AlbumTrackCount = 1;
							updater.Update();
						}

						UpdateCoverArtSize();
						MusicInfoButton.Visibility = Visibility.Visible;
						ShowLyricsButton.Visibility = string.IsNullOrEmpty(track?.Lyrics) ? Visibility.Collapsed : Visibility.Visible;
						LyricMenuOptions(embeddedLyrics: true);
						lyricsText = track?.Lyrics;

						if (!LrcParser.IsSyncedLyrics(lyricsText))
						{
							var externalContent = TryLoadExternalLrc(songPath);
							if (externalContent != null)
							{
								_externalLrcPath = Path.ChangeExtension(songPath, ".lrc");
								lyricsText = externalContent;
								ShowLyricsButton.Visibility = Visibility.Visible;
								LyricMenuOptions(embeddedLyrics: false);
								GlobalNotification.Info("Discovered external .lrc file:\n" + _externalLrcPath);
							}
							else
							{
								_externalLrcPath = null;
							}
						}
						else
						{
							_externalLrcPath = null;
						}

						return Task.CompletedTask;
					}
					else
					{
						if (notify)
							GlobalNotification.Error("Could not find track/song:\n" + songPath);
					}
				}
			}
			else
			{
				if (notify)
					GlobalNotification.Error("No tracks/songs found in library.");
			}
		}
		catch (Exception)
		{
			if (notify)
				GlobalNotification.Error("Could not load track/song.");
		}

		if (MusicPlayer.Instance.SMTC != null)
			MusicPlayer.Instance.SMTC.DisplayUpdater.ClearAll();
		BGbitmapImage = new BitmapImage(new Uri("ms-appx:///Assets/AppIcon.png"));
		if (!backNavigation)
			BackgroundImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/AppIcon.png"));
		coverArtAspectRatio = 1.0;
		coverArtImagePixelWidth = 500;
		coverArtImagePixelHeight = 500;
		UpdateCoverArtSize();
		CoverArtImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/AppIcon.png"));
		Title.Text = "Please select a song";
		MusicInfoButton.Visibility = Visibility.Collapsed;
		_externalLrcPath = null;
		return Task.CompletedTask;
	}

	/// <summary>
	/// Updates the dimensions of the cover art on the main player page.
	/// This method adjusts the width, height, and corner radius of the cover art based on the current page height and the aspect ratio of the cover art.
	/// </summary>
	private void UpdateCoverArtSize()
	{
		double availableHeight = pageHeight == 0 ? 500 : pageHeight;
		double targetHeight = availableHeight * 0.55;
		double targetWidth = coverArtAspectRatio * targetHeight;

		CoverArt.Width = targetWidth;
		CoverArt.Height = targetHeight;
		CoverArt.CornerRadius = new CornerRadius(targetHeight / 8);
	}

	private void UpdateLyricsGrid()
	{
		if (pageWidth == 0 || pageHeight == 0) return;
		if (!lyricsVisible) return;

		CalculateLyricsLayout(out double left, out double top, out double width, out double height);
		lyricsTargetWidth = width;
		lyricsTargetHeight = height;
		LyricsDisplay.Width = lyricsTargetWidth;
		LyricsDisplay.Height = lyricsTargetHeight;
		Canvas.SetLeft(LyricsDisplay, left);
		Canvas.SetTop(LyricsDisplay, top);
		PositionSyncControls(left, top, width);

		var visual = ElementCompositionPreview.GetElementVisual(LyricsDisplay);
		visual.Clip = compositor.CreateInsetClip(0, 0, 0, 0);
	}


	/// <summary>
	/// Executes tasks when navigation to the MainPlayerPage occurs.
	/// This method ensures the page's UI components and data are initialized or updated appropriately based on the navigation event context.
	/// </summary>
	/// <param name="e">The navigation event arguments containing details such as navigation mode and parameter data.</param>
	protected override async void OnNavigatedTo(NavigationEventArgs e)
	{
		BlurEffect.Amount = 50 + (double.Parse(Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.MainPlayerBGBlurValue)]?.ToString() ?? "5") * 10);
		BackgroundImage.Opacity = 0;
		await UpdateUI(backNavigation: e.NavigationMode == NavigationMode.Back);
		if (e.NavigationMode == NavigationMode.Back)
		{
			await Task.Delay(500);
			BackgroundImage.Source = BGbitmapImage;
			await BackgroundImage.AnimateDoublePropertyAsync("Opacity", 0, 1, 3000);
		}
		else
			BackgroundImage.Source = BGbitmapImage;

		BackgroundImage.Opacity = 1;
		base.OnNavigatedTo(e);
	}

	/// <summary>
	/// Executes tasks when navigation away from the MainPlayerPage occurs.
	/// This method clears resources and resets the page state to improve performance and free up memory.
	/// </summary>
	/// <param name="e">Provides data for the navigation event, including mode and parameters associated with the navigation request.</param>
	protected override void OnNavigatedFrom(NavigationEventArgs e)
	{
		BGbitmapImage = null;
		BackgroundImage.Source = null;
	}

	/// <summary>
	/// Handles the event that occurs when the size of the page is changed.
	/// This method updates layout-related properties and refreshes the UI accordingly.
	/// </summary>
	/// <param name="sender">The source of the event. Typically, this is the page whose size has changed.</param>
	/// <param name="e">The event data containing information about the new size of the page.</param>
	private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		pageWidth = e.NewSize.Width;
		pageHeight = e.NewSize.Height;
		UpdateCoverArtSize();
		UpdateLyricsGrid();

		if (_lyricButtons.Count > 0 && _centeringPaddingSet)
			SetCenteringPadding();
	}

	/// <summary>
	/// Handles the tap event on the music details section.
	/// Based on the current playlist or grouping, this method navigates the application to the appropriate detail or playlist page.
	/// </summary>
	/// <param name="sender">The source of the event, typically the UI element.</param>
	/// <param name="e">The event data containing details about the tap interaction.</param>
	private void MusicDetails_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
	{
		try
		{
			switch (Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CurrentPlayinglist)]?.ToString())
			{
				case "MostPlayed":
					App.Current.NavService.EnsureNavigationSelection("Tunetastic.Views.PlaylistViews.MostPlayed");
					break;

				case "RecentlyPlayed":
					App.Current.NavService.EnsureNavigationSelection("Tunetastic.Views.PlaylistViews.RecentlyPlayed");
					break;

				case "RecentlyAdded":
					App.Current.NavService.EnsureNavigationSelection("Tunetastic.Views.PlaylistViews.RecentlyAdded");
					break;

				case var playlist when playlist?.StartsWith("CustomPlaylist__") == true:
					App.Current.NavService.EnsureNavigationSelection("Tunetastic.Views.PlaylistViews." + Regex.Replace(playlist.Substring("CustomPlaylist__".Length), @"\s+", "_") + "CustomPlaylist");
					break;

				case var artist when artist?.StartsWith("ArtistGroup>") == true:
					App.Current.NavService.EnsureNavigationSelection("Tunetastic.Views.LibraryViews.ArtistsViewPage");
					App.Current.NavService.NavigateTo(typeof(ArtistDetailPage), artist?.Substring("ArtistGroup>".Length) == "Unknown" ? "Unknown Artist" : artist?.Substring("ArtistGroup>".Length), false);
					break;

				case var album when album?.StartsWith("AlbumGroup>") == true:
					App.Current.NavService.EnsureNavigationSelection("Tunetastic.Views.LibraryViews.AlbumsViewPage");
					App.Current.NavService.NavigateTo(typeof(AlbumDetailPage), album?.Substring("AlbumGroup>".Length) == "Unknown" ? "Unknown Album" : album?.Substring("AlbumGroup>".Length), false);
					break;

				case var genre when genre?.StartsWith("GenreGroup>") == true:
					App.Current.NavService.EnsureNavigationSelection("Tunetastic.Views.LibraryViews.GenresViewPage");
					App.Current.NavService.NavigateTo(typeof(GenreDetailPage), genre?.Substring("GenreGroup>".Length) == "Unknown" ? "Unknown Genre" : genre?.Substring("GenreGroup>".Length), false);
					break;

				case var year when year?.StartsWith("YearGroup>") == true:
					App.Current.NavService.EnsureNavigationSelection("Tunetastic.Views.LibraryViews.YearsViewPage");
					App.Current.NavService.NavigateTo(typeof(YearDetailPage), year?.Substring("YearGroup>".Length) == "Unknown" ? "Unknown Year" : year?.Substring("YearGroup>".Length), false);
					break;

				case "AllSongsViewPage":
				default:
					App.Current.NavService.EnsureNavigationSelection("Tunetastic.Views.LibraryViews.AllSongsViewPage");
					break;
			}
		}
		catch (Exception)
		{
			App.Current.NavService.EnsureNavigationSelection("Tunetastic.Views.LibraryViews.AllSongsViewPage");
		}
	}

	/// <summary>
	/// Handles the click event for the music information button.
	/// Retrieves the currently playing song's information and displays it within the main page.
	/// </summary>
	/// <param name="sender">The source of the event, typically the button being clicked.</param>
	/// <param name="e">The event data associated with the button click.</param>
	private async void MusicInfoButton_Click(object sender, RoutedEventArgs e)
	{
		MainPage._instance?.ShowSongInfo(await DatabaseHelper.Instance.GetSongByPath(_musicPlayer.CurrentSong));
	}

	private void TiltCoverArtAndShowLyrics()
	{
		if (_isTilted) return;
		_isTilted = true;

		CalculateLyricsLayout(out double left, out double top, out double width, out double height);
		lyricsTargetWidth = width;
		lyricsTargetHeight = height;
		LyricsDisplay.Width = lyricsTargetWidth;
		LyricsDisplay.Height = lyricsTargetHeight;
		Canvas.SetLeft(LyricsDisplay, left);
		Canvas.SetTop(LyricsDisplay, top);
		PositionSyncControls(left, top, width);

		lyricsVisible = true;
		LyricsDisplay.Visibility = Visibility.Visible;
		LyricsDisplay.Opacity = 1;

		TiltInStoryboard.Begin();
		AnimateLyricsReveal(true);

		DisplayLyrics();

		ShowLyricsButton.Visibility = Visibility.Collapsed;
		CloseLyricsButton.Visibility = Visibility.Visible;
		LyricsMenuButton.Visibility = Visibility.Visible;
	}

	private void HideLyricsAndResetCoverArt()
	{
		if (!_isTilted) return;
		_isTilted = false;
		lyricsVisible = false;

		TiltOutStoryboard.Begin();
		AnimateLyricsReveal(show: false);

		CloseLyricsButton.Visibility = Visibility.Collapsed;
		LyricsMenuButton.Visibility = Visibility.Collapsed;
		ShowLyricsButton.Visibility = Visibility.Visible;
		SyncControls.Visibility = Visibility.Collapsed;
	}

	private Microsoft.UI.Composition.Compositor compositor => ElementCompositionPreview.GetElementVisual(this).Compositor;

	private void AnimateLyricsReveal(bool show)
	{
		LyricsDisplay.Opacity = 1;
		if (show) LyricsDisplay.Visibility = Visibility.Visible;

		var visual = ElementCompositionPreview.GetElementVisual(LyricsDisplay);

		var clip = compositor.CreateInsetClip(
			leftInset: show ? (float)lyricsTargetWidth : 0f,
			topInset: 0,
			rightInset: 0,
			bottomInset: 0);
		visual.Clip = clip;

		var ease = compositor.CreateCubicBezierEasingFunction(
			new System.Numerics.Vector2(0.0f, 0.0f),
			new System.Numerics.Vector2(0.3f, 1.0f));

		var anim = compositor.CreateScalarKeyFrameAnimation();
		anim.InsertKeyFrame(0f, show ? (float)lyricsTargetWidth : 0f);
		anim.InsertKeyFrame(1f, show ? 0f : (float)lyricsTargetWidth, ease);
		anim.Duration = TimeSpan.FromMilliseconds(650);

		clip.StartAnimation("LeftInset", anim);

		if (!show)
		{
			var batch = compositor.CreateScopedBatch(Microsoft.UI.Composition.CompositionBatchTypes.Animation);
			batch.Completed += (s, _) =>
			{
				LyricsDisplay.Visibility = Visibility.Collapsed;
				visual.Clip = null;
				batch.Dispose();
			};
			clip.StartAnimation("LeftInset", anim);
			batch.End();
		}
	}

	private void CalculateLyricsLayout(out double left, out double top,
									out double width, out double height)
	{
		double coverW = CoverArt.Width;
		double coverH = CoverArt.Height;

		double coverTop = (pageHeight - coverH) / 2.0 - 32.5;

		double coverCenterX = pageWidth / 2.0;
		double angle = 38 * Math.PI / 180.0;
		double rotatedWidth = coverW * Math.Cos(angle) + coverH * Math.Sin(angle);
		double scale = coverW / rotatedWidth;
		double scaledHalfWidth = (coverW / 2.0) * scale;
		double postTiltRightEdge = coverCenterX + scaledHalfWidth - 110;
		double gapWidth = pageWidth - postTiltRightEdge;

		width = gapWidth * 0.8;
		height = coverH;
		left = postTiltRightEdge + (gapWidth - width) / 2.0;
		top = coverTop;
	}

	private void ShowLyricsButton_Click(object sender, RoutedEventArgs e) => TiltCoverArtAndShowLyrics();

	private void CloseLyricsButton_Click(object sender, RoutedEventArgs e) => HideLyricsAndResetCoverArt();

	/// <summary>
	/// Tries to load an external .lrc file from the same directory as the audio file.
	/// Returns the content only if it contains valid synced lyrics.
	/// </summary>
	private static string? TryLoadExternalLrc(string songPath)
	{
		if (string.IsNullOrEmpty(songPath)) return null;

		var lrcPath = Path.ChangeExtension(songPath, ".lrc");
		if (File.Exists(lrcPath))
		{
			try
			{
				var content = File.ReadAllText(lrcPath);
				if (LrcParser.IsSyncedLyrics(content))
					return content;
			}
			catch
			{
				// File read error — silently ignore
			}
		}
		return null;
	}

	public void DisplayLyrics()
	{
		CleanupSyncedLyrics();

		if (!string.IsNullOrEmpty(lyricsText))
		{
			if (LrcParser.IsSyncedLyrics(lyricsText))
				DisplaySyncedLyrics();
			else
				DisplayUnsyncedLyrics();
		}
	}

	private void DisplayUnsyncedLyrics()
	{
		LyricsPanel.Children.Clear();
		foreach (var line in lyricsText!.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
		{
			var text = new TextBlock
			{
				Text = line,
				Style = (Style)Resources["LyricTextStyle"]
			};

			var capsule = new Border
			{
				Style = (Style)Resources["LyricCapsuleStyle"],
				Child = text
			};

			LyricsPanel.Children.Add(capsule);
		}
	}

	private void DisplaySyncedLyrics()
	{
		_lines = LrcParser.Parse(lyricsText!);
		_lyricButtons.Clear();
		LyricsPanel.Children.Clear();

		for (int i = 0; i < _lines.Count; i++)
		{
			int index = i;
			var line = _lines[i];
			bool isEmpty = string.IsNullOrEmpty(line.Text);

			FrameworkElement content;
			if (isEmpty)
			{
				var spacerText = new TextBlock
				{
					Text = " ",
					Style = (Style)Resources["SyncedLyricTextStyle"]
				};
				var spacerCapsule = new Border
				{
					Style = (Style)Resources["LyricCapsuleStyle"],
					Child = spacerText,
					Opacity = 0
				};
				content = spacerCapsule;
			}
			else
			{
				var textBlock = new TextBlock
				{
					Text = line.Text,
					Style = (Style)Resources["SyncedLyricTextStyle"]
				};

				content = new Border
				{
					Style = (Style)Resources["LyricCapsuleStyle"],
					Child = textBlock
				};
			}

			var button = new Button
			{
				Style = (Style)Resources["LyricButtonStyle"],
				Content = content,
				Opacity = isEmpty ? 0 : 0.30,
				Tag = index,
				RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5),
				IsHitTestVisible = !isEmpty
			};

			var scaleTransform = new ScaleTransform { ScaleX = 1.0, ScaleY = 1.0 };
			button.RenderTransform = scaleTransform;

			if (!isEmpty)
			{
				button.Click += LyricButton_Click;
				button.PointerEntered += LyricButton_PointerEntered;
				button.PointerExited += LyricButton_PointerExited;
			}

			_lyricButtons.Add(button);
			LyricsPanel.Children.Add(button);
		}

		_lrcTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
		_lrcTimer.Tick += LrcTimer_Tick;
		_lrcTimer.Start();

		_centeringPaddingSet = false;
		SetCenteringPadding();
	}

	/// <summary>
	/// Sets top/bottom padding on LyricsPanel equal to half the ScrollView viewport height,
	/// so every lyric line (including first and last) can be centered in the viewport.
	/// </summary>
	private async void SetCenteringPadding()
	{
		await Task.Delay(100); // let layout settle
		double viewportHeight = LyricsScrollView.ViewportHeight;
		double halfViewport = viewportHeight / 2;

		LyricsPanel.Padding = new Thickness(24, halfViewport, 24, halfViewport);
		_centeringPaddingSet = true;
	}

	private void LrcTimer_Tick(object? sender, object e)
	{
		if (!_musicPlayer.IsPlaying) return;

		long currentTicks = _musicPlayer.CurTimeTicks;
		TimeSpan currentTime = TimeSpan.FromTicks(currentTicks);

		int activeIdx = -1;
		for (int i = _lines.Count - 1; i >= 0; i--)
		{
			if (_lines[i].Time <= currentTime)
			{
				activeIdx = i;
				break;
			}
		}

		_lastKnownTicks = currentTicks;

		if (activeIdx != _activeIndex)
		{
			int oldIndex = _activeIndex;
			_activeIndex = activeIdx;

			if (oldIndex >= 0 && oldIndex < _lyricButtons.Count)
			{
				AnimateLyricButton(_lyricButtons[oldIndex], targetOpacity: 0.30, targetScale: 1.0);
			}

			if (_activeIndex >= 0 && _activeIndex < _lyricButtons.Count)
			{
				AnimateLyricButton(_lyricButtons[_activeIndex], targetOpacity: 1.0, targetScale: 1.05);
			}

			ScrollToActiveLine();
		}
	}

	private void AnimateLyricButton(Button button, double targetOpacity, double targetScale)
	{
		var storyboard = new Storyboard();

		var opacityAnim = new DoubleAnimation
		{
			To = targetOpacity,
			Duration = TimeSpan.FromMilliseconds(300)
		};
		var opacityEase = new CubicEase { EasingMode = EasingMode.EaseOut };
		opacityAnim.EasingFunction = opacityEase;
		Storyboard.SetTarget(opacityAnim, button);
		Storyboard.SetTargetProperty(opacityAnim, "Opacity");
		storyboard.Children.Add(opacityAnim);

		var scaleXAnim = new DoubleAnimation
		{
			To = targetScale,
			Duration = TimeSpan.FromMilliseconds(300)
		};
		var scaleXEase = new CubicEase { EasingMode = EasingMode.EaseOut };
		scaleXAnim.EasingFunction = scaleXEase;
		Storyboard.SetTarget(scaleXAnim, button.RenderTransform);
		Storyboard.SetTargetProperty(scaleXAnim, "ScaleX");
		storyboard.Children.Add(scaleXAnim);

		var scaleYAnim = new DoubleAnimation
		{
			To = targetScale,
			Duration = TimeSpan.FromMilliseconds(300)
		};
		var scaleYEase = new CubicEase { EasingMode = EasingMode.EaseOut };
		scaleYAnim.EasingFunction = scaleYEase;
		Storyboard.SetTarget(scaleYAnim, button.RenderTransform);
		Storyboard.SetTargetProperty(scaleYAnim, "ScaleY");
		storyboard.Children.Add(scaleYAnim);

		storyboard.Begin();
	}

	private async void ScrollToActiveLine()
	{
		if (_activeIndex < 0 || _activeIndex >= _lyricButtons.Count) return;

		await Task.Delay(50);

		if (_activeIndex < 0 || _activeIndex >= _lyricButtons.Count) return;

		if (!_centeringPaddingSet)
		{
			double vh = LyricsScrollView.ViewportHeight;
			LyricsPanel.Padding = new Thickness(24, vh / 2, 24, vh / 2);
			_centeringPaddingSet = true;
			await Task.Delay(50);
		}

		if (_activeIndex < 0 || _activeIndex >= _lyricButtons.Count) return;

		var activeButton = _lyricButtons[_activeIndex];
		var scrollView = LyricsScrollView;

		var transform = activeButton.TransformToVisual(scrollView);
		var position = transform.TransformPoint(new Windows.Foundation.Point(0, 0));

		double viewportHeight = scrollView.ViewportHeight;
		double buttonHeight = activeButton.ActualHeight;
		double targetOffset = scrollView.VerticalOffset + position.Y - (viewportHeight / 2) + (buttonHeight / 2);

		double maxOffset = scrollView.ScrollableHeight;
		targetOffset = Math.Max(0, Math.Min(targetOffset, maxOffset));

		var options = new ScrollingScrollOptions(
			ScrollingAnimationMode.Enabled,
			ScrollingSnapPointsMode.Ignore
		);
		scrollView.ScrollTo(0, targetOffset, options);
	}


	private void LyricButton_Click(object sender, RoutedEventArgs e)
	{
		if (sender is Button button && button.Tag is int index && index >= 0 && index < _lines.Count)
		{
			var time = _lines[index].Time;

			_musicPlayer.CurTimeTicks = time.Ticks;

			var vm = App.GetService<MusicControlViewModel>();
			vm.ProgressBarValue = time.TotalSeconds;
			vm.ProgressBar?.SyncPosition(time.TotalSeconds);
		}
	}

	private void LyricButton_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
	{
		if (sender is Button button && button.Tag is int index && index != _activeIndex)
		{
			AnimateLyricButton(button, targetOpacity: 0.55, targetScale: 1.0);
		}
	}

	private void LyricButton_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
	{
		if (sender is Button button && button.Tag is int index && index != _activeIndex)
		{
			AnimateLyricButton(button, targetOpacity: 0.30, targetScale: 1.0);
		}
	}

	private void CleanupSyncedLyrics()
	{
		_lrcTimer?.Stop();
		_lrcTimer = null;

		_lines.Clear();
		_lyricButtons.Clear();
		LyricsPanel.Children.Clear();
		_activeIndex = -1;
		_lastKnownTicks = 0;
		_centeringPaddingSet = false;
		LyricsPanel.Padding = new Thickness(24, 12, 24, 12); // reset to default
	}

	private void CopyAppBarButton_Click(object sender, RoutedEventArgs e)
	{
		System.Windows.Clipboard.SetText(lyricsText);
	}
	private async void EditAppBarButton_Click(object sender, RoutedEventArgs e)
	{
		var songPath = _musicPlayer.CurrentSong;
		if (!string.IsNullOrEmpty(songPath))
		{
			var track = await DatabaseHelper.Instance.GetSongByPath(songPath);

			if (File.Exists(track?.Path))
			{
				LyricsTextBox.Text = track.Lyrics;

				MainWindow._instance.WindowResizePermission(false);

				var result = await LyricsEditBlock.ShowAsync();

				MainWindow._instance.WindowResizePermission(true);

				if (result == ContentDialogResult.Primary)
				{
					track.Lyrics = LyricsTextBox.Text;
					await DatabaseHelper.Instance.InsertMultipleSongs(new List<Song> { track });
					using var audioModel = TagLib.File.Create(songPath);
					audioModel.Tag.Lyrics = LyricsTextBox.Text;
					try
					{
						audioModel.Save();
					}
					catch (IOException)
					{
						await DatabaseHelper.Instance.AddPendingTagWrite(songPath, pendingLyrics: 1);
						GlobalNotification.Warning("File is in use. Tag changes will be applied upon exit.");
					}
					await UpdateUI();
				}
			}
		}
	}
	private void SearchAppBarButton_Click(object sender, RoutedEventArgs e)
	{
		//TODO
	}
	private async void ClearAppBarButton_Click(object sender, RoutedEventArgs e)
	{
		var songPath = _musicPlayer.CurrentSong;
		if (!string.IsNullOrEmpty(songPath))
		{
			var track = await DatabaseHelper.Instance.GetSongByPath(songPath);

			if (File.Exists(track?.Path))
			{
				track.Lyrics = null;
				await DatabaseHelper.Instance.InsertMultipleSongs(new List<Song> { track });
				using var audioModel = TagLib.File.Create(songPath);
				audioModel.Tag.Lyrics = null;
				try
				{
					audioModel.Save();
				}
				catch (IOException)
				{
					await DatabaseHelper.Instance.AddPendingTagWrite(songPath, pendingLyrics: 1);
					GlobalNotification.Warning("File is in use. Tag changes will be applied upon exit.");
				}
				await UpdateUI();
			}
			else
				GlobalNotification.Error($"File not found: {songPath}");
		}
	}

	private void OpenAppBarButton_Click(object sender, RoutedEventArgs e)
	{
		if (!string.IsNullOrEmpty(_externalLrcPath) && File.Exists(_externalLrcPath))
			System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_externalLrcPath) { UseShellExecute = true });
		else
			GlobalNotification.Error("Could not find external .lrc file.");
	}

	private void LyricMenuOptions(bool embeddedLyrics)
	{
		CopyLyricsButton.Visibility = embeddedLyrics ? Visibility.Visible : Visibility.Collapsed;
		Separator1.Visibility = embeddedLyrics ? Visibility.Visible : Visibility.Collapsed;
		EditLyricsButton.Visibility = embeddedLyrics ? Visibility.Visible : Visibility.Collapsed;
		//TODO
		//Separator2.Visibility = embeddedLyrics ? Visibility.Visible : Visibility.Collapsed;
		//SearchLyricsButton.Visibility = embeddedLyrics ? Visibility.Visible : Visibility.Collapsed;
		Separator3.Visibility = embeddedLyrics ? Visibility.Visible : Visibility.Collapsed;
		ClearLyricsButton.Visibility = embeddedLyrics ? Visibility.Visible : Visibility.Collapsed;
		OpenLyricsButton.Visibility = embeddedLyrics ? Visibility.Collapsed : Visibility.Visible;
	}

	private void SyncLyricsButton_Click(object sender, RoutedEventArgs e)
	{
		if (LyricsDisplay.IsLoaded && LyricsDisplay.Visibility == Visibility.Visible)
		{
			SyncControls.Visibility = Visibility.Visible;
			CalculateLyricsLayout(out double left, out double top, out double width, out _);
			PositionSyncControls(left, top, width);
		}
	}

	private void PositionSyncControls(double lyricsLeft, double lyricsTop, double lyricsWidth)
	{
		SyncCore.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));

		double coreWidth = SyncCore.DesiredSize.Width;
		double coreHeight = SyncCore.DesiredSize.Height;

		double syncLeft = lyricsLeft + (lyricsWidth - coreWidth) / 2.0;
		double syncTop = lyricsTop - coreHeight - SyncControlsGap;

		SyncControls.Margin = new Thickness(syncLeft, syncTop, 0, 0);
	}

	private void CancelSyncButton_Click(object sender, RoutedEventArgs e)
	{
		
	}

	private void AcceptSyncButton_Click(object sender, RoutedEventArgs e)
	{

	}

	private void DecreaseButton_Click(object sender, RoutedEventArgs e)
	{

	}

	private void IncreaseButton_Click(object sender, RoutedEventArgs e)
	{

	}
}
