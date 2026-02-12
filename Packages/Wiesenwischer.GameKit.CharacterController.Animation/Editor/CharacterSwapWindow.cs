using UnityEditor;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core;

namespace Wiesenwischer.GameKit.CharacterController.Animation.Editor
{
    /// <summary>
    /// EditorWindow zum Austauschen des Character-Modells im Player Prefab.
    /// Ersetzt das alte Modell, überträgt Animator-Controller und Komponenten,
    /// und triggert den IK-Wizard falls vorhanden.
    /// </summary>
    public class CharacterSwapWindow : EditorWindow
    {
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";

        private const string AnimatorControllerPath =
            "Packages/Wiesenwischer.GameKit.CharacterController.Animation/Resources/AnimatorControllers/CharacterAnimatorController.controller";

        private GameObject _newCharacterFBX;
        private bool _runIKSetup = true;
        private bool _adjustCapsule = true;
        private Vector2 _scrollPos;

        [MenuItem("Wiesenwischer/GameKit/Prefabs/Swap Character Model", false, 202)]
        private static void ShowWindow()
        {
            var window = GetWindow<CharacterSwapWindow>("Character Swap");
            window.minSize = new Vector2(350, 280);
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.LabelField("Character Model austauschen", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Zieht ein Humanoid-FBX hierher. Das Script ersetzt das alte Modell " +
                "im Player Prefab und richtet Animator + Komponenten neu ein.",
                MessageType.Info);

            EditorGUILayout.Space(8);

            // FBX Feld
            _newCharacterFBX = (GameObject)EditorGUILayout.ObjectField(
                "Neues Character FBX",
                _newCharacterFBX,
                typeof(GameObject),
                false);

            EditorGUILayout.Space(4);

            // Validierung anzeigen
            if (_newCharacterFBX != null)
            {
                string fbxPath = AssetDatabase.GetAssetPath(_newCharacterFBX);
                var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;

                if (importer == null)
                {
                    EditorGUILayout.HelpBox("Kein gültiges FBX/Model-Asset.", MessageType.Error);
                }
                else if (importer.animationType != ModelImporterAnimationType.Human)
                {
                    EditorGUILayout.HelpBox(
                        "Rig ist nicht Humanoid! Bitte im FBX-Import auf 'Humanoid' umstellen.\n" +
                        $"Aktuell: {importer.animationType}",
                        MessageType.Error);
                }
                else
                {
                    EditorGUILayout.HelpBox("Humanoid-Rig erkannt.", MessageType.None);
                }
            }

            EditorGUILayout.Space(8);

            // Optionen
            _runIKSetup = EditorGUILayout.Toggle("IK-Komponenten einrichten", _runIKSetup);
            _adjustCapsule = EditorGUILayout.Toggle("CapsuleCollider anpassen", _adjustCapsule);

            EditorGUILayout.Space(12);

            // Swap Button
            GUI.enabled = CanSwap();
            if (GUILayout.Button("Character austauschen", GUILayout.Height(32)))
            {
                PerformSwap();
            }
            GUI.enabled = true;

            EditorGUILayout.EndScrollView();
        }

        private bool CanSwap()
        {
            if (_newCharacterFBX == null) return false;

            string fbxPath = AssetDatabase.GetAssetPath(_newCharacterFBX);
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            return importer != null && importer.animationType == ModelImporterAnimationType.Human;
        }

        private void PerformSwap()
        {
            // Player Prefab laden
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[CharacterSwap] Player Prefab nicht gefunden: {PlayerPrefabPath}");
                return;
            }

            var animatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorControllerPath);
            if (animatorController == null)
            {
                Debug.LogError($"[CharacterSwap] Animator Controller nicht gefunden: {AnimatorControllerPath}");
                return;
            }

            // Prefab zum Bearbeiten öffnen
            var prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            var playerController = prefabRoot.GetComponent<PlayerController>();

