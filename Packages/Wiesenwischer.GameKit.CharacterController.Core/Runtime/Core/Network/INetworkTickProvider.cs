namespace Wiesenwischer.GameKit.CharacterController.Core
{
    /// <summary>
    /// Synchronisiert Tick-Zähler zwischen Server und Client.
    /// Im Offline-Modus verwendet der lokale TickSystem den eigenen Tick.
    /// Im Netzwerk liefert der Server den autoritativen Tick.
    /// </summary>
    public interface INetworkTickProvider
    {
        /// <summary>Der aktuelle Server-Tick (oder lokaler Tick im Offline-Modus).</summary>
        int ServerTick { get; }

        /// <summary>Geschätzte Round-Trip-Time in Sekunden.</summary>
        float EstimatedRtt { get; }

        /// <summary>Differenz zwischen lokalem und Server-Tick.</summary>
        int TickOffset { get; }
    }
}
