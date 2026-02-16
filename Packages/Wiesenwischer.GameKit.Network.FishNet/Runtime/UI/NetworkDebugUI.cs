using UnityEngine;

namespace Wiesenwischer.GameKit.Network
{
    /// <summary>
    /// Minimales Debug-UI zum Starten von Host/Client/Server.
    /// Nur für Entwicklung — wird später durch richtiges UI ersetzt.
    /// </summary>
    public class NetworkDebugUI : MonoBehaviour
    {
        private GameNetworkManager _manager;

        private void OnGUI()
        {
            if (_manager == null)
                _manager = GetComponent<GameNetworkManager>();
            if (_manager == null)
                _manager = FindObjectOfType<GameNetworkManager>();

            GUILayout.BeginArea(new Rect(10, 10, 200, 200));

            if (_manager == null)
            {
                GUILayout.Label("GameNetworkManager nicht gefunden!");
                GUILayout.EndArea();
                return;
            }

            if (!_manager.IsServer && !_manager.IsClient)
            {
                if (GUILayout.Button("Host"))
                    _manager.StartHost();
                if (GUILayout.Button("Client"))
                    _manager.StartClient();
                if (GUILayout.Button("Server"))
                    _manager.StartServer();
            }
            else
            {
                GUILayout.Label($"Server: {_manager.IsServer}");
                GUILayout.Label($"Client: {_manager.IsClient}");
                if (GUILayout.Button("Stop"))
                    _manager.Stop();
            }

            GUILayout.EndArea();
        }
    }
}
