using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Core.StateMachine.States
{
    /// <summary>
    /// Basis-State für alle Grounded-States.
    /// Gemeinsame Logik für Idle, Walking, Running, Sprinting, etc.
    /// </summary>
    public class PlayerGroundedState : PlayerMovementState
    {
        public override string StateName => "Grounded";

        public PlayerGroundedState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
        {
        }

        protected override void OnEnter()
        {
            // VerticalVelocity wird NICHT mehr hier resettet - CharacterLocomotion
            // handhabt das via GravityModule (GroundingVelocity wenn grounded).
            ReusableData.TimeSinceGrounded = 0f;

            // Step Detection aktivieren für Grounded States
            ReusableData.StepDetectionEnabled = true;

            // Animator explizit informieren: wir sind am Boden
            Player.AnimationController?.SetGrounded(true);
        }

        protected override void OnHandleInput()
        {
            // Track ob Jump-Taste losgelassen wurde
            if (!ReusableData.JumpHeld)
            {
                ReusableData.JumpWasReleased = true;
            }

            // Jump Input prüfen
            if (ReusableData.JumpPressed && CanJump())
            {
                OnJump();
                return;
            }
        }

        protected override void OnUpdate()
        {
            float currentY = Player.transform.position.y;

            // Stabilitäts-Check: Wenn grounded aber Snapping verhindert wurde (z.B. Ledge Edge), zu Falling wechseln
            // Dies verhindert das "Kleben" an Slope-Kanten
            // WICHTIG: Verwende Motor's State (nicht externes GroundingDetection) für Konsistenz
            var motor = Player.Locomotion?.Motor;
            if (motor != null && ReusableData.IsGrounded && motor.GroundingStatus.SnappingPrevented)
            {
                // Auf instabiler Oberfläche (z.B. Ledge Edge) - zu Falling wechseln
                ChangeState(stateMachine.FallingState);
                return;
            }

            // Track time since last grounded (for Coyote Time)
            if (ReusableData.IsGrounded)
            {
                ReusableData.TimeSinceGrounded = 0f;
                ReusableData.LastGroundedY = currentY;
            }
            else
            {
                ReusableData.TimeSinceGrounded += Time.deltaTime;

                // Berechne wie weit der Character gefallen ist seit letztem Frame
                float fallDistance = ReusableData.LastGroundedY - currentY;

                // Aktualisiere LastGroundedY wenn Character NICHT fällt
                // (geht hoch oder bleibt gleich - z.B. auf Treppen)
                if (currentY >= ReusableData.LastGroundedY)
                {
                    ReusableData.LastGroundedY = currentY;
                }
                // Bei kleinem Drop (unter MinFallDistance): auch aktualisieren
                else if (fallDistance <= Config.MinFallDistance)
                {
                    ReusableData.LastGroundedY = currentY;
                }
                // Bei großem Drop (>= MinFallDistance): Zu Falling wechseln
                else if (ReusableData.TimeSinceGrounded > Config.CoyoteTime)
                {
                    ChangeState(stateMachine.FallingState);
                    return;
                }
            }
        }

        protected override void OnPhysicsUpdate(float deltaTime)
        {
            // Grounding-Velocity und Gravity werden von CharacterLocomotion gehandhabt.
            // GravityModule.CalculateVerticalVelocity() macht beides:
            // - Grounded + nicht aufwärts → GroundingVelocity (-2f)
            // - Nicht grounded (Coyote Time) → Gravity anwenden + MaxFallSpeed clamping
        }

        /// <summary>
        /// Wird aufgerufen wenn Jump gedrückt wird.
        /// Kann von Subklassen überschrieben werden für unterschiedliche Jump-Forces.
        /// </summary>
        protected virtual void OnJump()
        {
            ChangeState(stateMachine.JumpingState);
        }

        /// <summary>
        /// Prüft ob der Character springen kann.
        /// </summary>
        protected bool CanJump()
        {
            // Taste muss erst losgelassen werden bevor erneut gesprungen werden kann
            if (!ReusableData.JumpWasReleased) return false;

            // Kann springen wenn grounded ODER innerhalb Coyote Time
            return ReusableData.IsGrounded || ReusableData.TimeSinceGrounded <= Config.CoyoteTime;
        }

        /// <summary>
        /// Prüft ob Movement-Input vorhanden ist.
        /// </summary>
        protected bool HasMovementInput()
        {
            return ReusableData.MoveInput.sqrMagnitude > 0.01f;
        }
    }
}
