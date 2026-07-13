using System.Net.Sockets;
using ExamGuard.Protocol;

namespace StudentForm;

internal sealed class StudentSocketClient : IAsyncDisposable
{
    private readonly string _studentId;
    private readonly string _studentName;
    private readonly string _windowsUserName;
    private readonly string _sessionId;
    private readonly string _sessionToken;
    private readonly string _connectionId = Guid.NewGuid().ToString("N");
    private readonly Action<PolicySnapshot> _policyChanged;
    private readonly Func<ReceivedFrame, Task> _frameReceived;
    private readonly Action<string> _log;
    private readonly Action _handshakeCompleted;
    private readonly Action<string> _connectionClosed;
    private readonly bool _useRelay;
    private readonly string _relaySecret;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private CancellationTokenSource? _cts;

    public StudentSocketClient(
        string studentId,
        string studentName,
        string windowsUserName,
        string sessionId,
        string sessionToken,
        Action<PolicySnapshot> policyChanged,
        Func<ReceivedFrame, Task> frameReceived,
        Action<string> log,
        Action handshakeCompleted,
        Action<string> connectionClosed,
        bool useRelay = false,
        string relaySecret = "")
    {
        _studentId = studentId;
        _studentName = studentName;
        _windowsUserName = windowsUserName;
        _sessionId = sessionId;
        _sessionToken = sessionToken;
        _policyChanged = policyChanged;
        _frameReceived = frameReceived;
        _log = log;
        _handshakeCompleted = handshakeCompleted;
        _connectionClosed = connectionClosed;
        _useRelay = useRelay;
        _relaySecret = relaySecret;
    }

    public bool IsConnected => _tcpClient?.Connected == true;
    public string ConnectionId => _connectionId;

