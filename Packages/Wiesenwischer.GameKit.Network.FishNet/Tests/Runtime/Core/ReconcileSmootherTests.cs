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
        /// Helper: Initialisiert den Smoother mit einer Position und simuliert ein LateUpdate.
        /// Danach: _initialized=true, _smoothPos=initPos, velocity=zero,
        /// _hadLateUpdateSincePostTick=true (wie nach einem echten Frame).
        /// </summary>
        private void DoInit(Vector3 initPos)
        {
            _smoother.OnPreTick(initPos, Quaternion.identity, TickDelta);
            _smoother.OnPostTick(initPos, Quaternion.identity, TickDelta, Vector3.zero);
            // LateUpdate simulieren: setzt _hadLateUpdateSincePostTick = true.
            // Bei dt=0 (EditMode) aendert sich _smoothPos nicht (velocity=zero, += 0).
            _smoother.SendMessage("LateUpdate");
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

            _go.transform.position = new Vector3(5.2f, 1f, 3.1f);
            _smoother.OnPostTick(new Vector3(5.2f, 1f, 3.1f), Quaternion.identity, TickDelta, Vector3.zero);

            // visual = _smoothPos + offset preserves initPos + reconcile shift
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
        public void OnPostTick_UpdatesTarget_VisualPreserved()
        {
            DoInit(Vector3.zero);

            // Motor moved to (1,0,0). Drift = (0) - (1) = -1 → offset = -1
            // Visual = 1 + (-1) = 0 — preserved
            _smoother.OnPostTick(new Vector3(1f, 0f, 0f), Quaternion.identity, TickDelta,
                                  new Vector3(30f, 0f, 0f));

            Assert.AreEqual(0f, _go.transform.position.x, 0.001f);
        }

        [Test]
        public void SingleTick_VisualPreservedAcrossPostTick()
        {
            // Core invariant: Visual = _smoothPos + offset is preserved within OnPostTick.
            DoInit(Vector3.zero);

            // Single tick (LateUpdate ran before, so no inter-tick advance)
            _smoother.OnPreTick(Vector3.zero, Quaternion.identity, TickDelta);
            _smoother.OnPostTick(new Vector3(1f, 0f, 0f), Quaternion.identity, TickDelta,
                                  new Vector3(30f, 0f, 0f));

            // Visual preserved: smooth(1) + offset(-1) = 0
            Assert.AreEqual(0f, _go.transform.position.x, 0.001f);
            Assert.AreEqual(-1f, _smoother.CurrentOffset.x, 0.001f);
        }

        [Test]
        public void MultipleTicksSameFrame_InterTickAdvance_KeepsDriftSmall()
        {
            // With InterTickAdvance: on multi-tick frames, OnPreTick advances _smoothPos
            // by velocity*tickDelta for each consecutive tick. This keeps drift per extra tick
            // near zero instead of accumulating -V*tickDelta per tick.
            float vel = 1f / TickDelta; // velocity that matches 1 unit per tick exactly
            DoInit(Vector3.zero);

            // Tick 1 (first in frame — LateUpdate ran after DoInit, flag=true → no advance)
            _smoother.OnPreTick(Vector3.zero, Quaternion.identity, TickDelta);
            _smoother.OnPostTick(new Vector3(1f, 0f, 0f), Quaternion.identity, TickDelta,
                                  new Vector3(vel, 0f, 0f));

            // First tick: drift = 0 - 1 = -1 (normal, from LateUpdate timing gap)
            Assert.AreEqual(-1f, _smoother.CurrentOffset.x, 0.01f);

            // Tick 2 (no LateUpdate between → inter-tick advance fires!)
            _smoother.OnPreTick(new Vector3(1f, 0f, 0f), Quaternion.identity, TickDelta);
            _smoother.OnPostTick(new Vector3(2f, 0f, 0f), Quaternion.identity, TickDelta,
                                  new Vector3(vel, 0f, 0f));

            // With advance: _smoothPos advanced by vel*tickDelta = 1 before OnPostTick.
            // Drift = (1+1) - 2 = 0. Offset stays at -1 (only first tick's drift).
            Assert.AreEqual(-1f, _smoother.CurrentOffset.x, 0.01f);

            // Tick 3 (no LateUpdate → advance again)
            _smoother.OnPreTick(new Vector3(2f, 0f, 0f), Quaternion.identity, TickDelta);
            _smoother.OnPostTick(new Vector3(3f, 0f, 0f), Quaternion.identity, TickDelta,
                                  new Vector3(vel, 0f, 0f));

            // Offset still ~-1, NOT -3 as without the advance
            Assert.AreEqual(-1f, _smoother.CurrentOffset.x, 0.01f);
        }

        [Test]
        public void MultipleTicksSameFrame_WithoutMatchingVelocity_SmallExtraDrift()
        {
            // If velocity doesn't exactly match motor movement (e.g., acceleration),
            // the inter-tick advance produces small residual drift per extra tick.
            // This is MUCH smaller than without advance (0.01m vs 0.165m per tick).
            DoInit(Vector3.zero);

            float vel = 30f; // 30 m/s → 30*0.033 = 0.99m per tick, but motor moves 1.0m

            // Tick 1
            _smoother.OnPreTick(Vector3.zero, Quaternion.identity, TickDelta);
            _smoother.OnPostTick(new Vector3(1f, 0f, 0f), Quaternion.identity, TickDelta,
                                  new Vector3(vel, 0f, 0f));
            float offsetAfterTick1 = _smoother.CurrentOffset.x; // -1

            // Tick 2 (inter-tick advance: +0.99, but motor moves by 1.0 → tiny extra drift)
            _smoother.OnPreTick(new Vector3(1f, 0f, 0f), Quaternion.identity, TickDelta);
            _smoother.OnPostTick(new Vector3(2f, 0f, 0f), Quaternion.identity, TickDelta,
                                  new Vector3(vel, 0f, 0f));
            float offsetAfterTick2 = _smoother.CurrentOffset.x;

            // Extra drift from tick 2 should be tiny (0.99 - 1.0 = -0.01)
            float extraDrift = offsetAfterTick2 - offsetAfterTick1;
            Assert.Less(Mathf.Abs(extraDrift), 0.02f);
        }

        [Test]
        public void SnapReconcile_ResetsState()
        {
            DoInit(Vector3.zero);

            _smoother.OnPreTick(Vector3.zero, Quaternion.identity, TickDelta);
            _smoother.OnPostTick(new Vector3(1f, 0f, 0f), Quaternion.identity, TickDelta, Vector3.zero);

            // Grosser Reconcile → Snap (Error > 2m Threshold)
            _smoother.OnReconcileComplete(
                new Vector3(1f, 0f, 0f), 0f,
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

            _smoother.OnPreTick(Vector3.zero, Quaternion.identity, TickDelta);
            _smoother.OnPostTick(new Vector3(1f, 0f, 0f), Quaternion.identity, TickDelta, Vector3.zero);

            // Grosser Spectator-Error → Snap
            _smoother.OnSpectatorCorrection(
                Vector3.zero, new Vector3(5f, 0f, 0f), Quaternion.identity);

            Assert.AreEqual(Vector3.zero, _smoother.CurrentOffset);

            // Visual an geSnapter Position
            _smoother.OnPostTick(new Vector3(5f, 0f, 0f), Quaternion.identity, TickDelta, Vector3.zero);
            Assert.AreEqual(5f, _go.transform.position.x, 0.001f);
        }

        [Test]
        public void Reset_ClearsAllState()
        {
            DoInit(Vector3.zero);

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
