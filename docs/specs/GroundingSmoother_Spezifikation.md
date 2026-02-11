# GroundingSmoother — Visual Y-Smoothing Spezifikation

> **Status:** Geplant
> **Pakete:** `Wiesenwischer.GameKit.CharacterController.Core`
> **Abhängigkeit:** Phase 4 (Fortgeschrittene Lokomotion)

---

## Problem

Der KCC-Motor (Kinematic Character Controller) bewältigt Treppenstufen durch **diskrete Step-Up-Teleportation**: Die Capsule wird innerhalb eines einzigen Physik-Ticks um die volle Stufenhöhe nach oben versetzt. Da das visuelle Mesh direkt an der Capsule-Position hängt, springt der Character visuell von Stufe zu Stufe — ein "Daumenkino"-Effekt.

```
Ohne Smoothing:              Mit Smoothing:

  Capsule ┃  ←── Step 3       Capsule ┃  ←── Step 3
          ┃                        ╱  ┃
          ┃                      ╱    ┃  ←── Mesh gleitet
  ┃       ┃  ←── Step 2       ╱      ┃
  ┃                          ╱
  ┃          ←── Step 1    ╱
```

Die bestehende Motor-Interpolation (`CharacterMotorSettings.Interpolate`) glättet zwischen Simulations-Ticks, nicht innerhalb eines Ticks. Step-Ups passieren komplett innerhalb eines Ticks und werden daher nicht geglättet.

---

## Lösung: GroundingSmoother

Eine MonoBehaviour-Komponente auf dem **Player Root-Object**, die den Y-Offset des **Model-Child-Objects** per `SmoothDamp` interpoliert.

### Kern-Prinzip

```
Player (Root — Motor/Physics)
├── CharacterMotor          → Diskrete Position (inkl. Step-Ups)
├── GroundingSmoother       → Berechnet visuellen Y-Offset
└── CharacterModel (Child)  → localPosition.y = smoothedOffset
```

1. Jeden Frame die Y-Differenz zwischen aktuellem und vorherigem Motor-Y tracken
2. Bei plötzlichen Y-Sprüngen (Step-Ups) den Offset per `SmoothDamp` über mehrere Frames auflösen
3. Das Model-Child wird temporär "hinterher gezogen" und gleitet visuell hoch

---

## Architektur

### Datenfluss

```
CharacterMotor.UpdateVelocity()
  └── Step-Up: TransientPosition.y += stepHeight   (diskret, 1 Frame)

GroundingSmoother.LateUpdate()
  ├── deltaY = motor.position.y - previousY
  ├── Wenn |deltaY| > Threshold → Step erkannt
  │   └── smoothOffset -= deltaY (Offset aufbauen)
  ├── smoothOffset = SmoothDamp(smoothOffset, 0, smoothTime)
  │   └── Offset wird über Zeit zu 0 aufgelöst
  └── modelTransform.localPosition.y = smoothOffset

Ergebnis: Motor springt sofort hoch, Mesh gleitet über ~0.1s nach
```

### Wann NICHT smoothen

| Situation | Grund | Verhalten |
|-----------|-------|-----------|
| In der Luft (Jumping/Falling) | Smoothing würde Fall-Start verzögern | Offset sofort auf 0 setzen |
| Beim Landen | Character muss sofort am Boden ankommen | Offset sofort auf 0 setzen |
| Teleportation (> maxDelta) | Kein Step-Up, sondern Positions-Reset | Offset sofort auf 0 setzen |

### Wann smoothen

| Situation | Grund |
|-----------|-------|
| Grounded + Y-Sprung < maxDelta | Treppen, kleine Kanten |
| IsOnStairs = true | Bestätigung via Step-Frequenz-Erkennung |

---

## Komponenten-Design

### GroundingSmoother : MonoBehaviour

