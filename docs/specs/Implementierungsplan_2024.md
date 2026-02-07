# Implementierungsplan - GameKit Character Controller

## Statusübersicht

> **Letzte Aktualisierung:** 2026-02-07
> **Aktuelle Phase:** Phase 1 - Vorbereitung
> **Aktueller Schritt:** 1.1

---

## Tracking-Tabelle

### Phase 1: Animation-Vorbereitung
- [ ] 1.1 Character Asset beschaffen (Mixamo)
- [ ] 1.2 Character in Unity importieren
- [ ] 1.3 Basis-Animationen beschaffen
- [ ] 1.4 Animation Package Struktur erstellen

### Phase 2: Animator Setup
- [ ] 2.1 Avatar Masks erstellen
- [ ] 2.2 Animator Controller erstellen
- [ ] 2.3 Locomotion Blend Tree (Idle/Walk/Run/Sprint)
- [ ] 2.4 Airborne States (Jump/Fall/Land)
- [ ] 2.5 Parameter-Bridge zum Code

### Phase 3: Animation-Integration
- [ ] 3.1 IAnimationController Interface
- [ ] 3.2 CharacterAnimator Komponente
- [ ] 3.3 State Machine → Animator Sync
- [ ] 3.4 Player Prefab mit Animation
- [ ] 3.5 Testing & Feintuning

### Phase 4: Ability System Grundstruktur
- [ ] 4.1 IAbility Interface definieren
- [ ] 4.2 AbilitySystem Manager
- [ ] 4.3 JumpAbility implementieren
- [ ] 4.4 SprintAbility implementieren
- [ ] 4.5 Ability-Animation Layer Integration

### Phase 5: Netzwerk-Grundstruktur
- [ ] 5.1 FishNet Package einbinden
- [ ] 5.2 NetworkPlayer Komponente
- [ ] 5.3 Input Synchronisation
- [ ] 5.4 Position/Rotation Sync
- [ ] 5.5 Client-Side Prediction Basis

### Phase 6: Netzwerk-Animation
- [ ] 6.1 Animator Parameter Sync
- [ ] 6.2 State Sync (Grounded/Airborne)
- [ ] 6.3 Ability Sync
- [ ] 6.4 Lag Compensation

---

## Phase 1: Animation-Vorbereitung

### 1.1 Character Asset beschaffen
**Ziel:** Humanoid Character mit Rig für Animationen

**Optionen:**
- Mixamo (kostenlos, gute Qualität)
- Unity Asset Store
- Eigenes Modell

**Anforderungen:**
- Humanoid Rig
- T-Pose oder A-Pose
- FBX Format
- ~10-20k Polygone (für MMO geeignet)

**Deliverables:**
- [ ] Character FBX Datei
- [ ] Texturen/Materialien

---

### 1.2 Character in Unity importieren
**Ziel:** Character korrekt in Unity einrichten

**Schritte:**
1. FBX in `Assets/Characters/` importieren
2. Rig-Type auf "Humanoid" setzen
3. Avatar konfigurieren
4. Materialien zuweisen

**Deliverables:**
- [ ] Character in Unity importiert
- [ ] Avatar korrekt konfiguriert
- [ ] Materialien zugewiesen

---

### 1.3 Basis-Animationen beschaffen
**Ziel:** Alle benötigten Locomotion-Animationen

**Benötigte Animationen:**
| Animation | Loop | Quelle |
|-----------|------|--------|
| Idle | ✅ | Mixamo |
| Walk | ✅ | Mixamo |
| Run | ✅ | Mixamo |
| Sprint | ✅ | Mixamo |
| Jump_Start | ❌ | Mixamo |
| Jump_Loop/Fall | ✅ | Mixamo |
| Land | ❌ | Mixamo |

**Deliverables:**
- [ ] Alle Animationen heruntergeladen
- [ ] In Unity importiert
- [ ] Loop-Einstellungen korrekt

