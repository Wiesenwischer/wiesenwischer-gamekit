using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Core.Input
{
    /// <summary>
    /// Input Provider für KI-gesteuerte Characters oder Tests.
    /// Ermöglicht programmatisches Setzen von Input-Werten.
    /// </summary>
    public class AIInputProvider : MonoBehaviour, IMovementInputProvider
    {
        [Header("AI Input Settings")]
        [Tooltip("Ob dieser Input Provider aktiv ist")]
        [SerializeField] private bool _isActive = true;

        // Input Values (können von außen gesetzt werden)
        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private bool _jumpPressed;
        private bool _jumpHeld;
        private bool _sprintHeld;
        private bool _dashPressed;
        private bool _walkTogglePressed;

        // Zielposition für einfache Navigation
        private Vector3? _targetPosition;
        private Transform _targetTransform;

        #region IMovementInputProvider Implementation

        public Vector2 MoveInput => _moveInput;
        public Vector2 LookInput => _lookInput;
        public bool JumpPressed => _jumpPressed;
        public bool JumpHeld => _jumpHeld;
        public bool SprintHeld => _sprintHeld;
        public bool DashPressed => _dashPressed;
        public bool WalkTogglePressed => _walkTogglePressed;
        public bool IsActive => _isActive && enabled;

        public void UpdateInput()
        {
            if (!IsActive)
            {
                ResetInput();
                return;
            }

            // Automatische Navigation zum Ziel (falls gesetzt)
            UpdateNavigationInput();
        }

        public void ResetInput()
        {
            _moveInput = Vector2.zero;
            _lookInput = Vector2.zero;
            _jumpPressed = false;
            _jumpHeld = false;
            _sprintHeld = false;
            _dashPressed = false;
            _walkTogglePressed = false;
        }

        #endregion

        #region Input Setters (für KI/Tests)

        /// <summary>
        /// Setzt den Movement Input direkt.
        /// </summary>
        public void SetMoveInput(Vector2 input)
        {
            _moveInput = Vector2.ClampMagnitude(input, 1f);
        }

        /// <summary>
        /// Setzt den Movement Input von einer Weltrichtung.
        /// </summary>
        public void SetMoveDirection(Vector3 worldDirection)
        {
            // Konvertiere Weltrichtung zu lokalem Input
            Vector3 localDir = transform.InverseTransformDirection(worldDirection);
            _moveInput = new Vector2(localDir.x, localDir.z).normalized;
        }

        /// <summary>
        /// Setzt den Look Input direkt.
        /// </summary>
        public void SetLookInput(Vector2 input)
        {
            _lookInput = input;
        }

        /// <summary>
        /// Löst einen Sprung aus.
        /// </summary>
        public void TriggerJump()
        {
            _jumpPressed = true;
            _jumpHeld = true;
        }

        /// <summary>
        /// Beendet den Sprung (Button losgelassen).
        /// </summary>
        public void ReleaseJump()
        {
            _jumpHeld = false;
        }

        /// <summary>
        /// Setzt den Sprint-Status.
        /// </summary>
        public void SetSprint(bool sprinting)
        {
            _sprintHeld = sprinting;
        }

        /// <summary>
        /// Löst einen Dash aus.
        /// </summary>
        public void TriggerDash()
        {
            _dashPressed = true;
        }

        /// <summary>
        /// Aktiviert oder deaktiviert den Input Provider.
        /// </summary>
        public void SetActive(bool active)
        {
            _isActive = active;
            if (!active)
            {
                ResetInput();
                ClearTarget();
            }
        }

        #endregion

        #region Navigation

        /// <summary>
        /// Setzt ein Ziel für automatische Navigation.
        /// </summary>
        public void SetTargetPosition(Vector3 position)
        {
            _targetPosition = position;
            _targetTransform = null;
        }

        /// <summary>
        /// Setzt ein Transform als Ziel für automatische Navigation.
        /// </summary>
        public void SetTargetTransform(Transform target)
        {
            _targetTransform = target;
            _targetPosition = null;
        }

        /// <summary>
        /// Entfernt das aktuelle Navigationsziel.
        /// </summary>
        public void ClearTarget()
        {
            _targetPosition = null;
            _targetTransform = null;
        }

        /// <summary>
        /// Prüft ob das Ziel erreicht wurde.
        /// </summary>
        public bool HasReachedTarget(float threshold = 0.5f)
        {
            Vector3? target = GetTargetPosition();
            if (!target.HasValue) return true;

            float distance = Vector3.Distance(transform.position, target.Value);
            return distance <= threshold;
        }

        private Vector3? GetTargetPosition()
        {
            if (_targetTransform != null)
            {
                return _targetTransform.position;
            }
            return _targetPosition;
        }

        private void UpdateNavigationInput()
        {
            Vector3? target = GetTargetPosition();
            if (!target.HasValue)
            {
                return;
            }

            // Berechne Richtung zum Ziel
            Vector3 direction = target.Value - transform.position;
            direction.y = 0; // Ignoriere Höhe

            if (direction.sqrMagnitude > 0.01f)
            {
                SetMoveDirection(direction.normalized);
            }
            else
            {
                _moveInput = Vector2.zero;
            }
        }

        #endregion

        #region Unity Callbacks

        private void LateUpdate()
        {
            // Reset "pressed" flags nach dem Frame
            _jumpPressed = false;
            _dashPressed = false;
            _walkTogglePressed = false;
        }

        #endregion

        #region Utility

        /// <summary>
        /// Erstellt einen InputSnapshot für den aktuellen Frame.
        /// </summary>
        public InputSnapshot CreateSnapshot(int tick)
        {
            InputButtons buttons = InputButtons.None;

            if (_jumpPressed || _jumpHeld) buttons |= InputButtons.Jump;
            if (_sprintHeld) buttons |= InputButtons.Sprint;
            if (_dashPressed) buttons |= InputButtons.Dash;
            if (_walkTogglePressed) buttons |= InputButtons.Walk;

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
