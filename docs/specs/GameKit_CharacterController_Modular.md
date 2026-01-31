# 🎮 GameKit.CharacterController – Modulare Paketstruktur

Dieses Dokument beschreibt die modulare Aufteilung des Character Controllers in einzelne Unity-Pakete. Ziel ist eine **saubere Trennung** von Zuständigkeiten bei maximaler Wiederverwendbarkeit – lokal, in Teams oder für den Unity Asset Store.

---

## 🧱 Paketübersicht

| Paketname | Inhalt | Zweck |
|-----------|--------|-------|
| `wiesenwischer.gamekit.charactercontroller.core` | Bewegung, State Machine, Grounding, Input | Fundament aller Steuerungssysteme |
| `wiesenwischer.gamekit.charactercontroller.camera` | Cinemachine-Setup, Follow-Logik, LookAt | Kamerasystem für Third-Person-Ansicht |
| `wiesenwischer.gamekit.charactercontroller.animation` | Animator Controller, Blend Trees, Layer-Logik | Animationen für Bewegungszustände |
| `wiesenwischer.gamekit.charactercontroller` | Kombipaket mit Core, Camera, Animation, Demo | Einstiegspaket & Asset-Store-Version |

---

## 📦 Strukturbeispiel (für das Komplettpaket)

```
wiesenwischer.gamekit.charactercontroller/
├── Runtime/
│   ├── Core/                  # → aus core-Paket
│   ├── Camera/                # → aus camera-Paket
│   ├── Animation/             # → aus animation-Paket
│   └── Prefabs/
│       └── Player.prefab      # Kombinierter Player
├── Demo/
│   └── Scenes/
│       └── MovementTest.unity
├── Editor/
├── package.json
├── README.md
└── CHANGELOG.md
```

---

## 📄 Beispiel `package.json` für das Komplettpaket

```json
{
  "name": "wiesenwischer.gamekit.charactercontroller",
  "displayName": "GameKit Character Controller",
  "version": "1.0.0",
  "unity": "2022.3",
  "description": "Modularer Third-Person-Controller mit Bewegung, Kamera und Animation.",
  "keywords": [
    "character",
    "controller",
    "camera",
    "animation",
    "third-person",
    "modular"
  ],
  "author": {
    "name": "Wiesenwischer"
  },
  "dependencies": {
    "wiesenwischer.gamekit.charactercontroller.core": "1.0.0",
    "wiesenwischer.gamekit.charactercontroller.camera": "1.0.0",
    "wiesenwischer.gamekit.charactercontroller.animation": "1.0.0"
  }
}
```

---

## 🔄 Vorteile der Aufteilung

- **Modular & wartbar** – Einzelpakete können unabhängig entwickelt und getestet werden.
- **Wiederverwendbar** – Projekte können nur das einbinden, was sie benötigen.
- **Für Asset Store & UPM geeignet** – Mit Metapaket als Einstiegspunkt.

---

## 🚀 Erweiterungsmöglichkeiten

Spätere Add-ons:

- `wiesenwischer.gamekit.charactercontroller.ik`
- `wiesenwischer.gamekit.charactercontroller.networking`
- `wiesenwischer.gamekit.charactercontroller.combat`
- `wiesenwischer.gamekit.charactercontroller.abilities`

---

## 🧪 Testbarkeit

Die Demo-Szene enthält:

- `Player.prefab` mit integrierter Bewegung, Kamera und Animator
- Cinemachine Virtual Camera
- Unterstützung für Unity Input System

---

## 📌 Empfehlung

Verwende `wiesenwischer.gamekit.charactercontroller` für die Integration ins Spiel. Nutze die Submodule für Erweiterung, Anpassung oder Testing.