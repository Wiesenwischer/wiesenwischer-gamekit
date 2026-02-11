using NUnit.Framework;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Visual;
using static Wiesenwischer.GameKit.CharacterController.Core.Visual.GroundingSmoother;

namespace Wiesenwischer.GameKit.CharacterController.Core.Tests.Visual
{
    [TestFixture]
    public class GroundingSmootherTests
    {
        private const float DefaultSmoothTime = 0.075f;
        private const float DefaultMaxStepDelta = 0.5f;
        private const float DeltaTime = 1f / 60f;

        private SmootherState _state;

        [SetUp]
        public void SetUp()
        {
            _state = new SmootherState();
        }

        private SmootherState Calculate(
            float deltaY,
            bool isGrounded = true,
            bool justLanded = false,
            float maxStepDelta = DefaultMaxStepDelta,
            bool onlyWhenGrounded = true,
            float smoothTime = DefaultSmoothTime)
        {
            return CalculateOffset(
                deltaY, isGrounded, justLanded,
                maxStepDelta, onlyWhenGrounded, smoothTime,
                _state, DeltaTime);
        }

        #region Step-Up Detection

        [Test]
        public void StepUp_BuildsNegativeOffset()
        {
            var result = Calculate(deltaY: 0.15f);

            Assert.Less(result.SmoothOffset, 0f,
                "Step-Up (positives deltaY) sollte negativen Offset erzeugen (Model temporär nach unten)");
        }

        [Test]
        public void StepDown_BuildsPositiveOffset()
        {
            var result = Calculate(deltaY: -0.15f);

            Assert.Greater(result.SmoothOffset, 0f,
                "Step-Down (negatives deltaY) sollte positiven Offset erzeugen (Model temporär nach oben)");
        }

        [Test]
        public void SmallDelta_NoOffset()
        {
            var result = Calculate(deltaY: 0.005f);

            Assert.AreEqual(0f, result.SmoothOffset,
                "Delta unter StepThreshold (0.01) sollte keinen Offset erzeugen");
        }

        #endregion

        #region Smoothing Resolution

        [Test]
        public void Offset_ResolvesToZero_OverTime()
        {
            // Erst Step-Up, dann mehrfach ohne deltaY aufrufen
            _state = Calculate(deltaY: 0.15f);
            float initialOffset = _state.SmoothOffset;

            // 10 Frames ohne neues Delta
            for (int i = 0; i < 10; i++)
            {
                _state = CalculateOffset(
                    0f, true, false,
                    DefaultMaxStepDelta, true, DefaultSmoothTime,
                    _state, DeltaTime);
            }

            Assert.Less(Mathf.Abs(_state.SmoothOffset), Mathf.Abs(initialOffset),
                "Offset sollte sich über Zeit zu 0 auflösen");
        }

        [Test]
        public void Offset_SnapsToZero_WhenBelowThreshold()
        {
            _state.SmoothOffset = 0.0005f;
            _state.SmoothVelocity = 0.001f;

            var result = CalculateOffset(
                0f, true, false,
                DefaultMaxStepDelta, true, DefaultSmoothTime,
                _state, DeltaTime);

            Assert.AreEqual(0f, result.SmoothOffset,
                "Offset unter SnapThreshold sollte exakt auf 0 snappen");
            Assert.AreEqual(0f, result.SmoothVelocity,
                "Velocity sollte bei Snap auch auf 0 gesetzt werden");
        }

        [Test]
        public void Offset_FullyResolvesAfterManyFrames()
        {
            _state = Calculate(deltaY: 0.2f);

            // 60 Frames (1 Sekunde) — mehr als genug für smoothTime 0.075
            for (int i = 0; i < 60; i++)
            {
                _state = CalculateOffset(
                    0f, true, false,
                    DefaultMaxStepDelta, true, DefaultSmoothTime,
                    _state, DeltaTime);
            }

            Assert.AreEqual(0f, _state.SmoothOffset,
                "Nach 1 Sekunde sollte der Offset vollständig aufgelöst sein");
        }

        #endregion

        #region Teleport Check

        [Test]
        public void LargeDelta_ResetsOffset_Teleport()
        {
            // Erst einen Offset aufbauen
            _state.SmoothOffset = -0.1f;
            _state.SmoothVelocity = -0.5f;

            var result = CalculateOffset(
                2.0f, true, false,
                DefaultMaxStepDelta, true, DefaultSmoothTime,
                _state, DeltaTime);

            Assert.AreEqual(0f, result.SmoothOffset,
                "Teleport (großes positives Delta) sollte Offset auf 0 setzen");
            Assert.AreEqual(0f, result.SmoothVelocity,
                "Teleport sollte Velocity auf 0 setzen");
        }

