using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core;

namespace Wiesenwischer.GameKit.CharacterController.Animation.Editor
{
    /// <summary>
    /// EditorWindow zum Austauschen von Character-Modell und Animationen.
    /// Konfiguriert FBX-Import-Settings automatisch (Humanoid, Avatar, Root Transform)
    /// und weist Clips dem Animator Controller zu.
    /// </summary>
    public class CharacterSwapWindow : EditorWindow
    {
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";

        private const string AnimatorControllerPath =
            "Packages/Wiesenwischer.GameKit.CharacterController.Animation/Resources/AnimatorControllers/CharacterAnimatorController.controller";

        // --- Character Model ---
        private GameObject _characterModelFBX;

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

        // --- Options ---
        private bool _runIKSetup = true;
        private bool _adjustCapsule = true;

        // --- UI State ---
        private Vector2 _scrollPos;
        private bool _foldLocomotion = true;
        private bool _foldAirborne = true;
        private bool _foldStopping = true;

        [MenuItem("Wiesenwischer/GameKit/Prefabs/Character Setup Wizard", false, 202)]
        private static void ShowWindow()
        {
            var window = GetWindow<CharacterSwapWindow>("Character Setup");
            window.minSize = new Vector2(400, 600);
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

            // ========== ANIMATIONS ==========
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

            EditorGUILayout.Space(4);

            // Locomotion
            _foldLocomotion = EditorGUILayout.Foldout(_foldLocomotion, "Locomotion (Loop)", true, EditorStyles.foldoutHeader);
            if (_foldLocomotion)
            {
                EditorGUI.indentLevel++;
                _animIdle = DrawAnimSlot("Idle", _animIdle);
                _animWalk = DrawAnimSlot("Walk", _animWalk);
                _animRun = DrawAnimSlot("Run", _animRun);
                _animSprint = DrawAnimSlot("Sprint", _animSprint);
                EditorGUI.indentLevel--;
            }

            // Airborne
            _foldAirborne = EditorGUILayout.Foldout(_foldAirborne, "Airborne & Landing", true, EditorStyles.foldoutHeader);
            if (_foldAirborne)
            {
                EditorGUI.indentLevel++;
                _animJump = DrawAnimSlot("Jump", _animJump);
                _animFall = DrawAnimSlot("Fall", _animFall);
                _animSoftLand = DrawAnimSlot("Soft Land", _animSoftLand);
                _animHardLand = DrawAnimSlot("Hard Land", _animHardLand);
                EditorGUI.indentLevel--;
            }

            // Stopping
            _foldStopping = EditorGUILayout.Foldout(_foldStopping, "Stopping", true, EditorStyles.foldoutHeader);
            if (_foldStopping)
            {
                EditorGUI.indentLevel++;
                _animLightStop = DrawAnimSlot("Light Stop (Walk)", _animLightStop);
                _animMediumStop = DrawAnimSlot("Medium Stop (Run)", _animMediumStop);
                _animHardStop = DrawAnimSlot("Hard Stop (Sprint)", _animHardStop);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(8);

            GUI.enabled = HasAnyAnimation();
            if (GUILayout.Button("Animationen einrichten", GUILayout.Height(28)))
            {
                PerformAnimationSetup();
            }
            GUI.enabled = true;

            EditorGUILayout.Space(8);
            EditorGUILayout.EndScrollView();
        }

        #region UI Helpers

        private static GameObject DrawAnimSlot(string label, GameObject current)
        {
            return (GameObject)EditorGUILayout.ObjectField(label, current, typeof(GameObject), false);
        }

        private static void DrawSeparator()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
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
                   _animLightStop || _animMediumStop || _animHardStop;
        }

        #endregion

        #region Model Swap

        private void PerformModelSwap()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[CharacterSetup] Player Prefab nicht gefunden: {PlayerPrefabPath}");
                return;
            }

