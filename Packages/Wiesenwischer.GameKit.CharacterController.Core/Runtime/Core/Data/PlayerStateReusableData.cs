using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Core.Data
{
    /// <summary>
    /// Shared mutable state zwischen allen States während der Runtime.
    /// Basiert auf dem Genshin Impact Pattern - zentrale Stelle für alle
    /// veränderlichen Werte, die States teilen müssen.
    /// </summary>
    public class PlayerStateReusableData
    {
        #region Input Data

        /// <summary>Aktueller Movement Input (WASD/Stick).</summary>
        public Vector2 MoveInput { get; set; }

        /// <summary>Jump wurde diesen Tick gedrückt.</summary>
        public bool JumpPressed { get; set; }

        /// <summary>Jump wird gehalten.</summary>
        public bool JumpHeld { get; set; }

        /// <summary>Sprint wird gehalten.</summary>
        public bool SprintHeld { get; set; }

        /// <summary>Dash wurde gedrückt.</summary>
        public bool DashPressed { get; set; }

        /// <summary>
        /// Ob der Spieler gehen statt laufen möchte.
        /// Kann durch Walk-Toggle oder langsamen Stick-Input gesetzt werden.
        /// </summary>
        public bool ShouldWalk { get; set; }

        #endregion

        #region Movement State

        /// <summary>Aktuelle vertikale Geschwindigkeit.</summary>
        public float VerticalVelocity { get; set; }

        /// <summary>Aktuelle horizontale Geschwindigkeit.</summary>
        public Vector3 HorizontalVelocity { get; set; }

        /// <summary>Movement Speed Modifier (z.B. 1.0 für Walk, 1.5 für Run).</summary>
        public float MovementSpeedModifier { get; set; } = 1f;

        /// <summary>Deceleration Force Modifier.</summary>
        public float MovementDecelerationForce { get; set; } = 1f;

        #endregion

        #region Ground State

        /// <summary>Ob der Character auf dem Boden steht.</summary>
        public bool IsGrounded { get; set; }

        /// <summary>Ob der Character gerade auf einem steilen Hang rutscht.</summary>
        public bool IsSliding { get; set; }

        /// <summary>Zeit seit letztem Bodenkontakt (für Coyote Time).</summary>
        public float TimeSinceGrounded { get; set; }

        /// <summary>
        /// Letzte bekannte Y-Position als der Character geerdet war.
        /// Wird verwendet um zu prüfen ob der Character mehr als MaxStepHeight gefallen ist.
        /// </summary>
        public float LastGroundedY { get; set; }

        /// <summary>
        /// Ob Step Detection aktiv sein soll.
        /// Wird von Grounded States auf true gesetzt, von Airborne States auf false.
        /// </summary>
        public bool StepDetectionEnabled { get; set; }

        #endregion

        #region Jump State

        /// <summary>Aktuelle Jump Force (Stationary, Weak, Medium, Strong).</summary>
        public Vector3 CurrentJumpForce { get; set; }

        /// <summary>Ob der Jump-Button losgelassen wurde (für Variable Jump).</summary>
        public bool JumpButtonReleased { get; set; }

        /// <summary>Ob der Jump-Button losgelassen werden muss vor erneutem Sprung.</summary>
        public bool JumpWasReleased { get; set; } = true;

        /// <summary>
        /// Geschwindigkeit beim Aufprall (für Landing-Berechnung).
        /// Wird im FallingState gesetzt bevor zu LandingState gewechselt wird.
        /// </summary>
        public float LandingVelocity { get; set; }

        #endregion

        #region Movement Intent (One-Shot Flags)

        /// <summary>
        /// Jump-Intent. Wird vom JumpingState gesetzt, von PlayerController konsumiert.
        /// CharacterLocomotion wendet den Jump-Impulse an.
        /// </summary>
        public bool JumpRequested { get; set; }

        /// <summary>
        /// Variable Jump Cut Intent. Wird gesetzt wenn der Jump-Button
        /// während des Aufstiegs losgelassen wird.
        /// CharacterLocomotion reduziert die aufwärts-Velocity.
        /// </summary>
        public bool JumpCutRequested { get; set; }

        /// <summary>
        /// Setzt die vertikale Velocity auf 0 (z.B. bei Ceiling Hit).
        /// CharacterLocomotion setzt _verticalVelocity = 0.
        /// </summary>
        public bool ResetVerticalRequested { get; set; }

        #endregion

        #region Rotation State (ref für Performance bei Structs)

        private Vector3 _currentTargetRotation;
        private Vector3 _dampedTargetRotationCurrentVelocity;
        private Vector3 _dampedTargetRotationPassedTime;

        /// <summary>Aktuelle Ziel-Rotation (ref für Performance).</summary>
        public ref Vector3 CurrentTargetRotation => ref _currentTargetRotation;

        /// <summary>SmoothDamp Velocity für Rotation.</summary>
        public ref Vector3 DampedTargetRotationCurrentVelocity => ref _dampedTargetRotationCurrentVelocity;

        /// <summary>Vergangene Zeit für Rotation-Damping.</summary>
        public ref Vector3 DampedTargetRotationPassedTime => ref _dampedTargetRotationPassedTime;

        #endregion

        #region Tick System

        /// <summary>Aktueller Tick-Index (für CSP).</summary>
        public int CurrentTick { get; set; }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Setzt alle temporären State-Daten zurück.
        /// Wird z.B. bei State-Wechseln aufgerufen.
        /// </summary>
        public void ResetTemporaryData()
        {
            JumpPressed = false;
            DashPressed = false;
            JumpButtonReleased = false;
            JumpRequested = false;
            JumpCutRequested = false;
            ResetVerticalRequested = false;
        }

        /// <summary>
        /// Setzt alle Bewegungs-Daten zurück.
        /// </summary>
        public void ResetMovementData()
        {
            VerticalVelocity = 0f;
            HorizontalVelocity = Vector3.zero;
            MovementSpeedModifier = 1f;
            MovementDecelerationForce = 1f;
        }

        /// <summary>
        /// Kombinierte Velocity aus Horizontal und Vertical.
        /// </summary>
        public Vector3 Velocity => HorizontalVelocity + Vector3.up * VerticalVelocity;

        #endregion
    }
}
