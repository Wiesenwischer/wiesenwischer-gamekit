# Landing Roll — Spezifikation

> **Version:** 1.0
> **Datum:** 2026-02-13
> **Status:** Entwurf
> **Abhängigkeiten:** Phase 4 (Fortgeschrittene Lokomotion — Landing System)
> **Inspiriert von:** Genshin Impact Movement System (PlayerRollingState)

---

## 1. Übersicht

Der **Landing Roll** ist ein neuer Landing-State, der zwischen SoftLanding und HardLanding eingeordnet wird. Er ermöglicht dem Character, bei einem harten Aufprall durch eine Rolle Momentum beizubehalten und sich schneller zu erholen als bei einer HardLanding.

### Kernkonzept

```
Falling (hoher Fall)
  ├─ Kein Input          → HardLanding (Vollstopp, Recovery)
  ├─ Movement-Input      → LandingRoll (Rolle vorwärts, Momentum beibehalten)
  └─ Niedriger Fall      → SoftLanding (sofort weiter)
```

**Genshin Impact Referenz:** Im Genshin-Controller wird der Roll automatisch ausgelöst, wenn der Spieler bei einem hohen Sturz Movement-Input hält. Der Character rollt in Bewegungsrichtung und geht danach direkt in den laufenden State über — anstatt wie bei HardLanding komplett stehen zu bleiben.

---

## 2. Design-Entscheidungen

### 2.1 Scope

| Feature | Status | Begründung |
|---------|--------|------------|
| Landing Roll | ✅ Implementieren | Kernfeature dieser Spec |
| Aktiver Dodge (Input-getriggert) | ❌ Separate Spec | Eigenständige Mechanik mit anderem Scope |
| I-Frames (Schadens-Immunität) | ❌ Vorbereitet | Hook für späteres Combat-System |
| Stamina-Kosten | ❌ Vorbereitet | Hook für späteres Ressourcen-System |

### 2.2 Trigger-Modus (konfigurierbar)

Der Roll kann auf zwei Arten ausgelöst werden — wählbar per `RollTriggerMode` in der Config:

| Modus | Beschreibung | Use Case |
|-------|-------------|----------|
| `MovementInput` | Roll wird automatisch ausgelöst, wenn Movement-Input bei Landung aktiv ist | Genshin-Style, casual-friendly |
| `ButtonPress` | Roll nur bei gehaltener Dodge/Roll-Taste während der Landung | Skill-basiert, Souls-like |

**Default:** `MovementInput` (wie Genshin Impact)

### 2.3 Richtung

**Omni-Directional** — Die Roll-Richtung basiert auf dem aktuellen Movement-Input (kamerarelativ):
- Mit Movement-Input: Roll in Input-Richtung (8-Wege)
- Ohne Input (nur im `ButtonPress`-Modus relevant): Roll in Blickrichtung

---

## 3. State Machine Integration

### 3.1 State-Hierarchie (erweitert)

```
PlayerGroundedState
  └── ... (bestehende States)
  └── PlayerRollingState  ← NEU (Sibling zu SoftLanding/HardLanding)
```

**Vererbung:** `PlayerRollingState : PlayerGroundedState`

Der RollingState erbt von `PlayerGroundedState` (nicht von einer Landing-Basisklasse), da er eigenständige Bewegungslogik hat und nicht die SoftLanding/HardLanding-Recovery-Patterns teilt.

### 3.2 Transition-Diagramm

```
                          ┌──────────────────────────────┐
                          │       PlayerFallingState      │
                          └──────────┬───────────────────┘
                                     │ OnContactWithGround()
                                     │
                    ┌────────────────┼────────────────────┐
                    │                │                     │
            landingSpeed <     landingSpeed ≥         landingSpeed ≥
            HardThreshold    HardThreshold            HardThreshold
                    │          + RollCondition           + !RollCondition
                    │                │                     │
                    v                v                     v
            ┌──────────┐    ┌──────────────┐     ┌──────────────┐
            │ SoftLand │    │ RollingState │     │  HardLand    │
            └──────────┘    └──────┬───────┘     └──────────────┘
                                   │
                          Animation fertig
                                   │
                    ┌──────────────┼──────────────┐
                    │                              │
              Movement Input                 Kein Input
                    │                              │
                    v                              v
            ┌──────────────┐              ┌──────────────┐
            │ Walk/Run/    │              │ MediumStop   │
            │ Sprint       │              └──────────────┘
            └──────────────┘
```

