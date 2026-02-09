# Phase 3: Animation-Integration

**Integration-Branch:** `integration/phase-3-animation-integration`
**Epic:** Lebendige Charaktere — Animation Pipeline
**Abhängigkeiten:** Phase 1 (Animation-Vorbereitung), Phase 2 (Animator Setup)

---

## Ziel

Die in Phase 2 erstellte AnimatorParameterBridge und den Animator Controller mit dem bestehenden Character Controller verbinden:
- `IAnimationController` im PlayerController verfügbar machen
- States rufen Animation-Trigger auf (Jump, Landing)
- Kontinuierliche Parameter (Speed, IsGrounded, VerticalVelocity) werden automatisch von der Bridge synchronisiert
- Player Prefab mit allen Komponenten zusammenbauen
- Animationen in einer Test-Szene verifizieren

---

## Relevante Spezifikationen

- [Animationskonzept LayeredAbilities](../../specs/Animationskonzept_LayeredAbilities.md)
- [AAA Action Combat & Character Architecture](../../specs/AAA_Action_Combat_Character_Architecture.md)
- [GameKit CharacterController Modular](../../specs/GameKit_CharacterController_Modular.md)

---

## Schritte

| # | Schritt | Commit-Message | Branch-Typ | Status |
|---|---------|----------------|------------|--------|
| 3.1 | [PlayerController Animation-Anbindung](3.1-controller-binding.md) | `feat(phase-3): 3.1 PlayerController Animation-Anbindung` | `feat/animation-controller-binding` | [x] |
| 3.2 | [State Animation-Trigger](3.2-state-animation-triggers.md) | `feat(phase-3): 3.2 State Animation-Trigger` | `feat/state-animation-triggers` | [x] |
| 3.3 | [Player Prefab zusammenbauen](3.3-player-prefab.md) | `feat(phase-3): 3.3 Player Prefab zusammenbauen` | `feat/animated-player-prefab` | [x] |
| 3.4 | [Test-Szene & Verifikation](3.4-test-verification.md) | `test(phase-3): 3.4 Test-Szene und Verifikation` | `test/animation-integration` | [ ] |

---

## Voraussetzungen

- Phase 1 abgeschlossen:
  - Character FBX mit Humanoid Avatar importiert
  - Basis-Animationen vorhanden (Idle, Walk, Run, Sprint, Jump, Fall, Land)
  - Animation Package Struktur erstellt (`Wiesenwischer.GameKit.CharacterController.Animation`)
  - `IAnimationController` Interface und `AnimationParameters` Klasse vorhanden

- Phase 2 abgeschlossen:
  - Avatar Masks erstellt (UpperBody, LowerBody, ArmsOnly)
  - Animator Controller mit 3 Layern (Base Movement, Abilities, Status)
  - Locomotion Blend Tree (Idle -> Walk -> Run -> Sprint)
  - Airborne States mit Transitionen (Jump, Fall, SoftLand, HardLand)
  - `AnimatorParameterBridge` Komponente erstellt (implementiert `IAnimationController`)

---

## Architektur-Überblick

### Datenfluss nach Phase 3

```
PlayerController.Update()
  ├── UpdateInput()                      → MoveInput, JumpPressed, etc.
  ├── StateMachine.Update()
  │   ├── CurrentState.HandleInput()     → JumpRequested, SprintHeld
  │   └── CurrentState.OnUpdate()        → State-Transitionen
  │       └── OnEnter(): AnimCtrl.TriggerJump() / TriggerLanding()
  ├── ConsumeMovementEvents()            → Locomotion-Intents
  └── TickSystem.Update()

PlayerController.OnFixedTick()
  ├── StateMachine.PhysicsUpdate()
  └── ApplyMovement()                    → Velocity-Berechnungen

AnimatorParameterBridge.LateUpdate()     ← NEU: Liest finale Werte
  ├── Read ReusableData (Speed, IsGrounded, VerticalVelocity)
  ├── Normalize Speed (/ RunSpeed)
  └── SetFloat/SetBool mit Damping       → Animator evaluiert
```

### Komponentenstruktur des Player Prefabs

```
Player (GameObject)
├── PlayerController
│   ├── LocomotionConfig (ScriptableObject)
│   └── IMovementInputProvider
├── CharacterMotor (KCC)
├── CapsuleCollider
│
└── Character Model (Child)
    ├── Animator
    │   └── CharacterAnimatorController
    └── AnimatorParameterBridge
        └── [RequireComponent(typeof(Animator))]
```

### Wer setzt welche Parameter?

| Parameter | Gesetzt durch | Wann |
|-----------|--------------|------|
| `Speed` | AnimatorParameterBridge (auto) | LateUpdate, aus HorizontalVelocity |
| `IsGrounded` | AnimatorParameterBridge (auto) | LateUpdate, aus ReusableData |
| `VerticalVelocity` | AnimatorParameterBridge (auto) | LateUpdate, aus ReusableData |
| `Jump` (Trigger) | State → IAnimationController | JumpingState.OnEnter() |
| `Land` (Trigger) | State → IAnimationController | SoftLandingState.OnEnter() |
| `HardLanding` (Bool) | State → IAnimationController | HardLandingState.OnEnter() |
| Ability Layer Weight | Ability System (Phase 4) | Nicht in Phase 3 |
| Status Layer Weight | Status System (Phase 4+) | Nicht in Phase 3 |

---

## Ergebnis

Nach Abschluss dieser Phase:
- [ ] PlayerController hat `IAnimationController` Property
- [ ] States rufen Animation-Trigger auf (Jump, Landing)
- [ ] AnimatorParameterBridge synchronisiert kontinuierliche Parameter automatisch
- [ ] Player Prefab mit allen Komponenten funktionsfähig
- [ ] Locomotion-Animationen (Idle, Walk, Run, Sprint) spielen korrekt
- [ ] Airborne-Animationen (Jump, Fall, SoftLand, HardLand) spielen korrekt
- [ ] Alle Dateien kompilieren fehlerfrei

---

## Nächste Phase

[Phase 4: Ability System](../phase-4-ability-system/README.md) (Epic: Fähigkeiten & Action Combat)
