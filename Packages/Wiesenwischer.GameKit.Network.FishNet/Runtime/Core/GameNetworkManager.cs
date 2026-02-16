using FishNet.Managing;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

namespace Wiesenwischer.GameKit.Network
{
    /// <summary>
    /// GameKit-Wrapper für FishNet NetworkManager.
    /// Handhabt Server/Client/Host Lifecycle und Player Spawning.
    /// </summary>
    public class GameNetworkManager : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private NetworkObject _playerPrefab;

        [Header("Settings")]
        [SerializeField] private ushort _port = 7770;
        [SerializeField] private string _address = "localhost";

        private NetworkManager _networkManager;
        private bool _initialized;

        public bool IsServer => _networkManager != null && _networkManager.IsServerStarted;
        public bool IsClient => _networkManager != null && _networkManager.ClientManager != null && _networkManager.ClientManager.Started;

        private void Start()
        {
            _networkManager = GetComponent<NetworkManager>();
            if (_networkManager == null)
            {
                Debug.LogError("[GameNetworkManager] NetworkManager nicht gefunden!");
                return;
            }

            if (_networkManager.ServerManager == null || _networkManager.ClientManager == null)
            {
                Debug.LogError("[GameNetworkManager] FishNet nicht initialisiert — ServerManager/ClientManager ist null.");
                return;
            }

            _networkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;
            _networkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
            _networkManager.SceneManager.OnClientLoadedStartScenes += OnClientLoadedScenes;
            _initialized = true;

            Debug.Log("[GameNetworkManager] Initialisiert.");
        }

        private void OnDestroy()
        {
            if (_networkManager != null && _initialized)
            {
                _networkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
                _networkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
                _networkManager.SceneManager.OnClientLoadedStartScenes -= OnClientLoadedScenes;
            }
        }

        /// <summary>Startet als Host (Server + Client).</summary>
        public void StartHost()
        {
            if (!_initialized)
            {
                Debug.LogError("[GameNetworkManager] Nicht initialisiert — kann Host nicht starten.");
                return;
            }
            Debug.Log($"[GameNetworkManager] Starte Host auf Port {_port}...");
            _networkManager.ServerManager.StartConnection(_port);
            _networkManager.ClientManager.StartConnection(_address, _port);
        }

        /// <summary>Startet nur den Server.</summary>
        public void StartServer()
        {
            if (!_initialized)
            {
                Debug.LogError("[GameNetworkManager] Nicht initialisiert — kann Server nicht starten.");
                return;
            }
            Debug.Log($"[GameNetworkManager] Starte Server auf Port {_port}...");
            _networkManager.ServerManager.StartConnection(_port);
        }

        /// <summary>Verbindet als Client zu einem Server.</summary>
        public void StartClient(string address = null, ushort port = 0)
        {
            if (!_initialized)
            {
                Debug.LogError("[GameNetworkManager] Nicht initialisiert — kann Client nicht starten.");
                return;
            }
            var targetAddress = address ?? _address;
            var targetPort = port > 0 ? port : _port;
            Debug.Log($"[GameNetworkManager] Verbinde als Client zu {targetAddress}:{targetPort}...");
            _networkManager.ClientManager.StartConnection(targetAddress, targetPort);
        }

        /// <summary>Stoppt Server und/oder Client.</summary>
        public void Stop()
        {
            if (_networkManager == null) return;
            if (_networkManager.IsServerStarted)
                _networkManager.ServerManager.StopConnection(true);
            if (_networkManager.ClientManager != null && _networkManager.ClientManager.Started)
                _networkManager.ClientManager.StopConnection();
        }

        private void OnServerConnectionState(ServerConnectionStateArgs args)
        {
            Debug.Log($"[GameNetworkManager] Server: {args.ConnectionState}");
        }

        private void OnClientConnectionState(ClientConnectionStateArgs args)
        {
            Debug.Log($"[GameNetworkManager] Client: {args.ConnectionState}");
        }

        private void OnClientLoadedScenes(
            FishNet.Connection.NetworkConnection conn, bool asServer)
        {
            if (!asServer) return;
            if (_playerPrefab == null) return;

            var player = Instantiate(_playerPrefab);
            _networkManager.ServerManager.Spawn(player, conn);
            Debug.Log($"[GameNetworkManager] Player gespawnt für Connection {conn.ClientId}");
        }
    }
}
