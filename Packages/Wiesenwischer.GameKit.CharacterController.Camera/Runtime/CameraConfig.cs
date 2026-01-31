using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Camera
{
    /// <summary>
    /// Konfiguration für das Third-Person Kamera-System.
    /// </summary>
    [CreateAssetMenu(
        fileName = "CameraConfig",
        menuName = "Wiesenwischer/GameKit/Camera Config",
        order = 2)]
    public class CameraConfig : ScriptableObject
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

        [Header("Sensitivity")]
        [Tooltip("Horizontale Drehgeschwindigkeit (Grad/Sekunde)")]
        [Range(50f, 500f)]
        public float HorizontalSensitivity = 200f;

        [Tooltip("Vertikale Drehgeschwindigkeit (Grad/Sekunde)")]
        [Range(50f, 500f)]
        public float VerticalSensitivity = 150f;

        [Tooltip("Zoom-Geschwindigkeit")]
        [Range(0.5f, 5f)]
        public float ZoomSensitivity = 2f;

        [Header("Vertical Limits")]
        [Tooltip("Minimaler vertikaler Winkel (nach unten schauen)")]
        [Range(-89f, 0f)]
        public float MinVerticalAngle = -40f;

        [Tooltip("Maximaler vertikaler Winkel (nach oben schauen)")]
        [Range(0f, 89f)]
        public float MaxVerticalAngle = 70f;

        [Header("Smoothing")]
        [Tooltip("Follow-Glättung (niedrigere = schneller)")]
        [Range(0f, 1f)]
        public float FollowDamping = 0.1f;

        [Tooltip("Rotations-Glättung")]
        [Range(0f, 1f)]
        public float RotationDamping = 0.05f;

        [Tooltip("Zoom-Glättung")]
        [Range(0f, 1f)]
        public float ZoomDamping = 0.1f;

        [Header("Offset")]
        [Tooltip("Offset vom Zielpunkt")]
        public Vector3 TargetOffset = new Vector3(0f, 1.5f, 0f);

        [Tooltip("Schulter-Offset (für Over-the-Shoulder Ansicht)")]
        public Vector3 ShoulderOffset = Vector3.zero;

        [Header("Collision")]
        [Tooltip("Layer für Kollisionserkennung")]
        public LayerMask CollisionLayers = ~0;

        [Tooltip("Radius für Kollisionserkennung")]
        [Range(0.1f, 1f)]
        public float CollisionRadius = 0.3f;

        [Tooltip("Wie schnell die Kamera bei Kollision näher kommt")]
        [Range(1f, 50f)]
        public float CollisionSnapSpeed = 10f;

        [Tooltip("Wie schnell die Kamera nach Kollision zurückgeht")]
        [Range(0.5f, 10f)]
        public float CollisionRecoverySpeed = 2f;

        [Header("Input")]
        [Tooltip("X-Achse invertieren")]
        public bool InvertX = false;

        [Tooltip("Y-Achse invertieren")]
        public bool InvertY = false;

        [Header("Cursor")]
        [Tooltip("Cursor im Spiel verstecken und sperren")]
        public bool LockCursor = true;

        /// <summary>
        /// Berechnet den angepassten Input basierend auf Invert-Einstellungen.
        /// </summary>
        public Vector2 ProcessInput(Vector2 rawInput)
        {
            return new Vector2(
                InvertX ? -rawInput.x : rawInput.x,
                InvertY ? -rawInput.y : rawInput.y
            );
        }

        /// <summary>
        /// Clampt den vertikalen Winkel auf die konfigurierten Limits.
        /// </summary>
        public float ClampVerticalAngle(float angle)
        {
            return Mathf.Clamp(angle, MinVerticalAngle, MaxVerticalAngle);
        }

        /// <summary>
        /// Clampt den Abstand auf die konfigurierten Limits.
        /// </summary>
        public float ClampDistance(float distance)
        {
            return Mathf.Clamp(distance, MinDistance, MaxDistance);
        }
    }
}
