using System;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Data;
using Wiesenwischer.GameKit.CharacterController.Core.StateMachine.States;

namespace Wiesenwischer.GameKit.CharacterController.Core.StateMachine
{
    /// <summary>
    /// State Machine für Character Movement.
    /// Basiert auf dem Genshin Impact Pattern:
    /// - Hält Referenz zum Player (PlayerController)
    /// - Verwaltet shared ReusableData
    /// - Cached alle State-Instanzen
    /// </summary>
    public class PlayerMovementStateMachine
    {
        #region References

        /// <summary>Referenz zum PlayerController.</summary>
        public PlayerController Player { get; }

        /// <summary>Shared runtime data zwischen allen States.</summary>
        public PlayerStateReusableData ReusableData { get; }

        /// <summary>Locomotion Config für Zugriff auf Einstellungen.</summary>
        public ILocomotionConfig Config => Player.LocomotionConfig;

        #endregion

        #region Grounded State Instances

        /// <summary>Idling State - wenn der Character stillsteht.</summary>
        public PlayerIdlingState IdlingState { get; }

        /// <summary>Walking State - langsames Gehen.</summary>
        public PlayerWalkingState WalkingState { get; }

        /// <summary>Running State - normales Laufen.</summary>
        public PlayerRunningState RunningState { get; }

        /// <summary>Sprinting State - Sprinten.</summary>
        public PlayerSprintingState SprintingState { get; }

        /// <summary>Soft Landing State - nach normaler Landung (Momentum bleibt).</summary>
        public PlayerSoftLandingState SoftLandingState { get; }

        /// <summary>Hard Landing State - nach hartem Fall (Recovery-Zeit).</summary>
        public PlayerHardLandingState HardLandingState { get; }

        /// <summary>Light Stopping State - Stopp nach Walking.</summary>
        public PlayerLightStoppingState LightStoppingState { get; }

        /// <summary>Medium Stopping State - Stopp nach Running.</summary>
        public PlayerMediumStoppingState MediumStoppingState { get; }

        /// <summary>Hard Stopping State - Stopp nach Sprinting.</summary>
        public PlayerHardStoppingState HardStoppingState { get; }

        /// <summary>Rolling State - Landing Roll bei hartem Aufprall mit Movement-Input.</summary>
        public PlayerRollingState RollingState { get; }

        #endregion

        #region Hybrid State Instances

        /// <summary>Sliding State - Rutschen auf steilen Hängen (Bodenkontakt, aber nicht stabil).</summary>
        public PlayerSlidingState SlidingState { get; }

        #endregion

        #region Airborne State Instances

        /// <summary>Jumping State - während des Springens (aufsteigend).</summary>
        public PlayerJumpingState JumpingState { get; }

        /// <summary>Falling State - während des Fallens (absteigend).</summary>
        public PlayerFallingState FallingState { get; }

        #endregion

        #region Current State

        private IPlayerMovementState _currentState;

        /// <summary>Der aktuell aktive State.</summary>
        public IPlayerMovementState CurrentState => _currentState;

        /// <summary>Name des aktuellen States.</summary>
        public string CurrentStateName => _currentState?.StateName ?? "None";

        #endregion

        #region Constructor

        /// <summary>
        /// Erstellt eine neue PlayerMovementStateMachine.
        /// </summary>
        /// <param name="player">Der PlayerController.</param>
        public PlayerMovementStateMachine(PlayerController player)
        {
            Player = player ?? throw new ArgumentNullException(nameof(player));
            ReusableData = new PlayerStateReusableData();

            // Erstelle Grounded States
            IdlingState = new PlayerIdlingState(this);
            WalkingState = new PlayerWalkingState(this);
            RunningState = new PlayerRunningState(this);
            SprintingState = new PlayerSprintingState(this);
            SoftLandingState = new PlayerSoftLandingState(this);
            HardLandingState = new PlayerHardLandingState(this);
            LightStoppingState = new PlayerLightStoppingState(this);
            MediumStoppingState = new PlayerMediumStoppingState(this);
            HardStoppingState = new PlayerHardStoppingState(this);
            RollingState = new PlayerRollingState(this);

            // Erstelle Hybrid States
            SlidingState = new PlayerSlidingState(this);

            // Erstelle Airborne States
            JumpingState = new PlayerJumpingState(this);
            FallingState = new PlayerFallingState(this);
        }

        #endregion

        #region Lifecycle

        /// <summary>
        /// Initialisiert die State Machine mit dem Start-State.
        /// </summary>
        public void Initialize()
        {
            _currentState = IdlingState;
            _currentState.Enter();
            Debug.Log($"[StateMachine] Initialized → {_currentState.StateName}");
        }

        /// <summary>
        /// Update der State Machine - ruft HandleInput, Update auf.
        /// </summary>
        public void Update()
        {
            _currentState?.HandleInput();
            _currentState?.Update();
        }

        /// <summary>
        /// Physics Update der State Machine.
        /// </summary>
        /// <param name="deltaTime">Fixed delta time.</param>
        public void PhysicsUpdate(float deltaTime)
        {
            _currentState?.PhysicsUpdate(deltaTime);
        }

        #endregion

        #region State Transitions

        /// <summary>
        /// Wechselt zu einem neuen State.
        /// </summary>
        /// <param name="newState">Der neue State.</param>
        public void ChangeState(IPlayerMovementState newState)
        {
            if (newState == null)
            {
                Debug.LogWarning("[PlayerMovementStateMachine] Kann nicht zu null State wechseln.");
                return;
            }

            string oldStateName = _currentState?.StateName ?? "None";
            _currentState?.Exit();
            _currentState = newState;
            _currentState.Enter();
            Debug.Log($"[StateMachine] {oldStateName} → {newState.StateName}");
        }

        #endregion
    }

    /// <summary>
    /// Interface für Player Movement States.
    /// Basiert auf dem Genshin Impact IState Pattern.
    /// </summary>
    public interface IPlayerMovementState
    {
        /// <summary>Name des States für Debugging.</summary>
        string StateName { get; }

        /// <summary>Wird aufgerufen wenn der State betreten wird.</summary>
        void Enter();

        /// <summary>Wird aufgerufen wenn der State verlassen wird.</summary>
        void Exit();

        /// <summary>Verarbeitet Input (jeden Frame in Update).</summary>
        void HandleInput();

        /// <summary>Update (jeden Frame).</summary>
        void Update();

        /// <summary>Physics Update (feste Tick-Rate).</summary>
        void PhysicsUpdate(float deltaTime);
    }
}
