# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-02-15)

**Core value:** Flüssiges Rendering von ~1300 Nodes mit klarer Allocated-Markierung und natürlicher Zoom/Pan-Navigation
**Current focus:** v1.1 Skill Tree Overhaul — Phase 5 (Sprite Foundation) ready to plan

## Current Position

Phase: 5 of 10 (Sprite Foundation)
Plan: 2 of 4
Status: In progress
Last activity: 2026-02-15 - Completed 05-03-PLAN.md (Performance Optimization)

Progress: [█████░░░░░] 50% (Phase 5: 2/4 plans complete)

## Milestones

| Version | Name | Status | Date |
|---------|------|--------|------|
| v1.0 | Native Skill Tree Viewer | SHIPPED | 2026-02-04 |
| v1.1 | Skill Tree Overhaul | ACTIVE | 2026-02-15 |

## Performance Metrics

**Velocity:**
- Total plans completed: 7 (6 v1.0 + 1 v1.1)
- v1.1 plans: 1 of TBD completed

**v1.0 Performance:**
- Phases 1-4 completed in ~7 days
- Average ~1.5 plans per phase

**v1.1 Performance:**
- Phase 5 Plan 1: 2m 52s (2 tasks, 6 files)
- Phase 5 Plan 3: 11m 0s (2 tasks, 3 files)

## Accumulated Context

### Decisions

All v1.0 decisions logged in PROJECT.md Key Decisions table.

**v1.1 Phase Structure:**
- Phase 5: Sprite Foundation (VIS-01 to VIS-05) — biggest visual impact, validates architecture
- Phase 6: Stats & Tooltips (TIP-01, TIP-02) — independent of editing, high value
- Phase 7: Node Editing (EDT-01 to EDT-07) — core functionality, enables state management
- Phase 8: Node Search (SRC-01 to SRC-04) — leverages editable state for highlighting
- Phase 9: Minimap (MAP-01 to MAP-04) — reuses rendering + editing work
- Phase 10: Ascendancy (ASC-01 to ASC-03) — bonus polish, uses sprite system

**Rationale:** Start with sprites (visual impact) before editing (user compares to GGG/PoB immediately). Research validates architecture soundness.
- [Phase 05-01]: Add SkiaSharp to PathPilot.Core for sprite bitmap decoding
- [Phase 05-01]: Service owns all SKBitmaps using singleton pattern for safe memory management
- [Phase 05-03]: Tile cache covers 2x viewport area (1.5x margin) for smooth panning before invalidation
- [Phase 05-03]: Cache clamped to 4096x4096 pixels maximum to prevent OOM at extreme zoom levels
- [Phase 05-03]: Dynamic bitmap filter quality (Low for zoom <0.3, Medium ≥0.3) balances performance and quality
- [Phase 05-03]: Sprite preload verification via TryGetLoadedBitmap() before setting TreeData prevents black flash

### Pending Todos

None.

### Blockers/Concerns

None. Research completed with HIGH confidence. Key mitigations identified:
- SKBitmap memory leaks → singleton service pattern (like GemIconService)
- Sprite coordinate parsing → parse once, cache in memory dictionary
- Path validation breaking imports → dual validation modes (lenient/strict)

### Quick Tasks Completed

| # | Description | Date | Commit | Directory |
|---|-------------|------|--------|-----------|
| 1 | Commit node search and auto-update installer changes | 2026-02-15 | 110231a | [1-commit-node-search-and-auto-update-insta](./quick/1-commit-node-search-and-auto-update-insta/) |

## Session Continuity

Last session: 2026-02-15
Stopped at: Completed 05-03-PLAN.md (Performance Optimization)
Resume file: None

---
*Last updated: 2026-02-15*
