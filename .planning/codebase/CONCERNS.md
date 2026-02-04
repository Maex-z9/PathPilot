# Codebase Concerns

**Analysis Date:** 2026-02-04

## Tech Debt

**Parser Complexity - Rough Tree Point Estimation:**
- Issue: `PobXmlParser.ParseSingleTreeSpec()` uses heuristic URL length division to estimate passive tree points used (line 562)
- Files: `src/PathPilot.Core/Parsers/PobXmlParser.cs` (lines 555-563)
- Impact: PointsUsed estimate is inaccurate (divides URL length by 2 with max cap of 123), not the actual allocated node count. Users see incorrect passive point counts
- Fix approach: Implement proper passive tree URL decoding to extract actual allocated node data, or store points separately in PoB export metadata

**Loose Exception Handling:**
- Issue: Generic catch-all blocks in `BuildStorage.GetSavedBuilds()` and `GemDataService.LoadDatabase()` silently skip invalid files/fail gracefully with empty state
- Files: `src/PathPilot.Core/Services/BuildStorage.cs` (lines 91-93), `src/PathPilot.Core/Services/GemDataService.cs` (lines 39-43)
- Impact: Silent failures make debugging difficult; corrupted build files are ignored without user notification
- Fix approach: Log specific exception types (malformed JSON, file permission errors) and provide user feedback via UI

**HttpClient Instantiation:**
- Issue: `PobUrlImporter` creates new HttpClient instance per class instantiation if not provided (line 12)
- Files: `src/PathPilot.Core/Parsers/PobUrlImporter.cs` (lines 10-13)
- Impact: Violates HttpClient best practices; instantiating multiple HttpClient objects can exhaust socket connections over time
- Fix approach: Implement static HttpClient or use HttpClientFactory pattern for dependency injection

**Overly Broad Regex in Item Parsing:**
- Issue: Range value extraction in `Item.ProcessLine()` uses floating point range interpolation (0.5 default) to calculate item stat values, but doesn't account for variance in stat rolls
- Files: `src/PathPilot.Core/Models/Item.cs` (lines 70-96)
- Impact: Displayed item stats may not match actual values; users may make decisions based on incorrect damage/defense numbers
- Fix approach: Document that ranges show mid-roll values, or parse actual item text to extract rolled values without interpolation

## Known Bugs

**Gem Database Not Loading at Runtime:**
- Symptoms: GemDataService looks for `gems-database.json` in `AppContext.BaseDirectory/Data/` but database may not be deployed or accessible
- Files: `src/PathPilot.Core/Services/GemDataService.cs` (line 15)
- Trigger: Run built application; gem acquisition info defaults to "Source unknown - gem not in database" for all gems
- Workaround: Place gems-database.json in correct Data directory relative to application executable

**Quest Progress Not Persisted:**
- Symptoms: No quest completion tracking saved between sessions
- Files: `src/PathPilot.Desktop/OverlayWindow.axaml.cs` (lines 49-56), no persistence layer implemented
- Trigger: Mark quests as complete in overlay, close app, reopen - quests reset to incomplete
- Workaround: None; intended as future feature per .claude/CLAUDE.md

## Security Considerations

**No Input Validation on PoB URL Downloads:**
- Risk: User can paste arbitrary URLs; application downloads and decompresses content without validation
- Files: `src/PathPilot.Core/Parsers/PobUrlImporter.cs` (lines 29-32)
- Current mitigation: URL host validation restricts to pobb.in and pastebin.com; 30-second timeout on requests
- Recommendations:
  1. Validate decompressed XML structure before parsing (check for malicious XML entity expansion)
  2. Implement size limits on downloaded content to prevent DoS via large files
  3. Log download attempts for audit trail

**Global Keyboard Hook - Privilege Requirements:**
- Risk: `HotkeyService` uses `SetWindowsHookEx()` low-level keyboard hook which may require elevated privileges or trigger antivirus detection
- Files: `src/PathPilot.Desktop/Services/HotkeyService.cs` (lines 14-116)
- Current mitigation: Windows-only, gracefully degrades on non-Windows platforms; hook is installed in application process
- Recommendations:
  1. Document requirement for unelevated user (should work as regular user)
  2. Add IAM/antivirus exception documentation for endpoints
  3. Consider alternative: RegisterHotKey() for less invasive global hotkey registration (limited to F1-F24, Ctrl/Alt/Shift modifiers)

