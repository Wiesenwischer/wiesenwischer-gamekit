using UnityEngine;

namespace Wiesenwischer.GameKit.Camera.Behaviours
{
    /// <summary>
    /// Zoom-Verhalten: Steuert Distance basierend auf Zoom-Input.
    /// </summary>
    public class ZoomBehaviour : MonoBehaviour, ICameraBehaviour, ICameraStateInitializer, ICameraPresetReceiver
    {
        [Header("Distance")]
        [SerializeField] private float _defaultDistance = 5f;
        [SerializeField] private float _minDistance = 2f;
        [SerializeField] private float _maxDistance = 15f;

        [Header("Smoothing")]
        [SerializeField] private float _zoomDamping = 0.1f;

        private float _zoomVelocity;

        public bool IsActive => enabled;

        public void ApplyPreset(CameraPreset preset)
        {
            _defaultDistance = preset.DefaultDistance;
            _minDistance = preset.MinDistance;
            _maxDistance = preset.MaxDistance;
            _zoomDamping = preset.ZoomDamping;
        }

        public void InitializeState(ref CameraState state)
        {
            state.Distance = _defaultDistance;
        }

        public void UpdateState(ref CameraState state, CameraContext ctx)
        {
            float targetDistance = state.Distance - ctx.Input.Zoom;
            targetDistance = Mathf.Clamp(targetDistance, _minDistance, _maxDistance);

            state.Distance = Mathf.SmoothDamp(
                state.Distance, targetDistance, ref _zoomVelocity,
                _zoomDamping, Mathf.Infinity, ctx.DeltaTime);
        }
    }
}
