# 🛠️ Spezifikation: Modularer Fähigkeiten-Controller für ein Unity-Spiel

## 🎯 Ziel

Diese Spezifikation beschreibt den Aufbau eines modularen Charakter-Controllers in Unity, der sowohl Bewegung als auch ein flexibles Fähigkeitensystem unterstützt. Das System ist ausgelegt für komplexe Spielmechaniken wie Reiten, Nahkampf, Zauberei, Luftbewegung und kombinierbare Kampfstile. 

## 🧱 Architekturüberblick

Die Architektur folgt den Prinzipien:

- **Lose Kopplung**: Fähigkeiten, Bewegungen, Controller und Eingaben sind klar voneinander getrennt.
- **Komposition statt Vererbung**: Fähigkeiten werden als eigenständige Komponenten entwickelt.
- **Datenorientierung**: Durch `ScriptableObjects` und Kontexte sind Erweiterungen einfach möglich.

### Hauptkomponenten

| Komponente             | Zweck |
|------------------------|-------|
| `PlayerController`     | Orchestrator für Bewegung, Fähigkeiten, Animation |
| `IPlayerMovement`      | Interface für Bewegungsarten (z. B. Ground, Mounted) |
| `IAbility`             | Interface für aktivierbare Fähigkeiten |
| `AbilityBar`           | Steuert belegbare Fähigkeitenslots und Eingabezuweisung |
| `PlayerContext`        | Liefert Kontextdaten (z. B. MovementMode, Mana, Transform) |
| `Spellbook`            | Verfügbare Zauber des Charakters (z. B. für Filterung) |

---

## 🧠 Getroffene Architekturentscheidungen & Begründungen

### 1. `IAbility` statt `CombatState`

> **Warum?**  
CombatStates skalieren schlecht, wenn Fähigkeiten kombiniert auftreten sollen (z. B. Reiten + Zauber + Nahkampf). Stattdessen wird jede Fähigkeit als eigene logische Einheit behandelt.

### 2. `AbilityBar` mit Slot-Zuweisung statt harter Tastenbindung

> **Warum?**  
Spieler sollen ihre Fähigkeiten frei auf Slots legen können. Dies erlaubt dynamische Loadouts, intuitive UI-Anbindung und erleichtert die spätere Gamepad-Unterstützung.

### 3. Kontextbasierte Filterung (z. B. `SpellContext.Mounted`)

> **Warum?**  
Fähigkeiten können Movement-spezifisch eingeschränkt sein. Die Verantwortung liegt bei der Fähigkeit selbst zu prüfen, ob sie im aktuellen Kontext aktiviert werden darf.

---

## 🧩 Beispiel-Datenmodell

```csharp
public interface IAbility
{
    string Name { get; }
    bool CanActivate(PlayerContext context);
    void Activate(PlayerContext context);
    void Update(PlayerContext context);
}

public class AbilityBarSlot
{
    public KeyCode key;
    public IAbility assignedAbility;
}
```

---

## 📋 Umsetzungsplan (Phasen)

### Phase 1: Basiscontroller + Bewegung
- [ ] Implementierung `PlayerController` mit `GroundMovement` und `MountedMovement`
- [ ] `PlayerContext` bereitstellen

### Phase 2: Fähigkeitensystem
- [ ] `IAbility` definieren
- [ ] Beispiel-Fähigkeiten: `Fireball`, `SwordSlash`
- [ ] Kontextprüfung (z. B. `SpellContext.Mounted`)

### Phase 3: AbilityBar
- [ ] 4 belegbare Slots (Taste 1–4)
- [ ] Slots rufen `TryActivate()` bei gedrückter Taste auf
- [ ] Fähigkeiten dynamisch zuweisbar

### Phase 4: Erweiterbarkeit
- [ ] Cooldown-System
- [ ] Ressourcenverbrauch (Mana, Ausdauer)
- [ ] Animation / VFX Trigger
- [ ] Drag & Drop in UI

