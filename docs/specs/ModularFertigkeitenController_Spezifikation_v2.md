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

## 🎬 Integration des Animationskonzepts in den modularen Controller

### 🔍 Entscheidung

Das Animationssystem (Animator Controller, Layer, Avatar Masks etc.) ist **nicht direkt Bestandteil** des `CharacterController`-Moduls. Stattdessen wird es als **separates, optionales Modul** realisiert, das über eine definierte Schnittstelle eingebunden werden kann.

---

### 📦 Struktur

| Modul                             | Aufgabe                                                |
|----------------------------------|---------------------------------------------------------|
| `Module.Character.Controller`    | Bewegung, Eingabe, Zustandshandling (StateMachine)     |
| `Module.Character.Animations`    | AnimatorController, Layer-Handling, AvatarMasks        |
| `Module.Character.Animations.*`  | Zusätzliche Animationspakete (z. B. Reiten, Gleiten)   |

---

### 🔌 Technische Anbindung

Der Controller implementiert eine optionale Schnittstelle für Animationen:

```csharp
public class PlayerCharacterController : NetworkBehaviour
{
    private MovementStateMachine movementStates;
    private IAnimationBridge animationBridge;

    public void InjectAnimator(IAnimationBridge bridge)
    {
        animationBridge = bridge;
        movementStates.SetAnimationBridge(bridge);
    }
}
```

Der `IAnimationBridge` kann aus jedem Animationsmodul stammen – z. B. ein `BasicAnimationBridge`, ein `RidingAnimationBridge`, etc.

---

### 🎯 Vorteile

- 🔁 Trennung von Logik und Visualisierung
- 🧩 Modularer Austausch möglich
- 🧪 Testbar ohne Animationen
- 🔄 Kombinierbar mit mehreren AnimationPackages

---

### 📝 Fazit

Das Animationssystem ist **modular, ersetzbar und erweiterbar** – es wird nicht fest in den Controller integriert, sondern über eine Bridge angebunden. Dies ermöglicht eine hohe Flexibilität für Multiplayer, Tests und modulare Erweiterung des Spiels.