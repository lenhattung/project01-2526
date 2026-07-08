using System.IO.Compression;
using System.Media;
using ExamGuard.Protocol;

namespace TeacherForm;

public sealed class MainForm : Form
{
    private readonly TextBox _sessionIdText = new() { Text = "EXAM-001", Width = 150 };
    private readonly TextBox _sessionTokenText = new() { Text = "classroom-token", Width = 150 };
    private readonly NumericUpDown _portInput = new() { Minimum = 1024, Maximum = 65535, Value = 9090, Width = 90 };
    private readonly ComboBox _connectionModePicker = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
    private readonly TextBox _publishedHostText = new() { Width = 180 };
    private readonly TextBox _relayHostText = new() { Text = "103.180.138.225", Width = 180 };
    private readonly NumericUpDown _relayPortInput = new() { Minimum = 1024, Maximum = 65535, Value = 9090, Width = 90 };
    private readonly TextBox _relaySecretText = new() { Text = "Da39qVAylFO0Gl4kVxMHm1CWz7JB9z_VUUl2wBMjCudY1or4i-Oyofmb_c1gyGgV", Width = 220, UseSystemPasswordChar = true };
    private readonly CheckBox _remoteJoinEnabledCheck = new() { Text = "Cho phép kết nối từ xa", AutoSize = true };

    private readonly NumericUpDown _screenIntervalInput = new() { Minimum = 250, Maximum = 10000, Value = 2000, Width = 90 };
    private readonly NumericUpDown _screenQualityInput = new() { Minimum = 20, Maximum = 85, Value = 40, Width = 80 };
    private readonly CheckBox _webcamEnabledCheck = new() { Text = "Theo dõi webcam", Checked = true, AutoSize = true };
    private readonly CheckBox _webcamSnapshotCheck = new() { Text = "Chụp ảnh khi vào", Checked = true, AutoSize = true };
    private readonly NumericUpDown _webcamIntervalInput = new() { Minimum = 0, Maximum = 10000, Value = 500, Width = 90 };
    private readonly NumericUpDown _webcamQualityInput = new() { Minimum = 25, Maximum = 90, Value = 55, Width = 80 };
    private readonly NumericUpDown _examDurationMinutesInput = new() { Minimum = 0, Maximum = 1440, Value = 0, Width = 90 };
    private readonly CheckBox _allowLateSubmissionCheck = new() { Text = "Cho phép nộp sau khi hết giờ", AutoSize = true };
    private readonly CheckBox _blockClipboardCheck = new() { Text = "Chặn copy/paste", Checked = true, AutoSize = true };
    private readonly TextBox _blockedProcessesText = new() { Text = "zalo;messenger;chatgpt;claude", Width = 320 };
    private readonly TextBox _blockedKeywordsText = new() { Text = "ChatGPT;Claude;Gemini;Messenger;Zalo", Width = 320 };
    private readonly TextBox _allowedWebsitesText = new() { Text = "dntu.edu.vn", Width = 320 };

    private readonly TextBox _backendUrlText = new() { Text = "http://103.180.138.225:8081", Width = 210 };
    private readonly TextBox _backendUserText = new() { Text = "teacher", Width = 110 };
    private readonly TextBox _backendPasswordText = new() { Text = "teacher123", Width = 130, UseSystemPasswordChar = true };
    private readonly NumericUpDown _backendSessionIdInput = new() { Minimum = 0, Maximum = 1000000, Value = 1, Width = 90 };
    private readonly ComboBox _backendSessionPicker = new() { Width = 280, DropDownStyle = ComboBoxStyle.DropDownList };

    private readonly TextBox _teacherMessageText = new()
    {
        Text = "Vui lòng tập trung làm bài.",
        Width = 420,
        Height = 78,
        Multiline = true,
        ScrollBars = ScrollBars.Vertical
    };
    private readonly TextBox _teacherCommandText = new() { Text = "notepad.exe", Width = 220 };
    private readonly ProgressBar _distributionProgress = new() { Minimum = 0, Maximum = 100, Width = 220 };

