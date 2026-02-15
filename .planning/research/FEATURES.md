# Feature Landscape

**Domain:** Path of Exile Skill Tree Viewer with Editing
**Researched:** 2026-02-15

## Table Stakes

Features users expect from any PoE skill tree viewer. Missing these means users will default to Path of Building.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| **Full visual rendering** | GGG website, PoB, all web planners use real node graphics not dots | High | Requires sprite loading, group backgrounds, node icons from GGG's skilltree-export repo (spritesheets introduced v3.20.0) |
| **Node icons by type** | Keystones, Notables, Normal nodes visually distinct in all viewers | Medium | Already have size differentiation (18f/12f/6f), need actual icon sprites from assets |
| **Stat tooltips** | Every viewer shows stats on hover - core functionality | Low | Already have hover detection + tooltips, just need to show `node.Stats` list (already parsed) |
| **Click to allocate/deallocate** | Standard editing pattern - left-click allocate, right-click deallocate | Medium | Need path validation (must connect to allocated nodes), point budget tracking |
| **Shortest path allocation** | PoB's shift+hover feature - users expect smart pathing | Medium | Dijkstra's algorithm on node graph, PoB uses this for "alternate path tracing" |
| **Zoom quality levels** | GGG data provides 4 zoom levels (0.1246, 0.2109, 0.2972, 0.3835) for asset swapping | High | Performance optimization - load different sprite quality based on zoom, already have zoom working |
| **Undo/Redo allocation** | Standard in skill tree planners - users experiment heavily | Low | Simple command pattern with allocation history stack |
| **Group backgrounds** | Visual clustering - shows related nodes together | Medium | GGG JSON has `background` property on groups (image, isHalfImage, offsetX/Y) since v3.20.0 |
| **Ascendancy tree display** | Part of skill tree, users need to see ascendancy nodes | Medium | Already parse `node.IsAscendancy`, need separate rendering zone near class start |
| **Jewel socket highlighting** | Critical mechanic - users need to find sockets easily | Low | Already parse `node.IsJewelSocket`, just need distinct visual treatment |

## Differentiators

Features that elevate the viewer beyond basic functionality. Not required for v1 but highly valued.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| **Node search with highlighting** | Find "life" or "crit" nodes instantly across 1300+ nodes | Medium | Text search through `node.Stats`, highlight matches in viewport, scroll-to-node |
| **Minimap overlay** | Fast navigation like GGG website - see where you are in massive tree | High | Render tree at tiny scale, show viewport bounds, click-to-navigate |
| **Stat aggregation sidebar** | Live DPS/defense totals as you allocate - PoB's killer feature | High | Complex: parse stat modifiers ("10% increased Physical Damage"), aggregate by type, calculate totals |
| **Path efficiency highlighting** | Show per-point power (PoB feature) - which nodes give best stats/point | High | Requires stat aggregation + scoring algorithm, very advanced |
| **Cluster jewel support** | PoE endgame mechanic - dynamically add node clusters | Very High | Cluster jewels create new node groups, need dynamic tree expansion, not v1 priority |
| **Import/Export allocation** | Share trees with others, load PoB builds | Medium | Already have PoB import for builds, extract allocated node IDs from tree URL |
| **Node comparison mode** | Compare 2+ paths side-by-side (which route better?) | Medium | Duplicate view state, show diffs, nice UX polish |
| **Point budget warning** | Visual indicator when approaching 123 point limit | Low | Simple counter + color coding (green → yellow → red) |
| **Class/Ascendancy selector** | Change starting position in tree viewer | Low | Update starting node, re-center view, validate path connectivity |

## Anti-Features

Features to explicitly NOT build or defer indefinitely.

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| **Full build calculation engine** | PoB already does this perfectly with 15+ years of refinement | Import builds from PoB, focus on visualization + editing |
| **Skill gem simulation** | Extremely complex, PoB handles all edge cases | Show gem info from PoB imports, don't calculate DPS ourselves |
| **Item editor** | Out of scope, not tree-related | Display items from PoB, link to external tools |
| **Trade integration** | Unrelated to tree planning | Link to poe.trade or official trade site if needed |
| **In-app tree comparison with online builds** | Massive scope creep, needs build database | Export tree URL, user can share manually |
| **Mastery effect selection** | PoE mechanic introduced later, GGG JSON has `isMastery` flag | Defer to Phase 3+ or never - complex UI, requires effect database |
| **Timeless jewel transformation** | Extremely niche, transforms node stats dynamically | Out of scope - PoB barely handles this well |
| **Animated node allocation** | Visual polish with minimal value | Static allocation is fine, spend time on features |

