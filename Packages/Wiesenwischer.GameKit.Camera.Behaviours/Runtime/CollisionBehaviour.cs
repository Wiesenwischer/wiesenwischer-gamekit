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

        [Header("Recovery")]
        [Tooltip("Wie schnell die Kamera nach Kollision zurückgeht (0 = sofort).")]
        [SerializeField] private float _recoverySpeed = 5f;

        private float _currentCollisionDistance;

        public bool IsActive => enabled;

        public void ApplyPreset(CameraPreset preset)
        {
            _collisionRadius = preset.CollisionRadius;
            _recoverySpeed = preset.CollisionRecoverySpeed;
        }

        public void UpdateState(ref CameraState state, CameraContext ctx)
        {
            float desiredDistance = state.Distance;
            float hitDistance = CastForCollision(ctx.AnchorPosition, state);

            // Snap-In (sofort näher), Recovery (sanft zurück)
            if (hitDistance < desiredDistance)
            {
                _currentCollisionDistance = hitDistance;
            }
            else if (_recoverySpeed > 0f)
            {
                _currentCollisionDistance = Mathf.MoveTowards(
                    _currentCollisionDistance, desiredDistance,
                    _recoverySpeed * ctx.DeltaTime);
            }
            else
            {
                _currentCollisionDistance = desiredDistance;
            }

            state.Distance = Mathf.Min(state.Distance, _currentCollisionDistance);
        }

        private float CastForCollision(Vector3 origin, CameraState state)
        {
            Quaternion yawRot = Quaternion.Euler(0f, state.Yaw, 0f);
            Quaternion pitchRot = Quaternion.Euler(state.Pitch, 0f, 0f);
            Vector3 direction = yawRot * pitchRot * Vector3.back;

            float maxDistance = state.Distance;

            if (Physics.SphereCast(origin, _collisionRadius, direction,
                out RaycastHit hit, maxDistance, _collisionLayers,
                QueryTriggerInteraction.Ignore))
            {
                return Mathf.Max(0f, hit.distance - _collisionRadius);
            }

            return maxDistance;
        }
    }
}
