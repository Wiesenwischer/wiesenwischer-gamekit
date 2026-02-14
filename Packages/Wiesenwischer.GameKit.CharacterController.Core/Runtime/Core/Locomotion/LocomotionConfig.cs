using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.StateMachine;

namespace Wiesenwischer.GameKit.CharacterController.Core.Locomotion
{
    /// <summary>
    /// ScriptableObject für Locomotion-Konfiguration.
    /// Enthält alle Parameter für Charakterbewegung, Springen und Ground Detection.
    /// Wird von CharacterLocomotion und anderen Locomotion-Typen verwendet.
    /// </summary>
    [CreateAssetMenu(fileName = "LocomotionConfig", menuName = "Wiesenwischer/GameKit/Locomotion Config", order = 0)]
    public class LocomotionConfig : ScriptableObject, ILocomotionConfig
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

        [Tooltip("Sprint-Multiplikator relativ zu Run (z.B. 1.5 = 50% schneller als Run)")]
        [SerializeField] private float _sprintMultiplier = 1.5f;

        [Header("Air Movement")]
        [Tooltip("Steuerbarkeit in der Luft (0 = keine Steuerung, 1 = volle Kontrolle). Beeinflusst wie stark der Spieler die Richtung in der Luft ändern kann.")]
        [Range(0f, 1f)]
        [SerializeField] private float _airControl = 0.3f;

        [Tooltip("Wie schnell horizontales Momentum in der Luft verloren geht (0 = kein Drag/volles Momentum, 1 = Abbremsung wie am Boden)")]
        [Range(0f, 1f)]
        [SerializeField] private float _airDrag = 0.8f;

        [Tooltip("Minimale Falldistanz in Metern, ab der der Character als fallend erkannt wird. Drops darunter werden ignoriert (Treppen, kleine Kanten).")]
        [SerializeField] private float _minFallDistance = 0.5f;

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

        [Tooltip("Wenn aktiviert, kann der Sprung durch frühes Loslassen abgebrochen werden (niedrigerer Sprung). Wenn deaktiviert, springt der Character immer die volle Höhe.")]
        [SerializeField] private bool _useVariableJump = true;

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

        [Tooltip("Ground Detection Strategy: Motor = KCC-Standard (IsStableOnGround), Collider = SphereCast von Capsule-Unterseite (Genshin-Style)")]
        [SerializeField] private GroundDetectionMode _groundDetection = GroundDetectionMode.Motor;

        [Tooltip("Fall Detection Strategy: Motor = SnappingPrevented + IsStable, Collider = Raycast von Capsule-Unterseite (Genshin-Style)")]
        [SerializeField] private FallDetectionMode _fallDetection = FallDetectionMode.Motor;

        [Tooltip("Raycast-Distanz von Capsule-Unterseite nach unten für Fall-Erkennung (m). " +
                 "Wenn kein Boden innerhalb dieser Distanz → Character fällt. Nur bei Fall Detection = Collider.")]
        [SerializeField] private float _groundToFallRayDistance = 1.0f;

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

        [Tooltip("Ob Treppen-Erkennung mit automatischer Geschwindigkeitsreduktion aktiviert ist")]
        [SerializeField] private bool _stairSpeedReductionEnabled = true;

        [Tooltip("Geschwindigkeitsreduktion auf Treppen (0=keine, 1=Stillstand). Wird angewendet wenn mehrere Steps in kurzer Zeit erkannt werden.")]
        [Range(0f, 1f)]
        [SerializeField] private float _stairSpeedReduction = 0.3f;

        [Header("Ledge & Ground Snapping")]
        [Tooltip("Ob Ledge Detection aktiviert ist (zusätzliche Raycasts für Kanten-Erkennung)")]
        [SerializeField] private bool _ledgeDetectionEnabled = true;

        [Tooltip("Maximale Distanz zur Kante, bei der der Character noch stabil steht (m). Typisch: Capsule Radius")]
        [SerializeField] private float _maxStableDistanceFromLedge = 0.5f;

