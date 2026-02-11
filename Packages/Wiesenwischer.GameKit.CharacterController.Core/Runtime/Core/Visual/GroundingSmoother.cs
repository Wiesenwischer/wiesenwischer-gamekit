using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Motor;
using Wiesenwischer.GameKit.CharacterController.Core.Locomotion;

namespace Wiesenwischer.GameKit.CharacterController.Core.Visual
{
    /// <summary>
    /// Glättet visuelle Y-Sprünge bei Step-Ups (Treppen/Kanten).
    /// Versetzt das Model-Child temporär nach unten und löst den Offset per SmoothDamp auf.
    /// Platzierung: Player Root-Object (neben CharacterMotor).
    /// </summary>
    [RequireComponent(typeof(CharacterMotor))]
    public class GroundingSmoother : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Transform des visuellen Modells (Child-Object mit Animator)")]
        [SerializeField] private Transform _modelTransform;

        [Header("Settings")]
        [Tooltip("Smooth-Zeit für Y-Offset-Auflösung (Sekunden). Kleinere Werte = schnelleres Nachziehen.")]
        [SerializeField] private float _smoothTime = 0.075f;

        [Tooltip("Maximaler Y-Sprung der als Step-Up erkannt wird (m). Größere Sprünge werden als Teleport behandelt.")]
        [SerializeField] private float _maxStepDelta = 0.5f;

        [Tooltip("Nur smoothen wenn Character am Boden ist.")]
        [SerializeField] private bool _onlyWhenGrounded = true;

        private float _previousMotorY;
        private float _smoothOffset;
        private float _smoothVelocity;
        private CharacterMotor _motor;
        private CharacterLocomotion _locomotion;

        private void Awake()
        {
            _motor = GetComponent<CharacterMotor>();
            _locomotion = GetComponent<CharacterLocomotion>();
        }

        private void OnEnable()
        {
            _previousMotorY = _motor.TransientPosition.y;
            _smoothOffset = 0f;
            _smoothVelocity = 0f;
        }

        private void LateUpdate()
        {
            float currentY = _motor.TransientPosition.y;
            float deltaY = currentY - _previousMotorY;
            _previousMotorY = currentY;

            if (_modelTransform == null)
                return;

            // Teleport-Check: Zu großer Sprung → kein Smoothing
            if (Mathf.Abs(deltaY) > _maxStepDelta)
            {
                _smoothOffset = 0f;
                _smoothVelocity = 0f;
                ApplyOffset();
                return;
            }

            // Airborne-Check: In der Luft → Offset sofort auflösen
            if (_onlyWhenGrounded && !_locomotion.IsGrounded)
            {
                _smoothOffset = 0f;
                _smoothVelocity = 0f;
                ApplyOffset();
                return;
            }

            // Landing-Check: Gerade gelandet → kein Offset aufbauen
            if (_motor.JustLanded)
            {
                _smoothOffset = 0f;
                _smoothVelocity = 0f;
                ApplyOffset();
                return;
            }

            // Step-Up erkannt: Offset aufbauen
            if (Mathf.Abs(deltaY) > 0.001f)
                _smoothOffset -= deltaY;

            // Offset über Zeit zu 0 auflösen
            _smoothOffset = Mathf.SmoothDamp(_smoothOffset, 0f, ref _smoothVelocity, _smoothTime);

            // Snap bei Minimal-Offset (kein ewiges Micro-Smoothing)
            if (Mathf.Abs(_smoothOffset) < 0.001f)
            {
                _smoothOffset = 0f;
                _smoothVelocity = 0f;
            }

            ApplyOffset();
        }

        private void ApplyOffset()
        {
            _modelTransform.localPosition = new Vector3(0f, _smoothOffset, 0f);
        }

        // --- Testbare Kern-Methode (internal für Unit Tests) ---

        internal struct SmootherState
        {
            public float SmoothOffset;
            public float SmoothVelocity;
        }

        internal static SmootherState CalculateOffset(
            float deltaY,
            bool isGrounded,
            bool justLanded,
            float maxStepDelta,
            bool onlyWhenGrounded,
            float smoothTime,
            SmootherState state,
            float deltaTime)
        {
            // Teleport-Check
            if (Mathf.Abs(deltaY) > maxStepDelta)
                return new SmootherState { SmoothOffset = 0f, SmoothVelocity = 0f };

            // Airborne-Check
            if (onlyWhenGrounded && !isGrounded)
                return new SmootherState { SmoothOffset = 0f, SmoothVelocity = 0f };

            // Landing-Check
            if (justLanded)
                return new SmootherState { SmoothOffset = 0f, SmoothVelocity = 0f };

            // Step-Up: Offset aufbauen
            if (Mathf.Abs(deltaY) > 0.001f)
                state.SmoothOffset -= deltaY;

            // SmoothDamp
            float velocity = state.SmoothVelocity;
            state.SmoothOffset = Mathf.SmoothDamp(
                state.SmoothOffset, 0f, ref velocity, smoothTime, Mathf.Infinity, deltaTime);
            state.SmoothVelocity = velocity;

            // Snap
            if (Mathf.Abs(state.SmoothOffset) < 0.001f)
            {
                state.SmoothOffset = 0f;
                state.SmoothVelocity = 0f;
            }

            return state;
        }
    }
}
