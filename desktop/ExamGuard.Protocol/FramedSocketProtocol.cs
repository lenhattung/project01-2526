using System.Buffers.Binary;
using System.Text;
using System.Text.Json;

namespace ExamGuard.Protocol;

public static class FramedSocketProtocol
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public static async Task SendAsync(
        Stream stream,
        SocketEnvelope envelope,
        byte[]? payload,
        CancellationToken cancellationToken = default)
    {
        payload ??= [];
        envelope.PayloadLength = payload.Length;
        envelope.Timestamp = DateTimeOffset.UtcNow;

        byte[] header = JsonSerializer.SerializeToUtf8Bytes(envelope, JsonOptions);
        byte[] headerLength = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(headerLength, header.Length);

        await stream.WriteAsync(headerLength, cancellationToken);
        await stream.WriteAsync(header, cancellationToken);
        if (payload.Length > 0)
        {
            await stream.WriteAsync(payload, cancellationToken);
        }

        await stream.FlushAsync(cancellationToken);
    }

    public static async Task<ReceivedFrame?> ReceiveAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        byte[] headerLength = new byte[4];
        if (!await ReadExactAsync(stream, headerLength, cancellationToken))
        {
            return null;
        }

        int length = BinaryPrimitives.ReadInt32BigEndian(headerLength);
        if (length <= 0 || length > 1024 * 1024)
        {
            throw new InvalidDataException($"Invalid header length: {length}");
        }

        byte[] header = new byte[length];
        if (!await ReadExactAsync(stream, header, cancellationToken))
        {
            return null;
        }

        SocketEnvelope envelope = JsonSerializer.Deserialize<SocketEnvelope>(header, JsonOptions)
            ?? throw new InvalidDataException("Invalid socket header.");

        if (envelope.PayloadLength < 0 || envelope.PayloadLength > 50 * 1024 * 1024)
        {
            throw new InvalidDataException($"Invalid payload length: {envelope.PayloadLength}");
        }

        byte[] payload = new byte[envelope.PayloadLength];
        if (payload.Length > 0 && !await ReadExactAsync(stream, payload, cancellationToken))
        {
            return null;
        }

        return new ReceivedFrame(envelope, payload);
    }

    public static SocketEnvelope CreateEnvelope(
        string messageType,
        string sessionId,
        string studentId = "",
        string machineName = "",
        Dictionary<string, string>? metadata = null,
        string connectionId = "")
    {
        return new SocketEnvelope
        {
            MessageType = messageType,
            SessionId = sessionId,
            ConnectionId = string.IsNullOrWhiteSpace(connectionId) ? studentId : connectionId,
            StudentId = studentId,
            MachineName = machineName,
            Metadata = metadata ?? new Dictionary<string, string>()
        };
    }

    private static async Task<bool> ReadExactAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        int offset = 0;
        while (offset < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(offset), cancellationToken);
            if (read == 0)
            {
                return false;
            }

            offset += read;
        }

        return true;
    }
}
