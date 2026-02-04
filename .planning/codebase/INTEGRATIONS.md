# External Integrations

**Analysis Date:** 2026-02-04

## APIs & External Services

**Path of Building Export:**
- pobb.in - Import builds from pobb.in URLs
  - Endpoint: `https://pobb.in/pob/{code}` (POST data retrieval)
  - Client: `PobUrlImporter` in `src/PathPilot.Core/Parsers/PobUrlImporter.cs`
  - Auth: None (public API)
  - Timeout: 30 seconds per request
  - Format: Raw gzip-compressed PoB XML data

- pastebin.com - Alternative build paste service
  - Endpoint: `https://pastebin.com/raw/{code}`
  - Client: Same `PobUrlImporter` (shared implementation)
  - Auth: None (public API)
  - Format: Raw gzip-compressed PoB XML data

**PoE Wiki Data:**
- poewiki.net - Gem acquisition and source data
  - Endpoints:
    - `https://www.poewiki.net/wiki/Skill_gem` - Active skill gem list
    - `https://www.poewiki.net/wiki/Support_gem` - Support gem list
    - `https://www.poewiki.net/wiki/Transfigured_gem` - Transfigured gem list
    - `https://www.poewiki.net/wiki/{GemName}` - Individual gem pages (scraped)
  - Client: `GemScraper` in `tools/GemScraper/GemScraper/Program.cs`
  - Auth: None (public web scraping)
  - Data Extracted: Quest rewards, vendor rewards, gem colors, availability by act
  - Rate Limiting: 100ms delay between requests (manual throttle)
  - Usage: One-time data generation tool, not called at runtime

## Data Storage

**Local Filesystem Only:**
- Build storage: `~/.config/PathPilot/Builds/` - JSON files (one per build)
  - Format: Gzip-compressed PoB XML stored in `Build.TreeSet` objects
  - Serialization: System.Text.Json with camelCase property naming
  - Example: `src/PathPilot.Core/Services/BuildStorage.cs` (lines 36-50)

- Settings storage: `~/.config/PathPilot/overlay-settings.json`
  - Format: JSON
  - Contents: Hotkey bindings (F11, Ctrl+F11), overlay position (X, Y)
  - Serialization: System.Text.Json
  - Persistence: `src/PathPilot.Desktop/Settings/OverlaySettings.cs` (lines 24-56)

- Gem database: `Data/gems-database.json` (bundled with application)
  - Format: Dictionary[string, GemAcquisitionInfo] JSON
  - Contents: Gem names, colors, quest/vendor sources per act
  - Generation: Produced once by GemScraper tool, checked into repo
  - Client: `GemDataService` in `src/PathPilot.Core/Services/GemDataService.cs`

**No Remote Database:**
- All data is local-first, client-only
- No cloud storage or backend API

## Authentication & Identity

**Auth Provider:** None

**Implementation:**
- No user authentication
- No account system
- Application is single-user, local-only
- Builds identified by filename in local directory

## Monitoring & Observability

**Error Tracking:** None

**Logging:**
- Console output only (development/debugging)
- Examples: `src/PathPilot.Core/Services/GemDataService.cs` (Console.WriteLine calls)
- Log locations: Runtime console, not persisted to file
- No structured logging framework

**Overlay Diagnostics:**
- Hotkey registration status logged to console
- Window hook installation feedback printed to console

## CI/CD & Deployment

**Hosting:** None (desktop application only)

**CI Pipeline:** Not detected

**Deployment:**
- Manual: Build with `dotnet build` or `dotnet publish`
- Distribution: Standalone executable (.exe) with bundled .NET runtime
- User installation: Download and run locally

## Environment Configuration

**Required env vars:** None

**Secrets:** None (no external APIs requiring keys)

**Build Configuration:**
- Solution file: `pathPilot.slnx`
- Projects:
  - `src/PathPilot.Core/PathPilot.Core.csproj` - Core library
  - `src/PathPilot.Desktop/PathPilot.Desktop.csproj` - Desktop UI
  - `tests/PathPilot.Core.Tests/PathPilot.Core.Tests.csproj` - Unit tests
  - `tools/GemScraper/GemScraper/GemScraper.csproj` - Data generation tool

## Webhooks & Callbacks

**Incoming:** None

**Outgoing:** None

## Network Communication

**HTTP Calls:**
- `PobUrlImporter.ImportFromUrlAsync()` - Single HttpClient for all pobb.in/pastebin requests
  - Instances: Injected as dependency, default one created if not provided
  - Timeout: 30 seconds
  - Error handling: HttpRequestException and TaskCanceledException caught with user-friendly messages
  - Location: `src/PathPilot.Core/Parsers/PobUrlImporter.cs` (lines 8-63)

**HTML Scraping:**
- `HtmlWeb` from HtmlAgilityPack (GemScraper only)
- No caching of wiki pages
- Full page load per gem (used in offline tool, not runtime)

## Cross-Platform Considerations

**Windows-Specific:**
- Global keyboard hook: `SetWindowsHookEx` WH_KEYBOARD_LL (HotkeyService)
- Window transparency manipulation: `GetWindowLong`/`SetWindowLong` with WS_EX_TRANSPARENT flag
- Runtime check: `RuntimeInformation.IsOSPlatform(OSPlatform.Windows)`
- Features disabled on non-Windows: Overlay hotkeys, click-through mode
- Location: `src/PathPilot.Desktop/Platform/WindowsOverlayPlatform.cs`

**Cross-Platform:**
- UI framework: Avalonia (runs on Windows, Linux, macOS)
- File paths: Uses `Path.Combine()` for OS-agnostic path handling
- Environment: Uses `Environment.SpecialFolder.ApplicationData` or `UserProfile` for config location

## Integration Points Summary

| Service | Type | Required | Read/Write | Real-time |
|---------|------|----------|-----------|-----------|
| pobb.in | Import API | No | Read | No (user-initiated) |
| pastebin.com | Import API | No | Read | No (user-initiated) |
| poewiki.net | Web Scraping | No | Read | No (offline tool) |
| Local Filesystem | Storage | Yes | Read/Write | Yes |

---

*Integration audit: 2026-02-04*
