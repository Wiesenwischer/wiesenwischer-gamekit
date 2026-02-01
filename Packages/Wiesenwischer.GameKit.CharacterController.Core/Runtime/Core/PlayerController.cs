using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Input;
using Wiesenwischer.GameKit.CharacterController.Core.Movement;
using Wiesenwischer.GameKit.CharacterController.Core.StateMachine;
using Wiesenwischer.GameKit.CharacterController.Core.StateMachine.States;

namespace Wiesenwischer.GameKit.CharacterController.Core
{
    /// <summary>
    /// Hauptkomponente für Character Controller.
    /// Integriert Input, Movement, State Machine und Prediction.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.CharacterController))]
    public class PlayerController : MonoBehaviour, IStateMachineContext
    {
        [Header("Configuration")]
        [Tooltip("Movement-Konfiguration")]
        [SerializeField] private MovementConfig _config;

        [Header("Input")]
        [Tooltip("Input Provider (optional - wird automatisch gesucht)")]
        [SerializeField] private MonoBehaviour _inputProviderComponent;

        [Header("Ground Check")]
        [Tooltip("Transform für Ground Check Position (optional)")]
        [SerializeField] private Transform _groundCheckTransform;

        [Header("Debug")]
        [Tooltip("Debug-Informationen anzeigen")]
        [SerializeField] private bool _showDebugInfo = true;
        [Tooltip("Debug-Gizmos zeichnen")]
        [SerializeField] private bool _drawGizmos = true;

        // Components
        private UnityEngine.CharacterController _characterController;
        private IMovementInputProvider _inputProvider;

        // Systems
        private GroundingDetection _groundingDetection;
        private MovementMotor _movementSimulator;
        private CharacterStateMachine _stateMachine;

        // States
        private GroundedState _groundedState;
        private JumpingState _jumpingState;
        private FallingState _fallingState;

        // State Machine Context
        private Vector2 _moveInput;
        private bool _jumpPressed;
        private bool _jumpHeld;
        private float _verticalVelocity;
        private Vector3 _horizontalVelocity;

        // Tick System
        private TickSystem _tickSystem;

        // Ground collision tracking (from OnControllerColliderHit)
        private Vector3 _lastGroundNormal = Vector3.up;
        private bool _hasGroundCollision;
        private float _groundCollisionAngle;

        #region IStateMachineContext Implementation

        public Vector2 MoveInput => _moveInput;
        public bool JumpPressed => _jumpPressed;
        public bool JumpHeld => _jumpHeld;
        public bool IsGrounded => _groundingDetection?.IsGrounded ?? false;
        public float VerticalVelocity { get => _verticalVelocity; set => _verticalVelocity = value; }
        public Vector3 HorizontalVelocity { get => _horizontalVelocity; set => _horizontalVelocity = value; }
        public IMovementConfig Config => _config;
        public int CurrentTick => _tickSystem?.CurrentTick ?? 0;

        #endregion

        #region Public Properties

        /// <summary>
        /// Der aktuelle State-Name.
        /// </summary>
        public string CurrentStateName => _stateMachine?.CurrentStateName ?? "None";

        /// <summary>
        /// Die aktuelle Geschwindigkeit.
        /// </summary>
        public Vector3 Velocity => _horizontalVelocity + Vector3.up * _verticalVelocity;

        /// <summary>
        /// Der CharacterController.
        /// </summary>
        public UnityEngine.CharacterController CharacterController => _characterController;

        /// <summary>
        /// Die Movement-Konfiguration.
        /// </summary>
        public MovementConfig MovementConfig => _config;

        /// <summary>
        /// Ground Detection System.
        /// </summary>
        public GroundingDetection GroundingDetection => _groundingDetection;

        /// <summary>
        /// Die State Machine.
        /// </summary>
        public CharacterStateMachine StateMachine => _stateMachine;

        /// <summary>
        /// Das Tick-System.
        /// </summary>
        public TickSystem TickSystem => _tickSystem;

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
            // Update Input
            UpdateInput();

            // Update Tick System (executes FixedTick via event)
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
            _groundingDetection?.DrawDebugGizmos();
        }

        private void OnGUI()
        {
            if (!_showDebugInfo || !Application.isPlaying) return;
            DrawDebugGUI();
        }

        /// <summary>
        /// Wird vom CharacterController bei jeder Kollision aufgerufen.
        /// Liefert die exakte Kollisions-Normal vom Physics-System.
        /// </summary>
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // Prüfe ob dies eine Boden-Kollision ist (Normal zeigt nach oben)
            float angle = Vector3.Angle(hit.normal, Vector3.up);

