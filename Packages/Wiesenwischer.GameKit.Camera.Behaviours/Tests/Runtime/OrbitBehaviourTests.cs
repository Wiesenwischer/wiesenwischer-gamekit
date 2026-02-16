using NUnit.Framework;
using UnityEngine;

namespace Wiesenwischer.GameKit.Camera.Behaviours.Tests
{
    [TestFixture]
    public class OrbitBehaviourTests
    {
        private OrbitBehaviour _orbit;
        private CameraContext _ctx;
        private GameObject _go;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("OrbitTest");
            _orbit = _go.AddComponent<OrbitBehaviour>();
            _ctx = new CameraContext { DeltaTime = 0.016f };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void UpdateState_AddsLookXToYaw()
        {
            var state = CameraState.Default;
            float initialYaw = state.Yaw;
            _ctx.Input = new CameraInputState { LookX = 5f, OrbitMode = CameraOrbitMode.FreeOrbit };

            _orbit.UpdateState(ref state, _ctx);

            Assert.AreEqual(initialYaw + 5f, state.Yaw, 0.001f);
        }

        [Test]
        public void UpdateState_SubtractsLookYFromPitch()
        {
            var state = CameraState.Default;
            float initialPitch = state.Pitch;
            _ctx.Input = new CameraInputState { LookY = 10f, OrbitMode = CameraOrbitMode.FreeOrbit };

            _orbit.UpdateState(ref state, _ctx);

            Assert.AreEqual(initialPitch - 10f, state.Pitch, 0.001f);
        }

        [Test]
        public void UpdateState_ClampsPitch()
        {
            var state = CameraState.Default;
            _ctx.Input = new CameraInputState { LookY = -200f, OrbitMode = CameraOrbitMode.FreeOrbit };

            _orbit.UpdateState(ref state, _ctx);

            // Default maxPitch=70, so pitch should be clamped
            Assert.LessOrEqual(state.Pitch, 70f);
            Assert.GreaterOrEqual(state.Pitch, -40f);
        }

        [Test]
        public void UpdateState_NoInput_NoChange()
        {
            var state = CameraState.Default;
            float initialYaw = state.Yaw;
            float initialPitch = state.Pitch;
            _ctx.Input = new CameraInputState { LookX = 0f, LookY = 0f, OrbitMode = CameraOrbitMode.FreeOrbit };

            _orbit.UpdateState(ref state, _ctx);

            Assert.AreEqual(initialYaw, state.Yaw, 0.001f);
            Assert.AreEqual(initialPitch, state.Pitch, 0.001f);
        }

        [Test]
        public void OrbitModeNone_IgnoresInput()
        {
            var state = CameraState.Default;
            float initialYaw = state.Yaw;
            float initialPitch = state.Pitch;
            _ctx.Input = new CameraInputState { LookX = 10f, LookY = 5f, OrbitMode = CameraOrbitMode.None };

            _orbit.UpdateState(ref state, _ctx);

            Assert.AreEqual(initialYaw, state.Yaw, 0.001f);
            Assert.AreEqual(initialPitch, state.Pitch, 0.001f);
        }

        [Test]
        public void SteerOrbit_AppliesInput()
        {
            var state = CameraState.Default;
            float initialYaw = state.Yaw;
            _ctx.Input = new CameraInputState { LookX = 5f, OrbitMode = CameraOrbitMode.SteerOrbit };

            _orbit.UpdateState(ref state, _ctx);

            Assert.AreEqual(initialYaw + 5f, state.Yaw, 0.001f);
        }

        [Test]
        public void FreeOrbit_AppliesInput()
        {
            var state = CameraState.Default;
            float initialYaw = state.Yaw;
            _ctx.Input = new CameraInputState { LookX = 5f, OrbitMode = CameraOrbitMode.FreeOrbit };

            _orbit.UpdateState(ref state, _ctx);

            Assert.AreEqual(initialYaw + 5f, state.Yaw, 0.001f);
        }
    }
}
