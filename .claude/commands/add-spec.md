# Neue Spec analysieren und in den Plan aufnehmen

Dieser Befehl nimmt eine Spezifikations-Datei entgegen, analysiert sie, teilt sie in Phasen auf und integriert sie in den Master-Implementierungsplan.

## Eingabe

Der User gibt optional den Pfad zu einer (oder mehreren) Markdown-Spezifikation(en) an, z.B.:
```
/add-spec docs/specs/MeineNeueSpec.md
/add-spec docs/specs/Spec_A.md docs/specs/Spec_B.md
```

Falls **kein Pfad** angegeben wird → automatische Erkennung (siehe Schritt 0).

## Anweisungen

### 0. Automatische Spec-Erkennung (falls kein Pfad angegeben)

Wenn der User keinen Pfad angibt, suche automatisch nach neuen Markdown-Dateien im Repo:

```bash
git status --porcelain -- "docs/specs/*.md" "docs/specs/**/*.md"
```

Das findet Dateien mit Status `??` (untracked), `A` (staged/added) oder `M` (modified).

**Fall A: Keine neuen Specs gefunden**
- Informiere den User: "Keine neuen Spec-Dateien gefunden."
- Frage nach dem Pfad zur Spec-Datei.

**Fall B: Genau eine neue Spec gefunden**
- Zeige dem User die gefundene Datei und fahre mit Schritt 1 fort.

**Fall C: Mehrere neue Specs gefunden**
- Liste alle gefundenen Specs auf, z.B.:
  ```
  Neue Spec-Dateien gefunden:
    1. docs/specs/WeatherSystem_Spec.md
    2. docs/specs/WeatherSystem_Effects.md
    3. docs/specs/CraftingSystem_Spec.md
  ```
- Frage den User, welche Spec(s) verarbeitet werden sollen:
  - **Einzelne Spec** → Normaler Ablauf (Schritt 1)
  - **Mehrere Specs für ein Epic** → Konsolidierung (Schritt 0b)
  - **Alle einzeln** → Nacheinander als separate Epics verarbeiten

### 0b. Mehrere Specs konsolidieren

Wenn der User mehrere Specs auswählt, die zu **einem** Epic gehören:

1. **Alle gewählten Specs vollständig lesen**
2. **Überschneidungen und Widersprüche identifizieren**
   - Gleiche Konzepte mit unterschiedlichen Details → zusammenführen
   - Widersprüchliche Definitionen → dem User zur Klärung vorlegen
3. **Konsolidierte Zusammenfassung erstellen**
   - Alle Kernkomponenten aus allen Specs zusammentragen
   - Einheitliches Datenmodell ableiten
   - Gemeinsame Abhängigkeiten identifizieren
4. **Konsolidierte Spec-Datei erstellen** unter `docs/specs/`:
   - Dateiname: beschreibend, z.B. `WeatherSystem_Specification.md`
   - Inhalt: Eigenständige, vollständige Spezifikation (keine Verweise auf die Originale nötig)
   - Die konsolidierte Spec muss ohne die Originale verständlich sein
   - **WICHTIG:** Alle Punkte aus allen Quell-Specs müssen im gleichen Detaillierungsgrad enthalten sein. Nichts darf zusammengefasst, gekürzt oder abstrahiert werden. Die konsolidierte Spec ist eine Zusammenführung, keine Zusammenfassung.
5. **User fragen ob Original-Specs gelöscht werden sollen**
   - Zeige die Quell-Dateien auf und frage: "Sollen die Original-Specs gelöscht werden?"
   - **Ja** → Dateien löschen (die konsolidierte Spec enthält alles)
   - **Nein** → Original-Specs bleiben erhalten
6. **Weiter mit Schritt 1** unter Verwendung der konsolidierten Spec

### 1. Spec-Datei(en) lesen und analysieren

Lies die angegebene(n) Spec-Datei(en) vollständig und identifiziere:
- **Titel/Name** des Features/Systems
- **Scope**: Was wird gebaut? Welche Packages/Ordner sind betroffen?
- **Kernkomponenten**: Welche Klassen, Systeme, Services werden benötigt?
- **Abhängigkeiten**: Braucht dieses Feature andere Epics/Phasen?
- **Komplexität**: Wie viele Phasen sind sinnvoll?
- **Implementierungs-Reihenfolge**: Was muss zuerst gebaut werden?

### 2. Master-Plan lesen

Lies `docs/implementation/README.md` und identifiziere:
- Welche Epics existieren bereits (beschreibende Titel, **keine Buchstaben-Prefixe**)
- Welche Phasen-Nummern sind bereits vergeben
- Die nächste freie Phasen-Nummer

### 3. Phasen-Aufteilung vorschlagen

Teile die Spec in sinnvolle Phasen auf. Dabei gelten folgende Regeln:

**Granularität:**
- Eine Phase = ein zusammenhängendes, abgeschlossenes Teilsystem
- Jede Phase soll 3–7 Schritte haben
- Jede Phase soll für sich kompilierbar und testbar sein
- Datenmodell/Core immer als erste Phase
- UI/Integration immer als letzte Phase(n)

**Reihenfolge:**
1. Package-Struktur + Datenmodell
2. Core-Logik / Services
3. Subsysteme (einzeln, nach Abhängigkeit sortiert)
4. UI / Scene
5. Persistenz / Integration / Polish

**Für jede Phase definieren:**
- Phasen-Nummer (fortlaufend nach letzter vergebener Nummer)
- Name (kurz, beschreibend)
- Integration-Branch-Name (Format: `integration/phase-X-beschreibung`)
- Ziel (1-2 Sätze)
- Vorläufige Schritte (als Checkbox-Liste)
- Referenz auf relevante Spec-Kapitel

