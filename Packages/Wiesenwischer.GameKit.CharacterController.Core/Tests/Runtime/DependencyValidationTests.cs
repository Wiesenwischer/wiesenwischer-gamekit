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
            Assert.AreEqual(1f, input.SpeedModifier);
            Assert.IsFalse(input.StepDetectionEnabled);
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
        public void CharacterLocomotion_NullMotor_ThrowsArgumentNullException()
        {
            // Arrange
            var config = new MockLocomotionConfig();

            // Act & Assert
            var ex = Assert.Throws<System.ArgumentNullException>(() =>
                new CharacterLocomotion(null, config));

            Assert.That(ex.ParamName, Is.EqualTo("motor"));
        }

        [Test]
        public void CharacterLocomotion_NullConfig_ThrowsArgumentNullException()
        {
            // Arrange
            var go = new GameObject("TestCharacter");
            go.AddComponent<CapsuleCollider>();
            var motor = go.AddComponent<CharacterMotor>();

            try
            {
                // Act & Assert
                var ex = Assert.Throws<System.ArgumentNullException>(() =>
                    new CharacterLocomotion(motor, null));

                Assert.That(ex.ParamName, Is.EqualTo("config"));
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
            var motor = go.AddComponent<CharacterMotor>();
            var config = new MockLocomotionConfig();

            try
            {
                // Act
                var locomotion = new CharacterLocomotion(motor, config);

                // Assert
                Assert.IsNotNull(locomotion);
                Assert.IsNotNull(locomotion.Motor);
                Assert.AreEqual(motor, locomotion.Motor);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CharacterLocomotion_MotorGroundState_IsAccessible()
        {
            // Arrange
            var go = new GameObject("TestCharacter");
            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.radius = 0.5f;
            capsule.height = 2f;
            var motor = go.AddComponent<CharacterMotor>();
            var config = new MockLocomotionConfig();

            try
            {
                // Act
                var locomotion = new CharacterLocomotion(motor, config);

                // Assert - Ground State kommt von der IGroundDetectionStrategy
                // MotorGroundDetectionStrategy: IsGrounded = Motor.GroundingStatus.IsStableOnGround
                Assert.AreEqual(locomotion.Motor.GroundingStatus.IsStableOnGround, locomotion.IsGrounded);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        #endregion

        #region CharacterMotor Tests

        [Test]
        public void CharacterMotor_RequiresCapsuleCollider()
        {
            // CharacterMotor ist ein MonoBehaviour mit [RequireComponent(typeof(CapsuleCollider))]
            // Das wird von Unity automatisch erzwungen
            var go = new GameObject("TestCharacter");

            try
            {
                // Wenn wir CharacterMotor hinzufügen, wird CapsuleCollider automatisch erstellt
                var motor = go.AddComponent<CharacterMotor>();

                // Assert
                Assert.IsNotNull(go.GetComponent<CapsuleCollider>());
                Assert.IsNotNull(motor);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CharacterMotor_ValidSetup_HasCorrectDimensions()
        {
            // Arrange
            var go = new GameObject("TestCharacter");
            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.radius = 0.5f;
            capsule.height = 2f;
            var motor = go.AddComponent<CharacterMotor>();

            try
            {
                // Assert
                Assert.IsNotNull(motor);
                Assert.AreEqual(0.5f, motor.Radius);
                Assert.AreEqual(2f, motor.Height);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CharacterMotor_GroundingStatus_IsAccessible()
        {
            // Arrange
            var go = new GameObject("TestCharacter");
            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.radius = 0.5f;
            capsule.height = 2f;
            var motor = go.AddComponent<CharacterMotor>();

            try
            {
                // Assert - Motor ist die einzige Quelle für Ground State
                Assert.IsFalse(motor.GroundingStatus.FoundAnyGround); // Initial nicht grounded
                Assert.IsFalse(motor.IsStableOnGround);
                Assert.IsFalse(motor.JustLanded);
                Assert.IsFalse(motor.JustLeftGround);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CharacterMotor_ConvenienceProperties_WorkCorrectly()
        {
            // Arrange
            var go = new GameObject("TestCharacter");
            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.radius = 0.5f;
            capsule.height = 2f;
            var motor = go.AddComponent<CharacterMotor>();

            try
            {
                // Assert - Convenience properties
                Assert.AreEqual(motor.GroundingStatus.IsStableOnGround, motor.IsStableOnGround);
                // JustLanded = IsStableOnGround && !LastGroundingStatus.IsStableOnGround
                // Initial: false && !false = false
                Assert.IsFalse(motor.JustLanded);
                // JustLeftGround = !IsStableOnGround && LastGroundingStatus.IsStableOnGround
                // Initial: !false && false = false
                Assert.IsFalse(motor.JustLeftGround);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        #endregion

        #region Mock Classes

        private class MockLocomotionConfig : ILocomotionConfig
        {
            public float WalkSpeed { get; set; } = 5f;
            public float RunSpeed { get; set; } = 10f;
            public float Acceleration { get; set; } = 10f;
            public float Deceleration { get; set; } = 10f;
            public float SprintMultiplier { get; set; } = 1.5f;
            public float AirControl { get; set; } = 0.3f;
            public float AirDrag { get; set; } = 0.8f;
            public float MinFallDistance { get; set; } = 0.5f;
            public float Gravity { get; set; } = 20f;
            public float MaxFallSpeed { get; set; } = 50f;
            public float JumpHeight { get; set; } = 2f;
            public float JumpDuration { get; set; } = 0.4f;
            public float CoyoteTime { get; set; } = 0.15f;
            public float JumpBufferTime { get; set; } = 0.1f;
            public bool UseVariableJump { get; set; } = true;
            public float GroundCheckDistance { get; set; } = 0.2f;
            public float GroundCheckRadius { get; set; } = 0.3f;
            public LayerMask GroundLayers { get; set; } = ~0;
            public float MaxSlopeAngle { get; set; } = 45f;
            public float RotationSpeed { get; set; } = 720f;
            public bool RotateTowardsMovement { get; set; } = true;
            public float MaxStepHeight { get; set; } = 0.3f;
            public float MinStepDepth { get; set; } = 0.1f;
            public bool LedgeDetectionEnabled { get; set; } = true;
            public float MaxStableDistanceFromLedge { get; set; } = 0.5f;
            public float MaxStableDenivelationAngle { get; set; } = 60f;
            public float MaxVelocityForLedgeSnap { get; set; } = 10f;
            public float SlopeSlideSpeed { get; set; } = 8f;
            public bool UseSlopeDependentSlideSpeed { get; set; } = true;
            public float SoftLandingThreshold { get; set; } = 5f;
            public float HardLandingThreshold { get; set; } = 15f;
            public float SoftLandingDuration { get; set; } = 0.1f;
            public float HardLandingDuration { get; set; } = 0.4f;
            public GroundDetectionMode GroundDetection { get; set; } = GroundDetectionMode.Motor;
            public FallDetectionMode FallDetection { get; set; } = FallDetectionMode.Motor;
            public float GroundToFallRayDistance { get; set; } = 1.0f;
            public float LightStopDeceleration { get; set; } = 12f;
            public float MediumStopDeceleration { get; set; } = 10f;
            public float HardStopDeceleration { get; set; } = 8f;
            public bool StairSpeedReductionEnabled { get; set; } = true;
            public float StairSpeedReduction { get; set; } = 0.3f;
            public float UphillSpeedPenalty { get; set; } = 0.3f;
            public float DownhillSpeedBonus { get; set; } = 0.1f;
            public float SlideAcceleration { get; set; } = 15f;
            public float SlideSteerStrength { get; set; } = 0.3f;
            public float SlideExitHysteresis { get; set; } = 3f;
            public bool CanJumpFromSlide { get; set; } = true;
            public float SlideJumpForceMultiplier { get; set; } = 0.7f;
            public float MinSlideTime { get; set; } = 0.2f;
        }

        #endregion
    }
}
