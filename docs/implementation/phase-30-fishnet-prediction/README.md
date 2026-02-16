# Phase 30: FishNet Native Prediction Migration

**Epic:** MMO-Netzwerk & Synchronisation
**Branch:** `integration/phase-30-fishnet-prediction`
**Status:** Offen
**Abhaengigkeiten:** Phase 6 (Netzwerk-Grundstruktur) + Phase 7 (Netzwerk-Animation) ✅

---

## Ziel

Das custom CSP-System (TickSystem, InputBuffer, PredictionBuffer, NetworkInputSync, NetworkStateSync) aus Phase 6 durch **FishNet Native Prediction** ersetzen (`[Replicate]`/`[Reconcile]`, `TimeManager`). Die Bewegungslogik (CharacterLocomotion, Motor, StateMachine) bleibt unveraendert — nur die Orchestrierungsschicht wird umgebaut.

**Bekannte Probleme die behoben werden:**
1. Keine Tick-Synchronisation (TickSystem startet bei Tick 0 unabhaengig)
2. CameraYaw wird nicht serialisiert (Server berechnet mit Yaw=0)
3. PredictionBuffer.TryGet() schlaegt staendig fehl (Hard-Correction jeden Frame)
4. Host-Input-Verlust nach Fokuswechsel

---

## Architektur-Uebersicht

```
┌─────────────────────────────────────────────────────────────────┐
│  CharacterController.Core (FishNet-frei)                         │
│  ┌──────────────┐ ┌──────────────────┐ ┌─────────────────┐      │
│  │ISimulation   │ │PlayerController  │ │CharacterMotor   │      │
│  │Driver (NEU)  │ │.SimulateTick()   │ │System           │      │
│  └──────────────┘ └──────────────────┘ └─────────────────┘      │
│  ┌──────────────┐ ┌──────────────────┐ ┌─────────────────┐      │
│  │StateMachine  │ │CharacterLoco-    │ │AbilitySystem    │      │
│  │(deltaTime)   │ │motion            │ │                 │      │
│  └──────────────┘ └──────────────────┘ └─────────────────┘      │
└─────────────────────────────────────────────────────────────────┘
                          ▲
                          │ treibt Simulation via ISimulationDriver
┌─────────────────────────┴───────────────────────────────────────┐
│  Network.FishNet (FishNet-spezifisch)                            │
│  ┌──────────────────┐ ┌──────────────────┐ ┌────────────────┐   │
│  │NetworkCharacter   │ │MoveReplicate     │ │CharacterRecon- │   │
│  │Driver (NEU)       │ │Data (NEU)        │ │cileData (NEU)  │   │
│  │[Replicate]        │ │IReplicateData    │ │IReconcileData  │   │
│  │[Reconcile]        │ │                  │ │                │   │
│  └──────────────────┘ └──────────────────┘ └────────────────┘   │
│  ┌──────────────┐ ┌──────────────────┐ ┌────────────────┐       │
│  │NetworkPlayer │ │NetworkAnimation  │ │GameNetwork     │       │
│  │              │ │Sync              │ │Manager         │       │
│  └──────────────┘ └──────────────────┘ └────────────────┘       │
└─────────────────────────────────────────────────────────────────┘
```

**Kernprinzip:** `CharacterController.Core` bleibt FishNet-frei. Der neue `NetworkCharacterDriver` (FishNet `TickNetworkBehaviour`) treibt die Simulation via `TimeManager.OnTick`. Im Offline-Modus treibt `PlayerController.FixedUpdate()` die Simulation direkt. Core weiss nicht ob offline/online.

---

## Relevante Spezifikationen

- [Phase 30 Spezifikation](../../specs/networking/Wiesenwischer_Phase30_FishNet_Native_Prediction_Migration.md)
- [CSP Spezifikation](../../specs/CSP_Spezifikation.md)
- [GameKit MMO Basics](../../specs/GameKit_MMO_Basics.md)
- [Simulation Tick vs FixedUpdate](../../specs/networking/Wiesenwischer_Simulation_Tick_vs_FixedUpdate.md)
- [FishNet Integration FULL](../../specs/networking/Wiesenwischer_FishNet_Integration_FULL.md)
- [AAA Simulation vs Rendering](../../specs/networking/Wiesenwischer_AAA_Simulation_vs_Rendering_FULL.md)

---

## Abgrenzung

