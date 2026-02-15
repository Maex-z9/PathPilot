# Phase 5: Sprite Foundation - Research

**Researched:** 2026-02-15
**Domain:** PoE Skill Tree Sprite Rendering with SkiaSharp
**Confidence:** MEDIUM-HIGH

## Summary

Path of Exile's skill tree uses a **sprite sheet system** where all node sprites (Normal, Notable, Keystone, Jewel Socket) and group backgrounds are provided as PNG sprite sheets with coordinate maps in the JSON data. GGG supplies these assets at **4 zoom levels** (0.1246, 0.2109, 0.2972, 0.3835) to enable quality-based rendering. The existing PathPilot codebase already has robust JSON parsing (SkillTreeDataService) and SkiaSharp rendering (SkillTreeCanvas), making sprite integration straightforward.

The implementation will follow the proven **GemIconService pattern** (HTTP download + local cache + async loading) and leverage **SkiaSharp's DrawImage** method with source/destination rectangles to extract sprites from sheets. Group backgrounds render behind nodes using similar sprite extraction. The 4 zoom thresholds require LOD (level of detail) switching to select appropriate sprite quality.

**Primary recommendation:** Download sprite sheets from GGG's skilltree-export repository (or web.poecdn.com CDN), cache locally for 30 days, parse sprite coordinate data from existing JSON, render using SKCanvas.DrawImage with source rects.

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| SkiaSharp | 2.88+ | 2D rendering, sprite sheet extraction | Already integrated, GPU-accelerated, cross-platform |
| System.Net.Http | Built-in | Async sprite sheet download | Standard .NET HTTP client |
| System.Text.Json | Built-in | Parse sprite coordinate data from JSON | Already used in SkillTreeDataService |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| System.Collections.Concurrent | Built-in | Deduplicate parallel downloads | Prevents multiple downloads of same sprite sheet |
| System.IO.Compression | Built-in | Optional: compress cached sprites | If cache size becomes issue (unlikely with ~10-20 sprite sheets) |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| SkiaSharp | Avalonia DrawingContext | SkiaSharp gives direct bitmap control, better performance for sprite sheets |
| Local Cache | Memory-only cache | Local disk cache persists across app restarts, faster subsequent launches |
| DrawImage | DrawAtlas | DrawAtlas is for batched sprites with transforms; DrawImage simpler for positioned nodes |

**Installation:**
```bash
# No new packages needed - SkiaSharp already in PathPilot.Desktop.csproj
```

## Architecture Patterns

### Recommended Project Structure
```
src/PathPilot.Core/Services/
├── SkillTreeDataService.cs       # Existing - enhance to parse sprite data
├── SkillTreeSpriteService.cs     # NEW - download/cache sprite sheets
└── GemIconService.cs              # Existing - pattern to follow

src/PathPilot.Desktop/Controls/
└── SkillTreeCanvas.cs             # Existing - enhance to render sprites

src/PathPilot.Core/Models/
├── SkillTreeData.cs               # Existing - add sprite metadata
└── SpriteSheet.cs                 # NEW - sprite coordinate model
```

### Pattern 1: Sprite Service (Cache + Download)
**What:** Service that downloads sprite sheets from GGG's CDN/repo and caches locally, following GemIconService pattern
**When to use:** On app startup, async preload sprite sheets
**Example:**
```csharp
// Modeled after GemIconService.cs (lines 5-91)
public class SkillTreeSpriteService : IDisposable
{
    private const int CACHE_DAYS = 30;
    private readonly HttpClient _httpClient;
    private readonly string _cacheDir; // ~/.config/PathPilot/tree-sprites/
    private readonly ConcurrentDictionary<string, Task<SKBitmap?>> _loadTasks = new();

    public async Task<SKBitmap?> GetSpriteSheetAsync(string spriteKey, int zoomLevel)
    {
        // 1. Build cache path from sprite key + zoom level
        // 2. Check cache freshness (30 days like gem icons)
        // 3. If stale/missing, download from URL
        // 4. Load PNG into SKBitmap
        // 5. Return cached SKBitmap (caller must NOT dispose - service owns it)
    }
}
```

