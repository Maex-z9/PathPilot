# PathPilot

## What This Is

PathPilot ist eine Desktop-App für Path of Exile Build-Guides. Sie importiert Builds aus Path of Building, zeigt Gems, Items und Skill Trees an, und bietet ein Ingame-Overlay. Der native SkiaSharp Skill Tree Viewer bietet volle Kontrolle über Darstellung und Interaktion mit Zoom, Pan und Hover-Tooltips.

## Core Value

Flüssiges Rendering von ~1300 Nodes mit klarer Allocated-Markierung und natürlicher Zoom/Pan-Navigation.

## Current Milestone: v1.1 Skill Tree Overhaul

**Goal:** Den Skill Tree Viewer visuell und funktional auf das Niveau der GGG Website / Path of Building bringen.

**Target features:**
- Volle Grafik (Node-Icons, Hintergrundbilder, Gruppen-Grafiken, Keystones mit Rahmen)
- Stats/Modifiers in Hover-Tooltips
- Node-Suche mit Hervorhebung
- Ascendancy Tree Anzeige
- Minimap als Overlay im Tree Viewer
- Node-Editing (Nodes per Klick allocieren/deallocieren)

## Previous: v1.0 (shipped 2026-02-04)

- SkiaSharp rendering (~1300 nodes at 60fps)
- GGG JSON parsing with 7-day cache
- Tree URL decoding from Path of Building
- Zoom (mouse wheel) and pan (drag) navigation
- Hover tooltips with node name and connections
- Allocated node highlighting in gold

**Tech Stack:**
- C# 12, .NET 10, Avalonia 11.3.11
- SkiaSharp for GPU-accelerated rendering
- 5,132 LOC across 51 files

## Requirements

### Validated

- ✓ PoB Import (paste code + pobb.in URLs) — v1.0
- ✓ Gem-Anzeige mit Farben, Levels, Quality, Acquisition — v1.0
- ✓ Item-Anzeige mit Rarity colors, Tooltips — v1.0
- ✓ Skill Tree Viewer (native SkiaSharp) — v1.0
- ✓ Ingame Overlay mit konfigurierbaren Hotkeys — v1.0
- ✓ Build Save/Load (JSON) — v1.0
- ✓ Unified Loadout Selector — v1.0
- ✓ GGG Skill Tree JSON laden und parsen — v1.0
- ✓ Allocated Nodes visuell markieren — v1.0
- ✓ Hover-Info: Node-Name + verbundene Nodes — v1.0
- ✓ Zoom (Mausrad) Navigation — v1.0
- ✓ Pan (Drag) Navigation — v1.0

### Active

- [ ] Volle Grafik (Node-Icons, Gruppen-Grafiken, Keystones mit Rahmen wie GGG Website)
- [ ] Stats/Modifiers in Hover-Tooltips
- [ ] Node-Suche mit Hervorhebung
- [ ] Ascendancy Tree Anzeige
- [ ] Minimap als Overlay im Tree Viewer
- [ ] Node-Editing (Nodes per Klick allocieren/deallocieren)

### Out of Scope (v2+)

(Aktuell leer — alle bisherigen v2+ Items in Active verschoben)

## Constraints

- **Tech Stack**: C# 12, .NET 10, Avalonia 11.3.11
- **Platform**: Windows primär (Overlay/Hotkeys), Tree Viewer ist cross-platform
- **Rendering**: SkiaSharp/Avalonia Canvas
- **Datenquelle**: poe-tool-dev/passive-skill-tree-json (PoE 1 data)

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Nativ Avalonia statt WebView | Volle Kontrolle, keine 200MB Chromium-Dependency | ✓ Good |
| GGG JSON als Datenquelle | Offizielle Daten, gut dokumentiert | ✓ Good |
| poe-tool-dev für PoE 1 | GGG export ist nur PoE 2 | ✓ Good |
| Nur Anzeige in v1 | Fokus auf solides Foundation, Editing später | ✓ Good |
| SkiaSharp für Rendering | GPU-beschleunigt, bereits in Avalonia verfügbar | ✓ Good |
| Background RENDERN für hit testing | Avalonia braucht gerenderten Background für Pointer Events | ✓ Good |
| canvas.Scale() für Zoom | RenderTransform skaliert nur visuell, nicht Koordinaten | ✓ Good |

---
*Last updated: 2026-02-15 after v1.1 milestone start*
