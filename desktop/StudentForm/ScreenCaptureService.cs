using System.Drawing.Imaging;

namespace StudentForm;

internal static class ScreenCaptureService
{
    private const int MaxEncodedWidth = 1366;
    private const int MaxEncodedHeight = 768;

    public static byte[] CapturePrimaryScreenJpeg(long quality = 45L)
    {
        Rectangle bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1280, 720);
        using Bitmap bitmap = new(bounds.Width, bounds.Height);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);

        using Bitmap encodedBitmap = ResizeIfNeeded(bitmap);

        using MemoryStream output = new();
        ImageCodecInfo jpegCodec = ImageCodecInfo.GetImageEncoders().First(x => x.MimeType == "image/jpeg");
        using EncoderParameters encoderParameters = new(1);
        encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);
        encodedBitmap.Save(output, jpegCodec, encoderParameters);
        return output.ToArray();
    }

    private static Bitmap ResizeIfNeeded(Bitmap source)
    {
        if (source.Width <= MaxEncodedWidth && source.Height <= MaxEncodedHeight)
        {
            return new Bitmap(source);
        }

        double scale = Math.Min((double)MaxEncodedWidth / source.Width, (double)MaxEncodedHeight / source.Height);
        int width = Math.Max(1, (int)Math.Round(source.Width * scale));
        int height = Math.Max(1, (int)Math.Round(source.Height * scale));
        Bitmap resized = new(width, height);
        using Graphics graphics = Graphics.FromImage(resized);
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        graphics.DrawImage(source, 0, 0, width, height);
        return resized;
    }
}
