using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Core.Motor
{
    /// <summary>
    /// Konfiguration für den CharacterMotor.
    /// ScriptableObject für einfache Bearbeitung im Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterMotorConfig", menuName = "GameKit/Character/Motor Config")]
    public class CharacterMotorConfig : ScriptableObject
    {
        #region Step Handling

        [Header("Step Handling")]
        [Tooltip("Methode für Step-Handling")]
        [SerializeField] private StepHandlingMethod _stepHandling = StepHandlingMethod.Standard;

        [Tooltip("Maximale Höhe einer Stufe, die überschritten werden kann")]
        [SerializeField] private float _maxStepHeight = 0.3f;

        [Tooltip("Minimale Tiefe einer Stufe für gültiges Step-Up")]
        [SerializeField] private float _minStepDepth = 0.1f;

        #endregion

        #region Slope Handling

        [Header("Slope Handling")]
        [Tooltip("Maximaler begehbarer Hangwinkel in Grad")]
        [Range(0f, 89f)]
        [SerializeField] private float _maxSlopeAngle = 55f;

        #endregion

        #region Ground Detection

        [Header("Ground Detection")]
        [Tooltip("Zusätzliche Distanz für Ground-Probing")]
        [SerializeField] private float _groundDetectionExtraDistance = 0.05f;

        [Tooltip("Layer-Maske für Boden-Erkennung")]
        [SerializeField] private LayerMask _groundLayers = ~0;

        #endregion

        #region Ledge Handling

        [Header("Ledge Handling")]
        [Tooltip("Aktiviert Ledge-Erkennung und -Handling")]
        [SerializeField] private bool _ledgeHandling = true;

        [Tooltip("Maximale stabile Distanz von einer Kante")]
        [SerializeField] private float _maxStableDistanceFromLedge = 0.5f;

        [Tooltip("Maximaler Winkelunterschied zwischen Inner- und Outer-Normal")]
        [Range(0f, 180f)]
        [SerializeField] private float _maxDenivelationAngle = 60f;

        #endregion

        #region Public Properties

        /// <summary>Methode für Step-Handling.</summary>
        public StepHandlingMethod StepHandling => _stepHandling;

        /// <summary>Maximale Höhe einer Stufe, die überschritten werden kann.</summary>
        public float MaxStepHeight => _maxStepHeight;

        /// <summary>Minimale Tiefe einer Stufe für gültiges Step-Up.</summary>
        public float MinStepDepth => _minStepDepth;

        /// <summary>Maximaler begehbarer Hangwinkel in Grad.</summary>
        public float MaxSlopeAngle => _maxSlopeAngle;

        /// <summary>Zusätzliche Distanz für Ground-Probing.</summary>
        public float GroundDetectionExtraDistance => _groundDetectionExtraDistance;

        /// <summary>Layer-Maske für Boden-Erkennung.</summary>
        public LayerMask GroundLayers => _groundLayers;

        /// <summary>Aktiviert Ledge-Erkennung und -Handling.</summary>
        public bool LedgeHandling => _ledgeHandling;

        /// <summary>Maximale stabile Distanz von einer Kante.</summary>
        public float MaxStableDistanceFromLedge => _maxStableDistanceFromLedge;

        /// <summary>Maximaler Winkelunterschied zwischen Inner- und Outer-Normal.</summary>
        public float MaxDenivelationAngle => _maxDenivelationAngle;

        #endregion

        #region Runtime Creation

        /// <summary>
        /// Erstellt eine Runtime-Instanz mit Standardwerten.
        /// </summary>
        public static CharacterMotorConfig CreateDefault()
        {
            var config = CreateInstance<CharacterMotorConfig>();
            config.name = "Default Motor Config (Runtime)";
            return config;
        }

        /// <summary>
        /// Erstellt eine Runtime-Instanz mit benutzerdefinierten Werten.
        /// </summary>
        public static CharacterMotorConfig CreateCustom(
            StepHandlingMethod stepHandling = StepHandlingMethod.Standard,
            float maxStepHeight = 0.3f,
            float minStepDepth = 0.1f,
            float maxSlopeAngle = 55f,
            float groundDetectionExtraDistance = 0.05f,
            LayerMask? groundLayers = null,
            bool ledgeHandling = true,
            float maxStableDistanceFromLedge = 0.5f,
            float maxDenivelationAngle = 60f)
        {
            var config = CreateInstance<CharacterMotorConfig>();
            config.name = "Custom Motor Config (Runtime)";

            config._stepHandling = stepHandling;
            config._maxStepHeight = maxStepHeight;
            config._minStepDepth = minStepDepth;
            config._maxSlopeAngle = maxSlopeAngle;
            config._groundDetectionExtraDistance = groundDetectionExtraDistance;
            config._groundLayers = groundLayers ?? ~0;
            config._ledgeHandling = ledgeHandling;
            config._maxStableDistanceFromLedge = maxStableDistanceFromLedge;
            config._maxDenivelationAngle = maxDenivelationAngle;

            return config;
        }

        #endregion

        #region Validation

        private void OnValidate()
        {
            _maxStepHeight = Mathf.Max(0f, _maxStepHeight);
            _minStepDepth = Mathf.Max(0f, _minStepDepth);
            _groundDetectionExtraDistance = Mathf.Max(0f, _groundDetectionExtraDistance);
            _maxStableDistanceFromLedge = Mathf.Max(0f, _maxStableDistanceFromLedge);
        }

        #endregion
    }
}
