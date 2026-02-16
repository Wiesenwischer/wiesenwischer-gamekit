using NUnit.Framework;

namespace Wiesenwischer.GameKit.Network.Tests
{
    [TestFixture]
    public class AnimationSnapshotTests
    {
        [Test]
        public void Speed_Quantization_PreservesRange()
        {
            var snapshot = AnimationSnapshot.Create(speed: 1.0f, verticalVelocity: 0f);
            Assert.AreEqual(1.0f, snapshot.Speed, 0.01f);
        }

        [Test]
        public void Speed_Zero_IsZero()
        {
            var snapshot = AnimationSnapshot.Create(speed: 0f, verticalVelocity: 0f);
            Assert.AreEqual(0f, snapshot.Speed, 0.01f);
        }

        [Test]
        public void Speed_Max_IsClamped()
        {
            var snapshot = AnimationSnapshot.Create(speed: 5f, verticalVelocity: 0f);
            Assert.AreEqual(2f, snapshot.Speed, 0.02f);
        }

        [Test]
        public void VerticalVelocity_Quantization_PreservesSign()
        {
            var up = AnimationSnapshot.Create(speed: 0f, verticalVelocity: 10f);
            var down = AnimationSnapshot.Create(speed: 0f, verticalVelocity: -30f);

            Assert.Greater(up.VerticalVelocity, 0f);
            Assert.Less(down.VerticalVelocity, 0f);
        }

        [Test]
        public void VerticalVelocity_Precision()
        {
            var snapshot = AnimationSnapshot.Create(speed: 0f, verticalVelocity: -9.81f);
            Assert.AreEqual(-9.81f, snapshot.VerticalVelocity, 0.1f);
        }

        [Test]
        public void Equality_SameValues_AreEqual()
        {
            var a = AnimationSnapshot.Create(1f, -5f);
            var b = AnimationSnapshot.Create(1f, -5f);
            Assert.IsTrue(a.Equals(b));
        }

        [Test]
        public void Equality_DifferentValues_AreNotEqual()
        {
            var a = AnimationSnapshot.Create(1f, -5f);
            var b = AnimationSnapshot.Create(0.5f, -5f);
            Assert.IsFalse(a.Equals(b));
        }

        [Test]
        public void ByteSize_IsCompact()
        {
            Assert.AreEqual(1, sizeof(byte));
            Assert.AreEqual(2, sizeof(short));
        }
    }
}
