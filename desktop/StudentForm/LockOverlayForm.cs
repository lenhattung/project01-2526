namespace StudentForm;

internal sealed class LockOverlayForm : Form
{
    private readonly Label _messageLabel = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleCenter,
        ForeColor = Color.White,
        Font = new Font("Segoe UI", 28, FontStyle.Bold)
    };

    public LockOverlayForm()
    {
        Text = "ExamGuard - Khóa màn hình";
        AppIcons.SetFormIcon(this, "lock");
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Maximized;
        TopMost = true;
        BackColor = Color.FromArgb(25, 30, 40);
        ShowInTaskbar = false;
        Controls.Add(_messageLabel);
    }

    public void SetMessage(string message)
    {
        _messageLabel.Text = string.IsNullOrWhiteSpace(message)
            ? "Giáo viên đã khóa máy này."
            : message;
    }
}
