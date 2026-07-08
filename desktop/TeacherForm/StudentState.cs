namespace TeacherForm;

internal sealed class StudentState
{
    public string StudentId { get; init; } = "";
    public string StudentCode { get; set; } = "";
    public string StudentName { get; set; } = "";
    public string WindowsUserName { get; set; } = "";
    public string MachineName { get; init; } = "";
    public string RemoteEndPoint { get; init; } = "";
    public DateTimeOffset ConnectedAt { get; init; } = DateTimeOffset.Now;
    public DateTimeOffset LastSeen { get; set; } = DateTimeOffset.Now;
    public string SubmissionStatus { get; set; } = "Chưa nộp";
    public string LastViolation { get; set; } = "";
    public string WebcamStatus { get; set; } = "Chưa kiểm tra";
    public bool HandRaised { get; set; }
    public int UnreadChatCount { get; set; }
    public string LastActivityEvent { get; set; } = "";
    public bool IsOnline { get; set; } = true;
    public Image? LatestFrame { get; set; }
    public Image? LatestWebcamFrame { get; set; }
    public DateTimeOffset? LastWebcamSeen { get; set; }

    public string StudentCodeOrId => string.IsNullOrWhiteSpace(StudentCode) ? StudentId : StudentCode;
    public string DisplayName => string.IsNullOrWhiteSpace(StudentName) ? StudentCodeOrId : $"{StudentCodeOrId} - {StudentName}";
}
