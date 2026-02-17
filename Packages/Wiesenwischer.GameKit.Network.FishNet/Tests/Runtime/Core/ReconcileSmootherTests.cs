using NUnit.Framework;
using UnityEngine;

namespace Wiesenwischer.GameKit.Network.Tests
{
    [TestFixture]
    public class ReconcileSmootherTests
    {
        private GameObject _go;
        private ReconcileSmoother _smoother;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestSmoother");
            _smoother = _go.AddComponent<ReconcileSmoother>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void SetCorrectionOffset_AccumulatesOffset()
        {
            Vector3 error = new Vector3(0.5f, 0.1f, -0.3f);
            _smoother.SetCorrectionOffset(error, 5f);

            Assert.AreEqual(error, _smoother.CurrentOffset);
            Assert.AreEqual(5f, _smoother.CurrentRotationOffset, 0.001f);
        }

        [Test]
        public void ClearOffset_SnapsToZero()
        {
            _smoother.SetCorrectionOffset(new Vector3(1f, 2f, 3f), 10f);
            _smoother.ClearOffset();

            Assert.AreEqual(Vector3.zero, _smoother.CurrentOffset);
            Assert.AreEqual(0f, _smoother.CurrentRotationOffset, 0.001f);
        }

        [Test]
        public void SnapThreshold_ReturnsConfiguredValue()
        {
            // Default-Wert aus SerializeField ist 2.0
            Assert.AreEqual(2f, _smoother.SnapThreshold, 0.001f);
        }

        [Test]
        public void InitialOffset_IsZero()
        {
            Assert.AreEqual(Vector3.zero, _smoother.CurrentOffset);
            Assert.AreEqual(0f, _smoother.CurrentRotationOffset, 0.001f);
        }

        [Test]
        public void SetCorrectionOffset_AccumulatesMultipleCalls()
        {
            _smoother.SetCorrectionOffset(new Vector3(1f, 0f, 0f), 5f);
            _smoother.SetCorrectionOffset(new Vector3(0f, 0f, 2f), 10f);

            Assert.AreEqual(new Vector3(1f, 0f, 2f), _smoother.CurrentOffset);
            Assert.AreEqual(15f, _smoother.CurrentRotationOffset, 0.001f);
        }
    }
}
