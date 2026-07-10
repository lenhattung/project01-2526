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

    Task SendRemoteControlStartAsync(string connectionId);

    Task SendRemoteControlStopAsync(string? connectionId);

    Task SendRemotePointerAsync(string connectionId, string action, double relativeX, double relativeY, string button = "", int wheelDelta = 0);

    Task SendRemoteKeyAsync(string connectionId, string action, string key = "", string text = "", string modifiers = "");

    Task SendClipboardSetAsync(string? studentId, string text);

    Task SendTeacherFrameAsync(byte[] jpeg);

    Task SendTeacherBroadcastStopAsync();

    Task SelectWebcamAsync(string connectionId, string cameraId);

    Task RequestWebcamDevicesAsync(string connectionId);

    Task DistributeFileAsync(string? studentId, string filePath, IProgress<int>? progress = null, CancellationToken cancellationToken = default);
}
