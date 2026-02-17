using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using Wiesenwischer.GameKit.CharacterController.Core.Input;

namespace Wiesenwischer.GameKit.CharacterController.Core.Editor
{
    /// <summary>
    /// Editor-Tool: Erstellt ein InputManager-GameObject mit PlayerInputProvider in der Scene.
    /// Re-run-sicher — findet vorhandenen InputManager und selektiert ihn.
    /// Weist automatisch das InputActionAsset zu (sucht nach *.inputactions in Assets/).
    /// </summary>
    public static class InputManagerSetup
    {
        private const string GameObjectName = "InputManager";

        [MenuItem("Wiesenwischer/GameKit/Core/Create InputManager", false, 310)]
        public static void CreateInputManager()
        {
            // Re-run-sicher: Pruefen ob bereits vorhanden
            var existing = Object.FindObjectOfType<PlayerInputProvider>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                Debug.Log($"[InputManagerSetup] InputManager existiert bereits: '{existing.gameObject.name}'");
                return;
            }

            var go = new GameObject(GameObjectName);
            var provider = go.AddComponent<PlayerInputProvider>();

            // InputActionAsset automatisch zuweisen
            var asset = FindInputActionAsset();
            if (asset != null)
            {
                var so = new SerializedObject(provider);
                so.FindProperty("_inputActions").objectReferenceValue = asset;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning("[InputManagerSetup] Kein InputActionAsset in Assets/ gefunden. Bitte manuell zuweisen.");
            }

            Undo.RegisterCreatedObjectUndo(go, "Create InputManager");
            Selection.activeGameObject = go;

            Debug.Log($"[InputManagerSetup] InputManager erstellt. Asset: {(asset != null ? asset.name : "KEINS")}");
        }

        private static InputActionAsset FindInputActionAsset()
        {
            // Suche nach *.inputactions in Assets/ (nicht in Library/Packages)
            var guids = AssetDatabase.FindAssets("t:InputActionAsset", new[] { "Assets" });
            if (guids.Length == 0) return null;

            // Erstes gefundenes Asset verwenden
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
        }

        [MenuItem("Wiesenwischer/GameKit/Core/Create InputManager", true)]
        private static bool Validate()
        {
            return !Application.isPlaying;
        }
    }
}
