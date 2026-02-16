using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Animation;

namespace Wiesenwischer.GameKit.Network
{
    /// <summary>
    /// Synchronisiert Animation-State-Wechsel und Parameter über das Netzwerk.
    /// State-Wechsel: Event-basiert (bei Änderung), reliable + SyncVar für Late-Joiner.
    /// Parameter (Speed, VerticalVelocity): Periodisch, unreliable, quantisiert.
    /// </summary>
    [RequireComponent(typeof(NetworkPlayer))]
    public class NetworkAnimationSync : NetworkBehaviour, IAnimationNetworkSync
    {
        [Header("Parameter Sync")]
        [Tooltip("Parameter-Sync alle N Frames (~20 Hz bei 3)")]
        [SerializeField] private int _parameterSyncRate = 3;

        private IAnimationController _animController;
        private CharacterAnimationState _lastSyncedState;
        private bool _initialized;

        private AnimationSnapshot _lastSnapshot;
        private int _framesSinceLastParamSync;

        /// <summary>
        /// SyncVar für Initial State Sync: Neue Clients erhalten den aktuellen State sofort.
        /// </summary>
        [SyncVar(OnChange = nameof(OnSyncAnimStateChanged))]
        private byte _syncAnimState;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _animController = GetComponentInChildren<IAnimationController>();
        }

        private void Update()
        {
            if (!IsOwner) return;
            if (_animController == null) return;

            _framesSinceLastParamSync++;
            if (_framesSinceLastParamSync >= _parameterSyncRate)
            {
                SyncParameters();
                _framesSinceLastParamSync = 0;
            }
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
                // Host: SyncVar direkt setzen + an Observer broadcasten
                _syncAnimState = (byte)state;
                ObserverRpcAnimationState((byte)state);
            }
            else
            {
                ServerRpcAnimationState((byte)state);
            }
        }

        private void OnSyncAnimStateChanged(byte prev, byte next, bool asServer)
        {
            if (IsOwner) return;
            if (_animController == null) return;

            var state = (CharacterAnimationState)next;
            _animController.PlayState(state);
        }

        private void SyncParameters()
        {
            var snapshot = AnimationSnapshot.Create(
                speed: _animController.CurrentSpeed,
                verticalVelocity: _animController.CurrentVerticalVelocity
            );

            if (snapshot.Equals(_lastSnapshot)) return;

            _lastSnapshot = snapshot;

            if (IsServerStarted)
            {
                ObserverRpcAnimationParams(snapshot);
            }
            else
            {
                ServerRpcAnimationParams(snapshot);
            }
        }

        #region State RPCs

        [ServerRpc]
        private void ServerRpcAnimationState(byte stateValue)
        {
            // SyncVar aktualisieren für Late-Joiner
            _syncAnimState = stateValue;
            ObserverRpcAnimationState(stateValue);
        }

        [ObserversRpc(ExcludeOwner = true)]
        private void ObserverRpcAnimationState(byte stateValue)
        {
            var state = (CharacterAnimationState)stateValue;
            _animController?.PlayState(state);
        }

        #endregion

        #region Parameter RPCs

        [ServerRpc(Channel = Channel.Unreliable)]
        private void ServerRpcAnimationParams(AnimationSnapshot snapshot)
        {
            ObserverRpcAnimationParams(snapshot);
        }

        [ObserversRpc(ExcludeOwner = true, Channel = Channel.Unreliable)]
        private void ObserverRpcAnimationParams(AnimationSnapshot snapshot)
        {
            _animController?.SetSpeed(snapshot.Speed);
            _animController?.SetVerticalVelocity(snapshot.VerticalVelocity);
        }

        #endregion
    }
}
