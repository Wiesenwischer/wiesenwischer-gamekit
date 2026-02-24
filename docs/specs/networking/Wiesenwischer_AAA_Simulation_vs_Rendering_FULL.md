
# Wiesenwischer GameKit
# Simulation vs Rendering Separation (AAA Standard)

Simulation != Rendering

Simulation:
- tick-based
- authoritative

Rendering:
- interpolated
- smooth visuals

Structure:

CharacterRoot
    SimulationObject
    VisualRoot

VisualRoot folgt interpoliert der Simulation.

