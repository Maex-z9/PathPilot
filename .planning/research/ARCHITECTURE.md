# Architecture Research: Full Visual Skill Tree Integration

**Domain:** SkiaSharp-based Skill Tree Rendering in .NET/Avalonia Desktop App
**Researched:** 2026-02-15
**Confidence:** HIGH

## Executive Summary

PathPilot already has a working SkiaSharp-based skill tree foundation with zoom/pan/hover. Integrating full visual features (sprite sheet rendering, stats tooltips, node search, ascendancy trees, minimap, and editing) requires **extending existing components** rather than rebuilding. The architecture leverages PathPilot's proven patterns: ICustomDrawOperation for rendering, StyledProperty for reactive bindings, and service-based data loading with caching.

**Critical insight:** GGG's tree JSON already contains sprite coordinates, zoom level mappings, and group backgrounds—currently **unused**. The existing position calculation system (orbit-based math) stays intact; new rendering layers add visual polish on top.

**Key architectural decisions:**
1. **Sprite rendering:** Cache SKBitmap sheets per zoom level, use DrawBitmap with source/dest rects
2. **State management:** Add AllocatedNodeIds as mutable HashSet property, expose INotifyCollectionChanged
3. **Node editing:** Click handlers modify HashSet, regenerate tree URL on-demand, invalidate visual
4. **Stats tooltips:** Parse node.Stats[] with regex for color highlighting, render multi-line text blocks
5. **Minimap:** Separate smaller SKCanvas with scaled-down rendering, overlay viewport rectangle
6. **Search:** MVVM pattern with ObservableCollection filtering, scroll/highlight on match

## Standard Architecture for 2D Interactive Tree Editors

### System Overview

```
┌─────────────────────────────────────────────────────────────┐
│                        UI Layer (AXAML)                      │
├─────────────────────────────────────────────────────────────┤
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
│  │ TreeCanvas   │  │ MinimapCanvas│  │ SearchPanel  │       │
│  │ (main view)  │  │ (navigation) │  │ (filtering)  │       │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘       │
│         │                 │                  │               │
├─────────┴─────────────────┴──────────────────┴───────────────┤
│                     Control Layer                            │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────┐    │
│  │          SkillTreeCanvas (Control)                   │    │
│  │  - AllocatedNodeIds (mutable HashSet)               │    │
│  │  - HoveredNodeId, SelectedNodeId                    │    │
│  │  - OnNodeClicked event                               │    │
│  │  - FindNodeAtPosition(), ToggleNode()               │    │
│  └────────────────────┬────────────────────────────────┘    │
│                       │                                      │
├───────────────────────┴──────────────────────────────────────┤
│                  Rendering Layer                             │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────┐    │
│  │     SkillTreeDrawOperation (ICustomDrawOperation)    │    │
│  │  - DrawBackgrounds() → group images                 │    │
│  │  - DrawConnections() → batched path                 │    │
│  │  - DrawNodes() → sprite extraction                  │    │
│  │  - DrawStats() → multi-line text blocks             │    │
│  └─────────────────────────────────────────────────────┘    │
├─────────────────────────────────────────────────────────────┤
│                   Services Layer                             │
├─────────────────────────────────────────────────────────────┤
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
│  │TreeDataSvc   │  │SpriteSvc     │  │TreeUrlSvc    │       │
│  │(GGG JSON)    │  │(sheet cache) │  │(encode/decode)│       │
│  └──────────────┘  └──────────────┘  └──────────────┘       │
└─────────────────────────────────────────────────────────────┘
```

### Component Responsibilities

| Component | Responsibility | Integration Point |
|-----------|----------------|-------------------|
| **SkillTreeCanvas** | User interaction (click, hover), state (AllocatedNodeIds), events | **MODIFY EXISTING** - Add click handlers, mutable state |
| **SkillTreeDrawOperation** | Pure rendering (sprites, stats text, backgrounds) | **MODIFY EXISTING** - Replace DrawCircle with DrawBitmap |
| **SpriteSheetService** | Load/cache sprite sheets per zoom, provide extraction rects | **NEW** - Parallels GemIconService pattern |
| **TreeUrlEncoder** | Encode HashSet<int> → base64 URL, decode URL → HashSet<int> | **EXTEND EXISTING** TreeUrlDecoder (currently decode-only) |
| **MinimapCanvas** | Scaled-down tree view with viewport overlay | **NEW** - Similar to SkillTreeCanvas but simplified |
| **NodeSearchViewModel** | Filter nodes by name/stats, expose ObservableCollection | **NEW** - Standard MVVM search pattern |

## Integration with Existing Architecture

### What Stays Unchanged

| Component | Current Implementation | Why It Stays |
|-----------|------------------------|--------------|
| **SkillTreeDataService** | Loads GGG JSON, parses nodes/groups | Works perfectly, already caches |
| **SkillTreePositionHelper** | Orbit-based position calculation | Math is correct, positions stable |
| **TreeViewerWindow** | Hosts canvas, zoom buttons | Just add search box, minimap panel |
| **PassiveNode model** | CalculatedX/Y, Stats[], connections | Has all needed fields already |
| **Zoom/pan transform** | canvas.Translate/Scale in DrawOperation | Proven, no need to change |

