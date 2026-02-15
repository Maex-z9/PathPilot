# PathPilot

A desktop companion app for Path of Exile — import builds, track gems & items, and use an in-game overlay.

Built with C# 12, .NET 10, Avalonia UI, and SkiaSharp.

**[Website](https://maex-z9.github.io/PathPilotWebsite/)** · **[Download](https://github.com/Maex-z9/PathPilot/releases/latest)** · **Open Source · MIT License**

## Features

- **Build Import** — Paste Path of Building codes or pobb.in URLs directly
- **Save/Load** — Save and load builds locally, auto-loads last build on startup
- **Unified Loadout Selector** — Switch SkillSet, ItemSet, and TreeSet together
- **Gem Display** — Skill gems with real PoE Wiki icons, levels, quality, colors, and acquisition info (quest/vendor)
- **Item Display** — Items with rarity colors, mod highlighting, and tooltips with full details
- **Skill Tree Viewer** — 1300+ nodes with real PoE sprites (Normal, Notable, Keystone), group backgrounds, zoom-based LOD switching, and hover tooltips
- **Node Search** — Search nodes by name, navigate to results, and highlight matches in the tree
- **Ingame Overlay** — Transparent overlay showing gems and quest tracker over the game (Windows)
- **Quest Tracker** — Track skill point quests, ascendancy trials (with trap types), and labs with progress saving

## Overlay

The overlay shows your gems and quest progress while playing:

- **Hotkeys** (configurable):
  - `F11` - Toggle overlay visibility
  - `Ctrl+F11` - Toggle between click-through and interactive mode
- **Click-Through**: Clicks pass through the overlay to the game
- **Draggable**: Drag via header in interactive mode
- **Position saved**: Always opens at the last position
- **Tabs**: Switch between Gems and Quests view
- **Quest categories**: Quests, Trials, Labs with progress counters

> **Note**: The overlay only works with "Windowed Fullscreen" in PoE (standard for all PoE overlays).

## Installation

Download the latest installer or portable ZIP from the [Releases](https://github.com/Maex-z9/PathPilot/releases/latest) page.

### Build from source

```bash
git clone https://github.com/Maex-z9/PathPilot.git
cd PathPilot
dotnet run --project src/PathPilot.Desktop/PathPilot.Desktop.csproj
```

## System Requirements

- Windows (full functionality including overlay) or Linux (limited)
- .NET 10 SDK (only when building from source)

## Screenshots

### Main Window — Gems & Items
![Main Window](screenshots/main-window.png)

### Skill Tree Viewer
![Skill Tree](screenshots/skill-tree.png)

![Skill Tree Detail](screenshots/skill-tree-detail.png)

### Ingame Overlay
![Overlay — Gems](screenshots/overlay.png)

![Overlay — Quests](screenshots/overlay-quests.png)

![Overlay — Trials](screenshots/overlay-trials.png)

![Overlay — Labs](screenshots/overlay-labs.png)

## Roadmap

| Version | Status | Highlights |
|---------|--------|------------|
| v1.0 | Released | Core skill tree rendering, build import, gem/item display |
| v1.1 | In Progress | Real PoE sprites, node search, stat tooltips, node editing |
| v2.0 | Planned | Minimap, ascendancy trees, advanced quest tracking |

## License

MIT
