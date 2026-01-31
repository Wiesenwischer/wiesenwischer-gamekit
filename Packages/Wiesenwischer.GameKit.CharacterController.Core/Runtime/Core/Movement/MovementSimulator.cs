using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.StateMachine;

namespace Wiesenwischer.GameKit.CharacterController.Core.Movement
{
    /// <summary>
    /// Deterministischer Movement Simulator.
    /// Berechnet Bewegung basierend auf Input und Config.
    /// Verwendet festes Delta für CSP-Kompatibilität.
    /// </summary>
    public class MovementSimulator : IMovementController
    {
        private readonly UnityEngine.CharacterController _characterController;
        private readonly Transform _transform;
        private readonly IMovementConfig _config;
        private readonly GroundingDetection _groundingDetection;

        // State
        private Vector3 _velocity;
        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;
        private bool _isGrounded;

        // Rotation
        private float _targetYaw;
        private float _currentYaw;

        /// <summary>
        /// Erstellt einen neuen MovementSimulator.
        /// </summary>
        public MovementSimulator(
            UnityEngine.CharacterController characterController,
            IMovementConfig config)
        {
            _characterController = characterController;
            _transform = characterController.transform;
            _config = config;

            _groundingDetection = new GroundingDetection(
                _transform,
                config,
                characterController.radius,
                characterController.height
            );

            _currentYaw = _transform.eulerAngles.y;
            _targetYaw = _currentYaw;
        }

        /// <summary>
        /// Erstellt einen neuen MovementSimulator mit externer GroundingDetection.
        /// </summary>
        public MovementSimulator(
            Transform transform,
            UnityEngine.CharacterController characterController,
            IMovementConfig config,
            GroundingDetection groundingDetection)
        {
            _characterController = characterController;
            _transform = transform;
            _config = config;
            _groundingDetection = groundingDetection;

            _currentYaw = _transform.eulerAngles.y;
            _targetYaw = _currentYaw;
        }

        #region IMovementController Implementation

        public Vector3 Position => _transform.position;
        public Quaternion Rotation => _transform.rotation;
        public Vector3 Velocity => _velocity;
        public bool IsGrounded => _groundingDetection.IsGrounded;
        public GroundInfo GroundInfo => _groundingDetection.GroundInfo;

        public void Simulate(MovementInput input, float deltaTime)
        {
            // 1. Ground Check
            _groundingDetection.UpdateGroundCheck();
            _isGrounded = _groundingDetection.IsGrounded;

            // 2. Horizontale Bewegung berechnen
            Vector3 targetHorizontalVelocity = CalculateTargetHorizontalVelocity(input);
            _horizontalVelocity = ApplyAcceleration(_horizontalVelocity, targetHorizontalVelocity, deltaTime);

            // 3. Vertikale Bewegung (Gravity + Jump)
            _verticalVelocity = CalculateVerticalVelocity(input.VerticalVelocity, deltaTime);

            // 4. Slope Handling
            Vector3 moveDirection = _horizontalVelocity;
            if (_isGrounded && _groundingDetection.GroundInfo.IsWalkable)
            {
                moveDirection = _groundingDetection.GetSlopeDirection(_horizontalVelocity);
                // Behalte die Magnitude
                moveDirection = moveDirection.normalized * _horizontalVelocity.magnitude;
            }

            // 5. Step Detection und Handling
            if (_isGrounded && moveDirection.sqrMagnitude > 0.01f)
            {
                if (_groundingDetection.CheckForStep(moveDirection, out float stepHeight))
                {
                    // Füge vertikale Komponente für Step-Up hinzu
                    _verticalVelocity = Mathf.Max(_verticalVelocity, stepHeight / deltaTime * 0.5f);
                }
            }

            // 6. Finale Velocity zusammensetzen
            _velocity = moveDirection + Vector3.up * _verticalVelocity;

            // 7. Character bewegen
            _characterController.Move(_velocity * deltaTime);

            // 8. Rotation
            if (_config.RotateTowardsMovement && input.MoveDirection.sqrMagnitude > 0.01f)
            {
                UpdateRotation(input.LookDirection, deltaTime);
            }

            // 9. Snap to Ground wenn geerdet
            if (_isGrounded && _verticalVelocity <= 0)
            {
                SnapToGround();
            }
        }

        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            // Deaktiviere CharacterController temporär für Teleport
            _characterController.enabled = false;
            _transform.position = position;
            _transform.rotation = rotation;
            _characterController.enabled = true;