### What Needs Modification

#### 1. SkillTreeCanvas.cs (Control Layer)

**Current:** Read-only display, hover detection, tooltip

**Add:**
```csharp
// Mutable allocated state (currently immutable HashSet property)
public static readonly StyledProperty<ObservableCollection<int>?> AllocatedNodeIdsProperty = ...;

// Node selection state
public static readonly StyledProperty<int?> SelectedNodeIdProperty = ...;

// Events for editing
public event EventHandler<NodeClickedEventArgs>? NodeClicked;
public event EventHandler? AllocationChanged;

// Click handling (add to OnPointerPressed)
protected override void OnPointerPressed(PointerPressedEventArgs e)
{
    if (currentPoint.Properties.IsLeftButtonPressed && !_isPanning)
    {
        var worldPos = ScreenToWorld(currentPoint.Position);
        var nodeId = FindNodeAtPosition(worldPos);
        if (nodeId.HasValue)
        {
            ToggleNode(nodeId.Value);
            NodeClicked?.Invoke(this, new NodeClickedEventArgs(nodeId.Value));
        }
    }
}

// Node toggling
public void ToggleNode(int nodeId)
{
    if (AllocatedNodeIds == null) return;

    if (AllocatedNodeIds.Contains(nodeId))
        AllocatedNodeIds.Remove(nodeId);
    else
        AllocatedNodeIds.Add(nodeId);

    AllocationChanged?.Invoke(this, EventArgs.Empty);
    InvalidateVisual();
}
```

**Rationale:** Change AllocatedNodeIds from immutable HashSet to ObservableCollection enables two-way binding and change notifications. Keeps existing zoom/pan logic intact.

#### 2. SkillTreeDrawOperation.cs (Rendering Layer)

**Current:** Draws colored circles for nodes

**Replace DrawNodes() with:**
```csharp
private void DrawNodes(SKCanvas canvas)
{
    var spriteService = _services.GetRequiredService<SpriteSheetService>();
    var currentZoomLevel = DetermineOptimalSpriteZoom(_zoomLevel);

    foreach (var node in _treeData.Nodes.Values)
    {
        if (!node.CalculatedX.HasValue || !node.CalculatedY.HasValue)
            continue;

        var x = node.CalculatedX.Value;
        var y = node.CalculatedY.Value;

        // Determine sprite type
        var spriteCategory = GetSpriteCategory(node);
        var allocated = _allocatedNodeIds.Contains(node.Id);

        // Get sprite sheet and coordinates
        var (bitmap, sourceRect) = spriteService.GetNodeSprite(
            spriteCategory,
            node.Icon,
            currentZoomLevel,
            allocated);

        if (bitmap != null)
        {
            var destRect = new SKRect(
                x - sourceRect.Width / 2,
                y - sourceRect.Height / 2,
                x + sourceRect.Width / 2,
                y + sourceRect.Height / 2);

            canvas.DrawBitmap(bitmap, sourceRect, destRect);
        }
    }
}

private string GetSpriteCategory(PassiveNode node)
{
    if (node.IsKeystone) return "keystoneActive";
    if (node.IsNotable) return "notableActive";
    if (node.IsJewelSocket) return "jewelSocket";
    if (node.IsMastery) return "mastery";
    return "normalActive";
}

private int DetermineOptimalSpriteZoom(float currentZoom)
{
    // GGG zoom levels: [0.1246, 0.2109, 0.2972, 0.3835]
    // Pick closest zoom level to current view scale
    var zoomLevels = new[] { 0.1246f, 0.2109f, 0.2972f, 0.3835f };
    return zoomLevels
        .Select((z, i) => (zoom: z, index: i))
        .OrderBy(x => Math.Abs(x.zoom - currentZoom))
        .First()
        .index;
}
```

**Rationale:** SKBitmap.DrawBitmap with source/dest rects is efficient for sprite extraction. Determining zoom level at render time ensures correct sprite sheet is used. Fallback to colored circles if sprite not found.

### What Needs Creating

#### 1. SpriteSheetService.cs (NEW)

**Location:** `PathPilot.Core/Services/SpriteSheetService.cs`

**Responsibility:** Download, cache, and extract sprites from GGG sprite sheets

```csharp
public class SpriteSheetService
{
    private readonly HttpClient _httpClient;
    private readonly string _cacheDir;

    // Cache: (category, zoomIndex) -> SKBitmap
    private readonly ConcurrentDictionary<(string, int), SKBitmap> _sheetCache = new();

    // Coordinate mappings from tree JSON
    private readonly Dictionary<string, SpriteCoordinate> _coordinates = new();

    public async Task InitializeAsync(SkillTreeData treeData)
    {
        // Parse skillSprites from tree JSON
        // Download sprite sheets for each zoom level
        // Cache SKBitmaps in memory
    }

    public (SKBitmap? bitmap, SKRect sourceRect) GetNodeSprite(
        string category,
        string iconPath,
        int zoomLevel,
        bool allocated)
    {
        var key = (category, zoomLevel);
        if (!_sheetCache.TryGetValue(key, out var bitmap))
            return (null, SKRect.Empty);

        if (!_coordinates.TryGetValue(iconPath, out var coords))
            return (null, SKRect.Empty);

        return (bitmap, new SKRect(coords.X, coords.Y,
                                    coords.X + coords.W,
                                    coords.Y + coords.H));
    }
}
```

