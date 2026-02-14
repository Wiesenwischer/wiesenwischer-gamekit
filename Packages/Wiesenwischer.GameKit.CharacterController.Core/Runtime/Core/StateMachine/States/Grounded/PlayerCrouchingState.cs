using Wiesenwischer.GameKit.CharacterController.Core.Animation;

namespace Wiesenwischer.GameKit.CharacterController.Core.StateMachine.States
{
    /// <summary>
    /// State für Crouching. Handhabt sowohl Crouch-Idle als auch Crouch-Moving.
    /// Speed-Parameter steuert den Crouch Blend Tree (Idle + Walk).
    /// Toggle-basiert: C-Taste zum Ein/Ausschalten.
    /// </summary>
    public class PlayerCrouchingState : PlayerGroundedState
    {
        public override string StateName => "Crouching";

        public PlayerCrouchingState(PlayerMovementStateMachine stateMachine)
            : base(stateMachine) { }

        protected override void OnEnter()
        {
            base.OnEnter();

            ReusableData.IsCrouching = true;
            Player.Locomotion.SetCrouching(true);

            Player.AnimationController?.PlayState(CharacterAnimationState.Crouch);

            UpdateSpeedModifier();
        }

        protected override void OnHandleInput()
        {
            base.OnHandleInput();

            // Toggle: C-Taste nochmal → Aufstehen
            if (ReusableData.CrouchTogglePressed && Player.Locomotion.CanStandUp())
            {
                ExitCrouch();
                return;
            }

            // Sprint beendet Crouch (wenn konfiguriert)
            if (Config.CanSprintFromCrouch && ReusableData.SprintHeld
                && Player.Locomotion.CanStandUp())
            {
                ExitCrouchToState(stateMachine.SprintingState);
                return;
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            UpdateSpeedModifier();
        }

        protected override void OnExit()
        {
            base.OnExit();

            // Safety: Immer Crouch beenden wenn State verlassen wird
            // (z.B. bei Falling über Kante)
            if (ReusableData.IsCrouching)
            {
                ReusableData.IsCrouching = false;
                Player.Locomotion.SetCrouching(false);
            }
        }

        protected override void OnJump()
        {
            if (!Config.CanJumpFromCrouch) return;

            ReusableData.IsCrouching = false;
            Player.Locomotion.SetCrouching(false);
            base.OnJump();
        }

        private void UpdateSpeedModifier()
        {
            if (ReusableData.MoveInput.sqrMagnitude > 0.01f)
            {
                float crouchSpeedModifier = Config.CrouchSpeed / Config.WalkSpeed;
                ReusableData.MovementSpeedModifier = crouchSpeedModifier;
            }
            else
            {
                ReusableData.MovementSpeedModifier = 0f;
            }
        }

        private void ExitCrouch()
        {
            ReusableData.IsCrouching = false;
            Player.Locomotion.SetCrouching(false);

            if (HasMovementInput())
            {
                if (ReusableData.ShouldWalk)
                    ChangeState(stateMachine.WalkingState);
                else
                    ChangeState(stateMachine.RunningState);
            }
            else
            {
                ChangeState(stateMachine.IdlingState);
            }
        }

        private void ExitCrouchToState(IPlayerMovementState targetState)
        {
            ReusableData.IsCrouching = false;
            Player.Locomotion.SetCrouching(false);
            ChangeState(targetState);
        }
    }
}
