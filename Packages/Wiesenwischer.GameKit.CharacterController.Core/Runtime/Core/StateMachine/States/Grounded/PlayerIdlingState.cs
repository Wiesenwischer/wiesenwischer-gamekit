using Wiesenwischer.GameKit.CharacterController.Core.Animation;

namespace Wiesenwischer.GameKit.CharacterController.Core.StateMachine.States
{
    /// <summary>
    /// State wenn der Character stillsteht.
    /// Wartet auf Movement-Input um zu Walking/Running zu wechseln.
    /// </summary>
    public class PlayerIdlingState : PlayerGroundedState
    {
        public override string StateName => "Idling";

        public PlayerIdlingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
        {
        }

        protected override void OnEnter()
        {
            base.OnEnter();
            Player.AnimationController?.PlayState(CharacterAnimationState.Locomotion);
            ReusableData.MovementSpeedModifier = 0f;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            // Transition zu Moving wenn Input vorhanden
            if (HasMovementInput())
            {
                OnMove();
            }
        }

        /// <summary>
        /// Wird aufgerufen wenn Movement-Input erkannt wird.
        /// Wechselt je nach Sprint-Status zu Walking oder Running.
        /// </summary>
        private void OnMove()
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
    }
}
