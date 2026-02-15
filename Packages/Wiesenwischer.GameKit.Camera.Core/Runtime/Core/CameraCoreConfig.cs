using UnityEngine;

namespace Wiesenwischer.GameKit.Camera
{
    /// <summary>
    /// Konfiguration für das Camera Core System.
    /// Enthält Orbit-, Zoom- und Collision-Parameter die vom CameraBrain gelesen werden.
    /// </summary>
    [CreateAssetMenu(
        fileName = "CameraCoreConfig",
        menuName = "Wiesenwischer/GameKit/Camera Core Config",
        order = 2)]
    public class CameraCoreConfig : ScriptableObject
    {
        [Header("Distance")]
        [Tooltip("Standard-Abstand zum Ziel")]
        [Range(1f, 20f)]
        public float DefaultDistance = 5f;

        [Tooltip("Minimaler Abstand (Zoom In)")]
        [Range(0.5f, 10f)]
        public float MinDistance = 2f;

        [Tooltip("Maximaler Abstand (Zoom Out)")]
        [Range(5f, 30f)]
        public float MaxDistance = 15f;

        [Header("Vertical Limits")]
        [Tooltip("Minimaler vertikaler Winkel (nach unten schauen)")]
        [Range(-89f, 0f)]
        public float MinVerticalAngle = -40f;

        [Tooltip("Maximaler vertikaler Winkel (nach oben schauen)")]
        [Range(0f, 89f)]
        public float MaxVerticalAngle = 70f;

        [Header("Smoothing")]
        [Tooltip("Zoom-Glättung")]
        [Range(0f, 1f)]
        public float ZoomDamping = 0.1f;

        [Header("Collision")]
        [Tooltip("Radius für SphereCast Kollisionserkennung")]
        [Range(0.1f, 1f)]
        public float CollisionRadius = 0.3f;
    }
}
