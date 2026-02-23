using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.IK;

namespace Wiesenwischer.GameKit.Network
{
    /// <summary>
    /// IIKTargetProvider für Remote-Spieler.
    /// Empfängt LookAt-Zielposition vom Netzwerk und interpoliert
    /// sie für flüssiges Head-Tracking.
    /// </summary>
    public class NetworkLookAtTargetProvider : NetworkBehaviour, IIKTargetProvider
    {
        [Header("Interpolation")]
        [SerializeField] private float _interpolationSpeed = 8f;

        [Header("Sync Settings")]
        [Tooltip("Sync-Rate in Frames (6 = ~10 Hz bei 60 FPS)")]
        [SerializeField] private int _syncRate = 6;

        private Vector3 _targetPosition;
        private Vector3 _smoothedPosition;
        private bool _hasTarget;
        private int _framesSinceSync;

        private IIKTargetProvider _localProvider;

        public bool HasLookTarget => _hasTarget;
        public Vector3 GetLookTarget() => _smoothedPosition;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();

            if (Owner.IsLocalClient)
            {
                // Owner: Lokalen Provider finden (z.B. CameraTargetProvider)
                var providers = GetComponentsInChildren<IIKTargetProvider>();
                foreach (var p in providers)
                {
                    if (!ReferenceEquals(p, this))
                    {
                        _localProvider = p;
                        break;
                    }
                }
            }
        }

        private void Update()
        {
            if (IsOwner)
            {
                OwnerUpdate();
            }
            else
            {
                RemoteUpdate();
            }
        }

        private void OwnerUpdate()
        {
            if (_localProvider == null) return;

            _framesSinceSync++;
            if (_framesSinceSync < _syncRate) return;
            _framesSinceSync = 0;

            bool hasTarget = _localProvider.HasLookTarget;
            Vector3 target = hasTarget ? _localProvider.GetLookTarget() : Vector3.zero;

            if (IsServerStarted)
            {
                ObserversRpcSyncLookTarget(hasTarget, target);
            }
            else
            {
                ServerRpcSyncLookTarget(hasTarget, target);
            }
        }

        private void RemoteUpdate()
        {
            if (!_hasTarget) return;

            _smoothedPosition = Vector3.Lerp(
                _smoothedPosition, _targetPosition,
                _interpolationSpeed * Time.deltaTime);
        }

        [ServerRpc]
        private void ServerRpcSyncLookTarget(bool hasTarget, Vector3 target, Channel channel = Channel.Unreliable)
        {
            ObserversRpcSyncLookTarget(hasTarget, target, channel);
        }

        [ObserversRpc(ExcludeOwner = true)]
        private void ObserversRpcSyncLookTarget(bool hasTarget, Vector3 target, Channel channel = Channel.Unreliable)
        {
            _hasTarget = hasTarget;
            _targetPosition = target;
        }
    }
}
