using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Animation;
using Wiesenwischer.GameKit.CharacterController.Core.Data;
using Wiesenwischer.GameKit.CharacterController.Core.Input;
using Wiesenwischer.GameKit.CharacterController.Core.Locomotion;
using Wiesenwischer.GameKit.CharacterController.Core.Motor;
using Wiesenwischer.GameKit.CharacterController.Core.Prediction;
using Wiesenwischer.GameKit.CharacterController.Core.StateMachine;

namespace Wiesenwischer.GameKit.CharacterController.Core
{
    /// <summary>
    /// Hauptkomponente für Character Controller.
    /// Basiert auf dem Genshin Impact Pattern:
    /// - Zentraler Zugriffspunkt für alle Komponenten
    /// - Verwendet PlayerMovementStateMachine mit ReusableData
    /// - CSP (Client-Side Prediction) kompatibel für MMO-Nutzung
    /// Ground-State kommt direkt vom Motor (keine Events, direkte Abfrage).
    /// </summary>
    [RequireComponent(typeof(CharacterMotor))]
    public class PlayerController : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Configuration")]
        [Tooltip("Locomotion-Konfiguration")]
        [SerializeField] private LocomotionConfig _config;

        [Header("Input")]
        [Tooltip("Input Provider (optional - wird automatisch gesucht)")]
        [SerializeField] private MonoBehaviour _inputProviderComponent;

        [Header("Debug")]
        [Tooltip("Debug-Informationen anzeigen")]
        [SerializeField] private bool _showDebugInfo = true;
        [Tooltip("Debug-Gizmos zeichnen")]
        [SerializeField] private bool _drawGizmos = true;

        #endregion

        #region Components (Genshin Pattern - Read-Only Properties)

        /// <summary>Der Character Motor (exakte KCC-Kopie).</summary>
        public CharacterMotor CharacterMotor { get; private set; }

        /// <summary>Der CapsuleCollider (vom Motor).</summary>
        public CapsuleCollider CapsuleCollider => CharacterMotor?.Capsule;

        /// <summary>Der Input Provider.</summary>
        public IMovementInputProvider InputProvider { get; private set; }

        /// <summary>Character Locomotion System.</summary>
        public CharacterLocomotion Locomotion { get; private set; }

        /// <summary>Die Locomotion-Konfiguration.</summary>
        public LocomotionConfig LocomotionConfig => _config;

        /// <summary>Der Animation Controller (optional, auf Child-Object).</summary>
        public IAnimationController AnimationController { get; private set; }

        /// <summary>Das Ability System (optional).</summary>
        public IAbilitySystem AbilitySystem { get; private set; }

        /// <summary>Netzwerk-Rolle (Owner/Server/Client). Default: Offline.</summary>
        public INetworkRole NetworkRole { get; private set; }

        #endregion

        #region State Machine

        private PlayerMovementStateMachine _movementStateMachine;

        /// <summary>Die Movement State Machine.</summary>
        public PlayerMovementStateMachine MovementStateMachine => _movementStateMachine;

        /// <summary>Shared runtime data (Shortcut).</summary>
        public PlayerStateReusableData ReusableData => _movementStateMachine?.ReusableData;

        #endregion

        #region Camera Integration

        private ICameraOrbitProvider _orbitProvider;
        private IOrientationProvider _orientationProvider;
        private IFacingProvider _facingProvider;

        /// <summary>
        /// Override fuer LookDirection im Netzwerk-Modus.
        /// NetworkCharacterDriver setzt dies aus dem CameraYaw des Inputs.
        /// </summary>
        private Vector3? _lookDirectionOverride;

        #endregion

        #region Simulation Driver

        private ISimulationDriver _simulationDriver;

        /// <summary>
        /// Externer SimulationDriver (z.B. NetworkCharacterDriver).
        /// Null im Offline-Modus.
        /// </summary>
        public ISimulationDriver SimulationDriver => _simulationDriver;

        #endregion

        #region Public Properties (Convenience)

        /// <summary>Der aktuelle State-Name.</summary>
        public string CurrentStateName => _movementStateMachine?.CurrentStateName ?? "None";

        /// <summary>Ob der Character auf dem Boden steht (von der IGroundDetectionStrategy).</summary>
        public bool IsGrounded => Locomotion?.IsGrounded ?? false;

