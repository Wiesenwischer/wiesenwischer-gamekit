namespace Wiesenwischer.GameKit.Abilities.Core
{
    /// <summary>
    /// Default priority values for ability categories.
    /// Higher value = higher priority = can interrupt lower priorities.
    /// Custom abilities can use any int value.
    /// </summary>
    public static class AbilityPriority
    {
        public const int Interaction = 1;    // Interact, Loot, Talk
        public const int Utility = 5;        // Buffs, Heals
        public const int Attack = 10;        // Melee, Ranged, Spells
        public const int Dodge = 20;         // Dodge roll, Evasion
        public const int Ultimate = 30;      // Ultimate abilities
        public const int ForcedStatus = 100; // Stun, Knockback (system-imposed)
    }
}
