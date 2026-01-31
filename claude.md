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

## 🧾 Git-Workflow & Arbeitsweise

### 🌳 Branching-Modell

Es wird **Trunk-Based Development** verwendet:
- `main` ist der **Hauptzweig** und ist **geschützt**
- **Keine direkten Commits auf `main`** - alle Änderungen über Pull Requests
- Feature-Branches sind kurzlebig (max. wenige Tage)

### 📝 Branch-Namenskonventionen

Format: `<type>/<kurze-beschreibung>`

| Typ | Verwendung | Beispiel |
|-----|------------|----------|
| `feature/` | Neue Funktionalität | `feature/jump-mechanics` |
| `fix/` | Bugfixes | `fix/ground-detection` |
| `docs/` | Dokumentation | `docs/api-reference` |
| `refactor/` | Code-Umbau ohne Funktionsänderung | `refactor/state-machine` |
| `test/` | Tests hinzufügen/ändern | `test/movement-tests` |
| `chore/` | Wartung, Config, CI/CD | `chore/release-drafter` |

### 🔁 Commit-Richtlinien

**Conventional Commits** Format (deutsch):
```
<type>: <Beschreibung>

[optionaler Body]
```

| Type | Bedeutung |
|------|-----------|
| `feat` | Neues Feature |
| `fix` | Bugfix |
| `docs` | Dokumentation |
| `test` | Tests |
| `refactor` | Refactoring |
| `chore` | Wartung |

**Regeln:**
- Häufige, kleine Commits
- Jeder Commit behandelt **nur ein fachliches Thema**
- Keine Claude-spezifischen Footer in Commit-Messages oder PRs
- Breaking Changes mit `!` markieren: `feat!: Neue API`

### 🚀 Feature-Implementierung Workflow

1. **Branch erstellen**
   ```bash
   git checkout main
   git pull origin main
   git checkout -b feature/<name>
   ```

2. **Entwickeln & Committen**
   - Kleine, atomare Commits
   - Regelmäßig pushen

3. **Kompilierung prüfen (PFLICHT)**
   - Unity Editor Logs auf Compiler-Fehler prüfen
   - Alle Packages müssen fehlerfrei kompilieren
   - Auch Test-Projekte beachten (MockConfig-Klassen etc.)
   ```bash
   # Unity Editor Log prüfen:
   powershell -Command "Get-Content 'C:\Users\marcu\AppData\Local\Unity\Editor\Editor.log' -Tail 100 | Select-String -Pattern 'error|CS\d{4}'"
   ```

4. **Pull Request erstellen**
   ```bash
   git push -u origin feature/<name>
   gh pr create --title "feat: <Beschreibung>" --body "..."
   ```

4. **PR-Titel für Release Drafter**
   - Muss mit Conventional Commit Prefix beginnen
   - Beispiel: `feat: Implementiere Jump-Mechanik`
   - Labels werden automatisch gesetzt (Autolabeler)

5. **Nach Merge**
   ```bash
   git checkout main
   git pull origin main
   git branch -d feature/<name>
   ```

### 🏷️ PR Labels (für Release Notes)

Labels werden automatisch basierend auf PR-Titel gesetzt:

| PR-Titel Prefix | Auto-Label | Release-Kategorie |
|-----------------|------------|-------------------|
| `feat:` | `feature` | 🚀 Features |
| `fix:` | `fix` | 🐛 Bug Fixes |
| `docs:` | `documentation` | 📚 Documentation |
| `test:` | `test` | 🧪 Tests |
| `chore:` | `chore` | 🔧 Maintenance |
| `refactor:` | `refactor` | 🔧 Maintenance |
| `...!:` | `breaking` | ⚠️ Breaking Changes |

### 📦 Releases

**Release Drafter** erstellt automatisch Release Notes:

1. Bei jedem Push/PR auf `main` wird ein Draft-Release aktualisiert
2. Release Notes werden aus PR-Titeln generiert
3. Version wird automatisch ermittelt:
   - `feature` Label → Minor Version (0.x.0)
   - `fix` Label → Patch Version (0.0.x)
   - `breaking` Label → Major Version (x.0.0)

**Release veröffentlichen:**
1. GitHub → Releases → Draft bearbeiten
2. Version prüfen/anpassen
3. "Publish release" klicken

### 🔢 Versionierung mit GitVersion

**GitVersion** berechnet automatisch semantische Versionen aus Git-History:

| Branch | Version-Format | Beispiel |
|--------|---------------|----------|
| `main` | `{major}.{minor}.{patch}` | `0.2.0` |
| `feature/*` | `{major}.{minor}.{patch}-alpha.{n}` | `0.2.0-alpha.3` |
| `fix/*` | `{major}.{minor}.{patch}-beta.{n}` | `0.2.1-beta.1` |

**Konfiguration:** `GitVersion.yml` im Root

**CI-Integration:**
- GitHub Actions Workflow `.github/workflows/ci.yml`
- Version wird bei jedem Build berechnet
- Kann für Package-Versionierung verwendet werden

### ⚠️ Wichtige Regeln

- **NIEMALS** direkt auf `main` committen
- **NIEMALS** `--force` auf `main` pushen
- **IMMER** Feature-Branch für Änderungen erstellen
- **IMMER** PR-Titel mit Conventional Commit Prefix
- **IMMER** Kompilierung prüfen vor PR-Erstellung (inkl. Tests/Mocks)
- Branch Protection ist aktiviert und erzwingt diese Regeln

---

## 🛠️ CI/CD Konfiguration

### GitHub Actions Workflows

| Workflow | Datei | Trigger | Funktion |
|----------|-------|---------|----------|
| CI | `.github/workflows/ci.yml` | Push/PR auf main | GitVersion, Build |
| Release Drafter | `.github/workflows/release-drafter.yml` | Push/PR auf main | Release Notes Draft |

### Konfigurationsdateien

| Datei | Zweck |
|-------|-------|
| `GitVersion.yml` | Semantische Versionierung |
| `.github/release-drafter.yml` | Release Notes Kategorien & Templates |