using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Core.StateMachine.States
{
    /// <summary>
    /// Basis-State für alle Airborne-States.
    /// Gemeinsame Logik für Jumping, Falling, Landing.
    /// </summary>
    public class PlayerAirborneState : PlayerMovementState
    {
        public override string StateName => "Airborne";

        public PlayerAirborneState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
        {
        }

        protected override void OnEnter()
        {
            // Reset jump flags
            ReusableData.JumpWasReleased = false;

            // Step Detection deaktivieren für Airborne States
            ReusableData.StepDetectionEnabled = false;
        }

        protected override void OnUpdate()
        {
            // Air Control - reduzierter Movement-Modifier in der Luft
            ReusableData.MovementSpeedModifier = Config.AirControl;
        }

        protected override void OnPhysicsUpdate(float deltaTime)
        {
            // Gravity anwenden - State Machine ist Owner der Vertical Velocity
            ReusableData.VerticalVelocity -= Config.Gravity * deltaTime;

            // Fallgeschwindigkeit begrenzen
            if (ReusableData.VerticalVelocity < -Config.MaxFallSpeed)
            {
                ReusableData.VerticalVelocity = -Config.MaxFallSpeed;
            }
        }
    }
}
