using FishNet.Object.Prediction;
using FishNet.Transporting;
using FishNet.Utility.Template;
using System.Collections.Generic;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core;
using Wiesenwischer.GameKit.CharacterController.Core.Motor;
using Wiesenwischer.GameKit.CharacterController.Core.Prediction;

namespace Wiesenwischer.GameKit.Network
{
    /// <summary>
    /// Treibt die Character-Simulation ueber FishNet's TimeManager.OnTick.
    /// Ersetzt NetworkInputSync + NetworkStateSync durch native [Replicate]/[Reconcile].
    ///
    /// Tick-Flow (One-Tick-Behind):
    /// OnTick:     Smoother.OnPreTick (Buffer-Shift) → BuildInput → [Replicate](SimulateTick + Motor.Simulate)
    /// OnPostTick: CreateReconcile → Smoother.OnPostTick (speichert Pending fuer naechsten Tick)
    ///
    /// KCC-Interpolation ist deaktiviert (Settings.Interpolate = false).
    /// ReconcileSmoother handhabt Tick-Interpolation UND Reconcile-Smoothing in einem System.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class NetworkCharacterDriver : TickNetworkBehaviour, ISimulationDriver
    {
        // --- Referenzen ---
        private PlayerController _player;
        private CharacterMotor _motor;
        private NetworkAnimationSync _animSync;
        private ReconcileSmoother _smoother;
        private readonly List<CharacterMotor> _motorList = new(1);

        // --- ISimulationDriver ---
        public bool IsActive => IsSpawned;
        public float TickDelta => (float)TimeManager.TickDelta;
        public uint CurrentTick => TimeManager.Tick;

        // --- One-Shot Input Akkumulation ---
        private bool _jumpRequested;
        private bool _jumpCutRequested;
        private bool _resetVerticalRequested;
        private bool _lastJumpHeld;

        // --- Spectator Prediction ---
        [SerializeField] private int _spectatorMaxPredictTicks = 4;
        private MoveReplicateData _lastTickedReplicateData;
        private Vector3 _spectatorPreCorrectionPos;
        private bool _spectatorNeedsCorrection;

        // --- Reconcile Smoothing ---
        private bool _didReconcile;
        private Vector3 _preReconcilePosition;
        private float _preReconcileRotation;

        // --- Diagnose ---
        [SerializeField] private bool _debugLog;

        #region Lifecycle

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();

            _player = GetComponent<PlayerController>();
            _motor = GetComponent<CharacterMotor>();
            _animSync = GetComponent<NetworkAnimationSync>();
            _smoother = GetComponent<ReconcileSmoother>();

            // Cache motor list (vermeidet GC-Alloc pro Tick)
            _motorList.Clear();
            _motorList.Add(_motor);

            // Motor-Simulation manuell steuern (kein FixedUpdate).
            // ACHTUNG: Beide Settings sind global — betrifft ALLE Motoren.
            // Korrekt fuer MMO (alle Spieler netzwerk-getrieben).
            CharacterMotorSystem.Settings.AutoSimulation = false;

            // KCC-Interpolation deaktivieren — ReconcileSmoother uebernimmt.
            // CustomInterpolationUpdate kaempft sonst gegen den Smoother (Doppel-Korrektur).
            CharacterMotorSystem.Settings.Interpolate = false;

            if (_debugLog)
            {
                Debug.Log($"[Driver] OnStartNetwork: {gameObject.name} | " +
                    $"isOwner={base.Owner.IsLocalClient} isServer={base.IsServerStarted} " +
                    $"smoother={(_smoother != null ? "OK" : "MISSING!")} " +
                    $"motor={(_motor != null ? "OK" : "MISSING!")} " +
                    $"tickRate={TimeManager.TickRate}Hz " +
                    $"tickDelta={TimeManager.TickDelta:F4}s " +
                    $"AutoSim={CharacterMotorSystem.Settings.AutoSimulation} " +
                    $"KCC-Interp={CharacterMotorSystem.Settings.Interpolate}");

                if (_smoother == null)
                    Debug.LogError($"[Driver] ReconcileSmoother NICHT GEFUNDEN auf {gameObject.name}! " +
                        "Tick-Interpolation ist deaktiviert — Character wird mit Tick-Rate statt Frame-Rate gerendert!");
            }
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();

            // Smoother zuruecksetzen
            if (_smoother != null)
                _smoother.Reset();

            // Zurueck zum Offline-Modus (KCC handhabt alles selbst)
            CharacterMotorSystem.Settings.AutoSimulation = true;
            CharacterMotorSystem.Settings.Interpolate = true;
        }

