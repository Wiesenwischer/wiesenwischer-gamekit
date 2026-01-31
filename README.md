# 🎮 Wiesenwischer GameKit - Character Controller

Ein modularer, MMO-fähiger Character Controller für Unity, entwickelt als Teil des Wiesenwischer GameKit Frameworks.

## 🎯 Features

- **Modulare Paketstruktur**: Aufgeteilt in Core, Camera und Animation-Pakete
- **MMO-Ready**: Vorbereitet für FishNet-Integration und Client-Side Prediction
- **Input System**: Basierend auf Unity's neuem Input System
- **Erweiterbar**: Konzipiert für zusätzliche Module wie Riding, Combat, Abilities
- **IK-Unterstützung**: Inverse Kinematics für natürliche Animationen

## 📦 Paketübersicht

| Paket | Beschreibung |
|-------|--------------|
| `wiesenwischer.gamekit.charactercontroller.core` | Basis-Movement, State Machine, Grounding |
| `wiesenwischer.gamekit.charactercontroller.camera` | Cinemachine-Setup, Follow-Logik |
| `wiesenwischer.gamekit.charactercontroller.animation` | Animator Controller, Blend Trees |
| `wiesenwischer.gamekit.charactercontroller` | Komplettpaket mit allen Modulen |

## 🚀 Installation

### Voraussetzungen

- Unity 2022.3 LTS oder höher
- Git mit Git LFS installiert
- Unity Input System Package

### Als Unity Package installieren

```json
{
  "dependencies": {
    "wiesenwischer.gamekit.charactercontroller": "https://github.com/Wiesenwischer/wiesenwischer-gamekit-charactercontroller.git#1.0.0"
  }
}
```

## 📁 Projektstruktur

```
Wiesenwischer.GameKit.CharacterController/
├── Packages/
│   └── Wiesenwischer.GameKit.CharacterController/
│       ├── Runtime/
│       │   ├── Core/
│       │   ├── Camera/
│       │   └── Animation/
│       ├── Editor/
│       ├── Tests/
│       └── package.json
├── docs/
│   └── specs/
├── .github/
│   └── workflows/
└── README.md
```

## 🧩 Architektur

Das System folgt diesen Prinzipien:

1. **Modularität**: Jedes Feature als eigenständiges Paket
2. **Interface-basiert**: Klare Schnittstellen zwischen Komponenten
3. **Netzwerkfähig**: Client-Side Prediction und Server Authority
4. **Testbar**: Unabhängig testbare Module
5. **Erweiterbar**: Einfache Integration neuer Features

## 📚 Dokumentation

Detaillierte Spezifikationen finden sich im [docs/specs](docs/specs/) Verzeichnis:

- [Character Controller Modular](docs/specs/GameKit_CharacterController_Modular.md)
- [Input System Spezifikation](docs/specs/GameKit_InputSystem_Spezifikation.md)
- [MMO Basics](docs/specs/GameKit_MMO_Basics.md)
- [Client-Side Prediction](docs/specs/CSP_Spezifikation.md)
- [IK Spezifikation](docs/specs/GameKit_IK_Spezifikation.md)

## 🔧 Entwicklung

### Branching-Modell

Das Projekt verwendet **Trunk-Based Development**:

- `main` ist der Hauptzweig
- Alle Änderungen erfolgen über Pull Requests
- Feature-Branches: `feature/feature-name`
- Bugfix-Branches: `fix/bug-name`

### Commit-Richtlinien

- Häufige, kleine Commits
- Ein Commit = Ein fachliches Thema
- Klare, beschreibende Commit-Messages

## 🤝 Contributing

Beiträge sind willkommen! Bitte beachte die Dokumentation in [claude.md](claude.md) für Architekturprinzipien und Entwicklungsrichtlinien.

## 📄 Lizenz

TBD

## 🔗 Links

- [Wiesenwischer GameKit](https://github.com/Wiesenwischer)
- [Dokumentation](docs/specs/)
