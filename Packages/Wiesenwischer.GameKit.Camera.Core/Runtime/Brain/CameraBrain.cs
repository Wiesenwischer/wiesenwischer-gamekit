using UnityEngine;

namespace Wiesenwischer.GameKit.Camera
{
    /// <summary>
    /// Zentraler Camera-Orchestrator. Koordiniert Input, Anchor, Behaviours und PivotRig.
    /// </summary>
    [RequireComponent(typeof(PivotRig))]
    public class CameraBrain : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CameraAnchor _anchor;
        [SerializeField] private CameraInputPipeline _inputPipeline;
        [SerializeField] private UnityEngine.Camera _camera;

        [Header("Config")]
        [SerializeField] private CameraCoreConfig _config;

        [Header("Behaviours (Phase 27)")]
        [Tooltip("Externe Behaviours. Werden nach der eingebetteten Logik evaluiert.")]
        [SerializeField] private MonoBehaviour[] _behaviourComponents;

        private PivotRig _rig;
        private CameraState _state;
        private CameraContext _context;
        private float _zoomVelocity;

        /// <summary>Aktueller Camera State (readonly).</summary>
        public CameraState State => _state;

        /// <summary>Camera Forward in Welt-Space (Y=0, normalisiert).</summary>
        public Vector3 Forward
        {
            get
            {
                var fwd = _rig.GetCameraForward();
                fwd.y = 0f;
                return fwd.sqrMagnitude > 0.001f ? fwd.normalized : transform.forward;
            }
        }

        /// <summary>Setzt das Follow-Target (Character Root).</summary>
        public void SetTarget(Transform followTarget, Transform lookTarget = null)
        {
            if (_anchor != null)
                _anchor.FollowTarget = followTarget;

            if (_context != null)
                _context.LookTarget = lookTarget;
        }

        /// <summary>Teleportiert Kamera sofort hinter das Target.</summary>
        public void SnapBehindTarget()
        {
            if (_anchor != null && _anchor.FollowTarget != null)
            {
                _anchor.SnapToTarget();
                _state.Yaw = _anchor.FollowTarget.eulerAngles.y;
                _state.Pitch = 0f;
                _state.Distance = _config != null ? _config.DefaultDistance : 5f;
                _rig.ApplyState(_state, _anchor.AnchorPosition);
            }
        }

        private void Awake()
        {
            _rig = GetComponent<PivotRig>();
            _rig.EnsureHierarchy();

            _context = new CameraContext();

            _state = CameraState.Default;
            if (_config != null)
                _state.Distance = _config.DefaultDistance;

            if (_camera == null)
                _camera = GetComponentInChildren<UnityEngine.Camera>();
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;

            // 1. Input
            CameraInputState input = default;
            if (_inputPipeline != null)
                input = _inputPipeline.ProcessInput(dt);

            // 2. Anchor
            if (_anchor != null)
                _anchor.UpdateAnchor(dt);

            // 3. Context
            _context.Input = input;
            _context.AnchorPosition = _anchor != null ? _anchor.AnchorPosition : transform.position;
            _context.DeltaTime = dt;

            // 4. Eingebettete Logik (migriert aus ThirdPersonCamera)
            UpdateOrbit(input);
            UpdateZoom(input, dt);

            // 5. Externe Behaviours (Phase 27)
            if (_behaviourComponents != null)
            {
                foreach (var comp in _behaviourComponents)
                {
                    if (comp is ICameraBehaviour behaviour && behaviour.IsActive)
                        behaviour.UpdateState(ref _state, _context);
                }
            }

            // 6. Collision
            float collisionDistance = CheckCollision();
            float appliedDistance = Mathf.Min(_state.Distance, collisionDistance);

            var rigState = _state;
            rigState.Distance = appliedDistance;

            // 7. Apply
            _rig.ApplyState(rigState, _context.AnchorPosition);

            // 8. FOV
            if (_camera != null)
                _camera.fieldOfView = _state.Fov;
        }

        private void UpdateOrbit(CameraInputState input)
        {
            _state.Yaw += input.LookX;
            _state.Pitch -= input.LookY;

            if (_config != null)
                _state.Pitch = Mathf.Clamp(_state.Pitch, _config.MinVerticalAngle, _config.MaxVerticalAngle);
            else
                _state.Pitch = Mathf.Clamp(_state.Pitch, -40f, 70f);
        }

        private void UpdateZoom(CameraInputState input, float deltaTime)
        {
            if (_config == null) return;

            float targetDistance = _state.Distance - input.Zoom;
            targetDistance = Mathf.Clamp(targetDistance, _config.MinDistance, _config.MaxDistance);

            _state.Distance = Mathf.SmoothDamp(
                _state.Distance, targetDistance, ref _zoomVelocity,
                _config.ZoomDamping, Mathf.Infinity, deltaTime);
        }

        private float CheckCollision()
        {
            if (_config == null) return _state.Distance;

            Vector3 origin = _context.AnchorPosition;
            Vector3 direction = _rig.GetCameraWorldPosition() - origin;
            float maxDistance = direction.magnitude;

            if (maxDistance < 0.01f) return _state.Distance;

            direction /= maxDistance;

            if (Physics.SphereCast(origin, _config.CollisionRadius, direction,
                out RaycastHit hit, maxDistance, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
            {
                return hit.distance - _config.CollisionRadius;
            }

            return _state.Distance;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_anchor == null || !Application.isPlaying) return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_anchor.AnchorPosition, 0.1f);

            if (_config != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(_rig.GetCameraWorldPosition(), _config.CollisionRadius);
            }
        }
#endif
    }
}
