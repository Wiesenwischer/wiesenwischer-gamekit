# Network-Only Migration: Offline-Modus entfernen

## Context

Das GameKit hat aktuell eine **Dual-Mode-Architektur**: Offline (kein Netzwerk) und Online (FishNet).
Dieser duale Ansatz verursacht erhebliche Komplexitaet:

- ~12 Branching-Stellen ueber 4+ Dateien (`if (driver != null && driver.IsActive)`, `if (!NetworkRole.IsNetworkActive)`, etc.)
- Doppelte Konfiguration: `AutoSimulation`, `Interpolate`, `GroundingSmoother` muessen je nach Modus umgeschaltet werden
- `OnStopNetwork()` muss den Offline-Zustand wiederherstellen (fragil, fehleranfaellig)
- Zwei verschiedene Smoothing-Systeme (KCC-Interpolation vs NetworkTickSmoother)
- `UseVisualVelocity` Flag nur fuer Netzwerk, obwohl es immer besser waere
- `IsRemoteMode` Flag auf AnimatorParameterBridge (Animation-Package kennt Netzwerk-Konzept)
- `OfflineNetworkRole` als Singleton-Fallback (Code-Pfad der nie getestet wird im Produktionsbetrieb)

**Ziel:** Netzwerk als **einziger Betriebsmodus**. "Offline" = automatischer lokaler Host.

---

## Kern-Entscheidung: "Offline" = Auto-Host

Statt den Offline-Codepfad zu pflegen, startet das Spiel **immer** als FishNet Host wenn kein Dedicated Server oder expliziter Client-Modus aktiv ist.

| Szenario | Vorher | Nachher |
|----------|--------|---------|
| Spieler drueckt Play im Editor | Offline-Modus (kein FishNet) | Auto-Host auf localhost |
| Dedicated Server | `--server` Flag, BatchMode | Unveraendert |
| Client verbindet | NetworkDebugUI F6 | Unveraendert |
| Singleplayer/Testing | Offline-Modus | Auto-Host (Zero Config) |

**Vorteil:** Jeder Code-Pfad wird immer im Netzwerk-Kontext ausgefuehrt. Bugs die nur im Netzwerk auftreten, werden sofort sichtbar.

---

## Betroffene Dateien und Aenderungen

### 1. PlayerController.cs (Core Package)

**Aktuell:**
```csharp
// Start(): Offline-spezifische Initialisierung
if (!NetworkRole.IsNetworkActive)
{
    ResolveProviders();
    if (InputProvider == null)
        InputProvider = FindObjectOfType<PlayerInputProvider>();
}

// Update(): Driver-Check
if (_simulationDriver != null && _simulationDriver.IsActive) return;
if (!NetworkRole.IsOwner) return;
UpdateInput();

// FixedUpdate(): Driver-Check
if (_simulationDriver != null && _simulationDriver.IsActive) return;
if (!NetworkRole.IsOwner) return;
SimulateTick(Time.fixedDeltaTime);

// InitializeSystems(): Offline-Fallback
NetworkRole = GetComponent<INetworkRole>() ?? OfflineNetworkRole.Instance;
_simulationDriver = GetComponent<ISimulationDriver>();
```

**Nachher:**
```csharp
// Start(): Nichts — NetworkPlayer.EnableLocalPlayer() uebernimmt immer
private void Start() { }

// Update(): Entfaellt komplett — NetworkCharacterDriver.Update() akkumuliert Input immer
// FixedUpdate(): Entfaellt komplett — NetworkCharacterDriver.TimeManager_OnTick() simuliert immer

// InitializeSystems(): Kein Fallback
NetworkRole = GetComponent<INetworkRole>();
Debug.Assert(NetworkRole != null, "INetworkRole (NetworkPlayer) fehlt! Network-Only Architektur.");
// _simulationDriver wird nicht mehr gebraucht — Driver ist immer vorhanden
```

**Entfernbar:**
- `FixedUpdate()` komplett (Simulation laeuft immer ueber Driver)
- `Update()` komplett (Input-Akkumulation laeuft immer ueber Driver)
- `_simulationDriver` Feld und Property (Driver ist Pflicht, nicht optional)
- `Start()` Offline-Branch

