using FishNet.Component.Transforming.Beta;
using FishNet.Managing;
using FishNet.Managing.Object;
using FishNet.Object;
using FishNet.Transporting.Tugboat;
using UnityEditor;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core;

namespace Wiesenwischer.GameKit.Network.Editor
{
    /// <summary>
    /// Editor-Fenster für Netzwerk-Setup:
    /// 1. Player Prefab mit Network-Komponenten ausstatten
    /// 2. Scene NetworkManager erstellen
    /// </summary>
    public class NetworkSetupWizard : EditorWindow
    {
        private GameObject _playerPrefab;
        private Vector2 _scrollPos;

        [MenuItem("Wiesenwischer/GameKit/Network/Network Setup Wizard", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<NetworkSetupWizard>("Network Setup Wizard");
            window.minSize = new Vector2(360, 420);
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.LabelField("Network Setup Wizard", EditorStyles.boldLabel);
            EditorGUILayout.Space(8);

            DrawPlayerPrefabSection();
            EditorGUILayout.Space(12);
            DrawSceneNetworkManagerSection();

            EditorGUILayout.EndScrollView();
        }

        #region Player Prefab Setup

        private void DrawPlayerPrefabSection()
        {
            EditorGUILayout.LabelField("Player Prefab Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            _playerPrefab = (GameObject)EditorGUILayout.ObjectField(
                "Player Prefab", _playerPrefab, typeof(GameObject), false);

            if (_playerPrefab == null)
            {
                EditorGUILayout.HelpBox(
                    "Ziehe ein Player Prefab in das Feld oben.\n" +
                    "Das Prefab muss einen PlayerController besitzen.",
                    MessageType.Info);
                return;
            }

            // Prüfe ob es ein Prefab ist
            if (PrefabUtility.GetPrefabAssetType(_playerPrefab) == PrefabAssetType.NotAPrefab)
            {
                EditorGUILayout.HelpBox("Das ausgewählte Objekt ist kein Prefab.", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Status:", EditorStyles.miniBoldLabel);

            // Status-Anzeige
            bool hasPlayerController = _playerPrefab.GetComponent<PlayerController>() != null;
            bool hasNetworkObject = _playerPrefab.GetComponent<NetworkObject>() != null;
            bool hasNetworkPlayer = _playerPrefab.GetComponent<NetworkPlayer>() != null;
            bool hasNetworkCharacterDriver = _playerPrefab.GetComponent<NetworkCharacterDriver>() != null;
            bool hasNetworkAnimationSync = _playerPrefab.GetComponent<NetworkAnimationSync>() != null;
            bool hasNetworkTickSmoother = _playerPrefab.GetComponent<NetworkTickSmoother>() != null;

            DrawStatusLine("PlayerController", hasPlayerController, true);
            DrawStatusLine("NetworkObject", hasNetworkObject);
            DrawStatusLine("NetworkPlayer", hasNetworkPlayer);
            DrawStatusLine("NetworkCharacterDriver", hasNetworkCharacterDriver);
            DrawStatusLine("NetworkAnimationSync", hasNetworkAnimationSync);
            DrawStatusLine("NetworkTickSmoother", hasNetworkTickSmoother);

            if (!hasPlayerController)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(
                    "PlayerController ist Voraussetzung!\n" +
                    "Bitte zuerst den CharacterController Setup Wizard ausführen.",
                    MessageType.Error);
                return;
            }

            bool allPresent = hasNetworkObject && hasNetworkPlayer && hasNetworkCharacterDriver && hasNetworkAnimationSync && hasNetworkTickSmoother;

            if (allPresent)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox("Alle Network-Komponenten sind vorhanden.", MessageType.Info);

                if (GUILayout.Button("In DefaultPrefabObjects registrieren"))
                    RegisterInDefaultPrefabObjects(_playerPrefab.GetComponent<NetworkObject>());
            }
            else
            {
                EditorGUILayout.Space(4);

                if (GUILayout.Button("Setup Network Components", GUILayout.Height(28)))
                    SetupNetworkComponents();
            }
        }

        private void SetupNetworkComponents()
        {
            string prefabPath = AssetDatabase.GetAssetPath(_playerPrefab);
            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

            Undo.SetCurrentGroupName("Setup Network Components");
            int undoGroup = Undo.GetCurrentGroup();

            // 1. NetworkObject (FishNet)
            if (prefabRoot.GetComponent<NetworkObject>() == null)
                prefabRoot.AddComponent<NetworkObject>();

            // 1b. Prediction aktivieren (Voraussetzung fuer [Replicate]/[Reconcile])
            var nob = prefabRoot.GetComponent<NetworkObject>();
            if (nob != null)
            {
                var nobSo = new SerializedObject(nob);
                var predProp = nobSo.FindProperty("_enablePrediction");
                if (predProp != null) predProp.boolValue = true;
                var fwdProp = nobSo.FindProperty("_enableStateForwarding");
                if (fwdProp != null) fwdProp.boolValue = true;
                nobSo.ApplyModifiedProperties();
            }

            // 2. NetworkPlayer
            if (prefabRoot.GetComponent<NetworkPlayer>() == null)
                prefabRoot.AddComponent<NetworkPlayer>();

            // 3. NetworkCharacterDriver (ersetzt NetworkInputSync + NetworkStateSync + RemotePlayerInterpolator)
            if (prefabRoot.GetComponent<NetworkCharacterDriver>() == null)
                prefabRoot.AddComponent<NetworkCharacterDriver>();

            // 4. NetworkAnimationSync (Animation State + Parameter Sync)
            if (prefabRoot.GetComponent<NetworkAnimationSync>() == null)
                prefabRoot.AddComponent<NetworkAnimationSync>();

            // 5. NetworkTickSmoother (FishNet's eingebautes visuelles Smoothing)
            if (prefabRoot.GetComponent<NetworkTickSmoother>() == null)
            {
                var smoother = prefabRoot.AddComponent<NetworkTickSmoother>();

                // TargetTransform = root (das Objekt das sich jeden Tick bewegt)
                var smootherSo = new SerializedObject(smoother);
                var initSettings = smootherSo.FindProperty("_initializationSettings");
                if (initSettings != null)
                {
                    var targetProp = initSettings.FindPropertyRelative("TargetTransform");
                    if (targetProp != null)
                        targetProp.objectReferenceValue = prefabRoot.transform;
                }

                // Adaptive Interpolation fuer Owner (Low = RTT + 3 Ticks)
                var controllerSettings = smootherSo.FindProperty("_controllerMovementSettings");
                if (controllerSettings != null)
                {
                    var adaptiveProp = controllerSettings.FindPropertyRelative("AdaptiveInterpolationValue");
                    if (adaptiveProp != null)
                        adaptiveProp.intValue = (int)AdaptiveInterpolationType.Low;

                    var teleportProp = controllerSettings.FindPropertyRelative("EnableTeleport");
                    if (teleportProp != null)
                        teleportProp.boolValue = true;

                    var thresholdProp = controllerSettings.FindPropertyRelative("TeleportThreshold");
                    if (thresholdProp != null)
                        thresholdProp.floatValue = 5f;
                }

                // Adaptive Interpolation fuer Spectator (Moderate = RTT + 4 Ticks)
                var spectatorSettings = smootherSo.FindProperty("_spectatorMovementSettings");
                if (spectatorSettings != null)
                {
                    var adaptiveProp = spectatorSettings.FindPropertyRelative("AdaptiveInterpolationValue");
                    if (adaptiveProp != null)
                        adaptiveProp.intValue = (int)AdaptiveInterpolationType.Moderate;

                    var teleportProp = spectatorSettings.FindPropertyRelative("EnableTeleport");
                    if (teleportProp != null)
                        teleportProp.boolValue = true;

                    var thresholdProp = spectatorSettings.FindPropertyRelative("TeleportThreshold");
                    if (thresholdProp != null)
                        thresholdProp.floatValue = 5f;
                }

                // FavorPredictionNetworkTransform deaktivieren (wir nutzen kein NetworkTransform)
                var favorProp = smootherSo.FindProperty("_favorPredictionNetworkTransform");
                if (favorProp != null)
                    favorProp.boolValue = false;

                smootherSo.ApplyModifiedProperties();
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            Undo.CollapseUndoOperations(undoGroup);

            // Referenz aktualisieren
            _playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            // In DefaultPrefabObjects registrieren
            var networkObject = _playerPrefab.GetComponent<NetworkObject>();
            if (networkObject != null)
                RegisterInDefaultPrefabObjects(networkObject);

            Debug.Log("[NetworkSetupWizard] Network-Komponenten erfolgreich hinzugefügt.");
        }

        private void RegisterInDefaultPrefabObjects(NetworkObject networkObject)
        {
            // DefaultPrefabObjects Asset finden
            var guids = AssetDatabase.FindAssets("t:DefaultPrefabObjects");
            if (guids.Length == 0)
            {
                Debug.LogWarning("[NetworkSetupWizard] Kein DefaultPrefabObjects Asset gefunden. " +
                                 "Bitte manuell in FishNet NetworkManager zuweisen.");
                return;
            }

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var defaultPrefabs = AssetDatabase.LoadAssetAtPath<DefaultPrefabObjects>(path);

            if (defaultPrefabs == null)
            {
                Debug.LogWarning("[NetworkSetupWizard] DefaultPrefabObjects konnte nicht geladen werden.");
                return;
            }

            Undo.RecordObject(defaultPrefabs, "Register Network Prefab");
            defaultPrefabs.AddObject(networkObject, checkForDuplicates: true);
            EditorUtility.SetDirty(defaultPrefabs);
            AssetDatabase.SaveAssets();

            Debug.Log($"[NetworkSetupWizard] Prefab in DefaultPrefabObjects registriert: {path}");
        }

        private void DrawStatusLine(string label, bool present, bool isPrerequisite = false)
        {
            EditorGUILayout.BeginHorizontal();
            string icon = present ? "\u2705" : (isPrerequisite ? "\u274C" : "\u2B1C");
            EditorGUILayout.LabelField($"  {icon} {label}");
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Scene NetworkManager Setup

        private void DrawSceneNetworkManagerSection()
        {
            EditorGUILayout.LabelField("Scene NetworkManager Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            var existingManager = FindObjectOfType<NetworkManager>();

            if (existingManager != null)
            {
                EditorGUILayout.HelpBox(
                    $"NetworkManager gefunden: \"{existingManager.gameObject.name}\"",
                    MessageType.Info);

                // Status der GameKit-Komponenten
                bool hasGameNetworkManager = existingManager.GetComponent<GameNetworkManager>() != null;
                bool hasNetworkDebugUI = existingManager.GetComponent<NetworkDebugUI>() != null;
                bool hasTugboat = existingManager.GetComponent<Tugboat>() != null;

                // Player Prefab Zuweisung prüfen
                bool hasPlayerPrefab = false;
                var gnm = existingManager.GetComponent<GameNetworkManager>();
                if (gnm != null)
                {
                    var so = new SerializedObject(gnm);
                    var prefabProp = so.FindProperty("_playerPrefab");
                    hasPlayerPrefab = prefabProp != null && prefabProp.objectReferenceValue != null;
                }

                DrawStatusLine("NetworkManager (FishNet)", true);
                DrawStatusLine("Tugboat Transport", hasTugboat);
                DrawStatusLine("GameNetworkManager", hasGameNetworkManager);
                DrawStatusLine("NetworkDebugUI", hasNetworkDebugUI);
                if (hasGameNetworkManager)
                    DrawStatusLine("Player Prefab zugewiesen", hasPlayerPrefab);

                bool allPresent = hasGameNetworkManager && hasNetworkDebugUI && hasTugboat;

                if (!allPresent)
                {
                    EditorGUILayout.Space(4);
                    if (GUILayout.Button("Fehlende Komponenten hinzufügen", GUILayout.Height(28)))
                        AddMissingManagerComponents(existingManager.gameObject);
                }

                if (hasGameNetworkManager && !hasPlayerPrefab && _playerPrefab != null)
                {
                    EditorGUILayout.Space(4);
                    if (GUILayout.Button("Player Prefab zuweisen"))
                        AssignPlayerPrefabToManager(existingManager.gameObject);
                }

                EditorGUILayout.Space(4);
                if (GUILayout.Button("Im Inspector auswählen"))
                    Selection.activeGameObject = existingManager.gameObject;
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Kein NetworkManager in der aktuellen Szene gefunden.",
                    MessageType.Warning);

                EditorGUILayout.Space(4);

                if (GUILayout.Button("Create NetworkManager in Scene", GUILayout.Height(28)))
                    CreateNetworkManager();
            }
        }

        private void CreateNetworkManager()
        {
            var go = new GameObject("NetworkManager");
            Undo.RegisterCreatedObjectUndo(go, "Create NetworkManager");

            // FishNet NetworkManager
            Undo.AddComponent<NetworkManager>(go);

            // Tugboat Transport
            Undo.AddComponent<Tugboat>(go);

            // Transport dem NetworkManager zuweisen
            var nm = go.GetComponent<NetworkManager>();
            var tugboat = go.GetComponent<Tugboat>();
            if (nm != null && tugboat != null)
            {
                var so = new SerializedObject(nm);
                var transportProp = so.FindProperty("_transport");
                if (transportProp != null)
                {
                    transportProp.objectReferenceValue = tugboat;
                    so.ApplyModifiedProperties();
                }
            }

            // SpawnablePrefabs zuweisen
            var prefabGuids = AssetDatabase.FindAssets("t:DefaultPrefabObjects");
            if (prefabGuids.Length > 0)
            {
                var prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[0]);
                var defaultPrefabs = AssetDatabase.LoadAssetAtPath<PrefabObjects>(prefabPath);
                if (defaultPrefabs != null && nm != null)
                {
                    nm.SpawnablePrefabs = defaultPrefabs;
                    EditorUtility.SetDirty(nm);
                }
            }

            // GameKit-Komponenten
            Undo.AddComponent<GameNetworkManager>(go);
            Undo.AddComponent<NetworkDebugUI>(go);

            // Player Prefab zuweisen falls im Wizard ausgewählt
            AssignPlayerPrefabToManager(go);

            Selection.activeGameObject = go;
            Debug.Log("[NetworkSetupWizard] NetworkManager erstellt mit allen Komponenten.");
        }

        private void AddMissingManagerComponents(GameObject go)
        {
            Undo.SetCurrentGroupName("Add Missing Network Components");
            int undoGroup = Undo.GetCurrentGroup();

            if (go.GetComponent<Tugboat>() == null)
                Undo.AddComponent<Tugboat>(go);

            // Transport zuweisen falls nötig
            var nm = go.GetComponent<NetworkManager>();
            var tugboat = go.GetComponent<Tugboat>();
            if (nm != null && tugboat != null)
            {
                var so = new SerializedObject(nm);
                var transportProp = so.FindProperty("_transport");
                if (transportProp != null && transportProp.objectReferenceValue == null)
                {
                    transportProp.objectReferenceValue = tugboat;
                    so.ApplyModifiedProperties();
                }
            }

            if (go.GetComponent<GameNetworkManager>() == null)
                Undo.AddComponent<GameNetworkManager>(go);

            if (go.GetComponent<NetworkDebugUI>() == null)
                Undo.AddComponent<NetworkDebugUI>(go);

            // Player Prefab zuweisen falls im Wizard ausgewählt
            AssignPlayerPrefabToManager(go);

            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log("[NetworkSetupWizard] Fehlende Komponenten hinzugefügt.");
        }

        private void AssignPlayerPrefabToManager(GameObject managerGo)
        {
            if (_playerPrefab == null) return;

            var networkObject = _playerPrefab.GetComponent<NetworkObject>();
            if (networkObject == null) return;

            var gameNetworkManager = managerGo.GetComponent<GameNetworkManager>();
            if (gameNetworkManager == null) return;

            var so = new SerializedObject(gameNetworkManager);
            var prefabProp = so.FindProperty("_playerPrefab");
            if (prefabProp != null)
            {
                prefabProp.objectReferenceValue = networkObject;
                so.ApplyModifiedProperties();
                Debug.Log($"[NetworkSetupWizard] Player Prefab zugewiesen: {_playerPrefab.name}");
            }
        }

        #endregion
    }
}
