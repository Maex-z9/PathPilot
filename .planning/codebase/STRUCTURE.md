# Codebase Structure

**Analysis Date:** 2026-02-04

## Directory Layout

```
PathPilot/
├── src/                                # Source code
│   ├── PathPilot.Core/                # Domain and business logic library
│   │   ├── Models/                    # Data models (Build, Gem, Item, SkillSet, etc.)
│   │   ├── Parsers/                   # Path of Building import parsers
│   │   ├── Services/                  # Business logic (BuildStorage, GemDataService, QuestDataService)
│   │   ├── Data/                      # Static data (gems-database.json)
│   │   └── PathPilot.Core.csproj      # Core project file
│   │
│   └── PathPilot.Desktop/             # Avalonia desktop application
│       ├── MainWindow.axaml           # Main window UI and code-behind
│       ├── MainWindow.axaml.cs        # Build import/save/load, loadout management
│       ├── OverlayWindow.axaml        # In-game overlay UI
│       ├── OverlayWindow.axaml.cs     # Overlay display and interaction
│       ├── SettingsWindow.axaml       # Settings dialog for hotkey configuration
│       ├── SettingsWindow.axaml.cs    # Settings dialog code-behind
│       ├── TreeViewerWindow.axaml     # WebView for skill tree display
│       ├── TreeViewerWindow.axaml.cs  # Embedded Chromium tree viewer
│       ├── App.axaml                  # Application-level XAML resources
│       ├── App.axaml.cs               # Application initialization
│       ├── Program.cs                 # Application entry point
│       ├── Platform/                  # Windows-specific overlay implementation
│       │   ├── IOverlayPlatform.cs    # Platform abstraction interface
│       │   └── WindowsOverlayPlatform.cs  # Windows API implementation
│       ├── Services/                  # Desktop-specific services
│       │   ├── HotkeyService.cs       # Global keyboard hook (Windows)
│       │   └── OverlayService.cs      # Overlay lifecycle management
│       ├── Settings/                  # Settings persistence
│       │   └── OverlaySettings.cs     # Hotkey and overlay position storage
│       ├── Converters/                # XAML value converters
│       │   ├── RarityColorConverter.cs    # Item rarity → color
│       │   ├── GemColorConverter.cs       # Gem type → color
│       │   ├── OverlaySupportColorConverter.cs  # Support gem styling
│       │   ├── CompletedTextDecorationConverter.cs  # Quest strikethrough
│       │   ├── StringNotEmptyConverter.cs    # String validation
│       │   └── SupportFontWeightConverter.cs # Font weight for gems
│       ├── ImportDialog.cs             # Import dialog helper (WIP)
│       └── PathPilot.Desktop.csproj   # Desktop project file
│
├── tests/                             # Test projects
│   └── PathPilot.Core.Tests/          # Unit tests for Core
│       └── PathPilot.Core.Tests.csproj
│
├── tools/                             # Utility tools
│   └── GemScraper/                    # Tool to scrape gem data from PoE Wiki
│       └── GemScraper.csproj
│
├── test-console/                      # Console test utility
│   └── TestDecoder/                   # PoB decoder testing
│
├── PathPilot.slnx                     # Visual Studio solution
├── README.md                          # Project documentation
└── .planning/                         # Planning and documentation
    └── codebase/                      # Codebase analysis documents
```

## Directory Purposes

**src/PathPilot.Core/:**
- Purpose: Core domain models and business logic, reusable across all platforms
- Contains: All data models, parsers, and services
- Key files: `Build.cs`, `SkillSet.cs`, `Item.cs`, `Gem.cs`, `PobXmlParser.cs`, `BuildStorage.cs`

**src/PathPilot.Core/Models/:**
- Purpose: Domain data structures representing Path of Exile build components
- Contains:
  - `build.cs` - Main Build container with SkillSets, ItemSets, TreeSets
  - `SkillSet.cs` - Skill loadout with GemLinkGroups
  - `ItemSet.cs` - Item loadout with Items
  - `Item.cs` - Individual gear item with rarity and mods
  - `Gem.cs` - Skill gem with level, quality, type, socket color
  - `GemLinkGroup.cs` - Linked socket group with gem list
  - `SkillTreeSet.cs` - Passive tree configuration with URL and points
  - `Quest.cs` - Quest with rewards and completion tracking
  - `GemSource.cs` - Gem acquisition info (quest, vendor, drop)
  - `ProgressionStep.cs` - Progression tracking (planned)

