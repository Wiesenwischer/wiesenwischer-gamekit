# Foot Locking — Spezifikation

> **Status:** Entwurf
> **Betrifft:** `Wiesenwischer.GameKit.CharacterController.IK` — neues Modul `FootLock.cs`
> **Problem:** Füße gleiten sichtbar bei Animations-Übergängen (Walk→Idle, Run→Stop)
> **Ziel:** Füße werden an ihrer Welt-Position "festgenagelt" solange sie am Boden stehen

---

## Problemanalyse

### Symptom

Wenn der Character stehenbleibt, stehen die Füße vom letzten Walk/Run-Schritt auseinander. Die Animation blendet zum Idle über und die Füße gleiten langsam zusammen — sichtbares "Foot Sliding".

### Ursache

Unity's Animator blendet zwischen Walk-Pose (Füße auseinander) und Idle-Pose (Füße zusammen) linear. Es gibt keinen Mechanismus der die Füße an ihrer aktuellen Position festhält.

### Abgrenzung zu FootIK

| System | Zweck | Wann aktiv |
|--------|-------|-----------|
| **FootIK** (existierend) | Terrain-Anpassung — Füße an den Boden setzen | Idle auf unebenem Terrain |
| **FootLock** (neu) | Anti-Sliding — Füße am Ort festhalten | Animations-Übergänge, Idle |

Beide Systeme sind **komplementär** und werden als separate `IIKModule`-Implementierungen vom `IKManager` orchestriert.

---

## Lösungskonzept

### Velocity-basierte Erkennung

Kein Animations-Kurven-Authoring nötig — funktioniert automatisch mit allen Mixamo-Animationen.

**Erkennung:**
1. Pro Frame: Geschwindigkeit jedes Fußknochens in Welt-Raum berechnen
2. Geschwindigkeit < Lock-Threshold für N Frames → Fuß wird gelockt
3. Geschwindigkeit > Release-Threshold → Lock wird gelöst (smooth Blend-Out)

**Hysterese** verhindert Flackern:
- Lock-Threshold: `0.05 m/s` (Fuß steht praktisch still)
- Release-Threshold: `0.15 m/s` (Fuß beginnt sich deutlich zu bewegen)
- Stable Frames: `2` (Fuß muss 2 Frames unter Threshold sein)

### Foot Lock Mechanismus

```
1. DETECT: footVelocity < lockThreshold für N Frames
     ↓
2. LOCK: Welt-Position → Character-Local-Space speichern
     ↓
3. HOLD: Jedes Frame: Local→World transformieren, per IK anwenden
     ↓
4. RELEASE: footVelocity > releaseThreshold → Smooth Blend-Out (0.15s)
```

### Character-Local-Space

**Kritisch:** Position wird in **Character-Local-Space** gespeichert, nicht in Welt-Raum. Wenn der Character sich dreht oder leicht bewegt, bleibt der Fuß korrekt relativ zum Character.

```csharp
// Beim Locken:
_lockedLocalPos = transform.InverseTransformPoint(footWorldPos);
_lockedLocalRot = Quaternion.Inverse(transform.rotation) * footWorldRot;

// Beim Anwenden:
Vector3 worldPos = transform.TransformPoint(_lockedLocalPos);
Quaternion worldRot = transform.rotation * _lockedLocalRot;
```

---

## Architektur

### Neues Modul: `FootLock : MonoBehaviour, IIKModule`

```
IKManager (orchestriert)
├── FootIK   (Terrain-Anpassung)  — existierend
└── FootLock (Anti-Sliding)       — NEU
```

**Ausführungsreihenfolge im IKManager:**
1. `FootLock.PrepareIK()` — Erkennung + Lock-Zustand aktualisieren
2. `FootIK.PrepareIK()` — Raycasts für Terrain-Anpassung
3. `FootLock.ProcessIK()` — Gelockte Position per IK setzen
4. `FootIK.ProcessIK()` — Terrain-Anpassung (nur wenn FootLock nicht aktiv)

**Priorität:** FootLock hat Vorrang vor FootIK. Wenn ein Fuß gelockt ist, überschreibt FootLock die IK-Position. FootIK greift nur ein, wenn der Fuß NICHT gelockt ist.

### Registrierungsreihenfolge

