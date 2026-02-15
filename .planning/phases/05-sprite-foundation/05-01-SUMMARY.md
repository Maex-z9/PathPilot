---
phase: 05-sprite-foundation
plan: 01
subsystem: skill-tree-rendering
tags: [data-layer, sprites, parsing, caching]

dependency_graph:
  requires: [04-quest-tracker]
  provides: [sprite-models, sprite-parsing, sprite-service]
  affects: [tree-rendering, node-display]

tech_stack:
  added:
    - SkiaSharp 2.88.* (PathPilot.Core)
  patterns:
    - Singleton service pattern for sprite sheet management
    - In-memory bitmap caching
    - Disk cache with TTL (30 days)
    - Download deduplication via ConcurrentDictionary

key_files:
  created:
    - src/PathPilot.Core/Models/SpriteSheet.cs
    - src/PathPilot.Core/Services/SkillTreeSpriteService.cs
  modified:
    - src/PathPilot.Core/Models/SkillTreeData.cs
    - src/PathPilot.Core/Models/SkillTree.cs
    - src/PathPilot.Core/Services/SkillTreeDataService.cs
    - src/PathPilot.Core/PathPilot.Core.csproj

decisions:
  - what: "Add SkiaSharp to PathPilot.Core instead of Desktop"
    why: "Service needs SKBitmap for decoding, keeps all sprite logic in Core layer"
    alternatives: ["Put service in Desktop", "Abstract bitmap loading interface"]
    impact: "Core has rendering dependency but cleaner architecture"

  - what: "Service owns all SKBitmaps, consumers never dispose"
    why: "Prevents double-dispose bugs, centralizes lifetime management"
    alternatives: ["Consumers clone bitmaps", "Reference counting"]
    impact: "Clear ownership model, safer memory management"

  - what: "Parse 9 sprite types (normal/notable/keystone active/inactive + frame/jewel/groupBackground)"
    why: "Minimal set needed for Phase 5 rendering goals"
    alternatives: ["Parse all sprite types", "Parse on-demand"]
    impact: "Reduced memory footprint, faster parsing"

metrics:
  duration: "2m 52s"
  tasks_completed: 2
  files_created: 2
  files_modified: 4
  commits: 2
  completed_date: "2026-02-15"
---

# Phase 5 Plan 1: Sprite Foundation Data Layer

> Sprite sheet coordinate models, enhanced JSON parsing for sprite metadata, and download+cache service for sprite sheet bitmaps.

## Summary

Created the complete data layer for sprite-based skill tree rendering. This includes models for sprite sheet coordinates, parsing sprite metadata from GGG's JSON (sprites section, node icons, group backgrounds), and a service to download, cache (disk + memory), and manage sprite sheet SKBitmaps. All rendering work in Plan 02 depends on this foundation.

## Tasks Completed

### Task 1: Sprite data models and JSON parsing
**Commit:** `dba7c20`

Created three new models:
- **SpriteSheet.cs**: `SpriteCoordinate` (x, y, w, h), `SpriteSheetData` (filename, sheet dimensions, coord dictionary), `GroupBackground` (image key, isHalfImage flag)

Extended existing models:
- **SkillTreeData**: Added `SpriteSheets` dictionary (sprite type -> zoom level -> SpriteSheetData), `ImageZoomLevels` list
- **PassiveNode**: Added `Icon` property (sprite coord key like "Art/2DArt/SkillIcons/passives/2handeddamage.png")
- **NodeGroup**: Added `Background` property (GroupBackground model)

Enhanced SkillTreeDataService parsing:
- Parse `imageZoomLevels` array from JSON root
- Parse `sprites` section with 9 types: normalActive/Inactive, notableActive/Inactive, keystoneActive/Inactive, frame, jewel, groupBackground
- For each type, parse all zoom levels (typically 4: 0.1246, 0.2109, 0.2972, 0.3835)
- For each zoom level, parse filename, sheet dimensions (w, h), and all coordinate entries
- Parse `icon` property on each node during ParseNode()
- Parse `background` property on each group during ParseGroup()
- Console logging of sprite parsing results (types, zoom levels, coord counts, nodes with icons, groups with backgrounds)

**Files:**
- `src/PathPilot.Core/Models/SpriteSheet.cs` (new)
- `src/PathPilot.Core/Models/SkillTreeData.cs` (extended)
- `src/PathPilot.Core/Models/SkillTree.cs` (extended PassiveNode)
- `src/PathPilot.Core/Services/SkillTreeDataService.cs` (enhanced parsing)

### Task 2: Sprite sheet download and cache service
**Commit:** `9d49656`

Created SkillTreeSpriteService following the proven GemIconService pattern:

