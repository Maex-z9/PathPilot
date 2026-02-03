namespace PathPilot.Core.Parsers;

/// <summary>
/// Imports Path of Building builds from pobb.in URLs
/// </summary>
public class PobUrlImporter
{
    private readonly HttpClient _httpClient;

    public PobUrlImporter(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Downloads and decodes a PoB build from a pobb.in URL
    /// </summary>
    /// <param name="url">pobb.in URL (e.g. https://pobb.in/abc123)</param>
    /// <returns>Decoded XML string</returns>
    public async Task<string> ImportFromUrlAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL cannot be empty", nameof(url));
        }

        // Validate URL format
        if (!IsValidPobbUrl(url))
        {
            throw new ArgumentException("Invalid pobb.in URL format. Expected: https://pobb.in/CODE or https://pastebin.com/CODE", nameof(url));
        }

        try
        {
            // Extract the paste code from URL
            string code = ExtractPasteCode(url);
            
            // Build the raw content URL
            string rawUrl = GetRawContentUrl(url, code);
            
            // Download the paste content
            string pasteContent = await _httpClient.GetStringAsync(rawUrl);
            
            if (string.IsNullOrWhiteSpace(pasteContent))
            {
                throw new InvalidOperationException("Downloaded content is empty");
            }

            // Decode the PoB data
            string xml = PobDecoder.DecodeToXml(pasteContent);
            
            return xml;
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Failed to download from {url}. Check your internet connection.", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new InvalidOperationException("Request timed out. The server may be unavailable.", ex);
        }
    }

    /// <summary>
    /// Validates if URL is a valid pobb.in or pastebin URL
    /// </summary>
    public static bool IsValidPobbUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        try
        {
            var uri = new Uri(url);
            
            // Accept pobb.in and pastebin.com
            return uri.Host.ToLower() == "pobb.in" || 
                   uri.Host.ToLower() == "www.pobb.in" ||
                   uri.Host.ToLower() == "pastebin.com" ||
                   uri.Host.ToLower() == "www.pastebin.com";
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Extracts the paste code from a URL
    /// </summary>
    private static string ExtractPasteCode(string url)
    {
        var uri = new Uri(url);
        string path = uri.AbsolutePath.TrimStart('/');
        
        // Remove any query parameters
        int queryIndex = path.IndexOf('?');
        if (queryIndex >= 0)
        {
            path = path.Substring(0, queryIndex);
        }
        
        return path;
    }

    /// <summary>
    /// Constructs the raw content URL based on the service
    /// </summary>
    private static string GetRawContentUrl(string originalUrl, string code)
    {
        var uri = new Uri(originalUrl);
        
        if (uri.Host.ToLower().Contains("pobb.in"))
        {
            // pobb.in format: https://pobb.in/raw/CODE
            return $"https://pobb.in/raw/{code}";
        }
        else if (uri.Host.ToLower().Contains("pastebin.com"))
        {
            // pastebin format: https://pastebin.com/raw/CODE
            return $"https://pastebin.com/raw/{code}";
        }
        
        throw new NotSupportedException($"Unsupported URL host: {uri.Host}");
    }

    /// <summary>
    /// Downloads build from URL and parses it to a Build object
    /// </summary>
    public async Task<string> DownloadBuildXmlAsync(string url)
    {
        return await ImportFromUrlAsync(url);
    }
}