            // Nur Kollisionen berücksichtigen, die "Boden-artig" sind (< 85°)
            // Wände (>85°) ignorieren
            if (angle < 85f)
            {
                // Dies ist eine potenzielle Boden-Kollision
                // Prüfe ob der Kontaktpunkt unter dem Character ist
                if (hit.point.y < transform.position.y + 0.1f)
                {
                    _hasGroundCollision = true;
                    _lastGroundNormal = hit.normal;
                    _groundCollisionAngle = angle;

                    // Aktualisiere GroundingDetection mit der echten Kollisions-Normal
                    if (_groundingDetection != null)
                    {
                        _groundingDetection.SetGroundNormalFromCollision(hit.normal, angle);
                    }
                }
            }
        }

        private void LateUpdate()
        {
            // Reset ground collision flag am Ende des Frames
            // Wird im nächsten Frame durch OnControllerColliderHit wieder gesetzt
            _hasGroundCollision = false;
        }

        #endregion

        #region Initialization

        private void InitializeTickSystem()
        {
            _tickSystem = new TickSystem(TickSystem.DefaultTickRate);
            _tickSystem.OnTick += OnFixedTick;
        }

        private void InitializeComponents()
        {
            // Get CharacterController
            _characterController = GetComponent<UnityEngine.CharacterController>();
            if (_characterController == null)
            {
                Debug.LogError($"[PlayerController] FEHLER auf '{gameObject.name}': " +
                    "CharacterController-Komponente fehlt! " +
                    "Füge eine CharacterController-Komponente zum GameObject hinzu. " +
                    "(Menü: Component > Physics > Character Controller)");
                enabled = false;
                return;
            }

            // Validate CharacterController settings
            if (_characterController.radius <= 0f)
            {
                Debug.LogError($"[PlayerController] FEHLER auf '{gameObject.name}': " +
                    $"CharacterController.radius ist {_characterController.radius}, muss aber > 0 sein.");
            }

            if (_characterController.height <= 0f)
            {
                Debug.LogError($"[PlayerController] FEHLER auf '{gameObject.name}': " +
                    $"CharacterController.height ist {_characterController.height}, muss aber > 0 sein.");
            }

            // Find Input Provider
            if (_inputProviderComponent != null)
            {
                _inputProvider = _inputProviderComponent as IMovementInputProvider;
                if (_inputProvider == null)
                {
                    Debug.LogWarning($"[PlayerController] WARNUNG auf '{gameObject.name}': " +
                        $"Das zugewiesene Input Provider Component '{_inputProviderComponent.GetType().Name}' " +
                        "implementiert nicht IMovementInputProvider.");
                }
            }

            if (_inputProvider == null)
            {
                _inputProvider = GetComponent<IMovementInputProvider>();
            }

            if (_inputProvider == null)
            {
                Debug.LogWarning($"[PlayerController] WARNUNG auf '{gameObject.name}': " +
                    "Kein Input Provider gefunden. Der Character wird sich nicht bewegen können. " +
                    "Lösungen: " +
                    "(1) Füge PlayerInputProvider für Spieler-Steuerung hinzu, oder " +
                    "(2) Füge AIInputProvider für KI-Steuerung hinzu, oder " +
                    "(3) Weise einen Input Provider im Inspector zu.");
            }

            // Validate config
            if (_config == null)
            {
                Debug.LogError($"[PlayerController] FEHLER auf '{gameObject.name}': " +
                    "Keine MovementConfig zugewiesen! " +
                    "Erstelle ein MovementConfig ScriptableObject (Create > Wiesenwischer > Movement Config) " +
                    "und weise es im Inspector zu.");
                enabled = false;
                return;
            }

            // Validate config values
            ValidateMovementConfig();
        }

        private void ValidateMovementConfig()
        {
            if (_config.WalkSpeed <= 0f)
            {
                Debug.LogWarning($"[PlayerController] WARNUNG: MovementConfig.WalkSpeed ist {_config.WalkSpeed}, sollte > 0 sein.");
            }

            if (_config.Gravity <= 0f)
            {
                Debug.LogWarning($"[PlayerController] WARNUNG: MovementConfig.Gravity ist {_config.Gravity}, sollte > 0 sein.");
            }

            if (_config.MaxSlopeAngle < 0f || _config.MaxSlopeAngle > 90f)
            {
                Debug.LogWarning($"[PlayerController] WARNUNG: MovementConfig.MaxSlopeAngle ist {_config.MaxSlopeAngle}, sollte zwischen 0 und 90 liegen.");
            }

            if (_config.GroundCheckDistance <= 0f)
            {
                Debug.LogWarning($"[PlayerController] WARNUNG: MovementConfig.GroundCheckDistance ist {_config.GroundCheckDistance}, sollte > 0 sein.");
            }

            if (_config.GroundLayers == 0)
            {
                Debug.LogWarning("[PlayerController] WARNUNG: MovementConfig.GroundLayers ist leer. " +
                    "Der Character wird keinen Boden erkennen können.");
            }
        }

        private void InitializeSystems()
        {
            if (_config == null) return;

            // Initialize Ground Detection
            _groundingDetection = new GroundingDetection(
                transform,
                _config,
                _characterController.radius,
                _characterController.height
            );

            // Initialize Movement Simulator
            _movementSimulator = new MovementMotor(
                transform,
                _characterController,
                _config,
                _groundingDetection
            );
        }

        private void InitializeStateMachine()
        {
            // Create states
            _jumpingState = new JumpingState();
            _fallingState = new FallingState();
            _groundedState = new GroundedState(_jumpingState, _fallingState);

            // Set circular references
            _jumpingState.SetStateReferences(_fallingState, _groundedState);
            _fallingState.SetStateReferences(_groundedState, _jumpingState);

            // Create and initialize state machine
            _stateMachine = new CharacterStateMachine();
            _stateMachine.RegisterStates(_groundedState, _jumpingState, _fallingState);
            _stateMachine.Initialize(this, GroundedState.Name);
        }

        #endregion

        #region Update Loop

        private void UpdateInput()
        {
            if (_inputProvider == null) return;

            _inputProvider.UpdateInput();

            _moveInput = _inputProvider.MoveInput;
            _jumpPressed = _inputProvider.JumpPressed;
            _jumpHeld = _inputProvider.JumpHeld;
        }

        private void OnFixedTick(int tick, float deltaTime)
        {
            // Ground Detection wird in MovementMotor.Simulate() aufgerufen
            // um sicherzustellen dass Kollisionsdaten nicht verloren gehen

            // 1. Update State Machine
            _stateMachine?.Update(deltaTime);

            // 2. Apply Movement
            ApplyMovement(deltaTime);
        }

        private void ApplyMovement(float deltaTime)
        {
            if (_movementSimulator == null) return;

            // Create movement input
            var input = new MovementInput
            {
                MoveDirection = _moveInput,
                LookDirection = GetCameraForward(),
                IsSprinting = _inputProvider?.SprintHeld ?? false,
                VerticalVelocity = _verticalVelocity
            };

            // Simulate movement
            _movementSimulator.Simulate(input, deltaTime);

            // Update velocities from simulator
            _verticalVelocity = _movementSimulator.VerticalVelocity;
            _horizontalVelocity = _movementSimulator.HorizontalVelocity;
        }

        /// <summary>
        /// Ermittelt die Forward-Richtung der Kamera für kamera-relative Bewegung.
        /// Fällt auf Character-Forward zurück, wenn keine Kamera verfügbar ist.
        /// </summary>
        private Vector3 GetCameraForward()
        {
            var mainCamera = UnityEngine.Camera.main;
            if (mainCamera != null)
            {
                // Nutze nur die horizontale Komponente der Kamera-Forward
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
            _characterController.enabled = false;
            transform.position = position;
            _characterController.enabled = true;
        }

        /// <summary>
        /// Setzt die Geschwindigkeit zurück.
        /// </summary>
        public void ResetVelocity()
        {
            _verticalVelocity = 0f;
            _horizontalVelocity = Vector3.zero;
        }

        /// <summary>
        /// Erzwingt einen State-Wechsel.
        /// </summary>
        public void ForceState(string stateName)
        {
            _stateMachine?.TransitionTo(stateName, StateTransitionReason.Forced);
        }

        #endregion

        #region Debug

        private void DrawDebugGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.BeginVertical("box");

            GUILayout.Label($"<b>PlayerController Debug</b>");
            GUILayout.Label($"State: {CurrentStateName}");
            GUILayout.Label($"Grounded: {IsGrounded}");
            GUILayout.Label($"Velocity: {Velocity:F2}");
            GUILayout.Label($"H-Velocity: {_horizontalVelocity.magnitude:F2}");
            GUILayout.Label($"V-Velocity: {_verticalVelocity:F2}");
            GUILayout.Label($"Tick: {CurrentTick}");

            if (_groundingDetection != null)
            {
                var gi = _groundingDetection.GroundInfo;
                GUILayout.Label($"Slope: {gi.SlopeAngle:F1}° ({(gi.IsWalkable ? "walkable" : "too steep")})");
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        #endregion
    }
}
