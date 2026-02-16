using UnityEditor;
using UnityEngine;
using Wiesenwischer.GameKit.Camera.Behaviours;

namespace Wiesenwischer.GameKit.Camera.Editor
{
    /// <summary>
    /// Editor-Fenster für Camera Brain Setup und Konfiguration.
    /// Setup-Flow: Preset wählen → Setup ausführen → fertig.
    /// Danach: Preset-Wechsel, Behaviour-Steuerung, Live-Debugging.
    /// </summary>
    public class CameraBrainWindow : EditorWindow
    {
        private CameraBrain _brain;
        private CameraPreset _presetToApply;
        private Vector2 _scrollPos;
        private bool _showState = true;
        private bool _showBehaviours = true;
        private bool _showPresets = true;
        private bool _showQuickActions = true;

        // Preset-Dropdown Cache
        private CameraPreset[] _availablePresets;
        private string[] _presetNames;
        private int _selectedPresetIndex = -1;

        [MenuItem("Wiesenwischer/GameKit/Camera/Setup Camera Brain", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<CameraBrainWindow>("Camera Brain");
            window.minSize = new Vector2(320, 400);
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            RefreshPresetList();
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            _brain = null;
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            DrawHeader();
            DrawBrainFinder();

            if (_brain == null)
            {
                DrawSetupSection();
                EditorGUILayout.EndScrollView();
                return;
            }

            DrawPresetSection();
            DrawBehaviourSection();

            if (Application.isPlaying)
            {
                DrawStateSection();
                DrawQuickActions();
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Starte Play Mode für Live-Ansicht und Runtime-Steuerung.",
                    MessageType.Info);
            }

            EditorGUILayout.EndScrollView();

            if (Application.isPlaying)
                Repaint();
        }

        #region Setup (kein CameraBrain vorhanden)

        private void DrawSetupSection()
        {
            EditorGUILayout.HelpBox(
                "Kein CameraBrain gefunden.\n\n" +
                "1. Platziere einen Player in der Szene\n" +
                "2. Wähle ein Camera-Preset\n" +
                "3. Klicke 'Setup Camera Brain'",
                MessageType.Info);

            EditorGUILayout.Space(4);

            // Preset-Auswahl VOR dem Setup
            EditorGUILayout.LabelField("Camera-Stil wählen:", EditorStyles.boldLabel);

            if (_availablePresets == null || _availablePresets.Length == 0)
                RefreshPresetList();

            if (_availablePresets != null && _availablePresets.Length > 0)
            {
                int newIndex = EditorGUILayout.Popup("Preset", _selectedPresetIndex, _presetNames);
                if (newIndex != _selectedPresetIndex && newIndex >= 0 && newIndex < _availablePresets.Length)
                {
                    _selectedPresetIndex = newIndex;
                    _presetToApply = _availablePresets[newIndex];
                }

                // Preset-Info anzeigen
                if (_presetToApply != null)
                {
                    EditorGUILayout.HelpBox(
                        $"{_presetToApply.Description}\n\n" +
                        $"Orbit: {_presetToApply.OrbitActivation} | " +
                        $"FOV: {_presetToApply.DefaultFov} | " +
                        $"Distance: {_presetToApply.DefaultDistance}",
                        MessageType.None);
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Keine Camera-Presets gefunden.\n" +
                    "Setup erstellt Standardkonfiguration (AlwaysOn/ActionCombat).",
                    MessageType.Warning);
            }

            EditorGUILayout.Space(8);

            // Setup Button — EIN Klick macht alles
            string buttonLabel = _presetToApply != null
                ? $"Setup Camera Brain ({_presetToApply.name.Replace("CameraPreset_", "")})"
                : "Setup Camera Brain (Standard)";

            if (GUILayout.Button(buttonLabel, GUILayout.Height(32)))
            {
                CameraSetupEditor.SetupCameraBrain(_presetToApply);

                // Brain nach Setup finden
                _brain = FindObjectOfType<CameraBrain>();
            }
        }

        #endregion

        #region Header & Brain Finder

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Camera Brain", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
        }

