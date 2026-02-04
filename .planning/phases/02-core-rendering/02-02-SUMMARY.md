---
phase: 02-core-rendering
plan: 02
subsystem: ui
tags: [avalonia, skiasharp, tree-rendering, native-canvas, url-decoding]

# Dependency graph
requires:
  - phase: 02-01
    provides: SkillTreeCanvas control with ICustomDrawOperation
  - phase: 01-data-foundation
    provides: SkillTreeDataService, SkillTreePositionHelper, PassiveNode models
provides:
  - TreeViewerWindow with native SkiaSharp rendering (no WebView)
  - Tree URL decoding from Path of Building URLs
  - Allocated node highlighting (gold color)
  - ~1300 nodes rendered with connections
affects: [03-viewport-navigation, 04-interactivity]

# Tech tracking
tech-stack:
  added: []
  patterns: [TreeUrlDecoder for PoB URL parsing, Direct URL decoding instead of cached nodes]

key-files:
  created: [src/PathPilot.Core/Parsers/TreeUrlDecoder.cs]
  modified: [src/PathPilot.Desktop/TreeViewerWindow.axaml, src/PathPilot.Desktop/TreeViewerWindow.axaml.cs, src/PathPilot.Desktop/Controls/SkillTreeCanvas.cs, src/PathPilot.Core/Services/SkillTreeDataService.cs]

key-decisions:
  - "Decode tree URL directly in TreeViewerWindow instead of relying on cached AllocatedNodes"
  - "Switch from GGG PoE 2 data source to poe-tool-dev PoE 1 data (version 3.25.0)"
  - "Fix Lua to C# byte offset (Lua 1-indexed, C# 0-indexed)"
  - "Use 10000x10000 canvas size with 5000 offset to center tree"
  - "Add ZoomLevel property to SkillTreeCanvas for proper SkiaSharp zoom via canvas.Scale()"

patterns-established:
  - "TreeUrlDecoder: Version 6 URL format (bytes 0-3 version, byte 4 class, byte 5 ascendancy, byte 6 count, byte 7+ node IDs)"
  - "Direct URL decoding: Always decode from URL, don't trust cached/stored node IDs"
  - "PoE 1 data source: Use poe-tool-dev/passive-skill-tree-json for PoE 1 builds"

# Metrics
duration: 51min
completed: 2026-02-04
---

# Phase 02 Plan 02: TreeViewerWindow Integration Summary

**Native SkiaSharp skill tree rendering replacing WebView, with tree URL decoding and ~1300 nodes rendered with allocated highlighting**

## Performance

- **Duration:** 51 minutes (including checkpoint verification)
- **Started:** 2026-02-04T14:46:08+01:00
- **Completed:** 2026-02-04T15:37:01+01:00
- **Tasks:** 3 (2 implementation + 1 checkpoint)
- **Files modified:** 8

## Accomplishments

- Replaced WebView with native SkillTreeCanvas in TreeViewerWindow
- Implemented TreeUrlDecoder for parsing Path of Building URL format
- Correctly identified and fixed Lua-to-C# indexing offset (1-indexed vs 0-indexed)
- Switched to PoE 1 tree data source for compatibility with existing builds
- Rendered ~1300 skill tree nodes with connections
- Allocated nodes highlighted in gold, unallocated in dark gray

## Task Commits

Each task was committed atomically:

1. **Task 1: Update TreeViewerWindow XAML** - `406db40` (feat)
2. **Task 2: Update TreeViewerWindow code-behind** - `3f5a1d0` (feat)

**Bug fixes during execution:**
- `4477389` - fix(02-02): Pass allocated nodes to TreeViewerWindow for highlighting
- `9441cc0` - fix(tree): Correct tree URL decoding and use PoE 1 data

## Files Created/Modified