### Pattern 2: Sprite Coordinate Extraction
**What:** Parse sprite coordinates from JSON, extract subrect from sprite sheet
**When to use:** During node rendering in SkillTreeCanvas
**Example:**
```csharp
// In SkillTreeCanvas.DrawNodes() - replace DrawCircle with DrawImage
private void DrawNodes(SKCanvas canvas, SKBitmap spriteSheet, SpriteCoordMap coords)
{
    foreach (var node in _treeData.Nodes.Values)
    {
        var spriteKey = GetSpriteKey(node); // e.g. "PSSkillFrame" from JSON
        if (!coords.TryGetValue(spriteKey, out var coord))
            continue;

        // Source rect: region in sprite sheet
        var srcRect = new SKRect(coord.X, coord.Y, coord.X + coord.W, coord.Y + coord.H);

        // Dest rect: world position + size
        var destRect = new SKRect(
            node.CalculatedX.Value - coord.W / 2f,
            node.CalculatedY.Value - coord.H / 2f,
            node.CalculatedX.Value + coord.W / 2f,
            node.CalculatedY.Value + coord.H / 2f
        );

        canvas.DrawImage(SKImage.FromBitmap(spriteSheet), srcRect, destRect);
    }
}
```

### Pattern 3: Zoom-Based LOD Switching
**What:** Select sprite sheet quality based on current zoom level
**When to use:** When zoom level changes in SkillTreeCanvas
**Example:**
```csharp
// GGG zoom thresholds: 0.1246, 0.2109, 0.2972, 0.3835
private int GetSpriteQualityLevel(double currentZoom)
{
    if (currentZoom < 0.1728) return 0;      // Use 0.1246 sprites
    else if (currentZoom < 0.2540) return 1; // Use 0.2109 sprites
    else if (currentZoom < 0.3403) return 2; // Use 0.2972 sprites
    else return 3;                           // Use 0.3835 sprites
    // Midpoints between GGG thresholds for smooth switching
}
```

### Pattern 4: Group Background Rendering
**What:** Render group background images behind node clusters
**When to use:** Before drawing connections/nodes in RenderTree()
**Example:**
```csharp
// In SkillTreeCanvas.RenderTree() - add before DrawConnections
private void DrawGroupBackgrounds(SKCanvas canvas, SKBitmap backgroundSheet, SpriteCoordMap coords)
{
    foreach (var group in _treeData.Groups.Values)
    {
        if (group.Background == null) continue; // No background for this group

        var bgKey = group.Background.Image; // e.g. "PSGroupBackground3"
        if (!coords.TryGetValue(bgKey, out var coord))
            continue;

        var srcRect = new SKRect(coord.X, coord.Y, coord.X + coord.W, coord.Y + coord.H);

        // Apply half-image flag if set
        var width = group.Background.IsHalfImage ? coord.W / 2f : coord.W;

        var destRect = new SKRect(
            group.X + (group.Background.OffsetX ?? 0),
            group.Y + (group.Background.OffsetY ?? 0),
            group.X + width,
            group.Y + coord.H
        );

        canvas.DrawImage(SKImage.FromBitmap(backgroundSheet), srcRect, destRect);
    }
}
```

### Anti-Patterns to Avoid
- **Don't dispose service-owned SKBitmaps:** Service loads sprite sheets once and reuses them. Canvas should never dispose sheets.
- **Don't load all zoom levels upfront:** Only load the current quality level, lazy-load others on demand to save memory.
- **Don't use RenderTransform for zoom:** Existing code correctly uses `canvas.Scale()` in SkiaSharp (lines 574-575 in SkillTreeCanvas.cs).
- **Avoid subpixel source rects:** Integer-align sprite coordinates to prevent pixel bleeding from adjacent sprites in sheet.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Sprite sheet parsing | Custom PNG parser | SKBitmap.Decode(stream) | Handles all formats, color spaces, alpha channels |
| Async download + cache | Manual file I/O + locks | GemIconService pattern + ConcurrentDictionary | Proven in codebase, deduplicates parallel requests |
| Sprite coordinate lookup | String parsing from JSON | System.Text.Json + Dictionary<string, SpriteCoord> | Fast O(1) lookup, type-safe |
| Zoom threshold calculation | Complex conditional logic | Simple thresholding with midpoints | GGG provides exact values, midpoints handle transitions |
| Memory management | Manual bitmap disposal | Service-owned singleton bitmaps | Prevents dispose-after-use bugs, reuses memory |

