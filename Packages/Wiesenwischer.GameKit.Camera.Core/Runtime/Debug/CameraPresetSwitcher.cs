using UnityEngine;

namespace Wiesenwischer.GameKit.Camera
{
    /// <summary>
    /// Runtime-Helfer zum Testen von Camera-Presets.
    /// Eine Taste schaltet durch alle zugewiesenen Presets.
    /// HUD zeigt permanent das aktive Preset an.
    /// </summary>
    public class CameraPresetSwitcher : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("CameraBrain der konfiguriert wird. Wird automatisch gesucht wenn leer.")]
        [SerializeField] private CameraBrain _brain;

        [Header("Presets")]
        [Tooltip("Liste aller verfügbaren Presets zum Durchschalten.")]
        [SerializeField] private CameraPreset[] _presets;

        [Header("Input")]
        [Tooltip("Taste zum Durchschalten der Presets.")]
        [SerializeField] private KeyCode _cycleKey = KeyCode.F1;

        [Header("HUD")]
        [Tooltip("Zeigt aktives Preset als HUD-Element an.")]
        [SerializeField] private bool _showHud = true;

        private int _activeIndex = -1;
        private string _activePresetName = "";
        private float _switchFlashTimer;

        private void Awake()
        {
            if (_brain == null)
                _brain = FindObjectOfType<CameraBrain>();

            if (_brain == null)
            {
                Debug.LogWarning("[PresetSwitcher] Kein CameraBrain gefunden. Component deaktiviert.");
                enabled = false;
                return;
            }

            // Erstes Preset aktivieren
            if (_presets != null && _presets.Length > 0)
            {
                _activeIndex = 0;
                ApplyPreset(0);
            }
        }

        private void Update()
        {
            if (_presets == null || _presets.Length == 0) return;

            if (Input.GetKeyDown(_cycleKey))
            {
                _activeIndex = (_activeIndex + 1) % _presets.Length;
                ApplyPreset(_activeIndex);
            }

            if (_switchFlashTimer > 0f)
                _switchFlashTimer -= Time.deltaTime;
        }

        private void ApplyPreset(int index)
        {
            if (index < 0 || index >= _presets.Length || _presets[index] == null) return;

            var preset = _presets[index];
            _brain.SetPreset(preset);
            _activePresetName = preset.name.Replace("CameraPreset_", "");
            _switchFlashTimer = 2f;
            Debug.Log($"[PresetSwitcher] Preset gewechselt: {_activePresetName} [{_cycleKey}]");
        }

        private void OnGUI()
        {
            if (!_showHud || _presets == null || _presets.Length == 0) return;

            // Aktives Preset — permanent oben rechts
            var hudStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperRight,
                normal = { textColor = new Color(1f, 1f, 1f, 0.7f) }
            };

            float x = Screen.width - 220f;
            GUI.Label(new Rect(x, 10, 210, 24), $"[{_cycleKey}] {_activePresetName}", hudStyle);

            // Kurzes Flash nach Wechsel
            if (_switchFlashTimer > 0f)
            {
                float alpha = Mathf.Clamp01(_switchFlashTimer);
                var flashStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 22,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperCenter,
                    normal = { textColor = new Color(0.2f, 1f, 0.4f, alpha) }
                };

                GUI.Label(
                    new Rect(0, Screen.height * 0.15f, Screen.width, 36),
                    _activePresetName,
                    flashStyle);
            }
        }
    }
}