**Pattern:** Mirrors GemIconService (download + cache), but caches entire sheets not individual icons. Use ConcurrentDictionary for thread-safety. Parse coordinates from tree JSON skillSprites section.

**Cache location:** `~/.config/PathPilot/sprite-cache/skill_sprite-active-0-[hash].png`

**Cache duration:** 30 days (same as gem icons)

#### 2. TreeUrlEncoder.cs (EXTEND TreeUrlDecoder)

**Location:** `PathPilot.Core/Parsers/TreeUrlEncoder.cs`

**Responsibility:** Convert HashSet<int> → base64url tree URL

```csharp
public static class TreeUrlEncoder
{
    public static string EncodeTreeUrl(
        HashSet<int> allocatedNodes,
        int classId,
        int ascendancyId = 0,
        int version = 6)
    {
        var nodeCount = allocatedNodes.Count;
        var bytes = new byte[7 + nodeCount * 2];

        // Version (4 bytes, big endian)
        bytes[0] = (byte)(version >> 24);
        bytes[1] = (byte)(version >> 16);
        bytes[2] = (byte)(version >> 8);
        bytes[3] = (byte)version;

        // Class ID (byte 4)
        bytes[4] = (byte)classId;

        // Ascendancy ID (byte 5)
        bytes[5] = (byte)ascendancyId;

        // Node count (byte 6)
        bytes[6] = (byte)nodeCount;

        // Nodes (2 bytes each, big endian, sorted)
        var sortedNodes = allocatedNodes.OrderBy(n => n).ToArray();
        for (int i = 0; i < nodeCount; i++)
        {
            var offset = 7 + i * 2;
            bytes[offset] = (byte)(sortedNodes[i] >> 8);
            bytes[offset + 1] = (byte)sortedNodes[i];
        }

        // Base64url encode
        var base64 = Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        return $"https://www.pathofexile.com/passive-skill-tree/{base64}";
    }
}
```

**Rationale:** Inverse of existing TreeUrlDecoder. Nodes must be sorted for URL consistency. Use base64url encoding (RFC 4648 section 5) as per GGG spec.

#### 3. MinimapCanvas.cs (NEW)

**Location:** `PathPilot.Desktop/Controls/MinimapCanvas.cs`

**Responsibility:** Small overview map with viewport indicator

```csharp
public class MinimapCanvas : Control
{
    public static readonly StyledProperty<SkillTreeData?> TreeDataProperty = ...;
    public static readonly StyledProperty<SKRect> ViewportProperty = ...;

    public override void Render(DrawingContext context)
    {
        // Simplified rendering: connections + dots only (no sprites)
        var operation = new MinimapDrawOperation(
            Bounds,
            TreeData,
            AllocatedNodeIds,
            Viewport, // From main canvas
            MinimapScale); // e.g., 0.02 vs main 0.08

        context.Custom(operation);
    }
}

private class MinimapDrawOperation : ICustomDrawOperation
{
    public void Render(ImmediateDrawingContext context)
    {
        using var lease = leaseFeature.Lease();
        var canvas = lease.SkCanvas;

        canvas.Save();
        canvas.Scale(_minimapScale);

        // Draw simplified tree (just connections + small dots)
        DrawConnections(canvas);
        DrawNodes(canvas); // Small colored dots only

        // Draw viewport rectangle overlay
        using var paint = new SKPaint
        {
            Color = SKColors.Yellow,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };
        canvas.DrawRect(_viewportRect, paint);

        canvas.Restore();
    }
}
```

**Pattern:** Reuse existing connection/node rendering logic but at smaller scale. Viewport rectangle calculated from main canvas offset/zoom. Click on minimap jumps main view.

**UI placement:** Bottom-right corner of TreeViewerWindow, 200x200px, semi-transparent background.

#### 4. NodeSearchViewModel.cs (NEW)

**Location:** `PathPilot.Desktop/ViewModels/NodeSearchViewModel.cs`

**Responsibility:** Filter nodes by name/stats, expose results for UI binding

```csharp
public class NodeSearchViewModel : INotifyPropertyChanged
{
    private string _searchText = "";
    private ObservableCollection<PassiveNode> _filteredNodes = new();
    private readonly SkillTreeData _treeData;

    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            OnPropertyChanged();
            FilterNodes();
        }
    }

    public ObservableCollection<PassiveNode> FilteredNodes
    {
        get => _filteredNodes;
        private set
        {
            _filteredNodes = value;
            OnPropertyChanged();
        }
    }

    private void FilterNodes()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            FilteredNodes.Clear();
            return;
        }

        var results = _treeData.Nodes.Values
            .Where(n =>
                n.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                n.Stats.Any(s => s.Contains(SearchText, StringComparison.OrdinalIgnoreCase)))
            .Take(50) // Limit results
            .ToList();

        FilteredNodes = new ObservableCollection<PassiveNode>(results);
    }
}
```

