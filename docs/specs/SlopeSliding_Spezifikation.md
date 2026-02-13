# Slope Sliding Spezifikation

> **Version:** 1.0
> **Datum:** 2026-02-13
> **Status:** Entwurf
> **Abhängigkeiten:** Phase 4 (Fortgeschrittene Lokomotion), CharacterController.Core

---

## 1. Motivation & Problemstellung

### 1.1 Aktueller Zustand

Wenn der Character auf einer Oberfläche steht, deren Winkel `MaxSlopeAngle` überschreitet, passiert Folgendes:

1. Der KCC-Motor markiert die Surface als `!IsStableOnGround`
2. `HandleVelocityProjection` im Motor projiziert die **bestehende** Velocity auf die Oberfläche (`ProjectOnPlane`)
3. Es wird **keine aktive Rutsch-Kraft** hinzugefügt
4. Der Character gleitet nur passiv mit dem Momentum, das er bereits hatte

**Konsequenz:** Der Character bleibt auf steilen Hängen quasi stehen oder rutscht extrem langsam. Die Config-Werte `SlopeSlideSpeed` und `UseSlopeDependentSlideSpeed` in `LocomotionConfig` existieren, werden aber **nirgends im Production-Code gelesen**. Das `SlopeModule.CalculateSlideVelocity()` wird nur in Unit Tests aufgerufen.

### 1.2 Zusätzliche Probleme

- `CharacterLocomotion.IsSliding` gibt immer `false` zurück (`// TODO: Implementierung`)
- Kein dedizierter Animation-State für Sliding
- Keine klare Transition-Logik: Character hängt zwischen "Grounded" (Motor sagt unstable) und "Falling" (hat Bodenkontakt)
- Spieler hat keine Möglichkeit, das Rutsch-Verhalten zu steuern (z.B. bremsen, lenken)

### 1.3 Ziel

Ein vollständiger **Slope Sliding State** mit:
- Aktiver Rutsch-Kraft basierend auf Slope-Steilheit und Konfiguration
- Dedizierter Animation
- Klaren Entry/Exit-Bedingungen
- Optionaler Spieler-Steuerung (Lenken, Bremsen)
- Korrekter Integration in die bestehende State Machine

---

## 2. Referenzen aus anderen Spielen

| Spiel | Verhalten |
|-------|-----------|
| **Genshin Impact** | Automatisches Rutschen auf steilen Slopes, Character sitzt/rutscht, Geschwindigkeit skaliert mit Steilheit, minimale Lenkung |
| **Breath of the Wild** | Ähnlich wie Genshin, mit Shield-Surf als Mechanik |
| **Dark Souls** | Sliding auf steilen Surfaces, kurze Recovery nach Slide-Ende |
| **GW2** | Kein explizites Sliding — Character wird einfach instabil und rutscht unkontrolliert |

**Empfehlung:** Genshin-Ansatz — kontrolliertes, konfigurierbares Rutschen mit visueller Darstellung.

---

## 3. Architektur

### 3.1 Neuer State: `PlayerSlidingState`

```
PlayerMovementState (abstract)
├── PlayerGroundedState (abstract)
│   ├── PlayerIdlingState
│   ├── PlayerMovingState (abstract)
│   │   ├── PlayerWalkingState
│   │   ├── PlayerRunningState
│   │   └── PlayerSprintingState
│   ├── PlayerStoppingState (abstract)
│   │   ├── PlayerLightStoppingState
│   │   ├── PlayerMediumStoppingState
│   │   └── PlayerHardStoppingState
│   ├── PlayerSoftLandingState
│   └── PlayerHardLandingState
├── PlayerSlidingState          ← NEU (eigenständig, nicht unter Grounded)
└── PlayerAirborneState (abstract)
    ├── PlayerJumpingState
    └── PlayerFallingState
```

**Warum nicht unter `PlayerGroundedState`?**
- `PlayerGroundedState` setzt `IsStableOnGround` voraus (Step Detection, Jump, Fall-Checks)
- Sliding passiert auf `!IsStableOnGround` Surfaces — die Grounded-Logik passt nicht
- Der Character hat Bodenkontakt, ist aber nicht "stabil geerdet" → eigenständiger State

### 3.2 State-Verantwortlichkeiten

