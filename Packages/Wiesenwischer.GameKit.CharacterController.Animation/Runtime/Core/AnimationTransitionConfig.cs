using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Animation;

namespace Wiesenwischer.GameKit.CharacterController.Animation
{
    /// <summary>
    /// ScriptableObject mit CrossFade-Übergangszeiten für jeden Animation-State.
    /// Ermöglicht Designern das Feintuning aller Übergangszeiten im Inspector
    /// ohne Code-Änderungen.
    /// </summary>
    [CreateAssetMenu(
        fileName = "AnimationTransitionConfig",
        menuName = "Wiesenwischer/GameKit/Animation Transition Config",
        order = 1)]
    public class AnimationTransitionConfig : ScriptableObject
    {
        [Header("Locomotion")]
        [Tooltip("CrossFade-Dauer beim Wechsel zu Locomotion (Idle/Walk/Run/Sprint)")]
        [SerializeField] private float _locomotionTransitionDuration = 0.15f;

        [Header("Airborne")]
        [Tooltip("CrossFade-Dauer beim Wechsel zu Jump")]
        [SerializeField] private float _jumpTransitionDuration = 0.1f;

        [Tooltip("CrossFade-Dauer beim Wechsel zu Fall")]
        [SerializeField] private float _fallTransitionDuration = 0.05f;

        [Header("Landing")]
        [Tooltip("CrossFade-Dauer beim Wechsel zu SoftLand")]
        [SerializeField] private float _softLandTransitionDuration = 0.1f;

        [Tooltip("CrossFade-Dauer beim Wechsel zu HardLand")]
        [SerializeField] private float _hardLandTransitionDuration = 0.08f;

        [Tooltip("CrossFade-Dauer beim Wechsel zu Roll")]
        [SerializeField] private float _rollTransitionDuration = 0.1f;

        [Header("Stopping")]
        [Tooltip("CrossFade-Dauer beim Wechsel zu LightStop (Walk-Stopp)")]
        [SerializeField] private float _lightStopTransitionDuration = 0.1f;

        [Tooltip("CrossFade-Dauer beim Wechsel zu MediumStop (Run-Stopp)")]
        [SerializeField] private float _mediumStopTransitionDuration = 0.1f;

        [Tooltip("CrossFade-Dauer beim Wechsel zu HardStop (Sprint-Stopp)")]
        [SerializeField] private float _hardStopTransitionDuration = 0.1f;

        public float LocomotionTransitionDuration => _locomotionTransitionDuration;
        public float JumpTransitionDuration => _jumpTransitionDuration;
        public float FallTransitionDuration => _fallTransitionDuration;
        public float SoftLandTransitionDuration => _softLandTransitionDuration;
        public float HardLandTransitionDuration => _hardLandTransitionDuration;
        public float RollTransitionDuration => _rollTransitionDuration;
        public float LightStopTransitionDuration => _lightStopTransitionDuration;
        public float MediumStopTransitionDuration => _mediumStopTransitionDuration;
        public float HardStopTransitionDuration => _hardStopTransitionDuration;

        /// <summary>
        /// Gibt die Transition-Dauer für den angegebenen State zurück.
        /// </summary>
        public float GetTransitionDuration(CharacterAnimationState state)
        {
            switch (state)
            {
                case CharacterAnimationState.Locomotion: return _locomotionTransitionDuration;
                case CharacterAnimationState.Jump:       return _jumpTransitionDuration;
                case CharacterAnimationState.Fall:        return _fallTransitionDuration;
                case CharacterAnimationState.SoftLand:    return _softLandTransitionDuration;
                case CharacterAnimationState.HardLand:    return _hardLandTransitionDuration;
                case CharacterAnimationState.Roll:        return _rollTransitionDuration;
                case CharacterAnimationState.LightStop:  return _lightStopTransitionDuration;
                case CharacterAnimationState.MediumStop: return _mediumStopTransitionDuration;
                case CharacterAnimationState.HardStop:   return _hardStopTransitionDuration;
                default: return 0.15f;
            }
        }

        private void OnValidate()
        {
            _locomotionTransitionDuration = Mathf.Max(0f, _locomotionTransitionDuration);
            _jumpTransitionDuration = Mathf.Max(0f, _jumpTransitionDuration);
            _fallTransitionDuration = Mathf.Max(0f, _fallTransitionDuration);
            _softLandTransitionDuration = Mathf.Max(0f, _softLandTransitionDuration);
            _hardLandTransitionDuration = Mathf.Max(0f, _hardLandTransitionDuration);
            _rollTransitionDuration = Mathf.Max(0f, _rollTransitionDuration);
            _lightStopTransitionDuration = Mathf.Max(0f, _lightStopTransitionDuration);
            _mediumStopTransitionDuration = Mathf.Max(0f, _mediumStopTransitionDuration);
            _hardStopTransitionDuration = Mathf.Max(0f, _hardStopTransitionDuration);
        }
    }
}
