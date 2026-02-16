using NUnit.Framework;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core;

namespace Wiesenwischer.GameKit.Camera.Tests
{
    [TestFixture]
    public class CameraOrientationProviderTests
    {
        private GameObject _brainGO;
        private CameraBrain _brain;
        private CameraOrientationProvider _provider;

        [SetUp]
        public void SetUp()
        {
            // Minimaler CameraBrain-Setup: PivotRig (RequireComponent) + CameraBrain + Provider
            _brainGO = new GameObject("TestCameraBrain");
            _brainGO.AddComponent<PivotRig>();
            _brain = _brainGO.AddComponent<CameraBrain>();
            _provider = _brainGO.AddComponent<CameraOrientationProvider>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_brainGO);
        }

        #region Interface Implementation

        [Test]
        public void ImplementsIOrientationProvider()
        {
            Assert.IsInstanceOf<IOrientationProvider>(_provider);
        }

        [Test]
        public void ImplementsIFacingProvider()
        {
            Assert.IsInstanceOf<IFacingProvider>(_provider);
        }

        #endregion

        #region Default Behavior (AlwaysOn, kein InputPipeline)

        [Test]
        public void Default_OrbitActivation_IsAlwaysOn()
        {
            // Ohne InputPipeline → Fallback auf AlwaysOn
            Assert.AreEqual(OrbitActivation.AlwaysOn, _brain.OrbitActivation);
        }

        [Test]
        public void Default_CurrentOrbitMode_IsFreeOrbit()
        {
            Assert.AreEqual(CameraOrbitMode.FreeOrbit, _brain.CurrentOrbitMode);
        }

        [Test]
        public void AlwaysOn_GetFacingMode_ReturnsMovementDirection()
        {
            // AlwaysOn → immer MovementDirection (nicht CameraForward)
            var mode = _provider.GetFacingMode();

            Assert.AreEqual(FacingMode.MovementDirection, mode);
        }

        #endregion

        #region Movement Right

        [Test]
        public void GetMovementRight_IsHorizontal()
        {
            Vector3 right = _provider.GetMovementRight();

            Assert.AreEqual(0f, right.y, 0.001f);
        }

        [Test]
        public void GetMovementRight_IsPerpendicularToForward()
        {
            Vector3 forward = _provider.GetMovementForward();
            Vector3 right = _provider.GetMovementRight();

            float dot = Vector3.Dot(forward, right);
            Assert.AreEqual(0f, dot, 0.01f);
        }

        #endregion

        #region Facing Direction

        [Test]
        public void GetFacingDirection_ReturnsNonZero()
        {
            Vector3 dir = _provider.GetFacingDirection();

            Assert.Greater(dir.sqrMagnitude, 0.001f);
        }

        #endregion
    }
}
