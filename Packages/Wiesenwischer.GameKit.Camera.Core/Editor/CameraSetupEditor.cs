using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using Wiesenwischer.GameKit.Camera.Behaviours;

namespace Wiesenwischer.GameKit.Camera.Editor
{
    /// <summary>
    /// Editor-Tools für das modulare Camera-Setup.
    /// Erstellt CameraBrain + PivotRig + CameraAnchor + CameraInputPipeline + Standard-Behaviours.
    /// </summary>
    public static class CameraSetupEditor
    {
        private const string ConfigPath = "Assets/Config/CameraCoreConfig.asset";
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

        public static void SetupCameraBrain()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                player = GameObject.Find("Player");

            if (player == null)
            {
                EditorUtility.DisplayDialog(
                    "Kein Player gefunden",
                    "In der aktuellen Szene wurde kein Player gefunden.\n\n" +
                    "Bitte zuerst einen Player in die Szene platzieren.",
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

            var cameraRoot = mainCamera.gameObject;

            // PivotRig hinzufügen + Hierarchie erstellen
            var pivotRig = cameraRoot.GetComponent<PivotRig>();
            if (pivotRig == null)
            {
                pivotRig = cameraRoot.AddComponent<PivotRig>();
                Debug.Log("[CameraSetup] PivotRig hinzugefügt.");
            }
            pivotRig.EnsureHierarchy();

            // CameraAnchor als separate Komponente am Camera Root
            var anchor = cameraRoot.GetComponent<CameraAnchor>();
            if (anchor == null)
            {
                anchor = cameraRoot.AddComponent<CameraAnchor>();
                Debug.Log("[CameraSetup] CameraAnchor hinzugefügt.");
            }
            anchor.FollowTarget = player.transform;

            // CameraInputPipeline
            var inputPipeline = cameraRoot.GetComponent<CameraInputPipeline>();
            if (inputPipeline == null)
            {
                inputPipeline = cameraRoot.AddComponent<CameraInputPipeline>();
                Debug.Log("[CameraSetup] CameraInputPipeline hinzugefügt.");
            }

            // InputActionAsset zuweisen
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (inputActions != null)
            {
                var inputSo = new SerializedObject(inputPipeline);
                inputSo.FindProperty("_inputActions").objectReferenceValue = inputActions;
                inputSo.ApplyModifiedProperties();
                Debug.Log("[CameraSetup] InputActionAsset zugewiesen.");
            }
            else
            {
                Debug.LogWarning($"[CameraSetup] InputActionAsset nicht gefunden: {InputActionsPath}");
            }

            // CameraBrain
            var brain = cameraRoot.GetComponent<CameraBrain>();
            if (brain == null)
            {
                brain = cameraRoot.AddComponent<CameraBrain>();
                Debug.Log("[CameraSetup] CameraBrain hinzugefügt.");
            }

            // Korrekte Camera-Referenz ermitteln (Child-Camera unter OffsetPivot, nicht Root-Camera)
            var actualCamera = pivotRig.CameraTransform != null
                ? pivotRig.CameraTransform.GetComponent<UnityEngine.Camera>()
                : mainCamera;

            // Config zuweisen
            var config = FindOrCreateConfig();
            if (config != null)
            {
                var brainSo = new SerializedObject(brain);
                brainSo.FindProperty("_config").objectReferenceValue = config;
                brainSo.FindProperty("_anchor").objectReferenceValue = anchor;
                brainSo.FindProperty("_inputPipeline").objectReferenceValue = inputPipeline;
                brainSo.FindProperty("_camera").objectReferenceValue = actualCamera;
                brainSo.ApplyModifiedProperties();
                Debug.Log("[CameraSetup] CameraBrain konfiguriert.");
            }

            // Standard-Behaviours hinzufügen (Reihenfolge wichtig!)
            // 1. DynamicOrbitCenter muss zuerst (modifiziert AnchorPosition)
            var dynamicOrbit = AddBehaviourIfMissing<DynamicOrbitCenterBehaviour>(cameraRoot);
            dynamicOrbit.enabled = false;
            // 2-4. Orbit, Recenter, Zoom
            AddBehaviourIfMissing<OrbitBehaviour>(cameraRoot);
            AddBehaviourIfMissing<RecenterBehaviour>(cameraRoot);
            AddBehaviourIfMissing<ZoomBehaviour>(cameraRoot);
            // 5. ShoulderOffset
            var shoulder = AddBehaviourIfMissing<ShoulderOffsetBehaviour>(cameraRoot);
            shoulder.enabled = false;
            // 6. SoftTargeting
            var softTargeting = AddBehaviourIfMissing<SoftTargetingBehaviour>(cameraRoot);
            softTargeting.enabled = false;
            // 7-8. Collision, Inertia
            AddBehaviourIfMissing<CollisionBehaviour>(cameraRoot);
            AddBehaviourIfMissing<InertiaBehaviour>(cameraRoot);
            Debug.Log("[CameraSetup] Alle Behaviours hinzugefügt.");

#if CINEMACHINE_AVAILABLE
            var cinemachineDriver = AddBehaviourIfMissing<CinemachineDriver>(cameraRoot);
            cinemachineDriver.enabled = false;
            Debug.Log("[CameraSetup] CinemachineDriver hinzugefügt (deaktiviert).");
#endif

            // Snap hinter Target
            anchor.SnapToTarget();
            brain.SnapBehindTarget();

            Selection.activeGameObject = cameraRoot;
            EditorGUIUtility.PingObject(cameraRoot);

            Debug.Log("[CameraSetup] Camera Brain Setup abgeschlossen!");
        }

        private static T AddBehaviourIfMissing<T>(GameObject go) where T : Component
        {
            var existing = go.GetComponent<T>();
            if (existing != null) return existing;

            var component = go.AddComponent<T>();
            Debug.Log($"[CameraSetup] {typeof(T).Name} hinzugefügt.");
            return component;
        }

        private static CameraCoreConfig FindOrCreateConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<CameraCoreConfig>(ConfigPath);
            if (config != null) return config;

            var guids = AssetDatabase.FindAssets("t:CameraCoreConfig");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<CameraCoreConfig>(path);
            }

            EnsureDirectoryExists("Assets/Config");
            config = ScriptableObject.CreateInstance<CameraCoreConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[CameraSetup] CameraCoreConfig erstellt: {ConfigPath}");

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
                        AssetDatabase.CreateFolder(currentPath, parts[i]);
                    currentPath = nextPath;
                }
            }
        }
    }
}
