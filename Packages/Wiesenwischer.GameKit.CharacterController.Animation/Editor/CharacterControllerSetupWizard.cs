using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Animation.Editor
{
    /// <summary>
    /// One-Click Wizard zum Einrichten des kompletten Character Controller Systems.
    /// Erstellt Configs, Animator Controller und Player Prefab.
    /// Player wird separat über "Animation > Place Player in Scene" platziert.
    /// Menü: Wiesenwischer > GameKit > Animation > Setup Character Controller
    /// </summary>
    public static class CharacterControllerSetupWizard
    {
        private const string AnimatorControllerPath =
            "Packages/Wiesenwischer.GameKit.CharacterController.Animation/Resources/AnimatorControllers/CharacterAnimatorController.controller";

        private const string LocomotionConfigPath = "Assets/Config/DefaultLocomotionConfig.asset";

        [MenuItem("Wiesenwischer/GameKit/Animation/Setup Character Controller", false, 0)]
        public static void RunSetup()
        {
            if (!EditorUtility.DisplayDialog(
                "Character Controller Setup",
                "Dieser Wizard erstellt/überschreibt:\n\n" +
                "1. DefaultLocomotionConfig\n" +
                "2. Avatar Masks\n" +
                "3. Animator Controller (Locomotion + Airborne + Stopping + Slide)\n" +
                "4. Player Prefab\n\n" +
                "Bestehende Assets werden überschrieben.\n" +
                "Fortfahren?",
                "Setup starten", "Abbrechen"))
            {
                return;
            }

            int totalSteps = 4;
            int step = 0;

            // === 1. LocomotionConfig ===
            EditorUtility.DisplayProgressBar("Character Controller Setup",
                "1/4 — LocomotionConfig erstellen...", (float)step++ / totalSteps);

            EnsureLocomotionConfig();

            // === 2. Avatar Masks ===
            EditorUtility.DisplayProgressBar("Character Controller Setup",
                "2/4 — Avatar Masks erstellen...", (float)step++ / totalSteps);

            AvatarMaskCreator.CreateAllMasks();

            // === 3. Animator Controller ===
            EditorUtility.DisplayProgressBar("Character Controller Setup",
                "3/4 — Animator Controller erstellen...", (float)step++ / totalSteps);

            RecreateAnimatorController();
            LocomotionBlendTreeCreator.SetupLocomotionBlendTree();
            AirborneStatesCreator.SetupAirborneStates();
            StoppingStatesCreator.SetupStoppingStates();

            // === 4. Player Prefab ===
            EditorUtility.DisplayProgressBar("Character Controller Setup",
                "4/4 — Player Prefab erstellen...", (float)step++ / totalSteps);

            PlayerPrefabCreator.CreatePlayerPrefab();

            EditorUtility.ClearProgressBar();

            Debug.Log("=== Character Controller Setup abgeschlossen! ===");

            EditorUtility.DisplayDialog(
                "Setup abgeschlossen!",
                "Character Controller ist bereit.\n\n" +
                "Nächste Schritte:\n" +
                "• Animation > Character & Animation Wizard: Model + Clips zuweisen\n" +
                "• Animation > Create Playground: Testumgebung erstellen\n" +
                "• Animation > Place Player in Scene: Player platzieren\n" +
                "• Camera > Setup Third Person Camera: Kamera einrichten\n" +
                "• IK > Setup IK on Player Prefab: Foot/LookAt IK\n" +
                "• Play Mode starten und testen",
                "OK");
        }

        [MenuItem("Wiesenwischer/GameKit/Animation/Setup Character Controller", true)]
        private static bool ValidateRunSetup()
        {
            return !Application.isPlaying;
        }

        private static void EnsureLocomotionConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Object>(LocomotionConfigPath);
            if (existing != null)
            {
                Debug.Log("[Setup] LocomotionConfig existiert bereits.");
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Config"))
                AssetDatabase.CreateFolder("Assets", "Config");

            var config = ScriptableObject.CreateInstance<Core.Locomotion.LocomotionConfig>();
            AssetDatabase.CreateAsset(config, LocomotionConfigPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Setup] LocomotionConfig erstellt: {LocomotionConfigPath}");
        }

        private static void RecreateAnimatorController()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(AnimatorControllerPath);
                Debug.Log("[Setup] Bestehender Animator Controller gelöscht.");
            }

            AnimatorControllerCreator.CreateController();
        }
    }
}
