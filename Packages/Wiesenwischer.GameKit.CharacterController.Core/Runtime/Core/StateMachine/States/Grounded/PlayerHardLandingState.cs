using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Core.StateMachine.States
{
    /// <summary>
    /// State nach einer harten Landung (hoher Fall).
    /// Stoppt Bewegung komplett und erzwingt eine Recovery-Zeit.
    /// </summary>
    public class PlayerHardLandingState : PlayerGroundedState
    {
        public override string StateName => "HardLanding";

        private float _landingRecoveryTimer;
        private bool _jumpBuffered;

        public PlayerHardLandingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
        {
        }

        protected override void OnEnter()
        {
            base.OnEnter();

            // Animation-Trigger (Hard Landing)
            Player.AnimationController?.TriggerLanding(true);

            _jumpBuffered = false;

            // Recovery-Zeit berechnen basierend auf Aufprallgeschwindigkeit
            float landingSpeed = Mathf.Abs(ReusableData.LandingVelocity);

            if (landingSpeed < Config.HardLandingThreshold)
            {
                // Zwischen soft und hard - interpoliere
                float t = Mathf.InverseLerp(Config.SoftLandingThreshold, Config.HardLandingThreshold, landingSpeed);
                _landingRecoveryTimer = Mathf.Lerp(Config.SoftLandingDuration, Config.HardLandingDuration, t);
            }
            else
            {
                // Maximale Recovery
                _landingRecoveryTimer = Config.HardLandingDuration;
            }

            // Intent: Keine Bewegung während Recovery
            // AccelerationModule bremst über Deceleration ab (kein direkter Velocity-Reset)
            ReusableData.MovementSpeedModifier = 0f;
        }

        protected override void OnHandleInput()
        {
            base.OnHandleInput();

            // Jump-Buffer: Speichere Jump-Input während Recovery
            if (ReusableData.JumpPressed)
            {
                _jumpBuffered = true;
            }
        }

        protected override void OnUpdate()
        {
            // NICHT base.OnUpdate() - Coyote Time Check überspringen

            _landingRecoveryTimer -= Time.deltaTime;

            if (_landingRecoveryTimer <= 0f)
            {
                // Gebufferter Jump?
                if (_jumpBuffered && ReusableData.JumpWasReleased)
                {
                    ReusableData.JumpPressed = true;
                    OnJump();
                    return;
                }

                // Zum passenden Movement State wechseln
                if (ReusableData.MoveInput.sqrMagnitude > 0.01f)
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
                else
                {
                    ChangeState(stateMachine.IdlingState);
                }
            }
        }

        protected override void OnExit()
        {
            base.OnExit();
            ReusableData.MovementSpeedModifier = 1f;
        }
    }
}
