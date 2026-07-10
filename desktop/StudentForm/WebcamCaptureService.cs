using OpenCvSharp;

namespace StudentForm;

internal sealed class WebcamCaptureService : IDisposable
{
    private const int MaxCameraIndex = 15;
    private const int CaptureWidth = 400;
    private const int CaptureHeight = 225;
    private const int CaptureFps = 20;
    private VideoCapture? _capture;
    private string? _lastError;
    private int _selectedCameraIndex;
    private string _selectedCameraId = "camera-0";
    private readonly object _sync = new();

    public bool IsAvailable => _capture?.IsOpened() == true;
    public string SelectedCameraId => _selectedCameraId;
    public string LastStatusMessage => _lastError ?? (IsAvailable ? "Webcam ready." : "Webcam not initialized.");

    public IReadOnlyList<WebcamDeviceDescriptor> ProbeDevices(int maxIndex = MaxCameraIndex)
    {
        List<WebcamDeviceDescriptor> devices = [];
        lock (_sync)
        {
            for (int index = 0; index <= maxIndex; index++)
            {
                string cameraId = CameraId(index);
                try
                {
                    using VideoCapture probe = new(index, VideoCaptureAPIs.DSHOW);
                    bool opened = probe.IsOpened();
                    if (!opened)
                    {
                        probe.Open(index, VideoCaptureAPIs.DSHOW);
                        opened = probe.IsOpened();
                    }

                    if (opened)
                    {
                        using Mat frame = new();
                        probe.Grab();
                        opened = probe.Read(frame) && !frame.Empty();
                    }

                    devices.Add(new WebcamDeviceDescriptor(
                        cameraId,
                        index,
                        $"Camera {index}{(index == _selectedCameraIndex ? " (đang chọn)" : "")}",
                        opened,
                        opened ? "available" : "unavailable"));
                }
                catch (Exception ex)
                {
                    devices.Add(new WebcamDeviceDescriptor(cameraId, index, $"Camera {index}", false, ex.Message));
                }
            }
        }

        return devices.Where(x => x.IsAvailable).Concat(devices.Where(x => !x.IsAvailable)).ToList();
    }

    public void SelectCamera(string cameraId)
    {
        if (string.IsNullOrWhiteSpace(cameraId))
        {
            return;
        }

        int index = ParseCameraIndex(cameraId);
        lock (_sync)
        {
            if (index == _selectedCameraIndex && string.Equals(cameraId, _selectedCameraId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _selectedCameraIndex = index;
            _selectedCameraId = CameraId(index);
            _lastError = null;
            ResetCapture();
        }
    }

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
        foreach (int candidateIndex in BuildCandidateIndexes())
        {
            VideoCapture capture = new(candidateIndex, VideoCaptureAPIs.DSHOW);
            capture.Open(candidateIndex, VideoCaptureAPIs.DSHOW);
            if (!capture.IsOpened())
            {
                capture.Dispose();
                continue;
            }

            capture.Set(VideoCaptureProperties.FourCC, VideoWriter.FourCC('M', 'J', 'P', 'G'));
            capture.Set(VideoCaptureProperties.FrameWidth, CaptureWidth);
            capture.Set(VideoCaptureProperties.FrameHeight, CaptureHeight);
            capture.Set(VideoCaptureProperties.Fps, CaptureFps);
            capture.Set(VideoCaptureProperties.BufferSize, 1);

            using Mat probe = new();
            capture.Grab();
            if (!capture.Read(probe) || probe.Empty())
            {
                capture.Release();
                capture.Dispose();
                continue;
            }

            _capture = capture;
            _selectedCameraIndex = candidateIndex;
            _selectedCameraId = CameraId(candidateIndex);
            _lastError = null;
            return;
        }

        _lastError = "Khong tim thay webcam hoat dong.";
        ResetCapture();
    }

    private void ResetCapture()
    {
        _capture?.Release();
        _capture?.Dispose();
        _capture = null;
    }

    private static Mat NormalizeFrame(Mat frame)
    {
        if (frame.Width <= CaptureWidth && frame.Height <= CaptureHeight)
        {
            return frame.Clone();
        }

        Mat resized = new();
        double scale = Math.Min((double)CaptureWidth / frame.Width, (double)CaptureHeight / frame.Height);
        int width = Math.Max(1, (int)Math.Round(frame.Width * scale));
        int height = Math.Max(1, (int)Math.Round(frame.Height * scale));
        Cv2.Resize(frame, resized, new OpenCvSharp.Size(width, height), 0, 0, InterpolationFlags.Area);
        return resized;
    }

    private IEnumerable<int> BuildCandidateIndexes()
    {
        yield return _selectedCameraIndex;
        for (int index = 0; index <= MaxCameraIndex; index++)
        {
            if (index != _selectedCameraIndex)
            {
                yield return index;
            }
        }
    }

    private static string CameraId(int index) => $"camera-{index}";

    private static int ParseCameraIndex(string cameraId)
    {
        if (cameraId.StartsWith("camera-", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(cameraId["camera-".Length..], out int parsed))
        {
            return Math.Clamp(parsed, 0, MaxCameraIndex);
        }

        return 0;
    }
}

internal sealed record WebcamDeviceDescriptor(string CameraId, int CameraIndex, string DisplayName, bool IsAvailable, string Status);

internal readonly record struct WebcamCaptureResult(bool IsSuccess, byte[]? Jpeg, string Message)
{
    public static WebcamCaptureResult Success(byte[] jpeg) => new(true, jpeg, "Webcam frame captured.");
    public static WebcamCaptureResult Failure(string message) => new(false, null, message);
}
