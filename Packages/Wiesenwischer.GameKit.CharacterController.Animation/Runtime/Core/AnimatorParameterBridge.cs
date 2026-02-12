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

        private Animator _animator;
        private bool _isValid;
        private int _currentAnimStateHash;
        private bool _canExitAnimation;

#if UNITY_EDITOR
        private int _prevAnimStateHash;
#endif

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
            float horizontalSpeed = data.HorizontalVelocity.magnitude;
            float normalizedSpeed = config.RunSpeed > 0f
                ? horizontalSpeed / config.RunSpeed
                : 0f;

            // Terrain-Kompensation: Auf Treppen/Slopes wird die physische Geschwindigkeit
            // reduziert, aber die Animation soll im "Flachboden-Tempo" laufen.
            float terrainMultiplier = _playerController.Locomotion?.CurrentTerrainSpeedMultiplier ?? 1f;
            if (terrainMultiplier > 0.01f && terrainMultiplier < 1f)
            {
                if (config.FullAnimSpeedOnTerrain)
                {
                    // 3D-Geschwindigkeit: kompensiert sowohl Terrain-Penalty als auch
                    // geometrische cos(angle)-Reduktion der horizontalen Geschwindigkeit.
                    float speed3D = Mathf.Sqrt(horizontalSpeed * horizontalSpeed +
                                               data.VerticalVelocity * data.VerticalVelocity);
                    normalizedSpeed = config.RunSpeed > 0f ? speed3D / config.RunSpeed : 0f;
                    normalizedSpeed /= terrainMultiplier;
                }
                else
                {
                    normalizedSpeed /= terrainMultiplier;
                }
            }

            // Minimum Animation Speed: Wenn der Character sich physisch bewegt, muss
            // der Speed-Parameter hoch genug sein um sichtbare Bein-Animation im Blend Tree
            // auszulösen. Ohne dieses Minimum gleitet der Character bei niedrigen
            // Geschwindigkeiten mit Idle-Pose über den Boden ("Ice Skating").
            if (horizontalSpeed > 0.01f && normalizedSpeed < 0.35f)
            {
                normalizedSpeed = 0.35f;
            }

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

#if UNITY_EDITOR
            var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.fullPathHash != _prevAnimStateHash)
            {
                string stateName = "?";
                if (stateInfo.IsName("Locomotion")) stateName = "Locomotion";
                else if (stateInfo.IsName("Jump")) stateName = "Jump";
                else if (stateInfo.IsName("Fall")) stateName = "Fall";
                else if (stateInfo.IsName("SoftLand")) stateName = "SoftLand";
                else if (stateInfo.IsName("HardLand")) stateName = "HardLand";
                else if (stateInfo.IsName("LightStop")) stateName = "LightStop";
                else if (stateInfo.IsName("MediumStop")) stateName = "MediumStop";
                else if (stateInfo.IsName("HardStop")) stateName = "HardStop";

                Debug.Log($"[AnimBridge] Animator → {stateName} | Y={transform.position.y:F2}");
                _prevAnimStateHash = stateInfo.fullPathHash;
            }
#endif
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

            // Prüfen ob der State im Animator existiert, bevor CrossFade aufgerufen wird.
            // Fehlende States (z.B. LightStop ohne zugewiesenen Clip) verursachen
            // "Animator.GotoState: State could not be found" + "Invalid Layer Index '-1'"
            // was den gesamten Animator korrumpiert und andere Animationen stört.
            if (!_animator.HasState(AnimationParameters.BaseLayerIndex, hash))
            {
                Debug.LogWarning($"[AnimatorParameterBridge] State '{state}' nicht im Animator Controller gefunden. " +
                                 "Bitte Wizard erneut ausführen: Wiesenwischer > GameKit > Prefabs > Character Setup Wizard");
                return;
            }

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
