using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Motor;
using Wiesenwischer.GameKit.CharacterController.Core.StateMachine;

namespace Wiesenwischer.GameKit.CharacterController.Core.Locomotion
{
    /// <summary>
    /// Character Locomotion - Bewegungsverhalten für humanoid Characters.
    /// Berechnet Bewegung basierend auf Input und Config.
    /// Verwendet KinematicMotor für die physikalische Kollisionserkennung.
    /// Deterministisch für CSP (Client-Side Prediction) Kompatibilität.
    ///
    /// Dies ist die Standard-Locomotion für:
    /// - Walking, Running
    /// - Jumping, Falling
    /// - Slope Sliding
    ///
    /// Für andere Bewegungsarten (Reiten, Gleiten) werden separate
    /// Locomotion-Klassen erstellt, die denselben KinematicMotor verwenden.
    /// </summary>
    public class CharacterLocomotion : ILocomotionController
    {
        private readonly KinematicMotor _motor;
        private readonly Transform _transform;
        private readonly ILocomotionConfig _config;
        private readonly GroundingDetection _groundingDetection;

        // State
        private Vector3 _velocity;
        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;
        private bool _isGrounded;

        // Sliding State (persistent über Frames)
        private bool _isSliding;
        private float _slidingTime;
        private Vector3 _currentSlideDirection;
        private float _noSlopeContactTime;
        private const float SLIDE_EXIT_DELAY = 0.15f;

        // Rotation
        private float _targetYaw;
        private float _currentYaw;

        /// <summary>
        /// Erstellt eine neue CharacterLocomotion mit eigenem KinematicMotor.
        /// </summary>
        public CharacterLocomotion(
            Transform transform,
            CapsuleCollider capsule,
            ILocomotionConfig config,
            float skinWidth = 0.02f)
        {
            if (transform == null)
                throw new System.ArgumentNullException(nameof(transform));
            if (capsule == null)
                throw new System.ArgumentNullException(nameof(capsule));
            if (config == null)
                throw new System.ArgumentNullException(nameof(config));

            _transform = transform;
            _config = config;

            _motor = new KinematicMotor(transform, capsule, config, skinWidth);
            _groundingDetection = new GroundingDetection(transform, config, capsule.radius, capsule.height);

            _currentYaw = transform.eulerAngles.y;
            _targetYaw = _currentYaw;
        }

        /// <summary>
        /// Erstellt eine CharacterLocomotion mit externer GroundingDetection.
        /// </summary>
        public CharacterLocomotion(
            Transform transform,
            CapsuleCollider capsule,
            ILocomotionConfig config,
            GroundingDetection groundingDetection,
            float skinWidth = 0.02f)
        {
            if (transform == null)
                throw new System.ArgumentNullException(nameof(transform));
            if (capsule == null)
                throw new System.ArgumentNullException(nameof(capsule));
            if (config == null)
                throw new System.ArgumentNullException(nameof(config));
            if (groundingDetection == null)
                throw new System.ArgumentNullException(nameof(groundingDetection));

            _transform = transform;
            _config = config;
            _groundingDetection = groundingDetection;

            _motor = new KinematicMotor(transform, capsule, config, skinWidth);

            _currentYaw = transform.eulerAngles.y;
            _targetYaw = _currentYaw;
        }

        #region ILocomotionController Implementation

        public Vector3 Position => _transform.position;
        public Quaternion Rotation => _transform.rotation;
        public Vector3 Velocity => _velocity;
        public bool IsGrounded => _groundingDetection.IsGrounded;
        public GroundInfo GroundInfo => _groundingDetection.GroundInfo;

        /// <summary>Ob der Character gerade auf einem steilen Hang rutscht.</summary>
        public bool IsSliding => _isSliding;

        /// <summary>Wie lange der Character bereits rutscht (in Sekunden).</summary>
        public float SlidingTime => _slidingTime;

        /// <summary>Der zugrundeliegende KinematicMotor.</summary>
        public KinematicMotor Motor => _motor;

