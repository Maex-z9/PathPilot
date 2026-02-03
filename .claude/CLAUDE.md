# PathPilot

A Path of Exile build guide desktop application built with .NET and Avalonia UI.

## Project Structure

```
PathPilot/
├── src/
│   ├── PathPilot.Core/          # Core library
│   │   ├── Models/              # Data models (Build, Gem, Item, SkillSet, ItemSet, SkillTreeSet)
│   │   ├── Parsers/             # PoB import parsing (PobXmlParser, PobCodeDecoder, PobUrlImporter)
│   │   ├── Services/            # BuildStorage (save/load), GemDataService
│   │   └── Data/                # Gem database (gems-database.json)
│   └── PathPilot.Desktop/       # Avalonia desktop app
│       ├── MainWindow.axaml     # Main UI (gems, items, loadout selector)
│       ├── TreeViewerWindow.axaml # WebView for skill tree display
│       ├── OverlayWindow.axaml  # Ingame overlay (transparent, topmost)
│       ├── SettingsWindow.axaml # Settings dialog (hotkeys, position)
│       ├── Converters/          # Value converters (RarityColor, GemColor, etc.)
│       ├── Services/            # OverlayService, HotkeyService
│       ├── Settings/            # OverlaySettings (JSON persistence)
│       └── Platform/            # Platform-specific (WindowsOverlayPlatform)
├── tests/
│   └── PathPilot.Core.Tests/
└── tools/
    └── GemScraper/              # Tool to scrape gem acquisition data from PoE Wiki
```

## Running the Project

```bash
dotnet run --project src/PathPilot.Desktop/PathPilot.Desktop.csproj
```

## Key Features

- **Import builds**: From Path of Building paste code or pobb.in URLs
- **Save/Load builds**: Stored locally in `~/.config/PathPilot/Builds/` as JSON
- **Unified loadout selector**: Changes SkillSet, ItemSet, and TreeSet together
- **Gem display**: Colors, levels, quality, acquisition info (quest/vendor)
- **Item display**: Rarity colors, mod highlighting, tooltips with full details
- **Skill tree viewer**: Embedded WebView (Chromium) to display passive tree in-app
- **Ingame Overlay**: Transparent overlay showing gems, works over PoE (Windows)

## Technical Notes

### Models
- **Build**: Main container with SkillSets, ItemSets, TreeSets
- **SkillTreeSet**: Title, TreeUrl, PointsUsed for each loadout
- **Item.FormattedText**: Cleans PoB raw text, calculates range values

### Parsers
- **PobUrlImporter**: Uses `https://pobb.in/pob/{code}` endpoint (not /raw/)
- **PobXmlParser**: Parses skills, items, and tree specs from PoB XML

### Services
- **BuildStorage**: Save/load builds to JSON, list saved builds, delete builds
- **GemDataService**: Loads gem database, provides acquisition info

### UI Components
- **TreeViewerWindow**: Uses WebViewControl-Avalonia (Chromium) for embedded browser
- **Converters**: RarityColorConverter (Unique=orange, Rare=yellow, Magic=blue)
- **OverlayWindow**: Transparent, topmost, draggable overlay
- **SettingsWindow**: Hotkey configuration with key recording

### Overlay System (Windows)
- **HotkeyService**: Global keyboard hook via `SetWindowsHookEx(WH_KEYBOARD_LL)`
- **WindowsOverlayPlatform**: Click-through via `WS_EX_TRANSPARENT | WS_EX_LAYERED`
- **OverlaySettings**: Persisted to `~/.config/PathPilot/overlay-settings.json`
- **Default Hotkeys**: F11 (toggle visibility), Ctrl+F11 (toggle interactive)
- **Features**: Draggable, position saved, click-through mode, configurable hotkeys

### Gem Scraper
- Scrapes gem data from poewiki.net
- Uses `WebUtility.HtmlDecode()` for HTML entities (fixes apostrophe gems like "Battlemage's Cry")
- Uses `Uri.EscapeDataString()` for URL encoding

## Nächste Session - Quest Tracker fortsetzen

### Bereits erstellt:
- `PathPilot.Core/Models/Quest.cs` - Quest Model mit QuestReward enum
- `PathPilot.Core/Services/QuestDataService.cs` - Alle wichtigen Quests (Act 1-10, Skill Points, Trials, Labs)

### Nächste Schritte (Quest Tracker im Overlay):
1. **OverlayWindow.axaml erweitern** - Tabs oder Toggle zwischen Gems/Quests
2. Quest-Liste im Overlay anzeigen (kompakte Darstellung)
3. Filtering nach Act (basierend auf Character-Level oder manuell)
4. Checkbox zum Markieren erledigter Quests (click im Interactive-Mode)
5. Progress-Speicherung in `~/.config/PathPilot/quest-progress.json`
6. Nur unerledigte Quests anzeigen (oder Toggle)

### UI-Konzept fürs Overlay:
```
+------------------+
| Build: Name      |
| [Gems] [Quests]  |  <- Toggle Buttons
+------------------+
| Act 2            |
| [ ] Bandit Quest |
|     The Forest   |
| [x] Great White  |
+------------------+
```

### Quest-Daten enthalten:
- Alle Skill Point Quests (Act 1-10)
- Ascendancy Trials (6 für Normal Lab, 3 für Cruel, 3 für Merciless)
- Labyrinth-Quests (Normal, Cruel, Merciless)
- Location, RecommendedLevel, IsOptional

## TODOs / Planned Features

- [ ] **Skilltree Viewer wie pobb.in**: Interaktiver Skilltree mit Node-Auswahl, Hover-Infos, und voller Funktionalität wie auf pobb.in
- [x] **Ingame Overlay**: Transparentes Overlay das über dem Spiel angezeigt wird (Build-Info, Gems, Items)
- [ ] **Quest Tracker**: Zeigt an welche Quests als nächstes erledigt werden sollten (Skill Points, Trials, wichtige Items) - **IN PROGRESS**

## Language

The developer prefers German for communication.
