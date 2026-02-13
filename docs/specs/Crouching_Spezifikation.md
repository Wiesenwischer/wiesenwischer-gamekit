# Crouching Spezifikation

> **Version:** 1.0
> **Datum:** 2026-02-13
> **Status:** Entwurf
> **Abhängigkeiten:** Phase 4 (Fortgeschrittene Lokomotion), CharacterController.Core

---

## 1. Motivation & Problemstellung

### 1.1 Aktueller Zustand

Der Character Controller unterstützt aktuell kein Crouching. Die Input-Infrastruktur ist bereits vorbereitet:

- `ControllerButtons.Crouch` existiert in der Prediction-Schicht (`1 << 2`)
- `InputButtons.Crouch` existiert in `IMovementInputProvider` (`1 << 3`)
- **Keine** `CrouchPressed`/`CrouchHeld` Properties in `IMovementInputProvider`
- **Kein** `PlayerCrouchingState` in der State Machine
- **Keine** Capsule-Höhen-Anpassung zur Laufzeit
- **Keine** Animation für Crouching

### 1.2 Ziel

Ein vollständiges **Crouching-System** mit:
- Toggle-basierter Aktivierung (C-Taste)
- Dynamischer Capsule-Höhe (reduziert beim Crouchen)
- Reduzierter Bewegungsgeschwindigkeit
- Stand-Up-Check (Decken-Erkennung verhindert Aufstehen in engen Räumen)
- Dedizierter Crouch-Animation (Idle + Walk Blend Tree)
- Korrekter Integration in die bestehende State Machine

---

## 2. Referenzen aus anderen Spielen

| Spiel | Verhalten |
|-------|-----------|
| **Genshin Impact** | Kein Crouching — nicht relevant für Vergleich |
| **GW2** | Kein Crouching — nicht relevant |
| **Dark Souls / Elden Ring** | Crouch-Toggle, reduzierte Geschwindigkeit, Stealth-Modifier, Character läuft geduckt |
| **Skyrim** | Crouch-Toggle (Ctrl), Stealth-Modus, Geschwindigkeit reduziert, Capsule-Höhe angepasst |
| **Fortnite** | Crouch-Toggle, volle Bewegung möglich (langsamer), Capsule kleiner |

**Empfehlung:** Elden Ring / Skyrim-Ansatz — Toggle-basiert, reduzierte Speed, geduckte Haltung, kein Sprint möglich.

---

## 3. Design-Entscheidungen

### 3.1 Toggle statt Hold

- **C-Taste** togglet Crouching ein/aus (wie Walk-Toggle mit Y)
- Kein Hold erforderlich — Spieler drückt C zum Ducken, nochmal C zum Aufstehen
- Bei Sprint-Input wird Crouch automatisch verlassen (aufstehen + sprinten)

### 3.2 Crouch unter PlayerGroundedState

Anders als Sliding (eigenständig, weil `!IsStableOnGround`) gehört Crouching **unter `PlayerGroundedState`**:
- Character ist stabil auf dem Boden
- Ground Detection, Step Handling und Fall-Checks bleiben aktiv
- Jump aus Crouch ist möglich (steht dabei zuerst auf)

### 3.3 Kein Sprint während Crouch

- Sprint-Input beendet den Crouch-State und wechselt zu Sprinting
- Crouch + Sprint ist nicht möglich

### 3.4 Capsule-Höhe

- Die Capsule-Höhe wird beim Crouchen reduziert (z.B. von 2.0m auf 1.2m)
- Die Capsule bleibt am Boden verankert (YOffset passt sich an)
- Smooth Transition der Höhe über eine konfigurierbare Dauer
- `CharacterMotor.SetCapsuleDimensions()` steuert dies

### 3.5 Stand-Up-Check (Ceiling Detection)

- Vor dem Aufstehen wird geprüft, ob genügend Platz über dem Character ist
- SphereCast / CapsuleCast nach oben mit stehender Capsule-Höhe
- Falls blockiert: Character bleibt im Crouch-State (visuelles Feedback optional)

---

## 4. Architektur

### 4.1 Neuer State: `PlayerCrouchingState`

```
PlayerMovementState (abstract)
├── PlayerGroundedState (abstract)
│   ├── PlayerIdlingState
│   ├── PlayerMovingState (abstract)
│   │   ├── PlayerWalkingState
│   │   ├── PlayerRunningState
│   │   └── PlayerSprintingState
│   ├── PlayerCrouchingState          ← NEU (unter Grounded)
│   ├── PlayerStoppingState (abstract)
│   ├── PlayerSoftLandingState
│   └── PlayerHardLandingState
├── PlayerSlidingState
└── PlayerAirborneState (abstract)
    ├── PlayerJumpingState
    └── PlayerFallingState
```