---

## 🎯 Vorbereitung für Epic- & Feature-Liste

### Features (Auszug)
- [ ] Bewegung: Ground / Mounted / Air
- [ ] Kombinierbare Fähigkeiten (Spell, Melee, Dash etc.)
- [ ] Kontextbasiertes Aktivieren von Fähigkeiten
- [ ] Fähigkeitenleiste mit freier Belegung
- [ ] Unterstützt Gamepad / Unity Input System
- [ ] Modular erweiterbare `.unitypackage`-fähige Pakete

---

## 🧪 Mögliche Use Cases

1. **Spieler aktiviert "Feuerball", wenn zu Fuß unterwegs**  
→ Kontext `Ground`, Fähigkeit aktivierbar

2. **Spieler reitet und nutzt "Schwertschlag" auf Taste 2**  
→ Fähigkeit aktiviert, Kontext `Mounted`

3. **Spieler ändert Loadout und ersetzt Fähigkeit im Slot 1 mit "Teleport"**  
→ Kein Code nötig, nur neue Instanz im Slot

4. **Spieler fliegt und "Feuerball" ist deaktiviert**  
→ Kontextprüfung blockiert Ausführung

---

## 🏁 Nächste Schritte

1. Aufteilen in Feature-Epics und User Stories
2. Anlegen von Packages pro Fähigkeitstyp (Melee, Spells, Buffs)
3. UI-Vorbereitung für Drag & Drop
4. Optional: Netzwerkfähigkeit (Mirror-kompatibel)

---

© Spezifikation erstellt mit ChatGPT für Unity 2022.3+ Projekte.

---

## 🧭 Erweiterung: Austauschbare Movement-Controller mit eigener interner State Machine

Neben dem modularen Fähigkeitensystem setzen wir auf ein flexibles Bewegungssystem, bei dem **komplexe Movement-Typen als austauschbare Controller** realisiert werden.

### 🎯 Ziel

Bewegungssysteme wie Reiten, Gleiten oder Fliegen sollen jeweils ihre eigene Logik, eigene States und Physikmodelle besitzen, ohne sich gegenseitig zu beeinflussen. Diese Modularisierung ermöglicht komplexes Movement mit klarer Trennung von Verantwortlichkeiten.

---

## 🧱 Architekturaufbau

```plaintext
PlayerController
├── ActiveMovementController : IMovementController
│   ├── MovementStateMachine (lokal)
│   │   ├── z. B. Grounded, Jumping, Dashing
│   └── Eigene Eingabe-, Kamera-, und Physiklogik
├── AbilitySystem (bleibt gleich)
└── PlayerContext (liefert MovementMode etc.)
```

---

### 🔄 Ablauf

1. Spieler verwendet `GroundMovementController` (Bewegung am Boden)
2. Bei Tastendruck `G` wird auf `GlidingMovementController` gewechselt
3. Jeder Controller führt eine eigene State-Maschine mit z. B.:
   - Ground: `GroundedState`, `JumpingState`, `FallingState`, `DashingState`
   - Gliding: `GlideState`, `DiveState`
   - Riding: `MountedIdle`, `MountedRun`, `MountedJump`

---

### 🧩 Beispiel-Interfaces

#### `IMovementController`

```csharp
public interface IMovementController
{
    void Enter(PlayerController player);
    void Exit(PlayerController player);
    void HandleInput(PlayerController player);
    void Update(PlayerController player);
}
```

#### `MovementStateMachine` (lokal pro Controller)

```csharp
public class MovementStateMachine
{
    private IMovementState currentState;

    public void SetState(IMovementState newState, PlayerController player)
    {
        currentState?.Exit(player);
        currentState = newState;
        currentState?.Enter(player);
    }

    public void HandleInput(PlayerController player) => currentState?.HandleInput(player);
    public void Update(PlayerController player) => currentState?.Update(player);
}
```

#### Beispiel `GroundMovementController`

