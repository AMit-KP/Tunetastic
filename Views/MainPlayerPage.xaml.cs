using Google.Protobuf.Collections;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using Tunetastic.Generated.Protos;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Tunetastic.Views;

public sealed partial class MainPlayerPage : Page
{
    private readonly MusicPlayer _musicPlayer = MusicPlayer.Instance;
    private readonly DispatcherQueue _dispatcherQueue;
    private RepeatedField<Song> AllSongs = new();
    private double pageHeight = 0;

    public MainPlayerPage()
    {
        this.InitializeComponent();

        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        AllSongs = ProtobufData.LoadFromBin<SongList>(DataFile.AllSongsMetaData).Songs;
        _musicPlayer.CurrentSongChanged += OnCurrentSongChanged;
    }

    private void OnCurrentSongChanged(object? sender, string e)
    {
        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, async () =>
        {
            await Task.Delay(200);
            await UpdateUI();
        });
    }

    private async Task<Task> UpdateUI()
    {
        var song = _musicPlayer.CurrentSong;
        if (song != null && song != string.Empty)
        {
            var track = AllSongs.FirstOrDefault(s => s.Path == song);

            Title.Text = track?.Title;
            Album.Text = track?.Album;
            Artist.Text = track?.Artists;

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
            //TODO: get settings
            GlobalNotification.Info($"Now playing: {track?.Title} by {track?.Artists}");
            return Task.CompletedTask;
        }

        BackgroundImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/AppIcon.png"));
        CoverArt.Width = 500;
        CoverArt.Height = 500;
        CoverArt.CornerRadius = new CornerRadius(50);
        CoverArtImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/AppIcon.png"));
        Title.Text = "Please select a song";
        return Task.CompletedTask;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, async () =>
        {
            await UpdateUI();
        });
    }

    private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        pageHeight = e.NewSize.Height;
        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, async () =>
        {
            await UpdateUI();
        });
    }
}
