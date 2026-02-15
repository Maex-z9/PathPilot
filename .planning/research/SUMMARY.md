# Research Summary: Full Visual Skill Tree Integration

**Domain:** SkiaSharp Skill Tree Enhancement for PathPilot
**Researched:** 2026-02-15
**Overall confidence:** HIGH

## Executive Summary

PathPilot already has a solid foundation for skill tree rendering with native SkiaSharp, proven zoom/pan/hover interactions, and orbit-based position calculations. Upgrading to full visual features (sprite sheets, stat tooltips, node search, minimap, and editing) is **primarily an extension project, not a rewrite**. The existing architecture is sound; we're adding visual polish and interaction layers on top.

**Key strategic insight:** GGG's tree JSON already contains everything needed—sprite coordinates, zoom level mappings, group backgrounds, node connections, stats. The existing codebase parses this data but only uses ~30% of it (positions, connections, basic node types). Full visual rendering means **using the other 70%** that's already parsed and cached.

The architecture follows PathPilot's proven patterns:
- **Services for resources:** SpriteSheetService mirrors GemIconService (download + cache sprites)
- **ICustomDrawOperation for rendering:** Extend existing SkillTreeDrawOperation to draw sprites instead of circles
- **StyledProperty for state:** Add editable AllocatedNodeIds as ObservableCollection
- **MVVM for search:** Standard Avalonia pattern with filtered ObservableCollection

**Critical dependencies validated:**
- SkiaSharp 2.88+ has native sprite atlas rendering (DrawBitmap with source/dest rects)
- No new major dependencies required (FuzzySharp optional for search)
- All features achievable with .NET built-ins + existing SkiaSharp integration
- GGG sprite data publicly available via poe-tool-dev/passive-skill-tree-json

## Key Findings

**Stack:** SkiaSharp + Avalonia + .NET 8 (already integrated, zero new dependencies for core features)
**Architecture:** Layer-based rendering (backgrounds → connections → nodes → overlays), service-based sprite caching, MVVM search/editing
**Critical pitfall:** SKBitmap memory leaks if sprites cached improperly—use singleton service, dispose on app shutdown only

## Implications for Roadmap

Based on research, suggested phase structure:

### Phase 1: Sprite Foundation (2-3 days)
**Goal:** Replace colored dots with real PoE sprites, establish caching architecture

**Rationale:** Visual upgrade with minimal risk. Validates SpriteSheetService pattern before building on it.

**Addresses:**
- Full visual rendering (FEATURES.md table stakes)
- Sprite sheet loading and caching (STACK.md implementation)
- Group background rendering

**Avoids:**
- SKBitmap memory leak pitfall (use singleton service from day 1)
- Coordinate space confusion (establish center vs corner convention)
- DrawAtlas performance trap (use DrawImageRect)

**Complexity:** Medium (sprite coordinate parsing, HTTP download, caching logic)

**Dependencies:** None (pure rendering upgrade, no state changes)

---

### Phase 2: Stats Tooltips & Search (1-2 days)
**Goal:** Enhanced tooltips with formatted stats, node search with highlighting

**Rationale:** Low complexity, high value. Stats already parsed, search is standard MVVM.

**Addresses:**
- Stat tooltips (FEATURES.md table stakes)
- Node search (FEATURES.md differentiator)
- Text rendering in SkiaSharp

**Avoids:**
- Search re-render performance collapse (debounce, layer separation)
- Full tree re-render on hover (proper ICustomDrawOperation.Equals)

**Complexity:** Low-Medium (text layout in SkiaSharp requires manual line breaks)

**Dependencies:** Phase 1 (sprites + stats together look cohesive)

---

### Phase 3: Node Editing (2 days)
**Goal:** Click to allocate/deallocate nodes, path validation, undo/redo

**Rationale:** Core editing functionality. Must work before search/minimap reference editable state.

**Addresses:**
- Click allocation (FEATURES.md table stakes)
- Path validation
- TreeUrlEncoder (encode HashSet<int> → base64 URL)
- Undo/redo stack

**Avoids:**
- Path validation breaking imports (dual validation modes)
- Mastery nodes as regular nodes (filter at parse time)
- ObservableCollection mutation triggering full re-render

**Complexity:** Medium (state management, URL encoding, validation algorithm)

**Dependencies:** Phase 1 (visual feedback on allocation needed)

---