**Key insight:** SkiaSharp and existing PathPilot patterns (GemIconService, SkillTreeDataService) already solve 90% of sprite rendering. The challenge is **data parsing** (sprite coords from JSON) and **LOD switching** (4 quality levels), not rendering itself.

## Common Pitfalls

### Pitfall 1: Disposing Service-Owned Bitmaps
**What goes wrong:** Canvas renders sprites, disposes SKBitmap, next render crashes with ObjectDisposedException
**Why it happens:** SkiaSharp objects are unmanaged resources; disposal is required, but ownership is unclear
**How to avoid:**
- Service owns sprite sheets, loads once, never disposes until app shutdown
- Canvas receives SKBitmap references, never disposes them
- Use `using var paint = ...` for SKPaint, but NOT for sprite sheets
**Warning signs:** Crashes on second render, "Cannot access disposed object" errors

### Pitfall 2: Sprite Bleeding (Adjacent Sprite Overlap)
**What goes wrong:** Node sprites show thin lines from neighboring sprites in sheet
**Why it happens:** Subpixel source rectangles cause GPU to sample adjacent pixels
**How to avoid:**
- Integer-align sprite coordinates: `var srcRect = SKRect.Create((int)x, (int)y, (int)w, (int)h)`
- Verify JSON coordinates are already integers (GGG data should be)
- Add 0.5px inset if bleeding persists: `srcRect.Inflate(-0.5f, -0.5f)`
**Warning signs:** Colored lines/dots around sprites, visual artifacts at high zoom

### Pitfall 3: Memory Leaks from SKImage.FromBitmap
**What goes wrong:** Creating SKImage from SKBitmap in render loop leaks memory
**Why it happens:** SKImage doesn't auto-dispose; each render creates new image from same bitmap
**How to avoid:**
- **Option A:** Create SKImage once per sprite sheet in service, reuse in renders
- **Option B:** Use `SKCanvas.DrawBitmap(bitmap, srcRect, destRect)` directly (SkiaSharp 2.88+)
- **Option C:** Use `using var image = SKImage.FromBitmap(bitmap)` in render, dispose after draw
**Warning signs:** Steadily increasing memory usage, GC pressure, Linux memory not released

### Pitfall 4: Incorrect Zoom Threshold Mapping
**What goes wrong:** Sprites appear blurry or overly sharp, don't match GGG's web tree
**Why it happens:** Using zoom level directly instead of selecting nearest GGG threshold
**How to avoid:**
- GGG thresholds: **0.1246, 0.2109, 0.2972, 0.3835** (from PoE Wiki documentation)
- Use midpoints for switching: 0.1728, 0.2540, 0.3403
- Test at each threshold to verify sprite quality matches expectations
**Warning signs:** Sprites look pixelated when zoomed in, or too detailed when zoomed out

### Pitfall 5: Missing Group Background Properties
**What goes wrong:** Crash when reading background.Image from group without background
**Why it happens:** Not all groups have backgrounds; property is optional (added in v3.20.0)
**How to avoid:**
- Always null-check: `if (group.Background == null) continue;`
- Parse optional properties with `TryGetProperty` in SkillTreeDataService
- Set default values: `IsHalfImage = false`, `OffsetX = 0`, `OffsetY = 0`
**Warning signs:** NullReferenceException in DrawGroupBackgrounds, crashes on specific tree sections

