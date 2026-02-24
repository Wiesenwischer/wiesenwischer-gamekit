
# ⭐ FishNet Prediction Reality Check (MMO Character Controller)

## Ziel

Mit diesem Test kann schnell überprüft werden, ob das Prediction-Setup korrekt implementiert wurde oder ob grundlegende Synchronisationsprobleme bestehen.

Dieser Test hilft speziell bei:

- Vorwärts/Rückwärts-Stottern
- Micro-Jitter
- permanentes Reconcile

---

# ⭐ Grundidee

Prediction funktioniert nur stabil, wenn:

- Client und Server dieselbe Tick Timeline nutzen
- Simulation deterministisch ist
- Simulation nur einmal pro Tick läuft

---

# ⭐ Test 1 — Tick Synchronisation prüfen

Logge auf Client UND Server:

```csharp
Debug.Log(TimeManager.Tick);
```

### Erwartetes Verhalten

- Tick steigt kontinuierlich.
- Client Tick läuft leicht voraus (Prediction).
- Server Tick folgt stabil.

### Warnzeichen

- Tick springt.
- Tick driftet stark auseinander.
- Tick wird mehrfach pro Frame erhöht.

---

# ⭐ Test 2 — Double Simulation erkennen

Füge temporär Log ein:

```csharp
Debug.Log("Simulate called");
```

### Erwartet:

- Anzahl Logs pro Sekunde ≈ TickRate

### Problem:

- Mehr Logs → Simulation läuft mehrfach.

Typische Ursache:

- FixedUpdate + OnTick gleichzeitig aktiv.

---

# ⭐ Test 3 — DeltaTime Konsistenz

Suche im Code nach:

```
Time.deltaTime
Time.fixedDeltaTime
```

Diese dürfen NICHT in der Network Simulation verwendet werden.

Erlaubt:

```
TimeManager.TickDelta
```

---

# ⭐ Test 4 — Reconcile Spam

Logge Reconcile:

```csharp
Debug.Log("Reconcile");
```

### Erwartet:

- gelegentliche Reconcile Events.

### Problem:

- Reconcile jedes Tick.

Das bedeutet:

Client Prediction != Server Simulation

---

# ⭐ Test 5 — Input Synchronisation

Input MUSS pro Tick gesammelt werden:

```csharp
OnTick()
{
    input = GatherInput();
    Replicate(input);
}
```

Warnzeichen:

- Input wird nur in Update gelesen.
- Input wird nicht mit Tick synchronisiert.

---

# ⭐ Häufigste Ursachen für Vor/Zurück Stottern

- Simulation läuft in FixedUpdate UND OnTick.
- DeltaTime nicht synchronisiert.
- Physics außerhalb Tick.
- Input nicht tick-basiert.

---

# ⭐ Zusammenfassung

FishNet Prediction benötigt:

- Eine einzige Simulation Timeline.
- Simulation ausschließlich im Tick.
- Deterministische DeltaTime.
- Tick-basierte Inputs.

Wenn einer dieser Punkte verletzt wird:

Server korrigiert permanent.

Sichtbares Vor/Zurück-Jitter entsteht.