FootLock registriert sich VOR FootIK beim IKManager. Da der IKManager Module in Registrierungsreihenfolge durchläuft, wird FootLock zuerst aufgerufen.

### Koordination mit FootIK

FootLock setzt ein Flag pro Fuß: `IsLeftFootLocked`, `IsRightFootLocked`. FootIK prüft dieses Flag und überspringt gelockte Füße:

```csharp
// In FootIK.ProcessIK:
var footLock = _ikManager.GetModule<FootLock>();
if (footLock != null && footLock.IsLeftFootLocked)
{
    // Fuß wird von FootLock kontrolliert — nicht anfassen
}
else
{
    // Normales Terrain-IK anwenden
}
```

---

## Parameter

| Parameter | Default | Beschreibung |
|-----------|---------|-------------|
| `_lockVelocityThreshold` | 0.05 | Geschwindigkeit (m/s) unter der ein Fuß als "stehend" gilt |
| `_releaseVelocityThreshold` | 0.15 | Geschwindigkeit (m/s) über der ein Lock gelöst wird |
| `_stableFramesRequired` | 2 | Frames unter Lock-Threshold bevor Lock greift |
| `_releaseDuration` | 0.15 | Smooth Blend-Out Zeit beim Lösen (Sekunden) |
| `_maxLockDistance` | 0.3 | Maximaler Abstand (m) zwischen gelockter und animierter Position. Darüber wird Lock gelöst (Sicherheitsmechanismus). |

---

## Felder

```csharp
// Pro Fuß: Lock-Zustand
private bool _leftFootLocked;
private bool _rightFootLocked;
private Vector3 _leftFootLocalPos;
private Vector3 _rightFootLocalPos;
private Quaternion _leftFootLocalRot;
private Quaternion _rightFootLocalRot;

// Erkennung
private Vector3 _leftFootPrevPos;
private Vector3 _rightFootPrevPos;
private int _leftFootStableCount;
private int _rightFootStableCount;

// Release Blend
private float _leftReleaseTimer;
private float _rightReleaseTimer;
private bool _leftReleasing;
private bool _rightReleasing;
```

---

## Methoden

### PrepareIK (LateUpdate)

```csharp
public void PrepareIK()
{
    var animator = GetComponent<Animator>();
    var leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
    var rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);

    UpdateLock(leftFoot, ref _leftFootLocked, ref _leftFootLocalPos,
               ref _leftFootLocalRot, ref _leftFootPrevPos,
               ref _leftFootStableCount, ref _leftReleaseTimer,
               ref _leftReleasing);

    // Analog für rechts...
}
```

### UpdateLock (interne Methode)

```csharp
private void UpdateLock(Transform footBone, ref bool isLocked,
                        ref Vector3 localPos, ref Quaternion localRot,
                        ref Vector3 prevPos, ref int stableCount,
                        ref float releaseTimer, ref bool isReleasing)
{
    Vector3 worldPos = footBone.position;
    float velocity = (worldPos - prevPos).magnitude / Time.deltaTime;
    prevPos = worldPos;

    if (isLocked)
    {
        // Sicherheits-Check: Lock lösen wenn Fuß zu weit weg driftet
        Vector3 lockedWorldPos = transform.TransformPoint(localPos);
        if ((worldPos - lockedWorldPos).magnitude > _maxLockDistance)
        {
            isLocked = false;
            isReleasing = true;
            releaseTimer = 0f;
            return;
        }

        // Release-Check: Fuß bewegt sich wieder
        if (velocity > _releaseVelocityThreshold)
        {
            isLocked = false;
            isReleasing = true;
            releaseTimer = 0f;
        }
    }
    else if (!isReleasing)
    {
        // Lock-Check: Fuß steht still
        if (velocity < _lockVelocityThreshold && _playerController.IsGrounded)
        {
            stableCount++;
            if (stableCount >= _stableFramesRequired)
            {
                localPos = transform.InverseTransformPoint(worldPos);
                localRot = Quaternion.Inverse(transform.rotation) * footBone.rotation;
                isLocked = true;
                stableCount = 0;
            }
        }
        else
        {
            stableCount = 0;
        }
    }

    // Release Blend-Out Timer
    if (isReleasing)
    {
        releaseTimer += Time.deltaTime;
        if (releaseTimer >= _releaseDuration)
            isReleasing = false;
    }
}
```

### ProcessIK (OnAnimatorIK)

