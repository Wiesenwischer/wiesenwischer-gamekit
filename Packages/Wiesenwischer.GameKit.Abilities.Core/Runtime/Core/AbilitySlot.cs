namespace Wiesenwischer.GameKit.Abilities.Core
{
    /// <summary>
    /// Internal tracking wrapper for a registered ability.
    /// Manages cooldown timer and state transitions.
    /// </summary>
    internal class AbilitySlot
    {
        public IAbility Ability { get; }
        public AbilityDefinition Definition { get; }
        public float CooldownRemaining { get; set; }

        public bool IsOnCooldown => CooldownRemaining > 0f;
        public bool IsActive => Ability.State == AbilityState.Active;

        public AbilitySlot(IAbility ability, AbilityDefinition definition)
        {
            Ability = ability;
            Definition = definition;
        }
    }
}
