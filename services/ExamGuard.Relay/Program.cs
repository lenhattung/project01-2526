using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using ExamGuard.Protocol;

int port = ReadInt("RELAY_PORT", 9090);
int maxPayloadBytes = ReadInt("RELAY_MAX_PAYLOAD_BYTES", 50 * 1024 * 1024);
string relaySecret = Environment.GetEnvironmentVariable("RELAY_SHARED_SECRET") ?? "";

RelayHub hub = new(relaySecret, maxPayloadBytes);
TcpListener listener = new(IPAddress.Any, port);
listener.Start();
Console.WriteLine($"ExamGuard relay listening on TCP {port}.");

while (true)
{
    TcpClient client = await listener.AcceptTcpClientAsync();
    _ = Task.Run(() => hub.HandleClientAsync(client));
}

static int ReadInt(string key, int fallback)
{
    return int.TryParse(Environment.GetEnvironmentVariable(key), out int value) ? value : fallback;
}

internal sealed class RelayHub
{
    private readonly ConcurrentDictionary<string, RelayRoom> _rooms = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _relaySecret;
    private readonly int _maxPayloadBytes;

    public RelayHub(string relaySecret, int maxPayloadBytes)
    {
        _relaySecret = relaySecret;
        _maxPayloadBytes = maxPayloadBytes;
    }

