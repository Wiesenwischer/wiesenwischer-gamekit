namespace Wiesenwischer.GameKit.Abilities.Core
{
    /// <summary>
    /// Lifecycle states of an ability instance.
    /// </summary>
    public enum AbilityState
    {
        /// <summary>Ready to be activated.</summary>
        Ready,

        /// <summary>Currently executing (between Activate and Deactivate).</summary>
        Active,

        /// <summary>On cooldown, cannot be activated.</summary>
        Cooldown,

        /// <summary>Disabled (e.g. by a status effect like Silence).</summary>
        Disabled
    }
}
