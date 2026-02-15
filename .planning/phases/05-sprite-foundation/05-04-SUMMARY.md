---
phase: 05-sprite-foundation
plan: 04
subsystem: skill-tree-rendering
tags: [sprites, alignment, cosmetic-polish, gap-closure]

dependency_graph:
  requires: [05-03-performance]
  provides: [aligned-sprites]
  affects: [keystone-rendering, group-backgrounds]

tech_stack:
  added: []
  patterns:
    - Python-based sprite alpha analysis for debugging
    - Visual verification-first debugging approach
    - Bitmap mirroring via SkiaSharp transformations

key_files:
  created: []
  modified:
    - src/PathPilot.Desktop/Controls/SkillTreeCanvas.cs

decisions:
  - what: "Use Python/Pillow for sprite alpha channel analysis"
    why: "Quick diagnostic tool to determine sprite content layout without trial-and-error rendering"
    alternatives: ["Trial-and-error in C#", "Manual inspection in GIMP"]
    impact: "Fast root cause identification - revealed PSGroupBackground3 sprites are 100% opaque with TOP-half content"

  - what: "Group background half-image mirroring: draw original as top half, then vertically mirror around groupY"
    why: "PSGroupBackground3 sprites contain the TOP half of vertically symmetric ornamental circle patterns"
    alternatives: ["Horizontal mirroring (original code)", "Bottom-half positioning"]
    impact: "Produces complete ornamental medallion pattern matching GGG's design intent"

  - what: "No changes needed for keystone sprite alignment"
    why: "Investigation confirmed keystone frames (83x85) and icons (52x53) are both perfectly centered with content center matching geometric center"
    alternatives: ["Add offset compensation", "Adjust centering logic"]
    impact: "Avoided unnecessary complexity - existing centering logic is correct"

metrics:
  duration: "15m 32s"
  tasks_completed: 2
  files_created: 0
  files_modified: 1
  commits: 2
  completed_date: "2026-02-15"
---

# Phase 5 Plan 4: Sprite Alignment Gap Closure

> Fix keystone sprite background and group background image alignment issues identified in UAT.

## Summary

Fixed alignment issues with group background sprites that rendered incorrectly when using half-image mirroring. Investigated keystone sprite alignment and confirmed no changes needed. User reported "at some keystones the background is not aligned right" and "not all of [group backgrounds] are aligned properly" - analysis revealed only group backgrounds needed fixing. Used Python-based sprite analysis to determine correct mirroring axis, then implemented proper vertical mirroring for half-image backgrounds.

## Tasks Completed

### Task 1: Fix keystone and group background sprite alignment
**Commits:** `4419b59`, `fc42781`

**Root Cause Analysis:**
Used Python/Pillow to analyze sprite alpha channels and determine content layout:

```python
from PIL import Image
sprite = Image.open('groupBackground-3.png')
alpha = sprite.getchannel('A')
alpha_array = np.array(alpha)
print(f"Unique alpha values: {np.unique(alpha_array)}")
# Output: [255] - 100% opaque everywhere, NO transparent regions
```

Generated combined sprite previews to visualize proper mirroring axis. Found:
- PSGroupBackground3 sprites contain the **TOP half** of vertically symmetric ornamental circle patterns
- Original code used horizontal mirroring `Scale(-1, 1)` - wrong axis
- Content is 100% opaque (no alpha transparency) - all shape definition comes from visible artwork

**Investigation: Keystone Sprites**
- Keystone frame sprite: 83x85 pixels
- Keystone icon sprite: 52x53 pixels
- Both sprites: content center = geometric center (perfectly centered)
- Existing centering logic (`coord.W/2 * spriteScale`) is correct
- **Conclusion:** No changes needed for keystones

**Fix: Group Background Half-Image Mirroring**

**First attempt (commit `4419b59`):**
```csharp
// Changed from horizontal to vertical mirroring
canvas.Scale(1, -1);
```
**Result:** Content positioned below center - showed two disconnected halves.

**Final fix (commit `fc42781`):**
```csharp
// Draw original sprite as TOP half (from groupY-spriteH to groupY)
var topHalfDestRect = new SKRect(
    groupX - halfW,
    groupY - spriteH,  // Top edge above center
    groupX + halfW,
    groupY);           // Bottom edge AT center

canvas.DrawBitmap(bgBitmap, srcRect, topHalfDestRect, bitmapPaint);

// Then vertically mirror around groupY to create BOTTOM half
canvas.Save();
canvas.Translate(group.X, group.Y);
canvas.Scale(1, -1);  // Vertical flip
canvas.Translate(-group.X, -group.Y);
canvas.DrawBitmap(bgBitmap, srcRect, topHalfDestRect, bitmapPaint);
canvas.Restore();
```

