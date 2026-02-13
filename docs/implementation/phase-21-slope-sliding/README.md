# Phase 21: Slope Sliding

> **Epic:** Lebendige Charaktere — Animation Pipeline
> **Branch:** `integration/phase-21-slope-sliding`
> **Abhängigkeiten:** Phase 4 (Fortgeschrittene Lokomotion) ✅ Abgeschlossen
> **Spezifikation:** [Slope Sliding Spezifikation](../../specs/SlopeSliding_Spezifikation.md)

---

## Ziel

Dedizierter Sliding-State für steile Hänge (`SlopeAngle > MaxSlopeAngle`) mit:
- Aktiver Rutsch-Kraft (statt passiver Motor-Projektion)
- Konfigurierbarer Geschwindigkeit (steilheitabhängig)
- Spieler-Lenkung und optionalem Abspringen
- Dedizierter Animation
- Hysterese gegen Flackern an Grenzwinkeln

**Kernproblem:** `SlopeModule.CalculateSlideVelocity()` und `LocomotionConfig.SlopeSlideSpeed` existieren bereits, werden aber **nirgends im Production-Code** aufgerufen. `CharacterLocomotion.IsSliding` gibt immer `false` zurück.

---

## Voraussetzungen

- [x] Phase 4: Landing System, Stopping States, Slope Speed Modifiers
- [x] `SlopeModule` mit `ShouldSlide()`, `CalculateSlideVelocity()` (bereits vorhanden)
- [x] `LocomotionConfig.SlopeSlideSpeed` + `UseSlopeDependentSlideSpeed` (bereits vorhanden)
- [ ] Slide-Animation (Mixamo, wird in 21.1 beschafft)

---

## Schritte

| Schritt | Beschreibung | Branch-Typ | Commit-Message |
|---------|-------------|------------|----------------|
| [21.1](21.1-slide-animation-asset.md) | Slide-Animation beschaffen + importieren | `feat/slide-animation-asset` | `feat(phase-21): 21.1 Slide-Animation Asset` |
| [21.2](21.2-config-erweiterung.md) | Config-Erweiterung (neue Sliding-Parameter) | `feat/slide-config` | `feat(phase-21): 21.2 Sliding Config-Parameter` |
| [21.3](21.3-locomotion-slide-intent.md) | Slide-Intent in CharacterLocomotion | `feat/slide-locomotion-intent` | `feat(phase-21): 21.3 Slide-Intent + Physik` |
| [21.4](21.4-sliding-state.md) | PlayerSlidingState implementieren | `feat/slide-state` | `feat(phase-21): 21.4 PlayerSlidingState` |
| [21.5](21.5-entry-exit-transitions.md) | Entry/Exit-Transitions (Grounded, Falling) | `feat/slide-transitions` | `feat(phase-21): 21.5 Slide Entry/Exit Transitions` |
| [21.6](21.6-animation-integration.md) | CharacterAnimationState.Slide + Animator | `feat/slide-animation-integration` | `feat(phase-21): 21.6 Slide Animation-Integration` |
| [21.7](21.7-unit-tests.md) | Unit Tests | `test/slide-tests` | `test(phase-21): 21.7 Sliding Unit Tests` |
| [21.8](21.8-play-mode-verifikation.md) | Play Mode Verifikation | — (kein eigener Branch) | — |

---

## Erwartetes Ergebnis

Nach Abschluss:
- Character rutscht auf steilen Hängen mit konfigurierbarer Geschwindigkeit
- Slide-Animation zeigt korrekte Rutsch-Pose
- Smooth Transitions: Grounded → Slide → Grounded und Falling → Slide
- Optional: Abspringen und Lenken während des Slidings
- Kein Flackern an Grenzwinkeln (Hysterese)
- `IsSliding` gibt korrekt `true`/`false` zurück
- `SlopeModule.CalculateSlideVelocity()` wird im Production-Code genutzt

---

## Impact Notes

> **Phase 22 (Landing Roll)** — Beide Phasen modifizieren `PlayerFallingState` Landing-Kategorisierung
> Betrifft: `PlayerFallingState.cs`, `CharacterAnimationState` enum, `ILocomotionConfig`, `PlayerMovementStateMachine`
> Aktion: Phase 22 sollte nach Phase 21 implementiert werden, oder die FallingState-Änderungen koordinieren

---

## Nächste Phase im Epic

→ [Phase 22: Landing Roll](../phase-22-landing-roll/README.md) (gleiche Abhängigkeiten, kann parallel aber empfohlen danach)
