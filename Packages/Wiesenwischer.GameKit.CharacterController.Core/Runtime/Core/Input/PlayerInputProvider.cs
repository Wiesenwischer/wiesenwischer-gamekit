using UnityEngine;
using UnityEngine.InputSystem;

namespace Wiesenwischer.GameKit.CharacterController.Core.Input
{
    /// <summary>
    /// Input Provider für Spieler-Input.
    /// Verwendet Event-Callbacks wie Genshin Impact.
    /// </summary>
    public class PlayerInputProvider : MonoBehaviour, IMovementInputProvider
    {
        [Header("Input Settings")]
        [SerializeField] private bool _isActive = true;

        [Header("Input System References")]
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

        // Flags die bei "started" gesetzt werden
        private bool _jumpStarted;
        private bool _dashStarted;

        #region IMovementInputProvider Implementation

        public Vector2 MoveInput => _isActive && _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
        public Vector2 LookInput => _isActive && _lookAction != null ? _lookAction.ReadValue<Vector2>() : Vector2.zero;

        public bool JumpPressed
        {
            get
            {
                if (!_isActive || !_jumpStarted) return false;
                // Einmal konsumieren, dann false
                _jumpStarted = false;
                return true;
            }
        }

        public bool JumpHeld => _isActive && _jumpAction != null && _jumpAction.IsPressed();
        public bool SprintHeld => _isActive && _sprintAction != null && _sprintAction.IsPressed();

        public bool DashPressed
        {
            get
            {
                if (!_isActive || !_dashStarted) return false;
                _dashStarted = false;
                return true;
            }
        }

        public bool IsActive => _isActive && enabled;

        public void UpdateInput()
        {
            // Nichts zu tun - Events setzen die Flags
        }

        public void ResetInput()
        {
            _jumpStarted = false;
            _dashStarted = false;
        }

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            // Nur Referenz auf PlayerInput finden, Actions noch nicht auflösen
            // (PlayerInput aktiviert Actions erst in OnEnable)
            if (_playerInput == null)
            {
                _playerInput = GetComponent<PlayerInput>();
            }

            if (_playerInput == null)
            {
                Debug.LogError($"[PlayerInputProvider] PlayerInput-Komponente fehlt auf '{gameObject.name}'!");
                enabled = false;
            }
        }

        private void Start()
        {
            // Actions erst in Start() auflösen, damit PlayerInput.OnEnable() bereits gelaufen ist
            InitializeInputSystem();
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        #endregion

        #region Event Callbacks

        private void OnJumpStarted(InputAction.CallbackContext context)
        {
            _jumpStarted = true;
        }

        private void OnDashStarted(InputAction.CallbackContext context)
        {
            _dashStarted = true;
        }

        private void SubscribeEvents()
        {
            if (_jumpAction != null) _jumpAction.started += OnJumpStarted;
            if (_dashAction != null) _dashAction.started += OnDashStarted;
        }

        private void UnsubscribeEvents()
        {
            if (_jumpAction != null) _jumpAction.started -= OnJumpStarted;
            if (_dashAction != null) _dashAction.started -= OnDashStarted;
        }

        #endregion

        #region Input System

        private void InitializeInputSystem()
        {
            if (_playerInput == null || _playerInput.actions == null)
            {
                Debug.LogError($"[PlayerInputProvider] PlayerInput oder Actions Asset fehlt auf '{gameObject.name}'!");
                enabled = false;
                return;
            }

            _moveAction = _playerInput.actions.FindAction(_moveActionName);
            _lookAction = _playerInput.actions.FindAction(_lookActionName);
            _jumpAction = _playerInput.actions.FindAction(_jumpActionName);
            _sprintAction = _playerInput.actions.FindAction(_sprintActionName);
            _dashAction = _playerInput.actions.FindAction(_dashActionName);

            if (_moveAction == null)
            {
                Debug.LogError($"[PlayerInputProvider] Move-Action '{_moveActionName}' nicht gefunden!");
            }

            // Event-Callbacks registrieren (OnEnable lief vor Start, Actions waren null)
            SubscribeEvents();
        }

        #endregion

        #region Public Methods

        public void SetActive(bool active)
        {
            _isActive = active;
            if (!active) ResetInput();
        }

        public InputSnapshot CreateSnapshot(int tick)
        {
            InputButtons buttons = InputButtons.None;

            if (_jumpStarted || JumpHeld) buttons |= InputButtons.Jump;
            if (SprintHeld) buttons |= InputButtons.Sprint;
            if (_dashStarted) buttons |= InputButtons.Dash;

            return new InputSnapshot
            {
                Tick = tick,
                MoveInput = MoveInput,
                LookInput = LookInput,
                Buttons = buttons,
                Timestamp = Time.time
            };
        }

        #endregion
    }
}
