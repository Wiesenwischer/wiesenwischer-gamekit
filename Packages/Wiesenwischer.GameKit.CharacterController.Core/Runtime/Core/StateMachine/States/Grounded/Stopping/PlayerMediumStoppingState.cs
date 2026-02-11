using Wiesenwischer.GameKit.CharacterController.Core.Animation;

namespace Wiesenwischer.GameKit.CharacterController.Core.StateMachine.States
{
    /// <summary>
    /// Stopping-State nach Running.
    /// Mittlere Deceleration, Run-Stopp-Animation.
    /// </summary>
    public class PlayerMediumStoppingState : PlayerStoppingState
    {
        public override string StateName => "MediumStopping";

        public PlayerMediumStoppingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
        {
        }

        protected override float GetDeceleration() => Config.MediumStopDeceleration;
        protected override CharacterAnimationState GetAnimationState() => CharacterAnimationState.MediumStop;
    }
}