**UI binding:** TextBox → SearchText property, ListBox → FilteredNodes, SelectionChanged → CenterOnNode() + highlight.

**Pattern:** Standard Avalonia MVVM with ObservableCollection. Use Contains for simple substring search (regex for advanced later).

## Data Flow Patterns

### 1. Tree Loading Flow (Existing, Unchanged)

```
User opens build
    ↓
TreeViewerWindow created with TreeUrl
    ↓
TreeUrlDecoder.DecodeAllocatedNodes(treeUrl) → HashSet<int>
    ↓
SkillTreeDataService.GetTreeDataAsync() → SkillTreeData (cached)
    ↓
SkillTreePositionHelper.CalculateAllPositions(treeData)
    ↓
TreeCanvas.TreeData = treeData
TreeCanvas.AllocatedNodeIds = allocatedNodes
    ↓
Render triggered via InvalidateVisual()
```

### 2. Sprite Loading Flow (NEW)

```
TreeCanvas first render
    ↓
SkillTreeDrawOperation.Render() called
    ↓
SpriteSheetService.GetNodeSprite(category, icon, zoom)
    ↓
Sheet cached? YES → Return (bitmap, rect)
             NO ↓
    Download sprite sheet from GGG
    Parse skillSprites coordinates from tree JSON
    Cache SKBitmap in ConcurrentDictionary
    Return (bitmap, rect)
    ↓
canvas.DrawBitmap(bitmap, sourceRect, destRect)
```

### 3. Node Editing Flow (NEW)

```
User clicks node
    ↓
SkillTreeCanvas.OnPointerPressed()
    ↓
FindNodeAtPosition(worldPos) → nodeId?
    ↓
ToggleNode(nodeId) → AllocatedNodeIds.Add/Remove
    ↓
AllocationChanged event fired
    ↓
TreeViewerWindow listens → TreeUrlEncoder.EncodeTreeUrl()
    ↓
Update Build.SkillTreeSet.TreeUrl
Update Build.SkillTreeSet.AllocatedNodes
    ↓
InvalidateVisual() → re-render with new allocation state
```

### 4. Search Flow (NEW)

```
User types in search box
    ↓
SearchText property changed
    ↓
FilterNodes() → LINQ filter on Nodes.Values
    ↓
FilteredNodes updated (ObservableCollection)
    ↓
ListBox auto-updates via binding
    ↓
User clicks result
    ↓
TreeCanvas.CenterOnNode(nodeId)
TreeCanvas.SelectedNodeId = nodeId → highlight in render
```

## Architectural Patterns

### Pattern 1: Sprite Sheet Caching with Zoom Levels

**What:** Load full sprite sheets (not individual sprites), cache per zoom level, extract regions on-demand

**When to use:** Any 2D app rendering many small icons from texture atlases

**Implementation:**
```csharp
// Bad: Load individual sprite per node (3000+ requests)
foreach (var node in nodes)
{
    var sprite = await LoadSpriteAsync(node.Icon); // SLOW
    canvas.DrawImage(sprite, x, y);
}

// Good: Load sheet once, extract regions
var sheet = await LoadSpriteSheetAsync("normalActive", zoomLevel: 2);
foreach (var node in nodes)
{
    var rect = GetSpriteCoords(node.Icon);
    canvas.DrawBitmap(sheet, rect, destRect); // FAST
}
```

**Trade-offs:**
- **Pro:** 4-5 HTTP requests vs 3000+, memory efficient (shared sheets)
- **Pro:** SkiaSharp DrawBitmap with source rect is GPU-accelerated
- **Con:** Initial load slower (larger files), need coordinate mappings
- **Con:** Must handle zoom level switches (invalidate cache or keep all levels)

**PathPilot application:** Download sprite sheets on first render, cache in `~/.config/PathPilot/sprite-cache/`. Keep all 4 zoom levels in memory (~20MB total). Switch sheets when zoom crosses thresholds (0.1246, 0.2109, 0.2972, 0.3835).

### Pattern 2: Mutable State with ObservableCollection

**What:** Use ObservableCollection<int> instead of immutable HashSet for allocated nodes, enable two-way binding

**When to use:** Any scenario where UI needs to modify data and reflect changes reactively

**Implementation:**
```csharp
// Bad: Immutable HashSet (must replace entire collection)
public HashSet<int> AllocatedNodeIds { get; set; } // Can't bind to mutations

void ToggleNode(int id)
{
    var newSet = new HashSet<int>(AllocatedNodeIds);
    if (!newSet.Remove(id)) newSet.Add(id);
    AllocatedNodeIds = newSet; // Triggers property change, but inefficient
}

// Good: ObservableCollection (mutations notify automatically)
public ObservableCollection<int> AllocatedNodeIds { get; set; }

void ToggleNode(int id)
{
    if (!AllocatedNodeIds.Remove(id))
        AllocatedNodeIds.Add(id); // CollectionChanged event auto-fires
}
```

**Trade-offs:**
- **Pro:** Automatic change notifications, MVVM-friendly, less boilerplate
- **Pro:** UI updates reactively without manual InvalidateVisual() calls
- **Con:** Slower lookups than HashSet (use backing HashSet if perf critical)
- **Con:** Thread-safety requires synchronization

