using System.IO.Compression;
using System.Media;
using System.Diagnostics;
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

    private readonly NumericUpDown _screenIntervalInput = new() { Minimum = 120, Maximum = 10000, Value = 500, Width = 90 };
    private readonly NumericUpDown _screenQualityInput = new() { Minimum = 20, Maximum = 85, Value = 58, Width = 80 };
    private readonly CheckBox _webcamEnabledCheck = new() { Text = "Theo dõi webcam", Checked = true, AutoSize = true };
    private readonly CheckBox _webcamSnapshotCheck = new() { Text = "Chụp ảnh khi vào", Checked = true, AutoSize = true };
    private readonly NumericUpDown _webcamIntervalInput = new() { Minimum = 0, Maximum = 10000, Value = 50, Width = 90 };
    private readonly NumericUpDown _webcamQualityInput = new() { Minimum = 25, Maximum = 90, Value = 45, Width = 80 };
    private readonly NumericUpDown _examDurationMinutesInput = new() { Minimum = 0, Maximum = 1440, Value = 0, Width = 90 };
    private readonly CheckBox _allowLateSubmissionCheck = new() { Text = "Cho phép nộp sau khi hết giờ", AutoSize = true };
    private readonly CheckBox _blockClipboardCheck = new() { Text = "Chặn copy/paste", Checked = true, AutoSize = true };
    private readonly TextBox _blockedProcessesText = new() { Text = "zalo;messenger;chatgpt;claude", Width = 320 };
    private readonly TextBox _blockedAiCliText = new() { Text = "codex;claude;openclaw;hermes;gemini;aider", Width = 320 };
    private readonly TextBox _blockedProxyToolsText = new() { Text = "clash;v2ray;xray;sing-box;proxifier;openvpn;wireguard;shadowsocks", Width = 320 };
    private readonly TextBox _blockedIdeExtensionsText = new() { Text = "codex;claude;copilot;continue;codeium;tabnine", Width = 320 };
    private readonly TextBox _blockedKeywordsText = new() { Text = "ChatGPT;Claude;Gemini;Messenger;Zalo", Width = 320 };
    private readonly TextBox _blockedWebsitesText = new() { Text = "chatgpt.com;claude.ai;gemini.google.com;grok.com;x.ai;deepseek.com;discord.com;telegram.org;web.whatsapp.com", Width = 320 };
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
    private readonly Label _webcamInventoryLabel = new()
    {
        AutoSize = false,
        Dock = DockStyle.Fill,
        Height = 24,
        TextAlign = ContentAlignment.MiddleLeft,
        Text = "Chưa có camera",
        Margin = new Padding(0, 6, 0, 0),
        AutoEllipsis = true
    };
    private readonly ComboBox _studentWebcamPicker = new() { Visible = false };
    private readonly Button _refreshStudentWebcamsButton = new() { Visible = false };
    private readonly ListBox _eventLog = new() { Dock = DockStyle.Fill };
    private readonly TextBox _chatHistoryText = new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
    private readonly TextBox _selectedStudentInfoText = new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
    private readonly ListView _historySessionsList = new() { View = View.Details, FullRowSelect = true, HideSelection = false, Dock = DockStyle.Fill };
    private readonly PictureBox _historyStartImage = new() { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.WhiteSmoke };
    private readonly PictureBox _historyEndImage = new() { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.WhiteSmoke };
    private readonly TextBox _historyDetailsText = new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
    private readonly Label _mosaicEmptyLabel = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Text = "Chưa có sinh viên kết nối", Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point) };
    private readonly Label _statusLabel = new() { AutoSize = true, Text = "Đã dừng" };
    private readonly Label _backendStatusLabel = new() { AutoSize = true, Text = "Máy chủ: chưa kết nối" };
    private readonly Label _targetScopeLabel = new() { AutoSize = true, Text = "Phạm vi: toàn bộ sinh viên" };
    private readonly Label _sessionQuickLabel = new() { AutoSize = true, Text = "Phiên: EXAM-001" };
    private readonly Label _studentCountLabel = new() { AutoSize = true, Text = "Sinh viên: 0" };
    private readonly Button _remoteControlToggleButton = new() { Text = "Điều khiển máy SV", Width = 150, Tag = "mouse-pointer" };
    private readonly Button _remoteControlToggleButton2 = new() { Text = "Điều khiển máy SV", Width = 150, Tag = "mouse-pointer" };
    private readonly Button _teacherBroadcastToggleButton = new() { Text = "Chia sẻ màn hình GV", Width = 170, Tag = "cast" };
    private readonly Button _trayModeButton = new() { Text = "Chạy ngầm: Tắt", Width = 145, Tag = "wifi" };

    private readonly Dictionary<string, StudentCard> _cards = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, StudentState> _studentStates = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SessionRosterStudentDto> _sessionRoster = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, FloatingImageViewerForm> _liveViewers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, WorkspaceChildForm> _workspacePages = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Panel> _accordionBodies = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Button> _sidebarButtons = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Control> _accordionSections = new(StringComparer.OrdinalIgnoreCase);
    private readonly BackendClient _backendClient = new();
    private readonly SessionHistoryStore _sessionHistory;
    private readonly System.Windows.Forms.Timer _teacherBroadcastTimer = new() { Interval = 1500 };
    private readonly System.Windows.Forms.Timer _sessionAccessRefreshTimer = new() { Interval = 15000 };
    private readonly TeacherDiscoveryBroadcaster _discoveryBroadcaster;
    private DateTimeOffset? _sessionStartedAtUtc;
    private ITeacherSessionTransport? _server;
    private FloatingChatForm? _chatPopup;
    private NotifyIcon? _trayIcon;
    private bool _allowExit;
    private bool _trayModeEnabled;
    private bool _remoteControlEnabled;
    private string? _remoteControlTargetId;
    private DateTime _lastRemoteMoveSentAt = DateTime.MinValue;

    public MainForm()
    {
        Text = "ExamGuard - Máy giáo viên";
        Width = 1480;
        Height = 920;
        MinimumSize = new Size(1360, 760);
        StartPosition = FormStartPosition.CenterScreen;
        IsMdiContainer = true;
        AppIcons.SetFormIcon(this, "shield");
        _sessionHistory = new SessionHistoryStore(this);

        _discoveryBroadcaster = new TeacherDiscoveryBroadcaster(message => BeginInvoke(() => Log(message)));
        _connectionModePicker.Items.AddRange(["Cùng mạng LAN", "Qua máy chủ relay"]);
        _connectionModePicker.SelectedIndex = 0;
        _sessionIdText.TextChanged += (_, _) => _sessionQuickLabel.Text = $"Phiên: {_sessionIdText.Text.Trim()}";
        _connectionModePicker.SelectedIndexChanged += (_, _) => HandleConnectionModeChanged();
        _sessionAccessRefreshTimer.Tick += async (_, _) => await RefreshPublishedAccessAsync();

        ConfigureStudentsList();
        ConfigureHistoryList();
        ConfigureContextMenus();
        _mosaicPanel.SizeChanged += (_, _) => ResizeMosaicCards();
        _teacherBroadcastTimer.Tick += async (_, _) => await BroadcastTeacherFrameAsync();
        _backendSessionPicker.SelectedIndexChanged += (_, _) => _ = RefreshSessionRosterAsync();
        _trayModeButton.Click += (_, _) => ToggleTrayMode();
        HandleConnectionModeChanged();
        UpdateTeacherBroadcastButton();
        InitializeTrayIcon();

        Controls.Add(BuildBottomDockPanel());
        Controls.Add(BuildRightDockPanel());
        Controls.Add(BuildLeftDockPanel());
        Controls.Add(BuildTopDockPanel());
        ConfigureMdiClientArea();
        UpdateMosaicEmptyState();
        UiTheme.Apply(this);
        Shown += (_, _) => ShowWorkspacePage("dashboard");
        FormClosing += async (_, e) =>
        {
            if (e.CloseReason == CloseReason.UserClosing && !_allowExit && _trayModeEnabled)
            {
                e.Cancel = true;
                HideToTray();
                return;
            }

            _teacherBroadcastTimer.Stop();
            _sessionHistory.AppendAudit("teacher_broadcast_stop", null, "Stopped teacher screen broadcast.");
            _sessionAccessRefreshTimer.Stop();
            _trayIcon?.Dispose();
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

    private void ConfigureHistoryList()
    {
        _historySessionsList.Columns.Add("Phiên", 150);
        _historySessionsList.Columns.Add("Bắt đầu", 135);
        _historySessionsList.Columns.Add("Kết thúc", 135);
        _historySessionsList.Columns.Add("SV", 55);
        _historySessionsList.Columns.Add("Vi phạm", 70);
        _historySessionsList.Columns.Add("Máy GV", 120);
        _historySessionsList.SelectedIndexChanged += (_, _) => ShowSelectedHistoryEntry();
    }

    private void InitializeTrayIcon()
    {
        ContextMenuStrip menu = new();
        menu.Items.Add("Mở lại", null, (_, _) => RestoreFromTray());
        menu.Items.Add("Tắt chạy ngầm", null, (_, _) =>
        {
            _trayModeEnabled = false;
            UpdateTrayModeUi();
            RestoreFromTray();
        });
        menu.Items.Add("Thoát hẳn", null, (_, _) =>
        {
            _allowExit = true;
            _trayIcon?.Dispose();
            Close();
        });

        _trayIcon = new NotifyIcon
        {
            Text = "ExamGuard - Máy giáo viên",
            Icon = Icon,
            Visible = false,
            ContextMenuStrip = menu
        };
        _trayIcon.DoubleClick += (_, _) => RestoreFromTray();
        UpdateTrayModeUi();
    }

    private void ToggleTrayMode()
    {
        _trayModeEnabled = !_trayModeEnabled;
        UpdateTrayModeUi();
        Log(_trayModeEnabled
            ? "Đã bật chế độ chạy ngầm."
            : "Đã tắt chế độ chạy ngầm.");

        if (_trayModeEnabled)
        {
            HideToTray();
        }
    }

    private void UpdateTrayModeUi()
    {
        _trayModeButton.Text = _trayModeEnabled ? "Chạy ngầm: Bật" : "Chạy ngầm: Tắt";
        if (_trayIcon?.ContextMenuStrip is { Items.Count: >= 2 } menu)
        {
            menu.Items[1].Visible = _trayModeEnabled;
        }
    }

    private void HideToTray()
    {
        if (_trayIcon is null || !_trayModeEnabled)
        {
            return;
        }

        ShowInTaskbar = false;
        Hide();
        _trayIcon.Visible = true;
    }

    private void RestoreFromTray()
    {
        if (_trayIcon is not null)
        {
            _trayIcon.Visible = false;
        }

        ShowInTaskbar = true;
        Show();
        WindowState = FormWindowState.Normal;
        BringToFront();
        Activate();
    }

    private void ConfigureContextMenus()
    {
        ContextMenuStrip screenMenu = BuildImageContextMenu("screen");
        ContextMenuStrip webcamMenu = BuildImageContextMenu("webcam");
        _detailFrame.ContextMenuStrip = screenMenu;
        _webcamFrame.ContextMenuStrip = webcamMenu;
        _detailFrame.TabStop = true;
        _detailFrame.MouseDown += async (_, e) => await SendRemotePointerAsync("down", e);
        _detailFrame.MouseUp += async (_, e) => await SendRemotePointerAsync("up", e);
        _detailFrame.MouseMove += async (_, e) => await SendRemoteMoveAsync(e);
        _detailFrame.MouseWheel += async (_, e) => await SendRemoteWheelAsync(e);
        _detailFrame.KeyPress += async (_, e) => await SendRemoteTextKeyAsync(e.KeyChar);
        _detailFrame.KeyDown += async (_, e) => await SendRemoteSpecialKeyAsync("down", e);
        _detailFrame.KeyUp += async (_, e) => await SendRemoteSpecialKeyAsync("up", e);
        _detailFrame.PreviewKeyDown += (_, e) => e.IsInputKey = true;
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
        Panel host = new() { Dock = DockStyle.Left, Width = 166, Padding = new Padding(12, 8, 8, 8) };
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
            _trayModeButton,
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
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 152F));
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
            RefreshHistorySessions();
            ShowWorkspacePage("history");
            _historySessionsList.Focus();
            _targetScopeLabel.Text = "Phạm vi: lịch sử phiên";
        }));
        return sidebar;
    }

    private Button CreateSidebarButton(string key, string text, string iconName, Action onClick)
    {
        Button button = IconButton(text, iconName, 136);
        button.Name = "sidebar-button";
        button.TextAlign = ContentAlignment.MiddleLeft;
        button.ImageAlign = ContentAlignment.MiddleLeft;
        button.TextImageRelation = TextImageRelation.ImageBeforeText;
        button.Height = 52;
        button.Width = 136;
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
            "history" => new WorkspaceChildForm(key, "Nhật ký", BuildHistoryWorkspacePage()),
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
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 245F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.Controls.Add(BuildTeacherWorkflowPanel(), 0, 0);
        layout.Controls.Add(WrapCard("Giám sát màn hình sinh viên", BuildMosaicArea(), "monitor"), 0, 1);
        return layout;
    }

    private Control BuildTeacherWorkflowPanel()
    {
        GroupBox box = CreateGroupBox("Bắt đầu buổi thi");
        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(2)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 390F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        TableLayoutPanel sessionGrid = CreateFieldGrid(105);
        sessionGrid.Dock = DockStyle.Fill;
        sessionGrid.Controls.Add(LabelFor("Mã phiên"), 0, 0);
        sessionGrid.Controls.Add(_sessionIdText, 1, 0);
        sessionGrid.Controls.Add(LabelFor("Mã bảo vệ"), 0, 1);
        sessionGrid.Controls.Add(_sessionTokenText, 1, 1);
        sessionGrid.Controls.Add(LabelFor("Cổng"), 0, 2);
        sessionGrid.Controls.Add(_portInput, 1, 2);
        sessionGrid.Controls.Add(LabelFor("Kết nối"), 0, 3);
        sessionGrid.Controls.Add(_connectionModePicker, 1, 3);

        TableLayoutPanel actions = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 2,
            Padding = new Padding(6, 6, 0, 0),
            Margin = new Padding(4, 0, 0, 0)
        };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
        actions.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        actions.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        Button loginButton = LargeActionButton("1. Đăng nhập máy chủ", "log-in", 0);
        loginButton.Click += async (_, _) => await LoginBackendAsync();
        Button startButton = LargeActionButton("2. Bắt đầu phiên", "play-circle", 0);
        startButton.Click += async (_, _) => await StartServerAsync();
        Button policyButton = LargeActionButton("3. Gửi quy định", "save", 0);
        policyButton.Click += async (_, _) => await SendPolicyAsync();
        Button examButton = LargeActionButton("4. Phát đề thi", "folder", 0);
        examButton.Click += (_, _) => ShowWorkspacePage("distribution");
        Button stopButton = LargeActionButton("Dừng phiên", "stop-circle", 0);
        stopButton.Click += async (_, _) => await StopServerAsync();

        actions.Controls.Add(loginButton, 0, 0);
        actions.Controls.Add(startButton, 1, 0);
        actions.Controls.Add(policyButton, 2, 0);
        actions.Controls.Add(examButton, 0, 1);
        actions.Controls.Add(stopButton, 1, 1);
        actions.SetColumnSpan(stopButton, 2);
        layout.Controls.Add(sessionGrid, 0, 0);
        layout.Controls.Add(actions, 1, 0);
        box.Controls.Add(layout);
        return box;
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
        stack.Controls.Add(CreateAccordionSection("control", "Điều khiển và cảnh báo", "bell", BuildControlGroup(), "Tin nhắn, giơ tay, khóa, lệnh"), 0, 3);
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
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 38F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 42F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 14F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 8F));

        Panel screenPanel = new() { Dock = DockStyle.Fill };
        screenPanel.Controls.Add(_detailFrame);
        screenPanel.Controls.Add(_detailStatusLabel);

        TableLayoutPanel webcamPanel = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        webcamPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        webcamPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
        webcamPanel.Controls.Add(_webcamFrame, 0, 0);
        webcamPanel.Controls.Add(BuildWebcamStatusBar(), 0, 1);

        layout.Controls.Add(WrapCard("Màn hình đang chọn", screenPanel, "monitor"), 0, 0);
        layout.Controls.Add(WrapCard("Webcam đang chọn", webcamPanel, "video"), 0, 1);
        Button chatPopupButton = IconButton("Mở chat", "message-circle", 130);
        chatPopupButton.Name = "secondary-button";
        chatPopupButton.Dock = DockStyle.Fill;
        chatPopupButton.Click += (_, _) => ShowChatPopup();

        layout.Controls.Add(WrapCard("Thông tin sinh viên", _selectedStudentInfoText, "user"), 0, 2);
        layout.Controls.Add(WrapCard("Trao đổi", chatPopupButton, "message-circle"), 0, 3);
        return layout;
    }

    private Control BuildWebcamStatusBar()
    {
        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(0, 4, 0, 0),
            Margin = new Padding(0)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        layout.Controls.Add(_webcamInventoryLabel, 0, 0);
        layout.Controls.Add(_webcamDetailStatus, 0, 1);
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

    private Control BuildHistoryWorkspacePage()
    {
        SplitContainer root = new()
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 510
        };

        TableLayoutPanel left = new()
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1
        };
        left.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        Button refreshButton = IconButton("Tải lại lịch sử", "refresh-cw", 130);
        refreshButton.Click += (_, _) => RefreshHistorySessions();
        Button openFolderButton = IconButton("Mở thư mục", "folder", 110);
        openFolderButton.Click += (_, _) => OpenSelectedHistoryFolder();
        Button exportCsvButton = IconButton("Xuất CSV", "file-text", 95);
        exportCsvButton.Click += (_, _) => ExportHistoryIndex("csv");
        Button exportJsonButton = IconButton("Xuất JSON", "file-text", 100);
        exportJsonButton.Click += (_, _) => ExportHistoryIndex("json");
        left.Controls.Add(CreateButtonRow(refreshButton, openFolderButton, exportCsvButton, exportJsonButton), 0, 0);
        left.Controls.Add(WrapCard("Phiên đã lưu cục bộ", _historySessionsList, "file-text"), 0, 1);

        TableLayoutPanel right = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(8, 0, 0, 0)
        };
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 32F));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 32F));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 36F));
        right.Controls.Add(WrapCard("Ảnh lúc bắt đầu", _historyStartImage, "monitor"), 0, 0);
        right.Controls.Add(WrapCard("Ảnh lúc kết thúc", _historyEndImage, "monitor"), 0, 1);
        right.Controls.Add(WrapCard("Chi tiết phiên", _historyDetailsText, "file-text"), 0, 2);

        root.Panel1.Controls.Add(left);
        root.Panel2.Controls.Add(right);
        RefreshHistorySessions();
        return root;
    }

    private GroupBox BuildControlGroup()
    {
        GroupBox controlBox = CreateGroupBox("Điều khiển và cảnh báo");
        TableLayoutPanel controlGrid = CreateFieldGrid(155);
        Button chatButton = IconButton("Gửi tin nhắn", "send", 125);
        chatButton.Click += async (_, _) => await SendChatAsync();
        Button chatAllButton = IconButton("Chat tất cả", "message-circle", 115);
        chatAllButton.Click += async (_, _) => await SendChatAllAsync();
        Button clearHandButton = IconButton("Xóa giơ tay", "x-circle", 115);
        clearHandButton.Click += async (_, _) => await ClearHandRaiseAsync();
        Button attentionButton = IconButton("Gọi chú ý", "bell", 110);
        attentionButton.Click += async (_, _) => await SendAttentionAsync();
        Button lockButton = IconButton("Khóa màn hình", "lock", 125);
        lockButton.Click += async (_, _) => await SendLockAsync();
        Button unlockButton = IconButton("Mở khóa", "unlock", 95);
        unlockButton.Click += async (_, _) => await SendUnlockAsync();
        Button typeTextButton = IconButton("Nhập văn bản", "type", 120);
        typeTextButton.Click += async (_, _) => await SendRemoteTextInputAsync();
        _remoteControlToggleButton.Click += async (_, _) => await ToggleRemoteControlAsync();
        Button clipboardButton = IconButton("Đặt clipboard", "clipboard", 120);
        clipboardButton.Click += async (_, _) => await SendClipboardSetAsync();
        Button commandButton = IconButton("Chạy lệnh", "terminal", 105);
        commandButton.Click += async (_, _) => await SendCommandAsync();

        controlGrid.Controls.Add(LabelFor("Tin nhắn / ghi chú"), 0, 0);
        controlGrid.Controls.Add(_teacherMessageText, 1, 0);
        FlowLayoutPanel row1 = CreateButtonRow(chatButton, chatAllButton, clearHandButton, attentionButton, lockButton, unlockButton);
        FlowLayoutPanel row2 = CreateButtonRow(_remoteControlToggleButton, typeTextButton, clipboardButton);
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
        _teacherBroadcastToggleButton.Click += (_, _) => ToggleTeacherBroadcast();
        distributionGrid.Controls.Add(LabelFor("Tiến trình"), 0, 0);
        distributionGrid.Controls.Add(_distributionProgress, 1, 0);
        FlowLayoutPanel distributionActions = CreateButtonRow(fileButton, folderButton, _teacherBroadcastToggleButton);
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
        FlowLayoutPanel actions = CreateButtonRow(startButton, stopButton);
        grid.Controls.Add(actions, 0, 4);
        grid.SetColumnSpan(actions, 2);
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
        grid.Controls.Add(LabelFor("AI CLI bị chặn"), 0, 11);
        grid.Controls.Add(_blockedAiCliText, 1, 11);
        grid.Controls.Add(LabelFor("Proxy/VPN bị chặn"), 0, 12);
        grid.Controls.Add(_blockedProxyToolsText, 1, 12);
        grid.Controls.Add(LabelFor("Extension IDE bị chặn"), 0, 13);
        grid.Controls.Add(_blockedIdeExtensionsText, 1, 13);
        grid.Controls.Add(LabelFor("Website bị chặn"), 0, 14);
        grid.Controls.Add(_blockedWebsitesText, 1, 14);
        FlowLayoutPanel actions = CreateButtonRow(sendPolicyButton, exportLogsButton);
        grid.Controls.Add(actions, 0, 15);
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
        Button clearHandButton = IconButton("Xóa giơ tay", "x-circle", 115);
        clearHandButton.Click += async (_, _) => await ClearHandRaiseAsync();
        Button attentionButton = IconButton("Gọi chú ý", "bell", 110);
        attentionButton.Click += async (_, _) => await SendAttentionAsync();
        Button lockButton = IconButton("Khóa màn hình", "lock", 125);
        lockButton.Click += async (_, _) => await SendLockAsync();
        Button unlockButton = IconButton("Mở khóa", "unlock", 95);
        unlockButton.Click += async (_, _) => await SendUnlockAsync();
        Button typeTextButton = IconButton("Nhập văn bản", "type", 120);
        typeTextButton.Click += async (_, _) => await SendRemoteTextInputAsync();
        _remoteControlToggleButton2.Click += async (_, _) => await ToggleRemoteControlAsync();
        Button clipboardButton = IconButton("Đặt clipboard", "clipboard", 120);
        clipboardButton.Click += async (_, _) => await SendClipboardSetAsync();
        Button commandButton = IconButton("Chạy lệnh", "terminal", 105);
        commandButton.Click += async (_, _) => await SendCommandAsync();

        controlGrid.Controls.Add(LabelFor("Tin nhắn / ghi chú"), 0, 0);
        controlGrid.Controls.Add(_teacherMessageText, 1, 0);
        FlowLayoutPanel row1 = CreateButtonRow(chatButton, chatAllButton, clearHandButton, attentionButton, lockButton, unlockButton);
        FlowLayoutPanel row2 = CreateButtonRow(_remoteControlToggleButton2, typeTextButton, clipboardButton);
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

    private static Button LargeActionButton(string text, string iconName, int width)
    {
        return new Button
        {
            Name = "large-action-button",
            Text = text,
            Width = width,
            Height = 56,
            Dock = DockStyle.Fill,
            Margin = new Padding(6),
            Tag = iconName,
            TextAlign = ContentAlignment.MiddleCenter,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextImageRelation = TextImageRelation.ImageBeforeText
        };
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
        label.TextChanged += (_, _) => UiTheme.StyleStatusChip(panel);
        UiTheme.StyleStatusChip(panel);
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
        await EnsureBackendSessionRunningAsync(connectionMode);
        await RefreshSessionRosterAsync();
        if (connectionMode == "relay")
        {
            TeacherRelayClient relay = new(
                BuildPolicy,
                state => BeginInvoke(() => UpsertStudent(state)),
                message => BeginInvoke(() => Log(message)),
                (state, process, title) =>
                {
                    _sessionHistory.AppendViolation(state, "process_violation", process, title, "reported");
                    _ = PostEventAsync(state, "process_violation", new { processName = process, windowTitle = title });
                },
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
            (state, process, title) =>
            {
                _sessionHistory.AppendViolation(state, "process_violation", process, title, "reported");
                _ = PostEventAsync(state, "process_violation", new { processName = process, windowTitle = title });
            },
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
        _sessionHistory.Start(_sessionIdText.Text.Trim(), _sessionIdText.Text.Trim());
        _statusLabel.Text = "Đang hoạt động";
        UpdateTargetScopeLabel();
        _sessionAccessRefreshTimer.Start();
        await PublishSessionAccessAsync();
    }

    private async Task EnsureBackendSessionRunningAsync(string desiredConnectionMode)
    {
        if (!_backendClient.IsAuthenticated || (int)_backendSessionIdInput.Value <= 0)
        {
            return;
        }

        try
        {
            ExamSessionSummaryDto? session = await _backendClient.StartExamSessionAsync(CurrentBackendSessionId());
            if (session is null)
            {
                return;
            }

            ApplyBackendSessionSelection(session);
            SelectConnectionMode(desiredConnectionMode);
            _sessionStartedAtUtc = ParseUtcOrNull(session.StartedAtUtc);
            Log($"Đã mở phiên {session.Code} trên máy chủ.");
        }
        catch (Exception ex)
        {
            Log($"Không mở được phiên trên máy chủ: {ex.Message}");
        }
    }

    private async Task StopServerAsync()
    {
        if (_server is null)
        {
            return;
        }

        _sessionHistory.Finish(_sessionIdText.Text.Trim(), _cards.Count);
        await _server.StopAsync();
        _server = null;
        _cards.Clear();
        _studentStates.Clear();
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
        UpdateTeacherBroadcastButton();
        _sessionAccessRefreshTimer.Stop();
        _discoveryBroadcaster.Stop();
        _sessionStartedAtUtc = null;
        RefreshHistorySessions();
        _sessionHistory.Reset();
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
        await SendChatMessageAsync(SelectedTransportId(), SelectedStudentCode(), _teacherMessageText.Text.Trim());
    }

    private async Task SendChatAllAsync()
    {
        await SendChatMessageAsync(null, null, _teacherMessageText.Text.Trim());
    }

    private async Task SendChatMessageAsync(string? targetTransportId, string? targetStudentCode, string message)
    {
        if (_server is null)
        {
            Log("Hãy bắt đầu phiên trước khi gửi tin nhắn.");
            return;
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            Log("Tin nhắn đang trống.");
            return;
        }

        await _server.SendChatAsync(targetTransportId, message);
        await _backendClient.PostChatMessageAsync(CurrentBackendSessionId(), "teacher", "teacher", targetStudentCode, message, targetTransportId is null ? "all" : "one");
        AppendChatHistory($"Giáo viên -> {(targetStudentCode ?? "Tất cả")}: {message}");
    }

    private async Task ClearHandRaiseAsync()
    {
        if (_server is null)
        {
            return;
        }

        await _server.SendHandRaiseClearAsync(SelectedTransportId());
        if (TryGetSelectedCard(out StudentCard? card))
        {
            AppendChatHistory($"Đã xóa giơ tay của {card!.DisplayName}.");
        }
    }

    private async Task SendAttentionAsync()
    {
        if (_server is null)
        {
            Log("Hãy bắt đầu phiên trước khi gửi cảnh báo.");
            return;
        }

        await _server.SendAttentionAsync(SelectedTransportId(), _teacherMessageText.Text.Trim());
    }

    private async Task SendLockAsync()
    {
        if (_server is null)
        {
            Log("Hãy bắt đầu phiên trước khi khóa màn hình.");
            return;
        }

        await _server.SendLockAsync(SelectedTransportId(), _teacherMessageText.Text.Trim());
    }

    private async Task SendUnlockAsync()
    {
        if (_server is null)
        {
            Log("Hãy bắt đầu phiên trước khi mở khóa.");
            return;
        }

        await _server.SendUnlockAsync(SelectedTransportId());
    }

    private async Task SendCommandAsync()
    {
        if (_server is null)
        {
            Log("Hãy bắt đầu phiên trước khi chạy lệnh.");
            return;
        }

        await _server.SendExecuteCommandAsync(SelectedTransportId(), _teacherCommandText.Text.Trim());
    }

    private async Task SendRemotePointerAsync(string action, MouseEventArgs e, int wheelDelta = 0)
    {
        if (!_remoteControlEnabled || _server is null || SelectedTransportId() is not { } studentId || _detailFrame.Image is null)
        {
            return;
        }

        if (!TryResolveZoomRelativePoint(_detailFrame, e.Location, out double relativeX, out double relativeY))
        {
            return;
        }

        string button = e.Button == MouseButtons.Right ? "right" : "left";
        await _server.SendRemotePointerAsync(studentId, action, relativeX, relativeY, button, wheelDelta);
    }

    private async Task SendRemoteMoveAsync(MouseEventArgs e)
    {
        if (!_remoteControlEnabled || _server is null || _detailFrame.Image is null || e.Button == MouseButtons.None)
        {
            return;
        }

        if ((DateTime.UtcNow - _lastRemoteMoveSentAt).TotalMilliseconds < 30)
        {
            return;
        }

        _lastRemoteMoveSentAt = DateTime.UtcNow;
        await SendRemotePointerAsync("move", e);
    }

    private async Task SendRemoteWheelAsync(MouseEventArgs e)
    {
        await SendRemotePointerAsync("wheel", e, e.Delta);
    }

    private async Task SendRemoteSpecialKeyAsync(string action, KeyEventArgs e)
    {
        if (!_remoteControlEnabled || _server is null || SelectedTransportId() is not { } studentId)
        {
            return;
        }

        string protocolKey = ToProtocolKey(e.KeyCode);
        if (string.IsNullOrWhiteSpace(protocolKey) && (e.Control || e.Alt) && e.KeyCode is >= Keys.A and <= Keys.Z or >= Keys.D0 and <= Keys.D9)
        {
            protocolKey = e.KeyCode.ToString().Replace("D", "", StringComparison.Ordinal).ToUpperInvariant();
        }

        if (string.IsNullOrWhiteSpace(protocolKey))
        {
            return;
        }

        await _server.SendRemoteKeyAsync(studentId, action, protocolKey, "", BuildModifiers(e));
        e.SuppressKeyPress = true;
        e.Handled = true;
    }

    private async Task SendRemoteTextKeyAsync(char value)
    {
        if (!_remoteControlEnabled || _server is null || SelectedTransportId() is not { } studentId || char.IsControl(value))
        {
            return;
        }

        await _server.SendRemoteKeyAsync(studentId, "text", "", value.ToString(), "");
    }

    private async Task ToggleRemoteControlAsync()
    {
        if (_server is null || SelectedTransportId() is not { } studentId || !TryGetSelectedCard(out StudentCard? card) || card?.CurrentImage is null)
        {
            Log("Hãy chọn một sinh viên online đã có màn hình trước khi điều khiển.");
            return;
        }

        if (_remoteControlEnabled)
        {
            await StopRemoteControlAsync();
            return;
        }

        _remoteControlEnabled = true;
        _remoteControlTargetId = studentId;
        await _server.SendRemoteControlStartAsync(studentId);
        _detailFrame.Focus();
        UpdateRemoteControlButtons();
        _sessionHistory.AppendAudit("remote_control_start", studentId, card.DisplayName);
        Log($"Bắt đầu điều khiển {card.DisplayName}.");
    }

    private async Task StopRemoteControlAsync()
    {
        if (_server is not null)
        {
            await _server.SendRemoteControlStopAsync(_remoteControlTargetId);
        }

        _remoteControlEnabled = false;
        _remoteControlTargetId = null;
        UpdateRemoteControlButtons();
        _sessionHistory.AppendAudit("remote_control_stop", null, "Teacher stopped remote control.");
        Log("Đã dừng điều khiển từ xa.");
    }

    private void UpdateRemoteControlButtons()
    {
        string text = _remoteControlEnabled
            ? $"Dừng điều khiển {SelectedStudentDisplayName() ?? _remoteControlTargetId}"
            : "Điều khiển máy SV";
        _remoteControlToggleButton.Text = text;
        _remoteControlToggleButton2.Text = text;
    }

    private static bool TryResolveZoomRelativePoint(PictureBox box, Point point, out double relativeX, out double relativeY)
    {
        relativeX = 0;
        relativeY = 0;
        if (box.Image is null || box.Width <= 0 || box.Height <= 0)
        {
            return false;
        }

        double imageRatio = box.Image.Width / (double)box.Image.Height;
        double boxRatio = box.Width / (double)box.Height;
        int drawWidth;
        int drawHeight;
        int offsetX;
        int offsetY;
        if (boxRatio > imageRatio)
        {
            drawHeight = box.Height;
            drawWidth = (int)Math.Round(drawHeight * imageRatio);
            offsetX = (box.Width - drawWidth) / 2;
            offsetY = 0;
        }
        else
        {
            drawWidth = box.Width;
            drawHeight = (int)Math.Round(drawWidth / imageRatio);
            offsetX = 0;
            offsetY = (box.Height - drawHeight) / 2;
        }

        if (point.X < offsetX || point.X > offsetX + drawWidth || point.Y < offsetY || point.Y > offsetY + drawHeight)
        {
            return false;
        }

        relativeX = Math.Clamp((point.X - offsetX) / (double)Math.Max(1, drawWidth), 0, 1);
        relativeY = Math.Clamp((point.Y - offsetY) / (double)Math.Max(1, drawHeight), 0, 1);
        return true;
    }

    private async Task SendRemoteTextInputAsync()
    {
        if (_server is null)
        {
            Log("Hãy bắt đầu phiên trước khi nhập văn bản từ xa.");
            return;
        }

        await _server.SendRemoteTextInputAsync(SelectedTransportId(), _teacherMessageText.Text);
    }

    private async Task SendClipboardSetAsync()
    {
        if (_server is null)
        {
            Log("Hãy bắt đầu phiên trước khi đặt clipboard.");
            return;
        }

        await _server.SendClipboardSetAsync(SelectedTransportId(), _teacherMessageText.Text);
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
            SelectedTransportId(),
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
            SelectedTransportId(),
            zipPath,
            new Progress<int>(value => _distributionProgress.Value = Math.Clamp(value, 0, 100)));
        _distributionProgress.Value = 100;
        Log($"Đã phát thư mục dưới dạng tệp nén: {Path.GetFileName(zipPath)}");
    }

    private async void ToggleTeacherBroadcast()
    {
        if (_server is null)
        {
            Log("Hãy bắt đầu phiên trước khi phát màn hình.");
            return;
        }

        if (_teacherBroadcastTimer.Enabled)
        {
            _teacherBroadcastTimer.Stop();
            if (_server is not null)
            {
                await _server.SendTeacherBroadcastStopAsync();
            }
            _sessionHistory.AppendAudit("teacher_broadcast_stop", null, "Stopped teacher screen broadcast.");
            Log("Đã dừng phát màn hình giáo viên.");
        }
        else
        {
            _teacherBroadcastTimer.Start();
            _sessionHistory.AppendAudit("teacher_broadcast_start", null, "Started teacher screen broadcast.");
            Log("Đang phát màn hình giáo viên.");
        }

        UpdateTeacherBroadcastButton();
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
            await RefreshSessionRosterAsync();
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
            await RefreshSessionRosterAsync();
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
            await RefreshSessionRosterAsync();
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
            await RefreshSessionRosterAsync();
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
        BeginInvoke(() => AppendChatHistory($"{state.DisplayName} đã giơ tay{(string.IsNullOrWhiteSpace(message) ? "" : $": {message}")}"));
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
            BlockedAiCliTools = SplitRules(_blockedAiCliText.Text),
            BlockedProxyTools = SplitRules(_blockedProxyToolsText.Text),
            BlockedIdeExtensions = SplitRules(_blockedIdeExtensionsText.Text),
            BlockedWebsiteHosts = SplitRules(_blockedWebsitesText.Text),
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
        _blockedAiCliText.Text = string.Join(';', policy.BlockedAiCliTools);
        _blockedProxyToolsText.Text = string.Join(';', policy.BlockedProxyTools);
        _blockedIdeExtensionsText.Text = string.Join(';', policy.BlockedIdeExtensions);
        _blockedWebsitesText.Text = string.Join(';', policy.BlockedWebsiteHosts);
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
        ApplyRosterToState(state);
        string studentKey = NormalizeStudentKey(state.StudentCodeOrId);
        string transportId = state.TransportId;
        _studentStates[studentKey] = state;
        if (!_cards.TryGetValue(studentKey, out StudentCard? card))
        {
            card = new StudentCard(studentKey);
            card.Click += (_, _) => SelectStudent(studentKey);
            card.ScreenDoubleClicked += (_, _) => ShowImageViewer(studentKey, "screen", $"Màn hình {card.DisplayName}", card.CurrentImage);
            card.WebcamDoubleClicked += (_, _) => ShowImageViewer(studentKey, "webcam", $"Webcam {card.DisplayName}", card.CurrentWebcamImage);
            _cards[studentKey] = card;
            _mosaicPanel.Controls.Add(card);
            ResizeMosaicCards();
        }

        card.Update(state);
        UpdateMosaicEmptyState();
        UpdateLiveViewer(studentKey, "screen", $"Màn hình {state.DisplayName}", card.CurrentImage);
        UpdateLiveViewer(studentKey, "webcam", $"Webcam {state.DisplayName}", card.CurrentWebcamImage);
        UpsertListItem(studentKey, state);
        if (string.Equals(SelectedStudentId(), transportId, StringComparison.Ordinal))
        {
            ShowSelectedStudent();
        }
    }

    private void UpsertListItem(string studentKey, StudentState state)
    {
        ListViewItem? item = _studentsList.Items.Cast<ListViewItem>().FirstOrDefault(x => string.Equals(x.Name, studentKey, StringComparison.OrdinalIgnoreCase));
        if (item is null)
        {
            item = new ListViewItem(state.DisplayName)
            {
                Name = studentKey,
                Tag = state.TransportId
            };
            for (int i = 0; i < 7; i++)
            {
                item.SubItems.Add("");
            }

            _studentsList.Items.Add(item);
        }

        item.Text = state.DisplayName;
        item.Tag = state.TransportId;
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
        string hand = state.HandRaised ? "Giơ tay; " : "";
        string chat = state.UnreadChatCount > 0 ? $"Chat mới ({state.UnreadChatCount}); " : "";
        return $"{hand}{chat}{state.LastViolation}".Trim();
    }

    private void SelectStudent(string studentKey)
    {
        if (_remoteControlEnabled &&
            _remoteControlTargetId is not null &&
            !string.Equals(_remoteControlTargetId, SelectedTransportIdByKey(studentKey), StringComparison.OrdinalIgnoreCase))
        {
            _ = StopRemoteControlAsync();
        }

        foreach (ListViewItem item in _studentsList.Items)
        {
            item.Selected = string.Equals(item.Name, studentKey, StringComparison.OrdinalIgnoreCase);
        }

        UpdateTargetScopeLabel();
    }

    private string? SelectedStudentId()
    {
        return _studentsList.SelectedItems.Count == 0 ? null : _studentsList.SelectedItems[0].Tag as string;
    }

    private string? SelectedTransportId()
    {
        return SelectedStudentId();
    }

    private string? SelectedStudentKey()
    {
        return _studentsList.SelectedItems.Count == 0 ? null : _studentsList.SelectedItems[0].Name;
    }

    private string? SelectedStudentCode()
    {
        return SelectedStudentKey() is { } studentKey && _studentStates.TryGetValue(studentKey, out StudentState? state)
            ? state.StudentCodeOrId
            : null;
    }

    private string? SelectedStudentDisplayName()
    {
        return SelectedStudentKey() is { } studentKey && _cards.TryGetValue(studentKey, out StudentCard? card)
            ? card.DisplayName
            : null;
    }

    private string? SelectedTransportIdByKey(string studentKey)
    {
        return _studentStates.TryGetValue(studentKey, out StudentState? state) ? state.TransportId : null;
    }

    private bool TryGetSelectedCard(out StudentCard? card)
    {
        card = null;
        return SelectedStudentKey() is { } studentKey && _cards.TryGetValue(studentKey, out card);
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

    private async Task RefreshSessionRosterAsync()
    {
        if (!_backendClient.IsAuthenticated || CurrentBackendSessionId() <= 0)
        {
            _sessionRoster.Clear();
            return;
        }

        try
        {
            List<SessionRosterStudentDto> roster = await _backendClient.GetSessionRosterAsync(CurrentBackendSessionId());
            _sessionRoster.Clear();
            foreach (SessionRosterStudentDto student in roster)
            {
                string key = NormalizeStudentKey(student.StudentCode);
                if (!string.IsNullOrWhiteSpace(key))
                {
                    _sessionRoster[key] = student;
                }
            }

            foreach (StudentState state in _studentStates.Values.ToList())
            {
                ApplyRosterToState(state);
            }

            foreach (StudentState state in _studentStates.Values.ToList())
            {
                UpsertStudent(state);
            }

            if (roster.Count > 0)
            {
                Log($"Đã tải roster {roster.Count} sinh viên cho phiên hiện tại.");
            }
        }
        catch (Exception ex)
        {
            Log($"Tải roster thất bại: {ex.Message}");
        }
    }

    private void ApplyRosterToState(StudentState state)
    {
        string key = NormalizeStudentKey(state.StudentCodeOrId);
        if (_sessionRoster.TryGetValue(key, out SessionRosterStudentDto? roster) && !string.IsNullOrWhiteSpace(roster.FullName))
        {
            state.StudentName = roster.FullName;
            state.StudentCode = roster.StudentCode;
        }
    }

    private async Task RefreshSelectedStudentWebcamsAsync()
    {
        await Task.CompletedTask;
    }

    private async Task SelectStudentWebcamAsync()
    {
        await Task.CompletedTask;
    }

    private void PopulateWebcamPicker(StudentState? state)
    {
        try
        {
            if (state is null)
            {
                _webcamInventoryLabel.Text = "Chưa có camera";
                return;
            }

            List<WebcamPickerItem> desiredItems = state.WebcamDevices
                .Select(device => new WebcamPickerItem(device.CameraId, $"{device.DisplayName} ({device.Status})"))
                .ToList();
            bool sameItems = _studentWebcamPicker.Items.Count == desiredItems.Count &&
                _studentWebcamPicker.Items.Cast<WebcamPickerItem>().Zip(desiredItems).All(pair =>
                    string.Equals(pair.First.CameraId, pair.Second.CameraId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(pair.First.Text, pair.Second.Text, StringComparison.Ordinal));
            string? currentSelection = (_studentWebcamPicker.SelectedItem as WebcamPickerItem)?.CameraId;
            if (!sameItems || !string.Equals(currentSelection, state.SelectedCameraId, StringComparison.OrdinalIgnoreCase))
            {
                _studentWebcamPicker.Items.Clear();
                foreach (WebcamPickerItem desiredItem in desiredItems)
                {
                    _studentWebcamPicker.Items.Add(desiredItem);
                }

                if (_studentWebcamPicker.Items.Count > 0)
                {
                    int selectedIndex = desiredItems.FindIndex(x => string.Equals(x.CameraId, state.SelectedCameraId, StringComparison.OrdinalIgnoreCase));
                    _studentWebcamPicker.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
                }
            }

            _studentWebcamPicker.Enabled = _studentWebcamPicker.Items.Count > 0;
            _refreshStudentWebcamsButton.Enabled = state.IsOnline;
            _webcamInventoryLabel.Text = state.WebcamDevices.Count == 0
                ? "Chưa nhận được danh sách camera"
                : $"Có {state.WebcamDevices.Count} camera, chọn để xem webcam chi tiết";
        }
        finally
        {
        }
    }

    private void ShowSelectedStudent()
    {
        UpdateTargetScopeLabel();
        if (SelectedStudentKey() is not { } studentKey)
        {
            ReplaceImage(_detailFrame, null);
            ReplaceImage(_webcamFrame, null);
            _detailStatusLabel.Text = "Màn hình: chưa chọn sinh viên";
            _webcamDetailStatus.Text = "Webcam: chưa chọn sinh viên";
            _selectedStudentInfoText.Text = "Chưa chọn sinh viên.";
            PopulateWebcamPicker(null);
            return;
        }

        if (_cards.TryGetValue(studentKey, out StudentCard? card))
        {
            ReplaceImage(_detailFrame, card.CurrentImage);
            ReplaceImage(_webcamFrame, card.CurrentWebcamImage);
            _detailStatusLabel.Text = $"Màn hình: {card.DisplayName}";
            _webcamDetailStatus.Text = $"Webcam: {card.CurrentWebcamStatus}";
            StudentState? state = _studentStates.GetValueOrDefault(studentKey);
            PopulateWebcamPicker(state);
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

            if (state is { IsOnline: true } && state.WebcamDevices.Count == 0)
            {
                _ = RefreshSelectedStudentWebcamsAsync();
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
        if (message.Contains("giơ tay", StringComparison.OrdinalIgnoreCase)
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
            bool remoteJoinEnabled = relayMode;
            string? publishedHost = ResolvePublishedHost(connectionMode);
            if (string.IsNullOrWhiteSpace(publishedHost))
            {
                Log("Chưa xác định được điểm kết nối. Hãy chọn cùng mạng LAN hoặc qua máy chủ relay.");
                return;
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
        bool relayMode = mode == "relay";
        if (relayMode && string.IsNullOrWhiteSpace(_publishedHostText.Text))
        {
            _publishedHostText.Text = _relayHostText.Text.Trim();
        }
        else if (!relayMode && string.IsNullOrWhiteSpace(_publishedHostText.Text))
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

        return TeacherDiscoveryBroadcaster.GetLikelyLocalIp();
    }

    private string SelectedConnectionMode()
    {
        return _connectionModePicker.SelectedIndex switch
        {
            1 => "relay",
            _ => "lan"
        };
    }

    private void SelectConnectionMode(string? mode)
    {
        _connectionModePicker.SelectedIndex = string.Equals(mode, "relay", StringComparison.OrdinalIgnoreCase)
            || string.Equals(mode, "remote", StringComparison.OrdinalIgnoreCase)
                ? 1
                : 0;
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
        _targetScopeLabel.Text = SelectedStudentDisplayName() is { } displayName
            ? $"Phạm vi: {displayName}"
            : "Phạm vi: toàn bộ sinh viên";
    }

    private void UpdateMosaicEmptyState()
    {
        _mosaicEmptyLabel.Visible = _cards.Count == 0;
        _studentCountLabel.Text = $"Sinh viên: {_cards.Count}";
    }

    private void UpdateTeacherBroadcastButton()
    {
        _teacherBroadcastToggleButton.Text = _teacherBroadcastTimer.Enabled
            ? "Dừng chia sẻ màn hình GV"
            : "Chia sẻ màn hình GV";
    }

    private void ResizeMosaicCards()
    {
        if (_mosaicPanel.ClientSize.Width <= 0 || _cards.Count == 0)
        {
            return;
        }

        int availableWidth = Math.Max(220, _mosaicPanel.ClientSize.Width - _mosaicPanel.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth - 8);
        int columns = availableWidth >= 900 ? 5 : Math.Clamp(availableWidth / 190, 1, 4);
        int margin = 6;
        int cardWidth = Math.Max(160, (availableWidth / columns) - (margin * 2) - 2);
        int imageHeight = (int)Math.Round(cardWidth * 9 / 16.0);
        int cardHeight = imageHeight + 48;

        foreach (StudentCard card in _cards.Values)
        {
            card.Margin = new Padding(margin);
            card.Width = cardWidth;
            card.Height = cardHeight;
        }
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

    private void ShowChatPopup()
    {
        if (_chatPopup is null || _chatPopup.IsDisposed)
        {
            _chatPopup = new FloatingChatForm(
                _chatHistoryText,
                SelectedTransportId,
                async (transportId, message) => await SendChatMessageAsync(transportId, SelectedStudentCode(), message));
        }

        if (!_chatPopup.Visible)
        {
            _chatPopup.Show(this);
        }

        _chatPopup.BringToFront();
        _chatPopup.Activate();
    }

    private void RefreshHistorySessions()
    {
        IReadOnlyList<SessionHistoryEntry> entries = SessionHistoryStore.LoadHistoryEntries();
        _historySessionsList.BeginUpdate();
        _historySessionsList.Items.Clear();
        foreach (SessionHistoryEntry entry in entries)
        {
            ListViewItem item = new(entry.SessionCode)
            {
                Tag = entry
            };
            item.SubItems.Add(entry.StartedAtUtc?.ToLocalTime().ToString("dd/MM HH:mm:ss") ?? "");
            item.SubItems.Add(entry.FinishedAtUtc?.ToLocalTime().ToString("dd/MM HH:mm:ss") ?? "");
            item.SubItems.Add(entry.StudentCount.ToString());
            item.SubItems.Add(entry.ViolationCount.ToString());
            item.SubItems.Add(entry.TeacherMachine);
            _historySessionsList.Items.Add(item);
        }

        _historySessionsList.EndUpdate();
        if (_historySessionsList.Items.Count > 0 && _historySessionsList.SelectedItems.Count == 0)
        {
            _historySessionsList.Items[0].Selected = true;
        }
        else
        {
            ShowSelectedHistoryEntry();
        }
    }

    private void ShowSelectedHistoryEntry()
    {
        if (_historySessionsList.SelectedItems.Count == 0 || _historySessionsList.SelectedItems[0].Tag is not SessionHistoryEntry entry)
        {
            ReplaceImage(_historyStartImage, null);
            ReplaceImage(_historyEndImage, null);
            _historyDetailsText.Text = "Chưa chọn phiên lịch sử.";
            return;
        }

        ReplaceImage(_historyStartImage, LoadImageOrNull(entry.StartImagePath));
        ReplaceImage(_historyEndImage, LoadImageOrNull(entry.EndImagePath));
        _historyDetailsText.Text =
            $"Phiên: {entry.SessionCode}{Environment.NewLine}" +
            $"Tiêu đề: {entry.Title}{Environment.NewLine}" +
            $"Máy giáo viên: {entry.TeacherMachine}{Environment.NewLine}" +
            $"Bắt đầu: {entry.StartedAtUtc?.ToLocalTime():dd/MM/yyyy HH:mm:ss}{Environment.NewLine}" +
            $"Kết thúc: {entry.FinishedAtUtc?.ToLocalTime():dd/MM/yyyy HH:mm:ss}{Environment.NewLine}" +
            $"Sinh viên: {entry.StudentCount}{Environment.NewLine}" +
            $"Vi phạm: {entry.ViolationCount}{Environment.NewLine}" +
            $"Thư mục: {entry.FolderPath}{Environment.NewLine}" +
            $"session.json: {entry.SessionJsonPath}{Environment.NewLine}" +
            $"violations.jsonl: {entry.ViolationsPath ?? "(không có)"}{Environment.NewLine}" +
            $"audit.jsonl: {entry.AuditPath ?? "(không có)"}";
    }

    private void OpenSelectedHistoryFolder()
    {
        if (_historySessionsList.SelectedItems.Count == 0 || _historySessionsList.SelectedItems[0].Tag is not SessionHistoryEntry entry)
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = entry.FolderPath,
            UseShellExecute = true
        });
    }

    private void ExportHistoryIndex(string format)
    {
        IReadOnlyList<SessionHistoryEntry> entries = SessionHistoryStore.LoadHistoryEntries();
        string folder = Path.Combine(AppContext.BaseDirectory, "Exports");
        Directory.CreateDirectory(folder);
        string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
        {
            string jsonPath = Path.Combine(folder, $"session-history-{timestamp}.json");
            string json = System.Text.Json.JsonSerializer.Serialize(entries, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(jsonPath, json);
            Log($"Đã xuất lịch sử phiên: {jsonPath}");
            return;
        }

        string csvPath = Path.Combine(folder, $"session-history-{timestamp}.csv");
        List<string> lines =
        [
            "sessionCode,title,teacherMachine,startedAtLocal,finishedAtLocal,studentCount,violationCount,folderPath",
            .. entries.Select(entry =>
                $"\"{entry.SessionCode}\",\"{entry.Title.Replace("\"", "\"\"")}\",\"{entry.TeacherMachine}\",\"{entry.StartedAtUtc?.ToLocalTime():dd/MM/yyyy HH:mm:ss}\",\"{entry.FinishedAtUtc?.ToLocalTime():dd/MM/yyyy HH:mm:ss}\",\"{entry.StudentCount}\",\"{entry.ViolationCount}\",\"{entry.FolderPath.Replace("\"", "\"\"")}\"")
        ];
        File.WriteAllLines(csvPath, lines);
        Log($"Đã xuất lịch sử phiên: {csvPath}");
    }

    private void ShowSelectedStudentViewer(string streamKind)
    {
        if (SelectedStudentKey() is not { } studentKey || !_cards.TryGetValue(studentKey, out StudentCard? card))
        {
            return;
        }

        if (streamKind == "screen")
        {
            ShowImageViewer(studentKey, "screen", $"Màn hình {card.DisplayName}", card.CurrentImage);
            return;
        }

        ShowImageViewer(studentKey, "webcam", $"Webcam {card.DisplayName}", card.CurrentWebcamImage);
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
        if (ReferenceEquals(pictureBox.Image, source))
        {
            return;
        }

        Image? old = pictureBox.Image;
        pictureBox.Image = source is null ? null : (Image)source.Clone();
        old?.Dispose();
    }

    private static Image? LoadImageOrNull(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using Image image = Image.FromStream(stream);
        return (Image)image.Clone();
    }

    private static string NormalizeStudentKey(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? ""
            : value.Trim().ToUpperInvariant();
    }

    private static string BuildModifiers(KeyEventArgs e)
    {
        List<string> modifiers = [];
        if (e.Control)
        {
            modifiers.Add("CTRL");
        }

        if (e.Alt)
        {
            modifiers.Add("ALT");
        }

        if (e.Shift)
        {
            modifiers.Add("SHIFT");
        }

        return string.Join('+', modifiers);
    }

    private static string ToProtocolKey(Keys key)
    {
        return key switch
        {
            Keys.Enter => "ENTER",
            Keys.Escape => "ESC",
            Keys.Tab => "TAB",
            Keys.Back => "BACKSPACE",
            Keys.Delete => "DELETE",
            Keys.Insert => "INSERT",
            Keys.Home => "HOME",
            Keys.End => "END",
            Keys.PageUp => "PAGEUP",
            Keys.PageDown => "PAGEDOWN",
            Keys.Up => "UP",
            Keys.Down => "DOWN",
            Keys.Left => "LEFT",
            Keys.Right => "RIGHT",
            Keys.Space => "SPACE",
            Keys.F1 => "F1",
            Keys.F2 => "F2",
            Keys.F3 => "F3",
            Keys.F4 => "F4",
            Keys.F5 => "F5",
            Keys.F6 => "F6",
            Keys.F7 => "F7",
            Keys.F8 => "F8",
            Keys.F9 => "F9",
            Keys.F10 => "F10",
            Keys.F11 => "F11",
            Keys.F12 => "F12",
            _ => ""
        };
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
            _picture.Click += (_, _) => OnClick(EventArgs.Empty);
            _webcamThumbnail.Click += (_, _) => OnClick(EventArgs.Empty);
            _caption.Click += (_, _) => OnClick(EventArgs.Empty);
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
                Image? oldThumb = _webcamThumbnail.Image;
                CurrentWebcamImage = (Image)state.LatestWebcamFrame.Clone();
                _webcamThumbnail.Image = CurrentWebcamImage;
                oldCam?.Dispose();
                if (!ReferenceEquals(oldThumb, oldCam))
                {
                    oldThumb?.Dispose();
                }
                _webcamThumbnail.Visible = true;
            }

            if (_handRaised || !_isOnline)
            {
                Invalidate();
            }
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

    private sealed class WebcamPickerItem
    {
        public WebcamPickerItem(string cameraId, string text)
        {
            CameraId = cameraId;
            Text = text;
        }

        public string CameraId { get; }
        public string Text { get; }

        public override string ToString()
        {
            return Text;
        }
    }

    private sealed class FloatingChatForm : Form
    {
        private readonly TextBox _messageText = new()
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Height = 84
        };
        private readonly Func<string?> _selectedTarget;
        private readonly Func<string?, string, Task> _sendAsync;

        public FloatingChatForm(TextBox historyText, Func<string?> selectedTarget, Func<string?, string, Task> sendAsync)
        {
            _selectedTarget = selectedTarget;
            _sendAsync = sendAsync;
            Text = "Trao đổi với sinh viên";
            Width = 560;
            Height = 520;
            MinimumSize = new Size(460, 380);
            StartPosition = FormStartPosition.CenterParent;
            AppIcons.SetFormIcon(this, "message-circle");

            TableLayoutPanel layout = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(12)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 94F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));

            historyText.Dock = DockStyle.Fill;
            historyText.ReadOnly = true;
            historyText.Multiline = true;
            historyText.ScrollBars = ScrollBars.Vertical;

            FlowLayoutPanel buttons = new()
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };
            Button sendSelectedButton = new() { Text = "Gửi sinh viên đang chọn", Width = 190, Tag = "send" };
            Button sendAllButton = new() { Text = "Gửi tất cả", Width = 120, Tag = "message-circle" };
            sendSelectedButton.Click += async (_, _) => await SendSelectedAsync();
            sendAllButton.Click += async (_, _) => await SendAllAsync();
            buttons.Controls.AddRange([sendSelectedButton, sendAllButton]);

            layout.Controls.Add(historyText, 0, 0);
            layout.Controls.Add(_messageText, 0, 1);
            layout.Controls.Add(buttons, 0, 2);
            Controls.Add(layout);
            UiTheme.Apply(this);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }

            base.OnFormClosing(e);
        }

        private async Task SendSelectedAsync()
        {
            string? target = _selectedTarget();
            if (string.IsNullOrWhiteSpace(target))
            {
                MessageBox.Show(this, "Hãy chọn một sinh viên trước khi gửi tin nhắn 1-1.", "ExamGuard", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            await SendAndClearAsync(target);
        }

        private async Task SendAllAsync()
        {
            await SendAndClearAsync(null);
        }

        private async Task SendAndClearAsync(string? target)
        {
            string message = _messageText.Text.Trim();
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            await _sendAsync(target, message);
            _messageText.Clear();
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
