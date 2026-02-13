using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Animation;

namespace Wiesenwischer.GameKit.CharacterController.Core.StateMachine.States
{
    /// <summary>
    /// State für das Rutschen auf steilen Hängen (SlopeAngle > MaxSlopeAngle).
    /// Hat Bodenkontakt, ist aber nicht "stabil geerdet" — eigenständiger State
    /// (nicht unter Grounded oder Airborne).
    /// </summary>
    public class PlayerSlidingState : PlayerMovementState
    {
        private float _slideStartTime;

        public override string StateName => "Sliding";

        public PlayerSlidingState(PlayerMovementStateMachine stateMachine)
            : base(stateMachine) { }

        protected override void OnEnter()
        {
            _slideStartTime = Time.time;

            // Slide-Intent aktivieren
            Player.Locomotion.SetSliding(true);

            // Step Detection deaktivieren (keine Steps auf steilen Slopes)
            ReusableData.StepDetectionEnabled = false;

            Player.AnimationController?.PlayState(CharacterAnimationState.Slide);

            // Rotation in Rutsch-Richtung
            UpdateRotationToSlideDirection();
        }

        protected override void OnExit()
        {
            // Slide-Intent deaktivieren
            Player.Locomotion.SetSliding(false);

            // Step Detection wieder aktivieren
            ReusableData.StepDetectionEnabled = true;
        }

        protected override void OnHandleInput()
        {
            // Track JumpWasReleased
            if (!ReusableData.JumpHeld)
            {
                ReusableData.JumpWasReleased = true;
            }

            // Jump aus Slide (optional, konfigurierbar)
            if (Config.CanJumpFromSlide && ReusableData.JumpPressed && ReusableData.JumpWasReleased)
            {
                ReusableData.JumpWasReleased = false;
                ChangeState(stateMachine.JumpingState);
                return;
            }
        }

        protected override void OnUpdate()
        {
            float timeInSlide = Time.time - _slideStartTime;

            // MinSlideTime einhalten (Flacker-Schutz)
            if (timeInSlide < Config.MinSlideTime)
                return;

            CheckExitConditions();
        }

        protected override void OnPhysicsUpdate(float deltaTime)
        {
            // Rotation Richtung Hangabwärts (smooth)
            UpdateRotationToSlideDirection();
        }

        private void CheckExitConditions()
        {
            // 1. Boden verloren → Falling
            if (!Player.Locomotion.Motor.GroundingStatus.FoundAnyGround)
            {
                ChangeState(stateMachine.FallingState);
                return;
            }

            // 2. Slope wieder begehbar (mit Hysterese)
            float exitAngle = Config.MaxSlopeAngle - Config.SlideExitHysteresis;
            float currentSlopeAngle = Player.Locomotion.GroundInfo.SlopeAngle;
            if (currentSlopeAngle < exitAngle)
            {
                TransitionToGroundedState();
                return;
            }
        }

        private void TransitionToGroundedState()
        {
            if (ReusableData.MoveInput.sqrMagnitude > 0.01f)
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

        private void UpdateRotationToSlideDirection()
        {
            var groundNormal = Player.Locomotion.GroundInfo.Normal;
            Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;

            if (slideDirection.sqrMagnitude < 0.001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(slideDirection, Vector3.up);
            Player.transform.rotation = Quaternion.RotateTowards(
                Player.transform.rotation,
                targetRotation,
                Config.RotationSpeed * Time.deltaTime);
        }
    }
}
