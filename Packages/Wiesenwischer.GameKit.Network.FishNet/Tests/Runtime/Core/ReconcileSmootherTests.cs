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
        /// Helper: Initialisiert den Smoother mit einer Position.
        /// Nach Aufruf: _initialized=true, _smoothPos=initPos, velocity=zero.
        /// </summary>
        private void DoInit(Vector3 initPos)
        {
            _smoother.OnPreTick(initPos, Quaternion.identity, TickDelta);
            _smoother.OnPostTick(initPos, Quaternion.identity, TickDelta, Vector3.zero);
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

        #region Initialization

        [Test]
        public void OnPreTick_FirstCall_Activates()
        {
            _smoother.OnPreTick(Vector3.zero, Quaternion.identity, TickDelta);
            Assert.IsTrue(_smoother.IsActive);
        }

        [Test]
        public void Reset_SetsActiveFalse()
        {
            DoInit(Vector3.zero);
            _smoother.Reset();
            Assert.IsFalse(_smoother.IsActive);
        }

        [Test]
        public void Reset_ClearsOffset()
        {
            DoInit(Vector3.zero);
            _smoother.OnReconcileComplete(Vector3.one, 0f, Vector3.zero, 0f);
            _smoother.Reset();

            Assert.AreEqual(Vector3.zero, _smoother.CurrentOffset);
            Assert.AreEqual(0f, _smoother.CurrentRotationOffset, 0.001f);
        }

        [Test]
        public void LateUpdate_DoesNothing_WhenNotInitialized()
        {
            Vector3 initialPos = new Vector3(99f, 0f, 0f);
            _go.transform.position = initialPos;

            // LateUpdate ohne Initialisierung → transform bleibt unveraendert
            _smoother.SendMessage("LateUpdate");

            Assert.AreEqual(initialPos.x, _go.transform.position.x, 0.001f);
        }

        #endregion

        #region Reconcile Correction

        [Test]
        public void OnReconcileComplete_AccumulatesOffset()
        {
            DoInit(Vector3.zero);

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
            DoInit(Vector3.zero);

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
            DoInit(Vector3.zero);

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
            DoInit(Vector3.zero);

            _smoother.OnReconcileComplete(Vector3.zero, 90f, Vector3.zero, 85f);

            // DeltaAngle(85, 90) = 5
            Assert.AreEqual(5f, _smoother.CurrentRotationOffset, 0.1f);
        }

        [Test]
        public void OnReconcileComplete_PreservesVisual()
        {
            // Verifies: _smoothPos + offset is unchanged after correction.
            // _smoothPos shifts by correction, offset absorbs error → net visual = same.
            Vector3 initPos = new Vector3(5f, 0f, 0f);
            DoInit(initPos);
            // _smoothPos = (5,0,0), offset = (0,0,0)

            // Get visual position before correction
            _smoother.OnPostTick(new Vector3(6f, 0f, 0f), Quaternion.identity, TickDelta, Vector3.zero);
            Vector3 visualBefore = _go.transform.position;

            // Correction: pre=10, corrected=10.1
            _smoother.OnReconcileComplete(
                new Vector3(10f, 0f, 0f), 0f,
                new Vector3(10.1f, 0f, 0f), 0f);

            // Check visual after correction
            _smoother.OnPostTick(new Vector3(6.1f, 0f, 0f), Quaternion.identity, TickDelta, Vector3.zero);
            Vector3 visualAfter = _go.transform.position;

            Assert.AreEqual(visualBefore.x, visualAfter.x, 0.001f);
        }

        #endregion

        #region Spectator Correction

        [Test]
        public void OnSpectatorCorrection_AccumulatesOffset()
        {
            DoInit(Vector3.zero);

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
            DoInit(Vector3.zero);

            // Erst kleinen Offset aufbauen
            _smoother.OnSpectatorCorrection(Vector3.one, Vector3.zero, Quaternion.identity);
            Assert.AreNotEqual(Vector3.zero, _smoother.CurrentOffset);

            // Grosser Error
            _smoother.OnSpectatorCorrection(
                Vector3.zero, new Vector3(5f, 0f, 0f), Quaternion.identity);

            Assert.AreEqual(Vector3.zero, _smoother.CurrentOffset);
        }

        [Test]
        public void OnSpectatorCorrection_PreservesVisual()
        {
            // _smoothPos shifts by correction, offset absorbs error → visual unchanged.
            Vector3 initPos = new Vector3(9f, 0f, 0f);
            DoInit(initPos);

            _smoother.OnPostTick(new Vector3(10f, 0f, 0f), Quaternion.identity, TickDelta, Vector3.zero);
            Vector3 visualBefore = _go.transform.position;

            // Spectator correction
            _smoother.OnSpectatorCorrection(
                new Vector3(11f, 0f, 0f),
                new Vector3(11.05f, 0f, 0f),
                Quaternion.identity);

            _smoother.OnPostTick(new Vector3(11.05f, 0f, 0f), Quaternion.identity, TickDelta, Vector3.zero);
            Vector3 visualAfter = _go.transform.position;

            Assert.AreEqual(visualBefore.x, visualAfter.x, 0.001f);
        }

        #endregion

        #region ClearOffset

        [Test]
        public void ClearOffset_SnapsToZero()
        {
            DoInit(Vector3.zero);
            _smoother.OnReconcileComplete(Vector3.one, 10f, Vector3.zero, 0f);
            _smoother.ClearOffset();

            Assert.AreEqual(Vector3.zero, _smoother.CurrentOffset);
            Assert.AreEqual(0f, _smoother.CurrentRotationOffset, 0.001f);
        }

        #endregion

        #region OnPostTick Behavior

        [Test]
        public void OnPostTick_SetsVisualToSmoothPos()
        {
            // After init: _smoothPos = initPos. OnPostTick snaps _smoothPos to motorPos,
            // absorbs drift into offset → net visual = same position.
            Vector3 initPos = new Vector3(5f, 1f, 3f);
            DoInit(initPos);

            // Motor bewegt sich, smoother absorbed drift
            Vector3 simPos = new Vector3(5.2f, 1f, 3.1f);
            _go.transform.position = simPos;

            _smoother.OnPostTick(simPos, Quaternion.identity, TickDelta, Vector3.zero);

            // Visual = _smoothPos + offset = simPos + (initPos - simPos) = initPos
            Assert.AreEqual(initPos.x, _go.transform.position.x, 0.001f);
            Assert.AreEqual(initPos.y, _go.transform.position.y, 0.001f);
            Assert.AreEqual(initPos.z, _go.transform.position.z, 0.001f);
        }

        [Test]
        public void OnPostTick_IncludesCorrectionOffset()
        {
            Vector3 initPos = new Vector3(5f, 1f, 3f);
            DoInit(initPos);

            // Reconcile Correction: 0.1m Error
            _smoother.OnReconcileComplete(
                new Vector3(5f, 1f, 3f), 0f,
                new Vector3(4.9f, 1f, 3f), 0f);

            // error = (0.1, 0, 0), correction = (-0.1, 0, 0)
            // _smoothPos shifted: (5,1,3) + (-0.1,0,0) = (4.9, 1, 3)
            // offset = (0.1, 0, 0)

            _go.transform.position = new Vector3(5.2f, 1f, 3.1f);
            _smoother.OnPostTick(new Vector3(5.2f, 1f, 3.1f), Quaternion.identity, TickDelta, Vector3.zero);

            // drift = (4.9,1,3) - (5.2,1,3.1) = (-0.3, 0, -0.1)
            // offset = (0.1,0,0) + (-0.3,0,-0.1) = (-0.2, 0, -0.1)
            // visual = (5.2,1,3.1) + (-0.2,0,-0.1) = (5.0, 1.0, 3.0)
            Assert.AreEqual(5.0f, _go.transform.position.x, 0.001f);
            Assert.AreEqual(1.0f, _go.transform.position.y, 0.001f);
            Assert.AreEqual(3.0f, _go.transform.position.z, 0.001f);
        }

        [Test]
        public void OnPostTick_DoesNotModifyWhenNotInitialized()
        {
            Vector3 somePos = new Vector3(3f, 1f, 2f);
            _go.transform.position = somePos;

            // OnPostTick OHNE Initialisierung → kein Snap+Absorb
            _smoother.OnPostTick(somePos, Quaternion.identity, TickDelta, Vector3.zero);

            Assert.AreEqual(somePos.x, _go.transform.position.x, 0.001f);
            Assert.IsFalse(_smoother.IsActive);
        }

        [Test]
        public void OnPostTick_AbsorbsDriftIntoOffset()
        {
            // Velocity-based: between ticks, _smoothPos may drift from motor position.
            // OnPostTick absorbs this drift into _positionOffset.
            DoInit(new Vector3(0f, 0f, 0f));

            // Motor moved to (1,0,0) but no LateUpdate ran → _smoothPos still at (0,0,0)
            // Drift = (0,0,0) - (1,0,0) = (-1,0,0)
            _smoother.OnPostTick(new Vector3(1f, 0f, 0f), Quaternion.identity, TickDelta,
                                  new Vector3(30f, 0f, 0f));

            // Drift absorbed: offset = (-1,0,0)
            Assert.AreEqual(-1f, _smoother.CurrentOffset.x, 0.001f);

            // Visual preserved: _smoothPos + offset = (1,0,0) + (-1,0,0) = (0,0,0)
            Assert.AreEqual(0f, _go.transform.position.x, 0.001f);
        }

        #endregion

        #region Velocity-Based Movement

        [Test]
        public void OnPostTick_UpdatesTarget_VisualStaysAtSmoothPos()
        {
            DoInit(Vector3.zero);

            // Motor moved to (1,0,0). Drift = (0) - (1) = -1 → offset = -1
            // Visual = (1,0,0) + (-1,0,0) = (0,0,0) — unchanged
            Vector3 newTarget = new Vector3(1f, 0f, 0f);
            _smoother.OnPostTick(newTarget, Quaternion.identity, TickDelta, new Vector3(30f, 0f, 0f));

            Assert.AreEqual(0f, _go.transform.position.x, 0.001f);
        }

        [Test]
        public void MultipleTicksSameFrame_DriftAccumulates()
        {
            // Multiple ticks in same frame without LateUpdate: drift accumulates in offset.
            // Visual stays at initial position because no velocity*dt movement happened.
            DoInit(Vector3.zero);

            // 3 Ticks im selben Frame: Motor springt von 0 → 1 → 2 → 3
            _smoother.OnPreTick(Vector3.zero, Quaternion.identity, TickDelta);
            _smoother.OnPostTick(new Vector3(1f, 0f, 0f), Quaternion.identity, TickDelta,
                                  new Vector3(30f, 0f, 0f));

            _smoother.OnPreTick(new Vector3(1f, 0f, 0f), Quaternion.identity, TickDelta);
            _smoother.OnPostTick(new Vector3(2f, 0f, 0f), Quaternion.identity, TickDelta,
                                  new Vector3(30f, 0f, 0f));

            _smoother.OnPreTick(new Vector3(2f, 0f, 0f), Quaternion.identity, TickDelta);
            _smoother.OnPostTick(new Vector3(3f, 0f, 0f), Quaternion.identity, TickDelta,
                                  new Vector3(30f, 0f, 0f));

            // Visual bleibt bei (0,0,0) — kein LateUpdate lief
            Assert.AreEqual(0f, _go.transform.position.x, 0.001f);
            // Offset hat 3 Ticks Drift akkumuliert: -3
            Assert.AreEqual(-3f, _smoother.CurrentOffset.x, 0.001f);
        }

        [Test]
        public void VelocityBased_VisualPreservedOnEveryTick()
        {
            // Core invariant: Visual = _smoothPos + offset is ALWAYS preserved across ticks.
            // No jump, no stutter — just continuous velocity*dt movement in LateUpdate.
            DoInit(Vector3.zero);

            // Tick 1
            _smoother.OnPreTick(Vector3.zero, Quaternion.identity, TickDelta);
            _smoother.OnPostTick(new Vector3(1f, 0f, 0f), Quaternion.identity, TickDelta,
                                  new Vector3(30f, 0f, 0f));
            Assert.AreEqual(0f, _go.transform.position.x, 0.001f);

            // Tick 2
            _smoother.OnPreTick(new Vector3(1f, 0f, 0f), Quaternion.identity, TickDelta);
            _smoother.OnPostTick(new Vector3(2f, 0f, 0f), Quaternion.identity, TickDelta,
                                  new Vector3(30f, 0f, 0f));
            Assert.AreEqual(0f, _go.transform.position.x, 0.001f);
        }

        [Test]
        public void SnapReconcile_ResetsState()
        {
            // Nach Snap: _smoothPos wird auf korrigierte Position gesetzt.
            // Naechstes OnPostTick: drift = 0, visual an geSnapter Position.
            DoInit(Vector3.zero);

            // Bewegung aufbauen
            _smoother.OnPreTick(Vector3.zero, Quaternion.identity, TickDelta);
            _smoother.OnPostTick(new Vector3(1f, 0f, 0f), Quaternion.identity, TickDelta, Vector3.zero);
            _smoother.OnPreTick(new Vector3(1f, 0f, 0f), Quaternion.identity, TickDelta);
            _smoother.OnPostTick(new Vector3(2f, 0f, 0f), Quaternion.identity, TickDelta, Vector3.zero);

            // Grosser Reconcile → Snap (Error > 2m Threshold)
            _smoother.OnReconcileComplete(
                new Vector3(2f, 0f, 0f), 0f,
                new Vector3(10f, 0f, 0f), 0f);

            Assert.AreEqual(Vector3.zero, _smoother.CurrentOffset);

            // OnPostTick nach Snap: drift = _smoothPos(10) - motor(10) = 0
            _smoother.OnPostTick(new Vector3(10f, 0f, 0f), Quaternion.identity, TickDelta, Vector3.zero);
            Assert.AreEqual(10f, _go.transform.position.x, 0.001f);
        }

        [Test]
        public void SnapSpectator_ResetsState()
        {
            DoInit(Vector3.zero);

            // Bewegung aufbauen
            _smoother.OnPreTick(Vector3.zero, Quaternion.identity, TickDelta);
            _smoother.OnPostTick(new Vector3(1f, 0f, 0f), Quaternion.identity, TickDelta, Vector3.zero);

            // Grosser Spectator-Error → Snap
            _smoother.OnSpectatorCorrection(
                Vector3.zero, new Vector3(5f, 0f, 0f), Quaternion.identity);

            Assert.AreEqual(Vector3.zero, _smoother.CurrentOffset);

            // Visual an geSnapter Position (drift = _smoothPos(5) - motor(5) = 0)
            _smoother.OnPostTick(new Vector3(5f, 0f, 0f), Quaternion.identity, TickDelta, Vector3.zero);
            Assert.AreEqual(5f, _go.transform.position.x, 0.001f);
        }

        [Test]
        public void Reset_ClearsAllState()
        {
            DoInit(Vector3.zero);

            // Bewegung + Offset aufbauen
            _smoother.OnPreTick(Vector3.zero, Quaternion.identity, TickDelta);
            _smoother.OnPostTick(new Vector3(1f, 0f, 0f), Quaternion.identity, TickDelta,
                                  new Vector3(30f, 0f, 0f));
            _smoother.OnReconcileComplete(Vector3.one, 0f, Vector3.zero, 0f);

            _smoother.Reset();

            Assert.IsFalse(_smoother.IsActive);
            Assert.AreEqual(Vector3.zero, _smoother.CurrentOffset);
        }

        #endregion
    }
}
