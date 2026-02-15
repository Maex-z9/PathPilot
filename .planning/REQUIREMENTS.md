# Requirements: PathPilot

**Defined:** 2026-02-15
**Core Value:** Flüssiges Rendering von ~1300 Nodes mit klarer Allocated-Markierung und natürlicher Zoom/Pan-Navigation

## v1.1 Requirements

Requirements for Skill Tree Overhaul milestone. Each maps to roadmap phases.

### Visual Rendering

- [ ] **VIS-01**: User sieht echte PoE Node-Sprites (Normal, Notable, Keystone, Jewel Socket) statt farbiger Punkte
- [ ] **VIS-02**: User sieht Group-Hintergrundbilder hinter zusammengehörigen Node-Gruppen
- [ ] **VIS-03**: Sprite-Qualität wechselt automatisch basierend auf Zoom-Level (4 GGG Zoom-Stufen)
- [ ] **VIS-04**: Allocated Nodes verwenden Active-Sprites, Unallocated verwenden Inactive-Sprites
- [ ] **VIS-05**: Sprite Sheets werden lokal gecacht (wie Gem Icons) für schnelles Laden

### Stat Tooltips

- [ ] **TIP-01**: User sieht beim Hover über einen Node alle Stats/Modifiers (z.B. "+10 to Strength")
- [ ] **TIP-02**: Stats werden mehrzeilig formatiert mit farbiger Hervorhebung (Zahlen, Keywords)

### Node Search

- [ ] **SRC-01**: User kann Nodes nach Name oder Stat-Text suchen
- [ ] **SRC-02**: Suchergebnisse werden im Tree visuell hervorgehoben (goldener Ring/Glow)
- [ ] **SRC-03**: Klick auf Suchergebnis zentriert den Tree auf den gefundenen Node
- [ ] **SRC-04**: Suche unterstützt Fuzzy-Matching (z.B. "fire dmg" findet "Fire Damage")

### Node Editing

- [ ] **EDT-01**: User kann Nodes per Linksklick allocieren und per Rechtsklick deallocieren
- [ ] **EDT-02**: Path Validation verhindert ungültige Allocations (Node muss mit Pfad verbunden sein)
- [ ] **EDT-03**: Import-Modus akzeptiert alle Nodes aus PoB (lenient), Edit-Modus ist strikt
- [ ] **EDT-04**: Undo/Redo für Allocation-Änderungen (Ctrl+Z / Ctrl+Y)
- [ ] **EDT-05**: Shortest Path: Shift+Hover zeigt kürzesten Pfad zu Node, Klick allociert gesamten Pfad
- [ ] **EDT-06**: Punkt-Budget Anzeige (z.B. "95/123 Points Used")
- [ ] **EDT-07**: Tree URL wird nach Editing aktualisiert (TreeUrlEncoder)

### Minimap

- [ ] **MAP-01**: Minimap Overlay im Tree Viewer zeigt komplette Tree-Übersicht
- [ ] **MAP-02**: Aktueller Viewport wird als Rechteck in der Minimap angezeigt
- [ ] **MAP-03**: Klick auf Minimap springt zum angeklickten Bereich im Tree
- [ ] **MAP-04**: Minimap verwendet vereinfachtes Rendering (Dots statt Sprites) für Performance

### Ascendancy

- [ ] **ASC-01**: User sieht Ascendancy-Nodes des gewählten Builds
- [ ] **ASC-02**: Ascendancy wird als separates Overlay/Bereich gerendert (eigener Koordinatenraum)
- [ ] **ASC-03**: Allocated Ascendancy Nodes werden gold hervorgehoben

## v2 Requirements

Deferred to future release. Tracked but not in current roadmap.

### Advanced Editing

- **ADV-01**: Stat Aggregation Sidebar (Live DPS/Defense Totals)
- **ADV-02**: Path Efficiency Highlighting (Stats/Point Rating)
- **ADV-03**: Cluster Jewel Support (Dynamic Node Generation)
- **ADV-04**: Mastery Effect Selection (Popup bei Mastery Nodes)

### Enhanced Navigation

- **NAV-01**: Class/Ascendancy Selector (Startposition ändern)
- **NAV-02**: Node Comparison Mode (2+ Pfade vergleichen)

## Out of Scope

| Feature | Reason |
|---------|--------|
| Full Build Calculation Engine | PoB macht das perfekt, wir fokussieren auf Visualisierung |
| Skill Gem Simulation | Extrem komplex, PoB handles edge cases |
| Item Editor | Nicht tree-related, out of scope |
| Trade Integration | Unrelated zu Tree Planning |
| Animated Node Allocation | Visueller Polish mit minimalem Wert |
| Timeless Jewel Transformation | Sehr nischig, selbst PoB kann das kaum |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| VIS-01 | — | Pending |
| VIS-02 | — | Pending |
| VIS-03 | — | Pending |
| VIS-04 | — | Pending |
| VIS-05 | — | Pending |
| TIP-01 | — | Pending |
| TIP-02 | — | Pending |
| SRC-01 | — | Pending |
| SRC-02 | — | Pending |
| SRC-03 | — | Pending |
| SRC-04 | — | Pending |
| EDT-01 | — | Pending |
| EDT-02 | — | Pending |
| EDT-03 | — | Pending |
| EDT-04 | — | Pending |
| EDT-05 | — | Pending |
| EDT-06 | — | Pending |
| EDT-07 | — | Pending |
| MAP-01 | — | Pending |
| MAP-02 | — | Pending |
| MAP-03 | — | Pending |
| MAP-04 | — | Pending |
| ASC-01 | — | Pending |
| ASC-02 | — | Pending |
| ASC-03 | — | Pending |

**Coverage:**
- v1.1 requirements: 25 total
- Mapped to phases: 0
- Unmapped: 25 ⚠️

---
*Requirements defined: 2026-02-15*
*Last updated: 2026-02-15 after initial definition*
