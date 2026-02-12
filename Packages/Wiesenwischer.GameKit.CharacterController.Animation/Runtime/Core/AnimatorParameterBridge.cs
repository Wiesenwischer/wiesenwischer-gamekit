using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core;
using Wiesenwischer.GameKit.CharacterController.Core.Animation;

namespace Wiesenwischer.GameKit.CharacterController.Animation
{
    /// <summary>
    /// Brücke zwischen State Machine und Unity Animator.
    /// Die State Machine ist die einzige Autorität für Animation-States:
    /// Jeder State ruft in OnEnter PlayState() auf → Animator.CrossFade().
    /// UpdateParameters() aktualisiert nur Blend Tree Parameter (Speed, VerticalVelocity).
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class AnimatorParameterBridge : MonoBehaviour, IAnimationController
    {
        [Header("References")]
        [SerializeField] private PlayerController _playerController;

        [Header("Animation Config")]
        [Tooltip("CrossFade-Übergangszeiten pro State. Wenn nicht gesetzt, werden Standard-Werte verwendet.")]
        [SerializeField] private AnimationTransitionConfig _transitionConfig;

        [Header("Smoothing")]
        [Tooltip("Wie schnell der Speed-Parameter sich dem Zielwert annähert.")]
        [SerializeField] private float _speedDampTime = 0.1f;

        [Tooltip("Wie schnell der VerticalVelocity-Parameter sich annähert.")]
        [SerializeField] private float _verticalVelocityDampTime = 0.05f;

        [Header("Stair Animation")]
        [Tooltip("Zusätzlicher Speed-Multiplikator auf Treppen. Kompensiert dass die Walk-Animation " +
                 "für flachen Boden designed ist, der Character auf Treppen aber visuell schneller läuft. " +
                 "1.0 = nur StairSpeedReduction-Kompensation, 1.5 = 50% schneller, 2.0 = doppelt so schnell.")]
        [Range(1f, 3f)]
        [SerializeField] private float _stairAnimSpeedMultiplier = 1.5f;

        private Animator _animator;
        private bool _isValid;
        private int _currentAnimStateHash;
        private bool _canExitAnimation;

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

            if (_transitionConfig == null)
            {
                Debug.LogWarning($"[AnimatorParameterBridge] Keine AnimationTransitionConfig zugewiesen auf '{gameObject.name}'. " +
                                 "Standard-Werte werden verwendet.");
            }
        }

        /// <summary>
        /// Aktualisiert nur Blend Tree Parameter. Animation-State-Wechsel werden
        /// ausschließlich von der State Machine via PlayState() gesteuert.
        /// </summary>
        private void UpdateParameters()
        {
            var data = _playerController.ReusableData;
            if (data == null) return;

            var config = _playerController.LocomotionConfig;

            // Speed: Normalisiert auf RunSpeed (0=Idle, 0.5=Walk, 1.0=Run, 1.5=Sprint)
            float movementSpeed = data.HorizontalVelocity.magnitude;

            // Treppen-Kompensation: StairSpeedReduction verlangsamt den Motor auf Treppen
            // (Gameplay-Entscheidung), aber visuell bewegt sich der Character durch die
            // Step-Up-Teleportationen mit annähernd normaler Geschwindigkeit. Ohne Kompensation
            // läuft die Walk-Animation deutlich langsamer als die sichtbare Körperbewegung.
            bool stairCompensated = false;
            if (_playerController.IsGrounded && _playerController.Locomotion.IsOnStairs)
            {
                float reduction = config.StairSpeedReduction;
                if (reduction > 0f && reduction < 1f)
                {
                    movementSpeed /= (1f - reduction);
                }
                movementSpeed *= _stairAnimSpeedMultiplier;
                stairCompensated = true;
            }

            float normalizedSpeed = config.RunSpeed > 0f
                ? movementSpeed / config.RunSpeed
                : 0f;

            _animator.SetFloat(
                AnimationParameters.SpeedHash,
                normalizedSpeed,
                _speedDampTime,
                Time.deltaTime);

            _animator.SetFloat(
                AnimationParameters.VerticalVelocityHash,
                data.VerticalVelocity,
                _verticalVelocityDampTime,
                Time.deltaTime);

        }

        #region IAnimationController

        public void PlayState(CharacterAnimationState state)
        {
            float duration = _transitionConfig != null
                ? _transitionConfig.GetTransitionDuration(state)
                : GetDefaultTransitionDuration(state);
            PlayState(state, duration);
        }

