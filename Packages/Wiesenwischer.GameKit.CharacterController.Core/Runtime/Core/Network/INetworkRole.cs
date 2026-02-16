namespace Wiesenwischer.GameKit.CharacterController.Core
{
    /// <summary>
    /// Abstraktion der Netzwerk-Rolle eines Objekts.
    /// Implementiert vom Network-Package (z.B. FishNet).
    /// Im Offline-Modus liefert eine Default-Implementierung alles als "lokal".
    /// </summary>
    public interface INetworkRole
    {
        /// <summary>Ist dies der lokale Spieler (hat Input-Authority)?</summary>
        bool IsOwner { get; }

        /// <summary>Läuft dieser Code auf dem Server?</summary>
        bool IsServer { get; }

        /// <summary>Läuft dieser Code auf einem Client?</summary>
        bool IsClient { get; }

        /// <summary>Ist das Netzwerk aktiv (Multiplayer-Modus)?</summary>
        bool IsNetworkActive { get; }
    }
}
