using System.Text.RegularExpressions;
using Microsoft.UI.Dispatching;
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
	private double pageHeight = 0;

	public MainPlayerPage()
	{
		this.InitializeComponent();
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
		_dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, async () =>
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
	private async Task<Task> UpdateUI(bool notify = true)
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

							BackgroundImage.Source = bitmapImage;

							int width = bitmapImage.PixelWidth;
							int height = bitmapImage.PixelHeight;

							double targetHeight = Math.Min(550, pageHeight == 0 ? 500 : pageHeight * 0.55);
							var aspectRatio = (double)bitmapImage.PixelWidth / bitmapImage.PixelHeight;
							var targetWidth = aspectRatio * targetHeight;

							CoverArt.Width = targetWidth;
							CoverArt.Height = targetHeight;
							CoverArt.CornerRadius = new CornerRadius(targetHeight / 8);

							CoverArtImage.Source = bitmapImage;
						}
						if ((Title.Text + "\n" + Artist.Text).Length > 128)
							App.TrayIcon.Text = Title.Text;
						else
							App.TrayIcon.Text = Title.Text + "\n" + Artist.Text;

						var updater = MusicPlayer.Instance.SMTC.DisplayUpdater;
						updater.Type = MediaPlaybackType.Music;
						updater.Thumbnail = RandomAccessStreamReference.CreateFromFile(await StorageFile.GetFileFromPathAsync(track?.Cover));
						updater.MusicProperties.Title = Title.Text;
						updater.MusicProperties.Artist = Artist.Text;
						updater.MusicProperties.AlbumTitle = Album.Text;
						updater.MusicProperties.AlbumArtist = Artist.Text;
						updater.MusicProperties.AlbumTrackCount = 1;
						updater.Update();

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
		BackgroundImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/AppIcon.png"));
		CoverArt.Width = 500;
		CoverArt.Height = 500;
		CoverArt.CornerRadius = new CornerRadius(50);
		CoverArtImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/AppIcon.png"));
		Title.Text = "Please select a song";
		return Task.CompletedTask;
	}

	/// <summary>
	/// Handles the Loaded event for the page. This method is triggered after the page's visual elements have been loaded.
	/// Performs UI updates and layout operations to ensure the user interface is initialized correctly.
	/// </summary>
	/// <param name="sender">The source of the event. This is typically the page itself.</param>
	/// <param name="e">Event arguments containing information about the Loaded event.</param>
	private void Page_Loaded(object sender, RoutedEventArgs e)
	{
		_dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, async () =>
		{
			await Task.Delay(200);
			await UpdateUI();
			this.SizeChanged += Page_SizeChanged;
		});
	}

	/// <summary>
	/// Handles the event that occurs when the size of the page is changed.
	/// This method updates layout-related properties and refreshes the UI accordingly.
	/// </summary>
	/// <param name="sender">The source of the event. Typically, this is the page whose size has changed.</param>
	/// <param name="e">The event data containing information about the new size of the page.</param>
	private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		pageHeight = e.NewSize.Height;
		_dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, async () =>
		{
			await UpdateUI(false);
		});
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
					App.Current.NavService.NavigateTo(typeof(ArtistDetailPage), artist?.Substring("ArtistGroup>".Length) == "Unknown" ? "Unknown Artist" : artist?.Substring("ArtistGroup>".Length), false);
					((App.Current.NavService.MenuItems[1] as NavigationViewItem)?.MenuItems.Select(x => x as NavigationViewItem).FirstOrDefault(x => x?.Tag.ToString() == "Tunetastic.Views.LibraryViews.ArtistsViewPage")).IsSelected = true;
					break;

				case var album when album?.StartsWith("AlbumGroup>") == true:
					App.Current.NavService.NavigateTo(typeof(AlbumDetailPage), album?.Substring("AlbumGroup>".Length) == "Unknown" ? "Unknown Album" : album?.Substring("AlbumGroup>".Length), false);
					((App.Current.NavService.MenuItems[1] as NavigationViewItem)?.MenuItems.Select(x => x as NavigationViewItem).FirstOrDefault(x => x?.Tag.ToString() == "Tunetastic.Views.LibraryViews.AlbumsViewPage")).IsSelected = true;
					break;

				case var genre when genre?.StartsWith("GenreGroup>") == true:
					App.Current.NavService.NavigateTo(typeof(GenreDetailPage), genre?.Substring("GenreGroup>".Length) == "Unknown" ? "Unknown Genre" : genre?.Substring("GenreGroup>".Length), false);
					((App.Current.NavService.MenuItems[1] as NavigationViewItem)?.MenuItems.Select(x => x as NavigationViewItem).FirstOrDefault(x => x?.Tag.ToString() == "Tunetastic.Views.LibraryViews.GenresViewPage")).IsSelected = true;
					break;

				case var year when year?.StartsWith("YearGroup>") == true:
					App.Current.NavService.NavigateTo(typeof(YearDetailPage), year?.Substring("YearGroup>".Length) == "Unknown" ? "Unknown Year" : year?.Substring("YearGroup>".Length), false);
					((App.Current.NavService.MenuItems[1] as NavigationViewItem)?.MenuItems.Select(x => x as NavigationViewItem).FirstOrDefault(x => x?.Tag.ToString() == "Tunetastic.Views.LibraryViews.YearsViewPage")).IsSelected = true;
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
}
