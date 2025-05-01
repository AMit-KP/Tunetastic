using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using TagLib;
using Image = SixLabors.ImageSharp.Image;

namespace Tunetastic.Common;

public static class ImageResizer
{
    /// Creates a thumbnail image from the provided pictures and saves it as a PNG file in the specified thumbnail folder.
    /// <param name="thumbnailFolder">
    /// The folder where the thumbnail image will be stored. This determines the subfolder under the Thumbnails directory.
    /// </param>
    /// <param name="pictures">
    /// An array of IPicture objects representing the image(s) to be resized. If no valid images are provided, a default image will be used.
    /// </param>
    /// <param name="width">
    /// The desired width of the thumbnail image in pixels.
    /// </param>
    /// <param name="height">
    /// The desired height of the thumbnail image in pixels.
    /// </param>
    /// <returns>
    /// The file path of the created thumbnail image.
    /// </returns>
    public static string CreateThumbnailImage(ThumbnailFolder thumbnailFolder, IPicture[] pictures, int width, int height)
    {
        var thumbnailFilePath = Path.Combine(Constants.ThumbnailsFolder, thumbnailFolder.ToString(), "Cover_" + new string(Enumerable.Range(0, 10).Select(_ => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"[new Random().Next(62)]).ToArray()) + ".png");
        Directory.CreateDirectory(Path.GetDirectoryName(thumbnailFilePath));
        byte[] imageData;
        try
        {
            imageData = pictures?.Length > 0
            ? pictures[0].Data.Data
            : System.IO.File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.png"));
        }
        catch (Exception)
        {
            imageData = System.IO.File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.png"));
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

    /// Creates a thumbnail image from the provided pictures and saves it as a PNG file in the specified thumbnail folder.
    /// If no valid image data is available, a default application icon is used.
    /// <param name="thumbnailFolder">
    /// The folder where the thumbnail image will be stored. This defines the subfolder under the main Thumbnails directory.
    /// </param>
    /// <param name="pictures">
    /// An array of IPicture objects containing the image data to be processed. If no pictures are provided, a default image will be used.
    /// </param>
    /// <param name="fileName">
    /// An optional string specifying the name of the thumbnail file. If not provided, a randomly generated name will be used.
    /// </param>
    /// <returns>
    /// The file path of the created thumbnail image.
    /// </returns>
    public static string CreateThumbnailImage(ThumbnailFolder thumbnailFolder, IPicture[] pictures, string? fileName = null)
    {
        var thumbnailFilePath = Path.Combine(Constants.ThumbnailsFolder, thumbnailFolder.ToString(), fileName ?? "Cover_" + new string(Enumerable.Range(0, 10).Select(_ => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"[new Random().Next(62)]).ToArray()) + ".png");
        Directory.CreateDirectory(Path.GetDirectoryName(thumbnailFilePath));
        byte[] imageData;
        try
        {
            imageData = pictures?.Length > 0
            ? pictures[0].Data.Data
            : System.IO.File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.png"));
        }
        catch (Exception)
        {
            imageData = System.IO.File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.png"));
        }

        using var image = Image.Load(imageData);

        image.Save(thumbnailFilePath, new PngEncoder());

        return thumbnailFilePath;
    }

    /// Creates a thumbnail image from the provided pictures and saves it as a PNG file in the specified thumbnail folder.
    /// <param name="thumbnailFolder">
    /// The folder where the thumbnail image will be stored. This determines the subfolder under the Thumbnails directory.
    /// </param>
    /// <param name="pictures">
    /// An array of IPicture objects representing the image(s) to be resized. If no valid images are provided, a default image will be used.
    /// </param>
    /// <param name="size">
    /// The desired dimension for both the width and height of the square thumbnail image in pixels.
    /// </param>
    /// <returns>
    /// The file path of the created thumbnail image.
    /// </returns>
    public static string CreateThumbnailImage(ThumbnailFolder thumbnailFolder, IPicture[] pictures, int size)
    {
        return CreateThumbnailImage(thumbnailFolder, pictures, size, size);
    }
}

/// <summary>
/// Represents the folder locations where thumbnail images are stored.
/// </summary>
public enum ThumbnailFolder
{
    AllSongView,
    MainPlayer
}
