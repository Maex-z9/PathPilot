using System.Text.Json;

namespace PathPilot.Core.Services;

public class UpdateCheckService
{
    private readonly HttpClient _httpClient;

    public UpdateCheckService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("PathPilot");
    }

    /// <summary>
    /// Checks GitHub releases for a newer version.
    /// Returns installer download URL from release assets if available.
    /// </summary>
    public async Task<(bool HasUpdate, string? NewVersion, string? InstallerUrl, string? ReleaseUrl, bool Error)> CheckForUpdateAsync(string currentVersion)
    {
        try
        {
            var response = await _httpClient.GetAsync("https://api.github.com/repos/Maex-z9/PathPilot/releases/latest");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tagName = root.GetProperty("tag_name").GetString();
            var htmlUrl = root.GetProperty("html_url").GetString();

            if (string.IsNullOrEmpty(tagName))
                return (false, null, null, null, false);

            var remoteVersionStr = tagName.TrimStart('v');

            if (!Version.TryParse(remoteVersionStr, out var remoteVersion) ||
                !Version.TryParse(currentVersion, out var localVersion))
                return (false, null, null, null, false);

            if (remoteVersion > localVersion)
            {
                // Find installer .exe in release assets
                string? installerUrl = null;
                if (root.TryGetProperty("assets", out var assets))
                {
                    foreach (var asset in assets.EnumerateArray())
                    {
                        var name = asset.GetProperty("name").GetString();
                        if (name != null && name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
                            name.Contains("Setup", StringComparison.OrdinalIgnoreCase))
                        {
                            installerUrl = asset.GetProperty("browser_download_url").GetString();
                            break;
                        }
                    }
                }

                return (true, remoteVersionStr, installerUrl, htmlUrl, false);
            }

            return (false, null, null, null, false);
        }
        catch
        {
            return (false, null, null, null, true);
        }
    }

    /// <summary>
    /// Downloads a file to a temporary path with progress reporting.
    /// </summary>
    public async Task<string?> DownloadInstallerAsync(string url, IProgress<int>? progress = null)
    {
        try
        {
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1;
            var tempPath = Path.Combine(Path.GetTempPath(), $"PathPilot-Setup.exe");

            await using var contentStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[81920];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                totalRead += bytesRead;

                if (totalBytes > 0)
                    progress?.Report((int)(totalRead * 100 / totalBytes));
            }

            progress?.Report(100);
            return tempPath;
        }
        catch
        {
            return null;
        }
    }
}
