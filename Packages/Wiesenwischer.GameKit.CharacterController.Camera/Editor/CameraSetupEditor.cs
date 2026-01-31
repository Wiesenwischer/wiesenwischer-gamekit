using UnityEditor;
using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Camera.Editor
{
    /// <summary>
    /// Editor-Tools für das Kamera-Setup.
    /// </summary>
    public static class CameraSetupEditor
    {
        private const string ConfigPath = "Assets/Config/DefaultCameraConfig.asset";

        [MenuItem("Wiesenwischer/GameKit/Camera/Setup Third Person Camera", false, 200)]
        public static void SetupThirdPersonCamera()
        {
            // Finde oder erstelle Main Camera
            var mainCamera = UnityEngine.Camera.main;
            if (mainCamera == null)
            {
                var cameraGO = new GameObject("Main Camera");
                cameraGO.tag = "MainCamera";
                mainCamera = cameraGO.AddComponent<UnityEngine.Camera>();
                cameraGO.AddComponent<AudioListener>();
                Debug.Log("[CameraSetup] Main Camera erstellt.");
            }

            // Füge ThirdPersonCamera hinzu
            var thirdPersonCamera = mainCamera.GetComponent<ThirdPersonCamera>();
            if (thirdPersonCamera == null)
            {
                thirdPersonCamera = mainCamera.gameObject.AddComponent<ThirdPersonCamera>();
                Debug.Log("[CameraSetup] ThirdPersonCamera Component hinzugefügt.");
            }

            // Füge CameraInputHandler hinzu
            var inputHandler = mainCamera.GetComponent<CameraInputHandler>();
            if (inputHandler == null)
            {
                inputHandler = mainCamera.gameObject.AddComponent<CameraInputHandler>();
                Debug.Log("[CameraSetup] CameraInputHandler Component hinzugefügt.");
            }

            // Suche oder erstelle CameraConfig
            var config = FindOrCreateCameraConfig();
            if (config != null)
            {
                // Setze Config via SerializedObject
                var serializedObject = new SerializedObject(thirdPersonCamera);
                var configProperty = serializedObject.FindProperty("_config");
                configProperty.objectReferenceValue = config;
                serializedObject.ApplyModifiedProperties();
                Debug.Log("[CameraSetup] CameraConfig zugewiesen.");
            }

            // Suche nach Player
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                player = GameObject.Find("Player");
            }

            if (player != null)
            {
                var serializedObject = new SerializedObject(thirdPersonCamera);
                var targetProperty = serializedObject.FindProperty("_target");
                targetProperty.objectReferenceValue = player.transform;
                serializedObject.ApplyModifiedProperties();
                Debug.Log($"[CameraSetup] Target gesetzt auf: {player.name}");

                // Positioniere Kamera hinter Player
                thirdPersonCamera.SnapBehindTarget();
            }
            else
            {
                Debug.LogWarning("[CameraSetup] Kein Player gefunden. Bitte Target manuell zuweisen.");
            }

            // Wähle die Kamera aus
            Selection.activeGameObject = mainCamera.gameObject;
            EditorGUIUtility.PingObject(mainCamera.gameObject);

            Debug.Log("[CameraSetup] Third Person Camera Setup abgeschlossen!");
        }

        [MenuItem("Wiesenwischer/GameKit/Camera/Create Default Camera Config", false, 201)]
        public static void CreateDefaultCameraConfig()
        {
            var config = FindOrCreateCameraConfig();
            if (config != null)
            {
                Selection.activeObject = config;
                EditorGUIUtility.PingObject(config);
            }
        }

        private static CameraConfig FindOrCreateCameraConfig()
        {
            // Suche existierende Config
            var config = AssetDatabase.LoadAssetAtPath<CameraConfig>(ConfigPath);
            if (config != null)
            {
                return config;
            }

            // Suche alle CameraConfigs
            var guids = AssetDatabase.FindAssets("t:CameraConfig");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<CameraConfig>(path);
            }

            // Erstelle neue Config
            EnsureDirectoryExists("Assets/Config");
            config = ScriptableObject.CreateInstance<CameraConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[CameraSetup] CameraConfig erstellt: {ConfigPath}");

            return config;
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
    }
}
