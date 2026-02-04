# Project Milestones: PathPilot

## v1.0 Native Skill Tree Viewer (Shipped: 2026-02-04)

**Delivered:** Native SkiaSharp skill tree viewer replacing WebView with full zoom/pan/hover functionality

**Phases completed:** 1-4 (6 plans total)

**Key accomplishments:**

- Native SkiaSharp rendering replacing WebView (no more 200MB Chromium dependency)
- GGG Skill Tree JSON parsing with 7-day local cache and streaming JSON for ~3000 nodes
- Tree URL decoding (Version 6 format) from Path of Building URLs
- Zoom/pan navigation with cursor-centered zoom and smooth drag panning
- Hover tooltips showing node name, stats, and connected nodes
- Allocated node highlighting in gold color for easy build visualization

**Stats:**

- 51 files created/modified
- 5,132 lines of C#
- 4 phases, 6 plans
- 1 day from start to ship

**Git range:** `feat(01-01)` → `docs(phase-04)`

**What's next:** v1.1 features (Quest Tracker completion, Node editing, Stats in tooltips)

---
