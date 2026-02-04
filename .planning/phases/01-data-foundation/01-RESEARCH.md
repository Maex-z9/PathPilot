# Phase 1: Data Foundation - Research

**Researched:** 2026-02-04
**Domain:** GGG Skill Tree JSON Parsing, Build Data Integration
**Confidence:** HIGH

## Summary

Phase 1 establishes the data foundation for PathPilot's interactive skill tree viewer by loading and parsing GGG's official Skill Tree JSON (~5.5MB, 3281 nodes) and mapping allocated node IDs from imported Path of Building builds.

The research reveals a mature ecosystem with official GGG data sources, well-documented JSON structure, and established patterns for handling large JSON files in .NET. The PathPilot codebase already imports PoB builds and stores tree URLs and allocated nodes in SkillTreeSet models, providing a solid foundation for tree data integration.

**Key discoveries:**
- GGG maintains official skill tree JSON at github.com/grindinggear/skilltree-export (5.5MB, updated with each league)
- JSON structure is well-documented: nodes keyed by ID with connections, groups, stats, and position data
- Path of Building's tree URL encoding uses base64 with version-aware binary format for node IDs
- .NET System.Text.Json with streaming provides optimal performance for large JSON (3-4x better than Newtonsoft)
- Caching strategy with IMemoryCache and HttpClient standard for desktop apps

**Primary recommendation:** Use System.Text.Json with JsonDocument for initial parse, cache in-memory with IMemoryCache, and map PoB allocated node IDs (already in SkillTreeSet.AllocatedNodes) to parsed tree data by integer key lookup.

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| System.Text.Json | .NET 10 BCL | JSON parsing/serialization | Built-in, 20-35% faster than Newtonsoft, 3x fewer allocations, official recommendation |
| System.Net.Http | .NET 10 BCL | HTTP data fetching | Built-in, modern async API, integrates with IHttpClientFactory |
| Microsoft.Extensions.Caching.Memory | .NET 10 | In-memory caching | Standard caching abstraction, simple setup, perfect for single-instance desktop apps |
| Microsoft.Extensions.Http | .NET 10 | HttpClient factory/DI | Prevents socket exhaustion, connection pooling, standard in .NET Core+ |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| System.IO.Compression | .NET 10 BCL | PoB URL decompression | Already used in PobDecoder for base64+deflate |
| System.Text.Encodings.Web | .NET 10 BCL | JSON encoding options | If custom escaping needed (unlikely) |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| System.Text.Json | Newtonsoft.Json | Slower (20-35%), 3x more allocations, but more features (better DateTime handling) |
| IMemoryCache | Manual Dictionary | No expiration policies, no memory pressure handling, harder to test |
| IHttpClientFactory | Static HttpClient | Must manually set PooledConnectionLifetime, no DI benefits, harder to mock |

**Installation:**
```bash
# All required libraries are part of .NET 10 BCL or already referenced
dotnet add package Microsoft.Extensions.Caching.Memory  # If not already added
dotnet add package Microsoft.Extensions.Http  # If not already added
```

## Architecture Patterns

### Recommended Project Structure

```
src/PathPilot.Core/
├── Models/
│   ├── SkillTree.cs            # Already exists
│   ├── PassiveNode.cs          # Already exists
│   └── SkillTreeData.cs        # NEW: Parsed GGG tree data model
├── Services/
│   ├── SkillTreeService.cs     # NEW: Main tree data service
│   └── SkillTreeCache.cs       # NEW: Caching wrapper (optional)
└── Data/
    └── tree-cache/             # NEW: Local JSON cache directory
```

### Pattern 1: Service-Based Tree Data Management

**What:** Central service that loads, parses, and provides access to skill tree data
**When to use:** Desktop applications with single data source and simple access patterns
**Example:**

