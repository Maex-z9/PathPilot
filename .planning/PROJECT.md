# PathPilot — Native Skill Tree Viewer

## What This Is

PathPilot ist eine Desktop-App für Path of Exile Build-Guides. Sie importiert Builds aus Path of Building, zeigt Gems, Items und Skill Trees an, und bietet ein Ingame-Overlay. Dieses Milestone ersetzt den aktuellen WebView-basierten Skill Tree Viewer durch einen nativen Avalonia-Renderer mit voller Kontrolle über Darstellung und Interaktion.

## Core Value

Der Skill Tree muss flüssig rendern (~1300 Nodes), allocated Nodes klar erkennbar markieren, und sich natürlich per Zoom/Pan navigieren lassen.

## Requirements

### Validated

- ✓ PoB Import (paste code + pobb.in URLs) — existing
- ✓ Gem-Anzeige mit Farben, Levels, Quality, Acquisition — existing
- ✓ Item-Anzeige mit Rarity colors, Tooltips — existing
- ✓ Skill Tree Viewer (WebView, externe URL) — existing (wird ersetzt)
- ✓ Ingame Overlay mit konfigurierbaren Hotkeys — existing
- ✓ Build Save/Load (JSON) — existing
- ✓ Unified Loadout Selector — existing
- ✓ Quest Tracker (Model + Service) — existing (in progress)

### Active

- [ ] GGG Skill Tree JSON laden und parsen
- [ ] Skill Tree nativ in Avalonia rendern (~1300 Nodes)
- [ ] Allocated Nodes aus Build visuell markieren
- [ ] Hover-Info: Node-Name + verbundene Nodes
- [ ] Zoom (Mausrad) Navigation
- [ ] Pan (Drag) Navigation

### Out of Scope

- Node-Editing (klicken zum allocieren/deallocieren) — v2 Feature
- Stats/Modifiers im Hover — v2 Feature
- Minimap für schnelle Navigation — v2 Feature
- Node-Suche — v2 Feature
- Ascendancy Tree Viewer — v2 Feature

## Context

**Aktueller Stand:**
- TreeViewerWindow nutzt WebViewControl-Avalonia mit Chromium
- Lädt externe URL (pathofexile.com passive tree)
- Keine Kontrolle über Darstellung oder Interaktion
- WebView-Abhängigkeit ist ~200MB

**GGG Skill Tree Daten:**
- JSON verfügbar unter pathofexile.com/passive-skill-tree
- Enthält: Node-Positionen, Verbindungen, Namen, Stats
- Muss für jede League-Version aktualisiert werden
- Build.TreeSets enthält bereits allocated Node IDs aus PoB Import

**Performance-Anforderungen:**
- ~1300 Nodes + ~1500 Verbindungen
- Muss bei 60fps rendern während Zoom/Pan
- SkiaSharp (via Avalonia) bietet GPU-beschleunigtes Rendering

## Constraints

- **Tech Stack**: C# 12, .NET 10, Avalonia 11.3.11 — bestehende Architektur
- **Platform**: Windows primär (Overlay/Hotkeys), aber Tree Viewer soll cross-platform sein
- **Rendering**: SkiaSharp/Avalonia Canvas — kein WebView
- **Datenquelle**: Offizielle GGG JSON — keine Third-Party APIs

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Nativ Avalonia statt WebView | Volle Kontrolle, keine 200MB Chromium-Dependency | — Pending |
| GGG JSON als Datenquelle | Offizielle Daten, gut dokumentiert | — Pending |
| Nur Anzeige in v1 | Fokus auf solides Foundation, Editing später | — Pending |
| SkiaSharp für Rendering | GPU-beschleunigt, bereits in Avalonia verfügbar | — Pending |

---
*Last updated: 2026-02-04 after initialization*
