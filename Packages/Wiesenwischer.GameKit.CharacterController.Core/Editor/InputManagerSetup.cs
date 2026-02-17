using UnityEditor;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Input;

namespace Wiesenwischer.GameKit.CharacterController.Core.Editor
{
    /// <summary>
    /// Editor-Tool: Erstellt ein InputManager-GameObject mit PlayerInputProvider in der Scene.
    /// Re-run-sicher — findet vorhandenen InputManager und selektiert ihn.
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
            go.AddComponent<PlayerInputProvider>();

            Undo.RegisterCreatedObjectUndo(go, "Create InputManager");
            Selection.activeGameObject = go;

            Debug.Log("[InputManagerSetup] InputManager erstellt. InputActionAsset im Inspector zuweisen.");
        }

        [MenuItem("Wiesenwischer/GameKit/Core/Create InputManager", true)]
        private static bool Validate()
        {
            return !Application.isPlaying;
        }
    }
}
