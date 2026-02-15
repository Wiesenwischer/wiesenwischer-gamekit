using UnityEngine;

namespace Wiesenwischer.GameKit.Camera
{
    /// <summary>
    /// Definiert einen kompletten Camera-Style als konfigurierbares Asset.
    /// Enthält Parameter für alle Standard-Behaviours.
    /// CameraBrain.SetPreset() wendet die Werte auf die aktiven Behaviours an.
    /// </summary>
    [CreateAssetMenu(
        fileName = "CameraPreset",
        menuName = "Wiesenwischer/GameKit/Camera Preset",
        order = 1)]
    public class CameraPreset : ScriptableObject
    {
        [Header("General")]
        [Tooltip("Beschreibung des Preset-Stils")]
        public string Description;

        [Tooltip("Standard Field of View")]
        [Range(40f, 100f)]
        public float DefaultFov = 60f;

        [Header("Orbit Mode")]
        [Tooltip("AlwaysOn = Maus steuert immer (Action Combat). ButtonActivated = Nur bei LMB/RMB (Classic MMO).")]
        public OrbitActivation OrbitActivation = OrbitActivation.AlwaysOn;

        [Header("Orbit")]
        [Tooltip("Minimaler Pitch-Winkel (nach unten)")]
        [Range(-89f, 0f)]
        public float PitchMin = -40f;

        [Tooltip("Maximaler Pitch-Winkel (nach oben)")]
        [Range(0f, 89f)]
        public float PitchMax = 70f;

        [Header("Zoom")]
        [Tooltip("Standard-Abstand zum Target")]
        [Range(1f, 20f)]
        public float DefaultDistance = 5f;

        [Tooltip("Minimaler Abstand")]
        [Range(0.5f, 10f)]
        public float MinDistance = 2f;

        [Tooltip("Maximaler Abstand")]
        [Range(5f, 30f)]
        public float MaxDistance = 15f;

        [Tooltip("Zoom-Glättung (SmoothDamp-Zeit)")]
        [Range(0f, 1f)]
        public float ZoomDamping = 0.1f;

        [Header("Collision")]
        [Tooltip("Radius für SphereCast")]
        [Range(0.1f, 1f)]
        public float CollisionRadius = 0.3f;

        [Tooltip("Geschwindigkeit der Kamera-Rückkehr nach Collision")]
        [Range(0.5f, 20f)]
        public float CollisionRecoverySpeed = 5f;

        [Header("Inertia")]
        [Tooltip("Inertia aktiviert?")]
        public bool InertiaEnabled = true;

        [Tooltip("Steifigkeit (wie schnell die Kamera folgt)")]
        [Range(1f, 50f)]
        public float InertiaStiffness = 15f;

        [Tooltip("Dämpfung (reduziert Oszillation)")]
        [Range(0.5f, 0.99f)]
        public float InertiaDamping = 0.85f;

        [Tooltip("Maximaler Offset in Metern")]
        [Range(0.1f, 5f)]
        public float InertiaMaxOffset = 1.5f;

        [Header("Recenter")]
        [Tooltip("Auto-Recenter aktiviert?")]
        public bool RecenterEnabled = true;

        [Tooltip("Verzögerung in Sekunden bevor Recenter startet")]
        [Range(0f, 10f)]
        public float RecenterDelay = 2f;

        [Tooltip("Recenter-Geschwindigkeit in Grad/Sekunde")]
        [Range(10f, 360f)]
        public float RecenterSpeed = 90f;

        [Tooltip("Minimum Character-Geschwindigkeit für Recenter")]
        [Range(0f, 5f)]
        public float RecenterMinSpeed = 0.5f;

        [Header("Shoulder Offset")]
        [Tooltip("Shoulder Offset aktiviert?")]
        public bool ShoulderEnabled = false;

        [Tooltip("Seitlicher Versatz (positiv = rechts)")]
        [Range(-2f, 2f)]
        public float ShoulderOffsetX = 0.5f;

        [Tooltip("Vertikaler Versatz")]
        [Range(-1f, 1f)]
        public float ShoulderOffsetY = 0f;

        [Tooltip("Smooth-Zeit beim Seiten-Wechsel")]
        [Range(0f, 1f)]
        public float ShoulderSwitchDamping = 0.2f;

        [Header("Dynamic Orbit Center")]
        [Tooltip("Dynamic Orbit Center aktiviert?")]
        public bool DynamicOrbitEnabled = false;

        [Tooltip("Forward Bias bei Bewegung (wie weit vor dem Character)")]
        [Range(0f, 3f)]
        public float ForwardBias = 0.5f;

        [Tooltip("Smooth-Zeit für Orbit-Center-Verschiebung")]
        [Range(0f, 0.5f)]
        public float OrbitCenterDamping = 0.1f;

        [Header("Soft Targeting")]
        [Tooltip("Soft Targeting aktiviert?")]
        public bool SoftTargetingEnabled = false;

        [Tooltip("Stärke des Movement-Forward-Bias (Grad)")]
        [Range(0f, 30f)]
        public float MovementBiasStrength = 5f;

        [Tooltip("Stärke des Target-Bias (Grad)")]
        [Range(0f, 45f)]
        public float TargetBiasStrength = 15f;

        [Tooltip("Maximaler Radius für Soft Target Erfassung")]
        [Range(0f, 50f)]
        public float SoftTargetRange = 20f;

        [Tooltip("Smooth-Zeit für Bias-Übergänge")]
        [Range(0f, 1f)]
        public float SoftTargetDamping = 0.15f;
    }
}
