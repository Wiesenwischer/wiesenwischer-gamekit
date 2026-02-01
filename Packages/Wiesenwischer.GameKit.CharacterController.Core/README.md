# Wiesenwischer GameKit - Character Controller Core

Ein modulares, MMO-fähiges Character Controller System für Unity 2022.3 LTS.

## Features

- **State Machine basiertes Movement**: Grounded, Jumping, Falling States mit konfigurierbaren Transitions
- **Kinematische Physik**: Eigene Kollisionserkennung mit CapsuleCollider (kein Unity CharacterController)
- **Deterministische Bewegung**: Fixed Tick System (60 Hz) für Client-Side Prediction (CSP)
- **Flexible Input Abstraktion**: Support für Unity Input System und Legacy Input
- **Umfangreiche Ground Detection**: SphereCast + Multi-Raycast, Slope Handling, Step Detection
- **Slope Sliding**: Automatisches Rutschen auf zu steilen Hängen
- **CSP-Ready**: InputBuffer, PredictionBuffer, Tick-basierte Strukturen für MMO-Integration
- **Editor Tools**: Custom Inspectors, Debug Gizmos, Setup Wizards

## Installation

### Via Package Manager (Git URL)

1. Öffne den Unity Package Manager (Window > Package Manager)
2. Klicke auf das "+" Symbol
3. Wähle "Add package from git URL..."
4. Füge ein: `https://github.com/Wiesenwischer/wiesenwischer-gamekit-charactercontroller.git?path=Packages/Wiesenwischer.GameKit.CharacterController.Core`

### Manuell

1. Clone das Repository
2. Kopiere den `Packages/Wiesenwischer.GameKit.CharacterController.Core` Ordner in dein Unity Projekt

## Schnellstart

### 1. LocomotionConfig erstellen

```
Rechtsklick im Project Window
→ Create > Wiesenwischer > GameKit > Locomotion Config
```

Oder via Menü: **Wiesenwischer > GameKit > Create Default LocomotionConfig**

### 2. Player Setup

1. Erstelle ein leeres GameObject
2. Füge `CapsuleCollider` hinzu (wird automatisch durch PlayerController hinzugefügt)
3. Füge `PlayerController` hinzu
4. Füge `PlayerInputProvider` hinzu
5. Weise die LocomotionConfig zu

Oder via Menü: **Wiesenwischer > GameKit > Create Demo Scene**

> **Hinweis**: Dieses System verwendet **kinematische Physik** mit CapsuleCollider anstelle von Unity's CharacterController. Dies ermöglicht deterministische Bewegung für Client-Side Prediction (CSP) in MMO-Szenarien.

### 3. Fertig!

WASD zum Bewegen, Leertaste zum Springen, Shift zum Sprinten.

## API Übersicht

### Haupt-Komponenten

| Klasse | Beschreibung |
|--------|--------------|
| `PlayerController` | Hauptkomponente, integriert alle Systeme |
| `LocomotionConfig` | ScriptableObject mit allen Parametern |
| `CharacterStateMachine` | State Machine für Character States |
| `CharacterLocomotion` | High-Level Locomotion-Verhalten |
| `KinematicMotor` | Kinematische Physik (Kollision, Depenetration) |
| `GroundingDetection` | Bodenerkennung mit Slope Support |

### Input Provider

| Klasse | Beschreibung |
|--------|--------------|
| `PlayerInputProvider` | Spieler-Input (Unity Input System + Legacy) |
| `AIInputProvider` | Programmatischer Input für KI/Tests |

### States

| State | Beschreibung |
|-------|--------------|
| `GroundedState` | Auf dem Boden, Walking/Running |
| `JumpingState` | Aktiver Sprung (aufwärts) |
| `FallingState` | Fallen (abwärts) |

### CSP Strukturen

| Klasse | Beschreibung |
|--------|--------------|
| `ControllerInput` | Serialisierbarer Input-Struct |
| `InputBuffer<T>` | Ring-Buffer für Input History |
| `PredictionBuffer` | Buffer für State History |
| `TickSystem` | Deterministische Tick-Verwaltung |

## Konfiguration

### LocomotionConfig Parameter

#### Ground Movement
- `WalkSpeed` (float): Normale Geschwindigkeit (m/s)
- `RunSpeed` (float): Sprint-Geschwindigkeit (m/s)
- `Acceleration` (float): Beschleunigung
- `Deceleration` (float): Verzögerung

#### Air Movement
- `AirControl` (0-1): Kontrolle in der Luft
- `Gravity` (float): Schwerkraft (m/s²)
- `MaxFallSpeed` (float): Maximale Fallgeschwindigkeit

#### Jumping
- `JumpHeight` (float): Sprunghöhe (m)
- `CoyoteTime` (float): Sprungmöglichkeit nach Ground-Verlust
- `JumpBufferTime` (float): Input-Buffer vor Landung

