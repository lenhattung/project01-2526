namespace StudentForm;

internal sealed class JoinSessionLookupDto
{
    public int SessionId { get; set; }
    public string SessionCode { get; set; } = "";
    public string Title { get; set; } = "";
    public string ConnectionMode { get; set; } = "lan";
    public bool RemoteJoinEnabled { get; set; }
    public string? Host { get; set; }
    public int? Port { get; set; }
    public bool RelayEnabled { get; set; }
    public string? RelayHost { get; set; }
    public int? RelayPort { get; set; }
    public string? RelaySecret { get; set; }
    public string? TeacherMachine { get; set; }
    public int ScreenIntervalMs { get; set; }
    public int ScreenJpegQuality { get; set; }
    public bool WebcamEnabled { get; set; }
    public bool WebcamSnapshotOnConnect { get; set; }
    public int WebcamIntervalMs { get; set; }
    public int WebcamJpegQuality { get; set; }
    public int ExamDurationMinutes { get; set; }
    public bool AllowSubmissionAfterDeadline { get; set; }
    public string? StartedAtUtc { get; set; }
    public string? ExamEndAtUtc { get; set; }
    public bool BlockClipboardShortcuts { get; set; }
    public string WebsitePolicyMode { get; set; } = "allowlist";
    public List<string> AllowedWebsiteHosts { get; set; } = [];
    public List<string> BlockedProcesses { get; set; } = [];
    public List<string> BlockedWindowKeywords { get; set; } = [];
}
