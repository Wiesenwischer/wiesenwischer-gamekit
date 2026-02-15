using NUnit.Framework;
using UnityEngine;

namespace Wiesenwischer.GameKit.Camera.Behaviours.Tests
{
    [TestFixture]
    public class RecenterBehaviourTests
    {
        private RecenterBehaviour _recenter;
        private CameraContext _ctx;
        private GameObject _go;
        private GameObject _target;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("RecenterTest");
            _recenter = _go.AddComponent<RecenterBehaviour>();
            _target = new GameObject("Target");
            _ctx = new CameraContext
            {
                DeltaTime = 0.016f,
                FollowTarget = _target.transform
            };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_target);
        }

        [Test]
        public void UpdateState_WithLookInput_DoesNotRecenter()
        {
            var state = CameraState.Default;
            state.Yaw = 45f;
            _ctx.Input = new CameraInputState { LookX = 1f };

            _recenter.UpdateState(ref state, _ctx);

            Assert.AreEqual(45f, state.Yaw, 0.001f);
        }

        [Test]
        public void UpdateState_NoMovement_DoesNotRecenter()
        {
            var state = CameraState.Default;
            state.Yaw = 45f;
            _ctx.Input = default;
            _target.transform.position = Vector3.zero;

            // Wait out the delay (default 1.5s)
            for (int i = 0; i < 200; i++) // 200 * 0.016 = 3.2s
                _recenter.UpdateState(ref state, _ctx);

            // Target didn't move → no recenter
            Assert.AreEqual(45f, state.Yaw, 0.001f);
        }

        [Test]
        public void UpdateState_AfterDelay_RecentersToMovement()
        {
            var state = CameraState.Default;
            state.Yaw = 90f; // Looking east
            _ctx.Input = default;
            _target.transform.position = Vector3.zero;

            // First pass: wait out the delay (1.5s)
            for (int i = 0; i < 120; i++) // ~1.9s
                _recenter.UpdateState(ref state, _ctx);

            // Now move target forward (+Z direction, targetYaw = 0)
            for (int i = 0; i < 100; i++)
            {
                _target.transform.position += Vector3.forward * 0.1f;
                _recenter.UpdateState(ref state, _ctx);
            }

            // Yaw should have moved towards 0 (forward direction)
            Assert.Less(state.Yaw, 90f, "Yaw should recenter towards movement direction");
        }
    }
}
