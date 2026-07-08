using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ExamGuard.Protocol;
namespace TeacherForm;

internal sealed class BackendClient
{
    private readonly HttpClient _httpClient = new();
    private string _baseUrl = "";
    private string _token = "";

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(_token);

    public async Task LoginAsync(string baseUrl, string username, string password, CancellationToken cancellationToken = default)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            $"{_baseUrl}/api/auth/login",
            new { username, password },
            cancellationToken);
        response.EnsureSuccessStatusCode();

        using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using JsonDocument json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        _token = json.RootElement.GetProperty("token").GetString() ?? "";
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
    }

    public async Task PostViolationAsync(int examSessionId, StudentState state, string processName, string windowTitle)
    {
        if (!IsAuthenticated || examSessionId <= 0)
        {
            return;
        }

        await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/exam-sessions/{examSessionId}/events", new
        {
            student_code = state.StudentCodeOrId,
            student_name = state.StudentName,
            event_type = "process_violation",
            machine_name = state.MachineName,
            ip_address = ParseIpAddress(state.RemoteEndPoint),
            payload_json = new
            {
                studentCode = state.StudentCodeOrId,
                studentName = state.StudentName,
                processName,
                windowTitle
            }
        });
    }

    public async Task PostSubmissionAsync(int examSessionId, StudentState state, string filePath, string sha256)
    {
        if (!IsAuthenticated || examSessionId <= 0)
        {
            return;
        }

        FileInfo file = new(filePath);
        await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/exam-sessions/{examSessionId}/submissions", new
        {
            student_code = state.StudentCodeOrId,
            student_name = state.StudentName,
            file_name = file.Name,
            storage_path = file.FullName,
            sha256,
            file_size = file.Length,
            status = "submitted",
            machine_name = state.MachineName,
            ip_address = ParseIpAddress(state.RemoteEndPoint)
        });
    }

    public async Task<SessionPolicyDto?> GetSessionPolicyAsync(int examSessionId, CancellationToken cancellationToken = default)
    {
        if (!IsAuthenticated || examSessionId <= 0)
        {
            return null;
        }

        return await _httpClient.GetFromJsonAsync<SessionPolicyDto>(
            $"{_baseUrl}/api/exam-sessions/{examSessionId}/policy",
            cancellationToken);
    }

    public async Task<SessionPolicyDto?> UpdateSessionPolicyAsync(int examSessionId, PolicySnapshot policy, CancellationToken cancellationToken = default)
    {
        if (!IsAuthenticated || examSessionId <= 0)
        {
            return null;
        }

        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            $"{_baseUrl}/api/exam-sessions/{examSessionId}/policy",
            new
            {
                blockedProcesses = policy.BlockedProcesses,
                blockedWindowKeywords = policy.BlockedWindowKeywords,
                screenIntervalMs = policy.ScreenIntervalMs,
                screenJpegQuality = policy.ScreenJpegQuality,
                webcamEnabled = policy.WebcamEnabled,
                webcamSnapshotOnConnect = policy.WebcamSnapshotOnConnect,
                webcamIntervalMs = policy.WebcamIntervalMs,
                webcamJpegQuality = policy.WebcamJpegQuality,
                examDurationMinutes = policy.ExamDurationMinutes,
                allowSubmissionAfterDeadline = policy.AllowSubmissionAfterDeadline,
                blockClipboardShortcuts = policy.BlockClipboardShortcuts,
                websitePolicyMode = policy.WebsitePolicyMode,
                allowedWebsiteHosts = policy.AllowedWebsiteHosts,
            },
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SessionPolicyDto>(cancellationToken: cancellationToken);
    }

    public async Task PostEventAsync(int examSessionId, StudentState state, string eventType, object payload)
    {
        if (!IsAuthenticated || examSessionId <= 0)
        {
            return;
        }

        await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/exam-sessions/{examSessionId}/events", new
        {
            student_code = state.StudentCodeOrId,
            student_name = state.StudentName,
            event_type = eventType,
            machine_name = state.MachineName,
            ip_address = ParseIpAddress(state.RemoteEndPoint),
            payload_json = payload
        });
    }

    public async Task PostChatMessageAsync(int examSessionId, string senderRole, string senderCode, string? targetCode, string message, string scope)
    {
        if (!IsAuthenticated || examSessionId <= 0)
        {
            return;
        }

        await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/exam-sessions/{examSessionId}/chat-messages", new
        {
            sender_role = senderRole,
            sender_code = senderCode,
            target_code = targetCode,
            message,
            scope
        });
    }

    public async Task<ExamSessionSummaryDto?> PublishSessionAccessAsync(
        int examSessionId,
        string connectionMode,
        string? publishedHost,
        int? publishedPort,
        bool remoteJoinEnabled,
        string teacherMachine,
        bool relayEnabled = false,
        string? relayHost = null,
        int? relayPort = null,
        string? relaySecret = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsAuthenticated || examSessionId <= 0)
        {
            return null;
        }

        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            $"{_baseUrl}/api/exam-sessions/{examSessionId}/publish-access",
            new
            {
                connectionMode,
                publishedHost,
                publishedPort,
                remoteJoinEnabled,
                teacherMachine,
                relayEnabled,
                relayHost,
                relayPort,
                relaySecret,
            },
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ExamSessionSummaryDto>(cancellationToken: cancellationToken);
    }

    public async Task<SessionReportDto?> GetSessionReportAsync(int examSessionId, CancellationToken cancellationToken = default)
    {
        if (!IsAuthenticated || examSessionId <= 0)
        {
            return null;
        }

        return await _httpClient.GetFromJsonAsync<SessionReportDto>(
            $"{_baseUrl}/api/exam-sessions/{examSessionId}/report",
            cancellationToken);
    }

    public async Task<List<ExamSessionSummaryDto>> GetExamSessionsAsync(CancellationToken cancellationToken = default)
    {
        if (!IsAuthenticated)
        {
            return [];
        }

        return await _httpClient.GetFromJsonAsync<List<ExamSessionSummaryDto>>(
            $"{_baseUrl}/api/exam-sessions",
            cancellationToken) ?? [];
    }

    public async Task<ExamSessionSummaryDto?> StartExamSessionAsync(int examSessionId, CancellationToken cancellationToken = default)
    {
        if (!IsAuthenticated || examSessionId <= 0)
        {
            return null;
        }

        using HttpResponseMessage response = await _httpClient.PostAsync(
            $"{_baseUrl}/api/exam-sessions/{examSessionId}/start",
            null,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ExamSessionSummaryDto>(cancellationToken: cancellationToken);
    }

    public async Task<ExamSessionSummaryDto?> FinishExamSessionAsync(int examSessionId, CancellationToken cancellationToken = default)
    {
        if (!IsAuthenticated || examSessionId <= 0)
        {
            return null;
        }

        using HttpResponseMessage response = await _httpClient.PostAsync(
            $"{_baseUrl}/api/exam-sessions/{examSessionId}/finish",
            null,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ExamSessionSummaryDto>(cancellationToken: cancellationToken);
    }

    private static string? ParseIpAddress(string remoteEndPoint)
    {
        if (string.IsNullOrWhiteSpace(remoteEndPoint))
        {
            return null;
        }

        int separatorIndex = remoteEndPoint.LastIndexOf(':');
        if (separatorIndex <= 0)
        {
            return remoteEndPoint;
        }

        return remoteEndPoint[..separatorIndex];
    }
}
