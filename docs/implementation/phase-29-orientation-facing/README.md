# Phase 29: Shared Orientation & Facing Integration

> **Status:** Ausgearbeitet
> **Branch:** `integration/phase-29-orientation-facing`
> **Abhängigkeiten:** Phase 28 (Camera Intents, muss abgeschlossen sein), Phase 4 (Locomotion)

---

## Ziel

**Camera↔Character Entkopplung** über ein Shared Orientation & Facing System. Damit werden alle 4 offenen Kamera-Punkte gelöst:

1. **ClassicMMO-Steuerung** — FreeOrbit/SteerOrbit mit korrektem Frame-Space und Character-Facing
2. **LookAt-Reparatur** — IK-System an neues Kamera-System anbinden
3. **Einstellbare Sensitivität** — Runtime-konfigurierbare Sensitivity mit FOV-Scaling
4. **Fehlende Kamera-Features** — Inventory der verbleibenden Lücken

### Kernproblem (Warum der erste ClassicMMO-Versuch scheiterte)

Der reverted Commit (`7b87831` → `8a6d44f`) nutzte `IsSteerMode` als Frame-Space-Selektor:

```csharp
// FALSCH — bricht ActionCombat
Vector3 lookDir = isSteerMode ? cameraForward : characterForward;
```

**Root Cause:** `AlwaysOn` (ActionCombat) gibt **immer** `FreeOrbit` zurück, nie `SteerOrbit`. Damit war `IsSteerMode` immer `false` → ActionCombat lief plötzlich im Character-Frame statt Camera-Frame.

**Lektion:** `IsSteerMode` ist ein **Facing-Intent** (Character soll zur Kamera rotieren), kein **Frame-Space-Selektor** (in welchem Koordinatensystem wird Input interpretiert). Diese beiden Konzepte müssen sauber getrennt werden.

---

## Architektur

### Neue Abstraktionsschicht

```
                    ┌──────────────────────┐
                    │     CameraBrain      │
                    │  (OrbitMode, Forward) │
                    └──────────┬───────────┘
                               │ liest
                    ┌──────────▼───────────┐
                    │ CameraOrientation-   │
                    │ Provider             │
                    │ (IOrientationProvider │
                    │  + IFacingProvider)   │
                    └──────────┬───────────┘
                               │ liefert
              ┌────────────────┼─────────────────┐
              │                │                  │
    ┌─────────▼──────┐  ┌─────▼──────┐  ┌───────▼────────┐
    │ PlayerController│  │  LookAtIK  │  │ (Zukunft:      │
    │ (Movement Frame)│  │ (Facing    │  │  Combat,       │
    │                 │  │  Target)   │  │  Abilities...) │
    └─────────┬──────┘  └────────────┘  └────────────────┘
              │
    ┌─────────▼──────────┐
    │ CharacterLocomotion │
    │ (Facing Rotation)   │
    └─────────────────────┘
```

### Die 3 getrennten Verantwortungen

| Verantwortung | Interface | Beschreibung |
|---------------|-----------|-------------|
| **Movement Frame** | `IOrientationProvider` | In welchem Koordinatensystem wird WASD interpretiert? |
| **Character Facing** | `IFacingProvider` | Wohin soll der Character rotieren? |
| **Camera Rotation** | CameraBrain (besteht) | Wohin zeigt die Kamera? (unverändert) |

---

## Design-Entscheidungen

### D1: Frame-Space pro Kamera-Modus

| Preset | OrbitMode | Movement Frame | Character Facing |
|--------|-----------|---------------|-----------------|
| **ActionCombat** (AlwaysOn) | FreeOrbit (immer) | **Camera** | Movement-Richtung |
| **ClassicMMO** (ButtonActivated) | None (kein Button) | **Character** | Movement-Richtung |
| **ClassicMMO** (ButtonActivated) | FreeOrbit (LMB) | **Character** | Movement-Richtung |
| **ClassicMMO** (ButtonActivated) | SteerOrbit (RMB) | **Camera** | Camera-Forward |

