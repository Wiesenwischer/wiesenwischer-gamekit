using UnityEngine;

namespace Wiesenwischer.GameKit.Camera
{
    /// <summary>
    /// Runtime-konfigurierbare Input-Settings für die Kamera.
    /// Als ScriptableObject: Änderungen über UI-Slider wirken sofort.
    /// </summary>
    public class CameraInputSettings : ScriptableObject
    {
        [Header("Mouse Sensitivity")]
        [Range(0.1f, 10f)]
        [Tooltip("Horizontale Maus-Sensitivität")]
        public float MouseSensitivityX = 1f;

        [Range(0.1f, 10f)]
        [Tooltip("Vertikale Maus-Sensitivität")]
        public float MouseSensitivityY = 1f;

        [Header("Gamepad Sensitivity")]
        [Range(0.1f, 10f)]
        [Tooltip("Horizontale Gamepad-Stick-Sensitivität")]
        public float GamepadSensitivityX = 1f;

        [Range(0.1f, 10f)]
        [Tooltip("Vertikale Gamepad-Stick-Sensitivität")]
        public float GamepadSensitivityY = 1f;

        [Header("Scroll")]
        [Range(0.1f, 5f)]
        [Tooltip("Zoom-Scroll-Sensitivität")]
        public float ScrollSensitivity = 1f;

        [Header("Inversion")]
        [Tooltip("Y-Achse invertieren")]
        public bool InvertY;

        [Header("FOV-Based Scaling")]
        [Tooltip("Referenz-FOV für Sensitivity-Skalierung (Grad)")]
        public float BaseFov = 60f;

        [Tooltip("Sensitivity automatisch mit FOV skalieren")]
        public bool EnableFovScaling = true;
    }
}
