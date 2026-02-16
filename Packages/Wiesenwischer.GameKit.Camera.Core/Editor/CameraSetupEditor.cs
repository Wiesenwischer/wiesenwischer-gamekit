using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using Wiesenwischer.GameKit.Camera.Behaviours;

namespace Wiesenwischer.GameKit.Camera.Editor
{
    /// <summary>
    /// Editor-Tools für das modulare Camera-Setup.
    /// Erstellt CameraBrain + PivotRig + CameraAnchor + CameraInputPipeline + Standard-Behaviours.
    /// Re-Run-sicher: Findet immer den korrekten Root, räumt fehlplatzierte Komponenten auf.
    /// </summary>
    public static class CameraSetupEditor
    {
        private const string ConfigPath = "Assets/Config/CameraCoreConfig.asset";
        private const string InputSettingsPath = "Assets/Config/CameraInputSettings.asset";
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

        /// <summary>
        /// Vollständiges Camera-Setup: Komponenten, Preset, Validierung.
        /// Re-Run-sicher — kann beliebig oft aufgerufen werden.
        /// </summary>
        public static void SetupCameraBrain(CameraPreset preset = null)
        {
            // --- 1. Player finden ---
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

            // --- 2. Camera Root finden (re-run-sicher) ---
            var cameraRoot = FindCameraRoot();
            Undo.RegisterFullObjectHierarchyUndo(cameraRoot, "Camera Brain Setup");

            // --- 3. PivotRig + Hierarchie ---
            var pivotRig = EnsureComponent<PivotRig>(cameraRoot);
            pivotRig.EnsureHierarchy();

            // --- 4. Fehlplatzierte Komponenten vom Child aufräumen ---
            CleanupMisplacedComponents(pivotRig, cameraRoot);

            // --- 5. Core-Komponenten ---
            var anchor = EnsureComponent<CameraAnchor>(cameraRoot);
            anchor.FollowTarget = player.transform;

            var inputPipeline = EnsureComponent<CameraInputPipeline>(cameraRoot);
            ConfigureInputPipeline(inputPipeline);

            var brain = EnsureComponent<CameraBrain>(cameraRoot);
            EnsureComponent<CameraOrientationProvider>(cameraRoot);

            // Camera-Referenz: immer Child-Camera aus PivotRig
            var actualCamera = pivotRig.CameraTransform != null
                ? pivotRig.CameraTransform.GetComponent<UnityEngine.Camera>()
                : UnityEngine.Camera.main;

            ConfigureBrain(brain, anchor, inputPipeline, actualCamera);

            // --- 6. Behaviours ---
            SetupBehaviours(cameraRoot);

            // --- 7. Preset anwenden ---
            if (preset != null)
                ApplyPresetInEditor(brain, inputPipeline, preset);

            // --- 8. Snap (nur wenn Awake schon lief, z.B. im Play Mode) ---
            if (Application.isPlaying)
            {
                anchor.SnapToTarget();
                brain.SnapBehindTarget();
            }

            // --- 9. Validierung ---
            ValidateSetup(cameraRoot, brain, pivotRig);

            // --- 10. Fokus ---
            Selection.activeGameObject = cameraRoot;
            EditorGUIUtility.PingObject(cameraRoot);

            string presetInfo = preset != null ? $" mit Preset '{preset.name}'" : "";
            Debug.Log($"[CameraSetup] Camera Brain Setup abgeschlossen{presetInfo}!");
        }

        /// <summary>
        /// Wendet ein Preset im Edit-Mode vollständig an:
        /// _activePreset, Behaviours, OrbitActivation.
        /// </summary>
        public static void ApplyPresetInEditor(CameraBrain brain, CameraInputPipeline inputPipeline, CameraPreset preset)
        {
            if (brain == null || preset == null) return;

            // _activePreset auf CameraBrain setzen
            var brainSo = new SerializedObject(brain);
            brainSo.FindProperty("_activePreset").objectReferenceValue = preset;
            brainSo.ApplyModifiedProperties();

            // Behaviours konfigurieren
            Undo.RecordObjects(brain.GetComponents<Component>(), "Apply Camera Preset");
            foreach (var receiver in brain.GetComponents<ICameraPresetReceiver>())
            {
                receiver.ApplyPreset(preset);
                if (receiver is MonoBehaviour mb)
                    EditorUtility.SetDirty(mb);
            }

            // OrbitActivation auf InputPipeline setzen
            if (inputPipeline != null)
            {
                Undo.RecordObject(inputPipeline, "Apply Camera Preset");
                var pipelineSo = new SerializedObject(inputPipeline);
                pipelineSo.FindProperty("_orbitActivation").enumValueIndex = (int)preset.OrbitActivation;
                pipelineSo.ApplyModifiedProperties();
                EditorUtility.SetDirty(inputPipeline);
            }

            EditorUtility.SetDirty(brain);
            Debug.Log($"[CameraSetup] Preset '{preset.name}' angewendet " +
                $"(OrbitActivation: {preset.OrbitActivation}).");
        }

