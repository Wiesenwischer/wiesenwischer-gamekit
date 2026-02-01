# 🧠 Client-Side Prediction (CSP) – Spezifikation für Wiesenwischer.GameKit

**Datum:** 2026-01-30

Diese Spezifikation beschreibt die grundlegende Architektur und Umsetzung von Client-Side Prediction (CSP) für den `PlayerController` innerhalb des modularen GameKit-Frameworks.

---

## 🎯 Ziel

Ein Movement-System, das:
- **lokal flüssig reagiert** (Sofortreaktion)
- **für Multiplayer geeignet** ist (mit FishNet o. ä.)
- **Synchronisation mit Server** ermöglicht (Server Authority)
- **Rollback und Reconciliation** unterstützt
- modular und **testbar im SP/Editor** bleibt

---

## 🧩 Komponentenübersicht

```
PlayerController (MonoBehaviour)
├── InputCollector (tickbasiert)
├── MovementMotor (lokale Vorhersage)
├── StateMachine (Grounded, Jumping, etc.)
├── AnimationBridge (optional)
├── PredictionBuffer (Vergangenheitsspeicher)
└── NetworkSyncBridge (z. B. via FishNet)
```

---

## 🔁 Ablauf pro Tick

1. **Input sammeln (Client)**  
   → `moveVector`, `jump`, `dash` usw.  
   → In `InputBuffer` speichern mit Tick-Index

2. **Lokal simulieren (Client)**  
   → Bewegung & States sofort ausführen  
   → Animationen abspielen, VFX, SFX

3. **Input an Server senden (RPC)**  
   → `ServerRpc(ControllerInput input, int tick)`

4. **Server simuliert denselben Input**  
   → Rechnet Position und State nach

5. **Server sendet Zustand zurück (Sync/ObserverRpc)**  
   → `position`, `rotation`, `state`, `tick`

6. **Client vergleicht**  
   → Wenn Abweichung → Rollback + Re-Simulation

---

## 📦 Wichtige Klassen & Strukturen

```csharp
struct ControllerInput
{
    public Vector2 moveVector;
    public bool jump;
    public bool dash;
    public int tick;
}

class InputBuffer<T>
{
    Dictionary<int, T> bufferedInputs;
}

class PredictionBuffer
{
    Dictionary<int, Vector3> positionHistory;
    Dictionary<int, CharacterState> stateHistory;
}
```

---

## 🛠 Anforderungen an MovementMotor

- Muss **deterministisch** sein
- Keine Nutzung von `Time.deltaTime`, sondern `fixedTickDelta`
- Muss rekonstruierbar aus Input sein
- Keine Unity-Physics verwenden (oder klar kapseln)

---

## 🧪 Vorteile des CSP-Setups

| Vorteil                        | Beschreibung |
|-------------------------------|--------------|
| 🔄 Reaktionsschnelles Gameplay | Bewegung wirkt sofort, auch bei 100+ ms Ping |
| 🧩 Modular testbar             | Offline lauffähig, Editor-Playmode möglich |
| 🌐 MMO-ready mit FishNet       | Server Authority, Rollbacks, Multi-Client |
| 🔧 Clean Code & Trennung       | Keine Logik im RPC selbst, sondern via Bridge-Klassen |
| 🚫 Keine Doppellogik nötig     | Ein Bewegungs-Code für Client & Server |

---

## 🧭 Nächste Schritte

- CSP-ready `PlayerController` als UnityPackage implementieren
- `FishNetNetworkBridge` als Add-on (optional)
- TickSystem zentralisieren (auch für AbilitySystem, Combat etc.)

