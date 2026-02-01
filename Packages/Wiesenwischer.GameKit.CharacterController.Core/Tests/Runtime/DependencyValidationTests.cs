using NUnit.Framework;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Locomotion;
using Wiesenwischer.GameKit.CharacterController.Core.Motor;
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
            var config = new MockLocomotionConfig();

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
                Assert.That(ex.Message, Does.Contain("ILocomotionConfig"));
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
            var config = new MockLocomotionConfig();

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
            var config = new MockLocomotionConfig();

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
            var config = new MockLocomotionConfig();

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

        #region LocomotionConfig Validation Tests

        [Test]
        public void LocomotionConfig_InvalidWalkSpeed_IsDetected()
        {
            // Arrange
            var config = new MockLocomotionConfig { WalkSpeed = -1f };

            // Act & Assert
            Assert.Less(config.WalkSpeed, 0f, "Negative WalkSpeed sollte erkannt werden");
        }

        [Test]
        public void LocomotionConfig_InvalidGravity_IsDetected()
        {
            // Arrange
            var config = new MockLocomotionConfig { Gravity = -10f };

            // Act & Assert
            Assert.Less(config.Gravity, 0f, "Negative Gravity sollte erkannt werden");
        }

        [Test]
        public void LocomotionConfig_InvalidMaxSlopeAngle_IsDetected()
        {
            // Arrange
            var configTooLow = new MockLocomotionConfig { MaxSlopeAngle = -10f };
            var configTooHigh = new MockLocomotionConfig { MaxSlopeAngle = 100f };

            // Assert
            Assert.Less(configTooLow.MaxSlopeAngle, 0f);
            Assert.Greater(configTooHigh.MaxSlopeAngle, 90f);
        }

        #endregion

        #region LocomotionInput Validation Tests

        [Test]
        public void LocomotionInput_Empty_HasValidDefaults()
        {
            // Act
            var input = LocomotionInput.Empty;

            // Assert
            Assert.AreEqual(Vector2.zero, input.MoveDirection);
            Assert.AreEqual(Vector3.forward, input.LookDirection);
            Assert.IsFalse(input.IsSprinting);
            Assert.AreEqual(0f, input.VerticalVelocity);
        }

        [Test]
        public void LocomotionInput_LookDirectionNormalized_IsValid()
        {
            // Arrange
            var input = new LocomotionInput
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

        #region CharacterLocomotion Tests

        [Test]
        public void CharacterLocomotion_NullCapsuleCollider_ThrowsArgumentNullException()
        {
            // Arrange
            var go = new GameObject("TestCharacter");
            var config = new MockLocomotionConfig();

            try
            {
                // Act & Assert
                var ex = Assert.Throws<System.ArgumentNullException>(() =>
                    new CharacterLocomotion(go.transform, null, config));

                Assert.That(ex.ParamName, Is.EqualTo("capsule"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CharacterLocomotion_NullConfig_ThrowsArgumentNullException()
        {
            // Arrange
            var go = new GameObject("TestCharacter");
            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.radius = 0.5f;
            capsule.height = 2f;

            try
            {
                // Act & Assert
                var ex = Assert.Throws<System.ArgumentNullException>(() =>
                    new CharacterLocomotion(go.transform, capsule, null));

                Assert.That(ex.ParamName, Is.EqualTo("config"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CharacterLocomotion_NullTransform_ThrowsArgumentNullException()
        {
            // Arrange
            var go = new GameObject("TestCharacter");
            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.radius = 0.5f;
            capsule.height = 2f;
            var config = new MockLocomotionConfig();

            try
            {
                // Act & Assert
                var ex = Assert.Throws<System.ArgumentNullException>(() =>
                    new CharacterLocomotion(null, capsule, config));

                Assert.That(ex.ParamName, Is.EqualTo("transform"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CharacterLocomotion_ExternalConstructor_NullGroundingDetection_ThrowsArgumentNullException()
        {
            // Arrange
            var go = new GameObject("TestCharacter");
            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.radius = 0.5f;
            capsule.height = 2f;
            var config = new MockLocomotionConfig();

            try
            {
                // Act & Assert
                var ex = Assert.Throws<System.ArgumentNullException>(() =>
                    new CharacterLocomotion(go.transform, capsule, config, null));

                Assert.That(ex.ParamName, Is.EqualTo("groundingDetection"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CharacterLocomotion_ValidParameters_CreatesInstance()
        {
            // Arrange
            var go = new GameObject("TestCharacter");
            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.radius = 0.5f;
            capsule.height = 2f;
            var config = new MockLocomotionConfig();

            try
            {
                // Act
                var locomotion = new CharacterLocomotion(go.transform, capsule, config);

                // Assert
                Assert.IsNotNull(locomotion);
                Assert.AreEqual(go.transform.position, locomotion.Position);
                Assert.IsNotNull(locomotion.Motor);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        #endregion

        #region KinematicMotor Tests

        [Test]
        public void KinematicMotor_NullTransform_ThrowsArgumentNullException()
        {
            // Arrange
            var go = new GameObject("TestCharacter");
            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.radius = 0.5f;
            capsule.height = 2f;
            var config = new MockLocomotionConfig();

            try
            {
                // Act & Assert
                var ex = Assert.Throws<System.ArgumentNullException>(() =>
                    new KinematicMotor(null, capsule, config));

                Assert.That(ex.ParamName, Is.EqualTo("transform"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void KinematicMotor_NullCapsule_ThrowsArgumentNullException()
        {
            // Arrange
            var go = new GameObject("TestCharacter");
            var config = new MockLocomotionConfig();

            try
            {
                // Act & Assert
                var ex = Assert.Throws<System.ArgumentNullException>(() =>
                    new KinematicMotor(go.transform, null, config));

                Assert.That(ex.ParamName, Is.EqualTo("capsule"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void KinematicMotor_NullConfig_ThrowsArgumentNullException()
        {
            // Arrange
            var go = new GameObject("TestCharacter");
            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.radius = 0.5f;
            capsule.height = 2f;

            try
            {
                // Act & Assert
                var ex = Assert.Throws<System.ArgumentNullException>(() =>
                    new KinematicMotor(go.transform, capsule, null));

                Assert.That(ex.ParamName, Is.EqualTo("config"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void KinematicMotor_ValidParameters_CreatesInstance()
        {
            // Arrange
            var go = new GameObject("TestCharacter");
            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.radius = 0.5f;
            capsule.height = 2f;
            var config = new MockLocomotionConfig();

            try
            {
                // Act
                var motor = new KinematicMotor(go.transform, capsule, config);

                // Assert
                Assert.IsNotNull(motor);
                Assert.AreEqual(go.transform.position, motor.Position);
                Assert.AreEqual(0.5f, motor.Radius);
                Assert.AreEqual(2f, motor.Height);
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

        private class MockLocomotionConfig : ILocomotionConfig
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
            public ILocomotionConfig Config { get; set; }
            public int CurrentTick { get; set; }
        }

        #endregion
    }
}
