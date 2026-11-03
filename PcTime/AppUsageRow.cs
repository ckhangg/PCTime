using System;
using System.ComponentModel;
namespace PcTime;
public sealed class AppUsageRow : INotifyPropertyChanged
{
    public string Key { get; }
    public string Name { get; }
    public string Process => Key + ".exe";
    public string Duration { get; private set; } = "";
    public double Percent { get; private set; }
    public string Share => $"{Percent / 100:P0}";
    public string[] Groups => Preferences.GroupNames;
    private string category;
    private readonly Action<string, string> changed;
    public string Category
    {
        get => category;
        set
        {
            if (value == category || Array.IndexOf(Groups, value) < 0) return;
            category = value;
            PropertyChanged?.Invoke(this, new(nameof(Category)));
            changed(Key, value);
        }
    }
    public AppUsageRow(string key, string name, string category, Action<string, string> changed)
    { Key = key; Name = name; this.category = category; this.changed = changed; }
    public void Update(string duration, double percent)
    {
        Duration = duration; Percent = percent;
        PropertyChanged?.Invoke(this, new(nameof(Duration)));
        PropertyChanged?.Invoke(this, new(nameof(Percent)));
        PropertyChanged?.Invoke(this, new(nameof(Share)));
    }
    public event PropertyChangedEventHandler? PropertyChanged;
}
