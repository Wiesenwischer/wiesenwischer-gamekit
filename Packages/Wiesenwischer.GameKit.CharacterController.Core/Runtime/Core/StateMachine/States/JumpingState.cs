using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Core.StateMachine.States
{
    /// <summary>
    /// State während der Character nach oben springt.
    /// Handhabt Jump-Velocity und Transition zu Falling.
    /// </summary>
    public class JumpingState : AirborneState
    {
        public const string Name = "Jumping";

        private FallingState _fallingState;
        private GroundedState _groundedState;

        // Jump Variables
        private bool _jumpButtonReleased;

        public override string StateName => Name;

        /// <summary>
        /// Setzt die Referenzen zu anderen States.
        /// Notwendig wegen zirkulärer Abhängigkeiten.
        /// </summary>
        public void SetStateReferences(FallingState fallingState, GroundedState groundedState)
        {
            _fallingState = fallingState;
            _groundedState = groundedState;
        }

        protected override void OnEnter(IStateMachineContext context)
        {
            // Berechne und setze Jump Velocity
            context.VerticalVelocity = CalculateJumpVelocity(context);
            _jumpButtonReleased = false;
        }

        protected override void OnUpdate(IStateMachineContext context, float deltaTime)
        {
            // Prüfe ob Jump-Button losgelassen wurde
            if (!context.JumpPressed && !_jumpButtonReleased)
            {
                _jumpButtonReleased = true;

                // Variable Jump: Reduziere Velocity wenn Button früh losgelassen
                if (context.VerticalVelocity > 0)
                {
                    context.VerticalVelocity *= 0.5f;
                }
            }

            // Gravity wird vom Movement System angewendet
        }

        public override ICharacterState EvaluateTransitions(IStateMachineContext context)
        {
            // Priority 1: Wenn wir fallen (negative Velocity), wechsle zu Falling
            if (context.VerticalVelocity <= 0)
            {
                return _fallingState;
            }

            // Priority 2: Wenn wir grounded sind (z.B. Decke getroffen), wechsle zu Grounded
            if (context.IsGrounded && StateTime > 0.1f) // Kleine Verzögerung um sofortiges Landen zu verhindern
            {
                return _groundedState;
            }

            // Kein Übergang
            return null;
        }

        /// <summary>
        /// Berechnet die initiale Jump-Velocity basierend auf der gewünschten Höhe.
        /// Verwendet die Formel: v = sqrt(2 * g * h)
        /// </summary>
        private float CalculateJumpVelocity(IStateMachineContext context)
        {
            // v = sqrt(2 * g * h)
            // g ist positiv in Config, Gravity geht nach unten
            float gravity = context.Config.Gravity;
            float jumpHeight = context.Config.JumpHeight;

            return Mathf.Sqrt(2f * gravity * jumpHeight);
        }
    }
}
