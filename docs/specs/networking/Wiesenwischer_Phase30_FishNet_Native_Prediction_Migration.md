# Phase 30: FishNet Native Prediction Migration

## Context

Phase 6+7 implementierte ein **custom CSP-System** (TickSystem, InputBuffer, PredictionBuffer, NetworkInputSync, NetworkStateSync). Dieses System hat fundamentale Probleme:

1. **Keine Tick-Synchronisation** — TickSystem startet bei Tick 0 auf jedem Client unabhaengig
2. **CameraYaw wird nicht serialisiert** — Server berechnet Bewegung immer mit Yaw=0
3. **PredictionBuffer.TryGet() schlaegt staendig fehl** — Hard-Correction (Teleport) jeden Frame
4. **Host-Input-Verlust** nach Fokuswechsel

**FishNet hat bereits ein komplettes, produktionsreifes CSP-System** mit `[Replicate]`/`[Reconcile]`, `TimeManager` (synchronisierte Tick-Timeline), und automatischem Replay. Beide AI-Analysen (Claude + ChatGPT) empfehlen uebereinstimmend: **Migration auf FishNet Native Prediction**.

**Kein Rewrite** — die Bewegungslogik (CharacterLocomotion, Motor, StateMachine) bleibt. Nur die Orchestrierungsschicht wird umgebaut.

---

## Architektur-Schluesselentscheidungen

### 1. Simulation Driver Pattern
- `ISimulationDriver` Interface in Core (FishNet-frei)
- Online: `NetworkCharacterDriver` (FishNet `TickNetworkBehaviour`) treibt Simulation via `TimeManager.OnTick`
- Offline: `PlayerController.FixedUpdate()` treibt Simulation direkt
- Core weiss nicht ob offline/online

### 2. Motor-Integration (KRITISCH)
`CharacterLocomotion.Simulate(input, delta)` setzt nur `_currentInput`. Die echte Physik laeuft
in KCC-Callbacks (`UpdateVelocity`/`UpdateRotation`) die vom `CharacterMotorSystem.FixedUpdate()`
getriggert werden — das ist lokale Unity-Zeit, NICHT netzwerk-synchronisiert!

**Loesung:** `CharacterMotorSystem` hat:
- `Settings.AutoSimulation` Flag → auf `false` setzen im Network-Modus
- `public static Simulate(float deltaTime, List<CharacterMotor> motors)` → extern aufrufbar

**Ablauf im Network-Modus:**
```
[Replicate] PerformReplicate():
  1. Locomotion.Simulate(input, delta)                    → setzt _currentInput
  2. CharacterMotorSystem.Simulate(delta, [motor])        → treibt UpdatePhase1 + UpdatePhase2
     → Motor ruft ICharacterController.UpdateVelocity()   → Locomotion berechnet Velocity
     → Motor ruft ICharacterController.UpdateRotation()   → Locomotion berechnet Rotation
     → Motor setzt TransientPosition/TransientRotation    → Physik-Ergebnis
```

**Offline-Modus:** `AutoSimulation = true` (Default) → Motor laeuft wie bisher in FixedUpdate.

### 3. Visual Separation (bereits vorhanden — ABER Interpolation muss verdrahtet werden!)
Der Motor hat **eingebaute Interpolation** (`CharacterMotorSystem`):
- `PreSimulationInterpolationUpdate()`: Speichert `InitialTickPosition`
- `PostSimulationInterpolationUpdate()`: Setzt Transform zurueck auf Initial + speichert Interpolations-Timestamps
- `CustomInterpolationUpdate()` (LateUpdate): Lerp zwischen Initial und Transient

Das IST bereits "Simulation vs Rendering Separation" — `TransientPosition` = Simulation,
`Transform.position` = interpoliertes Rendering.

**KRITISCH:** Wenn `AutoSimulation = false`, laeuft `FixedUpdate()` nicht → Pre/Post-Interpolation
werden NICHT aufgerufen! `LateUpdate` → `CustomInterpolationUpdate()` rechnet dann mit veralteten
Timestamps → **Visual Interpolation kaputt im Network-Modus!**

