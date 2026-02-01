using NUnit.Framework;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Movement;
using Wiesenwischer.GameKit.CharacterController.Core.StateMachine;

namespace Wiesenwischer.GameKit.CharacterController.Core.Tests
{
    /// <summary>
    /// Tests für Abhängigkeits-Validierung.
    /// Stellt sicher, dass alle Komponenten ihre Abhängigkeiten korrekt prüfen.
    /// </summary>
    [TestFixture]
    public class DependencyValidationTests
    {
        #region GroundingDetection Tests

        [Test]
        public void GroundingDetection_NullTransform_ThrowsArgumentNullException()
        {
            // Arrange
            var config = new MockMovementConfig();

            // Act & Assert
            var ex = Assert.Throws<System.ArgumentNullException>(() =>
                new GroundingDetection(null, config, 0.5f, 2f));

            Assert.That(ex.ParamName, Is.EqualTo("transform"));
            Assert.That(ex.Message, Does.Contain("Transform"));
        }

        [Test]
        public void GroundingDetection_NullConfig_ThrowsArgumentNullException()
        {
            // Arrange
            var go = new GameObject("TestCharacter");

            try
            {
                // Act & Assert
                var ex = Assert.Throws<System.ArgumentNullException>(() =>
                    new GroundingDetection(go.transform, null, 0.5f, 2f));

                Assert.That(ex.ParamName, Is.EqualTo("config"));
                Assert.That(ex.Message, Does.Contain("IMovementConfig"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void GroundingDetection_InvalidRadius_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var go = new GameObject("TestCharacter");
            var config = new MockMovementConfig();

            try
            {
                // Act & Assert
                var ex = Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                    new GroundingDetection(go.transform, config, -0.5f, 2f));

                Assert.That(ex.ParamName, Is.EqualTo("characterRadius"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void GroundingDetection_InvalidHeight_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var go = new GameObject("TestCharacter");
            var config = new MockMovementConfig();

            try
            {
                // Act & Assert
                var ex = Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                    new GroundingDetection(go.transform, config, 0.5f, 0f));

                Assert.That(ex.ParamName, Is.EqualTo("characterHeight"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void GroundingDetection_ValidParameters_CreatesInstance()
        {
            // Arrange
            var go = new GameObject("TestCharacter");
            var config = new MockMovementConfig();

            try
            {
                // Act
                var detection = new GroundingDetection(go.transform, config, 0.5f, 2f);

                // Assert
                Assert.IsNotNull(detection);
                Assert.IsFalse(detection.IsGrounded);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        #endregion

        #region MovementConfig Validation Tests

        [Test]
        public void MovementConfig_InvalidWalkSpeed_IsDetected()
        {
            // Arrange
            var config = new MockMovementConfig { WalkSpeed = -1f };

            // Act & Assert
            Assert.Less(config.WalkSpeed, 0f, "Negative WalkSpeed sollte erkannt werden");
        }

        [Test]
        public void MovementConfig_InvalidGravity_IsDetected()
        {
            // Arrange
            var config = new MockMovementConfig { Gravity = -10f };

            // Act & Assert
            Assert.Less(config.Gravity, 0f, "Negative Gravity sollte erkannt werden");
        }

        [Test]
        public void MovementConfig_InvalidMaxSlopeAngle_IsDetected()
        {
            // Arrange
            var configTooLow = new MockMovementConfig { MaxSlopeAngle = -10f };
            var configTooHigh = new MockMovementConfig { MaxSlopeAngle = 100f };

            // Assert
            Assert.Less(configTooLow.MaxSlopeAngle, 0f);
            Assert.Greater(configTooHigh.MaxSlopeAngle, 90f);
        }

        #endregion

        #region MovementInput Validation Tests

        [Test]
        public void MovementInput_Empty_HasValidDefaults()
        {
            // Act
            var input = MovementInput.Empty;

            // Assert
            Assert.AreEqual(Vector2.zero, input.MoveDirection);
            Assert.AreEqual(Vector3.forward, input.LookDirection);
            Assert.IsFalse(input.IsSprinting);
            Assert.AreEqual(0f, input.VerticalVelocity);
        }

        [Test]
        public void MovementInput_LookDirectionNormalized_IsValid()
        {
            // Arrange
            var input = new MovementInput
            {
                MoveDirection = Vector2.up,
                LookDirection = new Vector3(10f, 0f, 10f) // Nicht normalisiert
            };

            // Act
            var normalizedLook = input.LookDirection.normalized;

            // Assert
            Assert.AreEqual(1f, normalizedLook.magnitude, 0.001f);
        }

        #endregion

        #region MovementMotor Tests

        [Test]
        public void MovementMotor_NullCharacterController_ThrowsArgumentNullException()
        {
            // Arrange
            var config = new MockMovementConfig();

            // Act & Assert
            var ex = Assert.Throws<System.ArgumentNullException>(() =>
                new MovementMotor(null, config));

            Assert.That(ex.ParamName, Is.EqualTo("characterController"));
            Assert.That(ex.Message, Does.Contain("CharacterController"));
        }

        [Test]
        public void MovementMotor_NullConfig_ThrowsArgumentNullException()
        {
            // Arrange
            var go = new GameObject("TestCharacter");
            var cc = go.AddComponent<UnityEngine.CharacterController>();

            try
            {
                // Act & Assert
                var ex = Assert.Throws<System.ArgumentNullException>(() =>
                    new MovementMotor(cc, null));

                Assert.That(ex.ParamName, Is.EqualTo("config"));
                Assert.That(ex.Message, Does.Contain("IMovementConfig"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void MovementMotor_ExternalConstructor_NullTransform_ThrowsArgumentNullException()
        {
            // Arrange
            var go = new GameObject("TestCharacter");
            var cc = go.AddComponent<UnityEngine.CharacterController>();
            var config = new MockMovementConfig();
            var detection = new GroundingDetection(go.transform, config, 0.5f, 2f);

            try
            {
                // Act & Assert
                var ex = Assert.Throws<System.ArgumentNullException>(() =>
                    new MovementMotor(null, cc, config, detection));

                Assert.That(ex.ParamName, Is.EqualTo("transform"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void MovementMotor_ExternalConstructor_NullGroundingDetection_ThrowsArgumentNullException()
        {
            // Arrange
            var go = new GameObject("TestCharacter");
            var cc = go.AddComponent<UnityEngine.CharacterController>();
            var config = new MockMovementConfig();

            try
            {
                // Act & Assert
                var ex = Assert.Throws<System.ArgumentNullException>(() =>
                    new MovementMotor(go.transform, cc, config, null));

                Assert.That(ex.ParamName, Is.EqualTo("groundingDetection"));
                Assert.That(ex.Message, Does.Contain("GroundingDetection"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void MovementMotor_ValidParameters_CreatesInstance()
        {
            // Arrange
            var go = new GameObject("TestCharacter");
            var cc = go.AddComponent<UnityEngine.CharacterController>();
            var config = new MockMovementConfig();

            try
            {
                // Act
                var simulator = new MovementMotor(cc, config);

                // Assert
                Assert.IsNotNull(simulator);
                Assert.AreEqual(go.transform.position, simulator.Position);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        #endregion

        #region State Machine Context Validation Tests

        [Test]
        public void StateMachineContext_RequiresConfig()
        {
            // Arrange
            var context = new MockStateMachineContext { Config = null };

            // Assert
            Assert.IsNull(context.Config, "Context ohne Config sollte null sein");
        }

        #endregion

        #region Mock Classes

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
            public float SlopeSlideSpeed { get; set; } = 8f;
            public bool UseSlopeDependentSlideSpeed { get; set; } = true;
        }

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

        #endregion
    }
}
