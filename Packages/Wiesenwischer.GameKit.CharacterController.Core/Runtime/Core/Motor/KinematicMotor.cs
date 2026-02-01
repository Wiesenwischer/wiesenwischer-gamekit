using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Locomotion;
using Wiesenwischer.GameKit.CharacterController.Core.StateMachine;

namespace Wiesenwischer.GameKit.CharacterController.Core.Motor
{
    /// <summary>
    /// Kinematischer Motor - gemeinsame Physik-Schicht für alle Locomotion-Typen.
    /// Verwendet CapsuleCollider für Kollisionserkennung und manuelle Physik.
    /// Deterministisch für CSP (Client-Side Prediction) Kompatibilität.
    ///
    /// Wird von verschiedenen Locomotion-Klassen verwendet:
    /// - CharacterLocomotion (Walking, Running, Jumping)
    /// - Zukünftig: RidingLocomotion, GlidingLocomotion, SwimmingLocomotion
    /// </summary>
    public class KinematicMotor
    {
        // Configuration
        private readonly Transform _transform;
        private readonly CapsuleCollider _capsule;
        private readonly ILocomotionConfig _config;

        // Capsule dimensions (cached)
        private readonly float _radius;
        private readonly float _height;
        private readonly float _skinWidth;

        // Physics settings
        private const int MaxCollisionIterations = 3;
        private const int MaxDepenetrationIterations = 5;
        private const float MinMoveDistance = 0.001f;
        private const float CollisionOffset = 0.01f;

        // State
        private Vector3 _velocity;
        private bool _isGrounded;
        private GroundInfo _groundInfo;
        private CollisionFlags _collisionFlags;

        /// <summary>
        /// Erstellt einen neuen kinematischen Motor.
        /// </summary>
        /// <param name="transform">Transform des Characters.</param>
        /// <param name="capsule">CapsuleCollider für Kollisionserkennung.</param>
        /// <param name="config">Locomotion-Konfiguration.</param>
        /// <param name="skinWidth">Abstand zur Oberfläche (ähnlich CharacterController.skinWidth).</param>
        public KinematicMotor(Transform transform, CapsuleCollider capsule, ILocomotionConfig config, float skinWidth = 0.02f)
        {
            if (transform == null)
                throw new System.ArgumentNullException(nameof(transform));
            if (capsule == null)
                throw new System.ArgumentNullException(nameof(capsule));
            if (config == null)
                throw new System.ArgumentNullException(nameof(config));

            _transform = transform;
            _capsule = capsule;
            _config = config;
            _skinWidth = Mathf.Max(0.001f, skinWidth);

            // Cache capsule dimensions
            _radius = capsule.radius;
            _height = capsule.height;

            _groundInfo = GroundInfo.Empty;
        }

        #region Properties

        /// <summary>Position des Characters.</summary>
        public Vector3 Position => _transform.position;

        /// <summary>Rotation des Characters.</summary>
        public Quaternion Rotation => _transform.rotation;

        /// <summary>Aktuelle Velocity (für Debug).</summary>
        public Vector3 Velocity => _velocity;

        /// <summary>Ob der Character auf dem Boden steht.</summary>
        public bool IsGrounded => _isGrounded;

        /// <summary>Ground-Informationen.</summary>
        public GroundInfo GroundInfo => _groundInfo;

        /// <summary>Collision Flags vom letzten Move.</summary>
        public CollisionFlags CollisionFlags => _collisionFlags;

        /// <summary>Radius der Capsule.</summary>
        public float Radius => _radius;

        /// <summary>Höhe der Capsule.</summary>
        public float Height => _height;

        /// <summary>Skin Width (Kollisionsabstand).</summary>
        public float SkinWidth => _skinWidth;

        #endregion

        #region Movement