**Loesung:** `NetworkCharacterDriver` ruft Pre/Post-Interpolation manuell auf:
```
OnTick():
  PreSimulationInterpolationUpdate(tickDelta)    // NUR beim echten Tick!
  [Replicate] → SimulateTick + Motor.Simulate
  PostSimulationInterpolationUpdate(tickDelta)   // NUR beim echten Tick!

Waehrend Replay:
  [Replicate] nur Simulate → KEINE Pre/Post-Interpolation (nur Physik-State berechnen)
```
FishNet `ReplicateState` bietet `IsReplayed()` Flag zum Unterscheiden.

Kamera folgt `Transform.position` (= interpolierter Visual) ueber CameraAnchor.FollowTarget → korrekt.

### 4. Offline-Kompatibilitaet
`PlayerController` bekommt `FixedUpdate()` das `SimulateTick()` aufruft wenn kein `ISimulationDriver` vorhanden. Ersetzt den custom `TickSystem` Accumulator.

### 5. Deterministisches Timing (KRITISCH — Audit-Ergebnis)

**Problem:** Simulation-Code liest aktuell `Time.deltaTime` und `Time.time` direkt.
Bei FishNet Reconcile-Replay wird `[Replicate]` mehrfach hintereinander aufgerufen —
`Time.deltaTime`/`Time.time` sind waehrend Replay NICHT korrekt (sie zeigen Frame-Zeit, nicht Tick-Zeit).

**Audit-Ergebnis — SIMULATION-KRITISCHE Stellen:**

| Datei | Zeile | Code | Problem |
|-------|-------|------|---------|
| `PlayerMovementState.cs` | 66 | `stateTime += Time.deltaTime` | **BASE CLASS** — betrifft ALLE States! |
| `PlayerSlidingState.cs` | 22 | `_slideStartTime = Time.time` | Absoluter Timestamp, bricht bei Replay |
| `PlayerSlidingState.cs` | 64 | `Time.time - _slideStartTime` | Slide-Dauer-Berechnung |
| `PlayerSlidingState.cs` | 125 | `Config.RotationSpeed * Time.deltaTime` | Frame-Zeit statt Tick-Zeit |
| `PlayerGroundedState.cs` | 106 | `ReusableData.TimeSinceGrounded += Time.deltaTime` | Frame-Zeit statt Tick-Zeit |
| `PlayerMovingState.cs` | 38 | `_noInputTimer += Time.deltaTime` | Input-Grace-Period |
| `PlayerHardLandingState.cs` | 81 | `_fallbackTimer -= Time.deltaTime` | Landing-Recovery-Timer |
| `CharacterLocomotion.cs` | 491 | `float now = Time.time` | Treppen-Erkennung |
| `CharacterLocomotion.cs` | 549 | `Time.time - _lastStepTime` | Treppen-Window |
| `PlayerController.cs` | 155 | `AbilitySystem?.Tick(Time.deltaTime)` | Ability-Cooldowns |

**RENDERING-ONLY (kein Fix noetig):**
- Kamera (ThirdPersonCamera, CameraBrain) — nur visuelle Glaettung in LateUpdate
- Animation (AnimatorParameterBridge) — Blend-Smoothing in LateUpdate
- IK (LookAtIK, FootLock) — rein visuell
- Debug-Tools (CameraPresetSwitcher)

**Loesung: deltaTime als Parameter durchreichen**

```
SimulateTick(float deltaTime)
  → StateMachine.Update(deltaTime)
    → State.Update(deltaTime)          // statt Time.deltaTime
      → stateTime += deltaTime
      → OnUpdate(deltaTime)            // Subklassen bekommen deltaTime
  → AbilitySystem.Tick(deltaTime)      // bereits parametrisiert
  → Motor.Simulate(deltaTime)          // bereits parametrisiert
```

Fuer `Time.time`-basierte Stellen (Sliding, Treppen):
- Ersatz durch `stateTime` (bereits akkumuliert) oder `_accumulatedTime` in Locomotion
- Keine absoluten Timestamps, nur relative Dauer

