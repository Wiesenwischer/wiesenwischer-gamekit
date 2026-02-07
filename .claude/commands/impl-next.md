# Nächsten Schritt implementieren

Dieser Befehl ermittelt den aktuellen Fortschritt und implementiert den nächsten Schritt.

## Anweisungen

### 1. Fortschritt ermitteln

Lies `docs/implementation/README.md` und prüfe:
- Welche Schritte sind bereits abgehakt (`- [x]`)?
- Welcher Schritt ist der nächste offene (`- [ ]`)?
- In welcher Phase befinden wir uns?

### 2. Phase-Dokumentation prüfen

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

### 3. Schritt-Dokumentation lesen

Lies die Dokumentation für den nächsten Schritt:
- `docs/implementation/phase-X-*/X.Y-step-name.md`
- Verstehe Ziel, Anforderungen, erwartetes Ergebnis

### 4. Architektur-Review

Bevor implementiert wird:
- Lies relevante bestehende Dateien
- Verstehe Abhängigkeiten und Schnittstellen
- Prüfe ob Voraussetzungen erfüllt sind

### 5. Branch-Management

Prüfe aktuellen Branch:
```bash
git branch --show-current
```

**Falls auf `main`:**
```bash
git checkout main
git pull origin main
git checkout -b feature/phase-X-beschreibung
```

**Falls bereits auf richtigem Feature-Branch:**
- Weiter mit Implementierung

**Falls auf falschem Branch:**
- Informiere User und frage wie vorzugehen ist

### 6. Implementierung durchführen

Führe die Schritte aus der Dokumentation durch:
- Befolge die Anweisungen exakt
- Erstelle/modifiziere Dateien wie beschrieben
- Schreibe Tests für neue Funktionalität

### 7. Tests schreiben

Für jede neue Klasse/Modul:
- Unit Tests erstellen
- Pfad: `Packages/.../Tests/Runtime/` oder `Tests/Editor/`
- Test-Klasse mit `[TestFixture]` Attribut
- Mindestens: Konstruktion, Hauptfunktionalität

### 8. Kompilierung prüfen

```bash
powershell -Command "Get-Content 'C:\Users\marcu\AppData\Local\Unity\Editor\Editor.log' -Tail 100 | Select-String -Pattern 'error|CS\d{4}'"
```

**Bei Fehlern:** Beheben bevor Commit erstellt wird.

### 9. Commit erstellen

```bash
git add <geänderte-dateien>
git commit -m "feat(phase-X): X.Y Beschreibung"
```

**WICHTIG:**
- Kein Claude-Footer (kein "Co-Authored-By")
- Commit-Message exakt wie in Schritt-Dokumentation angegeben

### 10. Dokumentation aktualisieren

In `docs/implementation/README.md`:
- Checkbox abhaken: `- [ ]` → `- [x]`

In `docs/implementation/phase-X-*/README.md`:
- Checkbox abhaken
- Status aktualisieren

```bash
git add docs/implementation/
git commit -m "docs: Markiere Schritt X.Y als abgeschlossen"
```

### 11. Push (optional)

Falls Schritt abgeschlossen:
```bash
git push -u origin feature/phase-X-beschreibung
```

### 12. Nächsten Schritt anzeigen

Informiere den User:
- Was wurde implementiert
- Welcher Schritt ist als nächstes dran
- Ob die Phase abgeschlossen ist (dann PR erstellen)

## Beispiel-Ausgabe

```
Aktueller Fortschritt:
- Phase 1: 4/4 Schritte abgeschlossen ✓
- Phase 2: 0/5 Schritte abgeschlossen
- Aktuelle Phase: 2
- Nächster Schritt: 2.1 Avatar Masks erstellen

Branch: feature/phase-2-animator-setup (neu erstellt)

Implementiere Schritt 2.1...
[Implementierung]

Commit: feat(phase-2): 2.1 Avatar Masks erstellen

Nächster Schritt: 2.2 Animator Controller erstellen
```

## Bei Phase-Abschluss

Wenn der letzte Schritt einer Phase abgeschlossen wurde:

```bash
git push -u origin feature/phase-X-beschreibung
gh pr create --title "feat: Phase X - Beschreibung" --body "..."
```

**PR-Body Format:**
```markdown
## Summary
- Schritt X.1: ...
- Schritt X.2: ...
- ...

## Test plan
- [ ] Test 1
- [ ] Test 2
```

**WICHTIG:** Keine Claude-Attribution im PR!