**Warum ein einzelner State (nicht CrouchIdle + CrouchMoving)?**
- Einfachheit: Idle vs. Moving wird über `MovementSpeedModifier` gesteuert (0 = Idle, > 0 = Moving)
- Der State handhabt beides intern: kein Input → geduckt stehen, Input → geduckt laufen
- Blend Tree im Animator interpoliert zwischen Crouch-Idle und Crouch-Walk basierend auf Speed

### 4.2 State-Verantwortlichkeiten

| Komponente | Verantwortlichkeit |
|------------|-------------------|
| `PlayerCrouchingState` | Entry/Exit-Logik, Capsule-Höhe, Animation, Speed-Modifier |
| `CharacterLocomotion` | Capsule-Transition (Smooth Height Change), `IsCrouching` Property |
| `CharacterMotor` | `SetCapsuleDimensions()` (bereits vorhanden) |
| `LocomotionConfig` | Konfigurierbare Parameter |

### 4.3 Transition-Diagramm

```
                    ┌─────────────────────────────────────────┐
                    │                                         │
                    v                                         │
Idle/Walking ──[C-Taste]──→ Crouching                        │
                              │                               │
                              ├──[C-Taste && CanStandUp]──────┘ → Idle/Walking
                              │
                              ├──[Sprint-Input && CanStandUp]──→ Sprinting
                              │
                              ├──[Jump-Input]──→ Jumping (steht dabei auf)
                              │
                              ├──[Boden verloren]──→ Falling (steht dabei auf)
                              │
                              └──[Slope > MaxAngle]──→ Sliding (steht dabei auf)

Running ──[C-Taste]──→ Crouching (Geschwindigkeit reduziert)

Sprinting ──[C-Taste]──→ Crouching (Geschwindigkeit reduziert)

Landing ──[Crouch aktiv]──→ Crouching (wenn vor dem Sprung gecrouch war)
```

---

## 5. Detailliertes Verhalten

### 5.1 Entry-Bedingungen

Der Character wechselt zu `PlayerCrouchingState` wenn:

1. **Von Idle/Walking/Running:** C-Taste gedrückt → `CrouchTogglePressed`
2. **Von Sprinting:** C-Taste gedrückt → Bremst ab + duckt sich
3. **Von Landing:** Falls Crouch vor dem Sprung aktiv war (optional, konfigurierbar)

**Entry-Check in `PlayerGroundedState.HandleInput()`:**
```csharp
if (ReusableData.CrouchTogglePressed)
{
    ChangeState(stateMachine.CrouchingState);
    return;
}
```

### 5.2 Capsule-Höhen-Transition

**Beim Eintritt in CrouchingState:**
```csharp
// Ziel-Dimensionen berechnen
float crouchHeight = Config.CrouchHeight;       // z.B. 1.2
float crouchYOffset = crouchHeight * 0.5f;       // Boden-verankert
float radius = Player.Locomotion.Motor.Radius;   // Radius bleibt gleich

// Smooth Transition starten
Player.Locomotion.StartCrouchTransition(crouchHeight, crouchYOffset);
```

**Smooth Transition in `CharacterLocomotion.Update()`:**
```csharp
if (_crouchTransitionActive)
{
    _currentCapsuleHeight = Mathf.SmoothDamp(
        _currentCapsuleHeight,
        _targetCapsuleHeight,
        ref _capsuleHeightVelocity,
        _config.CrouchTransitionDuration);

    float yOffset = _currentCapsuleHeight * 0.5f;
    Motor.SetCapsuleDimensions(Motor.Radius, _currentCapsuleHeight, yOffset);

    if (Mathf.Abs(_currentCapsuleHeight - _targetCapsuleHeight) < 0.01f)
    {
        Motor.SetCapsuleDimensions(Motor.Radius, _targetCapsuleHeight, _targetCapsuleHeight * 0.5f);
        _crouchTransitionActive = false;
    }
}
```

### 5.3 Movement während Crouch

- **Geschwindigkeit:** `CrouchSpeed` (konfigurierbar, Default: ~2.5 m/s — zwischen Walk und Run)
- **Acceleration/Deceleration:** Reduziert (konfigurierbar)
- **Rotation:** Normal (gleiche `RotationSpeed` wie Grounded)
- **Step Handling:** Aktiv, aber mit reduzierter Step-Höhe (optional)

