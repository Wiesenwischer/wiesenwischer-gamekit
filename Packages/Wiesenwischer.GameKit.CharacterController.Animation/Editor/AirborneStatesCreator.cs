using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Animation.Editor
{
    public static class AirborneStatesCreator
    {
        private const string ControllerPath =
            "Packages/Wiesenwischer.GameKit.CharacterController.Animation/Resources/AnimatorControllers/CharacterAnimatorController.controller";

        private const string ClipBasePath = "Assets/Animations/Locomotion/";

        [MenuItem("Wiesenwischer/GameKit/Animation/Setup Airborne States", false, 102)]
        public static void SetupAirborneStates()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                Debug.LogError("[AirborneStatesCreator] Controller nicht gefunden. Bitte zuerst 'Create Animator Controller' ausführen.");
                return;
            }

            var jumpClip = LoadClipFromFbx("Anim_Jump");
            var fallClip = LoadClipFromFbx("Anim_Fall");
            var landClip = LoadClipFromFbx("Anim_Land");

            if (jumpClip == null || fallClip == null || landClip == null)
            {
                Debug.LogError("[AirborneStatesCreator] Nicht alle Clips gefunden. Erwartete FBX-Dateien: Anim_Jump, Anim_Fall, Anim_Land in: " + ClipBasePath);
                return;
            }

            var rootStateMachine = controller.layers[AnimationParameters.BaseLayerIndex].stateMachine;
            var locomotionState = FindState(rootStateMachine, "Locomotion");

            if (locomotionState == null)
            {
                Debug.LogError("[AirborneStatesCreator] Locomotion State nicht gefunden. Bitte zuerst 'Setup Locomotion Blend Tree' ausführen.");
                return;
            }

            // States erstellen
            var jumpState = rootStateMachine.AddState("Jump");
            jumpState.motion = jumpClip;
            jumpState.writeDefaultValues = false;

            var fallState = rootStateMachine.AddState("Fall");
            fallState.motion = fallClip;
            fallState.writeDefaultValues = false;

            var softLandState = rootStateMachine.AddState("SoftLand");
            softLandState.motion = landClip;
            softLandState.writeDefaultValues = false;

            // HardLand verwendet denselben Land-Clip mit reduzierter Speed
            var hardLandState = rootStateMachine.AddState("HardLand");
            hardLandState.motion = landClip;
            hardLandState.speed = 0.6f;
            hardLandState.writeDefaultValues = false;

            // Write Defaults auch für Locomotion deaktivieren
            locomotionState.writeDefaultValues = false;

            // --- Transitionen ---

            // 1. Locomotion → Jump
            var t1 = locomotionState.AddTransition(jumpState);
            t1.hasExitTime = false;
            t1.duration = 0.1f;
            t1.AddCondition(AnimatorConditionMode.If, 0, AnimationParameters.JumpTrigger);

            // 2. Jump → Fall
            var t2 = jumpState.AddTransition(fallState);
            t2.hasExitTime = false;
            t2.duration = 0.15f;
            t2.AddCondition(AnimatorConditionMode.Less, 0f, AnimationParameters.VerticalVelocityParam);

            // 3. Locomotion → Fall (Kantensturz)
            var t3 = locomotionState.AddTransition(fallState);
            t3.hasExitTime = false;
            t3.duration = 0.2f;
            t3.AddCondition(AnimatorConditionMode.IfNot, 0, AnimationParameters.IsGroundedParam);
            t3.AddCondition(AnimatorConditionMode.Less, -1f, AnimationParameters.VerticalVelocityParam);

            // 4. Fall → SoftLand
            var t4 = fallState.AddTransition(softLandState);
            t4.hasExitTime = false;
            t4.duration = 0.05f;
            t4.AddCondition(AnimatorConditionMode.If, 0, AnimationParameters.LandTrigger);
            t4.AddCondition(AnimatorConditionMode.IfNot, 0, AnimationParameters.HardLandingParam);

            // 5. Fall → HardLand
            var t5 = fallState.AddTransition(hardLandState);
            t5.hasExitTime = false;
            t5.duration = 0.05f;
            t5.AddCondition(AnimatorConditionMode.If, 0, AnimationParameters.LandTrigger);
            t5.AddCondition(AnimatorConditionMode.If, 0, AnimationParameters.HardLandingParam);

            // 6. SoftLand → Locomotion
            var t6 = softLandState.AddTransition(locomotionState);
            t6.hasExitTime = true;
            t6.exitTime = 0.8f;
            t6.duration = 0.2f;

            // 7. HardLand → Locomotion
            var t7 = hardLandState.AddTransition(locomotionState);
            t7.hasExitTime = true;
            t7.exitTime = 0.9f;
            t7.duration = 0.25f;

            // 8. Jump → Locomotion (Landung ohne Fall-Phase)
            var t8 = jumpState.AddTransition(locomotionState);
            t8.hasExitTime = false;
            t8.duration = 0.1f;
            t8.AddCondition(AnimatorConditionMode.If, 0, AnimationParameters.IsGroundedParam);

            // 9. Fall → Locomotion (Safety-Net: IsGrounded ohne Land-Trigger, z.B. Treppen)
            var t9 = fallState.AddTransition(locomotionState);
            t9.hasExitTime = false;
            t9.duration = 0.15f;
            t9.AddCondition(AnimatorConditionMode.If, 0, AnimationParameters.IsGroundedParam);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("[AirborneStatesCreator] Airborne States erstellt (Jump, Fall, SoftLand, HardLand).");
        }

        private static AnimatorState FindState(AnimatorStateMachine stateMachine, string name)
        {
            return stateMachine.states
                .Select(s => s.state)
                .FirstOrDefault(s => s.name == name);
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