        [Test]
        public void NegativeLargeDelta_ResetsOffset_Teleport()
        {
            _state.SmoothOffset = 0.1f;

            var result = CalculateOffset(
                -2.0f, true, false,
                DefaultMaxStepDelta, true, DefaultSmoothTime,
                _state, DeltaTime);

            Assert.AreEqual(0f, result.SmoothOffset,
                "Teleport (großes negatives Delta) sollte Offset auf 0 setzen");
        }

        [Test]
        public void ExactlyAtMaxDelta_IsNotTeleport()
        {
            var result = Calculate(deltaY: DefaultMaxStepDelta);

            Assert.AreNotEqual(0f, result.SmoothOffset,
                "Exakt am maxStepDelta-Grenzwert sollte noch als Step erkannt werden");
        }

        [Test]
        public void JustAboveMaxDelta_IsTeleport()
        {
            var result = Calculate(deltaY: DefaultMaxStepDelta + 0.01f);

            Assert.AreEqual(0f, result.SmoothOffset,
                "Knapp über maxStepDelta sollte als Teleport erkannt werden");
        }

        #endregion

        #region Airborne Check

        [Test]
        public void Airborne_ResetsOffset()
        {
            _state.SmoothOffset = -0.1f;
            _state.SmoothVelocity = -0.5f;

            var result = CalculateOffset(
                0.1f, isGrounded: false, false,
                DefaultMaxStepDelta, onlyWhenGrounded: true, DefaultSmoothTime,
                _state, DeltaTime);

            Assert.AreEqual(0f, result.SmoothOffset,
                "Airborne + onlyWhenGrounded sollte Offset auf 0 setzen");
        }

        [Test]
        public void Airborne_KeepsOffset_WhenOnlyWhenGroundedFalse()
        {
            var result = CalculateOffset(
                0.15f, isGrounded: false, false,
                DefaultMaxStepDelta, onlyWhenGrounded: false, DefaultSmoothTime,
                _state, DeltaTime);

            Assert.AreNotEqual(0f, result.SmoothOffset,
                "onlyWhenGrounded=false sollte Smoothing auch in der Luft erlauben");
        }

        #endregion

        #region Landing Check

        [Test]
        public void JustLanded_ResetsOffset()
        {
            _state.SmoothOffset = -0.1f;
            _state.SmoothVelocity = -0.5f;

            var result = CalculateOffset(
                0.05f, isGrounded: true, justLanded: true,
                DefaultMaxStepDelta, true, DefaultSmoothTime,
                _state, DeltaTime);

            Assert.AreEqual(0f, result.SmoothOffset,
                "JustLanded sollte Offset auf 0 setzen (kein falscher Offset nach Landing)");
        }

        #endregion

        #region Slopes

        [Test]
        public void ContinuousSmallDelta_MinimalOffset()
        {
            // Simuliere 30 Frames mit Slope-typischem deltaY (0.005m bei 60fps)
            // Entspricht ~2m/s auf ~15° Slope — unterhalb StepThreshold (0.01)
            for (int i = 0; i < 30; i++)
            {
                _state = CalculateOffset(
                    0.005f, true, false,
                    DefaultMaxStepDelta, true, DefaultSmoothTime,
                    _state, DeltaTime);
            }

            Assert.AreEqual(0f, _state.SmoothOffset,
                "Kontinuierliche Slope-Deltas unter StepThreshold sollten keinen Offset erzeugen");
        }

        [Test]
        public void SteepSlope_FastWalk_NoOffset()
        {
            // Simuliere 30 Frames: 3m/s auf 10° Slope → deltaY ≈ 0.0087m/frame
            // Knapp unter StepThreshold (0.01) — sollte NICHT als Step erkannt werden
            for (int i = 0; i < 30; i++)
            {
                _state = CalculateOffset(
                    0.0087f, true, false,
                    DefaultMaxStepDelta, true, DefaultSmoothTime,
                    _state, DeltaTime);
            }

            Assert.AreEqual(0f, _state.SmoothOffset,
                "Schnelle Bewegung auf Slopes sollte keinen Offset erzeugen (unterhalb StepThreshold)");
        }

        #endregion

        #region Accumulation

        [Test]
        public void RapidSteps_AccumulateOffset()
        {
            // Zwei schnelle Steps hintereinander
            _state = Calculate(deltaY: 0.1f);
            float afterFirstStep = _state.SmoothOffset;

            _state = CalculateOffset(
                0.1f, true, false,
                DefaultMaxStepDelta, true, DefaultSmoothTime,
                _state, DeltaTime);

            Assert.Less(_state.SmoothOffset, afterFirstStep,
                "Schnelle aufeinanderfolgende Steps sollten Offset akkumulieren");
        }

        #endregion
    }
}
