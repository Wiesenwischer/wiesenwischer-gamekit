using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Core.StateMachine
{
    /// <summary>
    /// Interface für alle Character States in der State Machine.
    /// Jeder State verwaltet seine eigene Enter/Update/Exit-Logik.
    /// </summary>
    public interface ICharacterState
    {
        /// <summary>
        /// Eindeutiger Name des States für Debugging und Netzwerk-Synchronisation.
        /// </summary>
        string StateName { get; }

        /// <summary>
        /// Wird aufgerufen, wenn der State betreten wird.
        /// </summary>
        /// <param name="context">Der State Machine Kontext mit Zugriff auf alle Systeme.</param>
        void Enter(IStateMachineContext context);

        /// <summary>
        /// Wird jeden Tick aufgerufen, während der State aktiv ist.
        /// </summary>
        /// <param name="context">Der State Machine Kontext.</param>
        /// <param name="deltaTime">Die Zeit seit dem letzten Tick (Fixed Delta für Determinismus).</param>
        void Update(IStateMachineContext context, float deltaTime);

        /// <summary>
        /// Wird aufgerufen, wenn der State verlassen wird.
        /// </summary>
        /// <param name="context">Der State Machine Kontext.</param>
        void Exit(IStateMachineContext context);

        /// <summary>
        /// Evaluiert mögliche Übergänge zu anderen States.
        /// </summary>
        /// <param name="context">Der State Machine Kontext.</param>
        /// <returns>Der nächste State oder null, wenn kein Übergang stattfinden soll.</returns>
        ICharacterState EvaluateTransitions(IStateMachineContext context);
    }

    /// <summary>
    /// Kontext-Interface für die State Machine.
    /// Bietet Zugriff auf alle relevanten Systeme und Daten.
    /// </summary>
    public interface IStateMachineContext
    {
        /// <summary>
        /// Der aktuelle Movement Input.
        /// </summary>
        Vector2 MoveInput { get; }

        /// <summary>
        /// Ob der Jump-Button gedrückt wurde (diesen Tick).
        /// </summary>
        bool JumpPressed { get; }

        /// <summary>
        /// Ob der Jump-Button gehalten wird.
        /// </summary>
        bool JumpHeld { get; }

        /// <summary>
        /// Ob der Character auf dem Boden steht.
        /// </summary>
        bool IsGrounded { get; }

        /// <summary>
        /// Die aktuelle vertikale Geschwindigkeit.
        /// </summary>
        float VerticalVelocity { get; set; }

        /// <summary>
        /// Die aktuelle horizontale Geschwindigkeit.
        /// </summary>
        Vector3 HorizontalVelocity { get; set; }

        /// <summary>
        /// Zugriff auf die Movement-Konfiguration.
        /// </summary>
        IMovementConfig Config { get; }

        /// <summary>
        /// Der aktuelle Tick-Index (für CSP).
        /// </summary>
        int CurrentTick { get; }
    }

    /// <summary>
    /// Interface für Movement-Konfiguration.
    /// Ermöglicht den Zugriff auf Movement-Parameter ohne direkte Abhängigkeit von ScriptableObject.
    /// </summary>
    public interface IMovementConfig
    {
        // Ground Movement
        float WalkSpeed { get; }
        float RunSpeed { get; }
        float Acceleration { get; }
        float Deceleration { get; }

        // Air Movement
        float AirControl { get; }
        float Gravity { get; }
        float MaxFallSpeed { get; }

        // Jumping
        float JumpHeight { get; }
        float JumpDuration { get; }
        float CoyoteTime { get; }
        float JumpBufferTime { get; }

        // Ground Detection
        float GroundCheckDistance { get; }
        float GroundCheckRadius { get; }
        LayerMask GroundLayers { get; }
        float MaxSlopeAngle { get; }

        // Rotation
        float RotationSpeed { get; }
        bool RotateTowardsMovement { get; }

        // Step Detection
        float MaxStepHeight { get; }
        float MinStepDepth { get; }
    }
}
