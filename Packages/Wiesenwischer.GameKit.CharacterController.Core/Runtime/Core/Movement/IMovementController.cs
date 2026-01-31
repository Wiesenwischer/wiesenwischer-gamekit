using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Core.Movement
{
    /// <summary>
    /// Interface für den Movement Controller.
    /// Definiert die Schnittstelle für deterministische Charakterbewegung.
    /// </summary>
    public interface IMovementController
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
        void Simulate(MovementInput input, float deltaTime);

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
    /// Struct für Movement Input.
    /// Enthält alle Eingaben, die für die Bewegung relevant sind.
    /// </summary>
    public struct MovementInput
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
        /// Ob der Sprint-Button gehalten wird.
        /// </summary>
        public bool IsSprinting;

        /// <summary>
        /// Die vertikale Geschwindigkeit (für Gravity/Jump).
        /// Wird vom State Machine gesetzt.
        /// </summary>
        public float VerticalVelocity;

        /// <summary>
        /// Erstellt einen leeren Movement Input.
        /// </summary>
        public static MovementInput Empty => new MovementInput
        {
            MoveDirection = Vector2.zero,
            LookDirection = Vector3.forward,
            IsSprinting = false,
            VerticalVelocity = 0f
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
        /// Erstellt einen leeren GroundInfo (nicht geerdet).
        /// </summary>
        public static GroundInfo Empty => new GroundInfo
        {
            IsGrounded = false,
            Point = Vector3.zero,
            Normal = Vector3.up,
            SlopeAngle = 0f,
            Distance = float.MaxValue,
            IsWalkable = false
        };
    }
}
