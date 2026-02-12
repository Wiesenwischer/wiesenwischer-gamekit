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

        // Strategies (evaluiert einmal pro Frame in PostGroundingUpdate)
        private readonly IGroundDetectionStrategy _groundDetectionStrategy;
        private readonly IFallDetectionStrategy _fallDetectionStrategy;

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

        // Stair Detection: Step-Frequenz tracken
        private float _lastStepTime;
        private int _recentStepCount;
        private const float StairDetectionWindow = 0.6f;
        private const int StairStepThreshold = 2;

        // Cached GroundInfo
        private GroundInfo _cachedGroundInfo;

        // Terrain Speed Multiplier: Kombination aus Slope + Stair Modifier.
        // Wird in UpdateVelocity berechnet, damit AnimatorParameterBridge kompensieren kann.
        private float _currentTerrainSpeedMultiplier = 1f;

        // Debug: Hovering-Diagnose
        private int _debugLandingFrames;
        private int _debugHoverLogCount;

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

            // Ground Detection Strategy
            _groundDetectionStrategy = config.GroundDetection switch
            {
                GroundDetectionMode.Collider => new ColliderGroundDetectionStrategy(
                    config.GroundCheckDistance, config.GroundCheckRadius, config.GroundLayers),
                _ => new MotorGroundDetectionStrategy()
            };

            // Fall Detection Strategy
            _fallDetectionStrategy = config.FallDetection switch
            {
                FallDetectionMode.Collider => new ColliderFallDetectionStrategy(
                    config.GroundToFallRayDistance, config.GroundLayers),
                _ => new MotorFallDetectionStrategy()
            };

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

        /// <summary>
        /// Ob der Character Bodenkontakt hat.
        /// Kommt von der IGroundDetectionStrategy (einmal pro Frame in PostGroundingUpdate evaluiert).
        /// </summary>
        public bool IsGrounded => _groundDetectionStrategy.IsGrounded;

        /// <summary>
        /// Ob der Character über einer Kante steht und fallen sollte.
        /// Kommt von der IFallDetectionStrategy.
        /// </summary>
        public bool IsOverEdge => _fallDetectionStrategy.IsOverEdge;

        public GroundInfo GroundInfo => _cachedGroundInfo;
        public bool SnappingPrevented => _motor.GroundingStatus.SnappingPrevented;
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
            if (_groundDetectionStrategy.IsGrounded)
            {
                // Am Boden: BaseVelocity ist slope-tangent-projiziert → hat Y-Komponente
                // vom Slope-Winkel. Flache Richtung extrahieren, volle 3D-Magnitude
                // beibehalten damit auf Slopes kein Speed verloren geht.
                Vector3 flatDir = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
                float flatMag = flatDir.magnitude;

                // Landing Inflation Cap: HandleVelocityProjection im Motor konvertiert beim
                // Aufkommen auf Rampen die Fallgeschwindigkeit in Surface-Speed (magnitude preservation).
                // Die 3D-Magnitude darf nie deutlich über dem liegen, was AccelerationModule
                // im letzten Frame berechnet hat. Auf normalen Slopes sind beide gleich
                // (Tangent-Projektion erhält die Magnitude). Bei Landung ist die 3D-Magnitude
                // durch die konvertierte Fallgeschwindigkeit aufgebläht.
                float mag3D = currentVelocity.magnitude;
                float lastAccelMag = _lastComputedHorizontal.magnitude;
                if (lastAccelMag > 0.01f && mag3D > lastAccelMag * 1.1f)
                {
                    mag3D = lastAccelMag;
                }

                if (flatMag > 0.01f)
                {
                    currentHorizontal = flatDir.normalized * mag3D;
                }
                else if (_lastComputedHorizontal.sqrMagnitude > 0.1f &&
                         _motor.GroundingStatus.IsStableOnGround)
                {
                    // Slope-Transition Fallback: Der Motor's Kollisions-Auflösung an
                    // Rampen-/Boden-Übergängen (konvexe Kanten) kann die horizontale
                    // Velocity auf 0 projizieren (Crease Resolution zwischen zwei
                    // Oberflächen). Wenn der Character stabil am Boden steht und sich
                    // im letzten Frame bewegt hat, behalten wir die geplante Velocity
                    // bei. Der Motor löst die Kollision physisch auf (keine Penetration),
                    // aber das Momentum bleibt erhalten → flüssiger Übergang.
                    currentHorizontal = _lastComputedHorizontal;
                }
                else
                {
                    currentHorizontal = Vector3.zero;
                }
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

            // Slope Speed Modifier: Target-Velocity bergauf reduzieren, bergab optional erhöhen.
            // Wirkt auf Target, damit AccelerationModule natürlich zum Ziel hin beschleunigt/bremst.
            float terrainMultiplier = 1f;
            if (_groundDetectionStrategy.IsGrounded &&
                _motor.GroundingStatus.IsStableOnGround &&
                targetHorizontal.sqrMagnitude > 0.01f)
            {
                float slopeMultiplier = CalculateSlopeSpeedMultiplier(
                    targetHorizontal, _motor.GroundingStatus.GroundNormal);
                terrainMultiplier *= slopeMultiplier;
                targetHorizontal *= slopeMultiplier;
            }

            // Stair Speed Modifier: Auf Treppen (häufige Steps) Target reduzieren.
            if (IsOnStairs && targetHorizontal.sqrMagnitude > 0.01f)
            {
                float stairMultiplier = 1f - _config.StairSpeedReduction;
                terrainMultiplier *= stairMultiplier;
                targetHorizontal *= stairMultiplier;
            }

            _currentTerrainSpeedMultiplier = terrainMultiplier;

            float deceleration = _currentInput.DecelerationOverride > 0f
                ? _currentInput.DecelerationOverride
                : _config.Deceleration;

            Vector3 newHorizontal = _accelerationModule.CalculateHorizontalVelocity(
                currentHorizontal,
                targetHorizontal,
                _config.Acceleration,
                deceleration,
                _config.AirControl,
                _config.AirDrag,
                _groundDetectionStrategy.IsGrounded,
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
                _groundDetectionStrategy.IsGrounded,
                _config.Gravity,
                _config.MaxFallSpeed,
                deltaTime);

            float vertical = _verticalVelocity;

            // ForceUnground wenn aufwärts (z.B. nach Jump-Impulse)
            if (vertical > 0f)
            {
                _motor.ForceUnground(0.1f);
            }

            // DEBUG: Hovering-Diagnose
            // Trigger 1: Nach Landung (30 Frames)
            if (_motor.JustLanded)
            {
                _debugLandingFrames = 30;
                _debugHoverLogCount = 0;
            }
            // Trigger 2: Persistent Gap Monitor — MOVING branch + ungewöhnlicher Gap (> 0.03)
            {
                var gs = _motor.GroundingStatus;
                float charY = _motor.TransientPosition.y;
                float groundY = gs.GroundPoint.y;
                float gap = charY - groundY;
                bool isMovingBranch = gs.IsStableOnGround && vertical <= 0 && newHorizontal.sqrMagnitude > 0.01f;
                bool shouldLog = _debugLandingFrames > 0 || (isMovingBranch && gap > 0.03f && _debugHoverLogCount < 60);

                if (shouldLog)
                {
                    if (_debugLandingFrames > 0) _debugLandingFrames--;
                    if (isMovingBranch && gap > 0.03f) _debugHoverLogCount++;
                    string trigger = _debugLandingFrames > 0 ? "LAND" : "GAP";
                    Debug.Log($"[Hovering:{trigger}] gap={gap:F4} " +
                        $"branch={(isMovingBranch ? "MOVING" : "else")} " +
                        $"input=({_currentInput.MoveDirection.x:F2},{_currentInput.MoveDirection.y:F2}) " +
                        $"speedMod={_currentInput.SpeedModifier:F1} " +
                        $"motorVelIn=({currentVelocity.x:F2},{currentVelocity.y:F2},{currentVelocity.z:F2}) " +
                        $"H={newHorizontal.magnitude:F2} V={vertical:F2} " +
                        $"snap={_motor.GroundSnappingEnabled} stable={gs.IsStableOnGround}");
                }
            }

            // === FINALE VELOCITY ===
            // Tangentiale Projektion NUR wenn der Motor eine stabile Surface bestätigt.
            // Die Strategy (Collider-SphereCast) kann "grounded" sagen obwohl der Motor
            // den Character nicht als stabil betrachtet (z.B. über Kante). Ohne stabile
            // Surface → else-Branch → vertikale Velocity wird angewendet → Character fällt.
            bool stableOnSurface = _motor.GroundingStatus.IsStableOnGround;
            if (stableOnSurface && vertical <= 0 && newHorizontal.sqrMagnitude > 0.01f)
            {
                // Am Boden mit Bewegung: Velocity auf Slope-Oberfläche reorientieren.
                // KEINE GroundingVelocity hier — HandleVelocityProjection nutzt velocity.magnitude
                // und konvertiert jede Y-Komponente in horizontale Speed-Inflation.
                // Ground-Snapping übernimmt der Motor via ProbeGround + GroundSnapping.
                currentVelocity = _motor.GetDirectionTangentToSurface(
                    newHorizontal, _motor.GroundingStatus.GroundNormal) * newHorizontal.magnitude;
            }
            else
            {
                // In der Luft, beim Springen, oder stehend am Boden:
                // Flache horizontale + vertikale Velocity (GroundingVelocity -2f zieht
                // den stehenden Character auf den Boden)
                currentVelocity = newHorizontal + Vector3.up * vertical;
            }
        }

        public void PostGroundingUpdate(float deltaTime)
        {
            // Strategies evaluieren (einmal pro Frame, nach Motor's ProbeGround)
            _groundDetectionStrategy.Evaluate(_motor);
            _fallDetectionStrategy.Evaluate(_motor);

            // Motor's Ground Snapping deaktivieren wenn Strategy sagt "nicht geerdet"
            // ProbeGround läuft weiter (Surface-Daten für Slopes etc.), aber kein Snap.
            _motor.GroundSnappingEnabled = _groundDetectionStrategy.IsGrounded;

            // Landing Velocity Cap wird in UpdateVelocity (grounded branch) behandelt.

            // DEBUG: PostGrounding-Diagnose (gleicher Trigger wie UpdateVelocity)
            {
                var gs = _motor.GroundingStatus;
                float postY = _motor.TransientPosition.y;
                float postGroundY = gs.GroundPoint.y;
                float postGap = postY - postGroundY;
                bool postMoving = _groundDetectionStrategy.IsGrounded && _lastComputedHorizontal.sqrMagnitude > 0.01f;
                if (_debugLandingFrames > 0 || (postMoving && postGap > 0.03f))
                {
                    Debug.Log($"[Hovering-Post] Y={postY:F4} groundY={postGroundY:F4} gap={postGap:F4} " +
                        $"strategyGrounded={_groundDetectionStrategy.IsGrounded} snapEnabled={_motor.GroundSnappingEnabled} " +
                        $"lastStable={_motor.LastGroundingStatus.IsStableOnGround} lastSnapPrev={_motor.LastGroundingStatus.SnappingPrevented}");
                }
            }

            UpdateCachedGroundInfo();
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
            // Stair Detection: Steps in OnMovementHit tracken
            if (hitStabilityReport.ValidStepDetected)
            {
                float now = Time.time;
                if (now - _lastStepTime < StairDetectionWindow)
                {
                    _recentStepCount++;
                }
                else
                {
                    _recentStepCount = 1;
                }
                _lastStepTime = now;
            }
        }

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
            // Slope-Edge Fix: An scharfen Kanten von Rampen/BoxCollidern erkennt die
            // Ledge-Detection den Übergang als unstable, obwohl die Hit-Normal im
            // begehbaren Bereich liegt (< MaxSlopeAngle). Das führt dazu, dass der
            // Motor die Kante als Wand behandelt und die Bewegung blockt.
            // Fix: Wenn die Hit-Normal stabil wäre aber Ledge-Detection sie überschreibt,
            // erzwingen wir Stabilität → Character kann über Kanten gehen.
            if (!hitStabilityReport.IsStable && hitStabilityReport.LedgeDetected)
            {
                float angleFromUp = Vector3.Angle(hitNormal, Vector3.up);
                if (angleFromUp <= _config.MaxSlopeAngle)
                {
                    hitStabilityReport.IsStable = true;
                }
            }
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

        /// <summary>
        /// Ob der Character aktuell auf Treppen läuft (mehrere Steps in kurzer Zeit).
        /// </summary>
        public bool IsOnStairs => _config.StairSpeedReductionEnabled
            && _recentStepCount >= StairStepThreshold
            && (Time.time - _lastStepTime) < StairDetectionWindow;

        /// <summary>
        /// Kombinierter Terrain-Speed-Multiplikator (Slope + Stair). 1.0 = flacher Boden.
        /// AnimatorParameterBridge kann damit die Animations-Geschwindigkeit kompensieren,
        /// damit die Fußbewegung zur visuellen Displacement-Rate passt.
        /// </summary>
        public float CurrentTerrainSpeedMultiplier => _currentTerrainSpeedMultiplier;

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

        /// <summary>
        /// Berechnet den Speed-Multiplikator basierend auf Slope-Winkel und Bewegungsrichtung.
        /// Bergauf: Reduktion (UphillSpeedPenalty), Bergab: Bonus (DownhillSpeedBonus).
        /// Skaliert linear mit slopeAngle / MaxSlopeAngle.
        /// </summary>
        private float CalculateSlopeSpeedMultiplier(Vector3 moveDirection, Vector3 groundNormal)
        {
            float slopeAngle = Vector3.Angle(groundNormal, Vector3.up);
            if (slopeAngle < 0.5f) return 1f; // Nahezu flach → kein Modifier

            float slopeFactor = _config.MaxSlopeAngle > 0f
                ? Mathf.Clamp01(slopeAngle / _config.MaxSlopeAngle)
                : 0f;

            // Bergauf/Bergab-Erkennung: Tangent-Projektion der Bewegungsrichtung auf die Slope-Surface.
            // Wenn die Y-Komponente des Tangent-Vektors positiv ist → bergauf, negativ → bergab.
            Vector3 tangent = _motor.GetDirectionTangentToSurface(moveDirection.normalized, groundNormal);
            bool goingUphill = tangent.y > 0.01f;

            float modifier = goingUphill
                ? -_config.UphillSpeedPenalty * slopeFactor    // Penalty → multiplier < 1
                : _config.DownhillSpeedBonus * slopeFactor;    // Bonus → multiplier > 1

            return Mathf.Clamp(1f + modifier, 0.1f, 2f);
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