**Phase 30 (diese Phase):**
- ISimulationDriver Interface in Core
- Deterministisches Timing (deltaTime durchreichen statt Time.deltaTime)
- FixedUpdate Offline-Modus
- FishNet `[Replicate]`/`[Reconcile]` Datenstrukturen
- NetworkCharacterDriver als zentrale Netzwerk-Komponente
- PlayerController + NetworkPlayer Integration
- Alten Prediction-Code aufraeumen
- NetworkAnimationSync Replay-Guard
- Tests & Verifikation

**Spaetere Phasen:**
- Adaptive Reconciliation (Smooth Blend statt Hard Snap)
- Lag Compensation fuer Combat
- Server-Authority Validation (Anti-Cheat)
- NPC-spezifische Netzwerk-Simulation

---

## Schritte

| Schritt | Beschreibung | Branch-Typ | Commit-Message |
|---------|-------------|------------|----------------|
| [30.1](30.1-simulation-driver.md) | ISimulationDriver + SimulateTick Extraktion | `feat/simulation-driver` | `feat(phase-30): 30.1 ISimulationDriver + SimulateTick Extraktion` |
| [30.2](30.2-deterministic-timing.md) | Deterministisches Timing — deltaTime durchreichen | `feat/deterministic-timing` | `feat(phase-30): 30.2 Deterministisches Timing` |
| [30.3](30.3-offline-fixedupdate.md) | FixedUpdate Offline-Modus | `feat/offline-fixedupdate` | `feat(phase-30): 30.3 FixedUpdate Offline-Modus` |
| [30.4](30.4-replicate-reconcile-data.md) | MoveReplicateData + CharacterReconcileData | `feat/replicate-reconcile-data` | `feat(phase-30): 30.4 MoveReplicateData + CharacterReconcileData` |
| [30.5](30.5-network-character-driver.md) | NetworkCharacterDriver | `feat/network-character-driver` | `feat(phase-30): 30.5 NetworkCharacterDriver` |
| [30.6](30.6-network-integration.md) | PlayerController + NetworkPlayer Integration | `feat/network-integration` | `feat(phase-30): 30.6 PlayerController + NetworkPlayer Integration` |
| [30.7](30.7-prediction-cleanup.md) | Alten Prediction-Code aufraeumen | `refactor/prediction-cleanup` | `refactor(phase-30): 30.7 Alten Prediction-Code aufraeumen` |
| [30.8](30.8-animation-sync-replay.md) | NetworkAnimationSync Tick-Anpassung | `fix/animation-sync-replay` | `fix(phase-30): 30.8 NetworkAnimationSync Replay-Guard` |
| [30.9](30.9-tests-verification.md) | Tests + Verifikation | `test/network-prediction-tests` | `test(phase-30): 30.9 Network Prediction Tests` |

---

## Voraussetzungen

- [x] Phase 6 (Netzwerk-Grundstruktur) abgeschlossen
- [x] Phase 7 (Netzwerk-Animation) abgeschlossen
- [x] FishNet Asset installiert (unter `Assets/FishNet/`)
- [x] ParrelSync fuer Multiplayer-Testing
- [ ] Phase 30 Spezifikation gelesen und verstanden

---

## Erwartetes Ergebnis

Nach Abschluss der Phase:
1. **FishNet Native Prediction aktiv:** `[Replicate]`/`[Reconcile]` statt custom CSP
2. **Tick-Synchronisation:** `TimeManager` synchronisiert Ticks zwischen Client und Server
3. **CameraYaw korrekt:** Server berechnet Bewegung mit richtigem Yaw
4. **Kein Teleport-Jitter:** Reconciliation nur bei echter Abweichung
5. **Offline-Modus:** `FixedUpdate()` treibt Simulation ohne FishNet
6. **Deterministisches Timing:** Kein `Time.deltaTime`/`Time.time` in Simulation-Code
7. **Visual Interpolation:** Smooth Frame-Interpolation via Motor Pre/Post-Interpolation
8. **Core bleibt FishNet-frei:** Keine FishNet-Dependency in `CharacterController.Core`

---

## Naechste Phase

Die naechste Netzwerk-Phase waere eine optionale Optimierung:
- Adaptive Reconciliation (Smooth Blend, Error-Threshold)
- Lag Compensation fuer Combat (Phase 9 Abhaengigkeit)
