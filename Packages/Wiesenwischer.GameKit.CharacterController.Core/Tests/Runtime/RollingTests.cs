using NUnit.Framework;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Locomotion;
using Wiesenwischer.GameKit.CharacterController.Core.StateMachine;

namespace Wiesenwischer.GameKit.CharacterController.Core.Tests
{
    [TestFixture]
    public class RollingTests
    {
        #region Config Default Tests

        [Test]
        public void Config_RollEnabled_DefaultIsTrue()
        {
            var config = CreateMockConfig();
            Assert.IsTrue(config.RollEnabled);
        }

        [Test]
        public void Config_RollTriggerMode_DefaultIsMovementInput()
        {
            var config = CreateMockConfig();
            Assert.AreEqual(RollTriggerMode.MovementInput, config.RollTriggerMode);
        }

        [Test]
        public void Config_RollSpeedModifier_DefaultIsOne()
        {
            var config = CreateMockConfig();
            Assert.AreEqual(1.0f, config.RollSpeedModifier, 0.001f);
        }

        [Test]
        public void Config_RollSpeedModifier_IsPositive()
        {
            var config = CreateMockConfig();
            Assert.That(config.RollSpeedModifier, Is.GreaterThan(0f));
        }

        #endregion

        #region Speed Modifier Calculation Tests

        [Test]
        public void SpeedModifier_CalculatedCorrectly_DefaultValues()
        {
            // Arrange
            var config = CreateMockConfig();

            // Act — replicate the calculation from PlayerRollingState.OnEnter()
            float speedModifier = config.RunSpeed * config.RollSpeedModifier / config.WalkSpeed;

            // Assert — RunSpeed(10) * RollSpeedModifier(1.0) / WalkSpeed(5) = 2.0
            Assert.AreEqual(2.0f, speedModifier, 0.001f);
        }

        [Test]
        public void SpeedModifier_ScalesWithRollSpeedModifier()
        {
            // Arrange
            var config = CreateMockConfig();
            config.RollSpeedModifier = 0.5f;

            // Act
            float speedModifier = config.RunSpeed * config.RollSpeedModifier / config.WalkSpeed;

            // Assert — RunSpeed(10) * 0.5 / WalkSpeed(5) = 1.0
            Assert.AreEqual(1.0f, speedModifier, 0.001f);
        }

        [Test]
        public void SpeedModifier_DoubleSpeed_WithHighModifier()
        {
            // Arrange
            var config = CreateMockConfig();
            config.RollSpeedModifier = 2.0f;

            // Act
            float speedModifier = config.RunSpeed * config.RollSpeedModifier / config.WalkSpeed;

            // Assert — RunSpeed(10) * 2.0 / WalkSpeed(5) = 4.0
            Assert.AreEqual(4.0f, speedModifier, 0.001f);
        }

        #endregion

        #region ShouldRoll Logic Tests

        [Test]
        public void ShouldRoll_MovementInputMode_ReturnsTrueWithInput()
        {
            // Simulate ShouldRoll() logic from FallingState
            bool rollEnabled = true;
            var triggerMode = RollTriggerMode.MovementInput;
            var moveInput = new Vector2(0f, 1f); // Forward input
            bool dashPressed = false;

            bool shouldRoll = EvaluateShouldRoll(rollEnabled, triggerMode, moveInput, dashPressed);

            Assert.IsTrue(shouldRoll);
        }

        [Test]
        public void ShouldRoll_MovementInputMode_ReturnsFalseWithoutInput()
        {
            bool rollEnabled = true;
            var triggerMode = RollTriggerMode.MovementInput;
            var moveInput = Vector2.zero;
            bool dashPressed = false;

            bool shouldRoll = EvaluateShouldRoll(rollEnabled, triggerMode, moveInput, dashPressed);

            Assert.IsFalse(shouldRoll);
        }

        [Test]
        public void ShouldRoll_ButtonPressMode_ReturnsTrueWhenDashPressed()
        {
            bool rollEnabled = true;
            var triggerMode = RollTriggerMode.ButtonPress;
            var moveInput = Vector2.zero;
            bool dashPressed = true;

            bool shouldRoll = EvaluateShouldRoll(rollEnabled, triggerMode, moveInput, dashPressed);

            Assert.IsTrue(shouldRoll);
        }

