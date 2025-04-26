using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using TagLib;
using Image = SixLabors.ImageSharp.Image;

namespace Tunetastic.Common;

public class ImageResizer
{
    public static string CreateThumbnailImage(ThumbnailFolder thumbnailFolder, IPicture[] pictures, int width, int height)
    {
        var thumbnailFilePath = Path.Combine(Constants.ThumbnailsFolder, thumbnailFolder.ToString(), "Cover_" + new string(Enumerable.Range(0, 10).Select(_ => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"[new Random().Next(62)]).ToArray()) + ".png");
        Directory.CreateDirectory(Path.GetDirectoryName(thumbnailFilePath));
        byte[] imageData;
        try
        {
            imageData = pictures?.Length > 0
            ? pictures[0].Data.Data
            : System.IO.File.ReadAllBytes("Assets/AppIcon.png");
        }
        catch (Exception)
        {
            imageData = System.IO.File.ReadAllBytes("Assets/AppIcon.png");
        }

        using var image = Image.Load(imageData);
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(width, height),
            Mode = ResizeMode.Max
        }));

        image.Save(thumbnailFilePath, new PngEncoder());

        return thumbnailFilePath;
    }

    public static string CreateThumbnailImage(ThumbnailFolder thumbnailFolder, IPicture[] pictures, string? fileName = null)
    {
        var thumbnailFilePath = Path.Combine(Constants.ThumbnailsFolder, thumbnailFolder.ToString(), fileName ?? "Cover_" + new string(Enumerable.Range(0, 10).Select(_ => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"[new Random().Next(62)]).ToArray()) + ".png");
        Directory.CreateDirectory(Path.GetDirectoryName(thumbnailFilePath));
        byte[] imageData;
        try
        {
            imageData = pictures?.Length > 0
            ? pictures[0].Data.Data
            : System.IO.File.ReadAllBytes("Assets/AppIcon.png");
        }
        catch (Exception)
        {
            imageData = System.IO.File.ReadAllBytes("Assets/AppIcon.png");
        }

        using var image = Image.Load(imageData);

        image.Save(thumbnailFilePath, new PngEncoder());

        return thumbnailFilePath;
    }

    public static string CreateThumbnailImage(ThumbnailFolder thumbnailFolder, IPicture[] pictures, int size)
    {
        return CreateThumbnailImage(thumbnailFolder, pictures, size, size);
    }
}
public enum ThumbnailFolder
{
    AllSongView,
    MainPlayer
}
