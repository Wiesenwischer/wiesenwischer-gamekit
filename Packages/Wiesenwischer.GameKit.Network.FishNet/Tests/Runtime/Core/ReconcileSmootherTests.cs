using NUnit.Framework;
using UnityEngine;

namespace Wiesenwischer.GameKit.Network.Tests
{
    [TestFixture]
    public class ReconcileSmootherTests
    {
        private const float TickDelta = 0.033f;

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

        /// <summary>
        /// Helper: Fuehrt die minimale Warmup-Sequenz durch (2 Ticks).
        /// Nach Warmup: displayStart=pos0, displayEnd=pos1, initialized=true.
        /// </summary>
        private void DoWarmup(Vector3 pos0, Vector3 pos1)
        {
            // Tick 0: Initialisierung
            _smoother.OnPreTick(pos0, Quaternion.identity, TickDelta);
            _smoother.OnPostTick(pos1, Quaternion.identity, TickDelta);
            // Tick 1: Buffer-Shift, Initialisierung abgeschlossen
            _smoother.OnPreTick(pos1, Quaternion.identity, TickDelta);
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

        #region Warmup

        [Test]
        public void OnPreTick_FirstCall_DoesNotActivate()
        {
            _smoother.OnPreTick(Vector3.zero, Quaternion.identity, TickDelta);
            Assert.IsFalse(_smoother.IsActive);
        }

        [Test]
        public void OnPreTick_SecondCall_Activates()
        {
            // Tick 0
            _smoother.OnPreTick(Vector3.zero, Quaternion.identity, TickDelta);
            _smoother.OnPostTick(Vector3.forward, Quaternion.identity, TickDelta);
            // Tick 1: Buffer-Shift → initialized
            _smoother.OnPreTick(Vector3.forward, Quaternion.identity, TickDelta);
            Assert.IsTrue(_smoother.IsActive);
        }

        [Test]
        public void Reset_SetsActiveFalse()
        {
            DoWarmup(Vector3.zero, Vector3.forward);
            _smoother.Reset();
            Assert.IsFalse(_smoother.IsActive);
        }

        [Test]
        public void Reset_ClearsOffset()
        {
            DoWarmup(Vector3.zero, Vector3.forward);
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
            DoWarmup(Vector3.zero, Vector3.forward);

            Vector3 preReconcile = new Vector3(10f, 0f, 5f);
            Vector3 corrected = new Vector3(10.1f, 0f, 5.05f);

            _smoother.OnReconcileComplete(preReconcile, 0f, corrected, 0f);

            Vector3 expectedOffset = preReconcile - corrected;
            Assert.AreEqual(expectedOffset.x, _smoother.CurrentOffset.x, 0.001f);
            Assert.AreEqual(expectedOffset.z, _smoother.CurrentOffset.z, 0.001f);
        }

        [Test]
        public void OnReconcileComplete_AccumulatesMultipleCorrections()
        {
            DoWarmup(Vector3.zero, Vector3.forward);

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
            DoWarmup(Vector3.zero, Vector3.forward);

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
            DoWarmup(Vector3.zero, Vector3.forward);

            _smoother.OnReconcileComplete(Vector3.zero, 90f, Vector3.zero, 85f);

            // DeltaAngle(85, 90) = 5
            Assert.AreEqual(5f, _smoother.CurrentRotationOffset, 0.1f);
        }

        [Test]
        public void OnReconcileComplete_BufferShiftPreservesVisual()
        {
            // Verifies: displayStart + offset = unchanged after correction.
            // This prevents visual jump at the moment of reconcile.
            Vector3 pos0 = new Vector3(5f, 0f, 0f);
            Vector3 pos1 = new Vector3(6f, 0f, 0f);
            DoWarmup(pos0, pos1);
            // displayStart = pos0 = (5,0,0), displayEnd = pos1 = (6,0,0)

            // OnPostTick sets visual = displayStart + offset (offset=0) = (5,0,0)
            _smoother.OnPostTick(new Vector3(7f, 0f, 0f), Quaternion.identity, TickDelta);
            Vector3 visualBefore = _go.transform.position; // (5,0,0)

            // Correction: pre=10, corrected=10.1
            _smoother.OnReconcileComplete(
                new Vector3(10f, 0f, 0f), 0f,
                new Vector3(10.1f, 0f, 0f), 0f);
            // correction = (0.1,0,0), displayStart shifted to (5.1,0,0), offset = (-0.1,0,0)

            // visual at factor=0 after correction: displayStart + offset = 5.1 + (-0.1) = 5.0
            _smoother.OnPostTick(new Vector3(7.1f, 0f, 0f), Quaternion.identity, TickDelta);
            Vector3 visualAfter = _go.transform.position;

            Assert.AreEqual(visualBefore.x, visualAfter.x, 0.001f);
        }

        #endregion

        #region Spectator Correction

        [Test]
        public void OnSpectatorCorrection_AccumulatesOffset()
        {
            DoWarmup(Vector3.zero, Vector3.forward);

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
            DoWarmup(Vector3.zero, Vector3.forward);

            // Erst kleinen Offset aufbauen
            _smoother.OnSpectatorCorrection(Vector3.one, Vector3.zero, Quaternion.identity);
            Assert.AreNotEqual(Vector3.zero, _smoother.CurrentOffset);

            // Grosser Error
            _smoother.OnSpectatorCorrection(
                Vector3.zero, new Vector3(5f, 0f, 0f), Quaternion.identity);

            Assert.AreEqual(Vector3.zero, _smoother.CurrentOffset);
        }

        [Test]
        public void OnSpectatorCorrection_NoVisualJump()
        {
            // Regression: Ohne Buffer-Shift wuerde die Correction das Visual springen lassen.
            // Mit Buffer-Shift hebt sich die Verschiebung mit dem Offset auf → kein Sprung.
            Vector3 pos0 = new Vector3(9f, 0f, 0f);
            Vector3 pos1 = new Vector3(10f, 0f, 0f);
            DoWarmup(pos0, pos1);
            // displayStart = pos0, displayEnd = pos1

            // Set visual via OnPostTick
            _smoother.OnPostTick(new Vector3(11f, 0f, 0f), Quaternion.identity, TickDelta);
            Vector3 visualBefore = _go.transform.position; // displayStart + offset = (9,0,0)

            // Spectator correction
            _smoother.OnSpectatorCorrection(
                new Vector3(11f, 0f, 0f),
                new Vector3(11.05f, 0f, 0f),
                Quaternion.identity);
            // correction = (0.05,0,0), displayStart → (9.05,0,0), offset = (-0.05,0,0)

            // Check via OnPostTick
            _smoother.OnPostTick(new Vector3(11.05f, 0f, 0f), Quaternion.identity, TickDelta);
            Vector3 visualAfter = _go.transform.position;

            // displayStart + offset = 9.05 + (-0.05) = 9.0 = unchanged
            Assert.AreEqual(visualBefore.x, visualAfter.x, 0.001f);
        }

        #endregion

        #region ClearOffset

        [Test]
        public void ClearOffset_SnapsToZero()
        {
            DoWarmup(Vector3.zero, Vector3.forward);
            _smoother.OnReconcileComplete(Vector3.one, 10f, Vector3.zero, 0f);
            _smoother.ClearOffset();

            Assert.AreEqual(Vector3.zero, _smoother.CurrentOffset);
            Assert.AreEqual(0f, _smoother.CurrentRotationOffset, 0.001f);
        }

        #endregion

        #region OnPostTick Behavior

        [Test]
        public void OnPostTick_SetsVisualToDisplayStart()
        {
            // After warmup: displayStart = pos0. factor=0 at OnPostTick time.
            // Visual should be at displayStart (one tick behind).
            Vector3 pos0 = new Vector3(5f, 1f, 3f);
            Vector3 pos1 = new Vector3(5.1f, 1f, 3.05f);
            DoWarmup(pos0, pos1);

            // Simulate: motor moves to new position
            Vector3 simPos = new Vector3(5.2f, 1f, 3.1f);
            _go.transform.position = simPos;

            _smoother.OnPostTick(simPos, Quaternion.identity, TickDelta);

            // Transform muss auf displayStart sein (= pos0, one tick behind)
            Assert.AreEqual(pos0.x, _go.transform.position.x, 0.001f);
            Assert.AreEqual(pos0.y, _go.transform.position.y, 0.001f);
            Assert.AreEqual(pos0.z, _go.transform.position.z, 0.001f);
        }

        [Test]
        public void OnPostTick_IncludesCorrectionOffset()
        {
            Vector3 pos0 = new Vector3(5f, 1f, 3f);
            Vector3 pos1 = new Vector3(5.1f, 1f, 3.05f);
            DoWarmup(pos0, pos1);

            // Reconcile Correction: 0.1m Error
            _smoother.OnReconcileComplete(
                new Vector3(5f, 1f, 3f), 0f,
                new Vector3(4.9f, 1f, 3f), 0f);

            // offset = (5,1,3) - (4.9,1,3) = (0.1, 0, 0)
            // correction = (4.9,1,3) - (5,1,3) = (-0.1, 0, 0)
            // displayStart shifted: pos0 + (-0.1, 0, 0) = (4.9, 1, 3)

            _go.transform.position = new Vector3(5.2f, 1f, 3.1f);
            _smoother.OnPostTick(new Vector3(5.2f, 1f, 3.1f), Quaternion.identity, TickDelta);

            // visual = displayStart + offset = (4.9,1,3) + (0.1,0,0) = (5.0, 1, 3)
            Assert.AreEqual(5.0f, _go.transform.position.x, 0.001f);
            Assert.AreEqual(1.0f, _go.transform.position.y, 0.001f);
            Assert.AreEqual(3.0f, _go.transform.position.z, 0.001f);
        }

        [Test]
        public void OnPostTick_DoesNotModifyWhenNotInitialized()
        {
            Vector3 somePos = new Vector3(3f, 1f, 2f);
            _go.transform.position = somePos;

            // OnPostTick OHNE Warmup → _initialized = false → kein Restore
            _smoother.OnPostTick(somePos, Quaternion.identity, TickDelta);

            Assert.AreEqual(somePos.x, _go.transform.position.x, 0.001f);
            Assert.IsFalse(_smoother.IsActive);
        }

        #endregion

        #region One-Tick-Behind Specifics

        [Test]
        public void BufferShift_DisplaysOneTickBehind()
        {
            // After 3 ticks: display should show tick 1's range (tick 0 end → tick 1 end).
            Vector3 pos0 = Vector3.zero;
            Vector3 pos1 = new Vector3(1f, 0f, 0f);
            Vector3 pos2 = new Vector3(2f, 0f, 0f);
            Vector3 pos3 = new Vector3(3f, 0f, 0f);

            // Tick 0: init
            _smoother.OnPreTick(pos0, Quaternion.identity, TickDelta);
            _smoother.OnPostTick(pos1, Quaternion.identity, TickDelta);
            // Tick 1: displayStart=pos0, displayEnd=pos1 (tick 0's range)
            _smoother.OnPreTick(pos1, Quaternion.identity, TickDelta);
            _smoother.OnPostTick(pos2, Quaternion.identity, TickDelta);
            // Tick 2: displayStart=pos1, displayEnd=pos2 (tick 1's range)
            _smoother.OnPreTick(pos2, Quaternion.identity, TickDelta);
            _smoother.OnPostTick(pos3, Quaternion.identity, TickDelta);

            // After OnPostTick of tick 2: visual at factor=0 = displayStart = pos1
            // (simulation is at pos3, but display is at pos1 = one tick behind)
            Assert.AreEqual(pos1.x, _go.transform.position.x, 0.001f);
        }

        [Test]
        public void WarmupPeriod_DoesNotWriteTransform()
        {
            Vector3 initialPos = new Vector3(99f, 0f, 0f);
            _go.transform.position = initialPos;

            // Tick 0 (warmup): should NOT modify transform
            _smoother.OnPreTick(Vector3.zero, Quaternion.identity, TickDelta);
            _smoother.OnPostTick(Vector3.one, Quaternion.identity, TickDelta);

            Assert.AreEqual(initialPos.x, _go.transform.position.x, 0.001f);
        }

        #endregion
    }
}
