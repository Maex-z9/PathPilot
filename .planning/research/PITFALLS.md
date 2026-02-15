# Domain Pitfalls: SkiaSharp Skill Tree Rendering Upgrade

**Domain:** Upgrading SkiaSharp-based skill tree viewer from simple dots to full sprite rendering with editing and search
**Researched:** 2026-02-15
**Confidence:** HIGH

## Critical Pitfalls

### Pitfall 1: SKBitmap Memory Leaks with Sprite Caching

**What goes wrong:**
SkiaSharp wraps native Skia resources (SKBitmap, SKCanvas, SKSurface) which must be manually disposed. Creating and disposing SKBitmap/SKCanvas in loops causes memory leaks where unmanaged memory is not released even after calling Dispose(). When caching large sprite atlases (which you must do for performance), forgetting to dispose OR disposing too early both cause critical failures. The memory leak persists even with proper using statements in some scenarios.

**Why it happens:**
Developers assume .NET garbage collection handles native resources. SkiaSharp's C# wrapper objects can be GC'd while native memory remains allocated. When caching bitmaps for sprite sheets, developers create them once but never dispose them, or dispose them when the cache is cleared but continue to use references elsewhere. The separation between managed wrapper lifetime and native resource lifetime creates confusion.

**How to avoid:**
- Create sprite atlas SKBitmap/SKImage objects exactly ONCE at startup and hold them for application lifetime
- Store in static or singleton service, NOT per-control instance (multiple TreeCanvas instances = multiple atlas loads)
- Use SKImage.FromEncodedData() for cached sprites instead of SKBitmap - SKImage can hold encoded data and decode lazily
- Only dispose when application shuts down or user explicitly unloads all builds
- For temporary operations (not caching), ALWAYS use `using` statements
- Monitor native memory with performance profilers, not just .NET heap

**Warning signs:**
- Memory usage grows with each tree view opened, even after window closes
- Task Manager shows memory not released when switching between builds
- After viewing 5-10 different builds, application memory usage is 500MB+ higher
- OOM exceptions despite "small" managed heap size

**Phase to address:**
Phase 1 (Sprite Rendering Foundation) - Implement sprite cache service with explicit lifecycle management BEFORE rendering sprites

---

### Pitfall 2: ICustomDrawOperation.Equals Always False Breaks with Sprite State

**What goes wrong:**
Your current implementation returns `false` from ICustomDrawOperation.Equals() to prevent Avalonia caching, which forces redraw every frame. This works fine with 1300 simple circles. When upgrading to sprites with search highlighting, hover effects, selection state, and minimap overlays, you create NEW ICustomDrawOperation instances on every state change (hover node changes, search query typed, node selected). Since Equals() always returns false, Avalonia re-renders EVERYTHING even when only one node changed color. With sprite rendering (expensive DrawImageRect calls), this causes frame drops from 60fps to <20fps.

**Why it happens:**
The existing v1.0 code intentionally returns false to avoid Avalonia's render caching because transformation state (zoom/pan) changes frequently. Developers extend this pattern to sprite rendering without realizing the performance implications. When adding interactive features (search, hover, selection), every keystroke or mouse movement triggers full re-render of 1300+ sprite draws instead of just the changed nodes.

**How to avoid:**
- Implement proper Equals() logic comparing zoom, pan, allocated nodes, hovered node, search results, and selection state
- Return TRUE when nothing changed - let Avalonia cache the expensive sprite rendering
- Create separate draw operations for layers: background (rarely changes) → connections (rarely) → nodes (zoom changes) → highlights (hover/search)
- Use SKPicture or SKPictureRecorder to cache static portions (all unallocated nodes, all connections)
- Only invalidate visual when actual state changes, not on every mouse move
- Batch state changes: if user types "fire", don't search after each letter - debounce 150ms

**Warning signs:**
- FPS drops when typing in search box
- Profiler shows DrawImageRect called 1300+ times per frame
- Moving mouse over canvas causes stuttering
- Zoom/pan feels sluggish with sprites vs. smooth with circles

