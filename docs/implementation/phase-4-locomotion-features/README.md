# Phase 4: Fortgeschrittene Lokomotion

**Integration-Branch:** `integration/phase-4-locomotion-features`
**Epic:** Lebendige Charaktere — Animation Pipeline
**Abhängigkeiten:** Phase 3 (Animation-Integration)

---

## Ziel

Erweiterte Locomotion-Features nach Genshin-Impact-Vorbild: Tier-basierte Stopping-Animationen, differenziertes Landing-System, MMO-Style Walk-Toggle, Terrain-abhängige Geschwindigkeit und modulare Detection Strategies.

---

## Relevante Spezifikationen

- [Animation CrossFade Architektur](../../specs/Animation_CrossFade_Architektur.md)
- [GameKit CharacterController Modular](../../specs/GameKit_CharacterController_Modular.md)
- [AAA Action Combat & Character Architecture](../../specs/AAA_Action_Combat_Character_Architecture.md)

---

## Architektur-Überblick

### State-Hierarchie

```
PlayerMovementState (abstract)
├── PlayerGroundedState (abstract)
│   ├── PlayerIdlingState
│   ├── PlayerMovingState (abstract)
│   │   ├── PlayerWalkingState
│   │   ├── PlayerRunningState
│   │   └── PlayerSprintingState
│   ├── PlayerStoppingState (abstract)
│   │   ├── PlayerLightStoppingState    (von Walk)
│   │   ├── PlayerMediumStoppingState   (von Run)
│   │   └── PlayerHardStoppingState     (von Sprint)
│   ├── PlayerSoftLandingState
│   └── PlayerHardLandingState
└── PlayerAirborneState (abstract)
    ├── PlayerJumpingState
    └── PlayerFallingState
```

### Transition-Diagramm

```
Idle ──[Input]──→ Walk/Run/Sprint (je nach ShouldWalk + SprintHeld)

Walking   ──[kein Input]──→ LightStopping  ──[v≈0 || animDone]──→ Idle
Running   ──[kein Input]──→ MediumStopping ──[v≈0 || animDone]──→ Idle
Sprinting ──[kein Input]──→ HardStopping   ──[v≈0 || animDone]──→ Idle

Grounded ──[Jump]──→ Jumping ──[v<0]──→ Falling ──[Ground]──→ Soft/HardLanding
Grounded ──[Kante]──→ Falling ──[Ground]──→ Soft/HardLanding

SoftLanding ──[sofort]──→ Idle oder Moving
HardLanding ──[Recovery-Zeit]──→ Idle oder Moving

Jeder StoppingState:
  ──[Movement Input]──→ Walk/Run/Sprint
  ──[Jump]──→ Jumping
  ──[Kante]──→ Falling
```

### Animation CrossFade-System

Die State Machine ist die **einzige Autorität** für Animation-States. Jeder State ruft `PlayState()` in `OnEnter()` auf, was `Animator.CrossFade()` mit konfigurierbaren Übergangszeiten auslöst.

Siehe: [Animation CrossFade Architektur](../../specs/Animation_CrossFade_Architektur.md)

### Intent-System Pattern

States setzen **Intents** (Jump, JumpCut, ResetVertical) auf `PlayerStateReusableData`. `CharacterLocomotion` ist der einzige Owner der vertikalen Velocity und interpretiert die Intents in `UpdateVelocity()`. Dies entkoppelt die State Machine von der Physik-Simulation.

---

## Schritte

