
# Wiesenwischer GameKit
# MMO Networking Architecture – Professional Foundation

## Ziel
Dieses Dokument fasst eine vollständige professionelle Architektur für ein MMO-fähiges
Character Controller + Networking Setup (FishNet basiert) zusammen.

---

## ⭐ Core Prinzipien

- Simulation ist deterministisch.
- Simulation kennt kein Netzwerk.
- Netzwerk ist Adapter/Driver.
- Rendering ist getrennt von Simulation.

Architecture:

Core Controller
    ↓
Network Adapter (FishNet)
    ↓
Tick Simulation
    ↓
Visual Rendering Layer

---

## ⭐ Core Controller

Der Core enthält:

- Movement Logic
- Physics Resolution
- State Machine
- Ability Integration

Core darf NICHT enthalten:

- NetworkBehaviour
- Server/Client Logic
- Prediction Code

Entry:

Simulate(InputData input, float deltaTime)

---

## ⭐ Network Adapter

Aufgaben:

- Input Replication
- Prediction
- Reconcile
- Tick Driver

---

## ⭐ Tick Driven Simulation

FishNet:

TimeManager.OnTick

Nur dort:

motor.Simulate(input, TimeManager.TickDelta)

---

## ⭐ Offline vs Network Driver

Offline:

FixedUpdate → Simulate()

Network:

OnTick → Simulate()