**Phase to address:**
Phase 2 (Interactive Features) - Before adding search/hover, refactor ICustomDrawOperation with layer separation and proper Equals()

---

### Pitfall 3: Coordinate Space Confusion with Sprite Positioning

**What goes wrong:**
Your existing code applies transformations via `canvas.Translate(-offsetX * zoom)` then `canvas.Scale(zoom)` to world coordinates. When switching from DrawCircle (which uses center point + radius) to DrawImage/DrawImageRect (which uses top-left corner + width/height), developers forget that sprite images are positioned by TOP-LEFT corner, not center. Node sprites appear offset by sprite-width/2. When zooming, sprites don't scale around their visual center, causing "swimming" effect where node graphics shift position during zoom. Hit testing breaks because FindNodeAtPosition() calculates distance from node center, but sprite visual center is actually at (x + spriteWidth/2, y + spriteHeight/2).

**Why it happens:**
Circle rendering uses center-based coordinates, sprite rendering uses corner-based. Developers port the existing node.CalculatedX/Y directly to DrawImage without realizing these are CENTER coordinates. The transformation order (Translate then Scale) works for circles but exposes the corner vs. center mismatch with sprites.

**How to avoid:**
- Create two coordinate systems: `NodeCenter` (calculated positions, used for physics/connections) and `SpriteTopLeft` (for rendering)
- Add helper: `GetSpriteDrawRect(PassiveNode node) => new SKRect(node.X - spriteWidth/2, node.Y - spriteHeight/2, node.X + spriteWidth/2, node.Y + spriteHeight/2)`
- Store sprite dimensions per node type (keystone=96x96, notable=64x64, normal=32x32 at 100% zoom)
- Update hit testing to use sprite bounds, not just radius from center
- Document in code: "CalculatedX/Y are NODE CENTERS for connections, adjust by sprite size for rendering"

**Warning signs:**
- Sprites appear offset from connection lines by ~16-32 pixels
- Hover detection misses the visual sprite area
- Sprites "swim" or shift position when zooming
- Selected node highlight ring doesn't align with sprite

**Phase to address:**
Phase 1 (Sprite Rendering Foundation) - Establish coordinate system convention before rendering first sprite

---

### Pitfall 4: Sprite Atlas DrawAtlas Performance Trap

**What goes wrong:**
DrawAtlas claims to be the high-performance API for drawing many sprites from one atlas. Developers load the PoE sprite sheet (2048x2048+), extract source rectangles for each node type, and call DrawAtlas with 1300+ sprites. Performance is WORSE than individual DrawImageRect calls. DrawAtlas has overhead from transforming each sprite's matrix, and if you use different SKSamplingOptions (needed for zoom levels - FilterQuality.Low at <0.1 zoom, High at >0.5 zoom), it breaks batching. The API is optimized for IDENTICAL sprites with different positions (like particle effects), not for varied sprites at varied scales with varied sampling.

**Why it happens:**
Developers see "DrawAtlas = fast sprite rendering" without understanding the use case. DrawAtlas is fast for drawing 10,000 identical grass sprites, not 1300 different node sprites with different sizes and sampling quality. PoE's sprite atlas contains different-sized sprites (small, notable, keystone, mastery, jewel socket), so you can't use a uniform transform. The API works best when ALL sprites share sampling options and transforms.

**How to avoid:**
- Use DrawImageRect for PoE skill tree - sprite variety and zoom-based quality changes make DrawAtlas unsuitable
- Batch DrawImageRect calls by node type (all keystones in one paint, all notables in another) to maximize GPU batching
- Pre-calculate source rectangles for each sprite type at startup, reuse across frames
- Use single SKPaint per node category to avoid paint switching overhead
- If atlas MUST be used, separate atlases per node type and use uniform sampling per atlas
- Profile both approaches with your actual sprite sheet before committing

**Warning signs:**
- DrawAtlas is slower than 1300 individual DrawCircle calls
- Changing zoom level causes frame drops (sampling option change breaks batches)
- Profiler shows high CPU time in matrix transform operations
- Different node types require separate DrawAtlas calls anyway, negating benefit

