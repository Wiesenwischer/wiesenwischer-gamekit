using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
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

        private readonly SyncVar<float> _abilityLayerWeight = new();
        private readonly SyncVar<string> _abilityAnimStateName = new("");
        private readonly SyncVar<float> _abilityTransitionDuration = new();

        private void Awake()
        {
            _abilityLayerWeight.OnChange += OnAbilityLayerWeightChanged;
            _abilityAnimStateName.OnChange += OnAbilityAnimChanged;
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _abilitySystem = GetComponent<AbilitySystem>();
            _animController = GetComponentInChildren<IAnimationController>();

            if (Owner.IsLocalClient && _abilitySystem != null)
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
                _abilityLayerWeight.Value = 1f;
                _abilityAnimStateName.Value = animState ?? "";
                _abilityTransitionDuration.Value = duration;
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
                _abilityLayerWeight.Value = 0f;
                _abilityAnimStateName.Value = "";
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
                _abilityLayerWeight.Value = 0f;
                _abilityAnimStateName.Value = "";
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
            _abilityLayerWeight.Value = 1f;
            _abilityAnimStateName.Value = animStateName;
            _abilityTransitionDuration.Value = transitionDuration;
        }

        [ServerRpc]
        private void ServerRpcAbilityDeactivated()
        {
            _abilityLayerWeight.Value = 0f;
            _abilityAnimStateName.Value = "";
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
                _animController?.PlayAbilityAnimation(next, _abilityTransitionDuration.Value);
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
