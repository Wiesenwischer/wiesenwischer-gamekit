# 🧠 Claude.md – Anweisungen für Modulverständnis und Umsetzung

Diese Datei dient als Einstiegspunkt für Claude AI, um sich schnell mit der Struktur, Philosophie und Architektur des GameKit-Systems vertraut zu machen.

---

## 🎯 Ziel

Das Ziel ist die Entwicklung eines **modularen, MMO-fähigen Unity GameKit Frameworks** mit folgenden Merkmalen:

- ⚙️ Modularisierung in eigene Unity-Packages
- 🧍‍♂️ Erweiterbare Character-Controller (Movement, Riding, Gliding, Combat…)
- 🧠 Adaptive Skill- und Fortschrittssysteme (siehe `GameKit_Adaptive_Progression.md`)
- 🎯 Input-System auf Basis des neuen Unity Input Systems, netzwerkfähig via FishNet
- 🛠 Berufssystem mit Langzeitmotivation
- 🏠 Bausystem mit Echtzeit-Platzierung und „Under Construction“-Visualisierung
- 🌐 MMO-Fokus mit Netzwerkabstraktion (FishNet), Client-Side Prediction, IK-Unterstützung

---

## 📁 Dateien & Einstiegspunkte

| Datei | Inhalt |
|-------|--------|
| `GameKit_Adaptive_Progression.md` | Fortschrittssystem inkl. CSP, Diminishing Returns, Berufssystem |
| `GameKit_InputSystem_Spezifikation.md` | Entkoppeltes, netzwerkfähiges Input Handling |
| `GameKit_IK_Spezifikation.md` | IK-Unterstützung für Animation und LookAt-Logik |
| `CSP_Spezifikation.md` | Client-Side Prediction-Grundlagen für Netzwerkbewegung |
| `BuildSystem_*.md` | Konzeptionen für ein modulares Bausystem |
| `UnityRepo_CompleteSetup.md` | Repository-Setup inkl. Git, LFS, Release, Versionierung |
| `GameKit_MMO_Basics.md` | Wichtige MMO-Grundlagen und Design-Prinzipien |

---

## 🧩 Architekturprinzipien

1. **Alle Module sind unabhängig testbar**
2. **Fokus auf Wiederverwendbarkeit & Austauschbarkeit**
3. **Schnittstellen statt Implementierungen im Kernsystem**
4. **Multiplayer-Ready durch separates Netzwerkpaket**
5. **Balance durch Spielzeit, Spezialisierung, natürliche Limits**

---

## ✅ Konkrete Aufgaben für Claude

- Unity-Komponenten gemäß Spezifikation erzeugen
- Neue Features als modulare Pakete designen
- Bestehende Spezifikationen (Markdown) erweitern, nie überschreiben
- Diagramme, JSON-Templates oder API-Contracts erzeugen, wenn gewünscht
- Für jede Änderung verständliche Commits oder Patches erzeugen

---

## 🔓 Berechtigungen & Dateizugriff

- **Vollständiger Dateisystemzugriff**: Claude hat kompletten Zugriff auf das Dateisystem innerhalb dieses Projekts
- **Keine Rückfragen bei Dateioperationen**: Dateien können direkt erstellt, gelesen, bearbeitet und gelöscht werden
- **Autonome Entwicklung**: Claude kann die Entwicklung durchführen, ohne nach Zugriffsrechten zu fragen
- **Proaktives Arbeiten**: Tools wie Write, Edit, Bash können ohne Benutzerbestätigung verwendet werden

---

## ℹ️ Hinweise

- Keine festen Klassen – Charaktere entwickeln sich durch Spielstil
- Hybride Builds sind gewünscht und balanciert möglich
- MMO first – lokale Nutzung optional
- Netzwerk: FishNet, Client-Side Prediction & IK Support sind vorgesehen

---

## 🚀 Nächste Schritte

1. Neue Feature-Spezifikation anlegen? → Neue `.md` im selben Stil wie oben
2. Neue Unity-Komponenten? → In eigenem Package-Verzeichnis strukturieren
3. Konfigurationen? → ScriptableObjects verwenden
---

## 🧾 Weitere wichtige Anweisungen

- 🔁 **Commit-Richtlinien**
  - Häufige, kleine Commits.
  - Jeder Commit behandelt **nur ein fachliches Thema oder eine Aufgabe**.
  - Keine Claude-spezifischen Footer oder automatischen Hinweise in Commit-Messages oder Pull Requests.

- 🌳 **Branching-Modell**
  - Es wird **Trunk-Based Development** verwendet.
  - `main` ist der **Hauptzweig**.
  - Alle Änderungen an `main` erfolgen ausschließlich über **Pull Requests** von Feature-Branches.