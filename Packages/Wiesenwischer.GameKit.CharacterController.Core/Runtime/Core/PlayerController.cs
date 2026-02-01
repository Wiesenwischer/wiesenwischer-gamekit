using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Input;
using Wiesenwischer.GameKit.CharacterController.Core.Locomotion;
using Wiesenwischer.GameKit.CharacterController.Core.Motor;
using Wiesenwischer.GameKit.CharacterController.Core.StateMachine;
using Wiesenwischer.GameKit.CharacterController.Core.StateMachine.States;

namespace Wiesenwischer.GameKit.CharacterController.Core
{
    /// <summary>
    /// Hauptkomponente für Character Controller.
    /// Verwendet kinematische Physik mit CapsuleCollider (kein Unity CharacterController).
    /// Integriert Input, Locomotion, State Machine und Prediction.
    /// CSP (Client-Side Prediction) kompatibel für MMO-Nutzung.
    /// </summary>
    [RequireComponent(typeof(CapsuleCollider))]
    public class PlayerController : MonoBehaviour, IStateMachineContext
    {
        [Header("Configuration")]
        [Tooltip("Locomotion-Konfiguration")]
        [SerializeField] private LocomotionConfig _config;

        [Header("Capsule Settings")]
        [Tooltip("Skin Width für Kollisionserkennung (ähnlich CharacterController.skinWidth)")]
        [SerializeField] private float _skinWidth = 0.02f;

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
        private CapsuleCollider _capsuleCollider;
        private IMovementInputProvider _inputProvider;

        // Systems
        private GroundingDetection _groundingDetection;
        private CharacterLocomotion _locomotion;
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

        #region IStateMachineContext Implementation

        public Vector2 MoveInput => _moveInput;
        public bool JumpPressed => _jumpPressed;
        public bool JumpHeld => _jumpHeld;
        public bool IsGrounded => _groundingDetection?.IsGrounded ?? false;
        public float VerticalVelocity { get => _verticalVelocity; set => _verticalVelocity = value; }
        public Vector3 HorizontalVelocity { get => _horizontalVelocity; set => _horizontalVelocity = value; }
        public ILocomotionConfig Config => _config;
        public int CurrentTick => _tickSystem?.CurrentTick ?? 0;

        #endregion

        #region Public Properties

        /// <summary>Der aktuelle State-Name.</summary>
        public string CurrentStateName => _stateMachine?.CurrentStateName ?? "None";

        /// <summary>Die aktuelle Geschwindigkeit.</summary>
        public Vector3 Velocity => _horizontalVelocity + Vector3.up * _verticalVelocity;

        /// <summary>Der CapsuleCollider für Kollisionserkennung.</summary>
        public CapsuleCollider CapsuleCollider => _capsuleCollider;

        /// <summary>Der kinematische Motor.</summary>
        public KinematicMotor KinematicMotor => _locomotion?.Motor;

        /// <summary>Die Locomotion-Konfiguration.</summary>
        public LocomotionConfig LocomotionConfig => _config;

        /// <summary>Ground Detection System.</summary>
        public GroundingDetection GroundingDetection => _groundingDetection;

        /// <summary>Die State Machine.</summary>
        public CharacterStateMachine StateMachine => _stateMachine;

        /// <summary>Das Tick-System.</summary>
        public TickSystem TickSystem => _tickSystem;

        /// <summary>Ob der Character gerade rutscht.</summary>
        public bool IsSliding => _locomotion?.IsSliding ?? false;

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
            UpdateInput();
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
            _locomotion?.DrawDebugGizmos();
        }

        private void OnGUI()
        {
            if (!_showDebugInfo || !Application.isPlaying) return;
            DrawDebugGUI();
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
            // Get CapsuleCollider
            _capsuleCollider = GetComponent<CapsuleCollider>();
            if (_capsuleCollider == null)
            {
                Debug.LogError($"[PlayerController] FEHLER auf '{gameObject.name}': " +
                    "CapsuleCollider-Komponente fehlt! " +
                    "Füge eine CapsuleCollider-Komponente zum GameObject hinzu.");
                enabled = false;
                return;
            }

            // Validate CapsuleCollider settings
            if (_capsuleCollider.radius <= 0f)
            {
                Debug.LogError($"[PlayerController] FEHLER auf '{gameObject.name}': " +
                    $"CapsuleCollider.radius ist {_capsuleCollider.radius}, muss aber > 0 sein.");
            }

            if (_capsuleCollider.height <= 0f)
            {
                Debug.LogError($"[PlayerController] FEHLER auf '{gameObject.name}': " +
                    $"CapsuleCollider.height ist {_capsuleCollider.height}, muss aber > 0 sein.");
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
                    "Keine LocomotionConfig zugewiesen! " +
                    "Erstelle ein LocomotionConfig ScriptableObject (Create > Wiesenwischer > GameKit > Locomotion Config) " +
                    "und weise es im Inspector zu.");
                enabled = false;
                return;
            }

