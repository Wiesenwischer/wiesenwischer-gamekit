using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.StateMachine;

namespace Wiesenwischer.GameKit.CharacterController.Core.Movement
{
    /// <summary>
    /// Deterministischer Movement Simulator.
    /// Berechnet Bewegung basierend auf Input und Config.
    /// Verwendet festes Delta für CSP-Kompatibilität.
    /// </summary>
    public class MovementMotor : IMovementController
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

        // Sliding State (persistent über Frames)
        private bool _isSliding;
        private float _slidingTime;
        private Vector3 _currentSlideDirection;
        private float _noSlopeContactTime;
        private const float SLIDE_EXIT_DELAY = 0.15f; // Zeit ohne Slope-Kontakt bevor Sliding endet

        // Rotation
        private float _targetYaw;
        private float _currentYaw;

        /// <summary>
        /// Erstellt einen neuen MovementMotor.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">Wenn characterController oder config null ist.</exception>
        public MovementMotor(
            UnityEngine.CharacterController characterController,
            IMovementConfig config)
        {
            if (characterController == null)
            {
                throw new System.ArgumentNullException(nameof(characterController),
                    "[MovementMotor] CharacterController darf nicht null sein. " +
                    "Füge eine CharacterController-Komponente zum GameObject hinzu.");
            }

            if (config == null)
            {
                throw new System.ArgumentNullException(nameof(config),
                    "[MovementMotor] IMovementConfig darf nicht null sein. " +
                    "Weise eine MovementConfig im Inspector zu.");
            }

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
        /// Erstellt einen neuen MovementMotor mit externer GroundingDetection.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">Wenn eine Abhängigkeit null ist.</exception>
        public MovementMotor(
            Transform transform,
            UnityEngine.CharacterController characterController,
            IMovementConfig config,
            GroundingDetection groundingDetection)
        {
            if (transform == null)
            {
                throw new System.ArgumentNullException(nameof(transform),
                    "[MovementMotor] Transform darf nicht null sein.");
            }

            if (characterController == null)
            {
                throw new System.ArgumentNullException(nameof(characterController),
                    "[MovementMotor] CharacterController darf nicht null sein. " +
                    "Füge eine CharacterController-Komponente zum GameObject hinzu.");
            }

            if (config == null)
            {
                throw new System.ArgumentNullException(nameof(config),
                    "[MovementMotor] IMovementConfig darf nicht null sein. " +
                    "Weise eine MovementConfig im Inspector zu.");
            }

            if (groundingDetection == null)
            {
                throw new System.ArgumentNullException(nameof(groundingDetection),
                    "[MovementMotor] GroundingDetection darf nicht null sein. " +
                    "Erstelle eine GroundingDetection-Instanz oder verwende den anderen Konstruktor.");
            }

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

        /// <summary>
        /// Ob der Character gerade auf einem steilen Hang rutscht.
        /// </summary>
        public bool IsSliding => _isSliding;

        /// <summary>
        /// Wie lange der Character bereits rutscht (in Sekunden).
        /// </summary>
        public float SlidingTime => _slidingTime;

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

            // 4. Step Detection ZUERST (vor Slope Handling)
            Vector3 moveDirection = _horizontalVelocity;
            bool isClimbingStep = false;

            if (_isGrounded && _horizontalVelocity.sqrMagnitude > 0.01f)
            {
                if (_groundingDetection.CheckForStep(_horizontalVelocity, out float stepHeight))
                {
                    // Stufe erkannt - überspringe Slope Sliding und erlaube normale Bewegung
                    isClimbingStep = true;
                    Debug.Log($"[Step] Stufe erkannt! Höhe: {stepHeight:F2}m");
                    // CharacterController.stepOffset handhabt das Step-Up automatisch
                }
            }

            // 5. Slope Handling mit persistenter Sliding-State
            float slopeAngleBelow = _groundingDetection.SlopeAngleDirectlyBelow;
            bool isWalkableBelow = _groundingDetection.IsWalkableDirectlyBelow;
            Vector3 slopeNormal = _groundingDetection.SlopeNormalDirectlyBelow;

            // Prüfe ob wir auf einem steilen Slope sind
            bool onSteepSlope = !isWalkableBelow && slopeAngleBelow > _config.MaxSlopeAngle && slopeAngleBelow < 85f;

            // Sliding State Management (persistent über Frames)
            if (onSteepSlope)
            {
                // Steiler Slope erkannt - starte oder setze Sliding fort
                _noSlopeContactTime = 0f;

                if (!_isSliding)
                {
                    // Sliding startet
                    _isSliding = true;
                    _slidingTime = 0f;
                    Debug.Log($"[Sliding-START] Angle: {slopeAngleBelow:F1}°");
                }

                // Berechne Slide-Richtung
                _currentSlideDirection = CalculateSlideDirection(slopeNormal);
                _slidingTime += deltaTime;
            }
            else if (_isSliding)
            {
                // Kein Slope erkannt, aber wir waren am Sliden
                _noSlopeContactTime += deltaTime;

                // Prüfe ob wir auf begehbarem Boden gelandet sind
                if (_isGrounded && isWalkableBelow)
                {
                    // Auf flachem Boden gelandet - Sliding sofort beenden
                    _isSliding = false;
                    _slidingTime = 0f;
                    _noSlopeContactTime = 0f;
                    Debug.Log($"[Sliding-END] Landed on walkable ground");
                }
                else if (_noSlopeContactTime > SLIDE_EXIT_DELAY)
                {
                    // Zu lange ohne Slope-Kontakt - Sliding beenden
                    _isSliding = false;
                    _slidingTime = 0f;
                    _noSlopeContactTime = 0f;
                    Debug.Log($"[Sliding-END] No slope contact timeout");
                }
            }

            // Bewegung berechnen
            if (!isClimbingStep)
            {
                if (_isSliding)
                {
                    // SLIDING-MODUS: Rutschen am Hang
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

                    // Zusätzliche Beschleunigung über Zeit (wie echtes Rutschen)
                    float timeAcceleration = Mathf.Min(_slidingTime * 0.5f, 1f); // Max +100% nach 2 Sekunden
                    slideSpeed *= (1f + timeAcceleration);

                    Debug.Log($"[Sliding] Angle: {slopeAngleBelow:F1}°, Speed: {slideSpeed:F2}, Time: {_slidingTime:F2}s");

                    // Slide-Velocity berechnen
                    Vector3 slideVelocity = _currentSlideDirection * slideSpeed;

                    // Horizontale und vertikale Komponenten trennen
                    moveDirection = new Vector3(slideVelocity.x, 0, slideVelocity.z);
                    _horizontalVelocity = moveDirection;
                    _verticalVelocity = slideVelocity.y;

                    // Spieler-Input für leichte Quersteuerung
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

                        // Nur Bewegung erlauben, die nicht bergauf geht
                        float upwardComponent = Vector3.Dot(inputDir.normalized, -_currentSlideDirection);
                        if (upwardComponent < 0.5f)
                        {
                            moveDirection += inputDir * _config.AirControl;
                        }
                    }
                }
                else if (_isGrounded && isWalkableBelow)
                {
                    // NORMAL-MODUS: Begehbarer Boden
                    moveDirection = _groundingDetection.GetSlopeDirection(_horizontalVelocity);
                    moveDirection = moveDirection.normalized * _horizontalVelocity.magnitude;
                }
                // Sonst: In der Luft oder an Wand - normale Bewegung mit Gravity
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

            // 9. Snap to Ground wenn geerdet (NICHT beim Sliding!)
            if (_isGrounded && _verticalVelocity <= 0 && !_isSliding)
            {
                SnapToGround();
            }
        }

        /// <summary>
        /// Berechnet die Rutschrichtung entlang eines Hangs.
        /// </summary>
        private Vector3 CalculateSlideDirection(Vector3 slopeNormal)
        {
            // Cross-Produkt für Querrichtung, dann nochmal Cross für Abwärtsrichtung
            Vector3 slopeRight = Vector3.Cross(slopeNormal, Vector3.up).normalized;

            if (slopeRight.sqrMagnitude > 0.001f)
            {
                // Hangabwärts = Cross von Normal und Querrichtung
                Vector3 slideDir = Vector3.Cross(slopeRight, slopeNormal).normalized;

                // Stelle sicher dass wir nach unten zeigen
                if (slideDir.y > 0)
                {
                    slideDir = -slideDir;
                }
                return slideDir;
            }

            // Fallback wenn Normal fast vertikal
            return Vector3.ProjectOnPlane(Vector3.down, slopeNormal).normalized;
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