    public async Task ConnectAsync(string host, int port)
    {
        _cts = new CancellationTokenSource();
        _tcpClient = new TcpClient();
        _tcpClient.NoDelay = true;
        _tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        await _tcpClient.ConnectAsync(host, port, _cts.Token);
        _stream = _tcpClient.GetStream();

        Dictionary<string, string> metadata = new()
        {
            ["token"] = _sessionToken,
            ["connectionId"] = _connectionId,
            ["os"] = Environment.OSVersion.ToString(),
            ["studentCode"] = _studentId,
            ["studentName"] = _studentName,
            ["windowsUserName"] = _windowsUserName,
            ["machineName"] = Environment.MachineName
        };
        if (_useRelay)
        {
            metadata["role"] = "student";
            metadata["relaySecret"] = _relaySecret;
        }

        await SendAsync(MessageType.Hello, metadata);

        _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));
        _log($"Đã mở kết nối tới {host}:{port}.");
    }

    public async Task DisconnectAsync()
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();
        }

        _stream?.Dispose();
        _tcpClient?.Dispose();
        _stream = null;
        _tcpClient = null;
        _log("Đã ngắt kết nối.");
    }

    public Task SendHeartbeatAsync() => SendAsync(MessageType.Heartbeat);

    public Task SendScreenFrameAsync(byte[] jpeg) => SendAsync(MessageType.ScreenFrame, payload: jpeg);

    public Task SendWebcamFrameAsync(byte[] jpeg, string cameraId = "")
    {
        Dictionary<string, string>? metadata = string.IsNullOrWhiteSpace(cameraId)
            ? null
            : new Dictionary<string, string> { ["cameraId"] = cameraId };
        return SendAsync(MessageType.WebcamFrame, metadata, jpeg);
    }

    public Task SendWebcamDevicesAsync(IReadOnlyList<WebcamDeviceDescriptor> devices)
    {
        Dictionary<string, string> metadata = new()
        {
            ["count"] = devices.Count.ToString()
        };

        for (int i = 0; i < devices.Count; i++)
        {
            WebcamDeviceDescriptor device = devices[i];
            metadata[$"camera.{i}.id"] = device.CameraId;
            metadata[$"camera.{i}.index"] = device.CameraIndex.ToString();
            metadata[$"camera.{i}.name"] = device.DisplayName;
            metadata[$"camera.{i}.available"] = device.IsAvailable ? "1" : "0";
            metadata[$"camera.{i}.status"] = device.Status;
        }

        return SendAsync(MessageType.WebcamDevices, metadata);
    }

    public Task SendWebcamStatusAsync(string status, string message)
    {
        return SendAsync(MessageType.WebcamStatus, new Dictionary<string, string>
        {
            ["status"] = status,
            ["message"] = message
        });
    }

    public Task SendViolationAsync(string processName, string windowTitle, string action)
    {
        return SendAsync(MessageType.ProcessViolation, new Dictionary<string, string>
        {
            ["processName"] = processName,
            ["windowTitle"] = windowTitle,
            ["action"] = action
        });
    }

    public Task SendChatAsync(string message)
    {
        return SendAsync(MessageType.ChatMessage, new Dictionary<string, string>
        {
            ["message"] = message
        });
    }

    public Task SendHandRaiseAsync(string message)
    {
        return SendAsync(MessageType.HandRaise, new Dictionary<string, string>
        {
            ["message"] = message
        });
    }

    public Task SendHandRaiseClearAsync(string message)
    {
        return SendAsync(MessageType.HandRaiseClear, new Dictionary<string, string>
        {
            ["message"] = message
        });
    }

    public Task SendActivityEventAsync(string eventType, Dictionary<string, string> metadata)
    {
        metadata["eventType"] = eventType;
        return SendAsync(MessageType.StudentActivityEvent, metadata);
    }

    public Task SendDisconnectNoticeAsync(string reason)
    {
        return SendAsync(MessageType.StudentDisconnecting, new Dictionary<string, string>
        {
            ["reason"] = reason
        });
    }

    public Task SendCommandResultAsync(string command, int exitCode, string output)
    {
        if (output.Length > 1200)
        {
            output = output[..1200];
        }

        return SendAsync(MessageType.CommandResult, new Dictionary<string, string>
        {
            ["command"] = command,
            ["exitCode"] = exitCode.ToString(),
            ["output"] = output
        });
    }

    public async Task SendSubmissionAsync(string zipPath, IProgress<int> progress, CancellationToken cancellationToken)
    {
        string fileName = Path.GetFileName(zipPath);
        FileInfo file = new(zipPath);
        string hash = await Sha256FileAsync(zipPath, cancellationToken);

        await SendAsync(MessageType.SubmissionStart, new Dictionary<string, string>
        {
            ["fileName"] = fileName,
            ["totalBytes"] = file.Length.ToString(),
            ["sha256"] = hash
        }, cancellationToken: cancellationToken);

        byte[] buffer = new byte[64 * 1024];
        long sent = 0;
        await using FileStream stream = File.OpenRead(zipPath);
        int read;
        int index = 0;
        while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
        {
            byte[] chunk = buffer[..read];
            await SendAsync(MessageType.SubmissionChunk, new Dictionary<string, string>
            {
                ["fileName"] = fileName,
                ["index"] = index.ToString(),
                ["offset"] = sent.ToString()
            }, chunk, cancellationToken);

            sent += read;
            index++;
            progress.Report((int)Math.Round(sent * 100.0 / file.Length));
        }

        await SendAsync(MessageType.SubmissionComplete, new Dictionary<string, string>
        {
            ["fileName"] = fileName,
            ["totalBytes"] = file.Length.ToString(),
            ["sha256"] = hash
        }, cancellationToken: cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _sendLock.Dispose();
        _cts?.Dispose();
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        if (_stream is null)
        {
            return;
        }

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ReceivedFrame? frame = await FramedSocketProtocol.ReceiveAsync(_stream, cancellationToken);
                if (frame is null)
                {
                    break;
                }

                if (frame.Envelope.MessageType == MessageType.HelloAck)
                {
                    if (frame.Envelope.Metadata.GetValueOrDefault("relay", "") == "examguard")
                    {
                        _log("Đã kết nối máy chủ relay, đang chờ máy giáo viên xác nhận.");
                        continue;
                    }

                    _handshakeCompleted();
                    _policyChanged(PolicySnapshot.FromMetadata(frame.Envelope.Metadata));
                    _log("Máy giáo viên đã chấp nhận kết nối.");
                }
                else if (frame.Envelope.MessageType == MessageType.PolicyUpdate)
                {
                    _policyChanged(PolicySnapshot.FromMetadata(frame.Envelope.Metadata));
                    _log("Quy định đã được cập nhật từ giáo viên.");
                }
                else if (frame.Envelope.MessageType == MessageType.Error)
                {
                    string code = frame.Envelope.Metadata.GetValueOrDefault("code", "");
                    string message = frame.Envelope.Metadata.GetValueOrDefault("message", "May chu tra ve loi.");
                    _log($"Relay/giáo viên từ chối kết nối [{code}]: {message}");
                    if (code.Equals("DUPLICATE_STUDENT", StringComparison.OrdinalIgnoreCase))
                    {
                        _log(message);
                        await DisconnectAsync();
                        _connectionClosed(message);
                        return;
                    }

                    if (_useRelay && (code.Equals("INVALID_RELAY_SECRET", StringComparison.OrdinalIgnoreCase) ||
                                      code.Equals("INVALID_HELLO", StringComparison.OrdinalIgnoreCase)))
                    {
                        await DisconnectAsync();
                        _connectionClosed(message);
                        return;
                    }
                }
                else
                {
                    await _frameReceived(frame);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _log($"Nhận dữ liệu thất bại: {ex.Message}");
        }
        finally
        {
            _connectionClosed("Kết nối tới máy giáo viên đã đóng.");
        }
    }

    private async Task SendAsync(
        string messageType,
        Dictionary<string, string>? metadata = null,
        byte[]? payload = null,
        CancellationToken cancellationToken = default)
    {
        if (_stream is null)
        {
            return;
        }

        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            await FramedSocketProtocol.SendAsync(
                _stream,
                FramedSocketProtocol.CreateEnvelope(messageType, _sessionId, _studentId, Environment.MachineName, metadata, _connectionId),
                payload,
                cancellationToken);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private static async Task<string> Sha256FileAsync(string path, CancellationToken cancellationToken)
    {
        await using FileStream stream = File.OpenRead(path);
        byte[] hash = await System.Security.Cryptography.SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
