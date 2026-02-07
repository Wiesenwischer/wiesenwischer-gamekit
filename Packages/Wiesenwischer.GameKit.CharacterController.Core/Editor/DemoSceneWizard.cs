using UnityEditor;
using UnityEngine;
using CameraPackage = Wiesenwischer.GameKit.CharacterController.Camera;

namespace Wiesenwischer.GameKit.CharacterController.Core.Editor
{
    /// <summary>
    /// Wizard-Fenster für das Setup der Demo-Szene.
    /// Führt durch alle notwendigen Schritte.
    /// </summary>
    public class DemoSceneWizard : EditorWindow
    {
        private Vector2 _scrollPosition;
        private GUIStyle _headerStyle;
        private GUIStyle _stepStyle;
        private GUIStyle _completedStyle;
        private GUIStyle _pendingStyle;
        private bool _stylesInitialized;

        [MenuItem("Wiesenwischer/GameKit/Demo Scene Wizard", false, 50)]
        public static void ShowWindow()
        {
            var window = GetWindow<DemoSceneWizard>("Demo Scene Wizard");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(0, 0, 10, 10)
            };

            _stepStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(0, 0, 5, 5)
            };

            _completedStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.2f, 0.8f, 0.2f) }
            };

            _pendingStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.8f, 0.6f, 0.2f) }
            };

            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // Header
            EditorGUILayout.LabelField("Demo Scene Setup Wizard", _headerStyle);
            EditorGUILayout.HelpBox(
                "Dieser Wizard führt dich durch alle Schritte zum Erstellen einer funktionierenden Demo-Szene.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // Scene Type Selection
            EditorGUILayout.LabelField("Szenen-Typ wählen", _headerStyle);

            EditorGUILayout.BeginVertical(_stepStyle);
            EditorGUILayout.LabelField("Option A: Primitive-basierte Szene", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Einfache Szene mit Cubes, Rampen und Treppen.", EditorStyles.wordWrappedLabel);
            if (GUILayout.Button("Erstelle Primitive Demo Scene", GUILayout.Height(30)))
            {
                DemoSceneSetup.CreateDemoScene();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(_stepStyle);
            EditorGUILayout.LabelField("Option B: Terrain-basierte Szene", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Realistische Szene mit Unity Terrain und verschiedenen Hangwinkeln.", EditorStyles.wordWrappedLabel);
            if (GUILayout.Button("Erstelle Terrain Demo Scene", GUILayout.Height(30)))
            {
                DemoSceneSetup.CreateTerrainDemoScene();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(20);

            // Step-by-Step Setup
            EditorGUILayout.LabelField("Schritt-für-Schritt Setup", _headerStyle);

            // Step 1: LocomotionConfig
            DrawStep(
                "1. LocomotionConfig erstellen",
                "Erstellt die Locomotion-Konfiguration (Geschwindigkeit, Sprung, Slope-Limit, etc.)",
                HasLocomotionConfig(),
                () => DemoSceneSetup.CreateDefaultLocomotionConfig()
            );

            // Step 2: Camera Setup
            DrawStep(
                "2. Third Person Camera einrichten",
                "Fügt die Kamera-Komponenten zur Main Camera hinzu und erstellt eine CameraConfig.",
                HasThirdPersonCamera(),
                () => CameraPackage.Editor.CameraSetupEditor.SetupThirdPersonCamera()
            );

            // Step 3: Assign Config
            DrawStep(
                "3. LocomotionConfig zuweisen",
                "Weise die LocomotionConfig dem Player im Inspector zu.",
                IsLocomotionConfigAssigned(),
                () => SelectPlayerAndPingConfig()
            );

            EditorGUILayout.Space(20);

            // Quick Actions
            EditorGUILayout.LabelField("Schnellaktionen", _headerStyle);

            EditorGUILayout.BeginVertical(_stepStyle);

            if (GUILayout.Button("Alle Schritte auf einmal ausführen", GUILayout.Height(35)))
            {
                RunAllSteps();
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Player auswählen"))
            {
                SelectPlayer();
            }
            if (GUILayout.Button("LocomotionConfig öffnen"))
            {
                OpenLocomotionConfig();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Prefabs erstellen"))
            {
                DemoSceneSetup.CreateCorePrefabs();
            }
            if (GUILayout.Button("Szene neu laden"))
            {
                RefreshScene();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(20);

            // Status
            EditorGUILayout.LabelField("Aktueller Status", _headerStyle);
            DrawStatusPanel();

            EditorGUILayout.EndScrollView();
        }

        private void DrawStep(string title, string description, bool isCompleted, System.Action action)
        {
            EditorGUILayout.BeginVertical(_stepStyle);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(
                isCompleted ? "✓ Erledigt" : "○ Ausstehend",
                isCompleted ? _completedStyle : _pendingStyle,
                GUILayout.Width(80)
            );
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(description, EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(5);

            using (new EditorGUI.DisabledGroupScope(isCompleted))
            {
                if (GUILayout.Button(isCompleted ? "Bereits erledigt" : "Ausführen"))
                {
                    action?.Invoke();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawStatusPanel()
        {
            EditorGUILayout.BeginVertical(_stepStyle);

            var player = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
            var camera = UnityEngine.Camera.main;
            var config = FindLocomotionConfig();

            DrawStatusRow("Player gefunden:", player != null, player?.name ?? "Nicht gefunden");
            DrawStatusRow("CapsuleCollider:", player?.GetComponent<CapsuleCollider>() != null);
            DrawStatusRow("PlayerController:", player?.GetComponent<PlayerController>() != null);
            DrawStatusRow("LocomotionConfig:", config != null, config?.name ?? "Nicht gefunden");
            DrawStatusRow("Config zugewiesen:", IsLocomotionConfigAssigned());
            DrawStatusRow("Main Camera:", camera != null);
            DrawStatusRow("ThirdPersonCamera:", HasThirdPersonCamera());

            EditorGUILayout.EndVertical();
        }

        private void DrawStatusRow(string label, bool status, string details = null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(150));
            EditorGUILayout.LabelField(
                status ? "✓" : "✗",
                status ? _completedStyle : _pendingStyle,
                GUILayout.Width(20)
            );
            if (!string.IsNullOrEmpty(details))
            {
                EditorGUILayout.LabelField(details, EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();
        }

        #region Status Checks

        private bool HasLocomotionConfig()
        {
            return FindLocomotionConfig() != null;
        }

        private bool HasThirdPersonCamera()
        {
            var camera = UnityEngine.Camera.main;
            return camera != null && camera.GetComponent<CameraPackage.ThirdPersonCamera>() != null;
        }

        private bool IsLocomotionConfigAssigned()
        {
            var player = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
            if (player == null) return false;

            var controller = player.GetComponent<PlayerController>();
            if (controller == null) return false;

            // Check via SerializedObject
            var so = new SerializedObject(controller);
            var configProp = so.FindProperty("_config");
            return configProp != null && configProp.objectReferenceValue != null;
        }

        private Locomotion.LocomotionConfig FindLocomotionConfig()
        {
            // Suche in bekannten Pfaden
            string[] paths = new[]
            {
                "Assets/Config/DefaultLocomotionConfig.asset",
                "Assets/Config/DefaultMovementConfig.asset", // Backwards compatibility
                "Assets/Settings/DefaultLocomotionConfig.asset",
                "Assets/Settings/DefaultMovementConfig.asset" // Backwards compatibility
            };

            foreach (var path in paths)
            {
                var config = AssetDatabase.LoadAssetAtPath<Locomotion.LocomotionConfig>(path);
                if (config != null) return config;
            }

            // Fallback: Suche alle LocomotionConfigs
            var guids = AssetDatabase.FindAssets("t:LocomotionConfig");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<Locomotion.LocomotionConfig>(path);
            }

            return null;
        }

        #endregion

        #region Actions

        private void RunAllSteps()
        {
            // Step 0: Create Demo Scene if no Player exists
            var player = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
            if (player == null)
            {
                DemoSceneSetup.CreateDemoScene();
            }

            // Step 1: Create LocomotionConfig if needed
            if (!HasLocomotionConfig())
            {
                DemoSceneSetup.CreateDefaultLocomotionConfig();
            }

            // Step 2: Setup Camera if needed
            if (!HasThirdPersonCamera())
            {
                CameraPackage.Editor.CameraSetupEditor.SetupThirdPersonCamera();
            }

            // Step 3: Assign Config
            AssignLocomotionConfigToPlayer();

            Debug.Log("[DemoSceneWizard] Alle Schritte abgeschlossen!");
            Repaint();
        }

        private void SelectPlayer()
        {
            var player = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
            if (player != null)
            {
                Selection.activeGameObject = player;
                EditorGUIUtility.PingObject(player);
            }
            else
            {
                EditorUtility.DisplayDialog("Player nicht gefunden",
                    "Kein Player in der Szene gefunden. Erstelle zuerst eine Demo-Szene.",
                    "OK");
            }
        }

        private void SelectPlayerAndPingConfig()
        {
            SelectPlayer();

            var config = FindLocomotionConfig();
            if (config != null)
            {
                EditorGUIUtility.PingObject(config);
                EditorUtility.DisplayDialog("LocomotionConfig zuweisen",
                    "1. Wähle den Player im Hierarchy\n" +
                    "2. Finde 'PlayerController' im Inspector\n" +
                    "3. Ziehe die LocomotionConfig in das Config-Feld\n\n" +
                    "Die Config wurde im Project-Fenster markiert.",
                    "OK");
            }
        }

        private void OpenLocomotionConfig()
        {
            var config = FindLocomotionConfig();
            if (config != null)
            {
                Selection.activeObject = config;
                EditorGUIUtility.PingObject(config);
            }
            else
            {
                if (EditorUtility.DisplayDialog("LocomotionConfig nicht gefunden",
                    "Keine LocomotionConfig gefunden. Soll eine erstellt werden?",
                    "Ja", "Nein"))
                {
                    DemoSceneSetup.CreateDefaultLocomotionConfig();
                }
            }
        }

        private void AssignLocomotionConfigToPlayer()
        {
            var player = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogWarning("[DemoSceneWizard] Kein Player gefunden.");
                return;
            }

            var controller = player.GetComponent<PlayerController>();
            if (controller == null)
            {
                Debug.LogWarning("[DemoSceneWizard] PlayerController nicht gefunden.");
                return;
            }

            var config = FindLocomotionConfig();
            if (config == null)
            {
                Debug.LogWarning("[DemoSceneWizard] LocomotionConfig nicht gefunden.");
                return;
            }

            // Assign via SerializedObject
            var so = new SerializedObject(controller);
            var configProp = so.FindProperty("_config");
            configProp.objectReferenceValue = config;
            so.ApplyModifiedProperties();

            Debug.Log($"[DemoSceneWizard] LocomotionConfig '{config.name}' dem Player zugewiesen.");
        }

        private void RefreshScene()
        {
            // Force repaint
            Repaint();
            SceneView.RepaintAll();
        }

        #endregion
    }
}
