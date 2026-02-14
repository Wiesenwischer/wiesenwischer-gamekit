# Wiesenwischer GameKit — AAA Third-Person Camera System
## Konsolidierte Spezifikation

> Konsolidiert aus:
> - AAA Camera Architecture
> - AAA Camera Klassenstruktur
> - Camera Character Animation Integration
> - CharacterController Camera Facing Integration

---

## 1. Ziel & Designziele

AAA-Level Third-Person Camera System mit:

- **Black Desert Online** Style Camera (Strong follow, Orbit always active, Soft targeting, Shoulder offset, Strong inertia)
- **ArcheAge** Style Camera (Orbit on input, Weak follow, No recenter, Minimal inertia)
- Modularer Aufbau (Unity Packages)
- MMO-fähige Architektur (Camera rein lokal, nie synchronisiert)
- Erweiterbar für Combat, Mount, Glide, Build Mode, First Person, Photo Mode
- Cinemachine nur als Rendering-Layer, nicht als Gameplay-Logik

---

## 2. High-Level Architektur

```
Input System
   ↓
Camera Input Pipeline (Filtering)
   ↓
Camera Brain (Orchestrator)
   ↓
Camera Intent System
   ↓
Behaviour Stack
   ↓
Dynamic Orbit Center
   ↓
Pivot Rig
   ↓
Cinemachine Driver (Rendering)
```

Zusätzlich gekoppelt (indirekt, über Abstraktionen):

```
Shared Orientation System
Movement Frame Space
Character Facing System
Character Controller
```

**Grundregel:** Kamera und Character kommunizieren **nicht direkt**. Stattdessen:

```
CameraBrain
   ↓
Shared Orientation System
   ↓
Movement Controller
   ↓
Character Facing System
```

Damit ist klar getrennt:
- **Kamera** = Sicht & Inputorientierung
- **Movement** = Physisches Bewegen
- **Facing** = Visuelle Ausrichtung / Animation

---

## 3. CameraState

Repräsentiert den finalen Kamerazustand pro Frame.

```csharp
public struct CameraState
{
    public float Yaw;
    public float Pitch;
    public float Distance;
    public Vector3 ShoulderOffset;
    public float Fov;
}
```

---

## 4. CameraContext

Transportiert Runtime-Daten zwischen Systemen innerhalb eines Frames.

```csharp
public class CameraContext
{
    public Transform FollowTarget;
    public Transform LookTarget;
    public CameraInputState Input;
    public Vector3 AnchorPosition;
    public Vector3 OrbitCenter;
    public float DeltaTime;
}
```

---

## 5. CameraBrain

Zentrale Steuerungseinheit der Kamera. Orchestriert alle Subsysteme.

### Aufgaben

- Input abrufen
- Input-Filter anwenden
- Aktive Intents sammeln und nach Priorität auflösen
- Behaviour Stack evaluieren
- Finalen CameraState berechnen
- Cinemachine Driver aktualisieren

### Interface

```csharp
public class CameraBrain : MonoBehaviour
{
    public void SetTarget(Transform followTarget, Transform lookTarget);
    public void SetPreset(CameraPreset preset);
    public void PushIntent(ICameraIntent intent);
}
```

### Camera Mode Interface

```csharp
public interface ICameraMode
{
    void Enter();
    void Exit();
    void Update();
}
```

---

## 6. Camera Input Pipeline

Nicht simples Smoothing — intelligente AAA Input-Verarbeitung.

### Filter-Stufen

1. **Deadzone Filter** — Ignoriert minimale Stick-/Maus-Bewegungen
2. **Acceleration Curve** — Langsame Inputs → präzise, schnelle → schnellere Rotation
3. **Adaptive Smoothing** — Glättet basierend auf Input-Geschwindigkeit
4. **Noise Reduction** — Entfernt Micro-Jitter

### Pipeline

```
Raw Input → Deadzone → Acceleration Curve → Adaptive Smoothing → Noise Reduction → Camera Rotation
```

---

## 7. Camera Intent System

Löst Konflikte zwischen Systemen, die gleichzeitig die Kamera beeinflussen wollen.

Systeme senden Wünsche (Intents):
- Combat (framing, lock-on)
- Movement (forward bias)
- Mount (offset)
- Lock-On (target tracking)
- Ability (spell targeting)
- Glide (distance/angle)

### Interface

```csharp
public interface ICameraIntent
{
    int Priority { get; }
    void Apply(ref CameraState state);
}
```

Intents werden nach Priorität sortiert und nacheinander auf den CameraState angewendet. Höhere Priorität überschreibt oder modifiziert niedrigere.

---

## 8. Behaviour Stack

Camera Modes sind Behaviour-Presets. Behaviours setzen den Intent-Ergebnis-State um.

### Interface

