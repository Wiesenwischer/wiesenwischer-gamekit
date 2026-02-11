using UnityEngine;
using UnityEngine.InputSystem;

namespace Wiesenwischer.GameKit.CharacterController.Core.Input
{
    /// <summary>
    /// Input Provider für Spieler-Input.
    /// Arbeitet direkt mit dem InputActionAsset — ohne PlayerInput-Component.
    /// Volle Kontrolle über ActionMap-Lifecycle (Enable/Disable).
    /// </summary>
    public class PlayerInputProvider : MonoBehaviour, IMovementInputProvider
    {
        [Header("Input Settings")]
        [SerializeField] private bool _isActive = true;

        [Header("Input Actions")]
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private string _actionMapName = "Player";

        private InputActionMap _actionMap;
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _jumpAction;
        private InputAction _sprintAction;
        private InputAction _dashAction;
        private InputAction _walkToggleAction;

        private bool _jumpStarted;
        private bool _dashStarted;
        private bool _walkToggleStarted;

        #region IMovementInputProvider

        public Vector2 MoveInput => _isActive && _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
        public Vector2 LookInput => _isActive && _lookAction != null ? _lookAction.ReadValue<Vector2>() : Vector2.zero;

        public bool JumpPressed
        {
            get
            {
                if (!_isActive || !_jumpStarted) return false;
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

        public bool WalkTogglePressed
        {
            get
            {
                if (!_isActive || !_walkToggleStarted) return false;
                _walkToggleStarted = false;
                return true;
            }
        }

        public bool IsActive => _isActive && enabled;

        public void UpdateInput()
        {
        }

        public void ResetInput()
        {
            _jumpStarted = false;
            _dashStarted = false;
            _walkToggleStarted = false;
        }

        #endregion

        #region Unity Callbacks

        private void OnEnable()
        {
            if (_inputActions == null)
            {
                Debug.LogError($"[PlayerInputProvider] InputActionAsset fehlt auf '{gameObject.name}'!");
                enabled = false;
                return;
            }

            _actionMap = _inputActions.FindActionMap(_actionMapName);
            if (_actionMap == null)
            {
                Debug.LogError($"[PlayerInputProvider] ActionMap '{_actionMapName}' nicht gefunden!");
                enabled = false;
                return;
            }

            _moveAction = _actionMap.FindAction("Move");
            _lookAction = _actionMap.FindAction("Look");
            _jumpAction = _actionMap.FindAction("Jump");
            _sprintAction = _actionMap.FindAction("Sprint");
            _dashAction = _actionMap.FindAction("Dash");
            _walkToggleAction = _actionMap.FindAction("WalkToggle");

            if (_moveAction == null)
                Debug.LogError($"[PlayerInputProvider] Move-Action nicht in ActionMap '{_actionMapName}'!");

            if (_jumpAction != null) _jumpAction.started += OnJumpStarted;
            if (_dashAction != null) _dashAction.started += OnDashStarted;
            if (_walkToggleAction != null) _walkToggleAction.started += OnWalkToggleStarted;

            _actionMap.Enable();
        }

        private void OnDisable()
        {
            if (_jumpAction != null) _jumpAction.started -= OnJumpStarted;
            if (_dashAction != null) _dashAction.started -= OnDashStarted;
            if (_walkToggleAction != null) _walkToggleAction.started -= OnWalkToggleStarted;

            _actionMap?.Disable();
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

        private void OnWalkToggleStarted(InputAction.CallbackContext context)
        {
            _walkToggleStarted = true;
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
            if (_walkToggleStarted) buttons |= InputButtons.Walk;

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
