# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-02-04)

**Core value:** Flüssiges Rendering von ~1300 Nodes mit klarer Allocated-Markierung und natürlicher Zoom/Pan-Navigation
**Current focus:** Phase 4 - Interaction

## Current Position

Phase: 4 of 4 (Interaction)
Plan: 1 of 1
Status: Phase complete
Last activity: 2026-02-04 — Completed 04-01-PLAN.md (Interactive Tooltips)

Progress: [██████████] 100%

## Performance Metrics

**Velocity:**
- Total plans completed: 6
- Average duration: 28.2 min
- Total execution time: 2.82 hours

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| 01-data-foundation | 2 | 3.0 min | 1.5 min |
| 02-core-rendering | 2 | 53.0 min | 26.5 min |
| 03-navigation | 1 | 87.0 min | 87.0 min |
| 04-interaction | 1 | 25.0 min | 25.0 min |

**Recent Trend:**
- Last 5 plans: 02-01 (2.0m), 02-02 (51m), 03-01 (87m), 04-01 (25m)
- Trend: 04-01 faster due to leveraging coordinate transformation patterns from Phase 3

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
- 04-01: World-space hit testing with FindNodeAtPosition using node-type-based radius
- 04-01: Tooltip connection limit (15 entries) to avoid overwhelming UI
- 04-01: Clear hover state during pan/zoom for smooth interaction
- 04-01: Filter ascendancy node connections to eliminate long lines from center to edges

### Pending Todos

None yet.

### Blockers/Concerns

None yet.

## Session Continuity

Last session: 2026-02-04T22:30:00Z
Stopped at: Completed Phase 4 - Interaction (Plan 04-01)
Resume file: None

**All phases complete!** Skill tree viewer fully functional with:
- Native SkiaSharp rendering (Phase 2)
- Mouse wheel zoom and drag pan (Phase 3)
- Hover tooltips with node details (Phase 4)

**Project ready for production use.** PathPilot now displays Path of Exile skill trees with full interactivity.
