namespace StudentForm;

internal sealed class BroadcastViewerForm : Form
{
    private readonly PictureBox _pictureBox = new()
    {
        Dock = DockStyle.Fill,
        SizeMode = PictureBoxSizeMode.Zoom,
        BackColor = Color.Black
    };

    public BroadcastViewerForm()
    {
        Text = "Màn hình giáo viên";
        Width = 1024;
        Height = 640;
        StartPosition = FormStartPosition.CenterScreen;
        AppIcons.SetFormIcon(this, "monitor");
        Controls.Add(_pictureBox);
    }

    public void ShowFrame(byte[] jpeg)
    {
        using MemoryStream stream = new(jpeg);
        Image next = Image.FromStream(stream);
        Image? old = _pictureBox.Image;
        _pictureBox.Image = (Image)next.Clone();
        old?.Dispose();

        if (!Visible)
        {
            Show();
        }
    }
}
