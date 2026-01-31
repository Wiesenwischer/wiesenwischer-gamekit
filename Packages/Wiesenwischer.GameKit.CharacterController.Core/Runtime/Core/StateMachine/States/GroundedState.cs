using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Core.StateMachine.States
{
    /// <summary>
    /// State wenn der Character auf dem Boden steht.
    /// Handhabt Bewegung und Sprung-Initiierung.
    /// </summary>
    public class GroundedState : BaseCharacterState
    {
        public const string Name = "Grounded";

        private readonly JumpingState _jumpingState;
        private readonly FallingState _fallingState;

        // Coyote Time Tracking
        private float _timeSinceGrounded;
        private bool _wasGrounded;

        public override string StateName => Name;

        /// <summary>
        /// Erstellt einen neuen GroundedState.
        /// </summary>
        /// <param name="jumpingState">Referenz zum JumpingState für Transitions.</param>
        /// <param name="fallingState">Referenz zum FallingState für Transitions.</param>
        public GroundedState(JumpingState jumpingState, FallingState fallingState)
        {
            _jumpingState = jumpingState;
            _fallingState = fallingState;
        }

        protected override void OnEnter(IStateMachineContext context)
        {
            // Reset vertical velocity when landing
            context.VerticalVelocity = 0f;
            _timeSinceGrounded = 0f;
            _wasGrounded = true;
        }

        protected override void OnUpdate(IStateMachineContext context, float deltaTime)
        {
            // Track time since last grounded (for Coyote Time)
            if (context.IsGrounded)
            {
                _timeSinceGrounded = 0f;
                _wasGrounded = true;
            }
            else
            {
                _timeSinceGrounded += deltaTime;
            }

            // Apply slight downward force to keep grounded
            if (context.IsGrounded && context.VerticalVelocity <= 0)
            {
                context.VerticalVelocity = -2f;
            }
        }

        public override ICharacterState EvaluateTransitions(IStateMachineContext context)
        {
            // Priority 1: Jump wenn gedrückt und Coyote Time noch gültig
            if (context.JumpPressed && CanJump(context))
            {
                return _jumpingState;
            }

            // Priority 2: Falling wenn nicht mehr grounded und Coyote Time abgelaufen
            if (!context.IsGrounded && _timeSinceGrounded > context.Config.CoyoteTime)
            {
                return _fallingState;
            }

            // Kein Übergang
            return null;
        }

        /// <summary>
        /// Prüft ob der Character springen kann.
        /// Berücksichtigt Coyote Time.
        /// </summary>
        private bool CanJump(IStateMachineContext context)
        {
            // Kann springen wenn grounded ODER innerhalb Coyote Time
            return context.IsGrounded || _timeSinceGrounded <= context.Config.CoyoteTime;
        }
    }
}