**Beibehaltbar:**
- `SimulateTick()` — wird weiterhin vom Driver aufgerufen
- `SetLookDirectionOverride()` — wird weiterhin vom Driver genutzt
- `ResolveProviders()` — wird von NetworkPlayer.EnableLocalPlayer() aufgerufen
- `SetInputProvider()` — wird von NetworkPlayer.EnableLocalPlayer() aufgerufen

---

### 2. OfflineNetworkRole.cs (Core Package) — ENTFERNEN

**Pfad:** `Packages/.../CharacterController.Core/Runtime/Core/Network/OfflineNetworkRole.cs`

Komplett loeschen. Wird nicht mehr referenziert.

---

### 3. ISimulationDriver.cs (Core Package) — ENTFERNEN

**Pfad:** `Packages/.../CharacterController.Core/Runtime/Core/Network/ISimulationDriver.cs`

Komplett loeschen. Der Driver ist immer vorhanden, kein Interface-Check noetig.

PlayerController braucht den Driver nicht zu kennen — der Driver ruft `SimulateTick()` auf, nicht umgekehrt.

---

### 4. NetworkCharacterDriver.cs (Network Package)

**Aktuell (OnStartNetwork):**
```csharp
CharacterMotorSystem.Settings.AutoSimulation = false;
CharacterMotorSystem.Settings.Interpolate = false;

var groundingSmoother = GetComponent<GroundingSmoother>();
if (groundingSmoother != null)
    groundingSmoother.enabled = false;
```

**Aktuell (OnStopNetwork):**
```csharp
// Zurueck zum Offline-Modus
CharacterMotorSystem.Settings.AutoSimulation = true;
CharacterMotorSystem.Settings.Interpolate = true;

var groundingSmoother = GetComponent<GroundingSmoother>();
if (groundingSmoother != null)
    groundingSmoother.enabled = true;
```

**Nachher:**
- `OnStartNetwork()`: Bleibt (Settings muessen gesetzt werden wenn FishNet startet)
- `OnStopNetwork()`: **Restore-Logik entfernen** — es gibt keinen Offline-Modus mehr
- `ISimulationDriver` Interface-Implementierung entfernen (Interface wird geloescht)
- ggf. `RequireComponent(typeof(PlayerController))` beibehalten

```csharp
public override void OnStopNetwork()
{
    base.OnStopNetwork();
    // Kein Restore noetig — Spiel beendet sich oder reconnected
}
```

---

### 5. AnimatorParameterBridge.cs (Animation Package)

**Aktuell:** Zwei Modi: Lokal (liest Motor-Daten oder Visual-Velocity) und Remote (extern gesetzte Werte).

**Problem:** `IsRemoteMode` und `UseVisualVelocity` sind Netzwerk-Konzepte im Animation-Package.

**Nachher:**
- `UseVisualVelocity` wird **Default** (immer aktiv fuer Owner) — Rename zu internem Feld, kein Public Property mehr
- `IsRemoteMode` bleibt als Public Property — wird von NetworkPlayer gesetzt fuer Non-Owner Characters
  - **Alternative (sauberer):** Zwei separate Bridges:
    - `AnimatorParameterBridge` (Owner, berechnet Parameter selbst)
    - `RemoteAnimatorBridge` (Non-Owner, empfaengt Parameter extern)
  - **Empfehlung:** IsRemoteMode beibehalten (weniger Aufwand, funktioniert), aber in Phase 40+ durch zwei Bridges ersetzen

**Konkret:**
```csharp
// UseVisualVelocity wird immer true, kein Toggle mehr
// _useVisualVelocity Feld entfernen, Visual-Velocity-Path ist der einzige Path

private void UpdateParameters()
{
    // NUR Visual-Velocity-Path (Motor-Velocity-Path entfaellt)
    Vector3 currentPos = transform.position;
    if (!_hasLastVisualPos) { ... }
    else
    {
        float dt = Time.deltaTime;
        movementSpeed = new Vector3(delta.x, 0f, delta.z).magnitude / dt;
        verticalVelocity = delta.y / dt;
    }
    // Treppen/Terrain-Kompensation entfaellt (Visual-Velocity inkludiert das bereits)
    // ...
}
```

