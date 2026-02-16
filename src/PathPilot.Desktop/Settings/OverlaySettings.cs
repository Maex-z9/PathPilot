using Avalonia.Input;
using System;
using System.IO;
using System.Text.Json;

namespace PathPilot.Desktop.Settings;

public class OverlaySettings
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config", "PathPilot", "overlay-settings.json");

    public Key ToggleKey { get; set; } = Key.F11;
    public KeyModifiers ToggleModifiers { get; set; } = KeyModifiers.None;

    public Key InteractiveKey { get; set; } = Key.F11;
    public KeyModifiers InteractiveModifiers { get; set; } = KeyModifiers.Control;

    // Overlay position
    public double OverlayX { get; set; } = 10;
    public double OverlayY { get; set; } = 10;

    // PoE log file path for zone detection
    public string? PoeLogFilePath { get; set; }

    public static string? AutoDetectLogPath()
    {
        string[] candidates =
        {
            @"C:\Program Files (x86)\Grinding Gear Games\Path of Exile\logs\Client.txt",
            @"C:\Program Files (x86)\Steam\steamapps\common\Path of Exile\logs\Client.txt",
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
                return path;
        }

        return null;
    }

    public static OverlaySettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<OverlaySettings>(json) ?? new OverlaySettings();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load overlay settings: {ex.Message}");
        }
        return new OverlaySettings();
    }

    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save overlay settings: {ex.Message}");
        }
    }
}
