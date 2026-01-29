# PathPilot

Ein Desktop-Overlay-Tool für Path of Exile, das Spielern hilft, ihre Charaktere gemäß einem Path of Building Build zu entwickeln.

## Features

- **Build-Import**: Lädt Path of Building `.xml` Dateien
- **Skill Tree Progression**: Zeigt optimale Reihenfolge der Passive Nodes
- **Gem Guide**: Listet benötigte Skill Gems mit Fundorten
- **Gear Planning**: Socket/Link-Anforderungen für Items
- **In-Game Overlay**: Transparentes, nicht-intrusives Overlay

## Tech Stack

- .NET 10
- Avalonia UI (Cross-Platform)
- xUnit (Testing)

## Build & Run

```bash
# Projekt bauen
dotnet build

# Tests ausführen
dotnet test

# App starten
dotnet run --project src/PathPilot.Desktop
