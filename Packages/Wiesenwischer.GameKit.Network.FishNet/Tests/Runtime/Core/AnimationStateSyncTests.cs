using NUnit.Framework;
using Wiesenwischer.GameKit.CharacterController.Core.Animation;

namespace Wiesenwischer.GameKit.Network.Tests
{
    [TestFixture]
    public class AnimationStateSyncTests
    {
        [Test]
        public void AllStates_FitInByte()
        {
            var states = System.Enum.GetValues(typeof(CharacterAnimationState));
            foreach (CharacterAnimationState state in states)
            {
                int value = (int)state;
                Assert.LessOrEqual(value, 255,
                    $"State {state} ({value}) passt nicht in byte");
            }
        }

        [Test]
        public void ByteRoundTrip_PreservesState()
        {
            foreach (CharacterAnimationState state in
                System.Enum.GetValues(typeof(CharacterAnimationState)))
            {
                byte serialized = (byte)state;
                var deserialized = (CharacterAnimationState)serialized;
                Assert.AreEqual(state, deserialized);
            }
        }

        [Test]
        public void StateCount_IsCorrect()
        {
            var count = System.Enum.GetValues(typeof(CharacterAnimationState)).Length;
            Assert.AreEqual(11, count);
        }
    }
}
