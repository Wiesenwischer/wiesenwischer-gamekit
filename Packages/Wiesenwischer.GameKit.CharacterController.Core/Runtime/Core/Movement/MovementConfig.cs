using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.StateMachine;

namespace Wiesenwischer.GameKit.CharacterController.Core.Movement
{
    /// <summary>
    /// ScriptableObject für Movement-Konfiguration.
    /// Enthält alle Parameter für Charakterbewegung, Springen und Ground Detection.
    /// </summary>
    [CreateAssetMenu(fileName = "MovementConfig", menuName = "GameKit/Movement Config", order = 0)]
    public class MovementConfig : ScriptableObject, IMovementConfig
    {
        [Header("Ground Movement")]
        [Tooltip("Geschwindigkeit beim Gehen (m/s)")]
        [SerializeField] private float _walkSpeed = 3.0f;

        [Tooltip("Geschwindigkeit beim Rennen (m/s)")]
        [SerializeField] private float _runSpeed = 6.0f;

        [Tooltip("Beschleunigung beim Starten der Bewegung (m/s²)")]
        [SerializeField] private float _acceleration = 10.0f;

        [Tooltip("Verzögerung beim Stoppen der Bewegung (m/s²)")]
        [SerializeField] private float _deceleration = 15.0f;

        [Header("Air Movement")]
        [Tooltip("Kontrolle in der Luft (0 = keine, 1 = volle Kontrolle)")]
        [Range(0f, 1f)]
        [SerializeField] private float _airControl = 0.3f;

        [Tooltip("Gravitationsbeschleunigung (m/s²)")]
        [SerializeField] private float _gravity = 20.0f;

        [Tooltip("Maximale Fallgeschwindigkeit (m/s)")]
        [SerializeField] private float _maxFallSpeed = 50.0f;

        [Header("Jumping")]
        [Tooltip("Sprunghöhe in Metern")]
        [SerializeField] private float _jumpHeight = 2.0f;

        [Tooltip("Zeit bis zum Höhepunkt des Sprungs (Sekunden)")]
        [SerializeField] private float _jumpDuration = 0.4f;

        [Tooltip("Coyote Time - Zeit nach Verlassen des Bodens, in der noch gesprungen werden kann (Sekunden)")]
        [SerializeField] private float _coyoteTime = 0.15f;

        [Tooltip("Jump Buffer - Zeit, in der ein Jump-Input gespeichert wird (Sekunden)")]
        [SerializeField] private float _jumpBufferTime = 0.1f;

        [Header("Ground Detection")]
        [Tooltip("Distanz für Ground Check Raycast (m)")]
        [SerializeField] private float _groundCheckDistance = 0.2f;

        [Tooltip("Radius für Ground Check SphereCast (m)")]
        [SerializeField] private float _groundCheckRadius = 0.3f;

        [Tooltip("Layer Mask für Ground Detection")]
        [SerializeField] private LayerMask _groundLayers = 1; // Default Layer

        [Tooltip("Maximaler Winkel für begehbare Oberflächen (Grad)")]
        [Range(0f, 90f)]
        [SerializeField] private float _maxSlopeAngle = 45.0f;

        [Header("Rotation")]
        [Tooltip("Rotationsgeschwindigkeit (Grad/Sekunde)")]
        [SerializeField] private float _rotationSpeed = 720.0f;

        [Tooltip("Ob der Character sich zur Bewegungsrichtung dreht")]
        [SerializeField] private bool _rotateTowardsMovement = true;

        [Header("Step Detection")]
        [Tooltip("Maximale Stufenhöhe, die automatisch überwunden wird (m)")]
        [SerializeField] private float _maxStepHeight = 0.3f;

        [Tooltip("Minimale Stufentiefe für Step-Up (m)")]
        [SerializeField] private float _minStepDepth = 0.1f;

        [Header("Slope Sliding")]
        [Tooltip("Geschwindigkeit beim Rutschen auf zu steilen Oberflächen (m/s)")]
        [SerializeField] private float _slopeSlideSpeed = 8.0f;

        [Tooltip("Wenn aktiviert, skaliert die Rutsch-Geschwindigkeit mit der Steilheit des Hangs (steilere Hänge = schneller). Wenn deaktiviert, wird immer die feste SlopeSlideSpeed verwendet.")]
        [SerializeField] private bool _useSlopeDependentSlideSpeed = true;

        // Interface Implementation
        public float WalkSpeed => _walkSpeed;
        public float RunSpeed => _runSpeed;
        public float Acceleration => _acceleration;
        public float Deceleration => _deceleration;
        public float AirControl => _airControl;
        public float Gravity => _gravity;
        public float MaxFallSpeed => _maxFallSpeed;
        public float JumpHeight => _jumpHeight;
        public float JumpDuration => _jumpDuration;
        public float CoyoteTime => _coyoteTime;
        public float JumpBufferTime => _jumpBufferTime;
        public float GroundCheckDistance => _groundCheckDistance;
        public float GroundCheckRadius => _groundCheckRadius;
        public LayerMask GroundLayers => _groundLayers;
        public float MaxSlopeAngle => _maxSlopeAngle;
        public float RotationSpeed => _rotationSpeed;
        public bool RotateTowardsMovement => _rotateTowardsMovement;
        public float MaxStepHeight => _maxStepHeight;
        public float MinStepDepth => _minStepDepth;
        public float SlopeSlideSpeed => _slopeSlideSpeed;
        public bool UseSlopeDependentSlideSpeed => _useSlopeDependentSlideSpeed;

        /// <summary>
        /// Berechnet die initiale Sprunggeschwindigkeit basierend auf Sprunghöhe und -dauer.
        /// Formel: v = 2 * h / t (für parabolische Flugbahn)
        /// </summary>
        public float CalculateJumpVelocity()
        {
            // v = 2 * h / t
            return (2f * _jumpHeight) / _jumpDuration;
        }

        /// <summary>
        /// Berechnet die Gravitation basierend auf Sprunghöhe und -dauer.
        /// Formel: g = 2 * h / t² (für konsistente Sprungphysik)
        /// </summary>
        public float CalculateJumpGravity()
        {
            // g = 2 * h / t²
            return (2f * _jumpHeight) / (_jumpDuration * _jumpDuration);
        }

        /// <summary>
        /// Validiert die Konfiguration und gibt Warnungen aus.
        /// </summary>
        private void OnValidate()
        {
            // Stelle sicher, dass Walk Speed <= Run Speed
            if (_walkSpeed > _runSpeed)
            {
                Debug.LogWarning($"[MovementConfig] Walk Speed ({_walkSpeed}) sollte nicht größer als Run Speed ({_runSpeed}) sein.");
            }

            // Stelle sicher, dass Ground Check Distance positiv ist
            if (_groundCheckDistance <= 0)
            {
                _groundCheckDistance = 0.1f;
                Debug.LogWarning("[MovementConfig] Ground Check Distance muss positiv sein. Auf 0.1 gesetzt.");
            }

            // Stelle sicher, dass Jump Height und Duration positiv sind
            if (_jumpHeight <= 0 || _jumpDuration <= 0)
            {
                Debug.LogWarning("[MovementConfig] Jump Height und Jump Duration müssen positiv sein.");
            }
        }
    }
}
