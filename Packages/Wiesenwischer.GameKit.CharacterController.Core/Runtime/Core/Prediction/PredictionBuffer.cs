using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Core.Prediction
{
    /// <summary>
    /// Buffer für Prediction-States.
    /// Speichert Character-Zustände nach Tick für Rollback.
    /// </summary>
    public class PredictionBuffer
    {
        private readonly PredictionState[] _buffer;
        private readonly int _capacity;

        private int _headIndex;
        private int _count;
        private int _oldestTick;
        private int _newestTick;

        /// <summary>
        /// Erstellt einen neuen PredictionBuffer.
        /// </summary>
        /// <param name="capacity">Maximale Anzahl gespeicherter States.</param>
        public PredictionBuffer(int capacity)
        {
            _capacity = capacity;
            _buffer = new PredictionState[capacity];
            _headIndex = 0;
            _count = 0;
            _oldestTick = -1;
            _newestTick = -1;
        }

        /// <summary>
        /// Anzahl der gespeicherten States.
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// Kapazität des Buffers.
        /// </summary>
        public int Capacity => _capacity;

        /// <summary>
        /// Der älteste gespeicherte Tick.
        /// </summary>
        public int OldestTick => _oldestTick;

        /// <summary>
        /// Der neueste gespeicherte Tick.
        /// </summary>
        public int NewestTick => _newestTick;

        /// <summary>
        /// Fügt einen State hinzu.
        /// </summary>
        public void Add(PredictionState state)
        {
            int tick = state.Tick;

            // Berechne Index im Ring-Buffer
            int index = (_headIndex + _count) % _capacity;

            // Wenn voll, überschreibe ältesten Eintrag
            if (_count >= _capacity)
            {
                _headIndex = (_headIndex + 1) % _capacity;
            }
            else
            {
                _count++;
            }

            _buffer[index] = state;

            // Update Tick-Range
            if (_oldestTick < 0)
            {
                _oldestTick = tick;
            }
            _newestTick = tick;

            // Aktualisiere oldest tick wenn nötig
            if (_count == _capacity)
            {
                _oldestTick = _buffer[_headIndex].Tick;
            }
        }

        /// <summary>
        /// Gibt den State für einen bestimmten Tick zurück.
        /// </summary>
        public bool TryGet(int tick, out PredictionState state)
        {
            state = default;

            if (_count == 0 || tick < _oldestTick || tick > _newestTick)
            {
                return false;
            }

            // Suche im Buffer
            for (int i = 0; i < _count; i++)
            {
                int index = (_headIndex + i) % _capacity;
                if (_buffer[index].Tick == tick)
                {
                    state = _buffer[index];
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gibt den neuesten State zurück.
        /// </summary>
        public bool TryGetLatest(out PredictionState state)
        {
            state = default;

            if (_count == 0)
            {
                return false;
            }

            int index = (_headIndex + _count - 1) % _capacity;
            state = _buffer[index];
            return true;
        }

        /// <summary>
        /// Gibt den State vor einem bestimmten Tick zurück (für Rollback).
        /// </summary>
        public bool TryGetBefore(int tick, out PredictionState state)
        {
            state = default;
            PredictionState? found = null;

            for (int i = 0; i < _count; i++)
            {
                int index = (_headIndex + i) % _capacity;
                if (_buffer[index].Tick < tick)
                {
                    if (!found.HasValue || _buffer[index].Tick > found.Value.Tick)
                    {
                        found = _buffer[index];
                    }
                }
            }

            if (found.HasValue)
            {
                state = found.Value;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Entfernt alle States nach einem bestimmten Tick (für Rollback).
        /// </summary>
        public void RemoveAfter(int tick)
        {
            while (_count > 0)
            {
                int lastIndex = (_headIndex + _count - 1) % _capacity;
                if (_buffer[lastIndex].Tick <= tick)
                {
                    break;
                }
                _count--;
            }

            if (_count > 0)
            {
                int lastIndex = (_headIndex + _count - 1) % _capacity;
                _newestTick = _buffer[lastIndex].Tick;
            }
            else
            {
                _oldestTick = -1;
                _newestTick = -1;
            }
        }

        /// <summary>
        /// Entfernt alle States vor einem bestimmten Tick.
        /// </summary>
        public void RemoveBefore(int tick)
        {
            while (_count > 0)
            {
                if (_buffer[_headIndex].Tick >= tick)
                {
                    break;
                }

                _headIndex = (_headIndex + 1) % _capacity;
                _count--;
            }

            if (_count > 0)
            {
                _oldestTick = _buffer[_headIndex].Tick;
            }
            else
            {
                _oldestTick = -1;
                _newestTick = -1;
            }
        }

        /// <summary>
        /// Leert den Buffer.
        /// </summary>
        public void Clear()
        {
            _headIndex = 0;
            _count = 0;
            _oldestTick = -1;
            _newestTick = -1;
        }

        /// <summary>
        /// Vergleicht einen State mit dem Server-State.
        /// </summary>
        /// <param name="serverState">Der Server-State.</param>
        /// <param name="positionThreshold">Maximale Positions-Abweichung.</param>
        /// <param name="velocityThreshold">Maximale Velocity-Abweichung.</param>
        /// <returns>True wenn der lokale State übereinstimmt.</returns>
        public bool ValidateAgainstServer(PredictionState serverState, float positionThreshold = 0.01f, float velocityThreshold = 0.1f)
        {
            if (!TryGet(serverState.Tick, out var localState))
            {
                return false;
            }

            // Vergleiche Position
            float positionDiff = Vector3.Distance(localState.Position, serverState.Position);
            if (positionDiff > positionThreshold)
            {
                return false;
            }

            // Vergleiche Velocity
            float velocityDiff = Vector3.Distance(localState.Velocity, serverState.Velocity);
            if (velocityDiff > velocityThreshold)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gibt alle States ab einem Tick zurück (für Re-Simulation nach Rollback).
        /// </summary>
        public List<PredictionState> GetFromTick(int fromTick)
        {
            var result = new List<PredictionState>();

            for (int i = 0; i < _count; i++)
            {
                int index = (_headIndex + i) % _capacity;
                if (_buffer[index].Tick >= fromTick)
                {
                    result.Add(_buffer[index]);
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Speichert den Zustand eines Characters für einen bestimmten Tick.
    /// </summary>
    [Serializable]
    public struct PredictionState : IEquatable<PredictionState>
    {
        /// <summary>
        /// Der Tick dieses States.
        /// </summary>
        public int Tick;

        /// <summary>
        /// Position des Characters.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// Rotation des Characters (Euler Y).
        /// </summary>
        public float Rotation;

        /// <summary>
        /// Aktuelle Geschwindigkeit.
        /// </summary>
        public Vector3 Velocity;

        /// <summary>
        /// Name des aktuellen States.
        /// </summary>
        public string StateName;

        /// <summary>
        /// Ob der Character auf dem Boden steht.
        /// </summary>
        public bool IsGrounded;

        /// <summary>
        /// Zeitstempel (Server-Zeit oder lokale Zeit).
        /// </summary>
        public float Timestamp;

        /// <summary>
        /// Erstellt einen PredictionState.
        /// </summary>
        public static PredictionState Create(
            int tick,
            Vector3 position,
            float rotation,
            Vector3 velocity,
            string stateName,
            bool isGrounded)
        {
            return new PredictionState
            {
                Tick = tick,
                Position = position,
                Rotation = rotation,
                Velocity = velocity,
                StateName = stateName ?? "Unknown",
                IsGrounded = isGrounded,
                Timestamp = Time.time
            };
        }

        public bool Equals(PredictionState other)
        {
            return Tick == other.Tick &&
                   Position.Equals(other.Position) &&
                   Mathf.Approximately(Rotation, other.Rotation) &&
                   Velocity.Equals(other.Velocity) &&
                   StateName == other.StateName &&
                   IsGrounded == other.IsGrounded;
        }

        public override bool Equals(object obj)
        {
            return obj is PredictionState other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Tick, Position, Rotation, Velocity, StateName, IsGrounded);
        }

        public override string ToString()
        {
            return $"[Tick {Tick}] Pos:{Position:F2} State:{StateName} Grounded:{IsGrounded}";
        }
    }
}
