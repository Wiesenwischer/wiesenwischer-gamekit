using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core;

namespace Wiesenwischer.GameKit.CharacterController.Animation
{
    [RequireComponent(typeof(Animator))]
    public class AnimatorParameterBridge : MonoBehaviour, IAnimationController
    {
        [Header("References")]
        [SerializeField] private PlayerController _playerController;

        [Header("Smoothing")]
        [Tooltip("Wie schnell der Speed-Parameter sich dem Zielwert annähert.")]
        [SerializeField] private float _speedDampTime = 0.1f;

        [Tooltip("Wie schnell der VerticalVelocity-Parameter sich annähert.")]
        [SerializeField] private float _verticalVelocityDampTime = 0.05f;

        private Animator _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();

            if (_playerController == null)
                _playerController = GetComponentInParent<PlayerController>();
        }

        private void LateUpdate()
        {
            if (_playerController == null || _animator == null) return;

            UpdateParameters();
        }

        private void UpdateParameters()
        {
            var data = _playerController.ReusableData;
            if (data == null) return;

            var config = _playerController.LocomotionConfig;

            // Speed: Normalisiert auf RunSpeed (0=Idle, 0.5=Walk, 1.0=Run, 1.5=Sprint)
            float horizontalSpeed = data.HorizontalVelocity.magnitude;
            float normalizedSpeed = config.RunSpeed > 0f
                ? horizontalSpeed / config.RunSpeed
                : 0f;

            _animator.SetFloat(
                AnimationParameters.SpeedHash,
                normalizedSpeed,
                _speedDampTime,
                Time.deltaTime);

            _animator.SetBool(AnimationParameters.IsGroundedHash, data.IsGrounded);

            _animator.SetFloat(
                AnimationParameters.VerticalVelocityHash,
                data.VerticalVelocity,
                _verticalVelocityDampTime,
                Time.deltaTime);
        }

        #region IAnimationController

        public void SetSpeed(float speed)
        {
            _animator.SetFloat(AnimationParameters.SpeedHash, speed);
        }

        public void SetGrounded(bool isGrounded)
        {
            _animator.SetBool(AnimationParameters.IsGroundedHash, isGrounded);
        }

        public void SetVerticalVelocity(float velocity)
        {
            _animator.SetFloat(AnimationParameters.VerticalVelocityHash, velocity);
        }

        public void TriggerJump()
        {
            _animator.SetTrigger(AnimationParameters.JumpHash);
        }

        public void TriggerLand()
        {
            _animator.SetTrigger(AnimationParameters.LandHash);
        }

        public void SetAbilityLayerWeight(float weight)
        {
            _animator.SetLayerWeight(AnimationParameters.AbilityLayerIndex, weight);
        }

        #endregion

        public void TriggerLanding(bool isHardLanding)
        {
            _animator.SetBool(AnimationParameters.HardLandingHash, isHardLanding);
            TriggerLand();
        }

        public void SetStatusLayerWeight(float weight)
        {
            _animator.SetLayerWeight(AnimationParameters.StatusLayerIndex, weight);
        }
    }
}
