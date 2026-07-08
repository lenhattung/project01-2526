namespace StudentForm;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, args) => ShowStartupError(args.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                ShowStartupError(ex);
            }
        };

        try
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
        catch (Exception ex)
        {
            ShowStartupError(ex);
        }
    }

    private static void ShowStartupError(Exception ex)
    {
        try
        {
            string path = Path.Combine(Path.GetTempPath(), "ExamGuard.Student.startup.log");
            File.WriteAllText(path, $"{DateTime.Now:O}{Environment.NewLine}{ex}");
            MessageBox.Show(
                $"StudentForm bị lỗi khi khởi động.{Environment.NewLine}{Environment.NewLine}{ex.Message}{Environment.NewLine}{Environment.NewLine}Chi tiết đã lưu tại:{Environment.NewLine}{path}",
                "ExamGuard Student",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch
        {
            MessageBox.Show(
                $"StudentForm bị lỗi khi khởi động.{Environment.NewLine}{Environment.NewLine}{ex}",
                "ExamGuard Student",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
