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

            // Animation-State wird von konkreten Subklassen in OnEnter gesetzt:
            // IdlingState/RunningState/etc. → PlayState(Locomotion)
            // SoftLandingState → PlayState(SoftLand)
            // HardLandingState → PlayState(HardLand)
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
            // === Slope Sliding Check (VOR Fall-Detection) ===
            // Muss vor Fall-Detection stehen, damit Character auf steiler Slope
            // nicht fälschlicherweise in Falling wechselt (IsOverEdge = true bei unstable ground).
            if (ShouldTransitionToSliding())
            {
                ChangeState(stateMachine.SlidingState);
                return;
            }

            float currentY = Player.transform.position.y;

            // === Fall-Detection Logik ===
            //
            // IsOverEdge (aus FallDetectionStrategy):
            //   Motor-Modus: SnappingPrevented || !IsStableOnGround
            //   Collider-Modus: Raycast von Capsule-Unterseite findet keinen Boden
            //
            // IsGrounded (aus GroundDetectionStrategy):
            //   Motor-Modus: IsStableOnGround (KCC internes Grounding)
            //   Collider-Modus: SphereCast nach unten findet Boden
            //
            // Ablauf:
            // 1. !IsOverEdge → stabil auf Boden, Timer/Y resetten
            // 2. IsOverEdge + IsGrounded → an Kante aber noch Bodenkontakt, Y weiter tracken
            // 3. IsOverEdge + !IsGrounded → in der Luft, Y NICHT mehr updaten (Referenzpunkt fixiert)
            // 4. Transition zu Falling wenn EINE der Bedingungen erfüllt:
            //    a) fallDistance > MinFallDistance (genug gefallen, sofort transitionieren)
            //    b) TimeSinceGrounded > CoyoteTime (Zeitfallback für kleine Drops/Kanten)
            //    Beide sind unabhängig (||), weil:
            //    - Nur fallDistance allein reicht nicht: Bei kleinen Steps ändert sich Y kaum
            //    - Nur CoyoteTime allein reicht nicht: Bei großen Drops soll sofort reagiert werden

            if (!IsOverEdge)
            {
                // Stabil auf dem Boden → alles resetten
                ReusableData.TimeSinceGrounded = 0f;
                ReusableData.LastGroundedY = currentY;
            }
            else
            {
                // Über Kante — potentieller Fall.
                // Solange IsGrounded=true (noch Bodenkontakt an Kante), Y weiter tracken
                // damit fallDistance erst ab tatsächlichem Bodenverlust gemessen wird.
                // Sobald IsGrounded=false bleibt LastGroundedY als Referenzpunkt fixiert.
                if (IsGrounded)
                {
                    ReusableData.LastGroundedY = currentY;
                }

                ReusableData.TimeSinceGrounded += Time.deltaTime;

                float fallDistance = ReusableData.LastGroundedY - currentY;

                if (fallDistance > Config.MinFallDistance || ReusableData.TimeSinceGrounded > Config.CoyoteTime)
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
            return IsGrounded || ReusableData.TimeSinceGrounded <= Config.CoyoteTime;
        }

        /// <summary>
        /// Prüft ob Movement-Input vorhanden ist.
        /// </summary>
        protected bool HasMovementInput()
        {
            return ReusableData.MoveInput.sqrMagnitude > 0.01f;
        }

        /// <summary>
        /// Prüft ob der Character auf einem zu steilen Hang steht und rutschen sollte.
        /// </summary>
        private bool ShouldTransitionToSliding()
        {
            var groundInfo = Player.Locomotion.GroundInfo;

            // Slope muss steiler als MaxSlopeAngle sein
            if (groundInfo.SlopeAngle <= Config.MaxSlopeAngle)
                return false;

            // Motor muss noch Bodenkontakt haben (aber instabil)
            if (!Player.Locomotion.Motor.GroundingStatus.FoundAnyGround)
                return false;

            // Auf Treppen erkennt das Step-Handling stabilen Boden trotz steiler Stufenflächen —
            // in diesem Fall NICHT sliden
            if (Player.Locomotion.Motor.GroundingStatus.IsStableOnGround)
                return false;

            return true;
        }
    }
}