**Begründung:**
- **ActionCombat** = Kamera steuert immer (BDO, TERA, GW2). W = Richtung Kamera.
- **ClassicMMO None/FreeOrbit** = Character-Frame (WoW, FFXIV). W = Character-Vorwärts. Kamera orbitet unabhängig.
- **ClassicMMO SteerOrbit** = Camera-Frame + Character folgt (WoW RMB). W = Kamera-Vorwärts, Character dreht sich zur Kamera.

**Entscheidungslogik im Provider:**

```csharp
bool UseCameraFrame =
    orbitActivation == AlwaysOn                          // ActionCombat: immer Camera
    || currentOrbitMode == CameraOrbitMode.SteerOrbit;   // SteerOrbit: immer Camera
// Sonst: Character-Frame
```

### D2: Interface-Lokation (Dependency Inversion)

```
CC.Core  ◄─── Camera.Core
(definiert Interfaces)    (implementiert Provider)
```

- **Interfaces** (`IOrientationProvider`, `IFacingProvider`, `FacingMode`) leben in **CC.Core**
- **CameraOrientationProvider** (implementiert beide) lebt in **Camera.Core**
- Camera.Core bekommt asmdef-Referenz auf CC.Core (nur für Interface-Typen)
- PlayerController (CC.Core) resolved Interface per `GetComponentInParent<>()` — keine Abhängigkeit auf Camera.Core

**Begründung:** Consumer (CC.Core) definiert was er braucht. Provider (Camera.Core) liefert. Dependency Inversion Principle. Gleiche Richtung wie der reverted Versuch, aber saubere Abstraktion statt direktem `ICameraOrbitProvider`.

### D3: Separate Bridge-Komponente statt CameraBrain-Erweiterung

`CameraOrientationProvider` ist eine **eigene MonoBehaviour**, nicht Teil von CameraBrain.

**Begründung:**
- CameraBrain bleibt fokussiert auf Kamera-Logik
- Provider kann unabhängig getestet werden
- Kann entfernt werden, ohne CameraBrain zu brechen (z.B. für reine Kamera-Szenen ohne Character)
- Sitzt auf dem CameraBrain-GameObject, liest per `[RequireComponent]` oder Serialized Reference

### D4: ICameraInputStrategy statt if/else in Pipeline

`CameraInputPipeline.DetermineOrbitMode()` enthält aktuell ein if/else auf `OrbitActivation`:

```csharp
// AKTUELL — if/else für jeden Mode
private CameraOrbitMode DetermineOrbitMode()
{
    if (_orbitActivation == OrbitActivation.AlwaysOn)
        return CameraOrbitMode.FreeOrbit;
    // ButtonActivated...
    if (_isGamepad) return CameraOrbitMode.FreeOrbit;
    if (steerHeld) return CameraOrbitMode.SteerOrbit;
    if (freeLookHeld) return CameraOrbitMode.FreeOrbit;
    return CameraOrbitMode.None;
}
```

**Refactoring zu Strategy Pattern:** Mode-spezifische Logik wird in austauschbare `ICameraInputStrategy`-Implementierungen extrahiert. Neue Modes (Mount-Kamera, Glide-Kamera) erfordern dann nur eine neue Strategy-Klasse, keinen Eingriff in die Pipeline.

**Begründung:**
- Open/Closed Principle — Pipeline bleibt geschlossen für Modifikation, offen für Erweiterung
- Auch `ProcessInput()`, `UpdateCursorState()` und `OnEnable()` haben mode-spezifische Branches — die Strategy kapselt alles
- Preset-Wechsel (`SetPreset()`) injiziert die passende Strategy → kein Runtime-Branching

### D5: FacingMode über LocomotionInput (nicht direkte Provider-Abfrage)

CharacterLocomotion fragt **nicht** direkt den IFacingProvider ab. Stattdessen füllt PlayerController die `LocomotionInput`-Struct:

```csharp
public struct LocomotionInput
{
    public Vector2 MoveDirection;       // Raw WASD
    public Vector3 LookDirection;       // Movement-Frame Forward (war: Camera Forward)
    public float SpeedModifier;
    public bool StepDetectionEnabled;
    public float DecelerationOverride;

    // NEU
    public FacingMode FacingMode;       // Wie soll Character rotieren?
    public Vector3 FacingDirection;     // Ziel für CameraForward-Modus
}
```