```csharp
public interface ICameraBehaviour
{
    void Update(ref CameraState state, CameraContext ctx);
}
```

### Behaviours

| Behaviour | Aufgabe |
|-----------|---------|
| **OrbitBehaviour** | Yaw/Pitch basierend auf Input |
| **FollowBehaviour** | Kamera folgt Target (mit Lag) |
| **ZoomBehaviour** | Distance (Scroll/Trigger) |
| **RecenterBehaviour** | Auto-Recenter hinter Character |
| **CollisionBehaviour** | Wand-/Hindernis-Vermeidung |
| **InertiaBehaviour** | Physik-basiertes Nachschwingen |
| **ShoulderOffsetBehaviour** | Seitlicher Versatz (Over-Shoulder) |

---

## 9. Camera Anchor

Kamera folgt NICHT direkt dem Character. Der Anchor stabilisiert gegen:
- Animation Noise
- IK-Bewegungen
- Root Motion Jitter

```
Character Movement Root
      ↓
Camera Anchor (smoothed)
      ↓
Camera Rig
```

Camera Anchor basiert auf dem **Movement Root** (PlayerController Transform), NICHT auf Bone-Positionen. Dadurch bleibt die Kamera stabil, auch wenn IK oder Animationen das Mesh bewegen.

---

## 10. Dynamic Orbit Center

Orbit-Pivot ist NICHT die Character-Position. Der Orbit-Center wird dynamisch berechnet:

| Zustand | Orbit Center |
|---------|-------------|
| Idle | Character Position |
| Movement | Leicht voraus (forward bias) |
| Combat | Zwischen Player und Target |

```csharp
orbitCenter = character.position + moveDir * forwardBias;
```

---

## 11. Pivot Rig (AAA Hierarchie)

```
CameraRoot (Inertia)
    └── YawPivot
          └── PitchPivot
                └── OffsetPivot
                      └── Camera
```

### Vorteile

- Getrennte Rotationsachsen (Yaw/Pitch unabhängig)
- Shoulder Offset ohne Rotation zu beeinflussen
- Zoom unabhängig von Rotation
- Ermöglicht cinematic Kamerabewegungen

---

## 12. Camera Inertia

Statt Lerp — physik-inspiriertes Spring-Damper-System:

```csharp
velocity += (target - current) * stiffness;
velocity *= damping;
position += velocity * deltaTime;
```

Ergebnis:
- Gewicht/Masse-Gefühl
- Cinematic Feeling
- Overshooting bei schnellen Bewegungen

---

## 13. Soft Targeting

Keine harte Lock-On Rotation. Stattdessen additive Bias-Rotation:

```
FinalRotation = InputRotation + SoftBias
```

Bias-Quellen:
- Movement Direction (leichtes Vorausschauen)
- Enemy Direction (sanftes Hinschauen zum Gegner)
- Ability Target (Zielrichtung bei Fernkampf/Zauber)

---

## 14. Dual Rotation System

Strikte Trennung:

- **Camera Rotation** — Wohin schaut die Kamera (Spieler-Input)
- **Character Facing Rotation** — Wohin schaut der Character (Movement/Target-abhängig)

Character folgt der Kamera **verzögert** (Slerp mit TurnSpeed), nicht sofort.

---

## 15. Camera Presets (ScriptableObjects)

Presets definieren den gesamten Camera Style als konfigurierbares Asset.

### Black Desert Preset

- Orbit always active
- Strong follow
- Strong inertia
- Soft targeting enabled
- Shoulder offset

### ArcheAge Preset

- Orbit on input only
- Weak follow
- Minimal inertia
- No recenter
- No shoulder offset

Presets ermöglichen Designer-Tuning ohne Code-Änderungen.

---

## 16. Cinemachine Integration

**WICHTIG:** Cinemachine = Rendering Layer. Keine Gameplay-Logik in Cinemachine.

CameraBrain berechnet CameraState → CinemachineDriver setzt die Werte auf die Virtual Camera:

```csharp
// CinemachineDriver (vereinfacht)
vcam.transform.position = CalculatedPosition;
vcam.transform.rotation = CalculatedRotation;
vcam.m_Lens.FieldOfView = state.Fov;
```

---

## 17. Shared Orientation System

Movement liest Orientierung aus einem abstrakten Service, NICHT direkt von der Kamera oder dem Character.

### Interface

```csharp
public interface IOrientationProvider
{
    Vector3 GetForward();
    Vector3 GetRight();
}
```

### Movement Frame Space

Der Frame Space bestimmt den Referenzraum für Movement-Input:

| Frame | Referenz | Verwendung |
|-------|----------|------------|
| **CameraFrame** | Kamera Forward/Right | BDO-Style (Standard) |
| **CharacterFrame** | Character Forward/Right | ArcheAge-Style, Tank Controls |
| **TargetFrame** | Richtung zum Lock-On Target | Combat Lock-On |
| **VehicleFrame** | Mount/Fahrzeug Forward | Reiten, Fliegen |