        [Test]
        public void ShouldRoll_ButtonPressMode_ReturnsFalseWithoutDash()
        {
            bool rollEnabled = true;
            var triggerMode = RollTriggerMode.ButtonPress;
            var moveInput = new Vector2(1f, 0f); // Has movement but no dash
            bool dashPressed = false;

            bool shouldRoll = EvaluateShouldRoll(rollEnabled, triggerMode, moveInput, dashPressed);

            Assert.IsFalse(shouldRoll);
        }

        [Test]
        public void ShouldRoll_Disabled_AlwaysReturnsFalse()
        {
            bool rollEnabled = false;
            var triggerMode = RollTriggerMode.MovementInput;
            var moveInput = new Vector2(0f, 1f);
            bool dashPressed = true;

            bool shouldRoll = EvaluateShouldRoll(rollEnabled, triggerMode, moveInput, dashPressed);

            Assert.IsFalse(shouldRoll, "Roll should never trigger when disabled");
        }

        [Test]
        public void ShouldRoll_SmallInput_TreatedAsNoInput()
        {
            // Very small input below the 0.01 threshold
            bool rollEnabled = true;
            var triggerMode = RollTriggerMode.MovementInput;
            var moveInput = new Vector2(0.05f, 0.05f); // sqrMagnitude = 0.005 < 0.01
            bool dashPressed = false;

            bool shouldRoll = EvaluateShouldRoll(rollEnabled, triggerMode, moveInput, dashPressed);

            Assert.IsFalse(shouldRoll, "Very small input below threshold should not trigger roll");
        }

        #endregion

        #region Landing Categorization Tests

        [Test]
        public void Landing_HardFall_WithInput_CategorizedAsRoll()
        {
            // Simulate the landing categorization logic
            float landingSpeed = 20f; // Above HardLandingThreshold(15)
            float hardLandingThreshold = 15f;
            bool shouldRoll = true;

            string result = CategorizeHardLanding(landingSpeed, hardLandingThreshold, shouldRoll);

            Assert.AreEqual("Roll", result);
        }

        [Test]
        public void Landing_HardFall_WithoutInput_CategorizedAsHardLanding()
        {
            float landingSpeed = 20f;
            float hardLandingThreshold = 15f;
            bool shouldRoll = false;

            string result = CategorizeHardLanding(landingSpeed, hardLandingThreshold, shouldRoll);

            Assert.AreEqual("HardLanding", result);
        }

        [Test]
        public void Landing_SoftFall_WithInput_CategorizedAsSoftLanding()
        {
            // Below HardLandingThreshold → always SoftLanding, never Roll
            float landingSpeed = 10f; // Below threshold
            float hardLandingThreshold = 15f;

            string result = CategorizeLanding(landingSpeed, hardLandingThreshold, true);

            Assert.AreEqual("SoftLanding", result);
        }

        [Test]
        public void Landing_AtThreshold_CategorizedAsHardOrRoll()
        {
            // Exactly at threshold → >= triggers Hard/Roll path
            float landingSpeed = 15f;
            float hardLandingThreshold = 15f;

            string resultWithRoll = CategorizeHardLanding(landingSpeed, hardLandingThreshold, true);
            string resultWithoutRoll = CategorizeHardLanding(landingSpeed, hardLandingThreshold, false);

            Assert.AreEqual("Roll", resultWithRoll);
            Assert.AreEqual("HardLanding", resultWithoutRoll);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Replicates the ShouldRoll() logic from PlayerFallingState.
        /// </summary>
        private static bool EvaluateShouldRoll(bool rollEnabled, RollTriggerMode triggerMode,
            Vector2 moveInput, bool dashPressed)
        {
            if (!rollEnabled) return false;

            return triggerMode switch
            {
                RollTriggerMode.MovementInput => moveInput.sqrMagnitude > 0.01f,
                RollTriggerMode.ButtonPress => dashPressed,
                _ => false,
            };
        }

        /// <summary>
        /// Categorizes a hard landing (speed >= threshold) into Roll or HardLanding.
        /// </summary>
        private static string CategorizeHardLanding(float landingSpeed, float hardLandingThreshold, bool shouldRoll)
        {
            if (landingSpeed >= hardLandingThreshold)
            {
                return shouldRoll ? "Roll" : "HardLanding";
            }
            return "SoftLanding";
        }

        /// <summary>
        /// Full landing categorization matching FallingState.HandleLanding().
        /// </summary>
        private static string CategorizeLanding(float landingSpeed, float hardLandingThreshold, bool shouldRoll)
        {
            if (landingSpeed >= hardLandingThreshold)
            {
                return shouldRoll ? "Roll" : "HardLanding";
            }
            return "SoftLanding";
        }

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