### 3.3 Roll-Bedingung (RollCondition)

```
RollCondition =
  WENN TriggerMode == MovementInput:
    ReusableData.MoveInput != Vector2.zero

  WENN TriggerMode == ButtonPress:
    ReusableData.DashPressed == true
    (Movement-Input optional, Fallback: Blickrichtung)
```

### 3.4 Blockierte Inputs während Roll

| Input | Verhalten | Begründung |
|-------|-----------|------------|
| Jump | Blockiert | Verhindert Jump-Cancel der Roll-Animation |
| Sprint | Deaktiviert (`ShouldSprint = false`) | Roll ist nicht mit Sprint kombinierbar |
| Movement | Lesend (für Rotation) | Erlaubt Richtungswechsel während Roll |
| Dash/Roll | Blockiert | Kein Re-Trigger während Roll |

---

## 4. Bewegungsphysik

### 4.1 Roll-Geschwindigkeit

```csharp
float rollSpeed = Config.RunSpeed * Config.RollSpeedModifier;
// Default: 6.0 * 1.0 = 6.0 m/s (= Run Speed)
```

- **RollSpeedModifier** (0.5–2.0, Default: 1.0): Multipliziert mit `RunSpeed`
- Die Geschwindigkeit wird zu Beginn des Rolls gesetzt und bleibt konstant
- Kein Beschleunigen/Abbremsen während der Roll-Animation

### 4.2 Roll-Richtung & Rotation

```csharp
// Bei Enter:
Vector3 rollDirection = GetCameraRelativeMovementDirection();
if (rollDirection == Vector3.zero)
    rollDirection = Player.transform.forward; // Fallback: Blickrichtung

// Rotation sofort in Roll-Richtung (kein SmoothDamp)
Player.transform.rotation = Quaternion.LookRotation(rollDirection);

// Geschwindigkeit setzen via Intent-System
ReusableData.MovementSpeedModifier = Config.RollSpeedModifier;
```

### 4.3 Momentum nach Roll

Wenn der Roll endet und Movement-Input aktiv ist, wird der Übergang nahtlos:
- → Walk/Run/Sprint je nach aktuellen Input-Flags
- Keine abrupte Geschwindigkeitsänderung (Speed Modifier wird vom Folge-State übernommen)

---

## 5. Animation

### 5.1 Neuer Animator-State

| Property | Wert |
|----------|------|
| State Name | `Roll` |
| Layer | Base Layer (Index 0) |
| Animation | `Anim_Roll` (One-Shot, ~0.6-0.8s) |
| Has Exit Time | Nein |
| Transition Condition | `AnimationState == Roll` |

### 5.2 CharacterAnimationState Erweiterung

```csharp
public enum CharacterAnimationState
{
    Locomotion,
    Jump,
    Fall,
    SoftLand,
    HardLand,
    Roll,        // ← NEU
    LightStop,
    MediumStop,
    HardStop
}
```

### 5.3 Animator-Controller Änderungen

Neue Transitions im Animator:
- **Any State → Roll**: Condition `AnimationState == Roll`
- **Roll → Locomotion**: Condition `AnimationState == Locomotion` (nach Roll-Ende)
- **Roll → MediumStop**: Condition `AnimationState == MediumStop`

### 5.4 Animation-Event

Die Roll-Animation benötigt ein **AllowExit** Event kurz vor Ende (~80-90% der Animation), um den State-Wechsel zu ermöglichen. Dies ist konsistent mit dem bestehenden HardLanding-Pattern.

### 5.5 Animation Asset

