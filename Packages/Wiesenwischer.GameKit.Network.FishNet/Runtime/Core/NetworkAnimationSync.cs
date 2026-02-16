using FishNet.Object;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Animation;

namespace Wiesenwischer.GameKit.Network
{
    /// <summary>
    /// Synchronisiert Animation-State-Wechsel über das Netzwerk.
    /// Erfasst PlayState-Aufrufe auf dem Owner und broadcastet sie
    /// an alle Observer-Clients.
    /// </summary>
    [RequireComponent(typeof(NetworkPlayer))]
    public class NetworkAnimationSync : NetworkBehaviour, IAnimationNetworkSync
    {
        private IAnimationController _animController;
        private CharacterAnimationState _lastSyncedState;
        private bool _initialized;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _animController = GetComponentInChildren<IAnimationController>();
        }

        /// <summary>
        /// Wird vom AnimatorParameterBridge aufgerufen wenn PlayState() getriggert wird.
        /// Nur der Owner sendet State-Änderungen.
        /// </summary>
        public void OnLocalStateChanged(CharacterAnimationState state)
        {
            if (!IsOwner) return;
            if (state == _lastSyncedState && _initialized) return;

            _lastSyncedState = state;
            _initialized = true;

            if (IsServerStarted)
            {
                // Host: direkt an Observer broadcasten
                ObserverRpcAnimationState((byte)state);
            }
            else
            {
                // Client: an Server senden
                ServerRpcAnimationState((byte)state);
            }
        }

        [ServerRpc]
        private void ServerRpcAnimationState(byte stateValue)
        {
            ObserverRpcAnimationState(stateValue);
        }

        [ObserversRpc(ExcludeOwner = true)]
        private void ObserverRpcAnimationState(byte stateValue)
        {
            var state = (CharacterAnimationState)stateValue;
            _animController?.PlayState(state);
        }
    }
}
