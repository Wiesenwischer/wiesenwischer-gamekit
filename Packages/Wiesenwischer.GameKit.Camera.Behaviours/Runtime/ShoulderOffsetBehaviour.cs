using UnityEngine;

namespace Wiesenwischer.GameKit.Camera.Behaviours
{
    /// <summary>
    /// Shoulder-Offset: Seitlicher Versatz für Over-the-Shoulder-Ansicht.
    /// </summary>
    public class ShoulderOffsetBehaviour : MonoBehaviour, ICameraBehaviour, ICameraPresetReceiver
    {
        [Header("Offset")]
        [Tooltip("Seitlicher Versatz (positiv = rechte Schulter).")]
        [SerializeField] private float _offsetX = 0.5f;

        [Tooltip("Vertikaler Versatz.")]
        [SerializeField] private float _offsetY = 0f;

        [Header("Side Switch")]
        [Tooltip("SmoothDamp-Zeit beim Seite-Wechseln.")]
        [SerializeField] private float _switchDamping = 0.2f;

        private float _currentOffsetX;
        private float _switchVelocity;
        private bool _rightShoulder = true;

        public bool IsActive => enabled;

        public void ApplyPreset(CameraPreset preset)
        {
            enabled = preset.ShoulderEnabled;
            _offsetX = preset.ShoulderOffsetX;
            _offsetY = preset.ShoulderOffsetY;
            _switchDamping = preset.ShoulderSwitchDamping;
        }

        /// <summary>Wechselt die Schulterseite.</summary>
        public void SwitchSide()
        {
            _rightShoulder = !_rightShoulder;
        }

        /// <summary>Aktuelle Seite: true = rechts, false = links.</summary>
        public bool IsRightShoulder => _rightShoulder;

        public void UpdateState(ref CameraState state, CameraContext ctx)
        {
            float targetX = _rightShoulder ? _offsetX : -_offsetX;

            _currentOffsetX = Mathf.SmoothDamp(
                _currentOffsetX, targetX, ref _switchVelocity,
                _switchDamping, Mathf.Infinity, ctx.DeltaTime);

            state.ShoulderOffset = new Vector3(_currentOffsetX, _offsetY, 0f);
        }
    }
}
