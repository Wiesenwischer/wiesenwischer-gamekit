using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Core.StateMachine.States
{
    /// <summary>
    /// State während der Character fällt.
    /// Handhabt Gravity und Landing-Detection.
    /// </summary>
    public class FallingState : AirborneState
    {
        public const string Name = "Falling";

        private GroundedState _groundedState;
        private JumpingState _jumpingState;

        // Jump Buffer
        private float _jumpBufferTimer;
        private bool _jumpBuffered;

        public override string StateName => Name;

        /// <summary>
        /// Setzt die Referenzen zu anderen States.
        /// Notwendig wegen zirkulärer Abhängigkeiten.
        /// </summary>
        public void SetStateReferences(GroundedState groundedState, JumpingState jumpingState)
        {
            _groundedState = groundedState;
            _jumpingState = jumpingState;
        }

        protected override void OnEnter(IStateMachineContext context)
        {
            _jumpBufferTimer = 0f;
            _jumpBuffered = false;
        }

        protected override void OnUpdate(IStateMachineContext context, float deltaTime)
        {
            // Jump Buffer: Speichere Jump-Input wenn gedrückt während des Fallens
            if (context.JumpPressed)
            {
                _jumpBuffered = true;
                _jumpBufferTimer = context.Config.JumpBufferTime;
            }

            // Reduziere Jump Buffer Timer
            if (_jumpBuffered)
            {
                _jumpBufferTimer -= deltaTime;
                if (_jumpBufferTimer <= 0)
                {
                    _jumpBuffered = false;
                }
            }

            // Gravity wird vom Movement System angewendet
        }

        public override ICharacterState EvaluateTransitions(IStateMachineContext context)
        {
            // Priority 1: Wenn gelandet
            if (context.IsGrounded)
            {
                // Prüfe Jump Buffer - wenn gültig, springe sofort wieder
                if (_jumpBuffered && _jumpBufferTimer > 0)
                {
                    _jumpBuffered = false;
                    return _jumpingState;
                }

                return _groundedState;
            }

            // Kein Übergang
            return null;
        }

        /// <summary>
        /// Ob ein Jump gebuffert ist.
        /// </summary>
        public bool HasBufferedJump => _jumpBuffered && _jumpBufferTimer > 0;
    }
}