```csharp
public void ProcessIK(Animator animator, int layerIndex)
{
    if (layerIndex != 0) return;

    ApplyFootLock(animator, AvatarIKGoal.LeftFoot,
                  _leftFootLocked, _leftReleasing,
                  _leftFootLocalPos, _leftFootLocalRot,
                  _leftReleaseTimer);

    // Analog für rechts...
}

private void ApplyFootLock(Animator animator, AvatarIKGoal goal,
                           bool isLocked, bool isReleasing,
                           Vector3 localPos, Quaternion localRot,
                           float releaseTimer)
{
    if (!isLocked && !isReleasing) return;

    float weight = isLocked
        ? 1f
        : 1f - Mathf.Clamp01(releaseTimer / _releaseDuration);

    Vector3 worldPos = transform.TransformPoint(localPos);
    Quaternion worldRot = transform.rotation * localRot;

    animator.SetIKPositionWeight(goal, weight);
    animator.SetIKRotationWeight(goal, weight);
    animator.SetIKPosition(goal, worldPos);
    animator.SetIKRotation(goal, worldRot);
}
```

---

## Deaktivierung

FootLock soll **nicht** aktiv sein wenn:
- Character in der Luft ist (`!IsGrounded`)
- Character sich schnell bewegt (Speed > `_speedBlendEnd` aus FootIK)
- IKManager Master-Weight = 0

```csharp
// In PrepareIK, am Anfang:
if (!_playerController.IsGrounded)
{
    ReleaseBothFeet();
    return;
}
```

---

## IKSetupWizard Erweiterung

Der `IKSetupWizard` muss erweitert werden um `FootLock` automatisch zum Prefab hinzuzufügen:

```csharp
// In IKSetupWizard, nach FootIK-Setup:
var footLock = modelGO.AddComponent<FootLock>();
// Default-Werte werden über SerializeField gesetzt
```

---

## Erwartetes Verhalten

| Szenario | Vorher | Nachher |
|----------|--------|---------|
| Walk → Idle | Füße gleiten zusammen | Füße bleiben stehen, Idle-Animation blendet über Body/Oberkörper |
| Run → Stop | Füße rutschen in Idle-Position | Füße locken, Stop-Animation spielt mit geplanteten Füßen |
| Idle Stehen | Füße leicht wackeln | Füße stabil am Ort |
| Drehen im Stand | Füße gleiten | Füße bleiben relativ zum Character |
| Walk auf Terrain | Terrain-IK aktiv | FootLock inaktiv, FootIK macht Terrain-Anpassung |

---

## Nicht-Ziele

- Keine Animation-Curves (zu viel manueller Aufwand für Mixamo-Animations)
- Kein neues Interface (nutzt bestehendes `IIKModule`)
- Keine Änderung an `IKManager`
- Kein Root-Motion System
- Kein Fuß-Sweep/Replanting (Fuß hebt und setzt sich neu — wäre Phase 2)

---

## Spätere Erweiterungen (nicht in dieser Phase)

1. **Foot Replanting:** Wenn der Character zu lange steht und die Füße unnatürlich auseinander sind → automatisches Umsetzen eines Fußes
2. **Animation Curves:** Für höhere Präzision bei eigenen Animationen
3. **Terrain-adaptiertes Locking:** Raycast von der gelockten Position, um auch auf unebenem Terrain korrekt zu locken

---

## Test-Plan

### Unit Tests

- `FootLock_VelocityBelowThreshold_LocksAfterStableFrames`
- `FootLock_VelocityAboveThreshold_ReleasesLock`
- `FootLock_ReleaseBlend_ReducesWeightOverTime`
- `FootLock_LocalSpaceStorage_SurvivesCharacterRotation`
- `FootLock_MaxLockDistance_ForcesRelease`
- `FootLock_Airborne_ReleasesAllLocks`

### Play Mode Verifikation

- [ ] Walk → Idle: Kein sichtbares Foot Sliding
- [ ] Run → Stop: Füße bleiben am Ort
- [ ] Idle auf flachem Boden: Füße stabil
- [ ] Walk auf Treppen: FootIK Terrain-Anpassung funktioniert (FootLock nicht aktiv)
- [ ] Schnelles Umkehren: Kein unnatürliches Fuß-Verhalten
- [ ] FootLock deaktivieren: Keine Fehlermeldungen
