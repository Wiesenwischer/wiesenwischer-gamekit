using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Motor;

namespace Wiesenwischer.GameKit.CharacterController.Core.Locomotion
{
    /// <summary>
    /// Interface für Locomotion Controller.
    /// Definiert die Schnittstelle für deterministische Charakterbewegung.
    /// Wird von verschiedenen Locomotion-Typen implementiert (Character, Riding, Gliding).
    /// </summary>
    public interface ILocomotionController
    {
        /// <summary>
        /// Die aktuelle Position des Characters.
        /// </summary>
        Vector3 Position { get; }

        /// <summary>
        /// Die aktuelle Rotation des Characters.
        /// </summary>
        Quaternion Rotation { get; }

        /// <summary>
        /// Die aktuelle Geschwindigkeit des Characters.
        /// </summary>
        Vector3 Velocity { get; }

        /// <summary>
        /// Ob der Character auf dem Boden steht.
        /// </summary>
        bool IsGrounded { get; }

        /// <summary>
        /// Informationen über den Boden unter dem Character.
        /// </summary>
        GroundInfo GroundInfo { get; }

        /// <summary>
        /// Führt die Bewegungssimulation für einen Tick aus.
        /// Muss deterministisch sein (keine Randomness, kein Time.deltaTime).
        /// </summary>
        /// <param name="input">Der Input für diesen Tick.</param>
        /// <param name="deltaTime">Die feste Zeit pro Tick.</param>
        void Simulate(LocomotionInput input, float deltaTime);

        /// <summary>
        /// Setzt den Character auf eine bestimmte Position und Rotation.
        /// Wird für Server-Reconciliation bei CSP verwendet.
        /// </summary>
        /// <param name="position">Die Zielposition.</param>
        /// <param name="rotation">Die Zielrotation.</param>
        void SetPositionAndRotation(Vector3 position, Quaternion rotation);

        /// <summary>
        /// Wendet eine Geschwindigkeit direkt an (z.B. für Knockback, Jump).
        /// </summary>
        /// <param name="velocity">Die anzuwendende Geschwindigkeit.</param>
        void ApplyVelocity(Vector3 velocity);
    }

    /// <summary>
    /// Struct für Locomotion Input.
    /// Enthält Intent-basierte Eingaben für die Bewegung.
    /// States setzen Intent (Modifier, Impulse, Flags), CharacterLocomotion führt Physik aus.
    /// </summary>
    public struct LocomotionInput
    {
        /// <summary>
        /// Bewegungsrichtung (X = horizontal, Y = vertikal/forward).
        /// Normalisiert von -1 bis 1.
        /// </summary>
        public Vector2 MoveDirection;

        /// <summary>
        /// Blickrichtung in Weltkoordinaten (für Rotation).
        /// </summary>
        public Vector3 LookDirection;

        /// <summary>
        /// Ob Step Detection aktiv sein soll.
        /// Wird von Grounded States auf true gesetzt, von Airborne States auf false.
        /// </summary>
        public bool StepDetectionEnabled;

        /// <summary>
        /// Geschwindigkeits-Multiplikator vom State Machine.
        /// Wird mit WalkSpeed multipliziert (z.B. 0 für Idle, 1 für Walk, 2 für Run).
        /// </summary>
        public float SpeedModifier;

        #region Vertical Intent

        /// <summary>
        /// Sprung-Intent. Wenn true, wird ein Jump-Impulse angewendet.
        /// One-Shot: Wird nach Konsum zurückgesetzt.
        /// </summary>
        public bool Jump;

        /// <summary>
        /// Variable Jump Cut. Wenn true, wird die aufwärts-Velocity reduziert.
        /// Wird gesetzt wenn der Jump-Button früh losgelassen wird.
        /// One-Shot: Wird nach Konsum zurückgesetzt.
        /// </summary>
        public bool JumpCut;

        /// <summary>
        /// Setzt die vertikale Velocity auf 0 (z.B. bei Ceiling Hit).
        /// One-Shot: Wird nach Konsum zurückgesetzt.
        /// </summary>
        public bool ResetVerticalVelocity;

        #endregion

        /// <summary>
        /// Erstellt einen leeren Locomotion Input.
        /// </summary>
        public static LocomotionInput Empty => new LocomotionInput
        {
            MoveDirection = Vector2.zero,
            LookDirection = Vector3.forward,
            StepDetectionEnabled = false,
            SpeedModifier = 1f
        };
    }

    /// <summary>
    /// Informationen über den Boden unter dem Character.
    /// </summary>
    public struct GroundInfo
    {
        /// <summary>
        /// Ob der Character auf dem Boden steht.
        /// </summary>
        public bool IsGrounded;

        /// <summary>
        /// Der Punkt, an dem der Ground Check getroffen hat.
        /// </summary>
        public Vector3 Point;

        /// <summary>
        /// Die Normale der Oberfläche.
        /// </summary>
        public Vector3 Normal;

        /// <summary>
        /// Der Winkel der Oberfläche relativ zur Horizontalen.
        /// </summary>
        public float SlopeAngle;

        /// <summary>
        /// Die Distanz zum Boden.
        /// </summary>
        public float Distance;

        /// <summary>
        /// Ob die Oberfläche begehbar ist (basierend auf Max Slope Angle).
        /// </summary>
        public bool IsWalkable;

        /// <summary>
        /// Detaillierter Stabilitäts-Report mit Ledge- und Step-Informationen.
        /// Verwendet direkt Motor.HitStabilityReport (KCC Pattern).
        /// </summary>
        public HitStabilityReport StabilityReport;

        /// <summary>
        /// Erstellt einen leeren GroundInfo (nicht geerdet).
        /// </summary>
        public static GroundInfo Empty => new GroundInfo
        {
            IsGrounded = false,
            Point = Vector3.zero,
            Normal = Vector3.up,
            SlopeAngle = 0f,
            Distance = float.MaxValue,
            IsWalkable = false,
            StabilityReport = new HitStabilityReport()
        };
    }
}
