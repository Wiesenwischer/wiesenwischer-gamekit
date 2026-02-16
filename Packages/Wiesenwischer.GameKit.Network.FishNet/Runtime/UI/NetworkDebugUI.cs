using UnityEngine;

namespace Wiesenwischer.GameKit.Network
{
    /// <summary>
    /// Minimales Debug-UI zum Starten von Host/Client/Server.
    /// Nur für Entwicklung — wird später durch richtiges UI ersetzt.
    /// Shortcuts: F5=Host, F6=Client, F7=Server, F8=Stop
    /// </summary>
    public class NetworkDebugUI : MonoBehaviour
    {
        private GameNetworkManager _manager;

        private void Update()
        {
            if (_manager == null)
                _manager = GetComponent<GameNetworkManager>() ?? FindObjectOfType<GameNetworkManager>();
            if (_manager == null) return;

            if (!_manager.IsServer && !_manager.IsClient)
            {
                if (Input.GetKeyDown(KeyCode.F5)) _manager.StartHost();
                if (Input.GetKeyDown(KeyCode.F6)) _manager.StartClient();
                if (Input.GetKeyDown(KeyCode.F7)) _manager.StartServer();
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.F8)) _manager.Stop();
            }
        }

        private void OnGUI()
        {
            if (_manager == null) return;

            GUILayout.BeginArea(new Rect(10, 10, 220, 200));

            if (!_manager.IsServer && !_manager.IsClient)
            {
                GUILayout.Label("Network [F5=Host F6=Client F7=Server]");
                GUILayout.Button("Host (F5)");
                GUILayout.Button("Client (F6)");
                GUILayout.Button("Server (F7)");
            }
            else
            {
                GUILayout.Label($"Server: {_manager.IsServer} | Client: {_manager.IsClient}");
                GUILayout.Label("Stop: F8");
            }

            GUILayout.EndArea();
        }
    }
}
