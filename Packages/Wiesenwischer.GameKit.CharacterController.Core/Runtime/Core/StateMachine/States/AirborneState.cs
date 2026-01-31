using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Core.StateMachine.States
{
    /// <summary>
    /// Abstrakte Basisklasse für alle Airborne States (Jumping, Falling).
    /// Bietet gemeinsame Funktionalität für Luftbewegung.
    /// </summary>
    public abstract class AirborneState : BaseCharacterState
    {
        /// <summary>
        /// Berechnet die horizontale Bewegung in der Luft.
        /// Air Control ist reduziert gegenüber Bodenbewegung.
        /// </summary>
        protected Vector3 CalculateAirMovement(IStateMachineContext context, float deltaTime)
        {
            var config = context.Config;
            var moveInput = context.MoveInput;

            if (moveInput.sqrMagnitude < 0.01f)
            {
                return context.HorizontalVelocity;
            }

            // Zielgeschwindigkeit basierend auf Input
            Vector3 targetVelocity = new Vector3(moveInput.x, 0, moveInput.y).normalized * config.WalkSpeed;

            // Reduzierte Kontrolle in der Luft
            float airControl = config.AirControl;

            // Interpoliere zur Zielgeschwindigkeit mit Air Control
            Vector3 currentHorizontal = context.HorizontalVelocity;
            return Vector3.Lerp(currentHorizontal, targetVelocity, airControl * deltaTime);
        }

        /// <summary>
        /// Zeit in der Luft (alias für StateTime).
        /// </summary>
        protected float AirTime => StateTime;
    }
}