**Begründung:** CharacterLocomotion bleibt entkoppelt — liest nur Struct-Daten. PlayerController ist der Integrationspunkt, der Interfaces auflöst und Daten zusammenführt.

---

## Interfaces

### IOrientationProvider

```csharp
namespace Wiesenwischer.GameKit.CharacterController.Core
{
    /// <summary>
    /// Liefert den Referenzraum für Movement-Input-Interpretation.
    /// Implementierungen: CameraOrientationProvider (Camera-Package),
    /// VehicleOrientationProvider (Mount-Package, Zukunft).
    /// </summary>
    public interface IOrientationProvider
    {
        /// <summary>
        /// Forward-Richtung für Movement-Input.
        /// Camera-Frame: Camera Forward (Y=0, normalisiert).
        /// Character-Frame: Character Forward.
        /// </summary>
        Vector3 GetMovementForward();

        /// <summary>
        /// Right-Richtung für Movement-Input.
        /// </summary>
        Vector3 GetMovementRight();
    }
}
```

### IFacingProvider

```csharp
namespace Wiesenwischer.GameKit.CharacterController.Core
{
    /// <summary>
    /// Bestimmt, wie der Character rotiert/facing bestimmt wird.
    /// Getrennt von IOrientationProvider, da Facing und Frame-Space
    /// unabhängige Konzepte sind.
    /// </summary>
    public interface IFacingProvider
    {
        /// <summary>Aktueller Facing-Modus.</summary>
        FacingMode GetFacingMode();

        /// <summary>
        /// Zielrichtung für CameraForward/TargetLockOn Modi.
        /// Nur relevant wenn FacingMode != MovementDirection.
        /// </summary>
        Vector3 GetFacingDirection();
    }
}
```

### FacingMode

```csharp
namespace Wiesenwischer.GameKit.CharacterController.Core
{
    /// <summary>
    /// Bestimmt, was die Character-Rotation antreibt.
    /// </summary>
    public enum FacingMode
    {
        /// <summary>
        /// Character rotiert in Bewegungsrichtung (aktuelles Default-Verhalten).
        /// Funktioniert sowohl im Camera-Frame als auch Character-Frame.
        /// </summary>
        MovementDirection,

        /// <summary>
        /// Character alignt sich zur Kamera-Vorwärtsrichtung (ClassicMMO SteerOrbit).
        /// Character dreht sich auch ohne Bewegung zur Kamera.
        /// </summary>
        CameraForward,

        /// <summary>
        /// Character schaut zum Lock-On Target (Zukunft: Combat).
        /// </summary>
        TargetLockOn,

        /// <summary>
        /// Keine Facing-Änderung. Character behält aktuelle Rotation.
        /// </summary>
        None
    }
}
```

---

## ICameraInputStrategy (Pipeline Refactoring)

### Interface

```csharp
namespace Wiesenwischer.GameKit.Camera
{
    /// <summary>
    /// Strategie für mode-spezifische Input-Interpretation.
    /// Wird vom CameraPreset bestimmt und in die Pipeline injiziert.
    /// Ersetzt das if/else auf OrbitActivation in DetermineOrbitMode().
    /// </summary>
    public interface ICameraInputStrategy
    {
        /// <summary>Bestimmt OrbitMode basierend auf Button-State.</summary>
        CameraOrbitMode DetermineOrbitMode(bool freeLookHeld, bool steerHeld, bool isGamepad);

        /// <summary>Soll Look-Input gelesen werden?</summary>
        bool ShouldReadLookInput(CameraOrbitMode mode);

        /// <summary>Initialer Cursor-State beim Aktivieren der Strategy.</summary>
        CursorLockMode InitialCursorState { get; }

        /// <summary>Cursor-State basierend auf aktuellem OrbitMode.</summary>
        CursorLockMode GetCursorState(CameraOrbitMode mode);
    }
}
```

### Implementierungen

**AlwaysOn (ActionCombat):**

