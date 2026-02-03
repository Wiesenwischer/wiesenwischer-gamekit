using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Motor;
using Wiesenwischer.GameKit.CharacterController.Core.StateMachine;

namespace Wiesenwischer.GameKit.CharacterController.Core.Locomotion
{
    /// <summary>
    /// Character Locomotion Controller - implementiert ICharacterController für den CharacterMotor.
    /// Handhabt die Bewegungslogik und gibt Velocity/Rotation an den Motor weiter.
    /// </summary>
    public class CharacterLocomotion : ILocomotionController, ICharacterController
    {
        private readonly CharacterMotor _motor;
        private readonly Transform _transform;
        private readonly ILocomotionConfig _config;

        // Input für den aktuellen Frame
        private LocomotionInput _currentInput;

        // Velocity state
        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;

        // Rotation state
        private float _targetYaw;
        private float _currentYaw;

        // Cached GroundInfo
        private GroundInfo _cachedGroundInfo;

        /// <summary>
        /// Erstellt eine neue CharacterLocomotion. Erwartet einen existierenden CharacterMotor.
        /// </summary>
        public CharacterLocomotion(CharacterMotor motor, ILocomotionConfig config)
        {
            _motor = motor ?? throw new System.ArgumentNullException(nameof(motor));
            _config = config ?? throw new System.ArgumentNullException(nameof(config));
            _transform = motor.Transform;

            // Registriere uns als Controller beim Motor
            _motor.CharacterController = this;

            // Motor-Einstellungen aus Config übernehmen
            ConfigureMotor();

            _currentYaw = _transform.eulerAngles.y;
            _targetYaw = _currentYaw;

            _cachedGroundInfo = GroundInfo.Empty;
        }

        private void ConfigureMotor()
        {
            // Motor-Einstellungen setzen
            _motor.MaxStepHeight = _config.MaxStepHeight;
            _motor.MinRequiredStepDepth = _config.MinStepDepth;
            _motor.MaxStableSlopeAngle = _config.MaxSlopeAngle;
            _motor.GroundDetectionExtraDistance = _config.GroundCheckDistance;
            _motor.StableGroundLayers = _config.GroundLayers;
            _motor.LedgeAndDenivelationHandling = _config.LedgeDetectionEnabled;
            _motor.MaxStableDistanceFromLedge = _config.MaxStableDistanceFromLedge;
            _motor.MaxStableDenivelationAngle = _config.MaxStableDenivelationAngle;
            _motor.StepHandling = StepHandlingMethod.Extra;
        }

        #region ILocomotionController

        public Vector3 Position => _motor.TransientPosition;
        public Quaternion Rotation => _motor.TransientRotation;
        public Vector3 Velocity => _motor.Velocity;

        // Ground-State kommt direkt vom Motor
        public bool IsGrounded => _motor.GroundingStatus.FoundAnyGround;
        public bool IsStableOnGround => _motor.GroundingStatus.IsStableOnGround;
        public GroundInfo GroundInfo => _cachedGroundInfo;

        public bool IsSliding => false; // TODO: Implementierung
        public float SlidingTime => 0f;
        public CharacterMotor Motor => _motor;

        public void Simulate(LocomotionInput input, float deltaTime)
        {
            // Speichere Input für die Motor-Callbacks
            _currentInput = input;

            // Der Motor wird automatisch vom CharacterMotorSystem aktualisiert
            // (in FixedUpdate). Die Locomotion-Logik passiert in den Callbacks.

            // Falls manuelle Simulation gewünscht ist (z.B. für Server-Side):
            // _motor.UpdatePhase1(deltaTime);
            // _motor.UpdatePhase2(deltaTime);
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
            _motor.BaseVelocity = velocity;
        }

        #endregion

        #region ICharacterController

        public void BeforeCharacterUpdate(float deltaTime)
        {
            // Step Detection vom State Machine übernehmen
            // Grounded States setzen StepDetectionEnabled = true
            // Airborne States setzen StepDetectionEnabled = false
            _motor.StepHandling = _currentInput.StepDetectionEnabled
                ? StepHandlingMethod.Extra
                : StepHandlingMethod.None;
        }

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // Rotation zur Bewegungsrichtung
            if (_config.RotateTowardsMovement && _horizontalVelocity.sqrMagnitude > 0.01f)
            {
                Vector3 dir = _horizontalVelocity.normalized;
                _targetYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                _currentYaw = Mathf.MoveTowardsAngle(_currentYaw, _targetYaw, _config.RotationSpeed * deltaTime);
                currentRotation = Quaternion.Euler(0, _currentYaw, 0);
            }
        }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            // Horizontale Bewegung
            Vector3 targetHorizontal = CalculateTargetHorizontalVelocity(_currentInput);
            _horizontalVelocity = ApplyAcceleration(_horizontalVelocity, targetHorizontal, deltaTime);

            // Vertikale Bewegung
            _verticalVelocity = CalculateVerticalVelocity(_currentInput.VerticalVelocity, deltaTime);

            // ForceUnground wenn wir springen
            if (_currentInput.VerticalVelocity > 0 && _verticalVelocity > 0)
            {
                _motor.ForceUnground(0.1f);
            }