        [Tooltip("Maximaler Winkelunterschied zwischen Oberflächen für Ground Snapping (Grad). Verhindert Kleben an steilen Kanten")]
        [Range(1f, 180f)]
        [SerializeField] private float _maxStableDenivelationAngle = 60f;

        [Tooltip("Geschwindigkeit ab der Ground Snapping an Kanten deaktiviert wird (m/s). " +
                 "Unter diesem Wert snappt der Character am Boden; darüber löst er sich von der Kante. " +
                 "Sollte höher als Sprint-Speed sein, damit normale Bewegung immer snappt.")]
        [SerializeField] private float _maxVelocityForLedgeSnap = 10f;

        [Header("Stopping")]
        [Tooltip("Deceleration beim Stoppen aus Walk (m/s²). Niedrigerer Wert = längerer Bremsweg.")]
        [SerializeField] private float _lightStopDeceleration = 12.0f;

        [Tooltip("Deceleration beim Stoppen aus Run (m/s²). Niedrigerer Wert = längerer Bremsweg.")]
        [SerializeField] private float _mediumStopDeceleration = 10.0f;

        [Tooltip("Deceleration beim Stoppen aus Sprint (m/s²). Niedrigerer Wert = längerer Bremsweg.")]
        [SerializeField] private float _hardStopDeceleration = 8.0f;

        [Header("Slope Animation")]
        [Tooltip("Animation auf Rampen/Treppen immer mit voller Geschwindigkeit abspielen (kompensiert geometrische Speed-Reduktion durch Steigung).")]
        [SerializeField] private bool _fullAnimSpeedOnTerrain = true;

        [Header("Slope Speed")]
        [Tooltip("Maximale Speed-Reduktion bergauf bei steilstem begehbaren Winkel (0=keine Reduktion, 1=Stillstand). Skaliert linear mit dem Slope-Winkel.")]
        [Range(0f, 1f)]
        [SerializeField] private float _uphillSpeedPenalty = 0.3f;

        [Tooltip("Speed-Bonus bergab bei steilstem begehbaren Winkel (positiv=schneller, 0=kein Effekt, negativ=langsamer). Skaliert linear mit dem Slope-Winkel.")]
        [Range(-0.5f, 0.5f)]
        [SerializeField] private float _downhillSpeedBonus = 0.1f;

        [Header("Slope Sliding")]
        [Tooltip("Geschwindigkeit beim Rutschen auf zu steilen Oberflächen (m/s)")]
        [SerializeField] private float _slopeSlideSpeed = 12.0f;

        [Tooltip("Wenn aktiviert, skaliert die Rutsch-Geschwindigkeit mit der Steilheit des Hangs (steilere Hänge = schneller). Wenn deaktiviert, wird immer die feste SlopeSlideSpeed verwendet.")]
        [SerializeField] private bool _useSlopeDependentSlideSpeed = true;

        [Tooltip("Beschleunigung beim Eingleiten in den Slide (m/s²)")]
        [Range(1f, 30f)]
        [SerializeField] private float _slideAcceleration = 15.0f;

        [Tooltip("Seitliche Lenkkraft während des Slidings (0 = keine, 1 = voll)")]
        [Range(0f, 1f)]
        [SerializeField] private float _slideSteerStrength = 0.3f;

        [Tooltip("Hysterese-Winkel für Slide-Exit (Grad). Verhindert Flackern am Grenzwinkel.")]
        [Range(0f, 10f)]
        [SerializeField] private float _slideExitHysteresis = 3.0f;

        [Tooltip("Ob aus dem Slide gesprungen werden kann")]
        [SerializeField] private bool _canJumpFromSlide = true;

        [Tooltip("Sprungkraft-Multiplikator beim Abspringen aus dem Slide")]
        [Range(0f, 1f)]
        [SerializeField] private float _slideJumpForceMultiplier = 0.7f;

