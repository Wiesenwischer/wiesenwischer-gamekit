using FishNet.Component.Transforming.Beta;
using UnityEngine;

namespace Wiesenwischer.GameKit.Network
{
    /// <summary>
    /// Debug-UI: Starten von Host/Client/Server + Diagnose-Overlay.
    /// Shortcuts: F5=Host, F6=Client, F7=Server, F8=Stop, F9=Toggle Diagnose
    /// </summary>
    public class NetworkDebugUI : MonoBehaviour
    {
        private GameNetworkManager _manager;
        private bool _showDiagnostics;

        // Diagnose-Cache (gesucht wenn noetig)
        private NetworkPlayer _localPlayer;
        private NetworkTickSmoother _smoother;
        private Transform _rootTransform;
        private Transform _visualTransform;

        // Jitter-Tracking
        private Vector3 _lastVisualPos;
        private float _maxFrameDelta;
        private float _maxFrameDeltaDecay;

        // Correction-Tracking (Richtungsumkehr = echte Korrektur, nicht normale Bewegung)
        private Vector3 _lastRootPos;
        private Vector3 _lastRootDelta;
        private int _correctionCount;
        private float _lastCorrectionMag;

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

            if (Input.GetKeyDown(KeyCode.F9))
                _showDiagnostics = !_showDiagnostics;

            if (_showDiagnostics)
                UpdateDiagnostics();
        }

        private void UpdateDiagnostics()
        {
            // Local Player suchen (lazy)
            if (_localPlayer == null)
            {
                foreach (var np in FindObjectsOfType<NetworkPlayer>())
                {
                    if (np.IsOwner)
                    {
                        _localPlayer = np;
                        _rootTransform = np.transform;

                        // VisualRoot ist VOR DetachOnStart gecached.
                        // Nach Detach ist GetComponentInChildren nicht mehr zuverlaessig.
                        _visualTransform = np.VisualRoot;
                        if (_visualTransform != null)
                            _smoother = _visualTransform.GetComponent<NetworkTickSmoother>();
                        break;
                    }
                }
            }

            // Frame-Delta tracken (Visual-Position)
            if (_visualTransform != null)
            {
                Vector3 currentVisual = _visualTransform.position;
                float frameDelta = (currentVisual - _lastVisualPos).magnitude;
                _lastVisualPos = currentVisual;

                // Max-Delta mit Decay (zeigt Peak-Jitter)
                if (frameDelta > _maxFrameDelta)
                    _maxFrameDelta = frameDelta;
                _maxFrameDeltaDecay += Time.deltaTime;
                if (_maxFrameDeltaDecay > 1f)
                {
                    _maxFrameDelta *= 0.5f;
                    _maxFrameDeltaDecay = 0f;
                }
            }

            // Correction-Tracking: Richtungsumkehr erkennen (echte Korrekturen)
            // Normale Bewegung hat konsistente Richtung. Reconcile-Korrekturen
            // verschieben die Position GEGEN die Bewegungsrichtung.
            if (_rootTransform != null)
            {
                Vector3 currentRoot = _rootTransform.position;
                Vector3 currentDelta = currentRoot - _lastRootPos;

                if (_lastRootDelta.sqrMagnitude > 0.0001f && currentDelta.sqrMagnitude > 0.0001f)
                {
                    // Dot < 0 = Richtungsumkehr = Korrektur
                    float dot = Vector3.Dot(_lastRootDelta.normalized, currentDelta.normalized);
                    if (dot < -0.3f)
                    {
                        _correctionCount++;
                        _lastCorrectionMag = currentDelta.magnitude;
                    }
                }

                _lastRootDelta = currentDelta;
                _lastRootPos = currentRoot;
            }
        }

