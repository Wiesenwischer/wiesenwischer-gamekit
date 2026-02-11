using Wiesenwischer.GameKit.CharacterController.Core.Animation;

namespace Wiesenwischer.GameKit.CharacterController.Core.StateMachine.States
{
    /// <summary>
    /// Stopping-State nach Walking.
    /// Kurze Deceleration, leichte Stopp-Animation.
    /// </summary>
    public class PlayerLightStoppingState : PlayerStoppingState
    {
        public override string StateName => "LightStopping";

        public PlayerLightStoppingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
        {
        }

        protected override float GetDeceleration() => Config.LightStopDeceleration;
        protected override CharacterAnimationState GetAnimationState() => CharacterAnimationState.LightStop;
    }
}
