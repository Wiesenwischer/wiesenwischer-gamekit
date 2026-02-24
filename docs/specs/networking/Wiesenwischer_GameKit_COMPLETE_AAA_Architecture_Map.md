
# Wiesenwischer GameKit
# COMPLETE AAA MMO ARCHITECTURE MAP
## Character Controller + Networking + Camera + Ability System

Dieses Dokument stellt die vollständige Architekturübersicht dar, wie alle
Systeme im GameKit zusammenarbeiten sollen.

Ziel:

- Klare Systemgrenzen
- Modularität
- MMO Networking readiness
- Erweiterbarkeit ohne Refactoring

---

# ⭐ High-Level System Overview

Player
    ↓
Input System
    ↓
Input Interpretation Layer
    ↓
Ability System
    ↓
Character Controller Core
    ↓
Network Adapter (FishNet)
    ↓
Simulation Layer (Tick-based)
    ↓
Visual Interpolation Layer
    ↓
Animation + Camera

---

# ⭐ 1. Input System

Aufgaben:

- Unity Input System lesen
- Device Abstraction
- RawInput erzeugen

RawInput enthält:

- Movement Input
- Mouse Delta
- Buttons
- Ability Trigger

---

# ⭐ 2. Input Behaviour Layer

Mode-spezifische Interpretation:

- BDO Camera Mode
- ArcheAge Camera Mode

Verarbeitet:

RawInput → CameraInput + CharacterIntent

---

# ⭐ 3. Ability System

Abilities sind Gameplay Aktionen:

- Movement Ability (walk/run/jump)
- Combat Ability
- Build Ability
- Mount Ability

Ability entscheidet:

- darf Movement passieren?
- verändert Movement Stats?

---

# ⭐ 4. Character Controller Core

Enthält:

- Movement Logic
- State Machine
- Physics
- IK Hooks
- Ability Integration

Wichtig:

KEIN Netzwerkcode.

Entry:

Simulate(InputData input, float deltaTime)

---

# ⭐ 5. Network Adapter (FishNet)

Aufgaben:

- Replicate Input
- Prediction
- Reconcile
- Tick Driver

Simulation wird hier gestartet:

TimeManager.OnTick → motor.Simulate()

---

# ⭐ 6. Simulation Layer

Tick basiert.

Deterministisch.

Autoritativ.

---

# ⭐ 7. Visual Layer (AAA Pattern)

Simulation Transform ≠ Visual Transform.

Structure:

CharacterRoot
    SimulationObject
    VisualRoot
        Mesh
        Animator
        CameraAnchor

VisualRoot folgt interpoliert der Simulation.

---

# ⭐ 8. Camera System

CameraBrain kennt KEINE Game Modes.

Pipeline:

RawInput
    ↓
CameraInputBehaviour
    ↓
OwnershipPolicy
    ↓
CameraBrain
    ↓
Cinemachine

---

# ⭐ 9. Animation System

Animator reagiert auf:

- Character States
- Movement Direction
- Ability States

Animator darf NICHT Simulation beeinflussen.

---

# ⭐ 10. Networking Core Rules

- Simulation nur im Tick.
- deltaTime = TimeManager.TickDelta.
- Input tick-basiert.
- Rendering interpoliert.

---

# ⭐ FULL DATA FLOW

Input → Ability → CharacterController → NetworkAdapter → Simulation
            ↓
        Animation + Camera (Visual Layer)

---

# ⭐ Ziel dieser Architektur

- Offline spielbar.
- Multiplayer ready.
- MMO skalierbar.
- Modular erweiterbar.