```csharp
// Source: Microsoft Learn + PathPilot patterns
public class SkillTreeService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private const string CACHE_KEY = "ggg_tree_data";
    private const string TREE_URL = "https://raw.githubusercontent.com/grindinggear/skilltree-export/master/data.json";

    public SkillTreeService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
    {
        _httpClient = httpClientFactory.CreateClient();
        _cache = cache;
    }

    public async Task<SkillTreeData?> GetTreeDataAsync()
    {
        // Check cache first
        if (_cache.TryGetValue(CACHE_KEY, out SkillTreeData? cachedData))
            return cachedData;

        // Download and parse
        var treeData = await LoadTreeDataAsync();

        // Cache for session (desktop app stays open)
        _cache.Set(CACHE_KEY, treeData, new MemoryCacheEntryOptions
        {
            Priority = CacheItemPriority.High,
            SlidingExpiration = TimeSpan.FromHours(24)
        });

        return treeData;
    }

    private async Task<SkillTreeData> LoadTreeDataAsync()
    {
        // Check local cache file first
        var localCache = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PathPilot", "tree-cache", "data.json");

        if (File.Exists(localCache))
        {
            var fileAge = DateTime.Now - File.GetLastWriteTime(localCache);
            if (fileAge < TimeSpan.FromDays(7))
            {
                await using var fileStream = File.OpenRead(localCache);
                return await ParseTreeDataAsync(fileStream);
            }
        }

        // Download from GitHub
        await using var stream = await _httpClient.GetStreamAsync(TREE_URL);

        // Save to local cache
        Directory.CreateDirectory(Path.GetDirectoryName(localCache)!);
        await using (var cacheFile = File.Create(localCache))
        {
            await stream.CopyToAsync(cacheFile);
            await cacheFile.FlushAsync();
        }

        // Parse from cache file
        await using var parseStream = File.OpenRead(localCache);
        return await ParseTreeDataAsync(parseStream);
    }

    private async Task<SkillTreeData> ParseTreeDataAsync(Stream stream)
    {
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;

        var treeData = new SkillTreeData();

        // Parse nodes (3281 items)
        if (root.TryGetProperty("nodes", out var nodesElement))
        {
            foreach (var nodeProp in nodesElement.EnumerateObject())
            {
                var nodeId = int.Parse(nodeProp.Name);
                var node = ParseNode(nodeId, nodeProp.Value);
                treeData.Nodes[nodeId] = node;
            }
        }

        // Parse groups for visual layout
        if (root.TryGetProperty("groups", out var groupsElement))
        {
            foreach (var groupProp in groupsElement.EnumerateObject())
            {
                var groupId = int.Parse(groupProp.Name);
                treeData.Groups[groupId] = ParseGroup(groupId, groupProp.Value);
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
                .ToList();
        }

        if (element.TryGetProperty("out", out var connections))
        {
            node.ConnectedNodes = connections.EnumerateArray()
                .Select(c => int.Parse(c.GetString() ?? "0"))
                .ToList();
        }

        // Classification
        node.IsKeystone = element.TryGetProperty("isKeystone", out var ks) && ks.GetBoolean();
        node.IsNotable = element.TryGetProperty("isNotable", out var nt) && nt.GetBoolean();
        node.IsJewelSocket = element.TryGetProperty("isJewelSocket", out var js) && js.GetBoolean();
        node.IsMastery = element.TryGetProperty("isMastery", out var ms) && ms.GetBoolean();

        return node;
    }
}
```

### Pattern 2: Node ID Mapping from PoB

**What:** Connect PoB imported allocated nodes to parsed tree data
**When to use:** After tree data is loaded, when displaying build's allocated nodes
**Example:**

```csharp
// Source: PathPilot PobXmlParser patterns
public class BuildTreeMapper
{
    private readonly SkillTreeService _treeService;

    public async Task<List<PassiveNode>> GetAllocatedNodesAsync(SkillTreeSet treeSet)
    {
        var treeData = await _treeService.GetTreeDataAsync();
        if (treeData == null) return new List<PassiveNode>();

        var allocatedNodes = new List<PassiveNode>();

        // Map node IDs to actual node data
        foreach (var nodeId in treeSet.AllocatedNodes)
        {
            if (treeData.Nodes.TryGetValue(nodeId, out var node))
            {
                allocatedNodes.Add(node);
            }
            else
            {
                Console.WriteLine($"Warning: Node {nodeId} not found in tree data");
            }
        }

        return allocatedNodes;
    }

    public async Task EnrichTreeSetAsync(SkillTreeSet treeSet)
    {
        var nodes = await GetAllocatedNodesAsync(treeSet);

        // Categorize nodes for UI display
        treeSet.Keystones = nodes
            .Where(n => n.IsKeystone)
            .Select(n => n.Name)
            .ToList();

        treeSet.Notables = nodes
            .Where(n => n.IsNotable)
            .Select(n => n.Name)
            .ToList();
    }
}
```

### Pattern 3: Dependency Injection Setup (Avalonia)

**What:** Register services in Avalonia App.xaml.cs for DI
**When to use:** Application startup configuration
**Example:**