```csharp
[RequireComponent(typeof(CharacterMotor))]
public class GroundingSmoother : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Transform des visuellen Modells (Child-Object mit Animator)")]
    [SerializeField] private Transform _modelTransform;

    [Header("Settings")]
    [Tooltip("Smooth-Zeit für Y-Offset-Auflösung (Sekunden). Kleinere Werte = schnelleres Nachziehen.")]
    [SerializeField] private float _smoothTime = 0.075f;

    [Tooltip("Maximaler Y-Sprung der als Step-Up erkannt wird (m). Größere Sprünge werden als Teleport behandelt.")]
    [SerializeField] private float _maxStepDelta = 0.5f;

    [Tooltip("Nur smoothen wenn Character am Boden ist.")]
    [SerializeField] private bool _onlyWhenGrounded = true;
}
```

### Felder (intern)

| Feld | Typ | Beschreibung |
|------|-----|-------------|
| `_previousMotorY` | float | Motor-Y-Position vom letzten Frame |
| `_smoothOffset` | float | Aktueller visueller Y-Offset |
| `_smoothVelocity` | float | SmoothDamp-Velocity (intern) |
| `_motor` | CharacterMotor | Cached Reference |
| `_locomotion` | CharacterLocomotion | Cached Reference (für IsGrounded) |

### Ablauf pro Frame

```
LateUpdate():

  1. currentY = _motor.transform.position.y
  2. deltaY = currentY - _previousMotorY
  3. _previousMotorY = currentY

  // Teleport-Check: Zu großer Sprung → kein Smoothing
  4. if |deltaY| > _maxStepDelta:
       _smoothOffset = 0
       _smoothVelocity = 0
       → Apply & Return

  // Airborne-Check: In der Luft → Offset sofort auflösen
  5. if _onlyWhenGrounded && !_locomotion.IsGrounded:
       _smoothOffset = 0
       _smoothVelocity = 0
       → Apply & Return

  // Step-Up erkannt: Offset aufbauen
  6. if |deltaY| > 0.001f:
       _smoothOffset -= deltaY

  // Offset über Zeit zu 0 auflösen
  7. _smoothOffset = SmoothDamp(_smoothOffset, 0, ref _smoothVelocity, _smoothTime)

  // Snap bei Minimal-Offset (kein ewiges Micro-Smoothing)
  8. if |_smoothOffset| < 0.001f:
       _smoothOffset = 0

  // Apply
  9. _modelTransform.localPosition = new Vector3(0, _smoothOffset, 0)
```

---

## Prefab-Integration

### Vorher

```
Player (Root)
├── CharacterMotor
├── PlayerController
├── CharacterLocomotion
└── CharacterModel (Child)
    ├── Animator
    └── AnimatorParameterBridge
```

### Nachher

```
Player (Root)
├── CharacterMotor
├── PlayerController
├── CharacterLocomotion
├── GroundingSmoother              ← NEU
│   └── _modelTransform → CharacterModel
└── CharacterModel (Child)
    ├── Animator
    └── AnimatorParameterBridge
```

**Keine Änderung** an bestehenden Komponenten nötig. Der Smoother liest nur Motor-Position und Grounding-Status, schreibt nur `localPosition.y` auf das Model-Child.

---

## Konfiguration

### Default-Werte

| Parameter | Default | Bereich | Beschreibung |
|-----------|---------|---------|-------------|
| `_smoothTime` | 0.075s | 0.03–0.2s | Kürzere Werte = snappiger, längere = weicher |
| `_maxStepDelta` | 0.5m | 0.3–1.0m | Sollte >= `LocomotionConfig.MaxStepHeight` sein |
| `_onlyWhenGrounded` | true | bool | false = auch in der Luft smoothen (selten sinnvoll) |

### Tuning-Guide

| Treppen-Feeling | smoothTime | Effekt |
|-----------------|------------|--------|
| Snappy (responsiv) | 0.04s | Kaum Smoothing, fast sofort |
| Balanced | 0.075s | Guter Kompromiss |
| Weich (cinematisch) | 0.15s | Sehr flüssig, leicht verzögert |

---

## Edge Cases

### 1. Slope vs Step

Slopes erzeugen kontinuierliche Y-Änderungen (viele kleine Deltas pro Frame), keine diskreten Sprünge. Der Smoother erkennt den Unterschied automatisch:
- **Slope:** deltaY pro Frame ist winzig (< 0.001m bei 60fps) → kein Offset aufgebaut
- **Step:** deltaY ist diskret (0.1–0.3m in einem Frame) → Offset wird aufgebaut und geglättet

