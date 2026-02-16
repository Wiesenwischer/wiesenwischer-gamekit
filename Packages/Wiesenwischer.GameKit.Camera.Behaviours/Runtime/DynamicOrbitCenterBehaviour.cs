using UnityEngine;

namespace Wiesenwischer.GameKit.Camera.Behaviours
{
    /// <summary>
    /// Verschiebt den Orbit-Pivot dynamisch basierend auf Character-Zustand.
    /// Idle: Character Position. Movement: Forward Bias. Combat: Target Midpoint.
    /// </summary>
    public class DynamicOrbitCenterBehaviour : MonoBehaviour, ICameraBehaviour, ICameraPresetReceiver
    {
        [Header("Config")]
        [Tooltip("Forward Bias in Metern bei Bewegung")]
        [SerializeField] private float _forwardBias = 0.5f;

        [Tooltip("Smooth-Zeit für Orbit-Center Verschiebung")]
        [SerializeField] private float _damping = 0.1f;

        [Tooltip("Minimale Character-Geschwindigkeit für Forward Bias")]
        [SerializeField] private float _minSpeed = 0.5f;

        [Tooltip("Anteil der Strecke zum Target für Combat-Midpoint (0=Player, 1=Target)")]
        [SerializeField] [Range(0f, 0.5f)] private float _targetMidpointRatio = 0.3f;

        private Vector3 _currentOffset;
        private Vector3 _offsetVelocity;

        public bool IsActive => enabled;

        public void UpdateState(ref CameraState state, CameraContext ctx)
        {
            Vector3 targetOffset = Vector3.zero;

            // Combat-Modus: Orbit-Center zwischen Player und Target
            if (ctx.LookTarget != null)
            {
                Vector3 toTarget = ctx.LookTarget.position - ctx.AnchorPosition;
                targetOffset = toTarget * _targetMidpointRatio;
            }
            // Movement-Modus: Forward Bias
            else if (ctx.CharacterVelocity.sqrMagnitude > _minSpeed * _minSpeed)
            {
                Vector3 moveDir = ctx.CharacterVelocity;
                moveDir.y = 0f;
                if (moveDir.sqrMagnitude > 0.001f)
                    targetOffset = moveDir.normalized * _forwardBias;
            }

            // Smooth Transition
            _currentOffset = Vector3.SmoothDamp(
                _currentOffset, targetOffset,
                ref _offsetVelocity, _damping,
                Mathf.Infinity, ctx.DeltaTime);

            // AnchorPosition verschieben
            ctx.AnchorPosition += _currentOffset;
        }

        /// <summary>Snap auf Target-Position (kein Smoothing).</summary>
        public void Snap()
        {
            _currentOffset = Vector3.zero;
            _offsetVelocity = Vector3.zero;
        }

        public void ApplyPreset(CameraPreset preset)
        {
            enabled = preset.DynamicOrbitEnabled;
            _forwardBias = preset.ForwardBias;
            _damping = preset.OrbitCenterDamping;
        }
    }
}
