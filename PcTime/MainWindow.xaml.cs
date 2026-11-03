using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Input;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace PcTime;
public partial class MainWindow : Window
{
    private readonly UsageStore store;
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly Forms.NotifyIcon tray;
    private DateTimeOffset previous = DateTimeOffset.Now;
    private DateTimeOffset trackingFloor = DateTimeOffset.Now;
    private string? previousApp;
    private bool paused, locked, suspended, quitting;
    private int ticks;
    private bool ready;
    private readonly bool preview = Array.Exists(Environment.GetCommandLineArgs(), a => a == "--preview");
    private readonly string preferencesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PcTime", "settings.json");
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private string currentPage = "Overview";
    private Preferences preferences = new();
    private readonly ObservableCollection<AppUsageRow> appRows = new();
    public MainWindow()
    {
        InitializeComponent();
        AppsGrid.ItemsSource = appRows;
        store = new UsageStore(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PcTime", "usage.json"));
        try
        {
            preferences = Preferences.Load(preferencesPath);
            foreach (ComboBoxItem item in IdleMinutes.Items)
                if (int.Parse((string)item.Tag) == preferences.IdleMinutes) IdleMinutes.SelectedItem = item;
            KeepCounting.IsChecked = preferences.KeepCounting;
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            StartupCheck.IsChecked = key?.GetValue("PcTime") != null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException) { SaveLabel.Text = "Không đọc được cài đặt: " + ex.Message; }
        ready = true;
        ReportDate.SelectedDate = DateTime.Today;
        if (preview)
        {
            tray = new Forms.NotifyIcon();
            PauseButton.IsEnabled = false;
            StartupCheck.IsEnabled = false;
            StatusLabel.Text = "Xem trước giao diện — không ghi thêm thời gian";
            SaveLabel.Text = "Chế độ xem trước • Đóng cửa sổ để thoát.";
            Closing += (_, _) => Application.Current.Shutdown();
            RefreshReport();
            return;
        }
        tray = new Forms.NotifyIcon { Icon = System.Drawing.SystemIcons.Application, Text = "PC Time", Visible = true };
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Mở PC Time", null, (_, _) => Dispatcher.Invoke(Restore));
        menu.Items.Add("Tạm dừng / Tiếp tục", null, (_, _) => Dispatcher.Invoke(TogglePause));
        menu.Items.Add("Thoát", null, (_, _) => Dispatcher.Invoke(Quit));
        tray.ContextMenuStrip = menu;
        tray.DoubleClick += (_, _) => Dispatcher.Invoke(Restore);
        Closing += OnClosing;
        SystemEvents.SessionSwitch += SessionSwitch;
        SystemEvents.PowerModeChanged += PowerChanged;
        SystemEvents.SessionEnding += SessionEnding;
        timer.Tick += (_, _) => Tick();
        timer.Start();
        RefreshReport();
        if (Array.Exists(Environment.GetCommandLineArgs(), a => a == "--tray"))
            Loaded += (_, _) => Hide();
    }
    private void Tick()
    {
        var now = DateTimeOffset.Now;
        var elapsed = now - previous;
        var idle = Native.Idle();
        bool exempt = KeepCounting.IsChecked == true;
        bool away = !exempt && idle.TotalMinutes >= IdleThreshold;
        if (!paused && !locked && !suspended && elapsed.TotalSeconds is > 0 and < 5)
        {
            if (away) store.TrimIdle(now - idle > trackingFloor ? now - idle : trackingFloor);
            else if (previousApp != null) store.Add(previous, now, previousApp);
        }
        if (exempt || paused || locked || suspended || elapsed.TotalSeconds >= 5) trackingFloor = now;
        previousApp = Native.Foreground();
        previous = now;
        StatusLabel.Text = paused ? "Đã tạm dừng" : locked || suspended ? "Máy đang khóa / nghỉ" : away ? "Đang nghỉ — không tính thời gian" : "Đang dùng: " + (previousApp ?? "chưa xác định");
        if (++ticks % 15 == 0) Save();
        RefreshReport();
    }
    private int IdleThreshold => IdleMinutes.SelectedItem is ComboBoxItem item ? int.Parse((string)item.Tag) : 5;
    private void RefreshReport()
    {
        if (store == null) return;
        var data = store.ForDay(ReportDate.SelectedDate ?? DateTime.Today);
        var total = data.Values.Sum();
        var duration = TimeSpan.FromSeconds(total);
        TotalLabel.Text = $"{(int)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}";
        TopAppLabel.Text = data.Count > 0 ? FriendlyName(data.OrderByDescending(x => x.Value).First().Key) : "Chưa có dữ liệu";
        AppCountLabel.Text = data.Count + " ứng dụng đã sử dụng";
        foreach (var row in appRows.Where(r => !data.ContainsKey(r.Key)).ToList()) appRows.Remove(row);
        foreach (var app in data.OrderByDescending(x => x.Value))
        {
            var row = appRows.FirstOrDefault(r => r.Key == app.Key);
            if (row == null)
            {
                row = new AppUsageRow(app.Key, FriendlyName(app.Key), preferences.GroupFor(app.Key), ChangeGroup);
                appRows.Add(row);
            }
            row.Update(Format(app.Value), total > 0 ? app.Value / total * 100 : 0);
        }
        RefreshGroupTotals();
        if (currentPage == "History" && ticks % 15 == 0) RefreshHistory();
    }
    private static string Format(double seconds) { var t = TimeSpan.FromSeconds(seconds); return $"{(int)t.TotalHours} giờ {t.Minutes:00} phút {t.Seconds:00} giây"; }
    private void Save()
    {
        try { store.Save(); SaveLabel.Text = "Đã lưu " + DateTime.Now.ToString("HH:mm:ss") + " • Đóng cửa sổ để thu xuống khay hệ thống."; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { SaveLabel.Text = "Chưa lưu được dữ liệu: " + ex.Message; }
    }
    private void TogglePause() { paused = !paused; previous = trackingFloor = DateTimeOffset.Now; previousApp = null; PauseButton.Content = paused ? "Tiếp tục" : "Tạm dừng"; Save(); }
    private void Pause_Click(object sender, RoutedEventArgs e) => TogglePause();
    private void Date_Changed(object sender, SelectionChangedEventArgs e) => RefreshReport();
    private void Idle_Changed(object sender, SelectionChangedEventArgs e) => SavePreferences();
    private void Settings_Changed(object sender, RoutedEventArgs e) => SavePreferences();
    private void SavePreferences()
    {
        if (!ready || preview) return;
        preferences.IdleMinutes = IdleThreshold;
        preferences.KeepCounting = KeepCounting.IsChecked == true;
        try { preferences.Save(preferencesPath); SaveLabel.Text = "Đã lưu cài đặt và phân nhóm ứng dụng."; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { SaveLabel.Text = "Không lưu được cài đặt: " + ex.Message; }
    }
    private void ChangeGroup(string process, string group)
    {
        preferences.AppGroups[process] = group;
        SavePreferences();
        RefreshGroupTotals();
        if (preview) SaveLabel.Text = "Xem trước: thay đổi nhóm chỉ áp dụng tạm, không lưu.";
    }
    private void RefreshGroupTotals()
    {
        var totals = preferences.GroupTotals(store.ForDay(ReportDate.SelectedDate ?? DateTime.Today));
        WorkTotal.Text = FormatGroup(totals["Work"]);
        GameTotal.Text = FormatGroup(totals["Game"]);
        EntertainmentTotal.Text = FormatGroup(totals["Giải trí"]);
        OtherTotal.Text = FormatGroup(totals["Khác"]);
    }
    private static string FormatGroup(double seconds)
    { var time = TimeSpan.FromSeconds(seconds); return $"{(int)time.TotalHours}h {time.Minutes:00}m {time.Seconds:00}s"; }
    private void Startup_Changed(object sender, RoutedEventArgs e)
    {
        if (!ready || preview) return;
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey);
            if (StartupCheck.IsChecked == true) key.SetValue("PcTime", $"\"{Environment.ProcessPath}\" --tray");
            else key.DeleteValue("PcTime", false);
            SaveLabel.Text = "Đã cập nhật tùy chọn khởi động cùng Windows.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            ready = false; StartupCheck.IsChecked = !StartupCheck.IsChecked; ready = true;
            SaveLabel.Text = "Không thay đổi được khởi động: " + ex.Message;
        }
    }
    private static string FriendlyName(string process) => process.ToLowerInvariant() switch
    {
        "chrome" => "Google Chrome", "msedge" => "Microsoft Edge", "brave" => "Brave", "code" => "Visual Studio Code",
        "explorer" => "File Explorer", "discord" => "Discord", "pctime" => "PC Time", "whatsapp.root" => "WhatsApp",
        "obs64" => "OBS Studio", "last epoch" => "Last Epoch", "pathofexile" => "Path of Exile", "pathofexile_x64" => "Path of Exile",
        _ => process
    };
    private sealed record DayRow(DateTime Day, string Label, string Duration, int Count);
    private void RefreshHistory()
    {
        HistoryGrid.ItemsSource = Enumerable.Range(0, 14).Select(i =>
        {
            var day = DateTime.Today.AddDays(-i); var data = store.ForDay(day);
            return new DayRow(day, day.ToString("dd/MM/yyyy"), Format(data.Values.Sum()), data.Count);
        }).ToList();
    }
    private void Nav_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton button || ReportPanel == null) return;
        ShowPage((string)button.Tag);
    }
    private void ShowPage(string page)
    {
        currentPage = page;
        ReportPanel.Visibility = page is "Overview" or "Applications" ? Visibility.Visible : Visibility.Collapsed;
        SummaryPanel.Visibility = page == "Overview" ? Visibility.Visible : Visibility.Collapsed;
        HistoryPanel.Visibility = page == "History" ? Visibility.Visible : Visibility.Collapsed;
        SettingsPanel.Visibility = page == "Settings" ? Visibility.Visible : Visibility.Collapsed;
        ExportButton.Visibility = page is "Overview" or "Applications" ? Visibility.Visible : Visibility.Collapsed;
        PageTitle.Text = page switch { "Applications" => "Ứng dụng", "History" => "Lịch sử", "Settings" => "Cài đặt", _ => "Tổng quan" };
        PageSubtitle.Text = page switch { "Applications" => "Thời gian sử dụng từng ứng dụng và game.", "History" => "Nhìn lại nhịp sử dụng máy trong hai tuần.", "Settings" => "Điều chỉnh cách PC Time hoạt động.", _ => "Nhìn lại thời gian sử dụng máy của bạn." };
        if (store != null) RefreshReport();
        if (page == "History" && store != null) RefreshHistory();
    }
    private void History_Open(object sender, MouseButtonEventArgs e)
    {
        if (HistoryGrid.SelectedItem is not DayRow row) return;
        ReportDate.SelectedDate = row.Day;
        OverviewNav.IsChecked = true;
    }
    private void Restore() { Show(); WindowState = WindowState.Normal; Activate(); }
    private void OnClosing(object? sender, CancelEventArgs e) { if (!quitting) { e.Cancel = true; Hide(); Save(); } }
    private void SessionSwitch(object sender, SessionSwitchEventArgs e) => Dispatcher.Invoke(() => { if (e.Reason == SessionSwitchReason.SessionLock) locked = true; if (e.Reason == SessionSwitchReason.SessionUnlock) locked = false; previous = trackingFloor = DateTimeOffset.Now; previousApp = null; Save(); });
    private void PowerChanged(object sender, PowerModeChangedEventArgs e) => Dispatcher.Invoke(() => { if (e.Mode == PowerModes.Suspend) suspended = true; if (e.Mode == PowerModes.Resume) suspended = false; previous = trackingFloor = DateTimeOffset.Now; previousApp = null; Save(); });
    private void SessionEnding(object sender, SessionEndingEventArgs e) => Dispatcher.Invoke(Save);
    private void Quit()
    {
        timer.Stop(); Save(); quitting = true; tray.Dispose();
        SystemEvents.SessionSwitch -= SessionSwitch; SystemEvents.PowerModeChanged -= PowerChanged;
        SystemEvents.SessionEnding -= SessionEnding;
        Application.Current.Shutdown();
    }
    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var day = ReportDate.SelectedDate ?? DateTime.Today;
        var dialog = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = $"pc-time-{day:yyyy-MM-dd}.csv" };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var lines = store.ForDay(day).OrderByDescending(x => x.Value).Select(x => $"\"{x.Key.Replace("\"", "\"\"")}\",{preferences.GroupFor(x.Key)},{(long)x.Value}");
            File.WriteAllLines(dialog.FileName, new[] { "Application,Group,Seconds" }.Concat(lines), new UTF8Encoding(true));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { MessageBox.Show("Không xuất được: " + ex.Message); }
    }
}
