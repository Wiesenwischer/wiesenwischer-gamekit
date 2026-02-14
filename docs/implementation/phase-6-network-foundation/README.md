# Phase 6: Netzwerk-Grundstruktur

**Epic:** MMO-Netzwerk & Synchronisation
**Branch:** `integration/phase-6-network-foundation`
**Status:** Offen
**Abhängigkeiten:** Phase 5 (Ability System) ✅

---

## Ziel

FishNet als Netzwerk-Framework integrieren und die bestehende CSP-Infrastruktur (TickSystem, InputBuffer, PredictionBuffer) zu einem funktionierenden Multiplayer-System verbinden. Am Ende der Phase kann ein lokaler Host gestartet werden, in dem sich ein zweiter Client verbindet und beide Spieler sich gegenseitig sehen und bewegen.

---

## Architektur-Übersicht

```
┌─────────────────────────────────────────────────────────────┐
│  CharacterController.Core (bestehend, FishNet-frei)         │
│  ┌──────────┐ ┌────────────┐ ┌──────────────┐              │
│  │TickSystem│ │InputBuffer │ │PredictionBuf.│              │
│  └──────────┘ └────────────┘ └──────────────┘              │
│  ┌──────────────┐ ┌────────────────┐ ┌──────────────┐      │
│  │IPrediction   │ │IMovementInput  │ │INetworkRole  │ NEU  │
│  │System        │ │Provider        │ │              │      │
│  └──────────────┘ └────────────────┘ └──────────────┘      │
│  ┌──────────────┐ ┌────────────────┐                        │
│  │PlayerControl.│ │CharacterMotor  │                        │
│  └──────────────┘ └────────────────┘                        │
└─────────────────────────────────────────────────────────────┘
                          ▲
                          │ referenziert
┌─────────────────────────┴───────────────────────────────────┐
│  Network.FishNet (NEU)                                       │
│  ┌──────────────┐ ┌────────────────┐ ┌──────────────┐       │
│  │NetworkPlayer │ │NetworkInput    │ │NetworkState  │       │
│  │(NetworkBeh.) │ │Sync            │ │Sync          │       │
│  └──────────────┘ └────────────────┘ └──────────────┘       │
│  ┌──────────────┐ ┌────────────────┐ ┌──────────────┐       │
│  │GameNetwork   │ │ClientPrediction│ │RemotePlayer  │       │
│  │Manager       │ │System          │ │Interpolator  │       │
│  └──────────────┘ └────────────────┘ └──────────────┘       │
└─────────────────────────────────────────────────────────────┘
```

**Kernprinzip:** `CharacterController.Core` bleibt FishNet-frei. Alle FishNet-spezifischen Klassen leben im neuen `Network.FishNet`-Package. Die Verbindung läuft über Interfaces (`INetworkRole`, `IPredictionSystem`, `IPredictable`).

---

## Bestehende CSP-Infrastruktur

Folgende Klassen existieren bereits und werden in Phase 6 **genutzt, nicht neu erstellt**:

| Klasse | Paket | Zweck |
|--------|-------|-------|
| `TickSystem` | Core | Deterministischer 60-Hz-Tick |
| `ControllerInput` | Core/Prediction | Serialisierbare Input-Daten (Tick, Move, Look, Buttons) |
| `ControllerButtons` | Core/Prediction | Bitflags für Aktionen (Jump, Sprint, etc.) |
| `InputBuffer<T>` | Core/Prediction | Ring-Buffer für Input-History |
| `PredictionState` | Core/Prediction | Snapshot des Character-Zustands pro Tick |
| `PredictionBuffer` | Core/Prediction | Ring-Buffer für State-History + ValidateAgainstServer() |
| `IPredictionSystem` | Core/Prediction | Interface für Prediction-Lifecycle |
| `IPredictable` | Core/Prediction | Interface für vorhersagbare Objekte |
| `InputSnapshot` | Core/Input | Alternative Input-Serialisierung (wird evaluiert) |

---

## Abgrenzung

**Phase 6 (diese Phase):**
- FishNet installieren & Package-Struktur
- Netzwerk-Interfaces in Core
- NetworkPlayer (Spawn, Authority)
- Input-Sync (Client → Server)
- State-Sync & Reconciliation (Server → Client)
- Remote-Player-Interpolation (Position/Rotation)
- Tests

**Phase 7 (nächste):**
- Animator-Parameter-Sync
- State-Machine-Sync (Animation States)
- Ability-Sync über Netzwerk
- IK-Target-Sync (nur wenn Spieler sichtbar)
- Lag Compensation für Animationen

---

## Schritte

| Schritt | Beschreibung | Branch-Typ | Commit-Message |
|---------|-------------|------------|----------------|
| [6.1](6.1-fishnet-package-setup.md) | FishNet Installation & Package-Struktur | `feat/network-package-setup` | `feat(phase-6): 6.1 FishNet Installation & Network Package` |
| [6.2](6.2-network-abstractions.md) | Netzwerk-Interfaces in Core | `feat/network-abstractions` | `feat(phase-6): 6.2 Network Abstractions in Core` |
| [6.3](6.3-network-manager.md) | NetworkManager & Connection Setup | `feat/network-manager` | `feat(phase-6): 6.3 NetworkManager & Connection Setup` |
| [6.4](6.4-network-player.md) | NetworkPlayer Component | `feat/network-player` | `feat(phase-6): 6.4 NetworkPlayer Component` |
| [6.5](6.5-input-sync.md) | Input Sync (Client → Server) | `feat/network-input-sync` | `feat(phase-6): 6.5 Input Sync` |
| [6.6](6.6-state-sync.md) | State Sync & Reconciliation | `feat/network-state-sync` | `feat(phase-6): 6.6 State Sync & Reconciliation` |
| [6.7](6.7-remote-interpolation.md) | Remote Player Interpolation | `feat/remote-interpolation` | `feat(phase-6): 6.7 Remote Player Interpolation` |
| [6.8](6.8-unit-tests.md) | Unit Tests & Verifikation | `test/network-tests` | `test(phase-6): 6.8 Network Unit Tests` |

---

## Voraussetzungen

- [x] Phase 5 (Ability System) abgeschlossen
- [ ] FishNet Asset (kostenlos) — Download von [GitHub](https://github.com/FirstGearGames/FishNet) oder Unity Asset Store
- [ ] Unity 2022.3 LTS (kompatibel mit FishNet 4.x)

---

## Erwartetes Ergebnis

Nach Abschluss der Phase:
1. **Host-Modus** funktioniert: Ein Spieler startet als Host, ein zweiter verbindet sich
2. **Bewegung synchronisiert:** Beide Spieler sehen sich gegenseitig in Echtzeit bewegen
3. **Client-Side Prediction aktiv:** Lokale Bewegung fühlt sich sofort an, Server korrigiert bei Abweichung
4. **Determinismus:** Gleicher Input → gleiche Position (Server und Client)
5. **Kein FishNet-Leak:** `CharacterController.Core` hat keine FishNet-Abhängigkeit

---

## Nächste Phase

→ [Phase 7: Netzwerk-Animation](../phase-7-network-animation/) — Animator-Sync, State-Machine-Sync, Ability-Sync
