using UnityEngine;
using Wiesenwischer.GameKit.Camera;
using Wiesenwischer.GameKit.Network;

/// <summary>
/// Scene-Coordinator: Verbindet den lokal gespawnten Netzwerk-Spieler mit der Kamera.
/// Auf ein beliebiges GameObject in der Test-Szene legen (z.B. NetworkManager).
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
