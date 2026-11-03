using System;
using System.IO;
using PcTime;
var path = Path.Combine(Path.GetTempPath(), "PcTime-check-" + Guid.NewGuid() + ".json");
void Check(bool value, string label) { if (!value) throw new Exception(label); Console.WriteLine("PASS " + label); }
try
{
    var store = new UsageStore(path);
    var start = new DateTimeOffset(new DateTime(2026, 11, 3, 23, 59, 0));
    store.Add(start, start.AddSeconds(30), "game");
    store.Add(start.AddSeconds(30), start.AddSeconds(120), "game");
    Check(store.Slices.Count == 1, "Merge contiguous samples");
    Check(store.ForDay(start.LocalDateTime)["game"] == 60, "Split before midnight");
    Check(store.ForDay(start.LocalDateTime.AddDays(1))["game"] == 60, "Split after midnight");
    store.Add(start.AddSeconds(120), start.AddSeconds(180), "editor");
    store.TrimIdle(start.AddSeconds(90));
    Check(store.Slices.Count == 1 && store.Slices[0].End == start.AddSeconds(90), "Idle rollback across application switch");
    store.Save();
    var loaded = new UsageStore(path);
    Check(loaded.Slices.Count == 1 && loaded.Slices[0] == store.Slices[0], "Save and reload");
    loaded.Add(start, start, "invalid");
    Check(loaded.Slices.Count == 1, "Ignore empty interval");
    var preferences = new Preferences { IdleMinutes = 10, KeepCounting = true };
    preferences.Save(path);
    var restored = Preferences.Load(path);
    Check(restored.IdleMinutes == 10 && restored.KeepCounting, "Restore saved idle and media preferences");
    preferences.AppGroups["code"] = "Work";
    preferences.AppGroups["game"] = "Game";
    preferences.Save(path);
    restored = Preferences.Load(path);
    Check(restored.GroupFor("CODE") == "Work" && restored.GroupFor("game") == "Game", "Persist groups case-insensitively");
    Check(restored.GroupFor("unknown") == "Khác", "Unassigned apps default to Other");
    var usage = new System.Collections.Generic.Dictionary<string, double> { ["code"] = 60, ["game"] = 120, ["unknown"] = 30 };
    var totals = restored.GroupTotals(usage);
    Check(totals["Work"] == 60 && totals["Game"] == 120 && totals["Khác"] == 30 && totals["Giải trí"] == 0, "Aggregate all four groups without overlap");
    restored.AppGroups["game"] = "Giải trí";
    totals = restored.GroupTotals(usage);
    Check(totals["Game"] == 0 && totals["Giải trí"] == 120, "Reclassification applies to existing usage");
    restored.AppGroups["code"] = "invalid";
    Check(restored.GroupFor("code") == "Khác", "Invalid group falls back to Other");
    int changes = 0;
    var row = new AppUsageRow("code", "VS Code", "Khác", (key, group) => { restored.AppGroups[key] = group; changes++; });
    row.Category = "Work";
    row.Update("1 phút", 50);
    row.Update("2 phút", 60);
    Check(changes == 1 && row.Category == "Work" && row.Percent == 60, "Live refresh preserves selected group without extra saves");
    row.Category = "invalid";
    Check(row.Category == "Work" && changes == 1, "UI row rejects invalid groups");
    File.WriteAllText(path, "{\"IdleMinutes\":-2}");
    Check(Preferences.Load(path).IdleMinutes == 5, "Reject invalid idle threshold");
    File.WriteAllText(path, "broken");
    Check(Preferences.Load(path).IdleMinutes == 5, "Recover malformed preferences");
}
finally { if (File.Exists(path)) File.Delete(path); }
