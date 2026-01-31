using System;
using System.Collections.Generic;

namespace Wiesenwischer.GameKit.CharacterController.Core.Prediction
{
    /// <summary>
    /// Generischer Ring-Buffer für Input-Daten.
    /// Speichert Inputs nach Tick für CSP-Zwecke.
    /// </summary>
    /// <typeparam name="T">Der Input-Typ (muss Tick-Property haben).</typeparam>
    public class InputBuffer<T> where T : struct
    {
        private readonly T[] _buffer;
        private readonly int _capacity;
        private readonly Func<T, int> _tickGetter;

        private int _headIndex;
        private int _count;
        private int _oldestTick;
        private int _newestTick;

        /// <summary>
        /// Erstellt einen neuen InputBuffer.
        /// </summary>
        /// <param name="capacity">Maximale Anzahl gespeicherter Inputs.</param>
        /// <param name="tickGetter">Funktion zum Extrahieren des Ticks aus einem Input.</param>
        public InputBuffer(int capacity, Func<T, int> tickGetter)
        {
            _capacity = capacity;
            _tickGetter = tickGetter ?? throw new ArgumentNullException(nameof(tickGetter));
            _buffer = new T[capacity];
            _headIndex = 0;
            _count = 0;
            _oldestTick = -1;
            _newestTick = -1;
        }

        /// <summary>
        /// Anzahl der gespeicherten Inputs.
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// Kapazität des Buffers.
        /// </summary>
        public int Capacity => _capacity;

        /// <summary>
        /// Ob der Buffer voll ist.
        /// </summary>
        public bool IsFull => _count >= _capacity;

        /// <summary>
        /// Der älteste gespeicherte Tick.
        /// </summary>
        public int OldestTick => _oldestTick;

        /// <summary>
        /// Der neueste gespeicherte Tick.
        /// </summary>
        public int NewestTick => _newestTick;

        /// <summary>
        /// Fügt einen Input hinzu.
        /// </summary>
        /// <param name="input">Der Input.</param>
        public void Add(T input)
        {
            int tick = _tickGetter(input);

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

            _buffer[index] = input;

            // Update Tick-Range
            if (_oldestTick < 0 || tick < _oldestTick)
            {
                _oldestTick = tick;
            }
            _newestTick = tick;

            // Aktualisiere oldest tick wenn nötig
            if (_count == _capacity)
            {
                _oldestTick = _tickGetter(_buffer[_headIndex]);
            }
        }

        /// <summary>
        /// Gibt den Input für einen bestimmten Tick zurück.
        /// </summary>
        /// <param name="tick">Der gesuchte Tick.</param>
        /// <param name="input">Der gefundene Input.</param>
        /// <returns>True wenn gefunden.</returns>
        public bool TryGet(int tick, out T input)
        {
            input = default;

            if (_count == 0 || tick < _oldestTick || tick > _newestTick)
            {
                return false;
            }

            // Suche im Buffer
            for (int i = 0; i < _count; i++)
            {
                int index = (_headIndex + i) % _capacity;
                if (_tickGetter(_buffer[index]) == tick)
                {
                    input = _buffer[index];
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gibt den neuesten Input zurück.
        /// </summary>
        public bool TryGetLatest(out T input)
        {
            input = default;

            if (_count == 0)
            {
                return false;
            }

            int index = (_headIndex + _count - 1) % _capacity;
            input = _buffer[index];
            return true;
        }

        /// <summary>
        /// Gibt alle Inputs ab einem bestimmten Tick zurück.
        /// </summary>
        /// <param name="fromTick">Der Start-Tick (inklusive).</param>
        /// <returns>Liste der Inputs.</returns>
        public List<T> GetFromTick(int fromTick)
        {
            var result = new List<T>();

            for (int i = 0; i < _count; i++)
            {
                int index = (_headIndex + i) % _capacity;
                if (_tickGetter(_buffer[index]) >= fromTick)
                {
                    result.Add(_buffer[index]);
                }
            }

            return result;
        }

        /// <summary>
        /// Gibt alle Inputs in einem Tick-Bereich zurück.
        /// </summary>
        /// <param name="fromTick">Start-Tick (inklusive).</param>
        /// <param name="toTick">End-Tick (inklusive).</param>
        /// <returns>Liste der Inputs.</returns>
        public List<T> GetRange(int fromTick, int toTick)
        {
            var result = new List<T>();

            for (int i = 0; i < _count; i++)
            {
                int index = (_headIndex + i) % _capacity;
                int tick = _tickGetter(_buffer[index]);
                if (tick >= fromTick && tick <= toTick)
                {
                    result.Add(_buffer[index]);
                }
            }

            return result;
        }

        /// <summary>
        /// Entfernt alle Inputs vor einem bestimmten Tick.
        /// </summary>
        /// <param name="beforeTick">Inputs vor diesem Tick werden entfernt.</param>
        public void RemoveBefore(int beforeTick)
        {
            while (_count > 0)
            {
                if (_tickGetter(_buffer[_headIndex]) >= beforeTick)
                {
                    break;
                }

                _headIndex = (_headIndex + 1) % _capacity;
                _count--;
            }

            if (_count > 0)
            {
                _oldestTick = _tickGetter(_buffer[_headIndex]);
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
        /// Ob ein Input für den Tick existiert.
        /// </summary>
        public bool HasTick(int tick)
        {
            return TryGet(tick, out _);
        }
    }
}
