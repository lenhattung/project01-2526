using OpenCvSharp;

namespace StudentForm;

internal sealed class WebcamCaptureService : IDisposable
{
    private VideoCapture? _capture;
    private string? _lastError;
    private readonly object _sync = new();

    public bool IsAvailable => _capture?.IsOpened() == true;
    public string LastStatusMessage => _lastError ?? (IsAvailable ? "Webcam ready." : "Webcam not initialized.");

    public WebcamCaptureResult TryCaptureJpeg(long quality)
    {
        try
        {
            lock (_sync)
            {
                EnsureCapture();
                if (_capture is null || !_capture.IsOpened())
                {
                    return WebcamCaptureResult.Failure(_lastError ?? "Could not open webcam.");
                }

                using Mat frame = new();
                _capture.Grab();
                if (!_capture.Read(frame) || frame.Empty())
                {
                    _lastError = "Webcam returned an empty frame.";
                    ResetCapture();
                    return WebcamCaptureResult.Failure(_lastError);
                }

                using Mat normalized = NormalizeFrame(frame);
                int jpegQuality = Math.Clamp((int)quality, 25, 80);
                Cv2.ImEncode(".jpg", normalized, out byte[] jpeg, [new ImageEncodingParam(ImwriteFlags.JpegQuality, jpegQuality)]);
                _lastError = null;
                return WebcamCaptureResult.Success(jpeg);
            }
        }
        catch (Exception ex)
        {
            _lastError = ex.Message;
            ResetCapture();
            return WebcamCaptureResult.Failure(_lastError);
        }
    }

    public void Dispose()
    {
        ResetCapture();
    }

    private void EnsureCapture()
    {
        if (_capture?.IsOpened() == true)
        {
            return;
        }

        ResetCapture();
        _capture = new VideoCapture(0, VideoCaptureAPIs.DSHOW);
        _capture.Open(0, VideoCaptureAPIs.DSHOW);

        if (!_capture.IsOpened())
        {
            _lastError = "No usable webcam was found on this workstation.";
            ResetCapture();
            return;
        }

        _capture.Set(VideoCaptureProperties.FourCC, VideoWriter.FourCC('M', 'J', 'P', 'G'));
        _capture.Set(VideoCaptureProperties.FrameWidth, 320);
        _capture.Set(VideoCaptureProperties.FrameHeight, 240);
        _capture.Set(VideoCaptureProperties.Fps, 15);
        _capture.Set(VideoCaptureProperties.BufferSize, 1);
        _lastError = null;
    }

    private void ResetCapture()
    {
        _capture?.Release();
        _capture?.Dispose();
        _capture = null;
    }

    private static Mat NormalizeFrame(Mat frame)
    {
        if (frame.Width <= 320 && frame.Height <= 240)
        {
            return frame.Clone();
        }

        Mat resized = new();
        Cv2.Resize(frame, resized, new OpenCvSharp.Size(320, 240), 0, 0, InterpolationFlags.Area);
        return resized;
    }
}

internal readonly record struct WebcamCaptureResult(bool IsSuccess, byte[]? Jpeg, string Message)
{
    public static WebcamCaptureResult Success(byte[] jpeg) => new(true, jpeg, "Webcam frame captured.");
    public static WebcamCaptureResult Failure(string message) => new(false, null, message);
}
