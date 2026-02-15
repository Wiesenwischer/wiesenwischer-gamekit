using System.Collections.Generic;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core;

namespace Wiesenwischer.GameKit.Camera
{
    /// <summary>
    /// Zentraler Camera-Orchestrator. Koordiniert Input, Anchor, Intents, Behaviours und PivotRig.
    /// Enthält keine eigene Kamera-Logik — alles läuft über ICameraIntent[] und ICameraBehaviour[].
    /// Implementiert ICameraOrbitProvider für Character Controller Integration.
    /// </summary>
    [RequireComponent(typeof(PivotRig))]
    [DefaultExecutionOrder(100)] // Nach Animator und Character-Scripts ausführen
    public class CameraBrain : MonoBehaviour, ICameraOrbitProvider
    {
        [Header("References")]
        [SerializeField] private CameraAnchor _anchor;
        [SerializeField] private CameraInputPipeline _inputPipeline;
        [SerializeField] private UnityEngine.Camera _camera;

        [Header("Config")]
        [SerializeField] private CameraCoreConfig _config;

        [Header("Preset")]
        [Tooltip("Aktives Camera-Preset (optional)")]
        [SerializeField] private CameraPreset _activePreset;

        private PivotRig _rig;
        private CameraState _state;
        private CameraContext _context;
        private ICameraBehaviour[] _behaviours;
        private readonly List<ICameraIntent> _intents = new List<ICameraIntent>();

        /// <summary>Aktueller Camera State (readonly).</summary>
        public CameraState State => _state;

        /// <summary>True wenn der aktuelle Frame im Steer-Modus ist (Character soll zur Kamera rotieren).</summary>
        public bool IsSteerMode => _context != null && _context.IsSteerMode;

        /// <summary>Camera Forward in Welt-Space (Y=0, normalisiert).</summary>
        public Vector3 Forward
        {
            get
            {
                var fwd = _rig.GetCameraForward();
                fwd.y = 0f;
                return fwd.sqrMagnitude > 0.001f ? fwd.normalized : transform.forward;
            }
        }

        /// <summary>Setzt das Follow-Target (Character Root).</summary>
        public void SetTarget(Transform followTarget, Transform lookTarget = null)
        {
            if (_anchor != null)
                _anchor.FollowTarget = followTarget;

            if (_context != null)
            {
                _context.FollowTarget = followTarget;
                _context.LookTarget = lookTarget;
            }
        }

        /// <summary>Registriert einen Intent. Wird bis zum Entfernen jeden Frame angewendet.</summary>
        public void PushIntent(ICameraIntent intent)
        {
            if (!_intents.Contains(intent))
            {
                _intents.Add(intent);
                _intents.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            }
        }

        /// <summary>Entfernt einen zuvor registrierten Intent.</summary>
        public void RemoveIntent(ICameraIntent intent)
        {
            _intents.Remove(intent);
        }

        /// <summary>Entfernt alle aktiven Intents.</summary>
        public void ClearIntents()
        {
            _intents.Clear();
        }

        /// <summary>Wendet ein Preset auf alle ICameraPresetReceiver-Behaviours an.</summary>
        public void SetPreset(CameraPreset preset)
        {
            _activePreset = preset;
            if (preset == null) return;

            _state.Fov = preset.DefaultFov;

            // OrbitActivation an InputPipeline weiterreichen
            if (_inputPipeline != null)
                _inputPipeline.OrbitActivationMode = preset.OrbitActivation;

            if (_behaviours != null)
            {
                foreach (var behaviour in _behaviours)
                {
                    if (behaviour is ICameraPresetReceiver receiver)
                        receiver.ApplyPreset(preset);
                }
            }
        }