    private readonly ListView _studentsList = new() { View = View.Details, FullRowSelect = true, HideSelection = false, Dock = DockStyle.Fill };
    private readonly FlowLayoutPanel _mosaicPanel = new()
    {
        AutoScroll = true,
        Dock = DockStyle.Fill,
        WrapContents = true,
        FlowDirection = FlowDirection.LeftToRight,
        Padding = new Padding(8)
    };
    private readonly PictureBox _detailFrame = new() { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Black };
    private readonly PictureBox _webcamFrame = new() { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Black };
    private readonly Label _detailStatusLabel = new() { Dock = DockStyle.Bottom, Height = 28, TextAlign = ContentAlignment.MiddleCenter, Text = "Màn hình: chưa chọn sinh viên" };
    private readonly Label _webcamDetailStatus = new() { Dock = DockStyle.Bottom, Height = 28, TextAlign = ContentAlignment.MiddleCenter, Text = "Webcam: chưa chọn sinh viên" };
    private readonly ListBox _eventLog = new() { Dock = DockStyle.Fill };
    private readonly TextBox _chatHistoryText = new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
    private readonly TextBox _selectedStudentInfoText = new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
    private readonly Label _mosaicEmptyLabel = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Text = "Chưa có sinh viên kết nối", Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point) };
    private readonly Label _statusLabel = new() { AutoSize = true, Text = "Đã dừng" };
    private readonly Label _backendStatusLabel = new() { AutoSize = true, Text = "Máy chủ: chưa kết nối" };
    private readonly Label _targetScopeLabel = new() { AutoSize = true, Text = "Phạm vi: toàn bộ sinh viên" };
    private readonly Label _sessionQuickLabel = new() { AutoSize = true, Text = "Phiên: EXAM-001" };
    private readonly Label _studentCountLabel = new() { AutoSize = true, Text = "Sinh viên: 0" };

    private readonly Dictionary<string, StudentCard> _cards = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, FloatingImageViewerForm> _liveViewers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, WorkspaceChildForm> _workspacePages = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Panel> _accordionBodies = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Button> _sidebarButtons = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Control> _accordionSections = new(StringComparer.OrdinalIgnoreCase);
    private readonly BackendClient _backendClient = new();
    private readonly System.Windows.Forms.Timer _teacherBroadcastTimer = new() { Interval = 1500 };
    private readonly System.Windows.Forms.Timer _sessionAccessRefreshTimer = new() { Interval = 15000 };
    private readonly TeacherDiscoveryBroadcaster _discoveryBroadcaster;
    private DateTimeOffset? _sessionStartedAtUtc;
    private ITeacherSessionTransport? _server;

    public MainForm()
    {
        Text = "ExamGuard - Máy giáo viên";
        Width = 1480;
        Height = 920;
        MinimumSize = new Size(1280, 720);
        StartPosition = FormStartPosition.CenterScreen;
        IsMdiContainer = true;
        AppIcons.SetFormIcon(this, "shield");

        _discoveryBroadcaster = new TeacherDiscoveryBroadcaster(message => BeginInvoke(() => Log(message)));
        _connectionModePicker.Items.AddRange(["Cùng mạng LAN", "Khác mạng / từ xa"]);
        _connectionModePicker.Items.Add("Qua máy chủ relay");
        _connectionModePicker.SelectedIndex = 0;
        _sessionIdText.TextChanged += (_, _) => _sessionQuickLabel.Text = $"Phiên: {_sessionIdText.Text.Trim()}";
        _connectionModePicker.SelectedIndexChanged += (_, _) => HandleConnectionModeChanged();
        _sessionAccessRefreshTimer.Tick += async (_, _) => await RefreshPublishedAccessAsync();

        ConfigureStudentsList();
        ConfigureContextMenus();
        _teacherBroadcastTimer.Tick += async (_, _) => await BroadcastTeacherFrameAsync();
        HandleConnectionModeChanged();

        Controls.Add(BuildBottomDockPanel());
        Controls.Add(BuildRightDockPanel());
        Controls.Add(BuildLeftDockPanel());
        Controls.Add(BuildTopDockPanel());
        ConfigureMdiClientArea();
        UpdateMosaicEmptyState();
        UiTheme.Apply(this);
        Shown += (_, _) => ShowWorkspacePage("dashboard");
        FormClosing += async (_, _) =>
        {
            _teacherBroadcastTimer.Stop();
            _sessionAccessRefreshTimer.Stop();
            await _discoveryBroadcaster.DisposeAsync();
            if (_server is not null)
            {
                await _server.DisposeAsync();
            }
        };
    }

    private void ConfigureStudentsList()
    {
        _studentsList.Columns.Add("Sinh viên", 190);
        _studentsList.Columns.Add("Trạng thái", 120);
        _studentsList.Columns.Add("Tên user máy", 130);
        _studentsList.Columns.Add("Máy", 160);
        _studentsList.Columns.Add("Lần cuối", 120);
        _studentsList.Columns.Add("Nộp bài", 110);
        _studentsList.Columns.Add("Webcam", 170);
        _studentsList.Columns.Add("Sự kiện", 230);
        _studentsList.SmallImageList = AppIcons.CreateImageList(
            16,
            ("online", "user", AppIcons.AccentColor),
            ("offline", "user", Color.FromArgb(178, 34, 34)),
            ("alert", "alert-triangle", Color.FromArgb(198, 117, 0)));
        _studentsList.SelectedIndexChanged += (_, _) => ShowSelectedStudent();
    }

    private void ConfigureContextMenus()
    {
        ContextMenuStrip screenMenu = BuildImageContextMenu("screen");
        ContextMenuStrip webcamMenu = BuildImageContextMenu("webcam");
        _detailFrame.ContextMenuStrip = screenMenu;
        _webcamFrame.ContextMenuStrip = webcamMenu;
        _detailFrame.MouseClick += async (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                await SendRemoteMouseClickAsync(e);
            }
        };
    }

    private ContextMenuStrip BuildImageContextMenu(string streamKind)
    {
        ContextMenuStrip menu = new();
        menu.Items.Add("Phóng to", null, (_, _) => ShowSelectedStudentViewer(streamKind));
        return menu;
    }

    private Control BuildTopDockPanel()
    {
        Control header = BuildDashboardHeader();
        header.Dock = DockStyle.Fill;
        Panel host = new() { Dock = DockStyle.Top, Height = 74, Padding = new Padding(12, 12, 12, 0) };
        host.Controls.Add(header);
        return host;
    }

    private Control BuildLeftDockPanel()
    {
        Control sidebar = BuildSidebar();
        sidebar.Dock = DockStyle.Fill;
        Panel host = new() { Dock = DockStyle.Left, Width = 122, Padding = new Padding(12, 8, 8, 8) };
        host.Controls.Add(sidebar);
        return host;
    }

    private Control BuildRightDockPanel()
    {
        Control preview = BuildRightPreviewPanel();
        preview.Dock = DockStyle.Fill;
        Panel host = new() { Dock = DockStyle.Right, Width = 332, Padding = new Padding(8, 8, 12, 8) };
        host.Controls.Add(preview);
        return host;
    }

    private Control BuildBottomDockPanel()
    {
        Control bottom = BuildBottomArea();
        bottom.Dock = DockStyle.Fill;
        Panel host = new() { Dock = DockStyle.Bottom, Height = 228, Padding = new Padding(12, 0, 12, 12) };
        host.Controls.Add(bottom);
        return host;
    }

    private void ConfigureMdiClientArea()
    {
        MdiClient? client = Controls.OfType<MdiClient>().FirstOrDefault();
        if (client is null)
        {
            return;
        }

        client.BackColor = Color.FromArgb(243, 246, 248);
        client.Dock = DockStyle.Fill;
    }

    private Control BuildLayout()
    {
        TableLayoutPanel root = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 220F));

        root.Controls.Add(BuildDashboardHeader(), 0, 0);
        root.Controls.Add(BuildWorkspaceArea(), 0, 1);
        root.Controls.Add(BuildBottomArea(), 0, 2);
        return root;
    }

    private Control BuildDashboardHeader()
    {
        Panel panel = new() { Dock = DockStyle.Fill, Padding = new Padding(16, 10, 16, 10) };

        FlowLayoutPanel title = new()
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Tag = "surface-flow"
        };
        Label titleLabel = new()
        {
            Text = "ExamGuard - Máy giáo viên",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 12.5F, FontStyle.Bold, GraphicsUnit.Point),
            Margin = new Padding(0, 4, 0, 0)
        };
        title.Controls.AddRange([AppIcons.Picture("shield", 26), titleLabel]);

        FlowLayoutPanel statusFlow = new()
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            WrapContents = false,
            Tag = "surface-flow"
        };
        statusFlow.Controls.AddRange([
            StatusChip(_statusLabel, "power"),
            StatusChip(_backendStatusLabel, "server"),
            StatusChip(_studentCountLabel, "users"),
            StatusChip(_sessionQuickLabel, "hash"),
            StatusChip(_targetScopeLabel, "monitor")]);

        panel.Controls.Add(statusFlow);
        panel.Controls.Add(title);
        return panel;
    }

    private Control BuildWorkspaceArea()
    {
        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320F));

        layout.Controls.Add(BuildSidebar(), 0, 0);
        layout.Controls.Add(BuildCenterWorkspace(), 1, 0);
        layout.Controls.Add(BuildRightPreviewPanel(), 2, 0);
        return layout;
    }

    private Control BuildSidebar()
    {
        FlowLayoutPanel sidebar = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(0, 0, 8, 0)
        };

        sidebar.Controls.Add(CreateSidebarButton("dashboard", "Dashboard", "layout-dashboard", () => ShowWorkspacePage("dashboard")));
        sidebar.Controls.Add(CreateSidebarButton("students", "Sinh viên", "users", () =>
        {
            ShowWorkspacePage("dashboard");
            _studentsList.Focus();
            _targetScopeLabel.Text = "Phạm vi: danh sách sinh viên";
        }));
        sidebar.Controls.Add(CreateSidebarButton("screen", "Màn hình", "monitor", () =>
        {
            ShowWorkspacePage("dashboard");
            _detailFrame.Focus();
            _targetScopeLabel.Text = "Phạm vi: màn hình đang chọn";
        }));
        sidebar.Controls.Add(CreateSidebarButton("webcam", "Webcam", "video", () =>
        {
            ShowWorkspacePage("dashboard");
            _webcamFrame.Focus();
            _targetScopeLabel.Text = "Phạm vi: webcam đang chọn";
        }));
        sidebar.Controls.Add(CreateSidebarButton("policy", "Quy định", "shield", () => ShowWorkspacePage("policy")));
        sidebar.Controls.Add(CreateSidebarButton("server", "Máy chủ", "server", () => ShowWorkspacePage("backend")));
        sidebar.Controls.Add(CreateSidebarButton("alert", "Cảnh báo", "bell", () => ShowWorkspacePage("control")));
        sidebar.Controls.Add(CreateSidebarButton("exam", "Đề thi", "folder", () => ShowWorkspacePage("distribution")));
        sidebar.Controls.Add(CreateSidebarButton("logs", "Nhật ký", "file-text", () =>
        {
            ShowWorkspacePage("dashboard");
            _eventLog.Focus();
            _targetScopeLabel.Text = "Phạm vi: nhật ký sự kiện";
        }));
        return sidebar;
    }

    private Button CreateSidebarButton(string key, string text, string iconName, Action onClick)
    {
        Button button = IconButton(text, iconName, 92);
        button.TextAlign = ContentAlignment.MiddleCenter;
        button.ImageAlign = ContentAlignment.TopCenter;
        button.TextImageRelation = TextImageRelation.ImageAboveText;
        button.Height = 60;
        button.Width = 96;
        button.Click += (_, _) =>
        {
            SetActiveSidebarButton(key);
            onClick();
        };
        _sidebarButtons[key] = button;
        return button;
    }

    private void ShowWorkspacePage(string key)
    {
        string resolvedKey = key switch
        {
            "students" or "screen" or "webcam" or "logs" => "dashboard",
            _ => key
        };

        if (!_workspacePages.TryGetValue(resolvedKey, out WorkspaceChildForm? page))
        {
            page = CreateWorkspacePage(resolvedKey);
            page.MdiParent = this;
            _workspacePages[resolvedKey] = page;
        }

        foreach ((string existingKey, WorkspaceChildForm existingPage) in _workspacePages)
        {
            if (!string.Equals(existingKey, resolvedKey, StringComparison.OrdinalIgnoreCase) && existingPage.Visible)
            {
                existingPage.Hide();
            }
        }

        if (!page.Visible)
        {
            page.Show();
        }

        page.WindowState = FormWindowState.Maximized;
        page.BringToFront();
        page.Activate();
        SetActiveSidebarButton(key);
    }

    private WorkspaceChildForm CreateWorkspacePage(string key)
    {
        return key switch
        {
            "policy" => new WorkspaceChildForm(key, "Quy định", BuildSingleSectionPage(BuildPolicyGroup())),
            "backend" => new WorkspaceChildForm(key, "Máy chủ", BuildSingleSectionPage(BuildBackendGroup())),
            "control" => new WorkspaceChildForm(key, "Cảnh báo", BuildSingleSectionPage(BuildControlGroup())),
            "distribution" => new WorkspaceChildForm(key, "Đề thi", BuildSingleSectionPage(BuildDistributionGroup())),
            _ => new WorkspaceChildForm(key, "Dashboard", BuildDashboardWorkspacePage())
        };
    }

    private Control BuildDashboardWorkspacePage()
    {
        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            Padding = new Padding(0, 8, 8, 8)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 270F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.Controls.Add(BuildSingleSectionPage(BuildSessionGroup()), 0, 0);
        layout.Controls.Add(WrapCard("Giám sát màn hình sinh viên", BuildMosaicArea(), "monitor"), 0, 1);
        return layout;
    }

    private Control BuildSingleSectionPage(Control section)
    {
        Panel host = new() { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(0, 8, 8, 8) };
        section.Dock = DockStyle.Top;
        host.Controls.Add(section);
        return host;
    }

    private Control BuildCenterWorkspace()
    {
        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            Margin = new Padding(0, 0, 8, 0)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 280F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        layout.Controls.Add(BuildAccordionHost(), 0, 0);
        layout.Controls.Add(WrapCard("Giám sát màn hình sinh viên", BuildMosaicArea(), "monitor"), 0, 1);
        return layout;
    }

    private Control BuildAccordionHost()
    {
        Panel host = new() { Dock = DockStyle.Fill, AutoScroll = true };
        TableLayoutPanel stack = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 5
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        stack.Controls.Add(CreateAccordionSection("session", "Phiên thi và kết nối", "play-circle", BuildSessionGroup(), "Mã phiên, bảo vệ, cổng"), 0, 0);
        stack.Controls.Add(CreateAccordionSection("policy", "Giám sát và quy định", "shield", BuildPolicyGroup(), "Màn hình, webcam, chặn truy cập"), 0, 1);
        stack.Controls.Add(CreateAccordionSection("backend", "Máy chủ dữ liệu", "server", BuildBackendGroup(), "Đăng nhập, phiên, báo cáo"), 0, 2);
        stack.Controls.Add(CreateAccordionSection("control", "Điều khiển và cảnh báo", "bell", BuildControlGroup(), "Tin nhắn, dơ tay, khóa, lệnh"), 0, 3);
        stack.Controls.Add(CreateAccordionSection("distribution", "Phân phối đề thi", "folder", BuildDistributionGroup(), "Phát tệp, thư mục, màn hình"), 0, 4);

        host.Controls.Add(stack);
        ExpandAccordion("session");
        return host;
    }

    private Control CreateAccordionSection(string key, string title, string iconName, Control content, string summary)
    {
        Panel root = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8)
        };

        Button header = new()
        {
            Dock = DockStyle.Top,
            Height = 42,
            Text = $"{title}  •  {summary}",
            TextAlign = ContentAlignment.MiddleLeft,
            Tag = iconName
        };
        header.Click += (_, _) => ExpandAccordion(key);

        Panel body = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 0),
            Visible = false
        };
        content.Dock = DockStyle.Top;
        body.Controls.Add(content);
        _accordionBodies[key] = body;
        _accordionSections[key] = root;

        root.Controls.Add(body);
        root.Controls.Add(header);
        return root;
    }

    private void ExpandAccordion(string key)
    {
        foreach ((string currentKey, Panel body) in _accordionBodies)
        {
            body.Visible = string.Equals(currentKey, key, StringComparison.OrdinalIgnoreCase) && !body.Visible
                ? true
                : string.Equals(currentKey, key, StringComparison.OrdinalIgnoreCase)
                    ? true
                    : false;
        }

        SetActiveSidebarButton(key);

        if (_accordionSections.TryGetValue(key, out Control? section) && section.Parent is ScrollableControl scrollable)
        {
            scrollable.ScrollControlIntoView(section);
        }
    }

    private void SetActiveSidebarButton(string key)
    {
        foreach ((string currentKey, Button button) in _sidebarButtons)
        {
            button.BackColor = string.Equals(currentKey, key, StringComparison.OrdinalIgnoreCase)
                ? Color.FromArgb(224, 243, 240)
                : Color.White;
        }
    }

    private Control BuildMosaicArea()
    {
        Panel panel = new() { Dock = DockStyle.Fill };
        _mosaicEmptyLabel.BringToFront();
        panel.Controls.Add(_mosaicPanel);
        panel.Controls.Add(_mosaicEmptyLabel);
        return panel;
    }

    private Control BuildRightPreviewPanel()
    {
        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 42F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 28F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 18F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 12F));

        Panel screenPanel = new() { Dock = DockStyle.Fill };
        screenPanel.Controls.Add(_detailFrame);
        screenPanel.Controls.Add(_detailStatusLabel);

        Panel webcamPanel = new() { Dock = DockStyle.Fill };
        webcamPanel.Controls.Add(_webcamFrame);
        webcamPanel.Controls.Add(_webcamDetailStatus);

        layout.Controls.Add(WrapCard("Màn hình đang chọn", screenPanel, "monitor"), 0, 0);
        layout.Controls.Add(WrapCard("Webcam đang chọn", webcamPanel, "video"), 0, 1);
        layout.Controls.Add(WrapCard("Thông tin sinh viên", _selectedStudentInfoText, "user"), 0, 2);
        layout.Controls.Add(WrapCard("Trao đổi nhanh", _chatHistoryText, "message-circle"), 0, 3);
        return layout;
    }

    private Control BuildBottomArea()
    {
        SplitContainer split = new()
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 760
        };
        split.Panel1.Controls.Add(WrapCard("Danh sách sinh viên", _studentsList, "users"));
        split.Panel2.Controls.Add(WrapCard("Nhật ký sự kiện", _eventLog, "file-text"));
        return split;
    }

    private GroupBox BuildControlGroup()
    {
        GroupBox controlBox = CreateGroupBox("Điều khiển và cảnh báo");
        TableLayoutPanel controlGrid = CreateFieldGrid(155);
        Button chatButton = IconButton("Gửi tin nhắn", "send", 125);
        chatButton.Click += async (_, _) => await SendChatAsync();
        Button chatAllButton = IconButton("Chat tất cả", "message-circle", 115);
        chatAllButton.Click += async (_, _) => await SendChatAllAsync();
        Button clearHandButton = IconButton("Xóa dơ tay", "x-circle", 115);
        clearHandButton.Click += async (_, _) => await ClearHandRaiseAsync();
        Button attentionButton = IconButton("Gọi chú ý", "bell", 110);
        attentionButton.Click += async (_, _) => await SendAttentionAsync();
        Button lockButton = IconButton("Khóa màn hình", "lock", 125);
        lockButton.Click += async (_, _) => await SendLockAsync();
        Button unlockButton = IconButton("Mở khóa", "unlock", 95);
        unlockButton.Click += async (_, _) => await SendUnlockAsync();
        Button typeTextButton = IconButton("Nhập văn bản", "type", 120);
        typeTextButton.Click += async (_, _) => await SendRemoteTextInputAsync();
        Button clipboardButton = IconButton("Đặt clipboard", "clipboard", 120);
        clipboardButton.Click += async (_, _) => await SendClipboardSetAsync();
        Button commandButton = IconButton("Chạy lệnh", "terminal", 105);
        commandButton.Click += async (_, _) => await SendCommandAsync();

        controlGrid.Controls.Add(LabelFor("Tin nhắn / ghi chú"), 0, 0);
        controlGrid.Controls.Add(_teacherMessageText, 1, 0);
        FlowLayoutPanel row1 = CreateButtonRow(chatButton, chatAllButton, clearHandButton, attentionButton, lockButton, unlockButton);
        FlowLayoutPanel row2 = CreateButtonRow(typeTextButton, clipboardButton);
        controlGrid.Controls.Add(row1, 0, 1);
        controlGrid.SetColumnSpan(row1, 2);
        controlGrid.Controls.Add(row2, 0, 2);
        controlGrid.SetColumnSpan(row2, 2);
        controlGrid.Controls.Add(LabelFor("Lệnh từ xa"), 0, 3);
        controlGrid.Controls.Add(_teacherCommandText, 1, 3);
        FlowLayoutPanel row3 = CreateButtonRow(commandButton);
        controlGrid.Controls.Add(row3, 0, 4);
        controlGrid.SetColumnSpan(row3, 2);
        controlBox.Controls.Add(controlGrid);
        return controlBox;
    }

    private GroupBox BuildDistributionGroup()
    {
        GroupBox distributionBox = CreateGroupBox("Phân phối đề thi");
        TableLayoutPanel distributionGrid = CreateFieldGrid(120);
        Button fileButton = IconButton("Phát tệp", "file", 100);
        fileButton.Click += async (_, _) => await DistributeFileAsync();
        Button folderButton = IconButton("Phát thư mục", "folder", 130);
        folderButton.Click += async (_, _) => await DistributeFolderAsync();
        Button broadcastButton = IconButton("Phát màn hình", "cast", 130);
        broadcastButton.Click += (_, _) => ToggleTeacherBroadcast();
        distributionGrid.Controls.Add(LabelFor("Tiến trình"), 0, 0);
        distributionGrid.Controls.Add(_distributionProgress, 1, 0);
        FlowLayoutPanel distributionActions = CreateButtonRow(fileButton, folderButton, broadcastButton);
        distributionGrid.Controls.Add(distributionActions, 0, 1);
        distributionGrid.SetColumnSpan(distributionActions, 2);
        distributionBox.Controls.Add(distributionGrid);
        return distributionBox;
    }

    private Control WrapCard(string title, Control body, string iconName)
    {
        GroupBox box = CreateGroupBox(title);
        box.Margin = new Padding(0, 0, 0, 8);
        box.Controls.Add(body);
        return box;
    }

    private Panel BuildTopPanel()
    {
        TableLayoutPanel content = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1
        };
        content.Controls.Add(BuildSummaryPanel(), 0, 0);
        content.Controls.Add(BuildConfigPanel(), 0, 1);
        content.Controls.Add(BuildActionPanel(), 0, 2);

        Panel scrollHost = new()
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(0, 0, 0, 6)
        };
        scrollHost.Controls.Add(content);
        return scrollHost;
    }

    private Panel BuildSummaryPanel()
    {
        Panel panel = new() { Dock = DockStyle.Fill, Height = 48, Padding = new Padding(14, 10, 14, 10) };
        FlowLayoutPanel title = new()
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Tag = "surface-flow"
        };
        Label titleLabel = new()
        {
            Text = "Bảng điều khiển giám sát phòng thi",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point),
            Margin = new Padding(0, 4, 0, 0)
        };
        title.Controls.AddRange([AppIcons.Picture("shield", 26), titleLabel]);

        FlowLayoutPanel statusFlow = new()
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            WrapContents = false,
            Tag = "surface-flow"
        };
        statusFlow.Controls.AddRange([
            StatusChip(_statusLabel, "power"),
            StatusChip(_backendStatusLabel, "server"),
            StatusChip(_targetScopeLabel, "users")]);
        panel.Controls.Add(statusFlow);
        panel.Controls.Add(title);
        return panel;
    }

    private TableLayoutPanel BuildConfigPanel()
    {
        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0, 8, 0, 8)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
        layout.Controls.Add(BuildSessionGroup(), 0, 0);
        layout.Controls.Add(BuildPolicyGroup(), 1, 0);
        layout.Controls.Add(BuildBackendGroup(), 2, 0);
        return layout;
    }

    private GroupBox BuildSessionGroup()
    {
        GroupBox box = CreateGroupBox("Phiên thi và kết nối");
        TableLayoutPanel grid = CreateFieldGrid(145);

        Button startButton = IconButton("Bắt đầu phiên", "play-circle", 130);
        startButton.Click += async (_, _) => await StartServerAsync();
        Button stopButton = IconButton("Dừng phiên", "stop-circle", 110);
        stopButton.Click += async (_, _) => await StopServerAsync();

        grid.Controls.Add(LabelFor("Mã phiên"), 0, 0);
        grid.Controls.Add(_sessionIdText, 1, 0);
        grid.Controls.Add(LabelFor("Mã bảo vệ"), 0, 1);
        grid.Controls.Add(_sessionTokenText, 1, 1);
        grid.Controls.Add(LabelFor("Cổng"), 0, 2);
        grid.Controls.Add(_portInput, 1, 2);
        grid.Controls.Add(LabelFor("Kiểu kết nối"), 0, 3);
        grid.Controls.Add(_connectionModePicker, 1, 3);
        grid.Controls.Add(LabelFor("Host/IP từ xa"), 0, 4);
        grid.Controls.Add(_publishedHostText, 1, 4);
        grid.Controls.Add(LabelFor("Máy chủ relay"), 0, 5);
        grid.Controls.Add(_relayHostText, 1, 5);
        grid.Controls.Add(LabelFor("Cổng relay"), 0, 6);
        grid.Controls.Add(_relayPortInput, 1, 6);
        grid.Controls.Add(LabelFor("Mã relay"), 0, 7);
        grid.Controls.Add(_relaySecretText, 1, 7);
        grid.Controls.Add(_remoteJoinEnabledCheck, 0, 8);
        grid.SetColumnSpan(_remoteJoinEnabledCheck, 2);
        FlowLayoutPanel actions = CreateButtonRow(startButton, stopButton);
        grid.Controls.Add(actions, 0, 9);
        grid.SetColumnSpan(actions, 2);
        RemoveGridRows(grid, 4, 5, 6, 7);
        box.Controls.Add(grid);
        return box;
    }

    private GroupBox BuildPolicyGroup()
    {
        GroupBox box = CreateGroupBox("Giám sát và quy định");
        TableLayoutPanel grid = CreateFieldGrid(190);

        Button sendPolicyButton = IconButton("Lưu và gửi quy định", "save", 165);
        sendPolicyButton.Click += async (_, _) => await SendPolicyAsync();
        Button exportLogsButton = IconButton("Xuất nhật ký", "file-text", 115);
        exportLogsButton.Click += (_, _) => ExportLogs();

        grid.Controls.Add(LabelFor("Chu kỳ màn hình (ms)"), 0, 0);
        grid.Controls.Add(_screenIntervalInput, 1, 0);
        grid.Controls.Add(LabelFor("Chất lượng màn hình"), 0, 1);
        grid.Controls.Add(_screenQualityInput, 1, 1);
        grid.Controls.Add(_webcamEnabledCheck, 0, 2);
        grid.Controls.Add(_webcamSnapshotCheck, 1, 2);
        grid.Controls.Add(LabelFor("Chu kỳ webcam (ms)"), 0, 3);
        grid.Controls.Add(_webcamIntervalInput, 1, 3);
        grid.Controls.Add(LabelFor("Chất lượng webcam"), 0, 4);
        grid.Controls.Add(_webcamQualityInput, 1, 4);
        grid.Controls.Add(LabelFor("Thời gian làm bài (phút)"), 0, 5);
        grid.Controls.Add(_examDurationMinutesInput, 1, 5);
        grid.Controls.Add(_allowLateSubmissionCheck, 0, 6);
        grid.SetColumnSpan(_allowLateSubmissionCheck, 2);
        grid.Controls.Add(_blockClipboardCheck, 0, 7);
        grid.SetColumnSpan(_blockClipboardCheck, 2);
        grid.Controls.Add(LabelFor("Phần mềm bị chặn"), 0, 8);
        grid.Controls.Add(_blockedProcessesText, 1, 8);
        grid.Controls.Add(LabelFor("Từ khóa tiêu đề chặn"), 0, 9);
        grid.Controls.Add(_blockedKeywordsText, 1, 9);
        grid.Controls.Add(LabelFor("Website được phép"), 0, 10);
        grid.Controls.Add(_allowedWebsitesText, 1, 10);
        FlowLayoutPanel actions = CreateButtonRow(sendPolicyButton, exportLogsButton);
        grid.Controls.Add(actions, 0, 11);
        grid.SetColumnSpan(actions, 2);
        box.Controls.Add(grid);
        return box;
    }

    private GroupBox BuildBackendGroup()
    {
        GroupBox box = CreateGroupBox("Máy chủ dữ liệu");
        TableLayoutPanel grid = CreateFieldGrid(145);

        Button loginButton = IconButton("Đăng nhập máy chủ", "log-in", 155);
        loginButton.Click += async (_, _) => await LoginBackendAsync();
        Button refreshButton = IconButton("Tải lại phiên", "refresh-cw", 120);
        refreshButton.Click += async (_, _) => await RefreshSessionsAsync();
        Button loadPolicyButton = IconButton("Nạp quy định", "settings", 120);
        loadPolicyButton.Click += async (_, _) => await LoadSessionPolicyAsync();
        Button startButton = IconButton("Mở phiên", "play-circle", 105);
        startButton.Click += async (_, _) => await StartBackendSessionAsync();
        Button finishButton = IconButton("Kết thúc phiên", "stop-circle", 130);
        finishButton.Click += async (_, _) => await FinishBackendSessionAsync();
        Button reportButton = IconButton("Xuất báo cáo", "bar-chart-2", 115);
        reportButton.Click += async (_, _) => await ExportSessionReportAsync();

        grid.Controls.Add(LabelFor("Địa chỉ máy chủ"), 0, 0);
        grid.Controls.Add(_backendUrlText, 1, 0);
        grid.Controls.Add(LabelFor("Tài khoản"), 0, 1);
        grid.Controls.Add(_backendUserText, 1, 1);
        grid.Controls.Add(LabelFor("Mật khẩu"), 0, 2);
        grid.Controls.Add(_backendPasswordText, 1, 2);
        grid.Controls.Add(LabelFor("Phiên máy chủ"), 0, 3);
        grid.Controls.Add(_backendSessionPicker, 1, 3);
        grid.Controls.Add(LabelFor("Mã số phiên"), 0, 4);
        grid.Controls.Add(_backendSessionIdInput, 1, 4);
        FlowLayoutPanel row1 = CreateButtonRow(loginButton, refreshButton, loadPolicyButton);
        FlowLayoutPanel row2 = CreateButtonRow(startButton, finishButton, reportButton);
        grid.Controls.Add(row1, 0, 5);
        grid.SetColumnSpan(row1, 2);
        grid.Controls.Add(row2, 0, 6);
        grid.SetColumnSpan(row2, 2);
        RemoveGridRows(grid, 0);
        box.Controls.Add(grid);
        return box;
    }

    private TableLayoutPanel BuildActionPanel()
    {
        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 8)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));

        GroupBox controlBox = CreateGroupBox("Điều khiển và cảnh báo");
        TableLayoutPanel controlGrid = CreateFieldGrid(155);
        Button chatButton = IconButton("Gửi tin nhắn", "send", 125);
        chatButton.Click += async (_, _) => await SendChatAsync();
        Button chatAllButton = IconButton("Chat tất cả", "message-circle", 115);
        chatAllButton.Click += async (_, _) => await SendChatAllAsync();
        Button clearHandButton = IconButton("Xóa dơ tay", "x-circle", 115);
        clearHandButton.Click += async (_, _) => await ClearHandRaiseAsync();
        Button attentionButton = IconButton("Gọi chú ý", "bell", 110);
        attentionButton.Click += async (_, _) => await SendAttentionAsync();
        Button lockButton = IconButton("Khóa màn hình", "lock", 125);
        lockButton.Click += async (_, _) => await SendLockAsync();
        Button unlockButton = IconButton("Mở khóa", "unlock", 95);
        unlockButton.Click += async (_, _) => await SendUnlockAsync();
        Button typeTextButton = IconButton("Nhập văn bản", "type", 120);
        typeTextButton.Click += async (_, _) => await SendRemoteTextInputAsync();
        Button clipboardButton = IconButton("Đặt clipboard", "clipboard", 120);
        clipboardButton.Click += async (_, _) => await SendClipboardSetAsync();
        Button commandButton = IconButton("Chạy lệnh", "terminal", 105);
        commandButton.Click += async (_, _) => await SendCommandAsync();

        controlGrid.Controls.Add(LabelFor("Tin nhắn / ghi chú"), 0, 0);
        controlGrid.Controls.Add(_teacherMessageText, 1, 0);
        FlowLayoutPanel row1 = CreateButtonRow(chatButton, chatAllButton, clearHandButton, attentionButton, lockButton, unlockButton);
        FlowLayoutPanel row2 = CreateButtonRow(typeTextButton, clipboardButton);
        controlGrid.Controls.Add(row1, 0, 1);
        controlGrid.SetColumnSpan(row1, 2);
        controlGrid.Controls.Add(row2, 0, 2);
        controlGrid.SetColumnSpan(row2, 2);
        controlGrid.Controls.Add(LabelFor("Lệnh từ xa"), 0, 3);
        controlGrid.Controls.Add(_teacherCommandText, 1, 3);
        FlowLayoutPanel row3 = CreateButtonRow(commandButton);
        controlGrid.Controls.Add(row3, 0, 4);
        controlGrid.SetColumnSpan(row3, 2);
        controlBox.Controls.Add(controlGrid);

        GroupBox distributionBox = CreateGroupBox("Phân phối đề thi");
        TableLayoutPanel distributionGrid = CreateFieldGrid(120);
        Button fileButton = IconButton("Phát tệp", "file", 100);
        fileButton.Click += async (_, _) => await DistributeFileAsync();
        Button folderButton = IconButton("Phát thư mục", "folder", 130);
        folderButton.Click += async (_, _) => await DistributeFolderAsync();
        Button broadcastButton = IconButton("Phát màn hình", "cast", 130);
        broadcastButton.Click += (_, _) => ToggleTeacherBroadcast();
        distributionGrid.Controls.Add(LabelFor("Tiến trình"), 0, 0);
        distributionGrid.Controls.Add(_distributionProgress, 1, 0);
        FlowLayoutPanel distributionActions = CreateButtonRow(fileButton, folderButton, broadcastButton);
        distributionGrid.Controls.Add(distributionActions, 0, 1);
        distributionGrid.SetColumnSpan(distributionActions, 2);
        distributionBox.Controls.Add(distributionGrid);

        layout.Controls.Add(controlBox, 0, 0);
        layout.Controls.Add(distributionBox, 1, 0);
        return layout;
    }

    private static Label LabelFor(string text)
    {
        return new Label { Text = text, AutoSize = true, Margin = new Padding(8, 8, 4, 0) };
    }

    private static Button IconButton(string text, string iconName, int width)
    {
        return new Button { Text = text, Width = width, Tag = iconName };
    }

    private static GroupBox CreateGroupBox(string text)
    {
        return new GroupBox
        {
            Text = text,
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(12),
            Margin = new Padding(0, 0, 8, 8)
        };
    }

    private static TableLayoutPanel CreateFieldGrid(int labelWidth)
    {
        TableLayoutPanel grid = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, labelWidth));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        return grid;
    }

    private static FlowLayoutPanel CreateButtonRow(params Control[] controls)
    {
        FlowLayoutPanel row = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,
            Margin = new Padding(0, 6, 0, 0)
        };
        row.Controls.AddRange(controls);
        return row;
    }

    private static void RemoveGridRows(TableLayoutPanel grid, params int[] rowsToRemove)
    {
        int[] rows = rowsToRemove.Distinct().OrderBy(x => x).ToArray();
        if (rows.Length == 0)
        {
            return;
        }

        foreach (Control control in grid.Controls.Cast<Control>().Where(control => rows.Contains(grid.GetRow(control))).ToList())
        {
            grid.Controls.Remove(control);
        }

        foreach (Control control in grid.Controls.Cast<Control>())
        {
            int row = grid.GetRow(control);
            int shift = rows.Count(removedRow => removedRow < row);
            if (shift > 0)
            {
                grid.SetRow(control, row - shift);
            }
        }
    }

    private static Panel StatusChip(Label label, string iconName)
    {
        FlowLayoutPanel panel = new()
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(10, 6, 10, 6),
            Margin = new Padding(6, 0, 0, 0),
            Tag = "status-chip"
        };
        label.Margin = new Padding(0, 1, 0, 0);
        panel.Controls.Add(AppIcons.Picture(iconName, 14));
        panel.Controls.Add(label);
        return panel;
    }

    private async Task StartServerAsync()
    {
        if (_server is not null)
        {
            return;
        }

        string rootPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            "ExamGuard - Bai nop sinh vien");
        Directory.CreateDirectory(rootPath);

        SubmissionReceiver submissionReceiver = new(rootPath);
        string connectionMode = SelectedConnectionMode();
        if (connectionMode == "relay")
        {
            TeacherRelayClient relay = new(
                BuildPolicy,
                state => BeginInvoke(() => UpsertStudent(state)),
                message => BeginInvoke(() => Log(message)),
                (state, process, title) => _ = PostEventAsync(state, "process_violation", new { processName = process, windowTitle = title }),
                (state, path, sha256) => _ = PostSubmissionAsync(state, path, sha256),
                submissionReceiver,
                (state, message, scope) => _ = OnStudentChatAsync(state, message, scope),
                (state, message) => _ = OnHandRaisedAsync(state, message),
                (state, eventType, payload) => _ = PostEventAsync(state, eventType, payload),
                (state, eventType, reason) => _ = PostEventAsync(state, eventType, new { reason }),
                state => _ = OnStudentConnectedAsync(state));

            await relay.StartAsync(
                _relayHostText.Text.Trim(),
                (int)_relayPortInput.Value,
                _sessionIdText.Text.Trim(),
                _sessionTokenText.Text.Trim(),
                _relaySecretText.Text.Trim());
            _server = relay;
            _discoveryBroadcaster.Stop();
        }
        else
        {
            TeacherSocketServer server = new(
            BuildPolicy,
            state => BeginInvoke(() => UpsertStudent(state)),
            message => BeginInvoke(() => Log(message)),
            (state, process, title) => _ = PostEventAsync(state, "process_violation", new { processName = process, windowTitle = title }),
            (state, path, sha256) => _ = PostSubmissionAsync(state, path, sha256),
            submissionReceiver,
            (state, message, scope) => _ = OnStudentChatAsync(state, message, scope),
            (state, message) => _ = OnHandRaisedAsync(state, message),
            (state, eventType, payload) => _ = PostEventAsync(state, eventType, payload),
            (state, eventType, reason) => _ = PostEventAsync(state, eventType, new { reason }),
            state => _ = OnStudentConnectedAsync(state));

            await server.StartAsync((int)_portInput.Value, _sessionIdText.Text.Trim(), _sessionTokenText.Text.Trim());
            _server = server;
            _discoveryBroadcaster.Start(_sessionIdText.Text.Trim(), (int)_portInput.Value);
        }
        _sessionStartedAtUtc ??= DateTimeOffset.UtcNow;
        _statusLabel.Text = "Đang hoạt động";
        UpdateTargetScopeLabel();
        _sessionAccessRefreshTimer.Start();
        await PublishSessionAccessAsync();
    }

    private async Task StopServerAsync()
    {
        if (_server is null)
        {
            return;
        }

        await _server.StopAsync();
        _server = null;
        _cards.Clear();
        _mosaicPanel.Controls.Clear();
        foreach (FloatingImageViewerForm viewer in _liveViewers.Values.ToList())
        {
            viewer.Close();
        }
        _liveViewers.Clear();
        _studentsList.Items.Clear();
        ReplaceImage(_detailFrame, null);
        ReplaceImage(_webcamFrame, null);
        _webcamDetailStatus.Text = "Webcam: chưa chọn sinh viên";
        _statusLabel.Text = "Đã dừng";
        _teacherBroadcastTimer.Stop();
        _sessionAccessRefreshTimer.Stop();
        _discoveryBroadcaster.Stop();
        _sessionStartedAtUtc = null;
        UpdateTargetScopeLabel();
    }

    private async Task SendPolicyAsync()
    {
        if (_server is null)
        {
            Log("Hãy bắt đầu phiên trước khi gửi quy định.");
            return;
        }

        if (_backendClient.IsAuthenticated && (int)_backendSessionIdInput.Value > 0)
        {
            try
            {
                SessionPolicyDto? saved = await _backendClient.UpdateSessionPolicyAsync((int)_backendSessionIdInput.Value, BuildPolicy());
                if (saved is not null)
                {
                    ApplyPolicyToControls(saved);
                    Log($"Đã lưu quy định lên máy chủ cho phiên {saved.SessionCode}.");
                }
            }
            catch (Exception ex)
            {
                Log($"Lưu quy định lên máy chủ thất bại: {ex.Message}");
            }
        }

        await _server.BroadcastPolicyAsync();
        await PublishSessionAccessAsync();
    }

    private async Task SendChatAsync()
    {
        if (_server is null)
        {
            Log("Hãy bắt đầu phiên trước khi gửi tin nhắn.");
            return;
        }

        string? target = SelectedStudentId();
        await _server.SendChatAsync(target, _teacherMessageText.Text.Trim());
        await _backendClient.PostChatMessageAsync(CurrentBackendSessionId(), "teacher", "teacher", target, _teacherMessageText.Text.Trim(), target is null ? "all" : "one");
        AppendChatHistory($"Giáo viên -> {(target ?? "Tất cả")}: {_teacherMessageText.Text.Trim()}");
    }

    private async Task SendChatAllAsync()
    {
        if (_server is null)
        {
            Log("Hãy bắt đầu phiên trước khi gửi tin nhắn.");
            return;
        }

        await _server.SendChatAsync(null, _teacherMessageText.Text.Trim());
        await _backendClient.PostChatMessageAsync(CurrentBackendSessionId(), "teacher", "teacher", null, _teacherMessageText.Text.Trim(), "all");
        AppendChatHistory($"Giáo viên -> Tất cả: {_teacherMessageText.Text.Trim()}");
    }

    private async Task ClearHandRaiseAsync()
    {
        if (_server is null)
        {
            return;
        }

        await _server.SendHandRaiseClearAsync(SelectedStudentId());
        if (SelectedStudentId() is { } studentId && _cards.TryGetValue(studentId, out StudentCard? card))
        {
            AppendChatHistory($"Đã xóa dơ tay của {card.DisplayName}.");
        }
    }

    private async Task SendAttentionAsync()
    {
        if (_server is null)
        {
            Log("Hãy bắt đầu phiên trước khi gửi cảnh báo.");
            return;
        }

        await _server.SendAttentionAsync(SelectedStudentId(), _teacherMessageText.Text.Trim());
    }

    private async Task SendLockAsync()
    {
        if (_server is null)
        {
            Log("Hãy bắt đầu phiên trước khi khóa màn hình.");
            return;
        }

        await _server.SendLockAsync(SelectedStudentId(), _teacherMessageText.Text.Trim());
    }

    private async Task SendUnlockAsync()
    {
        if (_server is null)
        {
            Log("Hãy bắt đầu phiên trước khi mở khóa.");
            return;
        }

        await _server.SendUnlockAsync(SelectedStudentId());
    }

    private async Task SendCommandAsync()
    {
        if (_server is null)
        {
            Log("Hãy bắt đầu phiên trước khi chạy lệnh.");
            return;
        }

        await _server.SendExecuteCommandAsync(SelectedStudentId(), _teacherCommandText.Text.Trim());
    }

    private async Task SendRemoteMouseClickAsync(MouseEventArgs e)
    {
        if (_server is null || SelectedStudentId() is not { } studentId || _detailFrame.Image is null)
        {
            return;
        }

        double relativeX = Math.Clamp(e.X / (double)Math.Max(1, _detailFrame.Width), 0, 1);
        double relativeY = Math.Clamp(e.Y / (double)Math.Max(1, _detailFrame.Height), 0, 1);
        string button = e.Button == MouseButtons.Right ? "right" : "left";
        await _server.SendRemoteMouseClickAsync(studentId, relativeX, relativeY, button);
    }

    private async Task SendRemoteTextInputAsync()
    {
        if (_server is null)
        {
            Log("Hãy bắt đầu phiên trước khi nhập văn bản từ xa.");
            return;
        }

        await _server.SendRemoteTextInputAsync(SelectedStudentId(), _teacherMessageText.Text);
    }

    private async Task SendClipboardSetAsync()
    {
        if (_server is null)
        {
            Log("Hãy bắt đầu phiên trước khi đặt clipboard.");
            return;
        }

        await _server.SendClipboardSetAsync(SelectedStudentId(), _teacherMessageText.Text);
    }

    private async Task DistributeFileAsync()
    {
        if (_server is null)
        {
            Log("Hãy bắt đầu phiên trước khi phát tệp.");
            return;
        }

        using OpenFileDialog dialog = new()
        {
            Title = "Chọn tệp đề thi để phát cho sinh viên"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _distributionProgress.Value = 0;
        await _server.DistributeFileAsync(
            SelectedStudentId(),
            dialog.FileName,
            new Progress<int>(value => _distributionProgress.Value = Math.Clamp(value, 0, 100)));
        _distributionProgress.Value = 100;
    }

    private async Task DistributeFolderAsync()
    {
        if (_server is null)
        {
            Log("Hãy bắt đầu phiên trước khi phát thư mục.");
            return;
        }

        using FolderBrowserDialog dialog = new()
        {
            Description = "Chọn thư mục đề thi muốn phát cho sinh viên"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        string zipPath = Path.Combine(Path.GetTempPath(), $"de-thi-{DateTime.Now:yyyyMMddHHmmss}.zip");
        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        ZipFile.CreateFromDirectory(dialog.SelectedPath, zipPath, CompressionLevel.Fastest, includeBaseDirectory: true);
        _distributionProgress.Value = 0;
        await _server.DistributeFileAsync(
            SelectedStudentId(),
            zipPath,
            new Progress<int>(value => _distributionProgress.Value = Math.Clamp(value, 0, 100)));
        _distributionProgress.Value = 100;
        Log($"Đã phát thư mục dưới dạng tệp nén: {Path.GetFileName(zipPath)}");
    }

    private void ToggleTeacherBroadcast()
    {
        if (_server is null)
        {
            Log("Hãy bắt đầu phiên trước khi phát màn hình.");
            return;
        }

        if (_teacherBroadcastTimer.Enabled)
        {
            _teacherBroadcastTimer.Stop();
            Log("Đã dừng phát màn hình giáo viên.");
        }
        else
        {
            _teacherBroadcastTimer.Start();
            Log("Đang phát màn hình giáo viên.");
        }
    }

    private async Task BroadcastTeacherFrameAsync()
    {
        if (_server is null)
        {
            return;
        }

        try
        {
            await _server.SendTeacherFrameAsync(TeacherScreenCaptureService.CapturePrimaryScreenJpeg(40L));
        }
        catch (Exception ex)
        {
            Log($"Phát màn hình thất bại: {ex.Message}");
        }
    }

    private async Task LoginBackendAsync()
    {
        try
        {
            await _backendClient.LoginAsync(_backendUrlText.Text, _backendUserText.Text, _backendPasswordText.Text);
            _backendStatusLabel.Text = "Máy chủ: đã kết nối";
            Log("Đã đăng nhập máy chủ.");
            await RefreshSessionsAsync();
            await PublishSessionAccessAsync();
        }
        catch (Exception ex)
        {
            _backendStatusLabel.Text = "Máy chủ: đăng nhập lỗi";
            Log($"Đăng nhập máy chủ thất bại: {ex.Message}");
        }
    }

    private async Task RefreshSessionsAsync()
    {
        try
        {
            List<ExamSessionSummaryDto> sessions = await _backendClient.GetExamSessionsAsync();
            _backendSessionPicker.BeginUpdate();
            _backendSessionPicker.Items.Clear();
            foreach (ExamSessionSummaryDto session in sessions)
            {
                _backendSessionPicker.Items.Add(session);
            }

            _backendSessionPicker.EndUpdate();
            if (sessions.Count > 0)
            {
                ExamSessionSummaryDto selected = sessions.FirstOrDefault(x => x.Id == (int)_backendSessionIdInput.Value) ?? sessions[0];
                _backendSessionPicker.SelectedItem = selected;
                _backendSessionIdInput.Value = selected.Id;
            }

            Log($"Đã tải {sessions.Count} phiên từ máy chủ.");
        }
        catch (Exception ex)
        {
            Log($"Tải phiên thất bại: {ex.Message}");
        }
    }

    private async Task StartBackendSessionAsync()
    {
        try
        {
            ExamSessionSummaryDto? session = await _backendClient.StartExamSessionAsync(CurrentBackendSessionId());
            if (session is null)
            {
                Log("Chưa đăng nhập máy chủ hoặc chưa chọn phiên.");
                return;
            }

            ApplyBackendSessionSelection(session);
            _sessionStartedAtUtc = ParseUtcOrNull(session.StartedAtUtc);
            Log($"Đã mở phiên {session.Code} trên máy chủ.");
            await PublishSessionAccessAsync();
        }
        catch (Exception ex)
        {
            Log($"Mở phiên máy chủ thất bại: {ex.Message}");
        }
    }

    private async Task FinishBackendSessionAsync()
    {
        try
        {
            ExamSessionSummaryDto? session = await _backendClient.FinishExamSessionAsync(CurrentBackendSessionId());
            if (session is null)
            {
                Log("Chưa đăng nhập máy chủ hoặc chưa chọn phiên.");
                return;
            }

            ApplyBackendSessionSelection(session);
            Log($"Đã kết thúc phiên {session.Code} trên máy chủ.");
        }
        catch (Exception ex)
        {
            Log($"Kết thúc phiên máy chủ thất bại: {ex.Message}");
        }
    }

    private async Task LoadSessionPolicyAsync()
    {
        try
        {
            SessionPolicyDto? policy = await _backendClient.GetSessionPolicyAsync((int)_backendSessionIdInput.Value);
            if (policy is null)
            {
                Log("Chưa đăng nhập máy chủ hoặc phiên không tồn tại.");
                return;
            }

            _sessionIdText.Text = policy.SessionCode;
            _sessionTokenText.Text = policy.SessionToken;
            _backendSessionIdInput.Value = policy.SessionId;
            _sessionStartedAtUtc = ParseUtcOrNull(policy.StartedAtUtc);
            ApplyPolicyToControls(policy);
            Log($"Đã nạp quy định cho phiên {policy.SessionCode}.");
        }
        catch (Exception ex)
        {
            Log($"Nạp quy định thất bại: {ex.Message}");
        }
    }

    private async Task ExportSessionReportAsync()
    {
        try
        {
            SessionReportDto? report = await _backendClient.GetSessionReportAsync((int)_backendSessionIdInput.Value);
            if (report is null)
            {
                Log("Chưa có báo cáo từ máy chủ.");
                return;
            }

            string folder = Path.Combine(AppContext.BaseDirectory, "Exports");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, $"session-report-{report.SessionCode}-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            string json = System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
            Log($"Đã xuất báo cáo: {path}");
        }
        catch (Exception ex)
        {
            Log($"Xuất báo cáo thất bại: {ex.Message}");
        }
    }

    private async Task OnStudentChatAsync(StudentState state, string message, string scope)
    {
        SystemSounds.Asterisk.Play();
        BeginInvoke(() => AppendChatHistory($"{state.DisplayName}: {message}"));
        await _backendClient.PostChatMessageAsync(CurrentBackendSessionId(), "student", state.StudentCodeOrId, "teacher", message, scope);
    }

    private async Task OnStudentConnectedAsync(StudentState state)
    {
        await PostEventAsync(state, "student_connected", new
        {
            studentCode = state.StudentCodeOrId,
            studentName = state.StudentName,
            windowsUserName = state.WindowsUserName,
            machineName = state.MachineName,
            remoteEndPoint = state.RemoteEndPoint,
            connectedAt = state.ConnectedAt.ToString("O")
        });
    }

    private async Task OnHandRaisedAsync(StudentState state, string message)
    {
        SystemSounds.Exclamation.Play();
        BeginInvoke(() => AppendChatHistory($"{state.DisplayName} đã dơ tay{(string.IsNullOrWhiteSpace(message) ? "" : $": {message}")}"));
        await PostEventAsync(state, "hand_raised", new { message });
    }

    private async Task PostEventAsync(StudentState state, string eventType, object payload)
    {
        try
        {
            await _backendClient.PostEventAsync((int)_backendSessionIdInput.Value, state, eventType, payload);
        }
        catch (Exception ex)
        {
            BeginInvoke(() => Log($"Ghi sự kiện máy chủ thất bại: {ex.Message}"));
        }
    }

    private async Task PostSubmissionAsync(StudentState state, string path, string sha256)
    {
        try
        {
            await _backendClient.PostSubmissionAsync((int)_backendSessionIdInput.Value, state, path, sha256);
        }
        catch (Exception ex)
        {
            BeginInvoke(() => Log($"Ghi bài nộp lên máy chủ thất bại: {ex.Message}"));
        }
    }

    private PolicySnapshot BuildPolicy()
    {
        return new PolicySnapshot
        {
            BlockedProcesses = SplitRules(_blockedProcessesText.Text),
            BlockedWindowKeywords = SplitRules(_blockedKeywordsText.Text),
            ScreenIntervalMs = (int)_screenIntervalInput.Value,
            ScreenJpegQuality = (long)_screenQualityInput.Value,
            WebcamEnabled = _webcamEnabledCheck.Checked,
            WebcamSnapshotOnConnect = _webcamSnapshotCheck.Checked,
            WebcamIntervalMs = (int)_webcamIntervalInput.Value,
            WebcamJpegQuality = (long)_webcamQualityInput.Value,
            ExamDurationMinutes = (int)_examDurationMinutesInput.Value,
            AllowSubmissionAfterDeadline = _allowLateSubmissionCheck.Checked,
            StartedAtUtc = _sessionStartedAtUtc?.ToUniversalTime().ToString("O") ?? "",
            ExamEndAtUtc = ComputeExamEndAtUtc(),
            ConnectionMode = SelectedConnectionMode(),
            BlockClipboardShortcuts = _blockClipboardCheck.Checked,
            WebsitePolicyMode = SplitRules(_allowedWebsitesText.Text).Count > 0 ? "allowlist" : "off",
            AllowedWebsiteHosts = SplitRules(_allowedWebsitesText.Text)
        };
    }

    private static List<string> SplitRules(string value)
    {
        return value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    private void ApplyPolicyToControls(SessionPolicyDto policy)
    {
        _blockedProcessesText.Text = string.Join(';', policy.BlockedProcesses);
        _blockedKeywordsText.Text = string.Join(';', policy.BlockedWindowKeywords);
        _allowedWebsitesText.Text = string.Join(';', policy.AllowedWebsiteHosts);
        _screenIntervalInput.Value = Math.Clamp(policy.ScreenIntervalMs, (int)_screenIntervalInput.Minimum, (int)_screenIntervalInput.Maximum);
        _screenQualityInput.Value = Math.Clamp(policy.ScreenJpegQuality, (int)_screenQualityInput.Minimum, (int)_screenQualityInput.Maximum);
        _webcamEnabledCheck.Checked = policy.WebcamEnabled;
        _webcamSnapshotCheck.Checked = policy.WebcamSnapshotOnConnect;
        _webcamIntervalInput.Value = Math.Clamp(policy.WebcamIntervalMs, (int)_webcamIntervalInput.Minimum, (int)_webcamIntervalInput.Maximum);
        _webcamQualityInput.Value = Math.Clamp(policy.WebcamJpegQuality, (int)_webcamQualityInput.Minimum, (int)_webcamQualityInput.Maximum);
        _examDurationMinutesInput.Value = Math.Clamp(policy.ExamDurationMinutes, (int)_examDurationMinutesInput.Minimum, (int)_examDurationMinutesInput.Maximum);
        _allowLateSubmissionCheck.Checked = policy.AllowSubmissionAfterDeadline;
        _blockClipboardCheck.Checked = policy.BlockClipboardShortcuts;
        _sessionStartedAtUtc = ParseUtcOrNull(policy.StartedAtUtc);
        SelectConnectionMode(policy.ConnectionMode);
        _remoteJoinEnabledCheck.Checked = policy.RemoteJoinEnabled;
        _publishedHostText.Text = policy.PublishedHost ?? "";
        _relayHostText.Text = policy.RelayHost ?? _relayHostText.Text;
        if (policy.RelayPort is > 0)
        {
            _relayPortInput.Value = Math.Clamp(policy.RelayPort.Value, (int)_relayPortInput.Minimum, (int)_relayPortInput.Maximum);
        }
        _relaySecretText.Text = policy.RelaySecret ?? _relaySecretText.Text;
        if (policy.PublishedPort is > 0)
        {
            _portInput.Value = Math.Clamp(policy.PublishedPort.Value, (int)_portInput.Minimum, (int)_portInput.Maximum);
        }
    }

    private void UpsertStudent(StudentState state)
    {
        if (!_cards.TryGetValue(state.StudentId, out StudentCard? card))
        {
            card = new StudentCard(state.StudentId);
            card.Click += (_, _) => SelectStudent(state.StudentId);
            card.ScreenDoubleClicked += (_, _) => ShowImageViewer(state.StudentId, "screen", $"Màn hình {state.DisplayName}", card.CurrentImage);
            card.WebcamDoubleClicked += (_, _) => ShowImageViewer(state.StudentId, "webcam", $"Webcam {state.DisplayName}", card.CurrentWebcamImage);
            _cards[state.StudentId] = card;
            _mosaicPanel.Controls.Add(card);
        }

        card.Update(state);
        UpdateMosaicEmptyState();
        UpdateLiveViewer(state.StudentId, "screen", $"Màn hình {state.DisplayName}", card.CurrentImage);
        UpdateLiveViewer(state.StudentId, "webcam", $"Webcam {state.DisplayName}", card.CurrentWebcamImage);
        UpsertListItem(state);
        if (string.Equals(SelectedStudentId(), state.StudentId, StringComparison.Ordinal))
        {
            ShowSelectedStudent();
        }
    }

    private void UpsertListItem(StudentState state)
    {
        ListViewItem? item = _studentsList.Items.Cast<ListViewItem>().FirstOrDefault(x => x.Tag as string == state.StudentId);
        if (item is null)
        {
            item = new ListViewItem(state.DisplayName) { Tag = state.StudentId };
            for (int i = 0; i < 7; i++)
            {
                item.SubItems.Add("");
            }

            _studentsList.Items.Add(item);
        }

        item.Text = state.DisplayName;
        item.SubItems[1].Text = state.IsOnline ? "Trực tuyến" : "Mất kết nối";
        item.SubItems[2].Text = state.WindowsUserName;
        item.SubItems[3].Text = state.MachineName;
        item.SubItems[4].Text = state.LastSeen.ToLocalTime().ToString("HH:mm:ss");
        item.SubItems[5].Text = state.SubmissionStatus;
        item.SubItems[6].Text = state.WebcamStatus;
        item.SubItems[7].Text = BuildActivityText(state);
        item.ImageKey = !string.IsNullOrWhiteSpace(state.LastViolation) ? "alert" : state.IsOnline ? "online" : "offline";
        item.BackColor = state.HandRaised
            ? Color.FromArgb(255, 247, 204)
            : state.IsOnline ? Color.White : Color.FromArgb(255, 225, 225);
        item.ForeColor = state.IsOnline ? Color.FromArgb(25, 43, 57) : Color.FromArgb(150, 25, 25);
    }

    private static string BuildActivityText(StudentState state)
    {
        string hand = state.HandRaised ? "Dơ tay; " : "";
        string chat = state.UnreadChatCount > 0 ? $"Chat mới ({state.UnreadChatCount}); " : "";
        return $"{hand}{chat}{state.LastViolation}".Trim();
    }

    private void SelectStudent(string studentId)
    {
        foreach (ListViewItem item in _studentsList.Items)
        {
            item.Selected = string.Equals(item.Tag as string, studentId, StringComparison.OrdinalIgnoreCase);
        }

        UpdateTargetScopeLabel();
    }

    private string? SelectedStudentId()
    {
        return _studentsList.SelectedItems.Count == 0 ? null : _studentsList.SelectedItems[0].Tag as string;
    }

    private int CurrentBackendSessionId()
    {
        if (_backendSessionPicker.SelectedItem is ExamSessionSummaryDto selected)
        {
            _backendSessionIdInput.Value = selected.Id;
            return selected.Id;
        }

        return (int)_backendSessionIdInput.Value;
    }

    private void ApplyBackendSessionSelection(ExamSessionSummaryDto session)
    {
        _backendSessionIdInput.Value = session.Id;
        _sessionIdText.Text = session.Code;
        _sessionTokenText.Text = session.SessionToken;
        _sessionStartedAtUtc = ParseUtcOrNull(session.StartedAtUtc);
        SelectConnectionMode(session.ConnectionMode);
        _remoteJoinEnabledCheck.Checked = session.RemoteJoinEnabled;
        _publishedHostText.Text = session.PublishedHost ?? _publishedHostText.Text;
        _relayHostText.Text = session.RelayHost ?? _relayHostText.Text;
        if (session.RelayPort is > 0)
        {
            _relayPortInput.Value = Math.Clamp(session.RelayPort.Value, (int)_relayPortInput.Minimum, (int)_relayPortInput.Maximum);
        }
        _relaySecretText.Text = session.RelaySecret ?? _relaySecretText.Text;
        if (session.PublishedPort is > 0)
        {
            _portInput.Value = Math.Clamp(session.PublishedPort.Value, (int)_portInput.Minimum, (int)_portInput.Maximum);
        }

        if (!_backendSessionPicker.Items.Cast<object>().Any(x => x is ExamSessionSummaryDto existing && existing.Id == session.Id))
        {
            _backendSessionPicker.Items.Add(session);
        }

        foreach (object item in _backendSessionPicker.Items)
        {
            if (item is ExamSessionSummaryDto existing && existing.Id == session.Id)
            {
                _backendSessionPicker.SelectedItem = item;
                break;
            }
        }
    }

    private void ShowSelectedStudent()
    {
        UpdateTargetScopeLabel();
        if (SelectedStudentId() is not { } studentId)
        {
            ReplaceImage(_detailFrame, null);
            ReplaceImage(_webcamFrame, null);
            _detailStatusLabel.Text = "Màn hình: chưa chọn sinh viên";
            _webcamDetailStatus.Text = "Webcam: chưa chọn sinh viên";
            _selectedStudentInfoText.Text = "Chưa chọn sinh viên.";
            return;
        }

        if (_cards.TryGetValue(studentId, out StudentCard? card))
        {
            ReplaceImage(_detailFrame, card.CurrentImage);
            ReplaceImage(_webcamFrame, card.CurrentWebcamImage);
            _detailStatusLabel.Text = $"Màn hình: {card.DisplayName}";
            _webcamDetailStatus.Text = $"Webcam: {card.CurrentWebcamStatus}";
            if (_studentsList.SelectedItems.Count > 0)
            {
                ListViewItem item = _studentsList.SelectedItems[0];
                _selectedStudentInfoText.Text =
                    $"Sinh viên: {card.DisplayName}{Environment.NewLine}" +
                    $"Kết nối: {item.SubItems[1].Text}{Environment.NewLine}" +
                    $"User máy: {item.SubItems[2].Text}{Environment.NewLine}" +
                    $"Máy: {item.SubItems[3].Text}{Environment.NewLine}" +
                    $"Lần cuối: {item.SubItems[4].Text}{Environment.NewLine}" +
                    $"Nộp bài: {item.SubItems[5].Text}{Environment.NewLine}" +
                    $"Webcam: {item.SubItems[6].Text}{Environment.NewLine}" +
                    $"Cảnh báo: {item.SubItems[7].Text}";
            }
        }
    }

    private void ExportLogs()
    {
        string folder = Path.Combine(AppContext.BaseDirectory, "Exports");
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, $"teacher-log-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
        File.WriteAllLines(path, _eventLog.Items.Cast<string>().Select(x => $"\"{x.Replace("\"", "\"\"")}\""));
        Log($"Đã xuất nhật ký: {path}");
    }

    private void Log(string message)
    {
        _eventLog.Items.Insert(0, $"{DateTime.Now:HH:mm:ss} {message}");
        if (message.Contains("dơ tay", StringComparison.OrdinalIgnoreCase)
            || message.Contains("mất kết nối", StringComparison.OrdinalIgnoreCase)
            || message.Contains("tin nhắn", StringComparison.OrdinalIgnoreCase)
            || message.Contains("vi phạm", StringComparison.OrdinalIgnoreCase))
        {
            SystemSounds.Exclamation.Play();
        }
    }

    private async Task PublishSessionAccessAsync()
    {
        if (!_backendClient.IsAuthenticated || (int)_backendSessionIdInput.Value <= 0)
        {
            return;
        }

        try
        {
            string connectionMode = SelectedConnectionMode();
            bool relayMode = connectionMode == "relay";
            bool remoteJoinEnabled = _remoteJoinEnabledCheck.Checked || connectionMode == "remote" || relayMode;
            string? publishedHost = ResolvePublishedHost(connectionMode);
            if (string.IsNullOrWhiteSpace(publishedHost))
            {
                Log("Chế độ khác mạng yêu cầu nhập Host/IP public hoặc domain public của máy giáo viên đã mở cổng.");
                return;
            }

            if (string.Equals(connectionMode, "remote", StringComparison.OrdinalIgnoreCase) &&
                LooksLikePrivateOrLoopbackHost(publishedHost))
            {
                Log($"Host/IP từ xa hiện là {publishedHost}. Đây là IP nội bộ hoặc localhost; sinh viên khác mạng sẽ không kết nối được.");
            }

            await _backendClient.PublishSessionAccessAsync(
                (int)_backendSessionIdInput.Value,
                connectionMode,
                publishedHost,
                relayMode ? null : (int)_portInput.Value,
                remoteJoinEnabled,
                Environment.MachineName,
                relayMode,
                relayMode ? _relayHostText.Text.Trim() : null,
                relayMode ? (int)_relayPortInput.Value : null,
                relayMode ? _relaySecretText.Text.Trim() : null);
        }
        catch (Exception ex)
        {
            Log($"Công bố điểm kết nối thất bại: {ex.Message}");
        }
    }

    private void HandleConnectionModeChanged()
    {
        string mode = SelectedConnectionMode();
        bool remoteMode = mode == "remote";
        bool relayMode = mode == "relay";
        _remoteJoinEnabledCheck.Checked = remoteMode || relayMode || _remoteJoinEnabledCheck.Checked;
        if (relayMode && string.IsNullOrWhiteSpace(_publishedHostText.Text))
        {
            _publishedHostText.Text = _relayHostText.Text.Trim();
        }
        else if (!remoteMode && !relayMode && string.IsNullOrWhiteSpace(_publishedHostText.Text))
        {
            _publishedHostText.Text = TeacherDiscoveryBroadcaster.GetLikelyLocalIp();
        }
    }

    private async Task RefreshPublishedAccessAsync()
    {
        if (_server is null || !_backendClient.IsAuthenticated)
        {
            return;
        }

        await PublishSessionAccessAsync();
    }

    private string? ResolvePublishedHost(string connectionMode)
    {
        if (string.Equals(connectionMode, "relay", StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(_relayHostText.Text.Trim()) ? null : _relayHostText.Text.Trim();
        }

        string configured = _publishedHostText.Text.Trim();
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        if (string.Equals(connectionMode, "remote", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return TeacherDiscoveryBroadcaster.GetLikelyLocalIp();
    }

    private static bool LooksLikePrivateOrLoopbackHost(string host)
    {
        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!System.Net.IPAddress.TryParse(host, out System.Net.IPAddress? ip))
        {
            return false;
        }

        byte[] bytes = ip.GetAddressBytes();
        if (bytes.Length != 4)
        {
            return false;
        }

        return bytes[0] == 10
            || bytes[0] == 127
            || (bytes[0] == 192 && bytes[1] == 168)
            || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31);
    }

    private string SelectedConnectionMode()
    {
        return _connectionModePicker.SelectedIndex switch
        {
            1 => "remote",
            2 => "relay",
            _ => "lan"
        };
    }

    private void SelectConnectionMode(string? mode)
    {
        _connectionModePicker.SelectedIndex = string.Equals(mode, "relay", StringComparison.OrdinalIgnoreCase)
            ? 2
            : (string.Equals(mode, "remote", StringComparison.OrdinalIgnoreCase) ? 1 : 0);
    }

    private string ComputeExamEndAtUtc()
    {
        if (_sessionStartedAtUtc is null || _examDurationMinutesInput.Value <= 0)
        {
            return "";
        }

        return _sessionStartedAtUtc.Value.ToUniversalTime().AddMinutes((double)_examDurationMinutesInput.Value).ToString("O");
    }

    private static DateTimeOffset? ParseUtcOrNull(string? value)
    {
        return DateTimeOffset.TryParse(value, out DateTimeOffset parsed)
            ? parsed.ToUniversalTime()
            : null;
    }

    private void UpdateTargetScopeLabel()
    {
        _studentCountLabel.Text = $"Sinh viên: {_cards.Count}";
        _targetScopeLabel.Text = SelectedStudentId() is { } studentId
            ? $"Phạm vi: {studentId}"
            : "Phạm vi: toàn bộ sinh viên";
    }

    private void UpdateMosaicEmptyState()
    {
        _mosaicEmptyLabel.Visible = _cards.Count == 0;
        _studentCountLabel.Text = $"Sinh viên: {_cards.Count}";
    }

    private void AppendChatHistory(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (_chatHistoryText.TextLength > 0)
        {
            _chatHistoryText.AppendText(Environment.NewLine);
        }

        _chatHistoryText.AppendText($"{DateTime.Now:HH:mm:ss} {message}");
        _chatHistoryText.SelectionStart = _chatHistoryText.TextLength;
        _chatHistoryText.ScrollToCaret();
    }

    private void ShowSelectedStudentViewer(string streamKind)
    {
        if (SelectedStudentId() is not { } studentId || !_cards.TryGetValue(studentId, out StudentCard? card))
        {
            return;
        }

        if (streamKind == "screen")
        {
            ShowImageViewer(studentId, "screen", $"Màn hình {card.DisplayName}", card.CurrentImage);
            return;
        }

        ShowImageViewer(studentId, "webcam", $"Webcam {card.DisplayName}", card.CurrentWebcamImage);
    }

    private void ShowImageViewer(string studentId, string streamKind, string title, Image? image)
    {
        if (image is null)
        {
            return;
        }

        string key = ViewerKey(studentId, streamKind);
        if (_liveViewers.TryGetValue(key, out FloatingImageViewerForm? existing))
        {
            existing.UpdateFrame(title, image);
            if (!existing.Visible)
            {
                existing.Show(this);
            }

            existing.BringToFront();
            return;
        }

        FloatingImageViewerForm viewer = new(title, image);
        viewer.FormClosed += (_, _) => _liveViewers.Remove(key);
        _liveViewers[key] = viewer;
        viewer.Show(this);
    }

    private void UpdateLiveViewer(string studentId, string streamKind, string title, Image? image)
    {
        if (image is null)
        {
            return;
        }

        string key = ViewerKey(studentId, streamKind);
        if (_liveViewers.TryGetValue(key, out FloatingImageViewerForm? viewer))
        {
            viewer.UpdateFrame(title, image);
        }
    }

    private static string ViewerKey(string studentId, string streamKind)
    {
        return $"{streamKind}:{studentId}";
    }

    private static void ReplaceImage(PictureBox pictureBox, Image? source)
    {
        Image? old = pictureBox.Image;
        pictureBox.Image = source is null ? null : (Image)source.Clone();
        old?.Dispose();
    }

    private sealed class StudentCard : Panel
    {
        private readonly PictureBox _picture = new() { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Black };
        private readonly PictureBox _webcamThumbnail = new()
        {
            Size = new Size(78, 56),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Black,
            BorderStyle = BorderStyle.FixedSingle,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        private readonly Label _caption = new() { Dock = DockStyle.Bottom, Height = 44, TextAlign = ContentAlignment.MiddleCenter };
        private bool _handRaised;
        private bool _isOnline = true;

        public StudentCard(string studentId)
        {
            Width = 188;
            Height = 150;
            Margin = new Padding(6);
            Padding = new Padding(2);
            Cursor = Cursors.Hand;
            _caption.Text = studentId;
            Controls.Add(_picture);
            Controls.Add(_webcamThumbnail);
            Controls.Add(_caption);
            _picture.DoubleClick += (_, _) => ScreenDoubleClicked?.Invoke(this, EventArgs.Empty);
            _webcamThumbnail.DoubleClick += (_, _) => WebcamDoubleClicked?.Invoke(this, EventArgs.Empty);
            Resize += (_, _) => PositionWebcamThumbnail();
            PositionWebcamThumbnail();
        }

        public event EventHandler? ScreenDoubleClicked;
        public event EventHandler? WebcamDoubleClicked;
        public Image? CurrentImage => _picture.Image;
        public Image? CurrentWebcamImage { get; private set; }
        public string DisplayName { get; private set; } = "";
        public string CurrentWebcamStatus { get; private set; } = "Chưa kiểm tra";

        public void Update(StudentState state)
        {
            CurrentWebcamStatus = state.WebcamStatus;
            DisplayName = state.DisplayName;
            _handRaised = state.HandRaised;
            _isOnline = state.IsOnline;
            string icons = $"{(state.HandRaised ? " ✋" : "")}{(state.UnreadChatCount > 0 ? " 💬" : "")}";
            _caption.Text = $"{state.DisplayName}{icons}\n{(state.IsOnline ? "Trực tuyến" : "Mất kết nối")} | {state.SubmissionStatus}";
            BackColor = state.IsOnline ? Color.White : Color.FromArgb(255, 225, 225);
            _caption.ForeColor = state.IsOnline ? Color.FromArgb(25, 43, 57) : Color.FromArgb(150, 25, 25);

            if (state.LatestFrame is not null)
            {
                Image? old = _picture.Image;
                _picture.Image = (Image)state.LatestFrame.Clone();
                old?.Dispose();
            }

            if (state.LatestWebcamFrame is not null)
            {
                Image? oldCam = CurrentWebcamImage;
                CurrentWebcamImage = (Image)state.LatestWebcamFrame.Clone();
                oldCam?.Dispose();
                Image? oldThumb = _webcamThumbnail.Image;
                _webcamThumbnail.Image = (Image)CurrentWebcamImage.Clone();
                oldThumb?.Dispose();
                _webcamThumbnail.Visible = true;
            }

            Invalidate();
        }

        private void PositionWebcamThumbnail()
        {
            _webcamThumbnail.Location = new Point(Math.Max(4, Width - _webcamThumbnail.Width - 10), 8);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Color border = _handRaised
                ? Color.FromArgb(234, 179, 8)
                : _isOnline ? Color.FromArgb(211, 220, 228) : Color.FromArgb(214, 96, 96);
            using Pen pen = new(border, _handRaised ? 3F : 1.5F);
            e.Graphics.DrawRectangle(pen, 1, 1, Width - 3, Height - 3);
        }
    }

    private sealed class FloatingImageViewerForm : Form
    {
        public FloatingImageViewerForm(string title, Image image)
        {
            Text = title;
            Width = 960;
            Height = 640;
            StartPosition = FormStartPosition.CenterParent;
            AppIcons.SetFormIcon(this, "monitor");
            PictureBox picture = new()
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black,
                Image = (Image)image.Clone()
            };
            Button closeButton = new()
            {
                Text = "Đóng",
                Dock = DockStyle.Bottom,
                Height = 38,
                Tag = "x-circle"
            };
            closeButton.Click += (_, _) => Close();
            Controls.Add(picture);
            Controls.Add(closeButton);
            UiTheme.Apply(this);
            FormClosed += (_, _) => picture.Image?.Dispose();
            _picture = picture;
        }

        private readonly PictureBox _picture;

        public void UpdateFrame(string title, Image image)
        {
            if (IsDisposed)
            {
                return;
            }

            Text = title;
            if (InvokeRequired)
            {
                BeginInvoke(() => UpdateFrame(title, image));
                return;
            }

            Image? old = _picture.Image;
            _picture.Image = (Image)image.Clone();
            old?.Dispose();
        }
    }
}
