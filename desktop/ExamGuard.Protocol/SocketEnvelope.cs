namespace ExamGuard.Protocol;

public sealed class SocketEnvelope
{
    public string MessageType { get; set; } = "";
    public string SessionId { get; set; } = "";
    public string ConnectionId { get; set; } = "";
    public string StudentId { get; set; } = "";
    public string MachineName { get; set; } = "";
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public int PayloadLength { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}