        #region Root Discovery

        /// <summary>
        /// Findet den korrekten Camera-Root. Re-Run-sicher:
        /// 1. Existierender CameraBrain → dessen GameObject
        /// 2. Camera.main → PivotRig Parent falls vorhanden
        /// 3. Camera.main → direktes GameObject
        /// 4. Neue Camera erstellen
        /// </summary>
        private static GameObject FindCameraRoot()
        {
            // Bevorzugt: Existierender CameraBrain
            var existingBrain = Object.FindObjectOfType<CameraBrain>();
            if (existingBrain != null)
            {
                Debug.Log($"[CameraSetup] Existierender CameraBrain gefunden: '{existingBrain.gameObject.name}'");
                return existingBrain.gameObject;
            }

            // Camera.main → traversiere zum PivotRig Root
            var mainCamera = UnityEngine.Camera.main;
            if (mainCamera != null)
            {
                var existingRig = mainCamera.GetComponentInParent<PivotRig>();
                if (existingRig != null)
                    return existingRig.gameObject;
                return mainCamera.gameObject;
            }

            // Neue Camera erstellen
            var cameraGO = new GameObject("Main Camera");
            cameraGO.tag = "MainCamera";
            cameraGO.AddComponent<UnityEngine.Camera>();
            cameraGO.AddComponent<AudioListener>();
            Debug.Log("[CameraSetup] Main Camera erstellt.");
            return cameraGO;
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Entfernt Komponenten die bei einem fehlerhaften Re-Run auf dem Child
        /// statt auf dem Root gelandet sind. Prüft alle Children unterhalb des Root.
        /// </summary>
        private static void CleanupMisplacedComponents(PivotRig pivotRig, GameObject root)
        {
            int cleaned = 0;

            // Alle Children durchgehen (nicht den Root selbst)
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.gameObject == root) continue;

                // Diese Komponenten dürfen NUR auf dem Root existieren
                cleaned += RemoveIfPresent<CameraBrain>(child.gameObject);
                cleaned += RemoveIfPresent<CameraOrientationProvider>(child.gameObject);
                cleaned += RemoveIfPresent<CameraAnchor>(child.gameObject);
                cleaned += RemoveIfPresent<CameraInputPipeline>(child.gameObject);
                cleaned += RemoveIfPresent<PivotRig>(child.gameObject);

                // Behaviours
                cleaned += RemoveIfPresent<OrbitBehaviour>(child.gameObject);
                cleaned += RemoveIfPresent<ZoomBehaviour>(child.gameObject);
                cleaned += RemoveIfPresent<CollisionBehaviour>(child.gameObject);
                cleaned += RemoveIfPresent<RecenterBehaviour>(child.gameObject);
                cleaned += RemoveIfPresent<InertiaBehaviour>(child.gameObject);
                cleaned += RemoveIfPresent<ShoulderOffsetBehaviour>(child.gameObject);
                cleaned += RemoveIfPresent<DynamicOrbitCenterBehaviour>(child.gameObject);
                cleaned += RemoveIfPresent<SoftTargetingBehaviour>(child.gameObject);
            }

            if (cleaned > 0)
                Debug.LogWarning($"[CameraSetup] {cleaned} fehlplatzierte Komponente(n) " +
                    "von Child-Objekten entfernt (vermutlich von einem früheren fehlerhaften Re-Run).");
        }

        private static int RemoveIfPresent<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            if (component == null) return 0;

