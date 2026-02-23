
# Wiesenwischer GameKit – FishNet Networking & Character Controller Integration
## Full Technical Summary (1:1 based on discussion)

This document summarizes ALL key points discussed regarding:

- Character Controller architecture
- Networking integration using FishNet
- Prediction & Reconciliation
- Tick synchronization
- Camera input ownership concepts
- Professional MMO-ready structure

This file is intended as an implementation reference (e.g. for Claude).

---

# ⭐ Core Philosophy

The Character Controller Core MUST remain:

- Network agnostic
- Deterministic
- Reusable offline

Architecture:

CharacterController.Core
        +
CharacterController.Network (FishNet Adapter)

The core must NOT know:

- FishNet
- Server/Client roles
- Prediction
- Networking timing

---

# ⭐ Simulation Entry Point (CRITICAL)

Core movement logic:

Simulate(InputData input, float deltaTime)

NO Unity loop inside the core.

---

# ⭐ Simulation Drivers

## Offline / Singleplayer

FixedUpdate()
{
    controller.Simulate(input, Time.fixedDeltaTime);
}

## Network Mode (FishNet)

TimeManager.OnTick += OnTick;

void OnTick()
{
    controller.Simulate(input, TimeManager.TickDelta);
}

IMPORTANT:

Core code stays identical.

ONLY the driver changes.

---

# ⭐ FishNet Tick System

FishNet requires deterministic simulation.

DO NOT use:

- Update()
- FixedUpdate()
- Time.deltaTime
- Time.fixedDeltaTime

Use ONLY:

TimeManager.Tick
TimeManager.TickDelta

---

# ⭐ Prediction Pipeline

FishNet workflow:

1) Client gathers input
2) Client predicts locally
3) Server simulates authoritatively
4) Server sends reconcile state
5) Client rewinds & replays

---

# ⭐ Replicate Structure

struct MoveInput : IReplicateData
{
    int tick;
    Vector2 move;
    Vector2 look;
}

[Replicate]
void Replicate(MoveInput input)
{
    motor.Simulate(input, TimeManager.TickDelta);
}

---

# ⭐ Reconcile Structure

struct MotorState : IReconcileData
{
    int tick;
    Vector3 position;
    Vector3 velocity;
}

[Reconcile]
void Reconcile(MotorState state)
{
    motor.SetState(state);
}

---

# ⭐ Common Causes of Stuttering (Forward/Backward Jitter)

Visible forward/back snapping usually means:

Client prediction != Server simulation.

Primary causes:

1) Double Simulation
Simulation runs in BOTH:

- FixedUpdate()
- TimeManager.OnTick

Result:
Client moves faster → server corrects → snapping.

2) Wrong Delta Time
Using:
- Time.deltaTime
- Time.fixedDeltaTime

Instead of:
TimeManager.TickDelta

3) Unity CharacterController / Rigidbody outside Tick
Movement executed outside network tick causes desync.

4) Input Not Bound to Tick
Input collected per frame instead of per tick.

Correct:

OnTick()
{
    input = GatherInput();
    Replicate(input);
}

---

# ⭐ Debug Checklist

Log tick values:

Debug.Log(TimeManager.Tick);

Check:

- Client tick matches server tick timeline
- No parallel loops running

---

# ⭐ Professional Architecture Pattern

CharacterMotor (Core)
        ↑
NetworkCharacterDriver (FishNet)
        ↑
TimeManager.OnTick

Core does NOT control timing.

---

# ⭐ Camera Ownership Model (High-Level)

Ownership determines who controls orientation:

- Camera owned (BDO style)
- Character owned (ArcheAge style)
- Target owned (future)

Pipeline:

RawInput
    ↓
InputBehaviour (Mode-specific)
    ↓
OwnershipPolicy
    ↓
CameraInputPipeline
    ↓
CameraBrain

CameraBrain must remain mode-independent.

---

# ⭐ Key Rule

Prediction requires:

- Single simulation timeline
- Network tick ownership
- Deterministic delta time

---

# ✅ Conclusion

The existing architecture is NOT wrong.

Likely issue:

- Simulation running in multiple loops
- Input or delta time mismatch

Fix is NOT a rewrite — only restructuring simulation driver.
