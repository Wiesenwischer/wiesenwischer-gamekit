
# Wiesenwischer GameKit – Simulation Loop (Offline vs FishNet Network)
## Warum Simulation über Network Ticks laufen muss

Diese Spezifikation fasst die wichtigsten Punkte zusammen, warum bei Verwendung von
FishNet Prediction die Simulation über das Tick-System laufen muss und NICHT über
Unity FixedUpdate oder Update.

---

# ⭐ Grundproblem

Prediction funktioniert nur stabil, wenn:

👉 Client und Server exakt dieselbe Simulations-Timeline verwenden.

FishNet garantiert diese Synchronität ausschließlich über:

```
TimeManager.OnTick
```

Unity Lifecycle Methoden wie:

- FixedUpdate()
- Update()
- LateUpdate()

sind lokal unterschiedlich getimed und NICHT zwischen Client und Server synchronisiert.

---

# ⭐ Warum FixedUpdate nicht geeignet ist

Unity FixedUpdate:

- läuft lokal abhängig von Performance
- kann auf Client und Server unterschiedliche Frequenzen haben
- basiert auf Unity Physics Timing
- ist NICHT deterministisch zwischen Netzwerkinstanzen

Folge:

Client predicted Bewegung ≠ Server Simulation

Server korrigiert → Client wird zurückgesetzt → sichtbares Vor/Zurück-Stottern.

---

# ⭐ FishNet Tick-System

FishNet stellt bereit:

```
TimeManager.Tick
TimeManager.TickDelta
TimeManager.OnTick
```

Diese Tick Timeline:

- ist zwischen Client und Server synchronisiert
- wird für Prediction genutzt
- garantiert gleiche Simulation Reihenfolge.

---

# ⭐ Simulation Architektur

Der Character Controller Core bleibt weiterhin:

✅ Netzwerk-unabhängig  
✅ deterministisch  
✅ wiederverwendbar offline

Core enthält KEINE MonoBehaviour Loops.

---

## Core Simulation

```
Simulate(InputData input, float deltaTime)
```

Core weiß NICHT:

- ob offline oder online
- ob Server oder Client
- ob Prediction aktiv ist.

---

# ⭐ Offline Simulation (Singleplayer)

Offline Driver ruft Simulation z.B. aus FixedUpdate auf:

```
FixedUpdate()
{
    motor.Simulate(input, Time.fixedDeltaTime);
}
```

Hier darf FixedUpdate genutzt werden, weil keine Netzwerk-Synchronisierung nötig ist.

---

# ⭐ Network Simulation (FishNet)

Network Adapter übernimmt Kontrolle:

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

WICHTIG:

Simulation darf jetzt NUR im Tick laufen.

---

# ⭐ Unterschied Offline vs Network

| Mode | Simulation Driver |
|------|------------------|
| Offline | FixedUpdate |
| Network | TimeManager.OnTick |

Core Code bleibt identisch.

---

# ⭐ Was konkret geändert werden muss

✅ Movement / Gravity / Controller.Move etc. nur im Tick ausführen.

❌ Entfernen aus:

- FixedUpdate()
- Update()
- LateUpdate()

wenn Networking aktiv ist.

---

## DeltaTime ersetzen

NICHT verwenden:

- Time.deltaTime
- Time.fixedDeltaTime

Stattdessen:

```
TimeManager.TickDelta
```

---

## Input Handling

Input sollte pro Tick gesammelt werden:

```
OnTick()
{
    input = GatherInput();
    Replicate(input);
}
```

---

# ⭐ Zusammenfassung

FixedUpdate = lokale Physik-Zeit  
FishNet Tick = synchronisierte Netzwerk-Zeit

Prediction benötigt synchronisierte Zeit.

Die Lösung ist KEIN Rewrite, sondern:

👉 Simulation Entry Point vom Unity Loop auf FishNet Tick verschieben.
