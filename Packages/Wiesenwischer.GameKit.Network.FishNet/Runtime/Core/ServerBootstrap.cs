using System;
using System.Collections;
using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;

namespace Wiesenwischer.GameKit.Network
{
    /// <summary>
    /// Server Bootstrap: Startet automatisch im passenden Modus.
    /// - BatchMode / --server Flag: Dedicated Server
    /// - --client Flag: Kein Auto-Start (manuell via NetworkDebugUI)
    /// - Sonst: Auto-Host (lokaler Host fuer Singleplayer/Editor-Testing)
    /// Network-Only Architektur: Netzwerk ist immer aktiv.
    /// </summary>
    public class ServerBootstrap : MonoBehaviour
    {
        [Header("Defaults")]
        [SerializeField] private ushort _defaultPort = 7770;
        [SerializeField] private int _defaultMaxPlayers = 100;
        [SerializeField] private string _defaultBindAddress = "0.0.0.0";

        [Header("References (auto-resolved if empty)")]
        [SerializeField] private GameNetworkManager _gameNetworkManager;

        public GameNetworkManager GameNetworkManager => _gameNetworkManager;

        private void OnValidate()
        {
            if (_gameNetworkManager == null)
                _gameNetworkManager = GetComponent<GameNetworkManager>();
        }

        private void Start()
        {
            if (_gameNetworkManager == null)
            {
                LogError("Kein GameNetworkManager — kann nicht starten.");
                if (Application.isBatchMode)
                    Application.Quit(1);
                return;
            }

            // Graceful Shutdown: Unity ruft OnApplicationQuit bei SIGTERM (docker stop)
            Application.quitting += OnServerShutdown;

            if (Application.isBatchMode || HasArgument("--server"))
            {
                // Dedicated Server Modus
                Log("Dedicated Server Bootstrap gestartet");
                ConfigureTransport();
                StartCoroutine(StartDelayed(server: true));
            }
            else if (HasArgument("--client"))
            {
                // Expliziter Client-Modus: Kein Auto-Start.
                // User verbindet manuell via NetworkDebugUI (F6).
                Log("Client-Modus — kein Auto-Start");
            }
            else
            {
                // Auto-Host: Lokaler Host fuer Singleplayer/Editor-Testing.
                // Network-Only Architektur: Netzwerk ist immer aktiv.
                Log("Auto-Host Modus");
                StartCoroutine(StartDelayed(server: false));
            }
        }

        private IEnumerator StartDelayed(bool server)
        {
            // Ein Frame warten damit GameNetworkManager.Start() abgeschlossen ist
            yield return null;

            if (server)
            {
                _gameNetworkManager.StartServer();
                Log("Dedicated Server gestartet.");
            }
            else
            {
                _gameNetworkManager.StartHost();
                Log("Auto-Host gestartet (localhost).");
            }
        }

        private void ConfigureTransport()
        {
            var networkManager = GetComponent<NetworkManager>();
            if (networkManager == null)
            {
                LogError("FishNet NetworkManager nicht gefunden!");
                return;
            }

            var transport = networkManager.TransportManager.Transport;

            ushort port = GetArgumentValue("--port", _defaultPort);
            transport.SetPort(port);
            Log($"Port: {port}");

            int maxPlayers = GetArgumentValue("--max-players", _defaultMaxPlayers);
            transport.SetMaximumClients(maxPlayers);
            Log($"Max Players: {maxPlayers}");

            string bindAddress = GetArgumentString("--address", _defaultBindAddress);
            transport.SetServerBindAddress(bindAddress, IPAddressType.IPv4);
            Log($"Bind Address: {bindAddress}");
        }

        #region Command-Line & Environment Helpers

        private static bool HasArgument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static T GetArgumentValue<T>(string name, T defaultValue) where T : IConvertible
        {
            // CLI-Argumente haben Prioritaet
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    try { return (T)Convert.ChangeType(args[i + 1], typeof(T)); }
                    catch { return defaultValue; }
                }
            }

            // Fallback: Environment-Variable (fuer Docker)
            string envValue = GetEnvForArgument(name);
            if (!string.IsNullOrEmpty(envValue))
            {
                try { return (T)Convert.ChangeType(envValue, typeof(T)); }
                catch { return defaultValue; }
            }

            return defaultValue;
        }

        private static string GetArgumentString(string name, string defaultValue)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }

            string envValue = GetEnvForArgument(name);
            return !string.IsNullOrEmpty(envValue) ? envValue : defaultValue;
        }

        /// <summary>
        /// Mappt CLI-Argument-Namen auf Environment-Variablen.
        /// --port → PORT, --max-players → MAX_PLAYERS, --address → ADDRESS
        /// </summary>
        private static string GetEnvForArgument(string argumentName)
        {
            string envName = argumentName.TrimStart('-').Replace("-", "_").ToUpperInvariant();
            return Environment.GetEnvironmentVariable(envName);
        }

        #endregion

        private void OnServerShutdown()
        {
            Log("Server wird heruntergefahren...");
            if (_gameNetworkManager != null)
                _gameNetworkManager.Stop();
        }

        private static void Log(string message)
            => Debug.Log($"[ServerBootstrap] {message}");

        private static void LogError(string message)
            => Debug.LogError($"[ServerBootstrap] {message}");
    }
}
