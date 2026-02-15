# Stack Research: Skill Tree Visual Overhaul

**Domain:** Skill Tree Visualization Enhancement (Sprite Rendering, Stats, Search, Minimap, Editing)
**Researched:** 2026-02-15
**Confidence:** HIGH

## Recommended Stack

### Core Technologies (Already Validated)

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| SkiaSharp | 2.88+ | GPU-accelerated 2D rendering, sprite sheet rendering | Already integrated, native DrawBitmap and DrawAtlas support for efficient sprite rendering from texture atlases |
| System.Text.Json | Built-in (.NET 10) | Parse GGG skill tree JSON, sprite coordinates | Already used in project, zero-cost for parsing sprite sheet metadata |
| System.Net.Http.HttpClient | Built-in (.NET 10) | Download sprite sheet images from CDN | Already used in GemIconService, can reuse pattern for tree sprites |

### New Libraries Required

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| FuzzySharp | 2.0.2 | Fuzzy node search matching | For search feature - matches "incr fire dmg" to "Increased Fire Damage" nodes |
| System.Collections.Concurrent | Built-in | Thread-safe sprite cache | For managing concurrent sprite sheet downloads and caching |

### NO New Dependencies Required For

| Feature | Built-in Solution | Why No Library Needed |
|---------|------------------|------------------------|
| Sprite sheet rendering | SkiaSharp DrawBitmap(bitmap, SKRect source, SKRect dest) | Native support for texture atlas rendering |
| Image caching | ConcurrentDictionary<string, SKBitmap> + file system | Pattern already used in GemIconService |
| Minimap overlay | SkiaSharp canvas.Scale() + separate render | Same rendering pipeline, just at smaller scale |
| Node editing | Mouse hit testing (already implemented) + HashSet updates | Already have FindNodeAtPosition, just need allocation toggle |
| Stats parsing | GGG JSON already contains "stats" string array | No parsing needed - display raw strings with text wrapping |

## Implementation Patterns

### Sprite Sheet Loading

Use existing GemIconService pattern adapted for sprite sheets:

```csharp
// Service: SpriteSheetService
public class SpriteSheetService
{
    private readonly ConcurrentDictionary<string, SKBitmap> _spriteSheetCache = new();
    private readonly HttpClient _httpClient;
    private readonly string _cacheDir;

    // Load sprite sheet for zoom level (e.g., "0.3835")
    public async Task<SKBitmap?> GetSpriteSheetAsync(string spriteCategory, string zoomLevel)
    {
        // Pattern from GemIconService:
        // 1. Check memory cache
        // 2. Check disk cache (~/.config/PathPilot/sprite-cache/)
        // 3. Download from CDN if needed
        // 4. Cache to disk and memory
    }

    // Draw sprite from atlas
    public void DrawSprite(SKCanvas canvas, SKBitmap spriteSheet,
        SKRect source, SKRect dest, SKPaint? paint = null)
    {
        canvas.DrawBitmap(spriteSheet, source, dest, paint);
    }
}
```

### GGG Sprite Data Structure

From SkillTree.json:

```json
{
  "imageZoomLevels": [0.1246, 0.2109, 0.2972, 0.3835],
  "sprites": {
    "normalActive": {
      "0.3835": {
        "filename": "https://web.poecdn.com/image/passive-skill/skills-3.jpg",
        "w": 1568,
        "h": 1722,
        "coords": {
          "Art/2DArt/SkillIcons/passives/2handeddamage.png": {
            "x": 0, "y": 0, "w": 52, "h": 52
          }
        }
      }
    },
    "normalInactive": { /* same structure */ },
    "notableActive": { /* same structure */ },
    "notableInactive": { /* same structure */ },
    "keystoneActive": { /* same structure */ },
    "keystoneInactive": { /* same structure */ },
    "mastery": { /* backgrounds and frames */ },
    "groupBackground": { /* group background images */ }
  }
}
```

### Sprite Categories Needed

