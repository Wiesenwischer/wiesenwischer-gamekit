# Nächsten Schritt implementieren

Dieser Befehl ermittelt den aktuellen Fortschritt und implementiert den nächsten Schritt.

## Anweisungen

### 1. Fortschritt ermitteln

Lies `docs/implementation/README.md` und erstelle eine Übersicht:
- Für jedes Epic: Wie viele Phasen sind offen / in Arbeit / abgeschlossen?
- Für jede Phase mit Status "In Arbeit" oder erste offene Phase: Wie viele Schritte sind abgehakt (`- [x]`) vs. offen (`- [ ]`)?
- Welche Phasen sind aktuell auf einem Integration-Branch in Arbeit?

Prüfe zusätzlich den aktuellen Git-Branch:
```bash
git branch --show-current
```

### 2. Aktive Phase bestimmen

**Fall A: Auf einem Feature-Branch (z.B. `feat/cc-appearance-model`)**
- Prüfe welcher Integration-Branch die Basis ist
- Die Phase ergibt sich aus dem Integration-Branch
- Weiter mit Schritt 3

**Fall B: Auf einem Integration-Branch (z.B. `integration/phase-10-cc-data-model`)**
- Die Phase ergibt sich aus dem Branch-Namen
- Es wird ein neuer Feature-Branch für den nächsten Schritt erstellt
- Weiter mit Schritt 3

**Fall C: Auf `main`, keine Phase in Arbeit**
- Zeige dem User die Übersicht aller Epics mit Fortschritt
- Zeige Phasen die als nächstes implementiert werden können (ausgearbeitet + Abhängigkeiten erfüllt)
- Frage den User welche Phase gestartet werden soll
- Erstelle den Integration-Branch

**Fall D: Auf `main`, aber Integration-Branch(es) existieren**
- Prüfe existierende Integration-Branches:
  ```bash
  git branch -r --list "origin/integration/*"
  ```
- Zeige welche Phasen in Arbeit sind
- Frage ob der User eine davon fortsetzen oder eine neue starten möchte

### 3. Phase-Dokumentation prüfen

Prüfe ob die aktuelle Phase vollständig ausgearbeitet ist:
```
docs/implementation/phase-X-*/
├── README.md
├── X.1-...md
├── X.2-...md
└── ...
```

**STOPP** falls Phase nicht ausgearbeitet ist:
- Informiere den User
- Empfehle `/plan-phase` auszuführen
- Fahre NICHT mit Implementierung fort

### 4. Spezifikationen lesen (PFLICHT)

Lies ZUERST die verlinkten Spezifikationen:
- Spezifikationen des Epics (im Master-Plan beim Epic-Abschnitt verlinkt)
- Haupt-Spezifikation des Epics (falls vorhanden, z.B. Character Creator Spec)
- Spezifikationen der Phase (in der Phase-README verlinkt)
- Verstehe die Architektur-Entscheidungen und Konzepte

**WICHTIG:** Die Spezifikationen sind bindend. Implementierungen müssen den Spezifikationen entsprechen.

### 5. Schritt-Dokumentation lesen

Lies die Dokumentation für den nächsten offenen Schritt:
- `docs/implementation/phase-X-*/X.Y-step-name.md`
- Verstehe Ziel, Anforderungen, erwartetes Ergebnis

### 5b. Impact Notes prüfen

Prüfe ob die Phase-README einen `## Impact Notes` Abschnitt enthält.

**Falls Impact Notes vorhanden:**
1. Lies alle Impact Notes vollständig
2. Prüfe ob der **aktuelle Schritt** von einer Impact Note betroffen ist
3. Falls ja → User warnen:
   ```
   ⚠ Impact Note gefunden:
     Phase Y (Name) benötigt Änderungen an [Datei/Interface].
     Wird in Phase Y, Schritt Y.Z adressiert.

     Optionen:
     → Schritt normal implementieren (Änderung kommt in Phase Y)
     → Änderung jetzt vorziehen (Interface schon jetzt erweitern)
     → Schritt überspringen und erst Impact klären
   ```
4. User entscheiden lassen, dann entsprechend fortfahren

**Falls keine Impact Notes oder Schritt nicht betroffen:** Weiter mit Schritt 6.

### 6. Architektur-Review

Bevor implementiert wird:
- Lies relevante bestehende Dateien
- Verstehe Abhängigkeiten und Schnittstellen
- Prüfe ob Voraussetzungen erfüllt sind

### 7. Branch-Management

**Neuen Feature-Branch vom Integration-Branch erstellen:**
```bash
git checkout integration/phase-X-beschreibung
git pull origin integration/phase-X-beschreibung
git checkout -b <type>/fachliche-beschreibung
```

