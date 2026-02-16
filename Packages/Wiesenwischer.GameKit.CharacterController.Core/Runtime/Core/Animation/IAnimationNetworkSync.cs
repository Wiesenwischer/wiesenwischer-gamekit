namespace Wiesenwischer.GameKit.CharacterController.Core.Animation
{
    /// <summary>
    /// Interface für Netzwerk-Synchronisation von Animation-State-Wechseln.
    /// Implementiert vom Network-Package (NetworkAnimationSync).
    /// AnimatorParameterBridge ruft dies auf, ohne FishNet zu kennen.
    /// </summary>
    public interface IAnimationNetworkSync
    {
        void OnLocalStateChanged(CharacterAnimationState state);
    }
}
