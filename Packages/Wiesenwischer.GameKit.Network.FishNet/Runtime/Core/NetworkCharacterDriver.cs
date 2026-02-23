using FishNet.Object.Prediction;
using FishNet.Transporting;
using FishNet.Utility.Template;
using System.Collections.Generic;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core;
using Wiesenwischer.GameKit.CharacterController.Core.Motor;
using Wiesenwischer.GameKit.CharacterController.Core.Prediction;
using Wiesenwischer.GameKit.CharacterController.Core.Visual;

namespace Wiesenwischer.GameKit.Network
{
    /// <summary>
    /// Treibt die Character-Simulation ueber FishNet's TimeManager.OnTick.
    /// Ersetzt NetworkInputSync + NetworkStateSync durch native [Replicate]/[Reconcile].
    ///
    /// Tick-Flow:
    /// OnTick:     BuildInput → [Replicate](SimulateTick + Motor.Simulate)
    /// OnPostTick: CreateReconcile
    ///
    /// KCC-Interpolation ist deaktiviert (Settings.Interpolate = false).
    /// FishNet's NetworkTickSmoother handhabt Tick-Interpolation UND Reconcile-Smoothing.
    /// Kein custom Smoother noetig — FishNet subscribed auf OnPreTick, OnPostTick,
    /// OnUpdate und OnPostReplicateReplay automatisch.
    ///
    /// KRITISCH: _motor.Transform.SetPositionAndRotation() wird VOR jeder Simulation
    /// aufgerufen, damit der Motor immer von der Simulations-Position startet
    /// (nicht von der visuellen Position die der Smoother gesetzt hat).
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class NetworkCharacterDriver : TickNetworkBehaviour, ISimulationDriver
    {
        // --- Referenzen ---
        private PlayerController _player;
        private CharacterMotor _motor;
        private NetworkAnimationSync _animSync;
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

        // --- Diagnose ---
        [SerializeField] private bool _debugLog;

        #region Lifecycle

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();

            _player = GetComponent<PlayerController>();
            _motor = GetComponent<CharacterMotor>();
            _animSync = GetComponent<NetworkAnimationSync>();

            // Cache motor list (vermeidet GC-Alloc pro Tick)
            _motorList.Clear();
            _motorList.Add(_motor);

            // Motor-Simulation manuell steuern (kein FixedUpdate).
            // ACHTUNG: Beide Settings sind global — betrifft ALLE Motoren.
            // Korrekt fuer MMO (alle Spieler netzwerk-getrieben).
            CharacterMotorSystem.Settings.AutoSimulation = false;

            // KCC-Interpolation deaktivieren — FishNet's NetworkTickSmoother uebernimmt.
            // CustomInterpolationUpdate kaempft sonst gegen den Smoother (Doppel-Korrektur).
            CharacterMotorSystem.Settings.Interpolate = false;

            // GroundingSmoother deaktivieren — kaempft mit NetworkTickSmoother.
            // GroundingSmoother setzt localPosition auf dem Visual-Child, das nach
            // DetachOnStart kein Child mehr ist → localPosition = worldPosition → kaputt.
            // NetworkTickSmoother interpoliert Step-Ups bereits smooth.
            var groundingSmoother = GetComponent<GroundingSmoother>();
            if (groundingSmoother != null)
                groundingSmoother.enabled = false;

            if (_debugLog)
            {
                bool isDedicatedServer = IsServerStarted && !IsClientStarted;
                Debug.Log($"[Driver] OnStartNetwork: {gameObject.name} | " +
                    $"isOwner={base.Owner.IsLocalClient} isServer={base.IsServerStarted} " +
                    $"isDedicatedServer={isDedicatedServer} " +
                    $"motor={(_motor != null ? "OK" : "MISSING!")} " +
                    $"tickRate={TimeManager.TickRate}Hz " +
                    $"tickDelta={TimeManager.TickDelta:F4}s " +
                    $"AutoSim={CharacterMotorSystem.Settings.AutoSimulation} " +
                    $"KCC-Interp={CharacterMotorSystem.Settings.Interpolate}");
            }
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();

            // Zurueck zum Offline-Modus (KCC handhabt alles selbst)
            CharacterMotorSystem.Settings.AutoSimulation = true;
            CharacterMotorSystem.Settings.Interpolate = true;

            // GroundingSmoother reaktivieren (Offline-Modus)
            var groundingSmoother = GetComponent<GroundingSmoother>();
            if (groundingSmoother != null)
                groundingSmoother.enabled = true;
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
            BuildAndReplicate();
        }

        protected override void TimeManager_OnPostTick()
        {
            CreateReconcile();
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
            // --- Spectator Prediction (Non-Owner) ---
            // FishNet reconciled Non-Owner-Objekte ebenfalls via ReconcileToStates.
            // FishNet's NetworkTickSmoother handhabt Corrections visuell (OnPostReplicateReplay).
            if (!IsServerStarted && !IsOwner)
            {
                if (state.ContainsTicked())
                {
                    // Input fuer Future-Tick Prediction speichern
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
                // FishNet's NetworkTickSmoother schreibt die visuelle Position auf transform.
                // Ohne diesen Sync startet der Motor von der visuellen statt der Simulations-Position
                // → falsche Collision/Ground-Queries → Server korrigiert → ewiger Jitter.
                _motor.Transform.SetPositionAndRotation(_motor.TransientPosition, _motor.TransientRotation);

                CharacterMotorSystem.Simulate((float)TimeManager.TickDelta, _motorList);
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