            // === Altes Modell finden und entfernen ===
            var oldAnimator = prefabRoot.GetComponentInChildren<Animator>();
            if (oldAnimator != null)
            {
                string oldName = oldAnimator.gameObject.name;
                Object.DestroyImmediate(oldAnimator.gameObject);
                Debug.Log($"[CharacterSwap] Altes Modell entfernt: {oldName}");
            }

            // === Neues Modell einfügen ===
            var newModel = (GameObject)PrefabUtility.InstantiatePrefab(_newCharacterFBX);
            newModel.name = "CharacterModel";
            newModel.transform.SetParent(prefabRoot.transform);
            newModel.transform.localPosition = Vector3.zero;
            newModel.transform.localRotation = Quaternion.identity;
            newModel.transform.localScale = Vector3.one;

            // Animator konfigurieren
            var newAnimator = newModel.GetComponent<Animator>();
            if (newAnimator == null)
                newAnimator = newModel.AddComponent<Animator>();

            newAnimator.runtimeAnimatorController = animatorController;
            newAnimator.applyRootMotion = false;
            newAnimator.updateMode = AnimatorUpdateMode.Normal;
            newAnimator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            // AnimatorParameterBridge hinzufügen
            var bridge = newModel.AddComponent<AnimatorParameterBridge>();
            var bridgeSo = new SerializedObject(bridge);
            bridgeSo.FindProperty("_playerController").objectReferenceValue = playerController;
            bridgeSo.ApplyModifiedProperties();

            // CapsuleCollider an neue Mesh-Größe anpassen
            if (_adjustCapsule)
            {
                AdjustCapsule(prefabRoot, newModel);
            }

            // Prefab speichern
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            Debug.Log($"[CharacterSwap] Neues Modell eingesetzt: {_newCharacterFBX.name}");
            Debug.Log("[CharacterSwap] Animator Controller + AnimatorParameterBridge konfiguriert.");

            // IK Setup (separater Wizard, arbeitet auf dem gespeicherten Prefab)
            if (_runIKSetup)
            {
                // IKSetupWizard über MenuItems triggern (vermeidet Assembly-Abhängigkeit)
                if (EditorApplication.ExecuteMenuItem("Wiesenwischer/GameKit/Prefabs/Setup IK on Player Prefab"))
                {
                    Debug.Log("[CharacterSwap] IK-Komponenten eingerichtet.");
                }
                else
                {
                    Debug.LogWarning("[CharacterSwap] IK-Wizard nicht gefunden. " +
                                     "IK-Paket installiert? Manuell ausführen: " +
                                     "Wiesenwischer > GameKit > Prefabs > Setup IK on Player Prefab");
                }
            }

            // Prefab selektieren
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            EditorGUIUtility.PingObject(Selection.activeObject);

            Debug.Log("=== Character Swap abgeschlossen! ===");
        }

        /// <summary>
        /// Passt den CapsuleCollider an die Bounds des neuen Meshes an.
        /// </summary>
        private static void AdjustCapsule(GameObject prefabRoot, GameObject model)
        {
            // Bounds aus allen Renderern berechnen
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            // Höhe und Radius aus Bounds ableiten
            float height = bounds.size.y;
            float radius = Mathf.Max(bounds.extents.x, bounds.extents.z);

            // Capsule-Werte über SerializedObject setzen (private fields im Motor)
            var motor = prefabRoot.GetComponent<Core.Motor.CharacterMotor>();
            if (motor == null) return;

            var motorSo = new SerializedObject(motor);
            motorSo.FindProperty("CapsuleRadius").floatValue = Mathf.Clamp(radius, 0.15f, 0.5f);
            motorSo.FindProperty("CapsuleHeight").floatValue = height;
            motorSo.FindProperty("CapsuleYOffset").floatValue = height * 0.5f;
            motorSo.ApplyModifiedProperties();

            Debug.Log($"[CharacterSwap] CapsuleCollider angepasst: " +
                      $"Height={height:F2}m, Radius={radius:F2}m");
        }
    }
}
