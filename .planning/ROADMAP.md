# Roadmap: PathPilot Native Skill Tree Viewer

## Overview

This milestone replaces the WebView-based Skill Tree Viewer with a native Avalonia renderer. The journey moves from data loading (GGG JSON) through core rendering (~1300 nodes) to navigation (zoom/pan) and finally interaction (hover tooltips). Each phase delivers a verifiable capability that unblocks the next.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [x] **Phase 1: Data Foundation** - Load and parse GGG Skill Tree JSON
- [x] **Phase 2: Core Rendering** - Render all nodes and connections in Avalonia Canvas
- [x] **Phase 3: Navigation** - Implement zoom and pan controls
- [ ] **Phase 4: Interaction** - Add hover tooltips and node info

## Phase Details

### Phase 1: Data Foundation
**Goal**: App loads and parses GGG Skill Tree JSON with allocated node mapping
**Depends on**: Nothing (first phase)
**Requirements**: DATA-01, DATA-02, DATA-03
**Success Criteria** (what must be TRUE):
  1. App successfully fetches GGG Skill Tree JSON from official source on startup
  2. All ~3000 nodes are parsed with positions, names, and connections
  3. Allocated node IDs from imported builds correctly map to parsed tree data
**Plans**: 2 plans

Plans:
- [x] 01-01-PLAN.md — Create SkillTreeDataService (fetch, cache, parse GGG JSON)
- [x] 01-02-PLAN.md — Create BuildTreeMapper (map allocated nodes to tree data)

### Phase 2: Core Rendering
**Goal**: Skill Tree renders natively in Avalonia Canvas with all nodes and connections visible
**Depends on**: Phase 1
**Requirements**: REND-01, REND-02, REND-03, REND-04
**Success Criteria** (what must be TRUE):
  1. Skill Tree displays in native Avalonia Canvas (no WebView)
  2. All ~1300 nodes render at correct positions from parsed data
  3. Connections between nodes are drawn correctly
  4. Allocated nodes are visually distinct (different color/style) from unallocated nodes
**Plans**: 2 plans

Plans:
- [x] 02-01-PLAN.md — Create SkillTreeCanvas control with ICustomDrawOperation
- [x] 02-02-PLAN.md — Wire Canvas to TreeViewerWindow and render tree

### Phase 3: Navigation
**Goal**: User can zoom and pan the skill tree naturally
**Depends on**: Phase 2
**Requirements**: NAV-01, NAV-02, NAV-03
**Success Criteria** (what must be TRUE):
  1. User can zoom in/out using mouse wheel
  2. User can drag to pan the view across the entire tree
  3. Tree view starts centered on allocated nodes (or start point if no allocated nodes)
**Plans**: 1 plan

Plans:
- [x] 03-01-PLAN.md — Implement zoom/pan controls and initial centering

### Phase 4: Interaction
**Goal**: User can hover over nodes to see detailed information
**Depends on**: Phase 3
**Requirements**: INT-01, INT-02
**Success Criteria** (what must be TRUE):
  1. Hovering over a node displays tooltip with node name
  2. Tooltip shows which nodes are connected to the hovered node
**Plans**: 1 plan

Plans:
- [ ] 04-01-PLAN.md — Implement hover detection and tooltip display

## Progress

**Execution Order:**
Phases execute in numeric order: 1 -> 2 -> 3 -> 4

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Data Foundation | 2/2 | Complete | 2026-02-04 |
| 2. Core Rendering | 2/2 | Complete | 2026-02-04 |
| 3. Navigation | 1/1 | Complete | 2026-02-04 |
| 4. Interaction | 0/1 | Not started | - |
