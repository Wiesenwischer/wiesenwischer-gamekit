using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Animation;
using Wiesenwischer.GameKit.CharacterController.Core.Locomotion;

namespace Wiesenwischer.GameKit.CharacterController.Core.StateMachine.States
{
    /// <summary>
    /// State während der Character fällt.
    /// Wenn grounded → SoftLanding oder HardLanding basierend auf Fallhöhe.
    /// </summary>
    public class PlayerFallingState : PlayerAirborneState
    {
        public override string StateName => "Falling";

        // === Warum _wasUngrounded? ===
        // Problem: FallingState kann betreten werden während IsGrounded noch true ist.
        // Das passiert z.B. wenn GroundedState.OnUpdate() den Übergang auslöst weil
        // CoyoteTime abgelaufen ist, aber die GroundDetectionStrategy noch Boden sieht.
        // Ohne diesen Guard würde der Character sofort "landen" ohne je gefallen zu sein.
        // Lösung: Mindestens 1 Frame lang IsGrounded=false abwarten vor Landing-Check.
        private bool _wasUngrounded;

        // === Warum _groundedFrameCount (Safety-Net)? ===
        // Edge-Case: FallingState wird betreten und IsGrounded bleibt DAUERHAFT true
        // (z.B. auf einer Rampe mit bestimmtem Winkel). _wasUngrounded wird nie true,
        // und der Character steckt fest. Nach GroundedFrameThreshold Frames wird
        // trotzdem HandleLanding() aufgerufen, damit der Character nicht ewig fällt.
        private int _groundedFrameCount;
        private const int GroundedFrameThreshold = 3;

        public PlayerFallingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
        {
        }

        protected override void OnEnter()
        {
            base.OnEnter();
            Player.AnimationController?.PlayState(CharacterAnimationState.Fall);

            // Wenn bereits in der Luft → _wasUngrounded sofort true (kein Warten nötig)
            _wasUngrounded = !IsGrounded;
            _groundedFrameCount = 0;

#if UNITY_EDITOR
            Debug.Log($"[FallingState] OnEnter | Y={Player.transform.position.y:F2} " +
                      $"LastGroundedY={ReusableData.LastGroundedY:F2} " +
                      $"IsGrounded={IsGrounded} " +
                      $"wasUngrounded={_wasUngrounded}");
#endif
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            // === Landing-Guard: Mindestens 1 Frame in der Luft gewesen? ===
            if (!_wasUngrounded)
            {
                // Noch nicht in der Luft gewesen → prüfen ob jetzt airborne
                if (!IsGrounded)
                {
                    // Jetzt airborne → ab nächstem Frame ist Landing erlaubt
                    _wasUngrounded = true;
                }
                else
                {
                    // Immer noch grounded → Safety-Net Zähler hochzählen
                    // Nach GroundedFrameThreshold Frames trotzdem landen
                    _groundedFrameCount++;
                    if (_groundedFrameCount >= GroundedFrameThreshold)
                    {
                        HandleLanding();
                        return;
                    }
                }
                // Kein Landing möglich solange _wasUngrounded=false (außer Safety-Net)
                return;
            }

            // _wasUngrounded=true → Character war mindestens 1 Frame in der Luft
            // Jetzt auf Bodenkontakt prüfen für Landing
            if (IsGrounded)
            {
                HandleLanding();
            }
        }

        /// <summary>
        /// Gemeinsame Landing-Logik: Berechnet Fallhöhe und wählt Landing-State.
        /// </summary>
        private void HandleLanding()
        {
            float fallDistance = ReusableData.LastGroundedY - Player.transform.position.y;
            float effectiveFallDistance = Mathf.Max(0f, fallDistance);
            float landingSpeed = Mathf.Sqrt(2f * Config.Gravity * effectiveFallDistance);
            ReusableData.LandingVelocity = -landingSpeed;

#if UNITY_EDITOR
            Debug.Log($"[FallingState] Landing! Y={Player.transform.position.y:F2} " +
                      $"LastGroundedY={ReusableData.LastGroundedY:F2} " +
                      $"fallDist={effectiveFallDistance:F2} " +
                      $"landingSpeed={landingSpeed:F1} " +
                      $"threshold={Config.HardLandingThreshold:F1} " +
                      $"→ {(landingSpeed >= Config.HardLandingThreshold ? "HARD" : "SOFT")}");
#endif

            // Landing auf steiler Slope → direkt Sliding (statt Land-Animation)
            var groundInfo = Player.Locomotion.GroundInfo;
            if (groundInfo.SlopeAngle > Config.MaxSlopeAngle
                && Player.Locomotion.Motor.GroundingStatus.FoundAnyGround)
            {
                ChangeState(stateMachine.SlidingState);
                return;
            }

            if (landingSpeed >= Config.HardLandingThreshold)
            {
                if (ShouldRoll())
                {
                    ChangeState(stateMachine.RollingState);
                    return;
                }
                ChangeState(stateMachine.HardLandingState);
            }
            else
            {
                ChangeState(stateMachine.SoftLandingState);
            }
        }

        /// <summary>
        /// Prüft ob der Character einen Landing Roll ausführen soll.
        /// </summary>
        private bool ShouldRoll()
        {
            if (!Config.RollEnabled) return false;

            return Config.RollTriggerMode switch
            {
                RollTriggerMode.MovementInput
                    => ReusableData.MoveInput.sqrMagnitude > 0.01f,
                RollTriggerMode.ButtonPress
                    => ReusableData.DashPressed,
                _ => false,
            };
        }
    }
}
