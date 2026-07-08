namespace StudentForm;

internal static class AppRuntime
{
    private const string DefaultBackendBaseUrl = "http://103.180.138.225:8081";

    public static string BackendBaseUrl =>
        Environment.GetEnvironmentVariable("EXAMGUARD_BACKEND_URL")
        ?? ReadOptionalFile("student-backend-url.txt")
        ?? DefaultBackendBaseUrl;

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