Eine Mixamo-Roll-Animation wird benötigt:
- **Typ:** "Rolling" oder "Combat Roll"
- **In Place:** Ja (Root Motion wird nicht verwendet)
- **Root Transform Settings:** Wie andere Grounded-Anims (Bake Into Pose, Feet-based)
- **Dauer:** ~0.6-0.8 Sekunden

---

## 6. Konfiguration

### 6.1 Neue Parameter in LocomotionConfig

```csharp
[Header("Landing Roll")]
[Tooltip("Trigger-Modus: MovementInput (automatisch bei Stick-Input) oder ButtonPress (Taste nötig)")]
public RollTriggerMode RollTriggerMode = RollTriggerMode.MovementInput;

[Tooltip("Geschwindigkeits-Multiplikator relativ zu RunSpeed")]
[Range(0.5f, 2.0f)]
public float RollSpeedModifier = 1.0f;

[Tooltip("Roll aktivieren/deaktivieren (false = immer HardLanding)")]
public bool RollEnabled = true;
```

### 6.2 RollTriggerMode Enum

```csharp
public enum RollTriggerMode
{
    /// Roll bei Movement-Input während Landung (Genshin-Style)
    MovementInput,

    /// Roll nur bei gehaltener Dodge/Roll-Taste
    ButtonPress
}
```

---

## 7. Erweiterbarkeit (Future Hooks)

### 7.1 I-Frames (für späteres Combat-System)

```csharp
// Vorbereiteter Hook in PlayerRollingState:
// protected virtual bool IsInvulnerable => false;
//
// Später durch Combat-System überschreibbar:
// protected override bool IsInvulnerable =>
//     _rollTimer < Config.RollIFrameDuration;
```

**Nicht implementieren**, nur als Kommentar/Dokumentation für Phase 9 (Combat).

### 7.2 Stamina-Kosten (für späteres Ressourcen-System)

```csharp
// Vorbereiteter Hook:
// protected virtual bool CanAffordRoll() => true;
//
// Später:
// protected override bool CanAffordRoll() =>
//     StaminaSystem.Current >= Config.RollStaminaCost;
```

**Nicht implementieren**, nur als Dokumentation.

### 7.3 Aktiver Dodge (separate Spezifikation)

Der Landing Roll teilt sich keine Logik mit einem aktiven Dodge-System. Ein zukünftiger `PlayerDodgingState` wäre:
- Eigenständiger Grounded-State (nicht Landing-basiert)
- Input-getriggert aus jedem Grounded-State
- Eigene Geschwindigkeit, Richtung, ggf. I-Frames
- Eigene Spezifikation erforderlich

---

## 8. Abhängigkeiten & Voraussetzungen

| Abhängigkeit | Status | Beschreibung |
|-------------|--------|-------------|
| Phase 4 — Landing System | ✅ Fertig | SoftLanding/HardLanding als Grundlage |
| `DashPressed` Input | ✅ Vorhanden | Bereits in ReusableData, für ButtonPress-Modus |
| Roll-Animation (Mixamo) | ❌ Beschaffen | Benötigt für Phase 22 |
| Animator-Controller | ✅ Vorhanden | Wird um Roll-State erweitert |

---

## 9. Testplan

### 9.1 Unit Tests

| Test | Beschreibung |
|------|-------------|
| `RollingState_Enter_SetsCorrectSpeedModifier` | SpeedModifier = Config.RollSpeedModifier |
| `RollingState_Enter_DisablesSprint` | ShouldSprint = false nach Enter |
| `RollingState_BlocksJumpInput` | Jump-Input wird während Roll ignoriert |
| `RollingState_TransitionsToMovement_WhenInputActive` | → Walk/Run/Sprint nach Animation-Ende |
| `RollingState_TransitionsToMediumStop_WhenNoInput` | → MediumStop nach Animation-Ende |
| `FallingState_TransitionsToRoll_WhenMovementInput` | Roll statt HardLand bei Movement-Input |
| `FallingState_TransitionsToHardLand_WhenNoInput` | HardLand bei keinem Input (unverändert) |
| `FallingState_TransitionsToRoll_WhenButtonPressed` | ButtonPress-Modus funktioniert |
| `FallingState_NoRoll_WhenDisabled` | Config.RollEnabled = false → immer HardLand |
| `RollingState_SetsCorrectRotation` | Character rotiert in Input-Richtung |

