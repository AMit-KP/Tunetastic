using System.Text.RegularExpressions;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Input;
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
	private double coverArtAspectRatio = 1.0;
	private double coverArtImagePixelWidth = 500;
	private double coverArtImagePixelHeight = 500;

	public MainPlayerPage()
	{
		this.InitializeComponent();
		this.NavigationCacheMode = NavigationCacheMode.Required;

		BlurEffect.Amount = 50 + (double.Parse(Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.MainPlayerBGBlurValue)]?.ToString() ?? "5") * 10);
		_dispatcherQueue = DispatcherQueue.GetForCurrentThread();
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
		// Use Normal priority and move the delay outside the enqueue.
		// High priority + async delay was blocking navigation/layout for 200ms+ on every song change.
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
			if (await DatabaseHelper.Instance.GetSongsCount() != 0)
			{
				var song = _musicPlayer.CurrentSong;
				if (song != null && song != string.Empty)
				{
					var track = await DatabaseHelper.Instance.GetSongByPath(song);
					if (File.Exists(track?.Path))
					{
						Title.Text = track?.Title;
						Album.Text = track?.Album;
						Artist.Text = track?.Artists;
						Title.FontSize = Album.FontSize * 1.5;
						Artist.FontSize = Album.FontSize * 1.1;

						var thumbnailFilePath = Path.Combine(Constants.ThumbnailsFolder, ThumbnailFolder.MainPlayer.ToString(), track?.Cover.Substring(track.Cover.LastIndexOf("Cover_")));
						if (!File.Exists(thumbnailFilePath))
						{
							using var audioModel = TagLib.File.Create(track.Path);
							ImageResizer.CreateThumbnailImage(ThumbnailFolder.MainPlayer, audioModel.Tag.Pictures, thumbnailFilePath);
						}

						StorageFile file = await StorageFile.GetFileFromPathAsync(thumbnailFilePath);
						using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read))
						{
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

						var updater = MusicPlayer.Instance.SMTC.DisplayUpdater;
						updater.Type = MediaPlaybackType.Music;
						updater.Thumbnail = RandomAccessStreamReference.CreateFromFile(await StorageFile.GetFileFromPathAsync(track?.Cover));
						updater.MusicProperties.Title = Title.Text;
						updater.MusicProperties.Artist = Artist.Text;
						updater.MusicProperties.AlbumTitle = Album.Text;
						updater.MusicProperties.AlbumArtist = Artist.Text;
						updater.MusicProperties.AlbumTrackCount = 1;
						updater.Update();

						UpdateCoverArtSize();
						MusicInfoButton.Visibility = Visibility.Visible;

						return Task.CompletedTask;
					}
					else
					{
						if (notify)
							GlobalNotification.Error("Could not find track/song:\n" + song);
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
		return Task.CompletedTask;
	}

	/// <summary>
	/// Updates the dimensions of the cover art on the main player page.
	/// This method adjusts the width, height, and corner radius of the cover art based on the current page height and the aspect ratio of the cover art.
	/// </summary>
	private void UpdateCoverArtSize()
	{
		double targetHeight = Math.Min(550, pageHeight == 0 ? 500 : pageHeight * 0.55);
		double targetWidth = coverArtAspectRatio * targetHeight;
		CoverArt.Width = targetWidth;
		CoverArt.Height = targetHeight;
		CoverArt.CornerRadius = new CornerRadius(targetHeight / 8);
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
	private void Page_SizeChanged(object? sender, SizeChangedEventArgs? e)
	{
		pageHeight = e.NewSize.Height;
		UpdateCoverArtSize();
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
	
	private bool _isTilted = false;

	private void TiltCoverArt()
	{
		if (_isTilted) return;
		_isTilted = true;
		TiltInStoryboard.Begin();
	}

	private void ResetCoverArt()
	{
		if (!_isTilted) return;
		_isTilted = false;
		TiltOutStoryboard.Begin();
	}

	// Called when ToggleButton is Checked (pressed ON → tilt)
	private void TiltButton_Click(object sender, RoutedEventArgs e) => TiltCoverArt();

	// Called when ToggleButton is Unchecked (pressed OFF → reset)
	private void ResetButton_Click(object sender, RoutedEventArgs e) => ResetCoverArt();
}
