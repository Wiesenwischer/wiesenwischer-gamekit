# Umsetzungsplan: Wiesenwischer GameKit Character Controller Core-Paket

## Übersicht

Entwicklung eines modularen, MMO-fähigen Character Controller Core-Pakets für Unity 2022.3 LTS mit State Machine, deterministischer Movement-Logik und Vorbereitung für Client-Side Prediction.

## 1. Unity-Projektstruktur aufsetzen

### 1.1 Verzeichnisstruktur erstellen

```
Wiesenwischer.GameKit.CharacterController/
├── Assets/
│   └── __ProjectSettings/
├── Packages/
│   ├── manifest.json
│   └── Wiesenwischer.GameKit.CharacterController.Core/
│       ├── Runtime/
│       │   ├── Core/
│       │   │   ├── StateMachine/
│       │   │   ├── Movement/
│       │   │   ├── Input/
│       │   │   ├── Prediction/
│       │   │   └── PlayerController.cs
│       │   └── Wiesenwischer.GameKit.CharacterController.Core.Runtime.asmdef
│       ├── Editor/
│       │   └── Wiesenwischer.GameKit.CharacterController.Core.Editor.asmdef
│       ├── Tests/
│       │   ├── Runtime/
│       │   └── Editor/
│       ├── Samples~/
│       │   └── BasicMovement/
│       ├── package.json
│       ├── README.md
│       └── CHANGELOG.md
└── ProjectSettings/
```

### 1.2 Unity-Version und Dependencies

- **Unity Version:** 2022.3 LTS
- **Package Dependencies:**
  - `com.unity.inputsystem: 1.7.0`
  - `com.unity.textmeshpro: 3.0.6`
  - `com.unity.cinemachine: 2.9.7`
  - `com.unity.test-framework: 1.1.33`

## 2. Kern-Komponenten (Implementierungsreihenfolge)

### Phase 1: Grundlagen (MVP)

#### 2.1 Interfaces definieren
- `ICharacterState` - Interface für State Machine States
- `IMovementController` - Interface für Movement-Logik
- `IMovementInputProvider` - Interface für Input-Abstraktion
- `IPredictionSystem` - Interface für CSP-Vorbereitung

#### 2.2 MovementConfig ScriptableObject
- Movement-Parameter (Geschwindigkeiten, Acceleration, etc.)
- Jump-Konfiguration
- Ground Detection Settings
- Gravity-Einstellungen

#### 2.3 Input System
- `PlayerInputProvider` mit Unity Input System (optional via Conditional Compilation)
- Fallback-Input mit Legacy Input System
- `AIInputProvider` für Tests

#### 2.4 Ground Detection
- `GroundingDetection` Klasse
- Raycast/SphereCast-basierte Detection
- Slope-Handling
- Layer-basierte Filterung

#### 2.5 Movement Motor
- `MovementMotor` Klasse
- Deterministische Velocity-Berechnungen
- Gravity Application
- Walk/Run Movement
- **Wichtig:** Kein `Time.deltaTime`, sondern Fixed Tick Delta

### Phase 2: State Machine

#### 2.6 State Machine Framework
- `CharacterStateMachine` Klasse
- State Transition Logik
- State-History für Debugging

#### 2.7 Concrete States
- `GroundedState`
- `AirborneState`
- `JumpingState`
- `FallingState`
- State Transition Conditions

### Phase 3: Integration

#### 2.8 PlayerController MonoBehaviour
- Integration aller Subsysteme
- State Machine Koordination
- Input → Movement Pipeline
- Fixed Tick System (60Hz)

#### 2.9 Jump Mechanik
- Jump-Input Handling
- Jump-Velocity Berechnung
- Jump-State Integration

### Phase 4: CSP-Vorbereitung

#### 2.10 Prediction Structures
- `ControllerInput` Struct (serialisierbar)
- `InputBuffer<T>` Klasse
- `PredictionBuffer` Klasse
- Tick-basiertes System

**Hinweis:** Netzwerk-Integration (Server Reconciliation, Rollback) ist NICHT Teil des Core-Packages und wird später im separaten Network-Package implementiert.

### Phase 5: Testing & Demo

#### 2.11 Unit Tests
- Movement Motor Tests
- Ground Detection Tests
- State Machine Tests
- State Transition Tests

#### 2.12 Demo-Szene
- `MovementTest.unity` Szene
- Test-Environment (Ground, Obstacles, Slopes)
- Lighting Setup

#### 2.13 Prefabs
- `BasicPlayer.prefab` mit allen Components
- `TestGround.prefab`
- `TestSlope.prefab`

#### 2.14 Dokumentation
- Package README
- Code-Kommentare (XML Docs)
- CHANGELOG
- Sample-Dokumentation

## 3. Architektur-Entscheidungen

### State Machine
- **Hierarchische State Machine** für klare Zustandsverwaltung
- Interface-basiert für Erweiterbarkeit
- States können über Netzwerk synchronisiert werden (später)

### Input System
- **Interface-basiert** für Testbarkeit und Flexibilität
- Conditional Compilation für optionales Unity Input System
- Vorbereitet für NetworkInputProvider (später)