        private void OnGUI()
        {
            if (_manager == null) return;

            if (!_manager.IsServer && !_manager.IsClient)
            {
                // Start-Buttons: links oben (nur sichtbar vor Verbindung)
                GUILayout.BeginArea(new Rect(10, 10, 250, 120));
                GUILayout.Label("Network [F5=Host F6=Client F7=Server]");
                if (GUILayout.Button("Host (F5)")) _manager.StartHost();
                if (GUILayout.Button("Client (F6)")) _manager.StartClient();
                if (GUILayout.Button("Server (F7)")) _manager.StartServer();
                GUILayout.EndArea();
            }
            else
            {
                // Status + Diagnose: rechts unten (kein Overlap mit PlayerController/CameraPreset UI)
                float panelWidth = 420f;
                float panelX = Screen.width - panelWidth - 10f;
                float diagHeight = _showDiagnostics ? 240f : 0f;
                float totalHeight = 24f + diagHeight;
                float panelY = Screen.height - totalHeight - 10f;

                GUILayout.BeginArea(new Rect(panelX, panelY, panelWidth, 24));
                string role = _manager.IsServer && _manager.IsClient ? "HOST" :
                    _manager.IsServer ? "SERVER" : "CLIENT";
                GUILayout.Label($"{role} | Stop: F8 | Diag: F9");
                GUILayout.EndArea();

                if (_showDiagnostics)
                    DrawDiagnostics(panelX, panelY + 26f);
            }
        }

        private void DrawDiagnostics(float panelX, float startY)
        {
            GUILayout.BeginArea(new Rect(panelX, startY, 420, 240));

            var style = new GUIStyle(GUI.skin.label) { fontSize = 13 };
            var bold = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };

            GUILayout.Label("=== NETWORK DIAGNOSTICS ===", bold);

            // 1. Smoother Status
            bool smootherFound = _smoother != null;
            bool smootherActive = smootherFound && _smoother.enabled && _smoother.SmootherController != null;
            string smootherStatus = !smootherFound ? "MISSING!" : !smootherActive ? "INACTIVE!" : "OK";
            GUI.color = smootherActive ? Color.green : Color.red;
            GUILayout.Label($"Smoother: {smootherStatus}", style);
            GUI.color = Color.white;

            // 2. Root vs Visual Position
            if (_rootTransform != null && _visualTransform != null)
            {
                Vector3 rootPos = _rootTransform.position;
                Vector3 visualPos = _visualTransform.position;
                Vector3 delta = visualPos - rootPos;

                GUILayout.Label($"Root:   ({rootPos.x:F2}, {rootPos.y:F2}, {rootPos.z:F2})", style);
                GUILayout.Label($"Visual: ({visualPos.x:F2}, {visualPos.y:F2}, {visualPos.z:F2})", style);

                GUI.color = delta.magnitude > 0.001f ? Color.yellow : Color.green;
                GUILayout.Label($"Delta:  {delta.magnitude:F4}m", style);
                GUI.color = Color.white;
            }
            else
            {
                GUILayout.Label(_localPlayer == null
                    ? "Kein lokaler Player"
                    : "Root/Visual fehlt", style);
            }

            // 3. Frame-Jitter
            GUI.color = _maxFrameDelta > 0.05f ? Color.red : _maxFrameDelta > 0.02f ? Color.yellow : Color.green;
            GUILayout.Label($"Frame-Delta Peak: {_maxFrameDelta:F4}m", style);
            GUI.color = Color.white;

            // 4. Corrections (Richtungsumkehr = Reconcile/Prediction-Error)
            GUI.color = _correctionCount > 10 ? Color.red : _correctionCount > 0 ? Color.yellow : Color.green;
            GUILayout.Label($"Corrections: {_correctionCount}  Last: {_lastCorrectionMag:F3}m  [Reset: F10]", style);
            GUI.color = Color.white;

            // 5. Visual detached
            bool detached = _visualTransform != null && _visualTransform.parent == null;
            GUI.color = detached ? Color.green : Color.red;
            GUILayout.Label($"Detached: {detached}", style);
            GUI.color = Color.white;

            // 6. FPS
            float fps = 1f / Time.unscaledDeltaTime;
            GUILayout.Label($"FPS: {fps:F0}", style);

            GUILayout.EndArea();

            // F10 fuer Reset (im OnGUI statt Update, da Buttons entfernt)
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.F10)
            {
                _correctionCount = 0;
                _lastCorrectionMag = 0f;
                _maxFrameDelta = 0f;
            }
        }
    }
}
