namespace TeacherForm;

internal sealed class WorkspaceChildForm : Form
{
    public string WorkspaceKey { get; }

    public WorkspaceChildForm(string workspaceKey, string title, Control content)
    {
        WorkspaceKey = workspaceKey;
        Text = title;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        MinimizeBox = false;
        MaximizeBox = false;
        Dock = DockStyle.Fill;

        content.Dock = DockStyle.Fill;
        Controls.Add(content);
    }
}
