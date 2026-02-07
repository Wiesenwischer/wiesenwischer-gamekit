using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Locomotion.Modules;
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

        // Module
        private readonly GravityModule _gravityModule;
        private readonly AccelerationModule _accelerationModule;
        private readonly GroundDetectionModule _groundDetectionModule;

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

        // Collision tracking für AfterCharacterUpdate sync
        private bool _wasBlockedThisFrame;

        /// <summary>
        /// Erstellt eine neue CharacterLocomotion. Erwartet einen existierenden CharacterMotor.
        /// </summary>
        public CharacterLocomotion(CharacterMotor motor, ILocomotionConfig config)
        {
            _motor = motor ?? throw new System.ArgumentNullException(nameof(motor));
            _config = config ?? throw new System.ArgumentNullException(nameof(config));
            _transform = motor.transform;

            // Module initialisieren
            _gravityModule = new GravityModule();
            _accelerationModule = new AccelerationModule();
            _groundDetectionModule = new GroundDetectionModule();

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
            _wasBlockedThisFrame = false;

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
            // Horizontale Bewegung (via AccelerationModule)
            Vector3 targetHorizontal = _accelerationModule.CalculateTargetVelocity(
                _currentInput.MoveDirection,
                _currentInput.LookDirection,
                _transform.forward,
                _config.WalkSpeed,
                _currentInput.SpeedModifier);

            _horizontalVelocity = _accelerationModule.CalculateHorizontalVelocity(
                _horizontalVelocity,
                targetHorizontal,
                _config.Acceleration,
                _config.Deceleration,
                _config.AirControl,
                _config.AirDrag,
                _motor.GroundingStatus.IsStableOnGround,
                deltaTime);

            // Vertikale Bewegung: State Machine ist Owner der Vertical Velocity
            // (Gravity wird in PlayerAirborneState.OnPhysicsUpdate angewendet)
            _verticalVelocity = _currentInput.VerticalVelocity;

            // ForceUnground wenn wir springen
            if (_verticalVelocity > 0)
            {
                _motor.ForceUnground(0.1f);
            }

            // Finale Velocity setzen
            if (_motor.GroundingStatus.IsStableOnGround && _verticalVelocity <= 0 && _horizontalVelocity.sqrMagnitude > 0.01f)
            {
                // Am Boden: Velocity auf Slope-Oberfläche reorientieren.
                // Verhindert Speed-Verlust auf Rampen und Hängenbleiben an Slope-Kanten,
                // da der Motor die Velocity nicht mehr intern reprojizieren muss.
                currentVelocity = _motor.GetDirectionTangentToSurface(
                    _horizontalVelocity, _motor.GroundingStatus.GroundNormal) * _horizontalVelocity.magnitude;
            }
            else
            {
                // In der Luft oder beim Springen: Flache horizontale + vertikale Velocity
                currentVelocity = _horizontalVelocity + Vector3.up * _verticalVelocity;
            }
        }

        public void PostGroundingUpdate(float deltaTime)
        {
            // Nach Ground Probing - hier haben wir den aktuellen Ground-State
            UpdateCachedGroundInfo();

            // Ground Snapping via GravityModule
            _verticalVelocity = _gravityModule.ApplyGroundSnapping(
                _verticalVelocity,
                _motor.GroundingStatus.IsStableOnGround);
        }

        public void AfterCharacterUpdate(float deltaTime)
        {
            // Sync horizontale Velocity NUR wenn eine Wand-Kollision erkannt wurde.
            // OnMovementHit setzt _wasBlockedThisFrame bei instabilen Treffern (Wände).
            // Ohne Sync behält _horizontalVelocity "Phantom Velocity" gegen Wände.
            if (_wasBlockedThisFrame)
            {
                Vector3 resolvedHorizontal = new Vector3(_motor.BaseVelocity.x, 0f, _motor.BaseVelocity.z);
                _horizontalVelocity = resolvedHorizontal;
            }
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
            // Instabile Treffer (Wände, zu steile Oberflächen) markieren,
            // damit AfterCharacterUpdate die Velocity korrigieren kann.
            if (!hitStabilityReport.IsStable)
            {
                _wasBlockedThisFrame = true;
            }
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

        #region GroundInfo Conversion

        private void UpdateCachedGroundInfo()
        {
            // GroundDetectionModule kapselt die Motor-Status Interpretation
            _cachedGroundInfo = _groundDetectionModule.GetGroundInfo(_motor);
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
