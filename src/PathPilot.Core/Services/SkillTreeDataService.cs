using System.Text.Json;
using PathPilot.Core.Models;

namespace PathPilot.Core.Services;

/// <summary>
/// Service for loading and caching GGG Skill Tree data
/// </summary>
public class SkillTreeDataService
{
    private const string TREE_URL = "https://raw.githubusercontent.com/grindinggear/skilltree-export/master/data.json";
    private const int CACHE_DAYS = 7;

    private readonly HttpClient _httpClient;
    private readonly string _cacheDir;
    private readonly string _cachePath;

    private SkillTreeData? _treeData;
    private bool _isLoaded = false;

    public SkillTreeDataService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(60); // Large file

        _cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PathPilot", "tree-cache");
        _cachePath = Path.Combine(_cacheDir, "data.json");
    }

    /// <summary>
    /// Gets parsed tree data, downloading if needed
    /// </summary>
    public async Task<SkillTreeData?> GetTreeDataAsync()
    {
        if (_isLoaded && _treeData != null)
            return _treeData;

        try
        {
            var cachePath = await EnsureCacheAsync();
            _treeData = await ParseTreeDataAsync(cachePath);
            _isLoaded = true;
            Console.WriteLine($"Loaded skill tree: {_treeData.TotalNodes} nodes");
            return _treeData;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading skill tree: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets a single node by ID
    /// </summary>
    public async Task<PassiveNode?> GetNodeAsync(int nodeId)
    {
        var data = await GetTreeDataAsync();
        return data?.Nodes.TryGetValue(nodeId, out var node) == true ? node : null;
    }

    private async Task<string> EnsureCacheAsync()
    {
        Directory.CreateDirectory(_cacheDir);

        // Check cache freshness
        if (File.Exists(_cachePath))
        {
            var age = DateTime.Now - File.GetLastWriteTime(_cachePath);
            if (age < TimeSpan.FromDays(CACHE_DAYS))
            {
                Console.WriteLine("Using cached skill tree data");
                return _cachePath;
            }
        }

        // Download fresh
        Console.WriteLine("Downloading skill tree data from GGG...");
        await using var stream = await _httpClient.GetStreamAsync(TREE_URL);
        await using var fileStream = File.Create(_cachePath);
        await stream.CopyToAsync(fileStream);
        Console.WriteLine("Skill tree data cached");

        return _cachePath;
    }

    private async Task<SkillTreeData> ParseTreeDataAsync(string filePath)
    {
        var treeData = new SkillTreeData();

        await using var stream = File.OpenRead(filePath);
        using var doc = await JsonDocument.ParseAsync(stream, new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        });

        var root = doc.RootElement;

        // Parse nodes
        if (root.TryGetProperty("nodes", out var nodesElement))
        {
            foreach (var nodeProp in nodesElement.EnumerateObject())
            {
                if (int.TryParse(nodeProp.Name, out var nodeId))
                {
                    var node = ParseNode(nodeId, nodeProp.Value);
                    treeData.Nodes[nodeId] = node;
                }
            }
        }

        // Parse groups
        if (root.TryGetProperty("groups", out var groupsElement))
        {
            foreach (var groupProp in groupsElement.EnumerateObject())
            {
                if (int.TryParse(groupProp.Name, out var groupId))
                {
                    var group = ParseGroup(groupId, groupProp.Value);
                    treeData.Groups[groupId] = group;
                }
            }
        }

        return treeData;
    }

    private PassiveNode ParseNode(int id, JsonElement element)
    {
        var node = new PassiveNode { Id = id };

        if (element.TryGetProperty("name", out var name))
            node.Name = name.GetString() ?? "";

        if (element.TryGetProperty("stats", out var stats))
        {
            node.Stats = stats.EnumerateArray()
                .Select(s => s.GetString() ?? "")
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        // Connection handling - GGG JSON has string array
        if (element.TryGetProperty("out", out var outNodes))
        {
            node.ConnectedNodes = outNodes.EnumerateArray()
                .Select(n => n.ValueKind == JsonValueKind.Number
                    ? n.GetInt32()
                    : int.TryParse(n.GetString(), out var parsed) ? parsed : 0)
                .Where(n => n != 0)
                .ToList();
        }

        // Node classification
        node.IsKeystone = element.TryGetProperty("isKeystone", out var ks) && ks.GetBoolean();
        node.IsNotable = element.TryGetProperty("isNotable", out var nt) && nt.GetBoolean();
        node.IsJewelSocket = element.TryGetProperty("isJewelSocket", out var js) && js.GetBoolean();
        node.IsMastery = element.TryGetProperty("isMastery", out var ms) && ms.GetBoolean();

        // Position data
        if (element.TryGetProperty("group", out var group))
            node.Group = group.GetInt32();
        if (element.TryGetProperty("orbit", out var orbit))
            node.Orbit = orbit.GetInt32();
        if (element.TryGetProperty("orbitIndex", out var orbitIndex))
            node.OrbitIndex = orbitIndex.GetInt32();

        // Ascendancy
        if (element.TryGetProperty("ascendancyName", out var ascName))
        {
            node.AscendancyName = ascName.GetString();
            node.IsAscendancy = true;
        }

        return node;
    }

    private NodeGroup ParseGroup(int id, JsonElement element)
    {
        var group = new NodeGroup { Id = id };

        if (element.TryGetProperty("x", out var x))
            group.X = x.GetSingle();
        if (element.TryGetProperty("y", out var y))
            group.Y = y.GetSingle();
        if (element.TryGetProperty("isProxy", out var isProxy))
            group.IsProxy = isProxy.GetBoolean();

        if (element.TryGetProperty("nodes", out var nodes))
        {
            group.NodeIds = nodes.EnumerateArray()
                .Select(n => n.ValueKind == JsonValueKind.Number
                    ? n.GetInt32()
                    : int.TryParse(n.GetString(), out var parsed) ? parsed : 0)
                .Where(n => n != 0)
                .ToList();
        }

        return group;
    }
}