---

## Schritte

### 30.1: ISimulationDriver + SimulateTick Extraktion

**Ziel:** Driver-Abstraktion in Core + `SimulateTick()` als aufrufbare Methode

**Erstellen:**
- `Packages/Wiesenwischer.GameKit.CharacterController.Core/Runtime/Core/Network/ISimulationDriver.cs`

```csharp
public interface ISimulationDriver
{
    bool IsActive { get; }
    float TickDelta { get; }
    uint CurrentTick { get; }
}
```

**Aendern:** `PlayerController.cs`
- Extrahiere `OnFixedTick()`-Logik in `public void SimulateTick(float deltaTime)`
- Resolve `ISimulationDriver` via `GetComponent<ISimulationDriver>()` in `InitializeSystems()`
- `Update()` ruft `_tickSystem.Update()` NUR wenn kein externer Driver aktiv

---

### 30.2: Deterministisches Timing — deltaTime durchreichen

**Ziel:** Alle Simulation-Systeme bekommen `deltaTime` als Parameter statt `Time.deltaTime`/`Time.time` zu lesen

**Aendern:** `PlayerMovementState.cs` (Base Class)
- `Update()` → `Update(float deltaTime)`
- `stateTime += deltaTime` statt `stateTime += Time.deltaTime`
- `OnUpdate()` → `OnUpdate(float deltaTime)`

**Aendern:** `IState.cs` (Interface)
- `void Update()` → `void Update(float deltaTime)`

**Aendern:** `PlayerMovementStateMachine.cs`
- `Update()` → `Update(float deltaTime)` — reicht deltaTime an State weiter

**Aendern:** Alle State-Subklassen die `OnUpdate()` ueberschreiben:
- `PlayerSlidingState.cs`: `Time.time` ersetzen durch `stateTime` (bereits akkumuliert!)
  - `_slideStartTime = Time.time` → entfaellt, `stateTime` startet bei 0 in `OnEnter()`
  - `Time.time - _slideStartTime` → `stateTime` (identisch!)
  - `Time.deltaTime` → `deltaTime` Parameter
- `PlayerGroundedState.cs`: `Time.deltaTime` → `deltaTime`
- `PlayerMovingState.cs`: `Time.deltaTime` → `deltaTime`
- `PlayerHardLandingState.cs`: `Time.deltaTime` → `deltaTime`
- Alle anderen States: Signatur-Update (`OnUpdate(float deltaTime)`)

**Aendern:** `CharacterLocomotion.cs`
- Treppen-Erkennung: `Time.time` → `_simulationTime` (neues Feld, akkumuliert in `BeforeCharacterUpdate`)
- `_simulationTime += deltaTime` in `BeforeCharacterUpdate(float deltaTime)`
- `_lastStepTime = _simulationTime` statt `Time.time`

**Aendern:** `PlayerController.cs`
- `SimulateTick(float deltaTime)` reicht deltaTime an StateMachine + AbilitySystem

**NICHT aendern** (Rendering-Layer, liest weiterhin Time.deltaTime):
- AnimatorParameterBridge, ThirdPersonCamera, CameraBrain, IK-Systeme

---

### 30.3: FixedUpdate Offline-Modus

**Ziel:** TickSystem durch Unity FixedUpdate ersetzen fuer Offline

**Aendern:** `PlayerController.cs`
- Entferne `_tickSystem` Feld und `InitializeTickSystem()`
- Neues `FixedUpdate()`: ruft `SimulateTick(Time.fixedDeltaTime)` wenn kein ISimulationDriver
- `ConsumeMovementEvents()` wird in `SimulateTick()` aufgerufen statt in `Update()`
- `CurrentTick`: einfacher Counter statt TickSystem

---

### 30.4: MoveReplicateData + CharacterReconcileData

**Ziel:** FishNet-native Datenstrukturen mit `IReplicateData`/`IReconcileData`

**Erstellen:**
- `Packages/Wiesenwischer.GameKit.Network.FishNet/Runtime/Core/MoveReplicateData.cs`
- `Packages/Wiesenwischer.GameKit.Network.FishNet/Runtime/Core/CharacterReconcileData.cs`

