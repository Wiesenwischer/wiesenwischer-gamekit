namespace Wiesenwischer.GameKit.CharacterController.Core
{
    /// <summary>
    /// Default-Implementierung für Offline/Singleplayer.
    /// Gibt immer Owner=true, Server=false, NetworkActive=false zurück.
    /// </summary>
    public sealed class OfflineNetworkRole : INetworkRole
    {
        public static readonly OfflineNetworkRole Instance = new();

        public bool IsOwner => true;
        public bool IsServer => false;
        public bool IsClient => true;
        public bool IsNetworkActive => false;
    }
}
