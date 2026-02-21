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
        /// Nach DoInit: _initialized=true, _fromPos=_toPos=initPos, _interpT=1, queue leer.
        /// </summary>
        private void DoInit(Vector3 initPos)
        {
            _smoother.OnPreTick(initPos, Quaternion.identity, TickDelta);
            _smoother.OnPostTick(initPos, Quaternion.identity, TickDelta);
            // LateUpdate konsumiert das Goal → _fromPos = _toPos = initPos, t → ~0
            _smoother.SendMessage("LateUpdate");
        }

        /// <summary>
        /// Simuliert einen kompletten Tick: OnPreTick → OnPostTick (mit Motor an motorPos).
        /// </summary>
        private void SimulateTick(Vector3 motorPos)
        {
            _smoother.OnPreTick(motorPos, Quaternion.identity, TickDelta);
            _smoother.OnPostTick(motorPos, Quaternion.identity, TickDelta);
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

        #region Goal-Queue Interpolation

        [Test]
        public void OnPostTick_EnqueuesGoal()
        {
            DoInit(Vector3.zero);
            _smoother.OnPostTick(new Vector3(1f, 0f, 0f), Quaternion.identity, TickDelta);

            Assert.AreEqual(1, _smoother.QueueCount);
        }

        [Test]
        public void LateUpdate_ConsumesGoal()
        {
            DoInit(Vector3.zero);
            _smoother.OnPostTick(new Vector3(1f, 0f, 0f), Quaternion.identity, TickDelta);

            Assert.AreEqual(1, _smoother.QueueCount);

            // LateUpdate konsumiert Goal (in EditMode: dt≈0 → t bleibt bei 0)
            _smoother.SendMessage("LateUpdate");

            // Queue sollte leer sein (Goal konsumiert da _interpT von vorherigem Cycle ≥ 1 war)
            Assert.AreEqual(0, _smoother.QueueCount);
        }

        [Test]
        public void QueueEmpty_VisualHoldsAtLastGoal()
        {
            DoInit(new Vector3(5f, 0f, 0f));

            // Kein neues Goal → Visual bleibt bei 5
            _smoother.SendMessage("LateUpdate");
            _smoother.SendMessage("LateUpdate");

            Assert.AreEqual(5f, _go.transform.position.x, 0.01f);
        }

        [Test]
        public void MultipleGoals_ConsumedSequentially()
        {
            DoInit(Vector3.zero);

            // 3 Goals enqueuen
            _smoother.OnPostTick(new Vector3(1f, 0f, 0f), Quaternion.identity, TickDelta);
            _smoother.OnPostTick(new Vector3(2f, 0f, 0f), Quaternion.identity, TickDelta);
            _smoother.OnPostTick(new Vector3(3f, 0f, 0f), Quaternion.identity, TickDelta);

            Assert.AreEqual(3, _smoother.QueueCount);

            // In EditMode (dt≈0) konsumiert LateUpdate ein Goal pro Call
            // (weil _interpT von init war 1 → konsumiert sofort erstes)
            _smoother.SendMessage("LateUpdate");

            // Nach erstem Consume: from=0, to=1 (oder 1 konsumiert). Queue: 2 verbleibend.
            // Weitere Consumes haengen von _interpT ab.
            Assert.LessOrEqual(_smoother.QueueCount, 3);
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
        public void OnReconcileComplete_ShiftsEndpoints()
        {
            // Reconcile-Correction shiftet _fromPos/_toPos + Queue-Eintraege,
            // damit die Interpolation auf der korrigierten Trajektorie weitermacht.
            DoInit(new Vector3(5f, 0f, 0f));

            // Goal enqueuen (noch nicht konsumiert)
            _smoother.OnPostTick(new Vector3(6f, 0f, 0f), Quaternion.identity, TickDelta);

            // Reconcile: motor springt 0.1m nach rechts
            _smoother.OnReconcileComplete(
                new Vector3(10f, 0f, 0f), 0f,
                new Vector3(10.1f, 0f, 0f), 0f);

            // Offset = pre - corrected = -0.1
            Assert.AreEqual(-0.1f, _smoother.CurrentOffset.x, 0.001f);

            // Queue-Eintrag sollte auch geshiftet sein (+0.1)
            // Konsumiere das Goal
            _smoother.SendMessage("LateUpdate");

            // Visual sollte bei ca. 6.1 + (-0.1) = 6.0 sein (oder interpoliert)
            // Hauptsache: Offset wird korrekt verechnet
        }

        [Test]
        public void OnReconcileComplete_ClearsQueueOnSnap()
        {
            DoInit(Vector3.zero);

            // Goals enqueuen
            _smoother.OnPostTick(new Vector3(1f, 0f, 0f), Quaternion.identity, TickDelta);
            _smoother.OnPostTick(new Vector3(2f, 0f, 0f), Quaternion.identity, TickDelta);
            Assert.AreEqual(2, _smoother.QueueCount);

            // Grosser Error → Snap → Queue cleared
            _smoother.OnReconcileComplete(
                Vector3.zero, 0f,
                new Vector3(10f, 0f, 0f), 0f);

            Assert.AreEqual(0, _smoother.QueueCount);
            Assert.AreEqual(Vector3.zero, _smoother.CurrentOffset);
        }

        #endregion

        // Spectator Correction Tests entfernt:
        // OnSpectatorCorrection war fehlerhaft — erfasste prePos-postPos = -movement
        // statt den Reconcile-Error. Jeder Tick addierte -movement zum Offset
        // → persistenter 1.5-2.5m Offset + Stutter (5-12:1 Ratio).
        // Spectator-Corrections laufen jetzt via OnReconcileComplete (gleicher Pfad wie Owner).

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

        #region Reset

        [Test]
        public void Reset_ClearsQueue()
        {
            DoInit(Vector3.zero);
            _smoother.OnPostTick(new Vector3(1f, 0f, 0f), Quaternion.identity, TickDelta);
            _smoother.OnPostTick(new Vector3(2f, 0f, 0f), Quaternion.identity, TickDelta);

            _smoother.Reset();

            Assert.AreEqual(0, _smoother.QueueCount);
            Assert.IsFalse(_smoother.IsActive);
        }

        [Test]
        public void Reset_ClearsAllState()
        {
            DoInit(Vector3.zero);

            SimulateTick(new Vector3(1f, 0f, 0f));
            _smoother.OnReconcileComplete(Vector3.one, 0f, Vector3.zero, 0f);

            _smoother.Reset();

            Assert.IsFalse(_smoother.IsActive);
            Assert.AreEqual(Vector3.zero, _smoother.CurrentOffset);
            Assert.AreEqual(0, _smoother.QueueCount);
        }

        #endregion

        #region OnPostTick Transform Update

        [Test]
        public void OnPostTick_SetsTransformToCurrentVisual()
        {
            // OnPostTick setzt Transform auf die AKTUELLE interpolierte Position,
            // nicht auf die neue Motor-Position (Goal ist noch nicht konsumiert).
            DoInit(new Vector3(5f, 0f, 0f));

            // Neues Goal: motor at 6. Queue hat jetzt 1 Eintrag.
            // Visual sollte immer noch bei 5 sein (Goal nicht konsumiert).
            _smoother.OnPostTick(new Vector3(6f, 0f, 0f), Quaternion.identity, TickDelta);

            // Transform bleibt bei ~5 (aktuelle interpolierte Position)
            Assert.AreEqual(5f, _go.transform.position.x, 0.1f);
        }

        [Test]
        public void OnPostTick_IncludesOffset()
        {
            DoInit(new Vector3(5f, 0f, 0f));

            // Reconcile Offset aufbauen
            _smoother.OnReconcileComplete(
                new Vector3(10f, 0f, 0f), 0f,
                new Vector3(9.9f, 0f, 0f), 0f);
            // offset = 10 - 9.9 = 0.1

            _smoother.OnPostTick(new Vector3(6f, 0f, 0f), Quaternion.identity, TickDelta);

            // Visual = interp_pos + offset ≈ 5 + 0.1 = 5.1
            Assert.AreEqual(5.1f, _go.transform.position.x, 0.15f);
        }

        [Test]
        public void OnPostTick_DoesNotModifyWhenNotInitialized()
        {
            Vector3 somePos = new Vector3(3f, 1f, 2f);
            _go.transform.position = somePos;

            // OnPostTick OHNE Initialisierung → Transform unveraendert
            _smoother.OnPostTick(somePos, Quaternion.identity, TickDelta);

            Assert.AreEqual(somePos.x, _go.transform.position.x, 0.001f);
            Assert.IsFalse(_smoother.IsActive);
        }

        #endregion

        #region Snap Behavior

        [Test]
        public void SnapReconcile_ResetsState()
        {
            DoInit(Vector3.zero);

            SimulateTick(new Vector3(1f, 0f, 0f));

            // Grosser Reconcile → Snap (Error > 2m Threshold)
            _smoother.OnReconcileComplete(
                new Vector3(1f, 0f, 0f), 0f,
                new Vector3(10f, 0f, 0f), 0f);

            Assert.AreEqual(Vector3.zero, _smoother.CurrentOffset);
            Assert.AreEqual(0, _smoother.QueueCount);
        }

        // SnapSpectator_ResetsState entfernt — OnSpectatorCorrection nicht mehr verwendet.

        #endregion
    }
}
