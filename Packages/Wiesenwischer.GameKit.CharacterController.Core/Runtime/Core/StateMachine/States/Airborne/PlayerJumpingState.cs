using Wiesenwischer.GameKit.CharacterController.Core.Animation;
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

        /// <summary>
        /// Ob der Jump-Impulse vom Motor bestätigt wurde (VerticalVelocity > 0).
        /// Verhindert vorzeitige IsFalling-Transition durch Sync-Back-Lag
        /// zwischen Intent-System (TickSystem 60Hz) und Motor (FixedUpdate ~50Hz).
        /// </summary>
        private bool _jumpImpulseConfirmed;

        public override string StateName => "Jumping";

        public PlayerJumpingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
        {
        }

        protected override void OnEnter()
        {
            base.OnEnter();

            Player.AnimationController?.PlayState(CharacterAnimationState.Jump);

            // Intent: Jump anmelden - CharacterLocomotion wendet den Impulse an
            ReusableData.JumpRequested = true;
            ReusableData.JumpButtonReleased = false;
            _jumpImpulseConfirmed = false;
        }

        protected override void OnHandleInput()
        {
            base.OnHandleInput();

            // Variable Jump: Nur wenn aktiviert
            if (!Config.UseVariableJump) return;

            // Intent: Jump Cut anmelden wenn Button während Aufstieg losgelassen
            if (!ReusableData.JumpHeld && !ReusableData.JumpButtonReleased
                && _jumpImpulseConfirmed && ReusableData.VerticalVelocity > 0)
            {
                ReusableData.JumpCutRequested = true;
                ReusableData.JumpButtonReleased = true;
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            // === Jump-Impulse Bestätigung ===
            // Problem: Jump wird per Intent angemeldet (JumpRequested=true), aber der Motor
            // verarbeitet den Impulse erst im nächsten FixedUpdate. Zwischen Intent und Motor
            // hat VerticalVelocity noch den alten Wert (z.B. -2f GroundingVelocity).
            // IsFalling() würde sofort true liefern → direkter Übergang zu FallingState.
            // Lösung: Warte bis VerticalVelocity > 0 (Motor hat Impulse verarbeitet).
            if (!_jumpImpulseConfirmed)
            {
                if (ReusableData.VerticalVelocity > 0f)
                {
                    _jumpImpulseConfirmed = true;
                }
                return;
            }

            // === Rampen-Landing ===
            // Reihenfolge wichtig: Grounded-Check VOR dem reinen IsFalling-Check.
            // Wenn Character während Aufstieg auf Rampe landet (IsGrounded=true) UND
            // Velocity bereits abfällt → direkt SoftLanding statt FallingState.
            // Ohne diesen Check: JumpingState → FallingState (obwohl grounded) →
            // FallingState._wasUngrounded wird nie true → Character steckt fest.
            if (IsGrounded && _jumpModule.IsFalling(ReusableData.VerticalVelocity))
            {
                ChangeState(stateMachine.SoftLandingState);
                return;
            }

            // === Apex-Erkennung (in der Luft) ===
            // Wenn Velocity ≤ 0 und NICHT grounded → Scheitelpunkt erreicht, ab jetzt fallen.
            if (_jumpModule.IsFalling(ReusableData.VerticalVelocity))
            {
                ChangeState(stateMachine.FallingState);
                return;
            }

            // === Ceiling Detection ===
            // Raycast nach oben prüft ob Decke getroffen wird.
            // stateTime > 0.05f: Kurze Verzögerung damit der Motor den Jump-Impulse
            // vollständig verarbeiten kann (verhindert False-Positives vom Boden).
            var motor = stateMachine.Player.CharacterMotor;
            if (stateTime > 0.05f && _jumpModule.CheckCeiling(motor, CeilingCheckDistance, Config.GroundLayers))
            {
                ReusableData.ResetVerticalRequested = true;
                ChangeState(stateMachine.FallingState);
            }
        }
    }
}
