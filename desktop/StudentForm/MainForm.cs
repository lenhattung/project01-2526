using System.Diagnostics;
using System.IO.Compression;
using System.Media;
using ExamGuard.Protocol;

namespace StudentForm;

public sealed class MainForm : Form
{
    private readonly TextBox _serverText = new() { Text = "127.0.0.1" };
    private readonly NumericUpDown _portInput = new() { Minimum = 1024, Maximum = 65535, Value = 9090 };
    private readonly TextBox _sessionIdText = new() { Text = "EXAM-001", Width = 180 };
    private readonly TextBox _tokenText = new() { Text = "classroom-token", Width = 180 };
    private readonly TextBox _studentCodeText = new() { Text = "SV001", Width = 180 };
    private readonly TextBox _studentNameText = new() { Text = Environment.UserName, Width = 220 };
    private readonly TextBox _windowsUserText = new() { Text = Environment.UserName, Width = 180, ReadOnly = true };
    private readonly Button _trayModeButton = new() { Text = "Chạy ngầm: Tắt", Width = 145, Tag = "wifi" };
    private readonly TextBox _chatText = new()
    {
        Text = "Em cần hỗ trợ.",
        Width = 320,
        Height = 72,
        Multiline = true,
        ScrollBars = ScrollBars.Vertical
    };
    private readonly TextBox _chatHistoryText = new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
    private readonly Label _statusLabel = new() { Text = "Chưa kết nối", AutoSize = true };
    private readonly Label _webcamStatusLabel = new() { Text = "Đang chờ", AutoSize = true };
    private readonly Label _sessionSummaryLabel = new() { Text = "Phiên EXAM-001", AutoSize = true };
    private readonly Label _screenStatusLabel = new() { Text = "Đang chờ", AutoSize = true };
    private readonly Label _submissionStateLabel = new() { Text = "Chưa nộp", AutoSize = true };
    private readonly Label _handRaiseStateLabel = new() { Text = "Chưa yêu cầu hỗ trợ", AutoSize = true };
    private readonly Label _examWindowLabel = new() { Text = "Thời gian: chưa giới hạn", AutoSize = true };
    private readonly Label _remoteControlBanner = new()
    {
        Dock = DockStyle.Top,
        Height = 30,
        TextAlign = ContentAlignment.MiddleCenter,
        Text = "Giảng viên đang điều khiển máy này",
        BackColor = Color.FromArgb(255, 243, 205),
        ForeColor = Color.FromArgb(133, 77, 14),
        Visible = false
    };
    private readonly ProgressBar _submissionProgress = new() { Minimum = 0, Maximum = 100, Width = 300 };
    private readonly ListBox _log = new() { Dock = DockStyle.Fill };
    private readonly System.Windows.Forms.Timer _heartbeatTimer = new() { Interval = 5000 };
    private readonly System.Windows.Forms.Timer _screenTimer = new() { Interval = 500 };
    private readonly System.Windows.Forms.Timer _webcamTimer = new();
    private readonly System.Windows.Forms.Timer _processTimer = new() { Interval = 5000 };
    private readonly ProcessMonitor _processMonitor = new();
    private readonly WebsitePolicyMonitor _websiteMonitor = new();
    private readonly WebcamCaptureService _webcamCapture = new();
    private readonly SessionLookupClient _sessionLookupClient = new();
    private readonly DistributedFileReceiver _fileReceiver = new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
        "ExamGuard - De thi giao vien"));
    private readonly ClipboardShortcutBlocker _clipboardBlocker;
    private LockOverlayForm? _lockOverlay;
    private BroadcastViewerForm? _broadcastViewer;
    private PolicySnapshot _policy = new();
    private StudentSocketClient? _client;
    private Button? _handRaiseButton;
    private bool _isHandRaised;
    private TableLayoutPanel? _contentGrid;
    private TableLayoutPanel? _rightColumnLayout;
    private GroupBox? _connectionBox;
    private GroupBox? _interactionBox;
    private GroupBox? _quickStatusBox;
    private string _lastWebcamStatusMessage = "";
    private string _lastSentWebcamIssue = "";
    private DateTimeOffset _lastClipboardEventAt = DateTimeOffset.MinValue;
    private DateTimeOffset _lastPolicyLogAt = DateTimeOffset.MinValue;
    private int _webcamSendBusy;
    private int _screenSendBusy;
    private int _policyScanBusy;
    private CancellationTokenSource? _reconnectCts;
    private NotifyIcon? _trayIcon;
    private string? _lastResolvedHost;
    private int _lastResolvedPort;
    private string? _lastLanHost;
    private int _lastLanPort;
    private string? _lastLanSessionId;
    private int _reconnectLoopStarted;
    private bool _manualDisconnectRequested;
    private bool _isClosing;
    private bool _allowExit;
    private bool _trayModeEnabled;

    public MainForm()
    {
        Text = "ExamGuard - Máy sinh viên";
        Width = 960;
        Height = 720;
        MinimumSize = new Size(880, 640);
        StartPosition = FormStartPosition.CenterScreen;
        AppIcons.SetFormIcon(this, "book-open");

        _clipboardBlocker = new ClipboardShortcutBlocker(kind => BeginInvoke(() => OnClipboardShortcutBlocked(kind)));

        _heartbeatTimer.Tick += async (_, _) =>
        {
            _examWindowLabel.Text = BuildExamWindowText();
            await SafeSendAsync(() => _client?.SendHeartbeatAsync());
        };
        _screenTimer.Tick += async (_, _) => await SendScreenAsync();
        _webcamTimer.Tick += async (_, _) => await SendWebcamAsync();
        _processTimer.Tick += async (_, _) => await ScanPolicyAsync();
        _trayModeButton.Click += (_, _) => ToggleTrayMode();

        Controls.Add(BuildLayout());
        Controls.Add(_remoteControlBanner);
        _remoteControlBanner.BringToFront();
        Resize += (_, _) => BeginInvoke(UpdateResponsiveLayout);
        Shown += (_, _) => BeginInvoke(UpdateResponsiveLayout);
        InitializeTrayIcon();
        UiTheme.Apply(this);
        FormClosing += async (_, e) =>
        {
            if (e.CloseReason == CloseReason.UserClosing && !_allowExit && _trayModeEnabled)
            {
                e.Cancel = true;
                HideToTray();
                return;
            }

            _isClosing = true;
            CancelReconnectLoop();
            StopTimers();
            _clipboardBlocker.Dispose();
            if (_client is not null)
            {
                await _client.DisposeAsync();
            }

            _webcamCapture.Dispose();
            if (_trayIcon is not null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }
        };
    }

    private void InitializeTrayIcon()
    {
        _trayIcon = new NotifyIcon
        {
            Icon = Icon ?? SystemIcons.Application,
            Text = "ExamGuard - Máy sinh viên",
            Visible = false
        };
        _trayIcon.DoubleClick += (_, _) => RestoreFromTray();

        ContextMenuStrip menu = new();
        menu.Items.Add("Mở lại", null, (_, _) => RestoreFromTray());
        menu.Items.Add("Tắt chạy ngầm", null, (_, _) =>
        {
            _trayModeEnabled = false;
            UpdateTrayModeUi();
            RestoreFromTray();
        });
        menu.Items.Add("Thoát", null, (_, _) =>
        {
            _allowExit = true;
            _trayIcon!.Visible = false;
            Close();
        });
        _trayIcon.ContextMenuStrip = menu;
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
        WindowState = FormWindowState.Minimized;
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
        Activate();
    }

    private Control BuildLayout()
    {
        TableLayoutPanel root = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 3
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 62F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 38F));

        Panel header = new() { Dock = DockStyle.Fill, Height = 54, Padding = new Padding(12, 8, 12, 8) };
        FlowLayoutPanel title = new()
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Tag = "surface-flow"
        };
        Label titleText = new()
        {
            Text = "Máy sinh viên làm bài",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold, GraphicsUnit.Point),
            Margin = new Padding(0, 4, 0, 0)
        };
        title.Controls.AddRange([AppIcons.Picture("book-open", 28), titleText]);
        FlowLayoutPanel headerStatus = new()
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            WrapContents = false,
            Tag = "surface-flow"
        };
        headerStatus.Controls.AddRange([
            StatusChip(_statusLabel, "wifi"),
            StatusChip(_sessionSummaryLabel, "key"),
            StatusChip(_screenStatusLabel, "monitor"),
            StatusChip(_webcamStatusLabel, "video")]);
        header.Controls.Add(headerStatus);
        header.Controls.Add(title);

        Button discoverButton = IconButton("Tìm/Kết nối trong LAN", "search", 185);
        discoverButton.Click += async (_, _) => await DiscoverTeacherAsync();
        Button remoteSearchButton = IconButton("Tìm/Kết nối khác mạng", "wifi", 190);
        remoteSearchButton.Click += async (_, _) => await ConnectRemoteAsync();
        Button disconnectButton = IconButton("Ngắt kết nối", "log-out", 130);
        disconnectButton.Click += async (_, _) => await DisconnectAsync();
        Button submitFileButton = IconButton("Nộp tệp", "upload-cloud", 110);
        submitFileButton.Click += async (_, _) => await SubmitFileAsync();
        Button submitFolderButton = IconButton("Nộp thư mục", "folder", 130);
        submitFolderButton.Click += async (_, _) => await SubmitFolderAsync();
        _handRaiseButton = IconButton("Giơ tay", "help-circle", 120);
        _handRaiseButton.Click += async (_, _) => await RaiseHandAsync();
        Button chatButton = IconButton("Gửi tin nhắn", "send", 125);
        chatButton.Click += async (_, _) => await SendChatAsync();

        _connectionBox = CreateGroupBox("Kết nối và phiên thi");
        TableLayoutPanel connectionGrid = CreateFieldGrid();
        connectionGrid.Controls.Add(LabelFor("Mã phiên"), 0, 0);
        _sessionIdText.Dock = DockStyle.Fill;
        connectionGrid.Controls.Add(_sessionIdText, 1, 0);
        connectionGrid.Controls.Add(LabelFor("Mã bảo vệ"), 0, 1);
        _tokenText.Dock = DockStyle.Fill;
        connectionGrid.Controls.Add(_tokenText, 1, 1);
        connectionGrid.Controls.Add(LabelFor("Mã sinh viên"), 0, 2);
        _studentCodeText.Dock = DockStyle.Fill;
        connectionGrid.Controls.Add(_studentCodeText, 1, 2);
        connectionGrid.Controls.Add(LabelFor("Tên sinh viên"), 0, 3);
        _studentNameText.Dock = DockStyle.Fill;
        connectionGrid.Controls.Add(_studentNameText, 1, 3);
        connectionGrid.Controls.Add(LabelFor("Tên user máy"), 0, 4);
        _windowsUserText.Dock = DockStyle.Fill;
        connectionGrid.Controls.Add(_windowsUserText, 1, 4);
        FlowLayoutPanel connectionButtons = CreateButtonRow(discoverButton, remoteSearchButton, _trayModeButton, disconnectButton);
        connectionGrid.Controls.Add(connectionButtons, 0, 5);
        connectionGrid.SetColumnSpan(connectionButtons, 2);
        _connectionBox.Controls.Add(connectionGrid);

        _interactionBox = CreateGroupBox("Nộp bài và liên hệ giáo viên");
        TableLayoutPanel interactionGrid = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 8
        };
        interactionGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        interactionGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        interactionGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        interactionGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        interactionGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        interactionGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        interactionGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        interactionGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        Label submissionLabel = LabelFor("Tiến độ nộp bài");
        submissionLabel.Margin = new Padding(6, 4, 4, 4);
        _submissionProgress.Dock = DockStyle.Top;
        _submissionProgress.Height = 18;
        FlowLayoutPanel submitRow = CreateButtonRow(submitFileButton, submitFolderButton);
        Label chatLabel = LabelFor("Tin nhắn");
        chatLabel.Margin = new Padding(6, 10, 4, 4);
        _chatText.Dock = DockStyle.Fill;
        _chatText.Height = 70;
        FlowLayoutPanel chatRow = CreateButtonRow(chatButton, _handRaiseButton);
        Label chatHistoryLabel = LabelFor("Lịch sử chat");
        chatHistoryLabel.Margin = new Padding(6, 10, 4, 4);
        _chatHistoryText.Dock = DockStyle.Fill;
        _chatHistoryText.MinimumSize = new Size(220, 84);
        interactionGrid.Controls.Add(submissionLabel, 0, 0);
        interactionGrid.Controls.Add(_submissionProgress, 0, 1);
        interactionGrid.Controls.Add(submitRow, 0, 2);
        interactionGrid.Controls.Add(chatLabel, 0, 3);
        interactionGrid.Controls.Add(_chatText, 0, 4);
        interactionGrid.Controls.Add(chatRow, 0, 5);
        interactionGrid.Controls.Add(chatHistoryLabel, 0, 6);
        interactionGrid.Controls.Add(_chatHistoryText, 0, 7);
        _interactionBox.Controls.Add(interactionGrid);

        _quickStatusBox = CreateGroupBox("Trạng thái nhanh");
        TableLayoutPanel quickStatusGrid = CreateFieldGrid();
        quickStatusGrid.Controls.Add(LabelFor("Kết nối"), 0, 0);
        quickStatusGrid.Controls.Add(_statusLabel, 1, 0);
        quickStatusGrid.Controls.Add(LabelFor("Màn hình"), 0, 1);
        quickStatusGrid.Controls.Add(_screenStatusLabel, 1, 1);
        quickStatusGrid.Controls.Add(LabelFor("Webcam"), 0, 2);
        quickStatusGrid.Controls.Add(_webcamStatusLabel, 1, 2);
        quickStatusGrid.Controls.Add(LabelFor("Nộp bài"), 0, 3);
        quickStatusGrid.Controls.Add(_submissionStateLabel, 1, 3);
        quickStatusGrid.Controls.Add(LabelFor("Hỗ trợ"), 0, 4);
        quickStatusGrid.Controls.Add(_handRaiseStateLabel, 1, 4);
        quickStatusGrid.Controls.Add(LabelFor("Thời gian"), 0, 5);
        quickStatusGrid.Controls.Add(_examWindowLabel, 1, 5);
        _quickStatusBox.Controls.Add(quickStatusGrid);

        _contentGrid = new TableLayoutPanel()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 10)
        };
        _contentGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64F));
        _contentGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));

        _rightColumnLayout = new TableLayoutPanel()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(8, 0, 0, 0)
        };
        _rightColumnLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
        _rightColumnLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
        _rightColumnLayout.Controls.Add(_interactionBox, 0, 0);
        _rightColumnLayout.Controls.Add(_quickStatusBox, 0, 1);

        _contentGrid.Controls.Add(_connectionBox, 0, 0);
        _contentGrid.Controls.Add(_rightColumnLayout, 1, 0);

        GroupBox logBox = CreateGroupBox("Trạng thái và nhật ký");
        logBox.Controls.Add(_log);

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(_contentGrid, 0, 1);
        root.Controls.Add(logBox, 0, 2);
        return root;
    }

    private static Label LabelFor(string text)
    {
        return new Label { Text = text, AutoSize = true, Margin = new Padding(10, 8, 4, 0) };
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
            Margin = new Padding(0, 0, 0, 10)
        };
    }

    private static TableLayoutPanel CreateFieldGrid()
    {
        TableLayoutPanel grid = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        return grid;
    }

    private void UpdateResponsiveLayout()
    {
        if (_contentGrid is null || _rightColumnLayout is null || _connectionBox is null || _interactionBox is null || _quickStatusBox is null)
        {
            return;
        }

        if (_contentGrid.ClientSize.Width <= 0)
        {
            return;
        }

        bool compact = ClientSize.Width < 1150;
        _contentGrid.ColumnStyles.Clear();
        _contentGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, compact ? 60F : 66F));
        _contentGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, compact ? 40F : 34F));

        _rightColumnLayout.RowStyles.Clear();
        _rightColumnLayout.RowStyles.Add(new RowStyle(SizeType.Percent, compact ? 64F : 58F));
        _rightColumnLayout.RowStyles.Add(new RowStyle(SizeType.Percent, compact ? 36F : 42F));

        _chatText.Height = compact ? 64 : 78;
        _chatHistoryText.MinimumSize = new Size(220, compact ? 72 : 96);
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

    private async Task ConnectRemoteAsync()
    {
        if (_client?.IsConnected == true)
        {
            return;
        }

        await ConnectWithResolverAsync(
            "Đang tìm giáo viên khác mạng qua máy chủ relay...",
            ResolveBackendEndpointAsync);
    }

    private async Task ConnectWithResolverAsync(
        string startMessage,
        Func<Task<(string host, int port, PolicySnapshot? policy, bool useRelay, string relaySecret)>> resolver)
    {
        if (!ValidateConnectionInputs())
        {
            return;
        }

        _manualDisconnectRequested = false;
        CancelReconnectLoop();
        try
        {
            Log(startMessage);
            (string host, int port, PolicySnapshot? lookupPolicy, bool useRelay, string relaySecret) = await resolver();
            await ConnectToResolvedEndpointAsync(host, port, lookupPolicy, useRelay, relaySecret);
        }
        catch (Exception ex)
        {
            Log($"Không kết nối được: {ex.Message}");
            SetConnectionStatus("Chưa kết nối");
        }
    }

    private async Task DiscoverTeacherAsync()
    {
        if (_client?.IsConnected == true)
        {
            return;
        }

        if (!ValidateConnectionInputs())
        {
            return;
        }

        Log("Đang tìm máy giáo viên trong mạng LAN (chỉ dùng khi cùng Wi-Fi/LAN)...");
        DiscoveryAnnouncement? announcement = await TeacherDiscoveryClient.DiscoverAsync(_sessionIdText.Text.Trim(), TimeSpan.FromSeconds(5));
        if (announcement is null)
        {
            Log("Không tìm thấy máy giáo viên trong LAN. Nếu thi từ xa/khác Wi-Fi, bấm Tìm/Kết nối khác mạng.");
            return;
        }

        _serverText.Text = announcement.Host;
        _portInput.Value = Math.Clamp(announcement.Port, (int)_portInput.Minimum, (int)_portInput.Maximum);
        _sessionIdText.Text = announcement.SessionId;
        _sessionSummaryLabel.Text = $"Phiên {announcement.SessionId}";
        RememberLanEndpoint(announcement.Host, announcement.Port, announcement.SessionId);
        Log($"Đã tìm thấy máy giáo viên {announcement.TeacherMachine} tại {announcement.Host}:{announcement.Port}. Đang kết nối LAN...");
        try
        {
            await ConnectToResolvedEndpointAsync(announcement.Host, announcement.Port, null, false, "");
        }
        catch (Exception ex)
        {
            Log($"Đã tìm thấy giáo viên nhưng kết nối LAN chưa thành công: {ex.Message}");
            SetConnectionStatus("Chưa kết nối");
        }
    }

    private async Task DisconnectAsync()
    {
        _manualDisconnectRequested = true;
        CancelReconnectLoop();
        StopTimers();
        _clipboardBlocker.Stop();
        _isHandRaised = false;
        if (_handRaiseButton is not null)
        {
            _handRaiseButton.Text = "Giơ tay";
        }
        if (_client is not null)
        {
            if (CanSubmitNow())
            {
                await SafeSendAsync(() => _client.SendDisconnectNoticeAsync("Sinh viên chủ động ngắt kết nối trước khi hết giờ làm bài."));
            }

            await _client.DisconnectAsync();
        }

        _client = null;
        _remoteControlBanner.Visible = false;

        SetConnectionStatus("Chưa kết nối");
        SetScreenStatus("Đang chờ");
        SetSubmissionStatus("Chưa nộp");
        _sessionSummaryLabel.Text = $"Phiên {_sessionIdText.Text.Trim()}";
    }

    private bool ValidateConnectionInputs()
    {
        if (string.IsNullOrWhiteSpace(_studentCodeText.Text) || string.IsNullOrWhiteSpace(_studentNameText.Text))
        {
            Log("Vui lòng nhập mã sinh viên và tên sinh viên.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_sessionIdText.Text) || string.IsNullOrWhiteSpace(_tokenText.Text))
        {
            Log("Vui lòng nhập mã phiên và mã bảo vệ.");
            return false;
        }

        return true;
    }

    private void RememberLanEndpoint(string host, int port, string sessionId)
    {
        _lastLanHost = host;
        _lastLanPort = port;
        _lastLanSessionId = sessionId;
    }

    private void StartTimers()
    {
        _heartbeatTimer.Start();
        _screenTimer.Start();
        _processTimer.Start();
        if (_policy.WebcamEnabled)
        {
            _webcamTimer.Start();
        }
    }

    private void StopTimers()
    {
        _heartbeatTimer.Stop();
        _screenTimer.Stop();
        _webcamTimer.Stop();
        _processTimer.Stop();
    }

    private async Task SendScreenAsync()
    {
        if (_client is null || !_client.IsConnected || Interlocked.Exchange(ref _screenSendBusy, 1) == 1)
        {
            return;
        }

        try
        {
            await SafeSendAsync(async () =>
            {
                byte[] jpeg = await Task.Run(() => ScreenCaptureService.CapturePrimaryScreenJpeg(_policy.ScreenJpegQuality));
                if (_client is not null && _client.IsConnected)
                {
                    await _client.SendScreenFrameAsync(jpeg);
                    SetScreenStatus("Đang gửi");
                }
            });
        }
        finally
        {
            Interlocked.Exchange(ref _screenSendBusy, 0);
        }
    }

    private async Task SendWebcamAsync()
    {
        if (_client is null || !_client.IsConnected || !_policy.WebcamEnabled)
        {
            return;
        }

        if (Interlocked.Exchange(ref _webcamSendBusy, 1) == 1)
        {
            return;
        }

        try
        {
            await SafeSendAsync(async () =>
            {
                WebcamCaptureResult result = await Task.Run(() => _webcamCapture.TryCaptureJpeg(_policy.WebcamJpegQuality));
                if (result.IsSuccess && result.Jpeg is not null)
                {
                    await _client.SendWebcamFrameAsync(result.Jpeg, _webcamCapture.SelectedCameraId);
                    _lastSentWebcamIssue = "";
                    SetWebcamStatus("Đang hoạt động");
                }
                else
                {
                    if (!string.Equals(_lastSentWebcamIssue, result.Message, StringComparison.Ordinal))
                    {
                        await _client.SendWebcamStatusAsync("unavailable", result.Message);
                        _lastSentWebcamIssue = result.Message;
                    }

                    SetWebcamStatus(result.Message);
                }
            });
        }
        finally
        {
            Interlocked.Exchange(ref _webcamSendBusy, 0);
        }
    }

    private async Task ScanPolicyAsync()
    {
        if (Interlocked.Exchange(ref _policyScanBusy, 1) == 1)
        {
            return;
        }

        try
        {
            Violation[] violations = await Task.Run(() => _processMonitor.ScanAndEnforce(
                _policy.BlockedProcesses,
                _policy.BlockedWindowKeywords,
                _policy.BlockedAiCliTools,
                _policy.BlockedProxyTools,
                _policy.BlockedIdeExtensions).ToArray());

            foreach (Violation violation in violations)
            {
                if (DateTimeOffset.Now - _lastPolicyLogAt > TimeSpan.FromMilliseconds(500))
                {
                    Log($"Vi phạm phần mềm: {violation.ProcessName} {violation.Action}");
                    _lastPolicyLogAt = DateTimeOffset.Now;
                }

                if (_client is not null)
                {
                    await _client.SendViolationAsync(violation.ProcessName, violation.WindowTitle, violation.Action);
                    await _client.SendActivityEventAsync("policy_violation", new Dictionary<string, string>
                    {
                        ["processName"] = violation.ProcessName,
                        ["windowTitle"] = violation.WindowTitle,
                        ["ruleKind"] = violation.RuleKind,
                        ["rule"] = violation.Rule,
                        ["action"] = violation.Action,
                        ["commandLine"] = violation.CommandLine
                    });
                }
            }

            List<string> websiteKeywords = _policy.BlockedWindowKeywords.Concat(_policy.BlockedWebsiteHosts).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (websiteKeywords.Count > 0)
            {
                WebsiteViolation[] websiteViolations = await Task.Run(() => _websiteMonitor.ScanAndEnforce(websiteKeywords, _policy.AllowedWebsiteHosts).ToArray());
                foreach (WebsiteViolation violation in websiteViolations)
                {
                    if (DateTimeOffset.Now - _lastPolicyLogAt > TimeSpan.FromMilliseconds(500))
                    {
                        Log($"Đã chặn website theo từ khóa cấm: {violation.ProcessName}");
                        _lastPolicyLogAt = DateTimeOffset.Now;
                    }

                    if (_client is not null)
                    {
                        await _client.SendActivityEventAsync("website_blocked", new Dictionary<string, string>
                        {
                            ["processName"] = violation.ProcessName,
                            ["windowTitle"] = violation.WindowTitle,
                            ["allowedHosts"] = violation.AllowedHosts,
                            ["blockedKeywords"] = violation.BlockedKeywords,
                            ["action"] = violation.Action
                        });
                    }
                }
            }
        }
        finally
        {
            Interlocked.Exchange(ref _policyScanBusy, 0);
        }
    }

    private void OnClipboardShortcutBlocked(string kind)
    {
        if (DateTimeOffset.Now - _lastClipboardEventAt < TimeSpan.FromSeconds(3))
        {
            return;
        }

        _lastClipboardEventAt = DateTimeOffset.Now;
        Log(kind == "copy" ? "Đã chặn thao tác copy." : "Đã chặn thao tác paste.");
        if (_client is not null)
        {
            _ = _client.SendActivityEventAsync("clipboard_blocked", new Dictionary<string, string>
            {
                ["shortcut"] = kind
            });
        }
    }

    private async Task SubmitFolderAsync()
    {
        if (!CanStartSubmission())
        {
            return;
        }

        using FolderBrowserDialog dialog = new()
        {
            Description = "Chọn thư mục chứa bài làm của bạn"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        string zipPath = CreateSubmissionZipPath();
        ZipFile.CreateFromDirectory(dialog.SelectedPath, zipPath, CompressionLevel.Fastest, includeBaseDirectory: true);
        await SubmitPathAsync(zipPath, "Đã nộp thư mục bài làm.");
    }

    private async Task SubmitFileAsync()
    {
        if (!CanStartSubmission())
        {
            return;
        }

        using OpenFileDialog dialog = new()
        {
            Title = "Chọn tệp bài làm để nộp"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        string zipPath = CreateSubmissionZipPath();
        using (FileStream zipStream = File.Create(zipPath))
        using (ZipArchive archive = new(zipStream, ZipArchiveMode.Create))
        {
            archive.CreateEntryFromFile(dialog.FileName, Path.GetFileName(dialog.FileName), CompressionLevel.Fastest);
        }

        await SubmitPathAsync(zipPath, "Đã nộp tệp bài làm.");
    }

    private async Task RaiseHandAsync()
    {
        if (_client is null || !_client.IsConnected)
        {
            Log("Hãy kết nối với máy giáo viên trước khi giơ tay.");
            return;
        }

        if (_isHandRaised)
        {
            await _client.SendHandRaiseClearAsync("Sinh viên đã tự hạ tay.");
            _isHandRaised = false;
            _handRaiseStateLabel.Text = "Đã tắt giơ tay";
            if (_handRaiseButton is not null)
            {
                _handRaiseButton.Text = "Giơ tay";
            }

            Log("Đã tắt yêu cầu hỗ trợ.");
            return;
        }

        await _client.SendHandRaiseAsync("Sinh viên cần hỗ trợ.");
        _isHandRaised = true;
        _handRaiseStateLabel.Text = "Đã gửi yêu cầu hỗ trợ";
        if (_handRaiseButton is not null)
        {
            _handRaiseButton.Text = "Tắt giơ tay";
        }

        Log("Đã gửi yêu cầu hỗ trợ tới giáo viên.");
        SystemSounds.Asterisk.Play();
    }

    private async Task SendChatAsync()
    {
        if (_client is null || !_client.IsConnected)
        {
            Log("Hãy kết nối với máy giáo viên trước khi gửi tin nhắn.");
            return;
        }

        string message = _chatText.Text.Trim();
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        await _client.SendChatAsync(message);
        AppendChatHistory($"Em: {message}");
        Log("Đã gửi tin nhắn tới giáo viên.");
    }

    private async Task HandleTeacherFrameAsync(ReceivedFrame frame)
    {
        switch (frame.Envelope.MessageType)
        {
            case MessageType.ChatMessage:
                BeginInvoke(() =>
                {
                    string message = frame.Envelope.Metadata.GetValueOrDefault("message", "");
                    AppendChatHistory($"Giáo viên: {message}");
                    Log($"Giáo viên: {message}");
                    SystemSounds.Asterisk.Play();
                });
                break;
            case MessageType.HandRaiseClear:
                BeginInvoke(() =>
                {
                    _isHandRaised = false;
                    _handRaiseStateLabel.Text = "Giáo viên đã xử lý yêu cầu";
                    if (_handRaiseButton is not null)
                    {
                        _handRaiseButton.Text = "Giơ tay";
                    }
                });
                break;
            case MessageType.Attention:
                BeginInvoke(() =>
                {
                    MessageBox.Show(
                        this,
                        frame.Envelope.Metadata.GetValueOrDefault("message", "Vui lòng chú ý."),
                        "Thông báo từ giáo viên",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    SystemSounds.Exclamation.Play();
                });
                break;
            case MessageType.LockScreen:
                BeginInvoke(() => ShowLockOverlay(frame.Envelope.Metadata.GetValueOrDefault("message", "")));
                break;
            case MessageType.UnlockScreen:
                BeginInvoke(HideLockOverlay);
                break;
            case MessageType.TeacherFrame:
                BeginInvoke(() => ShowTeacherFrame(frame.Payload));
                break;
            case MessageType.TeacherBroadcastStop:
                BeginInvoke(CloseTeacherFrame);
                break;
            case MessageType.WebcamDevices:
                if (_client is not null)
                {
                    await _client.SendWebcamDevicesAsync(_webcamCapture.ProbeDevices());
                }
                break;
            case MessageType.WebcamSelect:
                BeginInvoke(() =>
                {
                    string cameraId = frame.Envelope.Metadata.GetValueOrDefault("cameraId", "");
                    _webcamCapture.SelectCamera(cameraId);
                    _lastSentWebcamIssue = "";
                    SetWebcamStatus($"Đã chọn {cameraId}");
                });
                if (_client is not null)
                {
                    await _client.SendWebcamDevicesAsync(_webcamCapture.ProbeDevices());
                }
                break;
            case MessageType.FileDistributionStart:
                BeginInvoke(() => StartDistributedFile(frame.Envelope.Metadata.GetValueOrDefault("fileName", "teacher-file.bin")));
                break;
            case MessageType.FileDistributionChunk:
                await AppendDistributedFileAsync(frame);
                break;
            case MessageType.FileDistributionComplete:
                await CompleteDistributedFileAsync(frame.Envelope.Metadata.GetValueOrDefault("fileName", "teacher-file.bin"));
                break;
            case MessageType.ExecuteCommand:
                await ExecuteTeacherCommandAsync(frame.Envelope.Metadata.GetValueOrDefault("command", ""));
                break;
            case MessageType.RemoteMouseClick:
                BeginInvoke(() => RemoteInputService.Click(
                    frame.Envelope.Metadata.GetValueOrDefault("relativeX", "0.5"),
                    frame.Envelope.Metadata.GetValueOrDefault("relativeY", "0.5"),
                    frame.Envelope.Metadata.GetValueOrDefault("button", "left")));
                break;
            case MessageType.RemoteTextInput:
                BeginInvoke(() => RemoteInputService.TypeText(frame.Envelope.Metadata.GetValueOrDefault("text", "")));
                break;
            case MessageType.RemoteControlStart:
                BeginInvoke(() =>
                {
                    _remoteControlBanner.Visible = true;
                    Log("Giáo viên đang điều khiển máy này.");
                });
                break;
            case MessageType.RemoteControlStop:
                BeginInvoke(() =>
                {
                    _remoteControlBanner.Visible = false;
                    Log("Giáo viên đã dừng điều khiển máy này.");
                });
                break;
            case MessageType.RemotePointer:
                BeginInvoke(() => RemoteInputService.Pointer(
                    frame.Envelope.Metadata.GetValueOrDefault("action", "move"),
                    frame.Envelope.Metadata.GetValueOrDefault("relativeX", "0.5"),
                    frame.Envelope.Metadata.GetValueOrDefault("relativeY", "0.5"),
                    frame.Envelope.Metadata.GetValueOrDefault("button", "left"),
                    frame.Envelope.Metadata.GetValueOrDefault("wheelDelta", "0")));
                break;
            case MessageType.RemoteKey:
                BeginInvoke(() => RemoteInputService.Key(
                    frame.Envelope.Metadata.GetValueOrDefault("action", "text"),
                    frame.Envelope.Metadata.GetValueOrDefault("key", ""),
                    frame.Envelope.Metadata.GetValueOrDefault("text", ""),
                    frame.Envelope.Metadata.GetValueOrDefault("modifiers", "")));
                break;
            case MessageType.ClipboardSet:
                BeginInvoke(() =>
                {
                    Clipboard.SetText(frame.Envelope.Metadata.GetValueOrDefault("text", ""));
                    Log("Giáo viên đã đặt nội dung clipboard.");
                });
                break;
        }
    }

    private void ShowLockOverlay(string message)
    {
        _lockOverlay ??= new LockOverlayForm();
        _lockOverlay.SetMessage(message);
        if (!_lockOverlay.Visible)
        {
            _lockOverlay.Show(this);
        }

        Log("Giáo viên đã khóa màn hình này.");
    }

    private void HideLockOverlay()
    {
        _lockOverlay?.Hide();
        Log("Giáo viên đã mở khóa màn hình này.");
    }

    private void ShowTeacherFrame(byte[] jpeg)
    {
        _broadcastViewer ??= new BroadcastViewerForm();
        _broadcastViewer.ShowFrame(jpeg);
    }

    private void CloseTeacherFrame()
    {
        _broadcastViewer?.Close();
        _broadcastViewer = null;
        Log("Giáo viên đã dừng chia sẻ màn hình.");
    }

    private void StartDistributedFile(string fileName)
    {
        string path = _fileReceiver.Start(fileName);
        Log($"Đang nhận đề thi từ giáo viên: {path}");
    }

    private async Task AppendDistributedFileAsync(ReceivedFrame frame)
    {
        string fileName = frame.Envelope.Metadata.GetValueOrDefault("fileName", "teacher-file.bin");
        await _fileReceiver.AppendAsync(fileName, frame.Payload, CancellationToken.None);
    }

    private async Task CompleteDistributedFileAsync(string fileName)
    {
        string path = await _fileReceiver.CompleteAsync(fileName);
        BeginInvoke(() =>
        {
            Log($"Đã nhận xong đề thi từ giáo viên: {path}");
            SystemSounds.Asterisk.Play();
        });
    }

    private async Task ExecuteTeacherCommandAsync(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return;
        }

        try
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Không thể chạy lệnh.");
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            if (_client is not null)
            {
                await _client.SendCommandResultAsync(command, process.ExitCode, output + error);
            }

            BeginInvoke(() => Log($"Đã chạy lệnh của giáo viên: {command}"));
        }
        catch (Exception ex)
        {
            if (_client is not null)
            {
                await _client.SendCommandResultAsync(command, -1, ex.Message);
            }

            BeginInvoke(() => Log($"Chạy lệnh thất bại: {ex.Message}"));
        }
    }

    private async Task SafeSendAsync(Func<Task?> action)
    {
        try
        {
            Task? task = action();
            if (task is not null)
            {
                await task;
            }
        }
        catch (Exception ex)
        {
            Log($"Thao tác thất bại: {ex.Message}");
        }
    }

    private void Log(string message)
    {
        _log.Items.Insert(0, $"{DateTime.Now:HH:mm:ss} {message}");
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

    private void ApplyPolicyRuntime()
    {
        _screenTimer.Interval = Math.Clamp(_policy.ScreenIntervalMs, 120, 10000);
        _webcamTimer.Interval = _policy.WebcamIntervalMs <= 0
            ? 50
            : Math.Clamp(_policy.WebcamIntervalMs, 40, 10000);

        if (_client?.IsConnected == true && _policy.WebcamEnabled)
        {
            _webcamTimer.Start();
        }
        else
        {
            _webcamTimer.Stop();
        }

        if (_client?.IsConnected == true && _policy.BlockClipboardShortcuts)
        {
            _clipboardBlocker.Start();
        }
        else
        {
            _clipboardBlocker.Stop();
        }

        if (!_policy.WebcamEnabled)
        {
            SetWebcamStatus("Bị tắt theo quy định");
        }

        _examWindowLabel.Text = BuildExamWindowText();
    }

    private void MarkConnected()
    {
        SetConnectionStatus("Đã kết nối");
        _sessionSummaryLabel.Text = $"Phiên {_sessionIdText.Text.Trim()}";
        Log("Máy giáo viên đã chấp nhận kết nối. Màn hình và webcam sẽ gửi theo quy định.");
        ApplyPolicyRuntime();
    }

    private void HandleConnectionClosed(string message)
    {
        StopTimers();
        _clipboardBlocker.Stop();
        _isHandRaised = false;
        if (_handRaiseButton is not null)
        {
            _handRaiseButton.Text = "Giơ tay";
        }
        SetConnectionStatus("Chưa kết nối");
        SetScreenStatus("Đang chờ");
        _client = null;
        if (_client?.IsConnected != true)
        {
            Log(message);
        }

        if (!_manualDisconnectRequested && !_isClosing)
        {
            StartReconnectLoop();
        }
    }

    private void SetConnectionStatus(string message) => _statusLabel.Text = message;

    private void SetScreenStatus(string message) => _screenStatusLabel.Text = message;

    private void SetSubmissionStatus(string message) => _submissionStateLabel.Text = message;

    private async Task<(string host, int port, PolicySnapshot? policy, bool useRelay, string relaySecret)> ResolveTeacherEndpointAsync()
    {
        string sessionCode = _sessionIdText.Text.Trim();

        if (!string.IsNullOrWhiteSpace(_lastLanHost) &&
            _lastLanPort > 0 &&
            string.Equals(_lastLanSessionId, sessionCode, StringComparison.OrdinalIgnoreCase))
        {
            Log($"Dùng máy giáo viên LAN đã tìm thấy: {_lastLanHost}:{_lastLanPort}.");
            return (_lastLanHost, _lastLanPort, null, false, "");
        }

        Exception? backendLookupError = null;
        try
        {
            return await ResolveBackendEndpointAsync();
        }
        catch (Exception ex)
        {
            backendLookupError = ex;
            Log($"Chưa tra cứu được phiên qua máy chủ: {ex.Message}. Sẽ thử tìm giáo viên trong LAN.");
        }

        DiscoveryAnnouncement? announcement = await TeacherDiscoveryClient.DiscoverAsync(sessionCode, TimeSpan.FromSeconds(2));
        if (announcement is not null)
        {
            _serverText.Text = announcement.Host;
            _portInput.Value = Math.Clamp(announcement.Port, (int)_portInput.Minimum, (int)_portInput.Maximum);
            RememberLanEndpoint(announcement.Host, announcement.Port, announcement.SessionId);
            Log($"Đã tự tìm thấy máy giáo viên trong LAN tại {announcement.Host}:{announcement.Port}.");
            return (announcement.Host, announcement.Port, null, false, "");
        }

        if (backendLookupError is not null)
        {
            throw new InvalidOperationException("Không tìm thấy giáo viên trong LAN và chưa tra cứu được phiên qua máy chủ. Kiểm tra backend/VPS hoặc kết nối mạng.", backendLookupError);
        }

        throw new InvalidOperationException("Không tìm được thông tin phiên thi trên máy chủ và không tìm thấy giáo viên trong LAN.");
    }

    private async Task<(string host, int port, PolicySnapshot? policy, bool useRelay, string relaySecret)> ResolveBackendEndpointAsync()
    {
        string sessionCode = _sessionIdText.Text.Trim();
        string sessionToken = _tokenText.Text.Trim();
        JoinSessionLookupDto? lookup = await _sessionLookupClient.LookupAsync(AppRuntime.BackendBaseUrl, sessionCode, sessionToken);
        if (lookup is null)
        {
            throw new InvalidOperationException("Không tìm được thông tin phiên thi trên máy chủ.");
        }

        bool relayMode = string.Equals(lookup.ConnectionMode, "relay", StringComparison.OrdinalIgnoreCase) || lookup.RelayEnabled;
        if (relayMode)
        {
            if (string.IsNullOrWhiteSpace(lookup.RelayHost) || lookup.RelayPort is null or <= 0)
            {
                throw new InvalidOperationException("Phiên thi đã chọn relay nhưng máy chủ chưa có thông tin relay. Hãy nhấn Bắt đầu phiên trên máy giáo viên.");
            }

            _serverText.Text = lookup.RelayHost;
            _portInput.Value = Math.Clamp(lookup.RelayPort.Value, (int)_portInput.Minimum, (int)_portInput.Maximum);
            _sessionSummaryLabel.Text = $"Phiên {lookup.SessionCode}";
            Log("Đã nhận thông tin máy chủ relay. Đang kết nối qua máy chủ, không cần cùng Wi-Fi/LAN.");
            return (lookup.RelayHost, lookup.RelayPort.Value, BuildPolicyFromLookup(lookup), true, lookup.RelaySecret ?? "");
        }

        if (string.IsNullOrWhiteSpace(lookup.Host) || lookup.Port is null or <= 0)
        {
            throw new InvalidOperationException("Phiên thi trên máy chủ chưa công bố điểm kết nối.");
        }

        _serverText.Text = lookup.Host;
        _portInput.Value = Math.Clamp(lookup.Port.Value, (int)_portInput.Minimum, (int)_portInput.Maximum);
        _sessionSummaryLabel.Text = $"Phiên {lookup.SessionCode}";
        Log("Đã nhận điểm kết nối từ máy chủ.");
        return (lookup.Host, lookup.Port.Value, BuildPolicyFromLookup(lookup), false, "");
    }

    private static PolicySnapshot BuildPolicyFromLookup(JoinSessionLookupDto lookup)
    {
        return new PolicySnapshot
        {
            BlockedProcesses = lookup.BlockedProcesses,
            BlockedWindowKeywords = lookup.BlockedWindowKeywords,
            ScreenIntervalMs = lookup.ScreenIntervalMs,
            ScreenJpegQuality = lookup.ScreenJpegQuality,
            WebcamEnabled = lookup.WebcamEnabled,
            WebcamSnapshotOnConnect = lookup.WebcamSnapshotOnConnect,
            WebcamIntervalMs = lookup.WebcamIntervalMs,
            WebcamJpegQuality = lookup.WebcamJpegQuality,
            ExamDurationMinutes = lookup.ExamDurationMinutes,
            AllowSubmissionAfterDeadline = lookup.AllowSubmissionAfterDeadline,
            StartedAtUtc = lookup.StartedAtUtc ?? "",
            ExamEndAtUtc = lookup.ExamEndAtUtc ?? "",
            ConnectionMode = lookup.ConnectionMode,
            BlockClipboardShortcuts = lookup.BlockClipboardShortcuts,
            WebsitePolicyMode = lookup.WebsitePolicyMode,
            AllowedWebsiteHosts = lookup.AllowedWebsiteHosts,
            BlockedAiCliTools = lookup.BlockedAiCliTools,
            BlockedProxyTools = lookup.BlockedProxyTools,
            BlockedIdeExtensions = lookup.BlockedIdeExtensions,
            BlockedWebsiteHosts = lookup.BlockedWebsiteHosts
        };
    }

    private async Task ConnectToResolvedEndpointAsync(string host, int port, PolicySnapshot? lookupPolicy, bool useRelay, string relaySecret)
    {
        if (_client is not null)
        {
            await _client.DisposeAsync();
            _client = null;
        }

        if (lookupPolicy is not null)
        {
            _policy = lookupPolicy;
            ApplyPolicyRuntime();
        }

        _lastResolvedHost = host;
        _lastResolvedPort = port;
        _client = new StudentSocketClient(
            _studentCodeText.Text.Trim(),
            _studentNameText.Text.Trim(),
            _windowsUserText.Text.Trim(),
            _sessionIdText.Text.Trim(),
            _tokenText.Text.Trim(),
            policy => BeginInvoke(() =>
            {
                _policy = policy;
                Log($"Đã nhận quy định: {policy.BlockedProcesses.Count} tiến trình, {policy.BlockedWindowKeywords.Count} từ khóa, {policy.AllowedWebsiteHosts.Count} website cho phép.");
                ApplyPolicyRuntime();
            }),
            HandleTeacherFrameAsync,
            message => BeginInvoke(() => Log(message)),
            () => BeginInvoke(MarkConnected),
            message => BeginInvoke(() => HandleConnectionClosed(message)),
            useRelay,
            relaySecret);

        SetConnectionStatus("Đang kết nối...");
        SetScreenStatus("Đang chờ");
        await _client.ConnectAsync(host, port);
        _sessionSummaryLabel.Text = $"Phiên {_sessionIdText.Text.Trim()}";
        ApplyPolicyRuntime();
        StartTimers();
        await _client.SendWebcamDevicesAsync(_webcamCapture.ProbeDevices());
        if (_policy.WebcamEnabled && _policy.WebcamSnapshotOnConnect)
        {
            await SendWebcamAsync();
        }
    }

    private void StartReconnectLoop()
    {
        if (Interlocked.Exchange(ref _reconnectLoopStarted, 1) == 1)
        {
            return;
        }

        _reconnectCts?.Cancel();
        _reconnectCts?.Dispose();
        _reconnectCts = new CancellationTokenSource();
        _ = Task.Run(() => ReconnectLoopAsync(_reconnectCts.Token));
    }

    private void CancelReconnectLoop()
    {
        _reconnectCts?.Cancel();
        _reconnectCts?.Dispose();
        _reconnectCts = null;
        Interlocked.Exchange(ref _reconnectLoopStarted, 0);
    }

    private async Task ReconnectLoopAsync(CancellationToken cancellationToken)
    {
        int attempt = 0;
        try
        {
            while (!cancellationToken.IsCancellationRequested && !_manualDisconnectRequested && !_isClosing)
            {
                attempt++;
                BeginInvoke(() => SetConnectionStatus($"Đang thử kết nối lại lần {attempt}..."));
                TimeSpan delay = TimeSpan.FromSeconds(Math.Min(12, 2 + attempt));
                await Task.Delay(delay, cancellationToken);

                try
                {
                    (string host, int port, PolicySnapshot? lookupPolicy, bool useRelay, string relaySecret) = await ResolveTeacherEndpointAsync();
                    await ConnectToResolvedEndpointAsync(host, port, lookupPolicy, useRelay, relaySecret);
                    BeginInvoke(() => Log("Đã khôi phục kết nối với giáo viên."));
                    return;
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    BeginInvoke(() => Log($"Kết nối lại chưa thành công lần {attempt}: {ex.Message}"));
                }
            }
        }
        finally
        {
            Interlocked.Exchange(ref _reconnectLoopStarted, 0);
        }
    }

    private bool CanStartSubmission()
    {
        if (_client is null || !_client.IsConnected)
        {
            Log("Hãy kết nối với máy giáo viên trước khi nộp bài.");
            return false;
        }

        if (!CanSubmitNow())
        {
            SetSubmissionStatus("Đã khóa");
            Log("Đã hết thời gian làm bài. Giáo viên chưa mở cho phép nộp muộn.");
            return false;
        }

        return true;
    }

    private bool CanSubmitNow()
    {
        if (_policy.AllowSubmissionAfterDeadline)
        {
            return true;
        }

        DateTimeOffset? deadline = GetExamDeadline();
        return deadline is null || DateTimeOffset.UtcNow <= deadline.Value;
    }

    private DateTimeOffset? GetExamDeadline()
    {
        if (DateTimeOffset.TryParse(_policy.ExamEndAtUtc, out DateTimeOffset parsedEnd))
        {
            return parsedEnd.ToUniversalTime();
        }

        if (!DateTimeOffset.TryParse(_policy.StartedAtUtc, out DateTimeOffset parsedStart) || _policy.ExamDurationMinutes <= 0)
        {
            return null;
        }

        return parsedStart.ToUniversalTime().AddMinutes(_policy.ExamDurationMinutes);
    }

    private string BuildExamWindowText()
    {
        DateTimeOffset? deadline = GetExamDeadline();
        if (deadline is null)
        {
            return _policy.AllowSubmissionAfterDeadline ? "Thời gian: giáo viên cho nộp tự do" : "Thời gian: chưa giới hạn";
        }

        TimeSpan remaining = deadline.Value - DateTimeOffset.UtcNow;
        if (remaining <= TimeSpan.Zero)
        {
            return _policy.AllowSubmissionAfterDeadline ? "Thời gian: đã hết, vẫn cho nộp" : "Thời gian: đã hết giờ";
        }

        return $"Còn {remaining:hh\\:mm\\:ss}";
    }

    private async Task SubmitPathAsync(string path, string successMessage)
    {
        _submissionProgress.Value = 0;
        SetSubmissionStatus("Đang gửi");
        await _client!.SendSubmissionAsync(path, new Progress<int>(value => _submissionProgress.Value = Math.Clamp(value, 0, 100)), CancellationToken.None);
        _submissionProgress.Value = 100;
        SetSubmissionStatus("Đã gửi xong");
        Log($"{successMessage} {path}");
    }

    private string CreateSubmissionZipPath()
    {
        string safeSession = SanitizeFileName(_sessionIdText.Text.Trim());
        string safeStudent = SanitizeFileName(_studentCodeText.Text.Trim());
        string zipPath = Path.Combine(Path.GetTempPath(), $"{safeSession}_{safeStudent}_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        return zipPath;
    }

    private static string SanitizeFileName(string value)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(c, '_');
        }

        return string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();
    }

    private void SetWebcamStatus(string message)
    {
        _webcamStatusLabel.Text = message;
        if (!string.Equals(_lastWebcamStatusMessage, message, StringComparison.Ordinal))
        {
            _lastWebcamStatusMessage = message;
            Log($"Webcam: {message}");
        }
    }
}
