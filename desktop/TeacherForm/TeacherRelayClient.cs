using System.Net.Sockets;
using ExamGuard.Protocol;

namespace TeacherForm;

internal sealed class TeacherRelayClient : ITeacherSessionTransport
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
    private readonly Dictionary<string, RelayStudentContext> _clients = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private CancellationTokenSource? _cts;
    private string _sessionId = "";
    private string _sessionToken = "";

    public TeacherRelayClient(
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

    public async Task StartAsync(string host, int port, string sessionId, string sessionToken, string relaySecret)
    {
        if (_tcpClient is not null)
        {
            return;
        }

        _sessionId = sessionId;
        _sessionToken = sessionToken;
        _cts = new CancellationTokenSource();
        _tcpClient = new TcpClient();
        await _tcpClient.ConnectAsync(host, port, _cts.Token);
        _stream = _tcpClient.GetStream();

        await SendEnvelopeAsync(
            FramedSocketProtocol.CreateEnvelope(MessageType.Hello, _sessionId, "teacher", Environment.MachineName, new()
            {
                ["role"] = "teacher",
                ["token"] = _sessionToken,
                ["relaySecret"] = relaySecret,
                ["teacherMachine"] = Environment.MachineName
            }),
            null,
            _cts.Token);

        _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));
        _log($"Đã kết nối máy chủ relay {host}:{port} cho phiên {_sessionId}.");
    }

    public async Task StopAsync()
    {
        if (_cts is null)
        {
            return;
        }

        await _cts.CancelAsync();
        _stream?.Dispose();
        _tcpClient?.Dispose();
        _stream = null;
        _tcpClient = null;

        foreach (RelayStudentContext client in _clients.Values)
        {
            client.State.IsOnline = false;
            _studentChanged(client.State);
        }

        _clients.Clear();
        _log("Đã dừng kết nối relay.");
    }

    public async Task BroadcastPolicyAsync()
    {
        PolicySnapshot policy = _policyFactory();
        await SendCommandAsync(null, MessageType.PolicyUpdate, policy.ToMetadata());
        _log($"Đã gửi quy định tới {_clients.Count} sinh viên qua relay.");
    }

    public Task SendChatAsync(string? studentId, string message)
    {
        return SendCommandAsync(studentId, MessageType.ChatMessage, new() { ["message"] = message });
    }

    public Task SendAttentionAsync(string? studentId, string message)
    {
        return SendCommandAsync(studentId, MessageType.Attention, new() { ["message"] = message });
    }

    public Task SendHandRaiseClearAsync(string? studentId)
    {
        foreach (RelayStudentContext target in GetTargets(studentId))
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
        return SendCommandAsync(studentId, MessageType.LockScreen, new() { ["message"] = message });
    }

    public Task SendUnlockAsync(string? studentId)
    {
        return SendCommandAsync(studentId, MessageType.UnlockScreen, new());
    }

    public Task SendExecuteCommandAsync(string? studentId, string command)
    {
        return SendCommandAsync(studentId, MessageType.ExecuteCommand, new() { ["command"] = command });
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
        return SendCommandAsync(studentId, MessageType.RemoteTextInput, new() { ["text"] = text });
    }

    public Task SendClipboardSetAsync(string? studentId, string text)
    {
        return SendCommandAsync(studentId, MessageType.ClipboardSet, new() { ["text"] = text });
    }

    public Task SendTeacherFrameAsync(byte[] jpeg)
    {
        return SendCommandAsync(null, MessageType.TeacherFrame, new() { ["mode"] = "broadcast" }, jpeg);
    }

    public async Task DistributeFileAsync(string? studentId, string filePath, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        FileInfo file = new(filePath);
        List<RelayStudentContext> targets = GetTargets(studentId);
        if (targets.Count == 0)
        {
            _log("Không có sinh viên trực tuyến để phát tệp.");
            return;
        }

        await SendCommandAsync(studentId, MessageType.FileDistributionStart, new()
        {
            ["fileName"] = file.Name,
            ["totalBytes"] = file.Length.ToString()
        });

        byte[] buffer = new byte[64 * 1024];
        long sent = 0;
        int index = 0;
        await using FileStream stream = File.OpenRead(filePath);
        int read;
        while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
        {
            byte[] chunk = buffer[..read];
            await SendCommandAsync(studentId, MessageType.FileDistributionChunk, new()
            {
                ["fileName"] = file.Name,
                ["index"] = index.ToString(),
                ["offset"] = sent.ToString()
            }, chunk);

            sent += read;
            index++;
            progress?.Report((int)Math.Round(sent * 100.0 / file.Length));
        }

        await SendCommandAsync(studentId, MessageType.FileDistributionComplete, new()
        {
            ["fileName"] = file.Name,
            ["totalBytes"] = file.Length.ToString()
        });

        _log($"Đã phát {file.Name} qua relay.");
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
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

                if (frame.Envelope.MessageType == MessageType.HelloAck &&
                    frame.Envelope.Metadata.GetValueOrDefault("relay", "") == "examguard")
                {
                    _log("Máy chủ relay đã chấp nhận kết nối giáo viên.");
                    continue;
                }

                if (!frame.Envelope.SessionId.Equals(_sessionId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (frame.Envelope.MessageType == MessageType.Hello)
                {
                    await AcceptStudentHelloAsync(frame);
                    continue;
                }

                if (_clients.TryGetValue(frame.Envelope.StudentId, out RelayStudentContext? client))
                {
                    await HandleFrameAsync(client, frame, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _log($"Kết nối relay bị lỗi: {ex.Message}");
        }
        finally
        {
            foreach (RelayStudentContext client in _clients.Values.ToList())
            {
                MarkDisconnected(client, "Mất kết nối relay hoặc sinh viên đã thoát.");
            }
        }
    }

    private async Task AcceptStudentHelloAsync(ReceivedFrame hello)
    {
        if (!hello.Envelope.Metadata.TryGetValue("token", out string? token) || token != _sessionToken)
        {
            await SendToStudentAsync(hello.Envelope.StudentId, MessageType.Error, new()
            {
                ["message"] = "Sai mã phiên hoặc mã bảo vệ."
            });
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
            RemoteEndPoint = "relay",
            IsOnline = true
        };

        if (_clients.TryGetValue(state.StudentId, out RelayStudentContext? existing))
        {
            existing.State.IsOnline = false;
            existing.State.LastViolation = "Kết nối cũ đã được thay thế qua relay.";
            _studentChanged(existing.State);
            _clients.Remove(state.StudentId);
        }

        RelayStudentContext context = new(state);
        _clients[state.StudentId] = context;
        _studentChanged(state);
        _studentConnected?.Invoke(state);
        _log($"{state.DisplayName} đã kết nối qua relay.");

        PolicySnapshot policy = _policyFactory();
        await SendToStudentAsync(state.StudentId, MessageType.HelloAck, policy.ToMetadata());
        await SendToStudentAsync(state.StudentId, MessageType.PolicyUpdate, policy.ToMetadata());
    }

    private async Task HandleFrameAsync(RelayStudentContext client, ReceivedFrame frame, CancellationToken cancellationToken)
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
                if (!client.IsRateLimited("activity", TimeSpan.FromSeconds(2)))
                {
                    HandleStudentActivity(client, frame.Envelope.Metadata);
                }
                break;
            case MessageType.HandRaise:
                if (!client.IsRateLimited("hand_raise", TimeSpan.FromSeconds(5)))
                {
                    string helpMessage = frame.Envelope.Metadata.GetValueOrDefault("message", "");
                    client.State.HandRaised = true;
                    client.State.LastActivityEvent = "Dơ tay";
                    _studentChanged(client.State);
                    _log($"{client.State.DisplayName} đã dơ tay xin hỗ trợ.");
                    _handRaised?.Invoke(client.State, helpMessage);
                }
                break;
            case MessageType.HandRaiseClear:
                client.State.HandRaised = false;
                client.State.LastActivityEvent = "Hạ tay";
                _studentChanged(client.State);
                _activityReceived?.Invoke(client.State, "hand_raise_cleared", frame.Envelope.Metadata);
                _log($"{client.State.DisplayName} đã tắt dơ tay.");
                break;
            case MessageType.SubmissionStart:
                HandleSubmissionStart(client, frame);
                break;
            case MessageType.SubmissionChunk:
                await HandleSubmissionChunkAsync(client, frame, cancellationToken);
                break;
            case MessageType.SubmissionComplete:
                await HandleSubmissionCompleteAsync(client, frame);
                break;
            case MessageType.ChatMessage:
                if (!client.IsRateLimited("chat", TimeSpan.FromSeconds(1)))
                {
                    string chat = frame.Envelope.Metadata.GetValueOrDefault("message", "");
                    client.State.UnreadChatCount++;
                    client.State.LastActivityEvent = "Tin nhắn mới";
                    _studentChanged(client.State);
                    _log($"Tin nhắn từ {client.State.DisplayName}: {chat}");
                    _chatReceived?.Invoke(client.State, chat, "student_to_teacher");
                }
                break;
            case MessageType.StudentDisconnecting:
                string reason = frame.Envelope.Metadata.GetValueOrDefault("reason", "");
                client.State.LastViolation = "Sinh viên chủ động ngắt kết nối";
                _studentChanged(client.State);
                _disconnectNotified?.Invoke(client.State, "student_disconnect_warning", reason);
                _log($"{client.State.DisplayName} báo ngắt kết nối: {reason}");
                break;
            case MessageType.CommandResult:
                _log($"Kết quả lệnh từ {client.State.DisplayName}: {frame.Envelope.Metadata.GetValueOrDefault("command", "")} exit={frame.Envelope.Metadata.GetValueOrDefault("exitCode", "")} {frame.Envelope.Metadata.GetValueOrDefault("output", "")}");
                break;
        }
    }

    private void HandleStudentActivity(RelayStudentContext client, Dictionary<string, string> metadata)
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

    private void HandleSubmissionStart(RelayStudentContext client, ReceivedFrame frame)
    {
        string fileName = frame.Envelope.Metadata.GetValueOrDefault("fileName", "submission.zip");
        string path = _submissionReceiver.Start(_sessionId, client.State.StudentCodeOrId, fileName);
        client.State.SubmissionStatus = "Đang nhận";
        _studentChanged(client.State);
        _log($"Đang nhận {fileName} từ {client.State.DisplayName} vào {path}.");
    }

    private Task HandleSubmissionChunkAsync(RelayStudentContext client, ReceivedFrame frame, CancellationToken cancellationToken)
    {
        string fileName = frame.Envelope.Metadata.GetValueOrDefault("fileName", "submission.zip");
        return _submissionReceiver.AppendAsync(_sessionId, client.State.StudentCodeOrId, fileName, frame.Payload, cancellationToken);
    }

    private async Task HandleSubmissionCompleteAsync(RelayStudentContext client, ReceivedFrame frame)
    {
        string fileName = frame.Envelope.Metadata.GetValueOrDefault("fileName", "submission.zip");
        string hash = frame.Envelope.Metadata.GetValueOrDefault("sha256", "");
        string path = await _submissionReceiver.CompleteAsync(_sessionId, client.State.StudentCodeOrId, fileName, hash);
        client.State.SubmissionStatus = "Đã nộp";
        _studentChanged(client.State);
        _log($"Đã nhận bài từ {client.State.DisplayName}: {path}");
        _submissionCompleted?.Invoke(client.State, path, hash);
    }

    private void MarkDisconnected(RelayStudentContext client, string reason)
    {
        client.State.IsOnline = false;
        client.State.LastViolation = string.IsNullOrWhiteSpace(client.State.LastViolation) ? "Mất kết nối" : client.State.LastViolation;
        _studentChanged(client.State);
        _disconnectNotified?.Invoke(client.State, "student_disconnected", reason);
        _submissionReceiver.AbortStudent(_sessionId, client.State.StudentId);
    }

    private async Task SendCommandAsync(string? studentId, string messageType, Dictionary<string, string> metadata, byte[]? payload = null)
    {
        if (_stream is null)
        {
            _log("Chưa kết nối relay.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(studentId) && !_clients.ContainsKey(studentId))
        {
            _log("Không có sinh viên trực tuyến cho thao tác này.");
            return;
        }

        await SendToStudentAsync(studentId ?? "", messageType, metadata, payload);
        _log($"Đã gửi {messageType} tới {(studentId ?? "toàn bộ sinh viên")} qua relay.");
    }

    private Task SendToStudentAsync(string studentId, string messageType, Dictionary<string, string> metadata, byte[]? payload = null)
    {
        return SendEnvelopeAsync(
            FramedSocketProtocol.CreateEnvelope(messageType, _sessionId, studentId, Environment.MachineName, metadata),
            payload,
            _cts?.Token ?? CancellationToken.None);
    }

    private async Task SendEnvelopeAsync(SocketEnvelope envelope, byte[]? payload, CancellationToken cancellationToken)
    {
        if (_stream is null)
        {
            return;
        }

        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            await FramedSocketProtocol.SendAsync(_stream, envelope, payload, cancellationToken);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private List<RelayStudentContext> GetTargets(string? studentId)
    {
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return _clients.Values.Where(x => x.State.IsOnline).ToList();
        }

        return _clients.TryGetValue(studentId, out RelayStudentContext? client) && client.State.IsOnline ? [client] : [];
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

    private sealed class RelayStudentContext
    {
        private readonly Dictionary<string, DateTimeOffset> _rateLimits = new(StringComparer.OrdinalIgnoreCase);

        public RelayStudentContext(StudentState state)
        {
            State = state;
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
    }
}