| Komponente | Verantwortlichkeit |
|------------|-------------------|
| `PlayerSlidingState` | Entry/Exit-Logik, Animation, Spieler-Input |
| `SlopeModule` | Slide-Velocity-Berechnung (bereits vorhanden) |
| `CharacterLocomotion` | Velocity-Anwendung via Intent-System |
| `LocomotionConfig` | Konfigurierbare Parameter (bereits vorhanden) |

### 3.3 Transition-Diagramm

```
                   ┌────────────────────────────────┐
                   │                                │
                   v                                │
Grounded ──[slope > maxAngle]──→ Sliding            │
                                   │                │
                                   ├──[slope ≤ maxAngle]──→ Grounded (Idle/Moving)
                                   │
                                   ├──[Bodenkontakt verloren]──→ Falling
                                   │
                                   └──[Jump && canJumpFromSlide]──→ Jumping
                                                                       │
Falling ──[landet auf steiler Slope]──→ Sliding                        │
                                                                       │
Jumping ──[landet auf steiler Slope]──→ Sliding ◄──────────────────────┘
```

---

## 4. Detailliertes Verhalten

### 4.1 Entry-Bedingungen

Der Character wechselt zu `PlayerSlidingState` wenn:

1. **Von Grounded:** `GroundInfo.SlopeAngle > MaxSlopeAngle` UND Motor hat Bodenkontakt (`FoundAnyGround`)
2. **Von Falling:** Character landet auf Surface mit `slopeAngle > MaxSlopeAngle`
3. **Von Jumping:** Wie Falling (nach Apex, auf steiler Surface gelandet)

**Entry-Check in `PlayerGroundedState.OnUpdate()`:**
```csharp
// Vor dem bestehenden Fall-Detection-Block:
if (Player.Locomotion.GroundInfo.SlopeAngle > Config.MaxSlopeAngle
    && Player.Locomotion.Motor.GroundingStatus.FoundAnyGround)
{
    ChangeState(stateMachine.SlidingState);
    return;
}
```

**Entry-Check in `PlayerFallingState`** (Landing-Logik):
```csharp
// Bestehende Landing-Kategorisierung erweitern:
if (groundInfo.SlopeAngle > Config.MaxSlopeAngle)
{
    ChangeState(stateMachine.SlidingState);
    return;
}
```

### 4.2 Slide-Physik

**Velocity-Berechnung** (nutzt bestehendes `SlopeModule`):

```csharp
// In CharacterLocomotion.UpdateVelocity():
if (_isSliding)
{
    float slideSpeed = _config.SlopeSlideSpeed;

    if (_config.UseSlopeDependentSlideSpeed)
    {
        // Steilere Hänge = schnelleres Rutschen
        float slopeAngle = _cachedGroundInfo.SlopeAngle;
        float angleMultiplier = Mathf.Clamp01(slopeAngle / 90f);
        slideSpeed *= angleMultiplier;
    }

    // Rutsch-Richtung: "Downhill" auf der Oberfläche
    Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;
    Vector3 targetSlideVelocity = slideDirection * slideSpeed;

    // Sanftes Eingleiten (nicht sofort volle Speed)
    currentVelocity = Vector3.MoveTowards(
        currentVelocity,
        targetSlideVelocity,
        _config.SlideAcceleration * deltaTime);
}
```

**Spieler-Lenkung während des Slidings:**

```csharp
// Optional: Spieler kann seitlich lenken (reduzierte Kontrolle)
if (moveInput.sqrMagnitude > 0.01f)
{
    Vector3 steerDirection = GetCameraRelativeMoveDirection();
    // Nur die seitliche Komponente (quer zur Rutsch-Richtung)
    Vector3 lateral = Vector3.ProjectOnPlane(steerDirection, slideDirection);
    lateral = Vector3.ProjectOnPlane(lateral, groundNormal);

    targetSlideVelocity += lateral.normalized * slideSpeed * _config.SlideSteerStrength;
}
```

### 4.3 Exit-Bedingungen

| Bedingung | Ziel-State | Beschreibung |
|-----------|-----------|--------------|
| `slopeAngle ≤ MaxSlopeAngle` | Idle oder Moving | Character erreicht begehbaren Boden |
| `!FoundAnyGround` | Falling | Bodenkontakt verloren (Klippe) |
| Jump-Input (optional) | Jumping | Abspringen vom Hang (konfigurierbar) |

