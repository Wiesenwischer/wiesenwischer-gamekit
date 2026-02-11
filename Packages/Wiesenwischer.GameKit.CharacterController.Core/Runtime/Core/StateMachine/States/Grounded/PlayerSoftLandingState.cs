using Wiesenwischer.GameKit.CharacterController.Core.Animation;

namespace Wiesenwischer.GameKit.CharacterController.Core.StateMachine.States
{
    /// <summary>
    /// State nach einer weichen/normalen Landung (z.B. nach normalem Sprung).
    /// Momentum bleibt erhalten, sofortige Transition zum passenden Movement State.
    /// </summary>
    public class PlayerSoftLandingState : PlayerGroundedState
    {
        public override string StateName => "SoftLanding";

        public PlayerSoftLandingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
        {
        }

        protected override void OnEnter()
        {
            base.OnEnter();

            Player.AnimationController?.PlayState(CharacterAnimationState.SoftLand);

            // Momentum bleibt erhalten: AirborneState konserviert den SpeedModifier
            // aus dem Grounded State (z.B. 2.0 von RunningState).
            // OnUpdate() transitioniert sofort zum passenden Movement State.
            // Die SoftLand-Animation blendet via CrossFade sanft in Locomotion über.
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            // Sofort zum passenden Movement State wechseln
            if (HasMovementInput())
            {
                if (ReusableData.SprintHeld)
                {
                    ChangeState(stateMachine.SprintingState);
                }
                else if (ReusableData.ShouldWalk)
                {
                    ChangeState(stateMachine.WalkingState);
                }
                else
                {
                    ChangeState(stateMachine.RunningState);
                }
            }
            else
            {
                ChangeState(stateMachine.IdlingState);
            }
        }
    }
}
