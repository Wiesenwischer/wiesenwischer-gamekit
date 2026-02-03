# Character Controller Architektur-Refactoring

## Übersicht

Dieses Dokument beschreibt das geplante Refactoring des Character Controller Systems zur Verbesserung der Modularität und Wartbarkeit.

---

## Problemanalyse

### Aktuelle Architektur

```
CharacterMotor (~3000 LOC)     ← Physics + Step Detection + Ground Probing
       ↓
CharacterLocomotion (~370 LOC) ← Velocity + Gravity + Acceleration + Rotation
       ↓
StateMachine/States            ← Behavior + teilweise Physics-Logik
```

### Identifizierte Probleme

1. **Dreifache Datenhaltung**
   - `Motor.GroundingStatus` → Physics-Ebene
   - `Locomotion._cachedGroundInfo` → Behavior-Ebene Cache
   - `ReusableData.IsGrounded` → State Machine

2. **Monolithische Komponenten**
   - CharacterLocomotion enthält: Gravity, Acceleration, Rotation, Ground Snapping, Motor-Callbacks
   - Erschwert Testing und Wiederverwendung

3. **Unklare Verantwortlichkeiten**
   - States enthalten teilweise Physics-Logik (Ceiling Detection im JumpingState)
   - Locomotion interpretiert Motor-Status

---

## Zielarchitektur

### Prinzip: Single Source of Truth

`PlayerStateReusableData` wird zur einzigen Datenquelle. Alle anderen Komponenten lesen/schreiben nur dorthin.

### Neue Modulstruktur

```
Runtime/Core/
├── Motor/
│   └── CharacterMotor.cs          (unverändert - Physics only)
│
├── Locomotion/
│   ├── CharacterLocomotion.cs     (verschlankt: ~150 LOC)
│   ├── ILocomotionController.cs
│   ├── LocomotionConfig.cs
│   └── Modules/
│       ├── GravityModule.cs       (NEU)
│       ├── AccelerationModule.cs  (NEU)
│       ├── GroundDetectionModule.cs (NEU)
│       ├── JumpModule.cs          (NEU)
│       └── SlopeModule.cs         (NEU)
│
├── StateMachine/
│   └── (States bleiben, nutzen Module)
│
└── Data/
    └── PlayerStateReusableData.cs (erweitert)
```

---

## Module im Detail

### 1. GravityModule

**Pfad:** `Runtime/Core/Locomotion/Modules/GravityModule.cs`

**Verantwortung:** Vertikale Velocity-Berechnung, Grounding-Velocity

```csharp
public class GravityModule
{
    public float CalculateVerticalVelocity(
        float currentVelocity,
        bool isGrounded,
        float gravity,
        float maxFallSpeed,
        float deltaTime);

    public float GetGroundingVelocity(); // -2f
}
```

**Extrahiert aus:** `CharacterLocomotion.CalculateVerticalVelocity()`

---

### 2. AccelerationModule

**Pfad:** `Runtime/Core/Locomotion/Modules/AccelerationModule.cs`

**Verantwortung:** Horizontale Velocity mit Acceleration/Deceleration

```csharp
public class AccelerationModule
{
    public Vector3 CalculateHorizontalVelocity(
        Vector3 currentVelocity,
        Vector3 targetVelocity,
        float acceleration,
        float deceleration,
        float airControl,
        bool isGrounded,
        float deltaTime);
}
```

**Extrahiert aus:** `CharacterLocomotion.ApplyAcceleration()`, `CalculateTargetHorizontalVelocity()`

---

### 3. GroundDetectionModule

**Pfad:** `Runtime/Core/Locomotion/Modules/GroundDetectionModule.cs`

**Verantwortung:** Interpretation des Motor.GroundingStatus

```csharp
public class GroundDetectionModule
{
    public GroundInfo GetGroundInfo(CharacterMotor motor);
    public bool IsStableOnGround(CharacterMotor motor);
    public bool IsOnLedge(CharacterMotor motor);
    public float GetSlopeAngle(CharacterMotor motor);
}
```

**Kapselt:** Alle `motor.GroundingStatus.*` Zugriffe

---

### 4. JumpModule

**Pfad:** `Runtime/Core/Locomotion/Modules/JumpModule.cs`

**Verantwortung:** Jump-Berechnungen, Ceiling Detection