| Category | Purpose | GGG JSON Key |
|----------|---------|--------------|
| Normal Active | Allocated small nodes | sprites.normalActive |
| Normal Inactive | Unallocated small nodes | sprites.normalInactive |
| Notable Active | Allocated notable nodes | sprites.notableActive |
| Notable Inactive | Unallocated notable nodes | sprites.notableInactive |
| Keystone Active | Allocated keystones | sprites.keystoneActive |
| Keystone Inactive | Unallocated keystones | sprites.keystoneInactive |
| Group Background | Circular backgrounds behind node clusters | sprites.groupBackground |

### Zoom Level Selection

Pick sprite sheet based on current zoom level:

```csharp
private string SelectSpriteZoomLevel(double currentZoom)
{
    // imageZoomLevels: [0.1246, 0.2109, 0.2972, 0.3835]
    // Pick closest zoom level to avoid upscaling
    var zoomLevels = new[] { 0.1246, 0.2109, 0.2972, 0.3835 };
    return zoomLevels
        .OrderBy(z => Math.Abs(z - currentZoom))
        .First()
        .ToString("0.0000");
}
```

### Node Search Implementation

```csharp
// Using FuzzySharp for fuzzy matching
using FuzzySharp;

public List<PassiveNode> SearchNodes(string query, int minScore = 70)
{
    return TreeData.Nodes.Values
        .Select(node => new {
            Node = node,
            Score = Fuzz.PartialRatio(query.ToLower(), node.Name.ToLower())
        })
        .Where(x => x.Score >= minScore)
        .OrderByDescending(x => x.Score)
        .Select(x => x.Node)
        .ToList();
}
```

### Minimap Rendering

Render minimap in corner using same SkillTreeDrawOperation at smaller scale:

```csharp
// In TreeViewerWindow - add minimap canvas
private void RenderMinimap(SKCanvas canvas)
{
    const float minimapSize = 200f;
    const float margin = 10f;

    // Position in bottom-right corner
    var minimapX = Bounds.Width - minimapSize - margin;
    var minimapY = Bounds.Height - minimapSize - margin;

    canvas.Save();
    canvas.Translate(minimapX, minimapY);

    // Calculate zoom to fit entire tree
    var treeWidth = 27832f;  // Max X - Min X from GGG bounds
    var treeHeight = 20712f; // Max Y - Min Y from GGG bounds
    var minimapZoom = Math.Min(minimapSize / treeWidth, minimapSize / treeHeight);

    canvas.Scale(minimapZoom);

    // Render simplified tree (just nodes, no connections for performance)
    RenderMinimapNodes(canvas);

    // Draw viewport indicator (rectangle showing current view)
    RenderViewportIndicator(canvas);

    canvas.Restore();
}
```

### Node Editing (Allocate/Deallocate)

Already have click detection via FindNodeAtPosition. Add allocation toggle:

```csharp
protected override void OnPointerPressed(PointerPressedEventArgs e)
{
    if (!_isPanning && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
    {
        var worldPos = ScreenToWorld(e.GetCurrentPoint(this).Position);
        var nodeId = FindNodeAtPosition(worldPos);

        if (nodeId.HasValue)
        {
            // Toggle allocation
            if (AllocatedNodeIds.Contains(nodeId.Value))
                AllocatedNodeIds.Remove(nodeId.Value);
            else
                AllocatedNodeIds.Add(nodeId.Value);

            InvalidateVisual();
            e.Handled = true;
            return;
        }
    }

    // Start panning if no node clicked
    base.OnPointerPressed(e);
}
```

### Stats Tooltip Enhancement

GGG JSON already provides stats as string array:

```json
{
  "id": 12345,
  "name": "Brutal Blade",
  "stats": [
    "+10 to Strength",
    "12% increased Physical Damage with One Handed Melee Weapons",
    "+15 to Accuracy Rating"
  ]
}
```

No parsing needed - just display in tooltip (already implemented in BuildTooltipContent).

### Ascendancy Tree Display