**Entfernbar:**
- `_useVisualVelocity` Feld und Property
- Offline-Branch in `UpdateParameters()` (Motor-basierte Berechnung)
- Treppen-/Terrain-Kompensation (Visual-Velocity bildet das bereits ab)
- `_stairAnimSpeedMultiplier` SerializeField

---

### 6. CharacterMotorSystem Settings

**Aktuell:** `AutoSimulation` und `Interpolate` sind Runtime-Toggles.

**Nachher:** Default-Werte aendern:
- `AutoSimulation = false` (Default)
- `Interpolate = false` (Default)

NetworkCharacterDriver setzt sie trotzdem explizit (Safety), aber der Default ist jetzt korrekt.

**Optional:** Die Felder als `[HideInInspector]` markieren, da sie nicht mehr manuell umgeschaltet werden sollen.

---

### 7. GroundingSmoother.cs

**Aktuell:** Wird von NetworkCharacterDriver.OnStartNetwork() disabled.

**Nachher:** Zwei Optionen:
- **Option A:** Auf dem Prefab per Default deaktiviert (Inspector `enabled = false`). NetworkCharacterDriver muss es nicht mehr toggling.
- **Option B:** Komplett vom Player Prefab entfernen. NetworkTickSmoother uebernimmt Step-Up-Smoothing.

**Empfehlung:** Option B — GroundingSmoother ist mit DetachOnStart-Architektur inkompatibel und wird nie gebraucht.

---

### 8. GameNetworkManager.cs + ServerBootstrap.cs

**Neue Logik: Auto-Host**

```csharp
// ServerBootstrap.cs — erweiterte Start()-Logik:
private void Start()
{
    if (Application.isBatchMode || HasArgument("--server"))
    {
        // Dedicated Server Modus (unveraendert)
        ConfigureTransport();
        StartCoroutine(StartServerDelayed());
    }
    else if (HasArgument("--client"))
    {
        // Expliziter Client-Modus (optional, z.B. fuer Builds)
        // Nichts tun — User verbindet manuell oder via NetworkDebugUI
    }
    else
    {
        // Auto-Host: Lokaler Host fuer Singleplayer/Editor-Testing
        Log("Auto-Host Modus");
        StartCoroutine(StartHostDelayed());
    }
}

private IEnumerator StartHostDelayed()
{
    yield return null;
    _gameNetworkManager.StartHost();
    Log("Auto-Host gestartet (localhost).");
}
```

**NetworkDebugUI.cs:** Start-Buttons (F5/F6/F7) bleiben — sie sind weiterhin noetig um den Auto-Host zu stoppen und z.B. als Client gegen einen anderen Server zu verbinden.

---

## Entfernter Code (Zusammenfassung)

| Datei | Entfernt | Zeilen (ca.) |
|-------|----------|-------------|
| `OfflineNetworkRole.cs` | Komplett | ~15 |
| `ISimulationDriver.cs` | Komplett | ~20 |
| `PlayerController.cs` | `Update()`, `FixedUpdate()`, `_simulationDriver`, Offline-Branches in `Start()` und `InitializeSystems()` | ~30 |
| `NetworkCharacterDriver.cs` | `OnStopNetwork()` Restore-Logik, `ISimulationDriver` Implementierung | ~15 |
| `AnimatorParameterBridge.cs` | `_useVisualVelocity` Toggle, Motor-Velocity-Branch, Treppen/Terrain-Kompensation | ~40 |

**Total:** ~120 Zeilen entfernt, ~20 Zeilen hinzugefuegt (Auto-Host in ServerBootstrap)

---

## Abhaengigkeitsgraph (nachher)

