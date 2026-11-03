using System;
using System.Threading;
using System.Windows;
namespace PcTime;
public partial class App : Application
{
    private Mutex? instance;
    protected override void OnStartup(StartupEventArgs e)
    {
        if (Array.Exists(e.Args, a => a == "--preview")) { base.OnStartup(e); return; }
        instance = new Mutex(true, "Local\\PcTime.PersonalTracker", out bool created);
        if (!created) { MessageBox.Show("PC Time đang chạy ở khay hệ thống."); Shutdown(); return; }
        base.OnStartup(e);
    }
    protected override void OnExit(ExitEventArgs e) { instance?.Dispose(); base.OnExit(e); }
}