```csharp
public class AlwaysOnInputStrategy : ICameraInputStrategy
{
    public CameraOrbitMode DetermineOrbitMode(bool freeLookHeld, bool steerHeld, bool isGamepad)
        => CameraOrbitMode.FreeOrbit;

    public bool ShouldReadLookInput(CameraOrbitMode mode) => true;

    public CursorLockMode InitialCursorState => CursorLockMode.Locked;

    public CursorLockMode GetCursorState(CameraOrbitMode mode) => CursorLockMode.Locked;
}
```

**ButtonActivated (ClassicMMO):**

```csharp
public class ButtonActivatedInputStrategy : ICameraInputStrategy
{
    public CameraOrbitMode DetermineOrbitMode(bool freeLookHeld, bool steerHeld, bool isGamepad)
    {
        if (isGamepad) return CameraOrbitMode.FreeOrbit;
        if (steerHeld) return CameraOrbitMode.SteerOrbit;
        if (freeLookHeld) return CameraOrbitMode.FreeOrbit;
        return CameraOrbitMode.None;
    }

    public bool ShouldReadLookInput(CameraOrbitMode mode)
        => mode != CameraOrbitMode.None;

    public CursorLockMode InitialCursorState => CursorLockMode.None;

    public CursorLockMode GetCursorState(CameraOrbitMode mode)
        => mode != CameraOrbitMode.None ? CursorLockMode.Locked : CursorLockMode.None;
}
```

### Pipeline nach Refactoring

```csharp
public class CameraInputPipeline : MonoBehaviour
{
    private ICameraInputStrategy _strategy = new AlwaysOnInputStrategy();

    /// <summary>Strategy austauschen (wird von CameraBrain bei SetPreset aufgerufen).</summary>
    public ICameraInputStrategy Strategy
    {
        get => _strategy;
        set
        {
            _strategy = value ?? new AlwaysOnInputStrategy();
            UpdateCursorState(_strategy.InitialCursorState);
        }
    }

    public CameraInputState ProcessInput(float deltaTime)
    {
        bool freeLookHeld = _freeLookAction?.IsPressed() ?? false;
        bool steerHeld = _steerAction?.IsPressed() ?? false;

        // Strategy bestimmt Mode — kein if/else mehr
        CameraOrbitMode orbitMode = _strategy.DetermineOrbitMode(freeLookHeld, steerHeld, _isGamepad);
        UpdateCursorState(_strategy.GetCursorState(orbitMode));

        Vector2 rawLook = Vector2.zero;
        if (_strategy.ShouldReadLookInput(orbitMode))
            rawLook = _lookAction?.ReadValue<Vector2>() ?? Vector2.zero;

        // ... Rest der Pipeline (Deadzone, Acceleration, Smoothing) bleibt unverändert
    }
}
```

### Strategy-Wechsel über CameraBrain.SetPreset()

```csharp
// In CameraBrain.SetPreset():
_inputPipeline.Strategy = preset.OrbitActivation switch
{
    OrbitActivation.AlwaysOn => new AlwaysOnInputStrategy(),
    OrbitActivation.ButtonActivated => new ButtonActivatedInputStrategy(),
    _ => new AlwaysOnInputStrategy()
};
```

### Erweiterbarkeit (Zukunft)

| Mode | Strategy | Beschreibung |
|------|----------|-------------|
| Action Combat | `AlwaysOnInputStrategy` | Maus = immer Kamera |
| Classic MMO | `ButtonActivatedInputStrategy` | LMB/RMB gesteuert |
| Mount Camera | `MountInputStrategy` | Ggf. eingeschränkte Orbit-Achsen |
| Photo Mode | `PhotoModeInputStrategy` | Langsame, präzise Steuerung |
| Cutscene | `DisabledInputStrategy` | Kein Input |

---

## CameraOrientationProvider (Bridge)

