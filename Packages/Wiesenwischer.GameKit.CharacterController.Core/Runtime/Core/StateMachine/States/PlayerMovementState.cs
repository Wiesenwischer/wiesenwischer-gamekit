using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Data;
using Wiesenwischer.GameKit.CharacterController.Core.Locomotion;

namespace Wiesenwischer.GameKit.CharacterController.Core.StateMachine.States
{
    /// <summary>
    /// Basis-Klasse für alle Player Movement States.
    /// Basiert auf dem Genshin Impact Pattern:
    /// - Hält Referenz zur StateMachine
    /// - Zugriff auf ReusableData und Config über stateMachine
    /// </summary>
    public abstract class PlayerMovementState : IPlayerMovementState
    {
        protected readonly PlayerMovementStateMachine stateMachine;

        /// <summary>Zeit seit Betreten des States.</summary>
        protected float stateTime;

        public abstract string StateName { get; }

        protected PlayerMovementState(PlayerMovementStateMachine stateMachine)
        {
            this.stateMachine = stateMachine;
        }

        #region Shortcuts für häufigen Zugriff

        /// <summary>Shared runtime data.</summary>
        protected PlayerStateReusableData ReusableData => stateMachine.ReusableData;

        /// <summary>Locomotion Config.</summary>
        protected ILocomotionConfig Config => stateMachine.Config;

        /// <summary>Der PlayerController.</summary>
        protected PlayerController Player => stateMachine.Player;

        /// <summary>Ob der Character geerdet ist (von der IGroundDetectionStrategy).</summary>
        protected bool IsGrounded => Player.Locomotion?.IsGrounded ?? false;

        /// <summary>Ob der Character über einer Kante steht und fallen sollte (von der IGroundDetectionStrategy).</summary>
        protected bool IsOverEdge => Player.Locomotion?.IsOverEdge ?? false;

        #endregion

        #region IPlayerMovementState Implementation

        public virtual void Enter()
        {
            stateTime = 0f;
            OnEnter();
        }

        public virtual void Exit()
        {
            OnExit();
        }

        public virtual void HandleInput()
        {
            OnHandleInput();
        }

        public virtual void Update()
        {
            stateTime += Time.deltaTime;
            OnUpdate();
        }

        public virtual void PhysicsUpdate(float deltaTime)
        {
            OnPhysicsUpdate(deltaTime);
        }

        #endregion

        #region Template Methods

        protected virtual void OnEnter() { }
        protected virtual void OnExit() { }
        protected virtual void OnHandleInput() { }
        protected virtual void OnUpdate() { }
        protected virtual void OnPhysicsUpdate(float deltaTime) { }

        #endregion

        #region State Transitions

        /// <summary>
        /// Wechselt zu einem anderen State.
        /// </summary>
        protected void ChangeState(IPlayerMovementState newState)
        {
            stateMachine.ChangeState(newState);
        }

        #endregion

        #region Movement Helpers

        /// <summary>
        /// Berechnet die Bewegungsrichtung basierend auf Input und Kamera.
        /// </summary>
        protected Vector3 GetMovementInputDirection()
        {
            return new Vector3(ReusableData.MoveInput.x, 0f, ReusableData.MoveInput.y);
        }

        /// <summary>
        /// Berechnet die kamera-relative Bewegungsrichtung.
        /// </summary>
        protected Vector3 GetCameraRelativeMovementDirection()
        {
            Vector3 inputDir = GetMovementInputDirection();
            if (inputDir.sqrMagnitude < 0.01f) return Vector3.zero;

            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Vector3 cameraForward = mainCamera.transform.forward;
                cameraForward.y = 0f;
                if (cameraForward.sqrMagnitude > 0.01f)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(cameraForward.normalized, Vector3.up);
                    return lookRotation * inputDir;
                }
            }

            return Player.transform.TransformDirection(inputDir);
        }

        #endregion
    }
}
