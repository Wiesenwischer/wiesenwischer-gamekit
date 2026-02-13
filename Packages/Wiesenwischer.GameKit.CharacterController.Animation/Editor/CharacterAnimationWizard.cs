using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core;

namespace Wiesenwischer.GameKit.CharacterController.Animation.Editor
{
    /// <summary>
    /// Zentraler Wizard zum Einrichten von Character Model und Animationen.
    /// Kombiniert: FBX-Model-Wahl, FBX-Import-Konfiguration (Humanoid, Root Transform),
    /// Clip-Zuweisung, Animator Controller Aufbau, IK-Setup, CapsuleCollider-Anpassung.
    /// Menü: Wiesenwischer > GameKit > Character & Animation Wizard
    /// </summary>
    public class CharacterAnimationWizard : EditorWindow
    {
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";

        private const string AnimatorControllerPath =
            "Packages/Wiesenwischer.GameKit.CharacterController.Animation/Resources/AnimatorControllers/CharacterAnimatorController.controller";

        private const string ClipBasePath = "Assets/Animations/Locomotion/";

        // --- Character Model ---
        private GameObject _characterModelFBX;
        private bool _adjustCapsule = true;
        private bool _runIKSetup = true;

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

        // --- UI State ---
        private Vector2 _scrollPos;
        private bool _foldLocomotion = true;
        private bool _foldAirborne = true;
        private bool _foldStopping = true;

        [MenuItem("Wiesenwischer/GameKit/Character & Animation Wizard", false, 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<CharacterAnimationWizard>("Character & Animation");
            window.minSize = new Vector2(420, 650);
            window.Show();
        }

        [MenuItem("Wiesenwischer/GameKit/Character & Animation Wizard", true)]
        private static bool ValidateShowWindow()
        {
            return !Application.isPlaying;
        }

        private void OnEnable()
        {
            AutoDetectCharacterModel();
            AutoDetectAnimationFBXs();
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
            _runIKSetup = EditorGUILayout.Toggle("IK-Komponenten einrichten", _runIKSetup);

            EditorGUILayout.Space(4);

            GUI.enabled = CanSwapModel();
            if (GUILayout.Button("Character Model austauschen", GUILayout.Height(28)))
            {
                PerformModelSwap();
            }
            GUI.enabled = true;

            // ========== ANIMATIONEN ==========
            EditorGUILayout.Space(12);
            DrawSeparator();
            EditorGUILayout.Space(4);
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

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            StatusRow("Player Prefab", prefab != null);

            StatusRow("Character Model",
                _characterModelFBX != null,
                _characterModelFBX != null ? _characterModelFBX.name : null);

            int count = CountAnimSlots();
            StatusRow("Animation FBXs", count > 0, $"{count}/12 zugewiesen");
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

        private bool CanSwapModel()
        {
            if (_characterModelFBX == null) return false;
            string path = AssetDatabase.GetAssetPath(_characterModelFBX);
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            return importer != null && importer.animationType == ModelImporterAnimationType.Human;
        }

        private bool HasAnyAnimation()
        {
            return _animIdle || _animWalk || _animRun || _animSprint ||
                   _animJump || _animFall || _animSoftLand || _animHardLand ||
                   _animLightStop || _animMediumStop || _animHardStop || _animSlide;
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
            return c;
        }

        #endregion

        #region Auto-Detect

        private void AutoDetectCharacterModel()
        {
            // Existierendes Prefab prüfen — Model daraus lesen
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
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

            // Fallback: Erste FBX in Assets/Characters/
            if (!AssetDatabase.IsValidFolder("Assets/Characters")) return;
            var guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/Characters" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                {
                    _characterModelFBX = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (_characterModelFBX != null) return;
                }
            }
        }

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

            // Fallback: Anim_Land als SoftLand
            if (_animSoftLand == null)
                _animSoftLand = LoadFbxAsset("Anim_Land");

            Debug.Log($"[CharacterAnimationWizard] Auto-Erkennung: {CountAnimSlots()}/12 FBXs gefunden.");
        }

        private static GameObject LoadFbxAsset(string fbxName)
        {
            var path = ClipBasePath + fbxName + ".fbx";
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        #endregion

        #region Model Swap

        private void PerformModelSwap()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);

            // Kein Prefab? → Neu erstellen
            if (prefab == null)
            {
                CreatePlayerPrefab();
                return;
            }

