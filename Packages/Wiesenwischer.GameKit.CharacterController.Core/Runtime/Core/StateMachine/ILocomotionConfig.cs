using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Core.StateMachine
{
    /// <summary>
    /// Bestimmt welche Ground Detection Strategy verwendet wird.
    /// </summary>
    public enum GroundDetectionMode
    {
        /// <summary>Motor's internes IsStableOnGround (KCC-Standard).</summary>
        Motor = 0,
        /// <summary>SphereCast von Capsule-Unterseite (Genshin-Style).</summary>
        Collider = 1
    }

    /// <summary>
    /// Bestimmt welche Fall Detection Strategy verwendet wird.
    /// </summary>
    public enum FallDetectionMode
    {
        /// <summary>SnappingPrevented + IsStableOnGround aus dem Motor (KCC-Standard).</summary>
        Motor = 0,
        /// <summary>Raycast von Capsule-Mitte — kein Boden = über Kante (Genshin-Style).</summary>
        Collider = 1
    }

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

        /// <summary>
        /// Welche Ground Detection Strategy verwendet wird.
        /// Motor = KCC-Standard (IsStableOnGround), Collider = SphereCast (Genshin-Style).
        /// </summary>
        GroundDetectionMode GroundDetection { get; }

        /// <summary>
        /// Welche Fall Detection Strategy verwendet wird.
        /// Motor = SnappingPrevented, Collider = Raycast von Capsule-Mitte (Genshin-Style).
        /// </summary>
        FallDetectionMode FallDetection { get; }

        /// <summary>
        /// Raycast-Distanz von Capsule-Mitte nach unten für Fall-Erkennung.
        /// Wenn kein Boden innerhalb dieser Distanz → Character fällt.
        /// Nur aktiv bei FallDetection = Collider.
        /// </summary>
        float GroundToFallRayDistance { get; }

        // Rotation
        float RotationSpeed { get; }
        bool RotateTowardsMovement { get; }

        // Step Detection
        float MaxStepHeight { get; }
        float MinStepDepth { get; }

        /// <summary>
        /// Ob Treppen-Erkennung mit automatischer Geschwindigkeitsreduktion aktiviert ist.
        /// Wenn true, wird die Geschwindigkeit beim Treppensteigen reduziert.
        /// </summary>
        bool StairSpeedReductionEnabled { get; }

        /// <summary>
        /// Geschwindigkeitsreduktion auf Treppen (0 = keine Reduktion, 1 = Stillstand).
        /// Wird angewendet wenn der Motor mehrere Steps in kurzer Zeit erkennt.
        /// </summary>
        float StairSpeedReduction { get; }

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

        // Stopping
        /// <summary>Deceleration beim Stoppen aus Walk (m/s²).</summary>
        float LightStopDeceleration { get; }

        /// <summary>Deceleration beim Stoppen aus Run (m/s²).</summary>
        float MediumStopDeceleration { get; }

        /// <summary>Deceleration beim Stoppen aus Sprint (m/s²).</summary>
        float HardStopDeceleration { get; }

        // Slope Speed
        /// <summary>
        /// Maximale Speed-Reduktion bergauf bei steilstem begehbaren Winkel (MaxSlopeAngle).
        /// 0 = keine Reduktion, 1 = Stillstand bei MaxSlopeAngle.
        /// Skaliert linear mit dem Slope-Winkel.
        /// </summary>
        float UphillSpeedPenalty { get; }

        /// <summary>
        /// Speed-Bonus bergab bei steilstem begehbaren Winkel (MaxSlopeAngle).
        /// Positiv = schneller bergab, 0 = kein Effekt, negativ = langsamer bergab.
        /// Skaliert linear mit dem Slope-Winkel.
        /// </summary>
        float DownhillSpeedBonus { get; }

        // Slope Sliding
        float SlopeSlideSpeed { get; }

        /// <summary>
        /// Wenn true, skaliert die Sliding-Geschwindigkeit mit der Steilheit des Hangs.
        /// Wenn false, wird immer SlopeSlideSpeed als feste Geschwindigkeit verwendet.
        /// </summary>
        bool UseSlopeDependentSlideSpeed { get; }

        /// <summary>
        /// Beschleunigung beim Eingleiten in den Slide (m/s²).
        /// Höhere Werte = schnellerer Übergang zu voller Slide-Geschwindigkeit.
        /// </summary>
        float SlideAcceleration { get; }

        /// <summary>
        /// Seitliche Lenkkraft während des Slidings (0 = keine Lenkung, 1 = volle Kontrolle).
        /// Erlaubt dem Spieler, die Rutsch-Richtung zu beeinflussen.
        /// </summary>
        float SlideSteerStrength { get; }

        /// <summary>
        /// Hysterese-Winkel für den Exit aus dem Slide-State (Grad).
        /// Character verlässt Slide erst bei SlopeAngle &lt; MaxSlopeAngle - SlideExitHysteresis.
        /// Verhindert Flackern an der Grenzwinkel-Kante.
        /// </summary>
        float SlideExitHysteresis { get; }

        /// <summary>
        /// Ob der Spieler aus dem Slide-State abspringen kann.
        /// </summary>
        bool CanJumpFromSlide { get; }

        /// <summary>
        /// Reduzierte Sprungkraft beim Abspringen aus dem Slide (Multiplikator, 0-1).
        /// </summary>
        float SlideJumpForceMultiplier { get; }

        /// <summary>
        /// Mindestzeit im Slide-State (Sekunden).
        /// Verhindert sofortiges Flackern bei kurzzeitigem Slope-Kontakt.
        /// </summary>
        float MinSlideTime { get; }

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
