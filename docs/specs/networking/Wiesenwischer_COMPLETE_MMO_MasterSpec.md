
# Wiesenwischer GameKit
# COMPLETE MMO NETWORKING & CHARACTER CONTROLLER MASTER SPEC
## (All advanced insights, pitfalls, architecture and implementation notes)

This document contains ALL remaining advanced MMO networking and character controller
knowledge discussed or referenced. It is meant as a complete reference so no further
step-by-step prompting is required.

Goal:

- Stable MMO-ready architecture
- FishNet prediction compatibility
- AAA-level character feel
- Modular GameKit structure
- Future-proof system design

---

# ⭐ CORE PRINCIPLES

1) Simulation must be deterministic.
2) Simulation must be network-agnostic.
3) Network logic must exist in adapters.
4) Rendering must be separated from simulation.
5) Camera and animation must never drive simulation.

---

# ⭐ HIGH LEVEL ARCHITECTURE

Input System
    ↓
Input Interpretation Layer
    ↓
Ability System
    ↓
Character Controller Core (NO networking)
    ↓
Network Adapter (FishNet)
    ↓
Tick-based Simulation
    ↓
Visual Interpolation Layer
    ↓
Animation + Camera

---

# ⭐ CHARACTER CONTROLLER CORE

Responsibilities:

- Movement logic
- State machine (grounded, in air, dash, etc.)
- Physics resolution
- Ability integration
- Movement intent processing

Core MUST NOT include:

- NetworkBehaviour
- Tick management
- Prediction
- Server authority logic

Entry point:

Simulate(InputData input, float deltaTime)

---

# ⭐ SIMULATION DRIVER PATTERN

Offline:

FixedUpdate()
{
    motor.Simulate(input, Time.fixedDeltaTime);
}

Network:

TimeManager.OnTick → motor.Simulate(input, TimeManager.TickDelta);

Core logic stays identical.

---

# ⭐ WHY FISHNET TICKS (NOT FixedUpdate)

Prediction requires:

- identical simulation timeline
- identical delta time
- deterministic execution order

Unity FixedUpdate:

- local only
- not synchronized between peers

FishNet Tick:

- synchronized timeline
- deterministic sequence

---

# ⭐ CLIENT SIDE PREDICTION PIPELINE

Client:
- gather input
- simulate locally

Server:
- simulate authoritative

Server sends state → client reconcile.

---

# ⭐ RECONCILIATION STRATEGY (AAA)

Never always hard snap.

if(error > threshold)
    hard correct
else
    smooth blend

---

# ⭐ COMMON JITTER CAUSES

1. Double simulation (FixedUpdate + OnTick).
2. Using Time.deltaTime instead of TickDelta.
3. Physics executed outside tick loop.
4. Input frame-based instead of tick-based.
5. Animator influencing transform.
6. Camera following simulation directly.
7. Rendering using raw simulated position.

---

# ⭐ SIMULATION VS RENDERING (AAA PATTERN)

SimulationTransform (authoritative)
        ↓
Interpolation Layer
        ↓
VisualRoot (Animator, Mesh, CameraAnchor)

Never render directly from simulation transform.

---

# ⭐ VISUAL INTERPOLATION

visualPosition = Lerp(previousSimPos, currentSimPos, alpha);

Benefits:

- hides reconcile snaps
- smoother camera
- stable animation blending

---

# ⭐ CAMERA ARCHITECTURE

CameraBrain must remain mode independent.

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

Ownership examples:

BDO style:
- camera owns orientation

ArcheAge style:
- character owns orientation unless RMB pressed

---

# ⭐ INPUT ARCHITECTURE

Input collected per tick.

OnTick():
    input = GatherInput()
    Replicate(input)

Never simulate using frame-only input.

---

# ⭐ NETWORK ADAPTER (FishNet)

Responsibilities:

- replicate input
- run prediction
- receive reconcile
- trigger simulation via tick

---

# ⭐ ANIMATION ARCHITECTURE

Animator reads from:

- movement state
- velocity
- ability state

Animator must NEVER modify physics or simulation state.

---

# ⭐ IK (IMPORTANT NOTES)

IK should:

- run only visually
- not influence authoritative transform

Otherwise prediction mismatch occurs.

---

# ⭐ MMO PERFORMANCE STRATEGY

Server:

- simplified physics
- minimal animation logic

Client:

- visual smoothing
- camera effects
- animation blending

---

# ⭐ ADVANCED AAA INSIGHTS

1) Simulation tick != render frame.
2) Visual smoothing layer is mandatory.
3) Separate simulation root and visual root.
4) Prediction must be deterministic.
5) Input must be tied to tick timeline.
6) Avoid physics randomness.

---

# ⭐ DEBUG CHECKLIST

Log TimeManager.Tick on client and server.

Verify:

- simulation runs only once per tick
- reconcile not spamming every tick
- no FixedUpdate simulation active in network mode

---

# ⭐ FINAL TARGET ARCHITECTURE

Player
  Input
    Ability System
      Character Controller Core
        Network Adapter
          Tick Simulation
            Visual Interpolation
              Animation + Camera

---

# END OF MASTER SPEC
