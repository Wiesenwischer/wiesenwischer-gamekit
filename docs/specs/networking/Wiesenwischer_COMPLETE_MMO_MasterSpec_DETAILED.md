
# Wiesenwischer GameKit
# FULL AAA MMO NETWORKING + CHARACTER CONTROLLER MASTER SPEC (DETAILED)

This document is a COMPLETE, detailed reference intended to avoid incremental
explanations. It includes architecture, reasoning, implementation strategy,
pitfalls, AAA practices, and system integration guidelines.

This is NOT a summary — it is a full engineering reference.

=====================================================================
SECTION 1 — DESIGN PHILOSOPHY
=====================================================================

Goals:

- MMO-scale networking stability
- Deterministic character simulation
- Modular architecture (GameKit packages)
- Offline + Multiplayer compatibility
- Future extensibility (abilities, mounts, building, etc.)

Core principle:

    Simulation must be independent from networking.

Why?

Because:

- Offline gameplay must remain possible.
- Network stack should be replaceable.
- Prediction should wrap simulation, not rewrite it.

=====================================================================
SECTION 2 — HIGH LEVEL ARCHITECTURE
=====================================================================

Player Flow:

    Input System
        ↓
    Input Interpretation Layer
        ↓
    Ability System
        ↓
    Character Controller Core (Simulation)
        ↓
    Network Adapter (FishNet)
        ↓
    Tick-based Simulation Driver
        ↓
    Visual Interpolation Layer
        ↓
    Animation + Camera

Key rule:

    Simulation != Rendering.

=====================================================================
SECTION 3 — CHARACTER CONTROLLER CORE
=====================================================================

Responsibilities:

- Movement logic
- State machine (Idle, Walking, Running, Jumping, Airborne, Dash)
- Velocity integration
- Collision resolution
- Ability modifiers
- Orientation logic

Must NOT include:

- NetworkBehaviour
- Server authority
- Prediction logic
- Tick scheduling

Entry point:

    Simulate(InputData input, float deltaTime)

Important:

Simulation must be deterministic:

- No randomness.
- No frame-time dependency.
- Same input + same deltaTime = same result.

=====================================================================
SECTION 4 — SIMULATION DRIVER PATTERN
=====================================================================

Offline Mode:

    FixedUpdate()
    {
        motor.Simulate(input, Time.fixedDeltaTime);
    }

Network Mode:

    TimeManager.OnTick += OnTick;

    void OnTick()
    {
        motor.Simulate(input, TimeManager.TickDelta);
    }

Explanation:

Unity FixedUpdate is NOT synchronized across machines.

FishNet Tick IS synchronized.

Prediction requires synchronized timeline.

=====================================================================
SECTION 5 — WHY TICKS INSTEAD OF FIXEDUPDATE
=====================================================================

Unity FixedUpdate:

- local physics clock
- variable between machines
- not network deterministic

FishNet Tick:

- synchronized tick counter
- consistent delta time
- deterministic order

Without tick:

- client simulation diverges
- server corrections occur each tick
- visible forward/back jitter happens

=====================================================================
SECTION 6 — CLIENT SIDE PREDICTION
=====================================================================

Pipeline:

Client:
    - Gather input
    - Simulate locally (prediction)

Server:
    - Simulate authoritative state

Server sends reconcile data.

Client:
    - rewind to authoritative state
    - replay stored inputs

Requirements:

- deterministic simulation
- tick-based input
- identical delta time

=====================================================================
SECTION 7 — RECONCILIATION STRATEGY (AAA LEVEL)
=====================================================================

Bad:

    Always teleport to server position.

Good:

    error = distance(predicted, authoritative)

    if(error > hardThreshold)
        snap
    else
        smooth blend

Reason:

Micro-errors happen naturally — smoothing avoids visual jitter.

=====================================================================
SECTION 8 — SIMULATION VS RENDERING SEPARATION
=====================================================================

Core insight:

Never render directly from simulation transform.

Structure:

CharacterRoot
    SimulationObject (network authoritative)
    VisualRoot (mesh + animator + camera anchor)

SimulationObject:

- moved by prediction/reconcile.

VisualRoot:

- interpolates toward SimulationObject.

Benefits:

- hides reconcile snaps
- stabilizes camera
- smoother animation blending

=====================================================================
SECTION 9 — VISUAL INTERPOLATION
=====================================================================

Example:

    visualPosition = Lerp(previousSimPos, currentSimPos, alpha);

Where alpha represents render interpolation between ticks.

=====================================================================
SECTION 10 — CAMERA ARCHITECTURE
=====================================================================

CameraBrain must remain mode-independent.

Pipeline:

RawInput
    ↓
CameraInputBehaviour (BDO / ArcheAge etc.)
    ↓
OwnershipPolicy
    ↓
CameraBrain
    ↓
Cinemachine

Ownership examples:

BDO:
    camera owns facing.

ArcheAge:
    character owns facing unless RMB pressed.

=====================================================================
SECTION 11 — INPUT SYSTEM (NETWORK SAFE)
=====================================================================

Input MUST be tick-based.

Correct:

    OnTick()
    {
        input = GatherInput();
        Replicate(input);
    }

Incorrect:

    Simulating directly from Update() input.

=====================================================================
SECTION 12 — ANIMATION ARCHITECTURE
=====================================================================

Animator:

- reads simulation state.
- never writes to transform.

Animator modifying position causes prediction mismatch.

=====================================================================
SECTION 13 — IK RULES
=====================================================================

IK must be visual only.

Never modify authoritative transform via IK.

=====================================================================
SECTION 14 — COMMON MMO NETWORKING FAILURES
=====================================================================

- Simulation running in FixedUpdate AND Tick.
- Using Time.deltaTime instead of TickDelta.
- Animator driving movement.
- Camera bound directly to simulation transform.
- Physics randomness.

=====================================================================
SECTION 15 — DEBUG CHECKLIST
=====================================================================

1. Log TimeManager.Tick on client/server.
2. Verify simulation executes once per tick.
3. Check reconcile frequency.
4. Verify no FixedUpdate simulation in network mode.

=====================================================================
SECTION 16 — FINAL TARGET STRUCTURE
=====================================================================

Input
    ↓
Abilities
    ↓
Character Controller Core
    ↓
FishNet Adapter
    ↓
Tick Simulation
    ↓
Visual Interpolation
    ↓
Animation + Camera

END OF COMPLETE MASTER SPEC
