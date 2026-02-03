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

## Nächste Session - Quest Tracker erweitern

### Bereits implementiert:
- `PathPilot.Core/Models/Quest.cs` - Quest Model mit QuestReward enum
- `PathPilot.Core/Services/QuestDataService.cs` - Korrekte Quest-Liste:
  - 22 Skill Point Quests (Act 1-10)
  - 12 Ascendancy Trials (6 Normal, 3 Cruel, 3 Merciless)
  - 4 Labyrinth-Quests (Normal, Cruel, Merciless, Eternal)
- `OverlayWindow.axaml` - Tab-Buttons [Gems] [Quests] implementiert
- `CompletedTextDecorationConverter.cs` - Durchgestrichener Text für erledigte Quests

### Nächste Schritte:
1. Progress-Speicherung in `~/.config/PathPilot/quest-progress.json`
2. Filter nach Act oder nur unerledigte anzeigen
3. Trials und Labs auch anzeigen (separate Tabs oder Abschnitte)
4. Quest-Reward (+1 Passive, +2 Passives) anzeigen

### Quest-Daten (22 Skill Points total, +2 für Bandits):
- Act 1: 2 Quests (Dweller, Fairgraves)
- Act 2: 2 Quests (Great White Beast, Bandits +2)
- Act 3: 2 Quests (Victario, Piety's Pets)
- Act 4: 1 Quest (Deshret)
- Act 5: 2 Quests (Science, Kitava's Torments)
- Act 6: 3 Quests (Father of War, Tukohama, Abberath)
- Act 7: 3 Quests (Greust, Gruthkul, Kishara)
- Act 8: 3 Quests (Love is Dead, Yugul, Gemling Legion)
- Act 9: 2 Quests (Shakari, Kira)
- Act 10: 2 Quests (Vilenta, End to Hunger)

## TODOs / Planned Features

- [ ] **Skilltree Viewer wie pobb.in**: Interaktiver Skilltree mit Node-Auswahl, Hover-Infos, und voller Funktionalität wie auf pobb.in
- [x] **Ingame Overlay**: Transparentes Overlay das über dem Spiel angezeigt wird (Build-Info, Gems, Items)
- [ ] **Quest Tracker**: Zeigt an welche Quests als nächstes erledigt werden sollten (Skill Points, Trials, wichtige Items) - **IN PROGRESS**

## Language

The developer prefers German for communication.
