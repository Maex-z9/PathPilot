---
phase: 03-navigation
plan: 01
subsystem: ui
tags: [avalonia, skia, canvas, navigation, zoom, pan, pointer-events]

# Dependency graph
requires:
  - phase: 02-core-rendering
    provides: SkillTreeCanvas with SkiaSharp rendering pipeline
provides:
  - Complete navigation controls (zoom/pan) for skill tree viewer
  - Automatic centering on allocated nodes
  - Coordinate transformation system for pointer-to-world mapping
affects: [04-interactivity]

# Tech tracking
tech-stack:
  added: []
  patterns: [pointer-event-handling, coordinate-transformation, viewport-centering]

key-files:
  created: []
  modified: [src/PathPilot.Desktop/Controls/SkillTreeCanvas.cs, src/PathPilot.Desktop/TreeViewerWindow.axaml, src/PathPilot.Desktop/TreeViewerWindow.axaml.cs]

key-decisions:
  - "Use SkiaSharp canvas.Scale() as single zoom mechanism, remove Canvas.Width/Height scaling"
  - "Implement pointer events (wheel/press/move/release) for natural mouse navigation"
  - "Apply coordinate transformation in correct order: Translate(offset) then Scale(zoom)"
  - "Fix hit testing by rendering transparent Background in SkillTreeCanvas"
  - "Use pointer capture for drag operations to handle mouse leaving bounds"

patterns-established:
  - "ScreenToWorld coordinate transformation for pointer-to-canvas mapping"
  - "Zoom-centered-on-cursor via pre/post transformation coordinate tracking"
  - "Pan offset compensation (_offsetX/_offsetY divided by zoom for consistent speed)"
  - "CenterOnAllocatedNodes pattern for initial viewport positioning"

# Metrics
duration: 87min
completed: 2026-02-04
---

# Phase 3 Plan 1: Navigation Summary

**Mouse-wheel zoom centered on cursor, left-click drag pan, and automatic centering on allocated nodes using SkiaSharp coordinate transformation**

## Performance

- **Duration:** 87 min
- **Started:** 2026-02-04T18:40:00Z (approx, from first commit)
- **Completed:** 2026-02-04T20:36:43Z
- **Tasks:** 3 (2 implementation + 1 verification checkpoint)
- **Files modified:** 3

## Accomplishments
- Complete zoom/pan navigation using Avalonia pointer events
- Mouse wheel zoom centered on cursor position (content under cursor stays fixed)
- Left-click drag to pan with consistent speed at all zoom levels
- Automatic initial centering on allocated nodes when TreeViewerWindow opens
- Zoom limit guards (0.02f min to 2.0f max) prevent unusable zoom levels
- Button-based zoom (+ / - / 26%) working alongside mouse wheel zoom

## Task Commits

Each task was committed atomically:

1. **Task 1: Implement zoom and pan controls in SkillTreeCanvas** - `1699ae0` (feat)
2. **Task 2: Implement initial centering on allocated nodes** - `7a194d9` (feat)
3. **Task 3a: Fix tree navigation issues** - `c2319cb` (fix)
4. **Task 3b: Enable pan with Background property** - `3dbb18c` (fix)
5. **Task 3c: Fix pan by rendering Background** - `4a7edae` (fix)

**Plan metadata:** (pending - will be created after this SUMMARY)

_Note: Task 3 required multiple fix commits to resolve hit testing issues with pointer events_

## Files Created/Modified
- `src/PathPilot.Desktop/Controls/SkillTreeCanvas.cs` - Added zoom/pan state, pointer event handlers, coordinate transformation, CenterOnAllocatedNodes()
- `src/PathPilot.Desktop/TreeViewerWindow.axaml` - Added Background to SkillTreeCanvas, removed conflicting RenderTransform zoom controls
- `src/PathPilot.Desktop/TreeViewerWindow.axaml.cs` - Removed Canvas.Width/Height scaling, wired buttons to SkillTreeCanvas.ZoomIn/Out(), call CenterOnAllocatedNodes()

## Decisions Made

**1. SkiaSharp canvas.Scale() as single zoom mechanism**
- **Rationale:** Previous implementation scaled Canvas.Width/Height via RenderTransform, which conflicts with SkiaSharp's canvas.Scale(). Having both causes double-scaling.
- **Implementation:** Removed TreeViewerWindow's ApplyZoom() Canvas dimension scaling, kept only SkillTreeCanvas.ZoomLevel property that applies canvas.Scale() in draw operation.

**2. Pointer events for navigation instead of Gestures API**
- **Rationale:** Direct pointer events (OnPointerWheelChanged, OnPointerPressed/Moved/Released) provide more control over zoom/pan behavior than Avalonia's Gestures API.
- **Implementation:** Override pointer event handlers in SkillTreeCanvas, use pointer capture for drag operations.

