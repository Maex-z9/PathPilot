# Requirements: PathPilot Native Skill Tree Viewer

**Defined:** 2026-02-04
**Core Value:** Flüssiges Rendering von ~1300 Nodes mit klarer Allocated-Markierung und natürlicher Zoom/Pan-Navigation

## v1 Requirements

### Data Loading

- [x] **DATA-01**: App lädt GGG Skill Tree JSON beim Start
- [x] **DATA-02**: Skill Tree Daten werden geparst (Nodes, Verbindungen, Positionen)
- [x] **DATA-03**: Allocated Node IDs aus Build werden mit Tree-Daten verknüpft

### Rendering

- [ ] **REND-01**: Skill Tree wird nativ in Avalonia Canvas gerendert
- [ ] **REND-02**: Alle ~1300 Nodes werden mit korrekten Positionen angezeigt
- [ ] **REND-03**: Verbindungen zwischen Nodes werden gezeichnet
- [ ] **REND-04**: Allocated Nodes sind visuell unterscheidbar (andere Farbe/Stil)

### Navigation

- [ ] **NAV-01**: Benutzer kann per Mausrad zoomen
- [ ] **NAV-02**: Benutzer kann per Drag die Ansicht verschieben (Pan)
- [ ] **NAV-03**: Tree startet zentriert auf allocated Nodes oder Startpunkt

### Interaction

- [ ] **INT-01**: Hover über Node zeigt Tooltip mit Node-Name
- [ ] **INT-02**: Hover-Tooltip zeigt verbundene Nodes

## v2 Requirements

### Editing
- **EDIT-01**: Benutzer kann Nodes per Klick allocieren/deallocieren
- **EDIT-02**: Pfad-Berechnung zu angeklicktem Node

### Enhanced Display
- **DISP-01**: Stats/Modifiers im Hover-Tooltip
- **DISP-02**: Minimap für schnelle Navigation
- **DISP-03**: Node-Suche nach Name

### Ascendancy
- **ASC-01**: Ascendancy Tree Anzeige
- **ASC-02**: Ascendancy Node Details

## Out of Scope

| Feature | Reason |
|---------|--------|
| Node-Editing | Komplexität — Focus auf solides Display-Foundation in v1 |
| Stats im Hover | Benötigt zusätzliche Daten-Verarbeitung — v2 |
| Minimap | Nice-to-have, nicht kritisch für v1 |
| Suche | Nice-to-have, nicht kritisch für v1 |
| Ascendancy Tree | Separater Datenbereich — eigenes Feature |
| Jewel Socket Rendering | Komplexes Feature — v2+ |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| DATA-01 | Phase 1 | Complete |
| DATA-02 | Phase 1 | Complete |
| DATA-03 | Phase 1 | Complete |
| REND-01 | Phase 2 | Pending |
| REND-02 | Phase 2 | Pending |
| REND-03 | Phase 2 | Pending |
| REND-04 | Phase 2 | Pending |
| NAV-01 | Phase 3 | Pending |
| NAV-02 | Phase 3 | Pending |
| NAV-03 | Phase 3 | Pending |
| INT-01 | Phase 4 | Pending |
| INT-02 | Phase 4 | Pending |

**Coverage:**
- v1 requirements: 12 total
- Mapped to phases: 12
- Unmapped: 0 ✓

---
*Requirements defined: 2026-02-04*
*Last updated: 2026-02-04 after Phase 1 completion*