        public void Simulate(LocomotionInput input, float deltaTime)
        {
            // 1. Ground Check
            _groundingDetection.UpdateGroundCheck();
            _isGrounded = _groundingDetection.IsGrounded;

            // 2. Horizontale Bewegung berechnen
            Vector3 targetHorizontalVelocity = CalculateTargetHorizontalVelocity(input);
            _horizontalVelocity = ApplyAcceleration(_horizontalVelocity, targetHorizontalVelocity, deltaTime);

            // 3. Vertikale Bewegung (Gravity + Jump)
            _verticalVelocity = CalculateVerticalVelocity(input.VerticalVelocity, deltaTime);

            // 4. Step Detection
            Vector3 moveDirection = _horizontalVelocity;
            bool isClimbingStep = false;

            if (_isGrounded && _horizontalVelocity.sqrMagnitude > 0.01f)
            {
                if (_motor.TryStepUp(_horizontalVelocity, out Vector3 stepUpPos))
                {
                    isClimbingStep = true;
                    _motor.SetPosition(stepUpPos);
                    Debug.Log($"[Step] Step-Up ausgeführt!");
                }
            }

            // 5. Slope Handling
            float slopeAngleBelow = _groundingDetection.SlopeAngleDirectlyBelow;
            bool isWalkableBelow = _groundingDetection.IsWalkableDirectlyBelow;
            Vector3 slopeNormal = _groundingDetection.SlopeNormalDirectlyBelow;

            if (slopeAngleBelow > 5f)
            {
                Debug.Log($"[Slope-Status] Angle: {slopeAngleBelow:F1}°, Walkable: {isWalkableBelow}, MaxSlope: {_config.MaxSlopeAngle}°, Grounded: {_isGrounded}");
            }

            bool onSteepSlope = !isWalkableBelow && slopeAngleBelow >= _config.MaxSlopeAngle && slopeAngleBelow < 85f;

            // Sliding State Management
            if (onSteepSlope)
            {
                _noSlopeContactTime = 0f;

                if (!_isSliding)
                {
                    _isSliding = true;
                    _slidingTime = 0f;
                    Debug.Log($"[Sliding-START] Angle: {slopeAngleBelow:F1}°");
                }

                _currentSlideDirection = CalculateSlideDirection(slopeNormal);
                _slidingTime += deltaTime;
            }
            else if (_isSliding)
            {
                _noSlopeContactTime += deltaTime;

                if (_isGrounded && isWalkableBelow)
                {
                    _isSliding = false;
                    _slidingTime = 0f;
                    _noSlopeContactTime = 0f;
                    Debug.Log($"[Sliding-END] Landed on walkable ground");
                }
                else if (_noSlopeContactTime > SLIDE_EXIT_DELAY)
                {
                    _isSliding = false;
                    _slidingTime = 0f;
                    _noSlopeContactTime = 0f;
                    Debug.Log($"[Sliding-END] No slope contact timeout");
                }
            }

            // 6. Bewegung berechnen
            if (!isClimbingStep)
            {
                if (_isSliding)
                {
                    // SLIDING-MODUS
                    float slideSpeed;
                    float slideIntensity;

                    if (_config.UseSlopeDependentSlideSpeed)
                    {
                        slideIntensity = Mathf.InverseLerp(_config.MaxSlopeAngle, 90f, slopeAngleBelow);
                        slideIntensity = Mathf.Max(slideIntensity, 0.3f);
                        slideSpeed = _config.SlopeSlideSpeed * slideIntensity;
                    }
                    else
                    {
                        slideIntensity = 1f;
                        slideSpeed = _config.SlopeSlideSpeed;
                    }

                    float timeAcceleration = Mathf.Min(_slidingTime * 0.5f, 1f);
                    slideSpeed *= (1f + timeAcceleration);

                    Debug.Log($"[Sliding] Angle: {slopeAngleBelow:F1}°, Speed: {slideSpeed:F2}, Time: {_slidingTime:F2}s");

                    Vector3 slideVelocity = _currentSlideDirection * slideSpeed;
                    moveDirection = new Vector3(slideVelocity.x, 0, slideVelocity.z);
                    _horizontalVelocity = moveDirection;
                    _verticalVelocity = slideVelocity.y;

                    // Quersteuerung beim Sliden
                    if (input.MoveDirection.sqrMagnitude > 0.01f)
                    {
                        Vector3 inputDir = new Vector3(input.MoveDirection.x, 0, input.MoveDirection.y);
                        if (input.LookDirection.sqrMagnitude > 0.01f)
                        {
                            Quaternion lookRotation = Quaternion.LookRotation(
                                new Vector3(input.LookDirection.x, 0, input.LookDirection.z).normalized,
                                Vector3.up
                            );
                            inputDir = lookRotation * inputDir;
                        }

                        float upwardComponent = Vector3.Dot(inputDir.normalized, -_currentSlideDirection);
                        if (upwardComponent < 0.5f)
                        {
                            moveDirection += inputDir * _config.AirControl;
                        }
                    }
                }
                else if (_isGrounded && isWalkableBelow)
                {
                    // NORMAL-MODUS
                    moveDirection = _groundingDetection.GetSlopeDirection(_horizontalVelocity);
                    moveDirection = moveDirection.normalized * _horizontalVelocity.magnitude;
                }
            }

            // 7. Finale Velocity
            _velocity = moveDirection + Vector3.up * _verticalVelocity;

            // 8. Character bewegen via KinematicMotor
            _motor.Move(_velocity * deltaTime);

            // 9. Rotation
            if (_config.RotateTowardsMovement && input.MoveDirection.sqrMagnitude > 0.01f)
            {
                UpdateRotation(input.LookDirection, deltaTime);
            }

            // 10. Snap to Ground (nicht beim Sliding)
            if (_isGrounded && _verticalVelocity <= 0 && !_isSliding)
            {
                SnapToGround();
            }
        }