        /// <summary>Ob der Character gerade gelandet ist.</summary>
        public bool JustLanded => Locomotion?.Motor?.JustLanded ?? false;

        /// <summary>Ob der Character gerade den Boden verlassen hat.</summary>
        public bool JustLeftGround => Locomotion?.Motor?.JustLeftGround ?? false;

        /// <summary>Ob der Character gerade rutscht.</summary>
        public bool IsSliding => Locomotion?.IsSliding ?? false;

        /// <summary>Die aktuelle Geschwindigkeit.</summary>
        public Vector3 Velocity => ReusableData?.Velocity ?? Vector3.zero;

        /// <summary>Aktueller Tick (einfacher Counter, im Netzwerk-Modus vom Driver gesetzt).</summary>
        private int _currentTick;
        public int CurrentTick => _currentTick;

        /// <summary>Tick-Delta fuer die Simulation (FixedUpdate-Intervall).</summary>
        public float TickDelta => Time.fixedDeltaTime;

        /// <summary>Ground-Informationen vom Motor.</summary>
        public GroundInfo GroundInfo => Locomotion?.GroundInfo ?? GroundInfo.Empty;

        /// <summary>
        /// Aktueller CameraYaw in Grad (fuer MoveReplicateData).
        /// Liest den Yaw der Hauptkamera oder Fallback auf Character-Rotation.
        /// </summary>
        public float CameraYaw
        {
            get
            {
                var mainCamera = Camera.main;
                return mainCamera != null ? mainCamera.transform.eulerAngles.y : transform.eulerAngles.y;
            }
        }