**Core features:**
- HttpClient with 30s timeout, UserAgent "PathPilot/1.0"
- Disk cache: `~/.config/PathPilot/tree-sprites/` with 30-day TTL
- In-memory cache: `Dictionary<string, SKBitmap>` keyed by filename
- Download deduplication: `ConcurrentDictionary<string, Task<SKBitmap?>>` prevents parallel downloads of same file
- Service OWNS all SKBitmaps, consumers must NEVER dispose them (singleton pattern)

**Methods:**
- `GetSpriteSheetAsync(string fullUrl)`: Three-tier lookup (memory -> disk cache -> download), returns SKBitmap or null
- `PreloadSpriteSheetsAsync(SkillTreeData treeData, string zoomKey)`: Bulk load all sprite sheets for a zoom level in parallel, logs load count
- `Dispose()`: Disposes HttpClient and all loaded SKBitmaps

**Implementation details:**
- Filename extraction from full URL: takes everything after last `/`, strips query string
- Example: `https://web.poecdn.com/image/passive-skill/skills-0.jpg?511ee3db` -> `skills-0.jpg`
- SKBitmap.Decode() used for both disk cache and downloaded bytes
- Graceful error handling: returns null on any error, logs error message

**Package addition:**
- Added `SkiaSharp 2.88.*` to PathPilot.Core.csproj

**Files:**
- `src/PathPilot.Core/Services/SkillTreeSpriteService.cs` (new)
- `src/PathPilot.Core/PathPilot.Core.csproj` (SkiaSharp package)

## Verification

### Build Verification
- `dotnet build src/PathPilot.Core/PathPilot.Core.csproj` - **SUCCESS** (0 errors, 5 pre-existing warnings in PobXmlParser)
- `dotnet build src/PathPilot.Desktop/PathPilot.Desktop.csproj` - **SUCCESS** (0 errors, 3 pre-existing warnings)

### Expected Runtime Behavior
When the app runs and loads tree data:
- Console will show "Sprite parsing complete:" with counts
- ~9 sprite types parsed (normalActive, normalInactive, etc.)
- Each type has 3-4 zoom levels
- normalActive["0.3835"] should have ~385 sprite coordinates
- ~3135 nodes should have Icon property populated
- ~440 groups should have Background property populated

## Deviations from Plan

None - plan executed exactly as written.

## Impact

**Immediate:**
- All sprite metadata now available in SkillTreeData after JSON parsing
- Sprite sheet bitmaps can be downloaded and cached on demand
- Memory-safe bitmap management with clear ownership model

**Next Steps (Plan 02):**
- Use SpriteSheetData coords to calculate source rects for sprite rendering
- Use SkillTreeSpriteService.PreloadSpriteSheetsAsync() during tree viewer init
- Replace colored circle rendering with actual sprite icon rendering
- Render group backgrounds behind nodes
- Render node frames (keystoneFrame, notableFrame, etc.)

## Technical Notes

### Sprite Coordinate Key Patterns
- **Node icons**: `"Art/2DArt/SkillIcons/passives/2handeddamage.png"` (matches PassiveNode.Icon)
- **Frames**: `"KeystoneFrameUnallocated"`, `"NotableFrameUnallocated"`, `"NormalFrameUnallocated"`
- **Group backgrounds**: `"PSGroupBackground3"`, `"PSGroupBackground2"`, `"PSGroupBackground1"`

### Sprite Sheet File Sharing
- Active sprites (normalActive, notableActive, keystoneActive) share the SAME sprite sheet file (e.g. `skills-0.jpg`)
- Each type has different coordinate mappings in that shared file
- Inactive sprites share `skills-disabled-N.jpg`
- Frame, jewel, groupBackground each have their own dedicated files

### Memory Management
- Service holds one copy of each sprite sheet bitmap in memory
- Consumers use bitmaps directly via GetSpriteSheetAsync()
- Consumers must NEVER call bitmap.Dispose() (service manages lifetime)
- All bitmaps disposed when service is disposed

## Known Issues

None identified.

## Self-Check: PASSED

### Created Files
- FOUND: src/PathPilot.Core/Models/SpriteSheet.cs
- FOUND: src/PathPilot.Core/Services/SkillTreeSpriteService.cs

### Modified Files
- FOUND: src/PathPilot.Core/Models/SkillTreeData.cs
- FOUND: src/PathPilot.Core/Models/SkillTree.cs
- FOUND: src/PathPilot.Core/Services/SkillTreeDataService.cs
- FOUND: src/PathPilot.Core/PathPilot.Core.csproj

### Commits
- FOUND: dba7c20 (Task 1: Sprite data models and JSON parsing)
- FOUND: 9d49656 (Task 2: Sprite sheet download and cache service)
