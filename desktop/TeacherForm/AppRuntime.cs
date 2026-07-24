namespace TeacherForm;

internal static class AppRuntime
{
    private const string DefaultBackendBaseUrl = "http://127.0.0.1:8081";

    public static string BackendBaseUrl =>
        Environment.GetEnvironmentVariable("EXAMGUARD_TEACHER_BACKEND_URL")
        ?? Environment.GetEnvironmentVariable("EXAMGUARD_BACKEND_URL")
        ?? ReadOptionalFile("teacher-backend-url.txt")
        ?? ReadOptionalFile("backend-url.txt")
        ?? DefaultBackendBaseUrl;

    public static string RelayHost =>
        Environment.GetEnvironmentVariable("EXAMGUARD_RELAY_HOST")
        ?? ReadOptionalFile("teacher-relay-host.txt")
        ?? "";

    public static string RelaySecret =>
        Environment.GetEnvironmentVariable("EXAMGUARD_RELAY_SECRET")
        ?? ReadOptionalFile("teacher-relay-secret.txt")
        ?? "";

    private static string? ReadOptionalFile(string fileName)
    {
        try
        {
            string path = Path.Combine(AppContext.BaseDirectory, fileName);
            if (!File.Exists(path))
            {
                return null;
            }

            string value = File.ReadAllText(path).Trim();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        catch
        {
            return null;
        }
    }
}
