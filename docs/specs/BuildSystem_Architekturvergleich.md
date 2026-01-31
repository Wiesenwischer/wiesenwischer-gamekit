# 🏗 Architekturvergleich: BuildSystem – Modus vs. Dienst

Dieses Dokument vergleicht zwei unterschiedliche Architekturen für die Integration eines Bausystems in ein Spiel mit modularer Player-Steuerung.

---

## 🔹 Variante 1: BuildSystem als Spielmodus (BuildMode)

### 📐 Konzept

- Das Bauen ist ein **eigenständiger Spielmodus**
- Der Spieler **wechselt in den Baumodus** (z. B. durch Tastendruck)
- Die Bewegung, Kamera und UI werden vom BuildMode kontrolliert
- Input und States werden innerhalb eines `IPlayerMode` gekapselt

### ✅ Vorteile

- Klare Trennung zwischen Spielmodi
- Mehr visuelle Kontrolle: z. B. spezielle Baumodus-Kamera
- Natürlich für Sandbox- oder Simulationsspiele
- Kann zusätzliche Regeln oder UI je Modus nutzen

### 🚫 Nachteile

- Erfordert Zustandswechsel-Logik
- Movement muss im BuildMode separat oder gemeinsam implementiert werden
- Ggf. langsamerer Übergang zwischen Spielaktionen

---

## 🔸 Variante 2: BuildSystem als angebundener Dienst

### 📐 Konzept

- Das BuildSystem ist **autonom**
- Es wird bei Bedarf an den `PlayerController` gebunden
- Der Player bleibt in voller Kontrolle (Bewegung, Kamera, etc.)
- Das BuildSystem verwaltet nur Platzierung, Vorschau, Regeln usw.

### ✅ Vorteile

- Sehr modular – funktioniert mit Player, NPCs, Admin-Tools
- Einfache Integration in MMOs mit frei beweglichen Spielern
- Weniger komplexe Zustandsverwaltung
- Ideal für Kombination mit AbilitySystem oder Multiplayer

### 🚫 Nachteile

- Kein visueller Moduswechsel (es sei denn man kombiniert ihn)
- Kamerasteuerung, UI und Input müssen synchronisiert werden
- Weniger immersive Trennung zwischen „Spielen“ und „Bauen“

---

## 🔁 Kombinierte Lösung (empfohlen)

- Das **BuildSystem bleibt immer ein Dienst**
- Ein optionaler **BuildMode** (IPlayerMode) kann verwendet werden, um Bewegung, Kamera und UI zu kontrollieren
- Beide Architekturen greifen auf dasselbe BuildSystem als Backend zu

---

## 🧪 Entscheidungshilfe

| Frage | Empfehlung |
|-------|------------|
| Soll der Spieler frei herumlaufen und jederzeit bauen können? | ✅ Dienst-Architektur |
| Gibt es einen klaren Moduswechsel mit spezieller UI/Kamera? | ✅ Modus-Architektur |
| Sollen auch NPCs/Admins/etc. bauen können? | ✅ Dienst-Architektur |
| Sandbox-/Aufbau-Spiel mit klarer Trennung zwischen Spiel- und Baumodus? | ✅ Modus-Architektur |

---

## 🧱 Modulstruktur-Vorschlag

```
/Packages
├── Core.PlayerController
│   ├── PlayerController.cs
│   └── BuildSystemBridge.cs (nur bei Dienst-Architektur)
├── Module.BuildSystem
│   ├── BuildSystem.cs
│   ├── BuildPlacementRules.cs
│   └── BuildUI.cs
```