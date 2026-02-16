using UnityEngine;

namespace Wiesenwischer.GameKit.Camera.Behaviours
{
    /// <summary>
    /// Inertia-Verhalten: Spring-Damper-System für cinematic Camera-Lag.
    /// Glättet die Anchor-Position mit physik-inspiriertem Nachschwingen.
    /// </summary>
    public class InertiaBehaviour : MonoBehaviour, ICameraBehaviour, ICameraSnappable, ICameraPresetReceiver
    {
        [Header("Spring-Damper")]
        [Tooltip("Stiffness der Feder. Höher = enger am Target.")]
        [SerializeField] private float _stiffness = 15f;

        [Tooltip("Dämpfung. 0-1. Niedrig = mehr Overshooting.")]
        [SerializeField] private float _damping = 0.85f;

        [Header("Limits")]
        [Tooltip("Maximaler Offset vom Target (verhindert extremes Nachhinken).")]
        [SerializeField] private float _maxOffset = 1.5f;

        private Vector3 _velocity;
        private Vector3 _currentPosition;
        private bool _initialized;

        public bool IsActive => enabled;

        public void ApplyPreset(CameraPreset preset)
        {
            enabled = preset.InertiaEnabled;
            _stiffness = preset.InertiaStiffness;
            _damping = preset.InertiaDamping;
            _maxOffset = preset.InertiaMaxOffset;
        }

        public void UpdateState(ref CameraState state, CameraContext ctx)
        {
            if (!_initialized)
            {
                _currentPosition = ctx.AnchorPosition;
                _initialized = true;
                return;
            }

            Vector3 target = ctx.AnchorPosition;
            float dt = ctx.DeltaTime;

            // Spring-Damper
            _velocity += (target - _currentPosition) * _stiffness * dt;
            _velocity *= Mathf.Pow(_damping, dt * 60f); // Frame-rate independent damping

            _currentPosition += _velocity * dt;

            // Max-Offset clamp
            Vector3 offset = _currentPosition - target;
            if (offset.magnitude > _maxOffset)
                _currentPosition = target + offset.normalized * _maxOffset;

            // AnchorPosition im Context überschreiben (Klasse = Referenz)
            ctx.AnchorPosition = _currentPosition;
        }

        /// <summary>Sofort zur Zielposition teleportieren (kein Lag).</summary>
        public void Snap(Vector3 position)
        {
            _currentPosition = position;
            _velocity = Vector3.zero;
            _initialized = true;
        }
    }
}