            Debug.Log($"[CameraSetup] Entferne fehlplatzierte {typeof(T).Name} von '{go.name}'");
            Undo.DestroyObjectImmediate(component);
            return 1;
        }

        #endregion

        #region Component Setup

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var existing = go.GetComponent<T>();
            if (existing != null) return existing;

            var component = Undo.AddComponent<T>(go);
            Debug.Log($"[CameraSetup] {typeof(T).Name} hinzugefügt.");
            return component;
        }

        private static void ConfigureInputPipeline(CameraInputPipeline inputPipeline)
        {
            var inputSo = new SerializedObject(inputPipeline);

            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (inputActions != null)
            {
                inputSo.FindProperty("_inputActions").objectReferenceValue = inputActions;
            }
            else
            {
                Debug.LogWarning($"[CameraSetup] InputActionAsset nicht gefunden: {InputActionsPath}");
            }

            var inputSettings = FindOrCreateInputSettings();
            if (inputSettings != null)
                inputSo.FindProperty("_inputSettings").objectReferenceValue = inputSettings;

            inputSo.ApplyModifiedProperties();
        }

        private static void ConfigureBrain(
            CameraBrain brain, CameraAnchor anchor,
            CameraInputPipeline inputPipeline, UnityEngine.Camera actualCamera)
        {
            var config = FindOrCreateConfig();
            if (config == null) return;

            var brainSo = new SerializedObject(brain);
            brainSo.FindProperty("_config").objectReferenceValue = config;
            brainSo.FindProperty("_anchor").objectReferenceValue = anchor;
            brainSo.FindProperty("_inputPipeline").objectReferenceValue = inputPipeline;
            brainSo.FindProperty("_camera").objectReferenceValue = actualCamera;
            brainSo.ApplyModifiedProperties();
        }

        private static void SetupBehaviours(GameObject cameraRoot)
        {
            // Reihenfolge wichtig!
            // 1. DynamicOrbitCenter zuerst (modifiziert AnchorPosition)
            EnsureComponent<DynamicOrbitCenterBehaviour>(cameraRoot).enabled = false;
            // 2-4. Orbit, Recenter, Zoom
            EnsureComponent<OrbitBehaviour>(cameraRoot);
            EnsureComponent<RecenterBehaviour>(cameraRoot);
            EnsureComponent<ZoomBehaviour>(cameraRoot);
            // 5. ShoulderOffset
            EnsureComponent<ShoulderOffsetBehaviour>(cameraRoot).enabled = false;
            // 6. SoftTargeting
            EnsureComponent<SoftTargetingBehaviour>(cameraRoot).enabled = false;
            // 7-8. Collision, Inertia
            EnsureComponent<CollisionBehaviour>(cameraRoot);
            EnsureComponent<InertiaBehaviour>(cameraRoot);

#if CINEMACHINE_AVAILABLE
            EnsureComponent<CinemachineDriver>(cameraRoot).enabled = false;
#endif
        }

        #endregion

        #region Validation

        /// <summary>
        /// Prüft ob das Setup vollständig und korrekt ist.
        /// Gibt Warnungen aus für fehlende oder falsch konfigurierte Teile.
        /// </summary>
        private static void ValidateSetup(GameObject root, CameraBrain brain, PivotRig pivotRig)
        {
            int warnings = 0;

            // CameraOrientationProvider vorhanden?
            if (root.GetComponent<CameraOrientationProvider>() == null)
            {
                Debug.LogError("[CameraSetup] FEHLER: CameraOrientationProvider fehlt! " +
                    "LMB/RMB-Unterscheidung funktioniert nicht.");
                warnings++;
            }

            // Camera auf Child (nicht Root)?
            var rootCamera = root.GetComponent<UnityEngine.Camera>();
            if (rootCamera != null && rootCamera.enabled)
            {
                Debug.LogWarning("[CameraSetup] Camera-Komponente auf Root ist noch aktiv. " +
                    "Sollte auf dem Child (unter OffsetPivot) sein.");
                warnings++;
            }

            // PivotRig Hierarchie korrekt?
            if (pivotRig.CameraTransform == null)
            {
                Debug.LogWarning("[CameraSetup] PivotRig.CameraTransform ist null. " +
                    "Hierarchie möglicherweise defekt.");
                warnings++;
            }

            // MainCamera Tag auf Child?
            if (pivotRig.CameraTransform != null &&
                !pivotRig.CameraTransform.CompareTag("MainCamera"))
            {
                Debug.LogWarning("[CameraSetup] Child-Camera hat keinen 'MainCamera' Tag. " +
                    "Camera.main findet die Kamera möglicherweise nicht.");
                warnings++;
            }

            // Preset gesetzt?
            var brainSo = new SerializedObject(brain);
            var presetProp = brainSo.FindProperty("_activePreset");
            if (presetProp.objectReferenceValue == null)
            {
                Debug.LogWarning("[CameraSetup] Kein Camera-Preset zugewiesen. " +
                    "Bitte ein Preset im Camera Brain Window auswählen und anwenden.");
                warnings++;
            }

            if (warnings == 0)
                Debug.Log("[CameraSetup] Validierung bestanden — alle Komponenten korrekt.");
            else
                Debug.LogWarning($"[CameraSetup] Validierung: {warnings} Warnung(en) gefunden.");
        }

        #endregion

        #region Asset Helpers

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

        private static CameraInputSettings FindOrCreateInputSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<CameraInputSettings>(InputSettingsPath);
            if (settings != null) return settings;

            var guids = AssetDatabase.FindAssets("t:CameraInputSettings");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<CameraInputSettings>(path);
            }

            EnsureDirectoryExists("Assets/Config");
            settings = ScriptableObject.CreateInstance<CameraInputSettings>();
            AssetDatabase.CreateAsset(settings, InputSettingsPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[CameraSetup] CameraInputSettings erstellt: {InputSettingsPath}");

            return settings;
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

        #endregion
    }
}