### Pitfall 6: Hardcoded Sprite URLs
**What goes wrong:** Sprite URLs change between PoE versions, app breaks on update
**Why it happens:** Embedding version-specific URLs instead of reading from JSON
**How to avoid:**
- Parse sprite filenames from JSON's `skillSprites` section
- Build URLs dynamically: `https://web.poecdn.com/image/{filename}`
- Cache by filename, not URL (filename is stable key)
- Consider fallback to local bundled sprites if CDN unreachable
**Warning signs:** 404 errors on sprite downloads after PoE patch, broken tree rendering

## Code Examples

Verified patterns from official sources:

### Parsing Sprite Data from JSON
```csharp
// Source: Based on SkillTreeDataService.cs (lines 90-130)
// Extends existing ParseTreeDataAsync to include sprite data
private async Task<SkillTreeData> ParseTreeDataAsync(string filePath)
{
    var treeData = new SkillTreeData();

    await using var stream = File.OpenRead(filePath);
    using var doc = await JsonDocument.ParseAsync(stream);
    var root = doc.RootElement;

    // Parse sprite coordinates
    if (root.TryGetProperty("skillSprites", out var spritesElement))
    {
        // normalActive, normalInactive, notableActive, etc.
        foreach (var spriteProp in spritesElement.EnumerateObject())
        {
            var spriteType = spriteProp.Name; // e.g. "normalActive"

            foreach (var zoomLevel in spriteProp.Value.EnumerateObject())
            {
                // Keys: "0.1246", "0.2109", "0.2972", "0.3835"
                var zoom = float.Parse(zoomLevel.Name);

                if (zoomLevel.Value.TryGetProperty("filename", out var filename))
                {
                    var spriteSheet = new SpriteSheet
                    {
                        Type = spriteType,
                        ZoomLevel = zoom,
                        Filename = filename.GetString() ?? ""
                    };

                    if (zoomLevel.Value.TryGetProperty("coords", out var coords))
                    {
                        foreach (var coordProp in coords.EnumerateObject())
                        {
                            var key = coordProp.Name; // Sprite key (icon path)
                            var coord = coordProp.Value;

                            spriteSheet.Coordinates[key] = new SpriteCoordinate
                            {
                                X = coord.GetProperty("x").GetInt32(),
                                Y = coord.GetProperty("y").GetInt32(),
                                W = coord.GetProperty("w").GetInt32(),
                                H = coord.GetProperty("h").GetInt32()
                            };
                        }
                    }

                    treeData.SpriteSheets.Add(spriteSheet);
                }
            }
        }
    }

    return treeData;
}
```

### Rendering Sprites with SkiaSharp
```csharp
// Source: Based on SkillTreeCanvas.cs DrawNodes() (lines 654-695)
// Replace circle drawing with sprite rendering
private void DrawNodes(SKCanvas canvas)
{
    var qualityLevel = GetSpriteQualityLevel(_zoomLevel);
    var normalActive = _spriteService.GetSpriteSheet("normalActive", qualityLevel);
    var normalInactive = _spriteService.GetSpriteSheet("normalInactive", qualityLevel);
    // ... load other sprite types (notable, keystone, jewel)

    foreach (var node in _treeData.Nodes.Values)
    {
        if (!node.CalculatedX.HasValue || !node.CalculatedY.HasValue)
            continue;

        // Select sprite sheet based on node type and allocation
        SKBitmap? spriteSheet;
        string spriteKey;

        if (node.IsKeystone)
        {
            spriteSheet = _allocatedNodeIds.Contains(node.Id)
                ? keystoneActive : keystoneInactive;
            spriteKey = node.Icon; // From JSON node property
        }
        else if (node.IsNotable)
        {
            spriteSheet = _allocatedNodeIds.Contains(node.Id)
                ? notableActive : notableInactive;
            spriteKey = node.Icon;
        }
        // ... similar for jewel socket, normal nodes

        if (spriteSheet == null || !_treeData.GetSpriteCoord(spriteKey, out var coord))
        {
            // Fallback: draw colored circle (existing behavior)
            var paint = _allocatedNodeIds.Contains(node.Id) ? allocatedPaint : unallocatedPaint;
            canvas.DrawCircle(node.CalculatedX.Value, node.CalculatedY.Value, 6f, paint);
            continue;
        }

        // Extract sprite from sheet
        var srcRect = new SKRect(coord.X, coord.Y, coord.X + coord.W, coord.Y + coord.H);
        var destRect = new SKRect(
            node.CalculatedX.Value - coord.W / 2f,
            node.CalculatedY.Value - coord.H / 2f,
            node.CalculatedX.Value + coord.W / 2f,
            node.CalculatedY.Value + coord.H / 2f
        );

        // OPTION A: DrawBitmap directly (preferred, no memory leak)
        canvas.DrawBitmap(spriteSheet, srcRect, destRect, null);

        // OPTION B: DrawImage (requires using var to prevent leak)
        // using var image = SKImage.FromBitmap(spriteSheet);
        // canvas.DrawImage(image, srcRect, destRect);
    }
}
```

