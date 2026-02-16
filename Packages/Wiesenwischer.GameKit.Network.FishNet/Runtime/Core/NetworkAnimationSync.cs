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
    /// Lag Compensation: Transition-Dauer wird um Netzwerk-Delay gekürzt.
    /// Sequence Numbers: Out-of-order States werden ignoriert.
    /// </summary>
    [RequireComponent(typeof(NetworkPlayer))]
    public class NetworkAnimationSync : NetworkBehaviour, IAnimationNetworkSync
    {
        [Header("Parameter Sync")]
        [Tooltip("Parameter-Sync alle N Frames (~20 Hz bei 3)")]
        [SerializeField] private int _parameterSyncRate = 3;

        [Header("Lag Compensation")]
        [Tooltip("Default Transition-Dauer wenn keine Config vorhanden")]
        [SerializeField] private float _defaultTransitionDuration = 0.15f;

        private IAnimationController _animController;
        private CharacterAnimationState _lastSyncedState;
        private bool _initialized;

        private AnimationSnapshot _lastSnapshot;
        private int _framesSinceLastParamSync;

        // Lag Compensation
        private ushort _stateSequence;
        private ushort _lastReceivedSequence;

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
            _stateSequence++;

            float timestamp = (float)TimeManager.TicksToTime(TimeManager.Tick);

            if (IsServerStarted)
            {
                _syncAnimState = (byte)state;
                ObserversRpcAnimationState((byte)state, timestamp, _stateSequence);
            }
            else
            {
                ServerRpcAnimationState((byte)state, timestamp, _stateSequence);
            }
        }

        private void OnSyncAnimStateChanged(byte prev, byte next, bool asServer)
        {
            // SyncVar Callback für Late-Joiner — einfach PlayState ohne Lag Compensation
            if (IsOwner) return;
            if (_animController == null) return;

            _animController.PlayState((CharacterAnimationState)next);
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
                ObserversRpcAnimationParams(snapshot);
            }
            else
            {
                ServerRpcAnimationParams(snapshot);
            }
        }

        #region State RPCs

        [ServerRpc]
        private void ServerRpcAnimationState(byte stateValue, float timestamp, ushort sequence)
        {
            _syncAnimState = stateValue;
            ObserversRpcAnimationState(stateValue, timestamp, sequence);
        }

        [ObserversRpc(ExcludeOwner = true)]
        private void ObserversRpcAnimationState(byte stateValue, float timestamp, ushort sequence)
        {
            // Out-of-order Check
            if (IsSequenceOlder(sequence, _lastReceivedSequence)) return;
            _lastReceivedSequence = sequence;

            var state = (CharacterAnimationState)stateValue;

            // Lag Compensation: Netzwerk-Delay von Transition-Dauer abziehen
            float currentTime = (float)TimeManager.TicksToTime(TimeManager.Tick);
            float networkDelay = Mathf.Clamp(currentTime - timestamp, 0f, 0.5f);
            float adjustedTransition = Mathf.Max(0f, _defaultTransitionDuration - networkDelay);

            _animController?.PlayState(state, adjustedTransition);
        }

        #endregion

        #region Parameter RPCs

        [ServerRpc(Channel = Channel.Unreliable)]
        private void ServerRpcAnimationParams(AnimationSnapshot snapshot)
        {
            ObserversRpcAnimationParams(snapshot);
        }

        [ObserversRpc(ExcludeOwner = true, Channel = Channel.Unreliable)]
        private void ObserversRpcAnimationParams(AnimationSnapshot snapshot)
        {
            _animController?.SetSpeed(snapshot.Speed);
            _animController?.SetVerticalVelocity(snapshot.VerticalVelocity);
        }

        #endregion

        /// <summary>
        /// Wrap-around safe Sequence-Vergleich.
        /// </summary>
        private static bool IsSequenceOlder(ushort test, ushort reference)
        {
            return (short)(test - reference) < 0;
        }
    }
}
