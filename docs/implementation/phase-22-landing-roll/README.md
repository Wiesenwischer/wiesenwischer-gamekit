# Phase 22: Landing Roll

> **Epic:** Lebendige Charaktere — Animation Pipeline
> **Branch:** `integration/phase-22-landing-roll`
> **Abhängigkeiten:** Phase 4 (Fortgeschrittene Lokomotion — Landing System) ✅ Abgeschlossen
> **Status:** Offen

---

## Ziel

Neuer Landing-State **PlayerRollingState**, der zwischen SoftLanding und HardLanding eine dritte Landeoption bietet. Bei hartem Aufprall mit Movement-Input rollt der Character in Bewegungsrichtung und behält Momentum bei — anstatt wie bei HardLanding komplett stehen zu bleiben.

**Kernkonzept:**
```
Falling (hoher Fall)
  ├─ Kein Input          → HardLanding (Vollstopp, Recovery)
  ├─ Movement-Input      → LandingRoll (Rolle vorwärts, Momentum)
  └─ Niedriger Fall      → SoftLanding (sofort weiter)
```

## Spezifikation

- [Landing Roll Spezifikation](../../specs/LandingRoll_Spezifikation.md)

## Voraussetzungen

| Abhängigkeit | Status | Beschreibung |
|-------------|--------|-------------|
| Phase 4 — Landing System | ✅ | SoftLanding/HardLanding als Grundlage |
| Phase 21 — Slope Sliding | ✅ Implementiert | FallingState.HandleLanding() enthält bereits Sliding-Check |
| `ReusableData.MoveInput` | ✅ | Movement-Input für Roll-Trigger |
| Animator Controller | ✅ | Wird um Roll-State erweitert |

## Schritte

| # | Schritt | Branch | Commit-Message | Status |
|---|---------|--------|---------------|--------|
| 22.1 | [Roll-Animation beschaffen + importieren](22.1-roll-animation-asset.md) | `feat/roll-animation-asset` | `feat(phase-22): 22.1 Roll-Animation beschaffen und importieren` | - [ ] |
| 22.2 | [RollTriggerMode Enum + Config-Erweiterung](22.2-config-erweiterung.md) | `feat/roll-config` | `feat(phase-22): 22.2 RollTriggerMode Enum und Config-Erweiterung` | - [ ] |
| 22.3 | [CharacterAnimationState.Roll + Animator-State](22.3-animation-integration.md) | `feat/roll-animation-state` | `feat(phase-22): 22.3 Roll Animation-State und Animator-Integration` | - [ ] |
| 22.4 | [PlayerRollingState implementieren](22.4-rolling-state.md) | `feat/rolling-state` | `feat(phase-22): 22.4 PlayerRollingState implementieren` | - [ ] |
| 22.5 | [StateMachine + FallingState Transition](22.5-statemachine-transition.md) | `feat/roll-transitions` | `feat(phase-22): 22.5 RollingState in StateMachine und FallingState-Transition` | - [ ] |
| 22.6 | [Unit Tests](22.6-unit-tests.md) | `test/rolling-state-tests` | `test(phase-22): 22.6 Unit Tests für RollingState` | - [ ] |
| 22.7 | [Play Mode Verifikation](22.7-play-mode-verifikation.md) | — | `docs(phase-22): 22.7 Play Mode Verifikation` | - [ ] |

## Erwartetes Ergebnis

Nach Abschluss dieser Phase:

1. **Roll-Animation** (`Anim_Roll.fbx`) in `Assets/Animations/Locomotion/` importiert und konfiguriert
2. **Config-Parameter** in `ILocomotionConfig`/`LocomotionConfig`: `RollEnabled`, `RollTriggerMode`, `RollSpeedModifier`
3. **CharacterAnimationState.Roll** im Enum + Animator Controller State + CrossFade Transition
4. **PlayerRollingState** erbt von `PlayerGroundedState` mit:
   - Sofortige Rotation in Input-Richtung
   - Konstante Roll-Geschwindigkeit (RunSpeed × RollSpeedModifier)
   - Jump-Input blockiert, Sprint deaktiviert
   - Animation-basierte Recovery (AllowExit/IsAnimationComplete)
   - Transition zu Walk/Run/Sprint (bei Input) oder MediumStop (ohne Input)
5. **PlayerFallingState.HandleLanding()** erweitert: Roll statt HardLand bei aktivem Movement-Input
6. **Unit Tests** für alle Transitions und Verhaltensweisen
7. **Play Mode Verifikation** bestätigt Gameplay-Qualität

## Dateien (Übersicht)

| Datei | Aktion |
|-------|--------|
| `Assets/Animations/Locomotion/Anim_Roll.fbx` | Neu (Mixamo) |
| `Core/Runtime/Core/StateMachine/ILocomotionConfig.cs` | Erweitern (RollEnabled, RollTriggerMode, RollSpeedModifier) |
| `Core/Runtime/Core/Locomotion/LocomotionConfig.cs` | Erweitern (Felder + Properties) |
| `Core/Runtime/Core/Animation/IAnimationController.cs` | Erweitern (Roll im Enum) |
| `Animation/Runtime/AnimatorParameterBridge.cs` | Erweitern (Roll CrossFade Mapping) |
| `Animation/Runtime/AnimationTransitionConfig.cs` | Erweitern (RollTransition) |
| `Animation/Editor/AnimatorControllerCreator.cs` | Erweitern (Roll State programmatisch) |
| `Animation/Editor/AnimationWizard.cs` | Erweitern (Roll FBX-Slot, Auto-Detect, Zähler 12→13) |
| `Animation/Resources/AnimatorControllers/CharacterAnimatorController.controller` | Erweitern (Roll State) |
| `Core/Runtime/Core/StateMachine/States/Grounded/PlayerRollingState.cs` | Neu |
| `Core/Runtime/Core/StateMachine/PlayerMovementStateMachine.cs` | Erweitern (RollingState Property) |
| `Core/Runtime/Core/StateMachine/States/Airborne/PlayerFallingState.cs` | Erweitern (Roll-Condition in HandleLanding) |
| `Core/Tests/Runtime/RollingTests.cs` | Neu |

## Nächste Phase

→ Phase 5: Ability System (Epic: Fähigkeiten & Action Combat)
