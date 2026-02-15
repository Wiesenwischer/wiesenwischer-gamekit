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

        private float _targetDistance;
        private float _zoomVelocity;
        private bool _initialized;

        public bool IsActive => enabled;

        public void ApplyPreset(CameraPreset preset)
        {
            enabled = true;
            _defaultDistance = preset.DefaultDistance;
            _minDistance = preset.MinDistance;
            _maxDistance = preset.MaxDistance;
            _zoomDamping = preset.ZoomDamping;
            _targetDistance = _defaultDistance;
        }

        public void InitializeState(ref CameraState state)
        {
            _targetDistance = _defaultDistance;
            state.Distance = _defaultDistance;
            _initialized = true;
        }

        public void UpdateState(ref CameraState state, CameraContext ctx)
        {
            if (!_initialized)
            {
                _targetDistance = state.Distance;
                _initialized = true;
            }

            // Zoom-Input auf eigene Ziel-Distanz anwenden (nicht auf state.Distance,
            // da CollisionBehaviour diesen Wert überschreiben kann)
            _targetDistance -= ctx.Input.Zoom;
            _targetDistance = Mathf.Clamp(_targetDistance, _minDistance, _maxDistance);

            state.Distance = Mathf.SmoothDamp(
                state.Distance, _targetDistance, ref _zoomVelocity,
                _zoomDamping, Mathf.Infinity, ctx.DeltaTime);
        }
    }
}