| # | Feature | Status |
|---|---------|--------|
| 4.1 | [Stopping States](#41-stopping-states) | ✅ |
| 4.2 | [Landing System](#42-landing-system) | ✅ |
| 4.3 | [Walk Toggle](#43-walk-toggle) | ✅ |
| 4.4 | [Slope Speed Modifiers](#44-slope-speed-modifiers) | ✅ |
| 4.5 | [Air Movement](#45-air-movement) | ✅ |
| 4.6 | [Detection Strategies](#46-detection-strategies) | ✅ |
| 4.7 | [Stair Speed Reduction](#47-stair-speed-reduction) | ✅ |
| 4.8 | [Ledge & Ground Snapping Config](#48-ledge--ground-snapping-config) | ✅ |
| 4.9 | [Animation CrossFade-System](#49-animation-crossfade-system) | ✅ |

---

### 4.1 Stopping States

3 dedizierte Stopping-States mit tier-spezifischer Deceleration und Animation:

| State | Trigger | Deceleration | Animation |
|-------|---------|-------------|-----------|
| LightStopping | Walk → kein Input | `LightStopDeceleration` (12 m/s²) | LightStop |
| MediumStopping | Run → kein Input | `MediumStopDeceleration` (10 m/s²) | MediumStop |
| HardStopping | Sprint → kein Input | `HardStopDeceleration` (8 m/s²) | HardStop |

**Unterbrechbarkeit:** Movement-Input während des Stoppens → zurück zu Walk/Run/Sprint. Jump während des Stoppens → JumpingState.

**DecelerationOverride:** `PlayerStateReusableData.DecelerationOverride` ermöglicht States, die Standard-Deceleration zu überschreiben. `CharacterLocomotion` liest diesen Wert in `UpdateVelocity()`.

**Dateien:**
- `States/Grounded/Stopping/PlayerStoppingState.cs` (abstract)
- `States/Grounded/Stopping/PlayerLightStoppingState.cs`
- `States/Grounded/Stopping/PlayerMediumStoppingState.cs`
- `States/Grounded/Stopping/PlayerHardStoppingState.cs`
- `States/Grounded/Moving/PlayerMovingState.cs` (`GetStoppingState()`)
- `PlayerMovementStateMachine.cs` (3 neue State-Properties)

---

### 4.2 Landing System

Differenziertes Landing basierend auf Aufprallgeschwindigkeit:

| Landing-Typ | Bedingung | Recovery | Animation |
|------------|-----------|----------|-----------|
| Soft Landing | `fallSpeed < SoftLandingThreshold` (5 m/s) | 0.1s | SoftLand |
| Hard Landing | `fallSpeed >= HardLandingThreshold` (15 m/s) | 0.4s | HardLand |

Zwischen den Thresholds wird die Recovery-Zeit linear interpoliert.

**Dateien:**
- `States/Grounded/PlayerSoftLandingState.cs`
- `States/Grounded/PlayerHardLandingState.cs`
- `States/Airborne/PlayerFallingState.cs` (Landing-Kategorisierung)

---

### 4.3 Walk Toggle

MMO-Style Walk-Toggle per Y-Taste:

- `WalkTogglePressed` One-Shot Input Pattern (wie Jump/Dash)
- `ShouldWalk` Toggle in `PlayerController.UpdateInput()`
- Sprint deaktiviert Walk automatisch
- Walk/Run Mode HUD-Anzeige im Debug GUI (farbig: gelb=Walk, grün=Run)

**Dateien:**
- `Input/IMovementInputProvider.cs` (`WalkTogglePressed`, `InputButtons.Walk`)
- `Input/PlayerInputProvider.cs` (WalkToggle Action + Event)
- `Input/AIInputProvider.cs` (WalkToggle Support)
- `PlayerController.cs` (Toggle-Logik, HUD)
- `InputSystem_Actions.inputactions` (WalkToggle Action, Y-Taste)

---

### 4.4 Slope Speed Modifiers

Terrain-abhängige Geschwindigkeitsanpassung:

| Parameter | Default | Beschreibung |
|-----------|---------|-------------|
| `UphillSpeedPenalty` | 0.3 | Max. Speed-Reduktion bergauf (30% bei MaxSlopeAngle) |
| `DownhillSpeedBonus` | 0.1 | Speed-Bonus bergab (10% bei MaxSlopeAngle) |

Skaliert linear mit dem Slope-Winkel. Berechnung in `CharacterLocomotion.CalculateSlopeSpeedMultiplier()`.

**Dateien:**
- `Locomotion/CharacterLocomotion.cs` (`CalculateSlopeSpeedMultiplier()`)
- `Locomotion/LocomotionConfig.cs` (Slope Speed Header)
- `StateMachine/ILocomotionConfig.cs` (Properties)

---

### 4.5 Air Movement

Konfigurierbare Luftsteuerung und Momentum:

| Parameter | Default | Beschreibung |
|-----------|---------|-------------|
| `AirControl` | 0.3 | Steuerbarkeit in der Luft (0=keine, 1=voll) |
| `AirDrag` | 0.8 | Horizontaler Momentum-Verlust (0=kein Drag, 1=volle Abbremsung) |
| `MinFallDistance` | 0.5m | Minimale Falldistanz für Falling-State |

**Dateien:**
- `Locomotion/CharacterLocomotion.cs` (Air Movement in `UpdateVelocity()`)
- `Locomotion/LocomotionConfig.cs` (Air Movement Header)

---

### 4.6 Detection Strategies

Modulare Ground- und Fall-Detection mit austauschbaren Strategien:

| Strategie | Modus | Beschreibung |
|-----------|-------|-------------|
| `MotorGroundDetectionStrategy` | Motor | KCC-Standard: `IsStableOnGround` |
| `ColliderGroundDetectionStrategy` | Collider | SphereCast von Capsule-Unterseite (Genshin-Style) |
| `MotorFallDetectionStrategy` | Motor | `SnappingPrevented` + `IsStableOnGround` |
| `ColliderFallDetectionStrategy` | Collider | Raycast von Capsule-Mitte |

Konfigurierbar über `GroundDetectionMode` und `FallDetectionMode` Enums in `LocomotionConfig`.

**Dateien:**
- `Locomotion/Modules/IGroundDetectionStrategy.cs`
- `Locomotion/Modules/IFallDetectionStrategy.cs`
- `Locomotion/Modules/MotorGroundDetectionStrategy.cs`
- `Locomotion/Modules/ColliderGroundDetectionStrategy.cs`
- `Locomotion/Modules/MotorFallDetectionStrategy.cs`
- `Locomotion/Modules/ColliderFallDetectionStrategy.cs`

---

### 4.7 Stair Speed Reduction

Automatische Geschwindigkeitsreduktion auf Treppen:

- Step-Frequenz-Tracking via `OnMovementHit` (`ValidStepDetected`)
- `IsOnStairs` Property: >= 2 Steps innerhalb 0.6s Zeitfenster
- Konfigurierbarer Speed-Multiplikator (`StairSpeedReduction`, Default: 0.3 = 30% langsamer)
- Per Toggle abschaltbar (`StairSpeedReductionEnabled`)

**Dateien:**
- `Locomotion/CharacterLocomotion.cs` (Step-Tracking, `IsOnStairs`, Speed-Modifier)
- `Locomotion/LocomotionConfig.cs` (Step Detection Header)
- `Editor/LocomotionConfigEditor.cs` (Stair Speed Sub-Section)

---

### 4.8 Ledge & Ground Snapping Config

Konfigurierbare Kanten-Erkennung und Ground Snapping:

| Parameter | Default | Beschreibung |
|-----------|---------|-------------|
| `LedgeDetectionEnabled` | true | Zusätzliche Raycasts für Kanten |
| `MaxStableDistanceFromLedge` | 0.5m | Stabile Distanz zur Kante |
| `MaxStableDenivelationAngle` | 60° | Max. Winkelunterschied für Snapping |
| `MaxVelocityForLedgeSnap` | 0 | Speed-Grenze für Snapping (0=immer) |

**Dateien:**
- `Motor/CharacterMotor.cs` (Snapping-Config wird aus LocomotionConfig gelesen)
- `Locomotion/LocomotionConfig.cs` (Ledge & Ground Snapping Header)

---

### 4.9 Animation CrossFade-System

Direktes `CrossFade()` statt Animator-Transitions:

- `AnimatorParameterBridge.PlayState()` → `Animator.CrossFade(hash, duration)`
- `AnimationTransitionConfig` ScriptableObject für per-State Übergangszeiten
- Redundanz-Check: bereits im Ziel-State → kein erneuter CrossFade
- `CanExitAnimation` via Animation Events für frühzeitigen State-Exit
- `IsAnimationComplete()` und `GetAnimationNormalizedTime()` für Progress-Tracking

**Dateien:**
- `Animation/AnimatorParameterBridge.cs` (PlayState-Implementierung)
- `Animation/AnimationTransitionConfig.cs` (ScriptableObject)
- `Animation/AnimationParameters.cs` (State-Hashes)
- `Animation/IAnimationController.cs` (`CharacterAnimationState` Enum)
- `Animation/Editor/StoppingStatesCreator.cs` (Editor-Tool)

---

## LocomotionConfig — Vollständige Parameterliste

### Ground Movement
| Parameter | Typ | Default | Beschreibung |
|-----------|-----|---------|-------------|
| WalkSpeed | float | 3.0 | Gehgeschwindigkeit (m/s) |
| RunSpeed | float | 6.0 | Laufgeschwindigkeit (m/s) |
| Acceleration | float | 10.0 | Beschleunigung (m/s²) |
| Deceleration | float | 15.0 | Verzögerung (m/s²) |
| SprintMultiplier | float | 1.5 | Sprint = RunSpeed * Multiplier |

### Air Movement
| Parameter | Typ | Default | Beschreibung |
|-----------|-----|---------|-------------|
| AirControl | float | 0.3 | Luftsteuerung (0-1) |
| AirDrag | float | 0.8 | Momentum-Verlust in Luft (0-1) |
| MinFallDistance | float | 0.5 | Min. Falldistanz für Falling-State (m) |
| Gravity | float | 20.0 | Gravitation (m/s²) |
| MaxFallSpeed | float | 50.0 | Max. Fallgeschwindigkeit (m/s) |

### Jumping
| Parameter | Typ | Default | Beschreibung |
|-----------|-----|---------|-------------|
| JumpHeight | float | 2.0 | Sprunghöhe (m) |
| JumpDuration | float | 0.4 | Zeit bis Scheitelpunkt (s) |
| CoyoteTime | float | 0.15 | Coyote Time (s) |
| JumpBufferTime | float | 0.1 | Jump Buffer (s) |
| UseVariableJump | bool | true | Variable Sprunghöhe |

### Ground Detection
| Parameter | Typ | Default | Beschreibung |
|-----------|-----|---------|-------------|
| GroundCheckDistance | float | 0.2 | Raycast-Distanz (m) |
| GroundCheckRadius | float | 0.3 | SphereCast-Radius (m) |
| GroundLayers | LayerMask | Default | Boden-Layer |
| MaxSlopeAngle | float | 45 | Max. begehbarer Winkel (°) |
| GroundDetection | Enum | Motor | Ground Detection Modus |
| FallDetection | Enum | Motor | Fall Detection Modus |
| GroundToFallRayDistance | float | 1.0 | Raycast-Distanz für Fall (m) |

### Rotation
| Parameter | Typ | Default | Beschreibung |
|-----------|-----|---------|-------------|
| RotationSpeed | float | 720 | Drehgeschwindigkeit (°/s) |
| RotateTowardsMovement | bool | true | Zur Bewegungsrichtung drehen |

### Step Detection
| Parameter | Typ | Default | Beschreibung |
|-----------|-----|---------|-------------|
| MaxStepHeight | float | 0.3 | Max. Step-Up Höhe (m) |
| MinStepDepth | float | 0.1 | Min. Step-Tiefe (m) |
| StairSpeedReductionEnabled | bool | true | Treppen-Verlangsamung aktiv |
| StairSpeedReduction | float | 0.3 | Speed-Reduktion auf Treppen (0-1) |

### Ledge & Ground Snapping
| Parameter | Typ | Default | Beschreibung |
|-----------|-----|---------|-------------|
| LedgeDetectionEnabled | bool | true | Kanten-Erkennung aktiv |
| MaxStableDistanceFromLedge | float | 0.5 | Stabile Distanz zur Kante (m) |
| MaxStableDenivelationAngle | float | 60 | Max. Winkel für Snapping (°) |
| MaxVelocityForLedgeSnap | float | 0 | Speed-Grenze für Snapping (m/s) |

### Stopping
| Parameter | Typ | Default | Beschreibung |
|-----------|-----|---------|-------------|
| LightStopDeceleration | float | 12.0 | Deceleration aus Walk (m/s²) |
| MediumStopDeceleration | float | 10.0 | Deceleration aus Run (m/s²) |
| HardStopDeceleration | float | 8.0 | Deceleration aus Sprint (m/s²) |

### Slope Speed
| Parameter | Typ | Default | Beschreibung |
|-----------|-----|---------|-------------|
| UphillSpeedPenalty | float | 0.3 | Max. Reduktion bergauf (0-1) |
| DownhillSpeedBonus | float | 0.1 | Bonus bergab (-0.5 bis 0.5) |

### Slope Sliding
| Parameter | Typ | Default | Beschreibung |
|-----------|-----|---------|-------------|
| SlopeSlideSpeed | float | 8.0 | Rutsch-Geschwindigkeit (m/s) |
| UseSlopeDependentSlideSpeed | bool | true | Steilere Hänge = schneller |

### Landing
| Parameter | Typ | Default | Beschreibung |
|-----------|-----|---------|-------------|
| SoftLandingThreshold | float | 5.0 | Max. Speed für Soft Landing (m/s) |
| HardLandingThreshold | float | 15.0 | Min. Speed für Hard Landing (m/s) |
| SoftLandingDuration | float | 0.1 | Soft Recovery-Zeit (s) |
| HardLandingDuration | float | 0.4 | Hard Recovery-Zeit (s) |

---

## Verifikation

### Stopping States
- [ ] Walk → LightStopping → Idle (mit Bremsanimation)
- [ ] Run → MediumStopping → Idle (mit Bremsanimation)
- [ ] Sprint → HardStopping → Idle (mit Bremsanimation)
- [ ] Stopping + Movement Input → zurück zu Walk/Run/Sprint
- [ ] Stopping + Jump → JumpingState
- [ ] Stopping + Kante → FallingState

### Landing
- [ ] Niedriger Fall → SoftLanding (kurze Recovery)
- [ ] Hoher Fall → HardLanding (lange Recovery, Animation)
- [ ] Jump Buffer während HardLanding → Jump nach Recovery

### Walk Toggle
- [ ] Y-Taste → Umschalten Walk/Run
- [ ] Sprint deaktiviert Walk automatisch
- [ ] HUD zeigt Walk/Run Status

### Terrain
- [ ] Bergauf langsamer (Uphill Penalty sichtbar)
- [ ] Bergab schneller (Downhill Bonus sichtbar)
- [ ] Treppen: Automatische Verlangsamung
- [ ] Zu steile Hänge: Character rutscht

### Air Movement
- [ ] AirControl: Richtungsänderung in der Luft möglich
- [ ] AirDrag: Horizontales Momentum nimmt in der Luft ab

### Technisch
- [ ] Keine Compiler-Fehler
- [ ] Keine Runtime-Errors
- [ ] CrossFade-Übergänge smooth
- [ ] Debug HUD zeigt korrekte Werte

---

## Nächste Phasen

- [Phase 5: Ability System](../phase-5-ability-system/README.md) — Abilities nutzen den Ability-Layer des Animators
- [Phase 8: IK System](../phase-8-ik-system/README.md) — FootIK für Terrain-Anpassung, GroundingSmoother für Treppen