**Speed-Modifier im CrouchingState:**
```csharp
// Bei Movement-Input:
float crouchSpeedModifier = Config.CrouchSpeed / Config.WalkSpeed;
ReusableData.MovementSpeedModifier = crouchSpeedModifier;

// Ohne Movement-Input:
ReusableData.MovementSpeedModifier = 0f;
```

### 5.4 Exit-Bedingungen

| Bedingung | Ziel-State | Beschreibung |
|-----------|-----------|--------------|
| C-Taste + `CanStandUp()` | Idle oder Moving | Character steht auf |
| Sprint-Input + `CanStandUp()` | Sprinting | Aufstehen + Sprinten |
| Jump-Input | Jumping | Aufstehen + Springen |
| `!IsStableOnGround` (Kante) | Falling | Aufstehen + Fallen |
| `SlopeAngle > MaxSlopeAngle` | Sliding | Aufstehen + Rutschen |
| C-Taste + `!CanStandUp()` | Crouching (bleibt) | Kann nicht aufstehen (Decke) |

### 5.5 Stand-Up-Check (Ceiling Detection)

```csharp
private bool CanStandUp()
{
    float standingHeight = Config.StandingHeight;   // 2.0
    float currentHeight = Motor.Height;             // 1.2 (crouching)
    float heightDifference = standingHeight - currentHeight;
    float margin = Config.CrouchHeadClearanceMargin; // 0.1

    // CapsuleCast nach oben prüfen
    Vector3 origin = Motor.Transform.position + Motor.CharacterTransformToCapsuleTop;
    bool blocked = Physics.SphereCast(
        origin,
        Motor.Radius - 0.05f,    // Etwas kleiner als Capsule-Radius
        Vector3.up,
        out _,
        heightDifference + margin,
        Config.GroundLayers);

    return !blocked;
}
```

---

## 6. Konfiguration

### 6.1 Neue Parameter

| Parameter | Typ | Default | Beschreibung |
|-----------|-----|---------|--------------|
| `CrouchHeight` | float | 1.2 | Capsule-Höhe beim Crouchen (m) |
| `StandingHeight` | float | 2.0 | Capsule-Höhe im Stehen (m) — entspricht dem Motor-Default |
| `CrouchSpeed` | float | 2.5 | Bewegungsgeschwindigkeit beim Crouchen (m/s) |
| `CrouchAcceleration` | float | 8.0 | Beschleunigung beim Crouchen (m/s²) |
| `CrouchDeceleration` | float | 10.0 | Verzögerung beim Crouchen (m/s²) |
| `CrouchTransitionDuration` | float | 0.25 | Dauer der Capsule-Höhen-Transition (s) |
| `CrouchHeadClearanceMargin` | float | 0.1 | Sicherheitsabstand für Stand-Up-Check (m) |
| `CanJumpFromCrouch` | bool | true | Ob aus dem Crouch gesprungen werden kann |
| `CanSprintFromCrouch` | bool | true | Ob Sprint den Crouch automatisch beendet |
| `CrouchStepHeight` | float | 0.2 | Reduzierte Step-Höhe im Crouch (m), -1 = Motor-Default |

### 6.2 Empfohlene Startwerte

```
CrouchHeight:               1.2     (60% der Standhöhe)
StandingHeight:              2.0     (Motor-Default)
CrouchSpeed:                 2.5     (zwischen Walk 1.5 und Run 4.5)
CrouchAcceleration:          8.0     (etwas träger als normal)
CrouchDeceleration:         10.0
CrouchTransitionDuration:    0.25    (schnell genug, aber sichtbar)
CrouchHeadClearanceMargin:   0.1
CanJumpFromCrouch:          true
CanSprintFromCrouch:        true
CrouchStepHeight:           0.2     (niedrigere Steps als normal)
```

---

## 7. Input

### 7.1 Taste

- **C-Taste** — Toggle Crouch ein/aus
- Kein Hold-Modus (Spieler muss nicht halten)
- Input Action: `Crouch` im Unity Input System (Button-Type, Press-Only)

### 7.2 Input-Pipeline

```
Unity Input System (C-Taste)
  → PlayerInputProvider.CrouchTogglePressed
    → PlayerStateReusableData.CrouchTogglePressed
      → PlayerGroundedState.HandleInput() prüft Toggle
        → ChangeState(CrouchingState)
```

