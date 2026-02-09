using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using Wiesenwischer.GameKit.CharacterController.Core;
using Wiesenwischer.GameKit.CharacterController.Core.Input;
using Wiesenwischer.GameKit.CharacterController.Core.Locomotion;
using Wiesenwischer.GameKit.CharacterController.Core.Motor;

namespace Wiesenwischer.GameKit.CharacterController.Animation.Editor
{
    /// <summary>
    /// Editor-Tool zum Erstellen des Player Prefabs mit animiertem Character.
    /// Menü: Wiesenwischer > GameKit > Create Player Prefab
    /// </summary>
    public static class PlayerPrefabCreator
    {
        private const string CharacterModelPath = "Assets/Characters/Song/Song.Fbx";

        private const string AnimatorControllerPath =
            "Packages/Wiesenwischer.GameKit.CharacterController.Animation/Resources/AnimatorControllers/CharacterAnimatorController.controller";

        private const string LocomotionConfigPath = "Assets/Config/DefaultLocomotionConfig.asset";
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";
        private const string OutputPath = "Assets/Prefabs/Player.prefab";

        [MenuItem("Wiesenwischer/GameKit/Prefabs/Create Player Prefab", false, 200)]
        public static void CreatePlayerPrefab()
        {
            // Assets laden
            var characterModel = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterModelPath);
            var animatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorControllerPath);
            var locomotionConfig = AssetDatabase.LoadAssetAtPath<LocomotionConfig>(LocomotionConfigPath);
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);

            if (characterModel == null)
            {
                Debug.LogError($"[PlayerPrefabCreator] Character Model nicht gefunden: {CharacterModelPath}");
                return;
            }

            if (animatorController == null)
            {
                Debug.LogError($"[PlayerPrefabCreator] Animator Controller nicht gefunden: {AnimatorControllerPath}");
                return;
            }

            if (locomotionConfig == null)
            {
                Debug.LogWarning($"[PlayerPrefabCreator] LocomotionConfig nicht gefunden: {LocomotionConfigPath}. " +
                                 "Bitte manuell im Inspector zuweisen.");
            }

            // === Root GameObject ===
            var root = new GameObject("Player");

            // CharacterMotor (erstellt automatisch CapsuleCollider via RequireComponent)
            var motor = root.AddComponent<CharacterMotor>();

            // Capsule-Dimensionen sind private serialized fields → SerializedObject
            var motorSo = new SerializedObject(motor);
            motorSo.FindProperty("CapsuleRadius").floatValue = 0.3f;
            motorSo.FindProperty("CapsuleHeight").floatValue = 1.8f;
            motorSo.FindProperty("CapsuleYOffset").floatValue = 0.9f;
            motorSo.ApplyModifiedProperties();

            // Öffentliche Felder direkt setzen
            motor.MaxStableSlopeAngle = 60f;
            motor.StepHandling = StepHandlingMethod.Standard;
            motor.MaxStepHeight = 0.35f;
            motor.LedgeAndDenivelationHandling = true;
            motor.MaxStableDistanceFromLedge = 0.5f;
            motor.InteractiveRigidbodyHandling = true;

            // PlayerController
            var playerController = root.AddComponent<PlayerController>();
            if (locomotionConfig != null)
            {
                var pcSo = new SerializedObject(playerController);
                pcSo.FindProperty("_config").objectReferenceValue = locomotionConfig;
                pcSo.ApplyModifiedProperties();
            }

            // PlayerInput (Unity Input System)
            var playerInput = root.AddComponent<PlayerInput>();
            if (inputActions != null)
            {
                playerInput.actions = inputActions;
                playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            }

            // PlayerInputProvider
            var inputProvider = root.AddComponent<PlayerInputProvider>();
            var ipSo = new SerializedObject(inputProvider);
            ipSo.FindProperty("_playerInput").objectReferenceValue = playerInput;
            ipSo.ApplyModifiedProperties();

            // === Character Model (Child) ===
            var model = (GameObject)PrefabUtility.InstantiatePrefab(characterModel);
            model.name = "CharacterModel";
            model.transform.SetParent(root.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            // Animator konfigurieren (vom FBX-Import bereits vorhanden)
            var animator = model.GetComponent<Animator>();
            if (animator == null)
                animator = model.AddComponent<Animator>();

            animator.runtimeAnimatorController = animatorController;
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            // AnimatorParameterBridge
            model.AddComponent<AnimatorParameterBridge>();

            // === Als Prefab speichern ===
            // Sicherstellen dass der Ordner existiert
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, OutputPath);
            Object.DestroyImmediate(root);

            if (prefab != null)
            {
                Debug.Log($"[PlayerPrefabCreator] Player Prefab erstellt: {OutputPath}");
                Debug.Log("[PlayerPrefabCreator] Bitte im Inspector prüfen:");
                Debug.Log("  - LocomotionConfig zugewiesen?");
                Debug.Log("  - Animator Controller + Avatar korrekt?");
                Debug.Log("  - Apply Root Motion = false?");
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
            }
            else
            {
                Debug.LogError("[PlayerPrefabCreator] Fehler beim Speichern des Prefabs!");
            }
        }

        [MenuItem("Wiesenwischer/GameKit/Prefabs/Create Player Prefab", true)]
        private static bool ValidateCreatePlayerPrefab()
        {
            return !Application.isPlaying;
        }
    }
}
