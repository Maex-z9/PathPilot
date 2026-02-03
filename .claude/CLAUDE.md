# PathPilot

A Path of Exile build guide desktop application built with .NET and Avalonia UI.

## Project Structure

```
PathPilot/
├── src/
│   ├── PathPilot.Core/          # Core library
│   │   ├── Models/              # Data models (Build, Gem, Item, SkillSet, ItemSet)
│   │   ├── Parsers/             # PoB import parsing (PobXmlParser, PobCodeDecoder)
│   │   └── Data/                # Gem database
│   └── PathPilot.Desktop/       # Avalonia desktop app
│       ├── MainWindow.axaml     # Main UI
│       └── Converters/          # Value converters (RarityColor, GemColor, etc.)
├── tests/
│   └── PathPilot.Core.Tests/
└── tools/
    └── GemScraper/              # Tool to scrape gem data
```

## Running the Project

```bash
dotnet run --project src/PathPilot.Desktop/PathPilot.Desktop.csproj
```

## Key Features

- Import builds from Path of Building (paste code or pobb.in URLs)
- Save/Load builds locally (stored in ~/.config/PathPilot/Builds/)
- Unified loadout selector (changes SkillSet, ItemSet, and TreeSet together)
- Gem display with colors, levels, quality, and acquisition info
- Item display with rarity colors and mod highlighting
- Item tooltips showing full item details (formatted from PoB's internal format)
- Skill tree integration with "Open Tree" button to view in browser

## Technical Notes

- **Unified Loadout System**: `Build.SetActiveLoadout(name)` sets both `ActiveSkillSetIndex` and `ActiveItemSetIndex` by matching the loadout name

- **Item.FormattedText**: Computed property that cleans up PoB's raw item text by:
  - Removing metadata lines (Rarity, Prefix, Suffix, LevelReq, etc.)
  - Removing PoB tags (`{range:X}`, `{tags:...}`, `{crafted}`, etc.)
  - Calculating actual values from ranges (e.g., `{range:0.5}(10-16)` → `13`)

- **Converters**: RarityColorConverter maps item rarity to PoE colors (Unique=orange, Rare=yellow, Magic=blue)

## Language

The developer prefers German for communication.