- `src/PathPilot.Desktop/TreeViewerWindow.axaml` - Replaced WebView with SkillTreeCanvas in ScrollViewer
- `src/PathPilot.Desktop/TreeViewerWindow.axaml.cs` - Load tree data, decode URL, calculate positions, pass to canvas
- `src/PathPilot.Core/Parsers/TreeUrlDecoder.cs` - **New:** Decode tree URLs to node IDs (Version 6 format)
- `src/PathPilot.Desktop/Controls/SkillTreeCanvas.cs` - Added ZoomLevel property for SkiaSharp zoom
- `src/PathPilot.Desktop/MainWindow.axaml.cs` - Pass allocated nodes to TreeViewerWindow
- `src/PathPilot.Core/Services/SkillTreeDataService.cs` - Switch to PoE 1 data source (poe-tool-dev)
- `src/PathPilot.Core/Models/SkillTree.cs` - Minor model updates
- `.claude/CLAUDE.md` - Updated with skill tree implementation details

## Decisions Made

**1. Direct URL Decoding**
- Old builds had incorrectly cached node IDs due to previous parsing bugs
- Now decode tree URL fresh each time TreeViewerWindow opens
- Guarantees accurate allocated nodes regardless of saved build state

**2. PoE 1 Data Source**
- GGG's `skilltree-export` repo contains only PoE 2 data
- Switched to `poe-tool-dev/passive-skill-tree-json` for PoE 1 (version 3.25.0)
- Required for PathPilot which targets PoE 1 builds

**3. Byte Offset Fix**
- PoB Lua code is 1-indexed: `b:byte(7)` = byte at position 7
- C# is 0-indexed: `bytes[7]` = byte at position 8
- Fixed: `bytes[6]` in C# corresponds to Lua's `b:byte(7)`
- Node count and node IDs now parse correctly

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Allocated nodes not highlighted**
- **Found during:** Task 2 (integration testing)
- **Issue:** MainWindow was not passing allocated nodes to TreeViewerWindow constructor
- **Fix:** Updated MainWindow.axaml.cs to pass `_loadedBuild.AllocatedNodes`
- **Files modified:** src/PathPilot.Desktop/MainWindow.axaml.cs
- **Verification:** Allocated nodes now appear gold in rendered tree
- **Committed in:** 4477389

**2. [Rule 1 - Bug] Incorrect tree URL decoding**
- **Found during:** Checkpoint verification (all nodes gray despite build having allocations)
- **Issue:** Lua-to-C# indexing mismatch and wrong data source (PoE 2 instead of PoE 1)
- **Fix:** Created TreeUrlDecoder with correct byte offsets, switched to PoE 1 data
- **Files modified:** Multiple files (see commit 9441cc0)
- **Verification:** Tree now renders with correct allocated nodes
- **Committed in:** 9441cc0

---

**Total deviations:** 2 auto-fixed (both Rule 1 bugs)
**Impact on plan:** Both fixes essential for correct tree rendering. No scope creep.

## Issues Encountered

**Data Source Mismatch**
- Initially used GGG's official tree export repository
- Discovered it contains only PoE 2 data, not PoE 1
- PoE 1 builds would have missing nodes since skill tree differs
- Resolved by switching to poe-tool-dev community repository (PoE 1 version 3.25.0)

**Lua vs C# Indexing**
- PoB reference implementation is in Lua (1-indexed arrays)
- Translating to C# (0-indexed) requires careful offset adjustment
- `b:byte(7)` in Lua means "7th byte" = `bytes[6]` in C#
- Documented in CLAUDE.md for future reference

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

**Ready for Phase 03 (Viewport Navigation):**
- Native canvas rendering fully functional
- ~1300 nodes render correctly with connections
- ZoomLevel property ready for zoom controls
- ScrollViewer provides basic pan support

**Potential improvements for future phases:**
- Viewport culling (only render visible nodes) for better performance
- Smooth zoom animation
- Node hover detection and tooltips
- Node search functionality

---
*Phase: 02-core-rendering*
*Completed: 2026-02-04*
