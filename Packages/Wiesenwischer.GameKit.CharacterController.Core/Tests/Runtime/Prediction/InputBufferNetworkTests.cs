using NUnit.Framework;
using System.Linq;
using Wiesenwischer.GameKit.CharacterController.Core.Prediction;

namespace Wiesenwischer.GameKit.CharacterController.Core.Tests
{
    [TestFixture]
    public class InputBufferNetworkTests
    {
        private InputBuffer<ControllerInput> _buffer;

        [SetUp]
        public void SetUp()
        {
            _buffer = new InputBuffer<ControllerInput>(
                capacity: 64,
                tickGetter: input => input.Tick);
        }

        [Test]
        public void GetRange_ReturnsBatchForNetwork()
        {
            for (int i = 0; i < 10; i++)
            {
                _buffer.Add(ControllerInput.Create(
                    tick: i,
                    move: UnityEngine.Vector2.up,
                    look: UnityEngine.Vector2.zero,
                    rotation: 0f,
                    cameraYaw: 0f,
                    buttons: ControllerButtons.None));
            }

            var batch = _buffer.GetRange(3, 7);
            Assert.AreEqual(5, batch.Count);
            Assert.AreEqual(3, batch.First().Tick);
            Assert.AreEqual(7, batch.Last().Tick);
        }

        [Test]
        public void RemoveBefore_CleansUpAcknowledgedTicks()
        {
            for (int i = 0; i < 10; i++)
            {
                _buffer.Add(ControllerInput.Create(
                    tick: i,
                    move: UnityEngine.Vector2.zero,
                    look: UnityEngine.Vector2.zero,
                    rotation: 0f,
                    cameraYaw: 0f,
                    buttons: ControllerButtons.None));
            }

            _buffer.RemoveBefore(5);

            Assert.IsFalse(_buffer.HasTick(4));
            Assert.IsTrue(_buffer.HasTick(5));
            Assert.IsTrue(_buffer.HasTick(9));
        }

        [Test]
        public void ControllerInput_ButtonFlags_WorkCorrectly()
        {
            var input = ControllerInput.Create(
                tick: 1,
                move: UnityEngine.Vector2.zero,
                look: UnityEngine.Vector2.zero,
                rotation: 0f,
                cameraYaw: 0f,
                buttons: ControllerButtons.Jump | ControllerButtons.Sprint);

            Assert.IsTrue(input.Jump);
            Assert.IsTrue(input.Sprint);
            Assert.IsFalse(input.Crouch);
            Assert.IsFalse(input.PrimaryAction);
        }
    }
}
