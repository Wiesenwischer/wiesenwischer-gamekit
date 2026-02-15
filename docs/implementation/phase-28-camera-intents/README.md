# Phase 28: Camera Intent System & Presets

> **Status:** Ausgearbeitet
> **Branch:** `integration/phase-28-camera-intents`
> **Abhängigkeiten:** Phase 27 (Camera Behaviours, muss abgeschlossen sein)

---

## Ziel

Prioritäts-basiertes **Intent System** für Konfliktlösung zwischen Systemen, die gleichzeitig die Kamera beeinflussen wollen. **CameraPreset ScriptableObjects** für verschiedene Camera-Styles (BDO, ArcheAge). Zusätzlich: **DynamicOrbitCenter** (kontextabhängiger Orbit-Pivot), **Soft Targeting** (additive Bias-Rotation) und optionaler **CinemachineDriver** als Rendering-Schicht.

Nach Phase 28:
- Externe Systeme (Combat, Mount, Ability) können **Intents pushen**, die nach Priorität aufgelöst werden
- **CameraPresets** speichern Behaviour-Konfigurationen als ScriptableObject — Designer-Tuning ohne Code-Änderungen
- Camera orbitet kontextabhängig um einen **dynamischen Pivot** (Idle/Movement/Combat)
- **Soft Targeting** biased die Kamera sanft Richtung Ziele (Movement Forward, Enemy, Ability Target)
- Optional: **CinemachineDriver** setzt den CameraState auf eine Cinemachine VirtualCamera

---

## Relevante Spezifikation

- [Camera System Spezifikation](../../specs/Camera_System_Spezifikation.md) — Kapitel 7, 10, 13, 15, 16

---

## Neue/Erweiterte Packages

### Camera.Core (Erweiterung)

```
Packages/Wiesenwischer.GameKit.Camera.Core/
└── Runtime/
    └── Core/
        ├── ICameraIntent.cs          (NEU)
        ├── ICameraPresetReceiver.cs   (NEU)
        ├── CameraPreset.cs           (NEU)
        ├── CameraContext.cs           (ERWEITERT: CharacterVelocity, CharacterForward)
    └── Brain/
        └── CameraBrain.cs            (ERWEITERT: Intent Resolution, SetPreset)
    └── Rendering/
        └── CinemachineDriver.cs      (NEU, optional)
```

### Camera.Behaviours (Erweiterung)

```
Packages/Wiesenwischer.GameKit.Camera.Behaviours/
└── Runtime/
    ├── DynamicOrbitCenterBehaviour.cs  (NEU)
    └── SoftTargetingBehaviour.cs      (NEU)
```

### Camera.Presets (NEU)

```
Packages/Wiesenwischer.GameKit.Camera.Presets/
├── package.json
├── Runtime/
│   ├── Wiesenwischer.GameKit.Camera.Presets.Runtime.asmdef
│   └── CameraPresetLibrary.cs       (optional: Zugriff auf eingebettete Presets)
└── Presets/
    ├── CameraPreset_BDO.asset
    └── CameraPreset_ArcheAge.asset
```

**Abhängigkeiten:**
- Camera.Presets → Camera.Core (für CameraPreset-Typ)
- Camera.Behaviours → Camera.Core (bereits bestehend)
- CinemachineDriver → `com.unity.cinemachine` (2.10.1, bereits im Projekt)

---

## Architektur-Überblick

### Intent-Auflösung im CameraBrain

```
CameraInputPipeline → CameraInputState
                          ↓
CameraBrain.LateUpdate()
  ├── CameraAnchor.UpdateAnchor()
  ├── Context zusammenbauen (+ CharacterVelocity, CharacterForward)
  ├── ICameraIntent[] nach Priorität sortieren und anwenden   ← NEU
  ├── ICameraBehaviour[] iterieren:
  │     1. OrbitBehaviour
  │     2. RecenterBehaviour
  │     3. ZoomBehaviour
  │     4. DynamicOrbitCenterBehaviour                        ← NEU
  │     5. ShoulderOffsetBehaviour
  │     6. SoftTargetingBehaviour                             ← NEU
  │     7. CollisionBehaviour
  │     8. InertiaBehaviour
  └── PivotRig.ApplyState()
          ↓
      CinemachineDriver.SyncToVirtualCamera()                 ← NEU (optional)
```

### Intent vs. Behaviour

| Konzept | Lebensdauer | Zweck | Beispiel |
|---------|-------------|-------|----------|
| **ICameraIntent** | Transient (Push/Remove) | Externe Systeme wollen Kamera beeinflussen | Combat-Framing, Mount-Offset, Ability-FOV |
| **ICameraBehaviour** | Persistent (Komponente) | Kontinuierliche Kamera-Features | Orbit, Zoom, Collision, Inertia |

### CameraPreset-Workflow

