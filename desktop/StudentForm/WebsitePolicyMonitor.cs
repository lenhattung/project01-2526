using System.Diagnostics;

namespace StudentForm;

internal sealed class WebsitePolicyMonitor
{
    private static readonly string[] BrowserProcesses = ["chrome", "msedge", "firefox", "brave", "opera"];
    private readonly Dictionary<int, DateTimeOffset> _reported = new();

    public IEnumerable<WebsiteViolation> ScanAndEnforce(
        IReadOnlyCollection<string> blockedKeywords,
        IReadOnlyCollection<string> allowedHosts)
    {
        List<string> keywords = blockedKeywords
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        List<string> hosts = allowedHosts
            .Select(NormalizeHost)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (keywords.Count == 0)
        {
            yield break;
        }

        foreach (Process process in Process.GetProcesses())
        {
            string processName;
            string title;
            try
            {
                processName = process.ProcessName;
                title = process.MainWindowTitle;
            }
            catch
            {
                continue;
            }

            if (!BrowserProcesses.Any(browser => processName.Contains(browser, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            bool allowed = hosts.Any(host =>
                title.Contains(host, StringComparison.OrdinalIgnoreCase)
                || title.Contains(host.Replace("www.", "", StringComparison.OrdinalIgnoreCase), StringComparison.OrdinalIgnoreCase));
            bool blocked = keywords.Any(keyword =>
                title.Contains(keyword, StringComparison.OrdinalIgnoreCase));

            if (allowed || !blocked)
            {
                continue;
            }

            if (_reported.TryGetValue(process.Id, out DateTimeOffset lastReport)
                && DateTimeOffset.Now - lastReport < TimeSpan.FromSeconds(10))
            {
                continue;
            }

            string action = "reported";
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    action = "killed";
                }
            }
            catch
            {
                action = "kill_failed";
            }

            _reported[process.Id] = DateTimeOffset.Now;
            yield return new WebsiteViolation(processName, title, string.Join(';', hosts), string.Join(';', keywords), action);
        }
    }

    private static string NormalizeHost(string value)
    {
        string normalized = value.Trim();
        if (Uri.TryCreate(normalized, UriKind.Absolute, out Uri? uri))
        {
            return uri.Host;
        }

        normalized = normalized.Replace("https://", "", StringComparison.OrdinalIgnoreCase)
            .Replace("http://", "", StringComparison.OrdinalIgnoreCase)
            .Trim('/');
        int slash = normalized.IndexOf('/');
        return slash >= 0 ? normalized[..slash] : normalized;
    }
}

internal sealed record WebsiteViolation(string ProcessName, string WindowTitle, string AllowedHosts, string BlockedKeywords, string Action);
