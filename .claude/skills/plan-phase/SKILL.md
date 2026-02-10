---
name: plan-phase
description: Nächste nicht-ausgearbeitete Phase finden und detailliert ausarbeiten. Erstellt Phase-Ordner mit README und Schritt-Dateien.
disable-model-invocation: true
---

# Nächste Phase ausarbeiten

Dieser Befehl findet die nächste Phase, die noch nicht detailliert ausgearbeitet ist, und erstellt die vollständige Dokumentation dafür.

## Anweisungen

### 1. Master-Plan lesen

Lies die Datei `docs/implementation/README.md` und identifiziere:
- Alle Epics und ihre Phasen
- Welche Phasen als `✅` (ausgearbeitet) und welche als `❌` (nicht ausgearbeitet) markiert sind
- Welche Phasen als `Offen`, `In Arbeit` oder `Abgeschlossen` markiert sind
- Abhängigkeiten zwischen Phasen (siehe Abhängigkeiten-Diagramm)

### 2. Kandidaten ermitteln

Eine Phase kommt als nächste in Frage wenn:
- Sie als `❌` (nicht ausgearbeitet) markiert ist
- Alle ihre Abhängigkeiten (vorherige Phasen) bereits ausgearbeitet ODER abgeschlossen sind
- Sie nicht bereits in Arbeit ist

**WICHTIG:** Die Epics haben keine feste Reihenfolge. Es können Phasen aus verschiedenen Epics gleichzeitig als Kandidat in Frage kommen.

### 3. User wählen lassen

Falls mehrere Phasen als Kandidat in Frage kommen:
- Zeige dem User die Kandidaten gruppiert nach Epic
- Für jeden Kandidaten: Phase-Nummer, Name, Epic-Zugehörigkeit
- Frage den User welche Phase ausgearbeitet werden soll
- Empfehle eine Phase basierend auf Abhängigkeiten und Vollständigkeit des Epics

Falls nur eine Phase in Frage kommt:
- Informiere den User und fahre direkt fort

### 4. Phase-Ordner prüfen

Für die gewählte Phase prüfen ob bereits Dateien existieren:
```
docs/implementation/phase-X-*/
├── README.md           # Phase-Übersicht
├── X.1-step-name.md    # Schritt 1
├── X.2-step-name.md    # Schritt 2
└── ...
```

Eine Phase gilt als **nicht ausgearbeitet** wenn:
- Der Ordner nicht existiert
- Der Ordner leer ist
- README.md fehlt
- Schritt-Dateien fehlen

### 5. Bestehende Spezifikationen lesen (PFLICHT)

Bevor die Phase ausgearbeitet wird:
- Lies die im Master-Plan verlinkten Spezifikationen für diese Phase und das zugehörige Epic
- Lies die Haupt-Spezifikation des Epics (falls vorhanden)
- Lies weitere relevante Spezifikationen in `docs/specs/`
- Verstehe die Architektur und bestehenden Code
- Prüfe Abhängigkeiten zu vorherigen Phasen

**WICHTIG:** Die Spezifikationen sind bindend. Die Phase-Dokumentation muss den Spezifikationen entsprechen.

### 5b. Impact-Analyse auf aktive Phasen

Prüfe ob die neue Phase Auswirkungen auf **bereits ausgearbeitete oder in Arbeit befindliche Phasen** hat.

**Prüfschritte:**
1. Identifiziere alle Phasen mit Status `In Arbeit` oder `Offen` + `✅ Ausgearbeitet`
2. Lies deren Detail-Dokumentation (README.md + Schritt-Dateien)
3. Prüfe ob die neue Phase:
   - Interfaces/Klassen erweitert, die in einer aktiven Phase definiert werden
   - Neue Anforderungen an bestehende Komponenten stellt
   - Architektur-Entscheidungen beeinflusst, die in einer aktiven Phase getroffen wurden

**Falls Auswirkungen gefunden werden:**

1. **Impact Note in der aktiven Phase hinterlegen**
   - In der README.md der betroffenen Phase einen `## Impact Notes` Abschnitt ergänzen (falls noch nicht vorhanden)
   - Format:
     ```markdown
     ## Impact Notes

     > **Phase Y (Name)** — [Kurzbeschreibung der Auswirkung]
     > Betrifft: [Datei/Interface/Klasse]
     > Aktion: Wird in Phase Y, Schritt Y.1 adressiert
     ```

2. **Adaptierungsschritt in der neuen Phase einplanen**
   - Falls die neue Phase Änderungen an bestehenden Interfaces/Klassen braucht, einen expliziten Schritt am Anfang der Phase einplanen (z.B. "X.1 Interface-Erweiterung aus Phase Z")
   - Dieser Schritt dokumentiert exakt welche Anpassungen nötig sind

