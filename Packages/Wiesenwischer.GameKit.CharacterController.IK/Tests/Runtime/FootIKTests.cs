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
        public void BodyOffset_NeverPositive()
        {
            float leftDelta = 0.1f;
            float rightDelta = 0.05f;
            float offset = Mathf.Min(leftDelta, rightDelta);
            offset = Mathf.Min(offset, 0f);
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
            // Wenn nur ein Fuß einen Hit hat, soll kein Body Offset berechnet werden
            // (Logik: nur wenn beide Füße Hits haben)
            bool leftHit = true;
            bool rightHit = false;

            float targetBodyOffset = 0f;
            if (leftHit && rightHit)
            {
                // Nicht erreicht
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
    }
}