        private Vector3 CalculateSlideDirection(Vector3 slopeNormal)
        {
            Vector3 slopeRight = Vector3.Cross(slopeNormal, Vector3.up).normalized;

            if (slopeRight.sqrMagnitude > 0.001f)
            {
                Vector3 slideDir = Vector3.Cross(slopeRight, slopeNormal).normalized;
                if (slideDir.y > 0)
                {
                    slideDir = -slideDir;
                }
                return slideDir;
            }

            return Vector3.ProjectOnPlane(Vector3.down, slopeNormal).normalized;
        }

        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            _motor.SetPositionAndRotation(position, rotation);
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

        private Vector3 CalculateTargetHorizontalVelocity(LocomotionInput input)
        {
            Vector3 inputDirection = new Vector3(input.MoveDirection.x, 0, input.MoveDirection.y);

            if (input.LookDirection.sqrMagnitude > 0.01f)
            {
                Quaternion lookRotation = Quaternion.LookRotation(
                    new Vector3(input.LookDirection.x, 0, input.LookDirection.z).normalized,
                    Vector3.up
                );
                inputDirection = lookRotation * inputDirection;
            }
            else
            {
                inputDirection = _transform.TransformDirection(inputDirection);
            }

            float targetSpeed = input.IsSprinting ? _config.RunSpeed : _config.WalkSpeed;

            if (!_isGrounded)
            {
                targetSpeed *= _config.AirControl;
            }

            return inputDirection.normalized * targetSpeed * inputDirection.magnitude;
        }

        private Vector3 ApplyAcceleration(Vector3 currentVelocity, Vector3 targetVelocity, float deltaTime)
        {
            float currentSpeed = currentVelocity.magnitude;
            float targetSpeed = targetVelocity.magnitude;

            float acceleration;
            if (targetSpeed > currentSpeed || targetVelocity.sqrMagnitude > 0.01f)
            {
                acceleration = _config.Acceleration;
            }
            else
            {
                acceleration = _config.Deceleration;
            }

            if (!_isGrounded)
            {
                acceleration *= _config.AirControl;
            }

            return Vector3.MoveTowards(currentVelocity, targetVelocity, acceleration * deltaTime);
        }

        private float CalculateVerticalVelocity(float inputVerticalVelocity, float deltaTime)
        {
            float velocity = inputVerticalVelocity;

            if (_isGrounded && velocity <= 0)
            {
                return -2f;
            }

            velocity -= _config.Gravity * deltaTime;
            velocity = Mathf.Max(velocity, -_config.MaxFallSpeed);

            return velocity;
        }

        #endregion

        #region Rotation

        private void UpdateRotation(Vector3 lookDirection, float deltaTime)
        {
            if (_horizontalVelocity.sqrMagnitude < 0.01f) return;

            Vector3 targetDirection = _horizontalVelocity.normalized;
            _targetYaw = Mathf.Atan2(targetDirection.x, targetDirection.z) * Mathf.Rad2Deg;

            _currentYaw = Mathf.MoveTowardsAngle(
                _currentYaw,
                _targetYaw,
                _config.RotationSpeed * deltaTime
            );

            _transform.rotation = Quaternion.Euler(0, _currentYaw, 0);
        }

        public void SetRotation(float yaw)
        {
            _currentYaw = yaw;
            _targetYaw = yaw;
            _transform.rotation = Quaternion.Euler(0, yaw, 0);
        }

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
            if (_groundingDetection.GroundInfo.Distance > 0.01f &&
                _groundingDetection.GroundInfo.Distance < _config.GroundCheckDistance)
            {
                Vector3 snapPosition = _transform.position;
                snapPosition.y = _groundingDetection.GroundInfo.Point.y;
                _motor.SetPosition(snapPosition);
            }
        }

        public float GetJumpVelocity()
        {
            if (_config is LocomotionConfig locomotionConfig)
            {
                return locomotionConfig.CalculateJumpVelocity();
            }
            return Mathf.Sqrt(2f * _config.Gravity * _config.JumpHeight);
        }

        public Vector3 GetHorizontalVelocity() => _horizontalVelocity;
        public float GetVerticalVelocity() => _verticalVelocity;

        public Vector3 HorizontalVelocity => _horizontalVelocity;
        public float VerticalVelocity => _verticalVelocity;

        public void SetVerticalVelocity(float velocity)
        {
            _verticalVelocity = velocity;
        }

        public void StopMovement()
        {
            _velocity = Vector3.zero;
            _horizontalVelocity = Vector3.zero;
            _verticalVelocity = 0f;
        }

        public GroundingDetection GetGroundingDetection() => _groundingDetection;

        #endregion

        #region Debug

        public void DrawDebugGizmos()
        {
#if UNITY_EDITOR
            _groundingDetection?.DrawDebugGizmos();
            _motor?.DrawDebugGizmos();

            if (_transform == null) return;

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(_transform.position, _transform.position + _velocity);

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
