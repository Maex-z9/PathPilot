using System.Text.Json;
using PathPilot.Core.Models;

namespace PathPilot.Core.Services;

/// <summary>
/// Service for loading and caching PoE 1 Skill Tree data (from poe-tool-dev community repo)
/// </summary>
public class SkillTreeDataService
{
    // PoE 1 tree data (3.25.0) - GGG's official repo only has PoE 2 data now
    private const string TREE_URL = "https://raw.githubusercontent.com/poe-tool-dev/passive-skill-tree-json/master/3.25.0/SkillTree.json";
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
        _cachePath = Path.Combine(_cacheDir, "poe1-3.25.json");
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
        Console.WriteLine("Downloading PoE 1 skill tree data (3.25.0)...");
        await using var stream = await _httpClient.GetStreamAsync(TREE_URL);
        await using var fileStream = File.Create(_cachePath);
        await stream.CopyToAsync(fileStream);
        Console.WriteLine("PoE 1 skill tree data cached");

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

        // Parse imageZoomLevels
        if (root.TryGetProperty("imageZoomLevels", out var zoomLevels))
        {
            treeData.ImageZoomLevels = zoomLevels.EnumerateArray()
                .Select(z => z.GetSingle())
                .ToList();
        }

        // Parse sprites section
        if (root.TryGetProperty("sprites", out var spritesElement))
        {
            ParseSprites(spritesElement, treeData);
        }

        // Log sprite parsing results
        Console.WriteLine($"Sprite parsing complete:");
        Console.WriteLine($"  Sprite types: {treeData.SpriteSheets.Count}");
        foreach (var (spriteType, zoomDict) in treeData.SpriteSheets)
        {
            Console.WriteLine($"    {spriteType}: {zoomDict.Count} zoom levels");
        }

        // Count nodes with icons and groups with backgrounds
        var nodesWithIcons = treeData.Nodes.Values.Count(n => !string.IsNullOrEmpty(n.Icon));
        var groupsWithBackgrounds = treeData.Groups.Values.Count(g => g.Background != null);
        Console.WriteLine($"  Nodes with icons: {nodesWithIcons}");
        Console.WriteLine($"  Groups with backgrounds: {groupsWithBackgrounds}");

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

        // Icon sprite key
        if (element.TryGetProperty("icon", out var icon))
            node.Icon = icon.GetString();

        // Mastery effects (selectable stat bonuses on mastery nodes)
        if (element.TryGetProperty("masteryEffects", out var effects))
        {
            foreach (var effectElement in effects.EnumerateArray())
            {
                var me = new MasteryEffect();
                if (effectElement.TryGetProperty("effect", out var effectId))
                    me.EffectId = effectId.GetInt32();
                if (effectElement.TryGetProperty("stats", out var effectStats))
                {
                    me.Stats = effectStats.EnumerateArray()
                        .Select(s => s.GetString() ?? "")
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                }
                node.MasteryEffects.Add(me);
            }
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

        // Background
        if (element.TryGetProperty("background", out var bgElement))
        {
            group.Background = new GroupBackground
            {
                Image = bgElement.GetProperty("image").GetString() ?? "",
                IsHalfImage = bgElement.TryGetProperty("isHalfImage", out var half) && half.GetBoolean()
            };
        }

        return group;
    }

    private void ParseSprites(JsonElement spritesElement, SkillTreeData treeData)
    {
        var spriteTypes = new[]
        {
            "normalActive", "normalInactive",
            "notableActive", "notableInactive",
            "keystoneActive", "keystoneInactive",
            "mastery", "masteryConnected", "masteryActiveSelected",
            "masteryInactive", "masteryActiveEffect",
            "frame", "jewel", "groupBackground"
        };

        foreach (var spriteType in spriteTypes)
        {
            if (!spritesElement.TryGetProperty(spriteType, out var typeElement))
                continue;

            var zoomDict = new Dictionary<string, SpriteSheetData>();

            foreach (var zoomProp in typeElement.EnumerateObject())
            {
                var zoomKey = zoomProp.Name; // e.g. "0.3835"
                var sheetElement = zoomProp.Value;

                var sheetData = new SpriteSheetData();

                if (sheetElement.TryGetProperty("filename", out var filename))
                    sheetData.Filename = filename.GetString() ?? "";

                if (sheetElement.TryGetProperty("w", out var w))
                    sheetData.SheetWidth = w.GetInt32();

                if (sheetElement.TryGetProperty("h", out var h))
                    sheetData.SheetHeight = h.GetInt32();

                // Parse coords
                if (sheetElement.TryGetProperty("coords", out var coordsElement))
                {
                    foreach (var coordProp in coordsElement.EnumerateObject())
                    {
                        var coordKey = coordProp.Name; // e.g. "Art/2DArt/SkillIcons/passives/2handeddamage.png"
                        var coordElement = coordProp.Value;

                        var coord = new SpriteCoordinate();

                        if (coordElement.TryGetProperty("x", out var x))
                            coord.X = x.GetInt32();
                        if (coordElement.TryGetProperty("y", out var y))
                            coord.Y = y.GetInt32();
                        if (coordElement.TryGetProperty("w", out var cw))
                            coord.W = cw.GetInt32();
                        if (coordElement.TryGetProperty("h", out var ch))
                            coord.H = ch.GetInt32();

                        sheetData.Coords[coordKey] = coord;
                    }
                }

                zoomDict[zoomKey] = sheetData;
            }

            treeData.SpriteSheets[spriteType] = zoomDict;
        }
    }
}
