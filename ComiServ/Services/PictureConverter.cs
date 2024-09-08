//using System.Drawing;
using System.Drawing.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Bmp;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.StaticFiles;

namespace ComiServ.Background;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PictureFormats
{
    Webp,
    Jpg,
    Png,
    Gif,
    Bmp,
}
//never closes stream!
public interface IPictureConverter
{
    public static System.Drawing.Size ThumbnailResolution => new(200, 320);
    public static PictureFormats ThumbnailFormat => PictureFormats.Webp;
                          //keeps aspect ratio, crops to horizontally to center, vertically to top
                          //uses System.Drawing.Size so interface isn't dependant on ImageSharp
    public Task<Stream> Resize(Stream image, System.Drawing.Size newSize, PictureFormats? newFormat = null);
    public Task<Stream> ResizeIfBigger(Stream image, System.Drawing.Size maxSize, PictureFormats? newFormat = null);
    public Task<Stream> MakeThumbnail(Stream image);
    public static string GetMime(PictureFormats format)
    {
        switch (format)
        {
            case PictureFormats.Webp:
                return "image/webp";
            case PictureFormats.Gif:
                return "image/gif";
            case PictureFormats.Jpg:
                return "image/jpeg";
            case PictureFormats.Bmp:
                return "image/bmp";
            case PictureFormats.Png:
                return "image/png";
            default:
                throw new ArgumentException("Cannot handle this format", nameof(format));
        }
    }
}
public class ResharperPictureConverter(bool webpLossless = false)
    : IPictureConverter
{
    public static IImageFormat ConvertFormatEnum(PictureFormats format)
    {
        switch (format)
        {
            case PictureFormats.Webp:
                return WebpFormat.Instance;
            case PictureFormats.Jpg:
                return JpegFormat.Instance;
            case PictureFormats.Png:
                return PngFormat.Instance;
            case PictureFormats.Gif:
                return GifFormat.Instance;
            case PictureFormats.Bmp:
                return BmpFormat.Instance;
            default:
                throw new ArgumentException("Cannot handle this format", nameof(format));
        }
    }
    public bool WebpLossless { get; } = webpLossless;
    public async Task<Stream> Resize(Stream image, System.Drawing.Size newSize, PictureFormats? newFormat = null)
    {
        using var img = Image.Load(image);
        IImageFormat format;
        if (newFormat is PictureFormats nf)
            format = ConvertFormatEnum(nf);
        else if (img.Metadata.DecodedImageFormat is IImageFormat iif)
            format = img.Metadata.DecodedImageFormat;
        else
            format = WebpFormat.Instance;
        double oldAspect = ((double)img.Height) / img.Width;
        double newAspect = ((double)newSize.Height) / newSize.Width;
        Rectangle sourceRect;
        if (newAspect > oldAspect)
        {
            var y = 0;
            var h = newSize.Height;
            var w = (int)(h / newAspect);
            var x = (img.Width - w) / 2;
            sourceRect = new Rectangle(x, y, w, h);
        }
        else
        {
            var x = 0;
            var w = newSize.Width;
            var h = (int)(w * newAspect);
            var y = 0;
            sourceRect = new Rectangle(x, y, w, h);
        }
        img.Mutate(c => c.Crop(sourceRect).Resize(new Size(newSize.Width, newSize.Height)));
        var outStream = new MemoryStream();
        if (format is WebpFormat)
        {
            var enc = new WebpEncoder()
            {
                FileFormat = WebpLossless ? WebpFileFormatType.Lossless : WebpFileFormatType.Lossy
            };
            await img.SaveAsync(outStream, enc);
        }
        else
        {
            await img.SaveAsync(outStream, format);
        }
        return outStream;
    }
    public async Task<Stream> ResizeIfBigger(Stream image, System.Drawing.Size maxSize, PictureFormats? newFormat = null)
    {
        using Image img = Image.Load(image);
        IImageFormat format;
        if (newFormat is PictureFormats nf)
            format = ConvertFormatEnum(nf);
        else if (img.Metadata.DecodedImageFormat is IImageFormat iif)
            format = img.Metadata.DecodedImageFormat;
        else
            format = WebpFormat.Instance;
        double scale = 1;
        if (img.Size.Width > maxSize.Width)
        {
            scale = Math.Min(scale, ((double)maxSize.Width) / img.Size.Width);
        }
        if (img.Size.Height > maxSize.Height)
        {
            scale = Math.Min(scale, ((double)maxSize.Height) / img.Size.Height);
        }
        Size newSize = new((int)(img.Size.Width * scale), (int)(img.Size.Height * scale));
        img.Mutate(c => c.Resize(newSize));
        var outStream = new MemoryStream();
        if (format is WebpFormat)
        {
            var enc = new WebpEncoder()
            {
                FileFormat = WebpLossless ? WebpFileFormatType.Lossless : WebpFileFormatType.Lossy
            };
            await img.SaveAsync(outStream, enc);
        }
        else
        {
            await img.SaveAsync(outStream, format);
        }
        return outStream;
    }
    public async Task<Stream> MakeThumbnail(Stream image)
    {
        return await Resize(image, IPictureConverter.ThumbnailResolution, IPictureConverter.ThumbnailFormat);
    }
}