        #endregion

        #region Input Accumulation

        private void Update()
        {
            if (!IsOwner) return;

            var input = _player.InputProvider;
            if (input == null) return;

            // One-Shot Inputs akkumulieren (gehen nicht verloren zwischen Ticks)
            if (input.JumpPressed) _jumpRequested = true;

            // JumpCut: Jump wurde gehalten und jetzt losgelassen
            if (_lastJumpHeld && !input.JumpHeld) _jumpCutRequested = true;
            _lastJumpHeld = input.JumpHeld;
        }

        #endregion

        #region Tick Flow

        protected override void TimeManager_OnTick()
        {
            // 1. Buffer-Shift + Interpolations-Timing (One-Tick-Behind)
            if (_smoother != null && _motor != null)
            {
                _smoother.OnPreTick(_motor.TransientPosition, _motor.TransientRotation,
                                    (float)TimeManager.TickDelta);

                if (_debugLog)
                    Debug.Log($"[Driver] OnTick: PreTick pos={_motor.TransientPosition:F3} transform={transform.position:F3} isOwner={IsOwner} isServer={IsServerStarted}");
            }

            // 2. Input sammeln + Replicate aufrufen
            BuildAndReplicate();
        }

        protected override void TimeManager_OnPostTick()
        {
            // 3. Reconcile-Daten erstellen und senden
            CreateReconcile();

            // 4. Interpolations-Endpunkt speichern (Motor-Position NACH Simulation)
            if (_smoother != null && _motor != null)
            {
                _smoother.OnPostTick(_motor.TransientPosition, _motor.TransientRotation,
                                     (float)TimeManager.TickDelta);

                if (_debugLog)
                    Debug.Log($"[Driver] OnPostTick: PostTick pos={_motor.TransientPosition:F3} transform={transform.position:F3}");
            }
        }

        #endregion

        #region Replicate

        private void BuildAndReplicate()
        {
            MoveReplicateData input = default;

            if (IsOwner)
            {
                input = BuildReplicateData();
                // One-Shot Flags zuruecksetzen nach Einlesen
                _jumpRequested = false;
                _jumpCutRequested = false;
                _resetVerticalRequested = false;
            }

            PerformReplicate(input);
        }

        private MoveReplicateData BuildReplicateData()
        {
            var reusable = _player.ReusableData;

            return new MoveReplicateData
            {
                MoveDirection = _player.InputProvider.MoveInput,
                CameraYaw = _player.CameraYaw,
                CharacterRotation = _player.transform.eulerAngles.y,
                Buttons = BuildControllerButtons(),
                SpeedModifier = reusable.MovementSpeedModifier,
                JumpRequested = _jumpRequested,
                JumpCutRequested = _jumpCutRequested,
                ResetVerticalRequested = _resetVerticalRequested,
            };
        }

        private ControllerButtons BuildControllerButtons()
        {
            var buttons = ControllerButtons.None;
            var input = _player.InputProvider;

            if (input.SprintHeld) buttons |= ControllerButtons.Sprint;
            if (input.CrouchTogglePressed) buttons |= ControllerButtons.Crouch;
            if (_player.ReusableData.ShouldWalk) buttons |= ControllerButtons.Walk;

            return buttons;
        }