### 7.3 Neue Properties in IMovementInputProvider

```csharp
/// Ob Crouch-Toggle gedrückt wurde (einmalig pro Press).
bool CrouchTogglePressed { get; }
```

### 7.4 Neue Properties in PlayerStateReusableData

```csharp
/// Crouch-Toggle Input (einmalig pro Frame).
public bool CrouchTogglePressed { get; set; }

/// Ob der Character aktuell im Crouch-State ist (für Persistenz über Sprünge).
public bool IsCrouching { get; set; }
```

---

## 8. Animation

### 8.1 Animator-States

Neuer Sub-State oder Blend Tree `Crouch` im Locomotion Layer:

```
Locomotion Layer
├── Locomotion (Blend Tree: Idle/Walk/Run/Sprint)
├── Jump
├── Fall
├── SoftLand / HardLand
├── LightStop / MediumStop / HardStop
├── Slide
└── Crouch (Blend Tree)          ← NEU
    ├── Crouch Idle (Speed = 0)
    └── Crouch Walk (Speed > 0)
```

**Blend Tree:** 1D Blend basierend auf `Speed`-Parameter:
- Speed = 0 → Crouch Idle
- Speed > 0 → Crouch Walk

### 8.2 Animation Assets

Von Mixamo herunterladen:
- **Crouch Idle:** "Crouch Idle" — loopende geduckte Ruhepose
- **Crouch Walk:** "Crouch Walk Forward" oder "Sneaking Forward" — loopende geduckte Bewegung
- **Format:** FBX for Unity, **In Place** aktiviert
- **Loop Time:** Ja (beide Animationen)
- **Root Transform:** Bake Into Pose bei allen Achsen

### 8.3 CharacterAnimationState Erweiterung

```csharp
public enum CharacterAnimationState
{
    Locomotion,
    Jump,
    Fall,
    SoftLand,
    HardLand,
    LightStop,
    MediumStop,
    HardStop,
    Slide,
    Crouch,          // ← NEU
}
```

### 8.4 CrossFade

| Transition | CrossFade-Zeit | Beschreibung |
|-----------|---------------|--------------|
| Idle/Walk → Crouch | 0.25s | Eingleiten in geduckte Haltung |
| Crouch → Idle | 0.25s | Aufstehen aus Crouch |
| Crouch → Run/Sprint | 0.2s | Schnelles Aufstehen + Lossprinten |
| Crouch → Jump | 0.15s | Aufstehen + Absprung |
| Crouch → Falling | 0.15s | Aufstehen beim Fallen |
| Crouch → Slide | 0.2s | Aufstehen + in Slide übergehen |

---

## 9. Integration in CharacterLocomotion

### 9.1 Neue Properties

```csharp
// In CharacterLocomotion:
private bool _isCrouching;
private bool _crouchTransitionActive;
private float _currentCapsuleHeight;
private float _targetCapsuleHeight;
private float _capsuleHeightVelocity;

public bool IsCrouching => _isCrouching;

public void SetCrouching(bool crouching)
{
    _isCrouching = crouching;

    if (crouching)
    {
        _targetCapsuleHeight = _config.CrouchHeight;
    }
    else
    {
        _targetCapsuleHeight = _config.StandingHeight;
    }
    _crouchTransitionActive = true;
}
```

### 9.2 Capsule-Transition in UpdateVelocity/AfterCharacterUpdate

Die Capsule-Höhe wird in `AfterCharacterUpdate()` oder einem separaten Update-Schritt smooth interpoliert (siehe Abschnitt 5.2).

### 9.3 Motor-Interaktion

- **StepHandling:** Aktiv mit reduzierter Step-Höhe (`CrouchStepHeight`)
- **GroundSnapping:** Aktiv (Character ist geerdet)
- **Ground Detection:** Funktioniert automatisch, da `SetCapsuleDimensions()` die Raycast-Origins aktualisiert

---

## 10. Betroffene Dateien

### 10.1 Neue Dateien

| Datei | Beschreibung |
|-------|-------------|
| `States/PlayerCrouchingState.cs` | Neuer State |
| `Anim_CrouchIdle.fbx` | Crouch-Idle-Animation |
| `Anim_CrouchWalk.fbx` | Crouch-Walk-Animation |

### 10.2 Zu ändernde Dateien