        /// <summary>
        /// Bewegt den Character um den angegebenen Vektor.
        /// Kollidiert mit Geometry und löst Penetrationen auf.
        /// </summary>
        /// <param name="motion">Bewegungsvektor (nicht deltaTime-multipliziert).</param>
        /// <returns>CollisionFlags die anzeigen, wo Kollisionen auftraten.</returns>
        public CollisionFlags Move(Vector3 motion)
        {
            _collisionFlags = CollisionFlags.None;
            _velocity = motion;

            if (motion.sqrMagnitude < MinMoveDistance * MinMoveDistance)
            {
                // Nur Depenetration ohne Bewegung
                ResolveOverlaps();
                UpdateGroundState();
                return _collisionFlags;
            }

            Vector3 currentPosition = _transform.position;
            Vector3 targetPosition = currentPosition + motion;
            Vector3 remainingMotion = motion;

            // Iterative Kollisionsauflösung
            for (int i = 0; i < MaxCollisionIterations && remainingMotion.sqrMagnitude > MinMoveDistance * MinMoveDistance; i++)
            {
                if (CapsuleCast(currentPosition, remainingMotion.normalized, remainingMotion.magnitude, out RaycastHit hit))
                {
                    // Bewege bis zum Kollisionspunkt (minus skinWidth)
                    float moveDistance = Mathf.Max(0, hit.distance - _skinWidth);
                    currentPosition += remainingMotion.normalized * moveDistance;

                    // Bestimme Kollisionstyp basierend auf Normal
                    UpdateCollisionFlags(hit.normal);

                    // Slide entlang der Oberfläche
                    remainingMotion = SlideAlongSurface(remainingMotion, hit.normal, moveDistance);
                }
                else
                {
                    // Keine Kollision - bewege vollständig
                    currentPosition += remainingMotion;
                    break;
                }
            }

            // Setze finale Position
            _transform.position = currentPosition;

            // Löse verbleibende Überlappungen auf
            ResolveOverlaps();

            // Aktualisiere Ground State
            UpdateGroundState();

            return _collisionFlags;
        }

        /// <summary>
        /// Berechnet die verbleibende Bewegung nach Slide entlang einer Oberfläche.
        /// </summary>
        private Vector3 SlideAlongSurface(Vector3 motion, Vector3 normal, float distanceMoved)
        {
            // Berechne verbleibende Distanz
            float remainingDistance = motion.magnitude - distanceMoved;
            if (remainingDistance <= MinMoveDistance) return Vector3.zero;

            // Projiziere Bewegung auf Oberfläche
            Vector3 slideDirection = Vector3.ProjectOnPlane(motion, normal).normalized;
            return slideDirection * remainingDistance;
        }

        /// <summary>
        /// Aktualisiert CollisionFlags basierend auf der Kollisionsnormal.
        /// </summary>
        private void UpdateCollisionFlags(Vector3 normal)
        {
            float angle = Vector3.Angle(normal, Vector3.up);

            if (angle < 45f)
            {
                // Boden-Kollision
                _collisionFlags |= CollisionFlags.Below;
            }
            else if (angle > 135f)
            {
                // Decken-Kollision
                _collisionFlags |= CollisionFlags.Above;
            }
            else
            {
                // Seiten-Kollision
                _collisionFlags |= CollisionFlags.Sides;
            }
        }

        #endregion

        #region Collision Detection

        /// <summary>
        /// Führt einen CapsuleCast in der angegebenen Richtung aus.
        /// </summary>
        private bool CapsuleCast(Vector3 position, Vector3 direction, float distance, out RaycastHit hit)
        {
            GetCapsulePoints(position, out Vector3 point1, out Vector3 point2);

            // Reduziere Radius um skinWidth für CapsuleCast
            float castRadius = _radius - _skinWidth;

            return Physics.CapsuleCast(
                point1,
                point2,
                castRadius,
                direction,
                out hit,
                distance + _skinWidth,
                _config.GroundLayers,
                QueryTriggerInteraction.Ignore
            );
        }

        /// <summary>
        /// Prüft auf Überlappungen mit anderen Collidern.
        /// </summary>
        private Collider[] CheckOverlap(Vector3 position)
        {
            GetCapsulePoints(position, out Vector3 point1, out Vector3 point2);

            // Reduziere Radius leicht um false positives zu vermeiden
            float checkRadius = _radius - 0.001f;

            return Physics.OverlapCapsule(
                point1,
                point2,
                checkRadius,
                _config.GroundLayers,
                QueryTriggerInteraction.Ignore
            );
        }

