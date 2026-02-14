# Stair Animation Spezifikation

## Ziel

Dedizierte Treppensteig-Animationen statt der allgemeinen Walk/Run-Animation auf Treppen. Die Walk-Animation wurde für flachen Boden designed — auf Treppen fehlt die korrekte Fußhebung und die Schrittfrequenz ist zu langsam für die visuelle Körperbewegung (Step-Up-Teleportationen).

---

## Architektur

### Neuer AnimationState

```csharp
public enum CharacterAnimationState
{
    Locomotion,   // bestehend: Idle/Walk/Run/Sprint Blend Tree
    StairClimb,   // NEU: Treppensteig Blend Tree (Up/Down × Speed)
    Jump,
    Fall,
    SoftLand,
    HardLand,
    LightStop,
    MediumStop,
    HardStop
}
```

### Animator Controller Erweiterung

```
Base Layer (bestehend):
  ┌─────────────┐     ┌─────────────┐
  │ Locomotion   │◄───►│ StairClimb  │   ← NEU
  │ (Blend Tree) │     │ (Blend Tree)│
  └──────┬───────┘     └─────────────┘
         │
    ┌────┴────┐
    │ Jump    │  (bestehend)
    │ Fall    │
    │ Land... │
    └─────────┘
```

**Transition Locomotion ↔ StairClimb:**
- Trigger: `IsOnStairs`-Zustand aus `CharacterLocomotion`
- CrossFade-Dauer: 0.15–0.2s (weicher Übergang)
- Kein abrupter Wechsel — SmoothDamp auf dem Speed-Parameter bleibt aktiv

### StairClimb Blend Tree

**Typ:** Simple1D, Parameter: `Speed`

| Clip | Threshold | Beschreibung |
|------|-----------|-------------|
| StairUp_Idle | 0.0 | Stehen auf Treppe |
| StairUp_Walk | 0.5 | Langsames Treppensteigen |
| StairUp_Run | 1.0 | Schnelles Treppensteigen |

Optional (wenn Clips vorhanden):

| Clip | Threshold | Beschreibung |
|------|-----------|-------------|
| StairDown_Walk | -0.5 | Langsames Treppe-Absteigen |
| StairDown_Run | -1.0 | Schnelles Treppe-Absteigen |

**Hinweis zu Up/Down:** Falls nur Aufstiegs-Clips vorhanden sind, kann zunächst ein einzelner Blend Tree (nur Up) verwendet werden. Absteigen fällt dann auf die Locomotion-Animation zurück. Alternativ kann ein 2D Blend Tree (Speed × StairDirection) verwendet werden.

---

## Animation Clips

### Anforderungen

| Eigenschaft | Wert | Grund |
|-------------|------|-------|
| In Place | Ja | Motor steuert Bewegung, nicht Root Motion |
| Loop | Ja | Kontinuierliches Treppensteigen |
| Root Transform Rotation | Bake Into Pose, Body Orientation | Konsistenz mit Locomotion |
| Root Transform Position Y | Bake Into Pose, Feet | Füße als Referenz (grounded) |
| Root Transform Position XZ | Bake Into Pose, Original | Kein horizontaler Drift |
| Avatar | Create From This Model | CC-Character Rig, nicht Copy From Other |

### Empfohlene Clips (Mixamo)

- **"Walking Up Stairs"** — Treppensteigen, mittlere Geschwindigkeit
- **"Running Up Stairs"** — Schnelles Treppensteigen
- **"Walking Down Stairs"** — Treppe-Absteigen (optional)

Bei Mixamo-Download: **"In Place" aktivieren**.

---

## State Machine Integration

### Trigger-Logik

Die bestehende `CharacterLocomotion.IsOnStairs`-Property dient als Trigger:

```
IsOnStairs = StairSpeedReductionEnabled
           && _recentStepCount >= 2
           && (Time.time - _lastStepTime) < 0.6s
```

### Zustandswechsel in Moving-States

Die `PlayerMovingState`-Basisklasse (oder ein neuer `PlayerStairClimbingState`) prüft `IsOnStairs`:

**Option A: In bestehenden Moving-States (einfacher)**
```csharp
// In PlayerMovingState.OnUpdate():
if (Player.Locomotion.IsOnStairs)
    Player.AnimationController?.PlayState(CharacterAnimationState.StairClimb);
else
    Player.AnimationController?.PlayState(CharacterAnimationState.Locomotion);
```