            // Finale Velocity setzen
            currentVelocity = _horizontalVelocity + Vector3.up * _verticalVelocity;
        }

        public void PostGroundingUpdate(float deltaTime)
        {
            // Nach Ground Probing - hier haben wir den aktuellen Ground-State
            UpdateCachedGroundInfo();

            // Wenn wir stabil stehen und nicht fallen, kleine negative Velocity
            if (_motor.GroundingStatus.IsStableOnGround && _verticalVelocity <= 0f)
            {
                _verticalVelocity = -2f;
            }
        }

        public void AfterCharacterUpdate(float deltaTime)
        {
            // Nach dem Update
        }

        public bool IsColliderValidForCollisions(Collider coll)
        {
            // Alle Collider sind gültig
            return true;
        }

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            // Ground hit callback
        }

        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            // Movement hit callback
        }

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
            // Stabilität nachbearbeiten
        }

        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {
            // Discrete collision detected
        }

        #endregion

        #region Movement Logic

        private Vector3 CalculateTargetHorizontalVelocity(LocomotionInput input)
        {
            Vector3 inputDir = new Vector3(input.MoveDirection.x, 0, input.MoveDirection.y);

            // Transformiere Input relativ zur Kamera/Look-Richtung
            if (input.LookDirection.sqrMagnitude > 0.01f)
            {
                Quaternion lookRot = Quaternion.LookRotation(
                    new Vector3(input.LookDirection.x, 0, input.LookDirection.z).normalized,
                    Vector3.up);
                inputDir = lookRot * inputDir;
            }
            else
            {
                inputDir = _transform.TransformDirection(inputDir);
            }

            // SpeedModifier kontrolliert die Geschwindigkeit
            float speed = _config.WalkSpeed * input.SpeedModifier;

            return inputDir.normalized * speed * inputDir.magnitude;
        }

        private Vector3 ApplyAcceleration(Vector3 current, Vector3 target, float deltaTime)
        {
            float accel = target.sqrMagnitude > 0.01f ? _config.Acceleration : _config.Deceleration;

            // Weniger Kontrolle in der Luft
            if (!_motor.GroundingStatus.IsStableOnGround)
            {
                accel *= _config.AirControl;
            }

            return Vector3.MoveTowards(current, target, accel * deltaTime);
        }

        private float CalculateVerticalVelocity(float inputVerticalVelocity, float deltaTime)
        {
            float velocity = inputVerticalVelocity;

            const float GroundingVelocity = -2f;

            // Wenn die Input-Velocity die Grounding-Velocity ist, beibehalten
            if (Mathf.Approximately(velocity, GroundingVelocity))
            {
                return GroundingVelocity;
            }

            // Wenn grounded und velocity <= 0, Grounding-Velocity
            if (_motor.GroundingStatus.IsStableOnGround && velocity <= 0)
            {
                return GroundingVelocity;
            }

            // Gravity anwenden
            velocity -= _config.Gravity * deltaTime;
            velocity = Mathf.Max(velocity, -_config.MaxFallSpeed);

            return velocity;
        }

        #endregion

        #region GroundInfo Conversion

        private void UpdateCachedGroundInfo()
        {
            var grounding = _motor.GroundingStatus;

            _cachedGroundInfo = new GroundInfo
            {
                IsGrounded = grounding.FoundAnyGround,
                Point = grounding.GroundPoint,
                Normal = grounding.GroundNormal,
                SlopeAngle = Vector3.Angle(Vector3.up, grounding.GroundNormal),
                Distance = 0f,
                IsWalkable = grounding.IsStableOnGround,
                StabilityReport = ConvertStabilityReport(grounding)
            };
        }

        private HitStabilityReport ConvertStabilityReport(CharacterGroundingReport grounding)
        {
            return new HitStabilityReport
            {
                IsStable = grounding.IsStableOnGround,
                InnerNormal = grounding.InnerGroundNormal,
                OuterNormal = grounding.OuterGroundNormal,
                FoundInnerNormal = true,
                FoundOuterNormal = true
            };
        }

        #endregion

        #region Utility

        public Vector3 HorizontalVelocity => _horizontalVelocity;
        public float VerticalVelocity => _verticalVelocity;

        public void SetVerticalVelocity(float velocity)
        {
            _verticalVelocity = velocity;
        }

        public void SetRotation(float yaw)
        {
            _currentYaw = yaw;
            _targetYaw = yaw;
            _motor.SetRotation(Quaternion.Euler(0, yaw, 0));
        }

        public void StopMovement()
        {
            _horizontalVelocity = Vector3.zero;
            _verticalVelocity = 0f;
            _motor.BaseVelocity = Vector3.zero;
        }

        public float GetJumpVelocity()
        {
            return Mathf.Sqrt(2f * _config.Gravity * _config.JumpHeight);
        }

        /// <summary>
        /// Projiziert Bewegung auf die Boden-Oberfläche.
        /// </summary>
        public Vector3 GetSlopeDirection(Vector3 moveDirection)
        {
            var grounding = _motor.GroundingStatus;
            if (!grounding.FoundAnyGround || !grounding.IsStableOnGround)
                return moveDirection;

            return Vector3.ProjectOnPlane(moveDirection, grounding.GroundNormal).normalized * moveDirection.magnitude;
        }

        /// <summary>
        /// Gibt die Richtung tangential zur Oberfläche zurück.
        /// </summary>
        public Vector3 GetDirectionTangentToSurface(Vector3 direction, Vector3 surfaceNormal)
        {
            return _motor.GetDirectionTangentToSurface(direction, surfaceNormal);
        }

        #endregion

        #region Debug

        public void DrawDebugGizmos()
        {
#if UNITY_EDITOR
            if (_motor == null || _transform == null) return;

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(_motor.TransientPosition, _motor.TransientPosition + _motor.Velocity);

            var grounding = _motor.GroundingStatus;
            if (grounding.FoundAnyGround)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(grounding.GroundPoint, 0.05f);

                Gizmos.color = grounding.IsStableOnGround ? Color.green : Color.yellow;
                Gizmos.DrawWireSphere(grounding.GroundPoint, 0.1f);
            }
#endif
        }

        #endregion
    }
}