Movement-Code bleibt identisch — nur der FrameSpace wechselt:

```csharp
Vector3 moveDirWorld =
    orientation.GetForward() * input.y +
    orientation.GetRight()   * input.x;

moveDirWorld = Vector3.ClampMagnitude(moveDirWorld, 1f);
```

---

## 18. Camera ↔ Character Controller Integration

### Bewegungsfluss (clean & testbar)

```
Input → CameraRotation
Input → Move Vector
Move Vector + Orientation → World Move Direction
World Move Direction → Movement (velocity/CharacterController)
World Move Direction → Facing Rotation
```

### Die 3 getrennten Verantwortungen

**A) Camera Rotation (View)**
- Verarbeitet Look-Input
- Steuert Pivot Rig (Yaw/Pitch)
- Liefert "View Forward/Right"
- Dreht **nicht** den Character direkt

**B) Movement Orientation (Frame Space)**
- Movement fragt abstrakte Orientierung an (`IOrientationProvider`)
- Verschiedene FrameSpaces möglich (Camera, Character, Target, Vehicle)

**C) Character Facing Rotation**
- Dreht den Character abhängig von:
  - Move Direction (bei Locomotion)
  - Target Direction (bei Lock-On)
  - Aim Direction (bei Ranged/Combat)
- Mit Slerp/TurnSpeed für AAA-Feeling

### Beispiel Facing

```csharp
if (moveDirWorld.sqrMagnitude > 0.001f)
{
    var desired = Quaternion.LookRotation(moveDirWorld, Vector3.up);
    character.rotation = Quaternion.Slerp(character.rotation, desired, turnSpeed * dt);
}
```

---

## 19. Character Facing System (IFacingProvider)

Löst den Konflikt zwischen Camera-Rotation und Character-Facing.

### Interface

```csharp
public interface IFacingProvider
{
    Vector3 GetFacingDirection();
}
```

### Implementierungen

| Provider | Verhalten | Verwendung |
|----------|-----------|------------|
| **MovementFacingProvider** | Character schaut in Bewegungsrichtung | Standard-Locomotion (aktuelles System) |
| **CameraFacingProvider** | Character schaut Richtung Kamera | BDO-Style Strafing (Erweiterung) |
| **TargetFacingProvider** | Character schaut Gegner an | Lock-On Combat |

### Resolver Pipeline

```
Movement → Move Direction
FacingProvider → Desired Facing
Character Rotation = RotateTowards(current, desired, turnSpeed)
```

**WICHTIG:** Facing basiert NICHT direkt auf Camera-Rotation, sondern immer über den FacingProvider.

---

## 20. Camera-Relative Animation Space

### Problem

Wenn Animation nur Input oder CharacterForward nutzt:
- Kamera drehen → Animation "passt nicht mehr"
- Strafing fühlt sich falsch an
- Combat Drift
- Lock-On bricht Locomotion

### Lösung: Animation arbeitet auf "Movement Direction"

Animation liest nicht Raw Input, sondern die tatsächliche World Move Direction aus dem Orientation System.

### Pipeline

```
Orientation + Input → WorldMoveDir
WorldMoveDir → LocalMoveDir (Character Space)
LocalMoveDir → Animator Parameter (MoveX/MoveZ)
```

### Implementierung

**World → Local:**
```csharp
Vector3 localMove = character.InverseTransformDirection(moveDirWorld);
```

**Parameter (BlendTree):**
```csharp
animator.SetFloat("MoveX", localMove.x);
animator.SetFloat("MoveZ", localMove.z);
animator.SetFloat("Speed", localMove.magnitude);
```

### Ergebnis

- Bewegung & Animation fühlen sich camera-gestützt an (BDO)
- Facing bleibt unabhängig steuerbar
- Lock-On kann auf TargetFrame wechseln, ohne BlendTrees zu zerstören

### Erweiterung: Turn / Lean / Aim

Zusätzliche Animator-Parameter für AAA-Qualität:
- `TurnAngle`: SignedAngle zwischen CharacterForward und MoveDirWorld
- `AimYaw` / `AimPitch`: Aus Kamera-Richtung und Character-Facing

---

## 21. AAA Animation Layer Architektur

Ein Animator Controller, mehrere Layers. Keine neuen Controller für Mount/Glide/Combat.

### Layer-Aufteilung