**Hysterese:** Um Flackern an der Grenzwinkel-Kante zu vermeiden, wird ein kleiner Hysterese-Puffer verwendet:
- Entry: `slopeAngle > MaxSlopeAngle` (exakt)
- Exit: `slopeAngle < MaxSlopeAngle - SlideExitHysteresis` (z.B. 3°)

### 4.4 Rotation während Sliding

- Character rotiert **in Rutsch-Richtung** (Hangabwärts)
- Rotation-Speed kann gleich oder langsamer als normal sein
- Blick in Fahrtrichtung (nicht Spieler-Input-Richtung)

---

## 5. Konfiguration

### 5.1 Bestehende Parameter (bereits in `LocomotionConfig`)

| Parameter | Typ | Default | Beschreibung |
|-----------|-----|---------|--------------|
| `SlopeSlideSpeed` | float | 8.0 | Basis-Rutschgeschwindigkeit (m/s) |
| `UseSlopeDependentSlideSpeed` | bool | true | Steilere Hänge = schneller |

### 5.2 Neue Parameter

| Parameter | Typ | Default | Beschreibung |
|-----------|-----|---------|--------------|
| `SlideAcceleration` | float | 15.0 | Beschleunigung beim Eingleiten (m/s²) |
| `SlideSteerStrength` | float | 0.3 | Seitliche Lenkkraft (0 = keine, 1 = volle Kontrolle) |
| `SlideExitHysteresis` | float | 3.0 | Winkel-Puffer beim Verlassen des Slide-States (Grad) |
| `CanJumpFromSlide` | bool | true | Ob der Spieler vom Hang abspringen kann |
| `SlideJumpForceMultiplier` | float | 0.7 | Reduzierte Sprungkraft beim Abspringen aus Slide |
| `MinSlideTime` | float | 0.2 | Mindestzeit im Slide-State (verhindert Flackern) |

### 5.3 Empfohlene Startwerte

```
SlopeSlideSpeed:            12.0    (vorher 8.0 — spürbar schneller)
UseSlopeDependentSlideSpeed: true
SlideAcceleration:          15.0    (ca. 1 Sekunde bis volle Speed)
SlideSteerStrength:          0.3    (leichte Lenkung)
SlideExitHysteresis:         3.0
CanJumpFromSlide:           true
SlideJumpForceMultiplier:    0.7
MinSlideTime:                0.2
```

---

## 6. Animation

### 6.1 Animator-State

Neuer State `Slide` im Locomotion Layer des Animator Controllers:

```
Locomotion (Blend Tree)
├── Idle
├── Walk
├── Run
├── Sprint
├── ...
└── Slide          ← NEU
```

**Transition:** Via `AnimationState.Slide` enum-Wert und `PlayState()`.

### 6.2 Animation Asset

- **Mixamo:** "Standing Slide" oder "Slide" Animation
- **Alternative:** "Falling To Roll" angepasst, oder "Braced Hang To Crouch" als Platzhalter
- **Root Motion:** Aus (In Place) — Velocity wird vom Code gesteuert
- **Loop:** Ja (solange im SlidingState)

### 6.3 CrossFade

| Transition | CrossFade-Zeit | Beschreibung |
|-----------|---------------|--------------|
| Any → Slide | 0.2s | Eingleiten in Slide-Pose |
| Slide → Idle | 0.3s | Aufstehen nach Slide |
| Slide → Falling | 0.15s | Schneller Übergang wenn Boden verloren |
| Slide → Jumping | 0.1s | Abspringen |

---

## 7. Integration in CharacterLocomotion

### 7.1 Intent-System Erweiterung

Analog zum Jump-Intent wird ein **Slide-Intent** eingeführt:

```csharp
// In CharacterLocomotion:
private bool _isSliding;

public bool IsSliding => _isSliding;  // Ersetzt das aktuelle "=> false"

public void SetSliding(bool sliding) => _isSliding = sliding;
```

### 7.2 UpdateVelocity Erweiterung

In `CharacterLocomotion.UpdateVelocity()` wird ein neuer Branch für Sliding eingefügt:

