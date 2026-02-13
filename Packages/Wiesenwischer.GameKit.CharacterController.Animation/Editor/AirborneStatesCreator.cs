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
            var hardLandClip = LoadClipFromFbx("Anim_HardLand");

            if (jumpClip == null || fallClip == null || landClip == null)
            {
                Debug.LogError("[AirborneStatesCreator] Nicht alle Clips gefunden. Erwartete FBX-Dateien: Anim_Jump, Anim_Fall, Anim_Land in: " + ClipBasePath);
                return;
            }

            if (hardLandClip == null)
            {
                Debug.LogWarning("[AirborneStatesCreator] Anim_HardLand.fbx nicht gefunden — verwende Anim_Land als Fallback für HardLand.");
                hardLandClip = landClip;
            }

            var rootStateMachine = controller.layers[AnimationParameters.BaseLayerIndex].stateMachine;
            var locomotionState = FindState(rootStateMachine, "Locomotion");

            if (locomotionState == null)
            {
                Debug.LogError("[AirborneStatesCreator] Locomotion State nicht gefunden. Bitte zuerst 'Setup Locomotion Blend Tree' ausführen.");
                return;
            }

            var slideClip = LoadClipFromFbx("Anim_Slide");

            // --- Cleanup: Existierende Airborne States entfernen (idempotent) ---
            RemoveStateIfExists(rootStateMachine, "Jump");
            RemoveStateIfExists(rootStateMachine, "Fall");
            RemoveStateIfExists(rootStateMachine, "SoftLand");
            RemoveStateIfExists(rootStateMachine, "HardLand");
            RemoveStateIfExists(rootStateMachine, "Slide");
            RemoveTransitionsToMissing(locomotionState);

            // --- States erstellen ---
            // Keine Transitions nötig — die State Machine steuert den Animator
            // direkt via Animator.CrossFade() in jedem State's OnEnter.
            var jumpState = rootStateMachine.AddState("Jump");
            jumpState.motion = jumpClip;
            jumpState.writeDefaultValues = false;

            var fallState = rootStateMachine.AddState("Fall");
            fallState.motion = fallClip;
            fallState.writeDefaultValues = false;

            var softLandState = rootStateMachine.AddState("SoftLand");
            softLandState.motion = landClip;
            softLandState.writeDefaultValues = false;
            softLandState.iKOnFeet = true;

            var hardLandState = rootStateMachine.AddState("HardLand");
            hardLandState.motion = hardLandClip;
            hardLandState.writeDefaultValues = false;
            hardLandState.iKOnFeet = true;

            locomotionState.writeDefaultValues = false;

            // Slide State (Motion optional — wird über Anim_Slide.fbx zugewiesen)
            var slideState = rootStateMachine.AddState("Slide");
            slideState.motion = slideClip; // null wenn Clip noch nicht vorhanden
            slideState.writeDefaultValues = false;

            if (slideClip == null)
            {
                Debug.LogWarning("[AirborneStatesCreator] Anim_Slide.fbx nicht gefunden. " +
                                 "Slide State wurde ohne Motion erstellt — bitte Animation nachträglich zuweisen.");
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("[AirborneStatesCreator] Airborne States erstellt (CrossFade-Architektur). " +
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

        private static void RemoveTransitionsToMissing(AnimatorState state)
        {
            var validTransitions = state.transitions
                .Where(t => t.destinationState != null || t.destinationStateMachine != null || t.isExit)
                .ToArray();
            state.transitions = validTransitions;
        }

        private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
        {
            if (controller.parameters.Any(p => p.name == name))
                return;

            controller.AddParameter(name, type);
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
