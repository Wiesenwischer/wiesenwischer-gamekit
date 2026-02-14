using NUnit.Framework;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Locomotion;
using Wiesenwischer.GameKit.CharacterController.Core.StateMachine;

namespace Wiesenwischer.GameKit.CharacterController.Core.Tests.Locomotion
{
    /// <summary>
    /// Unit Tests für MovementMotor.
    /// </summary>
    [TestFixture]
    public class LocomotionTests
    {
        private MockLocomotionConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = new MockLocomotionConfig
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

        #region LocomotionInput Tests

        [Test]
        public void LocomotionInput_Empty_HasCorrectDefaults()
        {
            // Arrange & Act
            var input = LocomotionInput.Empty;

            // Assert
            Assert.AreEqual(Vector2.zero, input.MoveDirection);
            Assert.AreEqual(Vector3.forward, input.LookDirection);
            Assert.AreEqual(1f, input.SpeedModifier);
            Assert.IsFalse(input.StepDetectionEnabled);
        }

        [Test]
        public void LocomotionInput_WithValues_StoresCorrectly()
        {
            // Arrange & Act
            var input = new LocomotionInput
            {
                MoveDirection = new Vector2(0.5f, 0.5f),
                LookDirection = Vector3.right,
                SpeedModifier = 2f,
                StepDetectionEnabled = true
            };

            // Assert
            Assert.AreEqual(new Vector2(0.5f, 0.5f), input.MoveDirection);
            Assert.AreEqual(Vector3.right, input.LookDirection);
            Assert.AreEqual(2f, input.SpeedModifier);
            Assert.IsTrue(input.StepDetectionEnabled);
        }

        #endregion

        #region Slope Sliding Tests

        [Test]
        public void SlopeSliding_CalculateSlideDirection_OnSteepSlope()
        {
            // Arrange - 60° Hang (steiler als MaxSlopeAngle von 45°)
            float slopeAngle = 60f;
            Vector3 slopeNormal = Quaternion.Euler(-slopeAngle, 0f, 0f) * Vector3.up;

            // Act - Berechne Rutschrichtung
            Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, slopeNormal).normalized;

            // Assert
            Assert.Greater(slideDirection.z, 0f, "Sollte nach vorne/unten rutschen");
            Assert.Less(slideDirection.y, 0f, "Sollte nach unten gerichtet sein");
        }

        [Test]
        public void SlopeSliding_SlideIntensity_ScalesWithAngle()
        {
            // Arrange
            float maxSlopeAngle = _config.MaxSlopeAngle; // 45°

            // Act - Teste verschiedene Winkel
            float intensity45 = Mathf.InverseLerp(maxSlopeAngle, 90f, 45f);
            float intensity60 = Mathf.InverseLerp(maxSlopeAngle, 90f, 60f);
            float intensity75 = Mathf.InverseLerp(maxSlopeAngle, 90f, 75f);
            float intensity90 = Mathf.InverseLerp(maxSlopeAngle, 90f, 90f);

            // Assert
            Assert.AreEqual(0f, intensity45, 0.001f, "45° sollte keine Rutschintensität haben");
            Assert.AreEqual(1f / 3f, intensity60, 0.001f, "60° sollte 1/3 Intensität haben");
            Assert.AreEqual(2f / 3f, intensity75, 0.001f, "75° sollte 2/3 Intensität haben");
            Assert.AreEqual(1f, intensity90, 0.001f, "90° sollte volle Intensität haben");
        }

        [Test]
        public void SlopeSliding_CalculateSlideSpeed_WithConfig()
        {
            // Arrange
            float slopeSlideSpeed = _config.SlopeSlideSpeed; // 8f default
            float slopeAngle = 60f;
            float slideIntensity = Mathf.InverseLerp(_config.MaxSlopeAngle, 90f, slopeAngle);

            // Act
            float calculatedSpeed = slopeSlideSpeed * slideIntensity;

            // Assert
            Assert.AreEqual(8f * (1f / 3f), calculatedSpeed, 0.01f);
        }

        [Test]
        public void SlopeSliding_ProjectOnPlane_PreservesHorizontalDirection()
        {
            // Arrange - Hang geneigt nach Norden (Z+)
            Vector3 slopeNormal = new Vector3(0f, 0.866f, 0.5f).normalized; // ~30° nach Norden geneigt

            // Act
            Vector3 slideDir = Vector3.ProjectOnPlane(Vector3.down, slopeNormal).normalized;

            // Assert - Sollte primär in Z+ Richtung rutschen
            Assert.Greater(slideDir.z, 0f, "Sollte in Z+ Richtung rutschen");
            Assert.AreEqual(0f, slideDir.x, 0.001f, "Sollte keine X-Komponente haben");
        }

