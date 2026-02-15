# Phase 27: Camera Behaviours

> **Status:** Ausgearbeitet
> **Branch:** `integration/phase-27-camera-behaviours`
> **Abhängigkeiten:** Phase 26 (Camera Core, abgeschlossen)

---

## Ziel

Die **eingebettete** Orbit/Zoom/Collision-Logik aus dem CameraBrain in **eigenständige ICameraBehaviour-Implementierungen** extrahieren. Zusätzlich neue AAA-Behaviours implementieren: Inertia (Spring-Damper), Recenter (Auto-Recenter hinter Character) und ShoulderOffset (Over-Shoulder-Ansicht).

Nach Phase 27:
- CameraBrain enthält **keine eigene Kamera-Logik** mehr — alles läuft über ICameraBehaviour[]
- Behaviours sind **frei kombinierbar** — je nach Kamerastil werden unterschiedliche Behaviours aktiviert
- Grundlage für **CameraPresets** (Phase 28), die Behaviour-Konfigurationen als ScriptableObject speichern

---

## Relevante Spezifikation

- [Camera System Spezifikation](../../specs/Camera_System_Spezifikation.md) — Kapitel 8, 10, 12, 13

---

## Neues Package

```
Packages/Wiesenwischer.GameKit.Camera.Behaviours/
├── package.json
├── Runtime/
│   ├── Wiesenwischer.GameKit.Camera.Behaviours.Runtime.asmdef
│   ├── OrbitBehaviour.cs
│   ├── ZoomBehaviour.cs
│   ├── CollisionBehaviour.cs
│   ├── InertiaBehaviour.cs
│   ├── RecenterBehaviour.cs
│   └── ShoulderOffsetBehaviour.cs
└── Tests/
    ├── Wiesenwischer.GameKit.Camera.Behaviours.Tests.asmdef
    └── Runtime/
        ├── OrbitBehaviourTests.cs
        ├── ZoomBehaviourTests.cs
        ├── CollisionBehaviourTests.cs
        ├── InertiaBehaviourTests.cs
        ├── RecenterBehaviourTests.cs
        └── ShoulderOffsetBehaviourTests.cs
```

**Abhängigkeit:** `Camera.Behaviours` → `Camera.Core` (für ICameraBehaviour, CameraState, CameraContext)

---

## Architektur-Überblick

### Behaviour-Reihenfolge im CameraBrain

```
CameraInputPipeline → CameraInputState
                          ↓
CameraBrain.LateUpdate()
  ├── CameraAnchor.UpdateAnchor()
  ├── Context zusammenbauen
  ├── ICameraBehaviour[] iterieren:
  │     1. OrbitBehaviour        → Yaw/Pitch aus Input
  │     2. RecenterBehaviour     → Auto-Yaw hinter Movement
  │     3. ZoomBehaviour         → Distance aus Zoom-Input
  │     4. ShoulderOffsetBehaviour → ShoulderOffset setzen
  │     5. CollisionBehaviour    → Distance clampen bei Hindernissen
  │     6. InertiaBehaviour      → Position-Offset für cinematic lag
  └── PivotRig.ApplyState()
```

Die Reihenfolge ergibt sich aus der Komponenten-Reihenfolge auf dem GameObject (Inspector-Sortierung).

### Behaviour-Konfiguration

Jedes Behaviour hat **eigene SerializeField-Parameter** direkt auf der Komponente (kein zentrales Config-SO). In Phase 28 werden CameraPresets diese Werte per ScriptableObject überschreiben können.

---

## Abgrenzung

### In Phase 27 (diese Phase)
- OrbitBehaviour (Yaw/Pitch, Pitch-Clamp)
- ZoomBehaviour (Distance, Min/Max, SmoothDamp)
- CollisionBehaviour (SphereCast, Snap/Recovery)
- InertiaBehaviour (Spring-Damper für Position-Lag)
- RecenterBehaviour (Auto-Recenter hinter Movement)
- ShoulderOffsetBehaviour (Over-Shoulder, Side-Switch)
- CameraBrain-Refactor (eingebettete Logik entfernen)
- CameraSetupEditor-Update (Behaviours hinzufügen)
- Unit Tests

### NICHT in Phase 27 (spätere Phasen)
- CameraPreset ScriptableObjects (→ Phase 28)
- ICameraIntent System (→ Phase 28)
- Dynamic Orbit Center (→ Phase 28, braucht CameraContext-Erweiterung)
- Soft Targeting (→ Phase 28)
- IOrientationProvider / IFacingProvider (→ Phase 29)
- CinemachineDriver (→ Phase 28)

---

## Betroffene Dateien (aus Phase 26)

| Datei | Änderung |
|-------|----------|
| `CameraBrain.cs` | Eingebettete Logik entfernen, nur ICameraBehaviour[] verwenden |
| `CameraCoreConfig.cs` | Orbit/Zoom/Collision-Felder entfernen (wandern in Behaviours) |
| `CameraSetupEditor.cs` | Standard-Behaviours bei Setup hinzufügen |

---

## Schritte

| Schritt | Name | Dateien |
|---------|------|---------|
| [27.1](27.1-package-orbit.md) | Package-Struktur + OrbitBehaviour | Package, OrbitBehaviour.cs |
| [27.2](27.2-zoom-collision.md) | ZoomBehaviour + CollisionBehaviour | ZoomBehaviour.cs, CollisionBehaviour.cs |
| [27.3](27.3-inertia.md) | InertiaBehaviour | InertiaBehaviour.cs |
| [27.4](27.4-recenter-shoulder.md) | RecenterBehaviour + ShoulderOffsetBehaviour | RecenterBehaviour.cs, ShoulderOffsetBehaviour.cs |
| [27.5](27.5-brain-refactor.md) | CameraBrain Refactor + Editor Update | CameraBrain.cs, CameraCoreConfig.cs, CameraSetupEditor.cs |
| [27.6](27.6-unit-tests.md) | Unit Tests | Tests/ |
