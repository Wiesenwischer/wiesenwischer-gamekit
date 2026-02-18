using UnityEngine;
using Wiesenwischer.GameKit.Camera;
using NetworkPlayer = Wiesenwischer.GameKit.Network.NetworkPlayer;

/// <summary>
/// Scene-Coordinator: Verbindet den lokal gespawnten Netzwerk-Spieler mit der Kamera.
/// Auf ein beliebiges GameObject in der Test-Szene legen (z.B. NetworkManager).
///
/// Stellt sicher dass CameraOrientationProvider auf dem CameraBrain-Root liegt.
/// Ohne Provider hat der PlayerController keinen IOrientationProvider/IFacingProvider
/// und faellt auf Camera-Forward zurueck (keine Orbit-Unterstuetzung).
/// </summary>
public class NetworkCameraSetup : MonoBehaviour
{
    [SerializeField] private CameraBrain _cameraBrain;

    private void OnEnable()
    {
        NetworkPlayer.OnLocalPlayerReady += OnLocalPlayerReady;
        NetworkPlayer.OnLocalPlayerRemoved += OnLocalPlayerRemoved;
    }

    private void OnDisable()
    {
        NetworkPlayer.OnLocalPlayerReady -= OnLocalPlayerReady;
        NetworkPlayer.OnLocalPlayerRemoved -= OnLocalPlayerRemoved;
    }

    private void OnLocalPlayerReady(Transform playerTransform)
    {
        if (_cameraBrain == null)
            _cameraBrain = FindObjectOfType<CameraBrain>();

        if (_cameraBrain == null)
        {
            Debug.LogWarning("[NetworkCameraSetup] Kein CameraBrain in der Szene gefunden.");
            return;
        }

        // CameraOrientationProvider sicherstellen (IOrientationProvider + IFacingProvider).
        // Ohne Provider nutzt PlayerController den Camera-Forward Fallback,
        // was Orbit-Modi und SteerOrbit-Facing bricht.
        if (_cameraBrain.GetComponent<CameraOrientationProvider>() == null)
        {
            _cameraBrain.gameObject.AddComponent<CameraOrientationProvider>();
            Debug.Log("[NetworkCameraSetup] CameraOrientationProvider automatisch hinzugefuegt.");
        }

        _cameraBrain.SetTarget(playerTransform);
        _cameraBrain.SnapBehindTarget();
        Debug.Log($"[NetworkCameraSetup] Kamera folgt jetzt: {playerTransform.name}");
    }

    private void OnLocalPlayerRemoved()
    {
        if (_cameraBrain != null)
            _cameraBrain.SetTarget(null);
    }
}
