using FishNet.Object;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core;
using Wiesenwischer.GameKit.CharacterController.Core.Prediction;

namespace Wiesenwischer.GameKit.Network
{
    /// <summary>
    /// Synchronisiert Client-Input zum Server.
    /// Owner-Client sammelt ControllerInput pro Tick und sendet Batches.
    /// Server empfängt, validiert und simuliert.
    /// </summary>
    [RequireComponent(typeof(NetworkPlayer))]
    public class NetworkInputSync : NetworkBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int _batchSize = 3;
        [SerializeField] private float _maxMoveInputMagnitude = 1.1f;

        private PlayerController _player;
        private InputBuffer<ControllerInput> _inputBuffer;
        private int _lastSentTick = -1;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _player = GetComponent<PlayerController>();
            _inputBuffer = new InputBuffer<ControllerInput>(
                capacity: 128,
                tickGetter: input => input.Tick);
        }

        private void Update()
        {
            if (!IsOwner) return;
            if (_player == null || _player.InputProvider == null) return;

            RecordCurrentInput();

            // Host (Owner + Server): kein Input-Versand nötig,
            // lokaler PlayerController.Update() handhabt Bewegung direkt.
            if (IsServerStarted) return;

            if (ShouldSendBatch())
                SendInputBatch();
        }

        private void RecordCurrentInput()
        {
            var input = CreateControllerInput();
            _inputBuffer.Add(input);
        }

        private ControllerInput CreateControllerInput()
        {
            var inputProvider = _player.InputProvider;
            var buttons = ControllerButtons.None;

            if (inputProvider.JumpPressed) buttons |= ControllerButtons.Jump;
            if (inputProvider.SprintHeld) buttons |= ControllerButtons.Sprint;
            if (inputProvider.CrouchTogglePressed) buttons |= ControllerButtons.Crouch;

            return ControllerInput.Create(
                tick: _player.CurrentTick,
                move: inputProvider.MoveInput,
                look: inputProvider.LookInput,
                rotation: _player.transform.eulerAngles.y,
                buttons: buttons
            );
        }

        private bool ShouldSendBatch()
        {
            return _player.CurrentTick - _lastSentTick >= _batchSize;
        }

        private void SendInputBatch()
        {
            int fromTick = _lastSentTick + 1;
            int toTick = _player.CurrentTick;
            var inputs = _inputBuffer.GetRange(fromTick, toTick);

            if (inputs.Count == 0) return;

            ServerRpcReceiveInput(inputs.ToArray());
            _lastSentTick = toTick;
        }

        /// <summary>
        /// Server empfängt Input-Batch vom Client.
        /// Validiert und simuliert jeden Input.
        /// </summary>
        [ServerRpc]
        private void ServerRpcReceiveInput(ControllerInput[] inputs)
        {
            foreach (var input in inputs)
            {
                if (!ValidateInput(input))
                {
                    Debug.LogWarning(
                        $"[NetworkInputSync] Ungültiger Input von " +
                        $"Client {OwnerId} bei Tick {input.Tick}");
                    continue;
                }

                SimulateOnServer(input);
            }
        }

        private bool ValidateInput(ControllerInput input)
        {
            if (input.MoveDirection.magnitude > _maxMoveInputMagnitude)
                return false;

            return true;
        }

        private void SimulateOnServer(ControllerInput input)
        {
            if (_player?.TickSystem == null) return;

            float tickDelta = _player.TickSystem.TickDelta;
            _player.ApplyNetworkInput(input, tickDelta);

            // State an alle Clients broadcasten
            var stateSync = GetComponent<NetworkStateSync>();
            if (stateSync != null)
            {
                var state = PredictionState.Create(
                    tick: input.Tick,
                    position: _player.transform.position,
                    rotation: _player.transform.eulerAngles.y,
                    velocity: _player.Velocity,
                    stateName: _player.MovementStateMachine?.CurrentStateName ?? "Unknown",
                    isGrounded: _player.IsGrounded
                );
                stateSync.BroadcastState(input.Tick, state);
            }
        }
    }
}