### 9.2 Manuelle Tests (Play Mode)

- [ ] Hoher Fall + Vorwärts-Input → Roll-Animation + nahtloser Übergang zu Run
- [ ] Hoher Fall + Seitlicher Input → Roll in seitliche Richtung
- [ ] Hoher Fall + Kein Input → HardLanding (unverändert)
- [ ] Niedriger Fall + Input → SoftLanding (unverändert)
- [ ] Roll → sofort Sprint → Sprint startet flüssig
- [ ] Roll → Jump (blockiert) → kein Sprung während Roll
- [ ] ButtonPress-Modus: Fall + DashPressed → Roll
- [ ] ButtonPress-Modus: Fall + Movement ohne DashPressed → HardLanding
- [ ] RollEnabled = false → Roll wird nie ausgelöst

---

## 10. Referenz: Genshin Impact Implementation

### Trigger-Logik (aus PlayerFallingState)

```csharp
// Genshin Impact Pattern:
protected override void OnContactWithGround(Collider collider)
{
    float fallDistance = playerPositionOnEnter.y - transform.position.y;

    if (fallDistance < airborneData.FallData.MinimumDistanceToBeConsideredHardFall)
    {
        stateMachine.ChangeState(stateMachine.LightLandingState);
        return;
    }

    // HardLanding: Kein Input ODER Walking
    if (stateMachine.ReusableData.ShouldWalk ||
        stateMachine.ReusableData.MovementInput == Vector2.zero)
    {
        stateMachine.ChangeState(stateMachine.HardLandingState);
        return;
    }

    // Roll: Hoher Fall + Movement Input
    stateMachine.ChangeState(stateMachine.RollingState);
}
```

### Roll-State (aus PlayerRollingState)

```csharp
// Genshin Impact Pattern:
public class PlayerRollingState : PlayerLandingState
{
    public override void Enter()
    {
        stateMachine.ReusableData.MovementSpeedModifier = groundedData.RollData.SpeedModifier;
        base.Enter();
        StartAnimation(stateMachine.Player.AnimationData.RollParameterHash);
        stateMachine.ReusableData.ShouldSprint = false;
    }

    public override void OnAnimationTransitionEvent()
    {
        if (stateMachine.ReusableData.MovementInput == Vector2.zero)
        {
            stateMachine.ChangeState(stateMachine.MediumStoppingState);
            return;
        }
        OnMove(); // → Walk/Run/Sprint basierend auf Input
    }

    // Jump während Roll blockiert
    protected override void OnJumpStarted(InputAction.CallbackContext context) { }
}
```

### Unterschiede zum GameKit

| Aspekt | Genshin | GameKit (diese Spec) |
|--------|---------|---------------------|
| Trigger | Immer bei Movement-Input | Konfigurierbar (MovementInput / ButtonPress) |
| Deaktivierbar | Nein | Ja (RollEnabled) |
| Geschwindigkeit | Eigene Data-Klasse | In LocomotionConfig integriert |
| Rotation | SmoothDamp | Sofortige Rotation (snappy feel) |
| Animation-Ende | OnAnimationTransitionEvent | AllowExit Event + CanExitAnimation Pattern |
| Physik | Rigidbody.velocity direkt | Intent-System (MovementSpeedModifier) |

---

## 11. Zusammenfassung

Der Landing Roll erweitert das bestehende Landing-System um eine **dritte Landeoption** zwischen Soft und Hard Landing. Er ist:

- **Minimal invasiv**: Nur FallingState-Transition und neuer State + Config-Erweiterung
- **Konsistent**: Folgt dem bestehenden State-Pattern und Animation-System
- **Konfigurierbar**: Trigger-Modus, Geschwindigkeit, Ein/Aus per Config
- **Erweiterbar**: Hooks für I-Frames, Stamina, aktiven Dodge dokumentiert
