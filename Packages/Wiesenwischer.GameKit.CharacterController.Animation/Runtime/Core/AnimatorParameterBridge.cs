using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core;
using Wiesenwischer.GameKit.CharacterController.Core.Animation;

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
        private bool _isValid;

        private void Awake()
        {
            _animator = GetComponent<Animator>();

            if (_playerController == null)
                _playerController = GetComponentInParent<PlayerController>();
        }

        private void OnEnable()
        {
            ValidateSetup();
        }

        private void LateUpdate()
        {
            if (!_isValid) return;

            UpdateParameters();
        }

        private void ValidateSetup()
        {
            _isValid = _animator != null
                       && _animator.runtimeAnimatorController != null
                       && _playerController != null;

            if (_animator != null && _animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning($"[AnimatorParameterBridge] Animator auf '{gameObject.name}' hat keinen Controller zugewiesen. " +
                                 "Bitte Wizard erneut ausführen: Wiesenwischer > GameKit > Setup Character Controller");
            }
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
            if (!_isValid) return;
            _animator.SetFloat(AnimationParameters.SpeedHash, speed);
        }

        public void SetGrounded(bool isGrounded)
        {
            if (!_isValid) return;
            _animator.SetBool(AnimationParameters.IsGroundedHash, isGrounded);
        }

        public void SetVerticalVelocity(float velocity)
        {
            if (!_isValid) return;
            _animator.SetFloat(AnimationParameters.VerticalVelocityHash, velocity);
        }

        public void TriggerJump()
        {
            if (!_isValid) return;
            _animator.SetTrigger(AnimationParameters.JumpHash);
        }

        public void TriggerLand()
        {
            if (!_isValid) return;
            _animator.SetTrigger(AnimationParameters.LandHash);
        }

        public void SetAbilityLayerWeight(float weight)
        {
            if (!_isValid) return;
            _animator.SetLayerWeight(AnimationParameters.AbilityLayerIndex, weight);
        }

        #endregion

        public void TriggerLanding(bool isHardLanding)
        {
            if (!_isValid) return;
            _animator.SetBool(AnimationParameters.HardLandingHash, isHardLanding);
            TriggerLand();
        }

        public void SetStatusLayerWeight(float weight)
        {
            if (!_isValid) return;
            _animator.SetLayerWeight(AnimationParameters.StatusLayerIndex, weight);
        }
    }
}