---

### 1.4 Animation Package Struktur erstellen
**Ziel:** Neues Package für Animationen

**Struktur:**
```
Packages/Wiesenwischer.GameKit.CharacterController.Animation/
├── package.json
├── Runtime/
│   ├── Core/
│   │   ├── IAnimationController.cs
│   │   └── CharacterAnimator.cs
│   └── Wiesenwischer.GameKit.CharacterController.Animation.Runtime.asmdef
├── Editor/
│   └── Wiesenwischer.GameKit.CharacterController.Animation.Editor.asmdef
└── Resources/
    ├── AnimatorControllers/
    ├── AvatarMasks/
    └── BlendTrees/
```

**Deliverables:**
- [ ] Package-Ordner erstellt
- [ ] package.json konfiguriert
- [ ] Assembly Definitions erstellt

---

## Phase 2: Animator Setup

### 2.1 Avatar Masks erstellen
**Ziel:** Trennung von Ober- und Unterkörper für Layer

**Masks:**
| Name | Körperteile | Verwendung |
|------|-------------|------------|
| Mask_FullBody | Alle | Base Layer |
| Mask_UpperBody | Spine, Arms, Head | Ability Layer |
| Mask_LowerBody | Hips, Legs | Optional |

**Deliverables:**
- [ ] Mask_FullBody.mask
- [ ] Mask_UpperBody.mask
- [ ] Mask_LowerBody.mask

---

### 2.2 Animator Controller erstellen
**Ziel:** Haupt-Animator mit Layer-Struktur

**Layer-Struktur:**
```
Layer 0: Base Movement (Weight: 1.0, Mask: FullBody)
    └── Locomotion Blend Tree
    └── Airborne Sub-State Machine

Layer 1: Abilities (Weight: 0.0, Mask: UpperBody)
    └── Empty (für später)
```

**Deliverables:**
- [ ] CharacterAnimator.controller erstellt
- [ ] Layer konfiguriert
- [ ] Basis-Transitions

---

### 2.3 Locomotion Blend Tree
**Ziel:** Smooth blending zwischen Idle/Walk/Run/Sprint

**Blend Tree Struktur:**
```
Locomotion (1D Blend Tree)
├── Parameter: Speed (0.0 - 1.5)
├── Idle      @ 0.0
├── Walk      @ 0.4
├── Run       @ 1.0
└── Sprint    @ 1.5
```

**Deliverables:**
- [ ] Blend Tree erstellt
- [ ] Animationen zugewiesen
- [ ] Thresholds konfiguriert

---

### 2.4 Airborne States
**Ziel:** Jump, Fall, Land Animationen

**State Machine:**
```
Grounded ──[Jump Trigger]──> Jump_Start
Jump_Start ──[Exit Time]──> Fall_Loop
Fall_Loop ──[IsGrounded]──> Land
Land ──[Exit Time]──> Locomotion
```

**Parameters:**
- `IsGrounded` (Bool)
- `VerticalVelocity` (Float)
- `Jump` (Trigger)

**Deliverables:**
- [ ] Jump_Start State
- [ ] Fall_Loop State
- [ ] Land State
- [ ] Transitions konfiguriert

---

### 2.5 Parameter-Bridge zum Code
**Ziel:** Code-seitige Kontrolle des Animators

**Parameter-Mapping:**
| Parameter | Typ | Quelle |
|-----------|-----|--------|
| Speed | Float | Locomotion.SpeedModifier * InputMagnitude |
| IsGrounded | Bool | StateMachine.IsGrounded |
| VerticalVelocity | Float | Locomotion.VerticalVelocity |
| Jump | Trigger | JumpAbility.Activate |

**Deliverables:**
- [ ] Parameter-Namen als Konstanten
- [ ] Hash-IDs für Performance

---

## Phase 3: Animation-Integration

### 3.1 IAnimationController Interface
**Ziel:** Abstraktion für Animation-Steuerung

