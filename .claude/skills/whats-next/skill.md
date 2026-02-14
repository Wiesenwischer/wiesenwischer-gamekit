---
name: whats-next
description: Zeigt den aktuellen Fortschritt und welcher Schritt als nächstes zur Implementierung ansteht. Keine Implementierung, nur Übersicht.
disable-model-invocation: true
---

# Was steht als nächstes an?

Dieser Befehl ermittelt den aktuellen Fortschritt und zeigt, was als nächstes implementiert werden kann. **Es wird NICHT implementiert** — nur Analyse und Ausgabe.

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

Prüfe existierende Integration-Branches:
```bash
git branch -r --list "origin/integration/*"
```

### 2. Aktive Phase bestimmen

**Fall A: Auf einem Feature-Branch (z.B. `feat/cc-appearance-model`)**
- Prüfe welcher Integration-Branch die Basis ist
- Die Phase ergibt sich aus dem Integration-Branch

**Fall B: Auf einem Integration-Branch (z.B. `integration/phase-10-cc-data-model`)**
- Die Phase ergibt sich aus dem Branch-Namen

**Fall C: Auf `main`, keine Phase in Arbeit**
- Zeige Phasen die als nächstes implementiert werden können (ausgearbeitet + Abhängigkeiten erfüllt)

**Fall D: Auf `main`, aber Integration-Branch(es) existieren**
- Zeige welche Phasen in Arbeit sind und welche Schritte offen sind

### 3. Phase-Dokumentation prüfen

Für die aktive Phase bzw. die nächsten Kandidaten:
- Prüfe ob die Phase vollständig ausgearbeitet ist (README.md + Schritt-Dateien vorhanden)
- Lies die Phase-README und zähle offene vs. abgehakte Schritte

### 4. Nächsten Schritt identifizieren

Für Phasen in Arbeit:
- Finde den ersten offenen Schritt (`- [ ]`)
- Lies die Schritt-Dokumentation und zeige eine kurze Zusammenfassung (Ziel, Commit-Message)

### 5. Ausgabe

Zeige dem User eine strukturierte Übersicht:

```
📊 Aktueller Fortschritt:

Epic "Name":
  Phase X: Y/Z ✅ Abgeschlossen
  Phase X: Y/Z (in Arbeit) ← aktuell
  Phase X: nicht ausgearbeitet

🌿 Aktueller Branch: <branch-name>
🔀 Integration-Branches: <liste>

📋 Nächster Schritt:
  Phase X.Y: <Beschreibung>
  Branch: <empfohlener-branch-name>
  Commit: <commit-message>

  Kurzbeschreibung: <1-2 Sätze was gemacht wird>

💡 Weitere Kandidaten (ausgearbeitet + Abhängigkeiten erfüllt):
  Phase X: <Name> (Epic: <Epic-Name>)
```

### WICHTIG

- **NICHT implementieren** — nur analysieren und ausgeben
- **KEINE** Branches erstellen
- **KEINE** Dateien ändern
- **KEINE** Commits erstellen
- Nur lesen, analysieren, dem User die Übersicht zeigen
- Danach aufhören und auf den User warten
