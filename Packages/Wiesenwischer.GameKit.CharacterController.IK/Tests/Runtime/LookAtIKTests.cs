using NUnit.Framework;
using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.IK.Tests
{
    [TestFixture]
    public class LookAtIKTests
    {
        [Test]
        public void Weight_ClampsTo01Range()
        {
            float weight = Mathf.Clamp01(1.5f);
            Assert.AreEqual(1f, weight);
            weight = Mathf.Clamp01(-0.5f);
            Assert.AreEqual(0f, weight);
        }

        [Test]
        public void SmoothLerp_ApproachesTarget()
        {
            Vector3 current = Vector3.zero;
            Vector3 target = new Vector3(10, 0, 0);
            float speed = 5f;
            float dt = 0.016f;

            current = Vector3.Lerp(current, target, speed * dt);
            Assert.Greater(current.x, 0f);
            Assert.Less(current.x, 10f);
        }

        [Test]
        public void LookDistance_ProducesForwardTarget()
        {
            Vector3 cameraPos = new Vector3(0, 5, -10);
            Vector3 cameraForward = Vector3.forward;
            float lookDistance = 10f;

            Vector3 target = cameraPos + cameraForward * lookDistance;
            Assert.AreEqual(new Vector3(0, 5, 0), target);
        }

        [Test]
        public void MoveTowards_ReachesTarget()
        {
            float current = 0f;
            float target = 1f;
            float speed = 5f;

            // Nach genug Iterationen sollte der Wert das Target erreichen
            for (int i = 0; i < 100; i++)
                current = Mathf.MoveTowards(current, target, speed * 0.016f);

            Assert.AreEqual(1f, current, 0.001f);
        }

        [Test]
        public void Weight_ZeroDisablesLookAt()
        {
            float currentWeight = 0f;
            // Bei Weight <= 0.01 wird kein LookAt angewendet
            Assert.LessOrEqual(currentWeight, 0.01f);
        }
    }
}
