using NUnit.Framework;
using UnityEngine;

namespace Wiesenwischer.GameKit.Camera.Tests
{
    [TestFixture]
    public class CameraStateTests
    {
        [Test]
        public void Default_ReturnsExpectedValues()
        {
            var state = CameraState.Default;

            Assert.AreEqual(0f, state.Yaw);
            Assert.AreEqual(0f, state.Pitch);
            Assert.AreEqual(5f, state.Distance);
            Assert.AreEqual(Vector3.zero, state.ShoulderOffset);
            Assert.AreEqual(60f, state.Fov);
        }

        [Test]
        public void Struct_IsValueType()
        {
            Assert.IsTrue(typeof(CameraState).IsValueType,
                "CameraState sollte ein Struct (Wertsemantik) sein");
        }

        [Test]
        public void CameraInputState_IsValueType()
        {
            Assert.IsTrue(typeof(CameraInputState).IsValueType,
                "CameraInputState sollte ein Struct sein");
        }

        [Test]
        public void Default_MultipleCalls_ReturnSameValues()
        {
            var a = CameraState.Default;
            var b = CameraState.Default;

            Assert.AreEqual(a.Yaw, b.Yaw);
            Assert.AreEqual(a.Pitch, b.Pitch);
            Assert.AreEqual(a.Distance, b.Distance);
            Assert.AreEqual(a.Fov, b.Fov);
        }
    }
}