        /// <summary>
        /// Berechnet die oberen und unteren Mittelpunkte der Capsule.
        /// </summary>
        private void GetCapsulePoints(Vector3 position, out Vector3 point1, out Vector3 point2)
        {
            // Capsule center offset (Unity Capsule default)
            Vector3 center = position + _capsule.center;

            // Halbe Höhe minus Radius ergibt Abstand zum Halbkugel-Mittelpunkt
            float halfHeight = (_height * 0.5f) - _radius;

            point1 = center + Vector3.up * halfHeight;  // Oben
            point2 = center - Vector3.up * halfHeight;  // Unten
        }

        /// <summary>
        /// Löst Überlappungen mit anderen Collidern auf (Depenetration).
        /// </summary>
        private void ResolveOverlaps()
        {
            for (int i = 0; i < MaxDepenetrationIterations; i++)
            {
                Collider[] overlaps = CheckOverlap(_transform.position);
                if (overlaps.Length == 0) break;

                foreach (var overlap in overlaps)
                {
                    // Ignoriere eigene Collider
                    if (overlap == _capsule) continue;
                    if (overlap.transform == _transform) continue;

                    // Berechne Depenetration
                    if (Physics.ComputePenetration(
                        _capsule, _transform.position, _transform.rotation,
                        overlap, overlap.transform.position, overlap.transform.rotation,
                        out Vector3 direction, out float distance))
                    {
                        // Bewege aus dem Collider heraus
                        _transform.position += direction * (distance + CollisionOffset);

                        // Update collision flags
                        UpdateCollisionFlags(direction);
                    }
                }
            }
        }

        #endregion

        #region Ground Detection

        /// <summary>
        /// Aktualisiert den Ground State via SphereCast nach unten.
        /// </summary>
        private void UpdateGroundState()
        {
            Vector3 position = _transform.position;
            Vector3 origin = position + Vector3.up * (_radius + 0.01f);
            float checkDistance = _config.GroundCheckDistance + _radius;

            if (Physics.SphereCast(
                origin,
                _config.GroundCheckRadius,
                Vector3.down,
                out RaycastHit hit,
                checkDistance,
                _config.GroundLayers,
                QueryTriggerInteraction.Ignore))
            {
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                bool isWalkable = slopeAngle < _config.MaxSlopeAngle;

                _groundInfo = new GroundInfo
                {
                    IsGrounded = true,
                    Point = hit.point,
                    Normal = hit.normal,
                    SlopeAngle = slopeAngle,
                    Distance = hit.distance,
                    IsWalkable = isWalkable
                };

                _isGrounded = true;
                _collisionFlags |= CollisionFlags.Below;
            }
            else
            {
                _groundInfo = GroundInfo.Empty;
                _isGrounded = false;
            }
        }

        /// <summary>
        /// Führt einen Ground Check an einer bestimmten Position aus.
        /// </summary>
        public GroundInfo CheckGroundAtPosition(Vector3 position)
        {
            Vector3 origin = position + Vector3.up * (_radius + 0.01f);
            float checkDistance = _config.GroundCheckDistance + _radius;

            if (Physics.SphereCast(
                origin,
                _config.GroundCheckRadius,
                Vector3.down,
                out RaycastHit hit,
                checkDistance,
                _config.GroundLayers,
                QueryTriggerInteraction.Ignore))
            {
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                bool isWalkable = slopeAngle < _config.MaxSlopeAngle;

                return new GroundInfo
                {
                    IsGrounded = true,
                    Point = hit.point,
                    Normal = hit.normal,
                    SlopeAngle = slopeAngle,
                    Distance = hit.distance,
                    IsWalkable = isWalkable
                };
            }

            return GroundInfo.Empty;
        }

        #endregion

        #region Step Climbing

