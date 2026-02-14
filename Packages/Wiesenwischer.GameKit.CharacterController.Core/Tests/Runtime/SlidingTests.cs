using NUnit.Framework;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Locomotion;
using Wiesenwischer.GameKit.CharacterController.Core.Locomotion.Modules;
using Wiesenwischer.GameKit.CharacterController.Core.Motor;
using Wiesenwischer.GameKit.CharacterController.Core.StateMachine;

namespace Wiesenwischer.GameKit.CharacterController.Core.Tests
{
    [TestFixture]
    public class SlidingTests
    {
        #region SlopeModule Tests

        [Test]
        public void SlopeModule_ShouldSlide_ReturnsTrueWhenAboveMaxAngle()
        {
            var module = new SlopeModule();

            Assert.IsTrue(module.ShouldSlide(50f, 45f));
            Assert.IsTrue(module.ShouldSlide(90f, 45f));
        }

        [Test]
        public void SlopeModule_ShouldSlide_ReturnsFalseWhenAtOrBelowMaxAngle()
        {
            var module = new SlopeModule();

            Assert.IsFalse(module.ShouldSlide(45f, 45f));
            Assert.IsFalse(module.ShouldSlide(30f, 45f));
            Assert.IsFalse(module.ShouldSlide(0f, 45f));
        }

        [Test]
        public void SlopeModule_CalculateSlideVelocity_ReturnsDownhillDirection()
        {
            var module = new SlopeModule();
            // 30° slope tilted towards positive X
            var normal = Quaternion.Euler(30f, 0f, 0f) * Vector3.up;
            var velocity = module.CalculateSlideVelocity(normal, 10f, 30f);

            // Should have downward Y component
            Assert.That(velocity.y, Is.LessThan(0f), "Slide velocity should be downhill (negative Y)");
            Assert.That(velocity.magnitude, Is.GreaterThan(0f), "Slide velocity should not be zero");
        }

        [Test]
        public void SlopeModule_CalculateSlideVelocity_SteeperSlopeIsFaster()
        {
            var module = new SlopeModule();
            var normal30 = Quaternion.Euler(30f, 0f, 0f) * Vector3.up;
            var normal60 = Quaternion.Euler(60f, 0f, 0f) * Vector3.up;

            var v30 = module.CalculateSlideVelocity(normal30, 10f, 30f);
            var v60 = module.CalculateSlideVelocity(normal60, 10f, 60f);

            Assert.That(v60.magnitude, Is.GreaterThan(v30.magnitude),
                "60° slope should produce faster slide than 30°");
        }

        [Test]
        public void SlopeModule_CalculateSlideVelocity_FlatSurface_ReturnsZero()
        {
            var module = new SlopeModule();
            var velocity = module.CalculateSlideVelocity(Vector3.up, 10f, 0f);

            // Angle multiplier = 0/90 = 0, so speed should be 0
            Assert.That(velocity.magnitude, Is.LessThan(0.001f),
                "Flat surface should produce no slide velocity");
        }

        [Test]
        public void SlopeModule_IsWalkable_ReturnsCorrectly()
        {
            var module = new SlopeModule();

            Assert.IsTrue(module.IsWalkable(30f, 45f));
            Assert.IsTrue(module.IsWalkable(45f, 45f));
            Assert.IsFalse(module.IsWalkable(46f, 45f));
        }

        #endregion

        #region CharacterLocomotion Sliding Intent Tests

