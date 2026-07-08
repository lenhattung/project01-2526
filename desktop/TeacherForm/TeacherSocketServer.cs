using System.Net;
using System.Net.Sockets;
using ExamGuard.Protocol;

namespace TeacherForm;

internal sealed class TeacherSocketServer : ITeacherSessionTransport
{
    private const int MaxLiveImagePayloadBytes = 2 * 1024 * 1024;
    private const int MaxFileChunkBytes = 2 * 1024 * 1024;

    private readonly Func<PolicySnapshot> _policyFactory;
    private readonly Action<StudentState> _studentChanged;
    private readonly Action<string> _log;
    private readonly Action<StudentState, string, string>? _violationReceived;
    private readonly Action<StudentState, string, string>? _submissionCompleted;
    private readonly Action<StudentState, string, string>? _chatReceived;
    private readonly Action<StudentState, string>? _handRaised;
    private readonly Action<StudentState, string, Dictionary<string, string>>? _activityReceived;
    private readonly Action<StudentState, string, string>? _disconnectNotified;
    private readonly Action<StudentState>? _studentConnected;
    private readonly SubmissionReceiver _submissionReceiver;
    private readonly Dictionary<string, ClientContext> _clients = new(StringComparer.OrdinalIgnoreCase);
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private string _sessionId = "";
    private string _sessionToken = "";

    public TeacherSocketServer(
        Func<PolicySnapshot> policyFactory,
        Action<StudentState> studentChanged,
        Action<string> log,
        Action<StudentState, string, string>? violationReceived,
        Action<StudentState, string, string>? submissionCompleted,
        SubmissionReceiver submissionReceiver,
        Action<StudentState, string, string>? chatReceived = null,
        Action<StudentState, string>? handRaised = null,
        Action<StudentState, string, Dictionary<string, string>>? activityReceived = null,
        Action<StudentState, string, string>? disconnectNotified = null,
        Action<StudentState>? studentConnected = null)
    {
        _policyFactory = policyFactory;
        _studentChanged = studentChanged;
        _log = log;
        _violationReceived = violationReceived;
        _submissionCompleted = submissionCompleted;
        _submissionReceiver = submissionReceiver;
        _chatReceived = chatReceived;
        _handRaised = handRaised;
        _activityReceived = activityReceived;
        _disconnectNotified = disconnectNotified;
        _studentConnected = studentConnected;
    }

    public IReadOnlyCollection<StudentState> Students => _clients.Values.Select(x => x.State).ToList();

    public async Task StartAsync(int port, string sessionId, string sessionToken)
    {
        if (_listener is not null)
        {
            return;
        }

        _sessionId = sessionId;
        _sessionToken = sessionToken;
        _cts = new CancellationTokenSource();
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();
        _log($"Đang lắng nghe kết nối sinh viên tại cổng {port}.");

        _ = Task.Run(() => AcceptLoopAsync(_cts.Token));
        await Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_cts is null)
        {
            return;
        }

        await _cts.CancelAsync();
        _listener?.Stop();
        _listener = null;

        foreach (ClientContext client in _clients.Values)
        {
            client.Dispose();
        }