            var animatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorControllerPath);
            if (animatorController == null)
            {
                Debug.LogError("[CharacterAnimationWizard] Animator Controller nicht gefunden. " +
                               "Bitte zuerst 'Setup Character Controller' ausführen.");
                return;
            }

            var prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            var playerController = prefabRoot.GetComponent<PlayerController>();

            // Altes Modell entfernen
            var oldAnimator = prefabRoot.GetComponentInChildren<Animator>();
            if (oldAnimator != null)
            {
                string oldName = oldAnimator.gameObject.name;
                DestroyImmediate(oldAnimator.gameObject);
                Debug.Log($"[CharacterAnimationWizard] Altes Modell entfernt: {oldName}");
            }

            // Neues Modell einfügen
            var newModel = (GameObject)PrefabUtility.InstantiatePrefab(_characterModelFBX);
            newModel.name = "CharacterModel";
            newModel.transform.SetParent(prefabRoot.transform);
            newModel.transform.localPosition = Vector3.zero;
            newModel.transform.localRotation = Quaternion.identity;
            newModel.transform.localScale = Vector3.one;

            var newAnimator = newModel.GetComponent<Animator>();
            if (newAnimator == null)
                newAnimator = newModel.AddComponent<Animator>();

            newAnimator.runtimeAnimatorController = animatorController;
            newAnimator.applyRootMotion = false;
            newAnimator.updateMode = AnimatorUpdateMode.Normal;
            newAnimator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            // AnimatorParameterBridge
            var bridge = newModel.AddComponent<AnimatorParameterBridge>();
            var bridgeSo = new SerializedObject(bridge);
            bridgeSo.FindProperty("_playerController").objectReferenceValue = playerController;
            bridgeSo.ApplyModifiedProperties();

            if (_adjustCapsule)
                AdjustCapsule(prefabRoot, newModel);

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            Debug.Log($"[CharacterAnimationWizard] Character Model ausgetauscht: {_characterModelFBX.name}");

            // IK Setup (Reflection — IK-Package ist optional)
            if (_runIKSetup)
                RunIKSetup();

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            EditorGUIUtility.PingObject(Selection.activeObject);
        }

        private void CreatePlayerPrefab()
        {
            var animatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorControllerPath);
            var locomotionConfig = AssetDatabase.LoadAssetAtPath<Core.Locomotion.LocomotionConfig>(
                "Assets/Config/DefaultLocomotionConfig.asset");
            var inputActions = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
                "Assets/InputSystem_Actions.inputactions");

            if (animatorController == null)
            {
                Debug.LogError("[CharacterAnimationWizard] Animator Controller nicht gefunden. " +
                               "Bitte zuerst 'Setup Character Controller' ausführen.");
                return;
            }

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

            var playerController = root.AddComponent<PlayerController>();
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

            var model = (GameObject)PrefabUtility.InstantiatePrefab(_characterModelFBX);
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

            var bridge = model.AddComponent<AnimatorParameterBridge>();
            var bridgeSo = new SerializedObject(bridge);
            bridgeSo.FindProperty("_playerController").objectReferenceValue = playerController;
            bridgeSo.ApplyModifiedProperties();

            if (_adjustCapsule)
                AdjustCapsule(root, model);

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            DestroyImmediate(root);

            if (prefab != null)
            {
                Debug.Log($"[CharacterAnimationWizard] Player Prefab erstellt: {_characterModelFBX.name}");

                if (_runIKSetup)
                    RunIKSetup();

                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
            }
        }

        private static void AdjustCapsule(GameObject prefabRoot, GameObject model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            float height = bounds.size.y;
            float radius = Mathf.Max(bounds.extents.x, bounds.extents.z);

            var motor = prefabRoot.GetComponent<Core.Motor.CharacterMotor>();
            if (motor == null) return;

            var motorSo = new SerializedObject(motor);
            motorSo.FindProperty("CapsuleRadius").floatValue = Mathf.Clamp(radius, 0.15f, 0.5f);
            motorSo.FindProperty("CapsuleHeight").floatValue = height;
            motorSo.FindProperty("CapsuleYOffset").floatValue = height * 0.5f;
            motorSo.ApplyModifiedProperties();

            Debug.Log($"[CharacterAnimationWizard] CapsuleCollider: Height={height:F2}m, Radius={radius:F2}m");
        }

        private static void RunIKSetup()
        {
            // IK-Package ist optional — Aufruf via Reflection
            var ikType = System.Type.GetType(
                "Wiesenwischer.GameKit.CharacterController.IK.Editor.IKSetupWizard, " +
                "Wiesenwischer.GameKit.CharacterController.IK.Editor");

            if (ikType != null)
            {
                var method = ikType.GetMethod("SetupIKOnPrefab",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                method?.Invoke(null, null);
            }
            else
            {
                Debug.LogWarning("[CharacterAnimationWizard] IK-Package nicht installiert — IK-Setup übersprungen.");
            }
        }

        #endregion

        #region Animation Setup

        private void PerformAnimationSetup()
        {
            // Avatar vom Character Model holen (für "Copy From Other Avatar")
            Avatar sourceAvatar = GetSourceAvatar();
            if (sourceAvatar == null)
            {
                Debug.LogError("[CharacterAnimationWizard] Kein Avatar gefunden. Bitte Character Model FBX zuweisen " +
                               "und sicherstellen dass es als Humanoid importiert ist.");
                return;
            }

            Debug.Log($"[CharacterAnimationWizard] Source Avatar: {sourceAvatar.name} (isHuman={sourceAvatar.isHuman})");

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

            // Stopping (no loop, grounded → Feet, kein Bake XZ)
            configured += ConfigureAnim(_animLightStop, sourceAvatar, false, false, "LightStop", bakePositionXZ: false);
            configured += ConfigureAnim(_animMediumStop, sourceAvatar, false, false, "MediumStop", bakePositionXZ: false);
            configured += ConfigureAnim(_animHardStop, sourceAvatar, false, false, "HardStop", bakePositionXZ: false);

            if (configured > 0)
            {
                AssetDatabase.Refresh();
                AssignClipsToController();
            }

            Debug.Log($"=== {configured} Animation(en) eingerichtet! ===");
        }

        private Avatar GetSourceAvatar()
        {
            if (_characterModelFBX != null)
            {
                string modelPath = AssetDatabase.GetAssetPath(_characterModelFBX);
                var avatar = AssetDatabase.LoadAllAssetsAtPath(modelPath)
                    .OfType<Avatar>().FirstOrDefault();
                if (avatar != null) return avatar;
            }

            // Fallback: Avatar vom aktuellen Prefab-Modell
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab != null)
            {
                var animator = prefab.GetComponentInChildren<Animator>();
                if (animator != null) return animator.avatar;
            }

            return null;
        }

        /// <summary>
        /// Konfiguriert die Import-Settings einer Animation-FBX.
        /// Zwei-Schritt-Import: erst Humanoid-Rig, dann CopyFromOther + Clip-Settings.
        /// </summary>
        private static int ConfigureAnim(GameObject fbx, Avatar sourceAvatar, bool loop, bool airborne, string label,
            bool bakePositionXZ = true)
        {
            if (fbx == null) return 0;

            string path = AssetDatabase.GetAssetPath(fbx);
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[CharacterAnimationWizard] {label}: Kein gültiges FBX-Asset.");
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
                    // Airborne: Based Upon = Original
                    clip.keepOriginalPositionY = true;
                }
                else
                {
                    // Grounded: Based Upon = Feet
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

            Debug.Log($"[CharacterAnimationWizard] {label}: Import konfiguriert " +
                      $"(CopyFrom={sourceAvatar.name}, Loop={loop}, " +
                      $"{(airborne ? "Airborne/Original" : "Grounded/Feet")})");
            return 1;
        }

        /// <summary>
        /// Baut den Animator Controller vollständig auf und weist Clips zu. Idempotent.
        /// </summary>
        private void AssignClipsToController()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);

            if (controller == null)
            {
                AnimatorControllerCreator.CreateController();
                controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
                if (controller == null)
                {
                    Debug.LogError("[CharacterAnimationWizard] Animator Controller konnte nicht erstellt werden.");
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

            // === Einzelne States (immer erstellen, auch ohne Clip) ===
            string[] requiredStates = { "Jump", "Fall", "SoftLand", "HardLand", "Slide",
                                        "LightStop", "MediumStop", "HardStop" };
            foreach (string stateName in requiredStates)
                EnsureStateExists(rootSM, stateName);

            // Clips zuweisen
            TryAssignStateMotion(rootSM, "Jump", _animJump);
            TryAssignStateMotion(rootSM, "Fall", _animFall);
            TryAssignStateMotion(rootSM, "SoftLand", _animSoftLand);
            TryAssignStateMotion(rootSM, "HardLand", _animHardLand);
            TryAssignStateMotion(rootSM, "Slide", _animSlide);
            TryAssignStateMotion(rootSM, "LightStop", _animLightStop);
            TryAssignStateMotion(rootSM, "MediumStop", _animMediumStop);
            TryAssignStateMotion(rootSM, "HardStop", _animHardStop);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("[CharacterAnimationWizard] Animator Controller aktualisiert.");
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
