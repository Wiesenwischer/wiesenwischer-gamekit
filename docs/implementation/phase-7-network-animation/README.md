# Phase 7: Netzwerk-Animation

**Epic:** MMO-Netzwerk & Synchronisation
**Branch:** `integration/phase-7-network-animation`
**Status:** Offen
**Abhängigkeiten:** Phase 6 (Netzwerk-Grundstruktur) ✅, Phase 8 (IK System) ✅

---

## Ziel

Animation-Synchronisation über das Netzwerk. Remote-Spieler zeigen korrekte Bewegungsanimationen, Ability-Animationen und LookAt-IK. Am Ende der Phase sehen sich Spieler gegenseitig mit flüssigen, korrekten Animationen — nicht nur als gleitende Capsules.

---

## Architektur-Übersicht

```
┌─────────────────────────────────────────────────────────────┐
│  Lokaler Spieler (Owner)                                     │
│                                                              │
│  StateMachine ─── OnEnter() ──→ PlayState(state)             │
│       │                              │                       │
│       ▼                              ▼                       │
│  CharacterLocomotion      AnimatorParameterBridge             │
│  (Speed, Velocity)        (CrossFade, SetFloat)              │
│       │                              │                       │
│       ▼                              ▼                       │
│  NetworkAnimationSync ◄──── Erfasst State + Parameter ──────┤│
│       │                                                      │
│       ▼ [ObserverRpc]                                        │
└───────┼──────────────────────────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────────────────────────────────┐
│  Remote-Spieler (Non-Owner)                                  │
│                                                              │
│  NetworkAnimationSync ──→ AnimatorParameterBridge             │
│       │                   (PlayState, SetSpeed, etc.)         │
│       │                          │                           │
│       │                          ▼                           │
│       │                     Animator                         │
│       │                   (CrossFade)                        │
│       │                                                      │
│  NetworkAbilitySync ──→ AbilityLayerWeight + AnimState        │
│  NetworkIKSync ─────→ LookAtIK (IIKTargetProvider)           │
└─────────────────────────────────────────────────────────────┘
```

**Kernprinzip:** Die State Machine ist die einzige Autorität für Animationen. Auf dem lokalen Spieler ruft sie `PlayState()` auf. Diese Aufrufe werden erfasst und über FishNet an Remote-Clients verteilt, die denselben `PlayState()`-Aufruf ausführen.

---

## Sync-Daten pro Remote-Spieler

| Daten | Typ | Sync-Art | Rate |
|-------|-----|----------|------|
| `CharacterAnimationState` | `byte` (Enum) | Event (bei Änderung) | ~2-5/s |
| `Speed` | `float` → `byte` (quantisiert) | Periodisch | 20 Hz |
| `VerticalVelocity` | `float` → `short` (quantisiert) | Periodisch | 20 Hz |
| `AbilityLayerWeight` | `float` (0 oder 1) | Event (bei Änderung) | selten |
| `AbilityAnimationState` | `string` | Event (bei Änderung) | selten |
| `LookAtTarget` | `Vector3` (komprimiert) | Periodisch (wenn sichtbar) | 10 Hz |
| `HasLookTarget` | `bool` | Event (bei Änderung) | selten |

**Geschätzte Bandbreite pro Spieler:** ~50-100 Bytes/s (stark komprimiert)

---

## Abgrenzung

**Phase 7 (diese Phase):**
- Animation State Sync (Locomotion, Jump, Fall, Landing, etc.)
- Animator Parameter Sync (Speed, VerticalVelocity)
- Ability Animation Layer Sync
- LookAt IK Target Sync
- Lag Compensation für Animation Timing
- Tests

**Nicht in Phase 7:**
- Hand IK Sync (Phase 9 — erst wenn Combat Abilities existieren)
- Foot IK Sync (nicht nötig — Foot IK ist rein lokal, Terrain-basiert)
- Combat Hit Detection Sync (Phase 9)
- Particle/VFX Sync (spätere Phase)

---

## Schritte

| Schritt | Beschreibung | Branch-Typ | Commit-Message |
|---------|-------------|------------|----------------|
| [7.1](7.1-animation-state-sync.md) | Animation State Sync | `feat/network-animation-state` | `feat(phase-7): 7.1 Animation State Sync` |
| [7.2](7.2-parameter-sync.md) | Animator Parameter Sync | `feat/network-parameter-sync` | `feat(phase-7): 7.2 Animator Parameter Sync` |
| [7.3](7.3-remote-animation-setup.md) | Remote Player Animation Setup | `feat/remote-animation-setup` | `feat(phase-7): 7.3 Remote Player Animation Setup` |
| [7.4](7.4-ability-animation-sync.md) | Ability Animation Sync | `feat/network-ability-sync` | `feat(phase-7): 7.4 Ability Animation Sync` |
| [7.5](7.5-ik-target-sync.md) | IK Target Sync | `feat/network-ik-sync` | `feat(phase-7): 7.5 IK Target Sync` |
| [7.6](7.6-lag-compensation.md) | Lag Compensation & Smoothing | `feat/animation-lag-compensation` | `feat(phase-7): 7.6 Lag Compensation` |
| [7.7](7.7-unit-tests.md) | Unit Tests & Verifikation | `test/network-animation-tests` | `test(phase-7): 7.7 Network Animation Tests` |

---

## Voraussetzungen

- [x] Phase 6 (Netzwerk-Grundstruktur) ausgearbeitet — NetworkPlayer, Input/State Sync
- [x] Phase 8 (IK System) abgeschlossen — IKManager, LookAtIK, IIKTargetProvider
- [x] Phase 5 (Ability System) abgeschlossen — AbilitySystem Events
- [ ] FishNet installiert (Phase 6.1)
- [ ] NetworkPlayer funktioniert mit Bewegung-Sync (Phase 6.4-6.7)

---

## Erwartetes Ergebnis

Nach Abschluss der Phase:
1. **Remote-Spieler animiert:** Andere Spieler zeigen korrekte Lauf/Sprint/Idle-Animationen
2. **State-Übergänge korrekt:** Jump, Fall, Land, Roll, Crouch, Slide werden korrekt dargestellt
3. **Ability-Animationen sichtbar:** Combat-Animationen (Layer 1) werden auf Remote-Spielern abgespielt
4. **LookAt IK synchron:** Remote-Spieler schauen in die richtige Richtung
5. **Flüssig:** Keine Ruckler bei State-Wechseln durch Lag Compensation
6. **Bandbreiten-effizient:** Quantisierte Parameter, Event-basierte States

---

## Nächste Phase

→ Kein direkter Folge-Phase im selben Epic. Phase 7 schließt das Netzwerk-Epic (Grundlage) ab.
Spätere Erweiterungen: Combat-Sync (Phase 9), Alternative Movement Sync (Phase 10).
