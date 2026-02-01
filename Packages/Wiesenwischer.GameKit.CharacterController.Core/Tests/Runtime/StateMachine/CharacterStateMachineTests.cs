using NUnit.Framework;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.StateMachine;

namespace Wiesenwischer.GameKit.CharacterController.Core.Tests.StateMachine
{
    /// <summary>
    /// Unit Tests für CharacterStateMachine.
    /// </summary>
    [TestFixture]
    public class CharacterStateMachineTests
    {
        private CharacterStateMachine _stateMachine;
        private MockStateMachineContext _context;
        private MockState _stateA;
        private MockState _stateB;
        private MockState _stateC;

        [SetUp]
        public void SetUp()
        {
            _stateMachine = new CharacterStateMachine();
            _context = new MockStateMachineContext();
            _stateA = new MockState("StateA");
            _stateB = new MockState("StateB");
            _stateC = new MockState("StateC");
        }

        #region Registration Tests

        [Test]
        public void RegisterState_AddsStateToMachine()
        {
            // Act
            _stateMachine.RegisterState(_stateA);

            // Assert
            Assert.IsTrue(_stateMachine.HasState("StateA"));
            Assert.AreEqual(1, _stateMachine.RegisteredStateCount);
        }

        [Test]
        public void RegisterStates_AddsMultipleStates()
        {
            // Act
            _stateMachine.RegisterStates(_stateA, _stateB, _stateC);

            // Assert
            Assert.AreEqual(3, _stateMachine.RegisteredStateCount);
            Assert.IsTrue(_stateMachine.HasState("StateA"));
            Assert.IsTrue(_stateMachine.HasState("StateB"));
            Assert.IsTrue(_stateMachine.HasState("StateC"));
        }

        [Test]
        public void RegisterState_DuplicateName_ThrowsException()
        {
            // Arrange
            _stateMachine.RegisterState(_stateA);
            var duplicateState = new MockState("StateA");

            // Act & Assert
            Assert.Throws<System.ArgumentException>(() => _stateMachine.RegisterState(duplicateState));
        }

