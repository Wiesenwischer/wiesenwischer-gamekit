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
        private const string MaterialFolder = "Assets/Materials/TestScene";

        [MenuItem("Wiesenwischer/GameKit/Scenes/Create Animation Test Scene", false, 300)]
        public static void CreateTestScene()
        {
            // Materials erstellen/laden
            EnsureMaterialFolder();
            var groundMat = GetOrCreateMaterial("Ground", new Color(0.15f, 0.18f, 0.15f));
            var stepMat = GetOrCreateMaterial("Steps", new Color(0.6f, 0.5f, 0.35f));
            var platformMat = GetOrCreateMaterial("Platform", new Color(0.35f, 0.45f, 0.6f));
            var slopeMat = GetOrCreateMaterial("Slope", new Color(0.6f, 0.4f, 0.25f));
            var rampMat = GetOrCreateMaterial("Ramp", new Color(0.5f, 0.35f, 0.5f));

            // Neue Szene erstellen
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // === Boden ===
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(100f, 0.2f, 100f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMat;

            // === Niedrige Plattform (Soft Landing Test, ~3m) ===
            var lowPlatform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lowPlatform.name = "Platform_Low_3m";
            lowPlatform.transform.position = new Vector3(10f, 3f, 0f);
            lowPlatform.transform.localScale = new Vector3(4f, 0.3f, 4f);
            lowPlatform.GetComponent<Renderer>().sharedMaterial = platformMat;

            // Rampe zur niedrigen Plattform
            var rampLow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rampLow.name = "Ramp_Low";
            rampLow.transform.position = new Vector3(6f, 1.5f, 0f);
            rampLow.transform.localScale = new Vector3(6f, 0.3f, 3f);
            rampLow.transform.rotation = Quaternion.Euler(0f, 0f, 30f);
            rampLow.GetComponent<Renderer>().sharedMaterial = rampMat;

            // === Hohe Plattform (Hard Landing Test, ~10m) ===
            var highPlatform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            highPlatform.name = "Platform_High_10m";
            highPlatform.transform.position = new Vector3(-10f, 10f, 0f);
            highPlatform.transform.localScale = new Vector3(4f, 0.3f, 4f);
            highPlatform.GetComponent<Renderer>().sharedMaterial = platformMat;

            // Treppe zur hohen Plattform
            CreateStaircase(new Vector3(-10f, 0f, 5f), 10f, 20, stepMat);

            // === Sehr hohe Plattform (Extreme Hard Landing, ~20m) ===
            var veryHighPlatform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            veryHighPlatform.name = "Platform_VeryHigh_20m";
            veryHighPlatform.transform.position = new Vector3(-10f, 20f, -5f);
            veryHighPlatform.transform.localScale = new Vector3(4f, 0.3f, 4f);
            veryHighPlatform.GetComponent<Renderer>().sharedMaterial = platformMat;

            // === Slope (30°) für Slide-Tests ===
            var slope = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slope.name = "Slope_30deg";
            slope.transform.position = new Vector3(0f, 2f, 10f);
            slope.transform.localScale = new Vector3(5f, 0.3f, 10f);
            slope.transform.rotation = Quaternion.Euler(30f, 0f, 0f);
            slope.GetComponent<Renderer>().sharedMaterial = slopeMat;

            // === Steile Slope (60°) für Rutsch-Tests ===
            var steepSlope = GameObject.CreatePrimitive(PrimitiveType.Cube);
            steepSlope.name = "Slope_60deg";
            steepSlope.transform.position = new Vector3(0f, 4f, -10f);
            steepSlope.transform.localScale = new Vector3(5f, 0.3f, 10f);
            steepSlope.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
            steepSlope.GetComponent<Renderer>().sharedMaterial = slopeMat;

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

        private static void CreateStaircase(Vector3 startPos, float totalHeight, int steps, Material stepMat)
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
                step.GetComponent<Renderer>().sharedMaterial = stepMat;
            }
        }

        private static void EnsureMaterialFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                AssetDatabase.CreateFolder("Assets", "Materials");
            if (!AssetDatabase.IsValidFolder(MaterialFolder))
                AssetDatabase.CreateFolder("Assets/Materials", "TestScene");
        }

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            var path = $"{MaterialFolder}/TestScene_{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.SetColor("_BaseColor", color);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var shader = Shader.Find("HDRP/Lit");
            if (shader == null)
            {
                Debug.LogWarning($"[AnimationTestScene] HDRP/Lit Shader nicht gefunden — verwende Standard-Shader.");
                shader = Shader.Find("Standard");
            }

            var mat = new Material(shader);
            mat.SetColor("_BaseColor", color);
            mat.SetColor("_Color", color);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        [MenuItem("Wiesenwischer/GameKit/Scenes/Create Animation Test Scene", true)]
        private static bool ValidateCreateTestScene()
        {
            return !Application.isPlaying;
        }
    }
}
