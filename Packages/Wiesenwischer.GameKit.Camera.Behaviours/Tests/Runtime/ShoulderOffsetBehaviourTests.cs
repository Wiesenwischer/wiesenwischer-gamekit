using NUnit.Framework;
using UnityEngine;

namespace Wiesenwischer.GameKit.Camera.Behaviours.Tests
{
    [TestFixture]
    public class ShoulderOffsetBehaviourTests
    {
        private ShoulderOffsetBehaviour _shoulder;
        private CameraContext _ctx;
        private GameObject _go;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("ShoulderTest");
            _shoulder = _go.AddComponent<ShoulderOffsetBehaviour>();
            _ctx = new CameraContext { DeltaTime = 0.016f };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void UpdateState_SetsShoulderOffset()
        {
            var state = CameraState.Default;

            // Run a few frames to converge (SmoothDamp)
            for (int i = 0; i < 100; i++)
                _shoulder.UpdateState(ref state, _ctx);

            // Default: right shoulder, offsetX=0.5
            Assert.Greater(state.ShoulderOffset.x, 0.4f);
        }

        [Test]
        public void SwitchSide_NegatesOffsetX()
        {
            var state = CameraState.Default;

            // Converge to right shoulder
            for (int i = 0; i < 100; i++)
                _shoulder.UpdateState(ref state, _ctx);

            float rightOffset = state.ShoulderOffset.x;
            Assert.Greater(rightOffset, 0f, "Should be on right side initially");

            // Switch to left
            _shoulder.SwitchSide();
            Assert.IsFalse(_shoulder.IsRightShoulder);

            // Converge to left
            for (int i = 0; i < 100; i++)
                _shoulder.UpdateState(ref state, _ctx);

            Assert.Less(state.ShoulderOffset.x, 0f, "Should be on left side after switch");
        }

        [Test]
        public void SwitchSide_SmoothTransition()
        {
            var state = CameraState.Default;

            // Converge to right shoulder
            for (int i = 0; i < 100; i++)
                _shoulder.UpdateState(ref state, _ctx);

            float rightOffset = state.ShoulderOffset.x;

            // Switch to left and update only a few frames
            _shoulder.SwitchSide();
            _shoulder.UpdateState(ref state, _ctx);
            _shoulder.UpdateState(ref state, _ctx);

            // Should be between right and left (smooth transition)
            Assert.Less(state.ShoulderOffset.x, rightOffset, "Should have started transitioning");
            Assert.Greater(state.ShoulderOffset.x, -rightOffset, "Should not have finished transitioning yet");
        }
    }
}