```csharp
// Source: Avalonia DI documentation + Microsoft Learn
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        // HTTP client with proper configuration
        services.AddHttpClient();

        // Memory cache
        services.AddMemoryCache();

        // Application services
        services.AddSingleton<GemDataService>();
        services.AddSingleton<BuildStorage>();
        services.AddSingleton<SkillTreeService>();
        services.AddTransient<BuildTreeMapper>();

        // ViewModels (if using MVVM)
        services.AddTransient<MainWindowViewModel>();

        _serviceProvider = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
```

### Anti-Patterns to Avoid

- **Loading JSON with File.ReadAllText():** For 5.5MB file, risks LOH (Large Object Heap) allocation. Use streams instead.
- **Deserializing entire tree to POCO:** Dictionary<string, object> is slower than JsonDocument. Use typed access patterns.
- **Downloading tree on every app start:** Implement local cache with week-long expiration, check file age before re-downloading.
- **Blocking UI on tree load:** Always use async/await for HTTP and file I/O operations.
- **Ignoring node ID mismatches:** Log warnings when PoB node IDs don't exist in tree data (may indicate version mismatch).

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| HTTP client lifecycle | Manual HttpClient with using blocks | IHttpClientFactory | Prevents socket exhaustion, handles DNS changes, connection pooling |
| JSON streaming | Custom byte-by-byte parser | JsonDocument.ParseAsync | Optimized, handles encoding, proper error handling |
| Memory caching with expiration | Dictionary with timestamps | IMemoryCache | Memory pressure callbacks, sliding/absolute expiration, thread-safe |
| Base64 URL decoding | String.Replace + Convert.FromBase64 | PoB already has this in PobDecoder | Handles URL-safe base64, compression, version handling |
| HTTP retry/timeout policies | Manual try-catch with delays | Polly (optional) | Exponential backoff, circuit breaker patterns, well-tested |

**Key insight:** .NET BCL provides battle-tested implementations for all data loading patterns. The only custom code needed is business logic (parsing node structure, mapping IDs).

## Common Pitfalls

### Pitfall 1: Large Object Heap Fragmentation

**What goes wrong:** Loading 5.5MB JSON with `File.ReadAllText()` allocates string on Large Object Heap, causing fragmentation and GC pressure.

**Why it happens:** .NET places objects >85KB on LOH, which is only compacted during full GC (Gen 2).

**How to avoid:** Use streaming with `JsonDocument.ParseAsync(Stream)` - reads data in chunks, never allocates full string.

**Warning signs:** Memory usage spike on tree load, long GC pauses (>100ms).

### Pitfall 2: Node ID Integer vs String Key Mismatch

**What goes wrong:** GGG JSON keys are strings (`"26725"`), but PoB node IDs are integers (26725). Direct lookup fails.

**Why it happens:** JSON property names must be strings, but game engine uses integer IDs internally.

**How to avoid:** Parse string keys to integers when building node dictionary: `var nodeId = int.Parse(nodeProp.Name)`.

**Warning signs:** `TryGetValue()` always returns false despite valid node IDs, allocated nodes show as empty.

### Pitfall 3: Tree Version Mismatch

**What goes wrong:** PoB build from older league has node IDs that don't exist in current tree JSON.

**Why it happens:** GGG reworks passive tree with each major league, changing node IDs and connections.

