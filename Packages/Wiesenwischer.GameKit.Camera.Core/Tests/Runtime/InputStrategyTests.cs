using NUnit.Framework;
using UnityEngine;

namespace Wiesenwischer.GameKit.Camera.Tests
{
    [TestFixture]
    public class InputStrategyTests
    {
        #region AlwaysOnInputStrategy

        [Test]
        public void AlwaysOn_DetermineOrbitMode_AlwaysReturnsFreeOrbit()
        {
            var strategy = new AlwaysOnInputStrategy();

            Assert.AreEqual(CameraOrbitMode.FreeOrbit, strategy.DetermineOrbitMode(false, false, false));
            Assert.AreEqual(CameraOrbitMode.FreeOrbit, strategy.DetermineOrbitMode(true, false, false));
            Assert.AreEqual(CameraOrbitMode.FreeOrbit, strategy.DetermineOrbitMode(false, true, false));
            Assert.AreEqual(CameraOrbitMode.FreeOrbit, strategy.DetermineOrbitMode(true, true, true));
        }

        [Test]
        public void AlwaysOn_ShouldReadLookInput_AlwaysTrue()
        {
            var strategy = new AlwaysOnInputStrategy();

            Assert.IsTrue(strategy.ShouldReadLookInput(CameraOrbitMode.FreeOrbit));
            Assert.IsTrue(strategy.ShouldReadLookInput(CameraOrbitMode.SteerOrbit));
            Assert.IsTrue(strategy.ShouldReadLookInput(CameraOrbitMode.None));
        }

        [Test]
        public void AlwaysOn_InitialCursorState_IsLocked()
        {
            var strategy = new AlwaysOnInputStrategy();

            Assert.AreEqual(CursorLockMode.Locked, strategy.InitialCursorState);
        }

        [Test]
        public void AlwaysOn_GetCursorState_AlwaysLocked()
        {
            var strategy = new AlwaysOnInputStrategy();

            Assert.AreEqual(CursorLockMode.Locked, strategy.GetCursorState(CameraOrbitMode.FreeOrbit));
            Assert.AreEqual(CursorLockMode.Locked, strategy.GetCursorState(CameraOrbitMode.None));
        }

        #endregion

        #region ButtonActivatedInputStrategy

        [Test]
        public void ButtonActivated_NoButtons_ReturnsNone()
        {
            var strategy = new ButtonActivatedInputStrategy();

            Assert.AreEqual(CameraOrbitMode.None,
                strategy.DetermineOrbitMode(false, false, false));
        }

        [Test]
        public void ButtonActivated_FreeLookHeld_ReturnsFreeOrbit()
        {
            var strategy = new ButtonActivatedInputStrategy();

            Assert.AreEqual(CameraOrbitMode.FreeOrbit,
                strategy.DetermineOrbitMode(true, false, false));
        }

        [Test]
        public void ButtonActivated_SteerHeld_ReturnsSteerOrbit()
        {
            var strategy = new ButtonActivatedInputStrategy();

            Assert.AreEqual(CameraOrbitMode.SteerOrbit,
                strategy.DetermineOrbitMode(false, true, false));
        }

        [Test]
        public void ButtonActivated_BothHeld_SteerTakesPriority()
        {
            var strategy = new ButtonActivatedInputStrategy();

            Assert.AreEqual(CameraOrbitMode.SteerOrbit,
                strategy.DetermineOrbitMode(true, true, false));
        }

        [Test]
        public void ButtonActivated_Gamepad_ReturnsFreeOrbit()
        {
            var strategy = new ButtonActivatedInputStrategy();

            Assert.AreEqual(CameraOrbitMode.FreeOrbit,
                strategy.DetermineOrbitMode(false, false, true));
        }

        [Test]
        public void ButtonActivated_ShouldReadLookInput_FalseForNone()
        {
            var strategy = new ButtonActivatedInputStrategy();

            Assert.IsFalse(strategy.ShouldReadLookInput(CameraOrbitMode.None));
        }

        [Test]
        public void ButtonActivated_ShouldReadLookInput_TrueForFreeOrbit()
        {
            var strategy = new ButtonActivatedInputStrategy();

            Assert.IsTrue(strategy.ShouldReadLookInput(CameraOrbitMode.FreeOrbit));
        }

        [Test]
        public void ButtonActivated_ShouldReadLookInput_TrueForSteerOrbit()
        {
            var strategy = new ButtonActivatedInputStrategy();

            Assert.IsTrue(strategy.ShouldReadLookInput(CameraOrbitMode.SteerOrbit));
        }

        [Test]
        public void ButtonActivated_InitialCursorState_IsNone()
        {
            var strategy = new ButtonActivatedInputStrategy();

            Assert.AreEqual(CursorLockMode.None, strategy.InitialCursorState);
        }

        [Test]
        public void ButtonActivated_GetCursorState_LockedForOrbit_NoneForNone()
        {
            var strategy = new ButtonActivatedInputStrategy();

            Assert.AreEqual(CursorLockMode.Locked,
                strategy.GetCursorState(CameraOrbitMode.FreeOrbit));
            Assert.AreEqual(CursorLockMode.Locked,
                strategy.GetCursorState(CameraOrbitMode.SteerOrbit));
            Assert.AreEqual(CursorLockMode.None,
                strategy.GetCursorState(CameraOrbitMode.None));
        }

        #endregion
    }
}
