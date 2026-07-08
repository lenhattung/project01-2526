using System.Text;
using ExamGuard.Protocol;

await TestFramedProtocolRoundTripAsync();
TestPolicyMetadataRoundTrip();
TestDiscoveryRoundTrip();

Console.WriteLine("ExamGuard.Protocol smoke tests passed.");

static async Task TestFramedProtocolRoundTripAsync()
{
    using MemoryStream stream = new();
    byte[] payload = Encoding.UTF8.GetBytes("hello");
    SocketEnvelope envelope = FramedSocketProtocol.CreateEnvelope(
        MessageType.ChatMessage,
        "EXAM-001",
        "SV001",
        "LAB-PC-01",
        new Dictionary<string, string> { ["message"] = "hi" });

    await FramedSocketProtocol.SendAsync(stream, envelope, payload);
    stream.Position = 0;
    ReceivedFrame? received = await FramedSocketProtocol.ReceiveAsync(stream);

    Require(received is not null, "Expected a received frame.");
    Require(received!.Envelope.MessageType == MessageType.ChatMessage, "Message type mismatch.");
    Require(received.Envelope.Metadata["message"] == "hi", "Metadata mismatch.");
    Require(Encoding.UTF8.GetString(received.Payload) == "hello", "Payload mismatch.");
}

static void TestPolicyMetadataRoundTrip()
{
    PolicySnapshot policy = new()
    {
        BlockedProcesses = ["zalo", "chatgpt"],
        BlockedWindowKeywords = ["ChatGPT", "Claude"]
    };

    PolicySnapshot roundTrip = PolicySnapshot.FromMetadata(policy.ToMetadata());
    Require(roundTrip.BlockedProcesses.SequenceEqual(policy.BlockedProcesses), "Process policy mismatch.");
    Require(roundTrip.BlockedWindowKeywords.SequenceEqual(policy.BlockedWindowKeywords), "Window policy mismatch.");
}

static void TestDiscoveryRoundTrip()
{
    DiscoveryAnnouncement announcement = new()
    {
        SessionId = "EXAM-001",
        Host = "192.168.1.10",
        Port = 9090,
        TeacherMachine = "TEACHER-PC"
    };

    DiscoveryAnnouncement? parsed = DiscoveryAnnouncement.FromJson(announcement.ToJson());
    Require(parsed is not null, "Discovery parse failed.");
    Require(parsed!.Host == announcement.Host, "Discovery host mismatch.");
    Require(parsed.Port == announcement.Port, "Discovery port mismatch.");
}

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
