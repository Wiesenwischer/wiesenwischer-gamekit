# Nächste Schritte - GameKit Character Controller

## Übersicht

Nach dem erfolgreichen Release von **Core v0.1.0** folgen die nächsten Module in dieser Reihenfolge:

| Priorität | Package | Beschreibung | Abhängigkeiten |
|-----------|---------|--------------|----------------|
| 1 | **Camera** | Third-Person Kamera mit Cinemachine | Core |
| 2 | **Animation** | Animator Integration, Blend Trees | Core |
| 3 | **Network** | FishNet Integration, CSP | Core |
| 4 | **IK** | Animation Rigging, LookAt | Core, Animation |
| 5 | **Abilities** | Skills, Combat System | Core, Animation, Network |

---

## Phase 1: Camera-Package

**Package:** `wiesenwischer.gamekit.charactercontroller.camera`

### Ziele
- Third-Person Kamera mit Orbit-Kontrolle
- Kollisionserkennung (kein Clipping durch Wände)
- Smooth Follow und Look-At
- Cinemachine-basiert für maximale Flexibilität

### Komponenten

#### 1.1 CameraController
```
Runtime/
├── CameraController.cs          # Hauptkomponente
├── CameraConfig.cs              # ScriptableObject für Settings
├── CameraCollision.cs           # Kollisionserkennung
└── CameraInputProvider.cs       # Maus/Gamepad Input für Kamera
```

#### 1.2 Cinemachine Setup
- **FreeLook Camera**: Orbit um Player
- **Virtual Camera States**: Combat, Exploration, Dialogue
- **Collision Extension**: Keine Wand-Durchdringung

#### 1.3 CameraConfig (ScriptableObject)
```csharp
[CreateAssetMenu]
public class CameraConfig : ScriptableObject
{
    [Header("Distance")]
    public float DefaultDistance = 5f;
    public float MinDistance = 2f;
    public float MaxDistance = 15f;

    [Header("Sensitivity")]
    public float HorizontalSensitivity = 300f;
    public float VerticalSensitivity = 2f;

    [Header("Limits")]
    public float MinVerticalAngle = -30f;
    public float MaxVerticalAngle = 60f;

    [Header("Smoothing")]
    public float FollowSmoothing = 0.1f;
    public float RotationSmoothing = 0.05f;

    [Header("Collision")]
    public LayerMask CollisionLayers;
    public float CollisionRadius = 0.3f;
}
```

### Commits (geschätzt 8)
1. feat: Initialisiere Camera-Package Struktur
2. feat: Implementiere CameraConfig ScriptableObject
3. feat: Implementiere CameraController Basis
4. feat: Integriere Cinemachine FreeLook
5. feat: Implementiere Kamera-Kollision
6. feat: Implementiere CameraInputProvider
7. feat: Füge Camera States hinzu (Combat/Exploration)
8. docs: Dokumentiere Camera-Package

---

## Phase 2: Animation-Package

**Package:** `wiesenwischer.gamekit.charactercontroller.animation`

### Ziele
- Animator-Integration mit State Machine
- Blend Trees für Movement
- Procedural Animation Hooks
- Event-System für Footsteps, etc.

### Komponenten

#### 2.1 Animator Integration
```
Runtime/
├── CharacterAnimator.cs         # Hauptkomponente
├── AnimationConfig.cs           # ScriptableObject
├── AnimatorParameters.cs        # Parameter-Namen Constants
├── BlendTree/
│   ├── LocomotionBlendTree.cs   # Walk/Run Blending
│   └── AirborneBlendTree.cs     # Jump/Fall Blending
└── Events/
    ├── AnimationEventReceiver.cs
    └── FootstepHandler.cs
```

#### 2.2 Animator Controller Struktur
```
States:
├── Locomotion (Blend Tree)
│   ├── Idle
│   ├── Walk
│   └── Run
├── Airborne (Blend Tree)
│   ├── Jump
│   ├── Fall
│   └── Land
└── Actions (Sub-State Machine)
    ├── Attack
    └── Interact
```

#### 2.3 AnimationConfig (ScriptableObject)
```csharp
[CreateAssetMenu]
public class AnimationConfig : ScriptableObject
{
    [Header("Blend Thresholds")]
    public float IdleThreshold = 0.1f;
    public float WalkThreshold = 0.5f;

    [Header("Transition Times")]
    public float LocomotionTransitionTime = 0.15f;
    public float AirborneTransitionTime = 0.1f;

    [Header("Root Motion")]
    public bool UseRootMotion = false;
    public float RootMotionMultiplier = 1f;
}
```

