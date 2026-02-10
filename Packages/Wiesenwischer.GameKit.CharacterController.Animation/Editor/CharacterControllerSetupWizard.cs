using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Animation.Editor
{
    /// <summary>
    /// One-Click Wizard zum Einrichten des kompletten Character Controller Systems.
    /// Führt alle Setup-Schritte in der richtigen Reihenfolge aus.
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
                "4. Animator Controller (Locomotion + Airborne)\n" +
                "5. Player Prefab\n" +
                "6. Animation Test Scene + Kamera\n\n" +
                "Bestehende Assets werden überschrieben.\n" +
                "Fortfahren?",
                "Setup starten", "Abbrechen"))
            {
                return;
            }

            int totalSteps = 8;
            int step = 0;

            // === 1. LocomotionConfig ===
            EditorUtility.DisplayProgressBar("Character Controller Setup",
                "1/8 — LocomotionConfig erstellen...", (float)step++ / totalSteps);

            EnsureLocomotionConfig();

            // === 2. CameraConfig ===
            EditorUtility.DisplayProgressBar("Character Controller Setup",
                "2/8 — CameraConfig erstellen...", (float)step++ / totalSteps);

            EditorApplication.ExecuteMenuItem("Wiesenwischer/GameKit/Camera/Create Default Camera Config");

            // === 3. Avatar Masks ===
            EditorUtility.DisplayProgressBar("Character Controller Setup",
                "3/8 — Avatar Masks erstellen...", (float)step++ / totalSteps);

            AvatarMaskCreator.CreateAllMasks();

            // === 4. Animator Controller (frisch erstellen) ===
            EditorUtility.DisplayProgressBar("Character Controller Setup",
                "4/8 — Animator Controller erstellen...", (float)step++ / totalSteps);

            RecreateAnimatorController();

            // === 5. Locomotion Blend Tree ===
            EditorUtility.DisplayProgressBar("Character Controller Setup",
                "5/8 — Locomotion Blend Tree einrichten...", (float)step++ / totalSteps);

            LocomotionBlendTreeCreator.SetupLocomotionBlendTree();

            // === 6. Airborne States ===
            EditorUtility.DisplayProgressBar("Character Controller Setup",
                "6/8 — Airborne States einrichten...", (float)step++ / totalSteps);

            AirborneStatesCreator.SetupAirborneStates();

            // === 7. Player Prefab ===
            EditorUtility.DisplayProgressBar("Character Controller Setup",
                "7/8 — Player Prefab erstellen...", (float)step++ / totalSteps);

            PlayerPrefabCreator.CreatePlayerPrefab();

            // === 8. Test Scene + Kamera ===
            EditorUtility.DisplayProgressBar("Character Controller Setup",
                "8/8 — Test-Szene + Kamera einrichten...", (float)step++ / totalSteps);

            AnimationTestSceneCreator.CreateTestScene();
            EditorApplication.ExecuteMenuItem("Wiesenwischer/GameKit/Camera/Setup Third Person Camera");

            EditorUtility.ClearProgressBar();

            Debug.Log("=== Character Controller Setup abgeschlossen! ===");
            Debug.Log("Play Mode starten und mit WASD + Space + Shift testen.");

            EditorUtility.DisplayDialog(
                "Setup abgeschlossen!",
                "Character Controller ist bereit.\n\n" +
                "Play Mode starten und testen:\n" +
                "• WASD = Laufen\n" +
                "• Shift = Sprint\n" +
                "• Space = Jump\n" +
                "• Von Plattformen fallen für Landing-Tests",
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
            // Bestehenden Controller löschen für sauberen Neuaufbau
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
