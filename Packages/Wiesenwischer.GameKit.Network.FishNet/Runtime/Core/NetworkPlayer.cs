using FishNet.Object;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core;
using Wiesenwischer.GameKit.CharacterController.Core.Input;

namespace Wiesenwischer.GameKit.Network
{
    /// <summary>
    /// Root NetworkBehaviour für einen Spieler.
    /// Wraps PlayerController und stellt Network-Authority-Kontext bereit.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class NetworkPlayer : NetworkBehaviour, INetworkRole
    {
        private PlayerController _playerController;

        // INetworkRole Implementation — delegates to FishNet NetworkBehaviour
        bool INetworkRole.IsOwner => base.IsOwner;
        public bool IsServer => base.IsServerStarted;
        public bool IsClient => base.IsClientStarted;
        public bool IsNetworkActive => true;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _playerController = GetComponent<PlayerController>();
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
        }

        private void EnableLocalPlayer()
        {
            Debug.Log("[NetworkPlayer] Lokaler Spieler initialisiert.");
        }

        private void DisableRemotePlayerInput()
        {
            var inputProvider = GetComponent<IMovementInputProvider>();
            if (inputProvider is MonoBehaviour inputMono)
            {
                inputMono.enabled = false;
            }

            // CharacterMotor deaktivieren (Interpolator übernimmt Position für Remote)
            var motor = GetComponent<CharacterController.Core.Motor.CharacterMotor>();
            if (motor != null)
                motor.enabled = false;

            Debug.Log("[NetworkPlayer] Remote Spieler — Input + Motor deaktiviert.");
        }
    }
}
