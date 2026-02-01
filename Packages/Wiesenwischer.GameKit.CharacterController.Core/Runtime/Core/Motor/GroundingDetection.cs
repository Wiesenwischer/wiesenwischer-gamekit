using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Locomotion;
using Wiesenwischer.GameKit.CharacterController.Core.StateMachine;

namespace Wiesenwischer.GameKit.CharacterController.Core.Motor
{
    /// <summary>
    /// System für Boden-Erkennung.
    /// Verwendet Raycast und SphereCast für zuverlässige Ground Detection.
    /// Teil der gemeinsamen Motor-Schicht für alle Locomotion-Typen.
    /// </summary>
    public class GroundingDetection
    {
        private readonly Transform _transform;
        private readonly ILocomotionConfig _config;
        private readonly float _characterRadius;
        private readonly float _characterHeight;

        // Cached Results
        private GroundInfo _groundInfo;
        private bool _wasGroundedLastFrame;

        // Debug
        private Vector3 _lastRaycastOrigin;
        private Vector3 _lastRaycastHit;

        /// <summary>
        /// Erstellt eine neue GroundingDetection Instanz.
        /// </summary>
        /// <param name="transform">Transform des Characters.</param>
        /// <param name="config">Locomotion-Konfiguration.</param>
        /// <param name="characterRadius">Radius des CapsuleColliders.</param>
        /// <param name="characterHeight">Höhe des CapsuleColliders.</param>
        public GroundingDetection(Transform transform, ILocomotionConfig config, float characterRadius, float characterHeight)
        {
            if (transform == null)
            {
                throw new System.ArgumentNullException(nameof(transform),
                    "[GroundingDetection] Transform darf nicht null sein.");
            }

            if (config == null)
            {
                throw new System.ArgumentNullException(nameof(config),
                    "[GroundingDetection] ILocomotionConfig darf nicht null sein.");
            }

            if (characterRadius <= 0f)
            {
                throw new System.ArgumentOutOfRangeException(nameof(characterRadius),
                    $"[GroundingDetection] characterRadius muss größer als 0 sein, ist aber {characterRadius}.");
            }

            if (characterHeight <= 0f)
            {
                throw new System.ArgumentOutOfRangeException(nameof(characterHeight),
                    $"[GroundingDetection] characterHeight muss größer als 0 sein, ist aber {characterHeight}.");
            }

            _transform = transform;
            _config = config;
            _characterRadius = characterRadius;
            _characterHeight = characterHeight;
            _groundInfo = GroundInfo.Empty;
        }

        /// <summary>Aktuelle Ground-Informationen.</summary>
        public GroundInfo GroundInfo => _groundInfo;

        /// <summary>Ob der Character auf dem Boden steht.</summary>
        public bool IsGrounded => _groundInfo.IsGrounded;

        /// <summary>Ob der Character gerade gelandet ist.</summary>
        public bool JustLanded => _groundInfo.IsGrounded && !_wasGroundedLastFrame;

        /// <summary>Ob der Character gerade den Boden verlassen hat.</summary>
        public bool JustLeftGround => !_groundInfo.IsGrounded && _wasGroundedLastFrame;

        /// <summary>Slope-Winkel direkt unter dem Character.</summary>
        public float SlopeAngleDirectlyBelow { get; private set; }

        /// <summary>Normal des Bodens direkt unter dem Character.</summary>
        public Vector3 SlopeNormalDirectlyBelow { get; private set; } = Vector3.up;

        /// <summary>Ob der Boden direkt unter dem Character begehbar ist.</summary>
        public bool IsWalkableDirectlyBelow { get; private set; } = true;

        // Persistente Slope-Daten
        private float _lastValidSlopeAngle;
        private Vector3 _lastValidSlopeNormal = Vector3.up;
        private float _slopePersistenceTimer;
        private const float SLOPE_PERSISTENCE_DURATION = 0.3f;

        /// <summary>
        /// Führt die Ground Detection aus.
        /// Sollte jeden Frame/Tick aufgerufen werden.
        /// </summary>
        public void UpdateGroundCheck()
        {
            _wasGroundedLastFrame = _groundInfo.IsGrounded;

            // Primärer Check: SphereCast für stabilere Detection
            if (PerformSphereCast(out GroundInfo sphereResult))
            {
                _groundInfo = sphereResult;
            }
            // Fallback: Raycast für präzisere Slope-Detection
            else if (PerformRaycast(out GroundInfo rayResult))
            {
                _groundInfo = rayResult;
            }
            else
            {
                _groundInfo = GroundInfo.Empty;
            }

            // Separater Raycast DIREKT nach unten für Slope Sliding
            UpdateSlopeDirectlyBelow();
        }

