using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Wiesenwischer.GameKit.CharacterController.Core.Editor
{
    /// <summary>
    /// Editor-Tool zum Platzieren des Player Prefabs in der aktuell geöffneten Szene.
    /// Funktioniert mit jeder Szene — Playground, eigene Szene oder leere Szene.
    /// Menü: Wiesenwischer > GameKit > Core > Place Player in Scene
    /// </summary>
    public static class TestSceneCreator
    {
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";

        [MenuItem("Wiesenwischer/GameKit/Core/Place Player in Scene", false, 301)]
        public static void PlacePlayerInScene()
        {
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (playerPrefab == null)
            {
                Debug.LogError($"[TestScene] Player Prefab nicht gefunden: {PlayerPrefabPath}. " +
                               "Bitte zuerst über 'Animation > Character & Animation Wizard' erstellen.");
                return;
            }

            var activeScene = SceneManager.GetActiveScene();

            // Prüfen ob bereits ein Player in der Szene ist
            var existingPlayer = GameObject.FindObjectOfType<PlayerController>();
            if (existingPlayer != null)
            {
                if (!EditorUtility.DisplayDialog("Player bereits vorhanden",
                    $"In der Szene '{activeScene.name}' existiert bereits ein Player.\n\n" +
                    "Soll ein weiterer Player platziert werden?",
                    "Ja, weiteren platzieren", "Abbrechen"))
                {
                    return;
                }
            }

            // === Player Prefab in aktuelle Szene ===
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, activeScene);
            player.transform.position = new Vector3(0f, 1f, 0f);
            Undo.RegisterCreatedObjectUndo(player, "Place Player in Scene");

            // === Kamera positionieren (falls vorhanden) ===
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Undo.RecordObject(mainCamera.transform, "Position Camera for Player");
                mainCamera.transform.position = new Vector3(0f, 3f, -8f);
                mainCamera.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
            }

            // Szene als dirty markieren
            EditorSceneManager.MarkSceneDirty(activeScene);

            Selection.activeGameObject = player;

            Debug.Log($"[TestScene] Player in Szene '{activeScene.name}' platziert.");
            Debug.Log("[TestScene] Test-Checkliste:");
            Debug.Log("  1. Play Mode starten");
            Debug.Log("  2. WASD = Laufen, Shift = Sprint, Space = Jump");
            Debug.Log("  3. Von Plattformen fallen für Landing-Tests");
            Debug.Log("  4. Steile Rampen (60°) für Slope Sliding");
        }

        [MenuItem("Wiesenwischer/GameKit/Core/Place Player in Scene", true)]
        private static bool ValidatePlacePlayerInScene()
        {
            return !Application.isPlaying;
        }
    }
}
