using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Animation.Editor
{
    /// <summary>
    /// EditorWindow zum Einrichten der Animations-Clips.
    /// Konfiguriert FBX-Import-Settings und weist Clips dem Animator Controller zu.
    /// Menü: Wiesenwischer > GameKit > Animation > Animation Wizard
    /// </summary>
    public class AnimationWizard : EditorWindow
    {
        private const string AnimatorControllerPath =
            "Packages/Wiesenwischer.GameKit.CharacterController.Animation/Resources/AnimatorControllers/CharacterAnimatorController.controller";

        private const string ClipBasePath = "Assets/Animations/Locomotion/";

        // --- Animation FBX Slots ---
        private GameObject _animIdle;
        private GameObject _animWalk;
        private GameObject _animRun;
        private GameObject _animSprint;
        private GameObject _animJump;
        private GameObject _animFall;
        private GameObject _animSoftLand;
        private GameObject _animHardLand;
        private GameObject _animLightStop;
        private GameObject _animMediumStop;
        private GameObject _animHardStop;
        private GameObject _animSlide;
        private GameObject _animRoll;
        private GameObject _animCrouchIdle;
        private GameObject _animCrouchWalk;

        // --- UI State ---
        private Vector2 _scrollPos;
        private bool _foldLocomotion = true;
        private bool _foldAirborne = true;
        private bool _foldStopping = true;
        private bool _foldCrouching = true;

        [MenuItem("Wiesenwischer/GameKit/Animation/Animation Wizard", false, 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<AnimationWizard>("Animation Wizard");
            window.minSize = new Vector2(420, 550);
            window.Show();
        }

        [MenuItem("Wiesenwischer/GameKit/Animation/Animation Wizard", true)]
        private static bool ValidateShowWindow()
        {
            return !Application.isPlaying;
        }

        private void OnEnable()
        {
            AutoDetectAnimationFBXs();
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            // ========== ANIMATIONEN ==========
            EditorGUILayout.LabelField("Animationen", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "FBX-Dateien hier reinziehen. Der Wizard konfiguriert automatisch:\n" +
                "  - Humanoid Rig + Copy From Other Avatar\n" +
                "  - Root Transform Settings (Bake Into Pose)\n" +
                "  - Loop Time (je nach Typ)\n" +
                "  - Clip-Zuweisung im Animator Controller\n\n" +
                "Leere Slots werden nicht verändert.",
                MessageType.Info);

            EditorGUILayout.Space(2);

            if (GUILayout.Button("Auto-Erkennung (Assets/Animations/Locomotion/)", GUILayout.Height(22)))
            {
                AutoDetectAnimationFBXs();
            }

            EditorGUILayout.Space(4);

            // Locomotion
            _foldLocomotion = EditorGUILayout.Foldout(_foldLocomotion, "Locomotion (Loop)", true, EditorStyles.foldoutHeader);
            if (_foldLocomotion)
            {
                EditorGUI.indentLevel++;
                _animIdle = FbxSlot("Idle", _animIdle);
                _animWalk = FbxSlot("Walk", _animWalk);
                _animRun = FbxSlot("Run", _animRun);
                _animSprint = FbxSlot("Sprint", _animSprint);
                EditorGUI.indentLevel--;
            }

            // Airborne + Slide
            _foldAirborne = EditorGUILayout.Foldout(_foldAirborne, "Airborne + Slide", true, EditorStyles.foldoutHeader);
            if (_foldAirborne)
            {
                EditorGUI.indentLevel++;
                _animJump = FbxSlot("Jump", _animJump);
                _animFall = FbxSlot("Fall", _animFall);
                _animSoftLand = FbxSlot("Soft Land", _animSoftLand);
                _animHardLand = FbxSlot("Hard Land", _animHardLand);
                _animSlide = FbxSlot("Slide", _animSlide);
                _animRoll = FbxSlot("Roll", _animRoll);
                EditorGUI.indentLevel--;
            }

            // Stopping
            _foldStopping = EditorGUILayout.Foldout(_foldStopping, "Stopping", true, EditorStyles.foldoutHeader);
            if (_foldStopping)
            {
                EditorGUI.indentLevel++;
                _animLightStop = FbxSlot("Light Stop (Walk)", _animLightStop);
                _animMediumStop = FbxSlot("Medium Stop (Run)", _animMediumStop);
                _animHardStop = FbxSlot("Hard Stop (Sprint)", _animHardStop);
                EditorGUI.indentLevel--;
            }

            // Crouching
            _foldCrouching = EditorGUILayout.Foldout(_foldCrouching, "Crouching (Loop)", true, EditorStyles.foldoutHeader);
            if (_foldCrouching)
            {
                EditorGUI.indentLevel++;
                _animCrouchIdle = FbxSlot("Crouch Idle", _animCrouchIdle);
                _animCrouchWalk = FbxSlot("Crouch Walk", _animCrouchWalk);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(8);

            GUI.enabled = HasAnyAnimation();
            if (GUILayout.Button("Animationen einrichten", GUILayout.Height(28)))
            {
                PerformAnimationSetup();
            }
            GUI.enabled = true;

            // ========== STATUS ==========
            EditorGUILayout.Space(12);
            DrawSeparator();
            EditorGUILayout.Space(4);
            DrawStatusSection();

            EditorGUILayout.Space(8);
            EditorGUILayout.EndScrollView();
        }

        #region UI Helpers

        private static GameObject FbxSlot(string label, GameObject current)
        {
            return (GameObject)EditorGUILayout.ObjectField(label, current, typeof(GameObject), false);
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

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
            StatusRow("Animator Controller",
                controller != null,
                controller != null ? $"{controller.layers[0].stateMachine.states.Length} States" : null);

            int count = CountAnimSlots();
            StatusRow("Animation FBXs", count > 0, $"{count}/15 zugewiesen");

            // Character Model aus Prefab anzeigen (read-only)
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabCreator.OutputPath);
            if (prefab != null)
            {
                var animator = prefab.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    var source = PrefabUtility.GetCorrespondingObjectFromSource(animator.gameObject);
                    StatusRow("Character Model", source != null, source != null ? source.name : null);
                }
            }
            else
            {
                StatusRow("Player Prefab", false, "Bitte zuerst 'Setup Character Controller' ausführen");
            }
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

        #region Validation

        private bool HasAnyAnimation()
        {
            return _animIdle || _animWalk || _animRun || _animSprint ||
                   _animJump || _animFall || _animSoftLand || _animHardLand ||
                   _animLightStop || _animMediumStop || _animHardStop || _animSlide ||
                   _animRoll || _animCrouchIdle || _animCrouchWalk;
        }

        private int CountAnimSlots()
        {
            int c = 0;
            if (_animIdle) c++;
            if (_animWalk) c++;
            if (_animRun) c++;
            if (_animSprint) c++;
            if (_animJump) c++;
            if (_animFall) c++;
            if (_animSoftLand) c++;
            if (_animHardLand) c++;
            if (_animLightStop) c++;
            if (_animMediumStop) c++;
            if (_animHardStop) c++;
            if (_animSlide) c++;
            if (_animRoll) c++;
            if (_animCrouchIdle) c++;
            if (_animCrouchWalk) c++;
            return c;
        }

        #endregion

        #region Auto-Detect

        private void AutoDetectAnimationFBXs()
        {
            _animIdle = LoadFbxAsset("Anim_Idle");
            _animWalk = LoadFbxAsset("Anim_Walk");
            _animRun = LoadFbxAsset("Anim_Run");
            _animSprint = LoadFbxAsset("Anim_Sprint");
            _animJump = LoadFbxAsset("Anim_Jump");
            _animFall = LoadFbxAsset("Anim_Fall");
            _animSoftLand = LoadFbxAsset("Anim_SoftLand");
            _animHardLand = LoadFbxAsset("Anim_HardLand");
            _animLightStop = LoadFbxAsset("Anim_LightStop");
            _animMediumStop = LoadFbxAsset("Anim_MediumStop");
            _animHardStop = LoadFbxAsset("Anim_HardStop");
            _animSlide = LoadFbxAsset("Anim_Slide");
            _animRoll = LoadFbxAsset("Anim_Roll");
            _animCrouchIdle = LoadFbxAsset("Anim_CrouchIdle");
            _animCrouchWalk = LoadFbxAsset("Anim_CrouchWalk");

            // Fallback: Anim_Land als SoftLand
            if (_animSoftLand == null)
                _animSoftLand = LoadFbxAsset("Anim_Land");

            Debug.Log($"[AnimationWizard] Auto-Erkennung: {CountAnimSlots()}/15 FBXs gefunden.");
        }

        private static GameObject LoadFbxAsset(string fbxName)
        {
            var path = ClipBasePath + fbxName + ".fbx";
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        #endregion

        #region Animation Setup

        private void PerformAnimationSetup()
        {
            Avatar sourceAvatar = GetSourceAvatar();
            if (sourceAvatar == null)
            {
                Debug.LogError("[AnimationWizard] Kein Avatar gefunden. " +
                               "Bitte zuerst 'Setup Character Controller' ausführen und ein Humanoid-Model zuweisen.");
                return;
            }

            Debug.Log($"[AnimationWizard] Source Avatar: {sourceAvatar.name} (isHuman={sourceAvatar.isHuman})");

            int configured = 0;

            // Locomotion (loop, grounded → Feet)
            configured += ConfigureAnim(_animIdle, sourceAvatar, true, false, "Idle");
            configured += ConfigureAnim(_animWalk, sourceAvatar, true, false, "Walk");
            configured += ConfigureAnim(_animRun, sourceAvatar, true, false, "Run");
            configured += ConfigureAnim(_animSprint, sourceAvatar, true, false, "Sprint");

            // Airborne (airborne → Original für Y)
            configured += ConfigureAnim(_animJump, sourceAvatar, false, true, "Jump");
            configured += ConfigureAnim(_animFall, sourceAvatar, true, true, "Fall");
            configured += ConfigureAnim(_animSoftLand, sourceAvatar, false, true, "SoftLand");
            configured += ConfigureAnim(_animHardLand, sourceAvatar, false, true, "HardLand");
            configured += ConfigureAnim(_animSlide, sourceAvatar, true, false, "Slide");
            configured += ConfigureAnim(_animRoll, sourceAvatar, false, false, "Roll");

            // Stopping (no loop, grounded → Feet, kein Bake XZ)
            configured += ConfigureAnim(_animLightStop, sourceAvatar, false, false, "LightStop", bakePositionXZ: false);
            configured += ConfigureAnim(_animMediumStop, sourceAvatar, false, false, "MediumStop", bakePositionXZ: false);
            configured += ConfigureAnim(_animHardStop, sourceAvatar, false, false, "HardStop", bakePositionXZ: false);

            // Crouching (loop, grounded → Feet)
            configured += ConfigureAnim(_animCrouchIdle, sourceAvatar, true, false, "CrouchIdle");
            configured += ConfigureAnim(_animCrouchWalk, sourceAvatar, true, false, "CrouchWalk");

            if (configured > 0)
            {
                AssetDatabase.Refresh();
                AssignClipsToController();
            }

            Debug.Log($"=== {configured} Animation(en) eingerichtet! ===");
        }

        private static Avatar GetSourceAvatar()
        {
            // Avatar vom Player Prefab holen
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabCreator.OutputPath);
            if (prefab != null)
            {
                var animator = prefab.GetComponentInChildren<Animator>();
                if (animator != null && animator.avatar != null)
                    return animator.avatar;

                // Fallback: Avatar direkt aus dem Source-FBX
                var source = PrefabUtility.GetCorrespondingObjectFromSource(animator != null ? animator.gameObject : null);
                if (source != null)
                {
                    string modelPath = AssetDatabase.GetAssetPath(source);
                    var avatar = AssetDatabase.LoadAllAssetsAtPath(modelPath)
                        .OfType<Avatar>().FirstOrDefault();
                    if (avatar != null) return avatar;
                }
            }

            return null;
        }

        private static int ConfigureAnim(GameObject fbx, Avatar sourceAvatar, bool loop, bool airborne, string label,
            bool bakePositionXZ = true)
        {
            if (fbx == null) return 0;

            string path = AssetDatabase.GetAssetPath(fbx);
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[AnimationWizard] {label}: Kein gültiges FBX-Asset.");
                return 0;
            }

            // Schritt 1: Als Humanoid importieren
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.SaveAndReimport();
                importer = AssetImporter.GetAtPath(path) as ModelImporter;
            }

            // Schritt 2: CopyFromOther + Clip Settings
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = sourceAvatar;

            var clips = importer.defaultClipAnimations;
            if (clips.Length > 0)
            {
                var clip = clips[0];

                // Root Transform Rotation: Bake Into Pose, Body Orientation
                clip.lockRootRotation = true;
                clip.keepOriginalOrientation = false;

                // Root Transform Position Y: Bake Into Pose
                clip.lockRootHeightY = true;
                if (airborne)
                {
                    clip.keepOriginalPositionY = true;
                }
                else
                {
                    clip.keepOriginalPositionY = false;
                    clip.heightFromFeet = true;
                }

                // Root Transform Position XZ
                clip.lockRootPositionXZ = bakePositionXZ;
                clip.keepOriginalPositionXZ = bakePositionXZ;

                clip.loopTime = loop;
                importer.clipAnimations = clips;
            }

            importer.SaveAndReimport();

            Debug.Log($"[AnimationWizard] {label}: Import konfiguriert " +
                      $"(CopyFrom={sourceAvatar.name}, Loop={loop}, " +
                      $"{(airborne ? "Airborne/Original" : "Grounded/Feet")})");
            return 1;
        }

        private void AssignClipsToController()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);

            if (controller == null)
            {
                AnimatorControllerCreator.CreateController();
                controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
                if (controller == null)
                {
                    Debug.LogError("[AnimationWizard] Animator Controller konnte nicht erstellt werden.");
                    return;
                }
            }

            var rootSM = controller.layers[AnimationParameters.BaseLayerIndex].stateMachine;

            // === Locomotion Blend Tree ===
            var locomotionState = FindState(rootSM, "Locomotion");
            BlendTree blendTree = null;

            if (locomotionState != null && locomotionState.motion is BlendTree existingTree)
            {
                blendTree = existingTree;
            }
            else
            {
                if (locomotionState != null)
                    rootSM.RemoveState(locomotionState);

                blendTree = new BlendTree
                {
                    name = "Locomotion",
                    blendType = BlendTreeType.Simple1D,
                    blendParameter = AnimationParameters.SpeedParam,
                    useAutomaticThresholds = false
                };

                var emptyClip = new AnimationClip { name = "_empty" };
                blendTree.AddChild(emptyClip, 0.0f);
                blendTree.AddChild(emptyClip, 0.5f);
                blendTree.AddChild(emptyClip, 1.0f);
                blendTree.AddChild(emptyClip, 1.5f);

                AssetDatabase.AddObjectToAsset(blendTree, controller);

                locomotionState = rootSM.AddState("Locomotion");
                locomotionState.motion = blendTree;
                locomotionState.iKOnFeet = true;
                locomotionState.writeDefaultValues = false;
                rootSM.defaultState = locomotionState;
            }

            // Locomotion Clips zuweisen
            var children = blendTree.children;
            if (children.Length >= 4)
            {
                TryAssignBlendTreeChild(ref children, 0, _animIdle, "Idle");
                TryAssignBlendTreeChild(ref children, 1, _animWalk, "Walk");
                TryAssignBlendTreeChild(ref children, 2, _animRun, "Run");
                TryAssignBlendTreeChild(ref children, 3, _animSprint, "Sprint");
                blendTree.children = children;
            }

            // === Crouch Blend Tree ===
            if (_animCrouchIdle != null || _animCrouchWalk != null)
            {
                var crouchState = FindState(rootSM, "Crouch");
                BlendTree crouchTree = null;

                if (crouchState != null && crouchState.motion is BlendTree existingCrouchTree)
                {
                    crouchTree = existingCrouchTree;
                }
                else
                {
                    if (crouchState != null)
                        rootSM.RemoveState(crouchState);

                    crouchTree = new BlendTree
                    {
                        name = "Crouch",
                        blendType = BlendTreeType.Simple1D,
                        blendParameter = AnimationParameters.SpeedParam,
                        useAutomaticThresholds = false
                    };

                    var emptyClip = new AnimationClip { name = "_empty_crouch" };
                    crouchTree.AddChild(emptyClip, 0.0f);
                    crouchTree.AddChild(emptyClip, 0.5f);

                    AssetDatabase.AddObjectToAsset(crouchTree, controller);

                    crouchState = rootSM.AddState("Crouch");
                    crouchState.motion = crouchTree;
                    crouchState.iKOnFeet = true;
                    crouchState.writeDefaultValues = false;
                }

                var crouchChildren = crouchTree.children;
                if (crouchChildren.Length >= 2)
                {
                    TryAssignBlendTreeChild(ref crouchChildren, 0, _animCrouchIdle, "CrouchIdle");
                    TryAssignBlendTreeChild(ref crouchChildren, 1, _animCrouchWalk, "CrouchWalk");
                    crouchTree.children = crouchChildren;
                }
            }

            // === Einzelne States ===
            string[] requiredStates = { "Jump", "Fall", "SoftLand", "HardLand", "Slide", "Roll",
                                        "LightStop", "MediumStop", "HardStop" };
            foreach (string stateName in requiredStates)
                EnsureStateExists(rootSM, stateName);

            TryAssignStateMotion(rootSM, "Jump", _animJump);
            TryAssignStateMotion(rootSM, "Fall", _animFall);
            TryAssignStateMotion(rootSM, "SoftLand", _animSoftLand);
            TryAssignStateMotion(rootSM, "HardLand", _animHardLand);
            TryAssignStateMotion(rootSM, "Slide", _animSlide);
            TryAssignStateMotion(rootSM, "Roll", _animRoll);
            TryAssignStateMotion(rootSM, "LightStop", _animLightStop);
            TryAssignStateMotion(rootSM, "MediumStop", _animMediumStop);
            TryAssignStateMotion(rootSM, "HardStop", _animHardStop);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("[AnimationWizard] Animator Controller aktualisiert.");
        }

        #endregion

        #region Utilities

        private static void TryAssignBlendTreeChild(ref ChildMotion[] children, int index, GameObject fbx, string label)
        {
            if (fbx == null) return;
            var clip = LoadClipFromFBX(fbx);
            if (clip == null) return;
            children[index].motion = clip;
        }

        private static void EnsureStateExists(AnimatorStateMachine sm, string stateName)
        {
            var state = FindState(sm, stateName);
            if (state != null) return;
            state = sm.AddState(stateName);
            state.writeDefaultValues = false;
            state.iKOnFeet = true;
        }

        private static void TryAssignStateMotion(AnimatorStateMachine sm, string stateName, GameObject fbx)
        {
            if (fbx == null) return;
            var clip = LoadClipFromFBX(fbx);
            if (clip == null) return;

            var state = FindState(sm, stateName);
            if (state == null)
            {
                state = sm.AddState(stateName);
                state.writeDefaultValues = false;
                state.iKOnFeet = true;
            }

            state.motion = clip;
        }

        private static AnimatorState FindState(AnimatorStateMachine sm, string name)
        {
            return sm.states
                .Select(s => s.state)
                .FirstOrDefault(s => s.name == name);
        }

        private static AnimationClip LoadClipFromFBX(GameObject fbx)
        {
            string path = AssetDatabase.GetAssetPath(fbx);
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        }

        #endregion
    }
}