## Feature Dependencies

```
Visual Rendering
  ├─> Node Icons (required for visual quality)
  ├─> Group Backgrounds (required for visual quality)
  └─> Zoom Quality Levels (optional, performance optimization)

Node Editing
  ├─> Click Allocation (core editing)
  ├─> Path Validation (must connect to allocated nodes)
  ├─> Shortest Path (UX enhancement for editing)
  └─> Undo/Redo (required for good editing UX)

Stat Tooltips
  └─> No dependencies (data already parsed)

Node Search
  ├─> Stat Tooltips (users expect to see what they found)
  └─> Viewport Navigation (scroll-to-node after search)

Minimap
  ├─> Visual Rendering (minimap shows same visuals at small scale)
  └─> Viewport Tracking (show current view bounds)

Stat Aggregation (advanced)
  ├─> Node Editing (need to know what's allocated)
  ├─> Modifier Parsing (complex string parsing)
  └─> Calculation Engine (aggregate and calculate totals)
```

**Critical path for MVP:**
1. Stat tooltips (Low complexity, high value, data ready)
2. Node icons + group backgrounds (Visual quality baseline)
3. Click allocation + path validation (Core editing)
4. Undo/Redo (Required for editing UX)
5. Shortest path allocation (Expected editing enhancement)

**Defer to Phase 2:**
- Node search (Medium complexity, nice-to-have)
- Minimap (High complexity, navigation aid)
- Zoom quality levels (Performance optimization, not needed for desktop)

**Defer to Phase 3+ or Never:**
- Stat aggregation (Very high complexity, PoB already does this)
- Path efficiency highlighting (Requires stat aggregation)
- Cluster jewels (Complex, niche endgame mechanic)

## MVP Recommendation

Prioritize table stakes only:

1. **Stat tooltips** - Already have hover system, just display `node.Stats`
2. **Full visual rendering** - Load GGG sprite assets, render node icons + group backgrounds
3. **Click allocation** - Left-click allocate, right-click deallocate, path validation
4. **Shortest path** - Shift+hover to show path, click to allocate all
5. **Undo/Redo** - Stack-based allocation history
6. **Ascendancy display** - Render ascendancy nodes in separate zones
7. **Jewel socket highlighting** - Distinct color/frame for jewel sockets

**Defer:**
- Node search → Phase 2 (nice UX polish)
- Minimap → Phase 2 (navigation aid for large trees)
- Stat aggregation → Phase 3 or Never (PoB does this)
- Zoom quality levels → Performance optimization if needed

**Why this order:**
- Visual rendering is table stakes - users compare to GGG website
- Editing features build on each other (allocate → validate → undo → smart path)
- Stat tooltips are trivial and immediately useful
- Search and minimap are polish, not core functionality
- Stat aggregation is massive scope, defer indefinitely

## Technical Considerations

### Sprite Loading
- **GGG Data Source:** `https://github.com/grindinggear/skilltree-export` (PoE 2 only)
- **Community Source:** `poe-tool-dev/passive-skill-tree-json` (PoE 1, currently using v3.25.0)
- **Assets:** Spritesheets in `assets/` folder with JSON coords mapping
- **Sprites:** `normalActive`, `normalInactive`, `notableActive`, `notableInactive`, `keystoneActive`, `keystoneInactive`
- **Each sprite entry:** `{ filename, coords: { [iconPath]: { x, y, w, h } } }`

### Group Backgrounds
- **Property:** `groups[id].background = { image, isHalfImage?, offsetX, offsetY }`
- **Rendering:** Draw background sprite at group X/Y + offset before drawing nodes
- **Orbits:** Nodes positioned around group center using orbit radii (already have: `{ 0, 82, 162, 335, 493, 662, 846 }`)

