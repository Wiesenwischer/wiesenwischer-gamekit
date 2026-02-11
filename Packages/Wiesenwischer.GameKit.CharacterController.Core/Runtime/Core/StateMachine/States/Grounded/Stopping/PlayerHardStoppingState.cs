using Wiesenwischer.GameKit.CharacterController.Core.Animation;

namespace Wiesenwischer.GameKit.CharacterController.Core.StateMachine.States
{
    /// <summary>
    /// Stopping-State nach Sprinting.
    /// Längere Deceleration, Sprint-Stopp-Animation.
    /// Wie in Genshin: HardStop kann nur zu Run/Sprint unterbrochen werden (kein Walk).
    /// </summary>
    public class PlayerHardStoppingState : PlayerStoppingState
    {
        public override string StateName => "HardStopping";

        public PlayerHardStoppingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
        {
        }

        protected override float GetDeceleration() => Config.HardStopDeceleration;
        protected override CharacterAnimationState GetAnimationState() => CharacterAnimationState.HardStop;

        protected override void TransitionToMovement()
        {
            // HardStop → nur Run oder Sprint (kein Walk, wie Genshin)
            if (ReusableData.SprintHeld)
            {
                ChangeState(stateMachine.SprintingState);
            }
            else
            {
                ChangeState(stateMachine.RunningState);
            }
        }
    }
}
