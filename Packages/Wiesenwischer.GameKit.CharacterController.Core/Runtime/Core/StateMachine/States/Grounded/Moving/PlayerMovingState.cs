using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Animation;

namespace Wiesenwischer.GameKit.CharacterController.Core.StateMachine.States
{
    /// <summary>
    /// Basis-State für alle Moving-States (Walking, Running, Sprinting).
    /// Handhabt gemeinsame Bewegungslogik.
    /// </summary>
    public class PlayerMovingState : PlayerGroundedState
    {
        public override string StateName => "Moving";

        public PlayerMovingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
        {
        }

        protected override void OnEnter()
        {
            base.OnEnter();
            Player.AnimationController?.PlayState(CharacterAnimationState.Locomotion);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            // Transition zu Stopping-State wenn kein Input
            if (!HasMovementInput())
            {
                ChangeState(GetStoppingState());
                return;
            }
        }

        /// <summary>
        /// Gibt den passenden Stopping-State für diesen Movement-Tier zurück.
        /// Wird von Subklassen überschrieben (Walk→Light, Run→Medium, Sprint→Hard).
        /// </summary>
        protected virtual IPlayerMovementState GetStoppingState()
        {
            return stateMachine.MediumStoppingState;
        }
    }
}