**MoveReplicateData** (IReplicateData):
```
Vector2 MoveDirection, float CameraYaw, float CharacterRotation,
ControllerButtons Buttons, float SpeedModifier,
bool JumpRequested, bool JumpCutRequested, bool ResetVerticalRequested
+ uint _tick (GetTick/SetTick/Dispose)
```

**CharacterReconcileData** (IReconcileData):
```
Vector3 Position, float Rotation, Vector3 Velocity, float VerticalVelocity,
bool IsGrounded, bool IsCrouching, bool ShouldWalk, byte MovementStateIndex
+ uint _tick (GetTick/SetTick/Dispose)
```

Wichtig: `CameraYaw` in ReplicateData **behebt den bekannten Bug**.

---

### 30.5: NetworkCharacterDriver

**Ziel:** Kern-Komponente mit `[Replicate]`/`[Reconcile]` — ersetzt NetworkInputSync + NetworkStateSync

**Erstellen:** `Packages/Wiesenwischer.GameKit.Network.FishNet/Runtime/Core/NetworkCharacterDriver.cs`

Extends `TickNetworkBehaviour`, implementiert `ISimulationDriver`:

- `OnStartNetwork()`: `CharacterMotorSystem.Settings.AutoSimulation = false`
- `OnStopNetwork()`: `CharacterMotorSystem.Settings.AutoSimulation = true`

**Tick-Flow (KRITISCH — korrekte Reihenfolge):**
```csharp
TimeManager_OnTick():
  // 1. Interpolation vorbereiten (NUR hier, NICHT in Replay!)
  CharacterMotorSystem.PreSimulationInterpolationUpdate(tickDelta)

  // 2. Input sammeln + Replicate aufrufen
  var input = BuildReplicateData()
  PerformReplicate(input)

  // 3. Reconcile-State erstellen (Server only)
  CreateReconcile() → PerformReconcile(stateData)

  // 4. Interpolation abschliessen (NUR hier, NICHT in Replay!)
  CharacterMotorSystem.PostSimulationInterpolationUpdate(tickDelta)
```

**[Replicate] Methode:**
```csharp
[Replicate]
void PerformReplicate(MoveReplicateData input, ReplicateState state, Channel channel):
  // Input auf ReusableData setzen
  // Events konsumieren
  // PlayerController.SimulateTick(TimeManager.TickDelta)
  // CharacterMotorSystem.Simulate(delta, [motor])
  // KEINE Pre/Post-Interpolation hier! (wird auch bei Replay aufgerufen)
```

**[Reconcile] Methode:**
- Position/Rotation/Velocity/State zuruecksetzen (Motor TransientPosition/Rotation setzen)

**Weitere Features:**
- `Update()`: One-Shot-Inputs zwischen Ticks akkumulieren (FishNet OneTimeInput Pattern)
- Spectator-Prediction fuer Non-Owner via `state.IsFuture()` (letzten bekannten Input wiederholen)

**Hinweis:** `CustomInterpolationUpdate()` in `CharacterMotorSystem.LateUpdate()` laeuft weiterhin
automatisch — sie nutzt die von Post gesetzten Timestamps fuer die Frame-Interpolation.

---

### 30.6: PlayerController + NetworkPlayer Integration

**Ziel:** Alles zusammenverdrahten

**Aendern:** `PlayerController.cs`
- `Update()`: Wenn `_simulationDriver?.IsActive`, nur `UpdateInput()` + One-Shot-Akkumulation
- `ApplyNetworkInput()` als `[Obsolete]` markieren
- Locomotion/ReusableData Properties muessen fuer Driver zugreifbar sein

**Aendern:** `NetworkPlayer.cs`
- `DisableRemotePlayerInput()`: Motor NICHT mehr deaktivieren (FishNet Spectator-Prediction braucht ihn)
- Remote-Animation Setup bleibt