        /// <summary>
        /// Prüft den Slope-Winkel direkt unter dem Character.
        /// Verwendet Raycast vom Capsule-Zentrum für zuverlässige Erkennung.
        /// </summary>
        private void UpdateSlopeDirectlyBelow()
        {
            float detectedAngle = 0f;
            Vector3 detectedNormal = Vector3.up;
            bool foundGround = false;

            // PRIMÄRE METHODE: Raycast vom Capsule-Zentrum nach unten
            Vector3 capsuleCenter = _transform.position + Vector3.up * (_characterHeight * 0.5f);
            float rayDistance = _characterHeight * 0.5f + 1f;

            if (Physics.Raycast(
                capsuleCenter,
                Vector3.down,
                out RaycastHit centerHit,
                rayDistance,
                _config.GroundLayers,
                QueryTriggerInteraction.Ignore))
            {
                detectedAngle = Vector3.Angle(centerHit.normal, Vector3.up);
                detectedNormal = centerHit.normal;
                foundGround = true;

                Debug.Log($"[SlopeBelow-CenterRay] Angle: {detectedAngle:F1}°, MaxSlope: {_config.MaxSlopeAngle}°, Distance: {centerHit.distance:F2}");
            }

            // FALLBACK: SphereCast für breitere Detektion
            if (!foundGround)
            {
                Vector3 sphereOrigin = _transform.position + Vector3.up * (_characterRadius + 0.05f);
                float sphereCheckDistance = 2f;

                if (Physics.SphereCast(
                    sphereOrigin,
                    _characterRadius * 0.5f,
                    Vector3.down,
                    out RaycastHit sphereHit,
                    sphereCheckDistance,
                    _config.GroundLayers,
                    QueryTriggerInteraction.Ignore))
                {
                    detectedAngle = Vector3.Angle(sphereHit.normal, Vector3.up);
                    detectedNormal = sphereHit.normal;
                    foundGround = true;
                    Debug.Log($"[SlopeBelow-SphereCast] Angle: {detectedAngle:F1}°");
                }
            }

            // Bestimme ob dieser Winkel als "steep slope" gilt
            bool isSteepSlope = foundGround && detectedAngle >= _config.MaxSlopeAngle && detectedAngle < 85f;

            // Aktualisiere Slope-Daten
            if (foundGround)
            {
                SlopeAngleDirectlyBelow = detectedAngle;
                SlopeNormalDirectlyBelow = detectedNormal;
                IsWalkableDirectlyBelow = detectedAngle < _config.MaxSlopeAngle;

                if (isSteepSlope)
                {
                    Debug.Log($"[SlopeBelow-STEEP] Angle: {detectedAngle:F1}° >= MaxSlope: {_config.MaxSlopeAngle}° -> SLIDING!");
                }

                _lastValidSlopeAngle = detectedAngle;
                _lastValidSlopeNormal = detectedNormal;
                _slopePersistenceTimer = SLOPE_PERSISTENCE_DURATION;
            }
            else if (_slopePersistenceTimer > 0f)
            {
                _slopePersistenceTimer -= Time.deltaTime;

                SlopeAngleDirectlyBelow = _lastValidSlopeAngle;
                SlopeNormalDirectlyBelow = _lastValidSlopeNormal;
                IsWalkableDirectlyBelow = _lastValidSlopeAngle < _config.MaxSlopeAngle;

                Debug.Log($"[SlopeBelow-Persisted] Angle: {_lastValidSlopeAngle:F1}°, Timer: {_slopePersistenceTimer:F2}s");
            }
            else
            {
                SlopeAngleDirectlyBelow = 0f;
                SlopeNormalDirectlyBelow = Vector3.up;
                IsWalkableDirectlyBelow = true;
            }
        }

        /// <summary>
        /// Führt die Ground Detection mit angepasster Position aus.
        /// Nützlich für Prediction/Simulation.
        /// </summary>
        public GroundInfo CheckGroundAtPosition(Vector3 position)
        {
            Vector3 origin = position + Vector3.up * (_characterRadius + 0.01f);
            float checkDistance = _config.GroundCheckDistance + _characterRadius;

            if (Physics.SphereCast(
                origin,
                _config.GroundCheckRadius,
                Vector3.down,
                out RaycastHit hit,
                checkDistance,
                _config.GroundLayers,
                QueryTriggerInteraction.Ignore))
            {
                return CreateGroundInfo(hit, origin);
            }

            return GroundInfo.Empty;
        }

        #region Detection Methods

        private bool PerformSphereCast(out GroundInfo result)
        {
            Vector3 origin = _transform.position + Vector3.up * (_characterRadius + 0.01f);
            float checkDistance = _config.GroundCheckDistance + _characterRadius;

            _lastRaycastOrigin = origin;

            if (Physics.SphereCast(
                origin,
                _config.GroundCheckRadius,
                Vector3.down,
                out RaycastHit hit,
                checkDistance,
                _config.GroundLayers,
                QueryTriggerInteraction.Ignore))
            {
                _lastRaycastHit = hit.point;
                result = CreateGroundInfo(hit, origin);
                return true;
            }

            result = GroundInfo.Empty;
            return false;
        }