        private void DrawBrainFinder()
        {
            EditorGUILayout.BeginHorizontal();
            _brain = (CameraBrain)EditorGUILayout.ObjectField(
                "Camera Brain", _brain, typeof(CameraBrain), true);

            if (_brain == null && GUILayout.Button("Find", GUILayout.Width(50)))
                _brain = FindObjectOfType<CameraBrain>();

            EditorGUILayout.EndHorizontal();

            if (_brain == null)
                _brain = FindObjectOfType<CameraBrain>();

            EditorGUILayout.Space(4);
        }

        #endregion

        #region Presets (CameraBrain vorhanden)

        private void DrawPresetSection()
        {
            _showPresets = EditorGUILayout.Foldout(_showPresets, "Presets", true, EditorStyles.foldoutHeader);
            if (!_showPresets) return;

            EditorGUI.indentLevel++;

            if (_availablePresets == null)
                RefreshPresetList();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Refresh", GUILayout.Width(60)))
                RefreshPresetList();

            int newIndex = EditorGUILayout.Popup(_selectedPresetIndex, _presetNames ?? System.Array.Empty<string>());
            if (newIndex != _selectedPresetIndex && newIndex >= 0 && newIndex < _availablePresets.Length)
            {
                _selectedPresetIndex = newIndex;
                _presetToApply = _availablePresets[newIndex];
            }

            EditorGUILayout.EndHorizontal();

            // Apply Button
            GUI.enabled = _presetToApply != null;
            if (GUILayout.Button("Apply Preset"))
            {
                if (Application.isPlaying)
                {
                    _brain.SetPreset(_presetToApply);
                }
                else
                {
                    var inputPipeline = _brain.GetComponent<CameraInputPipeline>();
                    CameraSetupEditor.ApplyPresetInEditor(_brain, inputPipeline, _presetToApply);
                }
            }
            GUI.enabled = true;

            // Preset-Info
            if (_presetToApply != null)
            {
                EditorGUILayout.HelpBox(
                    $"{_presetToApply.name}\n" +
                    $"{_presetToApply.Description}\n\n" +
                    $"Orbit: {_presetToApply.OrbitActivation} | " +
                    $"FOV: {_presetToApply.DefaultFov} | Distance: {_presetToApply.DefaultDistance}\n" +
                    $"Inertia: {(_presetToApply.InertiaEnabled ? "An" : "Aus")} | " +
                    $"Recenter: {(_presetToApply.RecenterEnabled ? "An" : "Aus")} | " +
                    $"Shoulder: {(_presetToApply.ShoulderEnabled ? "An" : "Aus")}\n" +
                    $"DynamicOrbit: {(_presetToApply.DynamicOrbitEnabled ? "An" : "Aus")} | " +
                    $"SoftTarget: {(_presetToApply.SoftTargetingEnabled ? "An" : "Aus")}",
                    MessageType.None);
            }

            // Re-Run Button für existierendes Setup
            EditorGUILayout.Space(4);
            if (GUILayout.Button("Re-Run Setup (Reparatur)"))
            {
                CameraSetupEditor.SetupCameraBrain(_presetToApply);
                _brain = FindObjectOfType<CameraBrain>();
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        private void RefreshPresetList()
        {
            var guids = AssetDatabase.FindAssets("t:CameraPreset");
            _availablePresets = new CameraPreset[guids.Length];
            _presetNames = new string[guids.Length];

            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                _availablePresets[i] = AssetDatabase.LoadAssetAtPath<CameraPreset>(path);
                _presetNames[i] = _availablePresets[i] != null
                    ? _availablePresets[i].name.Replace("CameraPreset_", "")
                    : "(null)";
            }

            _selectedPresetIndex = -1;
            if (_presetToApply != null)
            {
                for (int i = 0; i < _availablePresets.Length; i++)
                {
                    if (_availablePresets[i] == _presetToApply)
                    {
                        _selectedPresetIndex = i;
                        break;
                    }
                }
            }
        }

        #endregion

        #region Behaviours

        private void DrawBehaviourSection()
        {
            _showBehaviours = EditorGUILayout.Foldout(_showBehaviours, "Behaviours", true, EditorStyles.foldoutHeader);
            if (!_showBehaviours) return;

            EditorGUI.indentLevel++;

            var behaviours = _brain.GetComponents<ICameraBehaviour>();
            foreach (var behaviour in behaviours)
            {
                var mb = behaviour as MonoBehaviour;
                if (mb == null) continue;

                EditorGUILayout.BeginHorizontal();

                string name = mb.GetType().Name.Replace("Behaviour", "");
                bool wasEnabled = mb.enabled;
                bool isEnabled = EditorGUILayout.ToggleLeft(name, mb.enabled);

                if (isEnabled != wasEnabled)
                {
                    Undo.RecordObject(mb, $"Toggle {name}");
                    mb.enabled = isEnabled;
                    EditorUtility.SetDirty(mb);
                }

                EditorGUILayout.EndHorizontal();
            }

            DrawCinemachineToggle();

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        private void DrawCinemachineToggle()
        {
#if CINEMACHINE_AVAILABLE
            var driver = _brain.GetComponent<CinemachineDriver>();
            if (driver != null)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.BeginHorizontal();

                bool wasEnabled = driver.enabled;
                bool isEnabled = EditorGUILayout.ToggleLeft("CinemachineDriver", driver.enabled);

                if (isEnabled != wasEnabled)
                {
                    Undo.RecordObject(driver, "Toggle CinemachineDriver");
                    driver.enabled = isEnabled;
                    EditorUtility.SetDirty(driver);
                }

                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("CinemachineDriver", "Nicht vorhanden");
                if (GUILayout.Button("Add", GUILayout.Width(50)))
                {
                    Undo.AddComponent<CinemachineDriver>(_brain.gameObject);
                    Debug.Log("[CameraBrain] CinemachineDriver hinzugefügt.");
                }
                EditorGUILayout.EndHorizontal();
            }
#else
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("CinemachineDriver", "Cinemachine nicht installiert");
#endif
        }

        #endregion

        #region Live State & Quick Actions

        private void DrawStateSection()
        {
            _showState = EditorGUILayout.Foldout(_showState, "Live Camera State", true, EditorStyles.foldoutHeader);
            if (!_showState) return;

            EditorGUI.indentLevel++;

            var state = _brain.State;

            EditorGUILayout.LabelField("Yaw", $"{state.Yaw:F1}\u00b0");
            EditorGUILayout.LabelField("Pitch", $"{state.Pitch:F1}\u00b0");
            EditorGUILayout.LabelField("Distance", $"{state.Distance:F2}m");
            EditorGUILayout.LabelField("FOV", $"{state.Fov:F1}\u00b0");

            if (state.ShoulderOffset != Vector3.zero)
                EditorGUILayout.LabelField("Shoulder", $"({state.ShoulderOffset.x:F2}, {state.ShoulderOffset.y:F2})");

            EditorGUILayout.LabelField("Orbit Mode", _brain.CurrentOrbitMode.ToString());
            EditorGUILayout.LabelField("Steer Mode", _brain.IsSteerMode ? "Active" : "Inactive");

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        private void DrawQuickActions()
        {
            _showQuickActions = EditorGUILayout.Foldout(_showQuickActions, "Quick Actions", true, EditorStyles.foldoutHeader);
            if (!_showQuickActions) return;

            EditorGUI.indentLevel++;

            if (GUILayout.Button("Snap Behind Target"))
                _brain.SnapBehindTarget();

            if (GUILayout.Button("Clear All Intents"))
                _brain.ClearIntents();

            if (GUILayout.Button("Refresh Behaviours"))
                _brain.RefreshBehaviours();

            var shoulder = _brain.GetComponent<ShoulderOffsetBehaviour>();
            if (shoulder != null && shoulder.enabled)
            {
                if (GUILayout.Button($"Switch Shoulder ({(shoulder.IsRightShoulder ? "R \u2192 L" : "L \u2192 R")})"))
                    shoulder.SwitchSide();
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        #endregion
    }
}
