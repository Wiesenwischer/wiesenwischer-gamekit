using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Wiesenwischer.GameKit.CharacterController.Core.Editor
{
    /// <summary>
    /// Editor-Tool zum Erstellen der Demo-Szene und Prefabs.
    /// </summary>
    public static class DemoSceneSetup
    {
        private const string DemoScenePath = "Assets/Scenes/MovementTest.unity";
        private const string PrefabsPath = "Assets/Prefabs";

        public static void CreateDemoScene()
        {
            // Erstelle neue Szene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Erstelle Ground
            CreateGround();

            // Erstelle Test Obstacles
            CreateObstacles();

            // Erstelle Player
            CreatePlayer();

            // Speichere Szene
            EnsureDirectoryExists("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, DemoScenePath);

            Debug.Log($"[DemoSceneSetup] Demo-Szene erstellt: {DemoScenePath}");
        }

        public static void CreateTerrainDemoScene()
        {
            // Erstelle neue Szene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Erstelle Terrain mit Hügeln und Hängen
            CreateTestTerrain();

            // Erstelle Player
            CreatePlayer();

            // Positioniere Player auf dem Terrain
            var player = GameObject.Find("Player");
            if (player != null)
            {
                player.transform.position = new Vector3(50f, 10f, 50f);
            }

            // Speichere Szene
            EnsureDirectoryExists("Assets/Scenes");
            string terrainScenePath = "Assets/Scenes/TerrainMovementTest.unity";
            EditorSceneManager.SaveScene(scene, terrainScenePath);

            Debug.Log($"[DemoSceneSetup] Terrain-Demo-Szene erstellt: {terrainScenePath}");
        }

        private static void CreateTestTerrain()
        {
            // Terrain Data erstellen
            TerrainData terrainData = new TerrainData();
            terrainData.heightmapResolution = 129; // Muss 2^n + 1 sein
            terrainData.size = new Vector3(100f, 20f, 100f);

            // Heightmap generieren mit verschiedenen Slopes
            int resolution = terrainData.heightmapResolution;
            float[,] heights = new float[resolution, resolution];

            // Generiere verschiedene Test-Bereiche
            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float normalizedX = (float)x / (resolution - 1);
                    float normalizedZ = (float)z / (resolution - 1);

                    float height = 0f;

                    // Bereich 1: Flacher Bereich (links)
                    if (normalizedX < 0.25f)
                    {
                        height = 0.1f;
                    }
                    // Bereich 2: Sanfter Hang ~20° (links-mitte)
                    else if (normalizedX < 0.4f)
                    {
                        float t = (normalizedX - 0.25f) / 0.15f;
                        height = 0.1f + t * 0.15f; // ~20° Steigung
                    }
                    // Bereich 3: Plateau
                    else if (normalizedX < 0.5f)
                    {
                        height = 0.25f;
                    }
                    // Bereich 4: Steiler Hang ~45° (mitte)
                    else if (normalizedX < 0.65f)
                    {
                        float t = (normalizedX - 0.5f) / 0.15f;
                        height = 0.25f + t * 0.3f; // ~45° Steigung
                    }
                    // Bereich 5: Hohes Plateau
                    else if (normalizedX < 0.75f)
                    {
                        height = 0.55f;
                    }
                    // Bereich 6: Sehr steiler Hang ~60° (rechts)
                    else if (normalizedX < 0.85f)
                    {
                        float t = (normalizedX - 0.75f) / 0.1f;
                        height = 0.55f + t * 0.35f; // ~60° Steigung
                    }
                    // Bereich 7: Höchstes Plateau
                    else
                    {
                        height = 0.9f;
                    }

                    // Füge leichte Variation hinzu
                    height += Mathf.PerlinNoise(x * 0.1f, z * 0.1f) * 0.02f;

                    heights[z, x] = height;
                }
            }

            terrainData.SetHeights(0, 0, heights);

            // Terrain Data speichern
            EnsureDirectoryExists("Assets/Terrain");
            string terrainDataPath = "Assets/Terrain/TestTerrainData.asset";
            AssetDatabase.CreateAsset(terrainData, terrainDataPath);

            // Terrain GameObject erstellen
            GameObject terrainGO = Terrain.CreateTerrainGameObject(terrainData);
            terrainGO.name = "TestTerrain";
            terrainGO.transform.position = Vector3.zero;

            // Terrain Layer für Ground Detection setzen
            terrainGO.layer = LayerMask.NameToLayer("Default");

            Debug.Log("[DemoSceneSetup] Test-Terrain erstellt mit verschiedenen Hangwinkeln: ~20°, ~45°, ~60°");
        }

        public static void CreateCorePrefabs()
        {
            EnsureDirectoryExists(PrefabsPath);

            CreatePlayerPrefab();
            CreateGroundPrefab();
            CreateSlopePrefabs();
            CreateStairsPrefab();

            AssetDatabase.Refresh();
            Debug.Log("[DemoSceneSetup] Core Prefabs erstellt.");
        }

        #region Scene Creation

        private static void CreateGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(10f, 1f, 10f); // 100x100 units

            // Set layer
            ground.layer = LayerMask.NameToLayer("Default");

            // Material
            SetMaterialColor(ground.GetComponent<Renderer>(), new Color(0.5f, 0.5f, 0.5f));
        }

        private static void CreateObstacles()
        {
            // Cube obstacles
            CreateCube("Obstacle_Cube_1", new Vector3(5f, 0.5f, 5f), Vector3.one);
            CreateCube("Obstacle_Cube_2", new Vector3(-5f, 1f, 5f), new Vector3(2f, 2f, 2f));
            CreateCube("Obstacle_Cube_3", new Vector3(0f, 0.25f, 8f), new Vector3(4f, 0.5f, 1f));

            // Slopes
            CreateSlope("Slope_30deg", new Vector3(8f, 0f, 0f), 30f);
            CreateSlope("Slope_45deg", new Vector3(-8f, 0f, 0f), 45f);
            CreateSlope("Slope_60deg", new Vector3(8f, 0f, -8f), 60f);

            // Stairs
            CreateStairs("Stairs", new Vector3(0f, 0f, -8f), 5, 0.3f, 0.3f);
        }

        private static void CreateCube(string name, Vector3 position, Vector3 scale)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.position = position;
            cube.transform.localScale = scale;

            SetMaterialColor(cube.GetComponent<Renderer>(), new Color(0.7f, 0.4f, 0.2f));
        }

        private static void CreateSlope(string name, Vector3 position, float angle)
        {
            // Dünne Rampe - funktioniert jetzt dank RaycastAll-Filter in GroundingDetection
            var slope = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slope.name = name;

            // Dünne Rampe (0.1 Einheiten)
            slope.transform.localScale = new Vector3(3f, 0.1f, 4f);
            slope.transform.rotation = Quaternion.Euler(angle, 0f, 0f);
            slope.transform.position = position + Vector3.up * 0.5f;

            SetMaterialColor(slope.GetComponent<Renderer>(), new Color(0.3f, 0.6f, 0.3f));
        }

        private static void CreateStairs(string name, Vector3 position, int steps, float stepHeight, float stepDepth)
        {
            var parent = new GameObject(name);
            parent.transform.position = position;

            for (int i = 0; i < steps; i++)
            {
                var step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.name = $"Step_{i + 1}";
                step.transform.parent = parent.transform;
                step.transform.localPosition = new Vector3(0f, stepHeight * (i + 0.5f), stepDepth * i);
                step.transform.localScale = new Vector3(2f, stepHeight, stepDepth);

                SetMaterialColor(step.GetComponent<Renderer>(), new Color(0.4f, 0.4f, 0.6f));
            }
        }

        private static void CreatePlayer()
        {
            // Erstelle Player GameObject
            var player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 1f, 0f);

            // CapsuleCollider (kinematische Physik - kein CharacterController)
            var capsule = player.AddComponent<CapsuleCollider>();
            capsule.height = 2f;
            capsule.radius = 0.5f;
            capsule.center = Vector3.up;

            // Visual (Capsule)
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.parent = player.transform;
            visual.transform.localPosition = Vector3.up;
            visual.transform.localScale = Vector3.one;

            // Entferne Collider vom Visual
            Object.DestroyImmediate(visual.GetComponent<CapsuleCollider>());

            SetMaterialColor(visual.GetComponent<Renderer>(), new Color(0.2f, 0.4f, 0.8f));

            // PlayerController Component
            player.AddComponent<PlayerController>();

            // PlayerInput Component (Unity Input System)
            var playerInput = player.AddComponent<PlayerInput>();
            playerInput.notificationBehavior = PlayerNotifications.InvokeUnityEvents;

            // Versuche Default Input Actions zu finden
            var inputActions = FindDefaultInputActions();
            if (inputActions != null)
            {
                playerInput.actions = inputActions;
                Debug.Log("[DemoSceneSetup] Input Actions zugewiesen.");
            }
            else
            {
                Debug.LogWarning("[DemoSceneSetup] Keine Input Actions gefunden. " +
                               "Bitte weise manuell ein InputActionAsset dem PlayerInput zu.");
            }

            // Input Provider
            var inputProvider = player.AddComponent<Input.PlayerInputProvider>();

            // Referenzen verdrahten via SerializedObject
            var inputProviderSO = new SerializedObject(inputProvider);
            inputProviderSO.FindProperty("_playerInput").objectReferenceValue = playerInput;
            inputProviderSO.ApplyModifiedProperties();

            var controllerSO = new SerializedObject(player.GetComponent<PlayerController>());
            controllerSO.FindProperty("_inputProviderComponent").objectReferenceValue = inputProvider;
            controllerSO.ApplyModifiedProperties();

            // Ground Check Transform
            var groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.parent = player.transform;
            groundCheck.transform.localPosition = Vector3.zero;

            Debug.Log("[DemoSceneSetup] Player erstellt mit Input-Referenzen.");
        }

        #endregion

        #region Prefab Creation

        private static void CreatePlayerPrefab()
        {
            var player = new GameObject("BasicPlayer");

            // CapsuleCollider (kinematische Physik - kein CharacterController)
            var capsule = player.AddComponent<CapsuleCollider>();
            capsule.height = 2f;
            capsule.radius = 0.5f;
            capsule.center = Vector3.up;

            // Visual
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.parent = player.transform;
            visual.transform.localPosition = Vector3.up;
            Object.DestroyImmediate(visual.GetComponent<CapsuleCollider>());

            // Components
            player.AddComponent<PlayerController>();

            // PlayerInput Component
            var playerInput = player.AddComponent<PlayerInput>();
            playerInput.notificationBehavior = PlayerNotifications.InvokeUnityEvents;

            // Input Actions
            var inputActions = FindDefaultInputActions();
            if (inputActions != null)
            {
                playerInput.actions = inputActions;
            }

            var inputProvider = player.AddComponent<Input.PlayerInputProvider>();

            // Referenzen verdrahten
            var inputProviderSO = new SerializedObject(inputProvider);
            inputProviderSO.FindProperty("_playerInput").objectReferenceValue = playerInput;
            inputProviderSO.ApplyModifiedProperties();

            var controllerSO = new SerializedObject(player.GetComponent<PlayerController>());
            controllerSO.FindProperty("_inputProviderComponent").objectReferenceValue = inputProvider;
            controllerSO.ApplyModifiedProperties();

            // Ground Check
            var groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.parent = player.transform;
            groundCheck.transform.localPosition = Vector3.zero;

            // Save Prefab
            string path = $"{PrefabsPath}/BasicPlayer.prefab";
            PrefabUtility.SaveAsPrefabAsset(player, path);
            Object.DestroyImmediate(player);

            Debug.Log($"[DemoSceneSetup] Prefab erstellt: {path}");
        }

        private static void CreateGroundPrefab()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "TestGround";
            ground.transform.localScale = new Vector3(10f, 1f, 10f);

            string path = $"{PrefabsPath}/TestGround.prefab";
            PrefabUtility.SaveAsPrefabAsset(ground, path);
            Object.DestroyImmediate(ground);

            Debug.Log($"[DemoSceneSetup] Prefab erstellt: {path}");
        }

        private static void CreateSlopePrefabs()
        {
            // 30 Degree Slope
            CreateSlopePrefab("TestSlope_30deg", 30f);
            CreateSlopePrefab("TestSlope_45deg", 45f);
            CreateSlopePrefab("TestSlope_60deg", 60f);
        }

        private static void CreateSlopePrefab(string name, float angle)
        {
            var slope = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slope.name = name;

            // Dünne Rampe
            slope.transform.localScale = new Vector3(3f, 0.1f, 4f);
            slope.transform.rotation = Quaternion.Euler(angle, 0f, 0f);

            string path = $"{PrefabsPath}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(slope, path);
            Object.DestroyImmediate(slope);

            Debug.Log($"[DemoSceneSetup] Prefab erstellt: {path}");
        }

        private static void CreateStairsPrefab()
        {
            var stairs = new GameObject("TestStairs");

            for (int i = 0; i < 5; i++)
            {
                var step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.name = $"Step_{i + 1}";
                step.transform.parent = stairs.transform;
                step.transform.localPosition = new Vector3(0f, 0.3f * (i + 0.5f), 0.3f * i);
                step.transform.localScale = new Vector3(2f, 0.3f, 0.3f);
            }

            string path = $"{PrefabsPath}/TestStairs.prefab";
            PrefabUtility.SaveAsPrefabAsset(stairs, path);
            Object.DestroyImmediate(stairs);

            Debug.Log($"[DemoSceneSetup] Prefab erstellt: {path}");
        }

        #endregion

        #region Utility

        private static void SetMaterialColor(Renderer renderer, Color color)
        {
            if (renderer == null) return;

            var shader = Shader.Find("HDRP/Lit");
            if (shader == null)
            {
                Debug.LogWarning("[DemoSceneSetup] HDRP/Lit Shader nicht gefunden. Material wird nicht gesetzt.");
                return;
            }

            var mat = new Material(shader);
            mat.SetColor("_BaseColor", color);
            renderer.material = mat;
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string[] parts = path.Split('/');
                string currentPath = parts[0];

                for (int i = 1; i < parts.Length; i++)
                {
                    string nextPath = currentPath + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(nextPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, parts[i]);
                    }
                    currentPath = nextPath;
                }
            }
        }

        private static InputActionAsset FindDefaultInputActions()
        {
            // Suche nach Input Actions in bekannten Pfaden
            string[] searchPaths = new[]
            {
                "Assets/InputSystem_Actions.inputactions",
                "Assets/Settings/InputSystem_Actions.inputactions",
                "Assets/Input/InputSystem_Actions.inputactions"
            };

            foreach (var path in searchPaths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
                if (asset != null)
                {
                    return asset;
                }
            }

            // Fallback: Suche alle InputActionAssets im Projekt
            var guids = AssetDatabase.FindAssets("t:InputActionAsset");
            if (guids.Length > 0)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);
            }

            return null;
        }

        #endregion

        #region Default Config

        [MenuItem("Wiesenwischer/GameKit/Config/Create Default LocomotionConfig", false, 400)]
        public static void CreateDefaultLocomotionConfig()
        {
            EnsureDirectoryExists("Assets/Config");

            var config = ScriptableObject.CreateInstance<Locomotion.LocomotionConfig>();

            AssetDatabase.CreateAsset(config, "Assets/Config/DefaultLocomotionConfig.asset");
            AssetDatabase.SaveAssets();

            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);

            Debug.Log("[DemoSceneSetup] DefaultLocomotionConfig erstellt: Assets/Config/DefaultLocomotionConfig.asset");
        }

        #endregion
    }
}