**Phase to address:**
Phase 1 (Sprite Rendering Foundation) - Test DrawImageRect vs DrawAtlas with prototype before implementing full renderer

---

### Pitfall 5: Mastery Node Data Structure Misinterpretation

**What goes wrong:**
Mastery nodes appear in the standard `nodes` array with `"m": true` flag. Developers treat them as regular allocable nodes, allowing users to click and allocate them directly. In PoE, mastery nodes are PASSIVE - they become allocable only when a connected notable is allocated, then show a popup menu to select ONE mastery effect. The tree JSON stores mastery selections as two uint16 values packed in uint32: [node_skill_hash | effect_hash]. Parsing this as a single int32 or not unpacking the two hashes causes mastery effects to not load or apply incorrectly.

**Why it happens:**
Mastery nodes are structurally identical to passive nodes in JSON (id, position, stats, connections), just with an `m` flag. Developers miss the flag or don't understand its significance. The bitwise packing of mastery selections (two 16-bit values in one 32-bit int) is not obvious from the data structure. PoB XML stores masteries differently than GGG JSON, so import logic must handle both.

**How to avoid:**
- Filter nodes with `isMastery == true` into separate collection at parse time
- Render mastery nodes differently (special icon, grayed out when not available)
- Implement mastery allocation rules: only available if ANY connected notable allocated
- Parse mastery selections with bitwise ops: `nodeHash = (uint16)(packed >> 16); effectHash = (uint16)(packed & 0xFFFF);`
- Store available mastery effects per node (from `masteryEffects` JSON object) for popup display
- Validate mastery allocations: max 1 effect per mastery group, require connected notable

**Warning signs:**
- Users can allocate mastery nodes without allocating connected notables
- Mastery effects don't persist after save/load
- Mastery node stats don't update when selected
- Tree URL decoding doesn't restore selected mastery effects

**Phase to address:**
Phase 3 (Mastery & Advanced Nodes) - After basic editing works, add mastery-specific logic

---

### Pitfall 6: Cluster Jewel Socket Dynamic Tree Expansion

**What goes wrong:**
Cluster jewels create DYNAMIC passive nodes when socketed in outer jewel sockets. The base tree JSON doesn't contain these nodes - they're generated at runtime based on jewel mods. Developers assume all nodes exist in the static tree data, so when a build imports with cluster jewels socketed, the node IDs in the tree URL don't exist in the tree data dictionary. The viewer crashes or shows "missing nodes" errors. Cluster jewels can contain other jewel sockets (medium clusters in large, small in medium), creating nested dynamic trees that must be generated recursively.

**Why it happens:**
PoE's base passive tree is static (3.25.0 has fixed node IDs), but cluster jewels add dynamic content. The complexity of generating 8-12 nodes per large cluster, 4-6 per medium, 2-3 per small - each with notable passive selection from a pool - is underestimated. PoB's SkillTreeData stores cluster jewel templates, not generated instances. Developers don't realize cluster nodes need special handling.

**How to avoid:**
- Phase 1-3: Explicitly NOT support cluster jewels - they're optional endgame content
- Show warning when importing build with cluster jewels: "This build uses cluster jewels which are not yet supported"
- Detect cluster nodes in tree URL (IDs in specific ranges or not in base tree) and filter them out with warning
- Phase 4+ (if implementing clusters): Create cluster jewel parser that generates dynamic nodes based on jewel mods
- Store cluster jewel items separately from allocated nodes
- Generate cluster nodes dynamically when jewel socketed, remove when jewel removed
- Recursively handle nested jewel sockets (large contains medium, medium contains small)

**Warning signs:**
- Build imports successfully but shows far fewer allocated nodes than expected
- Console logs show "node ID 12345 not found in tree data"
- Missing chunks of nodes in tree URL that correspond to outer socket areas
- Builds imported from PoB show different point counts

**Phase to address:**
Phase 1 (Sprite Rendering) - Add cluster jewel DETECTION and warning, explicitly defer support
Phase 4+ (Cluster Jewels) - If needed, implement dynamic node generation

---

### Pitfall 7: Ascendancy Tree Coordinate Space Separation

