using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Animation;

namespace Wiesenwischer.GameKit.CharacterController.Core.StateMachine.States
{
    /// <summary>
    /// State nach einer harten Landung (hoher Fall).
    /// Stoppt Bewegung komplett und wartet auf Animations-Ende oder AllowExit Event.
    /// Fallback auf Timer wenn kein AnimationController vorhanden.
    /// </summary>
    public class PlayerHardLandingState : PlayerGroundedState
    {
        public override string StateName => "HardLanding";

        private bool _jumpBuffered;
        private bool _useAnimationBasedRecovery;
        private float _fallbackTimer;

        public PlayerHardLandingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
        {
        }

        protected override void OnEnter()
        {
            base.OnEnter();

            _jumpBuffered = false;
            _useAnimationBasedRecovery = Player.AnimationController != null;

            if (_useAnimationBasedRecovery)
            {
                Player.AnimationController.PlayState(CharacterAnimationState.HardLand);
            }
            else
            {
                // Fallback: Timer basierend auf Aufprallgeschwindigkeit
                float landingSpeed = Mathf.Abs(ReusableData.LandingVelocity);
                if (landingSpeed < Config.HardLandingThreshold)
                {
                    float t = Mathf.InverseLerp(Config.SoftLandingThreshold, Config.HardLandingThreshold, landingSpeed);
                    _fallbackTimer = Mathf.Lerp(Config.SoftLandingDuration, Config.HardLandingDuration, t);
                }
                else
                {
                    _fallbackTimer = Config.HardLandingDuration;
                }
            }

            // Horizontales Momentum sofort stoppen bei Hard Landing (kein Gleiten nach Aufprall)
            ReusableData.MovementSpeedModifier = 0f;
            Player.Locomotion?.StopMovement();
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

            bool recoveryComplete;

            if (_useAnimationBasedRecovery)
            {
                // CanExitAnimation: Designer-gesetzter AllowExit Event auf dem Clip
                // IsAnimationComplete: Animation vollständig abgespielt
                var anim = Player.AnimationController;
                recoveryComplete = anim.CanExitAnimation || anim.IsAnimationComplete();
            }
            else
            {
                // Fallback: Timer
                _fallbackTimer -= Time.deltaTime;
                recoveryComplete = _fallbackTimer <= 0f;
            }

            if (!recoveryComplete) return;

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

        protected override void OnExit()
        {
            base.OnExit();
            ReusableData.MovementSpeedModifier = 1f;
        }
    }
}
