using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using HustLogin.Services;
using Microsoft.Win32;
using Color = System.Windows.Media.Color;

namespace HustLogin;

public partial class MainWindow : Window
{
    private static readonly SolidColorBrush GreenBrush = new(Color.FromRgb(0x10, 0xB9, 0x81));
    private static readonly SolidColorBrush RedBrush = new(Color.FromRgb(0xEF, 0x44, 0x44));
    private static readonly SolidColorBrush OrangeBrush = new(Color.FromRgb(0xF5, 0x9E, 0x0B));

    private Icon? _iconOnline, _iconOffline, _iconChecking, _iconIdle;
    private readonly LoginService _loginService = new();
    private readonly DispatcherTimer _timer = new();
    private NotifyIcon? _notifyIcon;
    private int _countdown;
    private int _interval = 60;
    private bool _checking;
    private bool? _online;

    private static readonly string ConfPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "my.conf");
    private const string AutoStartKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AutoStartName = "HustNetworkLogin";

    public MainWindow()
    {
        InitializeComponent();
        GenerateIcons();
        Icon = IconToImageSource(_iconIdle!);
        SetupTrayIcon();
        LoadConfig();
        UpdateAutoStartCheck();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += Timer_Tick;
        _timer.Start();
        NetworkChange.NetworkAvailabilityChanged += OnNetworkChanged;
        _ = DoCheckAsync();
    }

    // ── icons ───────────────────────────────────────────────────
    private void GenerateIcons()
    {
        _iconOnline = RenderIcon(System.Drawing.Color.FromArgb(0x25, 0x63, 0xEB));
        _iconOffline = RenderIcon(System.Drawing.Color.FromArgb(0xEF, 0x44, 0x44));
        _iconChecking = RenderIcon(System.Drawing.Color.FromArgb(0xF5, 0x9E, 0x0B));
        _iconIdle = RenderIcon(System.Drawing.Color.FromArgb(0x94, 0xA3, 0xB8));
    }

    private static Icon RenderIcon(System.Drawing.Color bg)
    {
        var bmp = new Bitmap(64, 64);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using (var bgBrush = new SolidBrush(bg))
            g.FillEllipse(bgBrush, 2, 2, 60, 60);

        using var borderPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(30, 0, 0, 0), 2);
        g.DrawEllipse(borderPen, 2, 2, 60, 60);

        using var wp = new System.Drawing.Pen(System.Drawing.Color.White, 4)
        { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawArc(wp, 18, 20, 28, 32, 180, 180);
        using var wp2 = new System.Drawing.Pen(System.Drawing.Color.White, 3.5f)
        { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawArc(wp2, 23, 30, 18, 24, 180, 180);
        using var wp3 = new System.Drawing.Pen(System.Drawing.Color.White, 3)
        { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawArc(wp3, 28, 40, 8, 16, 180, 180);

        using var db = new SolidBrush(System.Drawing.Color.White);
        g.FillEllipse(db, 29, 52, 6, 6);

        return System.Drawing.Icon.FromHandle(bmp.GetHicon());
    }

    private static ImageSource IconToImageSource(Icon icon)
    {
        return Imaging.CreateBitmapSourceFromHIcon(
            icon.Handle, Int32Rect.Empty,
            System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
    }

    // ── config ─────────────────────────────────────────────────
    private void LoadConfig()
    {
        string[] paths = { ConfPath, "/etc/hust-network-login.conf", "/etc/hust-network-login/config" };
        foreach (var p in paths)
        {
            try
            {
                var lines = File.ReadAllLines(p);
                if (lines.Length >= 2)
                {
                    UsernameBox.Text = lines[0].Trim();
                    PasswordBox.Password = lines[1].Trim();
                    if (lines.Length >= 3 && int.TryParse(lines[2].Trim(), out var iv))
                        _interval = Math.Clamp(iv, 5, 86400);
                    break;
                }
            }
            catch { }
        }
        IntervalBox.Text = _interval.ToString();
        _countdown = _interval;
    }

    private void SaveConfig()
    {
        if (int.TryParse(IntervalBox.Text.Trim(), out var iv))
            _interval = Math.Clamp(iv, 5, 86400);
        _countdown = _interval;
        File.WriteAllText(ConfPath,
            $"{UsernameBox.Text.Trim()}\n{PasswordBox.Password.Trim()}\n{_interval}\n");
    }

    // ── auto-start ─────────────────────────────────────────────
    private void UpdateAutoStartCheck()
    {
        using var key = Registry.CurrentUser.OpenSubKey(AutoStartKey);
        AutoStartCheck.IsChecked = key?.GetValue(AutoStartName) != null;
    }

    private void AutoStart_Click(object sender, RoutedEventArgs e)
    {
        using var key = Registry.CurrentUser.OpenSubKey(AutoStartKey, true);
        if (key == null) return;

        if (AutoStartCheck.IsChecked == true)
        {
            var exePath = Environment.ProcessPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HustLogin.exe");
            key.SetValue(AutoStartName, $"\"{exePath}\"");
        }
        else
        {
            key.DeleteValue(AutoStartName, false);
        }
    }

    // ── tray ───────────────────────────────────────────────────
    private void SetupTrayIcon()
    {
        _notifyIcon = new NotifyIcon { Icon = _iconIdle, Visible = false, Text = "HUST 校园网" };
        _notifyIcon.DoubleClick += (_, _) => RestoreFromTray();
        var menu = new ContextMenuStrip();
        menu.Items.Add("显示窗口", null, (_, _) => RestoreFromTray());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出程序", null, (_, _) => Shutdown());
        _notifyIcon.ContextMenuStrip = menu;
    }

    private void UpdateTray()
    {
        if (_notifyIcon == null) return;
        var s = _online switch { true => "已连接", false => "未连接", null => "等待检测" };
        var t = _online == false ? "\n断线快速重试中(10s)" : "";
        _notifyIcon.Text = $"HUST 校园网 - {s}{t}";
        _notifyIcon.Icon = _online switch { true => _iconOnline, false => _iconOffline, null => _iconIdle };
    }

    private void RestoreFromTray() { Show(); WindowState = WindowState.Normal; Activate();
        if (_notifyIcon != null) _notifyIcon.Visible = false; }

    private void Shutdown()
    {
        _loginService.Dispose();
        _iconOnline?.Dispose(); _iconOffline?.Dispose(); _iconChecking?.Dispose(); _iconIdle?.Dispose();
        _notifyIcon?.Dispose();
        System.Windows.Application.Current.Shutdown();
    }

    // ── status updates ─────────────────────────────────────────
    private void UpdateStatus(bool online)
    {
        _online = online;
        if (online)
        {
            StatusDot.Fill = GreenBrush;
            StatusText.Text = "已连接";
            StatusText.Foreground = GreenBrush;
            Icon = IconToImageSource(_iconOnline!);
            TitleText.Text = "HUST 校园网 - 已连接";
        }
        else
        {
            StatusDot.Fill = RedBrush;
            StatusText.Text = "未连接";
            StatusText.Foreground = RedBrush;
            Icon = IconToImageSource(_iconOffline!);
            TitleText.Text = "HUST 校园网 - 未连接";
        }
        UpdateCountdownText();
        UpdateTray();
        AddLog(online ? "[OK] 连接成功" : "[FAIL] 连接失败");
    }

    private void UpdateChecking()
    {
        StatusDot.Fill = OrangeBrush;
        StatusText.Text = "检测中...";
        StatusText.Foreground = OrangeBrush;
        if (_notifyIcon != null) _notifyIcon.Icon = _iconChecking;
        UpdateTray();
    }

    private void UpdateCountdownText()
    {
        if (_countdown > 0)
            CountdownText.Text = _online == false
                ? $"快速重试: {_countdown}s"
                : $"下次: {_countdown}s";
        else
            CountdownText.Text = "检测中...";

        LastCheckText.Text = DateTime.Now.ToString("HH:mm:ss");
    }

    // ── log ────────────────────────────────────────────────────
    private void AddLog(string msg)
    {
        var entry = $"{DateTime.Now:HH:mm:ss} {msg}";
        LogList.Items.Insert(0, entry);
        while (LogList.Items.Count > 100)
            LogList.Items.RemoveAt(LogList.Items.Count - 1);
    }

    // ── login ──────────────────────────────────────────────────
    private async Task DoCheckAsync()
    {
        if (_checking) return;
        _checking = true;

        Dispatcher.Invoke(UpdateChecking);
        var u = UsernameBox.Text.Trim();
        var p = PasswordBox.Password.Trim();

        try
        {
            if (!string.IsNullOrEmpty(u) && !string.IsNullOrEmpty(p))
            {
                var ok = await _loginService.LoginAsync(u, p);
                Dispatcher.Invoke(() => UpdateStatus(ok));
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    StatusDot.Fill = RedBrush;
                    StatusText.Text = "请填写账号密码";
                    StatusText.Foreground = RedBrush;
                    UpdateTray();
                    AddLog("[WARN] 未填写账号密码");
                });
            }
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() =>
            {
                _online = false;
                StatusDot.Fill = RedBrush;
                StatusText.Text = $"错误: {ex.Message}";
                StatusText.Foreground = RedBrush;
                Icon = IconToImageSource(_iconOffline!);
                UpdateTray();
                AddLog($"[ERR] {ex.Message}");
            });
        }
        finally { _checking = false; }
    }

    // ── timer ──────────────────────────────────────────────────
    private void Timer_Tick(object? sender, EventArgs e)
    {
        _countdown--;
        Dispatcher.Invoke(UpdateCountdownText);
        if (_countdown <= 0)
        {
            _countdown = _online == false ? 10 : _interval;
            _ = DoCheckAsync();
        }
    }

    // ── network change detection ───────────────────────────────
    private void OnNetworkChanged(object? sender, NetworkAvailabilityEventArgs e)
    {
        if (e.IsAvailable)
        {
            AddLog("[INFO] 网络恢复，立即检测");
            _countdown = 3;
        }
        else
        {
            AddLog("[INFO] 网络断开");
            Dispatcher.Invoke(() =>
            {
                _online = false;
                StatusDot.Fill = RedBrush;
                StatusText.Text = "网络断开";
                StatusText.Foreground = RedBrush;
                Icon = IconToImageSource(_iconOffline!);
                UpdateTray();
            });
        }
    }

    // ── events ─────────────────────────────────────────────────
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        SaveConfig();
        var orig = SaveBtn.Content;
        SaveBtn.Content = "已保存";
        _ = Task.Run(async () => { await Task.Delay(1500); Dispatcher.Invoke(() => SaveBtn.Content = orig); });
    }

    private void Check_Click(object sender, RoutedEventArgs e) { _countdown = _interval; _ = DoCheckAsync(); }

    private void Interval_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !int.TryParse(e.Text, out _);
    }

    private void Min_Click(object sender, RoutedEventArgs e) { Hide();
        if (_notifyIcon != null) _notifyIcon.Visible = true; }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized) { Hide();
            if (_notifyIcon != null) _notifyIcon.Visible = true; }
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    { if (e.ChangedButton == MouseButton.Left) DragMove(); }

    private void Close_Click(object sender, RoutedEventArgs e) { Hide();
        if (_notifyIcon != null) _notifyIcon.Visible = true; }
}
