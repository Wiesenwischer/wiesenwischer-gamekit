using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Animation.Editor
{
    public static class LocomotionBlendTreeCreator
    {
        private const string ControllerPath =
            "Packages/Wiesenwischer.GameKit.CharacterController.Animation/Resources/AnimatorControllers/CharacterAnimatorController.controller";

        private const string ClipBasePath = "Assets/Animations/Locomotion/";

        [MenuItem("Wiesenwischer/GameKit/Animation/Setup Locomotion Blend Tree", false, 101)]
        public static void SetupLocomotionBlendTree()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                Debug.LogError("[LocomotionBlendTreeCreator] Controller nicht gefunden. Bitte zuerst 'Create Animator Controller' ausführen.");
                return;
            }

            var idleClip = LoadClipFromFbx("Anim_Idle");
            var walkClip = LoadClipFromFbx("Anim_Walk");
            var runClip = LoadClipFromFbx("Anim_Run");
            var sprintClip = LoadClipFromFbx("Anim_Sprint");

            if (idleClip == null || walkClip == null || runClip == null || sprintClip == null)
            {
                Debug.LogError("[LocomotionBlendTreeCreator] Nicht alle Clips gefunden. Erwartete FBX-Dateien in: " + ClipBasePath);
                return;
            }

            var blendTree = new BlendTree
            {
                name = "Locomotion",
                blendType = BlendTreeType.Simple1D,
                blendParameter = AnimationParameters.SpeedParam,
                useAutomaticThresholds = false
            };

            blendTree.AddChild(idleClip, 0.0f);
            blendTree.AddChild(walkClip, 0.5f);
            blendTree.AddChild(runClip, 1.0f);
            blendTree.AddChild(sprintClip, 1.5f);

            AssetDatabase.AddObjectToAsset(blendTree, controller);

            var rootStateMachine = controller.layers[AnimationParameters.BaseLayerIndex].stateMachine;
            var locomotionState = rootStateMachine.AddState("Locomotion");
            locomotionState.motion = blendTree;
            locomotionState.iKOnFeet = true;
            rootStateMachine.defaultState = locomotionState;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("[LocomotionBlendTreeCreator] Locomotion Blend Tree erstellt (Idle/Walk/Run/Sprint).");
        }

        private static AnimationClip LoadClipFromFbx(string fbxName)
        {
            var fbxPath = ClipBasePath + fbxName + ".fbx";
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            return assets?
                .OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        }
    }
}
