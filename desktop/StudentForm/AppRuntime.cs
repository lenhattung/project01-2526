namespace StudentForm;

internal static class AppRuntime
{
    public static string BackendBaseUrl =>
        Environment.GetEnvironmentVariable("EXAMGUARD_BACKEND_URL")
        ?? ReadOptionalFile("student-backend-url.txt")
        ?? "http://127.0.0.1:8081";

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
