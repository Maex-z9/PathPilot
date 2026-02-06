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
│       ├── TreeViewerWindow.axaml # Native SkiaSharp skill tree display
│       ├── Controls/            # Custom controls (SkillTreeCanvas)
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
- **Skill tree viewer**: Native SkiaSharp rendering with zoom, pan, and hover tooltips
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
- **TreeViewerWindow**: Native SkiaSharp rendering (kein WebView mehr)
- **SkillTreeCanvas**: Custom Avalonia Control mit ICustomDrawOperation für SkiaSharp
- **Converters**: RarityColorConverter (Unique=orange, Rare=yellow, Magic=blue)
- **OverlayWindow**: Transparent, topmost, draggable overlay
- **SettingsWindow**: Hotkey configuration with key recording

### Skill Tree Viewer (Native Rendering)
- **SkillTreeCanvas** (`Controls/SkillTreeCanvas.cs`): Custom Control mit SkiaSharp rendering
- **SkillTreeDataService**: Lädt PoE 1 Tree Data von `poe-tool-dev/passive-skill-tree-json` (Version 3.25.0)
- **TreeUrlDecoder**: Decodiert Tree URLs zu Node IDs
- **Cache**: `~/.config/PathPilot/tree-cache/poe1-3.25.json`

#### Tree URL Decoding (Version 6 Format)
- Bytes 0-3: Version (big endian)
- Byte 4: Class ID
- Byte 5: Ascendancy ID
- Byte 6: Node Count (WICHTIG: Lua ist 1-indexed, also `b:byte(7)` = C# `bytes[6]`)
- Byte 7+: Node IDs (2 bytes pro Node, big endian)

#### Position Calculation
- GGG Tree Bounds: X von -13902 bis +12430, Y von -10689 bis +10023
- Offset anwenden: +14000 X, +11000 Y (shift in positive Koordinaten)
- Orbit Radii: `{ 0, 82, 162, 335, 493, 662, 846 }`
- Nodes Per Orbit: `{ 1, 6, 16, 16, 40, 72, 72 }`

#### Rendering
- Connections: Batched in single SKPath für Performance
- Nodes: Gold (200,150,50) für allocated, Dark Gray (60,60,60) für unallocated
- Node Sizes: Keystone=18f, Notable=12f, JewelSocket=10f, Normal=6f
- Zoom: Via `canvas.Scale()` in SkiaSharp, nicht RenderTransform

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

### Quest Tracker System
- **Quest Model** (`Quest.cs`): INotifyPropertyChanged, Id aus Act+Name+Location, TrapType Property
- **QuestDataService**: 22 Skill Point Quests, 12 Ascendancy Trials (mit Trap-Typen), 4 Labs
- **QuestProgressService**: Speichert/lädt erledigte Quest-IDs in `~/.config/PathPilot/quest-progress.json`
- **Overlay UI**: Sub-Tabs [Skills] [Trials] [Labs], "Erledigte ausblenden" Filter, Reward-Badges, Trap-Type Anzeige
- **Fortschrittsanzeige**: Zähler (z.B. "3/22") pro Kategorie

## TODOs / Planned Features

- [x] **Skilltree Viewer - Basic Rendering**: Native SkiaSharp Rendering mit Nodes, Connections, Allocated Nodes (gold)
- [x] **Skilltree Viewer - Navigation**: Mausrad-Zoom, Drag-Pan, Start zentriert auf allocated Nodes
- [x] **Skilltree Viewer - Interaktiv**: Node-Hover mit Tooltips, verbundene Nodes anzeigen
- [x] **Ingame Overlay**: Transparentes Overlay das über dem Spiel angezeigt wird (Build-Info, Gems, Items)
- [x] **Quest Tracker**: Quest-Progress mit Speicherung, Kategorie-Tabs (Skills/Trials/Labs), Filter, Rewards, Trial-Trap-Typen

## Bekannte Probleme / Gotchas

### Tree URL Decoding
- **Lua vs C# Indexing**: PoB Lua Code ist 1-indexed, C# ist 0-indexed. `b:byte(7)` in Lua = `bytes[6]` in C#
- **Gespeicherte Builds**: Alte Builds haben falsch decodierte Node IDs. TreeViewerWindow decodiert jetzt direkt aus URL statt gespeicherte Nodes zu verwenden
- **PoE 1 vs PoE 2**: GGG's `skilltree-export` Repo enthält nur PoE 2 Daten. Für PoE 1 nutze `poe-tool-dev/passive-skill-tree-json`

### SkiaSharp Rendering
- **Zoom**: Nicht RenderTransform verwenden - das skaliert nur visuell, nicht die Koordinaten. Stattdessen `canvas.Scale()` im SkiaSharp Render verwenden
- **ICustomDrawOperation.Equals**: Muss `false` returnen sonst cached Avalonia das Rendering
- **Hit Testing für Pointer Events**: Custom Controls müssen Background RENDERN um Pointer Events zu empfangen. `Background="Transparent"` Property allein reicht nicht - muss in Render() mit `context.FillRectangle(Background, ...)` gezeichnet werden
- **Transformation Order**: Für Zoom+Pan: erst `Translate(-offset * zoom)`, dann `Scale(zoom)`. Reihenfolge ist kritisch!

## Language

The developer prefers German for communication.
