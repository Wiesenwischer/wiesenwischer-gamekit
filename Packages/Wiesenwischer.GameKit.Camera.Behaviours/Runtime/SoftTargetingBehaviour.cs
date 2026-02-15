using UnityEngine;

namespace Wiesenwischer.GameKit.Camera.Behaviours
{
    /// <summary>
    /// Additive Bias-Rotation Richtung Movement und/oder LookTarget.
    /// Überschreibt nicht den Player-Input, sondern addiert sanft.
    /// </summary>
    public class SoftTargetingBehaviour : MonoBehaviour, ICameraBehaviour, ICameraPresetReceiver
    {
        [Header("Movement Bias")]
        [Tooltip("Stärke des Forward-Bias in Grad")]
        [SerializeField] private float _movementBiasStrength = 5f;

        [Header("Target Bias")]
        [Tooltip("Stärke des Target-Bias in Grad")]
        [SerializeField] private float _targetBiasStrength = 15f;

        [Tooltip("Maximaler Erfassungsradius für Soft Target")]
        [SerializeField] private float _targetRange = 20f;

        [Header("Smoothing")]
        [Tooltip("Smooth-Zeit für Bias-Übergänge")]
        [SerializeField] private float _damping = 0.15f;

        private float _currentYawBias;
        private float _currentPitchBias;
        private float _yawBiasVelocity;
        private float _pitchBiasVelocity;

        public bool IsActive => enabled;

        public void UpdateState(ref CameraState state, CameraContext ctx)
        {
            float targetYawBias = 0f;
            float targetPitchBias = 0f;

            // 1. Target Bias (höhere Priorität)
            if (ctx.LookTarget != null && _targetBiasStrength > 0f)
            {
                Vector3 toTarget = ctx.LookTarget.position - ctx.AnchorPosition;

                if (toTarget.sqrMagnitude <= _targetRange * _targetRange
                    && toTarget.sqrMagnitude > 0.01f)
                {
                    Vector3 currentForward = Quaternion.Euler(0f, state.Yaw, 0f) * Vector3.forward;
                    Vector3 toTargetFlat = new Vector3(toTarget.x, 0f, toTarget.z);

                    if (toTargetFlat.sqrMagnitude > 0.001f)
                    {
                        float angleToTarget = Vector3.SignedAngle(
                            currentForward, toTargetFlat.normalized, Vector3.up);
                        targetYawBias = Mathf.Clamp(
                            angleToTarget, -_targetBiasStrength, _targetBiasStrength);
                    }

                    float verticalAngle = Mathf.Atan2(toTarget.y, toTargetFlat.magnitude)
                                          * Mathf.Rad2Deg;
                    targetPitchBias = Mathf.Clamp(
                        verticalAngle * 0.3f, -10f, 10f);
                }
            }
            // 2. Movement Forward Bias
            else if (ctx.CharacterVelocity.sqrMagnitude > 0.25f
                     && _movementBiasStrength > 0f)
            {
                Vector3 moveDir = new Vector3(
                    ctx.CharacterVelocity.x, 0f, ctx.CharacterVelocity.z);

                if (moveDir.sqrMagnitude > 0.001f)
                {
                    Vector3 currentForward = Quaternion.Euler(0f, state.Yaw, 0f) * Vector3.forward;
                    float angleToMove = Vector3.SignedAngle(
                        currentForward, moveDir.normalized, Vector3.up);
                    targetYawBias = Mathf.Clamp(
                        angleToMove, -_movementBiasStrength, _movementBiasStrength);
                }
            }

            // Smooth Bias
            _currentYawBias = Mathf.SmoothDamp(
                _currentYawBias, targetYawBias,
                ref _yawBiasVelocity, _damping,
                Mathf.Infinity, ctx.DeltaTime);

            _currentPitchBias = Mathf.SmoothDamp(
                _currentPitchBias, targetPitchBias,
                ref _pitchBiasVelocity, _damping,
                Mathf.Infinity, ctx.DeltaTime);

            // Additive Rotation
            state.Yaw += _currentYawBias;
            state.Pitch += _currentPitchBias;
        }

        public void ApplyPreset(CameraPreset preset)
        {
            enabled = preset.SoftTargetingEnabled;
            _movementBiasStrength = preset.MovementBiasStrength;
            _targetBiasStrength = preset.TargetBiasStrength;
            _targetRange = preset.SoftTargetRange;
            _damping = preset.SoftTargetDamping;
        }
    }
}
