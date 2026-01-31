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

        [MenuItem("Wiesenwischer/GameKit/Create Demo Scene", false, 100)]
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

        [MenuItem("Wiesenwischer/GameKit/Create Core Prefabs", false, 101)]
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
            var renderer = ground.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    color = new Color(0.5f, 0.5f, 0.5f)
                };
            }
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

            var renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    color = new Color(0.7f, 0.4f, 0.2f)
                };
            }
        }

        private static void CreateSlope(string name, Vector3 position, float angle)
        {
            var slope = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slope.name = name;
            slope.transform.position = position + Vector3.up * 0.5f;
            slope.transform.localScale = new Vector3(3f, 0.1f, 4f);
            slope.transform.rotation = Quaternion.Euler(angle, 0f, 0f);

            var renderer = slope.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    color = new Color(0.3f, 0.6f, 0.3f)
                };
            }
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

                var renderer = step.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                    {
                        color = new Color(0.4f, 0.4f, 0.6f)
                    };
                }
            }
        }

        private static void CreatePlayer()
        {
            // Erstelle Player GameObject
            var player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 1f, 0f);

            // CharacterController
            var cc = player.AddComponent<UnityEngine.CharacterController>();
            cc.height = 2f;
            cc.radius = 0.5f;
            cc.center = Vector3.up;

            // Visual (Capsule)
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.parent = player.transform;
            visual.transform.localPosition = Vector3.up;
            visual.transform.localScale = Vector3.one;

            // Entferne Collider vom Visual
            Object.DestroyImmediate(visual.GetComponent<CapsuleCollider>());

            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    color = new Color(0.2f, 0.4f, 0.8f)
                };
            }

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
            player.AddComponent<Input.PlayerInputProvider>();

            // Ground Check Transform
            var groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.parent = player.transform;
            groundCheck.transform.localPosition = Vector3.zero;

            // Hinweis für MovementConfig
            Debug.Log("[DemoSceneSetup] Player erstellt. Bitte eine MovementConfig im PlayerController zuweisen!");
        }

        #endregion

        #region Prefab Creation

        private static void CreatePlayerPrefab()
        {
            var player = new GameObject("BasicPlayer");

            // CharacterController
            var cc = player.AddComponent<UnityEngine.CharacterController>();
            cc.height = 2f;
            cc.radius = 0.5f;
            cc.center = Vector3.up;

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

            player.AddComponent<Input.PlayerInputProvider>();

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

        [MenuItem("Wiesenwischer/GameKit/Create Default MovementConfig", false, 102)]
        public static void CreateDefaultMovementConfig()
        {
            EnsureDirectoryExists("Assets/Config");

            var config = ScriptableObject.CreateInstance<Movement.MovementConfig>();

            AssetDatabase.CreateAsset(config, "Assets/Config/DefaultMovementConfig.asset");
            AssetDatabase.SaveAssets();

            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);

            Debug.Log("[DemoSceneSetup] DefaultMovementConfig erstellt: Assets/Config/DefaultMovementConfig.asset");
        }

        #endregion
    }
}
