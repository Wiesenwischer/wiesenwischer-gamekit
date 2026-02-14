using System;
using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Core
{
    /// <summary>
    /// Verwaltet ein Fixed Tick System für deterministische Updates.
    /// Unabhängig von Unity's FixedUpdate für bessere Kontrolle.
    /// </summary>
    public class TickSystem
    {
        /// <summary>
        /// Standard Tick-Rate (60 Hz).
        /// </summary>
        public const float DefaultTickRate = 60f;

        private readonly float _tickRate;
        private readonly float _tickDelta;
        private float _accumulator;
        private int _currentTick;
        private bool _isPaused;

        /// <summary>
        /// Wird bei jedem Tick aufgerufen.
        /// </summary>
        public event Action<int, float> OnTick;

        /// <summary>
        /// Erstellt ein neues TickSystem.
        /// </summary>
        /// <param name="tickRate">Ticks pro Sekunde (default: 60).</param>
        public TickSystem(float tickRate = DefaultTickRate)
        {
            if (tickRate <= 0)
            {
                throw new ArgumentException("Tick rate must be positive.", nameof(tickRate));
            }

            _tickRate = tickRate;
            _tickDelta = 1f / tickRate;
            _accumulator = 0f;
            _currentTick = 0;
            _isPaused = false;
        }

        /// <summary>
        /// Die konfigurierte Tick-Rate (Ticks pro Sekunde).
        /// </summary>
        public float TickRate => _tickRate;

        /// <summary>
        /// Die Zeit zwischen Ticks (1 / TickRate).
        /// </summary>
        public float TickDelta => _tickDelta;

        /// <summary>
        /// Der aktuelle Tick-Index.
        /// </summary>
        public int CurrentTick => _currentTick;

        /// <summary>
        /// Akkumulierte Zeit seit dem letzten Tick.
        /// Nützlich für Interpolation.
        /// </summary>
        public float Accumulator => _accumulator;

        /// <summary>
        /// Interpolations-Faktor (0-1) zwischen letztem und nächstem Tick.
        /// </summary>
        public float InterpolationAlpha => _accumulator / _tickDelta;

        /// <summary>
        /// Ob das System pausiert ist.
        /// </summary>
        public bool IsPaused
        {
            get => _isPaused;
            set => _isPaused = value;
        }

        /// <summary>
        /// Aktualisiert das Tick-System.
        /// Sollte jeden Frame mit Time.deltaTime aufgerufen werden.
        /// </summary>
        /// <param name="deltaTime">Die verstrichene Zeit seit dem letzten Frame.</param>
        /// <returns>Anzahl der ausgeführten Ticks.</returns>
        public int Update(float deltaTime)
        {
            if (_isPaused)
            {
                return 0;
            }

            int ticksExecuted = 0;
            _accumulator += deltaTime;

            // Maximal 10 Ticks pro Frame um Spiral of Death zu vermeiden
            int maxTicksPerFrame = 10;

            while (_accumulator >= _tickDelta && ticksExecuted < maxTicksPerFrame)
            {
                ExecuteTick();
                _accumulator -= _tickDelta;
                ticksExecuted++;
            }

            // Wenn zu viel akkumuliert wurde, clampe (normal bei Editor-Frame-Spikes)
            if (_accumulator > _tickDelta * 2)
            {
                _accumulator = _tickDelta;
            }

            return ticksExecuted;
        }

        /// <summary>
        /// Führt einen einzelnen Tick aus.
        /// </summary>
        private void ExecuteTick()
        {
            _currentTick++;
            OnTick?.Invoke(_currentTick, _tickDelta);
        }

        /// <summary>
        /// Setzt das System auf einen bestimmten Tick zurück.
        /// Nützlich für Rollback.
        /// </summary>
        /// <param name="tick">Der Ziel-Tick.</param>
        public void SetTick(int tick)
        {
            _currentTick = tick;
        }

        /// <summary>
        /// Setzt das System komplett zurück.
        /// </summary>
        public void Reset()
        {
            _currentTick = 0;
            _accumulator = 0f;
            _isPaused = false;
        }

        /// <summary>
        /// Berechnet die Zeit für einen bestimmten Tick.
        /// </summary>
        /// <param name="tick">Der Tick.</param>
        /// <returns>Zeit in Sekunden seit Start.</returns>
        public float TickToTime(int tick)
        {
            return tick * _tickDelta;
        }

        /// <summary>
        /// Berechnet den Tick für eine bestimmte Zeit.
        /// </summary>
        /// <param name="time">Zeit in Sekunden.</param>
        /// <returns>Der ungefähre Tick.</returns>
        public int TimeToTick(float time)
        {
            return Mathf.FloorToInt(time / _tickDelta);
        }

        /// <summary>
        /// Berechnet die Differenz zwischen zwei Ticks in Sekunden.
        /// </summary>
        public float TickDifference(int fromTick, int toTick)
        {
            return (toTick - fromTick) * _tickDelta;
        }
    }

    /// <summary>
    /// Konfiguration für das Tick-System.
    /// </summary>
    [Serializable]
    public struct TickConfig
    {
        [Tooltip("Ticks pro Sekunde (Standard: 60)")]
        [Range(20, 128)]
        public float TickRate;

        [Tooltip("Maximale Ticks pro Frame")]
        [Range(1, 20)]
        public int MaxTicksPerFrame;

        /// <summary>
        /// Standard-Konfiguration (60 Hz).
        /// </summary>
        public static TickConfig Default => new TickConfig
        {
            TickRate = TickSystem.DefaultTickRate,
            MaxTicksPerFrame = 10
        };
    }
}