        /// <summary>
        /// Index des aktuellen Movement-States fuer Reconcile-Serialisierung.
        /// </summary>
        public byte CurrentMovementStateIndex => _movementStateMachine?.CurrentStateIndex ?? 0;

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            InitializeComponents();
            InitializeSystems();
            InitializeStateMachine();
        }

        private void Start()
        {
            // Provider nach Awake auflösen (CameraBrain muss zuerst initialisiert sein)
            ResolveProviders();
        }

        private void Update()
        {
            // Nur der Owner simuliert Input.
            // Im Offline-Modus: OfflineNetworkRole.IsOwner == true → alles läuft wie bisher.
            if (!NetworkRole.IsOwner) return;

            // Input wird immer in Update() gesammelt (Frame-Rate).
            // Simulation laeuft in FixedUpdate() oder ueber externen Driver.
            UpdateInput();
        }

        private void FixedUpdate()
        {
            // Nur simulieren wenn KEIN externer Driver aktiv ist.
            // Im Netzwerk-Modus treibt NetworkCharacterDriver die Simulation.
            if (_simulationDriver != null && _simulationDriver.IsActive)
                return;

            // Nur der Owner simuliert.
            if (!NetworkRole.IsOwner) return;

            SimulateTick(Time.fixedDeltaTime);
        }

        private void OnDrawGizmos()
        {
            if (!_drawGizmos) return;
            Locomotion?.DrawDebugGizmos();
        }

        private void OnGUI()
        {
            if (!_showDebugInfo || !Application.isPlaying) return;
            DrawDebugGUI();
        }

        #endregion

        #region Initialization

        private void InitializeComponents()
        {
            // Get CharacterMotor (exakte KCC-Kopie als MonoBehaviour)
            CharacterMotor = GetComponent<CharacterMotor>();
            if (CharacterMotor == null)
            {
                Debug.LogError($"[PlayerController] FEHLER auf '{gameObject.name}': " +
                    "CharacterMotor-Komponente fehlt!");
                enabled = false;
                return;
            }

            // Find Input Provider
            if (_inputProviderComponent != null)
            {
                InputProvider = _inputProviderComponent as IMovementInputProvider;
            }
            InputProvider ??= GetComponent<IMovementInputProvider>();

            if (InputProvider == null)
            {
                Debug.LogWarning($"[PlayerController] WARNUNG auf '{gameObject.name}': " +
                    "Kein Input Provider gefunden.");
            }

            // Validate config
            if (_config == null)
            {
                Debug.LogError($"[PlayerController] FEHLER auf '{gameObject.name}': " +
                    "Keine LocomotionConfig zugewiesen!");
                enabled = false;
                return;
            }

            ValidateLocomotionConfig();

            InitializeAnimationController();
        }

        private void InitializeAnimationController()
        {
            AnimationController = GetComponentInChildren<IAnimationController>();

            if (AnimationController == null)
                Debug.LogWarning("[PlayerController] Kein IAnimationController gefunden. " +
                                 "Animationen werden nicht abgespielt.");
        }

        private void ValidateLocomotionConfig()
        {
            if (_config.WalkSpeed <= 0f)
                Debug.LogWarning("[PlayerController] WARNUNG: WalkSpeed sollte > 0 sein.");
            if (_config.Gravity <= 0f)
                Debug.LogWarning("[PlayerController] WARNUNG: Gravity sollte > 0 sein.");
            if (_config.GroundLayers == 0)
                Debug.LogWarning("[PlayerController] WARNUNG: GroundLayers ist leer.");
        }

        private void InitializeSystems()
        {
            if (_config == null || CharacterMotor == null) return;

            // Initialize Character Locomotion
            // Motor ist die EINZIGE Quelle für Ground-State
            // CharacterLocomotion implementiert ICharacterController für den Motor
            Locomotion = new CharacterLocomotion(CharacterMotor, _config);

            // Optional: Ability System (kann fehlen)
            AbilitySystem = GetComponent<IAbilitySystem>();

            // Network Role (falls NetworkPlayer vorhanden, sonst Offline-Default)
            NetworkRole = GetComponent<INetworkRole>() ?? OfflineNetworkRole.Instance;

            // SimulationDriver suchen (optional, nur im Netzwerk-Modus vorhanden)
            _simulationDriver = GetComponent<ISimulationDriver>();
        }

        private void InitializeStateMachine()
        {
            _movementStateMachine = new PlayerMovementStateMachine(this);
            _movementStateMachine.Initialize();
        }

        #endregion

        #region Update Loop

        /// <summary>
        /// Liest Input und schreibt in ReusableData.
        /// </summary>
        private void UpdateInput()
        {
            if (InputProvider == null || ReusableData == null) return;

            ReusableData.MoveInput = InputProvider.MoveInput;
            ReusableData.JumpPressed = InputProvider.JumpPressed;
            ReusableData.JumpHeld = InputProvider.JumpHeld;
            ReusableData.SprintHeld = InputProvider.SprintHeld;
            ReusableData.DashPressed = InputProvider.DashPressed;
            ReusableData.CrouchTogglePressed = InputProvider.CrouchTogglePressed;

            // Walk Toggle (MMO-Style: Taste drücken → Walk ein/aus)
            if (InputProvider.WalkTogglePressed)
            {
                ReusableData.ShouldWalk = !ReusableData.ShouldWalk;
            }

            // Sprint deaktiviert Walk automatisch
            if (ReusableData.SprintHeld && ReusableData.ShouldWalk)
            {
                ReusableData.ShouldWalk = false;
            }
        }

        /// <summary>
        /// Leitet One-Shot Events von ReusableData an Locomotion weiter.
        /// Wird innerhalb von SimulateTick() aufgerufen.
        /// </summary>
        private void ConsumeMovementEvents()
        {
            if (Locomotion == null || ReusableData == null) return;

            if (ReusableData.JumpRequested)
            {
                Locomotion.RequestJump();
                ReusableData.JumpRequested = false;
            }
            if (ReusableData.JumpCutRequested)
            {
                Locomotion.RequestJumpCut();
                ReusableData.JumpCutRequested = false;
            }
            if (ReusableData.ResetVerticalRequested)
            {
                Locomotion.RequestResetVertical();
                ReusableData.ResetVerticalRequested = false;
            }
        }

        /// <summary>
        /// Wendet Bewegung über Locomotion an.
        /// Nur kontinuierlicher Input - Events gehen über ConsumeMovementEvents().
        /// </summary>
        private void ApplyMovement(float deltaTime)
        {
            if (Locomotion == null || ReusableData == null) return;

            // Frame-Space: Worin wird WASD interpretiert?
            // Im Netzwerk-Modus kann die LookDirection per Override gesetzt werden (CameraYaw).
            Vector3 lookDir;
            if (_lookDirectionOverride.HasValue)
            {
                lookDir = _lookDirectionOverride.Value;
            }
            else
            {
                lookDir = _orientationProvider != null
                    ? _orientationProvider.GetMovementForward()
                    : GetCameraForward(); // Fallback auf Legacy
            }

            // Facing: Wie soll Character rotieren?
            FacingMode facingMode = _facingProvider?.GetFacingMode()
                ?? FacingMode.MovementDirection;
            Vector3 facingDir = _facingProvider?.GetFacingDirection()
                ?? Vector3.zero;

            bool isSteerMode = _orbitProvider != null && _orbitProvider.IsSteerMode;

            var input = new LocomotionInput
            {
                MoveDirection = ReusableData.MoveInput,
                LookDirection = lookDir,
                SpeedModifier = ReusableData.MovementSpeedModifier,
                StepDetectionEnabled = ReusableData.StepDetectionEnabled,
                DecelerationOverride = ReusableData.DecelerationOverride,
                IsSteerMode = isSteerMode,
                FacingMode = facingMode,
                FacingDirection = facingDir,
            };

            Locomotion.Simulate(input, deltaTime);

            // Sync-Back: Locomotion → ReusableData
            ReusableData.HorizontalVelocity = Locomotion.HorizontalVelocity;
            ReusableData.VerticalVelocity = Locomotion.VerticalVelocity;
        }

        /// <summary>
        /// Ermittelt die Forward-Richtung der Kamera.
        /// </summary>
        private Vector3 GetCameraForward()
        {
            if (_orbitProvider != null)
                return _orbitProvider.Forward;

            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Vector3 cameraForward = mainCamera.transform.forward;
                cameraForward.y = 0f;
                if (cameraForward.sqrMagnitude > 0.01f)
                    return cameraForward.normalized;
            }
            return transform.forward;
        }

        /// <summary>
        /// Löst Orientation-, Facing- und OrbitProvider auf.
        /// IOrientationProvider/IFacingProvider sind die bevorzugten Interfaces (Phase 29).
        /// ICameraOrbitProvider bleibt als Fallback für IsSteerMode.
        /// </summary>
        private void ResolveProviders()
        {
            var mainCamera = Camera.main;
            if (mainCamera == null) return;

            // Neue Provider vom CameraBrain-Hierarchy auflösen
            _orientationProvider = mainCamera.GetComponentInParent<IOrientationProvider>();
            _facingProvider = mainCamera.GetComponentInParent<IFacingProvider>();

            if (_orientationProvider == null || _facingProvider == null)
                Debug.LogWarning($"[PlayerController] IOrientationProvider/IFacingProvider nicht gefunden! " +
                    "CameraOrientationProvider auf dem CameraBrain-GameObject hinzufügen. " +
                    "Fallback: Camera-Forward für alle Modi.");

            // Legacy OrbitProvider (für IsSteerMode Fallback)
            _orbitProvider = mainCamera.GetComponentInParent<ICameraOrbitProvider>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Fuehrt einen vollstaendigen Simulations-Tick aus.
        /// Wird von ISimulationDriver (online) oder FixedUpdate (offline, ab 30.3) aufgerufen.
        /// Kombiniert State-Machine-Logik, Events, Physics und Bewegung in einem Tick.
        /// </summary>
        public void SimulateTick(float deltaTime)
        {
            if (ReusableData == null) return;

            // 1. StateMachine Update (HandleInput + Update)
            _movementStateMachine?.Update(deltaTime);

            // 2. Movement Events konsumieren (Jump, etc.)
            ConsumeMovementEvents();

            // 3. StateMachine Physics Update
            _movementStateMachine?.PhysicsUpdate(deltaTime);

            // 4. Bewegung anwenden
            ApplyMovement(deltaTime);

            // 5. AbilitySystem Tick
            AbilitySystem?.Tick(deltaTime);

            // 6. Tick-Counter inkrementieren
            _currentTick++;
        }

        /// <summary>
        /// Setzt den Character auf eine Position.
        /// </summary>
        public void SetPosition(Vector3 position)
        {
            Locomotion?.Motor?.SetPosition(position);
        }

        /// <summary>
        /// Stellt einen Movement-State aus dem Reconcile-Snapshot wieder her.
        /// </summary>
        public void RestoreMovementState(byte stateIndex)
        {
            _movementStateMachine?.RestoreState(stateIndex);
        }

        /// <summary>
        /// Setzt eine LookDirection-Override fuer den Netzwerk-Modus.
        /// Wenn gesetzt, wird diese statt der Kamera-Richtung in ApplyMovement verwendet.
        /// Null zum Zuruecksetzen.
        /// </summary>
        public void SetLookDirectionOverride(Vector3? direction)
        {
            _lookDirectionOverride = direction;
        }

        /// <summary>
        /// Setzt die Geschwindigkeit zurück.
        /// </summary>
        public void ResetVelocity()
        {
            ReusableData?.ResetMovementData();
            Locomotion?.StopMovement();
        }

        /// <summary>
        /// Wendet einen ControllerInput an und simuliert einen Tick.
        /// Wird vom Server (über NetworkInputSync) und bei Client-Resimulation
        /// (Reconciliation Rollback) aufgerufen.
        /// Nutzt CameraYaw aus dem Input statt der lokalen Kamera,
        /// da die lokale Kamera einem anderen Spieler gehören kann.
        /// </summary>
        public void ApplyNetworkInput(ControllerInput input, float tickDelta)
        {
            if (ReusableData == null || Locomotion == null) return;

            ReusableData.MoveInput = input.MoveDirection;
            ReusableData.JumpPressed = input.Jump;
            ReusableData.SprintHeld = input.Sprint;
            ReusableData.CrouchTogglePressed = input.Crouch;

            _movementStateMachine?.Update(tickDelta);
            ConsumeMovementEvents();

            // Bewegungsrichtung aus Client-CameraYaw ableiten statt lokaler Kamera.
            // Auf dem Server gehört Camera.main dem Host, nicht dem Remote-Client.
            Vector3 lookDir = Quaternion.Euler(0f, input.CameraYaw, 0f) * Vector3.forward;

            var locomotionInput = new LocomotionInput
            {
                MoveDirection = ReusableData.MoveInput,
                LookDirection = lookDir,
                SpeedModifier = ReusableData.MovementSpeedModifier,
                StepDetectionEnabled = ReusableData.StepDetectionEnabled,
                DecelerationOverride = ReusableData.DecelerationOverride,
                IsSteerMode = false,
                FacingMode = FacingMode.MovementDirection,
                FacingDirection = Vector3.zero,
            };

            Locomotion.Simulate(locomotionInput, tickDelta);

            ReusableData.HorizontalVelocity = Locomotion.HorizontalVelocity;
            ReusableData.VerticalVelocity = Locomotion.VerticalVelocity;
        }

        #endregion

        #region Debug

        private void DrawDebugGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 350, 280));
            GUILayout.BeginVertical("box");

            GUILayout.Label($"<b>PlayerController (Genshin Pattern)</b>");
            GUILayout.Label($"State: {CurrentStateName}");
            GUILayout.Label($"Grounded: {IsGrounded}");
            GUILayout.Label($"Sliding: {IsSliding}");
            GUILayout.Label($"Velocity: {Velocity:F2}");

            if (ReusableData != null)
            {
                GUILayout.Label($"H-Velocity: {ReusableData.HorizontalVelocity.magnitude:F2}");
                GUILayout.Label($"V-Velocity: {ReusableData.VerticalVelocity:F2}");
                GUILayout.Label($"Mode: {(ReusableData.ShouldWalk ? "<color=yellow>Walk</color>" : "<color=lime>Run</color>")}");
            }

            if (Locomotion != null)
            {
                GUILayout.Label($"Stairs: {Locomotion.IsOnStairs} | Terrain: {Locomotion.CurrentTerrainSpeedMultiplier:F2}x");
            }
            GUILayout.Label($"Tick: {CurrentTick}");

            var gi = GroundInfo;
            GUILayout.Label($"Slope: {gi.SlopeAngle:F1}° ({(gi.IsWalkable ? "walkable" : "too steep")})");
            GUILayout.Label($"Stable: {gi.StabilityReport.IsStable} (Ledge: {gi.StabilityReport.LedgeDetected})");
            if (gi.StabilityReport.LedgeDetected)
            {
                GUILayout.Label($"  Distance: {gi.StabilityReport.DistanceFromLedge:F2}m");
            }

            GUILayout.Label($"<i>CSP-Ready</i>");

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        #endregion
    }
}
