
# Wiesenwischer GameKit
# FishNet MMO Character Controller – Advanced Prediction Stability Guide

## Ziel

Dieses Dokument fasst die erweiterten Erkenntnisse zur Stabilisierung eines MMO Character Controllers
mit FishNet Prediction zusammen.

Es ergänzt die bisherigen Dokumente mit einem AAA-orientierten Reality Check und beschreibt die
häufigsten Ursachen für Jitter/Stottern — selbst wenn die grundlegende Tick-Integration korrekt ist.

Dieses Dokument kann direkt als Implementierungsleitfaden verwendet werden.

---

# ⭐ Grundprinzip

Prediction funktioniert nur stabil wenn:

- Client und Server dieselbe Simulation Timeline nutzen.
- Simulation deterministisch ist.
- Simulation exakt einmal pro Tick ausgeführt wird.
- Input tick-basiert verarbeitet wird.

FishNet stellt diese Timeline bereit über:

```
TimeManager.OnTick
TimeManager.Tick
TimeManager.TickDelta
```

---

# ⭐ Warum NICHT FixedUpdate

Unity FixedUpdate:

- ist lokal gesteuert
- läuft abhängig von Performance
- ist NICHT zwischen Client und Server synchronisiert

Folge:

Client Simulation ≠ Server Simulation

Server korrigiert ständig → sichtbares Vor/Zurück-Stottern.

---

# ⭐ Richtige Architektur

Core Controller bleibt:

- Netzwerk-unabhängig
- deterministic
- ohne MonoBehaviour loops

Simulation Entry Point:

```
Simulate(InputData input, float deltaTime)
```

---

# ⭐ Simulation Driver Pattern

## Offline Mode

```
FixedUpdate()
{
    motor.Simulate(input, Time.fixedDeltaTime);
}
```

## Network Mode (FishNet)

```
void OnEnable()
{
    TimeManager.OnTick += OnTick;
}

void OnTick()
{
    motor.Simulate(input, TimeManager.TickDelta);
}
```

Core bleibt identisch.

---

# ⭐ Häufigste Ursachen für Jitter trotz korrekter Tick Nutzung

Selbst mit Tick-basierter Simulation kann es zu Problemen kommen.

## 1) Double Simulation

Simulation läuft gleichzeitig in:

- FixedUpdate
- OnTick

Symptom:

- sichtbares Vor/Zurück springen

---

## 2) DeltaTime Mismatch

Falsch:

```
Time.deltaTime
Time.fixedDeltaTime
```

Richtig:

```
TimeManager.TickDelta
```

---

## 3) Input nicht Tick-basiert

Input wird in Update gesammelt und asynchron verwendet.

Richtig:

```
OnTick()
{
    input = GatherInput();
    Replicate(input);
}
```

---

## 4) Reconcile Spam

Server korrigiert jeden Tick → Oscillation Effekt.

Ursache:

Client prediction ≠ Server simulation.

---

# ⭐ AAA-Level Insight – Warum Prediction trotzdem jittert

Viele Controller implementieren technisch korrekt Prediction, aber übersehen:

## Unterschied zwischen Simulation und Rendering

Simulation:

- läuft im Tick
- deterministisch

Rendering:

- läuft pro Frame
- interpoliert visuelle Darstellung

Wenn Rendering direkt die Simulation Position nutzt:

→ sichtbarer jitter trotz korrekter Simulation.

Professionelle Lösung:

- Simulation Position getrennt von Visual Position.
- Visual Transform interpoliert zwischen Ticks.

---

# ⭐ Recommended Visual Smoothing Layer

```
SimulatedPosition (Tick Based)
        ↓
VisualInterpolation
        ↓
Transform.position
```

Dies reduziert sichtbare Snap-Korrekturen.

---

# ⭐ Debug Reality Checks

## Tick Synchronisation prüfen

```
Debug.Log(TimeManager.Tick);
```

## Simulation Count prüfen

```
Debug.Log("Simulate called");
```

Erwartet: ≈ TickRate pro Sekunde.

## Reconcile Frequency prüfen

```
Debug.Log("Reconcile");
```

Zu häufig → Simulation mismatch.

---

# ⭐ Zusammenfassung

Prediction benötigt:

- eine einzige Simulation Timeline
- Tick-basierte Inputs
- deterministische DeltaTime
- Simulation ausschließlich im Network Tick
- getrennte Simulation und Visual Darstellung

Das Problem erfordert keinen Rewrite.

Meist reicht:

👉 Simulation Driver refactor.