        private bool PerformRaycast(out GroundInfo result)
        {
            Vector3 center = _transform.position + Vector3.up * 0.1f;
            float checkDistance = _config.GroundCheckDistance + 0.1f;

            // Center Raycast
            if (Physics.Raycast(
                center,
                Vector3.down,
                out RaycastHit hit,
                checkDistance,
                _config.GroundLayers,
                QueryTriggerInteraction.Ignore))
            {
                result = CreateGroundInfo(hit, center);
                return true;
            }

            // Offset Raycasts
            float offset = _characterRadius * 0.5f;
            Vector3[] offsets = new Vector3[]
            {
                _transform.forward * offset,
                -_transform.forward * offset,
                _transform.right * offset,
                -_transform.right * offset
            };

            foreach (var o in offsets)
            {
                Vector3 origin = center + o;
                if (Physics.Raycast(
                    origin,
                    Vector3.down,
                    out hit,
                    checkDistance,
                    _config.GroundLayers,
                    QueryTriggerInteraction.Ignore))
                {
                    result = CreateGroundInfo(hit, origin);
                    return true;
                }
            }

            result = GroundInfo.Empty;
            return false;
        }

        private GroundInfo CreateGroundInfo(RaycastHit hit, Vector3 origin)
        {
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            float distance = hit.distance;
            bool isWalkable = slopeAngle < _config.MaxSlopeAngle;

            return new GroundInfo
            {
                IsGrounded = true,
                Point = hit.point,
                Normal = hit.normal,
                SlopeAngle = slopeAngle,
                Distance = distance,
                IsWalkable = isWalkable
            };
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Berechnet die Bewegungsrichtung auf einer Slope.
        /// </summary>
        public Vector3 GetSlopeDirection(Vector3 moveDirection)
        {
            if (!_groundInfo.IsGrounded)
            {
                return moveDirection;
            }

            Vector3 slopeDirection = Vector3.ProjectOnPlane(moveDirection, _groundInfo.Normal).normalized;

            if (!_groundInfo.IsWalkable && Vector3.Dot(slopeDirection, Vector3.up) > 0)
            {
                slopeDirection = Vector3.ProjectOnPlane(slopeDirection, Vector3.up).normalized;
            }

            return slopeDirection;
        }

        /// <summary>
        /// Prüft ob eine Stufe vor dem Character ist.
        /// </summary>
        public bool CheckForStep(Vector3 moveDirection, out float stepHeight)
        {
            stepHeight = 0f;

            if (!_groundInfo.IsGrounded || moveDirection.sqrMagnitude < 0.01f)
            {
                return false;
            }

            Vector3 checkOrigin = _transform.position + Vector3.up * 0.05f;
            Vector3 checkDirection = moveDirection.normalized;

            if (!Physics.Raycast(
                checkOrigin,
                checkDirection,
                out RaycastHit obstacleHit,
                _characterRadius + 0.1f,
                _config.GroundLayers,
                QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            Vector3 topOrigin = checkOrigin + Vector3.up * _config.MaxStepHeight + checkDirection * (_characterRadius + 0.1f);

            if (Physics.Raycast(
                topOrigin,
                Vector3.down,
                out RaycastHit topHit,
                _config.MaxStepHeight + 0.1f,
                _config.GroundLayers,
                QueryTriggerInteraction.Ignore))
            {
                stepHeight = topHit.point.y - _transform.position.y;
                float topSlopeAngle = Vector3.Angle(topHit.normal, Vector3.up);

                return stepHeight > 0.01f &&
                       stepHeight <= _config.MaxStepHeight &&
                       topSlopeAngle <= _config.MaxSlopeAngle;
            }

            return false;
        }

        /// <summary>
        /// Gibt den nächsten Punkt auf dem Boden unter einer Position zurück.
        /// </summary>
        public bool GetGroundPoint(Vector3 position, out Vector3 groundPoint)
        {
            groundPoint = position;

            if (Physics.Raycast(
                position + Vector3.up * 0.5f,
                Vector3.down,
                out RaycastHit hit,
                10f,
                _config.GroundLayers,
                QueryTriggerInteraction.Ignore))
            {
                groundPoint = hit.point;
                return true;
            }

            return false;
        }

        #endregion

        #region Debug

        /// <summary>
        /// Zeichnet Debug-Gizmos für die Ground Detection.
        /// </summary>
        public void DrawDebugGizmos()
        {
#if UNITY_EDITOR
            if (_transform == null) return;

            Vector3 origin = _transform.position + Vector3.up * (_characterRadius + 0.01f);
            float checkDistance = _config.GroundCheckDistance + _characterRadius;
            Vector3 endPoint = origin + Vector3.down * checkDistance;

            Gizmos.color = _groundInfo.IsGrounded
                ? (_groundInfo.IsWalkable ? Color.green : Color.yellow)
                : Color.red;

            Gizmos.DrawWireSphere(origin, _config.GroundCheckRadius);
            Gizmos.DrawWireSphere(endPoint, _config.GroundCheckRadius);
            Gizmos.DrawLine(origin, endPoint);

            if (_groundInfo.IsGrounded)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(_groundInfo.Point, 0.05f);

                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(_groundInfo.Point, _groundInfo.Point + _groundInfo.Normal * 0.5f);
            }
#endif
        }

        #endregion
    }
}