**Result:** Complete ornamental circle medallion pattern matching GGG's design.

**Technical Details:**
- 84 groups use PSGroupBackground3 with `isHalfImage: true`
- Sprite dimensions vary, but all follow same pattern: top half of symmetric circle
- Mirroring happens around `group.Y` (vertical centerline)
- Original sprite rendered from `groupY - spriteH` to `groupY` (top half)
- Mirrored sprite rendered from `groupY` to `groupY + spriteH` (bottom half)

**Files:**
- `src/PathPilot.Desktop/Controls/SkillTreeCanvas.cs` (fixed half-image logic in DrawGroupBackgrounds)

### Task 2: Alignment visual verification (checkpoint:human-verify)
**Status:** User confirmed "now it looks good"

User verified:
1. Keystone nodes: icon sprites centered within decorative frames, no visible offset
2. Group backgrounds: properly centered on node clusters, continuous ornamental patterns
3. Half-image backgrounds: render as complete symmetric shapes, no gaps or disconnects

## Verification

### Build Verification
- `dotnet build src/PathPilot.Desktop/PathPilot.Desktop.csproj` - **SUCCESS**

### Visual Verification (User-Confirmed)
1. Keystone sprite frames properly aligned with icons
2. Group backgrounds properly centered on node clusters
3. Half-image backgrounds render as continuous ornamental circles
4. No visible misalignment compared to GGG's official tree rendering

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Added Python sprite analysis tooling**
- **Found during:** Task 1 root cause analysis
- **Issue:** Trial-and-error rendering would be slow; needed to determine sprite content layout
- **Fix:** Created Python script using Pillow to analyze sprite alpha channels and generate combined previews
- **Files created:** Temporary analysis scripts (not committed)
- **Impact:** Identified PSGroupBackground3 sprites are 100% opaque with top-half content in <5 minutes vs hours of guesswork

**2. [Rule 1 - Bug] Fixed half-image positioning in first attempt**
- **Found during:** Task 1 first iteration
- **Issue:** Vertical mirroring with original destRect showed content below center (two disconnected halves)
- **Fix:** Changed destRect to position original sprite from `groupY-spriteH` to `groupY` (top half), then mirror creates bottom half
- **Files modified:** `src/PathPilot.Desktop/Controls/SkillTreeCanvas.cs`
- **Commit:** `fc42781`
- **Impact:** Proper ornamental circle pattern rendering

## Impact

**Immediate:**
- All group backgrounds render as complete symmetric patterns
- Keystone sprites confirmed to be correctly aligned
- Visual quality matches GGG's official tree rendering

**UAT Gap Closure:**
- Gap 1 (cosmetic): Keystone sprite backgrounds aligned correctly - **CLOSED** (confirmed no fix needed)
- Gap 3 (cosmetic): Group background images aligned properly - **CLOSED** (vertical mirroring fix)

**Next Steps:**
- Continue Phase 5 sprite work (if more plans exist)
- Phase 6: Stats & Tooltips

## Technical Notes

### Sprite Analysis Approach
Instead of trial-and-error rendering, used Python for fast diagnostic:
1. Load sprite PNG with Pillow
2. Extract alpha channel: `sprite.getchannel('A')`
3. Analyze unique values: `np.unique(alpha_array)`
4. Generate combined preview: overlay original + mirrored versions
5. Visually determine correct axis and content position

This approach eliminated guesswork and provided immediate visual feedback.

### Half-Image Mirroring Pattern
GGG's `isHalfImage: true` flag indicates:
- Sprite contains one half of a symmetric pattern
- For PSGroupBackground3: sprite = top half of ornamental circle
- Rendering: draw original as top, mirror vertically to create bottom
- Mirror axis: horizontal line through `group.Y`

### Keystone Sprite Architecture
GGG's keystone sprites follow perfect centering:
- Frame sprite: outer decorative border (83x85px)
- Icon sprite: inner ability icon (52x53px)
- Both: content optically centered within sprite bounds
- Rendering: both centered on node (x,y) using `coord.W/2`, `coord.H/2` offset
- Result: icon naturally centers within frame

## Known Issues

None identified. All alignment issues resolved.

## Self-Check: PASSED

### Modified Files
- FOUND: src/PathPilot.Desktop/Controls/SkillTreeCanvas.cs

### Commits
- FOUND: 4419b59 (fix(05-04): fix group background half-image vertical mirroring)
- FOUND: fc42781 (fix(05-04): correct group background half-image mirroring)
