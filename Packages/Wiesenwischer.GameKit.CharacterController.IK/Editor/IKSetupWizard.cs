using UnityEditor;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core;
using Wiesenwischer.GameKit.CharacterController.IK.Modules;

namespace Wiesenwischer.GameKit.CharacterController.IK.Editor
{
    /// <summary>
    /// Editor-Tool zum Hinzufügen der IK-Komponenten auf ein Player Prefab.
    /// Menü: Wiesenwischer > GameKit > Setup IK on Player Prefab
    /// </summary>
    public static class IKSetupWizard
    {
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";

        [MenuItem("Wiesenwischer/GameKit/Prefabs/Setup IK on Player Prefab", false, 201)]
        public static void SetupIKOnPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[IKSetupWizard] Player Prefab nicht gefunden: {PlayerPrefabPath}. " +
                               "Bitte zuerst 'Create Player Prefab' ausführen.");
                return;
            }

            // Prefab zum Bearbeiten öffnen
            var prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);

            // CharacterModel finden (Child mit Animator)
            var animator = prefabRoot.GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Debug.LogError("[IKSetupWizard] Kein Animator im Prefab gefunden.");
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                return;
            }

            var modelGO = animator.gameObject;
            var playerController = prefabRoot.GetComponent<PlayerController>();

            // Prüfen ob IK bereits eingerichtet ist
            if (modelGO.GetComponent<IKManager>() != null)
            {
                Debug.LogWarning("[IKSetupWizard] IK-Komponenten sind bereits vorhanden. Übersprungen.");
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                return;
            }

            // === IKManager ===
            var ikManager = modelGO.AddComponent<IKManager>();
            var ikManagerSo = new SerializedObject(ikManager);
            ikManagerSo.FindProperty("_playerController").objectReferenceValue = playerController;
            ikManagerSo.FindProperty("_masterWeight").floatValue = 1f;
            ikManagerSo.FindProperty("_disableDuringAirborne").boolValue = true;
            ikManagerSo.ApplyModifiedProperties();

            // === FootIK ===
            var footIK = modelGO.AddComponent<FootIK>();
            var footSo = new SerializedObject(footIK);
            footSo.FindProperty("_playerController").objectReferenceValue = playerController;
            footSo.FindProperty("_weight").floatValue = 1f;
            footSo.FindProperty("_raycastHeight").floatValue = 0.5f;
            footSo.FindProperty("_raycastDepth").floatValue = 0.3f;
            footSo.FindProperty("_footOffset").floatValue = 0.02f;
            footSo.FindProperty("_maxFootAdjustment").floatValue = 0.4f;
            footSo.ApplyModifiedProperties();

            // === CameraTargetProvider ===
            var cameraProvider = modelGO.AddComponent<CameraTargetProvider>();

            // === LookAtIK ===
            var lookAtIK = modelGO.AddComponent<LookAtIK>();
            var lookAtSo = new SerializedObject(lookAtIK);
            lookAtSo.FindProperty("_targetProvider").objectReferenceValue = cameraProvider;
            lookAtSo.FindProperty("_weight").floatValue = 1f;
            lookAtSo.FindProperty("_bodyWeight").floatValue = 0.2f;
            lookAtSo.FindProperty("_headWeight").floatValue = 0.8f;
            lookAtSo.FindProperty("_eyesWeight").floatValue = 0f;
            lookAtSo.FindProperty("_clampWeight").floatValue = 0.6f;
            lookAtSo.FindProperty("_smoothSpeed").floatValue = 5f;
            lookAtSo.ApplyModifiedProperties();

            // Prefab speichern
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            Debug.Log("[IKSetupWizard] IK-Komponenten erfolgreich hinzugefügt:");
            Debug.Log("  - IKManager (Master-Weight=1, Airborne-Deaktivierung=true)");
            Debug.Log("  - FootIK (Raycast-basierte Fuß-Anpassung)");
            Debug.Log("  - LookAtIK (Kamera-Blickrichtung)");
            Debug.Log("  - CameraTargetProvider (Camera.main als LookAt-Ziel)");

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        }

        [MenuItem("Wiesenwischer/GameKit/Prefabs/Setup IK on Player Prefab", true)]
        private static bool ValidateSetupIK()
        {
            return !Application.isPlaying;
        }
    }
}