        /// <summary>Teleportiert Kamera sofort hinter das Target.</summary>
        public void SnapBehindTarget()
        {
            if (_anchor != null && _anchor.FollowTarget != null)
            {
                _anchor.SnapToTarget();
                _state.Yaw = _anchor.FollowTarget.eulerAngles.y;
                _state.Pitch = 0f;

                // Behaviours mit InitializeState aufrufen (z.B. ZoomBehaviour → Distance)
                if (_behaviours != null)
                {
                    foreach (var b in _behaviours)
                    {
                        if (b is ICameraStateInitializer init)
                            init.InitializeState(ref _state);
                        if (b is ICameraSnappable snap)
                            snap.Snap(_anchor.AnchorPosition);
                    }
                }

                _rig.ApplyState(_state, _anchor.AnchorPosition);
            }
        }

        /// <summary>Behaviour-Liste neu einlesen (z.B. nach AddComponent).</summary>
        public void RefreshBehaviours()
        {
            _behaviours = GetComponents<ICameraBehaviour>();
        }

        private void Awake()
        {
            _rig = GetComponent<PivotRig>();
            _rig.EnsureHierarchy();

            _context = new CameraContext();
            _state = CameraState.Default;

            if (_config != null)
                _state.Fov = _config.DefaultFov;

            // Auto-discover Behaviours auf diesem GameObject
            _behaviours = GetComponents<ICameraBehaviour>();

            // Behaviours mit InitializeState aufrufen
            foreach (var b in _behaviours)
            {
                if (b is ICameraStateInitializer init)
                    init.InitializeState(ref _state);
            }

            // Initiales Preset anwenden (überschreibt Config und Behaviour-Defaults)
            if (_activePreset != null)
                SetPreset(_activePreset);

            // Camera IMMER über PivotRig auflösen (serialisierte Referenz kann auf alte Root-Camera zeigen)
            if (_rig.CameraTransform != null)
            {
                var rigCam = _rig.CameraTransform.GetComponent<UnityEngine.Camera>();
                if (rigCam != null)
                    _camera = rigCam;
            }
            if (_camera == null)
                _camera = GetComponentInChildren<UnityEngine.Camera>();
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // 1. Input
            CameraInputState input = default;
            if (_inputPipeline != null)
                input = _inputPipeline.ProcessInput(dt);

            // 2. Anchor
            if (_anchor != null)
                _anchor.UpdateAnchor(dt);

            // 3. Context
            _context.Input = input;
            _context.AnchorPosition = _anchor != null ? _anchor.AnchorPosition : transform.position;
            _context.FollowTarget = _anchor != null ? _anchor.FollowTarget : null;
            _context.DeltaTime = dt;
            _context.IsSteerMode = input.OrbitMode == CameraOrbitMode.SteerOrbit;
            PopulateCharacterContext();

            // 4. Intents anwenden (nach Priorität, aufsteigend)
            for (int i = 0; i < _intents.Count; i++)
            {
                if (_intents[i].IsActive)
                    _intents[i].Apply(ref _state, _context);
            }

            // 5. Behaviours evaluieren
            foreach (var behaviour in _behaviours)
            {
                if (behaviour.IsActive)
                    behaviour.UpdateState(ref _state, _context);
            }

            // 6. Apply
            _rig.ApplyState(_state, _context.AnchorPosition);

            // 7. FOV
            if (_camera != null)
                _camera.fieldOfView = _state.Fov;
        }

        private void PopulateCharacterContext()
        {
            if (_anchor == null || _anchor.FollowTarget == null)
            {
                _context.CharacterVelocity = Vector3.zero;
                _context.CharacterForward = Vector3.forward;
                return;
            }

            Transform target = _anchor.FollowTarget;
            _context.CharacterForward = target.forward;

            var cc = target.GetComponent<UnityEngine.CharacterController>();
            if (cc != null)
            {
                _context.CharacterVelocity = cc.velocity;
            }
            else
            {
                var rb = target.GetComponent<Rigidbody>();
                _context.CharacterVelocity = rb != null ? rb.velocity : Vector3.zero;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_anchor == null || !Application.isPlaying) return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_anchor.AnchorPosition, 0.1f);
        }
#endif
    }
}
