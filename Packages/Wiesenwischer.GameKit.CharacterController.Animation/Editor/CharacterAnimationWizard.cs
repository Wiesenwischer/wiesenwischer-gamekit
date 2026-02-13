using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Animation.Editor
{
    /// <summary>
    /// EditorWindow zum Anpassen von Character Model und Animator States.
    /// Ermöglicht das Neuerstellen des Animator Controllers, Austauschen des
    /// Character Models und Umbenennen von Animation-Clips — ohne den
    /// kompletten Setup-Wizard erneut ausführen zu müssen.
    /// Menü: Wiesenwischer > GameKit > Character & Animation Wizard
    /// </summary>
    public class CharacterAnimationWizard : EditorWindow
    {
        private const string AnimatorControllerPath =
            "Packages/Wiesenwischer.GameKit.CharacterController.Animation/Resources/AnimatorControllers/CharacterAnimatorController.controller";

        private Vector2 _scrollPosition;
        private bool _stylesInitialized;
        private GUIStyle _headerStyle;
        private GUIStyle _sectionStyle;

        [MenuItem("Wiesenwischer/GameKit/Character & Animation Wizard", false, 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<CharacterAnimationWizard>("Character & Animation");
            window.minSize = new Vector2(420, 500);
            window.Show();
        }

        [MenuItem("Wiesenwischer/GameKit/Character & Animation Wizard", true)]
        private static bool ValidateShowWindow()
        {
            return !Application.isPlaying;
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(0, 0, 10, 5)
            };

            _sectionStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(0, 0, 5, 5)
            };

            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.LabelField("Character & Animation Wizard", _headerStyle);
            EditorGUILayout.HelpBox(
                "Hier kannst du Animator States neu erstellen, das Character Model wechseln " +
                "und Animation-Clips umbenennen — ohne den kompletten Setup-Wizard erneut auszuführen.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            DrawAnimatorSection();
            DrawPlayerPrefabSection();
            DrawAnimationClipsSection();
            DrawTestSceneSection();

            EditorGUILayout.Space(10);
            DrawStatusSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawAnimatorSection()
        {
            EditorGUILayout.LabelField("Animator Controller", _headerStyle);
            EditorGUILayout.BeginVertical(_sectionStyle);

            EditorGUILayout.LabelField(
                "Erstellt den Animator Controller mit allen States neu. " +
                "Nützlich nach dem Hinzufügen neuer Animations-Clips.",
                EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Animator States neu erstellen", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog(
                    "Animator neu erstellen?",
                    "Der bestehende Animator Controller wird gelöscht und neu erstellt:\n\n" +
                    "• Locomotion Blend Tree (Idle/Walk/Run/Sprint)\n" +
                    "• Airborne States (Jump/Fall/SoftLand/HardLand/Slide)\n" +
                    "• Stopping States (Light/Medium/Hard)\n" +
                    "• Avatar Masks + Layers\n\n" +
                    "Fortfahren?",
                    "Neu erstellen", "Abbrechen"))
                {
                    RebuildAnimatorController();
                }
            }

            EditorGUILayout.Space(3);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Nur Airborne + Slide States"))
            {
                AirborneStatesCreator.SetupAirborneStates();
            }
            if (GUILayout.Button("Nur Stopping States"))
            {
                StoppingStatesCreator.SetupStoppingStates();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawPlayerPrefabSection()
        {
            EditorGUILayout.LabelField("Player Prefab", _headerStyle);
            EditorGUILayout.BeginVertical(_sectionStyle);

            EditorGUILayout.LabelField(
                "Erstellt das Player Prefab mit Character Model, Motor, Input und Animator neu.",
                EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Player Prefab neu erstellen", GUILayout.Height(28)))
            {
                PlayerPrefabCreator.CreatePlayerPrefab();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAnimationClipsSection()
        {
            EditorGUILayout.LabelField("Animation Clips", _headerStyle);
            EditorGUILayout.BeginVertical(_sectionStyle);

            EditorGUILayout.LabelField(
                "Benennt Animation-Clips in FBX-Dateien um (Anim_Idle → Idle, etc.).",
                EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Clips umbenennen", GUILayout.Height(28)))
            {
                AnimationClipRenamer.RenameAllClips();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTestSceneSection()
        {
            EditorGUILayout.LabelField("Test-Szene", _headerStyle);
            EditorGUILayout.BeginVertical(_sectionStyle);

            EditorGUILayout.LabelField(
                "Erstellt eine Test-Szene mit Plattformen, Rampen und Slope-Sliding Testbereichen.",
                EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Animation Test-Szene erstellen", GUILayout.Height(28)))
            {
                AnimationTestSceneCreator.CreateTestScene();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawStatusSection()
        {
            EditorGUILayout.LabelField("Status", _headerStyle);
            EditorGUILayout.BeginVertical(_sectionStyle);

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
            bool hasController = controller != null;
            int stateCount = 0;

            if (hasController)
            {
                var rootSM = controller.layers[0].stateMachine;
                stateCount = rootSM.states.Length;
            }

            DrawStatusRow("Animator Controller:", hasController,
                hasController ? $"{stateCount} States" : "Nicht vorhanden");

            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            DrawStatusRow("Player Prefab:", playerPrefab != null);

            var player = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
            DrawStatusRow("Player in Szene:", player != null, player?.name);

            EditorGUILayout.EndVertical();
        }

        private void DrawStatusRow(string label, bool ok, string details = null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(160));

            var color = ok ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.8f, 0.3f, 0.3f);
            var style = new GUIStyle(EditorStyles.label) { normal = { textColor = color } };
            EditorGUILayout.LabelField(ok ? "OK" : "Fehlt", style, GUILayout.Width(40));

            if (!string.IsNullOrEmpty(details))
                EditorGUILayout.LabelField(details, EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();
        }

        private static void RebuildAnimatorController()
        {
            // Bestehenden Controller löschen
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(AnimatorControllerPath);
            }

            EditorUtility.DisplayProgressBar("Animator neu erstellen", "Controller erstellen...", 0.1f);
            AnimatorControllerCreator.CreateController();

            EditorUtility.DisplayProgressBar("Animator neu erstellen", "Avatar Masks...", 0.2f);
            AvatarMaskCreator.CreateAllMasks();

            EditorUtility.DisplayProgressBar("Animator neu erstellen", "Locomotion Blend Tree...", 0.4f);
            LocomotionBlendTreeCreator.SetupLocomotionBlendTree();

            EditorUtility.DisplayProgressBar("Animator neu erstellen", "Airborne + Slide States...", 0.6f);
            AirborneStatesCreator.SetupAirborneStates();

            EditorUtility.DisplayProgressBar("Animator neu erstellen", "Stopping States...", 0.8f);
            StoppingStatesCreator.SetupStoppingStates();

            EditorUtility.ClearProgressBar();

            Debug.Log("[CharacterAnimationWizard] Animator Controller komplett neu erstellt " +
                      "(Locomotion + Airborne + Slide + Stopping).");
        }
    }
}
