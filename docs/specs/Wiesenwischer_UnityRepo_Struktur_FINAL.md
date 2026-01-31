# 🧭 Repository- und Paketstruktur: Wiesenwischer GameKit (Unity)

Dies ist die finale Struktur für dein modulares Unity Game Framework **Wiesenwischer.GameKit**, ohne den Zusatz "unity" im Repository-Namen. Alle Repos sind lowercase für maximale Kompatibilität und Standardkonformität.

---

## 🏷️ Namensraum

**Haupt-Namespace im Code:**  
`Wiesenwischer.GameKit`

Beispielhafte Unterräume:
- `Wiesenwischer.GameKit.CharacterController`
- `Wiesenwischer.GameKit.BuildSystem`
- `Wiesenwischer.GameKit.Crafting`

---

## 📦 Repository-Konvention

Jedes Modul erhält ein eigenes GitHub-Repository nach dem Schema:

```
wiesenwischer-gamekit-<modulname>
```

Beispiele:
| Repository-Name                                 | Entspricht Package            | Beschreibung |
|-------------------------------------------------|-------------------------------|--------------|
| `wiesenwischer-gamekit-charactercontroller`     | `wiesenwischer.gamekit.charactercontroller` | Basismovement mit State Machine |
| `wiesenwischer-gamekit-ridingcontroller`        | `wiesenwischer.gamekit.ridingcontroller`    | Reiten als Erweiterung |
| `wiesenwischer-gamekit-buildsystem`             | `wiesenwischer.gamekit.buildsystem`         | Bauen, Terraforming |
| `wiesenwischer-gamekit-crafting`                | `wiesenwischer.gamekit.crafting`            | Berufe und Herstellung |
| `wiesenwischer-gamekit-combat`                  | `wiesenwischer.gamekit.combat`              | Kampfsystem |
| `wiesenwischer-gamekit-core` *(optional)*       | `wiesenwischer.gamekit.core`                | Abhängigkeiten, Interfaces |

---

## 📁 UnityPackage-Struktur im Repository

```
wiesenwischer-gamekit-<modulname>/
├── Packages/
│   └── Wiesenwischer.GameKit.<Modul>/
│       ├── Runtime/
│       ├── Editor/
│       ├── Tests/
│       └── package.json
├── SampleScenes/
├── README.md
└── CHANGELOG.md
```

### `package.json` Beispiel
```json
{
  "name": "wiesenwischer.gamekit.charactercontroller",
  "displayName": "GameKit Character Controller",
  "version": "1.0.0",
  "unity": "2022.3",
  "description": "Modularer Third-Person Character Controller mit State Machine.",
  "keywords": ["character", "controller", "movement", "state-machine"]
}
```

---

## 🧱 `.asmdef` Namensschema

```text
Wiesenwischer.GameKit.CharacterController.Runtime
Wiesenwischer.GameKit.CharacterController.Editor
```

---

## 📤 Nutzungsmöglichkeiten

- **Lokal in Unity:**  
  `file:../wiesenwischer-gamekit-charactercontroller/Packages/Wiesenwischer.GameKit.CharacterController`

- **Git-basierter Import:**  
  `git+https://github.com/Wiesenwischer/wiesenwischer-gamekit-charactercontroller.git#1.0.0`

---

## 🔁 Vorteile dieser Struktur

- 🌱 Modular, wachstumsfähig
- 🔄 Austauschbare Pakete
- 🤝 Einfaches Arbeiten im Team
- 🔧 GitHub CI/CD-freundlich
- 🧩 UnityPackage-Import ready
- 🧠 Gut dokumentierbar und versionierbar