namespace Wiesenwischer.GameKit.CharacterController.Core
{
    /// <summary>
    /// Abstraktion fuer den Simulation-Treiber.
    /// Online: NetworkCharacterDriver (FishNet TickNetworkBehaviour)
    /// Offline: PlayerController.FixedUpdate()
    /// </summary>
    public interface ISimulationDriver
    {
        /// <summary>
        /// True wenn ein externer Driver die Simulation treibt.
        /// Wenn true, darf PlayerController NICHT selbst simulieren.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Delta-Zeit pro Tick (z.B. TimeManager.TickDelta).
        /// </summary>
        float TickDelta { get; }

        /// <summary>
        /// Aktueller Tick-Zaehler (z.B. TimeManager.Tick).
        /// </summary>
        uint CurrentTick { get; }
    }
}