Branch-Typ passend zum Inhalt wählen:
- `feat/` — Neue Funktionalität
- `fix/` — Bugfix
- `refactor/` — Code-Umbau
- `test/` — Tests
- `docs/` — Dokumentation
- `chore/` — Setup, Config

**Falls bereits auf richtigem Feature-Branch:**
- Weiter mit Implementierung

### 8. Implementierung durchführen

Führe die Schritte aus der Dokumentation durch:
- Befolge die Anweisungen exakt
- Erstelle/modifiziere Dateien wie beschrieben
- Schreibe Tests für neue Funktionalität

### 9. Tests schreiben

Für jede neue Klasse/Modul:
- Unit Tests erstellen
- Pfad: `Packages/.../Tests/Runtime/` oder `Tests/Editor/`
- Test-Klasse mit `[TestFixture]` Attribut
- Mindestens: Konstruktion, Hauptfunktionalität

### 10. Kompilierung prüfen

```bash
powershell -Command "Get-Content 'C:\Users\marcu\AppData\Local\Unity\Editor\Editor.log' -Tail 100 | Select-String -Pattern 'error|CS\d{4}'"
```

**Bei Fehlern:** Beheben bevor Commit erstellt wird.

### 11. Commit erstellen

```bash
git add <geänderte-dateien>
git commit -m "feat(phase-X): X.Y Beschreibung"
```

**WICHTIG:**
- Kein Claude-Footer (kein "Co-Authored-By")
- Commit-Message exakt wie in Schritt-Dokumentation angegeben

### 12. PR in Integration-Branch erstellen

```bash
git push -u origin <type>/fachliche-beschreibung
gh pr create --base integration/phase-X-beschreibung --title "<type>: Beschreibung" --body "..."
```

**PR-Titel** muss Conventional Commit Prefix haben (für Auto-Labeler).

### 13. Dokumentation aktualisieren

In `docs/implementation/README.md`:
- Checkbox abhaken: `- [ ]` → `- [x]`

In `docs/implementation/phase-X-*/README.md`:
- Checkbox abhaken
- Status aktualisieren

```bash
git add docs/implementation/
git commit -m "docs: Markiere Schritt X.Y als abgeschlossen"
```

### 14. Nächsten Schritt anzeigen

Informiere den User:
- Epic und Phase in der wir arbeiten
- Was wurde implementiert
- Welcher Schritt ist als nächstes dran
- Fortschritt der Phase (z.B. "3/6 Schritte abgeschlossen")
- Ob die Phase abgeschlossen ist (dann Phase-PR → main erstellen)

## Beispiel-Ausgabe

```
Aktueller Fortschritt:

Epic "Lebendige Charaktere — Animation Pipeline":
  Phase 1: 4/4 ✅ | Phase 2: 0/5 | Phase 3: nicht ausgearbeitet

Epic "Character Creator & Ausrüstung":
  Phase 10: 2/6 (in Arbeit) | Phase 11–16: nicht ausgearbeitet

Aktueller Branch: integration/phase-10-cc-data-model
Nächster Schritt: 10.3 EquipmentSlot Enum und Grundtypen

Erstelle Feature-Branch: feat/cc-equipment-types
Basis: integration/phase-10-cc-data-model

Implementiere Schritt 10.3...
[Implementierung]

Commit: feat(phase-10): 10.3 EquipmentSlot Enum und Grundtypen
PR: feat/cc-equipment-types → integration/phase-10-cc-data-model

Fortschritt Phase 10: 3/6 Schritte abgeschlossen
Nächster Schritt: 10.4 ScriptableObject-Kataloge
```

## Bei Phase-Abschluss

Wenn der letzte Schritt einer Phase abgeschlossen wurde:

1. **Letzte Feature-PR mergen** in Integration-Branch
2. **Phase-PR erstellen** (Integration → main):
```bash
gh pr create --base main --title "feat: Phase X - Beschreibung" --body "..."
```

**PR-Body Format:**
```markdown
## Summary
**Epic:** [Epic-Name]

- Schritt X.1: ...
- Schritt X.2: ...
- ...

## Test plan
- [ ] Test 1
- [ ] Test 2
```

3. **Nach Merge: Integration-Branch aufräumen**
```bash
git checkout main && git pull origin main
git branch -d integration/phase-X-beschreibung
git push origin --delete integration/phase-X-beschreibung
```

4. **Master-Plan aktualisieren:** Phase-Status → `Abgeschlossen`

**WICHTIG:** Keine Claude-Attribution in PRs!
