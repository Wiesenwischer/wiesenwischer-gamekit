using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Wiesenwischer.GameKit.CharacterController.Camera.Editor
{
    /// <summary>
    /// Editor-Tools für das Kamera-Setup.
    /// </summary>
    public static class CameraSetupEditor
    {
        private const string ConfigPath = "Assets/Config/DefaultCameraConfig.asset";
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

        [MenuItem("Wiesenwischer/GameKit/Camera/Setup Third Person Camera", false, 200)]
        public static void SetupThirdPersonCamera()
        {
            // Suche nach Player — ohne Target ist Kamera-Setup sinnlos
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                player = GameObject.Find("Player");

            if (player == null)
            {
                EditorUtility.DisplayDialog(
                    "Kein Player gefunden",
                    "In der aktuellen Szene wurde kein Player gefunden.\n\n" +
                    "Bitte zuerst über 'Animation > Place Player in Scene' einen Player platzieren.",
                    "OK");
                return;
            }

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

            // InputActionAsset zuweisen
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (inputActions != null)
            {
                var inputSo = new SerializedObject(inputHandler);
                inputSo.FindProperty("_inputActions").objectReferenceValue = inputActions;
                inputSo.ApplyModifiedProperties();
                Debug.Log("[CameraSetup] InputActionAsset zugewiesen.");
            }
            else
            {
                Debug.LogWarning($"[CameraSetup] InputActionAsset nicht gefunden: {InputActionsPath}");
            }

            // Suche oder erstelle CameraConfig
            var config = FindOrCreateCameraConfig();
            if (config != null)
            {
                var serializedObject = new SerializedObject(thirdPersonCamera);
                var configProperty = serializedObject.FindProperty("_config");
                configProperty.objectReferenceValue = config;
                serializedObject.ApplyModifiedProperties();
                Debug.Log("[CameraSetup] CameraConfig zugewiesen.");
            }

            // Target zuweisen
            var targetSo = new SerializedObject(thirdPersonCamera);
            var targetProperty = targetSo.FindProperty("_target");
            targetProperty.objectReferenceValue = player.transform;
            targetSo.ApplyModifiedProperties();
            Debug.Log($"[CameraSetup] Target gesetzt auf: {player.name}");

            // Positioniere Kamera hinter Player
            thirdPersonCamera.SnapBehindTarget();

            // Wähle die Kamera aus
            Selection.activeGameObject = mainCamera.gameObject;
            EditorGUIUtility.PingObject(mainCamera.gameObject);

            Debug.Log("[CameraSetup] Third Person Camera Setup abgeschlossen!");
        }

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
