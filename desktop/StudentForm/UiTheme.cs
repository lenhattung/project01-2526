namespace StudentForm;

internal static class UiTheme
{
    private static readonly Color AppBackground = Color.FromArgb(243, 247, 250);
    private static readonly Color Surface = Color.White;
    private static readonly Color Accent = Color.FromArgb(19, 104, 97);
    private static readonly Color AccentSoft = Color.FromArgb(224, 243, 240);
    private static readonly Color Border = Color.FromArgb(211, 220, 228);
    private static readonly Color Text = Color.FromArgb(25, 43, 57);

    public static void Apply(Form form)
    {
        form.BackColor = AppBackground;
        form.ForeColor = Text;
        form.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        ApplyRecursive(form.Controls);
    }

    private static void ApplyRecursive(Control.ControlCollection controls)
    {
        foreach (Control control in controls)
        {
            switch (control)
            {
                case Button button:
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = Border;
                    button.FlatAppearance.BorderSize = 1;
                    button.BackColor = IsPrimaryButton(button.Text) ? Accent : Surface;
                    button.ForeColor = button.BackColor == Accent ? Color.White : Text;
                    button.Height = 36;
                    button.Padding = new Padding(10, 5, 10, 5);
                    button.AutoSize = true;
                    button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                    button.MinimumSize = new Size(105, 36);
                    button.Margin = new Padding(4);
                    if (button.Tag is string iconName)
                    {
                        AppIcons.ApplyToButton(button, iconName);
                    }

                    break;
                case TextBox textBox:
                    textBox.BackColor = textBox.ReadOnly ? Color.FromArgb(246, 249, 251) : Surface;
                    textBox.ForeColor = Text;
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                    break;
                case NumericUpDown numeric:
                    numeric.BackColor = Surface;
                    numeric.ForeColor = Text;
                    numeric.BorderStyle = BorderStyle.FixedSingle;
                    break;
                case ListBox listBox:
                    listBox.BackColor = Surface;
                    listBox.ForeColor = Text;
                    listBox.BorderStyle = BorderStyle.FixedSingle;
                    break;
                case GroupBox group:
                    group.BackColor = Surface;
                    group.ForeColor = Text;
                    group.Padding = new Padding(12);
                    break;
                case TableLayoutPanel table:
                    table.BackColor = AppBackground;
                    break;
                case FlowLayoutPanel flow:
                    flow.BackColor = Equals(flow.Tag, "status-chip")
                        ? AccentSoft
                        : Equals(flow.Tag, "surface-flow") ? Surface : AppBackground;
                    break;
                case Panel panel:
                    panel.BackColor = panel.Controls.Count == 1 && panel.Controls[0] is Label ? AccentSoft : Surface;
                    break;
            }

            if (control.Controls.Count > 0)
            {
                ApplyRecursive(control.Controls);
            }
        }
    }

    private static bool IsPrimaryButton(string text)
    {
        return text.Contains("Kết nối", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Tìm", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Nộp", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Gửi", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Giơ tay", StringComparison.OrdinalIgnoreCase);
    }
}
