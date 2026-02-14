using NUnit.Framework;
using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.IK.Tests
{
    [TestFixture]
    public class FootIKTests
    {
        [Test]
        public void BodyOffset_UsesMinOfBothFeet()
        {
            float leftDelta = -0.1f;
            float rightDelta = -0.2f;
            float offset = Mathf.Min(leftDelta, rightDelta);
            offset = Mathf.Min(offset, 0f);
            Assert.AreEqual(-0.2f, offset, 0.001f);
        }

        [Test]
        public void BodyOffset_ClampsAtZeroWithoutUpOffset()
        {
            float maxFootAdjustment = 0.4f;
            float maxBodyUpOffset = 0f;
            float leftDelta = 0.1f;
            float rightDelta = 0.05f;
            float offset = Mathf.Min(leftDelta, rightDelta);
            offset = Mathf.Clamp(offset, -maxFootAdjustment, maxBodyUpOffset);
            Assert.AreEqual(0f, offset, 0.001f);
        }

        [Test]
        public void FootRotation_FlatGround_NoRotation()
        {
            Vector3 normal = Vector3.up;
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, normal);
            Assert.AreEqual(Quaternion.identity, rot);
        }

        [Test]
        public void FootRotation_SlopedGround_Tilted()
        {
            Vector3 normal = new Vector3(0f, 0.866f, 0.5f).normalized;
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, normal);
            Assert.AreNotEqual(Quaternion.identity, rot);
            float angle = Quaternion.Angle(Quaternion.identity, rot);
            Assert.AreEqual(30f, angle, 2f);
        }

        [Test]
        public void MaxAdjustment_ClampsExcessiveOffset()
        {
            float maxAdjust = 0.4f;
            float delta = -0.8f;
            float clamped = Mathf.Clamp(delta, -maxAdjust, maxAdjust);
            Assert.AreEqual(-0.4f, clamped, 0.001f);
        }

        [Test]
        public void BodyOffset_OneFoot_NoOffset()
        {
            bool leftHit = true;
            bool rightHit = false;

            float targetBodyOffset = 0f;
            if (leftHit && rightHit)
            {
                targetBodyOffset = -0.1f;
            }

            Assert.AreEqual(0f, targetBodyOffset, 0.001f);
        }

        [Test]
        public void FootOffset_AddsHeightAboveGround()
        {
            float hitPointY = 1.0f;
            float footOffset = 0.02f;
            float targetY = hitPointY + footOffset;
            Assert.AreEqual(1.02f, targetY, 0.001f);
        }

        // === Phase 24: Body-Offset nach oben ===

        [Test]
        public void BodyOffset_AllowsSmallUpward()
        {
            float maxFootAdjustment = 0.4f;
            float maxBodyUpOffset = 0.05f;
            float targetBodyOffset = 0.04f;

            float clamped = Mathf.Clamp(targetBodyOffset, -maxFootAdjustment, maxBodyUpOffset);
            Assert.AreEqual(0.04f, clamped, 0.001f);
        }

        [Test]
        public void BodyOffset_ClampsAtMaxUp()
        {
            float maxFootAdjustment = 0.4f;
            float maxBodyUpOffset = 0.05f;
            float targetBodyOffset = 0.1f;

            float clamped = Mathf.Clamp(targetBodyOffset, -maxFootAdjustment, maxBodyUpOffset);
            Assert.AreEqual(0.05f, clamped, 0.001f);
        }

        [Test]
        public void BodyOffset_StillAllowsDownward()
        {
            float maxFootAdjustment = 0.4f;
            float maxBodyUpOffset = 0.05f;
            float targetBodyOffset = -0.2f;

            float clamped = Mathf.Clamp(targetBodyOffset, -maxFootAdjustment, maxBodyUpOffset);
            Assert.AreEqual(-0.2f, clamped, 0.001f);
        }

        // === Phase 24: Terrain-Varianz ===

        [Test]
        public void TerrainVariance_FlatGround_ReturnsZero()
        {
            float leftY = 0f;
            float rightY = 0f;
            Vector3 leftNormal = Vector3.up;
            Vector3 rightNormal = Vector3.up;

            float heightDiff = Mathf.Abs(leftY - rightY);
            float leftNormalDev = 1f - Vector3.Dot(leftNormal, Vector3.up);
            float rightNormalDev = 1f - Vector3.Dot(rightNormal, Vector3.up);
            float normalDev = Mathf.Max(leftNormalDev, rightNormalDev);
            float variance = heightDiff + normalDev * 0.1f;

            Assert.AreEqual(0f, variance, 0.001f);
        }

        [Test]
        public void TerrainVariance_UnevenGround_ReturnsPositive()
        {
            float leftY = 0f;
            float rightY = 0.1f;
            Vector3 leftNormal = Vector3.up;
            Vector3 rightNormal = Vector3.up;

            float heightDiff = Mathf.Abs(leftY - rightY);
            float leftNormalDev = 1f - Vector3.Dot(leftNormal, Vector3.up);
            float rightNormalDev = 1f - Vector3.Dot(rightNormal, Vector3.up);
            float normalDev = Mathf.Max(leftNormalDev, rightNormalDev);
            float variance = heightDiff + normalDev * 0.1f;

            Assert.Greater(variance, 0f);
            Assert.AreEqual(0.1f, variance, 0.001f);
        }

        [Test]
        public void TerrainVariance_Slope_IncludesNormalDeviation()
        {
            float leftY = 0f;
            float rightY = 0f;
            Vector3 leftNormal = Vector3.up;
            Vector3 rightNormal = new Vector3(0f, 0.866f, 0.5f).normalized;

            float heightDiff = Mathf.Abs(leftY - rightY);
            float leftNormalDev = 1f - Vector3.Dot(leftNormal, Vector3.up);
            float rightNormalDev = 1f - Vector3.Dot(rightNormal, Vector3.up);
            float normalDev = Mathf.Max(leftNormalDev, rightNormalDev);
            float variance = heightDiff + normalDev * 0.1f;

            Assert.Greater(variance, 0f);
            Assert.Greater(normalDev, 0.1f);
        }

        [Test]
        public void TerrainWeight_BelowThreshold_Interpolates()
        {
            float threshold = 0.03f;
            float variance = 0.015f;

            float terrainWeight = Mathf.InverseLerp(0f, threshold, variance);
            Assert.AreEqual(0.5f, terrainWeight, 0.001f);
        }

        [Test]
        public void TerrainWeight_AboveThreshold_IsOne()
        {
            float threshold = 0.03f;
            float variance = 0.1f;

            float terrainWeight = Mathf.InverseLerp(0f, threshold, variance);
            Assert.AreEqual(1f, terrainWeight, 0.001f);
        }

        // === Phase 24: Dead Zone ===

        [Test]
        public void FootDeadZone_SmallDelta_ReducesWeight()
        {
            float deadZone = 0.02f;
            float delta = 0.01f;
            float effectiveWeight = 1f;

            float footWeight = effectiveWeight * Mathf.InverseLerp(0f, deadZone, delta);
            Assert.AreEqual(0.5f, footWeight, 0.001f);
        }

        [Test]
        public void FootDeadZone_LargeDelta_FullWeight()
        {
            float deadZone = 0.02f;
            float delta = 0.05f;
            float effectiveWeight = 1f;

            float footWeight = effectiveWeight * Mathf.InverseLerp(0f, deadZone, delta);
            Assert.AreEqual(1f, footWeight, 0.001f);
        }

        [Test]
        public void FootDeadZone_ZeroDelta_ZeroWeight()
        {
            float deadZone = 0.02f;
            float delta = 0f;
            float effectiveWeight = 1f;

            float footWeight = effectiveWeight * Mathf.InverseLerp(0f, deadZone, delta);
            Assert.AreEqual(0f, footWeight, 0.001f);
        }
    }
}