**Overlay Click-Through Bypass:**
- Risk: Window style manipulation via `SetWindowLong()` to make overlay transparent - could theoretically be exploited to hide malicious content
- Files: `src/PathPilot.Desktop/Platform/WindowsOverlayPlatform.cs` (lines 24-49)
- Current mitigation: Overlay is application-controlled, only displays build/quest information
- Recommendations: Window title includes "PathPilot" to identify overlay; consider watermark to prevent spoofing

## Performance Bottlenecks

**Gem Database Linear Search on Miss:**
- Problem: `GemDataService.GetGemInfo()` performs up to 4 LINQ lookups with case-insensitive comparison (lines 60-69)
- Files: `src/PathPilot.Core/Services/GemDataService.cs` (lines 46-70)
- Cause: Database lookups fall through to case-insensitive search which iterates all ~200 gems
- Improvement path:
  1. Create case-insensitive dictionary on load: `Dictionary<string, string>` mapping lowercase name to canonical name
  2. Cache last 50 lookups
  3. Index gem names by support suffix to avoid concatenation

**PobXmlParser Item Parsing Overhead:**
- Problem: Parses mods line-by-line with regex replacements and multiple LINQ operations per item (lines 306-427)
- Files: `src/PathPilot.Core/Parsers/PobXmlParser.cs` (lines 364-414)
- Cause: Inefficient line processing for large item text (e.g., heavily modded rare items)
- Improvement path:
  1. Pre-compile regex patterns as static fields
  2. Use StringBuilder for concatenation instead of string.Replace()
  3. Parse mod section only if item has mods (skip lines[0:propertyStartIndex])

**Console Output in Hot Paths:**
- Problem: 35+ `Console.WriteLine()` statements in core parsing and service layers
- Files: Multiple (e.g., `PobXmlParser.cs` lines 247, 302, 513; `GemDataService.cs` lines 17, 32-37)
- Cause: Debug logging left in shipping code; console output blocks thread under high volume
- Improvement path: Replace with proper logging framework (Serilog, ILogger) with configurable levels

## Fragile Areas

**PobXmlParser - Hardcoded Attribute Names:**
- Files: `src/PathPilot.Core/Parsers/PobXmlParser.cs` (lines 57-67, 122-151)
- Why fragile: Build metadata parsing assumes "level" attribute exists for name (line 62); fails silently with "Unnamed Build" if PoB format changes
- Safe modification: Add validation that parses Build element exists, fall back to actual build metadata fields (CharacterName, CharacterClass)
- Test coverage: Zero unit tests; no test data for malformed XML

**Item Parsing State Machine:**
- Files: `src/PathPilot.Core/Parsers/PobXmlParser.cs` (lines 320-362)
- Why fragile: Line-by-line parsing with `inMods` flag assumes strict ordering (Rarity → Name → BaseType → Mods); PoB format variations break parsing
- Safe modification: Tokenize item text first, classify tokens (metadata, base, mod), then reassemble
- Test coverage: No tests for different item rarities (Unique, Rare, Magic, Normal)

**Overlay Window Platform-Specific Code:**
- Files: `src/PathPilot.Desktop/Platform/WindowsOverlayPlatform.cs`, `src/PathPilot.Desktop/OverlayWindow.axaml.cs`
- Why fragile: P/Invoke calls to user32.dll with magic hex constants (WS_EX_TRANSPARENT = 0x00000020); untested on non-Windows or under Windows Aero/DWM changes
- Safe modification: Wrap P/Invoke in try-catch, validate handle validity before SetWindowLong(), test on Windows 10/11 with DWM enabled
- Test coverage: No integration tests; manual testing only

**MainWindow UI Construction in Code-Behind:**
- Files: `src/PathPilot.Desktop/MainWindow.axaml.cs` (lines 77-188, 289-347)
- Why fragile: Dialogs created dynamically in event handlers with hardcoded styling; brittle to refactoring
- Safe modification: Extract dialog creation to separate methods, use MVVM pattern with DataTemplate for reuse
- Test coverage: No unit tests; UI testing requires manual interaction

## Scaling Limits

**Gem Database In-Memory:**
- Current capacity: ~200 gems loaded into memory as Dictionary
- Limit: Negligible concern at current size; if PoE adds 1000+ gems, consider lazy loading or database file
- Scaling path: Load gems incrementally from JSON on first access, or switch to SQLite for indexed lookups

