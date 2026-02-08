namespace Wiesenwischer.GameKit.CharacterController.Core.StateMachine.States
{
    /// <summary>
    /// State für Sprinten.
    /// Schnellste Bewegungsgeschwindigkeit, aktiviert durch Sprint-Button.
    /// </summary>
    public class PlayerSprintingState : PlayerMovingState
    {
        public override string StateName => "Sprinting";

        public PlayerSprintingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
        {
        }

        protected override void OnEnter()
        {
            base.OnEnter();
            // Sprint ist schneller als Run - Multiplikator aus Config
            ReusableData.MovementSpeedModifier = (Config.RunSpeed / Config.WalkSpeed) * Config.SprintMultiplier;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            // Transition zu Running wenn Sprint losgelassen
            if (!ReusableData.SprintHeld)
            {
                if (ReusableData.ShouldWalk)
                {
                    ChangeState(stateMachine.WalkingState);
                }
                else
                {
                    ChangeState(stateMachine.RunningState);
                }
                return;
            }
        }
    }
}