```csharp
// Nach Horizontal-Berechnung, vor Vertical:
if (_isSliding)
{
    // Slide-Physik übernimmt — kein normales AccelerationModule
    // Siehe Abschnitt 4.2
}
```

### 7.3 Motor-Interaktion

- `StepHandling` = `None` während Sliding (keine Steps auf steilen Slopes)
- `GroundSnapping` = aktiv (Character soll auf Oberfläche bleiben)
- `ForceUnground` wird **nicht** aufgerufen (Character hat Bodenkontakt)

---

## 8. Betroffene Dateien

### 8.1 Neue Dateien

| Datei | Beschreibung |
|-------|-------------|
| `States/PlayerSlidingState.cs` | Neuer State |
| (optional) Animation Asset | Slide-Animation |

### 8.2 Zu ändernde Dateien

| Datei | Änderung |
|-------|---------|
| `PlayerMovementStateMachine.cs` | `SlidingState` Property + Instanziierung |
| `PlayerGroundedState.cs` | Entry-Check für Sliding (OnUpdate) |
| `PlayerFallingState.cs` | Landing auf steiler Slope → Sliding |
| `CharacterLocomotion.cs` | `IsSliding` Implementierung, Slide-Velocity in `UpdateVelocity` |
| `ILocomotionConfig.cs` | Neue Config-Properties |
| `LocomotionConfig.cs` | Neue SerializeFields |
| `DefaultLocomotionConfig.asset` | Default-Werte |
| `AnimatorParameterBridge.cs` | Slide-Parameter weiterleiten (optional) |
| `CharacterAnimatorController.controller` | Slide-State im Animator |

### 8.3 Bestehende Dateien (unverändert nutzen)

| Datei | Nutzung |
|-------|---------|
| `SlopeModule.cs` | `CalculateSlideVelocity()` wird endlich im Production-Code aufgerufen |
| `LocomotionConfig.cs` | `SlopeSlideSpeed`, `UseSlopeDependentSlideSpeed` (bereits vorhanden) |

---

## 9. Tests

### 9.1 Unit Tests

| Test | Beschreibung |
|------|-------------|
| `SlidingState_EntersWhenSlopeExceedsMax` | Transition bei > MaxSlopeAngle |
| `SlidingState_ExitsWhenSlopeBecomesWalkable` | Transition zurück bei ≤ MaxSlopeAngle |
| `SlidingState_ExitsToFallingWhenGroundLost` | Kein Boden → Falling |
| `SlidingState_RespectsHysteresis` | Kein Flackern am Grenzwinkel |
| `SlidingState_RespectsMinSlideTime` | MinSlideTime wird eingehalten |
| `SlidingState_SpeedScalesWithAngle` | Steilere Slopes = schnelleres Rutschen |
| `SlidingState_SteerInputAffectsDirection` | Seitliche Lenkung funktioniert |
| `SlidingState_JumpFromSlide` | Abspringen mit reduzierter Kraft |
| `SlopeModule_CalculateSlideVelocity` | (bestehend) Velocity-Berechnung korrekt |

### 9.2 Manuelle Tests

- [ ] Character rutscht spürbar auf steilen Slopes (> MaxSlopeAngle)
- [ ] SlopeSlideSpeed Config hat sichtbaren Effekt
- [ ] Kein Flackern an Grenzwinkeln
- [ ] Smooth Transition: Walk → Slide → Walk
- [ ] Smooth Transition: Fall → Slide
- [ ] Slide → Jump funktioniert (wenn aktiviert)
- [ ] Lenkung während Slide funktioniert
- [ ] Animation passt zur Rutsch-Richtung
- [ ] Performance: Kein Overhead im normalen Grounded-State

---

## 10. Offene Fragen

1. **Slide-Animation:** Welche Mixamo-Animation passt am besten? Eventuell "Slide" oder Custom?
2. **Schaden beim Rutschen:** Soll der Spieler bei langen/schnellen Slides Schaden nehmen? (→ späteres Feature)
3. **Particles/VFX:** Staub/Funken beim Rutschen? (→ späteres Feature)
4. **Sound:** Rutsch-Geräusche? (→ späteres Feature)
5. **Netzwerk:** Slide-State muss synchronisiert werden (→ Phase 6/7)
