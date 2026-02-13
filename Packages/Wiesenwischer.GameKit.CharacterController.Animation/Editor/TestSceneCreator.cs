using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Animation.Editor
{
    /// <summary>
    /// Editor-Tool zum Erstellen der Test-Szene (Player Prefab + Kamera).
    /// Wird typischerweise additiv zusammen mit der Playground-Szene geladen.
    /// Menü: Wiesenwischer > GameKit > Scenes > Create Test Scene
    /// </summary>
    public static class TestSceneCreator
    {
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        private const string ScenePath = "Assets/Scenes/TestScene.unity";

        [MenuItem("Wiesenwischer/GameKit/Scenes/Create Test Scene", false, 301)]
        public static void CreateTestScene()
        {
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (playerPrefab == null)
            {
                Debug.LogError($"[TestScene] Player Prefab nicht gefunden: {PlayerPrefabPath}. " +
                               "Bitte zuerst über 'Character & Animation Wizard' erstellen.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // === Player Prefab ===
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = new Vector3(0f, 1f, 0f);

            // === Kamera positionieren ===
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.transform.position = new Vector3(0f, 3f, -8f);
                mainCamera.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
            }

            // === Szene speichern ===
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[TestScene] Test-Szene erstellt: {ScenePath}");
            Debug.Log("[TestScene] Tipp: Playground-Szene additiv öffnen für die komplette Testumgebung.");
            Debug.Log("[TestScene] Test-Checkliste:");
            Debug.Log("  1. Playground-Szene additiv laden (Rechtsklick > Open Scene Additive)");
            Debug.Log("  2. Play Mode starten");
            Debug.Log("  3. WASD = Laufen, Shift = Sprint, Space = Jump");
            Debug.Log("  4. Von Plattformen fallen für Landing-Tests");
            Debug.Log("  5. Steile Rampen (60°) für Slope Sliding");
        }

        [MenuItem("Wiesenwischer/GameKit/Scenes/Create Test Scene", true)]
        private static bool ValidateCreateTestScene()
        {
            return !Application.isPlaying;
        }
    }
}
