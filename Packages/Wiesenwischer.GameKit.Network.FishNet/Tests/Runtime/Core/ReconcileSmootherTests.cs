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

        #region Initial State

        [Test]
        public void InitialOffset_IsZero()
        {
            Assert.AreEqual(Vector3.zero, _smoother.CurrentOffset);
            Assert.AreEqual(0f, _smoother.CurrentRotationOffset, 0.001f);
        }

        [Test]
        public void IsActive_InitiallyFalse()
        {
            Assert.IsFalse(_smoother.IsActive);
        }

        [Test]
        public void SnapThreshold_ReturnsConfiguredValue()
        {
            Assert.AreEqual(2f, _smoother.SnapThreshold, 0.001f);
        }

        #endregion

        #region Tick Interpolation

        [Test]
        public void OnPreTick_SetsActiveTrue()
        {
            _smoother.OnPreTick(Vector3.zero, Quaternion.identity);
            Assert.IsTrue(_smoother.IsActive);
        }

        [Test]
        public void Reset_SetsActiveFalse()
        {
            _smoother.OnPreTick(Vector3.zero, Quaternion.identity);
            _smoother.Reset();
            Assert.IsFalse(_smoother.IsActive);
        }

        [Test]
        public void Reset_ClearsOffset()
        {
            _smoother.OnPreTick(Vector3.zero, Quaternion.identity);
            _smoother.OnReconcileComplete(Vector3.one, 0f, Vector3.zero, 0f);
            _smoother.Reset();

            Assert.AreEqual(Vector3.zero, _smoother.CurrentOffset);
            Assert.AreEqual(0f, _smoother.CurrentRotationOffset, 0.001f);
        }

        #endregion

        #region Reconcile Correction

        [Test]
        public void OnReconcileComplete_AccumulatesOffset()
        {
            _smoother.OnPreTick(Vector3.zero, Quaternion.identity);

            Vector3 preReconcile = new Vector3(10f, 0f, 5f);
            Vector3 corrected = new Vector3(10.1f, 0f, 5.05f);

            _smoother.OnReconcileComplete(preReconcile, 0f, corrected, 0f);

            Vector3 expectedOffset = preReconcile - corrected;
            Assert.AreEqual(expectedOffset.x, _smoother.CurrentOffset.x, 0.001f);
            Assert.AreEqual(expectedOffset.z, _smoother.CurrentOffset.z, 0.001f);
        }

        [Test]
        public void OnReconcileComplete_AccumulatesMultipleCorrectionss()
        {
            _smoother.OnPreTick(Vector3.zero, Quaternion.identity);

            // Erste Korrektur
            _smoother.OnReconcileComplete(
                new Vector3(10f, 0f, 0f), 0f,
                new Vector3(10.1f, 0f, 0f), 0f);

            // Zweite Korrektur — Offset akkumuliert
            _smoother.OnReconcileComplete(
                new Vector3(20f, 0f, 0f), 0f,
                new Vector3(20.05f, 0f, 0f), 0f);

            // -0.1 + -0.05 = -0.15
            Assert.AreEqual(-0.15f, _smoother.CurrentOffset.x, 0.001f);
        }

        [Test]
        public void OnReconcileComplete_SnapsOnLargeError()
        {
            _smoother.OnPreTick(Vector3.zero, Quaternion.identity);

            // Kleiner Error erst
            _smoother.OnReconcileComplete(Vector3.one, 0f, Vector3.zero, 0f);
            Assert.AreNotEqual(Vector3.zero, _smoother.CurrentOffset);

            // Grosser Error (> SnapThreshold 2m)
            _smoother.OnReconcileComplete(
                new Vector3(0f, 0f, 0f), 0f,
                new Vector3(5f, 0f, 0f), 0f);

            // Offset wurde gecleared (snap)
            Assert.AreEqual(Vector3.zero, _smoother.CurrentOffset);
        }

        [Test]
        public void OnReconcileComplete_HandlesRotation()
        {
            _smoother.OnPreTick(Vector3.zero, Quaternion.identity);

            _smoother.OnReconcileComplete(Vector3.zero, 90f, Vector3.zero, 85f);

            // DeltaAngle(85, 90) = 5
            Assert.AreEqual(5f, _smoother.CurrentRotationOffset, 0.1f);
        }

        #endregion

        #region Spectator Correction

        [Test]
        public void OnSpectatorCorrection_AccumulatesOffset()
        {
            _smoother.OnPreTick(Vector3.zero, Quaternion.identity);

            Vector3 prePos = new Vector3(5f, 0f, 3f);
            Vector3 postPos = new Vector3(5.1f, 0f, 3.05f);

            _smoother.OnSpectatorCorrection(prePos, postPos, Quaternion.identity);

            Vector3 expectedOffset = prePos - postPos;
            Assert.AreEqual(expectedOffset.x, _smoother.CurrentOffset.x, 0.001f);
            Assert.AreEqual(expectedOffset.z, _smoother.CurrentOffset.z, 0.001f);
        }

        [Test]
        public void OnSpectatorCorrection_SnapsOnLargeError()
        {
            _smoother.OnPreTick(Vector3.zero, Quaternion.identity);

            // Erst kleinen Offset aufbauen
            _smoother.OnSpectatorCorrection(Vector3.one, Vector3.zero, Quaternion.identity);
            Assert.AreNotEqual(Vector3.zero, _smoother.CurrentOffset);

            // Grosser Error
            _smoother.OnSpectatorCorrection(
                Vector3.zero, new Vector3(5f, 0f, 0f), Quaternion.identity);

            Assert.AreEqual(Vector3.zero, _smoother.CurrentOffset);
        }

        #endregion

        #region ClearOffset

        [Test]
        public void ClearOffset_SnapsToZero()
        {
            _smoother.OnPreTick(Vector3.zero, Quaternion.identity);
            _smoother.OnReconcileComplete(Vector3.one, 10f, Vector3.zero, 0f);
            _smoother.ClearOffset();

            Assert.AreEqual(Vector3.zero, _smoother.CurrentOffset);
            Assert.AreEqual(0f, _smoother.CurrentRotationOffset, 0.001f);
        }

        #endregion

        #region Visual Position Restore

        [Test]
        public void OnPostTick_RestoresVisualPositionAfterSimulate()
        {
            // Simuliert: Smoother setzt transform.position in LateUpdate
            Vector3 visualPos = new Vector3(5f, 1f, 3f);
            Quaternion visualRot = Quaternion.Euler(0f, 45f, 0f);
            _go.transform.SetPositionAndRotation(visualPos, visualRot);

            // OnPreTick initialisiert _lastSetPosition (erster Aufruf)
            _smoother.OnPreTick(visualPos, visualRot);

            // Simulate() wuerde jetzt transform.position auf TransientPosition setzen
            Vector3 simPos = new Vector3(5.2f, 1f, 3.1f);
            _go.transform.position = simPos;

            // OnPostTick muss visuelle Position wiederherstellen
            _smoother.OnPostTick(simPos, visualRot, 0.033f);

            // Transform muss zurueck auf die visuelle Position sein
            Assert.AreEqual(visualPos.x, _go.transform.position.x, 0.001f);
            Assert.AreEqual(visualPos.y, _go.transform.position.y, 0.001f);
            Assert.AreEqual(visualPos.z, _go.transform.position.z, 0.001f);
        }

        [Test]
        public void OnPostTick_DoesNotRestoreWhenNotInitialized()
        {
            Vector3 somePos = new Vector3(3f, 1f, 2f);
            _go.transform.position = somePos;

            // OnPostTick OHNE vorheriges OnPreTick → _initialized = false
            _smoother.OnPostTick(somePos, Quaternion.identity, 0.033f);

            // Transform bleibt unveraendert (kein Restore)
            Assert.AreEqual(somePos.x, _go.transform.position.x, 0.001f);
        }

        #endregion
    }
}
