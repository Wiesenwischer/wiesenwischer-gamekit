using NUnit.Framework;
using Wiesenwischer.GameKit.CharacterController.Core.Locomotion;

namespace Wiesenwischer.GameKit.CharacterController.Core.Tests
{
    [TestFixture]
    public class OrientationFacingInterfaceTests
    {
        #region FacingMode Default

        [Test]
        public void FacingMode_DefaultValue_IsMovementDirection()
        {
            var mode = default(FacingMode);

            Assert.AreEqual(FacingMode.MovementDirection, mode);
        }

        #endregion

        #region LocomotionInput Backward Compatibility

        [Test]
        public void LocomotionInput_WithoutNewFields_DefaultsToMovementDirection()
        {
            var input = new LocomotionInput
            {
                MoveDirection = new UnityEngine.Vector2(0, 1),
                LookDirection = UnityEngine.Vector3.forward,
                SpeedModifier = 1f,
            };

            Assert.AreEqual(FacingMode.MovementDirection, input.FacingMode);
            Assert.AreEqual(UnityEngine.Vector3.zero, input.FacingDirection);
        }

        [Test]
        public void LocomotionInput_WithFacingMode_RetainsValue()
        {
            var input = new LocomotionInput
            {
                FacingMode = FacingMode.CameraForward,
                FacingDirection = UnityEngine.Vector3.forward,
            };

            Assert.AreEqual(FacingMode.CameraForward, input.FacingMode);
            Assert.AreEqual(UnityEngine.Vector3.forward, input.FacingDirection);
        }

        #endregion
    }
}
