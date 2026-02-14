namespace Wiesenwischer.GameKit.CharacterController.Core.Locomotion
{
    /// <summary>
    /// Bestimmt wie der Landing Roll ausgelöst wird.
    /// </summary>
    public enum RollTriggerMode
    {
        /// <summary>Roll bei Movement-Input während Landung (Genshin-Style).</summary>
        MovementInput,

        /// <summary>Roll nur bei gehaltener Dodge/Roll-Taste.</summary>
        ButtonPress
    }
}
