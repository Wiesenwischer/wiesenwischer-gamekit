# Phase 22: Landing Roll

> **Epic:** Lebendige Charaktere — Animation Pipeline
> **Branch:** `integration/phase-22-landing-roll`
> **Abhängigkeiten:** Phase 4 (Fortgeschrittene Lokomotion — Landing System) ✅ Abgeschlossen
> **Spezifikation:** [Landing Roll Spezifikation](../../specs/LandingRoll_Spezifikation.md)

---

## Ziel

Neuer Landing-State für Roll bei hartem Aufprall mit Movement-Input — Character rollt in Bewegungsrichtung statt komplett zu stoppen (HardLanding). Dritte Landeoption zwischen Soft und Hard Landing.

**Kernkonzept:**
```
Falling (hoher Fall)
  ├─ Kein Input          → HardLanding (Vollstopp, Recovery)
  ├─ Movement-Input      → LandingRoll (Rolle, Momentum beibehalten)
  └─ Niedriger Fall      → SoftLanding (sofort weiter)
```

Konfigurierbarer Trigger-Modus (MovementInput / ButtonPress), omni-direktional, deaktivierbar.

---

## Voraussetzungen

- [x] Phase 4: Landing System (SoftLanding/HardLanding)
- [x] `DashPressed` Input (bereits in ReusableData)
- [x] Animator-Controller mit CrossFade-System
- [x] AnimationWizard mit Slot-System
- [ ] Roll-Animation (Mixamo, wird in 22.1 beschafft)

---

## Schritte

| Schritt | Beschreibung | Branch-Typ | Commit-Message |
|---------|-------------|------------|----------------|
| [22.1](22.1-roll-animation-asset.md) | Roll-Animation beschaffen + importieren | `feat/roll-animation-asset` | `feat(phase-22): 22.1 Roll-Animation Asset` |
| [22.2](22.2-config-erweiterung.md) | RollTriggerMode Enum + Config-Erweiterung | `feat/roll-config` | `feat(phase-22): 22.2 Roll Config-Parameter` |
| [22.3](22.3-animation-integration.md) | CharacterAnimationState.Roll + Animator-State | `feat/roll-animation-integration` | `feat(phase-22): 22.3 Roll Animation-Integration` |
| [22.4](22.4-rolling-state.md) | PlayerRollingState implementieren | `feat/roll-state` | `feat(phase-22): 22.4 PlayerRollingState` |
| [22.5](22.5-statemachine-transition.md) | StateMachine + FallingState Transition | `feat/roll-transitions` | `feat(phase-22): 22.5 Roll StateMachine-Integration` |
| [22.6](22.6-unit-tests.md) | Unit Tests | `test/roll-tests` | `test(phase-22): 22.6 Rolling Unit Tests` |
| [22.7](22.7-play-mode-verifikation.md) | Play Mode Verifikation | — (kein eigener Branch) | — |

---

## Erwartetes Ergebnis

Nach Abschluss:
- Character rollt bei hartem Aufprall mit Movement-Input in Bewegungsrichtung
- Roll-Animation wird korrekt abgespielt (One-Shot, ~0.6-0.8s)
- Nach Roll: Nahtloser Übergang zu Walk/Run/Sprint (wenn Input) oder MediumStop (ohne Input)
- HardLanding bleibt unverändert wenn kein Movement-Input (oder Roll deaktiviert)
- SoftLanding bleibt unverändert bei niedrigem Fall
- Jump ist während Roll blockiert
- Trigger-Modus konfigurierbar (MovementInput / ButtonPress)
- Roll deaktivierbar per Config (`RollEnabled = false`)

---

## Nächste Phase im Epic

→ Crouching (noch nicht als Phase eingeplant — Spezifikation vorhanden)
