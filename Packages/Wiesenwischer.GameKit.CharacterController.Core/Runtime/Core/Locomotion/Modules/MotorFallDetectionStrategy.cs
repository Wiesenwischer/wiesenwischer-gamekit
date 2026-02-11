using Wiesenwischer.GameKit.CharacterController.Core.Motor;

namespace Wiesenwischer.GameKit.CharacterController.Core.Locomotion.Modules
{
    /// <summary>
    /// Motor-basierte Fall Detection Strategy.
    /// Nutzt SnappingPrevented und IsStableOnGround aus dem GroundingStatus.
    /// IsOverEdge = SnappingPrevented (an Kante, kein Snap mehr) ODER nicht stabil auf dem Boden.
    /// </summary>
    public class MotorFallDetectionStrategy : IFallDetectionStrategy
    {
        public bool IsOverEdge { get; private set; }

        public void Evaluate(CharacterMotor motor)
        {
            var grounding = motor.GroundingStatus;
            IsOverEdge = grounding.SnappingPrevented || !grounding.IsStableOnGround;
        }
    }
}
