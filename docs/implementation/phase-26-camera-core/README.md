# Phase 26: Camera Core — Brain, State & Pivot Rig

> **Status:** Ausgearbeitet
> **Branch:** `integration/phase-26-camera-core`
> **Abhängigkeiten:** Phase 4 (Locomotion, abgeschlossen)

---

## Ziel

Die bestehende **ThirdPersonCamera-Monolith** (`ThirdPersonCamera.cs`, 374 Zeilen) in eine modulare AAA-Architektur refactoren:

- **CameraBrain** als zentraler Orchestrator
- **CameraState** als deklarativer Kamerazustand pro Frame
- **Pivot Rig** mit getrennten Rotationsachsen (Yaw/Pitch/Offset)
- **Camera Anchor** für Stabilisierung gegen Animation Noise
- **Camera Input Pipeline** mit AAA-Filterstufen (Deadzone, Acceleration, Smoothing)
- **ICameraBehaviour Interface** als Erweiterungspunkt (Behaviours selbst → Phase 27)

Die Kamera bleibt nach Phase 26 **voll funktionsfähig** — die bestehende Orbit/Follow/Collision-Logik wird in den CameraBrain eingebettet und in Phase 27 in separate Behaviours extrahiert.

---

## Relevante Spezifikation

- [Camera System Spezifikation](../../specs/Camera_System_Spezifikation.md) — Kapitel 2–6, 9, 11–12

---

## Bestehender Code (wird refactored)

| Datei | Paket | Funktion | Aktion |
|-------|-------|----------|--------|
| `ThirdPersonCamera.cs` | CharacterController.Camera | Monolithische Orbit/Follow/Collision-Kamera | → Logik migriert in CameraBrain |
| `CameraInputHandler.cs` | CharacterController.Camera | Input Bridge (Unity Input System) | → Ersetzt durch CameraInputPipeline |
| `CameraConfig.cs` | CharacterController.Camera | ScriptableObject Config | → Erweitert, bleibt kompatibel |
| `CameraSetupEditor.cs` | CharacterController.Camera | Editor-Menü für Setup | → Aktualisiert für neue Komponenten |

---

## Neues Package

```
Packages/Wiesenwischer.GameKit.Camera.Core/
├── package.json
├── README.md
├── Runtime/
│   ├── Wiesenwischer.GameKit.Camera.Core.Runtime.asmdef
│   ├── Core/
│   │   ├── CameraState.cs
│   │   ├── CameraContext.cs
│   │   ├── CameraInputState.cs
│   │   └── ICameraBehaviour.cs
│   ├── Brain/
│   │   └── CameraBrain.cs
│   ├── Rig/
│   │   └── PivotRig.cs
│   ├── Anchor/
│   │   └── CameraAnchor.cs
│   └── Input/
│       └── CameraInputPipeline.cs
├── Tests/
│   ├── Wiesenwischer.GameKit.Camera.Core.Tests.asmdef
│   └── Runtime/
│       ├── CameraStateTests.cs
│       ├── CameraInputPipelineTests.cs
│       ├── PivotRigTests.cs
│       └── CameraAnchorTests.cs
└── Editor/
    ├── Wiesenwischer.GameKit.Camera.Core.Editor.asmdef
    └── CameraSetupEditor.cs  (migriert)
```

---

## Architektur-Überblick

```
Unity Input System
       ↓
CameraInputPipeline (Deadzone → Acceleration → Smoothing → Noise Reduction)
       ↓
CameraBrain (Orchestrator)
  ├── liest CameraContext (Target, Input, DeltaTime)
  ├── iteriert ICameraBehaviour[] (Phase 27)
  ├── berechnet finalen CameraState
  └── schreibt CameraState → PivotRig
       ↓
CameraAnchor ── stabilisiert Follow-Position
       ↓
PivotRig (Root → Yaw → Pitch → Offset → Camera)
       ↓
Unity Camera / Cinemachine (Rendering)
```

---

## Abgrenzung

### In Phase 26 (diese Phase)
- CameraState struct + CameraContext
- CameraBrain mit **eingebetteter** Orbit/Follow/Zoom/Collision-Logik
- PivotRig Transform-Hierarchie
- CameraAnchor Stabilisierung
- CameraInputPipeline mit Filterstufen
- ICameraBehaviour Interface (Deklaration)
- Migration der bestehenden Funktionalität
- Unit Tests

### NICHT in Phase 26 (spätere Phasen)
- Separate ICameraBehaviour-Implementierungen (→ Phase 27)
- ICameraIntent System + Presets (→ Phase 28)
- IOrientationProvider / IFacingProvider (→ Phase 29)
- CinemachineDriver als eigene Klasse (→ Phase 28)
- Dynamic Orbit Center (→ Phase 27)
- Soft Targeting (→ Phase 27)

---

## Impact-Analyse

### Betroffene Systeme
- **PlayerController.cs** — Nutzt `Camera.main` direkt (`GetCameraForward()`). Bleibt unverändert in Phase 26, wird in Phase 29 auf IOrientationProvider umgestellt.
- **CameraSetupEditor.cs** — Muss aktualisiert werden für neue Komponenten.
- **Player Prefab / Szene** — Camera-Setup muss nach Migration neu eingerichtet werden.

### Keine Breaking Changes für
- CharacterController.Core (keine API-Änderungen)
- Animation Package (unabhängig)
- IK Package (unabhängig)

---

## Schritte

| Schritt | Name | Dateien |
|---------|------|---------|
| [26.1](26.1-package-grundtypen.md) | Package-Struktur & Grundtypen | Package, CameraState, CameraContext, CameraInputState, ICameraBehaviour |
| [26.2](26.2-pivot-rig.md) | Pivot Rig Hierarchie | PivotRig.cs |
| [26.3](26.3-camera-anchor.md) | Camera Anchor | CameraAnchor.cs |
| [26.4](26.4-input-pipeline.md) | Camera Input Pipeline | CameraInputPipeline.cs |
| [26.5](26.5-camera-brain.md) | CameraBrain Orchestrator | CameraBrain.cs |
| [26.6](26.6-migration-editor.md) | Migration & Editor Update | CameraSetupEditor.cs, alte Komponenten |
| [26.7](26.7-unit-tests.md) | Unit Tests | Tests/ |
