namespace TeacherForm;

internal static class UiTheme
{
    private static readonly Color AppBackground = Color.FromArgb(243, 247, 250);
    private static readonly Color Surface = Color.White;
    private static readonly Color Accent = Color.FromArgb(19, 104, 97);
    private static readonly Color AccentSoft = Color.FromArgb(224, 243, 240);
    private static readonly Color Border = Color.FromArgb(211, 220, 228);
    private static readonly Color Text = Color.FromArgb(25, 43, 57);
    private static readonly Color Muted = Color.FromArgb(95, 113, 126);
    private static readonly Color SuccessBack = Color.FromArgb(219, 245, 232);
    private static readonly Color SuccessText = Color.FromArgb(17, 104, 61);
    private static readonly Color WarningBack = Color.FromArgb(255, 244, 214);
    private static readonly Color WarningText = Color.FromArgb(137, 87, 0);
    private static readonly Color DangerBack = Color.FromArgb(255, 228, 230);
    private static readonly Color DangerText = Color.FromArgb(178, 32, 43);
    private static readonly Color InfoBack = Color.FromArgb(226, 239, 255);
    private static readonly Color InfoText = Color.FromArgb(29, 78, 137);

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
                    StyleButton(button);
                    break;
                case TextBox textBox:
                    StyleTextBox(textBox);
                    break;
                case NumericUpDown numeric:
                    StyleNumeric(numeric);
                    break;
                case ComboBox comboBox:
                    comboBox.BackColor = Surface;
                    comboBox.ForeColor = Text;
                    break;
                case ListBox listBox:
                    StyleListBox(listBox);
                    break;
                case ListView listView:
                    StyleListView(listView);
                    break;
                case GroupBox group:
                    StyleGroupBox(group);
                    break;
                case FlowLayoutPanel flow:
                    if (Equals(flow.Tag, "status-chip"))
                    {
                        StyleStatusChip(flow);
                    }
                    else
                    {
                        flow.BackColor = Equals(flow.Tag, "surface-flow") ? Surface : AppBackground;
                    }
                    break;
                case TableLayoutPanel table:
                    table.BackColor = AppBackground;
                    break;
                case SplitContainer split:
                    split.BackColor = AppBackground;
                    split.Panel1.BackColor = AppBackground;
                    split.Panel2.BackColor = AppBackground;
                    break;
                case Panel panel:
                    StylePanel(panel);
                    break;
                case Label label:
                    if (Equals(label.Parent?.Tag, "status-chip"))
                    {
                        break;
                    }

                    label.ForeColor = label.Text.Contains("Đã dừng", StringComparison.OrdinalIgnoreCase)
                        ? Muted
                        : Text;
                    break;
            }

            if (control.Controls.Count > 0)
            {
                ApplyRecursive(control.Controls);
            }
        }
    }

    private static void StyleButton(Button button)
    {
        bool isLargeAction = string.Equals(button.Name, "large-action-button", StringComparison.OrdinalIgnoreCase);
        bool isSidebar = string.Equals(button.Name, "sidebar-button", StringComparison.OrdinalIgnoreCase);
        bool isSecondary = string.Equals(button.Name, "secondary-button", StringComparison.OrdinalIgnoreCase);

        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = Border;
        button.FlatAppearance.BorderSize = 1;
        button.BackColor = isLargeAction || isSecondary ? Surface : (IsPrimaryButton(button.Text) ? Accent : Surface);
        button.ForeColor = button.BackColor == Accent ? Color.White : Text;
        button.Height = isLargeAction ? 56 : (isSidebar ? 52 : 36);
        button.Padding = isLargeAction ? new Padding(18, 8, 14, 8) : new Padding(10, 5, 10, 5);
        button.Margin = isLargeAction ? new Padding(6) : (isSidebar ? new Padding(0, 0, 0, 8) : new Padding(4));
        button.AutoSize = !isLargeAction && !isSidebar;
        button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        button.MinimumSize = isLargeAction
            ? new Size(Math.Max(button.Width, 150), 56)
            : (isSidebar ? new Size(Math.Max(button.Width, 132), 52) : new Size(105, 36));
        if (button.Tag is string iconName)
        {
            AppIcons.ApplyToButton(button, iconName);
        }
    }

    private static bool IsPrimaryButton(string text)
    {
        return text.Contains("Bắt đầu", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Gửi", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Lưu", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Phát", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Đăng nhập", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Mở phiên", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Chat", StringComparison.OrdinalIgnoreCase);
    }

    private static void StyleTextBox(TextBox textBox)
    {
        textBox.BackColor = textBox.ReadOnly ? Color.FromArgb(246, 249, 251) : Surface;
        textBox.ForeColor = Text;
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.Margin = new Padding(4, 4, 8, 4);
    }

    private static void StyleNumeric(NumericUpDown numeric)
    {
        numeric.BackColor = Surface;
        numeric.ForeColor = Text;
        numeric.BorderStyle = BorderStyle.FixedSingle;
        numeric.Margin = new Padding(4, 4, 8, 4);
    }

    private static void StyleListBox(ListBox listBox)
    {
        listBox.BackColor = Surface;
        listBox.ForeColor = Text;
        listBox.BorderStyle = BorderStyle.FixedSingle;
    }

    private static void StyleListView(ListView listView)
    {
        listView.BackColor = Surface;
        listView.ForeColor = Text;
        listView.GridLines = true;
        listView.FullRowSelect = true;
    }

    private static void StyleGroupBox(GroupBox group)
    {
        group.BackColor = Surface;
        group.ForeColor = Text;
        group.Padding = new Padding(12);
    }

    private static void StylePanel(Panel panel)
    {
        panel.BackColor = Surface;
        if (panel.Controls.Count == 1 && panel.Controls[0] is Label)
        {
            panel.BackColor = AccentSoft;
        }
    }

    public static void StyleStatusChip(FlowLayoutPanel chip)
    {
        Label? label = chip.Controls.OfType<Label>().FirstOrDefault();
        string text = label?.Text ?? string.Empty;
        (Color backColor, Color textColor) = StatusColors(text);

        chip.BackColor = backColor;
        foreach (Label childLabel in chip.Controls.OfType<Label>())
        {
            childLabel.ForeColor = textColor;
        }
    }

    private static (Color BackColor, Color TextColor) StatusColors(string text)
    {
        if (ContainsAny(text, "lỗi", "mất kết nối", "ngắt kết nối", "Đã dừng", "chưa chạy"))
        {
            return (DangerBack, DangerText);
        }

        if (ContainsAny(text, "chưa kết nối", "đang đăng nhập", "đang chờ", "Sinh viên: 0"))
        {
            return (WarningBack, WarningText);
        }

        if (ContainsAny(text, "đã kết nối", "đang hoạt động", "trực tuyến"))
        {
            return (SuccessBack, SuccessText);
        }

        if (text.StartsWith("Sinh viên:", StringComparison.OrdinalIgnoreCase)
            && !text.Contains(": 0", StringComparison.OrdinalIgnoreCase))
        {
            return (SuccessBack, SuccessText);
        }

        return (InfoBack, InfoText);
    }

    private static bool ContainsAny(string text, params string[] values)
    {
        return values.Any(value => text.Contains(value, StringComparison.OrdinalIgnoreCase));
    }
}
