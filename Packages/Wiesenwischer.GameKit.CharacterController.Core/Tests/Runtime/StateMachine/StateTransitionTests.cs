using NUnit.Framework;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.StateMachine;
using Wiesenwischer.GameKit.CharacterController.Core.StateMachine.States;

namespace Wiesenwischer.GameKit.CharacterController.Core.Tests.StateMachine
{
    /// <summary>
    /// Unit Tests für State Transitions.
    /// </summary>
    [TestFixture]
    public class StateTransitionTests
    {
        private MockStateMachineContext _context;
        private MockMovementConfig _config;

        private GroundedState _groundedState;
        private JumpingState _jumpingState;
        private FallingState _fallingState;

        [SetUp]
        public void SetUp()
        {
            _config = new MockMovementConfig();
            _context = new MockStateMachineContext { Config = _config };

            // Create states
            _jumpingState = new JumpingState();
            _fallingState = new FallingState();
            _groundedState = new GroundedState(_jumpingState, _fallingState);

            // Set circular references
            _jumpingState.SetStateReferences(_fallingState, _groundedState);
            _fallingState.SetStateReferences(_groundedState, _jumpingState);
        }

        #region Grounded State Transitions

        [Test]
        public void GroundedState_JumpPressed_TransitionsToJumping()
        {
            // Arrange
            _context.IsGrounded = true;
            _context.JumpPressed = true;
            _groundedState.Enter(_context);

            // Act
            var nextState = _groundedState.EvaluateTransitions(_context);

            // Assert
            Assert.AreEqual(_jumpingState, nextState);
        }

        [Test]
        public void GroundedState_NotGrounded_TransitionsToFalling()
        {
            // Arrange
            _context.IsGrounded = true;
            _groundedState.Enter(_context);

            // Simulate walking off edge
            _context.IsGrounded = false;

            // Simulate time passing beyond coyote time
            for (int i = 0; i < 20; i++)
            {
                _groundedState.Update(_context, 0.016f);
            }

            // Act
            var nextState = _groundedState.EvaluateTransitions(_context);

            // Assert
            Assert.AreEqual(_fallingState, nextState);
        }

        [Test]
        public void GroundedState_CoyoteTime_AllowsJump()
        {
            // Arrange
            _context.IsGrounded = true;
            _groundedState.Enter(_context);

            // Walk off edge
            _context.IsGrounded = false;
            _groundedState.Update(_context, 0.05f); // Within coyote time

            // Jump within coyote time
            _context.JumpPressed = true;

            // Act
            var nextState = _groundedState.EvaluateTransitions(_context);

            // Assert
            Assert.AreEqual(_jumpingState, nextState);
        }

        [Test]
        public void GroundedState_NoInput_NoTransition()
        {
            // Arrange
            _context.IsGrounded = true;
            _context.JumpPressed = false;
            _groundedState.Enter(_context);

            // Act
            var nextState = _groundedState.EvaluateTransitions(_context);

            // Assert
            Assert.IsNull(nextState);
        }

        #endregion

        #region Jumping State Transitions

        [Test]
        public void JumpingState_Enter_SetsPositiveVerticalVelocity()
        {
            // Arrange
            _context.VerticalVelocity = 0f;

            // Act
            _jumpingState.Enter(_context);

            // Assert
            Assert.Greater(_context.VerticalVelocity, 0f);
        }

        [Test]
        public void JumpingState_VelocityBecomesNegative_TransitionsToFalling()
        {
            // Arrange
            _jumpingState.Enter(_context);
            _context.VerticalVelocity = -0.1f; // Falling

            // Act
            var nextState = _jumpingState.EvaluateTransitions(_context);

            // Assert
            Assert.AreEqual(_fallingState, nextState);
        }

        [Test]
        public void JumpingState_PositiveVelocity_NoTransition()
        {
            // Arrange
            _jumpingState.Enter(_context);
            _context.VerticalVelocity = 5f; // Still rising

            // Act
            var nextState = _jumpingState.EvaluateTransitions(_context);

            // Assert
            Assert.IsNull(nextState);
        }

        [Test]
        public void JumpingState_JumpReleased_ReducesVelocity()
        {
            // Arrange
            _context.JumpHeld = true;
            _jumpingState.Enter(_context);
            float initialVelocity = _context.VerticalVelocity;

            // Release jump
            _context.JumpHeld = false;
            _jumpingState.Update(_context, 0.016f);

            // Assert
            Assert.Less(_context.VerticalVelocity, initialVelocity);
        }

