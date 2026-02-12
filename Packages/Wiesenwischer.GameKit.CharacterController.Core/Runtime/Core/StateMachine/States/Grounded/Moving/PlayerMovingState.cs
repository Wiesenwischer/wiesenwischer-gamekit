using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Animation;

namespace Wiesenwischer.GameKit.CharacterController.Core.StateMachine.States
{
    /// <summary>
    /// Basis-State für alle Moving-States (Walking, Running, Sprinting).
    /// Handhabt gemeinsame Bewegungslogik.
    /// </summary>
    public class PlayerMovingState : PlayerGroundedState
    {
        public override string StateName => "Moving";

        // Grace Period: Verhindert Animation-Oszillation bei schnellem Tasten-Tippen.
        // Ohne diese Toleranz wechselt die Animation bei jedem kurzen Loslassen sofort
        // zu CrossFade(StopAnim), wodurch der Animator permanent in unvollständigen
        // Transitions hängt und keine Bein-Animation sichtbar wird.
        private const float InputGracePeriod = 0.1f;
        private float _noInputTimer;

        public PlayerMovingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
        {
        }

        protected override void OnEnter()
        {
            base.OnEnter();
            _noInputTimer = 0f;
            Player.AnimationController?.PlayState(CharacterAnimationState.Locomotion);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (!HasMovementInput())
            {
                _noInputTimer += Time.deltaTime;
                if (_noInputTimer >= InputGracePeriod)
                {
                    ChangeState(GetStoppingState());
                    return;
                }
            }
            else
            {
                _noInputTimer = 0f;
            }
        }

        /// <summary>
        /// Gibt den passenden Stopping-State für diesen Movement-Tier zurück.
        /// Wird von Subklassen überschrieben (Walk→Light, Run→Medium, Sprint→Hard).
        /// </summary>
        protected virtual IPlayerMovementState GetStoppingState()
        {
            return stateMachine.MediumStoppingState;
        }
    }
}
