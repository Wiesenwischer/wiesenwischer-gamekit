# Phase 20: Visual Grounding Smoother

> **Epic:** Lebendige Charaktere — Animation Pipeline
> **Branch:** `integration/phase-20-grounding-smoother`
> **Abhängigkeit:** Phase 4 (Fortgeschrittene Lokomotion) — ✅ Abgeschlossen
> **Spec:** [GroundingSmoother Spezifikation](../../specs/GroundingSmoother_Spezifikation.md)

---

## Ziel

Visuelles Y-Smoothing für diskrete Step-Ups (Treppen, Kanten): Das Mesh gleitet per `SmoothDamp` über mehrere Frames hoch, statt pro Physik-Tick zu springen. Voraussetzung für gutes Foot IK (Phase 8).

**Kern-Prinzip:** Der CharacterMotor teleportiert die Capsule bei Step-Ups sofort nach oben. Der GroundingSmoother versetzt das Model-Child temporär nach unten und löst diesen Offset über ~0.075s auf — das Mesh "gleitet" visuell hoch.

---

## Voraussetzungen

- Phase 4 vollständig abgeschlossen (Locomotion Features, Detection Strategies, IsOnStairs)
- `CharacterMotor` mit `TransientPosition`, `JustLanded`
- `CharacterLocomotion` mit `IsGrounded`
- Player Prefab mit CharacterModel als Child-Object

---

## Schritte

| Schritt | Beschreibung | Branch | Commit-Message |
|---------|-------------|--------|----------------|
| [20.1](20.1-grounding-smoother-component.md) | GroundingSmoother Komponente | `feat/grounding-smoother-component` | `feat(phase-20): 20.1 GroundingSmoother Komponente` |
| [20.2](20.2-unit-tests.md) | Unit Tests | `feat/grounding-smoother-component` | `test(phase-20): 20.2 GroundingSmoother Tests` |
| [20.3](20.3-prefab-integration.md) | Prefab-Integration | `feat/grounding-smoother-integration` | `feat(phase-20): 20.3 Prefab-Integration` |
| [20.4](20.4-verification.md) | Verifikation | `feat/grounding-smoother-integration` | `test(phase-20): 20.4 Verifikation` |

**Branch-Zuordnung:**
- Schritte 20.1 + 20.2 → `feat/grounding-smoother-component` (Komponente + Tests zusammen)
- Schritte 20.3 + 20.4 → `feat/grounding-smoother-integration` (Prefab + Verifikation zusammen)

---

## Erwartetes Ergebnis

Nach Phase 20:
- `GroundingSmoother.cs` in `Runtime/Core/Visual/`
- Unit Tests in `Tests/Runtime/Visual/`
- Player Prefab enthält GroundingSmoother-Komponente
- Treppen-Laufen ist visuell glatt (kein Daumenkino-Effekt)
- Keine Auswirkung auf Slopes, Sprünge, Landungen oder Teleports

---

## Nächste Phase

→ Phase 8: IK System (Natürliche Bewegung — Inverse Kinematics)
