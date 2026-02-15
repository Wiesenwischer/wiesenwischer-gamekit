using UnityEditor;
using UnityEngine;
using Wiesenwischer.GameKit.Camera.Behaviours;

namespace Wiesenwischer.GameKit.Camera.Editor
{
    /// <summary>
    /// Editor-Fenster zum Testen des Camera Systems.
    /// Zeigt den aktuellen CameraState, erlaubt Preset-Wechsel und Behaviour-Steuerung.
    /// </summary>
    public class CameraTestWindow : EditorWindow
    {
        private CameraBrain _brain;
        private CameraPreset _presetToApply;
        private Vector2 _scrollPos;
        private bool _showState = true;
        private bool _showBehaviours = true;
        private bool _showPresets = true;
        private bool _showQuickActions = true;

        [MenuItem("Wiesenwischer/GameKit/Camera/Camera Test Window", false, 200)]
        public static void ShowWindow()
        {
            var window = GetWindow<CameraTestWindow>("Camera Test");
            window.minSize = new Vector2(320, 400);
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            _brain = null; // Reset bei Play Mode Wechsel
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            DrawHeader();
            DrawBrainFinder();

            if (_brain == null)
            {
                EditorGUILayout.HelpBox(
                    "Kein CameraBrain gefunden.\n\n" +
                    "1. Platziere einen Player in der Szene\n" +
                    "2. Nutze 'Setup Camera Brain' um das System aufzusetzen\n" +
                    "3. Starte Play Mode",
                    MessageType.Info);

                if (GUILayout.Button("Setup Camera Brain", GUILayout.Height(30)))
                    CameraSetupEditor.SetupCameraBrain();

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
                Repaint(); // Continuous update im Play Mode
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Camera Test & Debug", EditorStyles.boldLabel);
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

        private void DrawPresetSection()
        {
            _showPresets = EditorGUILayout.Foldout(_showPresets, "Presets", true, EditorStyles.foldoutHeader);
            if (!_showPresets) return;

            EditorGUI.indentLevel++;

            // Preset-Feld
            _presetToApply = (CameraPreset)EditorGUILayout.ObjectField(
                "Preset", _presetToApply, typeof(CameraPreset), false);

            EditorGUILayout.BeginHorizontal();

            GUI.enabled = _presetToApply != null && Application.isPlaying;
            if (GUILayout.Button("Apply Preset"))
            {
                _brain.SetPreset(_presetToApply);
                Debug.Log($"[CameraTest] Preset '{_presetToApply.name}' angewendet.");
            }
            GUI.enabled = true;

            // Schnellzugriff auf bekannte Presets
            if (GUILayout.Button("Find BDO"))
                _presetToApply = FindPresetByName("CameraPreset_BDO");
            if (GUILayout.Button("Find ArcheAge"))
                _presetToApply = FindPresetByName("CameraPreset_ArcheAge");

            EditorGUILayout.EndHorizontal();

            // Preset-Info
            if (_presetToApply != null)
            {
                EditorGUILayout.HelpBox(
                    $"{_presetToApply.name}\n" +
                    $"{_presetToApply.Description}\n\n" +
                    $"FOV: {_presetToApply.DefaultFov} | Distance: {_presetToApply.DefaultDistance}\n" +
                    $"Inertia: {(_presetToApply.InertiaEnabled ? "An" : "Aus")} | " +
                    $"Recenter: {(_presetToApply.RecenterEnabled ? "An" : "Aus")} | " +
                    $"Shoulder: {(_presetToApply.ShoulderEnabled ? "An" : "Aus")}\n" +
                    $"DynamicOrbit: {(_presetToApply.DynamicOrbitEnabled ? "An" : "Aus")} | " +
                    $"SoftTarget: {(_presetToApply.SoftTargetingEnabled ? "An" : "Aus")}",
                    MessageType.None);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

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

                // Select-Button
                if (GUILayout.Button("Select", GUILayout.Width(50)))
                    Selection.activeObject = mb;

                EditorGUILayout.EndHorizontal();
            }

            // CinemachineDriver (kein ICameraBehaviour, daher separater Toggle)
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

                if (GUILayout.Button("Select", GUILayout.Width(50)))
                    Selection.activeObject = driver;

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
                    Debug.Log("[CameraTest] CinemachineDriver hinzugefügt.");
                }
                EditorGUILayout.EndHorizontal();
            }
#else
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("CinemachineDriver", "Cinemachine nicht installiert");
#endif
        }

        private void DrawStateSection()
        {
            _showState = EditorGUILayout.Foldout(_showState, "Live Camera State", true, EditorStyles.foldoutHeader);
            if (!_showState) return;

            EditorGUI.indentLevel++;

            var state = _brain.State;

            EditorGUILayout.LabelField("Yaw", $"{state.Yaw:F1}°");
            EditorGUILayout.LabelField("Pitch", $"{state.Pitch:F1}°");
            EditorGUILayout.LabelField("Distance", $"{state.Distance:F2}m");
            EditorGUILayout.LabelField("FOV", $"{state.Fov:F1}°");

            if (state.ShoulderOffset != Vector3.zero)
                EditorGUILayout.LabelField("Shoulder", $"({state.ShoulderOffset.x:F2}, {state.ShoulderOffset.y:F2})");

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

            // Shoulder-Switch
            var shoulder = _brain.GetComponent<ShoulderOffsetBehaviour>();
            if (shoulder != null && shoulder.enabled)
            {
                if (GUILayout.Button($"Switch Shoulder ({(shoulder.IsRightShoulder ? "R → L" : "L → R")})"))
                    shoulder.SwitchSide();
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        private static CameraPreset FindPresetByName(string name)
        {
            var guids = AssetDatabase.FindAssets($"t:CameraPreset {name}");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<CameraPreset>(path);
            }

            // Fallback: alle Presets durchsuchen
            guids = AssetDatabase.FindAssets("t:CameraPreset");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var preset = AssetDatabase.LoadAssetAtPath<CameraPreset>(path);
                if (preset != null && preset.name == name)
                    return preset;
            }

            Debug.LogWarning($"[CameraTest] Preset '{name}' nicht gefunden.");
            return null;
        }
    }
}
