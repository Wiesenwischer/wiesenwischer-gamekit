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
    /// Tick-Flow (KRITISCH — korrekte Reihenfolge):
    /// OnTick:     PreSimInterp → BuildInput → [Replicate](SimulateTick + Motor.Simulate)
    /// OnPostTick: CreateReconcile → PostSimInterp
    ///
    /// Pre/Post-Interpolation DARF NICHT in [Replicate] stehen, da [Replicate]
    /// auch waehrend Reconcile-Replay aufgerufen wird (mehrere Ticks hintereinander).
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

        // --- Interpolation Tick Guard ---
        // PreSimInterp/PostSimInterp sind GLOBALE Calls (betreffen alle Motoren).
        // Muessen genau 1x pro Tick aufgerufen werden, nicht pro Spieler.
        private static uint _lastPreInterpTick;
        private static uint _lastPostInterpTick;

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

            // Motor-Simulation manuell steuern (kein FixedUpdate)
            // ACHTUNG: AutoSimulation ist global — betrifft ALLE Motoren.
            // Korrekt fuer MMO (alle Spieler netzwerk-getrieben).
            CharacterMotorSystem.Settings.AutoSimulation = false;
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();

            // Zurueck zum Offline-Modus
            CharacterMotorSystem.Settings.AutoSimulation = true;
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
            // 1. Interpolation vorbereiten (NUR beim echten Tick, NICHT in Replay!)
            // WICHTIG: PreSimInterp ist ein GLOBALER Call — darf nur 1x pro Tick laufen,
            // nicht pro Spieler (sonst doppelter Interpolation-Update → Jitter).
            uint tick = TimeManager.Tick;
            if (_motor != null && _lastPreInterpTick != tick)
            {
                _lastPreInterpTick = tick;
                CharacterMotorSystem.PreSimulationInterpolationUpdate((float)TimeManager.TickDelta);
            }

            // 2. Input sammeln + Replicate aufrufen
            BuildAndReplicate();
        }

        protected override void TimeManager_OnPostTick()
        {
            // 3. Reconcile (nach Simulation, vor Interpolation-Abschluss)
            CreateReconcile();

            // 4. Interpolation abschliessen (NUR beim echten Tick, NICHT in Replay!)
            // Gleicher Guard wie oben — globaler Call, nur 1x pro Tick.
            uint tick = TimeManager.Tick;
            if (_motor != null && _lastPostInterpTick != tick)
            {
                _lastPostInterpTick = tick;
                CharacterMotorSystem.PostSimulationInterpolationUpdate((float)TimeManager.TickDelta);
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
            // Owner Reconcile Smoothing: Error nach Replay berechnen.
            // state.ContainsTicked() = erster nicht-replayed Tick → alle Replays sind durch.
            // TransientPosition enthaelt jetzt die korrigierte Prediction (Reconcile + Replays).
            // NICHT auf dem Server/Host: Server ist autoritaet, Reconcile-Error ist nur FP-Noise.
            if (_didReconcile && !IsServerStarted && state.ContainsTicked() && _smoother != null)
            {
                Vector3 correctedPos = _motor.TransientPosition;
                float correctedRot = _motor.TransientRotation.eulerAngles.y;

                Vector3 posError = _preReconcilePosition - correctedPos;
                float rotError = Mathf.DeltaAngle(correctedRot, _preReconcileRotation);

                if (posError.sqrMagnitude > _smoother.SnapThreshold * _smoother.SnapThreshold)
                    _smoother.ClearOffset();
                else
                    _smoother.SetCorrectionOffset(posError, rotError);

                // KRITISCH: InitialTickPosition korrigieren.
                // PreSimInterp hat InitialTickPosition VOR dem Reconcile gespeichert.
                // CustomInterpolationUpdate lerpt von InitialTickPosition → TransientPosition.
                // Ohne dieses Update lerpt die Interpolation ueber die volle Korrektur-Distanz
                // UND der Smoother addiert den Error nochmal → Doppel-Korrektur = Jitter.
                _motor.InitialTickPosition = correctedPos;
                _motor.InitialTickRotation = Quaternion.Euler(0f, correctedRot, 0f);

                _didReconcile = false;
            }

            // Spectator Prediction: letzten bekannten Input fuer Non-Owner verwenden
            if (!IsServerStarted && !IsOwner)
            {
                if (state.ContainsTicked())
                {
                    // Neuer autoritativer Input — Position VOR Anwendung speichern
                    _spectatorPreCorrectionPos = _motor.TransientPosition;
                    _spectatorNeedsCorrection = true;

                    _lastTickedReplicateData.Dispose();
                    _lastTickedReplicateData = input;
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
            bool isReplay = state.IsReplayed();
            if (_animSync != null)
                _animSync.SetReplayMode(isReplay);

            // Input auf Player setzen
            ApplyInputToPlayer(input);

            // Simulation ausfuehren (StateMachine + Locomotion)
            _player.SimulateTick((float)TimeManager.TickDelta);

            // LookDirection Override zuruecksetzen nach Simulation
            _player.SetLookDirectionOverride(null);

            // Motor simulieren (KCC UpdateVelocity/UpdateRotation Callbacks)
            if (_motor != null)
            {
                CharacterMotorSystem.Simulate((float)TimeManager.TickDelta, _motorList);
            }

            // Spectator Correction: Error berechnen nachdem Simulation mit neuem Input gelaufen ist
            if (_spectatorNeedsCorrection && state.ContainsTicked() && _smoother != null)
            {
                Vector3 postPos = _motor.TransientPosition;
                Vector3 error = _spectatorPreCorrectionPos - postPos;

                if (error.sqrMagnitude > _smoother.SnapThreshold * _smoother.SnapThreshold)
                    _smoother.ClearOffset();
                else
                    _smoother.SetCorrectionOffset(error, 0f);

                // Gleicher Fix wie bei Owner Reconcile: InitialTickPosition korrigieren,
                // damit Interpolation nicht gegen den Smoother kaempft.
                _motor.InitialTickPosition = postPos;
                _motor.InitialTickRotation = _motor.TransientRotation;

                _spectatorNeedsCorrection = false;
            }

            // Replay-Guard zuruecksetzen
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

            // StateMachine State wiederherstellen
            _player.RestoreMovementState(data.MovementStateIndex);
        }

        #endregion
    }
}
