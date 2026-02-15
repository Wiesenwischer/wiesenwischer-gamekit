using UnityEngine;

namespace Wiesenwischer.GameKit.Camera
{
    /// <summary>
    /// Runtime-Helfer zum Testen von Camera-Presets per Tastendruck.
    /// F1–F4 wechseln zwischen bis zu 4 Presets.
    /// </summary>
    public class CameraPresetSwitcher : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("CameraBrain der konfiguriert wird. Wird automatisch gesucht wenn leer.")]
        [SerializeField] private CameraBrain _brain;

        [Header("Presets (F1–F4)")]
        [SerializeField] private CameraPreset _presetF1;
        [SerializeField] private CameraPreset _presetF2;
        [SerializeField] private CameraPreset _presetF3;
        [SerializeField] private CameraPreset _presetF4;

        [Header("Debug")]
        [Tooltip("Zeigt aktives Preset als Screen-Overlay")]
        [SerializeField] private bool _showOverlay = true;

        private string _activePresetName = "—";
        private float _overlayTimer;

        private void Awake()
        {
            if (_brain == null)
                _brain = FindObjectOfType<CameraBrain>();

            if (_brain == null)
            {
                Debug.LogWarning("[PresetSwitcher] Kein CameraBrain gefunden. Component deaktiviert.");
                enabled = false;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1) && _presetF1 != null)
                ApplyPreset(_presetF1);
            else if (Input.GetKeyDown(KeyCode.F2) && _presetF2 != null)
                ApplyPreset(_presetF2);
            else if (Input.GetKeyDown(KeyCode.F3) && _presetF3 != null)
                ApplyPreset(_presetF3);
            else if (Input.GetKeyDown(KeyCode.F4) && _presetF4 != null)
                ApplyPreset(_presetF4);

            if (_overlayTimer > 0f)
                _overlayTimer -= Time.deltaTime;
        }

        private void ApplyPreset(CameraPreset preset)
        {
            _brain.SetPreset(preset);
            _activePresetName = preset.name;
            _overlayTimer = 3f;
            Debug.Log($"[PresetSwitcher] Preset gewechselt: {preset.name}");
        }

        private void OnGUI()
        {
            if (!_showOverlay) return;

            // Immer die Legende anzeigen
            var legendStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(1f, 1f, 1f, 0.6f) }
            };

            float y = 10f;
            if (_presetF1 != null)
            {
                GUI.Label(new Rect(10, y, 300, 20), $"[F1] {_presetF1.name}", legendStyle);
                y += 18f;
            }
            if (_presetF2 != null)
            {
                GUI.Label(new Rect(10, y, 300, 20), $"[F2] {_presetF2.name}", legendStyle);
                y += 18f;
            }
            if (_presetF3 != null)
            {
                GUI.Label(new Rect(10, y, 300, 20), $"[F3] {_presetF3.name}", legendStyle);
                y += 18f;
            }
            if (_presetF4 != null)
            {
                GUI.Label(new Rect(10, y, 300, 20), $"[F4] {_presetF4.name}", legendStyle);
                y += 18f;
            }

            // Aktives Preset groß anzeigen (nach Wechsel)
            if (_overlayTimer > 0f)
            {
                float alpha = Mathf.Clamp01(_overlayTimer);
                var activeStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 24,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperCenter,
                    normal = { textColor = new Color(0.2f, 1f, 0.4f, alpha) }
                };

                GUI.Label(
                    new Rect(0, Screen.height * 0.15f, Screen.width, 40),
                    _activePresetName,
                    activeStyle);
            }
        }
    }
}
