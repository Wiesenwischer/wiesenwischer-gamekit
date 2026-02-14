using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using Wiesenwischer.GameKit.CharacterController.Core;
using Wiesenwischer.GameKit.CharacterController.Core.Input;
using Wiesenwischer.GameKit.CharacterController.Core.Locomotion;
using Wiesenwischer.GameKit.CharacterController.Core.Motor;
using Wiesenwischer.GameKit.CharacterController.Core.Visual;

namespace Wiesenwischer.GameKit.CharacterController.Animation.Editor
{
    /// <summary>
    /// Zentrale Stelle für Player-Prefab-Erstellung und Model-Swap.
    /// Wird vom CharacterControllerSetupWizard und AnimationWizard verwendet.
    /// </summary>
    public static class PlayerPrefabCreator
    {
        private const string AnimatorControllerPath =
            "Packages/Wiesenwischer.GameKit.CharacterController.Animation/Resources/AnimatorControllers/CharacterAnimatorController.controller";

        private const string LocomotionConfigPath = "Assets/Config/DefaultLocomotionConfig.asset";
        private const string TransitionConfigPath = "Assets/Config/DefaultAnimationTransitionConfig.asset";
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";
        public const string OutputPath = "Assets/Prefabs/Player.prefab";

        /// <summary>
        /// Erstellt ein neues Player Prefab mit dem angegebenen Character Model.
        /// </summary>
        public static GameObject CreatePlayerPrefab(GameObject characterModelFBX, bool adjustCapsule = true)
        {
            if (characterModelFBX == null)
            {
                Debug.LogError("[PlayerPrefabCreator] Kein Character Model angegeben.");
                return null;
            }

            var animatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorControllerPath);
            if (animatorController == null)
            {
                Debug.LogError($"[PlayerPrefabCreator] Animator Controller nicht gefunden: {AnimatorControllerPath}");
                return null;
            }

            var locomotionConfig = AssetDatabase.LoadAssetAtPath<LocomotionConfig>(LocomotionConfigPath);
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);

            if (locomotionConfig == null)
            {
                Debug.LogWarning($"[PlayerPrefabCreator] LocomotionConfig nicht gefunden: {LocomotionConfigPath}. " +
                                 "Bitte manuell im Inspector zuweisen.");
            }

            // === Root GameObject ===
            var root = new GameObject("Player");

            var motor = root.AddComponent<CharacterMotor>();

            var motorSo = new SerializedObject(motor);
            motorSo.FindProperty("CapsuleRadius").floatValue = 0.3f;
            motorSo.FindProperty("CapsuleHeight").floatValue = 1.8f;
            motorSo.FindProperty("CapsuleYOffset").floatValue = 0.9f;
            motorSo.ApplyModifiedProperties();

            motor.MaxStableSlopeAngle = 60f;
            motor.StepHandling = StepHandlingMethod.Standard;
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

            var inputProvider = root.AddComponent<PlayerInputProvider>();
            if (inputActions != null)
            {
                var ipSo = new SerializedObject(inputProvider);
                ipSo.FindProperty("_inputActions").objectReferenceValue = inputActions;
                ipSo.ApplyModifiedProperties();
            }

            // === Character Model (Child) ===
            var model = (GameObject)PrefabUtility.InstantiatePrefab(characterModelFBX);
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

            var transitionConfig = AssetDatabase.LoadAssetAtPath<AnimationTransitionConfig>(TransitionConfigPath);

            var bridge = model.AddComponent<AnimatorParameterBridge>();
            var bridgeSo = new SerializedObject(bridge);
            bridgeSo.FindProperty("_playerController").objectReferenceValue = playerController;
            if (transitionConfig != null)
                bridgeSo.FindProperty("_transitionConfig").objectReferenceValue = transitionConfig;
            bridgeSo.ApplyModifiedProperties();

            // === GroundingSmoother ===
            var smoother = root.AddComponent<GroundingSmoother>();
            var smootherSo = new SerializedObject(smoother);
            smootherSo.FindProperty("_modelTransform").objectReferenceValue = model.transform;
            smootherSo.ApplyModifiedProperties();

            if (adjustCapsule)
                AdjustCapsule(root, model);