            _currentYaw = rotation.eulerAngles.y;
            _targetYaw = _currentYaw;
        }

        public void ApplyVelocity(Vector3 velocity)
        {
            _horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
            _verticalVelocity = velocity.y;
            _velocity = velocity;
        }

        #endregion

        #region Movement Calculations

        private Vector3 CalculateTargetHorizontalVelocity(MovementInput input)
        {
            // Konvertiere Input zu Weltrichtung
            Vector3 inputDirection = new Vector3(input.MoveDirection.x, 0, input.MoveDirection.y);

            // Transformiere relativ zur Kamera/Character-Ausrichtung
            if (input.LookDirection.sqrMagnitude > 0.01f)
            {
                // Rotiere Input relativ zur Look-Direction
                Quaternion lookRotation = Quaternion.LookRotation(
                    new Vector3(input.LookDirection.x, 0, input.LookDirection.z).normalized,
                    Vector3.up
                );
                inputDirection = lookRotation * inputDirection;
            }
            else
            {
                // Fallback: Relativ zum Character
                inputDirection = _transform.TransformDirection(inputDirection);
            }

            // Bestimme Geschwindigkeit (Walk/Run)
            float targetSpeed = input.IsSprinting ? _config.RunSpeed : _config.WalkSpeed;

            // In der Luft: Reduzierte Kontrolle
            if (!_isGrounded)
            {
                targetSpeed *= _config.AirControl;
            }

            return inputDirection.normalized * targetSpeed * inputDirection.magnitude;
        }

        private Vector3 ApplyAcceleration(Vector3 currentVelocity, Vector3 targetVelocity, float deltaTime)
        {
            // Bestimme ob wir beschleunigen oder bremsen
            float currentSpeed = currentVelocity.magnitude;
            float targetSpeed = targetVelocity.magnitude;

            float acceleration;
            if (targetSpeed > currentSpeed || targetVelocity.sqrMagnitude > 0.01f)
            {
                // Beschleunigen
                acceleration = _config.Acceleration;
            }
            else
            {
                // Bremsen
                acceleration = _config.Deceleration;
            }

            // In der Luft: Reduzierte Beschleunigung
            if (!_isGrounded)
            {
                acceleration *= _config.AirControl;
            }

            // Interpoliere zur Zielgeschwindigkeit
            return Vector3.MoveTowards(currentVelocity, targetVelocity, acceleration * deltaTime);
        }

        private float CalculateVerticalVelocity(float inputVerticalVelocity, float deltaTime)
        {
            float velocity = inputVerticalVelocity;

            // Wenn geerdet und nicht springend, setze vertikale Velocity auf kleinen negativen Wert
            // (um am Boden zu bleiben)
            if (_isGrounded && velocity <= 0)
            {
                return -2f; // Kleine Kraft nach unten
            }

            // Gravity anwenden
            velocity -= _config.Gravity * deltaTime;

            // Max Fall Speed begrenzen
            velocity = Mathf.Max(velocity, -_config.MaxFallSpeed);

            return velocity;
        }

        #endregion

        #region Rotation

        private void UpdateRotation(Vector3 lookDirection, float deltaTime)
        {
            if (_horizontalVelocity.sqrMagnitude < 0.01f) return;

            // Zielrichtung basierend auf Bewegung
            Vector3 targetDirection = _horizontalVelocity.normalized;
            _targetYaw = Mathf.Atan2(targetDirection.x, targetDirection.z) * Mathf.Rad2Deg;

            // Smooth Rotation
            _currentYaw = Mathf.MoveTowardsAngle(
                _currentYaw,
                _targetYaw,
                _config.RotationSpeed * deltaTime
            );

            _transform.rotation = Quaternion.Euler(0, _currentYaw, 0);
        }

