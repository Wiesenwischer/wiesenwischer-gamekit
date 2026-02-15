using UnityEngine;

namespace Wiesenwischer.GameKit.Camera.Behaviours
{
    /// <summary>
    /// Collision-Verhalten: Verhindert Camera-Clipping durch SphereCast.
    /// Muss nach ZoomBehaviour in der Behaviour-Liste stehen.
    /// </summary>
    public class CollisionBehaviour : MonoBehaviour, ICameraBehaviour, ICameraPresetReceiver
    {
        [Header("Collision")]
        [SerializeField] private float _collisionRadius = 0.3f;
        [SerializeField] private LayerMask _collisionLayers = ~0;

        [Header("Limits")]
        [Tooltip("Minimaler Abstand bei Collision (verhindert Clipping in den Character).")]
        [SerializeField] private float _minCollisionDistance = 0.5f;

        [Header("Recovery")]
        [Tooltip("Wie schnell die Kamera nach Kollision zurückgeht (0 = sofort).")]
        [SerializeField] private float _recoverySpeed = 5f;

        [Tooltip("Damping beim Heranziehen (verhindert Oszillation in engen Räumen).")]
        [SerializeField] private float _snapDamping = 0.05f;

        private float _currentCollisionDistance;
        private float _snapVelocity;
        private bool _initialized;

        public bool IsActive => enabled;

        public void ApplyPreset(CameraPreset preset)
        {
            enabled = true;
            _collisionRadius = preset.CollisionRadius;
            _recoverySpeed = preset.CollisionRecoverySpeed;
        }

        public void UpdateState(ref CameraState state, CameraContext ctx)
        {
            if (!_initialized)
            {
                _currentCollisionDistance = state.Distance;
                _initialized = true;
            }

            float desiredDistance = state.Distance;
            float hitDistance = CastForCollision(ctx.AnchorPosition, state, desiredDistance);

            // Minimum-Distanz erzwingen (Character nie unsichtbar)
            hitDistance = Mathf.Max(hitDistance, _minCollisionDistance);

            if (hitDistance < _currentCollisionDistance)
            {
                // Snap-In mit leichtem Damping (verhindert Oszillation unter Rampen etc.)
                _currentCollisionDistance = Mathf.SmoothDamp(
                    _currentCollisionDistance, hitDistance, ref _snapVelocity,
                    _snapDamping, Mathf.Infinity, ctx.DeltaTime);
            }
            else if (_recoverySpeed > 0f)
            {
                // Recovery: sanft zurück zur gewünschten Distanz
                _currentCollisionDistance = Mathf.MoveTowards(
                    _currentCollisionDistance, desiredDistance,
                    _recoverySpeed * ctx.DeltaTime);
                _snapVelocity = 0f;
            }
            else
            {
                _currentCollisionDistance = desiredDistance;
                _snapVelocity = 0f;
            }

            state.Distance = Mathf.Min(state.Distance, _currentCollisionDistance);
        }

        private float CastForCollision(Vector3 origin, CameraState state, float maxDistance)
        {
            Quaternion yawRot = Quaternion.Euler(0f, state.Yaw, 0f);
            Quaternion pitchRot = Quaternion.Euler(state.Pitch, 0f, 0f);
            Vector3 direction = yawRot * pitchRot * Vector3.back;

            if (Physics.SphereCast(origin, _collisionRadius, direction,
                out RaycastHit hit, maxDistance, _collisionLayers,
                QueryTriggerInteraction.Ignore))
            {
                return Mathf.Max(_minCollisionDistance, hit.distance - _collisionRadius);
            }

            return maxDistance;
        }
    }
}
