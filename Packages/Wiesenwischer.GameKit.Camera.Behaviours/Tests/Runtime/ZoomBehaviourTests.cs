using NUnit.Framework;
using UnityEngine;

namespace Wiesenwischer.GameKit.Camera.Behaviours.Tests
{
    [TestFixture]
    public class ZoomBehaviourTests
    {
        private ZoomBehaviour _zoom;
        private CameraContext _ctx;
        private GameObject _go;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("ZoomTest");
            _zoom = _go.AddComponent<ZoomBehaviour>();
            _ctx = new CameraContext { DeltaTime = 0.016f };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void UpdateState_ZoomIn_ReducesDistance()
        {
            var state = CameraState.Default;
            state.Distance = 10f;
            _ctx.Input = new CameraInputState { Zoom = 2f };

            _zoom.UpdateState(ref state, _ctx);

            Assert.Less(state.Distance, 10f);
        }

        [Test]
        public void UpdateState_ClampsToMinDistance()
        {
            var state = CameraState.Default;
            state.Distance = 2f;
            _ctx.Input = new CameraInputState { Zoom = 100f };

            // Run multiple frames to converge
            for (int i = 0; i < 100; i++)
                _zoom.UpdateState(ref state, _ctx);

            // Default minDistance=2
            Assert.GreaterOrEqual(state.Distance, 2f - 0.01f);
        }

        [Test]
        public void UpdateState_ClampsToMaxDistance()
        {
            var state = CameraState.Default;
            state.Distance = 14f;
            _ctx.Input = new CameraInputState { Zoom = -100f };

            for (int i = 0; i < 100; i++)
                _zoom.UpdateState(ref state, _ctx);

            // Default maxDistance=15
            Assert.LessOrEqual(state.Distance, 15f + 0.01f);
        }

        [Test]
        public void InitializeState_SetsDefaultDistance()
        {
            var state = CameraState.Default;
            state.Distance = 999f;

            ((ICameraStateInitializer)_zoom).InitializeState(ref state);

            // Default _defaultDistance=5
            Assert.AreEqual(5f, state.Distance, 0.001f);
        }
    }
}
