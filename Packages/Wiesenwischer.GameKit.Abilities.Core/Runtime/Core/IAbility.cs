namespace Wiesenwischer.GameKit.Abilities.Core
{
    /// <summary>
    /// Core interface for all abilities in the GameKit system.
    /// Abilities operate on the Ability Layer (orthogonal to the Movement State Machine).
    /// The AbilitySystem manages the lifecycle of registered abilities.
    /// </summary>
    public interface IAbility
    {
        /// <summary>Unique identifier for this ability instance.</summary>
        string Id { get; }

        /// <summary>Current lifecycle state of this ability.</summary>
        AbilityState State { get; }

        /// <summary>
        /// Priority for interruption logic. Higher priority abilities can
        /// interrupt lower priority ones. See AbilityPriority for defaults.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Whether the ability can be activated in the current context.
        /// Called by AbilitySystem before activation. Checks conditions like
        /// grounding, resources, cooldown, gameplay tags etc.
        /// </summary>
        bool CanActivate(AbilityContext context);

        /// <summary>
        /// Activate the ability. Called by AbilitySystem after CanActivate returns true.
        /// Should trigger animations, apply effects, etc.
        /// </summary>
        void Activate(AbilityContext context);

        /// <summary>
        /// Called every frame while the ability is active.
        /// Used for continuous effects, timing windows, combo input etc.
        /// </summary>
        void Tick(AbilityContext context, float deltaTime);

        /// <summary>
        /// Deactivate the ability. Called when:
        /// - The ability completes naturally
        /// - A higher priority ability interrupts
        /// - The ability is cancelled externally
        /// </summary>
        void Deactivate(AbilityContext context);
    }
}