        #endregion

        #region Camera-Relative Movement Tests

        [Test]
        public void CameraRelativeMovement_ForwardInput_UsesLookDirection()
        {
            // Arrange
            Vector2 moveInput = Vector2.up; // W-Taste = Vorwärts
            Vector3 lookDirection = new Vector3(1f, 0f, 0f).normalized; // Kamera schaut nach rechts (X+)

            // Act - Transformiere Input relativ zur Look-Direction
            Quaternion lookRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);
            Vector3 worldDirection = lookRotation * inputDirection;

            // Assert - Vorwärts-Input sollte in X+ Richtung gehen (wo Kamera hinschaut)
            Assert.AreEqual(1f, worldDirection.x, 0.001f, "Sollte in X+ Richtung gehen");
            Assert.AreEqual(0f, worldDirection.z, 0.001f, "Sollte keine Z-Komponente haben");
        }

        [Test]
        public void CameraRelativeMovement_RightInput_IsPerpendicular()
        {
            // Arrange
            Vector2 moveInput = Vector2.right; // D-Taste = Rechts
            Vector3 lookDirection = Vector3.forward; // Kamera schaut nach vorne (Z+)

            // Act
            Quaternion lookRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);
            Vector3 worldDirection = lookRotation * inputDirection;

            // Assert - Rechts-Input sollte in X+ Richtung gehen
            Assert.AreEqual(1f, worldDirection.x, 0.001f, "Sollte in X+ Richtung gehen");
            Assert.AreEqual(0f, worldDirection.z, 0.001f, "Sollte keine Z-Komponente haben");
        }

        [Test]
        public void CameraRelativeMovement_DiagonalInput_MaintainsMagnitude()
        {
            // Arrange
            Vector2 moveInput = new Vector2(0.707f, 0.707f).normalized; // Diagonal
            Vector3 lookDirection = new Vector3(1f, 0f, 1f).normalized; // Kamera schaut diagonal

            // Act
            Quaternion lookRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
            Vector3 worldDirection = lookRotation * inputDirection;

            // Assert - Magnitude sollte 1 sein (normalisiert)
            Assert.AreEqual(1f, worldDirection.magnitude, 0.001f, "Magnitude sollte 1 sein");
            Assert.AreEqual(0f, worldDirection.y, 0.001f, "Sollte keine Y-Komponente haben");
        }

        [Test]
        public void CameraRelativeMovement_ZeroLookDirection_FallsBackToForward()
        {
            // Arrange
            Vector2 moveInput = Vector2.up;
            Vector3 lookDirection = Vector3.zero; // Ungültige Look-Direction

            // Act - Simuliere Fallback-Logik
            Vector3 effectiveLookDir = lookDirection.sqrMagnitude > 0.01f
                ? lookDirection.normalized
                : Vector3.forward; // Fallback

            Quaternion lookRotation = Quaternion.LookRotation(effectiveLookDir, Vector3.up);
            Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);
            Vector3 worldDirection = lookRotation * inputDirection;

            // Assert - Sollte Standard-Forward verwenden
            Assert.AreEqual(0f, worldDirection.x, 0.001f);
            Assert.AreEqual(1f, worldDirection.z, 0.001f);
        }

        [Test]
        public void CameraRelativeMovement_VerticalLookDirection_UsesHorizontalComponent()
        {
            // Arrange - Kamera schaut nach unten mit leichtem Forward
            Vector3 lookDirection = new Vector3(0f, -0.9f, 0.436f); // Fast vertikal nach unten
            Vector2 moveInput = Vector2.up;

            // Act - Extrahiere horizontale Komponente
            Vector3 horizontalLook = new Vector3(lookDirection.x, 0f, lookDirection.z);

            Vector3 worldDirection;
            if (horizontalLook.sqrMagnitude > 0.01f)
            {
                Quaternion lookRotation = Quaternion.LookRotation(horizontalLook.normalized, Vector3.up);
                Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);
                worldDirection = lookRotation * inputDirection;
            }
            else
            {
                worldDirection = Vector3.forward;
            }

            // Assert - Sollte in Z+ Richtung gehen (horizontale Komponente der Look-Direction)
            Assert.AreEqual(0f, worldDirection.x, 0.001f);
            Assert.Greater(worldDirection.z, 0.5f, "Sollte primär in Z+ Richtung gehen");
        }

        #endregion

        #region Mock Config

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
            public bool RollEnabled { get; set; } = true;
            public RollTriggerMode RollTriggerMode { get; set; } = RollTriggerMode.MovementInput;
            public float RollSpeedModifier { get; set; } = 1.0f;
        }

        #endregion
    }
}
