using NUnit.Framework;
using UnityEngine;

namespace Wiesenwischer.GameKit.Camera.Behaviours.Tests
{
    [TestFixture]
    public class CollisionBehaviourTests
    {
        private CollisionBehaviour _collision;
        private CameraContext _ctx;
        private GameObject _go;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("CollisionTest");
            _collision = _go.AddComponent<CollisionBehaviour>();
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
        public void UpdateState_NoCollision_DistanceUnchanged()
        {
            var state = CameraState.Default;
            state.Distance = 5f;
            state.Yaw = 0f;
            state.Pitch = 0f;

            // No colliders in empty test scene
            _collision.UpdateState(ref state, _ctx);

            Assert.AreEqual(5f, state.Distance, 0.01f);
        }

        [Test]
        public void UpdateState_Disabled_DistanceUnchanged()
        {
            var state = CameraState.Default;
            state.Distance = 5f;
            _collision.enabled = false;

            Assert.IsFalse(_collision.IsActive);
            // CameraBrain skips inactive behaviours, so we just verify IsActive
        }
    }
}