**PathPilot application:** SkillTreeCanvas.AllocatedNodeIds → ObservableCollection. Listen to CollectionChanged for URL regeneration. Use HashSet.Contains() in tight render loop (convert once in DrawOperation constructor).

### Pattern 3: Service-Based Resource Management

**What:** Centralize sprite/image loading in services, inject into controls, handle caching/disposal automatically

**When to use:** Any resource-heavy app with shared assets (images, fonts, data files)

**Implementation:**
```csharp
// Bad: Each control loads its own resources
public class TreeCanvas : Control
{
    private SKBitmap _spriteSheet;

    public override void Render(...)
    {
        _spriteSheet ??= SKBitmap.Decode("sprite.png"); // Duplicated memory
        canvas.DrawBitmap(_spriteSheet, ...);
    }
}

// Good: Service manages shared resources
public class SpriteSheetService : IDisposable
{
    private ConcurrentDictionary<string, SKBitmap> _cache = new();

    public SKBitmap GetSheet(string name)
    {
        return _cache.GetOrAdd(name, LoadSheet);
    }

    public void Dispose()
    {
        foreach (var bitmap in _cache.Values)
            bitmap.Dispose(); // Cleanup native memory
    }
}

public class TreeCanvas : Control
{
    private readonly SpriteSheetService _spriteService;

    public TreeCanvas(SpriteSheetService service)
    {
        _spriteService = service;
    }

    public override void Render(...)
    {
        var sheet = _spriteService.GetSheet("normalActive");
        canvas.DrawBitmap(sheet, ...);
    }
}
```

**Trade-offs:**
- **Pro:** Shared memory (one sheet for all nodes), automatic disposal
- **Pro:** Testable (mock service), single source of truth
- **Con:** Requires DI setup, slightly more complex initialization
- **Con:** Global state (but acceptable for caches)

**PathPilot application:** Create SpriteSheetService in TreeViewerWindow constructor, pass to SkillTreeCanvas. Mirrors existing GemIconService pattern. Dispose on window close.

### Pattern 4: Layered Rendering Pipeline

**What:** Render tree in layers (backgrounds → connections → nodes → overlays), use separate draw calls for clarity

**When to use:** Complex 2D scenes with depth sorting, transparency, or varying update frequencies

**Implementation:**
```csharp
// Bad: Interleaved rendering (hard to maintain)
foreach (var node in nodes)
{
    DrawConnection(node); // Mixed layers
    DrawNodeBackground(node);
    DrawNodeSprite(node);
    DrawNodeOverlay(node);
}

// Good: Layered rendering (clear, optimizable)
void RenderTree(SKCanvas canvas)
{
    DrawBackgrounds(canvas);     // Layer 1: Group backgrounds
    DrawConnections(canvas);     // Layer 2: Connection lines
    DrawNodes(canvas);           // Layer 3: Node sprites
    DrawOverlays(canvas);        // Layer 4: Selection rings, highlights
}
```

**Trade-offs:**
- **Pro:** Clear rendering order, easier to debug visual issues
- **Pro:** Can skip layers (e.g., minimap skips overlays), optimize individually
- **Con:** Multiple passes over data (but data is cached)
- **Con:** Can't interleave effects (but rarely needed)

**PathPilot application:** SkillTreeDrawOperation.RenderTree() already uses DrawConnections() then DrawNodes(). Add DrawBackgrounds() first (group images), DrawOverlays() last (selection rings, search highlights).

## Integration Points

### Existing Component Modifications

| Component | File | Lines to Change | New Methods | New Properties |
|-----------|------|-----------------|-------------|----------------|
| **SkillTreeCanvas** | Controls/SkillTreeCanvas.cs | ~50 | ToggleNode(), OnNodeClicked() | SelectedNodeId, EditMode |
| **SkillTreeDrawOperation** | Controls/SkillTreeCanvas.cs | ~150 | DrawBackgrounds(), DrawSprites() | Replace DrawNodes() |
| **TreeViewerWindow** | TreeViewerWindow.axaml.cs | ~30 | OnNodeClicked(), SaveTree() | Event handlers |
| **TreeViewerWindow.axaml** | TreeViewerWindow.axaml | ~20 | Add search box, minimap panel | SearchText binding |

### New Component Dependencies

| New Component | Depends On | Used By | Location |
|---------------|------------|---------|----------|
| **SpriteSheetService** | SkillTreeData, HttpClient | SkillTreeDrawOperation | Core/Services/ |
| **TreeUrlEncoder** | None | TreeViewerWindow | Core/Parsers/ |
| **MinimapCanvas** | SkillTreeData | TreeViewerWindow | Desktop/Controls/ |
| **NodeSearchViewModel** | SkillTreeData | TreeViewerWindow | Desktop/ViewModels/ |
| **StatsTextRenderer** | None | SkillTreeDrawOperation | Desktop/Rendering/ |

### Data Flow Dependencies

