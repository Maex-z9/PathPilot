using System.Text.Json;
using System.Text.Json.Serialization;
using PathPilot.Core.Models;

namespace PathPilot.Core.Services;

/// <summary>
/// Handles saving and loading builds to/from disk
/// </summary>
public class BuildStorage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Gets the default builds directory
    /// </summary>
    public static string BuildsDirectory
    {
        get
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var buildsDir = Path.Combine(appData, "PathPilot", "Builds");
            Directory.CreateDirectory(buildsDir);
            return buildsDir;
        }
    }

    /// <summary>
    /// Saves a build to a JSON file
    /// </summary>
    public static void SaveBuild(Build build, string filePath)
    {
        var json = JsonSerializer.Serialize(build, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Saves a build to the default builds directory
    /// </summary>
    public static string SaveBuild(Build build)
    {
        var fileName = SanitizeFileName(build.Name) + ".json";
        var filePath = Path.Combine(BuildsDirectory, fileName);
        SaveBuild(build, filePath);
        return filePath;
    }

    /// <summary>
    /// Loads a build from a JSON file
    /// </summary>
    public static Build? LoadBuild(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<Build>(json, JsonOptions);
    }

    /// <summary>
    /// Gets all saved builds from the default directory
    /// </summary>
    public static List<SavedBuildInfo> GetSavedBuilds()
    {
        var builds = new List<SavedBuildInfo>();

        if (!Directory.Exists(BuildsDirectory))
            return builds;

        foreach (var file in Directory.GetFiles(BuildsDirectory, "*.json"))
        {
            try
            {
                var build = LoadBuild(file);
                if (build != null)
                {
                    builds.Add(new SavedBuildInfo
                    {
                        FilePath = file,
                        Name = build.Name,
                        Description = build.CharacterDescription,
                        LastModified = File.GetLastWriteTime(file)
                    });
                }
            }
            catch
            {
                // Skip invalid files
            }
        }

        return builds.OrderByDescending(b => b.LastModified).ToList();
    }

    /// <summary>
    /// Deletes a saved build
    /// </summary>
    public static void DeleteBuild(string filePath)
    {
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(sanitized) ? "Unnamed_Build" : sanitized;
    }
}

/// <summary>
/// Info about a saved build for listing
/// </summary>
public class SavedBuildInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
}
