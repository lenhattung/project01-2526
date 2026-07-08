using System.Drawing.Imaging;

namespace StudentForm;

internal static class ScreenCaptureService
{
    public static byte[] CapturePrimaryScreenJpeg(long quality = 45L)
    {
        Rectangle bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1280, 720);
        using Bitmap bitmap = new(bounds.Width, bounds.Height);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);

        using MemoryStream output = new();
        ImageCodecInfo jpegCodec = ImageCodecInfo.GetImageEncoders().First(x => x.MimeType == "image/jpeg");
        using EncoderParameters encoderParameters = new(1);
        encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);
        bitmap.Save(output, jpegCodec, encoderParameters);
        return output.ToArray();
    }
}
