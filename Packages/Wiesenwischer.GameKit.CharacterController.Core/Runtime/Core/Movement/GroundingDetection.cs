using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.StateMachine;

namespace Wiesenwischer.GameKit.CharacterController.Core.Movement
{
    /// <summary>
    /// System für Boden-Erkennung.
    /// Verwendet Raycast und SphereCast für zuverlässige Ground Detection.
    /// </summary>
    public class GroundingDetection
    {
        private readonly Transform _transform;
        private readonly IMovementConfig _config;
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
        /// <param name="config">Movement-Konfiguration.</param>
        /// <param name="characterRadius">Radius des CharacterControllers.</param>
        /// <param name="characterHeight">Höhe des CharacterControllers.</param>
        /// <exception cref="System.ArgumentNullException">Wenn transform oder config null ist.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Wenn characterRadius oder characterHeight ungültig ist.</exception>
        public GroundingDetection(Transform transform, IMovementConfig config, float characterRadius, float characterHeight)
        {
            if (transform == null)
            {
                throw new System.ArgumentNullException(nameof(transform),
                    "[GroundingDetection] Transform darf nicht null sein. " +
                    "Stelle sicher, dass das GameObject ein gültiges Transform hat.");
            }

            if (config == null)
            {
                throw new System.ArgumentNullException(nameof(config),
                    "[GroundingDetection] IMovementConfig darf nicht null sein. " +
                    "Weise eine MovementConfig im Inspector zu oder übergebe eine gültige Konfiguration.");
            }

            if (characterRadius <= 0f)
            {
                throw new System.ArgumentOutOfRangeException(nameof(characterRadius),
                    $"[GroundingDetection] characterRadius muss größer als 0 sein, ist aber {characterRadius}. " +
                    "Prüfe die CharacterController-Einstellungen.");
            }

            if (characterHeight <= 0f)
            {
                throw new System.ArgumentOutOfRangeException(nameof(characterHeight),
                    $"[GroundingDetection] characterHeight muss größer als 0 sein, ist aber {characterHeight}. " +
                    "Prüfe die CharacterController-Einstellungen.");
            }

            _transform = transform;
            _config = config;
            _characterRadius = characterRadius;
            _characterHeight = characterHeight;
            _groundInfo = GroundInfo.Empty;
        }

        /// <summary>
        /// Aktuelle Ground-Informationen.
        /// </summary>
        public GroundInfo GroundInfo => _groundInfo;

        /// <summary>
        /// Ob der Character auf dem Boden steht.
        /// </summary>
        public bool IsGrounded => _groundInfo.IsGrounded;

        /// <summary>
        /// Ob der Character gerade gelandet ist (war in der Luft, jetzt auf dem Boden).
        /// </summary>
        public bool JustLanded => _groundInfo.IsGrounded && !_wasGroundedLastFrame;

        /// <summary>
        /// Ob der Character gerade den Boden verlassen hat.
        /// </summary>
        public bool JustLeftGround => !_groundInfo.IsGrounded && _wasGroundedLastFrame;

        /// <summary>
        /// Slope-Winkel direkt unter dem Character (via Raycast, nicht SphereCast).
        /// Verwendet für Slope Sliding, um Stufen-Kanten auszuschließen.
        /// </summary>
        public float SlopeAngleDirectlyBelow { get; private set; }

        /// <summary>
        /// Normal des Bodens direkt unter dem Character.
        /// </summary>
        public Vector3 SlopeNormalDirectlyBelow { get; private set; } = Vector3.up;

        /// <summary>
        /// Ob der Boden direkt unter dem Character begehbar ist.
        /// </summary>
        public bool IsWalkableDirectlyBelow { get; private set; } = true;

        // Collision-basierte Ground Detection (von OnControllerColliderHit)
        private bool _hasCollisionData;
        private Vector3 _collisionGroundNormal = Vector3.up;
        private float _collisionGroundAngle;

        // Persistente Slope-Daten (bleiben über mehrere Frames erhalten)
        private float _lastValidSlopeAngle;
        private Vector3 _lastValidSlopeNormal = Vector3.up;
        private float _slopePersistenceTimer;
        private const float SLOPE_PERSISTENCE_DURATION = 0.3f; // Wie lange Slope-Daten ohne neue Detektion erhalten bleiben

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
            // (SphereCast kann Stufen-Kanten VOR dem Character treffen)
            UpdateSlopeDirectlyBelow();
        }

