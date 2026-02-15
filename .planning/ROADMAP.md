# Roadmap: PathPilot

## Milestones

- **v1.0 Native Skill Tree Viewer** — Phases 1-4 (shipped 2026-02-04) — [Archive](milestones/v1.0-ROADMAP.md)
- **v1.1 Skill Tree Overhaul** — Phases 5-10 (active 2026-02-15)

## Phases

<details>
<summary>v1.0 Native Skill Tree Viewer (Phases 1-4) - SHIPPED 2026-02-04</summary>

### Phase 1: Data Foundation
**Goal**: Load and parse GGG skill tree JSON
**Plans**: 2 plans (complete)

### Phase 2: Core Rendering
**Goal**: Render ~1300 nodes with connections
**Plans**: 2 plans (complete)

### Phase 3: Navigation
**Goal**: Zoom and pan navigation
**Plans**: 1 plan (complete)

### Phase 4: Interaction
**Goal**: Hover tooltips with node info
**Plans**: 1 plan (complete)

</details>

## v1.1 Skill Tree Overhaul (Active)

**Milestone Goal:** Bring skill tree viewer to parity with GGG website and Path of Building in visual quality and functionality.

### Phase 5: Sprite Foundation
**Goal**: Replace colored dots with authentic PoE node sprites and group backgrounds
**Depends on**: Phase 4 (v1.0 complete)
**Requirements**: VIS-01, VIS-02, VIS-03, VIS-04, VIS-05
**Success Criteria** (what must be TRUE):
  1. User sees real PoE sprites (Normal/Notable/Keystone/Jewel) instead of colored dots
  2. Group background images render behind node clusters
  3. Sprite quality changes automatically based on zoom level (4 GGG zoom thresholds)
  4. Allocated nodes display active sprites, unallocated display inactive sprites
  5. Sprite sheets load from local cache on subsequent launches (no re-download)
**Plans**: 2 plans

Plans:
- [ ] 05-01-PLAN.md — Sprite data models, JSON parsing, and sprite download service
- [ ] 05-02-PLAN.md — Sprite-based node rendering with LOD and group backgrounds

### Phase 6: Stats & Tooltips
**Goal**: Display formatted node stats and modifiers in hover tooltips
**Depends on**: Phase 5 (sprites provide visual context for stats)
**Requirements**: TIP-01, TIP-02
**Success Criteria** (what must be TRUE):
  1. User sees all stats/modifiers when hovering over any node (e.g., "+10 to Strength")
  2. Stats display multi-line with color formatting (numbers in blue, keywords in gold)
  3. Tooltips render at stable 60 FPS without frame drops
**Plans**: TBD

Plans:
- [ ] 06-01: TBD during planning

### Phase 7: Node Editing
**Goal**: Enable users to allocate/deallocate nodes with click interaction and path validation
**Depends on**: Phase 5 (visual feedback on allocation state)
**Requirements**: EDT-01, EDT-02, EDT-03, EDT-04, EDT-05, EDT-06, EDT-07
**Success Criteria** (what must be TRUE):
  1. User can left-click to allocate nodes and right-click to deallocate them
  2. Path validation prevents orphaned nodes (must connect to starting class or allocated path)
  3. Import mode accepts all PoB nodes (lenient), Edit mode enforces strict path validation
  4. Undo/Redo works for all allocation changes (Ctrl+Z / Ctrl+Y)
  5. Shift+Hover shows shortest path to target node, Shift+Click allocates entire path
  6. Point budget displays current allocation count (e.g., "95/123 Points Used")
  7. Tree URL updates after edits and can be copied/imported back into PoB
**Plans**: TBD

Plans:
- [ ] 07-01: TBD during planning

### Phase 8: Node Search
**Goal**: Allow users to search nodes by name or stats with visual highlighting
**Depends on**: Phase 7 (search highlighting requires editable selection state)
**Requirements**: SRC-01, SRC-02, SRC-03, SRC-04
**Success Criteria** (what must be TRUE):
  1. User can search for nodes by name or stat text in search box
  2. Matching nodes highlight visually in the tree (gold ring/glow effect)
  3. Clicking a search result centers the tree view on that node
  4. Fuzzy matching works (e.g., "fire dmg" finds "Fire Damage")
  5. Search maintains >50 FPS while typing
**Plans**: TBD

Plans:
- [ ] 08-01: TBD during planning

### Phase 9: Minimap
**Goal**: Provide overview navigation with minimap overlay showing viewport and allocations
**Depends on**: Phase 7 (minimap displays allocation state from editing)
**Requirements**: MAP-01, MAP-02, MAP-03, MAP-04
**Success Criteria** (what must be TRUE):
  1. Minimap overlay shows complete tree overview in corner of viewer
  2. Current viewport renders as rectangle on minimap tracking pan/zoom
  3. Clicking minimap location pans main tree to that area
  4. Minimap uses simplified rendering (dots instead of sprites) maintaining >50 FPS
**Plans**: TBD

Plans:
- [ ] 09-01: TBD during planning

### Phase 10: Ascendancy
**Goal**: Display ascendancy nodes in separate coordinate space with allocation highlighting
**Depends on**: Phase 5 (uses same sprite rendering system)
**Requirements**: ASC-01, ASC-02, ASC-03
**Success Criteria** (what must be TRUE):
  1. User sees ascendancy nodes for the build's selected class/ascendancy
  2. Ascendancy renders as separate overlay or region (distinct coordinate space from main tree)
  3. Allocated ascendancy nodes highlight in gold matching main tree allocated style
**Plans**: TBD

Plans:
- [ ] 10-01: TBD during planning

## Progress

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 1. Data Foundation | v1.0 | 2/2 | Complete | 2026-02-04 |
| 2. Core Rendering | v1.0 | 2/2 | Complete | 2026-02-04 |
| 3. Navigation | v1.0 | 1/1 | Complete | 2026-02-04 |
| 4. Interaction | v1.0 | 1/1 | Complete | 2026-02-04 |
| 5. Sprite Foundation | v1.1 | 0/2 | Planning complete | - |
| 6. Stats & Tooltips | v1.1 | 0/TBD | Not started | - |
| 7. Node Editing | v1.1 | 0/TBD | Not started | - |
| 8. Node Search | v1.1 | 0/TBD | Not started | - |
| 9. Minimap | v1.1 | 0/TBD | Not started | - |
| 10. Ascendancy | v1.1 | 0/TBD | Not started | - |

---
*Roadmap created: 2026-02-04 (v1.0)*
*v1.1 phases added: 2026-02-15*
