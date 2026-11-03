using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace PcTime;
public record UsageSlice(DateTimeOffset Start, DateTimeOffset End, string Name);
public sealed class UsageStore
{
    public string FilePath { get; }
    public List<UsageSlice> Slices { get; private set; } = new();
    public UsageStore(string path)
    {
        FilePath = path;
        if (!File.Exists(path)) return;
        try { Slices = JsonSerializer.Deserialize<List<UsageSlice>>(File.ReadAllText(path)) ?? new(); }
        catch (JsonException) { File.Copy(path, path + ".corrupt-" + DateTime.Now.ToString("yyyyMMddHHmmss")); }
    }
    public void Add(DateTimeOffset start, DateTimeOffset end, string name)
    {
        if (end <= start) return;
        if (Slices.Count > 0 && Slices[^1].Name == name && Slices[^1].End == start)
            Slices[^1] = Slices[^1] with { End = end };
        else Slices.Add(new(start, end, name));
    }
    public void TrimIdle(DateTimeOffset lastInput)
    {
        // Roll back the grace period so five idle minutes are not charged.
        for (int i = Slices.Count - 1; i >= 0; i--)
        {
            if (Slices[i].End <= lastInput) break;
            if (Slices[i].Start >= lastInput) Slices.RemoveAt(i);
            else { Slices[i] = Slices[i] with { End = lastInput }; break; }
        }
    }
    public Dictionary<string, double> ForDay(DateTime day)
    {
        var from = new DateTimeOffset(day.Date);
        var to = new DateTimeOffset(day.Date.AddDays(1));
        return Slices.Where(s => s.End > from && s.Start < to)
            .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Sum(s => ( (s.End < to ? s.End : to) - (s.Start > from ? s.Start : from)).TotalSeconds));
    }
    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath + ".tmp", JsonSerializer.Serialize(Slices));
        File.Move(FilePath + ".tmp", FilePath, true);
    }
}
internal static class Native
{
    [StructLayout(LayoutKind.Sequential)] private struct LastInput { public uint Size; public uint Time; }
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint id);
    [DllImport("user32.dll")] private static extern bool GetLastInputInfo(ref LastInput input);
    public static TimeSpan Idle()
    {
        var input = new LastInput { Size = (uint)Marshal.SizeOf<LastInput>() };
        if (!GetLastInputInfo(ref input)) return TimeSpan.Zero;
        return TimeSpan.FromMilliseconds(unchecked((uint)Environment.TickCount - input.Time));
    }
    public static string? Foreground()
    {
        var window = GetForegroundWindow();
        if (window == IntPtr.Zero) return null;
        GetWindowThreadProcessId(window, out var id);
        try { using var p = Process.GetProcessById((int)id); return p.ProcessName; }
        catch (ArgumentException) { return null; }
        catch (System.ComponentModel.Win32Exception) { return null; }
        catch (InvalidOperationException) { return null; }
    }
}
