using NUnit.Framework;
using UnityEngine;

namespace Wiesenwischer.GameKit.Camera.Tests
{
    [TestFixture]
    public class PivotRigTests
    {
        private GameObject _rootGO;
        private PivotRig _rig;

        [SetUp]
        public void SetUp()
        {
            _rootGO = new GameObject("CameraRoot");
            _rig = _rootGO.AddComponent<PivotRig>();
            _rig.EnsureHierarchy();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_rootGO);
        }

        [Test]
        public void EnsureHierarchy_CreatesAllPivots()
        {
            Assert.IsNotNull(_rig.CameraTransform, "CameraTransform sollte erstellt werden");
            Assert.IsNotNull(_rig.Root, "Root sollte existieren");

            // Hierarchie prüfen: Root → Yaw → Pitch → Offset → Camera
            var yaw = _rig.Root.Find("YawPivot");
            Assert.IsNotNull(yaw, "YawPivot sollte existieren");

            var pitch = yaw.Find("PitchPivot");
            Assert.IsNotNull(pitch, "PitchPivot sollte existieren");

            var offset = pitch.Find("OffsetPivot");
            Assert.IsNotNull(offset, "OffsetPivot sollte existieren");
        }

        [Test]
        public void EnsureHierarchy_CalledTwice_DoesNotDuplicate()
        {
            int childCountBefore = _rig.Root.childCount;
            _rig.EnsureHierarchy();
            int childCountAfter = _rig.Root.childCount;

            Assert.AreEqual(childCountBefore, childCountAfter,
                "Erneuter EnsureHierarchy-Aufruf sollte keine Duplikate erzeugen");
        }

        [Test]
        public void ApplyState_SetsYawRotation()
        {
            var state = CameraState.Default;
            state.Yaw = 90f;

            _rig.ApplyState(state, Vector3.zero);

            var yaw = _rig.Root.Find("YawPivot");
            Assert.AreEqual(90f, yaw.localEulerAngles.y, 0.01f);
        }

        [Test]
        public void ApplyState_SetsPitchRotation()
        {
            var state = CameraState.Default;
            state.Pitch = -30f;

            _rig.ApplyState(state, Vector3.zero);

            var yaw = _rig.Root.Find("YawPivot");
            var pitch = yaw.Find("PitchPivot");
            // -30 Grad wird als 330 in eulerAngles dargestellt
            Assert.AreEqual(330f, pitch.localEulerAngles.x, 0.01f);
        }

        [Test]
        public void ApplyState_SetsDistance()
        {
            var state = CameraState.Default;
            state.Distance = 8f;

            _rig.ApplyState(state, Vector3.zero);

            Assert.AreEqual(-8f, _rig.CameraTransform.localPosition.z, 0.01f);
        }

        [Test]
        public void ApplyState_SetsShoulderOffset()
        {
            var state = CameraState.Default;
            state.ShoulderOffset = new Vector3(0.5f, 0f, 0f);

            _rig.ApplyState(state, Vector3.zero);

            var yaw = _rig.Root.Find("YawPivot");
            var pitch = yaw.Find("PitchPivot");
            var offset = pitch.Find("OffsetPivot");
            Assert.AreEqual(0.5f, offset.localPosition.x, 0.01f);
        }

        [Test]
        public void ApplyState_SetsAnchorPosition()
        {
            var state = CameraState.Default;
            var anchorPos = new Vector3(1f, 2f, 3f);

            _rig.ApplyState(state, anchorPos);

            Assert.AreEqual(anchorPos.x, _rig.Root.position.x, 0.01f);
            Assert.AreEqual(anchorPos.y, _rig.Root.position.y, 0.01f);
            Assert.AreEqual(anchorPos.z, _rig.Root.position.z, 0.01f);
        }

        [Test]
        public void GetCameraForward_DefaultState_ReturnsForward()
        {
            var state = CameraState.Default;
            _rig.ApplyState(state, Vector3.zero);

            var forward = _rig.GetCameraForward();

            // Bei Yaw=0, Pitch=0 sollte Forward ≈ (0,0,1) sein
            Assert.AreEqual(0f, forward.x, 0.01f);
            Assert.AreEqual(0f, forward.y, 0.01f);
            Assert.AreEqual(1f, forward.z, 0.01f);
        }
    }
}