```csharp
public interface IAnimationController
{
    void SetSpeed(float speed);
    void SetGrounded(bool isGrounded);
    void SetVerticalVelocity(float velocity);
    void TriggerJump();
    void TriggerLand();
    void SetAbilityLayerWeight(float weight);
}
```

**Deliverables:**
- [ ] Interface definiert
- [ ] Im Animation Package

---

### 3.2 CharacterAnimator Komponente
**Ziel:** MonoBehaviour das IAnimationController implementiert

```csharp
public class CharacterAnimator : MonoBehaviour, IAnimationController
{
    [SerializeField] private Animator _animator;

    // Parameter Hashes
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    // ...
}
```

**Deliverables:**
- [ ] CharacterAnimator.cs
- [ ] Parameter Hashes
- [ ] Alle Interface-Methoden

---

### 3.3 State Machine → Animator Sync
**Ziel:** Automatische Synchronisation

**Ansatz:**
```csharp
// In PlayerController oder eigene Komponente
void Update()
{
    _animationController.SetSpeed(GetCurrentSpeed());
    _animationController.SetGrounded(_stateMachine.IsGrounded);
    _animationController.SetVerticalVelocity(_locomotion.VerticalVelocity);
}
```

**Deliverables:**
- [ ] Sync-Logik implementiert
- [ ] Event-basiert wo möglich

---

### 3.4 Player Prefab mit Animation
**Ziel:** Funktionierendes Player Prefab

**Komponenten:**
- PlayerController
- CharacterMotor
- CharacterAnimator
- Animator (mit Controller)
- Character Mesh

**Deliverables:**
- [ ] Player.prefab aktualisiert
- [ ] Alle Komponenten verbunden
- [ ] In Demo-Szene getestet

---

### 3.5 Testing & Feintuning
**Ziel:** Polierte Animationen

**Tests:**
- [ ] Idle → Walk → Run → Sprint smooth
- [ ] Jump Animation timing
- [ ] Land Animation timing
- [ ] Keine Foot-Sliding
- [ ] Rotation während Bewegung

**Deliverables:**
- [ ] Alle Tests bestanden
- [ ] Blend-Zeiten angepasst

---

## Phase 4: Ability System Grundstruktur

### 4.1 IAbility Interface
**Ziel:** Basis-Interface für alle Abilities

```csharp
public interface IAbility
{
    string AbilityName { get; }
    bool CanActivate(IAbilityContext context);
    void Activate(IAbilityContext context);
    void Deactivate(IAbilityContext context);
    void Tick(float deltaTime);
}

public interface IAbilityContext
{
    PlayerController Player { get; }
    IAnimationController Animator { get; }
    ILocomotionController Locomotion { get; }
}
```

**Deliverables:**
- [ ] IAbility.cs
- [ ] IAbilityContext.cs
- [ ] Neues Abilities Package oder in Core

---

### 4.2 AbilitySystem Manager
**Ziel:** Verwaltet aktive Abilities

```csharp
public class AbilitySystem : MonoBehaviour
{
    private List<IAbility> _abilities = new();
    private List<IAbility> _activeAbilities = new();

    public void RegisterAbility(IAbility ability);
    public bool TryActivate(string abilityName);
    public void Tick(float deltaTime);
}
```

**Deliverables:**
- [ ] AbilitySystem.cs
- [ ] Registration/Activation Logik
- [ ] Tick-System

---

### 4.3 JumpAbility
**Ziel:** Jump als erste Ability

```csharp
public class JumpAbility : IAbility
{
    public bool CanActivate(IAbilityContext ctx)
        => ctx.Player.StateMachine.IsGrounded;

    public void Activate(IAbilityContext ctx)
    {
        ctx.Animator.TriggerJump();
        ctx.Player.StateMachine.ChangeState(JumpingState);
    }
}
```

