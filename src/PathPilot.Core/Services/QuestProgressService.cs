using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PathPilot.Core.Services;

public class QuestProgressService
{
    private static readonly string ProgressPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config", "PathPilot", "quest-progress.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public HashSet<string> LoadCompletedQuestIds()
    {
        try
        {
            if (File.Exists(ProgressPath))
            {
                var json = File.ReadAllText(ProgressPath);
                return JsonSerializer.Deserialize<HashSet<string>>(json, JsonOptions) ?? new HashSet<string>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load quest progress: {ex.Message}");
        }
        return new HashSet<string>();
    }

    public void SaveCompletedQuestIds(HashSet<string> completedIds)
    {
        try
        {
            var directory = Path.GetDirectoryName(ProgressPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(completedIds, JsonOptions);
            File.WriteAllText(ProgressPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save quest progress: {ex.Message}");
        }
    }
}