        [Test]
        public void CharacterLocomotion_SetSliding_SetsIsSliding()
        {
            var go = new GameObject("TestCharacter");
            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.radius = 0.5f;
            capsule.height = 2f;
            var motor = go.AddComponent<CharacterMotor>();
            var config = CreateMockConfig();

            try
            {
                var locomotion = new CharacterLocomotion(motor, config);

                Assert.IsFalse(locomotion.IsSliding, "IsSliding should be false initially");

                locomotion.SetSliding(true);
                Assert.IsTrue(locomotion.IsSliding, "IsSliding should be true after SetSliding(true)");

                locomotion.SetSliding(false);
                Assert.IsFalse(locomotion.IsSliding, "IsSliding should be false after SetSliding(false)");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CharacterLocomotion_SetSliding_ResetsSlidingTime()
        {
            var go = new GameObject("TestCharacter");
            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.radius = 0.5f;
            capsule.height = 2f;
            var motor = go.AddComponent<CharacterMotor>();
            var config = CreateMockConfig();

            try
            {
                var locomotion = new CharacterLocomotion(motor, config);

                locomotion.SetSliding(true);
                Assert.AreEqual(0f, locomotion.SlidingTime, "SlidingTime should be 0 initially");

                locomotion.SetSliding(false);
                Assert.AreEqual(0f, locomotion.SlidingTime, "SlidingTime should reset to 0 on exit");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        #endregion

        #region Config Default Tests

        [Test]
        public void Config_SlideAcceleration_HasPositiveDefault()
        {
            var config = CreateMockConfig();
            Assert.That(config.SlideAcceleration, Is.GreaterThan(0f));
        }

        [Test]
        public void Config_SlideSteerStrength_IsBetweenZeroAndOne()
        {
            var config = CreateMockConfig();
            Assert.That(config.SlideSteerStrength, Is.GreaterThanOrEqualTo(0f));
            Assert.That(config.SlideSteerStrength, Is.LessThanOrEqualTo(1f));
        }

        [Test]
        public void Config_SlideExitHysteresis_IsPositive()
        {
            var config = CreateMockConfig();
            Assert.That(config.SlideExitHysteresis, Is.GreaterThan(0f));
        }

        [Test]
        public void Config_MinSlideTime_IsPositive()
        {
            var config = CreateMockConfig();
            Assert.That(config.MinSlideTime, Is.GreaterThan(0f));
        }

        [Test]
        public void Config_SlideJumpForceMultiplier_IsBetweenZeroAndOne()
        {
            var config = CreateMockConfig();
            Assert.That(config.SlideJumpForceMultiplier, Is.GreaterThanOrEqualTo(0f));
            Assert.That(config.SlideJumpForceMultiplier, Is.LessThanOrEqualTo(1f));
        }

        #endregion

        #region Hysteresis Logic Tests

        [Test]
        public void Hysteresis_ExitAngle_IsLowerThanMaxSlopeAngle()
        {
            float maxSlopeAngle = 45f;
            float hysteresis = 3f;
            float exitAngle = maxSlopeAngle - hysteresis;

            Assert.That(exitAngle, Is.EqualTo(42f));
            Assert.That(exitAngle, Is.LessThan(maxSlopeAngle));
        }

        [Test]
        public void Hysteresis_SlopeWithinBuffer_ShouldNotExit()
        {
            float maxSlopeAngle = 45f;
            float hysteresis = 3f;
            float exitAngle = maxSlopeAngle - hysteresis;

            // Slope at 43° is within hysteresis buffer (between 42° exit and 45° entry)
            float currentSlope = 43f;
            bool shouldExit = currentSlope < exitAngle;

            Assert.IsFalse(shouldExit, "Should not exit slide at 43° with 3° hysteresis (exit at 42°)");
        }

        [Test]
        public void Hysteresis_SlopeBelowExitAngle_ShouldExit()
        {
            float maxSlopeAngle = 45f;
            float hysteresis = 3f;
            float exitAngle = maxSlopeAngle - hysteresis;

            // Slope at 41° is below exit angle
            float currentSlope = 41f;
            bool shouldExit = currentSlope < exitAngle;

            Assert.IsTrue(shouldExit, "Should exit slide at 41° (below 42° exit angle)");
        }

        #endregion

        #region Helper

        private static MockLocomotionConfig CreateMockConfig()
        {
            return new MockLocomotionConfig();
        }

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
            public float MaxVelocityForLedgeSnap { get; set; } = 0f;
            public float SlopeSlideSpeed { get; set; } = 12f;
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
            public bool RollEnabled { get; set; } = true;
            public RollTriggerMode RollTriggerMode { get; set; } = RollTriggerMode.MovementInput;
            public float RollSpeedModifier { get; set; } = 1.0f;
        }

        #endregion
    }
}
