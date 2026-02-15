using UnityEngine;

namespace Wiesenwischer.GameKit.Camera.Behaviours
{
    /// <summary>
    /// Orbit-Verhalten: Wendet Look-Input als Yaw/Pitch-Rotation an.
    /// </summary>
    public class OrbitBehaviour : MonoBehaviour, ICameraBehaviour, ICameraPresetReceiver
    {
        [Header("Pitch Limits")]
        [SerializeField] private float _minPitch = -40f;
        [SerializeField] private float _maxPitch = 70f;

        public bool IsActive => enabled;

        public void ApplyPreset(CameraPreset preset)
        {
            enabled = true;
            _minPitch = preset.PitchMin;
            _maxPitch = preset.PitchMax;
        }

        public void UpdateState(ref CameraState state, CameraContext ctx)
        {
            // Kein Orbit wenn kein Button gehalten (ButtonActivated-Modus)
            if (ctx.Input.OrbitMode == CameraOrbitMode.None)
                return;

            state.Yaw += ctx.Input.LookX;
            state.Pitch -= ctx.Input.LookY;
            state.Pitch = Mathf.Clamp(state.Pitch, _minPitch, _maxPitch);
        }
    }
}
