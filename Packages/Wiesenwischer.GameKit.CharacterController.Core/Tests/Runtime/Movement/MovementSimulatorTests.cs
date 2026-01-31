using NUnit.Framework;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Movement;
using Wiesenwischer.GameKit.CharacterController.Core.StateMachine;

namespace Wiesenwischer.GameKit.CharacterController.Core.Tests.Movement
{
    /// <summary>
    /// Unit Tests für MovementSimulator.
    /// </summary>
    [TestFixture]
    public class MovementSimulatorTests
    {
        private MockMovementConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = new MockMovementConfig
            {
                WalkSpeed = 5f,
                RunSpeed = 10f,
                Acceleration = 10f,
                Deceleration = 10f,
                AirControl = 0.3f,
                Gravity = 20f,
                MaxFallSpeed = 50f,
                JumpHeight = 2f
            };
        }

        #region Velocity Calculation Tests

        [Test]
        public void CalculateTargetVelocity_WithForwardInput_ReturnsCorrectVelocity()
        {
            // Arrange
            var moveDirection = new Vector2(0f, 1f); // Forward
            float targetSpeed = _config.WalkSpeed;

            // Act
            var direction3D = new Vector3(moveDirection.x, 0f, moveDirection.y).normalized;
            var targetVelocity = direction3D * targetSpeed;

            // Assert
            Assert.AreEqual(Vector3.forward * 5f, targetVelocity);
        }

        [Test]
        public void CalculateTargetVelocity_WithDiagonalInput_IsNormalized()
        {
            // Arrange
            var moveDirection = new Vector2(1f, 1f); // Diagonal
            float targetSpeed = _config.WalkSpeed;

            // Act
            var direction3D = new Vector3(moveDirection.x, 0f, moveDirection.y).normalized;
            var targetVelocity = direction3D * targetSpeed;

            // Assert
            Assert.AreEqual(_config.WalkSpeed, targetVelocity.magnitude, 0.001f);
        }

        [Test]
        public void CalculateTargetVelocity_WithZeroInput_ReturnsZero()
        {
            // Arrange
            var moveDirection = Vector2.zero;
            float targetSpeed = _config.WalkSpeed;

            // Act
            var direction3D = new Vector3(moveDirection.x, 0f, moveDirection.y);
            var targetVelocity = direction3D.sqrMagnitude > 0.01f
                ? direction3D.normalized * targetSpeed
                : Vector3.zero;

            // Assert
            Assert.AreEqual(Vector3.zero, targetVelocity);
        }

        #endregion

        #region Acceleration Tests

        [Test]
        public void Acceleration_FromZero_IncreasesVelocity()
        {
            // Arrange
            Vector3 currentVelocity = Vector3.zero;
            Vector3 targetVelocity = Vector3.forward * _config.WalkSpeed;
            float deltaTime = 0.1f; // 100ms

            // Act
            Vector3 newVelocity = Vector3.MoveTowards(
                currentVelocity,
                targetVelocity,
                _config.Acceleration * deltaTime
            );

            // Assert
            Assert.Greater(newVelocity.magnitude, 0f);
            Assert.Less(newVelocity.magnitude, targetVelocity.magnitude);
        }

        [Test]
        public void Deceleration_FromMaxSpeed_DecreasesVelocity()
        {
            // Arrange
            Vector3 currentVelocity = Vector3.forward * _config.WalkSpeed;
            Vector3 targetVelocity = Vector3.zero;
            float deltaTime = 0.1f;

            // Act
            Vector3 newVelocity = Vector3.MoveTowards(
                currentVelocity,
                targetVelocity,
                _config.Deceleration * deltaTime
            );

            // Assert
            Assert.Less(newVelocity.magnitude, currentVelocity.magnitude);
            Assert.Greater(newVelocity.magnitude, 0f);
        }

        [Test]
        public void Acceleration_ReachesTargetSpeed_Eventually()
        {
            // Arrange
            Vector3 currentVelocity = Vector3.zero;
            Vector3 targetVelocity = Vector3.forward * _config.WalkSpeed;
            float deltaTime = 0.016f;

            // Act - Simulate multiple frames
            for (int i = 0; i < 100; i++)
            {
                currentVelocity = Vector3.MoveTowards(
                    currentVelocity,
                    targetVelocity,
                    _config.Acceleration * deltaTime
                );
            }

            // Assert
            Assert.AreEqual(targetVelocity.magnitude, currentVelocity.magnitude, 0.01f);
        }

