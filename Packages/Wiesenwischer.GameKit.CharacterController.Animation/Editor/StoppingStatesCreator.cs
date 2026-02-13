using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Animation.Editor
{
    public static class StoppingStatesCreator
    {
        private const string ControllerPath =
            "Packages/Wiesenwischer.GameKit.CharacterController.Animation/Resources/AnimatorControllers/CharacterAnimatorController.controller";

        private const string ClipBasePath = "Assets/Animations/Locomotion/";

        public static void SetupStoppingStates()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                Debug.LogError("[StoppingStatesCreator] Controller nicht gefunden. Bitte zuerst 'Create Animator Controller' ausführen.");
                return;
            }

            var lightStopClip = LoadClipFromFbx("Anim_LightStop");
            var mediumStopClip = LoadClipFromFbx("Anim_MediumStop");
            var hardStopClip = LoadClipFromFbx("Anim_HardStop");

            if (lightStopClip == null || mediumStopClip == null || hardStopClip == null)
            {
                Debug.LogError("[StoppingStatesCreator] Nicht alle Clips gefunden. Erwartete FBX-Dateien: " +
                               "Anim_LightStop, Anim_MediumStop, Anim_HardStop in: " + ClipBasePath);
                return;
            }

            var rootStateMachine = controller.layers[AnimationParameters.BaseLayerIndex].stateMachine;

            // --- Cleanup: Existierende Stopping States entfernen (idempotent) ---
            RemoveStateIfExists(rootStateMachine, "LightStop");
            RemoveStateIfExists(rootStateMachine, "MediumStop");
            RemoveStateIfExists(rootStateMachine, "HardStop");

            // --- States erstellen ---
            // Keine Transitions nötig — die State Machine steuert den Animator
            // direkt via Animator.CrossFade() in jedem State's OnEnter.
            var lightStopState = rootStateMachine.AddState("LightStop");
            lightStopState.motion = lightStopClip;
            lightStopState.writeDefaultValues = false;
            lightStopState.iKOnFeet = true;

            var mediumStopState = rootStateMachine.AddState("MediumStop");
            mediumStopState.motion = mediumStopClip;
            mediumStopState.writeDefaultValues = false;
            mediumStopState.iKOnFeet = true;

            var hardStopState = rootStateMachine.AddState("HardStop");
            hardStopState.motion = hardStopClip;
            hardStopState.writeDefaultValues = false;
            hardStopState.iKOnFeet = true;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("[StoppingStatesCreator] Stopping States erstellt (LightStop, MediumStop, HardStop). " +
                      "State Machine steuert Animator direkt via PlayState() in OnEnter.");
        }

        private static AnimatorState FindState(AnimatorStateMachine stateMachine, string name)
        {
            return stateMachine.states
                .Select(s => s.state)
                .FirstOrDefault(s => s.name == name);
        }

        private static void RemoveStateIfExists(AnimatorStateMachine stateMachine, string name)
        {
            var state = FindState(stateMachine, name);
            if (state != null)
            {
                stateMachine.RemoveState(state);
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
    }
}