### Phase 4: Node Search (1 day)
**Goal:** Filter nodes by name/stats, jump to matches, highlight results

**Rationale:** Standard MVVM pattern. Builds on editable state for selection/highlighting.

**Addresses:**
- Node search (FEATURES.md differentiator)
- SearchViewModel with filtered ObservableCollection
- Scroll-to-node navigation

**Avoids:**
- Search highlighting re-render collapse (debounce, layer caching)
- Overwhelming search results (limit to 100)

**Complexity:** Low (standard Avalonia search pattern)

**Dependencies:** Phase 3 (selection highlighting requires editable state)

---

### Phase 5: Minimap (2 days)
**Goal:** Overview navigation with viewport indicator, click-to-pan

**Rationale:** Reuses rendering from Phase 1, benefits from editing (shows allocations).

**Addresses:**
- Minimap overlay (FEATURES.md differentiator)
- Viewport rectangle calculation
- Click navigation

**Avoids:**
- Minimap duplicate rendering (create simplified MinimapCanvas, not reuse main)
- Full detail at tiny scale (dots only, skip sprites)

**Complexity:** Medium (coordinate mapping between canvases)

**Dependencies:** Phase 1 (rendering reuse), Phase 3 (shows allocations)

---

### Phase 6: Ascendancy Trees (1-2 days, OPTIONAL)
**Goal:** Display ascendancy nodes separately or overlaid

**Rationale:** Bonus polish, leverages all previous work.

**Addresses:**
- Ascendancy display (FEATURES.md table stakes)
- Separate coordinate space rendering

**Avoids:**
- Ascendancy coordinate space confusion (separate layer, different offset)

**Complexity:** Low (filtering + rendering, positions already calculated)

**Dependencies:** Phase 1 (uses same sprite rendering)

---

## Phase Ordering Rationale

**Start with Sprites (Phase 1):** Biggest visual impact, validates architecture, no functional dependencies.

**Stats & Search (Phase 2):** Independent of editing, provides value while editing is in progress. Tooltips are trivial.

**Editing (Phase 3):** Core functionality, enables state management patterns. Must work before features that reference editable state.

**Search (Phase 4):** After editing because search highlighting requires selection mechanism.

**Minimap (Phase 5):** Reuses all previous work (rendering, editing, search), natural capstone feature.

**Ascendancy (Phase 6):** Optional bonus, can defer or skip based on time budget.

**Why NOT start with editing:** Users will compare visual quality to GGG website/PoB immediately. Colored dots + editing = "looks unfinished." Sprites + no editing = "beautiful viewer, editing coming soon."

## Research Flags for Phases

### Phase 1 (Sprite Foundation)
**Likely needs deeper research:** NO
- Sprite rendering well-documented (SkiaSharp DrawBitmap API stable since 2.88)
- GGG sprite structure verified (live data inspected)
- Caching pattern proven (GemIconService already works)

**Unexpected complexity:** Sprite coordinate mappings
- GGG JSON has nested structure: `sprites.normalActive[zoomLevel].coords[iconPath] = {x, y, w, h}`
- Need to build lookup: (node type, zoom level, icon path) → source rect
- Solution: Parse once at startup, cache in memory dictionary

---

### Phase 2 (Stats & Search)
**Likely needs deeper research:** MAYBE (text rendering)
- SkiaSharp has no built-in word wrap (must implement manually or use SkiaSharp.TextBlocks library)
- Multi-line stat tooltips require line breaking logic
- Regex for stat highlighting (numbers blue, keywords gold) is straightforward

**Mitigation:** Use SkiaSharp.TextBlocks (1.0.0) if manual text layout becomes complex. Library adds word wrap, rich text, tested on multiple platforms.

---

### Phase 3 (Node Editing)
**Likely needs deeper research:** NO
- TreeUrlEncoder is inverse of existing TreeUrlDecoder (algorithm understood)
- Path validation is BFS/Dijkstra on node graph (standard algorithms)
- ObservableCollection pattern is standard MVVM (well-documented in Avalonia)

**Critical testing:** Dual validation modes
- Import mode: accept all nodes from tree URL, validate connectivity as warning
- Interactive mode: only allow allocating nodes that connect to path
- Test with 10+ real PoB imports to ensure no false errors

---

### Phase 4 (Node Search)
**Likely needs deeper research:** NO
- Standard Avalonia MVVM search pattern (AutoCompleteBox examples in docs)
- FuzzySharp library well-documented (1.3M+ downloads, simple API)
- Debouncing with Rx or CancellationToken is common pattern

