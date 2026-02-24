
# Wiesenwischer GameKit
# AAA Camera & Character Rendering Separation
## Warum Simulation und Rendering getrennt werden müssen (MMO Networking)

Dieses Dokument beschreibt ein zentrales AAA-Prinzip bei Multiplayer/MMO Character Controllern:

👉 Simulation ≠ Rendering.

Viele Controller funktionieren technisch korrekt, fühlen sich aber trotzdem
ruckelig oder unstabil an, weil die visuelle Darstellung direkt an die
simulierte Netzwerkposition gebunden ist.

---

# ⭐ Grundproblem

Mit Client Prediction + Server Reconcile passiert folgendes:

1) Client simuliert Position (Prediction).
2) Server sendet authoritative Position.
3) Client korrigiert Simulation.

Wenn Rendering direkt die Simulation nutzt:

→ sichtbare Snaps
→ Micro-Jitter
→ Vor/Zurück Bewegung.

---

# ⭐ AAA Lösung: Simulation vs Visual Transform trennen

Anstatt:

```
Transform.position = SimulatedPosition;
```

nutzt man:

```
SimulatedPosition (Network Tick)
        ↓
Visual Interpolation Layer
        ↓
Rendered Transform.position
```

---

# ⭐ Architektur

## Simulation Layer

- läuft im FishNet Tick.
- deterministisch.
- authoritative.

Beispiel:

```
motor.Simulate(input, TimeManager.TickDelta);
```

---

## Visual Layer

- läuft pro Frame (Update/LateUpdate).
- interpoliert zwischen vergangenen Simulation States.

---

# ⭐ Beispielstruktur

```
CharacterRoot
    ├── SimulationObject (unsichtbar, network authoritative)
    └── VisualModel (Mesh, Animator, Camera Anchor)
```

SimulationObject bewegt sich direkt durch Prediction/Reconcile.

VisualModel folgt interpoliert.

---

# ⭐ Interpolation Beispiel

```
Vector3 targetPos = simulatedPosition;
visualPosition = Vector3.Lerp(visualPosition, targetPos, smoothingSpeed * deltaTime);
```

ODER besser:

Tick-basierte Interpolation zwischen vorherigem und aktuellem Tick.

---

# ⭐ Vorteile

- Snap-Korrekturen werden visuell geglättet.
- Prediction bleibt exakt.
- Rendering fühlt sich stabil an.
- Kamera wird ruhiger.

---

# ⭐ Wichtige Regel

Simulation darf NICHT vom Rendering beeinflusst werden.

Rendering darf nur lesen, niemals Simulation verändern.

---

# ⭐ Häufige Fehler

❌ Animator direkt auf Simulation Transform setzen.

❌ Camera direkt auf Simulation Position binden.

❌ Kein Visual Offset Layer.

---

# ⭐ AAA Pattern (Best Practice)

```
NetworkSimulation
        ↓
SimulationTransform
        ↓
Interpolation/Smoothing Layer
        ↓
VisualRoot (Animator, CameraAnchor)
```

---

# ⭐ Zusammenfassung

MMO Controller rendern fast nie die echte simulierte Position.

Stattdessen:

- Simulation bleibt exakt.
- Rendering interpoliert visuell.

Dies trennt Netzwerk-Genauigkeit von visueller Stabilität.
