using System.Diagnostics;

namespace StudentForm;

internal sealed class ProcessMonitor
{
    private static readonly string[] BrowserProcesses = ["chrome", "msedge", "firefox", "brave", "opera"];
    private readonly HashSet<int> _reported = [];

    public IEnumerable<Violation> ScanAndEnforce(IReadOnlyCollection<string> blockedProcesses, IReadOnlyCollection<string> blockedKeywords)
    {
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

            bool isBrowser = BrowserProcesses.Any(browser =>
                processName.Contains(browser, StringComparison.OrdinalIgnoreCase));
            bool processBlocked = blockedProcesses.Any(rule =>
                processName.Contains(rule, StringComparison.OrdinalIgnoreCase));
            bool titleBlocked = !isBrowser && !string.IsNullOrWhiteSpace(title) && blockedKeywords.Any(rule =>
                title.Contains(rule, StringComparison.OrdinalIgnoreCase));

            if (!processBlocked && !titleBlocked)
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

            if (_reported.Add(process.Id) || action == "killed")
            {
                yield return new Violation(processName, title, action);
            }
        }
    }
}

internal sealed record Violation(string ProcessName, string WindowTitle, string Action);