```csharp
public class GroundMovementController : IMovementController
{
    private MovementStateMachine stateMachine;

    public void Enter(PlayerController player)
    {
        stateMachine = new MovementStateMachine();
        stateMachine.SetState(new GroundedState());
    }

    public void HandleInput(PlayerController player) =>
        stateMachine?.HandleInput(player);

    public void Update(PlayerController player) =>
        stateMachine?.Update(player);

    public void Exit(PlayerController player) { }
}
```

---

## ✅ Vorteile

| Vorteil | Beschreibung |
|--------|--------------|
| 🔁 Movement austauschbar | `SetMovementController(new GlidingController())` |
| 🧠 Lokale Movement-Zustände | Keine globale FSM mit 50 Zuständen |
| 🧩 Modulares Design | Bewegungspakete als `.unitypackage` einbaubar |
| 🎯 Feature-getrennt | Laufen ≠ Reiten ≠ Fliegen – mit voller Kontrolle |

---

## 🛠️ Integration mit dem Fähigkeitensystem

- `PlayerContext` kennt `CurrentMovementMode`
- Fähigkeiten prüfen bei Aktivierung, ob sie im aktuellen Mode erlaubt sind
- z. B. `Fireball` kann nicht in `GlidingMovementController` aktiviert werden

---

## 📈 Erweiterungsmöglichkeiten

- Kameraverhalten je nach MovementController
- Custom Collider- oder Rigidbody-Logik
- Netzwerkfähige Controller mit Mirror

---

## 📦 Modularisierung mit UnityPackages

Um die Entwicklung schrittweise, teamfähig und komponentenbasiert zu gestalten, setzen wir auf **Unity-eigene `.unitypackage`-Module** für jede Funktionseinheit. Dadurch können Features unabhängig voneinander entwickelt, getestet und verteilt werden.

### 🎯 Ziel

- **Jede Funktionseinheit ist ein eigenes UnityPackage** (z. B. Fireball, GroundMovement)
- **Alle Pakete sind unabhängig voneinander entwickelbar und testbar**
- **Die Core-Komponenten (Player, AbilitySystem) sind zentrale Abhängigkeiten**
- **Feature-Pakete können sukzessive in das Hauptspiel eingebunden werden**

---

## 🧱 Empfohlene Paketstruktur

```plaintext
/Packages
├── Core.PlayerController
│   ├── PlayerController.cs
│   ├── PlayerContext.cs
│   └── ExampleScene.unity
│
├── Movement.Ground
│   ├── GroundMovementController.cs
│   ├── States/Jump.cs, Dash.cs, etc.
│   └── MovementTestScene.unity
│
├── Movement.Mounted
│   └── MountedMovementController.cs + States
│
├── Combat.AbilitySystem
│   ├── IAbility.cs
│   ├── AbilityBar.cs
│   └── ScriptableObjects for SlotConfig
│
├── Abilities.Fireball
│   └── FireballAbility.cs
│
├── Abilities.SwordSlash
│   └── SwordSlashAbility.cs
```

---

## 🔌 Import & Nutzung im Spiel

- Pakete werden einzeln über Unitys „Export Package...“ und „Import Package...“ Mechanismus verwaltet
- Jedes Paket enthält:
  - Source Code
  - Testszene
  - Prefabs / ScriptableObjects
  - Optional: eigene Editor-Komponenten

---

## 📦 Abhängigkeitsregeln

| Paket           | Darf referenzieren                      |
|-----------------|------------------------------------------|
| `Abilities.*`   | Nur `Combat.AbilitySystem`              |
| `Movement.*`    | Nur `Core.PlayerController`             |
| `Core.*`        | Keine Abhängigkeiten                    |
| `UI.*`          | Core + AbilitySystem                    |

Zirkuläre Abhängigkeiten sollen unbedingt vermieden werden.

---

## 🛠️ Entwicklungsstrategie in Phasen