        /// <summary>
        /// Prüft ob eine Stufe vor dem Character ist und versucht sie zu erklimmen.
        /// </summary>
        /// <param name="moveDirection">Bewegungsrichtung.</param>
        /// <param name="stepUpPosition">Neue Position nach Step-Up.</param>
        /// <returns>True wenn Step-Up erfolgreich.</returns>
        public bool TryStepUp(Vector3 moveDirection, out Vector3 stepUpPosition)
        {
            stepUpPosition = _transform.position;

            if (!_isGrounded || moveDirection.sqrMagnitude < 0.01f)
                return false;

            Vector3 horizontalDir = new Vector3(moveDirection.x, 0, moveDirection.z).normalized;
            if (horizontalDir.sqrMagnitude < 0.01f)
                return false;

            Vector3 currentPos = _transform.position;

            // 1. Prüfe ob Hindernis vor uns
            Vector3 lowOrigin = currentPos + Vector3.up * 0.05f;
            if (!Physics.Raycast(lowOrigin, horizontalDir, out RaycastHit obstacleHit,
                _radius + 0.2f, _config.GroundLayers, QueryTriggerInteraction.Ignore))
            {
                return false; // Kein Hindernis
            }

            // 2. Prüfe von oben, ob Stufe begehbar
            Vector3 highOrigin = currentPos + Vector3.up * (_config.MaxStepHeight + 0.05f) + horizontalDir * (_radius + 0.1f);
            if (!Physics.Raycast(highOrigin, Vector3.down, out RaycastHit topHit,
                _config.MaxStepHeight + 0.1f, _config.GroundLayers, QueryTriggerInteraction.Ignore))
            {
                return false; // Keine begehbare Oberfläche oben
            }

            // 3. Prüfe Stufenhöhe und Slope
            float stepHeight = topHit.point.y - currentPos.y;
            float topSlopeAngle = Vector3.Angle(topHit.normal, Vector3.up);

            if (stepHeight < 0.01f || stepHeight > _config.MaxStepHeight)
                return false;

            if (topSlopeAngle >= _config.MaxSlopeAngle)
                return false; // Oberfläche zu steil

            // 4. Prüfe ob genug Platz oben (Capsule-Check)
            Vector3 testPosition = new Vector3(topHit.point.x, topHit.point.y + 0.01f, topHit.point.z);
            Collider[] overlaps = CheckOverlap(testPosition);
            if (overlaps.Length > 0)
            {
                // Prüfe ob nur der Boden überlappt
                bool onlyGround = true;
                foreach (var overlap in overlaps)
                {
                    if (overlap != _capsule && overlap.gameObject != _capsule.gameObject)
                    {
                        onlyGround = false;
                        break;
                    }
                }
                if (!onlyGround) return false;
            }

            stepUpPosition = testPosition;
            return true;
        }

        #endregion

        #region Utility

        /// <summary>
        /// Teleportiert den Character zu einer Position.
        /// </summary>
        public void SetPosition(Vector3 position)
        {
            _transform.position = position;
            UpdateGroundState();
        }

        /// <summary>
        /// Teleportiert den Character und setzt Rotation.
        /// </summary>
        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            _transform.position = position;
            _transform.rotation = rotation;
            UpdateGroundState();
        }

        #endregion

        #region Debug

        /// <summary>
        /// Zeichnet Debug-Gizmos für die Capsule und Ground Detection.
        /// </summary>
        public void DrawDebugGizmos()
        {
#if UNITY_EDITOR
            if (_transform == null) return;

            GetCapsulePoints(_transform.position, out Vector3 point1, out Vector3 point2);

            // Capsule zeichnen
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(point1, _radius);
            Gizmos.DrawWireSphere(point2, _radius);
            Gizmos.DrawLine(point1 + Vector3.left * _radius, point2 + Vector3.left * _radius);
            Gizmos.DrawLine(point1 + Vector3.right * _radius, point2 + Vector3.right * _radius);
            Gizmos.DrawLine(point1 + Vector3.forward * _radius, point2 + Vector3.forward * _radius);
            Gizmos.DrawLine(point1 + Vector3.back * _radius, point2 + Vector3.back * _radius);

            // Ground Point
            if (_groundInfo.IsGrounded)
            {
                Gizmos.color = _groundInfo.IsWalkable ? Color.blue : Color.yellow;
                Gizmos.DrawSphere(_groundInfo.Point, 0.05f);
                Gizmos.DrawLine(_groundInfo.Point, _groundInfo.Point + _groundInfo.Normal * 0.3f);
            }
#endif
        }

        #endregion
    }
}
