using NUnit.Framework;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Movement;

namespace Wiesenwischer.GameKit.CharacterController.Core.Tests.Movement
{
    /// <summary>
    /// Unit Tests für GroundInfo Struktur.
    /// </summary>
    [TestFixture]
    public class GroundInfoTests
    {
        #region Empty Ground Info Tests

        [Test]
        public void Empty_IsNotGrounded()
        {
            // Arrange & Act
            var groundInfo = GroundInfo.Empty;

            // Assert
            Assert.IsFalse(groundInfo.IsGrounded);
        }

        [Test]
        public void Empty_IsNotWalkable()
        {
            // Arrange & Act
            var groundInfo = GroundInfo.Empty;

            // Assert
            Assert.IsFalse(groundInfo.IsWalkable);
        }

        [Test]
        public void Empty_HasZeroSlopeAngle()
        {
            // Arrange & Act
            var groundInfo = GroundInfo.Empty;

            // Assert
            Assert.AreEqual(0f, groundInfo.SlopeAngle);
        }

        [Test]
        public void Empty_HasZeroPoint()
        {
            // Arrange & Act
            var groundInfo = GroundInfo.Empty;

            // Assert
            Assert.AreEqual(Vector3.zero, groundInfo.Point);
        }

        #endregion

        #region Slope Angle Tests

        [Test]
        public void SlopeAngle_FlatGround_IsZero()
        {
            // Arrange
            var groundInfo = new GroundInfo
            {
                IsGrounded = true,
                Normal = Vector3.up,
                SlopeAngle = Vector3.Angle(Vector3.up, Vector3.up)
            };

            // Assert
            Assert.AreEqual(0f, groundInfo.SlopeAngle);
        }

        [Test]
        public void SlopeAngle_45DegreeSlope_Is45()
        {
            // Arrange
            Vector3 slopeNormal = new Vector3(1f, 1f, 0f).normalized;
            float angle = Vector3.Angle(slopeNormal, Vector3.up);

            var groundInfo = new GroundInfo
            {
                IsGrounded = true,
                Normal = slopeNormal,
                SlopeAngle = angle
            };

            // Assert
            Assert.AreEqual(45f, groundInfo.SlopeAngle, 0.1f);
        }

        [Test]
        public void SlopeAngle_VerticalWall_Is90()
        {
            // Arrange
            Vector3 wallNormal = Vector3.right;
            float angle = Vector3.Angle(wallNormal, Vector3.up);

            var groundInfo = new GroundInfo
            {
                IsGrounded = true,
                Normal = wallNormal,
                SlopeAngle = angle
            };

            // Assert
            Assert.AreEqual(90f, groundInfo.SlopeAngle, 0.1f);
        }

        #endregion

        #region Walkability Tests

        [Test]
        public void Walkable_WhenSlopeUnderMax_IsTrue()
        {
            // Arrange
            float maxSlopeAngle = 45f;
            var groundInfo = new GroundInfo
            {
                IsGrounded = true,
                SlopeAngle = 30f,
                IsWalkable = 30f <= maxSlopeAngle
            };

            // Assert
            Assert.IsTrue(groundInfo.IsWalkable);
        }

        [Test]
        public void Walkable_WhenSlopeEqualsMax_IsTrue()
        {
            // Arrange
            float maxSlopeAngle = 45f;
            var groundInfo = new GroundInfo
            {
                IsGrounded = true,
                SlopeAngle = 45f,
                IsWalkable = 45f <= maxSlopeAngle
            };

            // Assert
            Assert.IsTrue(groundInfo.IsWalkable);
        }

        [Test]
        public void Walkable_WhenSlopeOverMax_IsFalse()
        {
            // Arrange
            float maxSlopeAngle = 45f;
            var groundInfo = new GroundInfo
            {
                IsGrounded = true,
                SlopeAngle = 60f,
                IsWalkable = 60f <= maxSlopeAngle
            };

            // Assert
            Assert.IsFalse(groundInfo.IsWalkable);
        }

        #endregion

        #region Slope Direction Tests

        [Test]
        public void GetSlopeDirection_OnFlat_ReturnsSameDirection()
        {
            // Arrange
            Vector3 moveDirection = Vector3.forward;
            Vector3 normal = Vector3.up;

            // Act
            Vector3 slopeDirection = Vector3.ProjectOnPlane(moveDirection, normal).normalized;

            // Assert
            Assert.AreEqual(Vector3.forward, slopeDirection);
        }

        [Test]
        public void GetSlopeDirection_OnSlope_ProjectsCorrectly()
        {
            // Arrange
            Vector3 moveDirection = Vector3.forward;
            Vector3 slopeNormal = new Vector3(0f, 1f, 0.5f).normalized; // Tilted backward

            // Act
            Vector3 slopeDirection = Vector3.ProjectOnPlane(moveDirection, slopeNormal).normalized;

            // Assert
            // Direction should be tilted downward slightly
            Assert.Greater(slopeDirection.z, 0f); // Still moving forward
            Assert.Less(slopeDirection.y, 0f); // But also downward
        }

        [Test]
        public void GetSlopeDirection_Perpendicular_ReturnsZero()
        {
            // Arrange
            Vector3 moveDirection = Vector3.up;
            Vector3 normal = Vector3.up;

            // Act
            Vector3 projected = Vector3.ProjectOnPlane(moveDirection, normal);

            // Assert
            Assert.AreEqual(0f, projected.magnitude, 0.001f);
        }

        #endregion
    }
}
