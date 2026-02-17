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
        public bool IsNetworkActive => IsSpawned;

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
                EnableLocalPlayer();
            else
                ConfigureRemotePlayer();
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            if (IsOwner)
                OnLocalPlayerRemoved?.Invoke();
        }

        private void EnableLocalPlayer()
        {
            // Scene-InputProvider mit diesem Player verbinden.
            // PlayerInputProvider liegt in der Scene (nicht auf dem Prefab).
            var sceneInput = FindObjectOfType<PlayerInputProvider>();
            if (sceneInput != null)
                _playerController.SetInputProvider(sceneInput);

            // Event feuern → NetworkCameraSetup richtet Kamera ein (synchron).
            OnLocalPlayerReady?.Invoke(transform);

            // NACH Kamera-Setup: Orientation/Facing-Provider auflösen.
            // In Start() wird dies im Netzwerk-Modus übersprungen, weil die Kamera
            // erst hier (via OnLocalPlayerReady → CameraBrain.SetTarget) eingerichtet wird.
            _playerController.ResolveProviders();
        }

        private void ConfigureRemotePlayer()
        {
            // Animation Bridge in Remote-Modus setzen
            var animBridge = GetComponentInChildren<AnimatorParameterBridge>();
            if (animBridge != null)
                animBridge.IsRemoteMode = true;

            // LookAt IK: Provider auf Network umstellen
            var lookAtIK = GetComponentInChildren<LookAtIK>();
            var networkProvider = GetComponent<NetworkLookAtTargetProvider>();
            if (lookAtIK != null && networkProvider != null)
                lookAtIK.SetTargetProvider(networkProvider);
        }
    }
}
