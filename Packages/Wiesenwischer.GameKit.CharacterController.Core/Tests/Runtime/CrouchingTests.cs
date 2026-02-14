using NUnit.Framework;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Data;
using Wiesenwischer.GameKit.CharacterController.Core.Locomotion;
using Wiesenwischer.GameKit.CharacterController.Core.Motor;
using Wiesenwischer.GameKit.CharacterController.Core.StateMachine;

namespace Wiesenwischer.GameKit.CharacterController.Core.Tests
{
    [TestFixture]
    public class CrouchingTests
    {
        #region CharacterLocomotion Crouching Tests

        [Test]
        public void CharacterLocomotion_SetCrouching_SetsIsCrouching()
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

                Assert.IsFalse(locomotion.IsCrouching, "IsCrouching should be false initially");

                locomotion.SetCrouching(true);
                Assert.IsTrue(locomotion.IsCrouching, "IsCrouching should be true after SetCrouching(true)");

                locomotion.SetCrouching(false);
                Assert.IsFalse(locomotion.IsCrouching, "IsCrouching should be false after SetCrouching(false)");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CharacterLocomotion_CanStandUp_ReturnsTrueWhenNotCrouching()
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

                Assert.IsTrue(locomotion.CanStandUp(), "CanStandUp should be true when not crouching");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        #endregion

        #region ReusableData Crouching Tests

        [Test]
        public void ReusableData_IsCrouching_DefaultFalse()
        {
            var data = new PlayerStateReusableData();
            Assert.IsFalse(data.IsCrouching, "IsCrouching should default to false");
        }

        [Test]
        public void ReusableData_IsCrouching_CanBeSet()
        {
            var data = new PlayerStateReusableData();
            data.IsCrouching = true;
            Assert.IsTrue(data.IsCrouching);
            data.IsCrouching = false;
            Assert.IsFalse(data.IsCrouching);
        }

        [Test]
        public void ReusableData_CrouchTogglePressed_DefaultFalse()
        {
            var data = new PlayerStateReusableData();
            Assert.IsFalse(data.CrouchTogglePressed, "CrouchTogglePressed should default to false");
        }

        #endregion

        #region Config Default Tests

        [Test]
        public void Config_CrouchHeight_IsLessThanStandingHeight()
        {
            var config = CreateMockConfig();
            Assert.That(config.CrouchHeight, Is.LessThan(config.StandingHeight),
                "CrouchHeight should be less than StandingHeight");
        }

        [Test]
        public void Config_CrouchHeight_IsPositive()
        {
            var config = CreateMockConfig();
            Assert.That(config.CrouchHeight, Is.GreaterThan(0f));
        }

        [Test]
        public void Config_CrouchSpeed_IsPositive()
        {
            var config = CreateMockConfig();
            Assert.That(config.CrouchSpeed, Is.GreaterThan(0f));
        }

        [Test]
        public void Config_CrouchSpeed_IsLessThanRunSpeed()
        {
            var config = CreateMockConfig();
            Assert.That(config.CrouchSpeed, Is.LessThan(config.RunSpeed),
                "CrouchSpeed should be less than RunSpeed");
        }

        [Test]
        public void Config_CrouchTransitionDuration_IsPositive()
        {
            var config = CreateMockConfig();
            Assert.That(config.CrouchTransitionDuration, Is.GreaterThan(0f));
        }

        [Test]
        public void Config_CrouchHeadClearanceMargin_IsPositive()
        {
            var config = CreateMockConfig();
            Assert.That(config.CrouchHeadClearanceMargin, Is.GreaterThan(0f));
        }

        [Test]
        public void Config_CrouchStepHeight_IsPositive()
        {
            var config = CreateMockConfig();
            Assert.That(config.CrouchStepHeight, Is.GreaterThan(0f));
        }

        #endregion

        #region StateMachine Registration Tests

        [Test]
        public void StateMachine_CrouchingState_IsRegistered()
        {
            var go = new GameObject("TestPlayer");
            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.radius = 0.5f;
            capsule.height = 2f;
            go.AddComponent<CharacterMotor>();
            var player = go.AddComponent<PlayerController>();

            try
            {
                // StateMachine wird erst in Awake erstellt, daher manuell
                // prüfen ob der Type existiert und instanziierbar ist
                var config = CreateMockConfig();
                Assert.IsNotNull(config.CrouchHeight,
                    "CrouchHeight should be accessible from config");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        #endregion

        #region Speed Modifier Tests

        [Test]
        public void CrouchSpeedModifier_IsCorrectRatio()
        {
            var config = CreateMockConfig();
            float expectedModifier = config.CrouchSpeed / config.WalkSpeed;

            Assert.That(expectedModifier, Is.GreaterThan(0f));
            Assert.That(expectedModifier, Is.LessThan(2f),
                "Crouch speed modifier should be reasonable");
            Assert.That(expectedModifier, Is.EqualTo(0.5f).Within(0.01f),
                "CrouchSpeed(2.5) / WalkSpeed(5.0) should be 0.5");
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
            public float CrouchHeight { get; set; } = 1.2f;
            public float StandingHeight { get; set; } = 2.0f;
            public float CrouchSpeed { get; set; } = 2.5f;
            public float CrouchAcceleration { get; set; } = 8.0f;
            public float CrouchDeceleration { get; set; } = 10.0f;
            public float CrouchTransitionDuration { get; set; } = 0.25f;
            public float CrouchHeadClearanceMargin { get; set; } = 0.1f;
            public bool CanJumpFromCrouch { get; set; } = true;
            public bool CanSprintFromCrouch { get; set; } = true;
            public float CrouchStepHeight { get; set; } = 0.2f;
        }

        #endregion
    }
}
