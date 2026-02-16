using FishNet.Object;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core;
using Wiesenwischer.GameKit.CharacterController.Core.Prediction;

namespace Wiesenwischer.GameKit.Network
{
    /// <summary>
    /// Synchronisiert den autoritativen Server-State zu allen Clients.
    /// Owner-Client nutzt den State für Reconciliation (Rollback + Resim).
    /// Non-Owner-Clients nutzt den State für Interpolation.
    /// </summary>
    [RequireComponent(typeof(NetworkPlayer))]
    public class NetworkStateSync : NetworkBehaviour
    {
        [Header("Reconciliation")]
        [Tooltip("Positionsabweichung ab der ein Rollback ausgelöst wird (in Metern)")]
        [SerializeField] private float _positionThreshold = 0.1f;

        [Tooltip("Geschwindigkeitsabweichung ab der ein Rollback ausgelöst wird")]
        [SerializeField] private float _velocityThreshold = 0.5f;

        [Tooltip("Rate in Ticks für State-Broadcasts (z.B. alle 3 Ticks)")]
        [SerializeField] private int _broadcastRate = 3;

        [Header("Snap Correction")]
        [Tooltip("Maximale Distanz für sanfte Korrektur statt Teleport")]
        [SerializeField] private float _snapDistance = 2f;

        [Tooltip("Lerp-Speed für sanfte Position-Korrektur")]
        [SerializeField] private float _correctionSpeed = 10f;

        private PlayerController _player;
        private PredictionBuffer _predictionBuffer;
        private InputBuffer<ControllerInput> _inputBuffer;
        private int _lastBroadcastTick = -1;

        private bool _hasPendingCorrection;
        private Vector3 _correctionTarget;

        private RemotePlayerInterpolator _interpolator;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _player = GetComponent<PlayerController>();
            _interpolator = GetComponent<RemotePlayerInterpolator>();

            _predictionBuffer = new PredictionBuffer(capacity: 256);
            _inputBuffer = new InputBuffer<ControllerInput>(
                capacity: 256,
                tickGetter: input => input.Tick);
        }

        private void Update()
        {
            if (IsOwner)
            {
                RecordPredictionState();

                if (_hasPendingCorrection)
                    ApplySmoothCorrection();
            }
        }

        // === Server-Seite ===

        /// <summary>
        /// Vom Server aufgerufen nach jeder Simulation.
        /// Broadcastet den autoritativen State an alle Clients.
        /// </summary>
        [Server]
        public void BroadcastState(int tick, PredictionState state)
        {
            if (tick - _lastBroadcastTick < _broadcastRate) return;

            ObserverRpcReceiveState(state);
            _lastBroadcastTick = tick;
        }

        // === Client-Seite ===

        /// <summary>
        /// Alle Clients empfangen den Server-State.
        /// Owner → Reconciliation. Non-Owner → Interpolation Buffer.
        /// </summary>
        [ObserversRpc(BufferLast = true)]
        private void ObserverRpcReceiveState(PredictionState serverState)
        {
            if (IsOwner)
                HandleReconciliation(serverState);
            else
                HandleRemoteState(serverState);
        }

        private void HandleReconciliation(PredictionState serverState)
        {
            if (!_predictionBuffer.TryGet(serverState.Tick, out var localState))
            {
                ApplyHardCorrection(serverState);
                return;
            }

            bool positionMismatch =
                Vector3.Distance(localState.Position, serverState.Position) > _positionThreshold;
            bool velocityMismatch =
                Vector3.Distance(localState.Velocity, serverState.Velocity) > _velocityThreshold;

            if (!positionMismatch && !velocityMismatch)
            {
                _predictionBuffer.RemoveBefore(serverState.Tick);
                _inputBuffer.RemoveBefore(serverState.Tick);
                return;
            }

            PerformRollback(serverState);
        }

        private void PerformRollback(PredictionState serverState)
        {
            Debug.Log($"[Reconciliation] Rollback von Tick {serverState.Tick}" +
                      $" (Δpos={Vector3.Distance(_player.transform.position, serverState.Position):F3}m)");

            _player.SetPosition(serverState.Position);
            _predictionBuffer.RemoveAfter(serverState.Tick);

            int currentTick = _player.CurrentTick;
            var pendingInputs = _inputBuffer.GetRange(serverState.Tick + 1, currentTick);
            float tickDelta = _player.TickSystem.TickDelta;

            foreach (var input in pendingInputs)
            {
                _player.ApplyNetworkInput(input, tickDelta);

                var newState = CreateStateSnapshot(input.Tick);
                _predictionBuffer.Add(newState);
            }

            float distance = Vector3.Distance(
                _player.transform.position, serverState.Position);
            if (distance < _snapDistance)
            {
                _hasPendingCorrection = true;
                _correctionTarget = _player.transform.position;
            }
        }

        private void ApplyHardCorrection(PredictionState serverState)
        {
            _player.SetPosition(serverState.Position);
            _predictionBuffer.RemoveAfter(0);
        }

        private void ApplySmoothCorrection()
        {
            float step = _correctionSpeed * Time.deltaTime;
            _player.transform.position = Vector3.MoveTowards(
                _player.transform.position, _correctionTarget, step);

            if (Vector3.Distance(_player.transform.position, _correctionTarget) < 0.001f)
                _hasPendingCorrection = false;
        }

        private void HandleRemoteState(PredictionState serverState)
        {
            _predictionBuffer.Add(serverState);

            if (_interpolator != null)
                _interpolator.OnRemoteStateReceived(serverState);
        }

        // === Hilfsmethoden ===

        private void RecordPredictionState()
        {
            var state = CreateStateSnapshot(_player.CurrentTick);
            _predictionBuffer.Add(state);
        }

        private PredictionState CreateStateSnapshot(int tick)
        {
            return PredictionState.Create(
                tick: tick,
                position: _player.transform.position,
                rotation: _player.transform.eulerAngles.y,
                velocity: _player.Velocity,
                stateName: _player.MovementStateMachine?.CurrentStateName ?? "Unknown",
                isGrounded: _player.IsGrounded
            );
        }

        /// <summary>
        /// Gibt den PredictionBuffer zurück (für RemotePlayerInterpolator).
        /// </summary>
        public PredictionBuffer PredictionBuffer => _predictionBuffer;
    }
}
