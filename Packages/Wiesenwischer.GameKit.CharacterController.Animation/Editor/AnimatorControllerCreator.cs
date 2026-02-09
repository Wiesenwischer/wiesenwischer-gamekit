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

        [MenuItem("Wiesenwischer/GameKit/Create Animator Controller")]
        public static void CreateController()
        {
            var controller = AnimatorController.CreateAnimatorControllerAtPath(
                ControllerPath + "CharacterAnimatorController.controller");

            // Parameter hinzufügen
            controller.AddParameter(AnimationParameters.SpeedParam, AnimatorControllerParameterType.Float);
            controller.AddParameter(AnimationParameters.IsGroundedParam, AnimatorControllerParameterType.Bool);
            controller.AddParameter(AnimationParameters.VerticalVelocityParam, AnimatorControllerParameterType.Float);
            controller.AddParameter(AnimationParameters.JumpTrigger, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(AnimationParameters.LandTrigger, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(AnimationParameters.HardLandingParam, AnimatorControllerParameterType.Bool);

            // Layer 1: Abilities (Layer 0 existiert bereits als "Base Layer")
            var upperBodyMask = AssetDatabase.LoadAssetAtPath<AvatarMask>(
                MaskPath + "Mask_UpperBody.mask");

            var abilityLayer = new AnimatorControllerLayer
            {
                name = "Abilities",
                defaultWeight = 0f,
                avatarMask = upperBodyMask,
                blendingMode = AnimatorLayerBlendingMode.Override,
                stateMachine = new AnimatorStateMachine()
            };

            var emptyState = abilityLayer.stateMachine.AddState("Empty");
            abilityLayer.stateMachine.defaultState = emptyState;

            controller.AddLayer(abilityLayer);

            // Layer 2: Status (Full-Body Override für Stun, Knockback, Death)
            var statusLayer = new AnimatorControllerLayer
            {
                name = "Status",
                defaultWeight = 0f,
                avatarMask = null,
                blendingMode = AnimatorLayerBlendingMode.Override,
                stateMachine = new AnimatorStateMachine()
            };

            var statusEmptyState = statusLayer.stateMachine.AddState("Empty");
            statusLayer.stateMachine.defaultState = statusEmptyState;

            controller.AddLayer(statusLayer);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("[AnimatorControllerCreator] Animator Controller erstellt.");
        }
    }
}
