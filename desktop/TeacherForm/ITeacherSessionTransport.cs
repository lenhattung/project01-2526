using ExamGuard.Protocol;

namespace TeacherForm;

internal interface ITeacherSessionTransport : IAsyncDisposable
{
    IReadOnlyCollection<StudentState> Students { get; }

    Task StopAsync();

    Task BroadcastPolicyAsync();

    Task SendChatAsync(string? studentId, string message);

    Task SendAttentionAsync(string? studentId, string message);

    Task SendHandRaiseClearAsync(string? studentId);

    Task SendLockAsync(string? studentId, string message);

    Task SendUnlockAsync(string? studentId);

    Task SendExecuteCommandAsync(string? studentId, string command);

    Task SendRemoteMouseClickAsync(string studentId, double relativeX, double relativeY, string button);

    Task SendRemoteTextInputAsync(string? studentId, string text);

    Task SendClipboardSetAsync(string? studentId, string text);

    Task SendTeacherFrameAsync(byte[] jpeg);

    Task DistributeFileAsync(string? studentId, string filePath, IProgress<int>? progress = null, CancellationToken cancellationToken = default);
}