```
TreeViewerWindow
    ↓ creates
SpriteSheetService ← initializes from → SkillTreeData
    ↓ injected into
SkillTreeCanvas
    ↓ uses in
SkillTreeDrawOperation → renders sprites → SKCanvas

TreeViewerWindow
    ↓ listens to
SkillTreeCanvas.AllocationChanged
    ↓ calls
TreeUrlEncoder.EncodeTreeUrl
    ↓ saves to
Build.SkillTreeSet.TreeUrl
```

## Build Order (Feature Dependencies)

### Phase 1: Sprite Foundation (No UI Changes)
**Goal:** Replace colored dots with real sprites, no editing yet

1. **SpriteSheetService** - Download, cache, coordinate lookup
2. **Modify SkillTreeDrawOperation** - Replace DrawNodes() with sprite rendering
3. **Add zoom level detection** - Pick optimal sprite sheet for current zoom
4. **Fallback rendering** - Keep colored dots if sprites fail to load

**Validation:** Tree renders with real PoE sprites, zoom changes switch sprite sheets

**Complexity:** Medium (sprite coordinate parsing, caching logic)

**Dependencies:** None (pure rendering upgrade)

### Phase 2: Stat Tooltips Enhancement
**Goal:** Show formatted stats with color highlighting

1. **StatsTextRenderer** - Parse node.Stats[] with regex, identify ranges/values
2. **Multi-line text layout** - Use SkiaSharp.TextBlocks or manual line breaking
3. **Color highlighting** - Regex groups for numbers (blue), keywords (gold)
4. **Modify BuildTooltipContent()** - Replace TextBlock with rendered SKBitmap

**Validation:** Hovering node shows "10% increased Damage" with "10%" in blue

**Complexity:** Medium (text layout in SkiaSharp is manual)

**Dependencies:** Phase 1 (sprites + stats together look cohesive)

### Phase 3: Node Editing (State Management)
**Goal:** Click nodes to allocate/deallocate, persist changes

1. **TreeUrlEncoder** - Encode HashSet<int> → base64 URL
2. **Modify SkillTreeCanvas** - Add ToggleNode(), OnNodeClicked event
3. **Change AllocatedNodeIds** - HashSet → ObservableCollection
4. **Modify TreeViewerWindow** - Listen to AllocationChanged, regenerate URL

**Validation:** Clicking node toggles allocation, URL updates, reload preserves state

**Complexity:** Low (state management is straightforward)

**Dependencies:** Phase 1 (visual feedback on allocation needed)

### Phase 4: Node Search
**Goal:** Filter nodes by name/stats, jump to matches

1. **NodeSearchViewModel** - SearchText property, FilteredNodes collection
2. **Modify TreeViewerWindow.axaml** - Add search box, results ListBox
3. **CenterOnNode()** - Scroll main canvas to position, highlight node
4. **Advanced filters** - Keystone/Notable checkboxes, stat regex

**Validation:** Type "life" → shows Life nodes, click → jumps to node

**Complexity:** Low (standard MVVM pattern)

**Dependencies:** Phase 3 (selection highlighting requires editable state)

### Phase 5: Minimap
**Goal:** Overview navigation with viewport indicator

1. **MinimapCanvas** - Simplified rendering (dots + connections only)
2. **Viewport rectangle calculation** - Convert main offset/zoom to minimap coords
3. **Click navigation** - Click minimap → update main canvas offset
4. **UI layout** - Bottom-right overlay, semi-transparent background

**Validation:** Minimap shows full tree, yellow rectangle tracks viewport, click pans

**Complexity:** Medium (coordinate mapping between canvases)

**Dependencies:** Phase 1 (minimap rendering reuses connection logic)

### Phase 6: Ascendancy Trees (Bonus)
**Goal:** Display ascendancy nodes separately or overlaid

1. **Ascendancy filtering** - node.IsAscendancy, group by AscendancyName
2. **Toggle visibility** - Checkbox to show/hide ascendancy nodes
3. **Separate rendering** - Ascendancy nodes in different color/layer
4. **Position offsets** - Ascendancy nodes rendered around class start

**Validation:** Toggle ascendancy → shows 8 small trees around edge

**Complexity:** Low (filtering + rendering, positions already calculated)

**Dependencies:** Phase 1 (uses same sprite rendering)

## Recommended Build Order Rationale

**Start with Phase 1 (Sprites)** because it's the biggest visual upgrade with minimal architectural change. Validates SpriteSheetService pattern before building on it.

**Phase 2 (Stats)** next because tooltips are independent of editing. Users can explore tree with nice tooltips while editing is in progress.

**Phase 3 (Editing)** is core functionality, builds on stable sprite rendering. State management must work before search/minimap reference it.

**Phase 4 (Search)** after editing because searching requires the selection/highlight mechanism from editing.

**Phase 5 (Minimap)** reuses rendering from Phase 1, benefits from editing (shows allocations) and search (can highlight results).

**Phase 6 (Ascendancy)** is bonus polish, leverages all previous work.

## Anti-Patterns to Avoid

### Anti-Pattern 1: Recreating DrawOperation Every Frame

**What people do:**
```csharp
public override void Render(DrawingContext context)
{
    var operation = new SkillTreeDrawOperation(...); // Created every frame
    context.Custom(operation);
}
```