| Phase | Paket(e) | Beschreibung |
|-------|----------|--------------|
| 1     | `Core.PlayerController`, `Movement.Ground` | Basisbewegung mit austauschbarem Controller |
| 2     | `Combat.AbilitySystem`, `Abilities.Fireball`, `Abilities.SwordSlash` | Skillbar + erste Abilities |
| 3     | `Movement.Mounted`, `Movement.Gliding` | Erweiterte Controller für Reiten/Gleiten |
| 4     | `UI.SkillBar` | Skill-Leiste mit Drag & Drop |
| 5     | `System.Targeting`, `System.CombatCoordinator` | Zielsystem + Validierung |
| 6     | Multiplayer | Mirror-Unterstützung im `Core.Networking`

---

## 🔁 Vorteile

- 🔧 Feature-getrennte Entwicklung
- 🧪 Isolierte Testszenen
- 📦 Reuse in anderen Projekten
- 🧱 Bessere Übersicht bei wachsendem Codeumfang

---

## 🌐 Netzwerkfähigkeit mit FishNet (optional)

### 🧭 Ziel
Die Architektur unterstützt lokale Einzelspieler-Logik **und** optionale Netzwerkfähigkeit über FishNet – ohne doppelten Code. Das Netzwerkverhalten ist vollständig gekapselt und kann nach Bedarf eingebunden oder weggelassen werden.

---

## 🧱 Architekturprinzip

```plaintext
Player (Prefab)
├── PlayerController                # zentral, ohne Netcode
├── NetworkObject (FishNet)        # FishNet-Komponente
├── PlayerNetworkSync_FishNet      # Netzwerkadapter (optional)
```

- **Der zentrale `PlayerController`** ist netzwerk-unabhängig
- **Alle Netzwerkfunktionen** (Input-Sync, RPCs, SyncVars) sind in modularen Komponenten ausgelagert
- Die Logik basiert auf **Server Authority**: Aktionen werden via `ServerRpc` an den Server gesendet, dieser führt aus

---

## 🔌 Beispiel-Komponenten

```csharp
// PlayerController.cs (Core)
public class PlayerController : MonoBehaviour
{
    public void SetMoveInput(Vector3 input) { ... }
    public void ActivateAbility(string id) { ... }
}
```

```csharp
// PlayerNetworkSync_FishNet.cs (Networking.FishNet)
public class PlayerNetworkSync_FishNet : NetworkBehaviour
{
    private PlayerController controller;

    public override void OnStartNetwork()
    {
        controller = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (IsOwner)
        {
            Vector3 input = ReadInput();
            ServerSetInput(input);
        }
    }

    [ServerRpc]
    private void ServerSetInput(Vector3 input)
    {
        controller.SetMoveInput(input);
    }

    [ServerRpc]
    public void ServerActivateAbility(string id)
    {
        controller.ActivateAbility(id);
    }
}
```

---

## 📦 Paketstruktur

```plaintext
/Packages
├── Core.PlayerController
├── Core.AbilitySystem
├── Core.Networking.FishNet
│   ├── PlayerNetworkSync_FishNet.cs
│   ├── NetworkAbilityBridge.cs
│   └── NetworkAnimatorBridge.cs
```

---

## 🧪 Vorteile

| Vorteil | Beschreibung |
|--------|--------------|
| ✅ Nur ein PlayerController | Keine doppelte Logik |
| ✅ Server Authority | Sicherheit und Konsistenz |
| ✅ Modular | Multiplayer nur bei Bedarf |
| ✅ Austauschbar | Mirror oder FishNet möglich |
| ✅ Lokal testbar | Ohne Netzwerk lauffähig |

---

## 🛠️ Entwicklungsstrategie

1. Entwicklung aller Kernfunktionen (Movement, Abilities, StateMachine) **netzwerkunabhängig**
2. Erstellung separater Komponenten zur Netzwerkerweiterung mit FishNet
3. Modularisierung via UnityPackages (`Core.Networking.FishNet`)
4. Erstellung von Testszenen für Host-Client-Validierung