using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Animation;

namespace Wiesenwischer.GameKit.CharacterController.Core.StateMachine.States
{
    /// <summary>
    /// Abstrakte Basis für alle Stopping-States.
    /// Bremst den Character mit tier-spezifischer Deceleration ab und spielt Stopp-Animation.
    /// Kann durch Movement-Input unterbrochen werden (zurück zu Walk/Run/Sprint).
    /// Basiert auf dem Genshin Impact Pattern mit 3 Speed-Tiers.
    /// </summary>
    public abstract class PlayerStoppingState : PlayerGroundedState
    {
        protected PlayerStoppingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
        {
        }

        /// <summary>Deceleration für diesen Tier (m/s²).</summary>
        protected abstract float GetDeceleration();

        /// <summary>Animation-State für diesen Tier.</summary>
        protected abstract CharacterAnimationState GetAnimationState();

        protected override void OnEnter()
        {
            base.OnEnter();

            // MovementSpeedModifier wird NICHT auf 0 gesetzt.
            // Ohne Input ist die Target-Velocity sowieso 0 (MoveDirection=zero).
            // So bleibt der Blend Tree Speed-Parameter erhalten → Beine bewegen sich
            // während des Abbremsens weiter, statt abrupt zu stoppen.

            // Tier-spezifische Deceleration setzen
            ReusableData.DecelerationOverride = GetDeceleration();

            // Stopp-Animation abspielen
            Player.AnimationController?.PlayState(GetAnimationState());
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            // Unterbrechbar durch Movement-Input
            if (HasMovementInput())
            {
                TransitionToMovement();
                return;
            }

            // Velocity fast 0 → Idle
            if (ReusableData.HorizontalVelocity.sqrMagnitude < 0.01f)
            {
                ChangeState(stateMachine.IdlingState);
                return;
            }

            // Animation fertig → Idle
            if (Player.AnimationController != null &&
                (Player.AnimationController.CanExitAnimation || Player.AnimationController.IsAnimationComplete()))
            {
                ChangeState(stateMachine.IdlingState);
                return;
            }
        }

        protected override void OnExit()
        {
            base.OnExit();
            ReusableData.DecelerationOverride = 0f;
        }

        /// <summary>
        /// Wechselt zum passenden Movement State je nach aktuellem Input.
        /// </summary>
        protected virtual void TransitionToMovement()
        {
            if (ReusableData.SprintHeld)
            {
                ChangeState(stateMachine.SprintingState);
            }
            else if (ReusableData.ShouldWalk)
            {
                ChangeState(stateMachine.WalkingState);
            }
            else
            {
                ChangeState(stateMachine.RunningState);
            }
        }
    }
}