**src/PathPilot.Core/Parsers/:**
- Purpose: Convert Path of Building exports to domain models
- Contains:
  - `PobXmlParser.cs` - Parses decompressed XML into Build object
  - `PobDecoder.cs` - Decompresses PoB paste code to XML
  - `PobUrlImporter.cs` - Fetches XML from pobb.in URL endpoint
  - `PobParserException.cs` - Custom parser exception type

**src/PathPilot.Core/Services/:**
- Purpose: Business logic and data access
- Contains:
  - `BuildStorage.cs` - Save/load/list/delete builds to JSON files
  - `GemDataService.cs` - Load and lookup gem database (gems-database.json)
  - `QuestDataService.cs` - Provide skill point quests and trial locations

**src/PathPilot.Core/Data/:**
- Purpose: Static reference data
- Contains: `gems-database.json` - Complete gem acquisition and metadata

**src/PathPilot.Desktop/:**
- Purpose: Desktop UI and platform-specific implementations
- Entry point: `Program.cs` → `App.cs` → `MainWindow.cs`

**src/PathPilot.Desktop/Platform/:**
- Purpose: Abstract Windows-specific overlay behavior
- Contains:
  - `IOverlayPlatform.cs` - Interface for platform features (transparent window, click-through)
  - `WindowsOverlayPlatform.cs` - Windows API calls (WS_EX_TRANSPARENT, WS_EX_LAYERED)

**src/PathPilot.Desktop/Services/:**
- Purpose: Desktop-specific services for overlay and input
- Contains:
  - `HotkeyService.cs` - Global keyboard hook (SetWindowsHookEx, WH_KEYBOARD_LL)
  - `OverlayService.cs` - Lifecycle and communication for overlay window

**src/PathPilot.Desktop/Settings/:**
- Purpose: Persistent configuration
- Contains: `OverlaySettings.cs` - Hotkey bindings and overlay position (JSON-persisted)

**src/PathPilot.Desktop/Converters/:**
- Purpose: XAML value converters for data binding
- Contains:
  - `RarityColorConverter.cs` - Rarity string → color (Unique=orange, Rare=yellow, Magic=blue, Normal=gray)
  - `GemColorConverter.cs` - Gem socket color → color
  - `OverlaySupportColorConverter.cs` - Support gem highlight color
  - `CompletedTextDecorationConverter.cs` - Quest completion → strikethrough
  - `StringNotEmptyConverter.cs` - Null/empty string → visibility
  - `SupportFontWeightConverter.cs` - Gem type → font weight

## Key File Locations

**Entry Points:**
- `src/PathPilot.Desktop/Program.cs` - Application startup (Main method, Avalonia builder)
- `src/PathPilot.Desktop/App.axaml.cs` - App initialization, MainWindow creation
- `src/PathPilot.Desktop/MainWindow.axaml.cs` - Primary UI window, service initialization

**Configuration & Settings:**
- `PathPilot.slnx` - Solution file
- `src/PathPilot.Core/PathPilot.Core.csproj` - Core project dependencies
- `src/PathPilot.Desktop/PathPilot.Desktop.csproj` - Desktop project dependencies
- `src/PathPilot.Core/Data/gems-database.json` - Gem acquisition database

**Core Logic:**
- `src/PathPilot.Core/Models/build.cs` - Build aggregate root with loadout management
- `src/PathPilot.Core/Parsers/PobXmlParser.cs` - PoB XML parsing logic
- `src/PathPilot.Core/Services/BuildStorage.cs` - Build persistence (JSON I/O)
- `src/PathPilot.Core/Services/GemDataService.cs` - Gem database lookup

**UI Windows:**
- `src/PathPilot.Desktop/MainWindow.axaml` / `MainWindow.axaml.cs` - Build management, import/save/load
- `src/PathPilot.Desktop/OverlayWindow.axaml` / `OverlayWindow.axaml.cs` - In-game overlay (gems, quests)
- `src/PathPilot.Desktop/SettingsWindow.axaml` / `SettingsWindow.axaml.cs` - Hotkey configuration
- `src/PathPilot.Desktop/TreeViewerWindow.axaml` / `TreeViewerWindow.axaml.cs` - Skill tree WebView

