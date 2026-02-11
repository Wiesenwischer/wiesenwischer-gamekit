using Wiesenwischer.GameKit.CharacterController.Core.Motor;

namespace Wiesenwischer.GameKit.CharacterController.Core.Locomotion.Modules
{
    /// <summary>
    /// Strategy-Interface für Ground Detection.
    /// Wird einmal pro Frame in CharacterLocomotion.PostGroundingUpdate() evaluiert.
    /// States und Locomotion lesen nur die gecachten Ergebnisse.
    /// </summary>
    public interface IGroundDetectionStrategy
    {
        /// <summary>
        /// Evaluiert den aktuellen Ground-Status.
        /// Wird einmal pro Frame nach dem Motor's Ground Probing aufgerufen.
        /// </summary>
        void Evaluate(CharacterMotor motor);

        /// <summary>
        /// Ob der Character Bodenkontakt hat.
        /// Verwendet für: Landing Detection, Gravity, Velocity-Berechnung.
        /// </summary>
        bool IsGrounded { get; }
    }
}
