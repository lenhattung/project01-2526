using System.Drawing;
using System.Drawing.Imaging;
using System.Net.Sockets;
using ExamGuard.Protocol;

Options options = Options.Parse(args);
using CancellationTokenSource cts = new();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

Console.WriteLine($"Starting {options.Count} simulated student(s) against {options.Host}:{options.Port}.");
List<Task> tasks = Enumerable.Range(1, options.Count)
    .Select(index => RunStudentAsync(options, index, cts.Token))
    .ToList();

await Task.WhenAll(tasks);

static async Task RunStudentAsync(Options options, int index, CancellationToken cancellationToken)
{
    string studentId = options.DuplicateCode ? $"{options.Prefix}001" : $"{options.Prefix}{index:000}";
    string connectionId = Guid.NewGuid().ToString("N");
    try
    {
        using TcpClient client = new();
        await client.ConnectAsync(options.Host, options.Port, cancellationToken);
        await using NetworkStream stream = client.GetStream();
        DateTime nextWebcamFrameAtUtc = DateTime.UtcNow;

        await FramedSocketProtocol.SendAsync(
            stream,
            FramedSocketProtocol.CreateEnvelope(MessageType.Hello, options.Session, studentId, $"SIM-PC-{index:000}", new()
            {
                ["token"] = options.Token,
                ["connectionId"] = connectionId,
                ["studentCode"] = studentId,
                ["studentName"] = $"Simulated Student {index:000}",
                ["machineName"] = $"SIM-PC-{index:000}",
                ["os"] = "StudentSimulator"
            }, connectionId),
            null,
            cancellationToken);

        _ = Task.Run(() => ReceiveLoopAsync(stream, studentId, cancellationToken), cancellationToken);
        await SendWebcamDevicesAsync(stream, options.Session, studentId, connectionId, cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            await FramedSocketProtocol.SendAsync(
                stream,
                FramedSocketProtocol.CreateEnvelope(MessageType.Heartbeat, options.Session, studentId, $"SIM-PC-{index:000}", connectionId: connectionId),
                null,
                cancellationToken);

            byte[] jpeg = GenerateFrame(studentId);
            await FramedSocketProtocol.SendAsync(
                stream,
                FramedSocketProtocol.CreateEnvelope(MessageType.ScreenFrame, options.Session, studentId, $"SIM-PC-{index:000}", connectionId: connectionId),
                jpeg,
                cancellationToken);

            if (options.WebcamIntervalMs > 0 && DateTime.UtcNow >= nextWebcamFrameAtUtc)
            {
                byte[] webcam = GenerateWebcamFrame(studentId);
                await FramedSocketProtocol.SendAsync(
                    stream,
                    FramedSocketProtocol.CreateEnvelope(MessageType.WebcamFrame, options.Session, studentId, $"SIM-PC-{index:000}", new() { ["cameraId"] = "camera-0" }, connectionId),
                    webcam,
                    cancellationToken);
                nextWebcamFrameAtUtc = DateTime.UtcNow.AddMilliseconds(options.WebcamIntervalMs);
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }
    }
    catch (OperationCanceledException)
    {
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{studentId} failed: {ex.Message}");
    }
}

static Task SendWebcamDevicesAsync(NetworkStream stream, string session, string studentId, string connectionId, CancellationToken cancellationToken)
{
    return FramedSocketProtocol.SendAsync(
        stream,
        FramedSocketProtocol.CreateEnvelope(MessageType.WebcamDevices, session, studentId, Environment.MachineName, new()
        {
            ["count"] = "2",
            ["camera.0.id"] = "camera-0",
            ["camera.0.index"] = "0",
            ["camera.0.name"] = "Simulator Camera 0",
            ["camera.0.available"] = "1",
            ["camera.0.status"] = "available",
            ["camera.1.id"] = "camera-1",
            ["camera.1.index"] = "1",
            ["camera.1.name"] = "Simulator Virtual Camera",
            ["camera.1.available"] = "1",
            ["camera.1.status"] = "available"
        }, connectionId),
        null,
        cancellationToken);
}

static async Task ReceiveLoopAsync(NetworkStream stream, string studentId, CancellationToken cancellationToken)
{
    try
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            ReceivedFrame? frame = await FramedSocketProtocol.ReceiveAsync(stream, cancellationToken);
            if (frame is null)
            {
                break;
            }

            if (frame.Envelope.MessageType == MessageType.Error)
            {
                Console.WriteLine($"{studentId} error: {frame.Envelope.Metadata.GetValueOrDefault("message", "unknown")}");
            }
        }
    }
    catch
    {
    }
}

static byte[] GenerateFrame(string studentId)
{
    using Bitmap bitmap = new(800, 450);
    using Graphics graphics = Graphics.FromImage(bitmap);
    graphics.Clear(Color.FromArgb(34, 47, 62));
    using Font titleFont = new("Segoe UI", 38, FontStyle.Bold);
    using Font bodyFont = new("Segoe UI", 18, FontStyle.Regular);
    graphics.DrawString(studentId, titleFont, Brushes.White, new PointF(36, 42));
    graphics.DrawString(DateTime.Now.ToString("HH:mm:ss"), bodyFont, Brushes.LightGreen, new PointF(42, 130));
    graphics.DrawString("Simulated exam workstation", bodyFont, Brushes.LightGray, new PointF(42, 175));

    using MemoryStream output = new();
    ImageCodecInfo jpegCodec = ImageCodecInfo.GetImageEncoders().First(x => x.MimeType == "image/jpeg");
    using EncoderParameters parameters = new(1);
    parameters.Param[0] = new EncoderParameter(Encoder.Quality, 55L);
    bitmap.Save(output, jpegCodec, parameters);
    return output.ToArray();
}

static byte[] GenerateWebcamFrame(string studentId)
{
    using Bitmap bitmap = new(640, 480);
    using Graphics graphics = Graphics.FromImage(bitmap);
    graphics.Clear(Color.FromArgb(52, 73, 94));
    using Font titleFont = new("Segoe UI", 26, FontStyle.Bold);
    using Font bodyFont = new("Segoe UI", 16, FontStyle.Regular);
    graphics.FillEllipse(Brushes.Silver, 220, 60, 190, 190);
    graphics.DrawString("Webcam", titleFont, Brushes.White, new PointF(28, 22));
    graphics.DrawString(studentId, bodyFont, Brushes.LightGreen, new PointF(28, 300));
    graphics.DrawString(DateTime.Now.ToString("HH:mm:ss"), bodyFont, Brushes.LightGray, new PointF(28, 334));

    using MemoryStream output = new();
    ImageCodecInfo jpegCodec = ImageCodecInfo.GetImageEncoders().First(x => x.MimeType == "image/jpeg");
    using EncoderParameters parameters = new(1);
    parameters.Param[0] = new EncoderParameter(Encoder.Quality, 60L);
    bitmap.Save(output, jpegCodec, parameters);
    return output.ToArray();
}

internal sealed record Options(string Host, int Port, string Session, string Token, int Count, string Prefix, int WebcamIntervalMs, bool DuplicateCode)
{
    public static Options Parse(string[] args)
    {
        string GetValue(string name, string fallback)
        {
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
        }

        return new Options(
            GetValue("--host", "127.0.0.1"),
            int.Parse(GetValue("--port", "9090")),
            GetValue("--session", "EXAM-001"),
            GetValue("--token", "classroom-token"),
            int.Parse(GetValue("--count", "5")),
            GetValue("--prefix", "SIM"),
            int.Parse(GetValue("--webcam-interval-ms", GetValue("--webcam-interval", "500"))),
            args.Contains("--duplicate-code"));
    }
}
