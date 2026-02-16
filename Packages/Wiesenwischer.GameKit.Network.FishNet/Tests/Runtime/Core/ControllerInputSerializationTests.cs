using NUnit.Framework;
using UnityEngine;
using Wiesenwischer.GameKit.CharacterController.Core.Prediction;

namespace Wiesenwischer.GameKit.Network.Tests
{
    [TestFixture]
    public class ControllerInputSerializationTests
    {
        [Test]
        public void ControllerInput_Create_SetsAllFields()
        {
            var input = ControllerInput.Create(
                tick: 42,
                move: new Vector2(0.5f, -0.3f),
                look: new Vector2(1f, 0f),
                rotation: 180f,
                cameraYaw: 180f,
                buttons: ControllerButtons.Jump | ControllerButtons.Sprint);

            Assert.AreEqual(42, input.Tick);
            Assert.AreEqual(0.5f, input.MoveDirection.x, 0.001f);
            Assert.AreEqual(-0.3f, input.MoveDirection.y, 0.001f);
            Assert.AreEqual(180f, input.Rotation, 0.001f);
            Assert.IsTrue(input.Jump);
            Assert.IsTrue(input.Sprint);
        }

        [Test]
        public void ControllerInput_Empty_HasNoButtons()
        {
            var input = ControllerInput.Empty(0);
            Assert.IsFalse(input.Jump);
            Assert.IsFalse(input.Sprint);
            Assert.IsFalse(input.Crouch);
            Assert.AreEqual(Vector2.zero, input.MoveDirection);
        }

        [Test]
        public void ControllerInput_Equality_WorksCorrectly()
        {
            var a = ControllerInput.Create(1, Vector2.up, Vector2.zero, 0f, 0f,
                ControllerButtons.Jump);
            var b = ControllerInput.Create(1, Vector2.up, Vector2.zero, 0f, 0f,
                ControllerButtons.Jump);
            var c = ControllerInput.Create(2, Vector2.up, Vector2.zero, 0f, 0f,
                ControllerButtons.Jump);

            Assert.AreEqual(a, b);
            Assert.AreNotEqual(a, c);
        }
    }
}
