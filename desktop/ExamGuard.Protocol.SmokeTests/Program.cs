using System.Text;
using ExamGuard.Protocol;

await TestFramedProtocolRoundTripAsync();
TestPolicyMetadataRoundTrip();
TestNewMessageContracts();
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
        new Dictionary<string, string> { ["message"] = "hi" },
        "conn-001");

    await FramedSocketProtocol.SendAsync(stream, envelope, payload);
    stream.Position = 0;
    ReceivedFrame? received = await FramedSocketProtocol.ReceiveAsync(stream);

    Require(received is not null, "Expected a received frame.");
    Require(received!.Envelope.MessageType == MessageType.ChatMessage, "Message type mismatch.");
    Require(received.Envelope.ConnectionId == "conn-001", "Connection id mismatch.");
    Require(received.Envelope.Metadata["message"] == "hi", "Metadata mismatch.");
    Require(Encoding.UTF8.GetString(received.Payload) == "hello", "Payload mismatch.");
}

static void TestPolicyMetadataRoundTrip()
{
    PolicySnapshot policy = new()
    {
        BlockedProcesses = ["zalo", "chatgpt"],
        BlockedWindowKeywords = ["ChatGPT", "Claude"],
        BlockedAiCliTools = ["codex", "claude"],
        BlockedProxyTools = ["clash"],
        BlockedIdeExtensions = ["copilot"],
        BlockedWebsiteHosts = ["deepseek.com"]
    };

    PolicySnapshot roundTrip = PolicySnapshot.FromMetadata(policy.ToMetadata());
    Require(roundTrip.BlockedProcesses.SequenceEqual(policy.BlockedProcesses), "Process policy mismatch.");
    Require(roundTrip.BlockedWindowKeywords.SequenceEqual(policy.BlockedWindowKeywords), "Window policy mismatch.");
    Require(roundTrip.BlockedAiCliTools.SequenceEqual(policy.BlockedAiCliTools), "AI CLI policy mismatch.");
    Require(roundTrip.BlockedProxyTools.SequenceEqual(policy.BlockedProxyTools), "Proxy policy mismatch.");
    Require(roundTrip.BlockedIdeExtensions.SequenceEqual(policy.BlockedIdeExtensions), "IDE extension policy mismatch.");
    Require(roundTrip.BlockedWebsiteHosts.SequenceEqual(policy.BlockedWebsiteHosts), "Website policy mismatch.");
}

static void TestNewMessageContracts()
{
    Require(MessageType.WebcamDevices == "WEBCAM_DEVICES", "Webcam devices message mismatch.");
    Require(MessageType.WebcamSelect == "WEBCAM_SELECT", "Webcam select message mismatch.");
    Require(MessageType.TeacherBroadcastStop == "TEACHER_BROADCAST_STOP", "Broadcast stop message mismatch.");
    Require(MessageType.RemoteControlStart == "REMOTE_CONTROL_START", "Remote start message mismatch.");
    Require(MessageType.RemotePointer == "REMOTE_POINTER", "Remote pointer message mismatch.");
    Require(MessageType.RemoteKey == "REMOTE_KEY", "Remote key message mismatch.");
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
