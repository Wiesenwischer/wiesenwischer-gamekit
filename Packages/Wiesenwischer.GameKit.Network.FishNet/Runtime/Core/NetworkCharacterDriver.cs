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
        // KRITISCH: InputProvider-Properties sind consume-on-read (JumpPressed, CrouchTogglePressed, etc.).
        // Wir akkumulieren in Update() damit kein Input zwischen Ticks verloren geht.
        private bool _jumpRequested;
        private bool _jumpCutRequested;
        private bool _resetVerticalRequested;
        private bool _lastJumpHeld;
        private bool _crouchToggleRequested;
        private bool _walkToggleRequested;

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

            // ALLE One-Shot Inputs akkumulieren (gehen nicht verloren zwischen Ticks).
            // PlayerController.Update() ist deaktiviert wenn der Driver aktiv ist,
            // daher ist der Driver der einzige Consumer der consume-on-read Properties.
            if (input.JumpPressed) _jumpRequested = true;
            if (input.CrouchTogglePressed) _crouchToggleRequested = true;
            if (input.WalkTogglePressed) _walkToggleRequested = true;

            // JumpCut: Jump wurde gehalten und jetzt losgelassen
            if (_lastJumpHeld && !input.JumpHeld)
            {
                _jumpCutRequested = true;
                if (_debugLog)
                    Debug.Log($"[Driver] JumpCut detected in Update | jumpReq={_jumpRequested} state={_player.CurrentStateName}");
            }
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

                // Same-Tick Guard: Jump und JumpCut im gleichen Tick = JumpCut unterdruecken.
                // Passiert wenn Button-Press und Release in derselben Tick-Periode akkumulieren.
                if (input.JumpRequested && input.JumpCutRequested)
                    input.JumpCutRequested = false;

                // One-Shot Flags zuruecksetzen nach Einlesen
                _jumpRequested = false;
                _jumpCutRequested = false;
                _resetVerticalRequested = false;
                _crouchToggleRequested = false;
                _walkToggleRequested = false;
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
            if (input.JumpHeld) buttons |= ControllerButtons.Jump;

            // Akkumulierte One-Shot Flags (consume-on-read bereits in Update() verarbeitet)
            if (_crouchToggleRequested) buttons |= ControllerButtons.Crouch;

            // WalkToggle: Verarbeiten wie PlayerController.UpdateInput() es tut
            if (_walkToggleRequested)
                _player.ReusableData.ShouldWalk = !_player.ReusableData.ShouldWalk;

            // Sprint deaktiviert Walk automatisch
            if (input.SprintHeld && _player.ReusableData.ShouldWalk)
                _player.ReusableData.ShouldWalk = false;

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

            // Diagnose: Zustand nach Simulation loggen (nur aktueller Tick, nicht Replay)
            if (_debugLog && !isReplay && state.ContainsTicked())
            {
                Debug.Log($"[Driver] Tick: state={_player.CurrentStateName} " +
                    $"grounded={_player.IsGrounded} overEdge={_player.Locomotion.IsOverEdge} " +
                    $"motorStable={_motor.GroundingStatus.IsStableOnGround} " +
                    $"jump={input.JumpRequested} pos.y={_motor.TransientPosition.y:F3}");
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

            // One-Shot Events: MUSS unconditional gesetzt werden (= statt if+set).
            // Ohne explizites false bleibt JumpPressed nach dem ersten Jump ewig true,
            // weil UpdateInput() im Netzwerk-Modus nicht laeuft und es nie zuruecksetzt.
            reusable.JumpPressed = input.JumpRequested;

            // JumpCutRequested und ResetVerticalRequested werden NICHT aus Replikationsdaten gesetzt.
            // Die StateMachine (JumpingState.OnHandleInput) setzt JumpCut selbst basierend auf
            // !JumpHeld mit Guards (_jumpImpulseConfirmed, VerticalVelocity > 0).
            // Direktes Setzen aus dem Driver umgeht diese Guards → vorzeitiger JumpCut
            // wenn Jump-Press und JumpHeld-Release im gleichen Tick-Intervall akkumulieren.
            // ResetVerticalRequested wird nur von JumpingState bei Ceiling-Collision gesetzt.
            reusable.JumpCutRequested = false;
            reusable.ResetVerticalRequested = false;

            // Button-States (kontinuierlich)
            reusable.SprintHeld = input.Buttons.HasFlag(ControllerButtons.Sprint);
            reusable.ShouldWalk = input.Buttons.HasFlag(ControllerButtons.Walk);
            reusable.CrouchTogglePressed = input.Buttons.HasFlag(ControllerButtons.Crouch);

            // JumpHeld: Noetig fuer JumpWasReleased-Tracking in AirborneState.
            // Ohne JumpHeld ist JumpWasReleased immer true → CanJump() immer true → Endlos-Jump.
            reusable.JumpHeld = input.Buttons.HasFlag(ControllerButtons.Jump);
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

            // KRITISCH: Motor-Grounding-Cache aus Reconcile-Daten setzen.
            // Ohne diesen Fix hat der Motor nach Position-Restore stale GroundingStatus-Werte
            // (vom letzten lokalen Simulate VOR dem Reconcile). Die FallDetectionStrategy
            // liest IsStableOnGround → stale IsOverEdge=true → GroundedState transitioniert
            // faelschlich zu Falling → Animation oszilliert zwischen Idle und Fall.
            if (data.IsGrounded)
            {
                _motor.GroundingStatus.IsStableOnGround = true;
                _motor.GroundingStatus.SnappingPrevented = false;
                _motor.GroundingStatus.FoundAnyGround = true;
            }
            else
            {
                _motor.GroundingStatus.IsStableOnGround = false;
            }

            // Strategies re-evaluieren (IsOverEdge, IsGrounded) damit der erste
            // Replay-Tick korrekte Werte sieht, BEVOR Motor.Simulate() laeuft.
            _player.Locomotion.PostGroundingUpdate(0f);

            // Character-State wiederherstellen
            var reusable = _player.ReusableData;
            reusable.HorizontalVelocity = data.Velocity;
            reusable.VerticalVelocity = data.VerticalVelocity;
            reusable.IsCrouching = data.IsCrouching;
            reusable.ShouldWalk = data.ShouldWalk;

            // Fall-Detection-Daten resetten wenn Server bestaetigt dass Character grounded ist.
            // Verhindert dass TimeSinceGrounded ueber Reconcile-Grenzen akkumuliert und
            // dass LastGroundedY auf die Client-Prediction-Position zeigt.
            if (data.IsGrounded)
            {
                reusable.TimeSinceGrounded = 0f;
                reusable.LastGroundedY = data.Position.y;
            }

            // StateMachine State wiederherstellen.
            // AnimationController unterdruecken: RestoreState ruft Enter() auf,
            // was PlayState() triggert → Animator-CrossFade waehrend Reconcile ist unerwuenscht.
            _player.SuppressAnimationController();
            _player.RestoreMovementState(data.MovementStateIndex);
            _player.RestoreAnimationController();

            if (_debugLog)
            {
                Debug.Log($"[Driver] Reconcile: pos=({data.Position.x:F2},{data.Position.y:F2},{data.Position.z:F2}) " +
                    $"grounded={data.IsGrounded} stateIdx={data.MovementStateIndex} " +
                    $"motorGround={_motor.GroundingStatus.IsStableOnGround} " +
                    $"isOverEdge={_player.Locomotion.IsOverEdge}");
            }
        }

        #endregion
    }
}
