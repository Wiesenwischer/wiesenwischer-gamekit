using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Animation.Editor
{
    /// <summary>
    /// One-Click Wizard zum Einrichten des kompletten Character Controller Systems.
    /// Erstellt Configs, Animator Controller und Player Prefab.
    /// Player wird separat über "Scenes/Place Player in Scene" platziert.
    /// Menü: Wiesenwischer > GameKit > Setup Character Controller
    /// </summary>
    public static class CharacterControllerSetupWizard
    {
        private const string AnimatorControllerPath =
            "Packages/Wiesenwischer.GameKit.CharacterController.Animation/Resources/AnimatorControllers/CharacterAnimatorController.controller";

        private const string LocomotionConfigPath = "Assets/Config/DefaultLocomotionConfig.asset";

        [MenuItem("Wiesenwischer/GameKit/Setup Character Controller", false, 0)]
        public static void RunSetup()
        {
            if (!EditorUtility.DisplayDialog(
                "Character Controller Setup",
                "Dieser Wizard erstellt/überschreibt:\n\n" +
                "1. DefaultLocomotionConfig\n" +
                "2. DefaultCameraConfig\n" +
                "3. Avatar Masks\n" +
                "4. Animator Controller (Locomotion + Airborne + Stopping + Slide)\n" +
                "5. Player Prefab\n\n" +
                "Bestehende Assets werden überschrieben.\n\n" +
                "Kamera wird separat über 'Camera > Setup Third Person Camera' eingerichtet.\n" +
                "Fortfahren?",
                "Setup starten", "Abbrechen"))
            {
                return;
            }

            int totalSteps = 6;
            int step = 0;

            // === 1. LocomotionConfig ===
            EditorUtility.DisplayProgressBar("Character Controller Setup",
                "1/6 — LocomotionConfig erstellen...", (float)step++ / totalSteps);

            EnsureLocomotionConfig();

            // === 2. CameraConfig ===
            EditorUtility.DisplayProgressBar("Character Controller Setup",
                "2/6 — CameraConfig erstellen...", (float)step++ / totalSteps);

            EditorApplication.ExecuteMenuItem("Wiesenwischer/GameKit/Camera/Create Default Camera Config");

            // === 3. Avatar Masks ===
            EditorUtility.DisplayProgressBar("Character Controller Setup",
                "3/6 — Avatar Masks erstellen...", (float)step++ / totalSteps);

            AvatarMaskCreator.CreateAllMasks();

            // === 4. Animator Controller (frisch erstellen) ===
            EditorUtility.DisplayProgressBar("Character Controller Setup",
                "4/6 — Animator Controller erstellen...", (float)step++ / totalSteps);

            RecreateAnimatorController();

            // === 5. Locomotion Blend Tree + Airborne + Stopping States ===
            EditorUtility.DisplayProgressBar("Character Controller Setup",
                "5/6 — Animator States einrichten...", (float)step++ / totalSteps);

            LocomotionBlendTreeCreator.SetupLocomotionBlendTree();
            AirborneStatesCreator.SetupAirborneStates();
            StoppingStatesCreator.SetupStoppingStates();

            // === 6. Player Prefab ===
            EditorUtility.DisplayProgressBar("Character Controller Setup",
                "6/6 — Player Prefab erstellen...", (float)step++ / totalSteps);

            PlayerPrefabCreator.CreatePlayerPrefab();

            EditorUtility.ClearProgressBar();

            Debug.Log("=== Character Controller Setup abgeschlossen! ===");

            EditorUtility.DisplayDialog(
                "Setup abgeschlossen!",
                "Character Controller ist bereit.\n\n" +
                "Nächste Schritte:\n" +
                "• Character & Animation Wizard: Character Model + Clips zuweisen\n" +
                "• Scenes > Create Playground: Testumgebung erstellen\n" +
                "• Scenes > Place Player in Scene: Player in Szene platzieren\n" +
                "• Camera > Setup Third Person Camera: Kamera einrichten\n" +
                "• Play Mode starten und testen",
                "OK");
        }

        [MenuItem("Wiesenwischer/GameKit/Setup Character Controller", true)]
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

            EditorApplication.ExecuteMenuItem("Wiesenwischer/GameKit/Config/Create Default LocomotionConfig");
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
