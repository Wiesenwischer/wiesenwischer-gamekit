# Phase 23: Crouching

> **Epic:** Lebendige Charaktere — Animation Pipeline
> **Branch:** `integration/phase-23-crouching`
> **Abhängigkeiten:** Phase 4 (Fortgeschrittene Lokomotion) ✅ Abgeschlossen
> **Spezifikation:** [Crouching Spezifikation](../../specs/Crouching_Spezifikation.md)

---

## Ziel

Toggle-basiertes Crouching-System mit dynamischer Capsule-Höhe, reduzierter Bewegungsgeschwindigkeit und Ceiling-Detection (Stand-Up-Check). C-Taste togglet Crouch ein/aus.

**Kernkonzept:**
- C-Taste → Toggle (kein Hold)
- Capsule-Höhe: 2.0m → 1.2m (smooth Transition)
- Crouch-Speed: 2.5 m/s (zwischen Walk und Run)
- Sprint beendet Crouch automatisch
- Stand-Up-Check verhindert Aufstehen unter Decken
- Crouch Blend Tree: Crouch Idle + Crouch Walk

---

## Voraussetzungen

- [x] Phase 4: Fortgeschrittene Lokomotion (State Machine, Config, Inputs)
- [x] `InputButtons.Crouch` (bereits in `IMovementInputProvider`, `1 << 3`)
- [x] `ControllerButtons.Crouch` (bereits in Prediction-Schicht, `1 << 2`)
- [x] Crouch Input Action im `InputSystem_Actions.inputactions` (C-Taste, bereits konfiguriert)
- [x] `CharacterMotor.SetCapsuleDimensions()` für dynamische Capsule-Höhe
- [ ] Crouch-Animationen (Mixamo, wird in 23.1 beschafft)

---

## Schritte

| Schritt | Beschreibung | Branch-Typ | Commit-Message |
|---------|-------------|------------|----------------|
| [23.1](23.1-crouch-animation-assets.md) | Crouch-Animationen beschaffen + importieren | `feat/crouch-animation-assets` | `feat(phase-23): 23.1 Crouch-Animation Assets` |
| [23.2](23.2-config-erweiterung.md) | Config-Erweiterung (Crouch-Parameter) | `feat/crouch-config` | `feat(phase-23): 23.2 Crouch Config-Parameter` |
| [23.3](23.3-input-integration.md) | Input-Integration (CrouchTogglePressed) | `feat/crouch-input` | `feat(phase-23): 23.3 Crouch Input-Integration` |
| [23.4](23.4-animation-integration.md) | CharacterAnimationState.Crouch + Animator | `feat/crouch-animation-integration` | `feat(phase-23): 23.4 Crouch Animation-Integration` |
| [23.5](23.5-capsule-transition.md) | Capsule-Höhen-Transition in CharacterLocomotion | `feat/crouch-capsule` | `feat(phase-23): 23.5 Capsule-Höhen-Transition` |
| [23.6](23.6-crouching-state.md) | PlayerCrouchingState implementieren | `feat/crouch-state` | `feat(phase-23): 23.6 PlayerCrouchingState` |
| [23.7](23.7-statemachine-transition.md) | StateMachine + GroundedState Transition | `feat/crouch-transitions` | `feat(phase-23): 23.7 Crouch StateMachine-Integration` |
| [23.8](23.8-unit-tests.md) | Unit Tests | `test/crouch-tests` | `test(phase-23): 23.8 Crouching Unit Tests` |
| [23.9](23.9-play-mode-verifikation.md) | Play Mode Verifikation | — (kein eigener Branch) | — |

---

## Erwartetes Ergebnis

Nach Abschluss:
- C-Taste togglet Crouching ein/aus
- Capsule-Höhe reduziert sich smooth von 2.0m auf 1.2m
- Bewegungsgeschwindigkeit ist auf CrouchSpeed (2.5 m/s) reduziert
- Stand-Up-Check verhindert Aufstehen unter Decken
- Sprint-Input beendet Crouch und wechselt zu Sprinting
- Jump aus Crouch steht auf und springt
- Über Kante fallen → automatisch aufstehen
- Crouch-Animationen (Idle + Walk) werden korrekt abgespielt

---

## Impact Notes

> **Phase 22 (Landing Roll)** — Beide Phasen erweitern `ILocomotionConfig`, `LocomotionConfig`, `PlayerMovementStateMachine`, `CharacterAnimationState`
> Betrifft: Config-Interfaces, StateMachine, Animator
> Aktion: Rein additive Änderungen, kein Konflikt. Phase 23 nach Phase 22 implementieren.

---

## Nächste Phase im Epic

→ Keine weitere Locomotion-Phase geplant (Slope Sliding, Landing Roll, Crouching abgedeckt)
