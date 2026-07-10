using System.Diagnostics;
using System.Management;

namespace StudentForm;

internal sealed class ProcessMonitor
{
    private static readonly string[] BrowserProcesses = ["chrome", "msedge", "firefox", "brave", "opera"];
    private static readonly string[] IdeProcesses = ["code", "cursor", "code-insiders", "vscode", "devenv"];
    private readonly HashSet<string> _reported = new(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<Violation> ScanAndEnforce(
        IReadOnlyCollection<string> blockedProcesses,
        IReadOnlyCollection<string> blockedKeywords,
        IReadOnlyCollection<string>? blockedAiCliTools = null,
        IReadOnlyCollection<string>? blockedProxyTools = null,
        IReadOnlyCollection<string>? blockedIdeExtensions = null)
    {
        PolicyMatcher matcher = new(
            blockedProcesses,
            blockedKeywords,
            blockedAiCliTools ?? [],
            blockedProxyTools ?? [],
            blockedIdeExtensions ?? []);

        foreach (Process process in Process.GetProcesses())
        {
            ProcessSnapshot snapshot;
            try
            {
                snapshot = new ProcessSnapshot(
                    process.Id,
                    process.ProcessName,
                    process.MainWindowTitle,
                    TryGetCommandLine(process.Id));
            }
            catch
            {
                continue;
            }

            PolicyMatch? match = matcher.MatchProcess(snapshot);
            if (match is null)
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

            string reportKey = $"process:{snapshot.ProcessId}:{match.RuleKind}:{match.Rule}";
            if (_reported.Add(reportKey) || action == "killed")
            {
                yield return new Violation(snapshot.ProcessName, snapshot.WindowTitle, action, match.RuleKind, match.Rule, snapshot.CommandLine);
            }
        }

        foreach (Violation violation in ScanIdeExtensions(matcher))
        {
            yield return violation;
        }
    }

    private IEnumerable<Violation> ScanIdeExtensions(PolicyMatcher matcher)
    {
        IReadOnlyList<ExtensionSnapshot> blockedExtensions = matcher.FindBlockedExtensions(EnumerateExtensionFolders());
        if (blockedExtensions.Count == 0)
        {
            yield break;
        }

        bool ideRunning = Process.GetProcesses().Any(process =>
        {
            try
            {
                return IdeProcesses.Any(ide => process.ProcessName.Contains(ide, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        });

        foreach (ExtensionSnapshot extension in blockedExtensions)
        {
            string reportKey = $"extension:{extension.Path}:{ideRunning}";
            if (!_reported.Add(reportKey))
            {
                continue;
            }

            yield return new Violation("IDE extension", extension.Path, "reported", "ide_extension", extension.Rule, "");
        }
    }

    private static IEnumerable<string> EnumerateExtensionFolders()
    {
        string user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string[] roots =
        [
            Path.Combine(user, ".vscode", "extensions"),
            Path.Combine(user, ".vscode-insiders", "extensions"),
            Path.Combine(user, ".cursor", "extensions")
        ];

        foreach (string root in roots)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (string directory in Directory.EnumerateDirectories(root))
            {
                yield return directory;
            }
        }
    }

    private static string TryGetCommandLine(int processId)
    {
        try
        {
            using ManagementObjectSearcher searcher = new($"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {processId}");
            foreach (ManagementObject process in searcher.Get().Cast<ManagementObject>())
            {
                return process["CommandLine"]?.ToString() ?? "";
            }
        }
        catch
        {
        }

        return "";
    }

    private sealed class PolicyMatcher(
        IReadOnlyCollection<string> processes,
        IReadOnlyCollection<string> windowKeywords,
        IReadOnlyCollection<string> aiCliTools,
        IReadOnlyCollection<string> proxyTools,
        IReadOnlyCollection<string> ideExtensions)
    {
        public PolicyMatch? MatchProcess(ProcessSnapshot process)
        {
            bool isBrowser = BrowserProcesses.Any(browser =>
                process.ProcessName.Contains(browser, StringComparison.OrdinalIgnoreCase));

            if (TryMatch(process.ProcessName, processes, "process", out PolicyMatch? processMatch))
            {
                return processMatch;
            }

            string commandSurface = $"{process.ProcessName} {process.CommandLine}";
            if (TryMatch(commandSurface, aiCliTools, "ai_cli", out PolicyMatch? aiMatch))
            {
                return aiMatch;
            }

            if (TryMatch(commandSurface, proxyTools, "proxy_tool", out PolicyMatch? proxyMatch))
            {
                return proxyMatch;
            }

            if (!isBrowser &&
                !string.IsNullOrWhiteSpace(process.WindowTitle) &&
                TryMatch(process.WindowTitle, windowKeywords, "window_title", out PolicyMatch? titleMatch))
            {
                return titleMatch;
            }

            return null;
        }

        public IReadOnlyList<ExtensionSnapshot> FindBlockedExtensions(IEnumerable<string> folders)
        {
            List<ExtensionSnapshot> matches = [];
            foreach (string folder in folders)
            {
                string name = Path.GetFileName(folder);
                if (TryMatch(name, ideExtensions, "ide_extension", out PolicyMatch? match) && match is not null)
                {
                    matches.Add(new ExtensionSnapshot(folder, match.Rule));
                }
            }

            return matches;
        }

        private static bool TryMatch(string value, IReadOnlyCollection<string> rules, string kind, out PolicyMatch? match)
        {
            foreach (string rule in rules.Where(rule => !string.IsNullOrWhiteSpace(rule)))
            {
                if (value.Contains(rule, StringComparison.OrdinalIgnoreCase))
                {
                    match = new PolicyMatch(kind, rule);
                    return true;
                }
            }

            match = null;
            return false;
        }
    }
}

internal sealed record Violation(string ProcessName, string WindowTitle, string Action, string RuleKind = "process", string Rule = "", string CommandLine = "");

internal sealed record ProcessSnapshot(int ProcessId, string ProcessName, string WindowTitle, string CommandLine);

internal sealed record PolicyMatch(string RuleKind, string Rule);

internal sealed record ExtensionSnapshot(string Path, string Rule);
