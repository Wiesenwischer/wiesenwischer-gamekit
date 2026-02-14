# FootIK Verbesserungen — Spezifikation

> **Status:** Entwurf
> **Betrifft:** `Wiesenwischer.GameKit.CharacterController.IK` — `FootIK.cs`
> **Problem:** Spieler steht auf flachen Oberflächen mit angewinkelten Knien
> **Ziel:** IK wirkt nur dort, wo das Terrain tatsächlich uneben ist

---

## Problemanalyse

### Symptom

Auf ebenen Flächen steht der Character mit sichtbar gebeugten Knien, obwohl die Idle-Animation gerade Beine hat. Das Problem verschwindet wenn FootIK deaktiviert wird.

### Ursachen

#### 1. Body-Offset nur abwärts (Hauptursache)

```csharp
// Zeile 139 — FootIK.cs
targetBodyOffset = Mathf.Min(targetBodyOffset, 0f); // Nur nach unten
```

Der Body (Hüfte) kann sich **nie nach oben bewegen**. Wenn die IK-Targets aber durch `_footOffset` leicht über der animierten Fußposition liegen, werden die Füße per IK angehoben — der Body bleibt aber unten. Die Beine müssen sich beugen, weil der Abstand Hüfte→Fuß nicht mehr der natürlichen Beinlänge entspricht.

**Auf flachem Boden:**
- Raycast trifft bei `hit.point.y = 0.0`
- IK-Target: `hit.point.y + footOffset = 0.04`
- Animation hat Fuß bei ca. `0.0`
- Delta = `+0.04` → Body-Offset wird auf `0` geclampt
- Fuß wird 4cm angehoben, Body bleibt → Knie beugt sich

#### 2. Kein Terrain-Varianz-Check

Das System behandelt flachen Boden identisch zu unebenem Terrain. Es gibt keinen Mechanismus der erkennt: "Beide Füße sind fast auf gleicher Höhe, Normalen zeigen nach oben → hier braucht IK kaum einzugreifen."

#### 3. Keine Dead Zone für minimale Deltas

Selbst bei minimalem Unterschied (< 1cm) zwischen animierter und berechneter Fußposition wird voller IK-Weight angewendet. Die Animation hat die Füße auf flachem Boden bereits korrekt platziert — IK sollte erst eingreifen, wenn eine signifikante Terrain-Differenz vorliegt.

---

## Lösungskonzept

Drei gezielte, voneinander unabhängige Verbesserungen. Jede adressiert eine der Ursachen und kann einzeln getestet werden.

### Fix 1: Body-Offset darf leicht nach oben

**Problem:** `Mathf.Min(targetBodyOffset, 0f)` verhindert, dass der Body den Füßen nach oben folgt.

**Lösung:** Kleinen positiven Body-Offset erlauben, damit der Body den IK-adjustierten Füßen folgen kann.

```csharp
// NEU: Konfigurierbarer max. Aufwärts-Offset
[Tooltip("Maximaler Body-Offset nach oben (kompensiert footOffset).")]
[SerializeField] private float _maxBodyUpOffset = 0.05f;

// VORHER:
targetBodyOffset = Mathf.Min(targetBodyOffset, 0f);

// NACHHER:
targetBodyOffset = Mathf.Clamp(targetBodyOffset, -_maxFootAdjustment, _maxBodyUpOffset);
```

**Neuer Parameter:**

| Parameter | Default | Beschreibung |
|-----------|---------|-------------|
| `_maxBodyUpOffset` | 0.05 | Max. Aufwärts-Versatz des Body in Metern. Sollte etwas größer als `_footOffset` sein. |

**Warum nur 5cm?** Mehr als 5cm Aufwärts-Versatz würde den Character "schweben" lassen. Der Wert muss den `_footOffset` (4cm) kompensieren können, aber nicht mehr.

### Fix 2: Terrain-Varianz-basierter IK-Weight

**Problem:** IK greift auf flachem Boden genauso stark ein wie auf Treppen/Slopes.

**Lösung:** Die "Unebenheit" des Terrains messen und den IK-Weight proportional dazu skalieren. Auf flachem Boden → Weight ~0, auf unebenem Terrain → Weight ~1.

```csharp
// NEU: Konfigurierbare Schwellwerte
[Tooltip("Höhendifferenz (m) ab der IK voll aktiv wird.")]
[SerializeField] private float _terrainVarianceThreshold = 0.03f;

// In PrepareIK, nach den Raycasts:
float terrainVariance = 0f;
if (_leftFootHit && _rightFootHit)
{
    // Höhendifferenz zwischen beiden Fuß-Targets
    float heightDiff = Mathf.Abs(_leftFootTarget.y - _rightFootTarget.y);

    // Normalen-Abweichung von der Vertikalen (0 = flach, 1 = senkrechte Wand)
    float leftNormalDev = 1f - Vector3.Dot(_leftFootNormal, Vector3.up);
    float rightNormalDev = 1f - Vector3.Dot(_rightFootNormal, Vector3.up);
    float normalDev = Mathf.Max(leftNormalDev, rightNormalDev);

    // Kombinierte Terrain-Varianz
    terrainVariance = heightDiff + normalDev * 0.1f;
}

// IK-Weight proportional zur Terrain-Varianz
_terrainWeight = Mathf.InverseLerp(0f, _terrainVarianceThreshold, terrainVariance);
```

Die `_terrainWeight` wird dann als zusätzlicher Faktor in `effectiveWeight` eingerechnet:

```csharp
// In ProcessIK:
float effectiveWeight = _weight * _locomotionBlendWeight * _terrainWeight;
```

