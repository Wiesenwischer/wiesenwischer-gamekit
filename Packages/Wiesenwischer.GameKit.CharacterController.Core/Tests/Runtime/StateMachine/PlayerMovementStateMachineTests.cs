using NUnit.Framework;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Data;
using Wiesenwischer.GameKit.CharacterController.Core.Locomotion;
using Wiesenwischer.GameKit.CharacterController.Core.StateMachine;

namespace Wiesenwischer.GameKit.CharacterController.Core.Tests.StateMachine
{
    /// <summary>
    /// Unit Tests für PlayerStateReusableData.
    /// </summary>
    [TestFixture]
    public class PlayerStateReusableDataTests
    {
        private PlayerStateReusableData _reusableData;

        [SetUp]
        public void SetUp()
        {
            _reusableData = new PlayerStateReusableData();
        }

        #region Initial State Tests

        [Test]
        public void InitialState_MovementSpeedModifier_IsOne()
        {
            Assert.AreEqual(1f, _reusableData.MovementSpeedModifier);
        }

        [Test]
        public void InitialState_JumpWasReleased_IsTrue()
        {
            Assert.IsTrue(_reusableData.JumpWasReleased);
        }

        [Test]
        public void InitialState_Velocities_AreZero()
        {
            Assert.AreEqual(0f, _reusableData.VerticalVelocity);
            Assert.AreEqual(Vector3.zero, _reusableData.HorizontalVelocity);
        }

        #endregion

        #region Velocity Tests

        [Test]
        public void Velocity_CombinesHorizontalAndVertical()
        {
            // Arrange
            _reusableData.HorizontalVelocity = new Vector3(1f, 0f, 2f);
            _reusableData.VerticalVelocity = 5f;

            // Act
            Vector3 velocity = _reusableData.Velocity;

            // Assert
            Assert.AreEqual(new Vector3(1f, 5f, 2f), velocity);
        }

        #endregion

        #region Reset Tests

        [Test]
        public void ResetTemporaryData_ClearsInputFlags()
        {
            // Arrange
            _reusableData.JumpPressed = true;
            _reusableData.DashPressed = true;
            _reusableData.JumpButtonReleased = true;

            // Act
            _reusableData.ResetTemporaryData();

            // Assert
            Assert.IsFalse(_reusableData.JumpPressed);
            Assert.IsFalse(_reusableData.DashPressed);
            Assert.IsFalse(_reusableData.JumpButtonReleased);
        }

        [Test]
        public void ResetTemporaryData_ClearsIntentFlags()
        {
            // Arrange
            _reusableData.JumpRequested = true;
            _reusableData.JumpCutRequested = true;
            _reusableData.ResetVerticalRequested = true;

            // Act
            _reusableData.ResetTemporaryData();

            // Assert
            Assert.IsFalse(_reusableData.JumpRequested);
            Assert.IsFalse(_reusableData.JumpCutRequested);
            Assert.IsFalse(_reusableData.ResetVerticalRequested);
        }

        [Test]
        public void ResetMovementData_ClearsVelocitiesAndModifiers()
        {
            // Arrange
            _reusableData.VerticalVelocity = 10f;
            _reusableData.HorizontalVelocity = Vector3.one;
            _reusableData.MovementSpeedModifier = 2f;

            // Act
            _reusableData.ResetMovementData();

            // Assert
            Assert.AreEqual(0f, _reusableData.VerticalVelocity);
            Assert.AreEqual(Vector3.zero, _reusableData.HorizontalVelocity);
            Assert.AreEqual(1f, _reusableData.MovementSpeedModifier);
        }

        #endregion

        #region Rotation Ref Tests

        [Test]
        public void CurrentTargetRotation_CanBeModifiedByRef()
        {
            // Arrange & Act
            ref Vector3 rotation = ref _reusableData.CurrentTargetRotation;
            rotation = new Vector3(1f, 2f, 3f);

            // Assert
            Assert.AreEqual(new Vector3(1f, 2f, 3f), _reusableData.CurrentTargetRotation);
        }

        #endregion
    }

    /// <summary>
    /// Integration Tests für PlayerMovementStateMachine.
    /// Benötigt Unity PlayMode für volle Funktionalität.
    /// </summary>
    [TestFixture]
    public class PlayerMovementStateMachineIntegrationTests
    {
        private GameObject _playerGO;
        private PlayerController _player;

        [SetUp]
        public void SetUp()
        {
            // Create player GameObject with required components
            _playerGO = new GameObject("TestPlayer");
            var capsule = _playerGO.AddComponent<CapsuleCollider>();
            capsule.radius = 0.5f;
            capsule.height = 2f;

            // Create and assign config
            var config = ScriptableObject.CreateInstance<LocomotionConfig>();

            // Add PlayerController
            _player = _playerGO.AddComponent<PlayerController>();

            // Note: Full integration tests require proper setup in Unity Editor
            // These tests verify basic structure only
        }

        [TearDown]
        public void TearDown()
        {
            if (_playerGO != null)
            {
                Object.DestroyImmediate(_playerGO);
            }
        }

        [Test]
        public void PlayerController_HasRequiredComponents()
        {
            Assert.IsNotNull(_player.GetComponent<CapsuleCollider>());
        }
    }

    /// <summary>
    /// Mock LocomotionConfig für Tests.
    /// </summary>
    public class MockLocomotionConfig : ILocomotionConfig
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
}