```csharp
public class JumpModule
{
    public float CalculateJumpVelocity(float jumpHeight, float gravity);
    public float ApplyVariableJump(float velocity, bool jumpHeld, float cutMultiplier);
    public bool CheckCeiling(CharacterMotor motor, float checkDistance);
}
```

**Extrahiert aus:** `PlayerJumpingState.CalculateJumpVelocity()`, `HitCeiling()`

---

### 5. SlopeModule

**Pfad:** `Runtime/Core/Locomotion/Modules/SlopeModule.cs`

**Verantwortung:** Slope-Handling, Sliding

```csharp
public class SlopeModule
{
    public bool ShouldSlide(float slopeAngle, float maxSlopeAngle);
    public Vector3 CalculateSlideVelocity(Vector3 slopeNormal, float slideSpeed);
    public Vector3 ProjectOnSlope(Vector3 velocity, Vector3 slopeNormal);
}
```

**Hinweis:** Neues Feature - bisher nur in Config definiert, nicht implementiert

---

## CharacterLocomotion nach Refactoring

```csharp
public class CharacterLocomotion : ILocomotionController, ICharacterController
{
    // Module
    private readonly GravityModule _gravity;
    private readonly AccelerationModule _acceleration;
    private readonly GroundDetectionModule _groundDetection;
    private readonly JumpModule _jump;
    private readonly SlopeModule _slope;

    // Motor-Callbacks delegieren an Module
    public void UpdateVelocity(ref Vector3 velocity, float deltaTime)
    {
        var groundInfo = _groundDetection.GetGroundInfo(_motor);
        var horizontal = _acceleration.Calculate(...);
        var vertical = _gravity.Calculate(...);
        velocity = horizontal + Vector3.up * vertical;
    }
}
```

**Reduzierung:** ~370 LOC → ~150 LOC

---

## States-Anpassungen

### Ceiling Detection
- **Vorher:** Inline SphereCast in `PlayerJumpingState`
- **Nachher:** `JumpModule.CheckCeiling()`

### Ledge Detection
- **Vorher:** Direkter Zugriff auf `motor.GroundingStatus.SnappingPrevented`
- **Nachher:** `GroundDetectionModule.IsOnLedge()`

### Slope Sliding (optional)
- Neuer State `PlayerSlidingState` für Rutschen auf steilen Hängen
- Nutzt `SlopeModule`

---

## Implementierungsreihenfolge

| Schritt | Beschreibung | Risiko | Abhängigkeiten |
|---------|--------------|--------|----------------|
| 1 | `GravityModule` erstellen & einbinden | Niedrig | - |
| 2 | `AccelerationModule` erstellen & einbinden | Niedrig | - |
| 3 | `GroundDetectionModule` erstellen & einbinden | Mittel | - |
| 4 | `JumpModule` erstellen & JumpingState anpassen | Niedrig | Schritt 1 |
| 5 | `SlopeModule` erstellen (neues Feature) | Mittel | Schritt 3 |
| 6 | States aufräumen (Ledge Detection, etc.) | Niedrig | Schritt 3, 4 |
| 7 | ReusableData als Single Source konsolidieren | Mittel | Alle vorherigen |

---

## Verifikation

Nach jedem Schritt:

1. **Kompilierung prüfen**
   - Unity Editor öffnen
   - Keine Compiler-Fehler

2. **Play Mode Test**
   - [ ] Laufen auf ebenem Boden
   - [ ] Treppen hoch/runter (Step Detection)
   - [ ] Springen (Variable Jump)
   - [ ] Fallen von Rampe (Gravity)
   - [ ] Rampe hoch/runter laufen

---

## Umfang

- **5 neue Dateien** (Module)
- **3 modifizierte Dateien** (Locomotion, States, ReusableData)
- **~500 Zeilen neuer Code** (Module)
- **~200 Zeilen entfernter Code** (aus Locomotion/States)
- **Netto:** ~300 Zeilen mehr, aber deutlich bessere Struktur

---

## Vorteile

1. **Testbarkeit:** Module können isoliert getestet werden
2. **Wiederverwendbarkeit:** Module können in anderen Projekten genutzt werden
3. **Lesbarkeit:** CharacterLocomotion wird übersichtlich
4. **Wartbarkeit:** Änderungen an einem Feature betreffen nur ein Modul
5. **Erweiterbarkeit:** Neue Features als neue Module hinzufügen
