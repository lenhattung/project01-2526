using System.Drawing.Imaging;
using System.Text.Json;

namespace TeacherForm;

internal sealed class SessionHistoryStore
{
    private readonly Form _form;
    private string? _folder;
    private string _sessionCode = "";
    private bool _startCaptured;
    private bool _endCaptured;
    private int _violationCount;

    public SessionHistoryStore(Form form)
    {
        _form = form;
    }

    public string? CurrentFolder => _folder;

    public static string RootFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "ExamGuard",
        "SessionHistory");

    public void Start(string sessionCode, string sessionTitle)
    {
        if (_folder is not null)
        {
            return;
        }

        _sessionCode = Sanitize(sessionCode);
        Directory.CreateDirectory(RootFolder);
        _folder = Path.Combine(RootFolder, $"{_sessionCode}_{DateTime.Now:yyyyMMdd-HHmmss}");
        Directory.CreateDirectory(_folder);
        WriteSessionJson(sessionTitle, null, 0);
        CaptureStart();
    }

    public void CaptureStart()
    {
        if (_folder is null || _startCaptured)
        {
            return;
        }

        CaptureForm(Path.Combine(_folder, "start-teacher.jpg"));
        _startCaptured = true;
    }

    public void Finish(string sessionTitle, int studentCount)
    {
        if (_folder is null)
        {
            return;
        }

        if (!_endCaptured)
        {
            CaptureForm(Path.Combine(_folder, "end-teacher.jpg"));
            _endCaptured = true;
        }

        WriteSessionJson(sessionTitle, DateTimeOffset.UtcNow, studentCount);
    }

    public void AppendViolation(StudentState state, string type, string rule, string details, string action)
    {
        if (_folder is null)
        {
            return;
        }

        _violationCount++;
        AppendJsonLine("violations.jsonl", new
        {
            sessionCode = _sessionCode,
            studentCode = state.StudentCodeOrId,
            studentName = state.StudentName,
            machineName = state.MachineName,
            violationType = type,
            rule,
            details,
            action,
            timestampUtc = DateTimeOffset.UtcNow
        });
    }

    public void AppendAudit(string action, string? target, string details)
    {
        if (_folder is null)
        {
            return;
        }

        AppendJsonLine("audit.jsonl", new
        {
            sessionCode = _sessionCode,
            action,
            target,
            details,
            teacherMachine = Environment.MachineName,
            timestampUtc = DateTimeOffset.UtcNow
        });
    }

    public void Reset()
    {
        _folder = null;
        _sessionCode = "";
        _startCaptured = false;
        _endCaptured = false;
        _violationCount = 0;
    }

    private void WriteSessionJson(string sessionTitle, DateTimeOffset? finishedAtUtc, int studentCount)
    {
        if (_folder is null)
        {
            return;
        }

        object data = new
        {
            sessionCode = _sessionCode,
            title = sessionTitle,
            teacherMachine = Environment.MachineName,
            startedAtUtc = Directory.GetCreationTimeUtc(_folder),
            finishedAtUtc,
            studentCount,
            violationCount = _violationCount
        };
        File.WriteAllText(Path.Combine(_folder, "session.json"), JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
    }

    private void AppendJsonLine(string fileName, object value)
    {
        if (_folder is null)
        {
            return;
        }

        File.AppendAllText(Path.Combine(_folder, fileName), JsonSerializer.Serialize(value) + Environment.NewLine);
    }

    private void CaptureForm(string path)
    {
        using Bitmap raw = new(Math.Max(1, _form.Width), Math.Max(1, _form.Height));
        _form.DrawToBitmap(raw, new Rectangle(Point.Empty, raw.Size));
        using Bitmap output = ResizeIfNeeded(raw, 1600);
        ImageCodecInfo jpeg = ImageCodecInfo.GetImageEncoders().First(x => x.MimeType == "image/jpeg");
        using EncoderParameters parameters = new(1);
        parameters.Param[0] = new EncoderParameter(Encoder.Quality, 65L);
        output.Save(path, jpeg, parameters);
    }

    private static Bitmap ResizeIfNeeded(Bitmap source, int maxWidth)
    {
        if (source.Width <= maxWidth)
        {
            return (Bitmap)source.Clone();
        }

        int width = maxWidth;
        int height = (int)Math.Round(source.Height * (maxWidth / (double)source.Width));
        Bitmap resized = new(width, height);
        using Graphics graphics = Graphics.FromImage(resized);
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        graphics.DrawImage(source, 0, 0, width, height);
        return resized;
    }

    private static string Sanitize(string value)
    {
        string safe = string.Join("_", value.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();
        return string.IsNullOrWhiteSpace(safe) ? "session" : safe;
    }

    public static IReadOnlyList<SessionHistoryEntry> LoadHistoryEntries()
    {
        if (!Directory.Exists(RootFolder))
        {
            return [];
        }

        List<SessionHistoryEntry> entries = [];
        foreach (string folder in Directory.GetDirectories(RootFolder))
        {
            SessionHistoryEntry? entry = TryReadEntry(folder);
            if (entry is not null)
            {
                entries.Add(entry);
            }
        }

        return entries
            .OrderByDescending(x => x.StartedAtUtc ?? Directory.GetCreationTimeUtc(x.FolderPath))
            .ToList();
    }

    private static SessionHistoryEntry? TryReadEntry(string folder)
    {
        string sessionJsonPath = Path.Combine(folder, "session.json");
        if (!File.Exists(sessionJsonPath))
        {
            return null;
        }

        try
        {
            using JsonDocument json = JsonDocument.Parse(File.ReadAllText(sessionJsonPath));
            JsonElement root = json.RootElement;
            return new SessionHistoryEntry
            {
                FolderPath = folder,
                SessionCode = root.TryGetProperty("sessionCode", out JsonElement sessionCode) ? sessionCode.GetString() ?? Path.GetFileName(folder) : Path.GetFileName(folder),
                Title = root.TryGetProperty("title", out JsonElement title) ? title.GetString() ?? "" : "",
                TeacherMachine = root.TryGetProperty("teacherMachine", out JsonElement machine) ? machine.GetString() ?? "" : "",
                StudentCount = root.TryGetProperty("studentCount", out JsonElement studentCount) && studentCount.TryGetInt32(out int students) ? students : 0,
                ViolationCount = root.TryGetProperty("violationCount", out JsonElement violations) && violations.TryGetInt32(out int violationCount) ? violationCount : 0,
                StartedAtUtc = root.TryGetProperty("startedAtUtc", out JsonElement startedAt) && DateTimeOffset.TryParse(startedAt.GetString(), out DateTimeOffset started) ? started : null,
                FinishedAtUtc = root.TryGetProperty("finishedAtUtc", out JsonElement finishedAt) && DateTimeOffset.TryParse(finishedAt.GetString(), out DateTimeOffset finished) ? finished : null,
                StartImagePath = File.Exists(Path.Combine(folder, "start-teacher.jpg")) ? Path.Combine(folder, "start-teacher.jpg") : null,
                EndImagePath = File.Exists(Path.Combine(folder, "end-teacher.jpg")) ? Path.Combine(folder, "end-teacher.jpg") : null,
                SessionJsonPath = sessionJsonPath,
                ViolationsPath = File.Exists(Path.Combine(folder, "violations.jsonl")) ? Path.Combine(folder, "violations.jsonl") : null,
                AuditPath = File.Exists(Path.Combine(folder, "audit.jsonl")) ? Path.Combine(folder, "audit.jsonl") : null
            };
        }
        catch
        {
            return null;
        }
    }
}

internal sealed class SessionHistoryEntry
{
    public string FolderPath { get; init; } = "";
    public string SessionCode { get; init; } = "";
    public string Title { get; init; } = "";
    public string TeacherMachine { get; init; } = "";
    public int StudentCount { get; init; }
    public int ViolationCount { get; init; }
    public DateTimeOffset? StartedAtUtc { get; init; }
    public DateTimeOffset? FinishedAtUtc { get; init; }
    public string? StartImagePath { get; init; }
    public string? EndImagePath { get; init; }
    public string SessionJsonPath { get; init; } = "";
    public string? ViolationsPath { get; init; }
    public string? AuditPath { get; init; }
}
