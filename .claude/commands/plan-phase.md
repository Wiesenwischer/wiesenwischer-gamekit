# Nächste Phase ausarbeiten

Dieser Befehl findet die nächste Phase, die noch nicht detailliert ausgearbeitet ist, und erstellt die vollständige Dokumentation dafür.

## Anweisungen

### 1. Implementierungsübersicht lesen

Lies die Datei `docs/implementation/README.md` und identifiziere:
- Welche Phasen existieren
- Welche Phasen bereits Ordner mit detaillierten Schritt-Dokumenten haben
- Welche Phase als nächstes ausgearbeitet werden muss

### 2. Phase-Ordner prüfen

Für jede Phase prüfen ob der Ordner existiert und vollständig ist:
```
docs/implementation/phase-X-*/
├── README.md           # Phase-Übersicht
├── X.1-step-name.md    # Schritt 1
├── X.2-step-name.md    # Schritt 2
└── ...
```

Eine Phase gilt als **nicht ausgearbeitet** wenn:
- Der Ordner leer ist (nur Ordner existiert)
- README.md fehlt
- Schritt-Dateien fehlen

### 3. Nächste Phase identifizieren

Finde die erste Phase (nach Nummer sortiert), die nicht vollständig ausgearbeitet ist.

### 4. Bestehende Spezifikationen lesen (PFLICHT)

Bevor die Phase ausgearbeitet wird:
- Lies die in `docs/implementation/README.md` verlinkten Spezifikationen für diese Phase
- Lies weitere relevante Spezifikationen in `docs/specs/`
- Verstehe die Architektur und bestehenden Code
- Prüfe Abhängigkeiten zu vorherigen Phasen

**WICHTIG:** Die Spezifikationen sind bindend. Die Phase-Dokumentation muss den Spezifikationen entsprechen.

### 5. Phase-Dokumentation erstellen

Erstelle für die Phase:

**README.md** mit:
- Branch-Name
- Abhängigkeiten
- Geschätzte Dauer
- Ziel der Phase
- Tabelle aller Schritte mit Commit-Messages
- Voraussetzungen
- Erwartetes Ergebnis
- Link zur nächsten Phase

**Für jeden Schritt eine eigene Datei** mit:
- Commit-Message
- Ziel des Schritts
- Detaillierte Anweisungen
- Code-Beispiele (falls relevant)
- Verifikations-Checkliste
- Erwartete Dateien nach dem Schritt
- Link zum nächsten Schritt

### 6. Offene Fragen klären

Falls Unklarheiten bestehen:
- Liste die offenen Punkte auf
- Frage den User nach Klärung
- Warte auf Antwort bevor die Dokumentation finalisiert wird

### 7. Ausarbeitungsstatus aktualisieren

In `docs/implementation/README.md`:
- In der Phasen-Übersicht Tabelle: `❌` → `✅` für "Ausgearbeitet"
- Bei der Phase selbst: `**Ausgearbeitet:** ❌ Nein` → `**Ausgearbeitet:** ✅ Ja`
- Schritte mit Links versehen: `- [ ] X.Y Name` → `- [ ] [X.Y Name](phase-X-.../X.Y-name.md)`

### 8. Commit erstellen

Nach Erstellung der Dokumentation:
```bash
git add docs/implementation/
git commit -m "docs: Arbeite Phase X aus - [Phasen-Name]"
```

**WICHTIG:** Kein Claude-Footer in der Commit-Message!

## Beispiel-Ausgabe

```
Phase 2 (Animator Setup) ist die nächste nicht-ausgearbeitete Phase.

Erstelle Dokumentation für:
- docs/implementation/phase-2-animator-setup/README.md
- docs/implementation/phase-2-animator-setup/2.1-avatar-masks.md
- docs/implementation/phase-2-animator-setup/2.2-animator-controller.md
- docs/implementation/phase-2-animator-setup/2.3-locomotion-blend-tree.md
- docs/implementation/phase-2-animator-setup/2.4-airborne-states.md
- docs/implementation/phase-2-animator-setup/2.5-parameter-bridge.md
```
