using Wiesenwischer.GameKit.CharacterController.Core.Motor;

namespace Wiesenwischer.GameKit.CharacterController.Core.Locomotion.Modules
{
    /// <summary>
    /// Motor-basierte Ground Detection Strategy.
    /// Liest den GroundingStatus direkt vom Motor (IsStableOnGround).
    /// </summary>
    public class MotorGroundDetectionStrategy : IGroundDetectionStrategy
    {
        public bool IsGrounded { get; private set; }

        public void Evaluate(CharacterMotor motor)
        {
            IsGrounded = motor.GroundingStatus.IsStableOnGround;
        }
    }
}
