# Changelog

Alle wichtigen Änderungen an diesem Projekt werden in dieser Datei dokumentiert.

Das Format basiert auf [Keep a Changelog](https://keepachangelog.com/de/1.0.0/),
und dieses Projekt folgt [Semantic Versioning](https://semver.org/lang/de/).

## [0.1.0] - 2024

### Hinzugefügt

#### Core System
- `PlayerController` als zentrale MonoBehaviour-Komponente
- `TickSystem` für deterministische 60 Hz Fixed Updates
- `MovementConfig` ScriptableObject für alle Movement-Parameter

#### State Machine
- `CharacterStateMachine` Framework mit State Registration und Transitions
- `StateHistory` für Debugging und Replay
- States: `GroundedState`, `JumpingState`, `FallingState`
- `BaseCharacterState` und `AirborneState` als Basisklassen
- State Transition Reasons für Debugging und Netzwerk

#### Movement System
- `MovementMotor` mit deterministischer Velocity-Berechnung
- `GroundingDetection` mit SphereCast und Multi-Raycast
- Slope Handling mit konfigurierbarem Max-Angle
- Step Detection für automatisches Treppen-Steigen
- Walk/Run mit konfigurierbarer Acceleration/Deceleration

#### Input System
- `IMovementInputProvider` Interface
- `PlayerInputProvider` mit Unity Input System Support (Conditional Compilation)
- Legacy Input Fallback
- `AIInputProvider` für KI-gesteuerte Characters und Tests

#### CSP Vorbereitung
- `ControllerInput` Struct (serialisierbar)
- `InputBuffer<T>` generischer Ring-Buffer
- `PredictionBuffer` für State History
- `IPredictionSystem` und `IPredictable` Interfaces

#### Jump Mechanics
- Variable Jump Height (früh loslassen = weniger Höhe)
- Coyote Time (Sprung nach Ground-Verlust)
- Jump Buffer (Sprung vor Landung)
- Physikalisch korrekte Jump Velocity Berechnung

#### Editor Tools
- `PlayerControllerEditor` Custom Inspector mit Debug Info
- `MovementConfigEditor` mit gruppierten Parametern
- Debug Gizmos für Ground Check und Movement Direction
- `DemoSceneSetup` Wizard für automatisches Scene/Prefab Setup

#### Tests
- Unit Tests für MovementMotor
- Unit Tests für GroundInfo
- Unit Tests für CharacterStateMachine
- Unit Tests für State Transitions

#### Dokumentation
- Package README mit Quick Start Guide
- API Übersicht und Konfigurationsreferenz
- Samples~/BasicMovement mit Anleitung
- XML-Dokumentation für alle Public APIs

### Bekannte Einschränkungen

- Netzwerk-Integration ist vorbereitet aber nicht implementiert
- Animation-System nicht enthalten (separates Package geplant)
- Camera-System nicht enthalten (separates Package geplant)

## [Unreleased]

### Geplant

- Animation Package Integration
- Camera Package mit Cinemachine
- Network Package mit FishNet und vollständiger CSP Implementation
- IK Package für Animation Rigging
- Abilities Package für Skills und Combat