3. **User warnen bei kritischen Auswirkungen**
   - Falls eine Auswirkung bereits implementierte Schritte brechen könnte → User explizit warnen
   - Empfehlung geben: Erst aktive Phase abschließen, oder Anpassung jetzt einbauen

**Falls keine Auswirkungen:** Weiter mit Schritt 6.

### 6. Phase-Dokumentation erstellen

Erstelle für die Phase:

**README.md** mit:
- Integration-Branch-Name (Format: `integration/phase-X-beschreibung`)
- Epic-Zugehörigkeit
- Abhängigkeiten (welche Phasen müssen vorher abgeschlossen sein)
- Ziel der Phase
- Tabelle aller Schritte mit Commit-Messages und empfohlenem Feature-Branch-Typ
- Voraussetzungen
- Erwartetes Ergebnis
- Link zur nächsten Phase im selben Epic

**Für jeden Schritt eine eigene Datei** mit:
- Commit-Message
- Empfohlener Branch-Name und -Typ (z.B. `feat/cc-appearance-model`)
- Ziel des Schritts
- Detaillierte Anweisungen
- Code-Beispiele (falls relevant)
- Verifikations-Checkliste
- Erwartete Dateien nach dem Schritt
- Link zum nächsten Schritt

**Hinweis zum Branch-Modell:**
- Die Phase hat einen langlebigen `integration/`-Branch
- Jeder Schritt (oder 2-3 zusammengehörige Schritte) bekommt einen kurzlebigen Feature-Branch
- Feature-Branches gehen per PR in den Integration-Branch
- Am Phase-Ende geht der Integration-Branch per PR in main

### 7. Offene Fragen klären

Falls Unklarheiten bestehen:
- Liste die offenen Punkte auf
- Frage den User nach Klärung
- Warte auf Antwort bevor die Dokumentation finalisiert wird

### 8. Ausarbeitungsstatus aktualisieren

In `docs/implementation/README.md`:
- In der Phasen-Übersicht Tabelle: `❌` → `✅` für "Ausgearbeitet"
- In der Phasen-Übersicht Tabelle: `—` → `[Features](phase-X-.../README.md)` für die Features-Spalte
- Bei der Phase selbst: `**Ausgearbeitet:** ❌ Nein` → `**Ausgearbeitet:** ✅ Ja — [Detail-Dokument](phase-X-.../README.md)`
- Schritte mit Links versehen: `- [ ] X.Y Name` → `- [ ] [X.Y Name](phase-X-.../X.Y-name.md)`

### 9. Commit erstellen

Nach Erstellung der Dokumentation:
```bash
git add docs/implementation/
git commit -m "docs: Arbeite Phase X aus - [Phasen-Name]"
```

**WICHTIG:** Kein Claude-Footer in der Commit-Message!

## Beispiel-Ausgaben

### Beispiel 1: Ohne Auswirkungen

```
Nicht-ausgearbeitete Phasen mit erfüllten Abhängigkeiten:

Epic "Character Creator & Ausrüstung":
  → Phase 10: CC Core Data Model & Catalogs (keine Abhängigkeiten)

Welche Phase soll ausgearbeitet werden?
→ Phase 10

Impact-Analyse: Keine Auswirkungen auf aktive Phasen.

Erstelle Dokumentation für Phase 10: CC Core Data Model & Catalogs
- docs/implementation/phase-10-cc-data-model/README.md
- docs/implementation/phase-10-cc-data-model/10.1-package-structure.md
- ...
```

### Beispiel 2: Mit Auswirkungen auf aktive Phase

```
Ausarbeitung: Phase 4 (Ability System)

Impact-Analyse:
  ⚠ Phase 3 (Animation-Integration, ✅ ausgearbeitet, Status: Offen)
    Auswirkung: IAnimationController braucht neue Methode PlayAbilityAnimation(string)
    Betrifft: IAnimationController.cs (definiert in Phase 3, Schritt 3.1)
    → Adaptierungsschritt 4.1 eingeplant: "IAnimationController um Ability-Methoden erweitern"
    → Impact Note in Phase 3 README hinterlegt

Keine kritischen Konflikte (Phase 3 noch nicht implementiert).

Erstelle Dokumentation für Phase 4: Ability System
- docs/implementation/phase-4-ability-system/README.md
- docs/implementation/phase-4-ability-system/4.1-animation-interface-extension.md
- docs/implementation/phase-4-ability-system/4.2-iability-interface.md
- ...
```
