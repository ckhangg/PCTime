using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
namespace PcTime;
public sealed class Preferences
{
    public int IdleMinutes { get; set; } = 5;
    public bool KeepCounting { get; set; }
    public static string[] GroupNames { get; } = { "Work", "Game", "Giải trí", "Khác" };
    public Dictionary<string, string> AppGroups { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string GroupFor(string process) => AppGroups.TryGetValue(process, out var group) && GroupNames.Contains(group) ? group : "Khác";
    public Dictionary<string, double> GroupTotals(Dictionary<string, double> usage)
    {
        var totals = GroupNames.ToDictionary(g => g, _ => 0d);
        foreach (var app in usage) totals[GroupFor(app.Key)] += app.Value;
        return totals;
    }
    public static Preferences Load(string path)
    {
        if (!File.Exists(path)) return new();
        try
        {
            var value = JsonSerializer.Deserialize<Preferences>(File.ReadAllText(path)) ?? new();
            if (value.IdleMinutes is not (1 or 5 or 10 or 15)) value.IdleMinutes = 5;
            value.AppGroups = new Dictionary<string, string>(value.AppGroups ?? new(), StringComparer.OrdinalIgnoreCase);
            return value;
        }
        catch (JsonException) { return new(); }
    }
    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(this));
        File.Move(path + ".tmp", path, true);
    }
}