        [Test]
        public void RegisterState_NullState_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() => _stateMachine.RegisterState(null));
        }

        [Test]
        public void UnregisterState_RemovesState()
        {
            // Arrange
            _stateMachine.RegisterStates(_stateA, _stateB);

            // Act
            bool result = _stateMachine.UnregisterState("StateA");

            // Assert
            Assert.IsTrue(result);
            Assert.IsFalse(_stateMachine.HasState("StateA"));
            Assert.AreEqual(1, _stateMachine.RegisteredStateCount);
        }

        [Test]
        public void UnregisterState_ActiveState_ReturnsFalse()
        {
            // Arrange
            _stateMachine.RegisterStates(_stateA, _stateB);
            _stateMachine.Initialize(_context, "StateA");

            // Act
            bool result = _stateMachine.UnregisterState("StateA");

            // Assert
            Assert.IsFalse(result);
            Assert.IsTrue(_stateMachine.HasState("StateA"));
        }

        [Test]
        public void GetState_ReturnsCorrectState()
        {
            // Arrange
            _stateMachine.RegisterState(_stateA);

            // Act
            var state = _stateMachine.GetState("StateA");

            // Assert
            Assert.AreEqual(_stateA, state);
        }

        [Test]
        public void GetState_NotRegistered_ReturnsNull()
        {
            // Act
            var state = _stateMachine.GetState("NonExistent");

            // Assert
            Assert.IsNull(state);
        }

        #endregion

        #region Initialization Tests

        [Test]
        public void Initialize_SetsCurrentState()
        {
            // Arrange
            _stateMachine.RegisterState(_stateA);

            // Act
            _stateMachine.Initialize(_context, "StateA");

            // Assert
            Assert.AreEqual(_stateA, _stateMachine.CurrentState);
            Assert.AreEqual("StateA", _stateMachine.CurrentStateName);
            Assert.IsTrue(_stateMachine.IsInitialized);
        }

        [Test]
        public void Initialize_CallsEnterOnInitialState()
        {
            // Arrange
            _stateMachine.RegisterState(_stateA);

            // Act
            _stateMachine.Initialize(_context, "StateA");

            // Assert
            Assert.AreEqual(1, _stateA.EnterCount);
        }

        [Test]
        public void Initialize_InvalidState_ThrowsException()
        {
            // Arrange
            _stateMachine.RegisterState(_stateA);

            // Act & Assert
            Assert.Throws<System.ArgumentException>(() => _stateMachine.Initialize(_context, "NonExistent"));
        }

        [Test]
        public void Initialize_NullContext_ThrowsException()
        {
            // Arrange
            _stateMachine.RegisterState(_stateA);

            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() => _stateMachine.Initialize(null, "StateA"));
        }

        [Test]
        public void Initialize_AddsToHistory()
        {
            // Arrange
            _stateMachine.RegisterState(_stateA);

            // Act
            _stateMachine.Initialize(_context, "StateA");

            // Assert
            Assert.AreEqual(1, _stateMachine.History.Count);
            var entry = _stateMachine.History.GetLastEntry();
            Assert.IsNotNull(entry);
            Assert.AreEqual("StateA", entry.Value.ToStateName);
            Assert.AreEqual(StateTransitionReason.Initialization, entry.Value.Reason);
        }

        #endregion

        #region Update Tests

        [Test]
        public void Update_CallsUpdateOnCurrentState()
        {
            // Arrange
            _stateMachine.RegisterState(_stateA);
            _stateMachine.Initialize(_context, "StateA");

            // Act
            _stateMachine.Update(0.016f);

            // Assert
            Assert.AreEqual(1, _stateA.UpdateCount);
        }

        [Test]
        public void Update_NotInitialized_DoesNothing()
        {
            // Arrange
            _stateMachine.RegisterState(_stateA);

            // Act
            _stateMachine.Update(0.016f);

            // Assert
            Assert.AreEqual(0, _stateA.UpdateCount);
        }

        [Test]
        public void Update_EvaluatesTransitions()
        {
            // Arrange
            _stateA.NextState = _stateB;
            _stateMachine.RegisterStates(_stateA, _stateB);
            _stateMachine.Initialize(_context, "StateA");

            // Act
            _stateMachine.Update(0.016f);

            // Assert
            Assert.AreEqual(_stateB, _stateMachine.CurrentState);
        }

        #endregion

        #region Transition Tests

        [Test]
        public void TransitionTo_ChangesState()
        {
            // Arrange
            _stateMachine.RegisterStates(_stateA, _stateB);
            _stateMachine.Initialize(_context, "StateA");

            // Act
            _stateMachine.TransitionTo("StateB");

            // Assert
            Assert.AreEqual(_stateB, _stateMachine.CurrentState);
        }

        [Test]
        public void TransitionTo_CallsExitOnPreviousState()
        {
            // Arrange
            _stateMachine.RegisterStates(_stateA, _stateB);
            _stateMachine.Initialize(_context, "StateA");

            // Act
            _stateMachine.TransitionTo("StateB");

            // Assert
            Assert.AreEqual(1, _stateA.ExitCount);
        }

        [Test]
        public void TransitionTo_CallsEnterOnNewState()
        {
            // Arrange
            _stateMachine.RegisterStates(_stateA, _stateB);
            _stateMachine.Initialize(_context, "StateA");

            // Act
            _stateMachine.TransitionTo("StateB");

            // Assert
            Assert.AreEqual(1, _stateB.EnterCount);
        }

        [Test]
        public void TransitionTo_AddsToHistory()
        {
            // Arrange
            _stateMachine.RegisterStates(_stateA, _stateB);
            _stateMachine.Initialize(_context, "StateA");

            // Act
            _stateMachine.TransitionTo("StateB", StateTransitionReason.Forced);

            // Assert
            Assert.AreEqual(2, _stateMachine.History.Count);
            var entry = _stateMachine.History.GetLastEntry();
            Assert.AreEqual("StateA", entry.Value.FromStateName);
            Assert.AreEqual("StateB", entry.Value.ToStateName);
            Assert.AreEqual(StateTransitionReason.Forced, entry.Value.Reason);
        }

        [Test]
        public void TransitionTo_InvalidState_ReturnsFalse()
        {
            // Arrange
            _stateMachine.RegisterState(_stateA);
            _stateMachine.Initialize(_context, "StateA");

            // Act
            bool result = _stateMachine.TransitionTo("NonExistent");

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(_stateA, _stateMachine.CurrentState);
        }

        [Test]
        public void TransitionTo_Null_DoesNotCrash()
        {
            // Arrange
            _stateMachine.RegisterState(_stateA);
            _stateMachine.Initialize(_context, "StateA");

            // Act
            _stateMachine.TransitionTo((ICharacterState)null);

            // Assert
            Assert.AreEqual(_stateA, _stateMachine.CurrentState);
        }

        #endregion

        #region History Tests

        [Test]
        public void History_GetRecentEntries_ReturnsInReverseOrder()
        {
            // Arrange
            _stateMachine.RegisterStates(_stateA, _stateB, _stateC);
            _stateMachine.Initialize(_context, "StateA");
            _stateMachine.TransitionTo("StateB");
            _stateMachine.TransitionTo("StateC");

            // Act
            var entries = _stateMachine.History.GetRecentEntries(3);

            // Assert
            Assert.AreEqual(3, entries.Length);
            Assert.AreEqual("StateC", entries[0].ToStateName); // Most recent first
            Assert.AreEqual("StateB", entries[1].ToStateName);
            Assert.AreEqual("StateA", entries[2].ToStateName); // Oldest last
        }

        [Test]
        public void History_Clear_RemovesAllEntries()
        {
            // Arrange
            _stateMachine.RegisterStates(_stateA, _stateB);
            _stateMachine.Initialize(_context, "StateA");
            _stateMachine.TransitionTo("StateB");

            // Act
            _stateMachine.History.Clear();

            // Assert
            Assert.AreEqual(0, _stateMachine.History.Count);
        }

        #endregion

        #region Reset Tests

        [Test]
        public void Reset_ClearsCurrentState()
        {
            // Arrange
            _stateMachine.RegisterState(_stateA);
            _stateMachine.Initialize(_context, "StateA");

            // Act
            _stateMachine.Reset();

            // Assert
            Assert.IsNull(_stateMachine.CurrentState);
            Assert.IsFalse(_stateMachine.IsInitialized);
        }

        [Test]
        public void Reset_CallsExitOnCurrentState()
        {
            // Arrange
            _stateMachine.RegisterState(_stateA);
            _stateMachine.Initialize(_context, "StateA");

            // Act
            _stateMachine.Reset();

            // Assert
            Assert.AreEqual(1, _stateA.ExitCount);
        }

        #endregion

        #region Mock Classes

        private class MockState : ICharacterState
        {
            public string StateName { get; }
            public int EnterCount { get; private set; }
            public int UpdateCount { get; private set; }
            public int ExitCount { get; private set; }
            public ICharacterState NextState { get; set; }

            public MockState(string name)
            {
                StateName = name;
            }

            public void Enter(IStateMachineContext context)
            {
                EnterCount++;
            }

            public void Update(IStateMachineContext context, float deltaTime)
            {
                UpdateCount++;
            }

            public void Exit(IStateMachineContext context)
            {
                ExitCount++;
            }

            public ICharacterState EvaluateTransitions(IStateMachineContext context)
            {
                return NextState;
            }
        }

        private class MockStateMachineContext : IStateMachineContext
        {
            public Vector2 MoveInput { get; set; }
            public bool JumpPressed { get; set; }
            public bool JumpHeld { get; set; }
            public bool IsGrounded { get; set; }
            public float VerticalVelocity { get; set; }
            public Vector3 HorizontalVelocity { get; set; }
            public ILocomotionConfig Config { get; set; }
            public int CurrentTick { get; set; }
        }

        #endregion
    }
}