### 2. Schnelles Treppenlaufen

Bei hoher Geschwindigkeit kommen Steps in schneller Folge. Jeder Step baut neuen Offset auf, bevor der vorherige vollständig aufgelöst ist. Das ist gewünscht — der Smoother akkumuliert und löst kontinuierlich auf.

### 3. Treppen hoch + Kante → Fall

Character läuft Treppen hoch und fällt dann von einer Kante:
1. Treppen: Smoothing aktiv, Offset vorhanden
2. Kante → `IsGrounded = false`
3. Sofort `_smoothOffset = 0` → Mesh springt zur Motor-Position
4. Fallanimation startet ohne visuellen Offset

### 4. Landing nach Fall

Character landet nach einem Fall:
1. Motor setzt Position auf Boden (kann großer Y-Sprung sein)
2. `IsGrounded` wird true
3. Aber: Landing-Frame hat potentiell großen deltaY
4. Check: Wenn Character gerade erst gelandet (`JustLanded`), Offset nicht aufbauen

### 5. Abwärts-Treppen

Step-Downs erzeugen negative deltaY-Werte. Der Smoother behandelt diese identisch — Offset wird in negative Richtung aufgebaut und zu 0 geglättet. Das Mesh "schwebt" kurz und senkt sich dann sanft.

---

## Abgrenzung: Foot IK

| Aspekt | GroundingSmoother | Foot IK |
|--------|-------------------|---------|
| Löst | Diskrete Y-Sprünge des Körpers | Fuß-Position auf Stufen/Terrain |
| Ziel | Smooth Body Movement | Natürliche Fuß-Platzierung |
| Aufwand | ~50 Zeilen, 1 Komponente | IK-System, Raycasts pro Fuß |
| Abhängigkeit | Keine | Benötigt IK-System (Phase 8) |
| Reihenfolge | **Zuerst** | Danach |

Der GroundingSmoother ist Voraussetzung für gutes Foot IK — wenn der Körper ruckelt, helfen präzise Fuß-Positionen wenig. Erst den Körper glätten, dann die Füße anpassen.

---

## Verifikation

### Visuelle Tests

- [ ] Treppen hoch: Mesh gleitet statt zu springen
- [ ] Treppen runter: Mesh senkt sich sanft statt zu fallen
- [ ] Flacher Boden: Kein sichtbarer Effekt (Offset bleibt 0)
- [ ] Rampen/Slopes: Kein sichtbarer Effekt (kontinuierliche Y-Änderung)
- [ ] Sprung: Offset sofort 0 bei Airborne
- [ ] Landung: Kein falscher Offset nach Landing
- [ ] Teleport: Kein Smoothing bei großem Y-Sprung

### Performance

- [ ] Kein GC-Alloc pro Frame
- [ ] Nur 1 SmoothDamp + 1 Vector3-Zuweisung pro Frame
- [ ] Kein Einfluss auf Physics-Simulation

### Deaktivierung

- [ ] Komponente disablen → Mesh folgt Motor direkt (Fallback)
- [ ] `_smoothTime = 0` → Kein Smoothing (sofortige Position)

---

## Implementierungsschritte

1. `GroundingSmoother.cs` erstellen im Core-Package (`Runtime/Core/Visual/`)
2. Komponente auf Player-Root im Prefab hinzufügen
3. `_modelTransform` auf CharacterModel-Child setzen
4. In AnimationTestScene testen (Treppen-Bereich existiert bereits)
5. `_smoothTime` tunen bis Treppen-Laufen flüssig aussieht

---

## Verwandte Spezifikationen

- [GameKit IK Spezifikation](GameKit_IK_Spezifikation.md) — Foot IK als nächster Schritt
- [GameKit CharacterController Modular](GameKit_CharacterController_Modular.md) — Motor-Architektur
- [Animation CrossFade Architektur](Animation_CrossFade_Architektur.md) — Animations-System