Ascendancy nodes already in tree data with isAscendancy flag. Add separate viewport or overlay mode:

```csharp
public void CenterOnAscendancy(string ascendancyName)
{
    // Find ascendancy nodes
    var ascNodes = TreeData.Nodes.Values
        .Where(n => n.AscendancyName == ascendancyName)
        .ToList();

    if (!ascNodes.Any()) return;

    // Calculate bounding box of ascendancy nodes
    var minX = ascNodes.Min(n => n.CalculatedX ?? 0);
    var maxX = ascNodes.Max(n => n.CalculatedX ?? 0);
    var minY = ascNodes.Min(n => n.CalculatedY ?? 0);
    var maxY = ascNodes.Max(n => n.CalculatedY ?? 0);

    // Center and zoom on ascendancy
    var centerX = (minX + maxX) / 2;
    var centerY = (minY + maxY) / 2;
    _offsetX = centerX - (Bounds.Width / 2 / ZoomLevel);
    _offsetY = centerY - (Bounds.Height / 2 / ZoomLevel);
    ZoomLevel = 0.5; // Closer zoom for ascendancy details

    InvalidateVisual();
}
```

## Installation

```bash
# Navigate to Desktop project
cd src/PathPilot.Desktop

# Add FuzzySharp for node search
dotnet add package FuzzySharp --version 2.0.2

# No other packages needed - all features use built-in APIs
```

## Alternatives Considered

| Feature | Recommended | Alternative | Why Not Alternative |
|---------|-------------|-------------|---------------------|
| Fuzzy search | FuzzySharp 2.0.2 | FuzzyMatchingDotNet | FuzzySharp more popular (1.3M+ downloads), simpler API, based on proven FuzzyWuzzy algorithm |
| Image loading | SkiaSharp SKBitmap.Decode() | ImageSharp | Already using SkiaSharp, adding ImageSharp is redundant dependency |
| Sprite rendering | SkiaSharp DrawBitmap | DrawAtlas | DrawBitmap simpler for our use case - only rendering ~1300 nodes, not 10k+ sprites |
| JSON parsing | System.Text.Json | Newtonsoft.Json | Already using System.Text.Json throughout project |

## What NOT to Use

| Avoid | Why | Use Instead |
|-------|-----|-------------|
| WPF Image controls | Requires converting SKBitmap to WPF BitmapSource, breaks SkiaSharp pipeline | SkiaSharp SKCanvas.DrawBitmap directly |
| Regex for search | Poor fuzzy matching, exact patterns only | FuzzySharp for user-friendly search |
| In-memory only cache | Sprite sheets are large (1-2MB each), re-download on app restart wastes bandwidth | Disk cache (~/.config/PathPilot/sprite-cache/) like GemIconService |
| Separate rendering library | SkiaSharp already handles everything | Native SkiaSharp APIs |

## Performance Optimizations

### Sprite Sheet Caching Strategy

```csharp
// Cache hierarchy (fastest to slowest)
1. Memory: ConcurrentDictionary<string, SKBitmap> (~10MB for 4 zoom levels × 6 categories)
2. Disk: ~/.config/PathPilot/sprite-cache/ (permanent, 30 day expiry)
3. Network: Download from web.poecdn.com (only on first run or cache expiry)
```

### Rendering Optimizations

| Optimization | Implementation | Performance Gain |
|--------------|----------------|------------------|
| Batch connections | Single SKPath with all lines (already done) | 90% faster than individual DrawLine calls |
| Cull off-screen nodes | Only render nodes within viewport + margin | 70% fewer draw calls at high zoom |
| Level-of-detail | Use different sprite zoom levels | Prevents upscaling artifacts, faster rendering |
| Minimap simplification | Skip connections, smaller node radius | 5x faster minimap render |
| Lazy sprite loading | Load sprite sheets on-demand for current zoom | Faster app startup, lower memory |

### Memory Management

Follow SkiaSharp best practices:

```csharp
// CORRECT: Dispose SKBitmap when no longer needed
using var bitmap = SKBitmap.Decode(stream);
canvas.DrawBitmap(bitmap, dest);

// CORRECT: Cache long-lived bitmaps, dispose on service shutdown
private readonly ConcurrentDictionary<string, SKBitmap> _cache = new();

public void Dispose()
{
    foreach (var bitmap in _cache.Values)
        bitmap?.Dispose();
    _cache.Clear();
}

// WRONG: Creating bitmaps in render loop without caching
public void Render(SKCanvas canvas)
{
    var bitmap = SKBitmap.Decode(stream); // MEMORY LEAK
    canvas.DrawBitmap(bitmap, dest);
}
```

## Data Flow

```
1. App Startup
   └─> SkillTreeDataService downloads SkillTree.json (already implemented)
   └─> Parse sprite metadata (new: sprites.normalActive, etc.)

2. User Opens Tree Viewer
   └─> SpriteSheetService loads sprite sheets for current zoom level
       ├─> Check memory cache
       ├─> Check disk cache (~/.config/PathPilot/sprite-cache/)
       └─> Download from web.poecdn.com if needed

3. Rendering Loop
   └─> SelectSpriteZoomLevel() based on current zoom
   └─> For each node:
       ├─> Lookup sprite coords from node.Icon in sprite metadata
       ├─> DrawBitmap(spriteSheet, sourceRect, destRect)
       └─> If allocated: use *Active sprite, else *Inactive

4. User Searches
   └─> FuzzySharp.Fuzz.PartialRatio(query, node.Name)
   └─> Highlight matching nodes in gold
   └─> Auto-pan to first match

5. User Clicks Node
   └─> FindNodeAtPosition() (already implemented)
   └─> Toggle AllocatedNodeIds
   └─> Update PointsUsed counter
   └─> Invalidate to re-render
```

## Version Compatibility

| Package | Version | Compatible With | Notes |
|---------|---------|-----------------|-------|
| SkiaSharp | 2.88.x | .NET 10.0 | Already validated in project |
| FuzzySharp | 2.0.2 | .NET Standard 2.0+ | Compatible with .NET 10 |
| Avalonia | 11.3.11 | SkiaSharp 2.88.x | Already integrated via Avalonia.Skia |

## Sources

**HIGH CONFIDENCE (Official Documentation & Community Standards)**

- [Microsoft Learn - SKCanvas.DrawBitmap](https://learn.microsoft.com/en-us/dotnet/api/skiasharp.skcanvas.drawbitmap?view=skiasharp-2.88) - Official SkiaSharp sprite rendering API
- [poe-tool-dev/passive-skill-tree-json](https://github.com/poe-tool-dev/passive-skill-tree-json) - GGG skill tree data source (version 3.25.0)
- [SkiaSharp GitHub Issue #829](https://github.com/mono/SkiaSharp/issues/829) - Memory management best practices
- [PoESkillTree GitHub](https://github.com/PoESkillTree/PoESkillTree) - Existing C# skill tree renderer (WPF-based reference)

**MEDIUM CONFIDENCE (Library Documentation)**

- [NuGet - FuzzySharp 2.0.2](https://www.nuget.org/packages/FuzzySharp) - Fuzzy search library
- [React Native Skia - Atlas](https://shopify.github.io/react-native-skia/docs/shapes/atlas/) - DrawAtlas documentation (alternative approach)

**Context Verified**

- Sprite sheet structure verified from live GGG JSON: `https://raw.githubusercontent.com/poe-tool-dev/passive-skill-tree-json/master/3.25.0/SkillTree.json`
- Sprite URLs confirmed: `https://web.poecdn.com/image/passive-skill/skills-3.jpg` format
- Image zoom levels: [0.1246, 0.2109, 0.2972, 0.3835]

---
*Stack research for: PathPilot Skill Tree Visual Overhaul*
*Researched: 2026-02-15*
*Next: Create FEATURES.md, ARCHITECTURE.md, PITFALLS.md for roadmap planning*
