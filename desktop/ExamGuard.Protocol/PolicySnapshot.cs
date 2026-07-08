namespace ExamGuard.Protocol;

public sealed class PolicySnapshot
{
    public List<string> BlockedProcesses { get; set; } = new()
    {
        "zalo",
        "messenger",
        "chatgpt",
        "claude"
    };

    public List<string> BlockedWindowKeywords { get; set; } = new()
    {
        "ChatGPT",
        "Claude",
        "Gemini",
        "Messenger",
        "Zalo"
    };

    public int ScreenIntervalMs { get; set; } = 2000;
    public long ScreenJpegQuality { get; set; } = 40L;
    public bool WebcamEnabled { get; set; } = true;
    public bool WebcamSnapshotOnConnect { get; set; } = true;
    public int WebcamIntervalMs { get; set; } = 500;
    public long WebcamJpegQuality { get; set; } = 55L;
    public int ExamDurationMinutes { get; set; }
    public bool AllowSubmissionAfterDeadline { get; set; }
    public string StartedAtUtc { get; set; } = "";
    public string ExamEndAtUtc { get; set; } = "";
    public string ConnectionMode { get; set; } = "lan";
    public bool BlockClipboardShortcuts { get; set; } = true;
    public string WebsitePolicyMode { get; set; } = "allowlist";
    public List<string> AllowedWebsiteHosts { get; set; } = [];

    public Dictionary<string, string> ToMetadata()
    {
        return new Dictionary<string, string>
        {
            ["blockedProcesses"] = string.Join(';', BlockedProcesses),
            ["blockedWindowKeywords"] = string.Join(';', BlockedWindowKeywords),
            ["screenIntervalMs"] = ScreenIntervalMs.ToString(),
            ["screenJpegQuality"] = ScreenJpegQuality.ToString(),
            ["webcamEnabled"] = WebcamEnabled ? "1" : "0",
            ["webcamSnapshotOnConnect"] = WebcamSnapshotOnConnect ? "1" : "0",
            ["webcamIntervalMs"] = WebcamIntervalMs.ToString(),
            ["webcamIntervalSeconds"] = WebcamIntervalMs <= 0 ? "0" : Math.Max(1, WebcamIntervalMs / 1000).ToString(),
            ["webcamJpegQuality"] = WebcamJpegQuality.ToString(),
            ["examDurationMinutes"] = ExamDurationMinutes.ToString(),
            ["allowSubmissionAfterDeadline"] = AllowSubmissionAfterDeadline ? "1" : "0",
            ["startedAtUtc"] = StartedAtUtc,
            ["examEndAtUtc"] = ExamEndAtUtc,
            ["connectionMode"] = ConnectionMode,
            ["blockClipboardShortcuts"] = BlockClipboardShortcuts ? "1" : "0",
            ["websitePolicyMode"] = WebsitePolicyMode,
            ["allowedWebsiteHosts"] = string.Join(';', AllowedWebsiteHosts),
            ["imageTransportMode"] = "binary-jpeg"
        };
    }

    public static PolicySnapshot FromMetadata(IReadOnlyDictionary<string, string> metadata)
    {
        return new PolicySnapshot
        {
            BlockedProcesses = Split(metadata, "blockedProcesses"),
            BlockedWindowKeywords = Split(metadata, "blockedWindowKeywords"),
            ScreenIntervalMs = ReadInt(metadata, "screenIntervalMs", 2000, 250, 10000),
            ScreenJpegQuality = ReadInt(metadata, "screenJpegQuality", 40, 20, 85),
            WebcamEnabled = ReadBool(metadata, "webcamEnabled", true),
            WebcamSnapshotOnConnect = ReadBool(metadata, "webcamSnapshotOnConnect", true),
            WebcamIntervalMs = ReadWebcamIntervalMs(metadata),
            WebcamJpegQuality = ReadInt(metadata, "webcamJpegQuality", 55, 25, 90),
            ExamDurationMinutes = ReadInt(metadata, "examDurationMinutes", 0, 0, 1440),
            AllowSubmissionAfterDeadline = ReadBool(metadata, "allowSubmissionAfterDeadline", false),
            StartedAtUtc = ReadString(metadata, "startedAtUtc"),
            ExamEndAtUtc = ReadString(metadata, "examEndAtUtc"),
            ConnectionMode = ReadString(metadata, "connectionMode", "lan"),
            BlockClipboardShortcuts = ReadBool(metadata, "blockClipboardShortcuts", true),
            WebsitePolicyMode = ReadString(metadata, "websitePolicyMode", "allowlist"),
            AllowedWebsiteHosts = Split(metadata, "allowedWebsiteHosts")
        };
    }

    private static List<string> Split(IReadOnlyDictionary<string, string> metadata, string key)
    {
        if (!metadata.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    private static int ReadInt(IReadOnlyDictionary<string, string> metadata, string key, int fallback, int min, int max)
    {
        if (!metadata.TryGetValue(key, out string? value) || !int.TryParse(value, out int parsed))
        {
            return fallback;
        }

        return Math.Clamp(parsed, min, max);
    }

    private static int ReadWebcamIntervalMs(IReadOnlyDictionary<string, string> metadata)
    {
        if (metadata.TryGetValue("webcamIntervalMs", out string? intervalMsValue)
            && int.TryParse(intervalMsValue, out int intervalMs))
        {
            return Math.Clamp(intervalMs, 0, 10000);
        }

        int seconds = ReadInt(metadata, "webcamIntervalSeconds", 0, 0, 3600);
        if (seconds <= 0)
        {
            return 0;
        }

        return Math.Clamp(seconds * 1000, 250, 10000);
    }

    private static bool ReadBool(IReadOnlyDictionary<string, string> metadata, string key, bool fallback)
    {
        if (!metadata.TryGetValue(key, out string? value))
        {
            return fallback;
        }

        return value switch
        {
            "1" => true,
            "0" => false,
            _ => bool.TryParse(value, out bool parsed) ? parsed : fallback
        };
    }

    private static string ReadString(IReadOnlyDictionary<string, string> metadata, string key, string fallback = "")
    {
        return metadata.TryGetValue(key, out string? value) ? value ?? fallback : fallback;
    }
}