### 4. Vorschlag dem User präsentieren

Zeige dem User:
- Den vorgeschlagenen Epic-Namen (beschreibend, **kein Buchstaben-Prefix**)
- Die Anzahl der Phasen
- Für jede Phase: Nummer, Name, Integration-Branch, Ziel, vorläufige Schritte
- Abhängigkeiten zu bestehenden Epics/Phasen
- Frage ob Anpassungen gewünscht sind

**WICHTIG:** Nicht direkt in den Plan schreiben! Erst dem User zeigen und auf Bestätigung warten.

### 5. Nach Bestätigung: Plan aktualisieren

Aktualisiere `docs/implementation/README.md`:

**Epic-Übersicht Tabelle:**
- Neue Zeile mit Epic-Name (beschreibend), Phasen-Range, Status

**Phasen-Übersicht Tabelle:**
- Neue Zeilen für jede Phase mit allen Spalten:
  ```
  | Phase | Epic | Name | Features | Ausgearbeitet | Status |
  | X     | Epic | Name | —        | ❌            | Offen  |
  ```
- Alle neuen Phasen: Features = `—`, Ausgearbeitet = `❌`, Status = `Offen`

**Abhängigkeiten-Diagramm:**
- Neues Epic mit internen Abhängigkeiten ergänzen
- Kreuz-Abhängigkeiten zu bestehenden Epics/Phasen eintragen

**Neuer Epic-Abschnitt:**
- Epic-Überschrift (H1) mit beschreibendem Titel und kurzer Beschreibung
- Verweis auf Haupt-Spezifikation
- Package-Liste (falls relevant)
- Für jede Phase ein Abschnitt (H3) mit:
  - `**Branch:** \`integration/phase-X-beschreibung\``
  - `**Ausgearbeitet:** ❌ Nein`
  - Ziel-Beschreibung
  - `**Schritte (vorläufig):**` als Checkbox-Liste
  - Spec-Referenz

### 6. Zusammenfassung

Zeige dem User:
- Was hinzugefügt wurde
- Nächster empfohlener Schritt (`/plan-phase` für Detail-Ausarbeitung)
- Welche Phase als erstes dran wäre

### 7. KEINEN Commit erstellen

Die Änderungen werden NICHT automatisch committed.
Der User entscheidet selbst, wann und wie committed wird.

## Beispiel-Ausgaben

### Beispiel 1: Einzelne Spec (mit Pfad)

```
/add-spec docs/specs/CraftingSystem_Spec.md

Spec analysiert: "Crafting System Specification"

Vorschlag für neues Epic:

Crafting & Berufe (3 Phasen)
├── Phase 17: Crafting Core Data Model
│   Branch: integration/phase-17-crafting-data-model
│   Ziel: Rezepte, Materialien, Stationen als Datenmodell
│   Schritte: Package-Struktur, RecipeDefinition, MaterialType, CraftingStation, Tests
│
├── Phase 18: Crafting Pipeline & Logic
│   ...
└── ...

Soll ich das so in den Plan aufnehmen? (Anpassungen möglich)
```

### Beispiel 2: Automatische Erkennung (ohne Pfad)

```
/add-spec

Neue Spec-Dateien gefunden:
  1. docs/specs/WeatherSystem_Spec.md          (untracked)
  2. docs/specs/WeatherSystem_Effects.md        (untracked)
  3. docs/specs/CraftingSystem_Spec.md          (untracked)

Welche Specs sollen verarbeitet werden?
  → Einzelne Spec auswählen (1, 2 oder 3)
  → Mehrere für ein Epic konsolidieren (z.B. 1+2)
  → Alle einzeln nacheinander
```

### Beispiel 3: Konsolidierung mehrerer Specs

```
User wählt: 1+2 (konsolidieren)

Lese 2 Specs...
  ✓ WeatherSystem_Spec.md (Grundsystem, Wetterzyklus, Biome)
  ✓ WeatherSystem_Effects.md (VFX, Audio, Gameplay-Effekte)

Überschneidungen:
  - Beide definieren WeatherType Enum → zusammengeführt (Spec hat 6, Effects hat 8 → 8 übernommen)
  - Keine Widersprüche gefunden

Konsolidierte Spec erstellt: docs/specs/WeatherSystem_Specification.md

Original-Specs löschen?
  - docs/specs/WeatherSystem_Spec.md
  - docs/specs/WeatherSystem_Effects.md
User: Ja → gelöscht

Vorschlag für neues Epic:

Dynamisches Wetter & Umgebungseffekte (4 Phasen)
├── Phase 17: Weather Core Data Model
│   Branch: integration/phase-17-weather-data-model
│   Quellen: WeatherSystem_Spec.md Kap. 2-4, WeatherSystem_Effects.md Kap. 1
│   ...
└── ...

Soll ich das so in den Plan aufnehmen? (Anpassungen möglich)
```

## Hinweise

- Spec-Dateien liegen typischerweise unter `docs/specs/`
- Falls die Spec bereits im Plan referenziert wird, darauf hinweisen
- Falls die Spec zu klein ist für mehrere Phasen (< 3 Klassen), als einzelne Phase aufnehmen
- Falls die Spec zu vage ist, offene Fragen sammeln und dem User stellen
- Epics haben **keine Buchstaben-Prefixe** (A, B, C) — nur beschreibende Titel
- Die Reihenfolge zwischen Epics ist **nicht festgelegt** und ergibt sich aus Abhängigkeiten
- Bei Konsolidierung: Die konsolidierte Spec ist eigenständig (keine Verweise auf Originale), daher können Originale nach Rückfrage gelöscht werden
- `git status --porcelain` erkennt sowohl untracked (`??`) als auch staged (`A`) Dateien
