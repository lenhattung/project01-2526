namespace ExamGuard.Protocol;

public sealed record ReceivedFrame(SocketEnvelope Envelope, byte[] Payload);