### Movement
- **Deterministisch** für CSP-Kompatibilität
- Manueller CharacterController statt Unity Physics
- Fixed Tick Rate (60 Hz)
- Keine Verwendung von `Time.deltaTime` oder Randomness

### CSP-Vorbereitung
- Strukturen vorhanden, aber ohne Netzwerk-Code
- Lokaler Modus funktioniert standalone
- Input- und Prediction-Buffer implementiert
- Spätere Integration via Dependency Injection

### Konfiguration
- **ScriptableObject-basiert** für Designer-Freundlichkeit
- Wiederverwendbar für verschiedene Character-Typen
- Runtime-Switching möglich

## 4. Commit-Strategie

### Trunk-Based Development
- **Hauptbranch:** `main`
- **Feature-Branches:** `feature/<feature-name>`
- **Commit-Prinzip:** Häufig, klein, atomar

### Commit-Reihenfolge (20 Commits)

1. **feat: Initialisiere Unity-Projekt und Package-Struktur** - Unity-Projekt, Ordner-Hierarchie
2. **feat: Definiere Core-Package Metadata und Dependencies** - package.json, .asmdef-Dateien
3. **feat: Definiere Core-Interfaces** - Alle Interface-Definitionen
4. **feat: Implementiere MovementConfig ScriptableObject** - Konfiguration
5. **feat: Implementiere Input Provider System** - IMovementInputProvider, PlayerInputProvider
6. **feat: Implementiere Ground Detection System** - GroundingDetection
7. **feat: Implementiere Movement Motor** - Deterministische Movement-Logik
8. **feat: Implementiere State Machine Framework** - CharacterStateMachine Basis
9. **feat: Implementiere Character States** - Grounded, Airborne, Jumping, Falling
10. **feat: Implementiere PlayerController MonoBehaviour** - Integration aller Systeme
11. **feat: Implementiere CSP Strukturen** - InputBuffer, PredictionBuffer, ControllerInput
12. **feat: Implementiere Fixed Tick System** - Tick-Akkumulator, FixedTick-Methode
13. **feat: Implementiere Jump Mechanik** - Jump-Input, Velocity, State-Integration
14. **feat: Implementiere Editor Tools** - Custom Inspectors, Gizmos
15. **test: Füge Unit Tests für Movement System hinzu** - Movement Tests
16. **test: Füge Unit Tests für State Machine hinzu** - State Tests
17. **feat: Erstelle Demo-Szene** - MovementTest.unity, Environment
18. **feat: Erstelle Core Prefabs** - BasicPlayer, TestGround, TestSlope
19. **docs: Dokumentiere Core Package** - README, Code-Kommentare, CHANGELOG
20. **feat: Strukturiere Samples** - Samples~ Ordner, BasicMovement Sample

## 5. Kritische Dateien

Die 5 wichtigsten Dateien für die Implementierung:

1. **PlayerController.cs** - Zentrale Koordination aller Systeme
2. **CharacterStateMachine.cs** - State Management und Transitions
3. **MovementMotor.cs** - Deterministisches Movement, CSP-Kern
4. **IMovementInputProvider.cs** - Input-Abstraktion, Testbarkeit
5. **MovementConfig.cs** - ScriptableObject, Designer-Interface

## 6. Verifikation

### Test-Checkliste für Demo-Szene

**Basis-Bewegung:**
- [ ] WASD bewegt Character
- [ ] Maus dreht Character
- [ ] Geschwindigkeit entspricht Config (Walk/Run)

**Springen:**
- [ ] Spacebar löst Jump aus
- [ ] Jump-Höhe entspricht Config
- [ ] Kann nicht doppelt springen

**Gravity:**
- [ ] Character fällt ohne Ground
- [ ] Fällt mit korrekter Geschwindigkeit

**Ground Detection:**
- [ ] Erkennt Ground zuverlässig
- [ ] Funktioniert auf Slopes
- [ ] Funktioniert auf Treppen

**States:**
- [ ] Grounded State bei Boden-Kontakt
- [ ] Airborne State in der Luft
- [ ] State-Transitions sauber

**CSP-Vorbereitung:**
- [ ] Input wird gebuffert
- [ ] Position-History wird gespeichert
- [ ] Fixed Tick Rate läuft stabil

### Unit Tests

- [ ] MovementMotor Tests bestehen
- [ ] Ground Detection Tests bestehen
- [ ] State Machine Tests bestehen
- [ ] State Transition Tests bestehen

## 7. Was ist NICHT im Scope

- Animation-System (separates Package)
- Camera-System (separates Package)
- Combat-System
- Skills-System
- Riding/Mount-System
- Netzwerk-Integration (separates Network-Package)
- IK-System (separates Package)

## 8. Nächste Schritte nach Core-Package

1. Animation-Package (Animator Integration)
2. Camera-Package (Cinemachine Setup)
3. Network-Package (FishNet Integration, CSP Implementation)
4. IK-Package (Animation Rigging)
5. Abilities-Package (Skills, Combat)