```csharp
namespace Wiesenwischer.GameKit.Camera.Core
{
    /// <summary>
    /// Bridge zwischen Camera-System und Character-Controller.
    /// Übersetzt CameraBrain-State in IOrientationProvider/IFacingProvider Semantik.
    /// Sitzt auf dem CameraBrain-GameObject.
    /// </summary>
    [RequireComponent(typeof(CameraBrain))]
    public class CameraOrientationProvider : MonoBehaviour,
        IOrientationProvider, IFacingProvider
    {
        private CameraBrain _brain;

        public Vector3 GetMovementForward()
        {
            bool useCameraFrame =
                _brain.OrbitActivation == OrbitActivation.AlwaysOn
                || _brain.CurrentOrbitMode == CameraOrbitMode.SteerOrbit;

            if (useCameraFrame)
            {
                // Camera Forward (Y=0, normalisiert)
                return _brain.Forward;
            }
            else
            {
                // Character Forward
                var target = _brain.FollowTarget;
                return target != null ? target.forward : Vector3.forward;
            }
        }

        public Vector3 GetMovementRight()
        {
            // Abgeleitet von Forward
            Vector3 forward = GetMovementForward();
            return Vector3.Cross(Vector3.up, forward).normalized;
        }

        public FacingMode GetFacingMode()
        {
            if (_brain.OrbitActivation == OrbitActivation.AlwaysOn)
                return FacingMode.MovementDirection;

            // ButtonActivated (ClassicMMO)
            return _brain.CurrentOrbitMode switch
            {
                CameraOrbitMode.SteerOrbit => FacingMode.CameraForward,
                _ => FacingMode.MovementDirection
            };
        }

        public Vector3 GetFacingDirection()
        {
            return _brain.Forward;
        }
    }
}
```

### Benötigte CameraBrain API-Erweiterungen

CameraBrain muss folgende Properties exponieren (teilweise schon vorhanden):

| Property | Typ | Status |
|----------|-----|--------|
| `IsSteerMode` | `bool` | ✅ Existiert |
| `Forward` | `Vector3` | ✅ Existiert |
| `FollowTarget` | `Transform` | ⚠️ Über Context zugänglich, braucht public Property |
| `OrbitActivation` | `OrbitActivation` | ❌ Neu — aus aktuellem Preset lesen |
| `CurrentOrbitMode` | `CameraOrbitMode` | ❌ Neu — aus aktuellem InputState lesen |

---

## CharacterLocomotion Facing-Logik

### Neues UpdateRotation() mit FacingMode

```csharp
public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
{
    switch (_currentInput.FacingMode)
    {
        case FacingMode.MovementDirection:
            RotateTowardsMovement(ref currentRotation, deltaTime);
            break;

        case FacingMode.CameraForward:
            RotateTowardsDirection(_currentInput.FacingDirection,
                                  ref currentRotation, deltaTime);
            break;

        case FacingMode.TargetLockOn:
            RotateTowardsDirection(_currentInput.FacingDirection,
                                  ref currentRotation, deltaTime);
            break;

        case FacingMode.None:
            // Keine Rotation — Character behält aktuelle Ausrichtung
            break;
    }
}

private void RotateTowardsMovement(ref Quaternion currentRotation, float deltaTime)
{
    // Bestehendes Verhalten: Rotiere in Bewegungsrichtung
    if (_config.RotateTowardsMovement &&
        _lastComputedHorizontal.sqrMagnitude > 0.01f)
    {
        Vector3 dir = _lastComputedHorizontal.normalized;
        _targetYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        _currentYaw = Mathf.MoveTowardsAngle(
            _currentYaw, _targetYaw, _config.RotationSpeed * deltaTime);
        currentRotation = Quaternion.Euler(0, _currentYaw, 0);
    }
}

private void RotateTowardsDirection(Vector3 direction,
                                     ref Quaternion currentRotation,
                                     float deltaTime)
{
    if (direction.sqrMagnitude < 0.001f) return;

    Vector3 flat = new Vector3(direction.x, 0, direction.z).normalized;
    _targetYaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
    _currentYaw = Mathf.MoveTowardsAngle(
        _currentYaw, _targetYaw, _config.RotationSpeed * deltaTime);
    currentRotation = Quaternion.Euler(0, _currentYaw, 0);
}
```

**Wichtig:** `RotateTowardsDirection` nutzt die gleiche `RotationSpeed` wie `RotateTowardsMovement`. Für SteerOrbit könnte eine schnellere Rotation gewünscht sein — das kann über einen optionalen `SteerRotationSpeed` in LocomotionConfig gesteuert werden.