#### Ground Detection
- `GroundCheckDistance` (float): Check-Reichweite
- `GroundCheckRadius` (float): SphereCast-Radius
- `GroundLayers` (LayerMask): Boden-Layer
- `MaxSlopeAngle` (float): Max. begehbarer Winkel

#### Slope Sliding
- `SlopeSlideSpeed` (float): Rutsch-Geschwindigkeit
- `UseSlopeDependentSlideSpeed` (bool): Dynamische Geschwindigkeit je nach Steilheit

## Architektur

```
PlayerController (MonoBehaviour)
├── TickSystem (60 Hz Fixed Updates)
├── IMovementInputProvider (Input Abstraktion)
├── GroundingDetection (Ground Checks)
├── CharacterLocomotion (High-Level Locomotion)
│   └── KinematicMotor (Low-Level Physics)
└── CharacterStateMachine
    ├── GroundedState
    ├── JumpingState
    └── FallingState
```

### Ordnerstruktur

```
Core/
├── Motor/                  # Physik-Schicht (für alle Locomotion-Typen)
│   ├── KinematicMotor.cs   # Kinematische Kollisionserkennung
│   └── GroundingDetection.cs
├── Locomotion/             # Verhaltens-Schicht
│   ├── ILocomotionController.cs
│   ├── CharacterLocomotion.cs  # Walking, Running, Jumping
│   └── LocomotionConfig.cs
├── StateMachine/           # State Machine System
│   ├── ICharacterState.cs
│   ├── CharacterStateMachine.cs
│   └── States/
│       ├── BaseCharacterState.cs
│       ├── GroundedState.cs
│       ├── AirborneState.cs
│       ├── JumpingState.cs
│       └── FallingState.cs
├── Input/                  # Input Abstraktion
│   ├── IMovementInputProvider.cs
│   ├── PlayerInputProvider.cs
│   └── AIInputProvider.cs
├── Prediction/             # CSP Vorbereitung
│   ├── IPredictionSystem.cs
│   ├── ControllerInput.cs
│   ├── InputBuffer.cs
│   └── PredictionBuffer.cs
├── TickSystem.cs           # Fixed Tick System
└── PlayerController.cs     # Haupt-Controller
```

### Architektur-Konzept

Das System ist in zwei Schichten aufgeteilt:

**Motor-Schicht** (`Core/Motor/`):
- Gemeinsame Physik-Implementierung
- Wird von allen Locomotion-Typen verwendet
- `KinematicMotor`: CapsuleCast, Depenetration, Step-Up
- `GroundingDetection`: Boden-Erkennung, Slope-Analyse

**Locomotion-Schicht** (`Core/Locomotion/`):
- High-Level Bewegungsverhalten
- `CharacterLocomotion`: Gehen, Rennen, Springen, Slope Sliding
- Zukünftig: `RidingLocomotion`, `GlidingLocomotion`, etc.

## Erweiterung

### Custom States

```csharp
public class DashState : BaseCharacterState
{
    public override string StateName => "Dash";

    protected override void OnEnter(IStateMachineContext context)
    {
        // Dash starten
    }

    public override ICharacterState EvaluateTransitions(IStateMachineContext context)
    {
        // Transition-Logik
        return null;
    }
}
```

### Custom Input Provider

```csharp
public class NetworkInputProvider : MonoBehaviour, IMovementInputProvider
{
    public Vector2 MoveInput { get; private set; }
    public bool JumpPressed { get; private set; }
    // ... weitere Properties

    public void UpdateInput()
    {
        // Input von Server empfangen
    }
}
```

### Custom Locomotion (zukünftig)

```csharp
public class GlidingLocomotion : ILocomotionController
{
    private readonly KinematicMotor _motor;

    // Verwendet denselben KinematicMotor wie CharacterLocomotion
    public GlidingLocomotion(Transform transform, CapsuleCollider capsule, ILocomotionConfig config)
    {
        _motor = new KinematicMotor(transform, capsule, config);
    }

    public void Simulate(LocomotionInput input, float deltaTime)
    {
        // Gliding-spezifische Logik
    }
}
```

## Abhängigkeiten

- Unity 2022.3 LTS oder höher
- Unity Input System (optional)
- TextMeshPro
- Cinemachine (optional)

## Lizenz

Teil des Wiesenwischer GameKit Frameworks.

## Roadmap

Zukünftige Packages (nicht Teil des Core):

- **Animation Package**: Animator Integration
- **Camera Package**: Cinemachine Setup
- **Network Package**: FishNet Integration, CSP Implementation
- **IK Package**: Animation Rigging
- **Abilities Package**: Skills, Combat

Zukünftige Locomotion-Typen:
- **RidingLocomotion**: Reiten (Pferde, Mounts)
- **GlidingLocomotion**: Gleiten
- **SwimmingLocomotion**: Schwimmen
- **ClimbingLocomotion**: Klettern

## Support

Bei Fragen oder Problemen: [GitHub Issues](https://github.com/Wiesenwischer/wiesenwischer-gamekit-charactercontroller/issues)
