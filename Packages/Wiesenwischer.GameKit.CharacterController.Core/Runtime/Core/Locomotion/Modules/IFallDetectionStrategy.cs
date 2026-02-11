using Wiesenwischer.GameKit.CharacterController.Core.Motor;

namespace Wiesenwischer.GameKit.CharacterController.Core.Locomotion.Modules
{
    /// <summary>
    /// Strategy-Interface für Fall Detection.
    /// Wird einmal pro Frame in CharacterLocomotion.PostGroundingUpdate() evaluiert.
    /// States lesen nur das gecachte Ergebnis über IsOverEdge.
    /// </summary>
    public interface IFallDetectionStrategy
    {
        /// <summary>
        /// Evaluiert ob der Character über einer Kante steht und fallen sollte.
        /// Wird einmal pro Frame nach dem Motor's Ground Probing aufgerufen.
        /// </summary>
        void Evaluate(CharacterMotor motor);

        /// <summary>
        /// Ob der Character über einer Kante steht und fallen sollte.
        /// Verwendet für: Fall Detection im GroundedState (+ CoyoteTime).
        /// </summary>
        bool IsOverEdge { get; }
    }
}