**How to avoid:**
1. Log warnings for missing nodes (don't crash)
2. Display message to user "Build uses older tree version"
3. Consider storing tree JSON versioned by league (future enhancement)

**Warning signs:** Many "Node not found" warnings, keystones list empty despite PoB showing allocations.

### Pitfall 4: Synchronous I/O in UI Thread

**What goes wrong:** Calling `GetTreeDataAsync().Result` or `.Wait()` in UI code freezes application for 2-3 seconds on first load.

**Why it happens:** Network and file I/O are blocking operations, .NET runtime can't process UI messages.

**How to avoid:** Always use `await` in UI code, show loading indicator, load tree data early (app startup).

**Warning signs:** Application "hangs" on startup or when opening tree viewer, no error but UI unresponsive.

## Code Examples

Verified patterns from official sources:

### Streaming JSON Parse (Large Files)

```csharp
// Source: Microsoft Learn - System.Text.Json best practices
public async Task<Dictionary<int, PassiveNode>> ParseTreeNodesAsync(Stream stream)
{
    var nodes = new Dictionary<int, PassiveNode>();

    // JsonDocument disposes properly, uses pooled memory
    using var doc = await JsonDocument.ParseAsync(stream, new JsonDocumentOptions
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    });

    var root = doc.RootElement;

    if (!root.TryGetProperty("nodes", out var nodesElement))
        return nodes;

    // Enumerate without loading all into memory
    foreach (var nodeProp in nodesElement.EnumerateObject())
    {
        var nodeId = int.Parse(nodeProp.Name);
        var node = ParseSingleNode(nodeId, nodeProp.Value);
        nodes[nodeId] = node;
    }

    return nodes;
}

private PassiveNode ParseSingleNode(int id, JsonElement element)
{
    var node = new PassiveNode { Id = id };

    // TryGetProperty avoids exceptions for missing fields
    if (element.TryGetProperty("name", out var nameElement))
        node.Name = nameElement.GetString() ?? "";

    // Handle arrays efficiently
    if (element.TryGetProperty("out", out var outElement))
    {
        node.ConnectedNodes = outElement
            .EnumerateArray()
            .Select(e => int.Parse(e.GetString() ?? "0"))
            .ToList();
    }

    return node;
}
```

### HTTP Client with Caching

```csharp
// Source: Microsoft Learn - IHttpClientFactory guidelines
public class SkillTreeDownloader
{
    private readonly HttpClient _httpClient;
    private readonly string _cacheDir;

    public SkillTreeDownloader(IHttpClientFactory factory)
    {
        _httpClient = factory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);

        _cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PathPilot", "tree-cache");

        Directory.CreateDirectory(_cacheDir);
    }

    public async Task<string> GetCachedTreePathAsync()
    {
        var cachePath = Path.Combine(_cacheDir, "data.json");

        // Check if cache exists and is fresh
        if (File.Exists(cachePath))
        {
            var age = DateTime.Now - File.GetLastWriteTime(cachePath);
            if (age < TimeSpan.FromDays(7))
            {
                return cachePath;
            }
        }

        // Download fresh data
        const string url = "https://raw.githubusercontent.com/grindinggear/skilltree-export/master/data.json";

        await using var stream = await _httpClient.GetStreamAsync(url);
        await using var fileStream = File.Create(cachePath);
        await stream.CopyToAsync(fileStream);

        return cachePath;
    }
}
```

### PoB Tree URL Decoding (Adapted from Lua)

```csharp
// Source: PathOfBuilding PassiveSpec.lua DecodeURL function
public class PobTreeUrlDecoder
{
    public List<int> DecodeAllocatedNodes(string treeUrl)
    {
        // Extract base64 portion after last /
        var base64Part = treeUrl.Split('/').Last()
            .Replace("-", "+")
            .Replace("_", "/");

        var bytes = Convert.FromBase64String(base64Part);

        if (bytes.Length < 6)
            throw new ArgumentException("Invalid tree URL: too short");

        // Version (4 bytes, big endian)
        var version = (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];

        if (version > 6)
            throw new ArgumentException($"Unsupported tree version: {version}");

        // Class ID (byte 4)
        var classId = bytes[4];

        // Ascendancy IDs (byte 5, for version >= 4)
        var ascendancyIds = version >= 4 ? bytes[5] : 0;

        // Node count (byte 6 for version >= 5, otherwise calculate from remaining bytes)
        var nodesStart = version >= 4 ? 7 : 6;
        int nodesEnd;

        if (version >= 5)
        {
            var nodeCount = bytes[6];
            nodesEnd = 6 + (nodeCount * 2);
        }
        else
        {
            nodesEnd = bytes.Length - 1;
        }

        // Decode nodes (2 bytes per node ID, big endian)
        var nodes = new List<int>();
        for (int i = nodesStart; i <= nodesEnd; i += 2)
        {
            if (i + 1 < bytes.Length)
            {
                var nodeId = (bytes[i] << 8) | bytes[i + 1];
                nodes.Add(nodeId);
            }
        }

        return nodes;
    }
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Newtonsoft.Json everywhere | System.Text.Json for most cases | .NET Core 3.0+ | 20-35% faster, 3x fewer allocations |
| Manual HttpClient instances | IHttpClientFactory with DI | .NET Core 2.1+ | Prevents socket exhaustion, easier testing |
| JsonConvert.DeserializeObject\<T\> | JsonDocument + manual parsing | .NET Core 3.0+ | Better for large/partial JSON, memory efficient |
| Blocking I/O (WebClient) | Async HttpClient | .NET 4.5+ | Non-blocking UI, better resource usage |
| In-memory only | IMemoryCache with policies | .NET Core 1.0+ | Memory pressure handling, expiration support |

**Deprecated/outdated:**
- **WebClient:** Use HttpClient - better async support, modern API, maintained
- **Newtonsoft.Json for simple cases:** System.Text.Json preferred in .NET 5+ unless you need specific Newtonsoft features
- **Synchronous File.ReadAllText for large files:** Use async streams to avoid LOH and blocking

## Open Questions

Things that couldn't be fully resolved:

1. **Tree Version Compatibility Strategy**
   - What we know: GGG updates tree JSON with each league, node IDs change
   - What's unclear: Should we download multiple tree versions? How to detect build's target league?
   - Recommendation: Phase 1 focuses on current tree only. Log warnings for missing nodes. Document as future enhancement (version-aware tree cache).

2. **Mastery Effects Decoding**
   - What we know: PoB URL version 6 includes mastery effects (4 bytes per mastery: effect_id + node_id)
   - What's unclear: GGG JSON structure for mastery options not fully documented in wiki
   - Recommendation: Parse mastery selections from URL (algorithm provided), but may need to explore actual JSON to map effect IDs to descriptions. Can defer detailed mastery display to later phase.

3. **Ascendancy Node Handling**
   - What we know: Ascendancy nodes are in same "nodes" object, identified by `ascendancyName` property
   - What's unclear: Whether allocated ascendancy nodes need special visual treatment
   - Recommendation: Parse them like regular nodes, use `IsAscendancy` and `ascendancyName` fields for filtering. UI can display them separately if needed.

## Sources

### Primary (HIGH confidence)

- [GGG Official Skill Tree Export](https://github.com/grindinggear/skilltree-export) - Official source for tree JSON data
- [PoE Wiki - Passive Skill Tree JSON](https://www.poewiki.net/wiki/Passive_Skill_Tree_JSON) - Comprehensive JSON structure documentation
- [Microsoft Learn - System.Text.Json](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json) - Official parsing guidelines
- [Microsoft Learn - HttpClient Guidelines](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines) - Official HTTP client best practices
- [Microsoft Learn - IHttpClientFactory](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory) - DI and factory pattern
- [Microsoft Learn - Caching in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/caching) - IMemoryCache documentation
- [PathOfBuilding Community - PassiveSpec.lua](https://github.com/PathOfBuildingCommunity/PathOfBuilding/blob/master/src/Classes/PassiveSpec.lua) - Tree URL decoding algorithm

### Secondary (MEDIUM confidence)

- [Medium - Handling Large JSON in .NET](https://medium.com/@jeslurrahman/handling-large-json-in-net-strategies-to-enhance-performance-0cd5de4aa32b) - Performance strategies
- [Beyond The Semicolon - Crushing Large JSON Payloads](https://www.beyondthesemicolon.com/crushing-large-json-payloads-in-net-a-practical-guide-to-peak-performance/) - Practical patterns
- [Avalonia Docs - Dependency Injection](https://docs.avaloniaui.net/docs/guides/implementation-guides/how-to-implement-dependency-injection) - DI setup in Avalonia
- [Milan Jovanovic - HttpClient in .NET](https://www.milanjovanovic.tech/blog/the-right-way-to-use-httpclient-in-dotnet) - Best practices guide

### Tertiary (LOW confidence)

- Various GitHub issues and community discussions about PoB URL decoding - provides context but not authoritative

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - All libraries are .NET BCL or official Microsoft packages with extensive documentation
- Architecture: HIGH - Patterns verified against Microsoft Learn and existing PathPilot codebase
- Pitfalls: MEDIUM - Based on documented issues and performance articles, some are from experience rather than official sources
- PoB URL decoding: HIGH - Algorithm extracted directly from PathOfBuilding Lua source code
- GGG JSON structure: HIGH - Official GitHub repository and comprehensive wiki documentation

**Research date:** 2026-02-04
**Valid until:** 2026-03-04 (30 days - stable domain, but GGG updates tree with league releases every ~3 months)

**Notes:**
- Tree JSON structure is stable but content updates with each Path of Exile league (every 13 weeks)
- PoB URL encoding format hasn't changed since version 6, considered stable
- .NET BCL APIs are stable across .NET 6-10, no breaking changes expected