**Why it's wrong:** DrawOperation.Equals() returns false (correctly), so Avalonia doesn't cache. Recreating is expected, but **passing large data** (entire SkillTreeData) causes GC pressure.

**Do this instead:**
```csharp
// Current code already does this correctly
public override void Render(DrawingContext context)
{
    var operation = new SkillTreeDrawOperation(
        Bounds,                   // Small struct
        TreeData,                 // Reference (not copied)
        AllocatedNodeIds,         // Reference
        (float)ZoomLevel,         // Primitive
        _offsetX, _offsetY);      // Primitives
    context.Custom(operation);
}
```

**Key insight:** Passing references to shared data (TreeData) is fine; the DrawOperation doesn't own or copy it. Avoid creating new collections per frame.

### Anti-Pattern 2: Loading Sprites in Render Loop

**What people do:**
```csharp
private void DrawNodes(SKCanvas canvas)
{
    foreach (var node in nodes)
    {
        var sprite = SKBitmap.Decode($"sprites/{node.Icon}.png"); // DISASTER
        canvas.DrawBitmap(sprite, ...);
        sprite.Dispose();
    }
}
```

**Why it's wrong:** Render loop runs at 60fps. Loading 3000 sprites per frame = instant freeze. Even with caching, Decode() per node is too slow.

**Do this instead:**
```csharp
// Service pre-loads sheets, render loop just extracts
private void DrawNodes(SKCanvas canvas)
{
    var sheet = _spriteService.GetSheet("normalActive", zoomLevel); // Cached
    foreach (var node in nodes)
    {
        var rect = _spriteService.GetCoords(node.Icon); // Lookup
        canvas.DrawBitmap(sheet, rect, destRect); // Fast blit
    }
}
```

**Key insight:** Render loop should only read cached data and draw. All I/O, parsing, and allocations happen in initialization or async tasks.

### Anti-Pattern 3: Mutating TreeData Directly

**What people do:**
```csharp
void ToggleNode(int nodeId)
{
    if (TreeData.Nodes.TryGetValue(nodeId, out var node))
    {
        node.IsAllocated = !node.IsAllocated; // Modifying shared data
    }
}
```

**Why it's wrong:** SkillTreeData is shared (cached by SkillTreeDataService). Multiple windows viewing same build would interfere. PassiveNode model has no IsAllocated field (correctly).

**Do this instead:**
```csharp
// Allocated state lives in UI component, not data model
void ToggleNode(int nodeId)
{
    if (!AllocatedNodeIds.Remove(nodeId))
        AllocatedNodeIds.Add(nodeId); // UI state only
}
```

**Key insight:** Separation of concerns—data models are immutable, UI components own transient state. TreeData is read-only reference data.

### Anti-Pattern 4: Synchronous Sprite Loading on Startup

**What people do:**
```csharp
public TreeViewerWindow()
{
    InitializeComponent();
    _spriteService.LoadAllSprites(); // Blocks UI thread for 5 seconds
    LoadTree();
}
```

**Why it's wrong:** User sees frozen window, no feedback. Large sprite sheets (5-10MB) take time to download and decode.

**Do this instead:**
```csharp
public TreeViewerWindow()
{
    InitializeComponent();
    _ = LoadTreeAsync(); // Fire and forget
}

private async Task LoadTreeAsync()
{
    // Show tree with colored dots immediately
    var treeData = await _treeDataService.GetTreeDataAsync();
    TreeCanvas.TreeData = treeData;

    // Load sprites in background, re-render when ready
    await _spriteService.InitializeAsync(treeData);
    TreeCanvas.InvalidateVisual(); // Now shows sprites
}
```

**Key insight:** Progressive rendering—show basic view fast, enhance with sprites async. User sees content immediately, sprites upgrade visual fidelity when ready.

### Anti-Pattern 5: Recreating ObservableCollection on Every Change

**What people do:**
```csharp
void FilterNodes()
{
    FilteredNodes = new ObservableCollection<PassiveNode>(
        _nodes.Where(n => n.Name.Contains(SearchText))); // Full recreate
}
```

**Why it's wrong:** Creating new collection triggers full ListBox re-render, loses selection, scroll position resets. Performance degrades with large result sets.

**Do this instead:**
```csharp
void FilterNodes()
{
    var results = _nodes.Where(n => n.Name.Contains(SearchText)).ToList();

    // Update existing collection (preserves bindings)
    FilteredNodes.Clear();
    foreach (var node in results)
        FilteredNodes.Add(node);
}

// OR use ReactiveUI for advanced scenarios
this.WhenAnyValue(x => x.SearchText)
    .Throttle(TimeSpan.FromMilliseconds(300))
    .Select(text => _nodes.Where(n => n.Name.Contains(text)))
    .ObserveOn(RxApp.MainThreadScheduler)
    .Subscribe(results =>
    {
        FilteredNodes.Clear();
        FilteredNodes.AddRange(results);
    });
```

**Key insight:** Modify collection in-place when possible. For search, debounce input to avoid filtering on every keystroke.

## Memory Management Considerations

### SKBitmap Disposal Strategy

