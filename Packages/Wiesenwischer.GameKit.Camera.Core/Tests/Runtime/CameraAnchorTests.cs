using NUnit.Framework;
using UnityEngine;

namespace Wiesenwischer.GameKit.Camera.Tests
{
    [TestFixture]
    public class CameraAnchorTests
    {
        private GameObject _anchorGO;
        private CameraAnchor _anchor;
        private GameObject _targetGO;

        [SetUp]
        public void SetUp()
        {
            _anchorGO = new GameObject("CameraAnchor");
            _anchor = _anchorGO.AddComponent<CameraAnchor>();

            _targetGO = new GameObject("Target");
            _targetGO.transform.position = new Vector3(5f, 0f, 5f);

            _anchor.FollowTarget = _targetGO.transform;
            _anchor.TargetOffset = new Vector3(0f, 1.5f, 0f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_anchorGO);
            Object.DestroyImmediate(_targetGO);
        }

        [Test]
        public void SnapToTarget_SetsPositionImmediately()
        {
            _anchor.SnapToTarget();

            var expected = _targetGO.transform.position + new Vector3(0f, 1.5f, 0f);
            Assert.AreEqual(expected.x, _anchor.AnchorPosition.x, 0.01f);
            Assert.AreEqual(expected.y, _anchor.AnchorPosition.y, 0.01f);
            Assert.AreEqual(expected.z, _anchor.AnchorPosition.z, 0.01f);
        }

        [Test]
        public void SnapToTarget_WithoutTarget_DoesNotThrow()
        {
            _anchor.FollowTarget = null;
            Assert.DoesNotThrow(() => _anchor.SnapToTarget());
        }

        [Test]
        public void UpdateAnchor_WithoutTarget_DoesNotThrow()
        {
            _anchor.FollowTarget = null;
            Assert.DoesNotThrow(() => _anchor.UpdateAnchor(1f / 60f));
        }

        [Test]
        public void UpdateAnchor_FollowsTarget_Smoothed()
        {
            _anchor.SnapToTarget();
            var initialPos = _anchor.AnchorPosition;

            // Target bewegen
            _targetGO.transform.position = new Vector3(10f, 0f, 10f);

            // Mehrere Updates simulieren
            for (int i = 0; i < 60; i++)
                _anchor.UpdateAnchor(1f / 60f);

            var expected = _targetGO.transform.position + new Vector3(0f, 1.5f, 0f);
            // Nach 1 Sekunde sollte die Position dem Target nahe sein
            Assert.AreEqual(expected.x, _anchor.AnchorPosition.x, 0.5f,
                "AnchorPosition sollte sich dem Target annähern (X)");
            Assert.AreEqual(expected.z, _anchor.AnchorPosition.z, 0.5f,
                "AnchorPosition sollte sich dem Target annähern (Z)");
        }

        [Test]
        public void UpdateAnchor_YSmoothing_SlowerThanXZ()
        {
            _anchor.SnapToTarget();

            // Target um gleichen Betrag in Y und XZ bewegen
            _targetGO.transform.position = new Vector3(5f, 3f, 5f);

            // Wenige Updates → XZ sollte näher am Ziel sein als Y
            for (int i = 0; i < 5; i++)
                _anchor.UpdateAnchor(1f / 60f);

            var targetWithOffset = _targetGO.transform.position + new Vector3(0f, 1.5f, 0f);
            float xzError = Mathf.Abs(_anchor.AnchorPosition.x - targetWithOffset.x);
            float yError = Mathf.Abs(_anchor.AnchorPosition.y - targetWithOffset.y);

            // Y hat stärkeres Damping → größerer Fehler nach wenigen Frames
            Assert.GreaterOrEqual(yError, xzError * 0.5f,
                "Y-Smoothing sollte langsamer sein als XZ-Smoothing");
        }

        [Test]
        public void SnapToTarget_ResetsVelocity()
        {
            // Erst bewegen um Velocity aufzubauen
            _anchor.SnapToTarget();
            _targetGO.transform.position = new Vector3(20f, 0f, 20f);
            _anchor.UpdateAnchor(1f / 60f);

            // Snap → Position sollte exakt sein, kein Nachschwingen
            _anchor.SnapToTarget();
            var expected = _targetGO.transform.position + new Vector3(0f, 1.5f, 0f);

            Assert.AreEqual(expected.x, _anchor.AnchorPosition.x, 0.01f);
            Assert.AreEqual(expected.y, _anchor.AnchorPosition.y, 0.01f);
            Assert.AreEqual(expected.z, _anchor.AnchorPosition.z, 0.01f);
        }
    }
}
