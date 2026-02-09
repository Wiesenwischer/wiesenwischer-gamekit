# Phase wechseln

Dieser Befehl zeigt alle aktiven Phasen und wechselt zu einer anderen Phase.

## Anweisungen

### 1. Aktuelle Situation ermitteln

Führe parallel aus:

```bash
git branch --show-current
```

```bash
git branch --list "integration/*"
```

```bash
git branch -r --list "origin/integration/*"
```

```bash
git status --porcelain
```

Daraus ermitteln:
- **Aktueller Branch** (Feature-Branch, Integration-Branch oder main)
- **Aktuelle Phase** (aus Branch-Name ableiten)
- **Alle lokalen Integration-Branches**
- **Alle remote Integration-Branches** (die lokal noch nicht existieren)
- **Uncommitted Changes** (staged, unstaged, untracked)

### 2. Aktive Phasen anzeigen

Lies `docs/implementation/README.md` und zeige eine Übersicht:

```
Aktuelle Phase: Phase 3 (Animation-Integration)
Branch: feat/state-animation-triggers → integration/phase-3-animation-integration

Aktive Phasen:
  ● Phase 3: Animation-Integration         ← aktuell
    Branch: integration/phase-3-animation-integration
    Fortschritt: 2/4 Schritte

  ○ Phase 10: CC Core Data Model & Catalogs
    Branch: integration/phase-10-cc-data-model
    Fortschritt: 3/6 Schritte

  ○ main (kein Phase-Branch)
```

Markiere die aktuelle Phase mit `●`, andere mit `○`.

### 3. Uncommitted Changes behandeln

Falls uncommitted Changes vorhanden sind, frage den User:

- **Stash** → `git stash push -m "WIP: <aktueller-branch>"`
- **Commit** → Normaler Commit-Flow
- **Abbrechen** → Wechsel nicht durchführen

**WICHTIG:** Niemals uncommitted Changes stillschweigend verwerfen!

### 4. Ziel-Phase wählen

Falls nur eine andere Phase aktiv ist → direkt anbieten.
Falls mehrere → User wählen lassen.
Option `main` immer anbieten.

### 5. Branch wechseln

```bash
git checkout integration/phase-X-beschreibung
```

Falls der Branch nur remote existiert:
```bash
git checkout -b integration/phase-X-beschreibung origin/integration/phase-X-beschreibung
```

Nach dem Wechsel:
```bash
git pull origin integration/phase-X-beschreibung
```

### 6. Fortschritt der Ziel-Phase anzeigen

Nach dem Wechsel:
- Phase-Name und Epic
- Fortschritt (X/Y Schritte abgeschlossen)
- Nächster offener Schritt
- Ob ein Feature-Branch für diese Phase existiert, der fortgesetzt werden kann

Prüfe ob ein Feature-Branch für diese Phase existiert:
```bash
git branch --list | grep -v "integration/"
```

Falls ja:
```
Offener Feature-Branch gefunden: feat/cc-equipment-types
Soll dieser fortgesetzt werden? (Dann: git checkout feat/cc-equipment-types)
```

## Beispiel-Ausgaben

### Beispiel 1: Einfacher Wechsel

```
/switch-phase

Aktuelle Phase: Phase 3 (Animation-Integration)
Branch: feat/state-animation-triggers

Aktive Phasen:
  ● Phase 3: Animation-Integration (2/4)    ← aktuell
  ○ Phase 10: CC Core Data Model (3/6)

Zu welcher Phase wechseln?
  → Phase 10: CC Core Data Model
  → main

User: Phase 10

Uncommitted Changes gefunden (2 Dateien geändert).
  → Stash (empfohlen)
  → Commit
  → Abbrechen

User: Stash
✓ git stash push -m "WIP: feat/state-animation-triggers"
✓ git checkout integration/phase-10-cc-data-model
✓ git pull

Phase 10: CC Core Data Model & Catalogs
Epic: Character Creator & Ausrüstung
Fortschritt: 3/6 Schritte abgeschlossen
Nächster Schritt: 10.4 ScriptableObject-Kataloge

Offener Feature-Branch: feat/cc-catalogs
→ Fortsetzen mit: git checkout feat/cc-catalogs
```

### Beispiel 2: Keine anderen Phasen aktiv

```
/switch-phase

Aktuelle Phase: Phase 3 (Animation-Integration)
Branch: integration/phase-3-animation-integration

Keine anderen Integration-Branches gefunden.
Mögliche Aktionen:
  → Zu main wechseln
  → Neue Phase starten (siehe /impl-next)
```

### Beispiel 3: Von main zu Phase

```
/switch-phase

Aktueller Branch: main

Aktive Phasen:
  ○ Phase 3: Animation-Integration (2/4)
  ○ Phase 10: CC Core Data Model (3/6)

Zu welcher Phase wechseln?

User: Phase 3
✓ git checkout integration/phase-3-animation-integration
✓ git pull

Phase 3: Animation-Integration
Fortschritt: 2/4 Schritte
Nächster Schritt: 3.3 Player Prefab zusammenbauen
```

## Hinweise

- Der Befehl wechselt immer zum **Integration-Branch** der Phase, nicht zu einem Feature-Branch
- Feature-Branches werden nach dem Wechsel als Option angezeigt, falls vorhanden
- `git stash` Einträge können mit `git stash list` angezeigt und mit `git stash pop` wiederhergestellt werden
- Falls ein Stash für den Ziel-Branch existiert, darauf hinweisen
