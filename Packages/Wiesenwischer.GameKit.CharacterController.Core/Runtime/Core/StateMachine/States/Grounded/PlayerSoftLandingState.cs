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

            // Animation-Trigger (Soft Landing)
            Player.AnimationController?.TriggerLanding(false);

            // Momentum bleibt erhalten: AirborneState konserviert den SpeedModifier
            // aus dem Grounded State (z.B. 2.0 von RunningState).
            // Kein Override nötig – OnUpdate() transitioniert sofort zum passenden State.
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
