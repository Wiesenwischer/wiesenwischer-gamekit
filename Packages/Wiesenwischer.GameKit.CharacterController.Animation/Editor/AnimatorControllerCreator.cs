using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Animation.Editor
{
    public static class AnimatorControllerCreator
    {
        private const string ControllerPath =
            "Packages/Wiesenwischer.GameKit.CharacterController.Animation/Resources/AnimatorControllers/";

        private const string MaskPath =
            "Packages/Wiesenwischer.GameKit.CharacterController.Animation/Resources/AvatarMasks/";

        [MenuItem("Wiesenwischer/GameKit/Animation/Create Animator Controller", false, 100)]
        public static void CreateController()
        {
            var controller = AnimatorController.CreateAnimatorControllerAtPath(
                ControllerPath + "CharacterAnimatorController.controller");

            // Parameter hinzufügen
            controller.AddParameter(AnimationParameters.SpeedParam, AnimatorControllerParameterType.Float);
            controller.AddParameter(AnimationParameters.IsGroundedParam, AnimatorControllerParameterType.Bool);
            controller.AddParameter(AnimationParameters.VerticalVelocityParam, AnimatorControllerParameterType.Float);
            controller.AddParameter(AnimationParameters.FallingTimeParam, AnimatorControllerParameterType.Float);
            controller.AddParameter(AnimationParameters.IsFallingLongParam, AnimatorControllerParameterType.Bool);
            controller.AddParameter(AnimationParameters.JumpTrigger, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(AnimationParameters.LandTrigger, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(AnimationParameters.HardLandingParam, AnimatorControllerParameterType.Bool);

            // Base Layer: IK Pass aktivieren (benötigt für FootIK, LookAtIK)
            var layers = controller.layers;
            if (layers.Length > 0)
            {
                layers[0].iKPass = true;
                controller.layers = layers;
            }

            // Layer 1: Abilities (Layer 0 existiert bereits als "Base Layer")
            var upperBodyMask = AssetDatabase.LoadAssetAtPath<AvatarMask>(
                MaskPath + "Mask_UpperBody.mask");

            var controllerPath = ControllerPath + "CharacterAnimatorController.controller";

            // Layer 1: Abilities
            var abilityStateMachine = new AnimatorStateMachine { name = "Abilities" };
            AssetDatabase.AddObjectToAsset(abilityStateMachine, controllerPath);

            var abilityLayer = new AnimatorControllerLayer
            {
                name = "Abilities",
                defaultWeight = 0f,
                avatarMask = upperBodyMask,
                blendingMode = AnimatorLayerBlendingMode.Override,
                stateMachine = abilityStateMachine
            };

            var emptyState = abilityStateMachine.AddState("Empty");
            abilityStateMachine.defaultState = emptyState;

            controller.AddLayer(abilityLayer);

            // Layer 2: Status (Full-Body Override für Stun, Knockback, Death)
            var statusStateMachine = new AnimatorStateMachine { name = "Status" };
            AssetDatabase.AddObjectToAsset(statusStateMachine, controllerPath);

            var statusLayer = new AnimatorControllerLayer
            {
                name = "Status",
                defaultWeight = 0f,
                avatarMask = null,
                blendingMode = AnimatorLayerBlendingMode.Override,
                stateMachine = statusStateMachine
            };

            var statusEmptyState = statusStateMachine.AddState("Empty");
            statusStateMachine.defaultState = statusEmptyState;

            controller.AddLayer(statusLayer);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("[AnimatorControllerCreator] Animator Controller erstellt.");
        }
    }
}