### Node Allocation Graph
- **Validation:** Allocated nodes must form connected graph from class start node
- **Start nodes:** Class-specific (Scion at origin, others at specific positions)
- **Algorithm:** BFS/DFS from start node to validate connectivity

### Shortest Path
- **Algorithm:** Dijkstra's on unallocated nodes, find shortest path to target
- **Cost:** Distance in nodes (1 per node), or weighted by type (Keystone expensive, Normal cheap)
- **UI Pattern:** Shift+hover highlights path in gold, click to allocate all nodes in path

### Performance
- **Current:** Rendering 1300+ nodes as circles with batched paths - works fine
- **With sprites:** Each node is texture draw instead of circle - more GPU work
- **Optimization:** Cull nodes outside viewport (already have viewport bounds from zoom/pan)
- **Zoom levels:** GGG provides 4 quality levels, can swap based on zoom (complex, defer)

### SkiaSharp Integration
- **Current rendering:** Custom `ICustomDrawOperation` with direct SKCanvas access
- **Sprite loading:** `SKBitmap.Decode()` from HTTP stream or file
- **Spritesheet rendering:** `canvas.DrawBitmap(bitmap, srcRect, destRect, paint)`
- **Caching:** Load spritesheets once, cache `SKBitmap` instances, dispose on cleanup

## User Expectations from Reference Tools

### Path of Building
- **Alternate path tracing:** Shift+hover sequence of nodes, click to allocate all
- **Node search:** Search bar at bottom, finds by name or stat text
- **Per-point power:** Highlight nodes by efficiency (red = bad, green = good)
- **Stat panel:** Right sidebar shows totals, updates live as you allocate
- **Tree URL import/export:** Share builds via URL or code

### GGG Official Website
- **Beautiful rendering:** Full sprites, group backgrounds, smooth zoom
- **Minimap:** Bottom-right corner shows full tree, viewport box, click-to-navigate
- **Smooth zoom/pan:** Mouse wheel zoom, drag to pan, very responsive
- **Node tooltips:** Hover shows name + stats, no lag
- **Ascendancy tabs:** Click class portrait to switch between ascendancies

### Web Planners (poeplanner.com, poe-tree.com)
- **Click allocation:** Left-click allocate, right-click deallocate (standard)
- **Point counter:** Shows "Points: 95 / 123" prominently
- **Reset button:** Clear all allocations instantly
- **Class selector:** Dropdown to change starting class
- **URL sharing:** Every change updates URL, easy to share

## Sources

Research based on:
- [Path of Building Community Fork](https://pathofbuilding.community/) - Features: oil combinations, node highlighting by power, search, alternate path tracing
- [GGG Skill Tree Export Repository](https://github.com/grindinggear/skilltree-export) - Group backgrounds via `background` property, spritesheets introduced v3.20.0
- [Passive Skill Tree JSON Documentation](https://www.poewiki.net/wiki/Passive_Skill_Tree_JSON) - Node properties: `id`, `dn`, `g`, `o`, `oidx`, `sa/da/ia`, `ks/not/m`, `isJewelSocket`, `out`, `sd`, `icon`
- [PoE Planner](https://poeplanner.com/) - Tool for planning passive skill tree, atlas tree, equipment, skills
- [Path of Building Guide - Civenge](https://civenge.com/how-to-use-path-of-building-planning-path-of-exile-builds/) - Search bar for finding nodes by keyword, hover tooltips, persistent tooltips on click
- [PathOfPathing GitHub](https://github.com/Lilylicious/PathOfPathing) - Optimal-ish pathing generator using shortest path algorithms
- [Cluster Jewels Documentation](https://www.poewiki.net/wiki/Cluster_jewel) - Large/Medium/Small cluster jewels, socket hierarchy, extends skill tree dynamically
- [PoE Passive Skill Documentation](https://www.poewiki.net/wiki/Passive_skill) - Node types: Attribute (+5), Small Passives (minor bonuses), Notables (significant boost), Jewel Sockets (modular), Keystones (dramatic mechanics)
- [Ascendancy Class Documentation](https://www.poewiki.net/wiki/Ascendancy_class) - Ascendancy tree shown as tab near class start, 12-16 passives per tree, gated by generic nodes