**Problem:** SKBitmap wraps native Skia memory (not .NET GC'd). Failing to dispose causes memory leaks.

**Solution:**
```csharp
public class SpriteSheetService : IDisposable
{
    private ConcurrentDictionary<string, SKBitmap> _cache = new();

    public void Dispose()
    {
        foreach (var bitmap in _cache.Values)
            bitmap?.Dispose();
        _cache.Clear();
    }
}

// In TreeViewerWindow
protected override void OnClosed(EventArgs e)
{
    base.OnClosed(e);
    _spriteService?.Dispose(); // Release native memory
}
```

**Lifespan:** SpriteSheetService lives for duration of TreeViewerWindow. Dispose when window closes. Don't dispose SKBitmaps while they might be referenced in render loop.

### Sprite Sheet Memory Budget

**Sprite sheet sizes:**
- Normal nodes: ~2048x2048 RGBA = 16MB per sheet
- 4 zoom levels × 6 categories (normal, notable, keystone, etc.) = ~24 sheets
- **Total:** ~384MB worst case

**Optimization:** Only load sheets for visible zoom level (±1 for transitions) = ~96MB typical.

**Implementation:**
```csharp
private void OnZoomChanged(float newZoom)
{
    var newZoomIndex = DetermineOptimalSpriteZoom(newZoom);
    if (newZoomIndex != _currentZoomIndex)
    {
        // Keep current + adjacent zoom levels, unload others
        UnloadSheetsExcept(new[] {
            newZoomIndex - 1,
            newZoomIndex,
            newZoomIndex + 1
        });
        _currentZoomIndex = newZoomIndex;
    }
}
```

## Sources

**SkiaSharp Rendering:**
- [How to Implement Smooth Image Scaling and Rotation in SkiaSharp](https://skiasharp.com/how-to-implement-smooth-image-scaling-and-rotation-in-skiasharp/)
- [SkiaSharp Cross-Platform .NET Graphics Library](https://skiasharp.com/)
- [Sprite Controls - DrawnUI for .NET MAUI](https://drawnui.net/articles/controls/sprites.html)
- [SKBitmap Class Reference](https://api.skia.org/classSkBitmap.html)
- [SKBitmap Class - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/skiasharp.skbitmap?view=skiasharp-2.88)

**Avalonia Integration:**
- [Using SkiaSharp with AvaloniaUI Controls - Discussion #13527](https://github.com/AvaloniaUI/Avalonia/discussions/13527)
- [CustomSkiaPage.cs Sample](https://github.com/AvaloniaUI/Avalonia/blob/master/samples/RenderDemo/Pages/CustomSkiaPage.cs)
- [How To Draw Graphics - Avalonia Docs](https://docs.avaloniaui.net/docs/guides/graphics-and-animation/graphics-and-animations)

**Path of Exile Tree Architecture:**
- [Passive Skill Tree JSON - PoE Wiki](https://www.poewiki.net/wiki/Passive_Skill_Tree_JSON)
- [Passive Skill Tree JSON - Path of Exile Wiki](https://pathofexile.fandom.com/wiki/Passive_Skill_Tree_JSON)
- [Zoom level sprite bug on website passive skill tree](https://devtrackers.gg/pathofexile/p/01d30089-zoom-level-sprite-bug-on-website-passive-skill-tree)
- [skilltree-export - GitHub](https://github.com/grindinggear/skilltree-export)

**State Management:**
- [Introduction to the MVVM Toolkit - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [MVVM Pattern in Blazor For State Management](https://www.syncfusion.com/blogs/post/mvvm-pattern-blazor-state-management)
- [Observable grouped collection APIs](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/observablegroupedcollections)

**UI Patterns:**
- [A 2d Viewport for Canvas](https://karlagius.com/2013/03/23/a-2d-viewport-for-canvas/)
- [Godot Create Your First 2D Minimap](https://medium.com/@merxon22/godot-create-your-first-2d-minimap-c43dfda01802)
- [Creating a minimap for the canvas - Fabric.js](https://fabric5.fabricjs.com/build-minimap)
- [AutoCompleteBox - Avalonia Docs](https://docs.avaloniaui.net/docs/reference/controls/autocompletebox)
- [Binding to Sorted/Filtered Data - Avalonia Docs](https://docs.avaloniaui.net/docs/concepts/reactiveui/binding-to-sorted-filtered-list)

**Text Rendering:**
- [Draw Text in a Rectangle with SkiaSharp](https://swharden.com/csdv/skiasharp/drawtext-rectangle/)
- [SkiaSharp.TextBlocks - GitHub](https://github.com/wouterst79/SkiaSharp.TextBlocks)
- [Performance Tuning DrawShapedText](https://www.mrumpler.at/performance-tuning-drawshapedtext/)

**Tree URL Encoding:**
- [Passive Skill Tree Node Encoding - PoE Forum](https://www.pathofexile.com/forum/view-thread/83305)
- [Path of Exile Developer Docs](https://www.pathofexile.com/developer/docs/reference)

---
*Architecture research for: Full Visual Skill Tree Integration in PathPilot*
*Researched: 2026-02-15*
*Confidence: HIGH (SkiaSharp patterns verified, PoE tree structure documented, existing PathPilot architecture analyzed)*
