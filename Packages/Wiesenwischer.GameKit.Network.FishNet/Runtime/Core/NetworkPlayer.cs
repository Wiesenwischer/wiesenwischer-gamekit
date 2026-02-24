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

        /// <summary>
        /// Visual-Root (Animator-Child). Wird in OnStartNetwork() gecached, BEVOR
        /// NetworkTickSmoother.OnStartClient() das Child via DetachOnStart trennt.
        /// Danach ist GetComponentInChildren nicht mehr zuverlaessig.
        /// </summary>
        public Transform VisualRoot => _visualRoot;
        private Transform _visualRoot;

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

            // Visual-Root cachen BEVOR NetworkTickSmoother.OnStartClient() DetachOnStart ausfuehrt.
            // FishNet ruft Callbacks in Hierarchie-Reihenfolge: Root-NBs vor Child-NBs.
            var animator = GetComponentInChildren<Animator>();
            _visualRoot = (animator != null && animator.transform != transform)
                ? animator.transform
                : transform;
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
            // Visual-Root uebergeben (nicht Root!), damit die Kamera dem smooth-interpolierten
            // Visual folgt statt dem springenden Simulations-Root.
            OnLocalPlayerReady?.Invoke(_visualRoot);

            // NACH Kamera-Setup: Orientation/Facing-Provider auflösen.
            // In Start() wird dies im Netzwerk-Modus übersprungen, weil die Kamera
            // erst hier (via OnLocalPlayerReady → CameraBrain.SetTarget) eingerichtet wird.
            _playerController.ResolveProviders();
        }

        private void ConfigureRemotePlayer()
        {
            // _visualRoot wurde in OnStartNetwork() gecached (vor DetachOnStart).
            // GetComponentInChildren wuerde nach Detach fehlschlagen.

            // Animation Bridge in Remote-Modus setzen
            var animBridge = _visualRoot != null
                ? _visualRoot.GetComponent<AnimatorParameterBridge>()
                : GetComponentInChildren<AnimatorParameterBridge>();
            if (animBridge != null)
                animBridge.IsRemoteMode = true;

            // LookAt IK: Provider auf Network umstellen
            var lookAtIK = _visualRoot != null
                ? _visualRoot.GetComponentInChildren<LookAtIK>()
                : GetComponentInChildren<LookAtIK>();
            var networkProvider = GetComponent<NetworkLookAtTargetProvider>();
            if (lookAtIK != null && networkProvider != null)
                lookAtIK.SetTargetProvider(networkProvider);
        }
    }
}
