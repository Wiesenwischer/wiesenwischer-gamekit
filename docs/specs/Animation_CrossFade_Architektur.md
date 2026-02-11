# Animation CrossFade Architektur

> **Status:** Aktiv
> **Pakete:** `Wiesenwischer.GameKit.CharacterController.Core`, `Wiesenwischer.GameKit.CharacterController.Animation`

---

## Überblick

Das Animationssystem basiert auf einer **CrossFade-Architektur**: Die State Machine ist die einzige Autorität für Animation-States. Jeder State ruft in `OnEnter()` die Methode `PlayState()` auf, die den Unity Animator via `Animator.CrossFade()` direkt zum passenden State wechselt.

**Keine Animator-Transitions nötig** — der Animator Controller enthält nur States mit Clips, aber keine parameterbasierten Transitions.

### Warum CrossFade statt Animator Transitions?

| Eigenschaft | Animator Transitions | CrossFade |
|------------|---------------------|-----------|
| Autorität | Zwei State Machines (Game + Animator) | Eine Autorität (Game SM) |
| Synchronisation | Kann desynchronisieren | Animation = Game State |
| Timing-Abhängigkeit | Transition-Bedingungen + Timing | Sofortige Reaktion |
| Tuning | Visuell im Animator-Fenster | Über ScriptableObject Config |
| Exit Time | Unterstützt | Nicht nötig (State Machine entscheidet) |

---

## Architektur

```
┌─────────────────────────────┐
│     State Machine (Core)     │
│   IdlingState, RunningState, │
│   FallingState, etc.         │
│                              │
│   OnEnter() {                │
│     PlayState(Locomotion)    │
│   }                          │
└──────────┬──────────────────┘
           │ IAnimationController
           ▼
┌─────────────────────────────┐
│  AnimatorParameterBridge     │
│  (Animation Package)         │
│                              │
│  PlayState() {               │
│    CrossFade(hash, duration) │
│  }                           │
│                              │
│  LateUpdate() {              │
│    SetFloat(Speed, ...)      │
│    SetFloat(VertVelocity,..) │
│  }                           │
└──────────┬──────────────────┘
           │
           ▼
┌─────────────────────────────┐
│     Unity Animator           │
│  States: Locomotion, Jump,   │
│  Fall, SoftLand, HardLand,   │
│  LightStop, MediumStop,      │
│  HardStop (keine Transitions)│
└─────────────────────────────┘
```

### Package-Trennung

- **Core Package** definiert das `IAnimationController` Interface und das `CharacterAnimationState` Enum — kein Unity.Animation Import nötig
- **Animation Package** implementiert `AnimatorParameterBridge` mit Zugriff auf Unity Animator
- States im Core können das Interface nutzen ohne die Implementierung zu kennen

---

## IAnimationController Interface

```csharp
public enum CharacterAnimationState
{
    Locomotion, Jump, Fall, SoftLand, HardLand,
    LightStop, MediumStop, HardStop
}

public interface IAnimationController
{
    // State wechseln (Dauer aus AnimationTransitionConfig)
    void PlayState(CharacterAnimationState state);

    // State wechseln mit expliziter Dauer (für Sonderfälle)
    void PlayState(CharacterAnimationState state, float transitionDuration);

    // Animations-Fortschritt (0.0 bis 1.0+)
    float GetAnimationNormalizedTime() => 0f;

    // true wenn Animation komplett abgespielt (nicht während CrossFade)
    bool IsAnimationComplete() => false;

    // true nach "AllowExit" Animation Event
    bool CanExitAnimation => false;

    // Blend Tree Parameter
    void SetSpeed(float speed);
    void SetVerticalVelocity(float velocity);
    void SetAbilityLayerWeight(float weight);
}
```

