# Wiesenwischer GameKit - Character Controller Core

Ein modulares, MMO-fähiges Character Controller System für Unity 2022.3 LTS.

## Features

- **State Machine basiertes Movement**: Grounded, Jumping, Falling States mit konfigurierbaren Transitions
- **Deterministische Physik**: Fixed Tick System (60 Hz) für Client-Side Prediction
- **Flexible Input Abstraktion**: Support für Unity Input System und Legacy Input
- **Umfangreiche Ground Detection**: SphereCast + Multi-Raycast, Slope Handling, Step Detection
- **CSP-Ready**: InputBuffer, PredictionBuffer, Tick-basierte Strukturen
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

### 1. MovementConfig erstellen

```
Rechtsklick im Project Window
→ Create > Wiesenwischer > GameKit > Movement Config
```

Oder via Menü: **Wiesenwischer > GameKit > Create Default MovementConfig**

### 2. Player Setup

1. Erstelle ein leeres GameObject
2. Füge `CharacterController` hinzu (Unity Built-in)
3. Füge `PlayerController` hinzu
4. Füge `PlayerInputProvider` hinzu
5. Weise die MovementConfig zu

Oder via Menü: **Wiesenwischer > GameKit > Create Demo Scene**

### 3. Fertig!

WASD zum Bewegen, Leertaste zum Springen, Shift zum Sprinten.

## API Übersicht

### Haupt-Komponenten

| Klasse | Beschreibung |
|--------|--------------|
| `PlayerController` | Hauptkomponente, integriert alle Systeme |
| `MovementConfig` | ScriptableObject mit allen Parametern |
| `CharacterStateMachine` | State Machine für Character States |
| `MovementMotor` | Deterministische Movement-Berechnung |
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

### MovementConfig Parameter

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

## Architektur

```
PlayerController (MonoBehaviour)
├── TickSystem (60 Hz Fixed Updates)
├── IMovementInputProvider (Input Abstraktion)
├── GroundingDetection (Ground Checks)
├── MovementMotor (Velocity Calculation)
└── CharacterStateMachine
    ├── GroundedState
    ├── JumpingState
    └── FallingState
```

### Ordnerstruktur

```
Core/
├── StateMachine/           # State Machine System
│   ├── ICharacterState.cs
│   ├── CharacterStateMachine.cs
│   └── States/
│       ├── BaseCharacterState.cs
│       ├── GroundedState.cs
│       ├── AirborneState.cs
│       ├── JumpingState.cs
│       └── FallingState.cs
├── Movement/               # Movement System
│   ├── IMovementController.cs
│   ├── MovementMotor.cs
│   ├── GroundingDetection.cs
│   └── MovementConfig.cs
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

## Support

Bei Fragen oder Problemen: [GitHub Issues](https://github.com/Wiesenwischer/wiesenwischer-gamekit-charactercontroller/issues)