**Option B: Eigener PlayerStairClimbingState (sauberer)**
- Neuer State `PlayerStairClimbingState` erbt von `PlayerGroundedState`
- `OnEnter()` → `PlayState(StairClimb)`
- Transition von `PlayerMovingState` wenn `IsOnStairs == true`
- Transition zurück zu `PlayerMovingState` wenn `IsOnStairs == false`

**Empfehlung:** Option A für den Anfang (weniger Refactoring), Option B wenn die Treppen-Logik komplexer wird (z.B. eigene Physik, Handrail-IK).

### AnimatorParameterBridge Anpassung

```csharp
// In UpdateParameters():
// Treppen-Kompensation und Speed-Multiplikator entfallen,
// da der StairClimb Blend Tree eigene Thresholds hat.
// Der Speed-Parameter wird direkt aus HorizontalVelocity berechnet.
```

Der `_stairAnimSpeedMultiplier` wird durch den StairClimb Blend Tree ersetzt und kann dann entfernt werden.

---

## Zu ändernde Dateien

| Datei | Änderung |
|-------|----------|
| `IAnimationController.cs` | `StairClimb` zu `CharacterAnimationState` Enum |
| `AnimationParameters.cs` | `StairClimbStateHash` hinzufügen |
| `AnimatorParameterBridge.cs` | `StairClimb` Case in `PlayState()` Switch |
| `AnimationTransitionConfig.cs` | Transition-Dauer für `StairClimb` |
| `LocomotionBlendTreeCreator.cs` | StairClimb Blend Tree erzeugen |
| `PlayerMovingState.cs` | `IsOnStairs`-Check in `OnUpdate()` |
| `AnimatorParameterBridge.cs` | `_stairAnimSpeedMultiplier` entfernen nach Migration |

---

## Konfiguration

### AnimationTransitionConfig

| Transition | Dauer | Grund |
|-----------|-------|-------|
| Locomotion → StairClimb | 0.15s | Weicher Übergang beim Betreten der Treppe |
| StairClimb → Locomotion | 0.15s | Weicher Übergang beim Verlassen der Treppe |
| StairClimb → Jump | 0.1s | Absprung von der Treppe |
| StairClimb → Fall | 0.05s | Über Treppenkante fallen |

---

## Edge Cases

### 1. Einzelne Stufe (kein Treppenhaus)
`IsOnStairs` benötigt 2+ Steps in 600ms. Eine einzelne Stufe triggert NICHT den Stair-State → Locomotion-Animation bleibt aktiv. Das ist gewünscht.

### 2. Sehr steile Treppen (0.5m Stufen)
Die steile Test-Treppe hat 0.5m-Stufen. Hier ist die Step-Frequenz niedriger (größere Schritte). Prüfen ob `IsOnStairs` noch zuverlässig triggert. Ggf. `StairDetectionWindow` anpassen.

### 3. Richtungswechsel auf Treppe
Seitwärts-Laufen auf Treppen → StairClimb-Animation könnte falsch aussehen. Prüfen ob eine Mindest-Forward-Dot-Richtung zur Treppe nötig ist (z.B. nur StairClimb wenn Blickrichtung ≈ Treppenrichtung).

### 4. Treppe → Flat Ground Übergang
`IsOnStairs` hat eine 600ms-Nachlaufzeit (StairDetectionWindow). Der Übergang StairClimb → Locomotion passiert erst nach 600ms ohne Step-Up. Das CrossFade (0.15s) überbrückt den Rest visuell.

---

## Abhängigkeiten

- `CharacterLocomotion.IsOnStairs` — besteht bereits
- Stair-Animation Clips — müssen von Mixamo/Reallusion heruntergeladen werden
- CC-Avatar-Retargeting — Clips müssen auf den CC-Character Avatar gemapped werden

---

## Verwandte Spezifikationen

- [Animation CrossFade Architektur](Animation_CrossFade_Architektur.md) — State-basierte Animation
- [GroundingSmoother Spezifikation](GroundingSmoother_Spezifikation.md) — Visual Smoothing auf Treppen
- [GameKit IK Spezifikation](GameKit_IK_Spezifikation.md) — Foot IK (ergänzt Stair-Animation)