**What goes wrong:**
Ascendancy nodes exist in completely separate coordinate space from main tree. Your current code skips ascendancy connections (`if (node.IsAscendancy) continue;`) to avoid long lines from center to edges. When adding ascendancy tree rendering, developers try to render ascendancy nodes in the same viewport as main tree, resulting in tiny ascendancy clusters at extreme coordinates off-screen. Ascendancy nodes have different group positions that don't relate to main tree's 14000x11000 offset. The tree JSON's min_x/max_x explicitly excludes ascendancy nodes because of "odd locations."

**Why it happens:**
Ascendancy nodes appear in the same `nodes` array with same position data structure (group, orbit, orbitIndex). Developers assume same coordinate system. The `AscendancyName` field and `IsAscendancy` flag indicate different rendering, but the spatial separation is not obvious. PoE's official tree viewer renders ascendancy as separate overlays, not in the same world space.

**How to avoid:**
- Render ascendancy trees as separate layer/overlay, NOT in main tree coordinate space
- Calculate ascendancy node positions separately, without the 14000/11000 offset
- Group ascendancy nodes by AscendancyName (Juggernaut, Berserker, etc.)
- Position each ascendancy group in fixed UI space (e.g., 4 quadrants around character portrait)
- Don't apply zoom/pan transformations to ascendancy overlay - keep it fixed scale
- Alternative: Show ascendancy in separate popup window when user clicks "View Ascendancy"
- Filter ascendancy nodes from main tree rendering entirely, handle separately

**Warning signs:**
- Ascendancy nodes appear 20,000+ pixels away from main tree
- Can't find ascendancy nodes even when fully zoomed out
- Allocated ascendancy nodes don't show as highlighted
- Connections between ascendancy nodes cross entire tree

**Phase to address:**
Phase 3 (Ascendancy View) - After main tree sprite rendering works, add separate ascendancy overlay

---

### Pitfall 8: Search Highlighting Re-render Performance Collapse

**What goes wrong:**
User types in search box "increased damage". Search logic finds 200+ matching nodes, stores IDs in HashSet, invalidates visual to show highlights. Existing ICustomDrawOperation.Equals() returns false, so ALL 1300+ nodes re-render even though only 200 need highlight overlay. Typing each character triggers full re-render. At 1300 DrawImageRect calls per frame @ 60fps target = 16.6ms budget, spending 8-12ms just on sprite draws leaves no time for highlight overlays, text rendering, or search matching. Frame rate drops to 15-20fps while typing.

**Why it happens:**
Search highlighting is added as overlay in same render pass without layer separation. Every keystroke changes search results HashSet, causing InvalidateVisual(). ICustomDrawOperation Equals() doesn't cache when only highlights changed. Developers don't realize that highlighting 200 nodes requires rendering DIFFERENT graphics for those 200, which breaks sprite batching if using DrawAtlas, and adds 200 extra draw calls if using overlays.

**How to avoid:**
- Debounce search: only execute search 150-200ms after user stops typing (use Rx timer or CancellationToken)
- Separate rendering layers: base nodes (cached) → highlight overlays (re-render only when search changes)
- Implement ICustomDrawOperation.Equals() properly: return true when search results unchanged
- Use SKPictureRecorder to cache base node rendering, overlay highlights on playback
- Render highlights as simple circles or outline strokes, not full sprite re-draws
- Limit search to 100 results max, show "...and 50 more" message
- Consider rendering search results as separate overlay canvas positioned on top

**Warning signs:**
- Typing in search box causes frame drops
- Profiler shows high CPU time in DrawImageRect during search
- Entire tree flickers when search updates
- Search feels "laggy" with 200ms+ delay between typing and visual update

**Phase to address:**
Phase 2 (Search & Highlighting) - Implement layer separation and debouncing BEFORE adding search

---

### Pitfall 9: Minimap Second Viewport Render Duplication

