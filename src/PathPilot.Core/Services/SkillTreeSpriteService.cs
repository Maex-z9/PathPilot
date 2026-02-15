using System.Collections.Concurrent;
using PathPilot.Core.Models;
using SkiaSharp;

namespace PathPilot.Core.Services;

/// <summary>
/// Service for downloading, caching, and managing skill tree sprite sheet bitmaps
/// </summary>
public class SkillTreeSpriteService : IDisposable
{
    private const int CACHE_DAYS = 30;

    private readonly HttpClient _httpClient;
    private readonly string _cacheDir;

    // In-memory cache - service OWNS these bitmaps, consumers must NEVER dispose
    private readonly Dictionary<string, SKBitmap> _loadedBitmaps = new();

    // Download deduplication
    private readonly ConcurrentDictionary<string, Task<SKBitmap?>> _downloadTasks = new();

    public SkillTreeSpriteService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("PathPilot/1.0");

        _cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PathPilot", "tree-sprites");
        Directory.CreateDirectory(_cacheDir);
    }

    /// <summary>
    /// Synchronously gets an already-loaded sprite sheet from in-memory cache.
    /// Returns null if not yet loaded. Use this in render loops to avoid blocking.
    /// </summary>
    public SKBitmap? TryGetLoadedBitmap(string fullUrl)
    {
        if (string.IsNullOrEmpty(fullUrl))
            return null;

        var filename = ExtractFilename(fullUrl);
        if (string.IsNullOrEmpty(filename))
            return null;

        lock (_loadedBitmaps)
        {
            _loadedBitmaps.TryGetValue(filename, out var cached);
            return cached;
        }
    }

    /// <summary>
    /// Gets a sprite sheet bitmap from cache or downloads it
    /// </summary>
    /// <param name="fullUrl">Full URL from JSON (e.g. https://web.poecdn.com/image/passive-skill/skills-0.jpg?511ee3db)</param>
    /// <returns>SKBitmap owned by this service, or null on error</returns>
    public async Task<SKBitmap?> GetSpriteSheetAsync(string fullUrl)
    {
        if (string.IsNullOrEmpty(fullUrl))
            return null;

        // Extract filename for cache key (everything after last /, strip query string)
        var filename = ExtractFilename(fullUrl);
        if (string.IsNullOrEmpty(filename))
            return null;

        // Fast path: already loaded in memory
        lock (_loadedBitmaps)
        {
            if (_loadedBitmaps.TryGetValue(filename, out var cached))
                return cached;
        }

        // Deduplicate parallel downloads
        var task = _downloadTasks.GetOrAdd(filename, _ => LoadSpriteSheetAsync(fullUrl, filename));

        try
        {
            return await task;
        }
        finally
        {
            _downloadTasks.TryRemove(filename, out _);
        }
    }

    /// <summary>
    /// Preloads all sprite sheets for a given zoom level
    /// </summary>
    public async Task PreloadSpriteSheetsAsync(SkillTreeData treeData, string zoomKey)
    {
        var urls = new HashSet<string>();

        // Collect all unique sprite sheet URLs for this zoom level
        foreach (var (spriteType, zoomDict) in treeData.SpriteSheets)
        {
            if (zoomDict.TryGetValue(zoomKey, out var sheetData))
            {
                if (!string.IsNullOrEmpty(sheetData.Filename))
                    urls.Add(sheetData.Filename);
            }
        }

        Console.WriteLine($"Preloading {urls.Count} sprite sheets for zoom level {zoomKey}...");

        // Download all in parallel
        var tasks = urls.Select(url => GetSpriteSheetAsync(url)).ToList();
        await Task.WhenAll(tasks);

        var loaded = tasks.Count(t => t.Result != null);
        Console.WriteLine($"Sprite sheets loaded: {loaded}/{urls.Count}");
    }

    private async Task<SKBitmap?> LoadSpriteSheetAsync(string fullUrl, string filename)
    {
        try
        {
            var cachePath = Path.Combine(_cacheDir, filename);

            // Check disk cache
            if (File.Exists(cachePath))
            {
                var age = DateTime.Now - File.GetLastWriteTime(cachePath);
                if (age < TimeSpan.FromDays(CACHE_DAYS))
                {
                    // Load from disk cache
                    using var stream = File.OpenRead(cachePath);
                    var bitmap = SKBitmap.Decode(stream);
                    if (bitmap != null)
                    {
                        lock (_loadedBitmaps)
                        {
                            _loadedBitmaps[filename] = bitmap;
                        }
                        return bitmap;
                    }
                }
            }

            // Download from web
            using var response = await _httpClient.GetAsync(fullUrl);
            if (!response.IsSuccessStatusCode)
                return null;

            var bytes = await response.Content.ReadAsByteArrayAsync();
            if (bytes.Length == 0)
                return null;

            // Save to disk cache
            await File.WriteAllBytesAsync(cachePath, bytes);

            // Decode to bitmap
            var downloadedBitmap = SKBitmap.Decode(bytes);
            if (downloadedBitmap != null)
            {
                lock (_loadedBitmaps)
                {
                    _loadedBitmaps[filename] = downloadedBitmap;
                }
            }

            return downloadedBitmap;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading sprite sheet {filename}: {ex.Message}");
            return null;
        }
    }

    private static string ExtractFilename(string fullUrl)
    {
        try
        {
            var uri = new Uri(fullUrl);
            var path = uri.AbsolutePath;
            var filename = Path.GetFileName(path);

            // Strip query string if present
            var queryIndex = filename.IndexOf('?');
            if (queryIndex >= 0)
                filename = filename.Substring(0, queryIndex);

            return filename;
        }
        catch
        {
            return string.Empty;
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();

        // Dispose all loaded bitmaps
        lock (_loadedBitmaps)
        {
            foreach (var bitmap in _loadedBitmaps.Values)
            {
                bitmap.Dispose();
            }
            _loadedBitmaps.Clear();
        }
    }
}