**Platform/Services:**
- `src/PathPilot.Desktop/Platform/WindowsOverlayPlatform.cs` - Windows API for overlay
- `src/PathPilot.Desktop/Services/HotkeyService.cs` - Global keyboard hook registration
- `src/PathPilot.Desktop/Services/OverlayService.cs` - Overlay window lifecycle
- `src/PathPilot.Desktop/Settings/OverlaySettings.cs` - Hotkey and position persistence

## Naming Conventions

**Files:**
- **Windows (XAML):** PascalCase + .axaml (e.g., `MainWindow.axaml`, `OverlayWindow.axaml`)
- **Code-behind:** {WindowName}.axaml.cs (e.g., `MainWindow.axaml.cs`)
- **Services:** {Feature}Service.cs (e.g., `HotkeyService.cs`, `OverlayService.cs`)
- **Converters:** {Type}Converter.cs (e.g., `RarityColorConverter.cs`)
- **Models:** PascalCase matching class name (e.g., `build.cs` → Build class)
- **Parsers:** Pob{Feature}.cs (e.g., `PobXmlParser.cs`, `PobDecoder.cs`)

**Directories:**
- **Plural for collections:** `Models/`, `Parsers/`, `Services/`, `Converters/`, `Platform/`
- **Feature-based grouping:** `src/PathPilot.Core/`, `src/PathPilot.Desktop/`

**C# Classes:**
- **PascalCase:** Build, SkillSet, GemLinkGroup, RarityColorConverter
- **Enums:** GemType (Active, Support), SocketColor (Red, Green, Blue, White)
- **Properties:** PascalCase (Name, Level, IsEnabled, ActiveSkillSetIndex)
- **Methods:** PascalCase (SaveBuild, LoadBuild, GetLoadoutNames, SetActiveLoadout)
- **Private fields:** _camelCase (e.g., _currentBuild, _gemDataService)

## Where to Add New Code

**New Feature (e.g., Build Validator):**
- Primary code: `src/PathPilot.Core/Services/BuildValidator.cs`
- Tests: `tests/PathPilot.Core.Tests/BuildValidatorTests.cs`
- Integration in MainWindow: Call from import flow before parsing

**New UI Component (e.g., Build Comparison Window):**
- Window XAML: `src/PathPilot.Desktop/BuildComparisonWindow.axaml`
- Window code-behind: `src/PathPilot.Desktop/BuildComparisonWindow.axaml.cs`
- Launch from MainWindow button event
- Access builds via BuildStorage

**New Converter (e.g., DamageTypeColorConverter):**
- File: `src/PathPilot.Desktop/Converters/DamageTypeColorConverter.cs`
- Implement IValueConverter interface
- Register in MainWindow or App resources if used in XAML

**New Model (e.g., Gem Variant):**
- File: `src/PathPilot.Core/Models/GemVariant.cs`
- Add to appropriate aggregate (e.g., Gem.Variants property)
- Update serialization in BuildStorage JSON options if needed

**New Parser Feature (e.g., Flask Parsing):**
- Add method to `src/PathPilot.Core/Parsers/PobXmlParser.cs`
- Create corresponding model: `src/PathPilot.Core/Models/Flask.cs`
- Add Flask collection to Build model

**New Service (e.g., Build Exporter):**
- File: `src/PathPilot.Core/Services/BuildExporter.cs`
- Dependency-inject into MainWindow constructor
- Call from export button event

**Utilities/Helpers:**
- Shared helpers: `src/PathPilot.Core/Utilities/` (create if needed)
- Desktop helpers: `src/PathPilot.Desktop/Utilities/` (create if needed)
- Example: String formatters, validation helpers, color utils

## Special Directories

**src/PathPilot.Core/obj/Debug/:**
- Purpose: Generated compiler output
- Generated: Yes (by MSBuild)
- Committed: No (.gitignore)

**src/PathPilot.Desktop/bin/ and obj/:**
- Purpose: Compiled assemblies and intermediate build files
- Generated: Yes (by MSBuild)
- Committed: No (.gitignore)

**~/.config/PathPilot/Builds/:**
- Purpose: Persisted user builds (JSON files)
- Generated: Yes (by BuildStorage.SaveBuild)
- Committed: No (local user data)

**~/.config/PathPilot/overlay-settings.json:**
- Purpose: Hotkey and overlay position configuration
- Generated: Yes (by OverlaySettings.Save)
- Committed: No (local user settings)

---

*Structure analysis: 2026-02-04*