        /// <summary>
        /// Setzt die Rotation direkt (z.B. für Kamera-basierte Rotation).
        /// </summary>
        public void SetRotation(float yaw)
        {
            _currentYaw = yaw;
            _targetYaw = yaw;
            _transform.rotation = Quaternion.Euler(0, yaw, 0);
        }

        /// <summary>
        /// Rotiert den Character zur angegebenen Richtung.
        /// </summary>
        public void RotateTowards(Vector3 direction, float deltaTime)
        {
            if (direction.sqrMagnitude < 0.01f) return;

            _targetYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            _currentYaw = Mathf.MoveTowardsAngle(
                _currentYaw,
                _targetYaw,
                _config.RotationSpeed * deltaTime
            );
            _transform.rotation = Quaternion.Euler(0, _currentYaw, 0);
        }

        #endregion

        #region Utility

        private void SnapToGround()
        {
            // Nur wenn wir leicht über dem Boden schweben
            if (_groundingDetection.GroundInfo.Distance > 0.01f &&
                _groundingDetection.GroundInfo.Distance < _config.GroundCheckDistance)
            {
                Vector3 snapPosition = _transform.position;
                snapPosition.y = _groundingDetection.GroundInfo.Point.y;

                _characterController.enabled = false;
                _transform.position = snapPosition;
                _characterController.enabled = true;
            }
        }

        /// <summary>
        /// Berechnet die Jump-Velocity basierend auf der Config.
        /// </summary>
        public float GetJumpVelocity()
        {
            // v = sqrt(2 * g * h) oder aus Config berechnet
            if (_config is MovementConfig movementConfig)
            {
                return movementConfig.CalculateJumpVelocity();
            }

            // Fallback: Standard-Formel
            return Mathf.Sqrt(2f * _config.Gravity * _config.JumpHeight);
        }

        /// <summary>
        /// Gibt die aktuelle horizontale Geschwindigkeit zurück.
        /// </summary>
        public Vector3 GetHorizontalVelocity() => _horizontalVelocity;

        /// <summary>
        /// Gibt die aktuelle vertikale Geschwindigkeit zurück.
        /// </summary>
        public float GetVerticalVelocity() => _verticalVelocity;

        /// <summary>
        /// Aktuelle horizontale Geschwindigkeit (Property).
        /// </summary>
        public Vector3 HorizontalVelocity => _horizontalVelocity;

        /// <summary>
        /// Aktuelle vertikale Geschwindigkeit (Property).
        /// </summary>
        public float VerticalVelocity => _verticalVelocity;

        /// <summary>
        /// Setzt die vertikale Geschwindigkeit (z.B. für Jump).
        /// </summary>
        public void SetVerticalVelocity(float velocity)
        {
            _verticalVelocity = velocity;
        }

        /// <summary>
        /// Stoppt alle Bewegung sofort.
        /// </summary>
        public void StopMovement()
        {
            _velocity = Vector3.zero;
            _horizontalVelocity = Vector3.zero;
            _verticalVelocity = 0f;
        }

        /// <summary>
        /// Gibt die GroundingDetection für Debug-Zwecke zurück.
        /// </summary>
        public GroundingDetection GetGroundingDetection() => _groundingDetection;

        #endregion

        #region Debug

        /// <summary>
        /// Zeichnet Debug-Gizmos.
        /// </summary>
        public void DrawDebugGizmos()
        {
#if UNITY_EDITOR
            _groundingDetection?.DrawDebugGizmos();

            if (_transform == null) return;

            // Velocity Vector
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(_transform.position, _transform.position + _velocity);

            // Horizontal Velocity
            Gizmos.color = Color.green;
            Gizmos.DrawLine(
                _transform.position + Vector3.up * 0.1f,
                _transform.position + Vector3.up * 0.1f + _horizontalVelocity
            );
#endif
        }

        #endregion
    }
}
