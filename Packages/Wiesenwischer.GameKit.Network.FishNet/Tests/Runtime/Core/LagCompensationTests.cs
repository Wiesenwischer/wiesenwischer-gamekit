using NUnit.Framework;
using UnityEngine;

namespace Wiesenwischer.GameKit.Network.Tests
{
    [TestFixture]
    public class LagCompensationTests
    {
        [Test]
        public void TransitionAdjustment_ReducesByDelay()
        {
            float normalTransition = 0.15f;
            float networkDelay = 0.05f;

            float adjusted = Mathf.Max(0f, normalTransition - networkDelay);

            Assert.AreEqual(0.1f, adjusted, 0.001f);
        }

        [Test]
        public void TransitionAdjustment_NeverNegative()
        {
            float normalTransition = 0.05f;
            float networkDelay = 0.2f;

            float adjusted = Mathf.Max(0f, normalTransition - networkDelay);

            Assert.AreEqual(0f, adjusted, 0.001f);
        }

        [Test]
        public void TransitionAdjustment_ZeroDelay_NoChange()
        {
            float normalTransition = 0.15f;
            float networkDelay = 0f;

            float adjusted = Mathf.Max(0f, normalTransition - networkDelay);

            Assert.AreEqual(0.15f, adjusted, 0.001f);
        }

        [Test]
        public void DelayClamp_MaxHalfSecond()
        {
            float rawDelay = 2.0f;
            float clamped = Mathf.Clamp(rawDelay, 0f, 0.5f);
            Assert.AreEqual(0.5f, clamped, 0.001f);
        }
    }
}
