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

        public bool IsServer => _networkManager != null && _networkManager.IsServerStarted;
        public bool IsClient => _networkManager != null && _networkManager.ClientManager.Started;

        private void Awake()
        {
            _networkManager = GetComponent<NetworkManager>();
            if (_networkManager == null)
            {
                Debug.LogError("[GameNetworkManager] NetworkManager nicht gefunden!");
                return;
            }

            _networkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;
            _networkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
            _networkManager.SceneManager.OnClientLoadedStartScenes += OnClientLoadedScenes;
        }

        private void OnDestroy()
        {
            if (_networkManager != null)
            {
                _networkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
                _networkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
                _networkManager.SceneManager.OnClientLoadedStartScenes -= OnClientLoadedScenes;
            }
        }

        /// <summary>Startet als Host (Server + Client).</summary>
        public void StartHost()
        {
            _networkManager.ServerManager.StartConnection(_port);
            _networkManager.ClientManager.StartConnection(_address, _port);
        }

        /// <summary>Startet nur den Server.</summary>
        public void StartServer()
        {
            _networkManager.ServerManager.StartConnection(_port);
        }

        /// <summary>Verbindet als Client zu einem Server.</summary>
        public void StartClient(string address = null, ushort port = 0)
        {
            _networkManager.ClientManager.StartConnection(
                address ?? _address,
                port > 0 ? port : _port);
        }

        /// <summary>Stoppt Server und/oder Client.</summary>
        public void Stop()
        {
            if (_networkManager.IsServerStarted)
                _networkManager.ServerManager.StopConnection(true);
            if (_networkManager.ClientManager.Started)
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
