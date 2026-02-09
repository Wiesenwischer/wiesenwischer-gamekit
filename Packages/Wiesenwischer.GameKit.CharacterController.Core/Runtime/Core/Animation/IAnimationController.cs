namespace Wiesenwischer.GameKit.CharacterController.Core.Animation
{
    /// <summary>
    /// Interface für Animation-Steuerung.
    /// Liegt im Core-Package, damit PlayerController und States darauf zugreifen können.
    /// Implementierung liegt im Animation-Package (AnimatorParameterBridge).
    /// </summary>
    public interface IAnimationController
    {
        /// <summary>
        /// Setzt die Bewegungsgeschwindigkeit (0 = Idle, 1 = Run, 1.5 = Sprint).
        /// </summary>
        void SetSpeed(float speed);

        /// <summary>
        /// Setzt den Grounded-Status.
        /// </summary>
        void SetGrounded(bool isGrounded);

        /// <summary>
        /// Setzt die vertikale Velocity für Jump/Fall Blending.
        /// </summary>
        void SetVerticalVelocity(float velocity);

        /// <summary>
        /// Triggert die Jump-Animation.
        /// </summary>
        void TriggerJump();

        /// <summary>
        /// Triggert die Land-Animation.
        /// </summary>
        void TriggerLand();

        /// <summary>
        /// Setzt den HardLanding-Parameter und triggert die Land-Animation.
        /// Kombiniert SetHardLanding + TriggerLand in einem Aufruf.
        /// </summary>
        void TriggerLanding(bool isHardLanding);

        /// <summary>
        /// Setzt das Gewicht des Ability-Layers (0-1).
        /// </summary>
        void SetAbilityLayerWeight(float weight);
    }
}