        [Replicate]
        private void PerformReplicate(MoveReplicateData input, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
        {
            // --- Reconcile Correction (Owner + Non-Owner) ---
            // FishNet reconciled ALLE predicted Objects via ReconcileToStates.
            // state.ContainsTicked() = erster nicht-replayed Tick → alle Replays sind durch.
            // TransientPosition enthaelt jetzt die korrigierte Prediction (Reconcile + Replays).
            // NICHT auf dem Server/Host: Server ist autoritaet, Reconcile-Error ist nur FP-Noise.
            bool handledReconcile = false;
            if (_didReconcile && !IsServerStarted && state.ContainsTicked() && _smoother != null)
            {
                if (_debugLog)
                {
                    Vector3 error = _preReconcilePosition - _motor.TransientPosition;
                    Debug.Log($"[Driver] Reconcile: prePos={_preReconcilePosition:F3} correctedPos={_motor.TransientPosition:F3} error={error.magnitude:F4}m");
                }

                _smoother.OnReconcileComplete(
                    _preReconcilePosition, _preReconcileRotation,
                    _motor.TransientPosition, _motor.TransientRotation.eulerAngles.y);
                _didReconcile = false;
                handledReconcile = true;
            }

            // --- Spectator Prediction (Non-Owner) ---
            // FishNet reconciled Non-Owner-Objekte ebenfalls via ReconcileToStates.
            // Wenn Reconcile gerade gelaufen ist, KEINE zusaetzliche Spectator-Correction:
            //   OnReconcileComplete: offset += (preReconcile - postReplay) [korrekt]
            //   OnSpectatorCorrection: offset += (postReplay - postSim) = -movement [FALSCH!]
            //   → Jeder Tick addiert -movement zum Offset → persistenter visueller Lag bei Bewegung.
            // lastTickedReplicateData IMMER aktualisieren (fuer Future-Tick Prediction).
            if (!IsServerStarted && !IsOwner)
            {
                if (state.ContainsTicked())
                {
                    // Input fuer Future-Tick Prediction speichern (immer, auch nach Reconcile)
                    _lastTickedReplicateData.Dispose();
                    _lastTickedReplicateData = input;

                    // Spectator-Correction NUR wenn kein Reconcile diesen Tick gehandhabt hat
                    if (!handledReconcile)
                    {
                        _spectatorPreCorrectionPos = _motor.TransientPosition;
                        _spectatorNeedsCorrection = true;
                    }
                }
                else if (state.IsFuture())
                {
                    if (input.GetTick() - _lastTickedReplicateData.GetTick() > _spectatorMaxPredictTicks)
                        return;

                    input.Dispose();
                    input = _lastTickedReplicateData;
                    // One-Shot Events nicht predicten
                    input.JumpRequested = false;
                    input.JumpCutRequested = false;
                    input.ResetVerticalRequested = false;
                }
            }

            // Replay-Guard: Waehrend Reconcile-Replay keine Animations-RPCs senden
            // UND lokale Animator-Aufrufe unterdruecken (PlayState via CrossFade).
            bool isReplay = state.IsReplayed();
            if (_animSync != null)
                _animSync.SetReplayMode(isReplay);
            if (isReplay)
                _player.SuppressAnimationController();

            // Input auf Player setzen
            ApplyInputToPlayer(input);

            // Simulation ausfuehren (StateMachine + Locomotion)
            _player.SimulateTick((float)TimeManager.TickDelta);

            // LookDirection Override zuruecksetzen nach Simulation
            _player.SetLookDirectionOverride(null);

            // Motor simulieren (KCC UpdateVelocity/UpdateRotation Callbacks)
            if (_motor != null)
            {
                // KRITISCH: CharacterMotor.UpdatePhase1() liest _transientPosition = transform.position.
                // ReconcileSmoother schreibt die visuelle Position (interpoliert + Offset) auf transform.
                // Ohne diesen Sync startet der Motor von der visuellen statt der Simulations-Position
                // → falsche Collision/Ground-Queries → Server korrigiert → ewiger Jitter.
                _motor.Transform.SetPositionAndRotation(_motor.TransientPosition, _motor.TransientRotation);

                CharacterMotorSystem.Simulate((float)TimeManager.TickDelta, _motorList);
            }

            // Spectator Correction: Error berechnen nachdem Simulation mit neuem Input gelaufen ist
            if (_spectatorNeedsCorrection && state.ContainsTicked() && _smoother != null)
            {
                _smoother.OnSpectatorCorrection(
                    _spectatorPreCorrectionPos,
                    _motor.TransientPosition,
                    _motor.TransientRotation);
                _spectatorNeedsCorrection = false;
            }

            // Replay-Guard zuruecksetzen
            if (isReplay)
                _player.RestoreAnimationController();
            if (_animSync != null)
                _animSync.SetReplayMode(false);
        }

        private void ApplyInputToPlayer(MoveReplicateData input)
        {
            var reusable = _player.ReusableData;

            reusable.MoveInput = input.MoveDirection;
            reusable.MovementSpeedModifier = input.SpeedModifier;

            // CameraYaw → LookDirection Override
            // Server und Replay kennen die Client-Kamera nicht.
            Vector3 lookDir = Quaternion.Euler(0f, input.CameraYaw, 0f) * Vector3.forward;
            _player.SetLookDirectionOverride(lookDir);

            // One-Shot Events
            if (input.JumpRequested) reusable.JumpPressed = true;
            if (input.JumpCutRequested) reusable.JumpCutRequested = true;
            if (input.ResetVerticalRequested) reusable.ResetVerticalRequested = true;

            // Button-States
            reusable.SprintHeld = input.Buttons.HasFlag(ControllerButtons.Sprint);
            reusable.ShouldWalk = input.Buttons.HasFlag(ControllerButtons.Walk);
            reusable.CrouchTogglePressed = input.Buttons.HasFlag(ControllerButtons.Crouch);
        }

        #endregion

        #region Reconcile

        public override void CreateReconcile()
        {
            var reusable = _player.ReusableData;

            var data = new CharacterReconcileData
            {
                Position = _motor.TransientPosition,
                Rotation = _motor.TransientRotation.eulerAngles.y,
                Velocity = reusable.HorizontalVelocity,
                VerticalVelocity = reusable.VerticalVelocity,
                IsGrounded = _player.IsGrounded,
                IsCrouching = reusable.IsCrouching,
                ShouldWalk = reusable.ShouldWalk,
                MovementStateIndex = _player.CurrentMovementStateIndex,
            };

            PerformReconcile(data);
        }

        [Reconcile]
        private void PerformReconcile(CharacterReconcileData data, Channel channel = Channel.Unreliable)
        {
            // Pre-Reconcile Position speichern (fuer Correction-Offset Berechnung)
            _preReconcilePosition = _motor.TransientPosition;
            _preReconcileRotation = _motor.TransientRotation.eulerAngles.y;
            _didReconcile = true;

            // Position/Rotation auf Motor setzen (Physik-State)
            _motor.SetPositionAndRotation(
                data.Position,
                Quaternion.Euler(0f, data.Rotation, 0f)
            );

            // Character-State wiederherstellen
            var reusable = _player.ReusableData;
            reusable.HorizontalVelocity = data.Velocity;
            reusable.VerticalVelocity = data.VerticalVelocity;
            // IsGrounded wird vom Motor bei der naechsten Simulation recalculated
            reusable.IsCrouching = data.IsCrouching;
            reusable.ShouldWalk = data.ShouldWalk;

            // StateMachine State wiederherstellen.
            // AnimationController unterdruecken: RestoreState ruft Enter() auf,
            // was PlayState() triggert → Animator-CrossFade waehrend Reconcile ist unerwuenscht.
            _player.SuppressAnimationController();
            _player.RestoreMovementState(data.MovementStateIndex);
            _player.RestoreAnimationController();
        }

        #endregion
    }
}