        public void PlayState(CharacterAnimationState state, float transitionDuration)
        {
            if (!_isValid) return;

            int hash;
            switch (state)
            {
                case CharacterAnimationState.Locomotion: hash = AnimationParameters.LocomotionStateHash; break;
                case CharacterAnimationState.Jump: hash = AnimationParameters.JumpStateHash; break;
                case CharacterAnimationState.Fall: hash = AnimationParameters.FallStateHash; break;
                case CharacterAnimationState.SoftLand: hash = AnimationParameters.SoftLandStateHash; break;
                case CharacterAnimationState.HardLand: hash = AnimationParameters.HardLandStateHash; break;
                case CharacterAnimationState.LightStop: hash = AnimationParameters.LightStopStateHash; break;
                case CharacterAnimationState.MediumStop: hash = AnimationParameters.MediumStopStateHash; break;
                case CharacterAnimationState.HardStop: hash = AnimationParameters.HardStopStateHash; break;
                default: return;
            }

            // Redundante CrossFade-Aufrufe vermeiden (z.B. Idle→Running bleiben beide in Locomotion)
            if (_currentAnimStateHash == hash) return;
            _currentAnimStateHash = hash;
            _canExitAnimation = false;
            _animator.CrossFade(hash, transitionDuration);
        }

        public float GetAnimationNormalizedTime()
        {
            if (!_isValid) return 0f;

            // Während einer Transition ist der Ziel-State in NextAnimatorStateInfo
            if (_animator.IsInTransition(AnimationParameters.BaseLayerIndex))
            {
                return _animator.GetNextAnimatorStateInfo(AnimationParameters.BaseLayerIndex).normalizedTime;
            }

            var info = _animator.GetCurrentAnimatorStateInfo(AnimationParameters.BaseLayerIndex);

            // Sicherstellen dass der Animator im erwarteten State ist.
            // Nach CrossFade() im selben Frame hat der Animator den State noch nicht gewechselt.
            // shortNameHash vergleichen (CrossFade nutzt Short-Name Hashes).
            if (_currentAnimStateHash != 0 && info.shortNameHash != _currentAnimStateHash)
                return 0f;

            return info.normalizedTime;
        }

        public bool IsAnimationComplete()
        {
            if (!_isValid) return false;

            // Während einer Transition ist die Animation definitiv nicht fertig
            if (_animator.IsInTransition(AnimationParameters.BaseLayerIndex))
                return false;

            var info = _animator.GetCurrentAnimatorStateInfo(AnimationParameters.BaseLayerIndex);

            // Sicherstellen dass der Animator im erwarteten State ist.
            // Nach CrossFade() im selben Frame hat der Animator den State noch nicht gewechselt —
            // GetCurrentAnimatorStateInfo() gibt dann den ALTEN State zurück (z.B. Fall mit
            // normalizedTime >= 1.0), was fälschlicherweise "complete" signalisieren würde.
            if (_currentAnimStateHash != 0 && info.shortNameHash != _currentAnimStateHash)
                return false;

            return info.normalizedTime >= 1.0f;
        }

        public bool CanExitAnimation => _canExitAnimation;

        public void SetSpeed(float speed)
        {
            if (!_isValid) return;
            _animator.SetFloat(AnimationParameters.SpeedHash, speed);
        }

        public void SetVerticalVelocity(float velocity)
        {
            if (!_isValid) return;
            _animator.SetFloat(AnimationParameters.VerticalVelocityHash, velocity);
        }

        public void SetAbilityLayerWeight(float weight)
        {
            if (!_isValid) return;
            _animator.SetLayerWeight(AnimationParameters.AbilityLayerIndex, weight);
        }

        #endregion

        /// <summary>
        /// Animation Event Empfänger. Wird vom Animator aufgerufen wenn ein
        /// "AllowExit" Event auf dem aktuellen Clip platziert wurde.
        /// Erlaubt States, die Animation frühzeitig zu verlassen.
        /// </summary>
        public void AllowExit()
        {
            _canExitAnimation = true;
        }

        public void SetStatusLayerWeight(float weight)
        {
            if (!_isValid) return;
            _animator.SetLayerWeight(AnimationParameters.StatusLayerIndex, weight);
        }

        private static float GetDefaultTransitionDuration(CharacterAnimationState state)
        {
            switch (state)
            {
                case CharacterAnimationState.Locomotion: return 0.15f;
                case CharacterAnimationState.Jump:       return 0.1f;
                case CharacterAnimationState.Fall:        return 0.05f;
                case CharacterAnimationState.SoftLand:    return 0.1f;
                case CharacterAnimationState.HardLand:    return 0.08f;
                case CharacterAnimationState.LightStop:  return 0.1f;
                case CharacterAnimationState.MediumStop: return 0.1f;
                case CharacterAnimationState.HardStop:   return 0.1f;
                default: return 0.15f;
            }
        }
    }
}
