namespace Wiesenwischer.GameKit.Abilities.Core
{
    /// <summary>
    /// Optional interface for abilities that play animations on the Ability Layer.
    /// Implement this alongside IAbility to enable animation integration.
    /// </summary>
    public interface IAbilityAnimationHandler
    {
        /// <summary>
        /// Name des Animator-States auf Layer 1.
        /// Wird per Animator.CrossFade() getriggert.
        /// </summary>
        string AnimationStateName { get; }

        /// <summary>CrossFade-Dauer für den Übergang zur Ability-Animation.</summary>
        float TransitionDuration { get; }

        /// <summary>
        /// Ob die Animation gerade abgeschlossen ist.
        /// Wird vom AbilitySystem abgefragt für auto-deactivate bei Animation-Ende.
        /// </summary>
        bool IsAnimationComplete(AbilityContext context);
    }
}
