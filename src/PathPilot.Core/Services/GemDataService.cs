using System.Text.Json;
using PathPilot.Core.Models;

namespace PathPilot.Core.Services;

public class GemDataService
{
    private Dictionary<string, GemAcquisitionInfo>? _gemDatabase;
    private bool _isLoaded = false;

    public void LoadDatabase()
    {
        try
        {
            string jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "gems-database.json");
            
            Console.WriteLine($"Looking for gem database at: {jsonPath}");
            
            if (!File.Exists(jsonPath))
            {
                Console.WriteLine($"Gem database not found!");
                _gemDatabase = new Dictionary<string, GemAcquisitionInfo>();
                return;
            }

            string json = File.ReadAllText(jsonPath);
            var data = JsonSerializer.Deserialize<Dictionary<string, GemAcquisitionInfo>>(json);
            
            _gemDatabase = data ?? new Dictionary<string, GemAcquisitionInfo>();
            _isLoaded = true;
            
            Console.WriteLine($"Loaded {_gemDatabase.Count} gems from database");
            Console.WriteLine($"First 5 gem names:");
            foreach (var key in _gemDatabase.Keys.Take(5))
            {
                Console.WriteLine($"  - {key}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading gem database: {ex.Message}");
            _gemDatabase = new Dictionary<string, GemAcquisitionInfo>();
        }
    }

    public GemAcquisitionInfo? GetGemInfo(string gemName)
    {
        if (!_isLoaded) LoadDatabase();
        if (_gemDatabase == null) return null;

        // Try exact match first
        if (_gemDatabase.TryGetValue(gemName, out var info))
            return info;

        // Try with " Support" suffix
        if (_gemDatabase.TryGetValue(gemName + " Support", out info))
            return info;

        // Try case-insensitive match
        var key = _gemDatabase.Keys.FirstOrDefault(k => 
            k.Equals(gemName, StringComparison.OrdinalIgnoreCase));
        
        if (key != null) return _gemDatabase[key];

        // Try case-insensitive with " Support"
        key = _gemDatabase.Keys.FirstOrDefault(k => 
            k.Equals(gemName + " Support", StringComparison.OrdinalIgnoreCase));
        
        return key != null ? _gemDatabase[key] : null;
    }

    public GemSource? GetEarliestSource(string gemName)
    {
        var info = GetGemInfo(gemName);
        return info?.Sources.OrderBy(s => s.Act).FirstOrDefault();
    }
}

public class GemAcquisitionInfo
{
    public string Name { get; set; } = "";
    public string Color { get; set; } = "White";  // NEW
    public List<GemSource> Sources { get; set; } = new();
}