        _clients.Clear();
        _log("Đã dừng phiên giám sát.");
    }

    public async Task BroadcastPolicyAsync()
    {
        PolicySnapshot policy = _policyFactory();
        foreach (ClientContext client in _clients.Values.ToList())
        {
            await client.SendAsync(MessageType.PolicyUpdate, policy.ToMetadata(), null);
        }

        _log($"Đã gửi quy định tới {_clients.Count} sinh viên.");
    }

    public Task SendChatAsync(string? studentId, string message)
    {
        return SendCommandAsync(studentId, MessageType.ChatMessage, new()
        {
            ["message"] = message
        });
    }

    public Task SendAttentionAsync(string? studentId, string message)
    {
        return SendCommandAsync(studentId, MessageType.Attention, new()
        {
            ["message"] = message
        });
    }

    public Task SendHandRaiseClearAsync(string? studentId)
    {
        foreach (ClientContext target in GetTargets(studentId))
        {
            target.State.HandRaised = false;
            target.State.UnreadChatCount = 0;
            _studentChanged(target.State);
        }

        return SendCommandAsync(studentId, MessageType.HandRaiseClear, new()
        {
            ["message"] = "Giáo viên đã xử lý yêu cầu hỗ trợ."
        });
    }

    public Task SendLockAsync(string? studentId, string message)
    {
        return SendCommandAsync(studentId, MessageType.LockScreen, new()
        {
            ["message"] = message
        });
    }

    public Task SendUnlockAsync(string? studentId)
    {
        return SendCommandAsync(studentId, MessageType.UnlockScreen, new());
    }

    public Task SendExecuteCommandAsync(string? studentId, string command)
    {
        return SendCommandAsync(studentId, MessageType.ExecuteCommand, new()
        {
            ["command"] = command
        });
    }

    public Task SendRemoteMouseClickAsync(string studentId, double relativeX, double relativeY, string button)
    {
        return SendCommandAsync(studentId, MessageType.RemoteMouseClick, new()
        {
            ["relativeX"] = relativeX.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture),
            ["relativeY"] = relativeY.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture),
            ["button"] = button
        });
    }

    public Task SendRemoteTextInputAsync(string? studentId, string text)
    {
        return SendCommandAsync(studentId, MessageType.RemoteTextInput, new()
        {
            ["text"] = text
        });
    }

    public Task SendClipboardSetAsync(string? studentId, string text)
    {
        return SendCommandAsync(studentId, MessageType.ClipboardSet, new()
        {
            ["text"] = text
        });
    }

    public Task SendTeacherFrameAsync(byte[] jpeg)
    {
        return SendCommandAsync(null, MessageType.TeacherFrame, new()
        {
            ["mode"] = "broadcast"
        }, jpeg);
    }

    public async Task DistributeFileAsync(string? studentId, string filePath, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        FileInfo file = new(filePath);
        string fileName = file.Name;
        string targetId = studentId ?? "toàn bộ sinh viên";
        List<ClientContext> targets = GetTargets(studentId);
        if (targets.Count == 0)
        {
            _log("Không có sinh viên trực tuyến để phát tệp.");
            return;
        }

        foreach (ClientContext client in targets)
        {
            await client.SendAsync(MessageType.FileDistributionStart, new()
            {
                ["fileName"] = fileName,
                ["totalBytes"] = file.Length.ToString()
            }, null);
        }

        byte[] buffer = new byte[64 * 1024];
        long sent = 0;
        int index = 0;
        await using FileStream stream = File.OpenRead(filePath);
        int read;
        while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
        {
            byte[] chunk = buffer[..read];
            foreach (ClientContext client in targets.ToList())
            {
                await client.SendAsync(MessageType.FileDistributionChunk, new()
                {
                    ["fileName"] = fileName,
                    ["index"] = index.ToString(),
                    ["offset"] = sent.ToString()
                }, chunk);
            }

            sent += read;
            index++;
            progress?.Report((int)Math.Round(sent * 100.0 / file.Length));
        }

        foreach (ClientContext client in targets)
        {
            await client.SendAsync(MessageType.FileDistributionComplete, new()
            {
                ["fileName"] = fileName,
                ["totalBytes"] = file.Length.ToString()
            }, null);
        }

        _log($"Đã phát {fileName} tới {targetId}.");
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _cts?.Dispose();
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listener is not null)
        {
            try
            {
                TcpClient tcpClient = await _listener.AcceptTcpClientAsync(cancellationToken);
                _ = Task.Run(() => HandleClientAsync(tcpClient, cancellationToken), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _log($"Lỗi nhận kết nối: {ex.Message}");
            }
        }
    }

    private async Task HandleClientAsync(TcpClient tcpClient, CancellationToken cancellationToken)
    {
        string remote = tcpClient.Client.RemoteEndPoint?.ToString() ?? "unknown";
        await using NetworkStream stream = tcpClient.GetStream();
        ClientContext? context = null;

        try
        {
            ReceivedFrame? hello = await FramedSocketProtocol.ReceiveAsync(stream, cancellationToken);
            if (hello?.Envelope.MessageType != MessageType.Hello)
            {
                throw new InvalidDataException("Thông điệp đầu tiên phải là HELLO.");
            }

            if (!hello.Envelope.SessionId.Equals(_sessionId, StringComparison.OrdinalIgnoreCase) ||
                !hello.Envelope.Metadata.TryGetValue("token", out string? token) ||
                token != _sessionToken)
            {
                await FramedSocketProtocol.SendAsync(
                    stream,
                    FramedSocketProtocol.CreateEnvelope(MessageType.Error, _sessionId, metadata: new()
                    {
                        ["message"] = "Sai mã phiên hoặc mã bảo vệ."
                    }),
                    null,
                    cancellationToken);
                return;
            }

            string studentCode = hello.Envelope.Metadata.GetValueOrDefault("studentCode", hello.Envelope.StudentId);
            StudentState state = new()
            {
                StudentId = hello.Envelope.StudentId,
                StudentCode = studentCode,
                StudentName = hello.Envelope.Metadata.GetValueOrDefault("studentName", ""),
                WindowsUserName = hello.Envelope.Metadata.GetValueOrDefault("windowsUserName", ""),
                MachineName = hello.Envelope.Metadata.GetValueOrDefault("machineName", hello.Envelope.MachineName),
                RemoteEndPoint = remote,
                IsOnline = true
            };

            if (_clients.TryGetValue(state.StudentId, out ClientContext? existing))
            {
                existing.State.IsOnline = false;
                existing.State.LastViolation = "Kết nối cũ đã được thay thế.";
                _studentChanged(existing.State);
                existing.Dispose();
                _clients.Remove(state.StudentId);
                _log($"{existing.State.DisplayName} đã thay thế kết nối cũ.");
            }

            context = new ClientContext(tcpClient, stream, state, _sessionId);
            _clients[state.StudentId] = context;
            _studentChanged(state);
            _studentConnected?.Invoke(state);
            _log($"{state.DisplayName} đã kết nối từ {remote}.");

            await context.SendAsync(MessageType.HelloAck, _policyFactory().ToMetadata(), null);
            await context.SendAsync(MessageType.PolicyUpdate, _policyFactory().ToMetadata(), null);

            while (!cancellationToken.IsCancellationRequested)
            {
                ReceivedFrame? frame = await FramedSocketProtocol.ReceiveAsync(stream, cancellationToken);
                if (frame is null)
                {
                    break;
                }

                if (!frame.Envelope.SessionId.Equals(_sessionId, StringComparison.OrdinalIgnoreCase) ||
                    !frame.Envelope.StudentId.Equals(state.StudentId, StringComparison.OrdinalIgnoreCase))
                {
                    _log($"Bỏ qua thông điệp sai phiên hoặc sai mã sinh viên từ {remote}.");
                    continue;
                }

                await HandleFrameAsync(context, frame, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _log($"Kết nối {remote} lỗi: {ex.Message}");
        }
        finally
        {
            if (context is not null)
            {
                context.State.IsOnline = false;
                context.State.LastViolation = string.IsNullOrWhiteSpace(context.State.LastViolation)
                    ? "Mất kết nối"
                    : context.State.LastViolation;
                _studentChanged(context.State);
                _disconnectNotified?.Invoke(context.State, "student_disconnected", "Mất socket hoặc sinh viên đã thoát ứng dụng.");
                _submissionReceiver.AbortStudent(_sessionId, context.State.StudentId);
                if (_clients.TryGetValue(context.State.StudentId, out ClientContext? active) &&
                    ReferenceEquals(active, context))
                {
                    _clients.Remove(context.State.StudentId);
                }

                _log($"{context.State.DisplayName} đã mất kết nối.");
                context.Dispose();
            }
            else
            {
                tcpClient.Dispose();
            }
        }
    }

    private async Task HandleFrameAsync(ClientContext client, ReceivedFrame frame, CancellationToken cancellationToken)
    {
        client.State.LastSeen = DateTimeOffset.Now;

        if (!ValidatePayloadLimit(frame))
        {
            _log($"Bỏ qua {frame.Envelope.MessageType} từ {client.State.DisplayName}: payload quá lớn.");
            return;
        }

        switch (frame.Envelope.MessageType)
        {
            case MessageType.Heartbeat:
                _studentChanged(client.State);
                break;
            case MessageType.ScreenFrame:
                UpdateImage(frame.Payload, image =>
                {
                    Image? old = client.State.LatestFrame;
                    client.State.LatestFrame = image;
                    old?.Dispose();
                });
                _studentChanged(client.State);
                break;
            case MessageType.WebcamFrame:
                UpdateImage(frame.Payload, image =>
                {
                    Image? old = client.State.LatestWebcamFrame;
                    client.State.LatestWebcamFrame = image;
                    client.State.LastWebcamSeen = DateTimeOffset.Now;
                    client.State.WebcamStatus = "Đang hoạt động";
                    old?.Dispose();
                });
                _studentChanged(client.State);
                break;
            case MessageType.WebcamStatus:
                client.State.WebcamStatus = frame.Envelope.Metadata.GetValueOrDefault("message", "Trạng thái webcam đã được cập nhật.");
                _studentChanged(client.State);
                _log($"Webcam {client.State.DisplayName}: {client.State.WebcamStatus}");
                break;
            case MessageType.ProcessViolation:
                frame.Envelope.Metadata.TryGetValue("processName", out string? process);
                frame.Envelope.Metadata.TryGetValue("windowTitle", out string? title);
                client.State.LastViolation = $"{process} {title}".Trim();
                _studentChanged(client.State);
                _log($"Vi phạm từ {client.State.DisplayName}: {client.State.LastViolation}");
                _violationReceived?.Invoke(client.State, process ?? "", title ?? "");
                break;
            case MessageType.StudentActivityEvent:
                if (client.IsRateLimited("activity", TimeSpan.FromSeconds(2)))
                {
                    break;
                }

                HandleStudentActivity(client, frame.Envelope.Metadata);
                break;
            case MessageType.HandRaise:
                if (client.IsRateLimited("hand_raise", TimeSpan.FromSeconds(5)))
                {
                    break;
                }

                frame.Envelope.Metadata.TryGetValue("message", out string? helpMessage);
                client.State.HandRaised = true;
                client.State.LastActivityEvent = "Dơ tay";
                _studentChanged(client.State);
                _log($"{client.State.DisplayName} đã dơ tay xin hỗ trợ.");
                _handRaised?.Invoke(client.State, helpMessage ?? "");
                break;
            case MessageType.HandRaiseClear:
                client.State.HandRaised = false;
                client.State.LastActivityEvent = "Hạ tay";
                _studentChanged(client.State);
                _activityReceived?.Invoke(client.State, "hand_raise_cleared", frame.Envelope.Metadata);
                _log($"{client.State.DisplayName} đã tắt dơ tay.");
                break;
            case MessageType.SubmissionStart:
                await HandleSubmissionStartAsync(client, frame);
                break;
            case MessageType.SubmissionChunk:
                await HandleSubmissionChunkAsync(client, frame, cancellationToken);
                break;
            case MessageType.SubmissionComplete:
                await HandleSubmissionCompleteAsync(client, frame);
                break;
            case MessageType.ChatMessage:
                if (client.IsRateLimited("chat", TimeSpan.FromSeconds(1)))
                {
                    break;
                }

                frame.Envelope.Metadata.TryGetValue("message", out string? chat);
                client.State.UnreadChatCount++;
                client.State.LastActivityEvent = "Tin nhắn mới";
                _studentChanged(client.State);
                _log($"Tin nhắn từ {client.State.DisplayName}: {chat}");
                _chatReceived?.Invoke(client.State, chat ?? "", "student_to_teacher");
                break;
            case MessageType.StudentDisconnecting:
                frame.Envelope.Metadata.TryGetValue("reason", out string? reason);
                client.State.LastViolation = "Sinh viên chủ động ngắt kết nối";
                _studentChanged(client.State);
                _disconnectNotified?.Invoke(client.State, "student_disconnect_warning", reason ?? "");
                _log($"{client.State.DisplayName} báo ngắt kết nối: {reason}");
                break;
            case MessageType.CommandResult:
                frame.Envelope.Metadata.TryGetValue("command", out string? command);
                frame.Envelope.Metadata.TryGetValue("exitCode", out string? exitCode);
                frame.Envelope.Metadata.TryGetValue("output", out string? output);
                _log($"Kết quả lệnh từ {client.State.DisplayName}: {command} exit={exitCode} {output}");
                break;
        }
    }

    private void HandleStudentActivity(ClientContext client, Dictionary<string, string> metadata)
    {
        string eventType = metadata.GetValueOrDefault("eventType", "student_activity_event");
        client.State.LastActivityEvent = eventType;
        if (eventType is "clipboard_blocked" or "website_blocked")
        {
            client.State.LastViolation = eventType == "clipboard_blocked"
                ? "Đã chặn copy/paste"
                : $"Đã chặn website: {metadata.GetValueOrDefault("windowTitle", "")}";
        }

        _studentChanged(client.State);
        _activityReceived?.Invoke(client.State, eventType, metadata);
        _log($"Sự kiện từ {client.State.DisplayName}: {eventType}");
    }

    private static void UpdateImage(byte[] payload, Action<Image> setImage)
    {
        using MemoryStream ms = new(payload);
        using Image next = Image.FromStream(ms);
        setImage((Image)next.Clone());
    }

    private static bool ValidatePayloadLimit(ReceivedFrame frame)
    {
        int length = frame.Payload.Length;
        return frame.Envelope.MessageType switch
        {
            MessageType.ScreenFrame or MessageType.WebcamFrame => length <= MaxLiveImagePayloadBytes,
            MessageType.SubmissionChunk or MessageType.FileDistributionChunk => length <= MaxFileChunkBytes,
            _ => length <= MaxLiveImagePayloadBytes
        };
    }

    private async Task SendCommandAsync(string? studentId, string messageType, Dictionary<string, string> metadata, byte[]? payload = null)
    {
        List<ClientContext> targets = GetTargets(studentId);
        if (targets.Count == 0)
        {
            _log("Không có sinh viên trực tuyến cho thao tác này.");
            return;
        }

        foreach (ClientContext client in targets)
        {
            await client.SendAsync(messageType, metadata, payload);
        }

        _log($"Đã gửi {messageType} tới {(studentId ?? "toàn bộ sinh viên")}.");
    }

    private List<ClientContext> GetTargets(string? studentId)
    {
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return _clients.Values.Where(x => x.State.IsOnline).ToList();
        }

        return _clients.TryGetValue(studentId, out ClientContext? client) && client.State.IsOnline ? [client] : [];
    }

    private Task HandleSubmissionStartAsync(ClientContext client, ReceivedFrame frame)
    {
        string fileName = frame.Envelope.Metadata.GetValueOrDefault("fileName", "submission.zip");
        string path = _submissionReceiver.Start(_sessionId, client.State.StudentCodeOrId, fileName);
        client.State.SubmissionStatus = "Đang nhận";
        _studentChanged(client.State);
        _log($"Đang nhận {fileName} từ {client.State.DisplayName} vào {path}.");
        return Task.CompletedTask;
    }

    private Task HandleSubmissionChunkAsync(ClientContext client, ReceivedFrame frame, CancellationToken cancellationToken)
    {
        string fileName = frame.Envelope.Metadata.GetValueOrDefault("fileName", "submission.zip");
        return _submissionReceiver.AppendAsync(_sessionId, client.State.StudentCodeOrId, fileName, frame.Payload, cancellationToken);
    }

    private async Task HandleSubmissionCompleteAsync(ClientContext client, ReceivedFrame frame)
    {
        string fileName = frame.Envelope.Metadata.GetValueOrDefault("fileName", "submission.zip");
        string hash = frame.Envelope.Metadata.GetValueOrDefault("sha256", "");
        string path = await _submissionReceiver.CompleteAsync(_sessionId, client.State.StudentCodeOrId, fileName, hash);
        client.State.SubmissionStatus = "Đã nộp";
        _studentChanged(client.State);
        _log($"Đã nhận bài từ {client.State.DisplayName}: {path}");
        _submissionCompleted?.Invoke(client.State, path, hash);
    }

    private sealed class ClientContext : IDisposable
    {
        private readonly TcpClient _tcpClient;
        private readonly NetworkStream _stream;
        private readonly SemaphoreSlim _sendLock = new(1, 1);
        private readonly Dictionary<string, DateTimeOffset> _rateLimits = new(StringComparer.OrdinalIgnoreCase);
        private readonly string _sessionId;

        public ClientContext(TcpClient tcpClient, NetworkStream stream, StudentState state, string sessionId)
        {
            _tcpClient = tcpClient;
            _stream = stream;
            State = state;
            _sessionId = sessionId;
        }

        public StudentState State { get; }

        public bool IsRateLimited(string key, TimeSpan interval)
        {
            DateTimeOffset now = DateTimeOffset.Now;
            if (_rateLimits.TryGetValue(key, out DateTimeOffset last) && now - last < interval)
            {
                return true;
            }

            _rateLimits[key] = now;
            return false;
        }

        public async Task SendAsync(string messageType, Dictionary<string, string>? metadata, byte[]? payload)
        {
            await _sendLock.WaitAsync();
            try
            {
                SocketEnvelope envelope = FramedSocketProtocol.CreateEnvelope(
                    messageType,
                    _sessionId,
                    State.StudentId,
                    Environment.MachineName,
                    metadata);
                await FramedSocketProtocol.SendAsync(_stream, envelope, payload);
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public void Dispose()
        {
            _sendLock.Dispose();
            _tcpClient.Dispose();
        }
    }
}
