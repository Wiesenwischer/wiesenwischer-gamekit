# TODO: Character Controller Core Implementation

Implementierungs-Checkliste basierend auf [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md)

## Status Legende
- [ ] Offen
- [x] Abgeschlossen
- [~] In Arbeit

---

## Phase 1: Projekt-Setup & Grundlagen

### Commit 1: Unity-Projekt und Package-Struktur
- [x] Unity 2022.3 LTS Projekt erstellen
- [x] Assets-Ordner mit Unterordnern
- [x] Packages-Ordner erstellen
- [x] Core-Package-Struktur (Runtime/Editor/Tests/Samples~)
- [x] ProjectSettings initialisieren

### Commit 2: Package Metadata und Dependencies
- [x] package.json für Core-Package
- [x] Runtime Assembly Definition (.asmdef)
- [x] Editor Assembly Definition (.asmdef)
- [x] Test Assembly Definitions (Runtime/Editor)
- [x] manifest.json mit Unity Package Dependencies (Cinemachine hinzugefügt)

### Commit 3: Core-Interfaces
- [x] ICharacterState.cs - State Machine Interface
- [x] IMovementController.cs - Movement Interface
- [x] IMovementInputProvider.cs - Input Interface
- [x] IPredictionSystem.cs - CSP Interface

---

## Phase 2: Movement System

### Commit 4: MovementConfig ScriptableObject
- [ ] MovementConfig.cs erstellen
- [ ] Movement-Parameter (Walk/Run Speed, Acceleration)
- [ ] Jump-Konfiguration (Height, Duration)
- [ ] Ground Detection Settings
- [ ] Gravity-Einstellungen
- [ ] CreateAssetMenu Attribute

### Commit 5: Input Provider System
- [ ] IMovementInputProvider Interface (falls nicht in Commit 3)
- [ ] PlayerInputProvider.cs mit Unity Input System
- [ ] Conditional Compilation (#if UNITY_INPUT_SYSTEM_AVAILABLE)
- [ ] Fallback Input mit Legacy Input
- [ ] AIInputProvider.cs für Tests

### Commit 6: Ground Detection System
- [ ] GroundingDetection.cs Klasse
- [ ] Raycast-basierte Ground Check
- [ ] SphereCast für stabilere Detection
- [ ] Slope-Handling mit Max-Angle
- [ ] Layer-Mask Filterung

### Commit 7: Movement Simulator
- [ ] MovementSimulator.cs Klasse
- [ ] Velocity-Berechnungen (Walk/Run)
- [ ] Acceleration/Deceleration
- [ ] Gravity Application
- [ ] Deterministische Physik (Fixed Delta)
- [ ] CharacterController Integration

---

## Phase 3: State Machine

### Commit 8: State Machine Framework
- [ ] CharacterStateMachine.cs
- [ ] State Registration System
- [ ] State Transition Logik
- [ ] Current State Tracking
- [ ] State-History für Debugging

### Commit 9: Character States
- [ ] GroundedState.cs Implementation
- [ ] AirborneState.cs Implementation
- [ ] JumpingState.cs Implementation
- [ ] FallingState.cs Implementation
- [ ] State Transition Conditions

---

## Phase 4: Integration

### Commit 10: PlayerController MonoBehaviour
- [ ] PlayerController.cs Haupt-Klasse
- [ ] Subsystem-Integration (Input, Movement, StateMachine)
- [ ] Component-Referenzen (CharacterController)
- [ ] Config-Referenz (MovementConfig)
- [ ] Update-Loop Struktur

### Commit 11: CSP Strukturen
- [ ] ControllerInput.cs Struct (serialisierbar)
- [ ] InputBuffer.cs generische Klasse
- [ ] PredictionBuffer.cs Klasse
- [ ] Tick-Index System
- [ ] History-Speicherung

### Commit 12: Fixed Tick System
- [ ] Tick-Rate Konfiguration (60Hz)
- [ ] Tick-Akkumulator in PlayerController
- [ ] FixedTick-Methode
- [ ] Tick-Counter
- [ ] Delta-Time Berechnung (1/tickRate)

### Commit 13: Jump Mechanik
- [ ] Jump-Input Detection
- [ ] Jump-Velocity Berechnung
- [ ] Jump-State Activation
- [ ] Jump-State → Airborne Transition
- [ ] Airborne → Grounded Transition (Landing)

---

## Phase 5: Editor & Testing

### Commit 14: Editor Tools
- [ ] PlayerController Custom Inspector
- [ ] Debug-Info Anzeige (Current State, Velocity)
- [ ] MovementConfig Custom Editor (optional)
- [ ] Gizmos für Ground Check Raycast
- [ ] Gizmos für Movement Direction

### Commit 15: Unit Tests - Movement System
- [ ] MovementSimulator Tests (Velocity Calculations)
- [ ] Ground Detection Tests (Raycast, Slope)
- [ ] Acceleration/Deceleration Tests
- [ ] Gravity Tests

### Commit 16: Unit Tests - State Machine
- [ ] State Transition Tests
- [ ] State Enter/Exit Tests
- [ ] State Machine Tests
- [ ] Edge Case Tests

---

## Phase 6: Demo & Samples

### Commit 17: Demo-Szene
- [ ] MovementTest.unity Szene erstellen
- [ ] Ground Plane (100x100)
- [ ] Obstacles (Cubes, Slopes, Stairs)
- [ ] Lighting Setup (Directional Light, Skybox)
- [ ] Optional: Debug UI Canvas

### Commit 18: Core Prefabs
- [ ] BasicPlayer.prefab
  - [ ] PlayerController Component
  - [ ] CharacterController Component
  - [ ] Visual (Capsule Mesh)
  - [ ] GroundCheck Transform
- [ ] TestGround.prefab (100x100 Plane)
- [ ] TestSlope.prefab (verschiedene Winkel)
- [ ] TestStairs.prefab

---

## Phase 7: Dokumentation

### Commit 19: Package Dokumentation
- [ ] Package README.md
  - [ ] Feature-Übersicht
  - [ ] Installation-Guide
  - [ ] Quick Start
  - [ ] API-Übersicht
- [ ] CHANGELOG.md (Version 0.1.0)
- [ ] XML-Dokumentations-Kommentare in allen Public APIs
- [ ] Inline-Code-Kommentare für komplexe Logik

### Commit 20: Samples Strukturierung
- [ ] Samples~/BasicMovement Ordner
- [ ] Sample-Szene in Samples~
- [ ] Sample-README.md
- [ ] Sample-Integration in package.json

---

## Verifikation

### Funktionale Tests in Demo-Szene
- [ ] WASD bewegt Character
- [ ] Maus dreht Character
- [ ] Geschwindigkeit korrekt (Walk/Run)
- [ ] Spacebar löst Jump aus
- [ ] Jump-Höhe entspricht Config
- [ ] Kann nicht doppelt springen
- [ ] Character fällt ohne Ground
- [ ] Ground Detection zuverlässig
- [ ] Funktioniert auf Slopes
- [ ] Funktioniert auf Treppen
- [ ] States wechseln korrekt
- [ ] Input wird gebuffert
- [ ] Position-History wird gespeichert
- [ ] Fixed Tick Rate stabil

### Unit Tests
- [ ] Alle Movement Tests bestehen
- [ ] Alle Ground Detection Tests bestehen
- [ ] Alle State Machine Tests bestehen
- [ ] Alle State Transition Tests bestehen

---

## Nächste Features (außerhalb Scope)

- [ ] Animation-Package
- [ ] Camera-Package
- [ ] Network-Package
- [ ] IK-Package
- [ ] Abilities-Package
