# Architecture

**Analysis Date:** 2026-02-04

## Pattern Overview

**Overall:** Layered desktop application with separation between domain models (Core) and presentation (Desktop). Uses Avalonia UI framework with Windows-specific overlay implementation via platform abstractions.

**Key Characteristics:**
- Two-project architecture: `PathPilot.Core` (domain logic) and `PathPilot.Desktop` (presentation)
- Build as central data model that contains SkillSets, ItemSets, and TreeSets
- Event-driven communication between services (HotkeyService -> OverlayService)
- JSON-based persistence for builds stored in `~/.config/PathPilot/Builds/`
- Platform abstraction layer for Windows-specific overlay behavior

## Layers

**Domain Model Layer (PathPilot.Core/Models):**
- Purpose: Represent Path of Exile build structure, items, skills, quests
- Location: `src/PathPilot.Core/Models/`
- Contains: `Build`, `SkillSet`, `ItemSet`, `Item`, `Gem`, `GemLinkGroup`, `SkillTreeSet`, `Quest`
- Depends on: Nothing (pure domain objects)
- Used by: Parsers and Services in Core; all Desktop layer components

**Parser Layer (PathPilot.Core/Parsers):**
- Purpose: Convert Path of Building (PoB) import formats to domain models
- Location: `src/PathPilot.Core/Parsers/`
- Contains: `PobXmlParser` (parses XML structure), `PobDecoder` (decodes paste code), `PobUrlImporter` (fetches from pobb.in)
- Depends on: Models, GemDataService
- Used by: MainWindow import flow

**Service Layer (PathPilot.Core/Services):**
- Purpose: Provide business logic and data access
- Location: `src/PathPilot.Core/Services/`
- Contains: `BuildStorage` (JSON persistence), `GemDataService` (gem database), `QuestDataService` (quest data)
- Depends on: Models
- Used by: MainWindow, OverlayWindow, Parsers

**Presentation Layer (PathPilot.Desktop):**
- Purpose: UI windows, dialogs, converters, and overlay display
- Location: `src/PathPilot.Desktop/`
- Contains: Windows (MainWindow, OverlayWindow, SettingsWindow, TreeViewerWindow), Converters, Settings
- Depends on: Core models and services
- Used by: End users directly

**Platform Abstraction Layer (PathPilot.Desktop/Platform):**
- Purpose: Hide Windows-specific overlay implementation details
- Location: `src/PathPilot.Desktop/Platform/`
- Contains: `IOverlayPlatform` (interface), `WindowsOverlayPlatform` (Windows API implementation)
- Depends on: Nothing (raw Windows API)
- Used by: OverlayWindow

**Desktop Services Layer (PathPilot.Desktop/Services):**
- Purpose: Manage overlay interaction and global hotkey listening
- Location: `src/PathPilot.Desktop/Services/`
- Contains: `HotkeyService` (global keyboard hook), `OverlayService` (overlay lifecycle)
- Depends on: Core models, Settings, Platform abstraction
- Used by: MainWindow, OverlayWindow

## Data Flow

**Build Import and Parse:**

1. User clicks Import button → MainWindow shows import dialog
2. User pastes Path of Building code or URL
3. MainWindow detects format (URL vs. paste code) in ImportButton_Click
4. If URL: `PobUrlImporter.ImportFromUrlAsync()` fetches XML from pobb.in
5. If code: `PobDecoder.DecodeToXml()` decompresses paste code to XML
6. `PobXmlParser.Parse()` converts XML to `Build` object with:
   - Build metadata (name, class, level, ascendancy)
   - SkillSets with GemLinkGroups and Gems
   - ItemSets with Items
   - TreeSets with tree URLs and point counts
7. GemDataService enriches gems with acquisition info during parsing
8. MainWindow updates UI: LoadoutSelector, LinkGroupsListBox, ItemsListBox

**Build Persistence:**

1. User clicks Save → MainWindow shows save dialog
2. User enters build name
3. MainWindow calls `BuildStorage.SaveBuild(build)` → JSON written to `~/.config/PathPilot/Builds/{name}.json`
4. User clicks Load → `BuildStorage.GetSavedBuilds()` lists all saved JSON files
5. Selected build loaded via `BuildStorage.LoadBuild(filePath)` → deserialized to `Build` object

**Loadout Switching:**

1. User selects loadout from LoadoutSelector ComboBox
2. LoadoutSelector_SelectionChanged triggered
3. `Build.SetActiveLoadout(loadoutName)` synchronizes indices across:
   - `ActiveSkillSetIndex` → displays gems in LinkGroupsListBox
   - `ActiveItemSetIndex` → displays items in ItemsListBox
   - `ActiveTreeSetIndex` → displays tree info and enables OpenTreeButton
4. UpdateDisplayedLoadout() refreshes all UI ListBoxes with active set data