            // === Als Prefab speichern ===
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, OutputPath);
            Object.DestroyImmediate(root);

            if (prefab != null)
            {
                Debug.Log($"[PlayerPrefabCreator] Player Prefab erstellt: {characterModelFBX.name}");
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
            }
            else
            {
                Debug.LogError("[PlayerPrefabCreator] Fehler beim Speichern des Prefabs!");
            }

            return prefab;
        }

        /// <summary>
        /// Tauscht das Character Model in einem bestehenden Player Prefab aus.
        /// Falls kein Prefab existiert, wird ein neues erstellt.
        /// </summary>
        public static GameObject SwapModelInPrefab(GameObject characterModelFBX, bool adjustCapsule = true)
        {
            if (characterModelFBX == null)
            {
                Debug.LogError("[PlayerPrefabCreator] Kein Character Model angegeben.");
                return null;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputPath);
            if (prefab == null)
                return CreatePlayerPrefab(characterModelFBX, adjustCapsule);

            var animatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorControllerPath);
            if (animatorController == null)
            {
                Debug.LogError($"[PlayerPrefabCreator] Animator Controller nicht gefunden: {AnimatorControllerPath}");
                return null;
            }

            var prefabRoot = PrefabUtility.LoadPrefabContents(OutputPath);
            var playerController = prefabRoot.GetComponent<PlayerController>();

            // Altes Modell entfernen
            var oldAnimator = prefabRoot.GetComponentInChildren<Animator>();
            if (oldAnimator != null)
            {
                string oldName = oldAnimator.gameObject.name;
                Object.DestroyImmediate(oldAnimator.gameObject);
                Debug.Log($"[PlayerPrefabCreator] Altes Modell entfernt: {oldName}");
            }

            // Neues Modell einfügen
            var newModel = (GameObject)PrefabUtility.InstantiatePrefab(characterModelFBX);
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

            var transitionConfig = AssetDatabase.LoadAssetAtPath<AnimationTransitionConfig>(TransitionConfigPath);

            var bridge = newModel.AddComponent<AnimatorParameterBridge>();
            var bridgeSo = new SerializedObject(bridge);
            bridgeSo.FindProperty("_playerController").objectReferenceValue = playerController;
            if (transitionConfig != null)
                bridgeSo.FindProperty("_transitionConfig").objectReferenceValue = transitionConfig;
            bridgeSo.ApplyModifiedProperties();

            // GroundingSmoother: _modelTransform auf neues Modell zeigen
            var smoother = prefabRoot.GetComponent<GroundingSmoother>();
            if (smoother != null)
            {
                var smootherSo = new SerializedObject(smoother);
                smootherSo.FindProperty("_modelTransform").objectReferenceValue = newModel.transform;
                smootherSo.ApplyModifiedProperties();
            }

            if (adjustCapsule)
                AdjustCapsule(prefabRoot, newModel);

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, OutputPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            Debug.Log($"[PlayerPrefabCreator] Character Model ausgetauscht: {characterModelFBX.name}");

            var result = AssetDatabase.LoadAssetAtPath<GameObject>(OutputPath);
            Selection.activeObject = result;
            EditorGUIUtility.PingObject(result);
            return result;
        }

        /// <summary>
        /// Passt die CapsuleCollider-Dimensionen des CharacterMotor an das Model an.
        /// </summary>
        public static void AdjustCapsule(GameObject prefabRoot, GameObject model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            float height = bounds.size.y;
            float radius = Mathf.Max(bounds.extents.x, bounds.extents.z);

            var motor = prefabRoot.GetComponent<CharacterMotor>();
            if (motor == null) return;

            var motorSo = new SerializedObject(motor);
            motorSo.FindProperty("CapsuleRadius").floatValue = Mathf.Clamp(radius, 0.15f, 0.5f);
            motorSo.FindProperty("CapsuleHeight").floatValue = height;
            motorSo.FindProperty("CapsuleYOffset").floatValue = height * 0.5f;
            motorSo.ApplyModifiedProperties();

            Debug.Log($"[PlayerPrefabCreator] CapsuleCollider: Height={height:F2}m, Radius={radius:F2}m");
        }
    }
}