**What goes wrong:**
Developers implement minimap by creating second SkillTreeCanvas instance with smaller bounds, rendering same tree data at smaller zoom. Now every state change (hover, search, selection) renders tree TWICE - once for main view, once for minimap. 1300 sprites × 2 viewports = 2600 DrawImageRect calls per frame. Minimap doesn't need sprite details at 10% scale - 32x32 sprites become 3x3 pixels, but code still loads and renders full-res sprites. Memory doubles (two sprite caches), frame time doubles.

**Why it happens:**
Reusing existing SkillTreeCanvas for minimap seems like code reuse. Developers don't realize minimap needs different level-of-detail. The minimap should show: allocated node positions (gold dots), connections between allocated nodes, current viewport rectangle. It doesn't need: individual sprites, hover tooltips, search highlights, detailed connections. Rendering everything twice is wasteful.

**How to avoid:**
- Don't reuse SkillTreeCanvas for minimap - create MinimapCanvas with simplified rendering
- Minimap renders: simple dots for nodes (no sprites), thin lines for allocated paths, rectangle for viewport
- Update minimap only when allocated nodes change or viewport moves, NOT on hover/search
- Use lower resolution render target for minimap (256x256 texture max)
- Consider static image approach: render minimap to SKBitmap on build load, overlay viewport rectangle only
- Skip hover effects, tooltips, and search highlighting in minimap entirely
- Update minimap at 10fps max (every 100ms), not 60fps

**Warning signs:**
- FPS drops by 50% when minimap is visible
- Profiler shows double rendering time when minimap enabled
- Memory usage nearly doubles with minimap
- Minimap sprites are invisible/blurry at small scale anyway

**Phase to address:**
Phase 5 (Minimap) - Create separate simplified minimap renderer, don't reuse main canvas

---

### Pitfall 10: Path Validation Breaking Existing Allocations

**What goes wrong:**
When implementing node editing, developers add path validation: "node is allocable only if connected to an already-allocated node or is starting node." Existing builds imported from PoB fail validation because tree URL decoding provides node IDs without ordering information. Validation tries to process nodes in arbitrary order (dictionary iteration), finds nodes without connection to start, marks build as invalid. User sees "Cannot allocate node X - not connected to tree" for builds that work perfectly in PoB.

**Why it happens:**
PoB allocates nodes in specific order during build creation, ensuring each new node connects to existing path. The tree URL stores only WHICH nodes are allocated, not the ORDER they were allocated. When loading a build, all nodes are allocated "simultaneously" from the validator's perspective. Developers implement validation as "before allocating node X, check if it connects," which works for interactive editing but breaks bulk import.

