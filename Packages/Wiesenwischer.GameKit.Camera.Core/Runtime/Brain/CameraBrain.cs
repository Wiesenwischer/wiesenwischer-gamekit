using UnityEngine;

namespace Wiesenwischer.GameKit.Camera
{
    /// <summary>
    /// Zentraler Camera-Orchestrator. Koordiniert Input, Anchor, Behaviours und PivotRig.
    /// Enthält keine eigene Kamera-Logik — alles läuft über ICameraBehaviour[].
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

        private PivotRig _rig;
        private CameraState _state;
        private CameraContext _context;
        private ICameraBehaviour[] _behaviours;

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
            {
                _context.FollowTarget = followTarget;
                _context.LookTarget = lookTarget;
            }
        }

        /// <summary>Teleportiert Kamera sofort hinter das Target.</summary>
        public void SnapBehindTarget()
        {
            if (_anchor != null && _anchor.FollowTarget != null)
            {
                _anchor.SnapToTarget();
                _state.Yaw = _anchor.FollowTarget.eulerAngles.y;
                _state.Pitch = 0f;

                // Behaviours mit InitializeState aufrufen (z.B. ZoomBehaviour → Distance)
                if (_behaviours != null)
                {
                    foreach (var b in _behaviours)
                    {
                        if (b is ICameraStateInitializer init)
                            init.InitializeState(ref _state);
                        if (b is ICameraSnappable snap)
                            snap.Snap(_anchor.AnchorPosition);
                    }
                }

                _rig.ApplyState(_state, _anchor.AnchorPosition);
            }
        }

        /// <summary>Behaviour-Liste neu einlesen (z.B. nach AddComponent).</summary>
        public void RefreshBehaviours()
        {
            _behaviours = GetComponents<ICameraBehaviour>();
        }

        private void Awake()
        {
            _rig = GetComponent<PivotRig>();
            _rig.EnsureHierarchy();

            _context = new CameraContext();
            _state = CameraState.Default;

            if (_config != null)
                _state.Fov = _config.DefaultFov;

            // Auto-discover Behaviours auf diesem GameObject
            _behaviours = GetComponents<ICameraBehaviour>();

            // Behaviours mit InitializeState aufrufen
            foreach (var b in _behaviours)
            {
                if (b is ICameraStateInitializer init)
                    init.InitializeState(ref _state);
            }

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
            _context.FollowTarget = _anchor != null ? _anchor.FollowTarget : null;
            _context.DeltaTime = dt;

            // 4. Behaviours evaluieren
            foreach (var behaviour in _behaviours)
            {
                if (behaviour.IsActive)
                    behaviour.UpdateState(ref _state, _context);
            }

            // 5. Apply
            _rig.ApplyState(_state, _context.AnchorPosition);

            // 6. FOV
            if (_camera != null)
                _camera.fieldOfView = _state.Fov;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_anchor == null || !Application.isPlaying) return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_anchor.AnchorPosition, 0.1f);
        }
#endif
    }
}
