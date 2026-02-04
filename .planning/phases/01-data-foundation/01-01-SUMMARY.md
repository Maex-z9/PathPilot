---
phase: 01-data-foundation
plan: 01
subsystem: data
tags: [csharp, dotnet, json, http, caching, skilltree, ggg-api]

# Dependency graph
requires:
  - phase: none
    provides: Initial project structure
provides:
  - SkillTreeData model with node and group dictionaries
  - SkillTreeDataService with async download and caching
  - PassiveNode extended with position properties
  - 7-day local cache at ~/.config/PathPilot/tree-cache/
affects: [02-rendering, 03-interaction]

# Tech tracking
tech-stack:
  added: [System.Text.Json streaming, HttpClient]
  patterns: [async service pattern, local cache strategy, streaming JSON parsing]

key-files:
  created:
    - src/PathPilot.Core/Models/SkillTreeData.cs
    - src/PathPilot.Core/Services/SkillTreeDataService.cs
  modified:
    - src/PathPilot.Core/Models/SkillTree.cs

key-decisions:
  - "Use streaming JsonDocument.ParseAsync for large GGG JSON (~3000 nodes)"
  - "Cache locally for 7 days to avoid repeated downloads"
  - "Parse string node IDs to int for proper dictionary keys"

patterns-established:
  - "Async service with lazy loading (_isLoaded flag)"
  - "User config directory: ~/.config/PathPilot/{cache-type}/"
  - "Console logging for data operations"

# Metrics
duration: 99 seconds
completed: 2026-02-04
---

# Phase 01 Plan 01: Skill Tree Data Service Summary

**SkillTreeDataService downloads and caches GGG's official 3000-node skill tree JSON with streaming parse**

## Performance

- **Duration:** 1 min 39 sec
- **Started:** 2026-02-04T13:14:59Z
- **Completed:** 2026-02-04T13:16:38Z
- **Tasks:** 2
- **Files modified:** 3

## Accomplishments
- Created SkillTreeData model to hold parsed nodes and groups
- Extended PassiveNode with Group, Orbit, OrbitIndex, AscendancyName for position calculation
- Implemented SkillTreeDataService that downloads from GGG GitHub
- Local cache with 7-day expiry at ~/.config/PathPilot/tree-cache/data.json
- Streaming JSON parse using JsonDocument.ParseAsync to handle large file

## Task Commits

Each task was committed atomically:

1. **Task 1: Create SkillTreeData Model** - `259c692` (feat)
2. **Task 2: Create SkillTreeDataService** - `46cfa71` (feat)

## Files Created/Modified
- `src/PathPilot.Core/Models/SkillTreeData.cs` - Container for parsed tree data with Nodes and Groups dictionaries
- `src/PathPilot.Core/Models/SkillTree.cs` - Extended PassiveNode with position properties
- `src/PathPilot.Core/Services/SkillTreeDataService.cs` - Async service for downloading, caching, and parsing GGG skill tree JSON

## Decisions Made

1. **Streaming JSON parse:** Used JsonDocument.ParseAsync instead of JsonSerializer.Deserialize to avoid Large Object Heap allocation for ~3MB JSON file
2. **Integer dictionary keys:** Parse string node IDs to int (GGG JSON quirk) for proper Dictionary<int, PassiveNode> structure
3. **7-day cache:** Balance between having fresh data and avoiding repeated 3MB downloads
4. **Flexible parsing:** Handle both number and string values in JSON arrays (node connections, group nodes)

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None - implementation followed GemDataService pattern with async streaming additions as planned.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

Data foundation complete. Ready for Phase 02 (Rendering):
- SkillTreeData.Nodes contains ~3000 parsed nodes with positions
- SkillTreeData.Groups contains group positions for node layout calculation
- PassiveNode has all position properties needed for rendering (Group, Orbit, OrbitIndex)
- Service handles download and caching automatically on first call

Next phase can call `GetTreeDataAsync()` to get parsed tree data for rendering.

---
*Phase: 01-data-foundation*
*Completed: 2026-02-04*
