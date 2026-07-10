namespace ExamGuard.Protocol;

public static class MessageType
{
    public const string Hello = "HELLO";
    public const string HelloAck = "HELLO_ACK";
    public const string Heartbeat = "HEARTBEAT";
    public const string ScreenFrame = "SCREEN_FRAME";
    public const string WebcamFrame = "WEBCAM_FRAME";
    public const string WebcamStatus = "WEBCAM_STATUS";
    public const string WebcamDevices = "WEBCAM_DEVICES";
    public const string WebcamSelect = "WEBCAM_SELECT";
    public const string PolicyUpdate = "POLICY_UPDATE";
    public const string ProcessViolation = "PROCESS_VIOLATION";
    public const string SubmissionStart = "SUBMISSION_START";
    public const string SubmissionChunk = "SUBMISSION_CHUNK";
    public const string SubmissionComplete = "SUBMISSION_COMPLETE";
    public const string ChatMessage = "CHAT_MESSAGE";
    public const string ChatOpen = "CHAT_OPEN";
    public const string ChatClose = "CHAT_CLOSE";
    public const string HandRaise = "HAND_RAISE";
    public const string HandRaiseClear = "HAND_RAISE_CLEAR";
    public const string StudentActivityEvent = "STUDENT_ACTIVITY_EVENT";
    public const string Attention = "ATTENTION";
    public const string LockScreen = "LOCK_SCREEN";
    public const string UnlockScreen = "UNLOCK_SCREEN";
    public const string ExecuteCommand = "EXECUTE_COMMAND";
    public const string CommandResult = "COMMAND_RESULT";
    public const string FileDistributionStart = "FILE_DISTRIBUTION_START";
    public const string FileDistributionChunk = "FILE_DISTRIBUTION_CHUNK";
    public const string FileDistributionComplete = "FILE_DISTRIBUTION_COMPLETE";
    public const string TeacherFrame = "TEACHER_FRAME";
    public const string TeacherBroadcastStop = "TEACHER_BROADCAST_STOP";
    public const string RemoteMouseClick = "REMOTE_MOUSE_CLICK";
    public const string RemoteTextInput = "REMOTE_TEXT_INPUT";
    public const string RemoteControlStart = "REMOTE_CONTROL_START";
    public const string RemoteControlStop = "REMOTE_CONTROL_STOP";
    public const string RemotePointer = "REMOTE_POINTER";
    public const string RemoteKey = "REMOTE_KEY";
    public const string ClipboardSet = "CLIPBOARD_SET";
    public const string StudentDisconnecting = "STUDENT_DISCONNECTING";
    public const string Error = "ERROR";
}