            ValidateLocomotionConfig();
        }

        private void ValidateLocomotionConfig()
        {
            if (_config.WalkSpeed <= 0f)
            {
                Debug.LogWarning($"[PlayerController] WARNUNG: LocomotionConfig.WalkSpeed ist {_config.WalkSpeed}, sollte > 0 sein.");
            }

            if (_config.Gravity <= 0f)
            {
                Debug.LogWarning($"[PlayerController] WARNUNG: LocomotionConfig.Gravity ist {_config.Gravity}, sollte > 0 sein.");
            }

            if (_config.MaxSlopeAngle < 0f || _config.MaxSlopeAngle > 90f)
            {
                Debug.LogWarning($"[PlayerController] WARNUNG: LocomotionConfig.MaxSlopeAngle ist {_config.MaxSlopeAngle}, sollte zwischen 0 und 90 liegen.");
            }

            if (_config.GroundCheckDistance <= 0f)
            {
                Debug.LogWarning($"[PlayerController] WARNUNG: LocomotionConfig.GroundCheckDistance ist {_config.GroundCheckDistance}, sollte > 0 sein.");
            }

            if (_config.GroundLayers == 0)
            {
                Debug.LogWarning("[PlayerController] WARNUNG: LocomotionConfig.GroundLayers ist leer. " +
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
                _capsuleCollider.radius,
                _capsuleCollider.height
            );

            // Initialize Character Locomotion with KinematicMotor
            _locomotion = new CharacterLocomotion(
                transform,
                _capsuleCollider,
                _config,
                _groundingDetection,
                _skinWidth
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
            // 1. Update State Machine
            _stateMachine?.Update(deltaTime);

            // 2. Apply Movement
            ApplyMovement(deltaTime);
        }

        private void ApplyMovement(float deltaTime)
        {
            if (_locomotion == null) return;

            // Create locomotion input
            var input = new LocomotionInput
            {
                MoveDirection = _moveInput,
                LookDirection = GetCameraForward(),
                IsSprinting = _inputProvider?.SprintHeld ?? false,
                VerticalVelocity = _verticalVelocity
            };

            // Simulate locomotion
            _locomotion.Simulate(input, deltaTime);

            // Update velocities from locomotion
            _verticalVelocity = _locomotion.VerticalVelocity;
            _horizontalVelocity = _locomotion.HorizontalVelocity;
        }

        /// <summary>
        /// Ermittelt die Forward-Richtung der Kamera für kamera-relative Bewegung.
        /// </summary>
        private Vector3 GetCameraForward()
        {
            var mainCamera = UnityEngine.Camera.main;
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
            _locomotion?.Motor?.SetPosition(position);
        }

        /// <summary>
        /// Setzt die Geschwindigkeit zurück.
        /// </summary>
        public void ResetVelocity()
        {
            _verticalVelocity = 0f;
            _horizontalVelocity = Vector3.zero;
            _locomotion?.StopMovement();
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
            GUILayout.BeginArea(new Rect(10, 10, 350, 250));
            GUILayout.BeginVertical("box");

            GUILayout.Label($"<b>PlayerController Debug (Kinematic)</b>");
            GUILayout.Label($"State: {CurrentStateName}");
            GUILayout.Label($"Grounded: {IsGrounded}");
            GUILayout.Label($"Sliding: {IsSliding}");
            GUILayout.Label($"Velocity: {Velocity:F2}");
            GUILayout.Label($"H-Velocity: {_horizontalVelocity.magnitude:F2}");
            GUILayout.Label($"V-Velocity: {_verticalVelocity:F2}");
            GUILayout.Label($"Tick: {CurrentTick}");

            if (_groundingDetection != null)
            {
                var gi = _groundingDetection.GroundInfo;
                GUILayout.Label($"Slope: {gi.SlopeAngle:F1}° ({(gi.IsWalkable ? "walkable" : "too steep")})");
                GUILayout.Label($"MaxSlope: {_config.MaxSlopeAngle:F1}°");
            }

            GUILayout.Label($"<i>CSP-Ready: Kinematic Physics</i>");

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        #endregion
    }
}