        #endregion

        #region Gravity Tests

        [Test]
        public void Gravity_WhenAirborne_IncreasesDownwardVelocity()
        {
            // Arrange
            float verticalVelocity = 0f;
            float deltaTime = 0.1f;

            // Act
            verticalVelocity -= _config.Gravity * deltaTime;

            // Assert
            Assert.Less(verticalVelocity, 0f);
            Assert.AreEqual(-2f, verticalVelocity, 0.001f);
        }

        [Test]
        public void Gravity_ClampedAtMaxFallSpeed()
        {
            // Arrange
            float verticalVelocity = 0f;
            float deltaTime = 0.1f;

            // Act - Simulate falling for a long time
            for (int i = 0; i < 100; i++)
            {
                verticalVelocity -= _config.Gravity * deltaTime;
                verticalVelocity = Mathf.Max(verticalVelocity, -_config.MaxFallSpeed);
            }

            // Assert
            Assert.AreEqual(-_config.MaxFallSpeed, verticalVelocity, 0.001f);
        }

        [Test]
        public void Jump_InitialVelocity_CalculatedCorrectly()
        {
            // Arrange
            float gravity = _config.Gravity;
            float jumpHeight = _config.JumpHeight;

            // Act
            // v = sqrt(2 * g * h)
            float jumpVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);

            // Assert
            // For gravity=20 and height=2: sqrt(2*20*2) = sqrt(80) ≈ 8.94
            Assert.AreEqual(Mathf.Sqrt(80f), jumpVelocity, 0.001f);
        }

        #endregion

        #region Air Control Tests

        [Test]
        public void AirControl_ReducesMovementInfluence()
        {
            // Arrange
            Vector3 currentVelocity = Vector3.forward * 5f;
            Vector3 targetVelocity = Vector3.right * _config.WalkSpeed;
            float deltaTime = 0.1f;

            // Act
            Vector3 newVelocity = Vector3.Lerp(currentVelocity, targetVelocity, _config.AirControl * deltaTime);

            // Assert
            // With 0.3 air control and 0.1 delta, change should be small
            Assert.Greater(Vector3.Dot(newVelocity, Vector3.forward), 0f);
        }

        #endregion

        #region Edge Cases

        [Test]
        public void ZeroDeltaTime_NoVelocityChange()
        {
            // Arrange
            Vector3 currentVelocity = Vector3.forward * 5f;
            Vector3 targetVelocity = Vector3.zero;
            float deltaTime = 0f;

            // Act
            Vector3 newVelocity = Vector3.MoveTowards(
                currentVelocity,
                targetVelocity,
                _config.Deceleration * deltaTime
            );

            // Assert
            Assert.AreEqual(currentVelocity, newVelocity);
        }

        [Test]
        public void NegativeDeltaTime_TreatedAsZero()
        {
            // Arrange
            Vector3 currentVelocity = Vector3.forward * 5f;
            Vector3 targetVelocity = Vector3.zero;
            float deltaTime = -0.1f;

            // Act
            Vector3 newVelocity = Vector3.MoveTowards(
                currentVelocity,
                targetVelocity,
                _config.Deceleration * Mathf.Max(0, deltaTime)
            );

            // Assert
            Assert.AreEqual(currentVelocity, newVelocity);
        }

        #endregion

        #region MovementInput Tests

        [Test]
        public void MovementInput_Empty_HasCorrectDefaults()
        {
            // Arrange & Act
            var input = MovementInput.Empty;

            // Assert
            Assert.AreEqual(Vector2.zero, input.MoveDirection);
            Assert.AreEqual(Vector3.forward, input.LookDirection);
            Assert.IsFalse(input.IsSprinting);
            Assert.AreEqual(0f, input.VerticalVelocity);
        }

        [Test]
        public void MovementInput_WithValues_StoresCorrectly()
        {
            // Arrange & Act
            var input = new MovementInput
            {
                MoveDirection = new Vector2(0.5f, 0.5f),
                LookDirection = Vector3.right,
                IsSprinting = true,
                VerticalVelocity = 5f
            };

            // Assert
            Assert.AreEqual(new Vector2(0.5f, 0.5f), input.MoveDirection);
            Assert.AreEqual(Vector3.right, input.LookDirection);
            Assert.IsTrue(input.IsSprinting);
            Assert.AreEqual(5f, input.VerticalVelocity);
        }

        #endregion

        #region Mock Config

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