---

## CameraInputSettings & Sensitivität

### CameraInputSettings (ScriptableObject)

```csharp
[CreateAssetMenu(menuName = "GameKit/Camera/Input Settings")]
public class CameraInputSettings : ScriptableObject
{
    [Header("Mouse Sensitivity")]
    [Range(0.1f, 10f)] public float MouseSensitivityX = 1f;
    [Range(0.1f, 10f)] public float MouseSensitivityY = 1f;

    [Header("Scroll")]
    [Range(0.1f, 5f)] public float ScrollSensitivity = 1f;

    [Header("Inversion")]
    public bool InvertY;

    [Header("FOV-Based Scaling")]
    [Tooltip("Referenz-FOV für Sensitivity-Skalierung")]
    public float BaseFov = 60f;
    public bool EnableFovScaling = true;
}
```

### Integration in CameraInputPipeline

```csharp
// In ProcessLookInput():
Vector2 scaled = rawInput;
scaled.x *= _settings.MouseSensitivityX;
scaled.y *= _settings.MouseSensitivityY * (_settings.InvertY ? -1f : 1f);

if (_settings.EnableFovScaling)
{
    float fovScale = Mathf.Tan(currentFov * 0.5f * Mathf.Deg2Rad)
                   / Mathf.Tan(_settings.BaseFov * 0.5f * Mathf.Deg2Rad);
    scaled *= fovScale;
}
```

### Runtime-Zugriff

Settings werden als ScriptableObject referenziert und können zur Runtime über UI-Slider modifiziert werden. Da SOs Shared State sind, wirken Änderungen sofort auf alle Konsumenten.

---

## LookAt-System Reparatur

### Problemanalyse

`CameraTargetProvider` nutzt `Camera.main` für Position und Forward. Nach dem Phase-28-Refactoring sitzt die Kamera in der PivotRig-Hierarchie (Root → Yaw → Pitch → Offset → Camera). Mögliche Ursachen:

1. **Camera.main Tag fehlt** — Unity Camera-Component hat kein "MainCamera"-Tag
2. **Transform-Hierarchie** — Camera.main.transform zeigt auf verschachteltes Offset-Pivot statt erwarteter Position
3. **Timing** — LookAtIK läuft in `OnAnimatorIK()`, PivotRig in `LateUpdate()` → möglicher Frame-Versatz

### Lösungsansatz

1. Verifizieren, dass Camera.main korrekt gesetzt ist
2. `CameraTargetProvider` optional auf `IOrientationProvider` umstellen (statt direkt Camera.main)
3. Alternativ: CameraBrain.Forward + CameraBrain.Position als LookAt-Quelle exponieren

---

## Camera-Relative Animation Space (Vorbereitung)

### Ziel

Grundlage für zukünftiges Strafing: World Move Direction → lokale MoveX/MoveZ Animator-Parameter.

### Pipeline

```
IOrientationProvider + Input → WorldMoveDir
WorldMoveDir → InverseTransformDirection → LocalMoveDir
LocalMoveDir → Animator (MoveX, MoveZ, Speed)
```

### AnimationSpaceConverter

```csharp
public static class AnimationSpaceConverter
{
    /// <summary>
    /// Konvertiert World-Move-Direction in Character-lokale Richtung
    /// für Animator-Parameter (MoveX/MoveZ).
    /// </summary>
    public static Vector3 WorldToLocal(Vector3 worldMoveDir, Transform character)
    {
        return character.InverseTransformDirection(worldMoveDir);
    }
}
```

### Animator-Parameter (Zukunft)

| Parameter | Quelle | Verwendung |
|-----------|--------|------------|
| `MoveX` | `localMove.x` | Strafe Left/Right |
| `MoveZ` | `localMove.z` | Forward/Backward |
| `Speed` | `localMove.magnitude` | Blend Tree Speed (besteht) |
| `TurnAngle` | SignedAngle(CharForward, WorldMoveDir) | Turn-Animationen |