**Performance tuning:** Debounce timing
- Start with 200ms, tune based on feel
- Limit results to 100 max
- Use LINQ Contains() for simple search (fast enough for 1300 nodes)

---

### Phase 5 (Minimap)
**Likely needs deeper research:** MAYBE (coordinate mapping)
- Converting main viewport (world coords, zoom, offset) to minimap (screen coords, fixed scale) requires math
- Viewport rectangle calculation: `minimapRect = (offset * minimapZoom, size / mainZoom * minimapZoom)`
- Click-to-pan reverse mapping: `worldPos = (clickPos / minimapZoom) - offset`

**Mitigation:** Research 2D minimap patterns (already found resources: Godot minimap tutorial, 2D strategy game minimap article). Math is straightforward once coordinate systems are understood.

---

### Phase 6 (Ascendancy)
**Likely needs deeper research:** MAYBE (coordinate space)
- Ascendancy nodes in completely separate coordinate space
- Tree JSON min/max bounds explicitly exclude ascendancy ("odd locations")
- Need separate position calculation without 14000/11000 offset

**Mitigation:** Filter ascendancy nodes early, render as separate overlay or popup. PoE website uses overlay approach (4 quadrants around class portrait). Don't try to fit in main tree viewport.

---

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | All libraries verified, SkiaSharp 2.88+ has needed APIs, no new dependencies |
| Features | HIGH | GGG data contains everything, table stakes identified from competitor analysis |
| Architecture | HIGH | Existing patterns work (services, ICustomDrawOperation, MVVM), just extending |
| Pitfalls | HIGH | SkiaSharp memory issues well-documented, PoE tree gotchas identified from community repos |
| Sprite Rendering | HIGH | DrawBitmap API stable, sprite coordinates verified in live GGG JSON |
| State Management | HIGH | ObservableCollection + MVVM is standard Avalonia pattern, well-documented |
| Text Rendering | MEDIUM | SkiaSharp word wrap requires manual implementation or library, but straightforward |
| Minimap Coords | MEDIUM | Coordinate mapping math is standard but requires careful implementation |
| Ascendancy Space | MEDIUM | Separate coordinate system confirmed but rendering approach needs testing |

## Gaps to Address

### 1. Sprite Sheet Download URLs
**Gap:** GGG sprite sheet CDN URLs not verified in research (only structure documented)
**Impact:** Phase 1 blocked if URLs changed or inaccessible
**Mitigation:** Inspect live tree JSON from poe-tool-dev repo, extract actual sprite URLs, test HTTP access
**When:** Before Phase 1 starts

---

### 2. Multi-line Text Rendering Performance
**Gap:** SkiaSharp manual word wrap performance unknown at scale (200+ stat tooltips)
**Impact:** Phase 2 tooltips might be slow if text layout is expensive
**Mitigation:** Benchmark SkiaSharp.DrawText vs SkiaSharp.TextBlocks, cache rendered text as SKBitmap if needed
**When:** During Phase 2 implementation

---

### 3. Tree URL Encoding Edge Cases
**Gap:** Version 6 tree URL spec not fully documented (found references but not complete spec)
**Impact:** Phase 3 TreeUrlEncoder might generate invalid URLs for edge cases (cluster jewels, masteries)
**Mitigation:** Test with existing PoB imports, compare generated URLs with PoB's output, decode roundtrip test
**When:** Phase 3 testing phase

---

### 4. Minimap Performance at 1300+ Nodes
**Gap:** Minimap render cost unknown (simplified rendering might still be too slow at 60fps)
**Impact:** Phase 5 minimap might cause frame drops
**Mitigation:** Update minimap at 10fps max (every 100ms), render to static SKBitmap and only update viewport rect
**When:** Phase 5 performance tuning

---

### 5. Ascendancy Node Position Calculations
**Gap:** Exact ascendancy coordinate space transform not documented
**Impact:** Phase 6 ascendancy nodes might render at wrong positions
**Mitigation:** Inspect PoB source code (public on GitHub), compare with GGG tree JSON, test with builds that have ascendancy allocated
**When:** Before Phase 6 starts (if implemented)

---

## Technical Unknowns Remaining

