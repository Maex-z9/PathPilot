using System.Collections.Concurrent;

namespace PathPilot.Core.Services;

public class GemIconService : IDisposable
{
    private const int CACHE_DAYS = 30;

    private readonly HttpClient _httpClient;
    private readonly string _cacheDir;
    private readonly ConcurrentDictionary<string, Task<string?>> _downloadTasks = new();

    public GemIconService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("PathPilot/1.0");

        _cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PathPilot", "gem-icons");
        Directory.CreateDirectory(_cacheDir);
    }

    public async Task<string?> GetIconPathAsync(string gemName, string? iconUrl)
    {
        if (string.IsNullOrEmpty(iconUrl))
            return null;

        var safeFileName = GetSafeFileName(gemName) + ".png";
        var cachePath = Path.Combine(_cacheDir, safeFileName);

        // Check if cached and fresh
        if (File.Exists(cachePath))
        {
            var age = DateTime.Now - File.GetLastWriteTime(cachePath);
            if (age < TimeSpan.FromDays(CACHE_DAYS))
                return cachePath;
        }

        // Deduplicate parallel downloads for same gem
        var task = _downloadTasks.GetOrAdd(gemName, _ => DownloadIconAsync(iconUrl, cachePath));

        try
        {
            return await task;
        }
        finally
        {
            _downloadTasks.TryRemove(gemName, out _);
        }
    }

    private async Task<string?> DownloadIconAsync(string iconUrl, string cachePath)
    {
        try
        {
            using var response = await _httpClient.GetAsync(iconUrl);

            if (!response.IsSuccessStatusCode)
                return null;

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
            if (!contentType.StartsWith("image/"))
                return null;

            var bytes = await response.Content.ReadAsByteArrayAsync();
            if (bytes.Length == 0)
                return null;

            await File.WriteAllBytesAsync(cachePath, bytes);
            return cachePath;
        }
        catch
        {
            return null;
        }
    }

    private static string GetSafeFileName(string gemName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(gemName.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return safe;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