            var animatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorControllerPath);
            if (animatorController == null)
            {
                Debug.LogError($"[CharacterSetup] Animator Controller nicht gefunden: {AnimatorControllerPath}");
                return;
            }

            var prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            var playerController = prefabRoot.GetComponent<PlayerController>();

            // Altes Modell entfernen
            var oldAnimator = prefabRoot.GetComponentInChildren<Animator>();
            if (oldAnimator != null)
            {
                string oldName = oldAnimator.gameObject.name;
                Object.DestroyImmediate(oldAnimator.gameObject);
                Debug.Log($"[CharacterSetup] Altes Modell entfernt: {oldName}");
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

            Debug.Log($"[CharacterSetup] Neues Modell eingesetzt: {_characterModelFBX.name}");

            // IK Setup
            if (_runIKSetup)
            {
                if (!EditorApplication.ExecuteMenuItem("Wiesenwischer/GameKit/Prefabs/Setup IK on Player Prefab"))
                {
                    Debug.LogWarning("[CharacterSetup] IK-Wizard nicht gefunden. Manuell ausführen: " +
                                     "Wiesenwischer > GameKit > Prefabs > Setup IK on Player Prefab");
                }
            }

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            EditorGUIUtility.PingObject(Selection.activeObject);
            Debug.Log("=== Character Model Swap abgeschlossen! ===");
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

            Debug.Log($"[CharacterSetup] CapsuleCollider: Height={height:F2}m, Radius={radius:F2}m");
        }

        #endregion

        #region Animation Setup

        private void PerformAnimationSetup()
        {
            // Avatar vom Character Model holen (für "Copy From Other Avatar")
            Avatar sourceAvatar = null;
            if (_characterModelFBX != null)
            {
                string modelPath = AssetDatabase.GetAssetPath(_characterModelFBX);
                sourceAvatar = AssetDatabase.LoadAllAssetsAtPath(modelPath)
                    .OfType<Avatar>().FirstOrDefault();

                if (sourceAvatar == null)
                {
                    Debug.LogWarning($"[CharacterSetup] Kein Avatar im Character Model gefunden: {modelPath}. " +
                                     "Ist das Modell als Humanoid importiert?");
                }
            }

            if (sourceAvatar == null)
            {
                // Fallback: Avatar vom aktuellen Prefab-Modell
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
                if (prefab != null)
                {
                    var animator = prefab.GetComponentInChildren<Animator>();
                    sourceAvatar = animator != null ? animator.avatar : null;
                }
            }

            if (sourceAvatar == null)
            {
                Debug.LogError("[CharacterSetup] Kein Avatar gefunden. Bitte Character Model FBX zuweisen " +
                               "und sicherstellen dass es als Humanoid importiert ist.");
                return;
            }

            Debug.Log($"[CharacterSetup] Source Avatar: {sourceAvatar.name} (isHuman={sourceAvatar.isHuman})");

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

        /// <summary>
        /// Konfiguriert die Import-Settings einer Animation-FBX.
        /// Zwei-Schritt-Import: erst Humanoid-Rig erstellen, dann CopyFromOther + Clip-Settings.
        /// </summary>
        /// <returns>1 wenn konfiguriert, 0 wenn übersprungen.</returns>
        private static int ConfigureAnim(GameObject fbx, Avatar sourceAvatar, bool loop, bool airborne, string label,
            bool bakePositionXZ = true)
        {
            if (fbx == null) return 0;

            string path = AssetDatabase.GetAssetPath(fbx);
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[CharacterSetup] {label}: Kein gültiges FBX-Asset.");
                return 0;
            }

            // === Schritt 1: Erst als Humanoid importieren ===
            // Unity muss die FBX einmal als Humanoid verarbeiten, bevor
            // CopyFromOther + sourceAvatar gesetzt werden können.
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.SaveAndReimport();
                // Importer neu laden nach Reimport
                importer = AssetImporter.GetAtPath(path) as ModelImporter;
            }

            // === Schritt 2: CopyFromOther + Clip Settings ===
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = sourceAvatar;

            // Clip Settings: Root Transform + Loop
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
                    // Airborne (Jump/Fall/Land): Based Upon = Original
                    clip.keepOriginalPositionY = true;
                }
                else
                {
                    // Grounded: Based Upon = Feet
                    clip.keepOriginalPositionY = false;
                    clip.heightFromFeet = true;
                }

                // Root Transform Position XZ
                // Locomotion: Bake Into Pose (In-Place Animations)
                // Stopping: NICHT baken — Root Motion wird verworfen (applyRootMotion=false),
                //           sonst läuft das Mesh vom Collider weg auf Rampen.
                clip.lockRootPositionXZ = bakePositionXZ;
                clip.keepOriginalPositionXZ = bakePositionXZ;

                // Loop Time
                clip.loopTime = loop;

                importer.clipAnimations = clips;
            }

            importer.SaveAndReimport();

            // Verify
            var verifyImporter = AssetImporter.GetAtPath(path) as ModelImporter;
            if (verifyImporter != null && verifyImporter.sourceAvatar == null)
            {
                Debug.LogWarning($"[CharacterSetup] {label}: sourceAvatar nach Reimport verloren! " +
                                 "Bitte manuell unter Rig → Copy From Other Avatar zuweisen.");
            }

            Debug.Log($"[CharacterSetup] {label}: Import konfiguriert " +
                      $"(CopyFrom={sourceAvatar.name}, Loop={loop}, " +
                      $"{(airborne ? "Airborne/Original" : "Grounded/Feet")})");
            return 1;
        }

        /// <summary>
        /// Baut den Animator Controller vollständig auf (Controller, Parameter, Blend Tree, States)
        /// und weist die Clips zu. Idempotent — kann beliebig oft aufgerufen werden.
        /// </summary>
        private void AssignClipsToController()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);

            // === Controller erstellen falls nicht vorhanden ===
            if (controller == null)
            {
                Debug.Log("[CharacterSetup] Animator Controller nicht vorhanden — wird erstellt...");
                AnimatorControllerCreator.CreateController();
                controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
                if (controller == null)
                {
                    Debug.LogError("[CharacterSetup] Animator Controller konnte nicht erstellt werden.");
                    return;
                }
            }

            var rootSM = controller.layers[AnimationParameters.BaseLayerIndex].stateMachine;

            // === Locomotion Blend Tree: finden oder erstellen ===
            var locomotionState = FindState(rootSM, "Locomotion");
            BlendTree blendTree = null;

            if (locomotionState != null && locomotionState.motion is BlendTree existingTree)
            {
                blendTree = existingTree;
            }
            else
            {
                // Locomotion State + Blend Tree neu erstellen
                if (locomotionState != null)
                    rootSM.RemoveState(locomotionState);

                blendTree = new BlendTree
                {
                    name = "Locomotion",
                    blendType = BlendTreeType.Simple1D,
                    blendParameter = AnimationParameters.SpeedParam,
                    useAutomaticThresholds = false
                };

                // 4 leere Plätze mit korrekten Thresholds: Idle=0, Walk=0.5, Run=1.0, Sprint=1.5
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

                Debug.Log("[CharacterSetup] Locomotion Blend Tree erstellt.");
            }

            // === Locomotion Clips zuweisen ===
            var children = blendTree.children;
            if (children.Length >= 4)
            {
                TryAssignBlendTreeChild(ref children, 0, _animIdle, "Idle");
                TryAssignBlendTreeChild(ref children, 1, _animWalk, "Walk");
                TryAssignBlendTreeChild(ref children, 2, _animRun, "Run");
                TryAssignBlendTreeChild(ref children, 3, _animSprint, "Sprint");
                blendTree.children = children;
            }

            // === Einzelne States: finden oder erstellen + Clips zuweisen ===
            TryAssignStateMotion(rootSM, "Jump", _animJump);
            TryAssignStateMotion(rootSM, "Fall", _animFall);
            TryAssignStateMotion(rootSM, "SoftLand", _animSoftLand);
            TryAssignStateMotion(rootSM, "HardLand", _animHardLand);
            TryAssignStateMotion(rootSM, "LightStop", _animLightStop);
            TryAssignStateMotion(rootSM, "MediumStop", _animMediumStop);
            TryAssignStateMotion(rootSM, "HardStop", _animHardStop);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("[CharacterSetup] Animator Controller vollständig aufgebaut.");
        }

        private static void TryAssignBlendTreeChild(ref ChildMotion[] children, int index, GameObject fbx, string label)
        {
            if (fbx == null) return;

            var clip = LoadClipFromFBX(fbx);
            if (clip == null)
            {
                Debug.LogWarning($"[CharacterSetup] {label}: Kein AnimationClip im FBX gefunden.");
                return;
            }

            children[index].motion = clip;
            Debug.Log($"[CharacterSetup] Blend Tree: {label} → {clip.name}");
        }

        private static void TryAssignStateMotion(AnimatorStateMachine sm, string stateName, GameObject fbx)
        {
            if (fbx == null) return;

            var clip = LoadClipFromFBX(fbx);
            if (clip == null)
            {
                Debug.LogWarning($"[CharacterSetup] {stateName}: Kein AnimationClip im FBX gefunden.");
                return;
            }

            var state = FindState(sm, stateName);
            if (state == null)
            {
                state = sm.AddState(stateName);
                state.writeDefaultValues = false;
                state.iKOnFeet = true;
                Debug.Log($"[CharacterSetup] State '{stateName}' erstellt.");
            }

            state.motion = clip;
            Debug.Log($"[CharacterSetup] State: {stateName} → {clip.name}");
        }

        #endregion

        #region Utilities

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
