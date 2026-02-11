namespace Wiesenwischer.GameKit.CharacterController.Core.StateMachine.States
{
    /// <summary>
    /// State für langsames Gehen.
    /// Aktiviert wenn ShouldWalk true ist.
    /// </summary>
    public class PlayerWalkingState : PlayerMovingState
    {
        public override string StateName => "Walking";

        public PlayerWalkingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
        {
        }

        protected override void OnEnter()
        {
            base.OnEnter();
            // WalkSpeed ist die Basis, also Modifier 1.0
            // (WalkSpeed / WalkSpeed = 1.0)
            ReusableData.MovementSpeedModifier = 1f;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            // Transition zu Running wenn nicht mehr Walk-Modus
            if (!ReusableData.ShouldWalk)
            {
                ChangeState(stateMachine.RunningState);
                return;
            }

            // Transition zu Sprinting wenn Sprint gedrückt
            if (ReusableData.SprintHeld)
            {
                ChangeState(stateMachine.SprintingState);
                return;
            }
        }

        protected override IPlayerMovementState GetStoppingState()
        {
            return stateMachine.LightStoppingState;
        }
    }
}
