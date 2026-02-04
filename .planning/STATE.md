# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-02-04)

**Core value:** Flüssiges Rendering von ~1300 Nodes mit klarer Allocated-Markierung und natürlicher Zoom/Pan-Navigation
**Current focus:** Phase 3 - Navigation

## Current Position

Phase: 3 of 4 (Navigation)
Plan: 1 of TBD
Status: In progress
Last activity: 2026-02-04 — Completed 03-01-PLAN.md (Zoom/Pan Navigation)

Progress: [█████░░░░░] 50%

## Performance Metrics

**Velocity:**
- Total plans completed: 5
- Average duration: 28.6 min
- Total execution time: 2.38 hours

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| 01-data-foundation | 2 | 3.0 min | 1.5 min |
| 02-core-rendering | 2 | 53.0 min | 26.5 min |
| 03-navigation | 1 | 87.0 min | 87.0 min |

**Recent Trend:**
- Last 5 plans: 01-02 (1.4m), 02-01 (2.0m), 02-02 (51m), 03-01 (87m)
- Trend: 03-01 longer due to multiple hit testing fixes and coordinate transformation debugging

*Updated after each plan completion*

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

- Phase 1: Will use official GGG JSON as data source (offizielle Daten, gut dokumentiert)
- Phase 2: Will use SkiaSharp for rendering (GPU-beschleunigt, bereits in Avalonia verfügbar)
- All phases: Focus on display only, no editing in v1 (Fokus auf solides Foundation)
- 01-01: Use streaming JsonDocument.ParseAsync for large GGG JSON (~3000 nodes)
- 01-01: Cache locally for 7 days to avoid repeated downloads
- 01-01: Parse string node IDs to int for proper dictionary keys
- 01-02: Missing nodes logged as warnings (graceful degradation for outdated builds)
- 01-02: Position calculation uses GGG orbit radii (0, 82, 162, 335, 493 pixels)
- 01-02: Service composition pattern (BuildTreeMapper depends on SkillTreeDataService)
- 02-01: ICustomDrawOperation with non-generic TryGetFeature API for Avalonia 11
- 02-01: Batch all connections into single SKPath to minimize draw calls
- 02-01: Connection deduplication with HashSet to avoid drawing bidirectional edges twice
- 02-01: Node sizes by type (Keystone 18f, Notable 12f, JewelSocket 10f, Normal 6f)
- 02-01: Allocated nodes gold (200,150,50), unallocated dark gray (60,60,60)
- 02-02: Decode tree URL directly instead of relying on cached AllocatedNodes
- 02-02: Use poe-tool-dev/passive-skill-tree-json for PoE 1 data (not GGG's PoE 2 export)
- 02-02: Lua-to-C# byte offset fix: `b:byte(7)` in Lua = `bytes[6]` in C#
- 02-02: ZoomLevel property on canvas uses canvas.Scale() for proper SkiaSharp zoom
- 03-01: SkiaSharp canvas.Scale() as single zoom mechanism (removed Canvas.Width/Height scaling)
- 03-01: Pointer events (OnPointerWheelChanged/Pressed/Moved/Released) for navigation
- 03-01: Coordinate transformation order: Translate(offset) then Scale(zoom)
- 03-01: Fix hit testing by rendering transparent Background in SkiaSharp draw operation
- 03-01: Zoom centered on cursor via pre/post coordinate transformation tracking
- 03-01: Pan offset divided by ZoomLevel for consistent speed at all zoom levels

### Pending Todos

None yet.

### Blockers/Concerns

None yet.

## Session Continuity

Last session: 2026-02-04T20:36:43Z
Stopped at: Completed 03-01-PLAN.md (Zoom/Pan Navigation)
Resume file: None

**Plan 03-01 complete.** Mouse-wheel zoom centered on cursor, left-click drag pan, and automatic centering on allocated nodes all verified working. Phase 3 (Navigation) in progress.
