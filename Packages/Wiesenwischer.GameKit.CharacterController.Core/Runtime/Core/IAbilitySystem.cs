namespace Wiesenwischer.GameKit.CharacterController.Core
{
    /// <summary>
    /// Interface for the ability system, allowing PlayerController to
    /// tick abilities without a direct dependency on Abilities.Core.
    /// </summary>
    public interface IAbilitySystem
    {
        /// <summary>
        /// Updates cooldowns and ticks active abilities.
        /// Called by PlayerController after State Machine update.
        /// </summary>
        void Tick(float deltaTime);

        /// <summary>Whether any ability is currently active.</summary>
        bool HasActiveAbility { get; }
    }
}