| Layer | Typ | Inhalt | Avatar Mask |
|-------|-----|--------|-------------|
| **1. Base Locomotion** | Override | Idle/Walk/Run/Strafe BlendTrees, Jump/Fall, Grounded/InAir | Full Body |
| **2. Upper Body** | Override | Cast Spell, Melee Attack, Aim, Shield Block (while moving) | Upper Body Only |
| **3. Additive** | Additive | Lean into turns, Breathing, Combat stance, Recoil | Full Body |
| **4. IK** | — | Foot IK, Hand IK, Weapon alignment (separates Package) | — |
| **5. Override** | Override | Mount Locomotion, Swim, Glide (kompletter Locomotion-Ersatz) | Full Body |

### Base Locomotion Layer — Parameter

- `MoveX`, `MoveZ`, `Speed` (aus AnimationSpaceConverter)
- `Grounded`
- `VerticalVelocity` (optional)
- `TurnAngle` (optional)

### Upper Body Layer — Warum wichtig

Mounted Combat funktioniert, weil:
- Beine: Mount-Ride-Loop (Override Layer)
- Oberkörper: Sword Attack / Spell Cast (Upper Body Layer)

→ Hybride Animation ohne neue Controller.

### Globaler Parameter-Set (Minimal)

- `MoveX`, `MoveZ`, `Speed`
- `Grounded`
- `IsMounted`
- `IsInCombat`
- `AimYaw`, `AimPitch` (optional)
- `TurnAngle` (optional)
- `ActionId` / `UpperBodyState` (für Ability Triggers)

### Ability Triggering

Abilities übernehmen NICHT Base Locomotion. Sie triggern:
- Upper Body States (attack/cast)
- Additive Overlays (recoil)
- Ggf. Override (special moves wie Leap)

### Unity Umsetzungshinweise

- Avatar Masks für Upper Body Layer
- BlendTrees für Locomotion
- StateMachineBehaviours nur sparsam
- Bei großen Systemen: Playables als langfristige Option

---

## 22. MMO / Netzwerk Hinweise

- Kamera ist **rein lokal** — niemals synchronisieren
- Nur Character Rotation/Movement werden über das Netzwerk synchronisiert
- Camera Intent System ist lokal (Client-Only)
- FacingProvider ist lokal (Server sieht nur die resultierende Rotation)

---

## 23. Movement Paradigma — Aktuell vs. Erweiterung

### Aktuell: Directional Movement (stabil)

```
Move Direction == Facing Direction
```

- Character dreht in Bewegungsrichtung
- Kein Strafing notwendig
- Weniger Animationen benötigt
- Stabil für Networking
- Einfache State Machine

### Später: Strafing / BDO Style (Erweiterung)

```
Move Direction ≠ Facing Direction
```

Voraussetzungen bevor Strafing implementiert wird:
- Strafing-Animationen vorhanden
- FacingProvider-Architektur implementiert
- Camera-Konflikt-Resolver funktionsfähig

**Empfohlene Reihenfolge:**
1. Erst stabiler directional Core (aktueller Stand)
2. Dann FacingProvider Interface + CameraFacing
3. Dann Strafing-Animationen + BlendTree-Erweiterung (MoveX/MoveZ)

---

## 24. Package-Struktur

```
Wiesenwischer.GameKit.Camera.Core
├── CameraBrain
├── CameraState
├── CameraContext
├── ICameraMode
├── ICameraIntent
├── ICameraBehaviour
├── CameraInputPipeline
├── PivotRig
├── CameraAnchor
├── CinemachineDriver

Wiesenwischer.GameKit.Camera.Behaviours
├── OrbitBehaviour
├── FollowBehaviour
├── ZoomBehaviour
├── CollisionBehaviour
├── InertiaBehaviour
├── RecenterBehaviour
├── ShoulderOffsetBehaviour
├── DynamicOrbitCenter
├── SoftTargeting

Wiesenwischer.GameKit.Camera.Presets
├── CameraPreset (ScriptableObject)
├── BDOPreset
├── ArcheAgePreset

Wiesenwischer.GameKit.CharacterController.Core (Erweiterung)
├── IOrientationProvider
├── IFacingProvider
├── CameraFrame / CharacterFrame / TargetFrame / VehicleFrame
├── MovementFacingProvider / CameraFacingProvider / TargetFacingProvider
```

---

## 25. Lock-On Integration

Lock-On wechselt **nur** den Orientation/FrameSpace-Provider:

- Vorher: `CameraFrame`
- Während Lock-On: `TargetFrame`

Movement-Code bleibt identisch. Facing kann dann:
- Target-orientiert sein (Character schaut immer Gegner an)
- Oder moveDir-orientiert bleiben (je nach Design)

---

## 26. Mount / Glide Integration

Mount und Glide sind Controller-Wechsel (wie im Core geplant). Die Kamera reagiert über Intents/Presets, Movement bleibt sauber weil der FrameSpace gewechselt wird:

- Mount → VehicleFrame + MountPreset
- Glide → CameraFrame + GlidePreset + Override Animation Layer