### Commits (geschätzt 10)
1. feat: Initialisiere Animation-Package Struktur
2. feat: Implementiere AnimationConfig ScriptableObject
3. feat: Implementiere CharacterAnimator Basis
4. feat: Erstelle Locomotion Blend Tree
5. feat: Erstelle Airborne Blend Tree
6. feat: Implementiere AnimatorParameters
7. feat: Implementiere AnimationEventReceiver
8. feat: Implementiere FootstepHandler
9. feat: Integriere mit PlayerController
10. docs: Dokumentiere Animation-Package

---

## Phase 3: Network-Package

**Package:** `wiesenwischer.gamekit.charactercontroller.network`

### Ziele
- FishNet Integration
- Client-Side Prediction (CSP) Implementation
- Server Reconciliation
- Lag Compensation

### Komponenten

#### 3.1 Network Components
```
Runtime/
├── NetworkPlayerController.cs    # Networked Version
├── NetworkInputProvider.cs       # Input über Netzwerk
├── Prediction/
│   ├── ClientPrediction.cs       # Client-seitige Vorhersage
│   ├── ServerReconciliation.cs   # Server-Korrektur
│   └── StateBuffer.cs            # State History
└── Sync/
    ├── PositionSync.cs           # Position Synchronisation
    └── StateSync.cs              # State Machine Sync
```

#### 3.2 CSP Workflow
```
Client:
1. Input sammeln → InputBuffer
2. Input an Server senden
3. Lokal vorhersagen (Prediction)
4. Server-State empfangen
5. Vergleichen & ggf. Rollback

Server:
1. Input empfangen
2. Autoritativ simulieren
3. State an alle Clients senden
```

### Commits (geschätzt 12)
1. feat: Initialisiere Network-Package Struktur
2. feat: Füge FishNet Dependency hinzu
3. feat: Implementiere NetworkPlayerController
4. feat: Implementiere NetworkInputProvider
5. feat: Implementiere InputBuffer Netzwerk-Serialisierung
6. feat: Implementiere ClientPrediction
7. feat: Implementiere ServerReconciliation
8. feat: Implementiere StateBuffer
9. feat: Implementiere PositionSync
10. feat: Implementiere StateSync
11. test: Füge Network Tests hinzu
12. docs: Dokumentiere Network-Package

---

## Phase 4: IK-Package

**Package:** `wiesenwischer.gamekit.charactercontroller.ik`

### Ziele
- Foot IK für Terrain-Anpassung
- Look-At IK für Kopf/Augen
- Hand IK für Interaktionen
- Animation Rigging Integration

### Komponenten
```
Runtime/
├── FootIK.cs                    # Fuß-Platzierung
├── LookAtIK.cs                  # Kopf/Augen Tracking
├── HandIK.cs                    # Hand-Positionierung
└── IKConfig.cs                  # ScriptableObject
```

---

## Phase 5: Abilities-Package

**Package:** `wiesenwischer.gamekit.charactercontroller.abilities`

### Ziele
- Skill-System Framework
- Combat Basics (Attack, Block, Dodge)
- Ability Slots & Cooldowns
- Buff/Debuff System

### Komponenten
```
Runtime/
├── Abilities/
│   ├── IAbility.cs
│   ├── AbilityBase.cs
│   └── AbilityConfig.cs
├── Combat/
│   ├── CombatController.cs
│   ├── DamageSystem.cs
│   └── HitDetection.cs
└── Buffs/
    ├── BuffSystem.cs
    └── BuffConfig.cs
```

---

## Empfohlene Reihenfolge

### Kurzfristig (nächste 2 Wochen)
1. ✅ Core v0.1.0 released
2. 🎯 Camera-Package starten
3. 🎯 Animation-Package parallel vorbereiten

### Mittelfristig (1-2 Monate)
4. Camera + Animation fertigstellen
5. Network-Package beginnen
6. IK-Package (kann parallel zu Network)

### Langfristig (3+ Monate)
7. Network-Package fertigstellen
8. Abilities-Package
9. Integration & Polish

---

## Nächste Aktion

**Empfehlung:** Mit dem **Camera-Package** starten, da es:
- Sofort sichtbaren Mehrwert für die Demo bietet
- Relativ einfach zu implementieren ist
- Keine komplexen Abhängigkeiten hat
- Die Spielbarkeit der Demo drastisch verbessert

Soll ich mit dem Camera-Package beginnen?