**Overlay Lifecycle:**

1. MainWindow registers HotkeyService.ToggleOverlayRequested event handler
2. Global keyboard hook (Windows API) detects hotkey press (F11 by default)
3. HotkeyService fires ToggleOverlayRequested → OverlayService.ToggleVisibility()
4. OverlayService creates OverlayWindow if not exists, shows/hides it
5. OverlayWindow displays current Build via SetBuild() → updates GemsPanel and QuestsPanel

**State Management:**

- Build state: Stored in MainWindow._currentBuild (in-memory), persisted to disk via BuildStorage
- Overlay settings: OverlaySettings.Load() reads from `~/.config/PathPilot/overlay-settings.json`
- Hotkey configuration: Persisted in OverlaySettings, modified via SettingsWindow
- Quest completion: Will be stored in `~/.config/PathPilot/quest-progress.json` (planned)

## Key Abstractions

**Build Container:**
- Purpose: Root aggregate holding all loadouts and settings for one build
- Examples: `src/PathPilot.Core/Models/build.cs`
- Pattern: Central object that owns SkillSets, ItemSets, TreeSets; provides unified loadout switching via `SetActiveLoadout()`

**Link Group Pattern:**
- Purpose: Represent socket groups (6-socket chains) with their gems
- Examples: `src/PathPilot.Core/Models/GemLinkGroup.cs`
- Pattern: Key-Value pairs where Key is socket location (e.g., "Body Armour"), Value is gem list; includes metadata (enabled, main active skill)

**Platform Abstraction:**
- Purpose: Hide Windows-specific overlay API calls from OverlayWindow
- Examples: `src/PathPilot.Desktop/Platform/IOverlayPlatform.cs`, `WindowsOverlayPlatform.cs`
- Pattern: Interface-based abstraction allowing cross-platform support without UI changes

**Service Locator Pattern (Loose):**
- Purpose: Services instantiated in MainWindow and injected where needed
- Examples: MainWindow creates GemDataService, OverlaySettings, HotkeyService, OverlayService
- Pattern: Services accept dependencies in constructors; event-based communication between services

## Entry Points

**Application Entry:**
- Location: `src/PathPilot.Desktop/Program.cs`
- Triggers: Executable startup
- Responsibilities: Configures Avalonia AppBuilder, starts classic desktop lifetime with MainWindow

**Main Window:**
- Location: `src/PathPilot.Desktop/MainWindow.axaml.cs`
- Triggers: Application startup
- Responsibilities:
  - Initialize all services (GemDataService, HotkeyService, OverlayService)
  - Handle import/save/load workflows
  - Display build details and loadout selection
  - Coordinate overlay lifecycle
  - Register/unregister global hotkey on window open/close

**Overlay Window:**
- Location: `src/PathPilot.Desktop/OverlayWindow.axaml.cs`
- Triggers: User clicks Overlay button or presses F11 hotkey
- Responsibilities:
  - Display gems from active skill set
  - Display quest tracking tabs
  - Handle dragging and interactive mode toggle
  - Apply Windows-specific overlay transparency and click-through

**Hotkey Service:**
- Location: `src/PathPilot.Desktop/Services/HotkeyService.cs`
- Triggers: Global keyboard events (Windows hook)
- Responsibilities:
  - Install/uninstall low-level keyboard hook (WH_KEYBOARD_LL)
  - Detect configured hotkeys (F11 for toggle, Ctrl+F11 for interactive)
  - Fire events to OverlayService

## Error Handling

**Strategy:** Try-catch at boundaries (import, save/load); pass back to UI for display

**Patterns:**

- **Parser errors:** PobXmlParser.Parse() wraps XML parsing in try-catch, throws InvalidOperationException with message. MainWindow catches and shows error dialog with exception message.
- **File I/O errors:** BuildStorage methods catch during JSON read/write. LoadBuild() returns null on missing file. GetSavedBuilds() silently skips invalid files.
- **Hotkey registration:** HotkeyService.Start() catches hook setup failures, logs to console.
- **UI dialogs:** Errors shown in modal windows with TextBlock displaying exception message.

## Cross-Cutting Concerns

**Logging:** Console.WriteLine() used throughout for debugging. No structured logging framework.

**Validation:**
- Model setters ensure non-null defaults (e.g., `Name = "Unnamed Build"`)
- Parser validates XML structure before extraction
- File names sanitized via `BuildStorage.SanitizeFileName()`

**Authentication:** Not applicable (local desktop app)

**Settings Persistence:**
- `OverlaySettings` uses JSON serialization to `~/.config/PathPilot/overlay-settings.json`
- `BuildStorage` saves builds to `~/.config/PathPilot/Builds/*.json`
- Settings loaded on app startup, saved on settings dialog close
- Overlay position persisted and restored each session

---

*Architecture analysis: 2026-02-04*