| Datei | Änderung |
|-------|---------|
| `IMovementInputProvider.cs` | `CrouchTogglePressed` Property |
| `PlayerInputProvider.cs` | Input Action Anbindung für C-Taste |
| `PlayerStateReusableData.cs` | `CrouchTogglePressed`, `IsCrouching` |
| `PlayerMovementStateMachine.cs` | `CrouchingState` Property + Instanziierung |
| `PlayerGroundedState.cs` | Crouch-Toggle-Check in `HandleInput()` |
| `CharacterLocomotion.cs` | `IsCrouching`, Capsule-Transition-Logik |
| `ILocomotionConfig.cs` | Neue Config-Properties |
| `LocomotionConfig.cs` | Neue SerializeFields + Editor-Defaults |
| `LocomotionConfigEditor.cs` | UI-Sektion für Crouch-Parameter |
| `IAnimationController.cs` | `CharacterAnimationState.Crouch` |
| `AnimatorParameterBridge.cs` | Crouch-State Hash + Mapping |
| `AnimationTransitionConfig.cs` | Crouch-CrossFade-Dauer |
| `AnimationWizard.cs` | Crouch-Animation-Slots |
| `CharacterAnimatorController` | Crouch Blend Tree im Animator |
| `PlayerInputActions.inputactions` | Crouch-Action auf C-Taste |

### 10.3 Bestehende Dateien (unverändert nutzen)

| Datei | Nutzung |
|-------|---------|
| `CharacterMotor.cs` | `SetCapsuleDimensions()` für dynamische Capsule-Höhe |
| `ControllerInput.cs` | `ControllerButtons.Crouch` (bereits vorhanden) |
| `InputButtons` Enum | `Crouch = 1 << 3` (bereits vorhanden) |

---

## 11. Tests

### 11.1 Unit Tests

| Test | Beschreibung |
|------|-------------|
| `CrouchingState_EntersOnToggle` | C-Taste → Crouch aktiviert |
| `CrouchingState_ExitsOnToggle` | C-Taste erneut → Crouch deaktiviert |
| `CrouchingState_CannotStandUpUnderCeiling` | Stand-Up-Check blockiert bei Decke |
| `CrouchingState_SpeedIsReduced` | Crouch-Speed < Normal-Speed |
| `CrouchingState_CapsuleHeightReduced` | Capsule-Höhe = CrouchHeight |
| `CrouchingState_CapsuleHeightRestoredOnExit` | Capsule-Höhe zurück auf StandingHeight |
| `CrouchingState_SprintExitsCrouch` | Sprint-Input beendet Crouch (wenn CanSprintFromCrouch) |
| `CrouchingState_JumpExitsCrouch` | Jump aus Crouch steht auf + springt |
| `CrouchingState_FallingExitsCrouch` | Über Kante → Aufstehen + Fallen |
| `CrouchingState_MovementWorks` | Bewegung mit reduzierter Speed funktioniert |
| `CrouchingState_ToggleIsOneShot` | Toggle feuert nur einmal pro Tastendruck |

### 11.2 Manuelle Tests

- [ ] C-Taste togglet Crouching ein/aus
- [ ] Character-Capsule wird sichtbar kleiner (Debug-Visualisierung)
- [ ] Crouch-Animation (Idle + Walk) wird korrekt abgespielt
- [ ] Geschwindigkeit ist reduziert (CrouchSpeed vs. RunSpeed)
- [ ] Unter Decke: Character kann nicht aufstehen
- [ ] Sprint-Input beendet Crouch und Character sprintet
- [ ] Jump aus Crouch: Aufstehen + Springen
- [ ] Über Kante fallen: Aufstehen + Falling-State
- [ ] Smooth Capsule-Transition (kein Teleport der Höhe)
- [ ] Kein Performance-Overhead im normalen Grounded-State
- [ ] Kein GC-Allocation im Crouch-Path

---

## 12. Offene Fragen

1. **Stealth-System:** Soll Crouching einen Stealth-Modifier aktivieren (z.B. reduzierte Mob-Erkennung)? → Späteres Feature, nicht Teil dieser Spezifikation
2. **Crouch + Slide:** Soll der Character auch im Crouch rutschen können (z.B. Baseball-Slide)? → Aktuell: Aufstehen bei Slide-Transition
3. **Netzwerk:** Crouch-State muss synchronisiert werden → Phase 6/7
4. **Crouch-Jump:** Soll die Sprunghöhe aus Crouch reduziert sein? → Optional, Config-Parameter möglich
5. **Prone (Liegen):** Soll es zusätzlich zum Crouch einen Prone-State geben? → Späteres Feature
