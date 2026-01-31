using UnityEngine;
using UnityEngine.InputSystem;

namespace Wiesenwischer.GameKit.CharacterController.Core.Input
{
    /// <summary>
    /// Input Provider für Spieler-Input.
    /// Verwendet das Unity Input System.
    /// </summary>
    public class PlayerInputProvider : MonoBehaviour, IMovementInputProvider
    {
        [Header("Input Settings")]
        [Tooltip("Ob dieser Input Provider aktiv ist")]
        [SerializeField] private bool _isActive = true;

        [Header("Input System References")]
        [Tooltip("Referenz zum PlayerInput Component (optional - wird automatisch gesucht)")]
        [SerializeField] private PlayerInput _playerInput;

        [Header("Action Names")]
        [SerializeField] private string _moveActionName = "Move";
        [SerializeField] private string _lookActionName = "Look";
        [SerializeField] private string _jumpActionName = "Jump";
        [SerializeField] private string _sprintActionName = "Sprint";
        [SerializeField] private string _dashActionName = "Dash";

        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _jumpAction;
        private InputAction _sprintAction;
        private InputAction _dashAction;

        // Cached Input Values
        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private bool _jumpPressed;
        private bool _jumpHeld;
        private bool _sprintHeld;
        private bool _dashPressed;

        #region IMovementInputProvider Implementation

        public Vector2 MoveInput => _moveInput;
        public Vector2 LookInput => _lookInput;
        public bool JumpPressed => _jumpPressed;
        public bool JumpHeld => _jumpHeld;
        public bool SprintHeld => _sprintHeld;
        public bool DashPressed => _dashPressed;
        public bool IsActive => _isActive && enabled;

        public void UpdateInput()
        {
            if (!IsActive)
            {
                ResetInput();
                return;
            }

            UpdateInputSystem();
        }

        public void ResetInput()
        {
            _moveInput = Vector2.zero;
            _lookInput = Vector2.zero;
            _jumpPressed = false;
            _jumpHeld = false;
            _sprintHeld = false;
            _dashPressed = false;
        }

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            InitializeInputSystem();
        }

        private void OnEnable()
        {
            EnableInputActions();
        }

        private void OnDisable()
        {
            DisableInputActions();
        }

        private void LateUpdate()
        {
            // Reset "pressed" flags nach dem Frame
            _jumpPressed = false;
            _dashPressed = false;
        }

        #endregion

        #region Input System

        private void InitializeInputSystem()
        {
            // Versuche PlayerInput zu finden, falls nicht zugewiesen
            if (_playerInput == null)
            {
                _playerInput = GetComponent<PlayerInput>();
            }

            if (_playerInput != null)
            {
                // Hole Actions vom PlayerInput
                _moveAction = _playerInput.actions?.FindAction(_moveActionName);
                _lookAction = _playerInput.actions?.FindAction(_lookActionName);
                _jumpAction = _playerInput.actions?.FindAction(_jumpActionName);
                _sprintAction = _playerInput.actions?.FindAction(_sprintActionName);
                _dashAction = _playerInput.actions?.FindAction(_dashActionName);
            }
            else
            {
                Debug.LogWarning("[PlayerInputProvider] Kein PlayerInput Component gefunden. " +
                               "Bitte füge ein PlayerInput Component hinzu.");
            }
        }

        private void EnableInputActions()
        {
            // Subscribe to button events for pressed detection
            if (_jumpAction != null)
            {
                _jumpAction.performed += OnJumpPerformed;
                _jumpAction.canceled += OnJumpCanceled;
            }

            if (_dashAction != null)
            {
                _dashAction.performed += OnDashPerformed;
            }
        }

        private void DisableInputActions()
        {
            if (_jumpAction != null)
            {
                _jumpAction.performed -= OnJumpPerformed;
                _jumpAction.canceled -= OnJumpCanceled;
            }

            if (_dashAction != null)
            {
                _dashAction.performed -= OnDashPerformed;
            }
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            _jumpPressed = true;
            _jumpHeld = true;
        }

        private void OnJumpCanceled(InputAction.CallbackContext context)
        {
            _jumpHeld = false;
        }

        private void OnDashPerformed(InputAction.CallbackContext context)
        {
            _dashPressed = true;
        }

        private void UpdateInputSystem()
        {
            // Read continuous values
            if (_moveAction != null)
            {
                _moveInput = _moveAction.ReadValue<Vector2>();
            }

            if (_lookAction != null)
            {
                _lookInput = _lookAction.ReadValue<Vector2>();
            }

            if (_sprintAction != null)
            {
                _sprintHeld = _sprintAction.IsPressed();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Aktiviert oder deaktiviert den Input Provider.
        /// </summary>
        public void SetActive(bool active)
        {
            _isActive = active;
            if (!active)
            {
                ResetInput();
            }
        }

        /// <summary>
        /// Erstellt einen InputSnapshot für den aktuellen Frame.
        /// </summary>
        public InputSnapshot CreateSnapshot(int tick)
        {
            InputButtons buttons = InputButtons.None;

            if (_jumpPressed || _jumpHeld) buttons |= InputButtons.Jump;
            if (_sprintHeld) buttons |= InputButtons.Sprint;
            if (_dashPressed) buttons |= InputButtons.Dash;

            return new InputSnapshot
            {
                Tick = tick,
                MoveInput = _moveInput,
                LookInput = _lookInput,
                Buttons = buttons,
                Timestamp = Time.time
            };
        }

        #endregion
    }
}
