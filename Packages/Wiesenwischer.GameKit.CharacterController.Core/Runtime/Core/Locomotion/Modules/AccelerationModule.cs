using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Core.Locomotion.Modules
{
    /// <summary>
    /// Modul für horizontale Velocity-Berechnung mit Acceleration/Deceleration.
    /// Extrahiert aus CharacterLocomotion für bessere Testbarkeit und Wiederverwendbarkeit.
    /// </summary>
    public class AccelerationModule
    {
        /// <summary>
        /// Berechnet die horizontale Velocity mit Acceleration/Deceleration.
        /// </summary>
        /// <param name="currentVelocity">Aktuelle horizontale Velocity</param>
        /// <param name="targetVelocity">Ziel-Velocity basierend auf Input</param>
        /// <param name="acceleration">Beschleunigung beim Bewegen</param>
        /// <param name="deceleration">Abbremsen beim Stoppen</param>
        /// <param name="airControl">Multiplikator für Steuerbarkeit in der Luft (0-1)</param>
        /// <param name="airDrag">Multiplikator für Momentum-Verlust in der Luft (0-1)</param>
        /// <param name="isGrounded">Ist der Character am Boden?</param>
        /// <param name="deltaTime">Zeit seit letztem Frame</param>
        /// <returns>Neue horizontale Velocity</returns>
        public Vector3 CalculateHorizontalVelocity(
            Vector3 currentVelocity,
            Vector3 targetVelocity,
            float acceleration,
            float deceleration,
            float airControl,
            float airDrag,
            bool isGrounded,
            float deltaTime)
        {
            bool hasInput = targetVelocity.sqrMagnitude > 0.01f;
            float accel = hasInput ? acceleration : deceleration;

            if (!isGrounded)
            {
                // AirControl: Steuerbarkeit (Beschleunigung Richtung Input)
                // AirDrag: Momentum-Verlust (Abbremsung ohne Input)
                accel *= hasInput ? airControl : airDrag;
            }

            return Vector3.MoveTowards(currentVelocity, targetVelocity, accel * deltaTime);
        }

        /// <summary>
        /// Berechnet die Ziel-Velocity basierend auf Input-Richtung und Geschwindigkeit.
        /// </summary>
        /// <param name="inputDirection">Normalisierte Input-Richtung (X/Z)</param>
        /// <param name="lookDirection">Blickrichtung für kamera-relative Bewegung</param>
        /// <param name="characterForward">Forward-Richtung des Characters (Fallback)</param>
        /// <param name="speed">Basis-Geschwindigkeit</param>
        /// <param name="speedModifier">Speed-Multiplikator (z.B. Sprint)</param>
        /// <returns>Ziel-Velocity im World Space</returns>
        public Vector3 CalculateTargetVelocity(
            Vector2 inputDirection,
            Vector3 lookDirection,
            Vector3 characterForward,
            float speed,
            float speedModifier)
        {
            Vector3 inputDir = new Vector3(inputDirection.x, 0, inputDirection.y);

            // Transformiere Input relativ zur Kamera/Look-Richtung
            if (lookDirection.sqrMagnitude > 0.01f)
            {
                Vector3 flatLook = new Vector3(lookDirection.x, 0, lookDirection.z).normalized;
                Quaternion lookRot = Quaternion.LookRotation(flatLook, Vector3.up);
                inputDir = lookRot * inputDir;
            }
            else
            {
                // Fallback: Character-Forward verwenden
                Quaternion charRot = Quaternion.LookRotation(characterForward, Vector3.up);
                inputDir = charRot * inputDir;
            }

            float finalSpeed = speed * speedModifier;
            return inputDir.normalized * finalSpeed * inputDir.magnitude;
        }
    }
}
