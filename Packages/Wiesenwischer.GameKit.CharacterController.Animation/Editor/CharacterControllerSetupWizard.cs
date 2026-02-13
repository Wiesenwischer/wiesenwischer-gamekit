using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Animation.Editor
{
    /// <summary>
    /// EditorWindow zum Einrichten des kompletten Character Controller Systems.
    /// Erstellt Configs, Animator Controller und Player Prefab mit wählbarem Character Model.
    /// Menü: Wiesenwischer > GameKit > Setup Character Controller
    /// </summary>
    public class CharacterControllerSetupWizard : EditorWindow
    {
        private const string AnimatorControllerPath =
            "Packages/Wiesenwischer.GameKit.CharacterController.Animation/Resources/AnimatorControllers/CharacterAnimatorController.controller";

        private const string LocomotionConfigPath = "Assets/Config/DefaultLocomotionConfig.asset";

        private GameObject _characterModelFBX;
        private bool _adjustCapsule = true;
        private Vector2 _scrollPos;

        [MenuItem("Wiesenwischer/GameKit/Setup Character Controller", false, 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<CharacterControllerSetupWizard>("Character Controller Setup");
            window.minSize = new Vector2(400, 450);
            window.Show();
        }

        [MenuItem("Wiesenwischer/GameKit/Setup Character Controller", true)]
        private static bool ValidateShowWindow()
        {
            return !Application.isPlaying;
        }

        private void OnEnable()
        {
            AutoDetectCharacterModel();
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            // ========== CHARACTER MODEL ==========
            EditorGUILayout.LabelField("Character Model", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            _characterModelFBX = (GameObject)EditorGUILayout.ObjectField(
                "Character FBX", _characterModelFBX, typeof(GameObject), false);

            if (_characterModelFBX != null)
            {
                string path = AssetDatabase.GetAssetPath(_characterModelFBX);
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer != null && importer.animationType != ModelImporterAnimationType.Human)
                {
                    EditorGUILayout.HelpBox(
                        $"Rig ist nicht Humanoid (aktuell: {importer.animationType}).\n" +
                        "Bitte im FBX-Import auf Humanoid umstellen.",
                        MessageType.Error);
                }
            }

            EditorGUILayout.Space(4);
            _adjustCapsule = EditorGUILayout.Toggle("CapsuleCollider anpassen", _adjustCapsule);

            // ========== SETUP PIPELINE ==========
            EditorGUILayout.Space(12);
            DrawSeparator();
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Setup Pipeline", EditorStyles.boldLabel);

            bool prefabExists = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabCreator.OutputPath) != null;

            if (!prefabExists)
            {
                EditorGUILayout.HelpBox(
                    "Erstellt das komplette Character Controller System:\n\n" +
                    "1. DefaultLocomotionConfig\n" +
                    "2. Avatar Masks (UpperBody, LowerBody, ArmsOnly)\n" +
                    "3. Animator Controller (Locomotion + Airborne + Stopping)\n" +
                    "4. Player Prefab mit gewähltem Character Model",
                    MessageType.Info);

                EditorGUILayout.Space(4);

                GUI.enabled = IsModelValid();
                if (GUILayout.Button("Setup starten", GUILayout.Height(28)))
                {
                    RunFullSetup();
                }
                GUI.enabled = true;
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Player Prefab existiert bereits.\n" +
                    "Character Model kann ausgetauscht werden.",
                    MessageType.Info);

                EditorGUILayout.Space(4);

                GUI.enabled = IsModelValid();
                if (GUILayout.Button("Character Model austauschen", GUILayout.Height(28)))
                {
                    PlayerPrefabCreator.SwapModelInPrefab(_characterModelFBX, _adjustCapsule);
                    Repaint();
                }
                GUI.enabled = true;

                EditorGUILayout.Space(4);

                if (GUILayout.Button("Setup komplett neu ausführen", GUILayout.Height(22)))
                {
                    if (EditorUtility.DisplayDialog("Setup neu ausführen?",
                        "Bestehende Assets werden überschrieben:\n" +
                        "- Animator Controller\n" +
                        "- Avatar Masks\n" +
                        "- Player Prefab\n\n" +
                        "Fortfahren?",
                        "Ja, neu erstellen", "Abbrechen"))
                    {
                        RunFullSetup();
                    }
                }
            }

            // ========== STATUS ==========
            EditorGUILayout.Space(12);
            DrawSeparator();
            EditorGUILayout.Space(4);
            DrawStatusSection();

            EditorGUILayout.Space(8);
            EditorGUILayout.EndScrollView();
        }

        private void RunFullSetup()
        {
            int totalSteps = 4;
            int step = 0;

            EditorUtility.DisplayProgressBar("Character Controller Setup",
                "1/4 — LocomotionConfig erstellen...", (float)step++ / totalSteps);
            EnsureLocomotionConfig();

            EditorUtility.DisplayProgressBar("Character Controller Setup",
                "2/4 — Avatar Masks erstellen...", (float)step++ / totalSteps);
            AvatarMaskCreator.CreateAllMasks();

            EditorUtility.DisplayProgressBar("Character Controller Setup",
                "3/4 — Animator Controller erstellen...", (float)step++ / totalSteps);
            RecreateAnimatorController();
            LocomotionBlendTreeCreator.SetupLocomotionBlendTree();
            AirborneStatesCreator.SetupAirborneStates();
            StoppingStatesCreator.SetupStoppingStates();

            EditorUtility.DisplayProgressBar("Character Controller Setup",
                "4/4 — Player Prefab erstellen...", (float)step++ / totalSteps);
            PlayerPrefabCreator.CreatePlayerPrefab(_characterModelFBX, _adjustCapsule);

            EditorUtility.ClearProgressBar();

            Debug.Log("=== Character Controller Setup abgeschlossen! ===");

            EditorUtility.DisplayDialog(
                "Setup abgeschlossen!",
                "Character Controller ist bereit.\n\n" +
                "Nächste Schritte:\n" +
                "• Animation > Animation Wizard: Animations-Clips zuweisen\n" +
                "• Core > Create Playground: Testumgebung erstellen\n" +
                "• Core > Place Player in Scene: Player platzieren\n" +
                "• Camera > Setup Third Person Camera: Kamera einrichten\n" +
                "• IK > Setup IK on Player Prefab: Foot/LookAt IK\n" +
                "• Play Mode starten und testen",
                "OK");

            Repaint();
        }

        #region Helpers

        private bool IsModelValid()
        {
            if (_characterModelFBX == null) return false;
            string path = AssetDatabase.GetAssetPath(_characterModelFBX);
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            return importer != null && importer.animationType == ModelImporterAnimationType.Human;
        }

        private void AutoDetectCharacterModel()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabCreator.OutputPath);
            if (prefab != null)
            {
                var animator = prefab.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    var source = PrefabUtility.GetCorrespondingObjectFromSource(animator.gameObject);
                    if (source != null)
                    {
                        _characterModelFBX = source;
                        return;
                    }
                }
            }

            if (!AssetDatabase.IsValidFolder("Assets/Characters")) return;
            var guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/Characters" });
            foreach (var guid in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                if (p.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                {
                    _characterModelFBX = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                    if (_characterModelFBX != null) return;
                }
            }
        }

        private static void DrawSeparator()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }

        #endregion

        #region Status

        private void DrawStatusSection()
        {
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);

            var config = AssetDatabase.LoadAssetAtPath<Object>(LocomotionConfigPath);
            StatusRow("LocomotionConfig", config != null);

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
            StatusRow("Animator Controller",
                controller != null,
                controller != null ? $"{controller.layers[0].stateMachine.states.Length} States" : null);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabCreator.OutputPath);
            StatusRow("Player Prefab", prefab != null);

            StatusRow("Character Model",
                _characterModelFBX != null,
                _characterModelFBX != null ? _characterModelFBX.name : null);
        }

        private static void StatusRow(string label, bool ok, string details = null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(150));

            var color = ok ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.8f, 0.3f, 0.3f);
            var style = new GUIStyle(EditorStyles.label) { normal = { textColor = color } };
            EditorGUILayout.LabelField(ok ? "OK" : "\u2014", style, GUILayout.Width(30));

            if (!string.IsNullOrEmpty(details))
                EditorGUILayout.LabelField(details, EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Setup Pipeline

        private static void EnsureLocomotionConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Object>(LocomotionConfigPath);
            if (existing != null)
            {
                Debug.Log("[Setup] LocomotionConfig existiert bereits.");
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Config"))
                AssetDatabase.CreateFolder("Assets", "Config");

            var config = ScriptableObject.CreateInstance<Core.Locomotion.LocomotionConfig>();
            AssetDatabase.CreateAsset(config, LocomotionConfigPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Setup] LocomotionConfig erstellt: {LocomotionConfigPath}");
        }

        private static void RecreateAnimatorController()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(AnimatorControllerPath);
                Debug.Log("[Setup] Bestehender Animator Controller gelöscht.");
            }

            AnimatorControllerCreator.CreateController();
        }

        #endregion
    }
}
