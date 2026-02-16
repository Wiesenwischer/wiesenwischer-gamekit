using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Prediction;

namespace Wiesenwischer.GameKit.Network
{
    /// <summary>
    /// Interpoliert Position und Rotation für Remote-Spieler
    /// basierend auf gepufferten Server-States.
    /// Verwendet Entity Interpolation mit konfigurierbarem Delay.
    /// </summary>
    [RequireComponent(typeof(NetworkPlayer))]
    public class RemotePlayerInterpolator : NetworkBehaviour
    {
        [Header("Interpolation")]
        [Tooltip("Interpolations-Delay in Sekunden (Buffer für Jitter)")]
        [SerializeField] private float _interpolationDelay = 0.1f;

        [Tooltip("Extrapolations-Limit wenn keine neuen States ankommen")]
        [SerializeField] private float _maxExtrapolationTime = 0.25f;

        [Header("Smoothing")]
        [Tooltip("Teleport-Schwelle — über dieser Distanz wird direkt gesprungen")]
        [SerializeField] private float _teleportThreshold = 5f;

        private readonly List<TimedState> _stateBuffer = new();
        private float _lastReceiveTime;
        private bool _isExtrapolating;

        private struct TimedState
        {
            public float Time;
            public Vector3 Position;
            public float Rotation;
            public Vector3 Velocity;
            public bool IsGrounded;
        }

        /// <summary>
        /// Wird von NetworkStateSync aufgerufen wenn ein neuer
        /// Server-State für diesen Remote-Spieler eintrifft.
        /// </summary>
        public void OnRemoteStateReceived(PredictionState state)
        {
            float receiveTime = Time.time;
            _lastReceiveTime = receiveTime;
            _isExtrapolating = false;

            _stateBuffer.Add(new TimedState
            {
                Time = receiveTime,
                Position = state.Position,
                Rotation = state.Rotation,
                Velocity = state.Velocity,
                IsGrounded = state.IsGrounded
            });

            // Buffer aufräumen — maximal 1 Sekunde History
            while (_stateBuffer.Count > 0 &&
                   _stateBuffer[0].Time < receiveTime - 1f)
            {
                _stateBuffer.RemoveAt(0);
            }
        }

        private void Update()
        {
            if (IsOwner) return;
            // Auf dem Server: Position kommt aus der autoritativen Simulation, nicht Interpolation
            if (IsServerStarted) return;
            if (_stateBuffer.Count < 2) return;

            float renderTime = Time.time - _interpolationDelay;

            if (TryFindInterpolationStates(renderTime,
                out var from, out var to, out float t))
            {
                ApplyInterpolation(from, to, t);
            }
            else if (Time.time - _lastReceiveTime < _maxExtrapolationTime)
            {
                Extrapolate();
            }
        }

        private bool TryFindInterpolationStates(float targetTime,
            out TimedState from, out TimedState to, out float t)
        {
            from = default;
            to = default;
            t = 0f;

            for (int i = 0; i < _stateBuffer.Count - 1; i++)
            {
                if (_stateBuffer[i].Time <= targetTime &&
                    _stateBuffer[i + 1].Time >= targetTime)
                {
                    from = _stateBuffer[i];
                    to = _stateBuffer[i + 1];
                    float duration = to.Time - from.Time;
                    t = duration > 0 ? (targetTime - from.Time) / duration : 0f;
                    return true;
                }
            }

            return false;
        }

        private void ApplyInterpolation(TimedState from, TimedState to, float t)
        {
            Vector3 targetPos = Vector3.Lerp(from.Position, to.Position, t);
            float targetRot = Mathf.LerpAngle(from.Rotation, to.Rotation, t);

            float distance = Vector3.Distance(transform.position, targetPos);
            if (distance > _teleportThreshold)
            {
                transform.position = targetPos;
            }
            else
            {
                transform.position = targetPos;
            }

            transform.rotation = Quaternion.Euler(0, targetRot, 0);
        }

        private void Extrapolate()
        {
            if (_stateBuffer.Count == 0) return;

            var lastState = _stateBuffer[^1];
            float timeSinceLastState = Time.time - lastState.Time;

            Vector3 extrapolatedPos =
                lastState.Position + lastState.Velocity * timeSinceLastState;

            transform.position = extrapolatedPos;
            _isExtrapolating = true;
        }

        /// <summary>Buffer leeren (z.B. bei Respawn).</summary>
        public void ClearBuffer()
        {
            _stateBuffer.Clear();
            _isExtrapolating = false;
        }
    }
}