### Sprite Service Implementation
```csharp
// Source: Modeled after GemIconService.cs (lines 5-91)
public class SkillTreeSpriteService : IDisposable
{
    private const int CACHE_DAYS = 30;
    private const string CDN_BASE = "https://web.poecdn.com/image/";

    private readonly HttpClient _httpClient;
    private readonly string _cacheDir;
    private readonly Dictionary<string, SKBitmap> _loadedSprites = new();
    private readonly ConcurrentDictionary<string, Task<SKBitmap?>> _loadTasks = new();

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

    public async Task<SKBitmap?> GetSpriteSheetAsync(string filename)
    {
        // Check if already loaded in memory
        if (_loadedSprites.TryGetValue(filename, out var cached))
            return cached;

        // Deduplicate parallel loads
        var task = _loadTasks.GetOrAdd(filename, _ => LoadSpriteSheetAsync(filename));
        try
        {
            var bitmap = await task;
            if (bitmap != null)
                _loadedSprites[filename] = bitmap; // Keep in memory
            return bitmap;
        }
        finally
        {
            _loadTasks.TryRemove(filename, out _);
        }
    }

    private async Task<SKBitmap?> LoadSpriteSheetAsync(string filename)
    {
        var cachePath = Path.Combine(_cacheDir, filename);

        // Check cache freshness
        if (File.Exists(cachePath))
        {
            var age = DateTime.Now - File.GetLastWriteTime(cachePath);
            if (age < TimeSpan.FromDays(CACHE_DAYS))
            {
                // Load from cache
                using var stream = File.OpenRead(cachePath);
                return SKBitmap.Decode(stream);
            }
        }

        // Download from CDN
        try
        {
            var url = CDN_BASE + filename;
            using var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            var bytes = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(cachePath, bytes);

            // Decode to bitmap
            using var stream = new MemoryStream(bytes);
            return SKBitmap.Decode(stream);
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();

        // Dispose all loaded bitmaps
        foreach (var bitmap in _loadedSprites.Values)
            bitmap?.Dispose();

        _loadedSprites.Clear();
    }
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Individual image files per node type | Sprite sheets with coordinate maps | v3.18.1 (2022) | Fewer HTTP requests, faster loading |
| `backgroundOverride` property | `background` property in groups | v3.20.0 | Simpler data model, direct mapping |
| Single sprite resolution | 4 zoom levels (0.1246 - 0.3835) | Long-standing | Quality scales with zoom, better UX |
| Manual sprite URLs | Filenames in JSON | v3.18.1 | Version-agnostic, data-driven URLs |

**Deprecated/outdated:**
- **backgroundOverride**: Removed in v3.20.0, use `background.image` instead (groups without backgrounds have null property)
- **Individual sprite files**: Pre-v3.18.1 used separate images; now all sprites are in sheets
- **String node IDs in JSON**: Some old parsers expected strings; GGG data uses integer keys (PathPilot correctly parses as int)

## Open Questions

1. **Do all node types have icon properties in JSON?**
   - What we know: PoE Wiki mentions `icon` property in nodes, but existing PathPilot parser doesn't read it
   - What's unclear: Are Normal nodes generic (single sprite) or do they have unique icons?
   - Recommendation: Parse `icon` property if exists, fallback to type-based sprite key (e.g. "PSSkillFrame" for Normal)

2. **Are sprite filenames version-specific or stable?**
   - What we know: GGG supplies sprites with each data release; filenames include content hashes
   - What's unclear: Do filenames change between patches, requiring cache invalidation?
   - Recommendation: Use 30-day cache (matches gem icons), parse filenames from JSON each launch

3. **How to handle missing/failed sprite downloads?**
   - What we know: Existing code uses colored circles as current rendering
   - What's unclear: Should app bundle fallback sprites, or always fallback to circles?
   - Recommendation: Fallback to colored circles (VIS-01 allows graceful degradation), log warning for debugging

4. **What is the exact sprite coordinate structure in JSON?**
   - What we know: PoE Wiki mentions `{x, y, w, h}` coordinates
   - What's unclear: Are there additional properties (rotation, anchor point)?
   - Recommendation: Parse actual 3.25.0 JSON from poe-tool-dev repo to verify exact structure (LOW confidence without direct inspection)

5. **How are Ascendancy node sprites organized?**
   - What we know: Ascendancy nodes exist (`node.IsAscendancy` in PathPilot)
   - What's unclear: Separate sprite sheets per Ascendancy, or shared sheet with different coordinates?
   - Recommendation: Inspect JSON structure; likely has `ascendancyName` keyed sprite data

## Sources

### Primary (HIGH confidence)
- [PoE Wiki - Passive Skill Tree JSON](https://www.poewiki.net/wiki/Passive_Skill_Tree_JSON) - Sprite structure, zoom levels, coordinate format
- [GGG skilltree-export Repository](https://github.com/grindinggear/skilltree-export) - Official sprite sheets, background property structure (v3.20.0 changes)
- [poe-tool-dev/passive-skill-tree-json](https://github.com/poe-tool-dev/passive-skill-tree-json) - PoE 1 tree data (3.25.0), sprite filenames

### Secondary (MEDIUM confidence)
- [SkiaSharp DrawImage Documentation](https://learn.microsoft.com/en-us/dotnet/api/skiasharp.skcanvas.drawimage?view=skiasharp-2.88) - Source/dest rect usage
- [SkiaSharp Memory Management Issues](https://github.com/mono/SkiaSharp/issues/829) - What to cache vs dispose
- [.NET Caching Patterns](https://learn.microsoft.com/en-us/dotnet/core/extensions/caching) - IMemoryCache for sprite sheets

### Tertiary (LOW confidence, needs verification)
- WebSearch results about DrawAtlas for batched sprite rendering (not applicable to positioned nodes)
- WebSearch results about sprite bleeding prevention (needs testing to verify severity)

## Metadata

**Confidence breakdown:**
- Standard stack: **HIGH** - SkiaSharp already integrated, sprite rendering is core feature
- Architecture: **MEDIUM-HIGH** - Patterns proven in GemIconService, but sprite coord parsing untested
- Pitfalls: **MEDIUM** - Memory leaks and sprite bleeding are real SkiaSharp issues, but mitigation strategies are documented
- Sprite data structure: **LOW-MEDIUM** - Need to parse actual JSON to verify exact coordinate format and icon properties

**Research date:** 2026-02-15
**Valid until:** 2026-03-17 (30 days - PoE tree structure is stable, but check for PoE patches)

**Next steps for planner:**
1. Parse actual 3.25.0 JSON to verify sprite coordinate structure (resolve Open Question 4)
2. Create SpriteSheet and SpriteCoordinate models
3. Enhance SkillTreeDataService to parse skillSprites section
4. Implement SkillTreeSpriteService following GemIconService pattern
5. Replace DrawCircle with DrawBitmap in SkillTreeCanvas
6. Add group background rendering before connections
7. Implement zoom-based LOD switching