        [Tooltip("Mindestzeit im Slide-State (Sekunden). Verhindert Flackern bei kurzzeitigem Kontakt.")]
        [Range(0f, 1f)]
        [SerializeField] private float _minSlideTime = 0.2f;

        [Header("Landing")]
        [Tooltip("Fallgeschwindigkeit unter der sofort weitergelaufen werden kann (m/s)")]
        [SerializeField] private float _softLandingThreshold = 5.0f;

        [Tooltip("Fallgeschwindigkeit ab der maximale Recovery-Zeit gilt (m/s)")]
        [SerializeField] private float _hardLandingThreshold = 15.0f;

        [Tooltip("Recovery-Zeit bei weicher Landung (Sekunden)")]
        [SerializeField] private float _softLandingDuration = 0.1f;

        [Tooltip("Recovery-Zeit bei harter Landung (Sekunden)")]
        [SerializeField] private float _hardLandingDuration = 0.4f;

        [Header("Landing Roll")]
        [Tooltip("Roll aktivieren/deaktivieren (false = immer HardLanding)")]
        [SerializeField] private bool _rollEnabled = true;

        [Tooltip("Trigger-Modus: MovementInput (automatisch) oder ButtonPress (Taste)")]
        [SerializeField] private RollTriggerMode _rollTriggerMode = RollTriggerMode.MovementInput;

        [Tooltip("Geschwindigkeits-Multiplikator relativ zu RunSpeed (0.5-2.0)")]
        [Range(0.5f, 2.0f)]
        [SerializeField] private float _rollSpeedModifier = 1.0f;

        [Header("Crouching")]
        [Tooltip("Capsule-Höhe beim Crouchen (m)")]
        [SerializeField] private float _crouchHeight = 1.2f;

        [Tooltip("Capsule-Höhe im Stehen (m) — Motor-Default")]
        [SerializeField] private float _standingHeight = 2.0f;

        [Tooltip("Bewegungsgeschwindigkeit beim Crouchen (m/s)")]
        [SerializeField] private float _crouchSpeed = 2.5f;

        [Tooltip("Beschleunigung beim Crouchen (m/s²)")]
        [SerializeField] private float _crouchAcceleration = 8.0f;

        [Tooltip("Verzögerung beim Crouchen (m/s²)")]
        [SerializeField] private float _crouchDeceleration = 10.0f;

        [Tooltip("Dauer der Capsule-Höhen-Transition (s)")]
        [SerializeField] private float _crouchTransitionDuration = 0.25f;

        [Tooltip("Sicherheitsabstand für Stand-Up-Check (m)")]
        [SerializeField] private float _crouchHeadClearanceMargin = 0.1f;

        [Tooltip("Ob aus dem Crouch gesprungen werden kann")]
        [SerializeField] private bool _canJumpFromCrouch = true;

        [Tooltip("Ob Sprint den Crouch automatisch beendet")]
        [SerializeField] private bool _canSprintFromCrouch = true;

        [Tooltip("Reduzierte Step-Höhe im Crouch (m), -1 = Motor-Default")]
        [SerializeField] private float _crouchStepHeight = 0.2f;

