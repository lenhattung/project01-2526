namespace TeacherForm;

internal sealed class SessionPolicyDto
{
    public int SessionId { get; set; }
    public string SessionCode { get; set; } = "";
    public string SessionToken { get; set; } = "";
    public List<string> BlockedProcesses { get; set; } = [];
    public List<string> BlockedWindowKeywords { get; set; } = [];
    public int ScreenIntervalMs { get; set; }
    public int ScreenJpegQuality { get; set; }
    public bool WebcamEnabled { get; set; }
    public bool WebcamSnapshotOnConnect { get; set; }
    public int WebcamIntervalMs { get; set; }
    public int WebcamJpegQuality { get; set; }
    public int ExamDurationMinutes { get; set; }
    public bool AllowSubmissionAfterDeadline { get; set; }
    public string? StartedAtUtc { get; set; }
    public string? FinishedAtUtc { get; set; }
    public string? ExamEndAtUtc { get; set; }
    public string ConnectionMode { get; set; } = "lan";
    public bool RemoteJoinEnabled { get; set; }
    public string? PublishedHost { get; set; }
    public int? PublishedPort { get; set; }
    public bool RelayEnabled { get; set; }
    public string? RelayHost { get; set; }
    public int? RelayPort { get; set; }
    public string? RelaySecret { get; set; }
    public string? TeacherMachine { get; set; }
    public bool BlockClipboardShortcuts { get; set; }
    public string WebsitePolicyMode { get; set; } = "allowlist";
    public List<string> AllowedWebsiteHosts { get; set; } = [];
}

internal sealed class ExamSessionSummaryDto
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
    public string Status { get; set; } = "";
    public string SessionToken { get; set; } = "";
    public int ScreenIntervalMs { get; set; }
    public int ScreenJpegQuality { get; set; }
    public bool WebcamEnabled { get; set; }
    public bool WebcamSnapshotOnConnect { get; set; }
    public int WebcamIntervalMs { get; set; }
    public int WebcamJpegQuality { get; set; }
    public int ExamDurationMinutes { get; set; }
    public bool AllowSubmissionAfterDeadline { get; set; }
    public string? StartedAtUtc { get; set; }
    public string? FinishedAtUtc { get; set; }
    public string? ExamEndAtUtc { get; set; }
    public string ConnectionMode { get; set; } = "lan";
    public bool RemoteJoinEnabled { get; set; }
    public string? PublishedHost { get; set; }
    public int? PublishedPort { get; set; }
    public bool RelayEnabled { get; set; }
    public string? RelayHost { get; set; }
    public int? RelayPort { get; set; }
    public string? RelaySecret { get; set; }
    public string? TeacherMachine { get; set; }
    public bool BlockClipboardShortcuts { get; set; }
    public string WebsitePolicyMode { get; set; } = "allowlist";
    public List<string> AllowedWebsiteHosts { get; set; } = [];

    public override string ToString()
    {
        return $"{Code} | {Title} | {Status}";
    }
}

internal sealed class SessionReportDto
{
    public int SessionId { get; set; }
    public string SessionCode { get; set; } = "";
    public string Status { get; set; } = "";
    public int TotalEvents { get; set; }
    public int TotalSubmissions { get; set; }
    public List<SessionEventSummaryDto> RecentEvents { get; set; } = [];
    public List<SessionSubmissionSummaryDto> RecentSubmissions { get; set; } = [];
}

internal sealed class SessionEventSummaryDto
{
    public string EventType { get; set; } = "";
    public string? MachineName { get; set; }
    public string? PayloadJson { get; set; }
    public string? CreatedAt { get; set; }
}

internal sealed class SessionSubmissionSummaryDto
{
    public string FileName { get; set; } = "";
    public string? Status { get; set; }
    public long FileSize { get; set; }
    public string? CreatedAt { get; set; }
}