```
Designer erstellt CameraPreset SO → Konfiguriert alle Behaviour-Parameter
                                        ↓
CameraBrain.SetPreset(preset) → Iteriert ICameraBehaviour[]
                                        ↓
                               Behaviours lesen Preset-Werte (ICameraPresetReceiver)
                                        ↓
                               Kamera verhält sich wie konfiguriert
```

---

## Abgrenzung

### In Phase 28 (diese Phase)
- ICameraIntent Interface + CameraBrain Intent Resolution
- CameraPreset ScriptableObject + Preset Package
- BDO + ArcheAge Preset-Assets
- DynamicOrbitCenterBehaviour (Forward Bias, Target Midpoint)
- SoftTargetingBehaviour (additive Bias-Rotation)
- CinemachineDriver (optionale Rendering-Schicht)
- CameraContext-Erweiterung (CharacterVelocity, CharacterForward)
- CameraSetupEditor-Update
- Unit Tests

### NICHT in Phase 28 (spätere Phasen)
- IOrientationProvider / IFacingProvider (→ Phase 29)
- Camera-Relative Animation Space / MoveX/MoveZ (→ Phase 29)
- Lock-On Camera Mode (→ Combat-Erweiterung)
- Konkrete Intent-Implementierungen für Combat/Mount/Ability (→ jeweilige Phasen)
- First Person / Photo Mode (→ zukünftige Phasen)

---

## Impact-Analyse

### Phase 27 (Camera Behaviours, ✅ ausgearbeitet, Status: Offen)

Phase 28 baut direkt auf Phase 27 auf:
- **CameraBrain:** Phase 27 refactored den Brain (27.5), Phase 28 erweitert ihn um Intent Resolution + SetPreset. Kein Konflikt, da Phase 27 zuerst abgeschlossen sein muss.
- **Camera.Behaviours Package:** Phase 28 fügt neue Behaviours hinzu (DynamicOrbitCenter, SoftTargeting). Erweiterung, keine Änderung bestehender Behaviours.
- **CameraContext:** Phase 28 fügt neue Felder hinzu. Bestehende Behaviours aus Phase 27 sind nicht betroffen (sie lesen nur existierende Felder).

**Keine kritischen Konflikte.** Phase 27 muss nur vor Phase 28 abgeschlossen werden.

### Andere Phasen

- **Phase 6/7 (Netzwerk):** Kein Impact — Camera ist rein lokal (Spec Kapitel 22).
- **Phase 29 (Orientation/Facing):** Nicht ausgearbeitet. Phase 28 berührt keine Orientation/Facing-Interfaces.

---

## Schritte

| Schritt | Name | Branch | Dateien |
|---------|------|--------|---------|
| [28.1](28.1-intent-interface.md) | ICameraIntent Interface + CameraContext-Erweiterung | `feat/camera-intent-interface` | ICameraIntent.cs, ICameraPresetReceiver.cs, CameraContext.cs |
| [28.2](28.2-camera-preset.md) | CameraPreset ScriptableObject + Package | `feat/camera-presets` | CameraPreset.cs, Camera.Presets Package |
| [28.3](28.3-bdo-archage-presets.md) | BDO + ArcheAge Presets | `feat/camera-presets` | Preset-Assets, CameraPresetLibrary.cs |
| [28.4](28.4-dynamic-orbit-soft-targeting.md) | DynamicOrbitCenter + Soft Targeting | `feat/camera-dynamic-orbit` | DynamicOrbitCenterBehaviour.cs, SoftTargetingBehaviour.cs |
| [28.5](28.5-cinemachine-driver.md) | CinemachineDriver (optional) | `feat/cinemachine-driver` | CinemachineDriver.cs |
| [28.6](28.6-brain-integration-editor.md) | CameraBrain Integration + Editor Update | `feat/camera-brain-intents` | CameraBrain.cs, CameraSetupEditor.cs |
| [28.7](28.7-unit-tests.md) | Unit Tests | `test/camera-intents-presets` | Tests/ |

---

## Voraussetzungen

- Phase 27 (Camera Behaviours) **muss abgeschlossen** sein
- Phase 26 (Camera Core) muss abgeschlossen sein
- Cinemachine 2.10.1 installiert (bereits im Projekt)

---

## Erwartetes Ergebnis

Nach Abschluss von Phase 28:
1. Externe Systeme können `CameraBrain.PushIntent()` aufrufen, um die Kamera zu beeinflussen
2. Designer können Camera-Styles als ScriptableObject-Presets konfigurieren
3. BDO-Style und ArcheAge-Style Presets sind als Beispiel verfügbar
4. Orbit-Pivot verschiebt sich dynamisch bei Bewegung (Forward Bias)
5. Kamera biased sanft Richtung LookTarget (Soft Targeting)
6. Optional: Cinemachine kann als Rendering-Schicht verwendet werden

---

## Nächste Phase

→ [Phase 29: Shared Orientation & Facing Integration](../phase-29-orientation-facing/README.md)