**Motor-Steuerung:**
- `NetworkCharacterDriver.OnStartNetwork()`: `CharacterMotorSystem.Settings.AutoSimulation = false`
- `NetworkCharacterDriver.OnStopNetwork()`: `CharacterMotorSystem.Settings.AutoSimulation = true` (Fallback)
- ACHTUNG: `AutoSimulation` ist global — betrifft ALLE Motoren. Das ist korrekt fuer MMO (alle Spieler netzwerk-getrieben), aber bei Offline-NPCs muesste man das differenzieren. Fuer Phase 30 akzeptabel.

---

### 30.7: Alten Prediction-Code aufraeumen

**Entfernen** (ersetzt durch FishNet native):
- `NetworkInputSync.cs`
- `NetworkStateSync.cs`
- `RemotePlayerInterpolator.cs`
- `ControllerInputSerializer.cs`
- `PredictionStateSerializer.cs`

**Als `[Obsolete]` markieren** (spaeter entfernbar):
- `TickSystem.cs`
- `InputBuffer.cs`
- `PredictionBuffer.cs`
- `ControllerInput.cs`
- `IPredictionSystem.cs`

---

### 30.8: NetworkAnimationSync Tick-Anpassung

**Aendern:** `NetworkAnimationSync.cs`
- Guard: Animations-State-Changes waehrend Reconcile-Replay (`state.ContainsReplayed()`) NICHT netzwerk-syncen
- Verifizieren dass `TimeManager.Tick` korrekt verwendet wird (ist bereits der Fall)

---

### 30.9: Tests + Verifikation

**Neue Tests:**
- `MoveReplicateData`/`CharacterReconcileData` Serialization Round-Trip
- `ISimulationDriver` Interface-Contract
- `PlayerController.SimulateTick()` Determinismus (gleicher Input+Delta = gleiches Ergebnis)
- `PlayerMovementState.Update(float deltaTime)` — stateTime korrekt akkumuliert

**Play Mode Verifikation:**
- Host starten, lokaler Spieler bewegt sich korrekt
- Client verbinden, beide Spieler sehen sich
- Kein Teleport/Jitter bei normaler Bewegung
- Jump funktioniert (One-Shot Input Handling)
- CameraYaw korrekt uebertragen (Bewegungsrichtung stimmt)
- Animation-Sync funktioniert
- Offline-Modus: FixedUpdate funktioniert ohne FishNet
- Visual Interpolation smooth (kein Snapping zwischen Ticks)

**Debug-Checklist (aus Specs):**
- `Debug.Log(TimeManager.Tick)` auf Client + Server — Tick laeuft synchron, Client leicht voraus
- `Debug.Log("Simulate called")` — genau ~TickRate Aufrufe pro Sekunde, NICHT mehr
- `Debug.Log("Reconcile")` — nur gelegentlich, NICHT jeden Tick
- Kein FixedUpdate-Simulate aktiv wenn Network-Modus laeuft
- `Time.deltaTime` nirgends in Simulation-Code (Grep-Pruefung)

---

## Risiken & Mitigationen

| Risiko | Mitigation |
|--------|-----------|
| Motor FixedUpdate vs Tick Alignment | `AutoSimulation = false` → Motor manuell aus `[Replicate]` via `CharacterMotorSystem.Simulate()` treiben |
| StateMachine nutzt Time.deltaTime/Time.time | **Schritt 30.2** — deltaTime als Parameter durchreichen, Time.time durch stateTime/akkumulierte Zeit ersetzen |
| Viele State-Subklassen betroffen | Base Class `PlayerMovementState` aendern → Compiler erzwingt Signatur-Update in allen Subklassen |
| One-Shot Input Verlust bei Replay | `Dispose()` resettet Flags, `ReplicateState` prueft Replay-Modus |
| Offline 50Hz statt 60Hz | `Time.fixedDeltaTime = 1f/60f` in Project Settings oder 50Hz akzeptieren |
| Kamera waehrend Reconciliation | CameraYaw in ReplicateData gespeichert, Kamera selbst nicht betroffen |
| CharacterLocomotion.Time.time (Treppen) | `_simulationTime` Feld akkumuliert deltaTime deterministisch |
| Motor-Interpolation im Network-Modus kaputt | Pre/Post-Interpolation manuell aus NetworkCharacterDriver aufrufen, NUR beim echten Tick (nicht Replay) |
| Replay ruft Pre/Post-Interpolation auf | `ReplicateState.IsReplayed()` pruefen — waehrend Replay nur Simulate, keine Interpolation |

