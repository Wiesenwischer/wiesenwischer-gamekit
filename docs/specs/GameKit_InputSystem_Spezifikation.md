# 🎮 Input-System Spezifikation – Wiesenwischer.GameKit

**Datum:** 2026-01-30

Diese Spezifikation beschreibt den Aufbau eines modularen Input-Systems basierend auf dem Unity Input System für das GameKit-Framework. Ziel ist die saubere Entkopplung, zentrale Verwaltung und optionale Netzwerkerweiterung (FishNet).

---

## 🎯 Ziele

- Unterstützung für Unity Input System (`InputActionAsset`)
- Klare Trennung von Eingabe, Auswertung und Weitergabe
- Modulares System für:
  - Movement
  - Combat
  - BuildSystem
  - Interaktionen
- **Umschaltbare Action Maps** (per State oder Kontext)
- Einbindung von Mehrspieler-Unterstützung über **FishNet**
- Testbarkeit durch Abstraktion
- Kompatibilität mit Client-Side-Prediction und Server-Authority

---

## 🧱 Komponentenstruktur

```
GameInputManager (MonoBehaviour)
├── InputActionAsset (referenziert Unity InputAsset)
├── InputContextHandler (Map-Switching je nach State)
├── InputProviderRegistry (alle Interfaces wie IMovementInputProvider etc.)
└── (optional) InputSyncFishNet
```

---

## 🔌 Interfaces

```csharp
public interface IMovementInputProvider
{
    Vector2 MoveInput { get; }
    bool JumpPressed { get; }
    bool DashPressed { get; }
}

public interface ICombatInputProvider
{
    int SelectedSkillSlot { get; }
    bool SkillActivated(int slotIndex);
}
```

Weitere Interfaces möglich: `IBuildInputProvider`, `IVehicleInputProvider`, ...

---

## 🧩 Modularität

| Package                                | Beschreibung |
|----------------------------------------|--------------|
| `wiesenwischer.gamekit.input`          | Basissystem mit ActionAsset + Routing |
| `wiesenwischer.gamekit.input.fishnet`  | Erweiterung für Netzwerk-Input + Authority |
| `wiesenwischer.gamekit.controller.*`   | Verbraucht konkreten Input je nach Controller-Typ |

---

## 🌐 Netzwerk-Erweiterung via FishNet

### Aufgabe des Pakets `gamekit.input.fishnet`:

- Nur auf **lokalem Authority-Client** werden Inputs aktiv verarbeitet
- Optional: **InputForwarding an Server**
- Validierung von **Input-Frequenz, Gültigkeit, Bewegungslimits**
- Sync relevanter Input-Events per **`Command()` oder `Networked<T>`**
- Kann als Middleware eingeklinkt werden, z. B.:

```csharp
void OnMoveInput(Vector2 value)
{
    if (IsOwner)
        MoveInput = value;
    if (IsServer && Validate(value))
        ExecuteMovement(value);
}
```

---

## 🧠 State-abhängige Input-Umschaltung

```csharp
void SetInputContext(string contextName)
{
    inputActionAsset.SwitchCurrentActionMap(contextName);
}
```

Beispiel-Kontexte:
- `default`
- `combat`
- `buildmode`
- `vehicle`

---

## 🔁 Erweiterbarkeit

- Unterstützt Custom Devices (z. B. Gamepad, Touch, VR)
- Mehrere Spieler auf einem Gerät möglich (per InputUser)
- Eigene Konfigurations- und Rebinding-Systeme denkbar
- Modular über **Unity Packages + Assembly Definitions**

---

## 🧪 Testbarkeit

- Mockbare `IInputProvider`-Interfaces für Unit Tests
- Trennung von Eingabe und Verhalten (Clean Architecture)
- Simulierbares Input-Playback für Multiplayer Debugging

---

## 🧭 Nächste Schritte

- [ ] `GameInputManager` + zentrale ActionMap-Initialisierung
- [ ] Routing an Interfaces (`IMovementInputProvider` etc.)
- [ ] Umschaltmechanismus je nach State/Controller
- [ ] FishNet-Modul für Netzwerkinput
- [ ] Beispiele & Testszenen

