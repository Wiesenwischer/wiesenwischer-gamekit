namespace Wiesenwischer.GameKit.CharacterController.Core.StateMachine.States
{
    /// <summary>
    /// State für normales Laufen.
    /// Standard-Bewegungsgeschwindigkeit.
    /// </summary>
    public class PlayerRunningState : PlayerMovingState
    {
        public override string StateName => "Running";

        public PlayerRunningState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
        {
        }

        protected override void OnEnter()
        {
            base.OnEnter();
            // RunSpeed / WalkSpeed als Modifier
            ReusableData.MovementSpeedModifier = Config.RunSpeed / Config.WalkSpeed;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            // Transition zu Walking wenn Walk-Modus aktiviert
            if (ReusableData.ShouldWalk)
            {
                ChangeState(stateMachine.WalkingState);
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
            return stateMachine.MediumStoppingState;
        }
    }
}
