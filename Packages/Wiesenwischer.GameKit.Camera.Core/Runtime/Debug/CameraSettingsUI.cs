using UnityEngine;

namespace Wiesenwischer.GameKit.Camera
{
    /// <summary>
    /// Minimales Runtime-UI für Kamera-Einstellungen.
    /// Öffnet/schließt mit einer Taste (Standard: F2).
    /// Ändert CameraInputSettings direkt — wirkt sofort.
    /// </summary>
    public class CameraSettingsUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CameraInputSettings _settings;

        [Header("Input")]
        [SerializeField] private KeyCode _toggleKey = KeyCode.F2;

        private bool _visible;
        private Rect _windowRect = new Rect(0, 0, 300, 0);
        private bool _needsCenter = true;
        private CameraInputPipeline _pipeline;

        private void Awake()
        {
            _pipeline = FindObjectOfType<CameraInputPipeline>();

            if (_settings == null && _pipeline != null)
            {
                // Reflection-Fallback: Feld direkt lesen
                var field = typeof(CameraInputPipeline).GetField("_inputSettings",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                    _settings = field.GetValue(_pipeline) as CameraInputSettings;
            }

            if (_settings == null)
            {
                Debug.LogWarning("[CameraSettingsUI] Keine CameraInputSettings gefunden. " +
                    "Bitte im Inspector zuweisen.");
                enabled = false;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(_toggleKey))
            {
                _visible = !_visible;

                if (_visible)
                    _needsCenter = true;

                // Pipeline-Input unterdrücken solange UI offen
                if (_pipeline != null)
                    _pipeline.InputSuppressed = _visible;

                // Cursor freigeben wenn UI offen
                if (_visible)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
        }

        private void OnGUI()
        {
            if (!_visible || _settings == null) return;

            _windowRect = GUILayout.Window(
                947201, _windowRect, DrawWindow, "Camera Settings [F2]");

            if (_needsCenter && _windowRect.height > 0)
            {
                _windowRect.x = (Screen.width - _windowRect.width) * 0.5f;
                _windowRect.y = (Screen.height - _windowRect.height) * 0.5f;
                _needsCenter = false;
            }
        }

        private void DrawWindow(int id)
        {
            GUILayout.Space(4);

            // Mouse
            GUILayout.Label("Mouse", GUI.skin.box);
            _settings.MouseSensitivityX = DrawSlider("Sensitivity X",
                _settings.MouseSensitivityX, 0.1f, 5f);
            _settings.MouseSensitivityY = DrawSlider("Sensitivity Y",
                _settings.MouseSensitivityY, 0.1f, 5f);

            GUILayout.Space(4);

            // Gamepad
            GUILayout.Label("Gamepad", GUI.skin.box);
            _settings.GamepadSensitivityX = DrawSlider("Sensitivity X",
                _settings.GamepadSensitivityX, 0.1f, 5f);
            _settings.GamepadSensitivityY = DrawSlider("Sensitivity Y",
                _settings.GamepadSensitivityY, 0.1f, 5f);

            GUILayout.Space(4);

            // Zoom
            GUILayout.Label("Zoom", GUI.skin.box);
            _settings.ScrollSensitivity = DrawSlider("Zoom Speed",
                _settings.ScrollSensitivity, 0.1f, 5f);

            GUILayout.Space(4);

            // Toggles
            _settings.InvertY = GUILayout.Toggle(_settings.InvertY, " Invert Y");
            _settings.EnableFovScaling = GUILayout.Toggle(_settings.EnableFovScaling, " FOV Scaling");

            GUILayout.Space(8);

            if (GUILayout.Button("Reset"))
            {
                _settings.MouseSensitivityX = 1f;
                _settings.MouseSensitivityY = 1f;
                _settings.GamepadSensitivityX = 1f;
                _settings.GamepadSensitivityY = 1f;
                _settings.ScrollSensitivity = 1f;
                _settings.InvertY = false;
                _settings.EnableFovScaling = true;
            }

            GUI.DragWindow();
        }

        private float DrawSlider(string label, float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(100));
            float result = GUILayout.HorizontalSlider(value, min, max);
            GUILayout.Label(result.ToString("F2"), GUILayout.Width(36));
            GUILayout.EndHorizontal();
            return result;
        }
    }
}