**3. Coordinate transformation order: Translate then Scale**
- **Rationale:** Transform order matters in graphics. Translate(offset) moves viewport, Scale(zoom) magnifies. Reversing order would magnify the offset.
- **Implementation:** In SkillTreeDrawOperation.Render(), apply `canvas.Translate(-_offsetX * _zoomLevel, -_offsetY * _zoomLevel)` before `canvas.Scale(_zoomLevel, _zoomLevel)`.

**4. Fix hit testing by rendering transparent Background**
- **Rationale:** Without a rendered background, pointer events only fire when hovering over drawn content (nodes/connections), not empty canvas areas.
- **Implementation:** Added `Background = Brushes.Transparent` in XAML, render it in SkillTreeDrawOperation with `canvas.Clear(SKColors.Transparent)`.

**5. Zoom centered on cursor via coordinate tracking**
- **Rationale:** Natural zoom UX keeps content under cursor fixed as zoom changes.
- **Implementation:** In OnPointerWheelChanged, convert pointer position to world coordinates before AND after zoom change, adjust offset by the difference.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed zoom not centering on cursor**
- **Found during:** Task 3 verification
- **Issue:** Initial zoom implementation didn't track cursor position, causing content to shift unexpectedly when zooming
- **Fix:** Added ScreenToWorld() coordinate transformation, calculated world coords before/after zoom, adjusted offset by difference
- **Files modified:** src/PathPilot.Desktop/Controls/SkillTreeCanvas.cs
- **Verification:** Zoom now keeps content under cursor fixed
- **Committed in:** c2319cb (Task 3 commit)

**2. [Rule 1 - Bug] Fixed pan not working at all**
- **Found during:** Task 3 verification
- **Issue:** Left-click drag didn't pan the view - pointer events weren't firing on empty canvas areas
- **Fix:** Added `Background = Brushes.Transparent` property to SkillTreeCanvas in XAML to enable hit testing across entire canvas
- **Files modified:** src/PathPilot.Desktop/TreeViewerWindow.axaml
- **Verification:** Pan started working but background wasn't rendered
- **Committed in:** 3dbb18c (Task 3b commit)

**3. [Rule 1 - Bug] Fixed Background not rendering (pan still broken)**
- **Found during:** Task 3 verification after 3dbb18c
- **Issue:** Background property set but not actually rendered in SkiaSharp draw operation, so hit testing still didn't work reliably
- **Fix:** Added `canvas.Clear(SKColors.Transparent)` at start of SkillTreeDrawOperation.Render() to actually render the transparent background
- **Files modified:** src/PathPilot.Desktop/Controls/SkillTreeCanvas.cs
- **Verification:** Pan now works across entire canvas area
- **Committed in:** 4a7edae (Task 3c commit)

**4. [Rule 1 - Bug] Fixed initial centering not working**
- **Found during:** Task 3 verification
- **Issue:** CenterOnAllocatedNodes() was called but tree still appeared at origin
- **Fix:** Corrected zoom level initialization order - set ZoomLevel property before calculating offset, ensure InvalidateVisual() called
- **Files modified:** src/PathPilot.Desktop/Controls/SkillTreeCanvas.cs
- **Verification:** Tree now correctly centers on allocated nodes on window open
- **Committed in:** c2319cb (Task 3 commit)

**5. [Rule 1 - Bug] Fixed pan speed inconsistency at different zoom levels**
- **Found during:** Task 3 verification
- **Issue:** Pan felt faster when zoomed out, slower when zoomed in
- **Fix:** Divide pointer delta by ZoomLevel in OnPointerMoved: `_offsetX -= (float)delta.X / ZoomLevel`
- **Files modified:** src/PathPilot.Desktop/Controls/SkillTreeCanvas.cs
- **Verification:** Pan speed now feels consistent at all zoom levels
- **Committed in:** c2319cb (Task 3 commit)

---

**Total deviations:** 5 auto-fixed (5 bugs)
**Impact on plan:** All fixes necessary for navigation to work correctly. No scope creep - all issues were bugs in planned functionality.

## Issues Encountered

**Hit testing complexity with custom SkiaSharp rendering:**
- Custom ICustomDrawOperation controls don't automatically get hit testing on empty areas
- Required explicit Background property AND rendering that background in Skia draw operation
- Took 2 additional fix commits to discover proper solution (property + rendering)

**Coordinate transformation edge cases:**
- Initial implementation had several off-by-one or order-of-operations issues
- ScreenToWorld calculation, offset adjustment for zoom centering, and pan speed compensation all required careful debugging
- All resolved through systematic testing at checkpoint

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

**What's ready for Phase 4 (Interactivity):**
- Complete navigation system allows exploring tree at any zoom/position
- Coordinate transformation functions (ScreenToWorld) ready for hit testing nodes on hover
- Pointer event infrastructure in place for click/hover interactions
- AllocatedNodeIds tracking already highlights allocated nodes - foundation for showing connections

**No blockers or concerns.**

Navigation is fully functional and verified. Ready to add interactive features like node tooltips, hover highlighting, and path tracing.

---
*Phase: 03-navigation*
*Completed: 2026-02-04*
