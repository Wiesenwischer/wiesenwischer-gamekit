using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Locomotion.Modules;

namespace Wiesenwischer.GameKit.CharacterController.Core.StateMachine.States
{
    /// <summary>
    /// State während der Character nach oben springt.
    /// Nutzt JumpModule für Berechnungen.
    /// </summary>
    public class PlayerJumpingState : PlayerAirborneState
    {
        private readonly JumpModule _jumpModule = new JumpModule();
        private const float CeilingCheckDistance = 0.1f;

        public override string StateName => "Jumping";

        public PlayerJumpingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
        {
        }

        protected override void OnEnter()
        {
            base.OnEnter();

            // Intent: Jump anmelden - CharacterLocomotion wendet den Impulse an
            ReusableData.JumpRequested = true;
            ReusableData.JumpButtonReleased = false;
        }

        protected override void OnHandleInput()
        {
            // Variable Jump: Nur wenn aktiviert
            if (!Config.UseVariableJump) return;

            // Intent: Jump Cut anmelden wenn Button während Aufstieg losgelassen
            if (!ReusableData.JumpHeld && !ReusableData.JumpButtonReleased
                && ReusableData.VerticalVelocity > 0)
            {
                ReusableData.JumpCutRequested = true;
                ReusableData.JumpButtonReleased = true;
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            // Transition zu Falling wenn wir anfangen zu fallen
            if (_jumpModule.IsFalling(ReusableData.VerticalVelocity))
            {
                ChangeState(stateMachine.FallingState);
                return;
            }

            // Ceiling Detection via JumpModule (Sensing bleibt im State)
            var motor = stateMachine.Player.CharacterMotor;
            if (stateTime > 0.05f && _jumpModule.CheckCeiling(motor, CeilingCheckDistance, Config.GroundLayers))
            {
                // Intent: Vertical Reset anmelden - CharacterLocomotion setzt _verticalVelocity = 0
                ReusableData.ResetVerticalRequested = true;
                ChangeState(stateMachine.FallingState);
            }
        }
    }
}