**How to avoid:**
- Separate validation modes: INTERACTIVE (strict, during editing) vs. IMPORT (lenient, during load)
- Import mode: accept all nodes from tree URL, then validate connectivity as warning, not error
- Implement pathfinding: when importing, find shortest path from start node to each allocated node
- If no path exists, mark node as "orphaned" but still show it (with warning icon)
- Interactive mode: only allow allocating nodes that connect to existing path
- Store allocation order in saved builds (not in tree URL, but in PathPilot's JSON)
- Use BFS from starting node to build reachable set, allow allocating any reachable node

**Warning signs:**
- Imported builds show errors despite working in PoB
- User can't edit imported builds because validation blocks changes
- Console shows "node not connected" for nodes that are clearly part of path tree
- Builds become "corrupted" after import

**Phase to address:**
Phase 2 (Node Editing) - Implement dual validation modes from the start

---

## Technical Debt Patterns

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Storing sprites in control instance | Easy access in render method | Memory leak, multiple loads, can't share cache | Never - always use singleton service |
| ICustomDrawOperation.Equals() always false | Simplicity, guaranteed redraw | Poor performance with sprites, frame drops | Only for prototype/POC, remove before Phase 1 complete |
| DrawAtlas for varied sprites | "It's the fast API" mentality | Worse performance than DrawImageRect, complexity | Never for PoE tree - only for particle effects |
| Reusing main canvas for minimap | Code reuse, DRY principle | 2x render cost, 2x memory, poor minimap quality | Never - minimap needs different LOD |
| Single-pass rendering (all layers mixed) | Simple render logic | Can't cache anything, full redraw always | MVP only, refactor to layers in Phase 2 |
| Synchronous sprite loading | Simpler code flow | UI freezes during load, poor UX | Never - always async load with placeholder |
| Global InvalidateVisual() on any change | Ensures screen updates | Massive performance waste | Only during initial development, remove by Phase 1 end |
| Treating mastery nodes as regular nodes | Less code, simpler logic | Wrong behavior, invalid allocations | Never - flag at parse time |

## Integration Gotchas

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| GGG skilltree-export JSON | Assuming all nodes are regular passives | Filter by `isMastery`, `isAscendancy`, `isJewelSocket` flags at parse time |
| Sprite atlas coordinates | Using pixel coords from JSON directly | Validate against sprite sheet dimensions, handle missing sprites gracefully |
| PoB tree URL decoding | Using stored AllocatedNodes from old imports | Always decode URL directly, ignore stored nodes (might be from buggy decoder) |
| Avalonia ICustomDrawOperation | Returning true from Equals() too liberally | Compare ALL state: zoom, pan, allocated nodes, hover, search - miss one = stale render |
| SKBitmap disposal | Disposing cached sprites on window close | Keep sprites until app shutdown, dispose in Application.OnExit |
| Tree JSON min/max bounds | Using for ascendancy node rendering | Explicitly excludes ascendancy - calculate separately |

## Performance Traps

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| Full tree re-render on hover | Frame drops when moving mouse | Layer separation, proper Equals(), SKPicture caching | >500 sprites at >30fps target |
| Sprite loading synchronous | UI freeze 500-2000ms on build load | Async load with placeholder circles, background thread | Any sprite atlas >512KB |
| Search without debouncing | Keystroke lag, typing feels unresponsive | Debounce 150ms, use CancellationToken | >200 searchable nodes |
| Minimap full detail rendering | 50% FPS drop when minimap visible | Separate simplified minimap renderer | Minimap shows >100 nodes |
| DrawImageRect with new SKPaint each node | High GC pressure, frame time variance | Reuse SKPaint objects per node type | >1000 nodes rendered |
| InvalidateVisual() in pointer move handler | Constant redraws, poor battery life | Only invalidate when hover changes state | Always - fix immediately |
| No sprite rect caching | CPU time in rect calculations | Pre-calculate source rects at startup | >100 different sprite types |

## UX Pitfalls

| Pitfall | User Impact | Better Approach |
|---------|-------------|-----------------|
| No loading feedback for sprites | "Is it frozen?" uncertainty during load | Show progress: "Loading sprites... 45/100" |
| Search shows 500+ results | Overwhelming, slow rendering | Limit to 100, show "...and 400 more" |
| Can allocate invalid paths | User creates broken build | Real-time path validation, prevent invalid allocations |
| Minimap doesn't show viewport | Lost in huge tree, can't navigate | Always show current viewport rectangle in minimap |
| No visual distinction for mastery vs. passive | User allocates masteries incorrectly | Different sprite/icon for masteries, gray out when unavailable |
| Cluster jewel builds silently fail | User imports build, missing half the nodes | Show warning: "Cluster jewels not supported, X nodes hidden" |
| Zoom too sensitive | Small scroll = 10x zoom change | Clamp zoom steps: 1.1x in, 0.9x out (already implemented correctly) |

## "Looks Done But Isn't" Checklist

- [ ] **Sprite Rendering:** Sprites load successfully — verify sprites DISPOSE properly (check memory after 10 build loads)
- [ ] **Search:** Highlights appear when searching — verify doesn't re-render entire tree per keystroke (profile frame time)
- [ ] **Node Editing:** Can click to allocate nodes — verify path validation allows imported builds (test PoB imports)
- [ ] **Minimap:** Shows tree overview — verify uses simplified rendering, not full sprite pass (check if FPS halves)
- [ ] **Mastery Nodes:** Mastery nodes display — verify can't allocate without connected notable (test edge cases)
- [ ] **Ascendancy:** Ascendancy nodes visible — verify in separate coordinate space, not offset by 20k pixels
- [ ] **Zoom Quality:** Sprites look good at all zoom levels — verify sampling quality changes with zoom (check at 0.02x and 2.0x)
- [ ] **State Persistence:** Selected nodes save/load — verify mastery selections persist (load after save)
- [ ] **Memory Cleanup:** App runs for extended session — verify memory stable after 50+ build loads (profile)
- [ ] **Cluster Detection:** Cluster jewel builds import — verify shows warning, doesn't crash (test with cluster build)

## Recovery Strategies

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| SKBitmap memory leaks | MEDIUM | Refactor sprite cache to singleton service, add dispose on shutdown, force GC after old builds unload |
| ICustomDrawOperation always false | LOW | Implement Equals() with state comparison, measure before/after FPS with profiler |
| Coordinate space confusion | MEDIUM | Create coordinate helper class, refactor all Draw calls to use helper, update hit testing |
| DrawAtlas wrong usage | LOW | Replace with DrawImageRect batched by type, delete DrawAtlas code, verify performance improves |
| Mastery nodes as regular nodes | HIGH | Add mastery-specific data model, refactor allocation logic, add UI for mastery selection, migrate saved builds |
| Cluster jewel crashes | MEDIUM | Add cluster jewel detection at import, filter cluster nodes from tree, show warning UI |
| Ascendancy coordinate mismatch | MEDIUM | Create separate ascendancy renderer, calculate positions separately, add overlay or popup UI |
| Search re-render performance | MEDIUM | Add debouncing (2 hours), implement layer separation (1 day), add SKPicture caching (4 hours) |
| Minimap duplicates rendering | MEDIUM | Create MinimapCanvas with simplified render logic (1 day), remove reuse of main canvas |
| Path validation breaks imports | LOW | Add import vs. interactive validation modes (4 hours), implement BFS pathfinding (6 hours) |

## Pitfall-to-Phase Mapping

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| SKBitmap memory leaks | Phase 1 (Sprite Foundation) | Load 20 builds sequentially, verify memory returns to baseline |
| ICustomDrawOperation Equals | Phase 1 (Sprite Foundation) | Change zoom without state change, verify render skipped |
| Coordinate space confusion | Phase 1 (Sprite Foundation) | Verify sprite centers align with connections, hover hit testing accurate |
| DrawAtlas wrong usage | Phase 1 (Sprite Foundation) | Profile DrawImageRect vs DrawAtlas with actual sprites, choose winner |
| Search re-render | Phase 2 (Interactive Features) | Type search query, verify FPS stays >50, profiler shows debouncing |
| Path validation breaks imports | Phase 2 (Interactive Features) | Import 10 PoB builds, verify all allocations show correctly |
| Mastery nodes as regular | Phase 3 (Mastery & Advanced) | Try allocating mastery without connected notable, verify blocked |
| Ascendancy coordinate space | Phase 3 (Ascendancy View) | Verify ascendancy nodes visible without extreme zoom out |
| Cluster jewel crashes | Phase 1 (Detection only) | Import build with clusters, verify warning shown, no crash |
| Minimap duplicate rendering | Phase 5 (Minimap) | Enable minimap, verify FPS drop <10%, memory increase <50MB |

## PoE-Specific Data Gotchas

### Orbital Representation Inconsistency
The `oo` (orbit occupied) field in groups can be either boolean array OR associative array. Parser must handle both formats. When encountering array format, treat the index as the key.

### Mastery Effect Packing
Mastery selections in tree URLs are packed as two uint16 values in one uint32:
```csharp
uint32 packed = masteryData;
uint16 nodeHash = (uint16)(packed >> 16);
uint16 effectHash = (uint16)(packed & 0xFFFF);
```
Using int32 or not unpacking causes mastery effects to not load.

### Ascendancy Min/Max Bounds Exclusion
Tree JSON's `min_x`, `max_x`, `min_y`, `max_y` explicitly exclude ascendancy nodes due to "odd locations." Don't use these bounds for ascendancy rendering calculations.

### Sprite Sheet Coordinate Mapping
Sprite coordinates in JSON map icon paths to pixel locations in sprite sheet. Missing icons (new nodes in recent patches) cause crashes if not handled gracefully. Always validate sprite existence and fall back to colored circle.

### Hidden Nodes in Group Data
Some groups contain "hidden" nodes used for internal connections. These nodes have no stats and should not be rendered. Filter by checking if `stats` array is empty or if node has `isProxy` flag.

### Class Start Node IDs
Each class has a different starting node ID. When implementing path validation, must identify correct start node based on build's class (from PoB XML `classId` or build JSON).

### Jewel Socket Ranges
Regular jewel socket node IDs are in specific ranges. Cluster jewel socket IDs are in different ranges. Large cluster sockets are outermost, only accept large cluster jewels. Must validate jewel type matches socket type.

## Sources

**SkiaSharp Memory & Performance:**
- [SkiaSharp Memory Leak Issues - GitHub](https://github.com/mono/SkiaSharp/issues/1009)
- [SKCanvas Memory Leak - GitHub](https://github.com/mono/SkiaSharp/issues/192)
- [What needs to be disposed and cached - GitHub](https://github.com/mono/SkiaSharp/issues/829)
- [Optimizing Rendering Performance with Sprite Management](https://peerdh.com/blogs/programming-insights/optimizing-rendering-performance-in-2d-games-using-efficient-sprite-management-techniques)

**Sprite Atlas Rendering:**
- [Atlas Component - React Native Skia](https://shopify.github.io/react-native-skia/docs/shapes/atlas/)
- [Rendering Large Amount of Images - Skia Discussion](https://groups.google.com/g/skia-discuss/c/O_FFCRBANrQ)
- [DrawAtlas Method - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/skiasharp.skcanvas.drawatlas?view=skiasharp-2.88)

**PoE Skill Tree Data:**
- [Passive Skill Tree JSON - PoE Wiki](https://www.poewiki.net/wiki/Passive_Skill_Tree_JSON)
- [GGG Skill Tree Export - GitHub](https://github.com/grindinggear/skilltree-export)
- [Passive Skill Tree JSON Community Repo - GitHub](https://github.com/poe-tool-dev/passive-skill-tree-json)
- [Cluster Jewel Guide - PoE Vault](https://www.poe-vault.com/guides/cluster-jewel-guide)

**Avalonia ICustomDrawOperation:**
- [ICustomDrawOperation Render Not Called - GitHub](https://github.com/AvaloniaUI/Avalonia/issues/12247)
- [Drawing Many Objects Performance - Avalonia Discussion](https://github.com/AvaloniaUI/Avalonia/discussions/13149)
- [Flashing Artifacts on InvalidateVisual - Avalonia Discussion](https://github.com/AvaloniaUI/Avalonia/discussions/13291)

**SkiaSharp Coordinate Transforms:**
- [SkiaSharp Coordinate Transformation - Microsoft Q&A](https://learn.microsoft.com/en-us/answers/questions/1035159/skiasharp-(x-y)-on-canvasview-after-scale-and-tran)
- [SkiaScene - Pan and Zoom - GitHub](https://github.com/OndrejKunc/SkiaScene)
- [Building Infinite Canvas with Skia](https://robcost.com/building-an-infinite-canvas-with-skia-in-react-native/)

**Minimap Rendering Patterns:**
- [Minimap with Viewports - Godot Forum](https://forum.godotengine.org/t/how-to-do-in-2d-game-screen-in-one-viewport-and-minimap-in-another-different-sprites-on-each/17764)
- [2D Strategy Game Minimap - Patterns Game Programming](https://www.patternsgameprog.com/strategy-game-12-minimap)
- [Creating Efficient Viewport Minimap - Roblox DevForum](https://devforum.roblox.com/t/creating-an-efficient-viewport-minimap/1179030)

**PoE Search & Highlighting:**
- [Passive Skill Search - PoE Wiki](https://www.poewiki.net/wiki/Passive_skill)
- [PoE Skill Tree Guide](https://www.exitlag.com/blog/path-of-exile-skill-tree-guide/)

---
*Pitfalls research for: SkiaSharp Skill Tree Rendering Upgrade to Sprites, Editing, Search, and Minimap*
*Researched: 2026-02-15*
