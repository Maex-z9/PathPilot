# PathPilot

Ein Desktop-Tool für Path of Exile, das Spielern hilft, ihre Charaktere gemäß einem Path of Building Build zu entwickeln.

Built with .NET 10 and Avalonia UI.

## Features

- **Build Import**: Path of Building Code oder pobb.in URLs importieren
- **Save/Load**: Builds lokal speichern und laden
- **Gem-Anzeige**: Skill Gems mit Level, Quality, Farben und Acquisition-Info (Quest/Vendor)
- **Item-Anzeige**: Items mit Rarity-Farben, Mods und Tooltips
- **Skill Tree Viewer**: Eingebetteter Browser zur Anzeige des Passive Trees
- **Ingame Overlay**: Transparentes Overlay das über dem Spiel angezeigt wird

## Overlay

Das Overlay zeigt deine Gems während des Spielens an:

- **Hotkeys** (konfigurierbar):
  - `F11` - Overlay ein/ausblenden
  - `Ctrl+F11` - Zwischen Click-Through und Interaktiv-Modus wechseln
- **Click-Through**: Klicks gehen durch das Overlay zum Spiel
- **Verschiebbar**: Im Interaktiv-Modus am Header ziehen
- **Position wird gespeichert**: Öffnet immer an der letzten Position

> **Hinweis**: Das Overlay funktioniert nur mit "Windowed Fullscreen" in PoE (Standard für alle PoE-Overlays).

## Installation

```bash
# Repository klonen
git clone https://github.com/your-username/PathPilot.git
cd PathPilot

# Projekt starten
dotnet run --project src/PathPilot.Desktop/PathPilot.Desktop.csproj
```

## Systemanforderungen

- .NET 10 SDK
- Windows (für Overlay-Hotkeys), Linux (eingeschränkte Funktionalität)

## Screenshots

*Coming soon*

## Geplante Features

- [ ] Interaktiver Skilltree Viewer (wie pobb.in)
- [ ] Quest Tracker (Skill Points, Trials, wichtige Items)

## Lizenz

MIT
