using FishNet.Object;
using FishNet.Object.Synchronizing;
using Wiesenwischer.GameKit.Abilities.Core;
using Wiesenwischer.GameKit.CharacterController.Core.Animation;

namespace Wiesenwischer.GameKit.Network
{
    /// <summary>
    /// Synchronisiert Ability-Animationen über das Netzwerk.
    /// Lauscht auf AbilitySystem Events und broadcastet:
    /// - Layer-Weight-Änderungen (0 → 1 bei Aktivierung, 1 → 0 bei Deaktivierung)
    /// - Ability Animation State Name (für CrossFade auf Layer 1)
    /// </summary>
    [RequireComponent(typeof(NetworkPlayer))]
    public class NetworkAbilitySync : NetworkBehaviour
    {
        private AbilitySystem _abilitySystem;
        private IAnimationController _animController;

        [SyncVar(OnChange = nameof(OnAbilityLayerWeightChanged))]
        private float _abilityLayerWeight;

        [SyncVar(OnChange = nameof(OnAbilityAnimChanged))]
        private string _abilityAnimStateName = "";

        [SyncVar]
        private float _abilityTransitionDuration;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _abilitySystem = GetComponent<AbilitySystem>();
            _animController = GetComponentInChildren<IAnimationController>();

            if (IsOwner && _abilitySystem != null)
            {
                _abilitySystem.OnAbilityActivated += OnAbilityActivated;
                _abilitySystem.OnAbilityDeactivated += OnAbilityDeactivated;
                _abilitySystem.OnAbilityCancelled += OnAbilityCancelled;
            }
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            if (_abilitySystem != null)
            {
                _abilitySystem.OnAbilityActivated -= OnAbilityActivated;
                _abilitySystem.OnAbilityDeactivated -= OnAbilityDeactivated;
                _abilitySystem.OnAbilityCancelled -= OnAbilityCancelled;
            }
        }

        #region Owner: Ability Events

        private void OnAbilityActivated(IAbility ability)
        {
            if (!IsOwner) return;

            string animState = GetAnimStateName(ability);
            float duration = GetTransitionDuration(ability);

            if (IsServerStarted)
            {
                _abilityLayerWeight = 1f;
                _abilityAnimStateName = animState ?? "";
                _abilityTransitionDuration = duration;
            }
            else
            {
                ServerRpcAbilityActivated(animState ?? "", duration);
            }
        }

        private void OnAbilityDeactivated(IAbility ability)
        {
            if (!IsOwner) return;

            if (IsServerStarted)
            {
                _abilityLayerWeight = 0f;
                _abilityAnimStateName = "";
            }
            else
            {
                ServerRpcAbilityDeactivated();
            }
        }

        private void OnAbilityCancelled(IAbility ability)
        {
            if (!IsOwner) return;

            if (IsServerStarted)
            {
                _abilityLayerWeight = 0f;
                _abilityAnimStateName = "";
            }
            else
            {
                ServerRpcAbilityDeactivated();
            }
        }

        #endregion

        #region Server RPCs

        [ServerRpc]
        private void ServerRpcAbilityActivated(string animStateName, float transitionDuration)
        {
            _abilityLayerWeight = 1f;
            _abilityAnimStateName = animStateName;
            _abilityTransitionDuration = transitionDuration;
        }

        [ServerRpc]
        private void ServerRpcAbilityDeactivated()
        {
            _abilityLayerWeight = 0f;
            _abilityAnimStateName = "";
        }

        #endregion

        #region SyncVar Callbacks

        private void OnAbilityLayerWeightChanged(float prev, float next, bool asServer)
        {
            if (IsOwner) return;
            _animController?.SetAbilityLayerWeight(next);
        }

        private void OnAbilityAnimChanged(string prev, string next, bool asServer)
        {
            if (IsOwner) return;
            if (!string.IsNullOrEmpty(next))
            {
                _animController?.PlayAbilityAnimation(next, _abilityTransitionDuration);
            }
        }

        #endregion

        #region Helpers

        private string GetAnimStateName(IAbility ability)
        {
            if (ability is IAbilityAnimationHandler handler)
                return handler.AnimationStateName;
            return null;
        }

        private float GetTransitionDuration(IAbility ability)
        {
            if (ability is IAbilityAnimationHandler handler)
                return handler.TransitionDuration;
            return 0.1f;
        }

        #endregion
    }
}