---

## Dateien-Inventar

### NEU (4)
| Datei | Package |
|-------|---------|
| `Core/Runtime/Core/Network/ISimulationDriver.cs` | CharacterController.Core |
| `Network.FishNet/Runtime/Core/MoveReplicateData.cs` | Network.FishNet |
| `Network.FishNet/Runtime/Core/CharacterReconcileData.cs` | Network.FishNet |
| `Network.FishNet/Runtime/Core/NetworkCharacterDriver.cs` | Network.FishNet |

### AENDERN (12+)
| Datei | Aenderung |
|-------|----------|
| `Core/Runtime/Core/PlayerController.cs` | SimulateTick extrahieren, FixedUpdate, ISimulationDriver, deltaTime weiterreichen |
| `Core/Runtime/Core/StateMachine/IState.cs` | Update(float deltaTime) Signatur |
| `Core/Runtime/Core/StateMachine/PlayerMovementStateMachine.cs` | deltaTime durchreichen |
| `Core/Runtime/Core/StateMachine/States/PlayerMovementState.cs` | Base: stateTime += deltaTime, OnUpdate(float) |
| `Core/Runtime/Core/StateMachine/States/PlayerSlidingState.cs` | Time.time → stateTime, Time.deltaTime → deltaTime |
| `Core/Runtime/Core/StateMachine/States/Grounded/PlayerGroundedState.cs` | Time.deltaTime → deltaTime |
| `Core/Runtime/Core/StateMachine/States/Grounded/Moving/PlayerMovingState.cs` | Time.deltaTime → deltaTime |
| `Core/Runtime/Core/StateMachine/States/Grounded/PlayerHardLandingState.cs` | Time.deltaTime → deltaTime |
| `Core/Runtime/Core/StateMachine/States/*.cs` | Alle: OnUpdate(float deltaTime) Signatur |
| `Core/Runtime/Core/Locomotion/CharacterLocomotion.cs` | Time.time → _simulationTime (Treppen) |
| `Network.FishNet/Runtime/Core/NetworkPlayer.cs` | Motor-Deaktivierung entfernen |
| `Network.FishNet/Runtime/Core/NetworkAnimationSync.cs` | Replay-Guard |
| `docs/implementation/README.md` | Phase 30 eintragen |

### ENTFERNEN (5)
| Datei | Ersetzt durch |
|-------|-------------|
| `NetworkInputSync.cs` | [Replicate] in NetworkCharacterDriver |
| `NetworkStateSync.cs` | [Reconcile] in NetworkCharacterDriver |
| `RemotePlayerInterpolator.cs` | FishNet Spectator Prediction |
| `ControllerInputSerializer.cs` | FishNet auto-serialisiert IReplicateData |
| `PredictionStateSerializer.cs` | FishNet auto-serialisiert IReconcileData |

### UNVERAENDERT
- CharacterMotor (intern, nicht direkt geaendert)
- Animations-Code (AnimatorParameterBridge etc.) — Rendering-Layer, liest weiterhin Time.deltaTime
- IK-Code (LookAtIK, FootLock) — Rendering-Layer
- Kamera-Code (Phase 26-29) — Rendering-Layer
- NetworkAbilitySync, NetworkLookAtTargetProvider
- GameNetworkManager

---

## Abhaengigkeitsgraph

```
30.1 (ISimulationDriver + SimulateTick)
  -> 30.2 (Deterministisches Timing — deltaTime durchreichen)
      -> 30.3 (FixedUpdate Offline)
          -> 30.4 (Replicate/Reconcile Structs)
              -> 30.5 (NetworkCharacterDriver)
                  -> 30.6 (Integration)
                      -> 30.7 (Cleanup) + 30.8 (AnimSync)
                          -> 30.9 (Tests)
```