**Overlay Gem Display:**
- Current capacity: ListBox displays all gems from active skillset; tested with ~15 gems
- Limit: No pagination or virtual scrolling; large builds with 50+ gems may cause scroll lag
- Scaling path: Implement virtualizing StackPanel for ListBox, or split gems into multiple tabs (by slot)

**Build File Storage:**
- Current capacity: Saves builds as JSON files to `~/.config/PathPilot/Builds/`
- Limit: Linear scan of directory on load; 1000+ builds may cause UI hang
- Scaling path: Implement build index (JSON manifest), or switch to lightweight database for metadata queries

## Dependencies at Risk

**Path of Building Compatibility:**
- Risk: PoB export format may change; no version detection in XML parsing
- Impact: New builds from updated PoB will fail to parse silently or with generic "Failed to parse PoB XML" error
- Migration plan:
  1. Add version detection: parse `BuildVersion` or `PoB Version` attribute from XML root
  2. Handle format changes gracefully: detect missing Skill/Items elements and provide user-friendly error
  3. Maintain parser version compatibility matrix in tests

**Avalonia UI Framework Evolution:**
- Risk: Avalonia 11.0+ may change WebViewControl-Avalonia or P/Invoke compatibility
- Impact: Skill tree viewer and overlay may break on major framework updates
- Migration plan: Pin Avalonia version in csproj until tested; review breaking changes in release notes before updating

**pobb.in / Pastebin Availability:**
- Risk: External URL import depends on pobb.in and pastebin.com uptime and API compatibility
- Impact: Cannot import builds if services are down or change endpoint format
- Migration plan: Add fallback to paste code input when URL import fails; cache last successful build

## Missing Critical Features

**No Test Suite:**
- Problem: Only stub test file exists (`UnitTest1.cs` with empty test)
- Blocks: Cannot safely refactor parsers; no regression detection for PoB format changes
- Priority: High
- Approach:
  1. Add tests for `PobDecoder.DecodeToXml()` with known PoB codes
  2. Add tests for `PobXmlParser` with sample XML fixtures (from real builds)
  3. Add tests for `BuildStorage` file I/O
  4. Add snapshot tests for item/gem parsing

**No Error Recovery UI:**
- Problem: Import/parse errors show raw exception messages in pop-up; no guidance on fixing issues
- Blocks: Users cannot diagnose why a build fails to import
- Priority: Medium
- Approach:
  1. Categorize exceptions: InvalidPobCode, NetworkError, CorruptedBuild, UnsupportedFormat
  2. Show helpful error messages: "Paste code is invalid. Try copying directly from PoB window" etc.
  3. Log full stack trace to file for developer debugging

**No Undo/Undo Stack:**
- Problem: No way to revert accidental changes (e.g., deleting saved build)
- Blocks: Users may accidentally lose work
- Priority: Low
- Approach: Implement soft delete (archive instead of remove), or add build history/snapshots

## Test Coverage Gaps

**Parser Tests:**
- What's not tested: PobDecoder (base64/deflate decoding), PobXmlParser (skill/item/tree parsing), PobUrlImporter (HTTP download)
- Files: `src/PathPilot.Core/Parsers/PobDecoder.cs`, `src/PathPilot.Core/Parsers/PobXmlParser.cs`, `src/PathPilot.Core/Parsers/PobUrlImporter.cs`
- Risk: Regression not caught; broken import after refactoring
- Priority: Critical

**Item Formatting Tests:**
- What's not tested: Item.ProcessLine() range interpolation, HTML entity handling, mod parsing
- Files: `src/PathPilot.Core/Models/Item.cs` (lines 70-102)
- Risk: Item stat values displayed incorrectly; user makes wrong build decisions
- Priority: High

**Platform-Specific Tests:**
- What's not tested: WindowsOverlayPlatform click-through, HotkeyService global hook registration
- Files: `src/PathPilot.Desktop/Platform/WindowsOverlayPlatform.cs`, `src/PathPilot.Desktop/Services/HotkeyService.cs`
- Risk: Overlay doesn't work on Windows updates; hotkey conflicts with other apps
- Priority: Medium

**UI Integration Tests:**
- What's not tested: Full import → loadout selection → overlay update workflow
- Files: `src/PathPilot.Desktop/MainWindow.axaml.cs`, `src/PathPilot.Desktop/OverlayWindow.axaml.cs`
- Risk: Silent failures in UI flow; users see blank overlays or missing data
- Priority: Medium

---

*Concerns audit: 2026-02-04*