        /// <summary>
        /// Setzt die Ground-Normal von OnControllerColliderHit.
        /// Dies ist die zuverlässigste Quelle für die Boden-Normal.
        /// </summary>
        public void SetGroundNormalFromCollision(Vector3 normal, float angle)
        {
            _hasCollisionData = true;
            _collisionGroundNormal = normal;
            _collisionGroundAngle = angle;
        }

        /// <summary>
        /// Prüft den Slope-Winkel direkt unter dem Character.
        /// Verwendet mehrere Detektionsmethoden für zuverlässige Erkennung.
        /// </summary>
        private void UpdateSlopeDirectlyBelow()
        {
            bool foundValidSlope = false;
            float detectedAngle = 0f;
            Vector3 detectedNormal = Vector3.up;

            // METHODE 1: Kollisionsdaten vom CharacterController (OnControllerColliderHit)
            // Höchste Priorität - diese zeigen die echte Kontaktfläche
            if (_hasCollisionData)
            {
                detectedAngle = _collisionGroundAngle;
                detectedNormal = _collisionGroundNormal;
                foundValidSlope = true;
                _hasCollisionData = false;

                if (detectedAngle > 10f)
                {
                    Debug.Log($"[SlopeBelow-Collision] Angle: {detectedAngle:F1}°");
                }
            }

            // METHODE 2: SphereCast für breitere Detektion
            if (!foundValidSlope)
            {
                Vector3 sphereOrigin = _transform.position + Vector3.up * (_characterRadius + 0.05f);
                float sphereCheckDistance = 2f; // Längere Distanz für Slope-Detektion

                if (Physics.SphereCast(
                    sphereOrigin,
                    _characterRadius * 0.5f, // Kleinerer Radius für präzisere Slope-Detektion
                    Vector3.down,
                    out RaycastHit sphereHit,
                    sphereCheckDistance,
                    _config.GroundLayers,
                    QueryTriggerInteraction.Ignore))
                {
                    float angle = Vector3.Angle(sphereHit.normal, Vector3.up);
                    if (angle > _config.MaxSlopeAngle && angle < 85f)
                    {
                        detectedAngle = angle;
                        detectedNormal = sphereHit.normal;
                        foundValidSlope = true;

                        Debug.Log($"[SlopeBelow-SphereCast] Angle: {angle:F1}°, Distance: {sphereHit.distance:F2}");
                    }
                }
            }

            // METHODE 3: Multi-Raycast für präzise Detektion
            if (!foundValidSlope)
            {
                float rayCheckDistance = 3f; // Noch längere Distanz für Fallsituationen

                // Mehrere Raycasts: Center, Forward, und an den Seiten
                Vector3[] rayOrigins = new Vector3[]
                {
                    _transform.position + Vector3.up * 0.1f,
                    _transform.position + Vector3.up * 0.1f + _transform.forward * _characterRadius * 0.5f,
                    _transform.position + Vector3.up * 0.1f - _transform.forward * _characterRadius * 0.5f,
                };

                foreach (var origin in rayOrigins)
                {
                    if (Physics.Raycast(
                        origin,
                        Vector3.down,
                        out RaycastHit hit,
                        rayCheckDistance,
                        _config.GroundLayers,
                        QueryTriggerInteraction.Ignore))
                    {
                        float angle = Vector3.Angle(hit.normal, Vector3.up);

                        // Nur steile Slopes (> MaxSlopeAngle) aber keine Wände (< 85°)
                        if (angle > _config.MaxSlopeAngle && angle < 85f)
                        {
                            detectedAngle = angle;
                            detectedNormal = hit.normal;
                            foundValidSlope = true;

                            Debug.Log($"[SlopeBelow-Raycast] Angle: {angle:F1}°, Distance: {hit.distance:F2}");
                            break;
                        }
                    }
                }
            }

            // Aktualisiere Slope-Daten
            if (foundValidSlope)
            {
                // Neue valide Slope-Daten gefunden
                SlopeAngleDirectlyBelow = detectedAngle;
                SlopeNormalDirectlyBelow = detectedNormal;
                IsWalkableDirectlyBelow = detectedAngle <= _config.MaxSlopeAngle;

                // Speichere als letzte valide Daten
                _lastValidSlopeAngle = detectedAngle;
                _lastValidSlopeNormal = detectedNormal;
                _slopePersistenceTimer = SLOPE_PERSISTENCE_DURATION;
            }
            else if (_slopePersistenceTimer > 0f)
            {
                // Keine neue Detektion, aber vorherige Daten noch gültig
                // (Character könnte kurzzeitig den Kontakt verloren haben)
                _slopePersistenceTimer -= Time.deltaTime;

                SlopeAngleDirectlyBelow = _lastValidSlopeAngle;
                SlopeNormalDirectlyBelow = _lastValidSlopeNormal;
                IsWalkableDirectlyBelow = _lastValidSlopeAngle <= _config.MaxSlopeAngle;

                Debug.Log($"[SlopeBelow-Persisted] Angle: {_lastValidSlopeAngle:F1}°, Timer: {_slopePersistenceTimer:F2}s");
            }
            else
            {
                // Keine Slope-Daten und Persistence abgelaufen - flacher Boden
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
            // SphereCast von der gegebenen Position
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
            // Origin: Leicht über dem Boden, in der Mitte des Characters
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
            // Mehrere Raycasts für bessere Coverage
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

            // Offset Raycasts (vorne, hinten, links, rechts)
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
            // Berechne Slope Angle
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);

            // Berechne Distanz zum Boden
            float distance = hit.distance;

            // Prüfe ob begehbar
            bool isWalkable = slopeAngle <= _config.MaxSlopeAngle;

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
        /// Projiziert die Bewegungsrichtung auf die Oberfläche.
        /// </summary>
        public Vector3 GetSlopeDirection(Vector3 moveDirection)
        {
            if (!_groundInfo.IsGrounded)
            {
                return moveDirection;
            }

            // Projiziere Bewegungsrichtung auf die Slope
            Vector3 slopeDirection = Vector3.ProjectOnPlane(moveDirection, _groundInfo.Normal).normalized;

            // Wenn wir bergauf gehen und der Slope zu steil ist, stoppe
            if (!_groundInfo.IsWalkable && Vector3.Dot(slopeDirection, Vector3.up) > 0)
            {
                // Entferne die aufwärts-Komponente
                slopeDirection = Vector3.ProjectOnPlane(slopeDirection, Vector3.up).normalized;
            }

            return slopeDirection;
        }

        /// <summary>
        /// Prüft ob eine Stufe vor dem Character ist und gibt die Höhe zurück.
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

            // Erster Raycast: Prüfe ob ein Hindernis vor uns ist
            if (!Physics.Raycast(
                checkOrigin,
                checkDirection,
                out RaycastHit obstacleHit,
                _characterRadius + 0.1f,
                _config.GroundLayers,
                QueryTriggerInteraction.Ignore))
            {
                return false; // Kein Hindernis
            }

            // Zweiter Raycast: Prüfe von oben, wie hoch die Stufe ist
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

                // Prüfe ob die Stufe begehbar ist
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
        /// Sollte in OnDrawGizmos aufgerufen werden.
        /// </summary>
        public void DrawDebugGizmos()
        {
#if UNITY_EDITOR
            if (_transform == null) return;

            // Ground Check Sphere
            Vector3 origin = _transform.position + Vector3.up * (_characterRadius + 0.01f);
            float checkDistance = _config.GroundCheckDistance + _characterRadius;
            Vector3 endPoint = origin + Vector3.down * checkDistance;

            // Farbe basierend auf Ground Status
            Gizmos.color = _groundInfo.IsGrounded
                ? (_groundInfo.IsWalkable ? Color.green : Color.yellow)
                : Color.red;

            // Zeichne Sphere am Start und Ende
            Gizmos.DrawWireSphere(origin, _config.GroundCheckRadius);
            Gizmos.DrawWireSphere(endPoint, _config.GroundCheckRadius);

            // Zeichne Linie zwischen den Spheres
            Gizmos.DrawLine(origin, endPoint);

            // Zeichne Hit Point
            if (_groundInfo.IsGrounded)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(_groundInfo.Point, 0.05f);

                // Zeichne Normal
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(_groundInfo.Point, _groundInfo.Point + _groundInfo.Normal * 0.5f);
            }
#endif
        }

        #endregion
    }
}