    public async Task HandleClientAsync(TcpClient tcpClient)
    {
        string remote = tcpClient.Client.RemoteEndPoint?.ToString() ?? "unknown";
        RelayClient? relayClient = null;

        try
        {
            await using NetworkStream stream = tcpClient.GetStream();
            ReceivedFrame? hello = await FramedSocketProtocol.ReceiveAsync(stream);
            if (hello?.Envelope.MessageType != MessageType.Hello)
            {
                throw new InvalidDataException("First frame must be HELLO.");
            }

            ValidateHello(hello);
            string sessionId = hello.Envelope.SessionId.Trim();
            string role = hello.Envelope.Metadata.GetValueOrDefault("role", "student").Trim().ToLowerInvariant();
            if (role is not "teacher" and not "student")
            {
                throw new InvalidDataException("Relay role must be teacher or student.");
            }

            relayClient = new RelayClient(tcpClient, stream, hello.Envelope.StudentId, role, remote, hello);
            RelayRoom room = _rooms.GetOrAdd(sessionId, static id => new RelayRoom(id));
            room.Join(relayClient);
            Console.WriteLine($"{role} {relayClient.DisplayId} joined session {sessionId} from {remote}.");

            await relayClient.SendAsync(MessageType.HelloAck, sessionId, new()
            {
                ["relay"] = "examguard",
                ["role"] = role
            });

            if (role == "teacher")
            {
                await room.ForwardExistingStudentHellosAsync(relayClient);
            }
            else
            {
                await room.ForwardStudentHelloAsync(relayClient);
            }

            while (tcpClient.Connected)
            {
                ReceivedFrame? frame = await FramedSocketProtocol.ReceiveAsync(stream);
                if (frame is null)
                {
                    break;
                }

                if (!string.Equals(frame.Envelope.SessionId, sessionId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (frame.Payload.Length > _maxPayloadBytes)
                {
                    await relayClient.SendAsync(MessageType.Error, sessionId, new()
                    {
                        ["message"] = "Payload too large."
                    });
                    continue;
                }

                await room.ForwardAsync(relayClient, frame);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Relay client {remote} closed: {ex.Message}");
        }
        finally
        {
            if (relayClient is not null)
            {
                foreach (RelayRoom room in _rooms.Values)
                {
                    room.Leave(relayClient);
                }
            }

            tcpClient.Dispose();
        }
    }

    private void ValidateHello(ReceivedFrame hello)
    {
        if (string.IsNullOrWhiteSpace(hello.Envelope.SessionId))
        {
            throw new InvalidDataException("Session is required.");
        }

        if (!string.IsNullOrWhiteSpace(_relaySecret) &&
            !string.Equals(_relaySecret, "change-this-relay-secret", StringComparison.Ordinal) &&
            !string.Equals(hello.Envelope.Metadata.GetValueOrDefault("relaySecret", ""), _relaySecret, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Invalid relay secret.");
        }
    }
}

internal sealed class RelayRoom
{
    private readonly object _sync = new();
    private readonly Dictionary<string, RelayClient> _students = new(StringComparer.OrdinalIgnoreCase);
    private RelayClient? _teacher;

    public RelayRoom(string sessionId)
    {
        SessionId = sessionId;
    }

    public string SessionId { get; }

    public void Join(RelayClient client)
    {
        lock (_sync)
        {
            if (client.Role == "teacher")
            {
                _teacher?.Dispose();
                _teacher = client;
                return;
            }

            string studentId = string.IsNullOrWhiteSpace(client.StudentId) ? client.RemoteEndPoint : client.StudentId;
            if (_students.TryGetValue(studentId, out RelayClient? existing))
            {
                existing.Dispose();
            }

            _students[studentId] = client;
        }
    }

    public void Leave(RelayClient client)
    {
        lock (_sync)
        {
            if (ReferenceEquals(_teacher, client))
            {
                _teacher = null;
                return;
            }

            foreach ((string studentId, RelayClient existing) in _students.ToArray())
            {
                if (ReferenceEquals(existing, client))
                {
                    _students.Remove(studentId);
                }
            }
        }
    }

    public async Task ForwardAsync(RelayClient source, ReceivedFrame frame)
    {
        List<RelayClient> targets = ResolveTargets(source, frame);
        foreach (RelayClient target in targets)
        {
            await target.SendFrameAsync(frame);
        }
    }

    public async Task ForwardStudentHelloAsync(RelayClient student)
    {
        RelayClient? teacher;
        lock (_sync)
        {
            teacher = _teacher;
        }

        if (teacher is not null)
        {
            await teacher.SendFrameAsync(student.HelloFrame);
        }
    }

    public async Task ForwardExistingStudentHellosAsync(RelayClient teacher)
    {
        List<RelayClient> students;
        lock (_sync)
        {
            students = _students.Values.ToList();
        }

        foreach (RelayClient student in students)
        {
            await teacher.SendFrameAsync(student.HelloFrame);
        }
    }

    private List<RelayClient> ResolveTargets(RelayClient source, ReceivedFrame frame)
    {
        lock (_sync)
        {
            if (source.Role == "student")
            {
                return _teacher is null ? [] : [_teacher];
            }

            if (!string.IsNullOrWhiteSpace(frame.Envelope.StudentId) &&
                _students.TryGetValue(frame.Envelope.StudentId, out RelayClient? student))
            {
                return [student];
            }

            return _students.Values.ToList();
        }
    }
}

internal sealed class RelayClient : IDisposable
{
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly TcpClient _tcpClient;
    private readonly NetworkStream _stream;

    public RelayClient(TcpClient tcpClient, NetworkStream stream, string studentId, string role, string remoteEndPoint, ReceivedFrame helloFrame)
    {
        _tcpClient = tcpClient;
        _stream = stream;
        StudentId = studentId;
        Role = role;
        RemoteEndPoint = remoteEndPoint;
        HelloFrame = helloFrame;
    }

    public string StudentId { get; }
    public string Role { get; }
    public string RemoteEndPoint { get; }
    public ReceivedFrame HelloFrame { get; }
    public string DisplayId => string.IsNullOrWhiteSpace(StudentId) ? RemoteEndPoint : StudentId;

    public Task SendAsync(string messageType, string sessionId, Dictionary<string, string> metadata)
    {
        SocketEnvelope envelope = FramedSocketProtocol.CreateEnvelope(messageType, sessionId, StudentId, Environment.MachineName, metadata);
        return SendEnvelopeAsync(envelope, []);
    }

    public Task SendFrameAsync(ReceivedFrame frame)
    {
        return SendEnvelopeAsync(frame.Envelope, frame.Payload);
    }

    private async Task SendEnvelopeAsync(SocketEnvelope envelope, byte[] payload)
    {
        await _sendLock.WaitAsync();
        try
        {
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
        _stream.Dispose();
        _tcpClient.Dispose();
    }
}
