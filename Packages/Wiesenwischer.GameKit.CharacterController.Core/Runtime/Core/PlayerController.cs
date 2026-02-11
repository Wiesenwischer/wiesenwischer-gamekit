using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Animation;
using Wiesenwischer.GameKit.CharacterController.Core.Data;
using Wiesenwischer.GameKit.CharacterController.Core.Input;
using Wiesenwischer.GameKit.CharacterController.Core.Locomotion;
using Wiesenwischer.GameKit.CharacterController.Core.Motor;
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

        #endregion

        #region State Machine

        private PlayerMovementStateMachine _movementStateMachine;

        /// <summary>Die Movement State Machine.</summary>
        public PlayerMovementStateMachine MovementStateMachine => _movementStateMachine;

        /// <summary>Shared runtime data (Shortcut).</summary>
        public PlayerStateReusableData ReusableData => _movementStateMachine?.ReusableData;

        #endregion

        #region Tick System

        private TickSystem _tickSystem;

        /// <summary>Das Tick-System.</summary>
        public TickSystem TickSystem => _tickSystem;

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

        /// <summary>Aktueller Tick.</summary>
        public int CurrentTick => _tickSystem?.CurrentTick ?? 0;

        /// <summary>Ground-Informationen vom Motor.</summary>
        public GroundInfo GroundInfo => Locomotion?.GroundInfo ?? GroundInfo.Empty;

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            InitializeComponents();
            InitializeTickSystem();
            InitializeSystems();
            InitializeStateMachine();
        }

        private void Update()
        {
            // 1. Input lesen und in ReusableData schreiben
            UpdateInput();

            // 2. State Machine Update (HandleInput + Update)
            _movementStateMachine?.Update();

            // 3. Events direkt an Locomotion weiterleiten (vor TickSystem).
            // Events sind One-Shot und dürfen nicht durch den TickSystem-Bottleneck,
            // weil Simulate() per Overwrite arbeitet (latest-value-wins für kontinuierliche Daten).
            // RequestXxx() setzt intern ein Flag das bis zum nächsten Motor FixedUpdate bleibt.
            ConsumeMovementEvents();

            // 4. Tick System aktualisieren (nur kontinuierlicher Input)
            _tickSystem?.Update(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (_tickSystem != null)
            {
                _tickSystem.OnTick -= OnFixedTick;
            }
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

        private void InitializeTickSystem()
        {
            _tickSystem = new TickSystem(TickSystem.DefaultTickRate);
            _tickSystem.OnTick += OnFixedTick;
        }

        private void InitializeSystems()
        {
            if (_config == null || CharacterMotor == null) return;

            // Initialize Character Locomotion
            // Motor ist die EINZIGE Quelle für Ground-State
            // CharacterLocomotion implementiert ICharacterController für den Motor
            Locomotion = new CharacterLocomotion(CharacterMotor, _config);
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
        /// Fixed Tick - Physics Update.
        /// </summary>
        private void OnFixedTick(int tick, float deltaTime)
        {
            if (ReusableData == null) return;

            // Update Tick in ReusableData
            ReusableData.CurrentTick = tick;

            // Ground-State wird NICHT in ReusableData synchronisiert.
            // States befragen Locomotion direkt (Player.Locomotion.IsGrounded etc.)

            // State Machine Physics Update
            _movementStateMachine?.PhysicsUpdate(deltaTime);

            // === PHASE 2: Movement ===
            ApplyMovement(deltaTime);

            // === PHASE 3: Sync Motor State zurück ===
            // Nach ApplyMovement() hat der Motor den aktuellen Ground-State
            // Diesen für den NÄCHSTEN Frame verfügbar machen
            // (Wird am Anfang des nächsten Ticks gelesen)
        }

        /// <summary>
        /// Leitet One-Shot Events von ReusableData an Locomotion weiter.
        /// Wird in Update() aufgerufen (vor TickSystem), damit Events nicht
        /// durch den TickSystem/Simulate()-Bottleneck verloren gehen.
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

            var input = new LocomotionInput
            {
                MoveDirection = ReusableData.MoveInput,
                LookDirection = GetCameraForward(),
                SpeedModifier = ReusableData.MovementSpeedModifier,
                StepDetectionEnabled = ReusableData.StepDetectionEnabled,
                DecelerationOverride = ReusableData.DecelerationOverride,
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
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Vector3 cameraForward = mainCamera.transform.forward;
                cameraForward.y = 0f;
                if (cameraForward.sqrMagnitude > 0.01f)
                {
                    return cameraForward.normalized;
                }
            }
            return transform.forward;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Setzt den Character auf eine Position.
        /// </summary>
        public void SetPosition(Vector3 position)
        {
            Locomotion?.Motor?.SetPosition(position);
        }

        /// <summary>
        /// Setzt die Geschwindigkeit zurück.
        /// </summary>
        public void ResetVelocity()
        {
            ReusableData?.ResetMovementData();
            Locomotion?.StopMovement();
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
