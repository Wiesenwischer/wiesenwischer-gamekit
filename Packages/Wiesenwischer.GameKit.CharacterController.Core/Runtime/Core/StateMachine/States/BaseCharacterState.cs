using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Core.StateMachine.States
{
    /// <summary>
    /// Abstrakte Basisklasse für Character States.
    /// Bietet gemeinsame Funktionalität für alle States.
    /// </summary>
    public abstract class BaseCharacterState : ICharacterState
    {
        /// <summary>
        /// Eindeutiger Name des States.
        /// </summary>
        public abstract string StateName { get; }

        /// <summary>
        /// Zeit seit Betreten des States (in Sekunden).
        /// </summary>
        protected float StateTime { get; private set; }

        /// <summary>
        /// Tick bei dem der State betreten wurde.
        /// </summary>
        protected int EnterTick { get; private set; }

        /// <summary>
        /// Wird aufgerufen, wenn der State betreten wird.
        /// </summary>
        public virtual void Enter(IStateMachineContext context)
        {
            StateTime = 0f;
            EnterTick = context.CurrentTick;
            OnEnter(context);
        }

        /// <summary>
        /// Wird jeden Tick aufgerufen, während der State aktiv ist.
        /// </summary>
        public virtual void Update(IStateMachineContext context, float deltaTime)
        {
            StateTime += deltaTime;
            OnUpdate(context, deltaTime);
        }

        /// <summary>
        /// Wird aufgerufen, wenn der State verlassen wird.
        /// </summary>
        public virtual void Exit(IStateMachineContext context)
        {
            OnExit(context);
        }

        /// <summary>
        /// Evaluiert mögliche Übergänge zu anderen States.
        /// </summary>
        public abstract ICharacterState EvaluateTransitions(IStateMachineContext context);

        /// <summary>
        /// Callback für State-Enter. Überschreibe diese Methode für State-spezifische Logik.
        /// </summary>
        protected virtual void OnEnter(IStateMachineContext context) { }

        /// <summary>
        /// Callback für State-Update. Überschreibe diese Methode für State-spezifische Logik.
        /// </summary>
        protected virtual void OnUpdate(IStateMachineContext context, float deltaTime) { }

        /// <summary>
        /// Callback für State-Exit. Überschreibe diese Methode für State-spezifische Logik.
        /// </summary>
        protected virtual void OnExit(IStateMachineContext context) { }
    }
}
