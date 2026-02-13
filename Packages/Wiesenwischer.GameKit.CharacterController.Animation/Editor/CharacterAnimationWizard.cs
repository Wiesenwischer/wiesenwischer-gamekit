using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Animation.Editor
{
    /// <summary>
    /// EditorWindow zum Erstellen und Anpassen von Character Models und Animationen.
    /// Kernfeature: FBX-Model wählen, Animation-Clips individuell zuweisen,
    /// Player Prefab + Animator Controller erstellen/aktualisieren.
    /// Menü: Wiesenwischer > GameKit > Character & Animation Wizard
    /// </summary>
    public class CharacterAnimationWizard : EditorWindow
    {
        private const string AnimatorControllerPath =
            "Packages/Wiesenwischer.GameKit.CharacterController.Animation/Resources/AnimatorControllers/CharacterAnimatorController.controller";

        private const string ClipBasePath = "Assets/Animations/Locomotion/";

        // --- Character Model ---
        private GameObject _characterModel;

        // --- Animation Clips ---
        private AnimationClip _idleClip;
        private AnimationClip _walkClip;
        private AnimationClip _runClip;
        private AnimationClip _sprintClip;
        private AnimationClip _jumpClip;
        private AnimationClip _fallClip;
        private AnimationClip _softLandClip;
        private AnimationClip _hardLandClip;
        private AnimationClip _lightStopClip;
        private AnimationClip _mediumStopClip;
        private AnimationClip _hardStopClip;
        private AnimationClip _slideClip;

        // --- UI State ---
        private Vector2 _scrollPosition;
        private bool _stylesInitialized;
        private GUIStyle _headerStyle;
        private GUIStyle _sectionStyle;
        private bool _showLocomotionClips = true;
        private bool _showAirborneClips = true;
        private bool _showStoppingClips = true;

        [MenuItem("Wiesenwischer/GameKit/Character & Animation Wizard", false, 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<CharacterAnimationWizard>("Character & Animation");
            window.minSize = new Vector2(450, 600);
            window.Show();
        }

        [MenuItem("Wiesenwischer/GameKit/Character & Animation Wizard", true)]
        private static bool ValidateShowWindow()
        {
            return !Application.isPlaying;
        }

        private void OnEnable()
        {
            AutoDetectClips();
            AutoDetectCharacterModel();
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
                "Character Model wählen, Animation-Clips zuweisen und Player Prefab erstellen. " +
                "Clips werden automatisch aus Assets/Animations/Locomotion/ erkannt.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            DrawCharacterModelSection();
            DrawAnimationClipsSection();
            DrawActionsSection();
            DrawStatusSection();

            EditorGUILayout.EndScrollView();
        }

        #region Character Model

        private void DrawCharacterModelSection()
        {
            EditorGUILayout.LabelField("Character Model", _headerStyle);
            EditorGUILayout.BeginVertical(_sectionStyle);

            EditorGUI.BeginChangeCheck();
            _characterModel = (GameObject)EditorGUILayout.ObjectField(
                "FBX Model", _characterModel, typeof(GameObject), false);

            if (EditorGUI.EndChangeCheck() && _characterModel != null)
            {
                var path = AssetDatabase.GetAssetPath(_characterModel);
                if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase) &&
                    !path.EndsWith(".FBX", System.StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogWarning("[CharacterAnimationWizard] Bitte eine FBX-Datei wählen.");
                }
            }

            if (_characterModel == null)
            {
                EditorGUILayout.HelpBox(
                    "Kein Character Model zugewiesen. Ziehe eine FBX-Datei in das Feld oben.",
                    MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Animation Clips

        private void DrawAnimationClipsSection()
        {
            EditorGUILayout.LabelField("Animation Clips", _headerStyle);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Auto-Erkennung", GUILayout.Width(120)))
            {
                AutoDetectClips();
            }
            EditorGUILayout.HelpBox("Sucht Clips in Assets/Animations/Locomotion/", MessageType.None);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Locomotion
            _showLocomotionClips = EditorGUILayout.Foldout(_showLocomotionClips, "Locomotion", true);
            if (_showLocomotionClips)
            {
                EditorGUILayout.BeginVertical(_sectionStyle);
                _idleClip = ClipField("Idle", _idleClip);
                _walkClip = ClipField("Walk", _walkClip);
                _runClip = ClipField("Run", _runClip);
                _sprintClip = ClipField("Sprint", _sprintClip);
                EditorGUILayout.EndVertical();
            }

            // Airborne + Slide
            _showAirborneClips = EditorGUILayout.Foldout(_showAirborneClips, "Airborne + Slide", true);
            if (_showAirborneClips)
            {
                EditorGUILayout.BeginVertical(_sectionStyle);
                _jumpClip = ClipField("Jump", _jumpClip);
                _fallClip = ClipField("Fall", _fallClip);
                _softLandClip = ClipField("SoftLand", _softLandClip);
                _hardLandClip = ClipField("HardLand", _hardLandClip);
                _slideClip = ClipField("Slide", _slideClip);
                EditorGUILayout.EndVertical();
            }

            // Stopping
            _showStoppingClips = EditorGUILayout.Foldout(_showStoppingClips, "Stopping", true);
            if (_showStoppingClips)
            {
                EditorGUILayout.BeginVertical(_sectionStyle);
                _lightStopClip = ClipField("LightStop", _lightStopClip);
                _mediumStopClip = ClipField("MediumStop", _mediumStopClip);
                _hardStopClip = ClipField("HardStop", _hardStopClip);
                EditorGUILayout.EndVertical();
            }
        }

        private static AnimationClip ClipField(string label, AnimationClip current)
        {
            EditorGUILayout.BeginHorizontal();

            var clip = (AnimationClip)EditorGUILayout.ObjectField(
                label, current, typeof(AnimationClip), false);

            // Status-Indikator
            var color = clip != null ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.8f, 0.4f, 0.2f);
            var style = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = color } };
            EditorGUILayout.LabelField(clip != null ? "OK" : "—", style, GUILayout.Width(25));

            EditorGUILayout.EndHorizontal();
            return clip;
        }

        #endregion

        #region Actions

        private void DrawActionsSection()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Aktionen", _headerStyle);
            EditorGUILayout.BeginVertical(_sectionStyle);

            // Player Prefab erstellen
            using (new EditorGUI.DisabledGroupScope(_characterModel == null))
            {
                if (GUILayout.Button("Player Prefab erstellen / aktualisieren", GUILayout.Height(32)))
                {
                    CreatePlayerPrefabWithModel();
                }
            }

            EditorGUILayout.Space(5);

            // Animator Controller neu erstellen
            bool hasMinClips = _idleClip != null && _walkClip != null && _runClip != null;
            using (new EditorGUI.DisabledGroupScope(!hasMinClips))
            {
                if (GUILayout.Button("Animator Controller neu erstellen", GUILayout.Height(28)))
                {
                    if (EditorUtility.DisplayDialog(
                        "Animator Controller neu erstellen?",
                        "Der bestehende Animator Controller wird gelöscht und mit den " +
                        "aktuell zugewiesenen Clips neu erstellt.\n\nFortfahren?",
                        "Neu erstellen", "Abbrechen"))
                    {
                        RebuildAnimatorWithClips();
                    }
                }
            }

            if (!hasMinClips)
            {
                EditorGUILayout.HelpBox(
                    "Mindestens Idle, Walk und Run Clips werden benötigt.",
                    MessageType.Info);
            }

            EditorGUILayout.Space(5);

            // Clips umbenennen
            if (GUILayout.Button("Animation Clips in FBX umbenennen"))
            {
                AnimationClipRenamer.RenameAllClips();
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Status

        private void DrawStatusSection()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Status", _headerStyle);
            EditorGUILayout.BeginVertical(_sectionStyle);

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
            DrawStatusRow("Animator Controller:", controller != null,
                controller != null ? $"{controller.layers[0].stateMachine.states.Length} States" : null);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            DrawStatusRow("Player Prefab:", prefab != null);

            DrawStatusRow("Character Model:", _characterModel != null,
                _characterModel != null ? _characterModel.name : null);

            int clipCount = CountAssignedClips();
            DrawStatusRow("Animation Clips:", clipCount > 0, $"{clipCount}/12 zugewiesen");

            EditorGUILayout.EndVertical();
        }

        private static void DrawStatusRow(string label, bool ok, string details = null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(160));

            var color = ok ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.8f, 0.3f, 0.3f);
            var style = new GUIStyle(EditorStyles.label) { normal = { textColor = color } };
            EditorGUILayout.LabelField(ok ? "OK" : "—", style, GUILayout.Width(30));

            if (!string.IsNullOrEmpty(details))
                EditorGUILayout.LabelField(details, EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();
        }

        private int CountAssignedClips()
        {
            int count = 0;
            if (_idleClip != null) count++;
            if (_walkClip != null) count++;
            if (_runClip != null) count++;
            if (_sprintClip != null) count++;
            if (_jumpClip != null) count++;
            if (_fallClip != null) count++;
            if (_softLandClip != null) count++;
            if (_hardLandClip != null) count++;
            if (_lightStopClip != null) count++;
            if (_mediumStopClip != null) count++;
            if (_hardStopClip != null) count++;
            if (_slideClip != null) count++;
            return count;
        }

        #endregion

        #region Logic

        private void AutoDetectClips()
        {
            _idleClip = LoadClipFromFbx("Anim_Idle");
            _walkClip = LoadClipFromFbx("Anim_Walk");
            _runClip = LoadClipFromFbx("Anim_Run");
            _sprintClip = LoadClipFromFbx("Anim_Sprint");
            _jumpClip = LoadClipFromFbx("Anim_Jump");
            _fallClip = LoadClipFromFbx("Anim_Fall");
            _softLandClip = LoadClipFromFbx("Anim_SoftLand");
            _hardLandClip = LoadClipFromFbx("Anim_HardLand");
            _lightStopClip = LoadClipFromFbx("Anim_LightStop");
            _mediumStopClip = LoadClipFromFbx("Anim_MediumStop");
            _hardStopClip = LoadClipFromFbx("Anim_HardStop");
            _slideClip = LoadClipFromFbx("Anim_Slide");

            // Fallback: Anim_Land als SoftLand
            if (_softLandClip == null)
                _softLandClip = LoadClipFromFbx("Anim_Land");

            int count = CountAssignedClips();
            Debug.Log($"[CharacterAnimationWizard] Auto-Erkennung: {count}/12 Clips gefunden.");
        }

        private void AutoDetectCharacterModel()
        {
            // Versuche bestehenden Pfad aus PlayerPrefabCreator
            var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Song/Song.Fbx");
            if (model != null)
            {
                _characterModel = model;
                return;
            }

            // Fallback: Suche nach FBX in Assets/Characters/
            var guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/Characters" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                {
                    _characterModel = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (_characterModel != null)
                    {
                        Debug.Log($"[CharacterAnimationWizard] Character Model erkannt: {path}");
                        return;
                    }
                }
            }
        }

        private void CreatePlayerPrefabWithModel()
        {
            if (_characterModel == null)
            {
                Debug.LogError("[CharacterAnimationWizard] Kein Character Model zugewiesen.");
                return;
            }

            // PlayerPrefabCreator nutzt einen hardcodierten Pfad.
            // Wir aktualisieren den Pfad über Reflection oder erstellen das Prefab direkt.
            var modelPath = AssetDatabase.GetAssetPath(_characterModel);

            EditorUtility.DisplayProgressBar("Player Prefab erstellen", "Lade Assets...", 0.2f);

            var animatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorControllerPath);
            var locomotionConfig = AssetDatabase.LoadAssetAtPath<Core.Locomotion.LocomotionConfig>(
                "Assets/Config/DefaultLocomotionConfig.asset");
            var inputActions = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
                "Assets/InputSystem_Actions.inputactions");

            if (animatorController == null)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError("[CharacterAnimationWizard] Animator Controller nicht gefunden. " +
                               "Bitte zuerst 'Setup Character Controller' ausführen.");
                return;
            }

            EditorUtility.DisplayProgressBar("Player Prefab erstellen", "Erstelle Prefab...", 0.5f);

            // === Root GameObject ===
            var root = new GameObject("Player");

            var motor = root.AddComponent<Core.Motor.CharacterMotor>();
            var motorSo = new SerializedObject(motor);
            motorSo.FindProperty("CapsuleRadius").floatValue = 0.3f;
            motorSo.FindProperty("CapsuleHeight").floatValue = 1.8f;
            motorSo.FindProperty("CapsuleYOffset").floatValue = 0.9f;
            motorSo.ApplyModifiedProperties();

            motor.MaxStableSlopeAngle = 60f;
            motor.StepHandling = Core.Motor.StepHandlingMethod.Standard;
            motor.MaxStepHeight = 0.35f;
            motor.LedgeAndDenivelationHandling = true;
            motor.MaxStableDistanceFromLedge = 0.5f;
            motor.InteractiveRigidbodyHandling = true;

            var playerController = root.AddComponent<Core.PlayerController>();
            if (locomotionConfig != null)
            {
                var pcSo = new SerializedObject(playerController);
                pcSo.FindProperty("_config").objectReferenceValue = locomotionConfig;
                pcSo.ApplyModifiedProperties();
            }

            var inputProvider = root.AddComponent<Core.Input.PlayerInputProvider>();
            if (inputActions != null)
            {
                var ipSo = new SerializedObject(inputProvider);
                ipSo.FindProperty("_inputActions").objectReferenceValue = inputActions;
                ipSo.ApplyModifiedProperties();
            }

            // === Character Model (Child) ===
            EditorUtility.DisplayProgressBar("Player Prefab erstellen", "Character Model...", 0.7f);

            var model = (GameObject)PrefabUtility.InstantiatePrefab(_characterModel);
            model.name = "CharacterModel";
            model.transform.SetParent(root.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            var animator = model.GetComponent<Animator>();
            if (animator == null)
                animator = model.AddComponent<Animator>();

            animator.runtimeAnimatorController = animatorController;
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            model.AddComponent<AnimatorParameterBridge>();

            // === Als Prefab speichern ===
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Player.prefab");
            Object.DestroyImmediate(root);

            EditorUtility.ClearProgressBar();

            if (prefab != null)
            {
                Debug.Log($"[CharacterAnimationWizard] Player Prefab erstellt mit Model: {modelPath}");
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
            }
        }

        private void RebuildAnimatorWithClips()
        {
            EditorUtility.DisplayProgressBar("Animator erstellen", "Controller erstellen...", 0.1f);

            // Bestehenden Controller löschen
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
            if (existing != null)
                AssetDatabase.DeleteAsset(AnimatorControllerPath);

            AnimatorControllerCreator.CreateController();

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
            if (controller == null)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError("[CharacterAnimationWizard] Controller konnte nicht erstellt werden.");
                return;
            }

            // Locomotion Blend Tree
            EditorUtility.DisplayProgressBar("Animator erstellen", "Locomotion Blend Tree...", 0.3f);

            var rootStateMachine = controller.layers[AnimationParameters.BaseLayerIndex].stateMachine;

            var blendTree = new BlendTree
            {
                name = "Locomotion",
                blendType = BlendTreeType.Simple1D,
                blendParameter = AnimationParameters.SpeedParam,
                useAutomaticThresholds = false
            };

            if (_idleClip != null) blendTree.AddChild(_idleClip, 0.0f);
            if (_walkClip != null) blendTree.AddChild(_walkClip, 0.5f);
            if (_runClip != null) blendTree.AddChild(_runClip, 1.0f);
            if (_sprintClip != null) blendTree.AddChild(_sprintClip, 1.5f);

            AssetDatabase.AddObjectToAsset(blendTree, controller);

            var locomotionState = rootStateMachine.AddState("Locomotion");
            locomotionState.motion = blendTree;
            locomotionState.iKOnFeet = true;
            locomotionState.writeDefaultValues = false;
            rootStateMachine.defaultState = locomotionState;

            // Airborne States
            EditorUtility.DisplayProgressBar("Animator erstellen", "Airborne + Slide States...", 0.5f);

            AddStateIfClip(rootStateMachine, "Jump", _jumpClip, false);
            AddStateIfClip(rootStateMachine, "Fall", _fallClip, false);
            AddStateIfClip(rootStateMachine, "SoftLand", _softLandClip, true);
            AddStateIfClip(rootStateMachine, "HardLand", _hardLandClip, true);
            AddStateIfClip(rootStateMachine, "Slide", _slideClip, false);

            // Stopping States
            EditorUtility.DisplayProgressBar("Animator erstellen", "Stopping States...", 0.7f);

            AddStateIfClip(rootStateMachine, "LightStop", _lightStopClip, true);
            AddStateIfClip(rootStateMachine, "MediumStop", _mediumStopClip, true);
            AddStateIfClip(rootStateMachine, "HardStop", _hardStopClip, true);

            // Avatar Masks
            EditorUtility.DisplayProgressBar("Animator erstellen", "Avatar Masks...", 0.9f);
            AvatarMaskCreator.CreateAllMasks();

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            EditorUtility.ClearProgressBar();

            int stateCount = rootStateMachine.states.Length;
            Debug.Log($"[CharacterAnimationWizard] Animator Controller erstellt mit {stateCount} States.");
        }

        private static void AddStateIfClip(AnimatorStateMachine stateMachine, string name,
            AnimationClip clip, bool ikOnFeet)
        {
            var state = stateMachine.AddState(name);
            state.motion = clip; // null ist OK — State existiert für CrossFade
            state.writeDefaultValues = false;
            state.iKOnFeet = ikOnFeet;

            if (clip == null)
            {
                Debug.LogWarning($"[CharacterAnimationWizard] Kein Clip für '{name}' — " +
                                 "State wurde ohne Motion erstellt.");
            }
        }

        private static AnimationClip LoadClipFromFbx(string fbxName)
        {
            var fbxPath = ClipBasePath + fbxName + ".fbx";
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            return assets?
                .OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        }

        #endregion
    }
}