        #endregion

        #region Falling State Transitions

        [Test]
        public void FallingState_IsGrounded_TransitionsToGrounded()
        {
            // Arrange
            _context.IsGrounded = false;
            _fallingState.Enter(_context);

            // Land
            _context.IsGrounded = true;

            // Act
            var nextState = _fallingState.EvaluateTransitions(_context);

            // Assert
            Assert.AreEqual(_groundedState, nextState);
        }

        [Test]
        public void FallingState_JumpBuffer_TransitionsToJumping()
        {
            // Arrange
            _context.IsGrounded = false;
            _fallingState.Enter(_context);

            // Press jump while falling
            _context.JumpPressed = true;
            _fallingState.Update(_context, 0.016f);

            // Land within buffer time
            _context.IsGrounded = true;

            // Act
            var nextState = _fallingState.EvaluateTransitions(_context);

            // Assert
            Assert.AreEqual(_jumpingState, nextState);
        }

        [Test]
        public void FallingState_JumpBufferExpired_TransitionsToGrounded()
        {
            // Arrange
            _context.IsGrounded = false;
            _fallingState.Enter(_context);

            // Press jump while falling
            _context.JumpPressed = true;
            _fallingState.Update(_context, 0.016f);

            // Wait for buffer to expire
            for (int i = 0; i < 20; i++)
            {
                _context.JumpPressed = false;
                _fallingState.Update(_context, 0.016f);
            }

            // Land after buffer expired
            _context.IsGrounded = true;

            // Act
            var nextState = _fallingState.EvaluateTransitions(_context);

            // Assert
            Assert.AreEqual(_groundedState, nextState);
        }

        [Test]
        public void FallingState_NotGrounded_NoTransition()
        {
            // Arrange
            _context.IsGrounded = false;
            _fallingState.Enter(_context);

            // Act
            var nextState = _fallingState.EvaluateTransitions(_context);

            // Assert
            Assert.IsNull(nextState);
        }

        #endregion

        #region State Enter/Exit Tests

        [Test]
        public void GroundedState_Enter_ResetsVerticalVelocity()
        {
            // Arrange
            _context.IsGrounded = true;
            _context.VerticalVelocity = -10f;

            // Act
            _groundedState.Enter(_context);

            // Assert
            Assert.AreEqual(0f, _context.VerticalVelocity);
        }

        [Test]
        public void States_CanBeEnteredMultipleTimes()
        {
            // Arrange & Act
            _groundedState.Enter(_context);
            _groundedState.Exit(_context);
            _groundedState.Enter(_context);

            // Assert - no exception thrown
            Assert.Pass();
        }

        #endregion

        #region Mock Classes

        private class MockStateMachineContext : IStateMachineContext
        {
            public Vector2 MoveInput { get; set; }
            public bool JumpPressed { get; set; }
            public bool JumpHeld { get; set; }
            public bool IsGrounded { get; set; }
            public float VerticalVelocity { get; set; }
            public Vector3 HorizontalVelocity { get; set; }
            public IMovementConfig Config { get; set; }
            public int CurrentTick { get; set; }
        }

        private class MockMovementConfig : IMovementConfig
        {
            public float WalkSpeed { get; set; } = 5f;
            public float RunSpeed { get; set; } = 10f;
            public float Acceleration { get; set; } = 10f;
            public float Deceleration { get; set; } = 10f;
            public float AirControl { get; set; } = 0.3f;
            public float Gravity { get; set; } = 20f;
            public float MaxFallSpeed { get; set; } = 50f;
            public float JumpHeight { get; set; } = 2f;
            public float JumpDuration { get; set; } = 0.4f;
            public float CoyoteTime { get; set; } = 0.15f;
            public float JumpBufferTime { get; set; } = 0.1f;
            public float GroundCheckDistance { get; set; } = 0.2f;
            public float GroundCheckRadius { get; set; } = 0.3f;
            public LayerMask GroundLayers { get; set; } = ~0;
            public float MaxSlopeAngle { get; set; } = 45f;
            public float RotationSpeed { get; set; } = 720f;
            public bool RotateTowardsMovement { get; set; } = true;
            public float MaxStepHeight { get; set; } = 0.3f;
            public float MinStepDepth { get; set; } = 0.1f;
        }

        #endregion
    }
}