**Deliverables:**
- [ ] JumpAbility.cs
- [ ] In AbilitySystem registriert
- [ ] Input-Binding

---

### 4.4 SprintAbility
**Ziel:** Sprint als Hold-Ability

```csharp
public class SprintAbility : IAbility
{
    public bool IsActive { get; private set; }

    public void Activate(IAbilityContext ctx)
    {
        IsActive = true;
        ctx.Locomotion.SpeedModifier = 1.5f;
    }

    public void Deactivate(IAbilityContext ctx)
    {
        IsActive = false;
        ctx.Locomotion.SpeedModifier = 1.0f;
    }
}
```

**Deliverables:**
- [ ] SprintAbility.cs
- [ ] Hold-Input Logik
- [ ] Stamina-System Vorbereitung (optional)

---

### 4.5 Ability-Animation Layer
**Ziel:** Animator Layer 1 für Abilities

**Integration:**
- Abilities können Animator Layer 1 aktivieren
- Oberkörper-Animationen (Cast, Attack)
- Weight-Steuerung über IAnimationController

**Deliverables:**
- [ ] Layer 1 funktional
- [ ] Test mit Dummy-Animation

---

## Phase 5: Netzwerk-Grundstruktur

### 5.1 FishNet einbinden
**Ziel:** FishNet Package installieren

**Schritte:**
1. FishNet von Asset Store/GitHub
2. Package Manager import
3. NetworkManager Setup

**Deliverables:**
- [ ] FishNet installiert
- [ ] NetworkManager in Szene
- [ ] Basis-Verbindung funktioniert

---

### 5.2 NetworkPlayer Komponente
**Ziel:** Netzwerk-fähiger Player

```csharp
public class NetworkPlayer : NetworkBehaviour
{
    [SyncVar] private Vector3 _position;
    [SyncVar] private Quaternion _rotation;

    public override void OnStartClient() { }
    public override void OnStartServer() { }
}
```

**Deliverables:**
- [ ] NetworkPlayer.cs
- [ ] Spawn-System
- [ ] Owner vs. Remote Unterscheidung

---

### 5.3 Input Synchronisation
**Ziel:** Client Input → Server

```csharp
[ServerRpc]
void SendInput(InputPayload input)
{
    // Server verarbeitet Input
}
```

**Deliverables:**
- [ ] InputPayload struct
- [ ] ServerRpc für Input
- [ ] Input-Buffering

---

### 5.4 Position/Rotation Sync
**Ziel:** Authoritative Server Movement

**Ansatz:**
- Client sendet Input
- Server berechnet Position
- Server sendet State zurück
- Client interpoliert

**Deliverables:**
- [ ] Server-Side Movement
- [ ] State Broadcast
- [ ] Client Interpolation

---

### 5.5 Client-Side Prediction
**Ziel:** Responsive trotz Latenz

**Komponenten:**
- Input Buffer
- Prediction Buffer
- Reconciliation bei Mismatch

**Deliverables:**
- [ ] CSP Grundstruktur
- [ ] Prediction funktional
- [ ] Reconciliation funktional

---

## Phase 6: Netzwerk-Animation

### 6.1-6.4 (Details folgen nach Phase 5)

---

## Abhängigkeiten

```
Phase 1 ──> Phase 2 ──> Phase 3
                            │
                            v
                       Phase 4
                            │
                            v
                       Phase 5 ──> Phase 6
```

---

## Zeitschätzung

| Phase | Geschätzte Dauer |
|-------|------------------|
| Phase 1 | 1-2 Stunden |
| Phase 2 | 2-3 Stunden |
| Phase 3 | 2-3 Stunden |
| Phase 4 | 3-4 Stunden |
| Phase 5 | 4-6 Stunden |
| Phase 6 | 3-4 Stunden |

**Gesamt:** ~15-22 Stunden

---

## Notizen

_Hier können während der Implementierung Notizen hinzugefügt werden._