## Spec-Conformance-Checkliste

Alle 11 Spec-Dateien in `docs/specs/networking/` systematisch geprueft:

| Spec-Anforderung | Status | Adressiert in |
|------------------|--------|---------------|
| Simulation in TimeManager.OnTick, NICHT FixedUpdate | ✅ | 30.5 (NetworkCharacterDriver) |
| TimeManager.TickDelta statt Time.deltaTime | ✅ | 30.2 (Deterministisches Timing) |
| Core network-agnostic (kein NetworkBehaviour) | ✅ | 30.1 (ISimulationDriver in Core) |
| Simulate(InputData, float deltaTime) Entry Point | ✅ | 30.1 (SimulateTick) |
| Kein FixedUpdate + OnTick gleichzeitig | ✅ | 30.5/30.6 (AutoSimulation=false) |
| Input tick-basiert (GatherInput in OnTick) | ✅ | 30.5 (BuildReplicateData in OnTick) |
| [Replicate]/[Reconcile] Pattern | ✅ | 30.4 + 30.5 |
| Simulation != Rendering (Visual Interpolation) | ✅ | 30.5 (Pre/Post-Interpolation manuell) |
| Animator beeinflusst NICHT Simulation | ✅ | Bereits: applyRootMotion=false |
| Kamera folgt Visual-Transform, NICHT Simulation | ✅ | Bereits: CameraAnchor → Transform (interpoliert) |
| IK nur visuell, veraendert nie Simulation | ✅ | Bereits: IK in LateUpdate, kein Sim-Einfluss |
| Offline-Modus via FixedUpdate weiterhin moeglich | ✅ | 30.3 (FixedUpdate Offline) |
| Core-Code identisch zwischen Offline/Network | ✅ | 30.1 (ISimulationDriver Abstraktion) |
| Reconciliation-Strategie (adaptive) | ⚠️ SHOULD | Spaetere Phase — FishNet hat Smoother-Optionen |
| Separate SimulationObject + VisualRoot Hierarchy | ℹ️ | Motor-Ansatz funktionell equivalent (TransientPos ≠ Transform.pos) |
| Debug-Checklist (Tick-Log, Simulate-Count, Reconcile-Spam) | ✅ | 30.9 (Tests + Verifikation) |

**Anmerkung zu "Separate SimulationObject + VisualRoot":**
Die Specs empfehlen getrennte GameObjects fuer Simulation und Visual. Unser Motor-Ansatz erreicht
dasselbe Ziel anders: `TransientPosition` = Simulation-State (im Speicher), `Transform.position` =
interpolierter Visual-State (in LateUpdate gesetzt). Das ist funktionell equivalent und vermeidet
die Komplexitaet zweier Transform-Hierarchien. Die Kamera und der Animator sehen immer die
interpolierte Position — genau wie die Specs es fordern.

**Anmerkung zu "Adaptive Reconciliation":**
Die Specs empfehlen Error-Threshold: `if(error > threshold) snap; else smooth blend`.
FishNet bietet `PredictionManager`-Einstellungen und Smoother-Komponenten dafuer.
Fuer Phase 30 nutzen wir FishNet's Standard-Reconcile (hard restore + replay).
Smooth Reconciliation ist ein Optimierungsschritt fuer eine spaetere Phase, sobald das
Grundsystem stabil laeuft.

---

## Commit-Format
```
feat(phase-30): 30.X Beschreibung
```

## Zusammenfassung

**9 Schritte**, davon 4 neue Dateien, 12+ geaenderte Dateien, 5 zu entfernende Dateien.

Der kritischste Schritt ist **30.2 (Deterministisches Timing)** — er beruehrt die meisten Dateien
(alle StateMachine States + Locomotion + PlayerController), ist aber mechanisch einfach:
deltaTime als Parameter statt `Time.deltaTime` lesen, `Time.time` durch akkumulierte stateTime ersetzen.
Der Compiler erzwingt die Aenderung in allen Subklassen durch die Interface/Base-Class-Aenderung.
