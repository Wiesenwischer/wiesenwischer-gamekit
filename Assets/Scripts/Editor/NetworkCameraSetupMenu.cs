using FishNet.Managing;
using UnityEditor;
using UnityEngine;
using Wiesenwischer.GameKit.Camera;

/// <summary>
/// Menüeintrag zum Einrichten der Netzwerk-Kamera-Bridge.
/// Fügt NetworkCameraSetup auf dem NetworkManager-GameObject hinzu
/// und verknüpft den CameraBrain automatisch.
///
/// Game-Level Editor Script — nicht Teil des GameKit.
/// </summary>
public static class NetworkCameraSetupMenu
{
    [MenuItem("Wiesenwischer/Game/Setup Network Camera Bridge", false, 200)]
    public static void SetupNetworkCameraBridge()
    {
        var networkManager = Object.FindObjectOfType<NetworkManager>();
        if (networkManager == null)
        {
            EditorUtility.DisplayDialog(
                "Network Camera Bridge",
                "Kein NetworkManager in der Szene gefunden.\n\n" +
                "Bitte zuerst den Network Setup Wizard ausführen:\n" +
                "Wiesenwischer → GameKit → Network → Network Setup Wizard",
                "OK");
            return;
        }

        var go = networkManager.gameObject;
        var existing = go.GetComponent<NetworkCameraSetup>();

        if (existing != null)
        {
            EditorUtility.DisplayDialog(
                "Network Camera Bridge",
                "NetworkCameraSetup ist bereits vorhanden.",
                "OK");
            Selection.activeGameObject = go;
            return;
        }

        Undo.AddComponent<NetworkCameraSetup>(go);

        // CameraBrain automatisch zuweisen falls in der Szene vorhanden
        var cameraBrain = Object.FindObjectOfType<CameraBrain>();
        if (cameraBrain != null)
        {
            var setup = go.GetComponent<NetworkCameraSetup>();
            var so = new SerializedObject(setup);
            var prop = so.FindProperty("_cameraBrain");
            if (prop != null)
            {
                prop.objectReferenceValue = cameraBrain;
                so.ApplyModifiedProperties();
            }

            // CameraOrientationProvider sicherstellen (IOrientationProvider + IFacingProvider).
            // Ohne Provider nutzt PlayerController den Camera-Forward Fallback,
            // was Orbit-Modi und SteerOrbit-Facing bricht.
            if (cameraBrain.GetComponent<CameraOrientationProvider>() == null)
            {
                Undo.AddComponent<CameraOrientationProvider>(cameraBrain.gameObject);
                Debug.Log("[NetworkCameraSetupMenu] CameraOrientationProvider auf CameraBrain hinzugefuegt.");
            }
        }

        Selection.activeGameObject = go;
        Debug.Log("[NetworkCameraSetupMenu] NetworkCameraSetup hinzugefügt" +
                  (cameraBrain != null ? $" — CameraBrain verknüpft: {cameraBrain.name}" : " — kein CameraBrain in Szene gefunden"));
    }

    [MenuItem("Wiesenwischer/Game/Setup Network Camera Bridge", true)]
    public static bool ValidateSetupNetworkCameraBridge()
    {
        return Application.isPlaying == false;
    }
}
