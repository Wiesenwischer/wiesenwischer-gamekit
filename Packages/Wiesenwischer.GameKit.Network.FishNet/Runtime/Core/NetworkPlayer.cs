using FishNet.Object;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Animation;
using Wiesenwischer.GameKit.CharacterController.Core;
using Wiesenwischer.GameKit.CharacterController.Core.Input;
using Wiesenwischer.GameKit.CharacterController.IK.Modules;

namespace Wiesenwischer.GameKit.Network
{
    /// <summary>
    /// Root NetworkBehaviour für einen Spieler.
    /// Wraps PlayerController und stellt Network-Authority-Kontext bereit.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class NetworkPlayer : NetworkBehaviour, INetworkRole
    {
        /// <summary>Wird gefeuert wenn der lokale Spieler bereit ist. Parameter: Player Transform.</summary>
        public static event System.Action<Transform> OnLocalPlayerReady;

        /// <summary>Wird gefeuert wenn der lokale Spieler entfernt wird.</summary>
        public static event System.Action OnLocalPlayerRemoved;

        private PlayerController _playerController;
        private NetworkCharacterDriver _characterDriver;

        // INetworkRole Implementation — delegates to FishNet NetworkBehaviour
        bool INetworkRole.IsOwner => base.IsOwner;
        public bool IsServer => base.IsServerStarted;
        public bool IsClient => base.IsClientStarted;
        public bool IsNetworkActive => true;

        /// <summary>Der NetworkCharacterDriver (falls vorhanden).</summary>
        public NetworkCharacterDriver CharacterDriver => _characterDriver;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _playerController = GetComponent<PlayerController>();
            _characterDriver = GetComponent<NetworkCharacterDriver>();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (IsOwner)
            {
                EnableLocalPlayer();
            }
            else
            {
                DisableRemotePlayerInput();
            }
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            if (IsOwner)
                OnLocalPlayerRemoved?.Invoke();
        }

        private void EnableLocalPlayer()
        {
            Debug.Log("[NetworkPlayer] Lokaler Spieler initialisiert.");
            OnLocalPlayerReady?.Invoke(transform);
        }

        private void DisableRemotePlayerInput()
        {
            // Input immer deaktivieren für Remote-Player
            var inputProvider = GetComponent<IMovementInputProvider>();
            if (inputProvider is MonoBehaviour inputMono)
                inputMono.enabled = false;

            // Motor bleibt aktiv — FishNet Spectator-Prediction nutzt [Replicate]
            // auch fuer Non-Owner-Player (state.IsFuture() → letzter Input wird wiederholt).
            // CharacterMotorSystem.Simulate() steuert den Motor explizit pro Tick.

            // Animation Bridge in Remote-Modus setzen
            var animBridge = GetComponentInChildren<AnimatorParameterBridge>();
            if (animBridge != null)
                animBridge.IsRemoteMode = true;

            // LookAt IK: Provider auf Network umstellen
            var lookAtIK = GetComponentInChildren<LookAtIK>();
            var networkProvider = GetComponent<NetworkLookAtTargetProvider>();
            if (lookAtIK != null && networkProvider != null)
                lookAtIK.SetTargetProvider(networkProvider);

            Debug.Log("[NetworkPlayer] Remote Spieler — Input deaktiviert, Motor aktiv (Spectator-Prediction)");
        }
    }
}
