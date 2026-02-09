using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Wiesenwischer.GameKit.CharacterController.Animation.Editor
{
    /// <summary>
    /// Editor-Tool zum Erstellen der Animation-Test-Szene.
    /// Menü: Wiesenwischer > GameKit > Create Animation Test Scene
    /// </summary>
    public static class AnimationTestSceneCreator
    {
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        private const string ScenePath = "Assets/Scenes/AnimationTestScene.unity";

        [MenuItem("Wiesenwischer/GameKit/Scenes/Create Animation Test Scene", false, 300)]
        public static void CreateTestScene()
        {
            // Neue Szene erstellen
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // === Boden ===
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(100f, 0.2f, 100f);
            ground.GetComponent<Renderer>().sharedMaterial = GetDefaultMaterial();

            // === Niedrige Plattform (Soft Landing Test, ~3m) ===
            var lowPlatform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lowPlatform.name = "Platform_Low_3m";
            lowPlatform.transform.position = new Vector3(10f, 3f, 0f);
            lowPlatform.transform.localScale = new Vector3(4f, 0.3f, 4f);

            // Rampe zur niedrigen Plattform
            var rampLow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rampLow.name = "Ramp_Low";
            rampLow.transform.position = new Vector3(6f, 1.5f, 0f);
            rampLow.transform.localScale = new Vector3(6f, 0.3f, 3f);
            rampLow.transform.rotation = Quaternion.Euler(0f, 0f, 30f);

            // === Hohe Plattform (Hard Landing Test, ~10m) ===
            var highPlatform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            highPlatform.name = "Platform_High_10m";
            highPlatform.transform.position = new Vector3(-10f, 10f, 0f);
            highPlatform.transform.localScale = new Vector3(4f, 0.3f, 4f);

            // Treppe zur hohen Plattform
            CreateStaircase(new Vector3(-10f, 0f, 5f), 10f, 20);

            // === Sehr hohe Plattform (Extreme Hard Landing, ~20m) ===
            var veryHighPlatform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            veryHighPlatform.name = "Platform_VeryHigh_20m";
            veryHighPlatform.transform.position = new Vector3(-10f, 20f, -5f);
            veryHighPlatform.transform.localScale = new Vector3(4f, 0.3f, 4f);

            // === Slope (30°) für Slide-Tests ===
            var slope = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slope.name = "Slope_30deg";
            slope.transform.position = new Vector3(0f, 2f, 10f);
            slope.transform.localScale = new Vector3(5f, 0.3f, 10f);
            slope.transform.rotation = Quaternion.Euler(30f, 0f, 0f);

            // === Steile Slope (60°) für Rutsch-Tests ===
            var steepSlope = GameObject.CreatePrimitive(PrimitiveType.Cube);
            steepSlope.name = "Slope_60deg";
            steepSlope.transform.position = new Vector3(0f, 4f, -10f);
            steepSlope.transform.localScale = new Vector3(5f, 0.3f, 10f);
            steepSlope.transform.rotation = Quaternion.Euler(60f, 0f, 0f);

            // === Player Prefab platzieren ===
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (playerPrefab != null)
            {
                var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
                player.transform.position = new Vector3(0f, 1f, 0f);
            }
            else
            {
                Debug.LogWarning($"[AnimationTestScene] Player Prefab nicht gefunden: {PlayerPrefabPath}. " +
                                 "Bitte zuerst 'Create Player Prefab' ausführen.");
            }

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
            Debug.Log($"[AnimationTestScene] Test-Szene erstellt: {ScenePath}");
            Debug.Log("[AnimationTestScene] Test-Checkliste:");
            Debug.Log("  1. Play Mode starten");
            Debug.Log("  2. Window > Animation > Animator öffnen");
            Debug.Log("  3. WASD = Laufen, Shift = Sprint, Space = Jump");
            Debug.Log("  4. Von Plattformen fallen für Landing-Tests");
        }

        private static void CreateStaircase(Vector3 startPos, float totalHeight, int steps)
        {
            float stepHeight = totalHeight / steps;
            float stepDepth = 0.5f;
            float stepWidth = 3f;

            var parent = new GameObject("Staircase");
            parent.transform.position = startPos;

            for (int i = 0; i < steps; i++)
            {
                var step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.name = $"Step_{i + 1}";
                step.transform.SetParent(parent.transform);
                step.transform.localPosition = new Vector3(0f, stepHeight * (i + 0.5f), -stepDepth * i);
                step.transform.localScale = new Vector3(stepWidth, stepHeight, stepDepth);
            }
        }

        private static Material GetDefaultMaterial()
        {
            // Unity's Default-Lit-Material verwenden
            var primitive = GameObject.CreatePrimitive(PrimitiveType.Plane);
            var mat = primitive.GetComponent<Renderer>().sharedMaterial;
            Object.DestroyImmediate(primitive);
            return mat;
        }

        [MenuItem("Wiesenwischer/GameKit/Scenes/Create Animation Test Scene", true)]
        private static bool ValidateCreateTestScene()
        {
            return !Application.isPlaying;
        }
    }
}