**Neuer Parameter:**

| Parameter | Default | Beschreibung |
|-----------|---------|-------------|
| `_terrainVarianceThreshold` | 0.03 | Höhendifferenz in Metern ab der IK voll eingreift. Unter diesem Wert wird IK-Weight proportional reduziert. |

**Erfordert:** Die Normalen der Raycasts müssen gespeichert werden (aktuell werden sie in `CastFoot` berechnet aber nur für die Rotation verwendet, nicht separat gespeichert).

### Fix 3: Delta-basierte Dead Zone pro Fuß

**Problem:** Minimale Positionsunterschiede (< 2cm) triggern vollen IK-Eingriff.

**Lösung:** Wenn der Unterschied zwischen animierter Fußposition und IK-Target unter einem Schwellwert liegt, wird der Fuß-IK-Weight für diesen Fuß reduziert.

```csharp
// NEU: Dead Zone Schwellwert
[Tooltip("Minimaler Fuß-Versatz (m) ab dem IK eingreift.")]
[SerializeField] private float _footDeadZone = 0.02f;

// In ProcessIK, pro Fuß:
float leftDelta = (_leftFootTarget - animator.GetIKPosition(AvatarIKGoal.LeftFoot)).magnitude;
float leftFootWeight = effectiveWeight * Mathf.InverseLerp(0f, _footDeadZone, leftDelta);

animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, leftFootWeight);
animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, leftFootWeight);
```

**Neuer Parameter:**

| Parameter | Default | Beschreibung |
|-----------|---------|-------------|
| `_footDeadZone` | 0.02 | Versatz in Metern unter dem IK nicht eingreift. Verhindert Mikro-Korrekturen auf flachem Boden. |

---

## Zusammenfassung der neuen Parameter

| Parameter | Default | Header | Beschreibung |
|-----------|---------|--------|-------------|
| `_maxBodyUpOffset` | 0.05 | Adjustments | Max. Aufwärts-Versatz des Body |
| `_terrainVarianceThreshold` | 0.03 | Terrain Adaptation | Höhendifferenz für vollen IK-Weight |
| `_footDeadZone` | 0.02 | Terrain Adaptation | Minimaler Versatz für IK-Eingriff |

---

## Neue Felder

```csharp
// Terrain-Varianz
private float _terrainWeight = 1f;
private Vector3 _leftFootNormal;
private Vector3 _rightFootNormal;
```

---

## Änderungen an bestehenden Methoden

### CastFoot

Normalen separat speichern (out-Parameter oder Feld):

```csharp
private bool CastFoot(Vector3 footPos, out Vector3 targetPos, out Quaternion targetRot,
                       out Vector3 surfaceNormal)
{
    // ... bestehender Code ...
    surfaceNormal = hit.normal;  // NEU
    // ...
}
```

### PrepareIK

1. CastFoot-Aufrufe mit neuem Normal-Parameter
2. Terrain-Varianz berechnen nach den Raycasts
3. Body-Offset-Clamping anpassen (Fix 1)

### ProcessIK

1. `_terrainWeight` in `effectiveWeight` einrechnen (Fix 2)
2. Pro-Fuß Dead Zone anwenden (Fix 3)

---

## Erwartetes Verhalten nach den Fixes

| Szenario | Vorher | Nachher |
|----------|--------|---------|
| Flacher Boden, Idle | Gebeugte Knie | Gerade Beine (Animation unverändert) |
| Flacher Boden, Walking | OK (IK bereits aus) | OK (unverändert) |
| Treppenstufe, Idle | OK (Füße passen sich an) | OK (Terrain-Varianz hoch → IK aktiv) |
| Slope, Idle | OK (Füße passen sich an) | OK (Normalen-Abweichung → IK aktiv) |
| Kante (ein Fuß hängt) | OK (Body senkt sich) | OK (unverändert) |
| Übergang flach→uneben | Abrupter IK-Eingriff | Sanfter Übergang via SmoothDamp |

---

## Nicht-Ziele

- Kein neues IK-Backend (bleibt Unity Built-in)
- Kein neues Package oder Interface
- Keine Änderung an IKManager oder LookAtIK
- Keine neuen Config-ScriptableObjects (Parameter bleiben SerializeField auf der Komponente)

---

## Test-Plan

### Unit Tests

- `TerrainVariance_FlatGround_ReturnsZero` — Gleiche Höhe + vertikale Normalen → Varianz = 0
- `TerrainVariance_UnevenGround_ReturnsPositive` — Verschiedene Höhen → Varianz > 0
- `TerrainVariance_Slope_IncludesNormalDeviation` — Schräge Normalen erhöhen Varianz
- `BodyOffset_AllowsSmallUpward` — Positiver Offset wird nicht auf 0 geclampt
- `BodyOffset_ClampsAtMaxUp` — Offset über `_maxBodyUpOffset` wird geclampt
- `FootDeadZone_SmallDelta_ReducesWeight` — Delta < DeadZone → Weight < 1
- `FootDeadZone_LargeDelta_FullWeight` — Delta > DeadZone → Weight = 1

### Play Mode Verifikation

- [ ] Flacher Boden: Knie nicht mehr gebeugt im Idle
- [ ] Treppen: Füße passen sich weiterhin an Stufen an
- [ ] Slope: Fußsohlen folgen der Neigung
- [ ] Übergang flach→Treppe: Sanfter IK-Übergang
- [ ] Walking: Kein visueller Unterschied (IK bereits ausgeblendet)
- [ ] Crouching auf flachem Boden: Keine gebeugten Knie
