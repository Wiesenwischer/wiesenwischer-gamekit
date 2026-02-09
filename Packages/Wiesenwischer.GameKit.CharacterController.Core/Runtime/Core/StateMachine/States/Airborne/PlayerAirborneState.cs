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

            // Animator explizit informieren: wir sind in der Luft
            Player.AnimationController?.SetGrounded(false);

            // Mindestens Walk-Speed Modifier beibehalten,
            // damit von Idle fallende Characters steuerbar sind.
            // Höhere Modifier (z.B. 2.0 von RunningState) bleiben erhalten
            // → Momentum wird konserviert. AirControl/AirDrag im AccelerationModule
            // steuern die tatsächliche Luftphysik.
            if (ReusableData.MovementSpeedModifier < 1f)
            {
                ReusableData.MovementSpeedModifier = 1f;
            }
        }

        protected override void OnHandleInput()
        {
            // JumpWasReleased auch in der Luft tracken.
            // Ohne dieses Tracking bleibt JumpWasReleased=false nach der Landung,
            // wenn der Spieler den Button während des Flugs losgelassen hat,
            // und der nächste Jump wird von CanJump() blockiert.
            if (!ReusableData.JumpHeld)
            {
                ReusableData.JumpWasReleased = true;
            }
        }

        protected override void OnUpdate()
        {
            // Modifier wird NICHT überschrieben - Momentum aus dem Grounded State bleibt erhalten.
            // Luftphysik (Lenkbarkeit + Abbremsung) wird durch AirControl/AirDrag
            // im AccelerationModule gesteuert, nicht durch den SpeedModifier.
        }

        protected override void OnPhysicsUpdate(float deltaTime)
        {
            // Gravity wird von CharacterLocomotion via GravityModule angewendet (Intent System).
            // States setzen nur noch Intent (Jump, JumpCut, ResetVertical).
        }
    }
}
