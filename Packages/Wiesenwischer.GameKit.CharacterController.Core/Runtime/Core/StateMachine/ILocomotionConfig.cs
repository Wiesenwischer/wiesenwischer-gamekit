using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Core.StateMachine
{
    /// <summary>
    /// Interface für Locomotion-Konfiguration.
    /// Ermöglicht den Zugriff auf Locomotion-Parameter ohne direkte Abhängigkeit von ScriptableObject.
    /// Wird von CharacterLocomotion und anderen Locomotion-Typen verwendet.
    /// </summary>
    public interface ILocomotionConfig
    {
        // Ground Movement
        float WalkSpeed { get; }
        float RunSpeed { get; }
        float Acceleration { get; }
        float Deceleration { get; }

        /// <summary>
        /// Multiplikator für Sprint-Geschwindigkeit relativ zu Run.
        /// Sprint = (RunSpeed / WalkSpeed) * SprintMultiplier.
        /// </summary>
        float SprintMultiplier { get; }

        // Air Movement
        float AirControl { get; }

        /// <summary>
        /// Wie schnell horizontales Momentum in der Luft verloren geht (0 = kein Drag, 1 = volle Abbremsung).
        /// Getrennt von AirControl (Steuerbarkeit). AirDrag = 1 bedeutet Deceleration wie am Boden.
        /// </summary>
        float AirDrag { get; }

        /// <summary>
        /// Minimale Falldistanz in Metern, ab der der Character als "fallend" erkannt wird.
        /// Drops unterhalb dieses Werts werden ignoriert (z.B. Treppen, kleine Kanten).
        /// </summary>
        float MinFallDistance { get; }

        float Gravity { get; }
        float MaxFallSpeed { get; }

        // Jumping
        float JumpHeight { get; }
        float JumpDuration { get; }
        float CoyoteTime { get; }
        float JumpBufferTime { get; }

        /// <summary>
        /// Wenn true, kann der Sprung durch frühes Loslassen der Taste abgebrochen werden (niedrigerer Sprung).
        /// Wenn false, springt der Character immer die volle Höhe.
        /// </summary>
        bool UseVariableJump { get; }

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

        #region Ledge & Ground Snapping

        /// <summary>
        /// Maximale Distanz von der Capsule-Achse zur Kante, bei der der Character noch stabil steht.
        /// Typischer Wert: Capsule Radius (0.5f).
        /// Bei größerer Distanz wird der Character als instabil auf der Kante betrachtet.
        /// </summary>
        float MaxStableDistanceFromLedge { get; }

        /// <summary>
        /// Maximaler Winkelunterschied zwischen zwei aufeinanderfolgenden Oberflächen,
        /// bei dem Ground Snapping noch aktiv bleibt.
        /// Verhindert "Kleben" an steilen Kanten beim Herunterlaufen.
        /// Typischer Wert: 50-80 Grad.
        /// </summary>
        float MaxStableDenivelationAngle { get; }

        /// <summary>
        /// Geschwindigkeit ab der Ground Snapping an Kanten deaktiviert wird.
        /// Ermöglicht das "Abspringen" von Kanten bei hoher Geschwindigkeit.
        /// 0 = Immer snappen, 10 = Bei > 10 m/s nicht mehr snappen.
        /// </summary>
        float MaxVelocityForLedgeSnap { get; }

        /// <summary>
        /// Ob Ledge Detection aktiviert ist.
        /// Hat Performance-Kosten (zusätzliche Raycasts).
        /// </summary>
        bool LedgeDetectionEnabled { get; }

        #endregion

        // Slope Sliding
        float SlopeSlideSpeed { get; }

        /// <summary>
        /// Wenn true, skaliert die Sliding-Geschwindigkeit mit der Steilheit des Hangs.
        /// Wenn false, wird immer SlopeSlideSpeed als feste Geschwindigkeit verwendet.
        /// </summary>
        bool UseSlopeDependentSlideSpeed { get; }

        // Landing
        /// <summary>Fallgeschwindigkeit unter der sofort weitergelaufen werden kann.</summary>
        float SoftLandingThreshold { get; }

        /// <summary>Fallgeschwindigkeit ab der maximale Recovery-Zeit gilt.</summary>
        float HardLandingThreshold { get; }

        /// <summary>Recovery-Zeit bei weicher Landung (Sekunden).</summary>
        float SoftLandingDuration { get; }

        /// <summary>Recovery-Zeit bei harter Landung (Sekunden).</summary>
        float HardLandingDuration { get; }
    }
}
