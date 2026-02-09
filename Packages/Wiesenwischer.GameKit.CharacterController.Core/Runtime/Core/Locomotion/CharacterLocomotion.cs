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
        private readonly AccelerationModule _accelerationModule;
        private readonly GroundDetectionModule _groundDetectionModule;
        private readonly GravityModule _gravityModule;

        // Kontinuierlicher Input (latest-value-wins, Overwrite = korrekt)
        private LocomotionInput _currentInput;

        // Event-Flags (intern akkumuliert, konsumiert in UpdateVelocity)
        private bool _jumpRequested;
        private bool _jumpCutRequested;
        private bool _resetVerticalRequested;

        // Cached horizontal velocity (aus UpdateVelocity, für Rotation + Debug)
        private Vector3 _lastComputedHorizontal;

        // Vertical Velocity: Locomotion ist Owner (Intent System Pattern)
        // States setzen Intent (Jump, JumpCut, ResetVertical), Locomotion berechnet Physik.
        private float _verticalVelocity;

        // Rotation state
        private float _targetYaw;
        private float _currentYaw;

        // Cached GroundInfo
        private GroundInfo _cachedGroundInfo;

        // Debug: Frames nach Landung loggen
        private int _debugLandingFrames;

        /// <summary>
        /// Erstellt eine neue CharacterLocomotion. Erwartet einen existierenden CharacterMotor.
        /// </summary>
        public CharacterLocomotion(CharacterMotor motor, ILocomotionConfig config)
        {
            _motor = motor ?? throw new System.ArgumentNullException(nameof(motor));
            _config = config ?? throw new System.ArgumentNullException(nameof(config));
            _transform = motor.transform;

            // Module initialisieren
            _accelerationModule = new AccelerationModule();
            _groundDetectionModule = new GroundDetectionModule();
            _gravityModule = new GravityModule();

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
            // Nur kontinuierlicher Input - Overwrite ist korrekt (latest-value-wins).
            // Events (Jump etc.) gehen über RequestJump() und werden intern akkumuliert.
            _currentInput = input;
        }

        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            _motor.SetPositionAndRotation(position, rotation);
            _currentYaw = rotation.eulerAngles.y;
            _targetYaw = _currentYaw;
        }

        public void ApplyVelocity(Vector3 velocity)
        {
            _lastComputedHorizontal = new Vector3(velocity.x, 0, velocity.z);
            _verticalVelocity = velocity.y;
            _motor.BaseVelocity = velocity;
        }

        public void RequestJump() => _jumpRequested = true;
        public void RequestJumpCut() => _jumpCutRequested = true;
        public void RequestResetVertical() => _resetVerticalRequested = true;

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
            if (_config.RotateTowardsMovement && _lastComputedHorizontal.sqrMagnitude > 0.01f)
            {
                Vector3 dir = _lastComputedHorizontal.normalized;
                _targetYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                _currentYaw = Mathf.MoveTowardsAngle(_currentYaw, _targetYaw, _config.RotationSpeed * deltaTime);
                currentRotation = Quaternion.Euler(0, _currentYaw, 0);
            }
        }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            // === HORIZONTAL ===
            // Zwei Quellen je nach Situation:
            // - Am Boden: Motor's BaseVelocity (respektiert Wand-Kollisionen, Slope-Magnitude)
            // - In der Luft: Letzte berechnete Velocity (Kollisionen mit Hinderniss-Kanten
            //   sollen kein Momentum kosten, Step Handling ist aus)
            Vector3 currentHorizontal;
            if (_motor.GroundingStatus.IsStableOnGround)
            {
                // Am Boden: BaseVelocity ist slope-tangent-projiziert → hat Y-Komponente.
                // Flache Richtung extrahieren, volle Speed beibehalten
                // (GetDirectionTangentToSurface konserviert die Magnitude).
                Vector3 flatDir = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
                float flatMag = flatDir.magnitude;
                currentHorizontal = flatMag > 0.01f
                    ? flatDir.normalized * currentVelocity.magnitude
                    : Vector3.zero;
            }
            else
            {
                // In der Luft: AccelerationModule-Output vom letzten Frame.
                // Motor's Kollisions-Auflösung (z.B. Capsule streift Hinderniss-Kante)
                // wird physisch aufgelöst (keine Penetration), beeinflusst aber nicht
                // die Velocity-Planung. So bleibt Momentum beim Landen erhalten.
                currentHorizontal = _lastComputedHorizontal;
            }

            Vector3 targetHorizontal = _accelerationModule.CalculateTargetVelocity(
                _currentInput.MoveDirection,
                _currentInput.LookDirection,
                _transform.forward,
                _config.WalkSpeed,
                _currentInput.SpeedModifier);

            Vector3 newHorizontal = _accelerationModule.CalculateHorizontalVelocity(
                currentHorizontal,
                targetHorizontal,
                _config.Acceleration,
                _config.Deceleration,
                _config.AirControl,
                _config.AirDrag,
                _motor.GroundingStatus.IsStableOnGround,
                deltaTime);

            _lastComputedHorizontal = newHorizontal;

            // === VERTICAL (Intent System) ===
            // Locomotion ist Owner der vertikalen Velocity.
            // States setzen Intent (Jump, JumpCut, ResetVertical), hier wird Physik berechnet.

            // 1. Jump-Impulse verarbeiten
            if (_jumpRequested)
            {
                _jumpRequested = false;
                _verticalVelocity = GetJumpVelocity();
                _motor.ForceUnground(0.1f);
            }

            // 2. Variable Jump Cut (Button früh losgelassen)
            if (_jumpCutRequested && _verticalVelocity > 0f)
            {
                _jumpCutRequested = false;
                _verticalVelocity *= JumpModule.DefaultJumpCutMultiplier;
            }

            // 3. Vertical Reset (Ceiling Hit etc.)
            if (_resetVerticalRequested)
            {
                _resetVerticalRequested = false;
                _verticalVelocity = 0f;
            }

            // 4. Gravity via GravityModule (Single Source of Truth)
            _verticalVelocity = _gravityModule.CalculateVerticalVelocity(
                _verticalVelocity,
                _motor.GroundingStatus.IsStableOnGround,
                _config.Gravity,
                _config.MaxFallSpeed,
                deltaTime);

            float vertical = _verticalVelocity;

            // ForceUnground wenn aufwärts (z.B. nach Jump-Impulse)
            if (vertical > 0f)
            {
                _motor.ForceUnground(0.1f);
            }

            // DEBUG: Landing-Diagnose (5 Frames nach Landung loggen)
            if (_motor.JustLanded) _debugLandingFrames = 5;
            if (_debugLandingFrames > 0)
            {
                _debugLandingFrames--;
                Debug.Log($"[Locomotion] grounded={_motor.GroundingStatus.IsStableOnGround} justLanded={_motor.JustLanded} " +
                    $"baseVel={currentVelocity:F2} inputH={currentHorizontal.magnitude:F2} " +
                    $"target={targetHorizontal.magnitude:F2} newH={newHorizontal.magnitude:F2} " +
                    $"speedMod={_currentInput.SpeedModifier:F2} vert={vertical:F2} " +
                    $"stepDet={_currentInput.StepDetectionEnabled}");
            }

            // === FINALE VELOCITY ===
            if (_motor.GroundingStatus.IsStableOnGround && vertical <= 0 && newHorizontal.sqrMagnitude > 0.01f)
            {
                // Am Boden: Velocity auf Slope-Oberfläche reorientieren
                currentVelocity = _motor.GetDirectionTangentToSurface(
                    newHorizontal, _motor.GroundingStatus.GroundNormal) * newHorizontal.magnitude;
            }
            else
            {
                // In der Luft oder beim Springen: Flache horizontale + vertikale Velocity
                currentVelocity = newHorizontal + Vector3.up * vertical;
            }
        }

        public void PostGroundingUpdate(float deltaTime)
        {
            // Nach Ground Probing - hier haben wir den aktuellen Ground-State
            UpdateCachedGroundInfo();
            // Ground Snapping wird in UpdateVelocity gehandhabt
        }

        public void AfterCharacterUpdate(float deltaTime)
        {
            // Kein Wall-Sync nötig: UpdateVelocity liest direkt aus currentVelocity (= BaseVelocity),
            // die bereits die Kollisions-Auflösung vom Motor enthält.
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
            // Kollisions-Auflösung wird vom Motor in InternalCharacterMove gehandhabt
            // und fließt über currentVelocity ins nächste UpdateVelocity zurück.
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

        public Vector3 HorizontalVelocity => _lastComputedHorizontal;
        public float VerticalVelocity => _verticalVelocity;

        public void SetRotation(float yaw)
        {
            _currentYaw = yaw;
            _targetYaw = yaw;
            _motor.SetRotation(Quaternion.Euler(0, yaw, 0));
        }

        public void StopMovement()
        {
            _lastComputedHorizontal = Vector3.zero;
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