| Unknown | Risk Level | Investigation Needed | Fallback Plan |
|---------|------------|---------------------|---------------|
| GGG sprite CDN reliability | LOW | Test download from web.poecdn.com | Bundle sprites in app if CDN unreliable |
| SkiaSharp.TextBlocks performance | MEDIUM | Benchmark with 1000 multi-line text blocks | Use cached SKBitmap per tooltip if slow |
| ObservableCollection overhead at 1300 items | LOW | Profile Add/Remove performance | Use HashSet + manual change notifications |
| Minimap render cost | MEDIUM | Profile simplified rendering at 1300 nodes | Static image approach (render once on load) |
| Tree URL version 6 mastery encoding | MEDIUM | Reverse engineer from PoB URLs | Defer mastery support to Phase 3+ |
| Ascendancy coordinate transform | MEDIUM | Inspect PoB codebase | Separate popup window instead of overlay |

## Recommended Next Steps

1. **Validate Sprite URLs** (30 mins)
   - Download live tree JSON from poe-tool-dev
   - Extract sprite CDN URLs
   - Test HTTP GET requests
   - Verify sprite sheet format (PNG, dimensions)

2. **Prototype Sprite Rendering** (2 hours)
   - Load one sprite sheet
   - Parse coordinate mappings
   - Render 10 nodes with DrawBitmap
   - Verify no memory leaks (load/unload 5 times)

3. **Architecture Review** (1 hour)
   - Review ARCHITECTURE.md with team
   - Confirm layered rendering approach
   - Validate SpriteSheetService design
   - Approve phase ordering

4. **Phase 1 Implementation** (2-3 days)
   - Implement SpriteSheetService
   - Modify SkillTreeDrawOperation for sprites
   - Add group background rendering
   - Test with 5+ builds

5. **Iterative Development**
   - Complete Phase 1 → validate → proceed to Phase 2
   - Each phase validates architecture before next
   - Can defer/skip Phase 5-6 based on time budget

## Success Metrics

### Phase 1 Success
- [ ] Tree renders with real PoE sprites (not colored dots)
- [ ] Group backgrounds visible
- [ ] Zoom switches sprite sheets at thresholds
- [ ] Memory stable after loading 20 builds sequentially
- [ ] 60 FPS maintained at all zoom levels

### Phase 2 Success
- [ ] Tooltips show formatted stats (multi-line)
- [ ] Search finds nodes by name/stats
- [ ] Search results highlighted in gold
- [ ] Typing in search maintains >50 FPS
- [ ] Click result scrolls to node

### Phase 3 Success
- [ ] Click toggles node allocation
- [ ] Path validation prevents invalid allocations
- [ ] Imported PoB builds validate correctly
- [ ] Undo/redo works
- [ ] Tree URL updates after editing

### Phase 4 Success
- [ ] Search filters 1300 nodes <100ms
- [ ] Results limited to 100 max
- [ ] Selection highlights node
- [ ] FPS >50 during search

### Phase 5 Success
- [ ] Minimap shows full tree overview
- [ ] Viewport rectangle tracks main view
- [ ] Click minimap pans main view
- [ ] FPS drop <10% with minimap visible
- [ ] Memory increase <50MB

## Sources Summary

**HIGH CONFIDENCE (Official/Stable APIs):**
- SkiaSharp 2.88 API docs (DrawBitmap, SKCanvas, SKBitmap)
- Avalonia ICustomDrawOperation docs
- GGG skill tree JSON structure (poe-tool-dev/passive-skill-tree-json)
- .NET ObservableCollection and MVVM patterns

**MEDIUM CONFIDENCE (Community/Libraries):**
- SkiaSharp.TextBlocks for word wrap
- FuzzySharp for fuzzy search
- Community skill tree repos (PoESkillTree, PathOfPathing)

**VERIFIED BY INSPECTION:**
- Sprite coordinate structure in live GGG JSON
- Existing PathPilot codebase (SkillTreeCanvas, services)
- SkiaSharp memory management issues (GitHub issues #829, #1009)

**NEEDS VALIDATION:**
- Sprite CDN URLs (before Phase 1)
- Text rendering performance (during Phase 2)
- Tree URL encoding edge cases (during Phase 3)
- Minimap render cost (during Phase 5)

---

*Research Summary for: Full Visual Skill Tree Integration*
*Researched: 2026-02-15*
*Overall Confidence: HIGH—Architecture sound, dependencies validated, pitfalls identified, phases well-defined*
