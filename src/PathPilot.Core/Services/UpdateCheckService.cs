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
    /// </summary>
    public async Task<(bool HasUpdate, string? NewVersion, string? ReleaseUrl, bool Error)> CheckForUpdateAsync(string currentVersion)
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
                return (false, null, null, false);

            var remoteVersionStr = tagName.TrimStart('v');

            if (!Version.TryParse(remoteVersionStr, out var remoteVersion) ||
                !Version.TryParse(currentVersion, out var localVersion))
                return (false, null, null, false);

            if (remoteVersion > localVersion)
                return (true, remoteVersionStr, htmlUrl, false);

            return (false, null, null, false);
        }
        catch
        {
            return (false, null, null, true);
        }
    }
}