Die Default-Implementierungen (C# 8) stellen sicher, dass zukünftige Implementierungen (z.B. Netzwerk-Sync) die neuen Members nicht sofort implementieren müssen.

---

## AnimationTransitionConfig

ScriptableObject mit CrossFade-Übergangszeiten pro State.

**Erstellen:** `Create > Wiesenwischer > GameKit > Animation Transition Config`

**Zuweisen:** Auf der `AnimatorParameterBridge` Komponente im Inspector.

| State | Default-Duration | Beschreibung |
|-------|-----------------|--------------|
| Locomotion | 0.15s | Idle/Walk/Run/Sprint (Blend Tree) |
| Jump | 0.1s | Sprung-Animation |
| Fall | 0.05s | Fall-Animation (schneller Übergang) |
| SoftLand | 0.1s | Weiche Landung |
| HardLand | 0.08s | Harte Landung |
| LightStop | 0.1s | Walk-Stopp |
| MediumStop | 0.1s | Run-Stopp |
| HardStop | 0.1s | Sprint-Stopp |

Wenn keine Config zugewiesen ist, werden die Default-Werte verwendet.

---

## PlayState() Ablauf

```
State.OnEnter()
  │
  ▼
PlayState(CharacterAnimationState.Fall)
  │
  ├── Redundanz-Check: Bereits im Ziel-State? → Abbruch
  │
  ├── CanExitAnimation = false  (Reset)
  │
  ├── Duration aus Config lesen (oder Default)
  │
  └── Animator.CrossFade(stateHash, duration)
```

### State → Animation Mapping

| Game State | Animation State | Aufruf |
|-----------|----------------|--------|
| PlayerIdlingState | Locomotion (Speed=0) | `PlayState(Locomotion)` |
| PlayerMovingState | Locomotion (Speed>0) | `PlayState(Locomotion)` |
| PlayerJumpingState | Jump | `PlayState(Jump)` |
| PlayerFallingState | Fall | `PlayState(Fall)` |
| PlayerSoftLandingState | SoftLand | `PlayState(SoftLand)` |
| PlayerHardLandingState | HardLand | `PlayState(HardLand)` |
| PlayerLightStoppingState | LightStop | `PlayState(LightStop)` |
| PlayerMediumStoppingState | MediumStop | `PlayState(MediumStop)` |
| PlayerHardStoppingState | HardStop | `PlayState(HardStop)` |

**Hinweis:** Idle und Moving nutzen denselben Locomotion-State (Blend Tree). Der `Speed`-Parameter im Blend Tree steuert ob Idle, Walk, Run oder Sprint angezeigt wird. Redundante `PlayState(Locomotion)` Aufrufe werden automatisch ignoriert.

---

## Animation Progress

### GetAnimationNormalizedTime()

Gibt die normalisierte Zeit der aktuellen Animation zurück (0.0 = Anfang, 1.0 = Ende).

**Während einer CrossFade-Transition** wird `GetNextAnimatorStateInfo()` verwendet (Ziel-State), sonst `GetCurrentAnimatorStateInfo()`.

### IsAnimationComplete()

Gibt `true` zurück wenn:
- `normalizedTime >= 1.0` UND
- keine CrossFade-Transition aktiv

**Nur sinnvoll für One-Shot Animationen** (Jump, Fall, SoftLand, HardLand). Looping-Animationen (Locomotion Blend Tree) haben kein definiertes Ende.

---

## Animation Events — CanExitAnimation

Ermöglicht Designern, einen frühzeitigen Exit-Punkt in einer Animation zu markieren.

### Konzept

```
HardLand Animation (z.B. 2 Sekunden):
|█████████████████████|████████████|
0s                  1.4s          2.0s
     Gesperrt        AllowExit    IsComplete
                     (Input OK)   (Animation Ende)
```

### Einrichtung

1. Animation Clip im Unity Animation-Fenster öffnen
2. Event-Marker an gewünschter Stelle platzieren
3. Function: `AllowExit` (keine Parameter)
4. Der `AnimatorParameterBridge` (auf dem gleichen GameObject wie der Animator) empfängt das Event

### Verwendung in States

```csharp
protected override void OnUpdate()
{
    var anim = Player.AnimationController;

    // CanExitAnimation: Designer-gesetzter AllowExit Event
    // IsAnimationComplete: Animation komplett abgespielt
    bool recoveryComplete = anim.CanExitAnimation || anim.IsAnimationComplete();

    if (recoveryComplete && HasMovementInput())
    {
        ChangeState(stateMachine.RunningState);
    }
}
```

### Verhalten

- `PlayState()` setzt `CanExitAnimation = false` (Reset bei jedem State-Wechsel)
- `AllowExit` Event auf dem Clip setzt `CanExitAnimation = true`
- Wenn kein Event platziert ist → `CanExitAnimation` bleibt `false`, State wartet auf `IsAnimationComplete()`
- Funktioniert mit CrossFade — Unity feuert Events auf der Clip-Timeline unabhängig von der Transition-Methode

---

## Animator Controller Setup

Der Animator Controller wird über Editor-Tools erstellt:

- **Menu:** `Wiesenwischer > GameKit > Animation > Create Animator Controller`
- **Menu:** `Wiesenwischer > GameKit > Animation > Setup Locomotion Blend Tree`
- **Menu:** `Wiesenwischer > GameKit > Animation > Setup Airborne States`

Die States werden **ohne Transitions** erstellt. Die State Machine steuert den Animator direkt via `PlayState()` → `CrossFade()`.

### States im Animator Controller

| State | Motion | Loop | writeDefaultValues |
|-------|--------|------|--------------------|
| Locomotion | Blend Tree (Idle/Walk/Run/Sprint) | Ja | false |
| Jump | Anim_Jump | Nein | false |
| Fall | Anim_Fall | Ja | false |
| SoftLand | Anim_Land | Nein | false |
| HardLand | Anim_HardLand (oder Anim_Land) | Nein | false |
| LightStop | Anim_LightStop | Nein | false |
| MediumStop | Anim_MediumStop | Nein | false |
| HardStop | Anim_HardStop | Nein | false |

---

## Erweiterung um neue Animation States

1. **Enum erweitern** in `IAnimationController.cs`:
   ```csharp
   public enum CharacterAnimationState
   {
       Locomotion, Jump, Fall, SoftLand, HardLand,
       Swim, Glide, Climb  // Neue States
   }
   ```

2. **State-Hash hinzufügen** in `AnimationParameters.cs`:
   ```csharp
   public static readonly int SwimStateHash = Animator.StringToHash("Swim");
   ```

3. **Switch-Cases erweitern** in `AnimatorParameterBridge.PlayState()`

4. **Config erweitern** in `AnimationTransitionConfig`:
   ```csharp
   [SerializeField] private float _swimTransitionDuration = 0.2f;
   ```

5. **State erstellen** — Neuen State im Animator Controller anlegen (via Editor-Tool oder manuell)

6. **Game State implementieren** — `PlayState(CharacterAnimationState.Swim)` in `OnEnter()` aufrufen

---

## Verwandte Spezifikationen

- [Animationskonzept: Layered Abilities](Animationskonzept_LayeredAbilities.md) — Layer-System (Base, Ability, Status)
- [AAA Action Combat Character Architecture](AAA_Action_Combat_Character_Architecture.md) — Intent-State Architektur