```
CharacterController.Core (FishNet-frei)
  ├── INetworkRole (Interface, bleibt)
  ├── IAnimationNetworkSync (Interface, bleibt)
  ├── PlayerController (kein FixedUpdate, kein Update, kein Driver-Feld)
  └── CharacterMotorSystem (AutoSimulation=false Default)

CharacterController.Animation (FishNet-frei)
  └── AnimatorParameterBridge (immer Visual-Velocity, IsRemoteMode bleibt)

Network.FishNet (einziges FishNet-abhaengiges Package)
  ├── NetworkPlayer (INetworkRole)
  ├── NetworkCharacterDriver (treibt Simulation, IMMER aktiv)
  ├── NetworkAnimationSync (Sync States+Params)
  ├── ServerBootstrap (Auto-Host oder Dedicated Server)
  └── GameNetworkManager (Lifecycle)
```

**Unveraendert:** Core-Package bleibt FishNet-frei. Interfaces bleiben als Abstraktionsgrenze.
Die Package-Struktur aendert sich NICHT — nur die internen Code-Pfade werden vereinfacht.

---

## Migration — Schritt fuer Schritt

### Phase 1: Auto-Host (ServerBootstrap)
1. `ServerBootstrap.Start()` um Auto-Host-Logik erweitern
2. Test: Play im Editor → FishNet startet als Host → Player spawned → alles funktioniert

### Phase 2: PlayerController vereinfachen
1. `FixedUpdate()` entfernen
2. `Update()` entfernen
3. `_simulationDriver` Feld/Property entfernen
4. `Start()` Offline-Branch entfernen
5. `InitializeSystems()`: `OfflineNetworkRole` Fallback → Assert
6. Test: Play im Editor → Bewegung funktioniert (ueber NetworkCharacterDriver)

### Phase 3: OfflineNetworkRole + ISimulationDriver loeschen
1. `OfflineNetworkRole.cs` loeschen
2. `ISimulationDriver.cs` loeschen
3. Compiler-Fehler in NetworkCharacterDriver beheben (ISimulationDriver entfernen)
4. Test: Kompiliert fehlerfrei

### Phase 4: NetworkCharacterDriver aufraumen
1. `OnStopNetwork()` Restore-Logik entfernen
2. Test: Stop → Restart funktioniert noch (oder ist nicht mehr noetig)

### Phase 5: AnimatorParameterBridge vereinfachen
1. `UseVisualVelocity` Property/Feld entfernen
2. Motor-Velocity-Branch in `UpdateParameters()` entfernen
3. Visual-Velocity als einziger Pfad
4. Treppen/Terrain-Kompensation entfernen (visual velocity bildet das ab)
5. NetworkPlayer: `animBridge.UseVisualVelocity = true` Zeile entfernen
6. Test: Animationen smooth im Auto-Host

### Phase 6: GroundingSmoother entfernen
1. GroundingSmoother vom Player Prefab entfernen (oder disabled lassen)
2. NetworkCharacterDriver: GroundingSmoother-Toggle entfernen
3. Test: Step-Ups weiterhin smooth (NetworkTickSmoother)

### Phase 7: CharacterMotorSystem Defaults
1. `AutoSimulation = false`, `Interpolate = false` als Default
2. Optional: `[HideInInspector]` auf beide Felder

---

## Risiken und Mitigationen

| Risiko | Mitigation |
|--------|-----------|
| Editor-Play dauert laenger (FishNet muss starten) | FishNet Host-Start ist <100ms, kaum spuerbar |
| Tests die PlayerController ohne Netzwerk nutzen | Unit Tests muessen NetworkPlayer + Driver mocken oder als Integration Tests mit FishNet laufen |
| Auto-Host blockiert Port | Nur localhost, kein externer Listener. Mehrere Instanzen brauchen verschiedene Ports |
| Bestehende Szenen ohne NetworkManager | ServerBootstrap + GameNetworkManager muessen in jeder Szene vorhanden sein (oder via DontDestroyOnLoad) |

---

## Nicht im Scope

- **IsRemoteMode Refactoring** (zwei separate Bridges) — Phase 40+
- **Unit Test Migration** — separater Task
- **DontDestroyOnLoad NetworkManager** — separater Task (Multi-Scene Architektur)
- **Lobby/Matchmaking** — separater Task
