using NUnit.Framework;

namespace Wiesenwischer.GameKit.Network.Tests
{
    [TestFixture]
    public class SequenceTests
    {
        [Test]
        public void NewerSequence_IsAccepted()
        {
            Assert.IsFalse(IsSequenceOlder(5, 3));
        }

        [Test]
        public void OlderSequence_IsRejected()
        {
            Assert.IsTrue(IsSequenceOlder(3, 5));
        }

        [Test]
        public void SameSequence_IsNotOlder()
        {
            Assert.IsFalse(IsSequenceOlder(5, 5));
        }

        [Test]
        public void WrapAround_NewerIsAccepted()
        {
            Assert.IsFalse(IsSequenceOlder(1, 65534));
        }

        [Test]
        public void WrapAround_OlderIsRejected()
        {
            Assert.IsTrue(IsSequenceOlder(65534, 1));
        }

        private bool IsSequenceOlder(ushort test, ushort reference)
        {
            return (short)(test - reference) < 0;
        }
    }
}
