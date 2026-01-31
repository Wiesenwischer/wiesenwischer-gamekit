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
- [x] MovementConfig.cs erstellen
- [x] Movement-Parameter (Walk/Run Speed, Acceleration)
- [x] Jump-Konfiguration (Height, Duration, CoyoteTime, JumpBuffer)
- [x] Ground Detection Settings (Distance, Radius, Layers, MaxSlope)
- [x] Gravity-Einstellungen (Gravity, MaxFallSpeed)
- [x] CreateAssetMenu Attribute
- [x] IMovementConfig Interface erweitert

### Commit 5: Input Provider System
- [x] IMovementInputProvider Interface (bereits in Commit 3)
- [x] PlayerInputProvider.cs mit Unity Input System
- [x] Conditional Compilation (#if UNITY_INPUT_SYSTEM_AVAILABLE)
- [x] Fallback Input mit Legacy Input
- [x] AIInputProvider.cs für Tests/KI

### Commit 6: Ground Detection System
- [x] GroundingDetection.cs Klasse
- [x] Raycast-basierte Ground Check (Multi-Raycast)
- [x] SphereCast für stabilere Detection
- [x] Slope-Handling mit Max-Angle und IsWalkable
- [x] Layer-Mask Filterung
- [x] Step Detection für Treppen
- [x] Debug Gizmos

### Commit 7: Movement Simulator
- [x] MovementSimulator.cs Klasse
- [x] Velocity-Berechnungen (Walk/Run)
- [x] Acceleration/Deceleration
- [x] Gravity Application
- [x] Deterministische Physik (Fixed Delta)
- [x] CharacterController Integration

---

## Phase 3: State Machine

### Commit 8: State Machine Framework
- [x] CharacterStateMachine.cs
- [x] State Registration System
- [x] State Transition Logik
- [x] Current State Tracking
- [x] State-History für Debugging

### Commit 9: Character States
- [x] GroundedState.cs Implementation
- [x] AirborneState.cs Implementation
- [x] JumpingState.cs Implementation
- [x] FallingState.cs Implementation
- [x] State Transition Conditions

---

## Phase 4: Integration

### Commit 10: PlayerController MonoBehaviour
- [x] PlayerController.cs Haupt-Klasse
- [x] Subsystem-Integration (Input, Movement, StateMachine)
- [x] Component-Referenzen (CharacterController)
- [x] Config-Referenz (MovementConfig)
- [x] Update-Loop Struktur

### Commit 11: CSP Strukturen
- [x] ControllerInput.cs Struct (serialisierbar)
- [x] InputBuffer.cs generische Klasse
- [x] PredictionBuffer.cs Klasse
- [x] Tick-Index System
- [x] History-Speicherung

### Commit 12: Fixed Tick System
- [x] Tick-Rate Konfiguration (60Hz)
- [x] Tick-Akkumulator in PlayerController
- [x] FixedTick-Methode
- [x] Tick-Counter
- [x] Delta-Time Berechnung (1/tickRate)

### Commit 13: Jump Mechanik
- [x] Jump-Input Detection
- [x] Jump-Velocity Berechnung
- [x] Jump-State Activation
- [x] Jump-State → Airborne Transition
- [x] Airborne → Grounded Transition (Landing)

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