**Hinweis:** Aktuell nutzt das System nur `Speed` (1D Blend Tree). MoveX/MoveZ werden vorbereitet, aber erst aktiviert wenn Strafe-Animationen vorhanden sind (siehe Spec Kapitel 23: "Erst stabiler directional Core, dann FacingProvider, dann Strafing-Animationen").

---

## Abgrenzung

### In Phase 29 (diese Phase)

- IOrientationProvider + IFacingProvider Interfaces in CC.Core
- FacingMode Enum
- ICameraInputStrategy Interface + AlwaysOn/ButtonActivated Implementierungen
- CameraInputPipeline Refactoring (Strategy Pattern statt if/else)
- CameraOrientationProvider Bridge-Komponente in Camera.Core
- CameraBrain API-Erweiterungen (OrbitActivation, CurrentOrbitMode exponieren)
- LocomotionInput-Erweiterung (FacingMode, FacingDirection)
- PlayerController Integration (Provider auflösen, LocomotionInput füllen)
- CharacterLocomotion UpdateRotation() mit FacingMode-Switch
- CameraInputSettings SO + CameraInputPipeline Integration (Sensitivity, InvertY, FOV-Scaling)
- LookAt-System Reparatur (CameraTargetProvider)
- AnimationSpaceConverter (Vorbereitung für MoveX/MoveZ)
- Unit Tests + Play Mode Verifikation

### NICHT in Phase 29 (spätere Phasen)

- Strafing-Animationen / 2D Blend Tree (→ wenn Animationen vorhanden)
- TargetFacingProvider / Lock-On System (→ Combat-Phase)
- VehicleOrientationProvider (→ Mount/Glide-Phase)
- MountInputStrategy / PhotoModeInputStrategy (→ jeweilige Feature-Phasen)
- A/D als Turn-Keys im ClassicMMO-Mode (→ Input-Erweiterung, Design-Entscheidung)
- Velocity-Based Camera Rotation / Angular Velocity (→ optionales Pipeline-Upgrade)
- Adaptive Smoothing pro Achse, Flick-Detection (→ optionales Pipeline-Upgrade)

---

## Impact-Analyse

### CC.Core (CharacterController.Core)

| Datei | Änderung | Risiko |
|-------|----------|--------|
| `IOrientationProvider.cs` | **NEU** — Interface | Keins |
| `IFacingProvider.cs` | **NEU** — Interface + FacingMode Enum | Keins |
| `ILocomotionController.cs` | `LocomotionInput` erweitern (FacingMode, FacingDirection) | **Niedrig** — additive Felder |
| `PlayerController.cs` | Provider auflösen, `GetCameraForward()` ersetzen, Input füllen | **Mittel** — zentrale Logik |
| `CharacterLocomotion.cs` | `UpdateRotation()` erweitern mit FacingMode-Switch | **Mittel** — Rotationslogik |

### Camera.Core

| Datei | Änderung | Risiko |
|-------|----------|--------|
| `ICameraInputStrategy.cs` | **NEU** — Strategy Interface | Keins |
| `AlwaysOnInputStrategy.cs` | **NEU** — ActionCombat Strategy | Keins |
| `ButtonActivatedInputStrategy.cs` | **NEU** — ClassicMMO Strategy | Keins |
| `CameraInputPipeline.cs` | Strategy Pattern Refactoring + CameraInputSettings Integration | **Mittel** — Kernlogik ändert sich |
| `CameraOrientationProvider.cs` | **NEU** — Bridge-Komponente | Keins |
| `CameraBrain.cs` | Properties exponieren + Strategy-Wechsel in SetPreset() | **Niedrig** — additive Änderungen |
| `CameraInputSettings.cs` | **NEU** — ScriptableObject | Keins |

### Camera.Core asmdef

| Änderung | Details |
|----------|---------|
| Neue Referenz | `Wiesenwischer.GameKit.CharacterController.Core.Runtime` |
| Grund | Für `IOrientationProvider`, `IFacingProvider`, `FacingMode` Interface-Typen |

### IK Package

| Datei | Änderung | Risiko |
|-------|----------|--------|
| `CameraTargetProvider.cs` | Debugging + Fix, ggf. Umstellung auf Provider | **Niedrig** |