        // Interface Implementation
        public float WalkSpeed => _walkSpeed;
        public float RunSpeed => _runSpeed;
        public float Acceleration => _acceleration;
        public float Deceleration => _deceleration;
        public float SprintMultiplier => _sprintMultiplier;
        public float AirControl => _airControl;
        public float AirDrag => _airDrag;
        public float MinFallDistance => _minFallDistance;
        public float Gravity => _gravity;
        public float MaxFallSpeed => _maxFallSpeed;
        public float JumpHeight => _jumpHeight;
        public float JumpDuration => _jumpDuration;
        public float CoyoteTime => _coyoteTime;
        public float JumpBufferTime => _jumpBufferTime;
        public bool UseVariableJump => _useVariableJump;
        public float GroundCheckDistance => _groundCheckDistance;
        public float GroundCheckRadius => _groundCheckRadius;
        public LayerMask GroundLayers => _groundLayers;
        public float MaxSlopeAngle => _maxSlopeAngle;
        public GroundDetectionMode GroundDetection => _groundDetection;
        public FallDetectionMode FallDetection => _fallDetection;
        public float GroundToFallRayDistance => _groundToFallRayDistance;
        public float RotationSpeed => _rotationSpeed;
        public bool RotateTowardsMovement => _rotateTowardsMovement;
        public float MaxStepHeight => _maxStepHeight;
        public float MinStepDepth => _minStepDepth;
        public bool StairSpeedReductionEnabled => _stairSpeedReductionEnabled;
        public float StairSpeedReduction => _stairSpeedReduction;
        public bool LedgeDetectionEnabled => _ledgeDetectionEnabled;
        public float MaxStableDistanceFromLedge => _maxStableDistanceFromLedge;
        public float MaxStableDenivelationAngle => _maxStableDenivelationAngle;
        public float MaxVelocityForLedgeSnap => _maxVelocityForLedgeSnap;
        public float LightStopDeceleration => _lightStopDeceleration;
        public float MediumStopDeceleration => _mediumStopDeceleration;
        public float HardStopDeceleration => _hardStopDeceleration;
        public float UphillSpeedPenalty => _uphillSpeedPenalty;
        public bool FullAnimSpeedOnTerrain => _fullAnimSpeedOnTerrain;
        public float DownhillSpeedBonus => _downhillSpeedBonus;
        public float SlopeSlideSpeed => _slopeSlideSpeed;
        public bool UseSlopeDependentSlideSpeed => _useSlopeDependentSlideSpeed;
        public float SlideAcceleration => _slideAcceleration;
        public float SlideSteerStrength => _slideSteerStrength;
        public float SlideExitHysteresis => _slideExitHysteresis;
        public bool CanJumpFromSlide => _canJumpFromSlide;
        public float SlideJumpForceMultiplier => _slideJumpForceMultiplier;
        public float MinSlideTime => _minSlideTime;
        public float SoftLandingThreshold => _softLandingThreshold;
        public float HardLandingThreshold => _hardLandingThreshold;
        public float SoftLandingDuration => _softLandingDuration;
        public float HardLandingDuration => _hardLandingDuration;
        public bool RollEnabled => _rollEnabled;
        public RollTriggerMode RollTriggerMode => _rollTriggerMode;
        public float RollSpeedModifier => _rollSpeedModifier;
        public float CrouchHeight => _crouchHeight;
        public float StandingHeight => _standingHeight;
        public float CrouchSpeed => _crouchSpeed;
        public float CrouchAcceleration => _crouchAcceleration;
        public float CrouchDeceleration => _crouchDeceleration;
        public float CrouchTransitionDuration => _crouchTransitionDuration;
        public float CrouchHeadClearanceMargin => _crouchHeadClearanceMargin;
        public bool CanJumpFromCrouch => _canJumpFromCrouch;
        public bool CanSprintFromCrouch => _canSprintFromCrouch;
        public float CrouchStepHeight => _crouchStepHeight;

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
                Debug.LogWarning($"[LocomotionConfig] Walk Speed ({_walkSpeed}) sollte nicht größer als Run Speed ({_runSpeed}) sein.");
            }

            // Stelle sicher, dass Ground Check Distance positiv ist
            if (_groundCheckDistance <= 0)
            {
                _groundCheckDistance = 0.1f;
                Debug.LogWarning("[LocomotionConfig] Ground Check Distance muss positiv sein. Auf 0.1 gesetzt.");
            }

            // Stelle sicher, dass Jump Height und Duration positiv sind
            if (_jumpHeight <= 0 || _jumpDuration <= 0)
            {
                Debug.LogWarning("[LocomotionConfig] Jump Height und Jump Duration müssen positiv sein.");
            }
        }
    }
}
