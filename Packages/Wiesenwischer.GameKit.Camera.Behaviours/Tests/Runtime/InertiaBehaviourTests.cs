using NUnit.Framework;
using UnityEngine;

namespace Wiesenwischer.GameKit.Camera.Behaviours.Tests
{
    [TestFixture]
    public class InertiaBehaviourTests
    {
        private InertiaBehaviour _inertia;
        private CameraContext _ctx;
        private GameObject _go;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("InertiaTest");
            _inertia = _go.AddComponent<InertiaBehaviour>();
            _ctx = new CameraContext
            {
                DeltaTime = 0.016f,
                AnchorPosition = Vector3.zero
            };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void UpdateState_FirstFrame_InitializesPosition()
        {
            var state = CameraState.Default;
            _ctx.AnchorPosition = new Vector3(1f, 2f, 3f);

            _inertia.UpdateState(ref state, _ctx);

            // First frame initializes, returns early
            Assert.AreEqual(new Vector3(1f, 2f, 3f), _ctx.AnchorPosition);
        }

        [Test]
        public void UpdateState_TargetMoved_PositionFollowsWithLag()
        {
            var state = CameraState.Default;
            _ctx.AnchorPosition = Vector3.zero;
            _inertia.UpdateState(ref state, _ctx); // Initialize

            // Move target
            _ctx.AnchorPosition = new Vector3(5f, 0f, 0f);
            _inertia.UpdateState(ref state, _ctx);

            // Position should follow but not be at target yet
            float distance = Vector3.Distance(_ctx.AnchorPosition, new Vector3(5f, 0f, 0f));
            Assert.Greater(distance, 0.01f, "Position should lag behind target");
        }

        [Test]
        public void Snap_SetsPositionImmediately()
        {
            var state = CameraState.Default;
            _ctx.AnchorPosition = Vector3.zero;
            _inertia.UpdateState(ref state, _ctx); // Initialize

            Vector3 snapPos = new Vector3(10f, 5f, 3f);
            _inertia.Snap(snapPos);

            _ctx.AnchorPosition = snapPos;
            _inertia.UpdateState(ref state, _ctx);

            // After snap + update with same position, should be at target
            Assert.AreEqual(snapPos.x, _ctx.AnchorPosition.x, 0.01f);
            Assert.AreEqual(snapPos.y, _ctx.AnchorPosition.y, 0.01f);
            Assert.AreEqual(snapPos.z, _ctx.AnchorPosition.z, 0.01f);
        }

        [Test]
        public void UpdateState_Idle_ConvergesToTarget()
        {
            var state = CameraState.Default;
            Vector3 target = new Vector3(3f, 0f, 0f);

            // Initialize at origin
            _ctx.AnchorPosition = Vector3.zero;
            _inertia.UpdateState(ref state, _ctx);

            // Move target and run many frames
            _ctx.AnchorPosition = target;
            for (int i = 0; i < 500; i++)
                _inertia.UpdateState(ref state, _ctx);

            // Should have converged to target
            float distance = Vector3.Distance(_ctx.AnchorPosition, target);
            Assert.Less(distance, 0.05f, "Should converge to target after many frames");
        }
    }
}