### Bestehende Tests

- Phase-28-Tests bleiben grün (CameraBrain-Erweiterungen sind additiv)
- Phase-4-Tests: LocomotionInput-Erweiterung ist additiv (Default-Werte = bestehendes Verhalten)
- `FacingMode.MovementDirection` als Default → Rückwärtskompatibel

---

## Schritte

| Schritt | Name | Branch | Dateien |
|---------|------|--------|---------|
| [29.1](29.1-orientation-facing-interfaces.md) | IOrientationProvider + IFacingProvider Interfaces | `feat/orientation-facing-interfaces` | IOrientationProvider.cs, IFacingProvider.cs, FacingMode.cs, LocomotionInput.cs |
| [29.2](29.2-input-strategy-refactoring.md) | ICameraInputStrategy + Pipeline Refactoring | `feat/input-strategy-refactoring` | ICameraInputStrategy.cs, AlwaysOnInputStrategy.cs, ButtonActivatedInputStrategy.cs, CameraInputPipeline.cs |
| [29.3](29.3-camera-orientation-provider.md) | CameraOrientationProvider Implementierung | `feat/camera-orientation-provider` | CameraOrientationProvider.cs, CameraBrain.cs, asmdef |
| [29.4](29.4-character-controller-integration.md) | CharacterController Integration | `feat/cc-orientation-integration` | PlayerController.cs, CharacterLocomotion.cs |
| [29.5](29.5-camera-input-settings.md) | CameraInputSettings & Sensitivität | `feat/camera-input-settings` | CameraInputSettings.cs, CameraInputPipeline.cs |
| [29.6](29.6-lookat-reparatur.md) | LookAt-System Reparatur | `feat/lookat-fix` | CameraTargetProvider.cs |
| [29.7](29.7-animation-space.md) | Camera-Relative Animation Space (Vorbereitung) | `feat/animation-space` | AnimationSpaceConverter.cs |
| [29.8](29.8-unit-tests.md) | Unit Tests | `test/orientation-facing-tests` | Tests/ |
| [29.9](29.9-play-mode-verifikation.md) | Play Mode Verifikation | — | — |

---

## Voraussetzungen

- Phase 28 (Camera Intents) **muss abgeschlossen** sein
- Phase 4 (Locomotion) muss abgeschlossen sein
- Aktuelles CameraBrain mit OrbitMode/OrbitActivation Support

---

## Erwartetes Ergebnis

Nach Abschluss von Phase 29:

1. **ClassicMMO funktioniert korrekt:**
   - Kein Button → Cursor frei, Character-Frame Movement
   - LMB → FreeOrbit, Cursor gelockt, Character-Frame Movement
   - RMB → SteerOrbit, Cursor gelockt, Camera-Frame Movement + Character aligned zur Kamera
2. **ActionCombat bleibt unverändert** — Camera-Frame Movement, Movement-Direction Facing
3. **Sensitivität konfigurierbar** — Mouse X/Y, InvertY, FOV-Scaling über ScriptableObject
4. **LookAt funktioniert** — Character-Kopf folgt korrekt der Kamera/Blickrichtung
5. **Erweiterbar** — TargetLockOn, VehicleFrame, Strafing können sauber auf dem System aufbauen

---

## Nächste Phase

→ Phase 9: Combat Abilities (nutzt TargetFacingProvider + Lock-On)
→ Oder: Strafing-Animationen + MoveX/MoveZ Blend Trees (baut auf AnimationSpaceConverter auf)

---

## Referenzen

- [Camera System Spezifikation](../../specs/Camera_System_Spezifikation.md) — Kapitel 17–20, 23
- [Camera OrbitMode Spezifikation](../../specs/Camera_OrbitMode_Spezifikation.md)
- [AAA Camera Settings & Steuerung](../../specs/Wiesenwischer_AAA_Camera_Settings_und_Steuerung.md)
- [Phase 28 README](../phase-28-camera-intents/README.md) — Abgrenzung "NICHT in Phase 28"
- Revert-Commit: `8a6d44f` (Lessons Learned)
