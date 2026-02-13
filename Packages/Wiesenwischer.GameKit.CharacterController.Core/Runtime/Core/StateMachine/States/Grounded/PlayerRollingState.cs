using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Animation;

namespace Wiesenwischer.GameKit.CharacterController.Core.StateMachine.States
{
    /// <summary>
    /// Landing Roll State — wird bei hartem Aufprall mit Movement-Input ausgelöst.
    /// Character rollt in Bewegungsrichtung und geht danach nahtlos in Walk/Run/Sprint über.
    /// Jump ist während des Rolls blockiert, Sprint deaktiviert.
    /// </summary>
    public class PlayerRollingState : PlayerGroundedState
    {
        public override string StateName => "Rolling";

        private Vector3 _rollDirection;

        public PlayerRollingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
        {
        }

        protected override void OnEnter()
        {
            base.OnEnter();

            // 1. Roll-Richtung bestimmen (kamerarelativ)
            _rollDirection = GetCameraRelativeMovementDirection();
            if (_rollDirection.sqrMagnitude < 0.01f)
                _rollDirection = Player.transform.forward;
            _rollDirection.Normalize();

            // 2. Sofortige Rotation in Roll-Richtung (snappy feel)
            Player.transform.rotation = Quaternion.LookRotation(_rollDirection);

            // 3. Speed Modifier setzen: Roll bei RollSpeedModifier * RunSpeed
            // MovementSpeedModifier ist relativ zu WalkSpeed, daher Umrechnung
            ReusableData.MovementSpeedModifier = Config.RunSpeed * Config.RollSpeedModifier / Config.WalkSpeed;

            // 4. Sprint deaktivieren
            ReusableData.SprintHeld = false;

            // 5. Animation starten
            Player.AnimationController?.PlayState(CharacterAnimationState.Roll);
        }

        protected override void OnHandleInput()
        {
            // Jump während Roll blockiert — base.OnHandleInput() wird NICHT aufgerufen.
            // Movement-Input wird vom InputSystem automatisch in ReusableData geschrieben.
        }

        protected override void OnUpdate()
        {
            // Fall-Detection beibehalten (Character könnte über Kante rollen)
            base.OnUpdate();

            // Prüfe ob Animation fertig ist (AllowExit Event oder IsAnimationComplete)
            var anim = Player.AnimationController;
            if (anim != null && (anim.CanExitAnimation || anim.IsAnimationComplete()))
            {
                TransitionToNextState();
                return;
            }

            // Fallback: Timer-basiert (falls kein AnimationController)
            if (anim == null && stateTime > 0.7f)
            {
                TransitionToNextState();
            }
        }

        protected override void OnExit()
        {
            base.OnExit();
            ReusableData.MovementSpeedModifier = 1f;
        }

        private void TransitionToNextState()
        {
            if (ReusableData.MoveInput.sqrMagnitude > 0.01f)
            {
                // Mit Input → direkt zu Walk/Run/Sprint
                if (ReusableData.SprintHeld)
                    ChangeState(stateMachine.SprintingState);
                else if (ReusableData.ShouldWalk)
                    ChangeState(stateMachine.WalkingState);
                else
                    ChangeState(stateMachine.RunningState);
            }
            else
            {
                // Ohne Input → MediumStop (Abbremsen)
                ChangeState(stateMachine.MediumStoppingState);
            }
        }
    }
}
